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
/// Shows the picker in the top-left corner of the screen that contains the pointer, without activating it.
/// </summary>
internal sealed class PickerPresenter : IPickerPresenter
{
    private const double ItemHeight = 92;

    private readonly ICursorLocator cursorLocator;
    private readonly ILogger logger;
    private readonly PickerVisuals visuals;
    private readonly PickerViewModel viewModel = new();
    private readonly Dictionary<Guid, Bitmap?> thumbnails = [];
    private PickerWindow? window;

    public PickerPresenter(ICursorLocator cursorLocator, PickerVisuals visuals, ILogger logger)
    {
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
        var now = DateTimeOffset.Now;
        foreach (var item in viewModel.Items)
        {
            item.RefreshCopiedAgo(now);
        }

        var picker = window ??= new PickerWindow { DataContext = viewModel };
        Position(picker);
        picker.Show();

        // Only known once the window exists; without a compositor the window cannot be see-through.
        viewModel.ActionsFillHeight = picker.ActualTransparencyLevel == WindowTransparencyLevel.None;
        IsVisible = true;
        VisibilityChanged?.Invoke(this, true);
    }).GetTask();

    public Task UpdateAsync(PickerViewState state) => Dispatcher.UIThread.InvokeAsync(() =>
    {
        Apply(state);
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

        if (!viewModel.Actions.SequenceEqual(state.Actions))
        {
            viewModel.Actions.Clear();
            foreach (var action in state.Actions)
            {
                viewModel.Actions.Add(action);
            }
        }

        viewModel.SelectedIndex = state.SelectedIndex;
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
                bitmap = Bitmap.DecodeToHeight(stream, (int)ItemHeight * 3);
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

    private void Position(Window picker)
    {
        // The window sizes itself to its content (SizeToContent), so only the history needs a limit.
        var screen = FindScreen(picker.Screens);
        if (screen is null)
        {
            viewModel.MaxListHeight = 600;
            return;
        }

        // Factor between window size units (DIPs) and the units of Screen bounds and Window.Position.
        // On Windows and X11 those are physical pixels, so it is the screen's scaling. On macOS Avalonia
        // positions windows in points (DesktopScaling is 1) while Screen.Scaling is the Retina backing
        // factor, so using the latter would make the picker twice as tall as the screen allows on Retina displays.
        var scaling = OperatingSystem.IsMacOS() ? picker.DesktopScaling : screen.Scaling;
        var area = screen.WorkingArea;
        var rows = Math.Max(1, Math.Floor(area.Height / scaling / ItemHeight));
        viewModel.MaxListHeight = rows * ItemHeight;
        picker.Position = new PixelPoint(area.X, area.Y);
    }

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
