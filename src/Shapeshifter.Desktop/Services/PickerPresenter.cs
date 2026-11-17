using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Serilog;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Engine;
using Shapeshifter.Core.Input;
using Shapeshifter.Core.Platform;
using Shapeshifter.Desktop.ViewModels;
using Shapeshifter.Desktop.Views;

namespace Shapeshifter.Desktop.Services;

/// <summary>
/// Shows the picker centred near the top of the screen that contains the pointer, without activating it.
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

    private readonly ICursorLocator cursorLocator;
    private readonly ILogger logger;
    private readonly PickerVisuals visuals;
    private readonly PickerViewModel viewModel;
    private readonly Dictionary<Guid, Bitmap?> thumbnails = [];
    private (PickerItemViewModel Item, Bitmap? Bitmap)? previewImage;
    private PickerWindow? window;

    public PickerPresenter(ICursorLocator cursorLocator, PickerVisuals visuals, string modifierName, ILogger logger)
    {
        viewModel = new PickerViewModel(modifierName);
        this.cursorLocator = cursorLocator;
        this.visuals = visuals;
        this.logger = logger.ForContext<PickerPresenter>();
    }

    public event EventHandler<bool>? VisibilityChanged;

    public bool IsVisible { get; private set; }

    public int SelectedIndex => viewModel.SelectedIndex;

    public Task ShowAsync(PickerViewState state) => Dispatcher.UIThread.InvokeAsync(() =>
    {
        var picker = window ??= new PickerWindow { DataContext = viewModel };
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
        window?.Hide();
        IsVisible = false;
        VisibilityChanged?.Invoke(this, false);
    }).GetTask();

    private void Apply(PickerViewState state)
    {
        var ids = state.Items.Select(item => item.Id).ToArray();
        if (!viewModel.Items.Select(item => item.Id).SequenceEqual(ids)
            || viewModel.Items.Zip(state.Items).Any(pair => pair.First.IsPinned != pair.Second.IsPinned))
        {
            viewModel.Items.Clear();
            foreach (var item in state.Items)
            {
                viewModel.Items.Add(new PickerItemViewModel(item, GetThumbnail(item), visuals));
            }

            foreach (var stale in thumbnails.Keys.Except(ids).ToArray())
            {
                thumbnails[stale]?.Dispose();
                thumbnails.Remove(stale);
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
