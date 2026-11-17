using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Serilog;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Engine;
using Shapeshifter.Core.Input;
using Shapeshifter.Core.Previews;
using Shapeshifter.Core.Platform;
using Shapeshifter.Desktop.ViewModels;
using Shapeshifter.Desktop.Views;

namespace Shapeshifter.Desktop.Services;

/// <summary>
/// Shows the picker centred near the top of the screen that contains the pointer, without activating it. Only while the
/// user searches the history does the picker take the keyboard focus, until it is hidden.
/// </summary>
internal sealed class PickerPresenter : IPickerPresenter
{
    /// <summary>Height at which thumbnails of copied images are decoded: sharp on the tiles at 200 % scaling.</summary>
    private const int ThumbnailHeight = 128;

    /// <summary>Height at which the selected image is decoded for the preview pane: sharp there at 200 % scaling.</summary>
    private const int PreviewImageHeight = 360;

    /// <summary>The picker's top edge, as a fraction of the screen's working area below its top.</summary>
    private const double TopFraction = 0.16;

    /// <summary>The history may use this fraction of the working area's height.</summary>
    private const double HeightFraction = 0.62;

    /// <summary>How long the picker may take to get the keyboard focus when a search begins.</summary>
    private static readonly TimeSpan ActivationTimeout = TimeSpan.FromSeconds(1.5);

    private readonly ICursorLocator cursorLocator;
    private readonly IWindowFocus? windowFocus;
    private readonly ILogger logger;
    private readonly PickerVisuals visuals;
    private readonly PickerViewModel viewModel;
    private readonly LinkedImageLoader? linkedImages;
    private readonly Dictionary<Guid, Bitmap?> thumbnails = [];
    private readonly Dictionary<Guid, Bitmap> linkedThumbnails = [];
    private (PickerItemViewModel Item, Bitmap? Bitmap)? previewImage;
    private PickerWindow? window;
    private Func<PickerSearchInput, bool>? searchInput;
    private bool activatedForSearch;

    public PickerPresenter(
        ICursorLocator cursorLocator,
        PickerVisuals visuals,
        string modifierName,
        ILogger logger,
        IWindowFocus? windowFocus = null,
        LinkedImageLoader? linkedImages = null)
    {
        this.linkedImages = linkedImages;
        viewModel = new PickerViewModel(modifierName);
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PickerViewModel.SearchQuery))
            {
                searchInput?.Invoke(PickerSearchInput.Typed(viewModel.SearchQuery));
            }
        };
        this.cursorLocator = cursorLocator;
        this.windowFocus = windowFocus;
        this.visuals = visuals;
        this.logger = logger.ForContext<PickerPresenter>();
    }

    public event EventHandler<bool>? VisibilityChanged;

    public bool IsVisible { get; private set; }

    public int SelectedIndex => viewModel.SelectedIndex;

    /// <summary>Whether the user is searching in the picker (it has the keyboard focus).</summary>
    public bool IsSearching => viewModel.IsSearching;

    public bool HasSearchFocus { get; private set; }

    public Task ShowAsync(PickerViewState state) => Dispatcher.UIThread.InvokeAsync(() =>
    {
        var picker = window ??= CreateWindow();
        SetMaxListHeight(picker);

        // The preview is left out again below when the picker turns out too tall for this screen with it.
        viewModel.ShowsPreview = true;
        Apply(state);
        var now = DateTimeOffset.Now;
        foreach (var item in viewModel.Items)
        {
            item.RefreshCopiedAgo(now);
        }

        picker.Show();

        // Only known once the window exists; without a compositor the window cannot be see-through.
        viewModel.IsOpaque = picker.ActualTransparencyLevel == WindowTransparencyLevel.None;

        // The window sizes itself to its content, so it is placed once its size is known.
        picker.UpdateLayout();
        if (!FitsOnScreen(picker))
        {
            viewModel.ShowsPreview = false;
            UpdatePreviewImage();
            picker.UpdateLayout();
        }

        Position(picker);
        IsVisible = true;
        VisibilityChanged?.Invoke(this, true);
    }).GetTask();

    public Task UpdateAsync(PickerViewState state) => Dispatcher.UIThread.InvokeAsync(() =>
    {
        Apply(state);
        if (window is not null)
        {
            window.UpdateLayout();
            Position(window);
        }

        VisibilityChanged?.Invoke(this, true);
    }).GetTask();

    public Task HideAsync() => Dispatcher.UIThread.InvokeAsync(() =>
    {
        EndSearch();
        window?.Hide();
        IsVisible = false;
        VisibilityChanged?.Invoke(this, false);
    }).GetTask();

    public Task BeginSearchAsync(PickerViewState state, Func<PickerSearchInput, bool> onInput) => Dispatcher.UIThread.InvokeAsync(() =>
    {
        ArgumentNullException.ThrowIfNull(onInput);
        if (window is null || !IsVisible)
        {
            // Hidden in the meantime; there is nothing to search in.
            onInput(new PickerSearchInput(PickerSearchKey.Deactivated));
            return;
        }

        viewModel.SearchQuery = string.Empty;
        viewModel.IsSearching = true;
        Apply(state);
        window.UpdateLayout();
        Position(window);

        searchInput = onInput;
        activatedForSearch = false;
        window.SearchKeyHandler = key => searchInput?.Invoke(new PickerSearchInput(key)) ?? false;

        // The picker is shown without being activated, and another application is in the foreground.
        if (windowFocus is not null && GetNativeHandle(window) is { } handle and not 0)
        {
            windowFocus.ActivateOwnWindow(handle);
        }

        window.Activate();

        // If the system refuses to activate the picker, the keys still go to the other application, where Escape could
        // not close it: give up instead.
        var attempt = onInput;
        DispatcherTimer.RunOnce(
            () =>
            {
                if (ReferenceEquals(searchInput, attempt) && !activatedForSearch)
                {
                    logger.Warning("The picker did not get the keyboard focus for searching");
                    attempt(new PickerSearchInput(PickerSearchKey.Deactivated));
                }
            },
            ActivationTimeout);

        if (!window.FocusSearchBox())
        {
            // The search box is laid out with the next pass.
            Dispatcher.UIThread.Post(() => window.FocusSearchBox(), DispatcherPriority.Loaded);
        }

        VisibilityChanged?.Invoke(this, true);
    }).GetTask();

    private PickerWindow CreateWindow()
    {
        var picker = new PickerWindow { DataContext = viewModel };
        picker.Activated += (_, _) =>
        {
            if (searchInput is not null)
            {
                activatedForSearch = true;
                HasSearchFocus = true;
                picker.FocusSearchBox();
                VisibilityChanged?.Invoke(this, true);
            }
        };
        picker.Deactivated += (_, _) =>
        {
            HasSearchFocus = false;

            // Only once the search had the focus: the user clicked or switched to another window.
            if (searchInput is not null && activatedForSearch)
            {
                searchInput(new PickerSearchInput(PickerSearchKey.Deactivated));
            }
        };
        return picker;
    }

    private void EndSearch()
    {
        searchInput = null;
        activatedForSearch = false;
        HasSearchFocus = false;
        if (window is not null)
        {
            window.SearchKeyHandler = null;
        }

        viewModel.IsSearching = false;
        viewModel.SearchQuery = string.Empty;
    }

    private static nint? GetNativeHandle(Window window) => window.TryGetPlatformHandle() switch
    {
        IMacOSTopLevelPlatformHandle mac => mac.NSWindow,
        { } handle => handle.Handle,
        null => null,
    };

    private void Apply(PickerViewState state)
    {
        var ids = state.Items.Select(item => item.Id).ToArray();
        if (!viewModel.Items.Select(item => item.Id).SequenceEqual(ids)
            || viewModel.Items.Zip(state.Items).Any(pair => pair.First.IsPinned != pair.Second.IsPinned))
        {
            viewModel.Items.Clear();
            foreach (var item in state.Items)
            {
                var row = new PickerItemViewModel(item, GetThumbnail(item), visuals);
                viewModel.Items.Add(row);
                ShowLinkedThumbnail(row);
            }

            foreach (var stale in thumbnails.Keys.Except(ids).ToArray())
            {
                thumbnails[stale]?.Dispose();
                thumbnails.Remove(stale);
            }

            foreach (var stale in linkedThumbnails.Keys.Except(ids).ToArray())
            {
                linkedThumbnails[stale].Dispose();
                linkedThumbnails.Remove(stale);
            }
        }

        if (!viewModel.Actions.Select(action => action.View).SequenceEqual(state.Actions))
        {
            viewModel.Actions.Clear();
            for (var i = 0; i < state.Actions.Count; i++)
            {
                viewModel.Actions.Add(new PickerActionViewModel(state.Actions[i], isDefault: i == 0));
            }
        }

        viewModel.SelectedIndex = state.SelectedIndex;
        UpdatePreviewImage();
        viewModel.UpdateActionsHeader();
        viewModel.SelectedActionIndex = state.SelectedActionIndex;
        viewModel.IsActionsPaneActive = state.Pane == PickerPane.Actions;
        viewModel.IsHeldOpen = state.IsHeldOpen;
    }

    /// <summary>
    /// Decodes the selected image larger for the preview pane. Only the selected item keeps a large image, so the
    /// history never holds more than one full-size image in memory.
    /// </summary>
    private void UpdatePreviewImage()
    {
        var selected = viewModel.ShowsPreview ? viewModel.SelectedItem : null;
        if (previewImage is { } current && ReferenceEquals(current.Item, selected))
        {
            return;
        }

        if (previewImage is { } previous)
        {
            previous.Item.LargeImage = null;
            previous.Bitmap?.Dispose();
            previewImage = null;
        }

        if (selected?.LinkedImage is { } linked && linkedImages is not null)
        {
            previewImage = (selected, null);
            if (linkedImages.TryGetLoaded(linked, out var loaded))
            {
                ShowLinkedImage(selected, loaded);
            }
            else
            {
                _ = LoadLinkedImageAsync(selected, linked);
            }

            return;
        }

        if (selected is null || !selected.IsImageLayout)
        {
            return;
        }

        var png = selected.Image;
        Bitmap? bitmap = null;
        try
        {
            using var stream = new MemoryStream(png!);
            var height = PickerItemViewModel.ReadPngSize(png)?.Height ?? uint.MaxValue;
            bitmap = height <= PreviewImageHeight ? new Bitmap(stream) : Bitmap.DecodeToHeight(stream, PreviewImageHeight);
        }
        catch (Exception exception)
        {
            logger.Debug(exception, "Could not decode the preview of item {Id}", selected.Id);
        }

        selected.LargeImage = bitmap;
        previewImage = (selected, bitmap);
    }

    /// <summary>
    /// Shows the image of a copied link or path on the row's tile (issue #21) when it can be had without asking a web
    /// site: it was loaded before, or it is a file on this computer. A web link's image is only requested once the row
    /// is selected (<see cref="UpdatePreviewImage"/>), so scrolling past links does not contact their sites.
    /// </summary>
    private void ShowLinkedThumbnail(PickerItemViewModel row)
    {
        if (row.LinkedImage is not { } linked || linkedImages is null)
        {
            return;
        }

        if (linkedThumbnails.TryGetValue(row.Id, out var cached))
        {
            row.LinkedThumbnail = cached;
        }
        else if (linkedImages.TryGetLoaded(linked, out var bytes))
        {
            SetLinkedThumbnail(row, bytes);
        }
        else if (linked.FilePath is not null)
        {
            _ = LoadLinkedImageAsync(row, linked);
        }
    }

    /// <summary>Loads the image of a copied link or path, then shows it if the row is still in the picker.</summary>
    private async Task LoadLinkedImageAsync(PickerItemViewModel row, LinkedImage linked)
    {
        var bytes = await linkedImages!.LoadAsync(linked).ConfigureAwait(true);
        if (bytes is null || !viewModel.Items.Contains(row))
        {
            return;
        }

        if (previewImage is { } current && ReferenceEquals(current.Item, row))
        {
            ShowLinkedImage(row, bytes);
        }
        else
        {
            SetLinkedThumbnail(row, bytes);
        }

        // The preview may have grown with the image.
        if (window is not null && IsVisible)
        {
            window.UpdateLayout();
            Position(window);
        }
    }

    /// <summary>Shows the image of the selected copied link in the preview, and on its tile.</summary>
    private void ShowLinkedImage(PickerItemViewModel selected, byte[]? bytes)
    {
        // A file's image may also have been loaded for its tile, so this can be called twice for the same selection.
        if (bytes is null || previewImage is { Bitmap: not null } shown && ReferenceEquals(shown.Item, selected))
        {
            return;
        }

        SetLinkedThumbnail(selected, bytes);
        var bitmap = Decode(bytes, PreviewImageHeight, selected.Id);
        selected.LargeImage = bitmap;
        previewImage = (selected, bitmap);
    }

    private void SetLinkedThumbnail(PickerItemViewModel row, byte[]? bytes)
    {
        if (bytes is null || linkedThumbnails.ContainsKey(row.Id))
        {
            return;
        }

        if (Decode(bytes, ThumbnailHeight, row.Id) is { } bitmap)
        {
            linkedThumbnails[row.Id] = bitmap;
            row.LinkedThumbnail = bitmap;
        }
    }

    /// <summary>Decodes an image (PNG, JPEG, GIF, BMP or WebP) to at most the given height, or null when it cannot be.</summary>
    private Bitmap? Decode(byte[] bytes, int maxHeight, Guid id)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            var bitmap = new Bitmap(stream);

            // Small images are not scaled up.
            if (bitmap.PixelSize.Height <= maxHeight)
            {
                return bitmap;
            }

            using (bitmap)
            {
                var width = Math.Max(1, (int)Math.Round((double)bitmap.PixelSize.Width * maxHeight / bitmap.PixelSize.Height));
                return bitmap.CreateScaledBitmap(new PixelSize(width, maxHeight));
            }
        }
        catch (Exception exception)
        {
            logger.Debug(exception, "Could not decode the linked image of item {Id}", id);
            return null;
        }
    }

    private Bitmap? GetThumbnail(ClipboardItem item)
    {
        if (item.Snapshot.Image is null)
        {
            return null;
        }

        if (!thumbnails.TryGetValue(item.Id, out var bitmap))
        {
            try
            {
                using var stream = new MemoryStream(item.Snapshot.Image);
                bitmap = Bitmap.DecodeToHeight(stream, ThumbnailHeight);
            }
            catch (Exception exception)
            {
                logger.Debug(exception, "Could not decode the thumbnail of item {Id}", item.Id);
                bitmap = null;
            }

            thumbnails[item.Id] = bitmap;
        }

        return bitmap;
    }

    private void SetMaxListHeight(Window picker)
    {
        var screen = FindScreen(picker.Screens);
        if (screen is null)
        {
            viewModel.MaxListHeight = 600;
            return;
        }

        // Whole rows only, so the last visible row is never cut in half.
        var rowHeight = PlatformLook.Resource(Avalonia.Application.Current!, "PickerRowHeight", 64.0);
        var available = screen.WorkingArea.Height / GetScaling(picker, screen) * HeightFraction;
        viewModel.MaxListHeight = Math.Max(1, Math.Floor(available / rowHeight)) * rowHeight;
    }

    /// <summary>Whether the whole picker fits in the height of the working area of its screen.</summary>
    private bool FitsOnScreen(Window picker)
    {
        var screen = FindScreen(picker.Screens);
        return screen is null || picker.Bounds.Height * GetScaling(picker, screen) <= screen.WorkingArea.Height;
    }

    private void Position(Window picker)
    {
        var screen = FindScreen(picker.Screens);
        if (screen is null)
        {
            return;
        }

        var scaling = GetScaling(picker, screen);
        var area = screen.WorkingArea;
        var width = picker.Bounds.Width * scaling;
        var height = picker.Bounds.Height * scaling;

        // Moved up when the picker would otherwise reach below the working area (it is taller with its preview).
        var top = Math.Max(0, Math.Min(area.Height * TopFraction, area.Height - height));
        picker.Position = new PixelPoint(
            area.X + (int)Math.Max(0, (area.Width - width) / 2),
            area.Y + (int)top);
    }

    /// <summary>
    /// Factor between window size units (DIPs) and the units of Screen bounds and Window.Position. On Windows and X11
    /// those are physical pixels, so it is the screen's scaling. On macOS Avalonia positions windows in points
    /// (DesktopScaling is 1) while Screen.Scaling is the Retina backing factor.
    /// </summary>
    private static double GetScaling(Window picker, Screen screen) => OperatingSystem.IsMacOS() ? picker.DesktopScaling : screen.Scaling;

    private Screen? FindScreen(Screens screens)
    {
        Screen? screen = null;
        var cursor = cursorLocator.GetCursorPosition();
        if (cursor is { } position)
        {
            screen = screens.ScreenFromPoint(new PixelPoint(position.X, position.Y));

            // macOS reports the pointer in points rather than pixels.
            screen ??= screens.All.FirstOrDefault(candidate =>
            {
                var bounds = candidate.Bounds;
                var s = candidate.Scaling;
                return new Rect(bounds.X / s, bounds.Y / s, bounds.Width / s, bounds.Height / s).Contains(new Point(position.X, position.Y));
            });
        }

        return screen ?? screens.Primary ?? (screens.All.Count > 0 ? screens.All[0] : null);
    }
}
