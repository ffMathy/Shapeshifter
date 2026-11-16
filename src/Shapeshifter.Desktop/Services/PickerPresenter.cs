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

    /// <summary>The picker's top edge, as a fraction of the screen's working area below its top.</summary>
    private const double TopFraction = 0.16;

    /// <summary>The history may use this fraction of the working area's height.</summary>
    private const double HeightFraction = 0.62;

    private readonly ICursorLocator cursorLocator;
    private readonly ILogger logger;
    private readonly PickerVisuals visuals;
    private readonly PickerViewModel viewModel;
    private readonly Dictionary<Guid, Bitmap?> thumbnails = [];
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
        Apply(state);
        var picker = window ??= new PickerWindow { DataContext = viewModel };
        SetMaxListHeight(picker);
        picker.Show();

        // Only known once the window exists; without a compositor the window cannot be see-through.
        viewModel.IsOpaque = picker.ActualTransparencyLevel == WindowTransparencyLevel.None;

        // The window sizes itself to its content, so it is placed once its size is known.
        picker.UpdateLayout();
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
        viewModel.UpdateActionsHeader();
        viewModel.SelectedActionIndex = state.SelectedActionIndex;
        viewModel.IsActionsPaneActive = state.Pane == PickerPane.Actions;
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
        picker.Position = new PixelPoint(
            area.X + (int)Math.Max(0, (area.Width - width) / 2),
            area.Y + (int)(area.Height * TopFraction));
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
