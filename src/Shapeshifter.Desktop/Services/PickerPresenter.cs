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

/// <summary>Shows the picker on the screen that contains the pointer, without activating it.</summary>
internal sealed class PickerPresenter : IPickerPresenter
{
    private const double ItemHeight = 92;
    private const double ChromeHeight = 26;

    private readonly ICursorLocator cursorLocator;
    private readonly ILogger logger;
    private readonly PickerViewModel viewModel = new();
    private readonly Dictionary<Guid, Bitmap?> thumbnails = [];
    private PickerWindow? window;

    public PickerPresenter(ICursorLocator cursorLocator, string modifierName, ILogger logger)
    {
        this.cursorLocator = cursorLocator;
        this.logger = logger.ForContext<PickerPresenter>();
        viewModel.Hint = $"V or ↑↓: choose · ←→: actions · Delete: remove · Esc: cancel · release {modifierName} to paste";
    }

    public event EventHandler<bool>? VisibilityChanged;

    public bool IsVisible { get; private set; }

    public int SelectedIndex => viewModel.SelectedIndex;

    public Task ShowAsync(PickerViewState state) => Dispatcher.UIThread.InvokeAsync(() =>
    {
        Apply(state);
        var picker = window ??= new PickerWindow { DataContext = viewModel };
        Position(picker, state.Items.Count);
        picker.Show();
        IsVisible = true;
        VisibilityChanged?.Invoke(this, true);
    }).GetTask();

    public Task UpdateAsync(PickerViewState state) => Dispatcher.UIThread.InvokeAsync(() =>
    {
        var countChanged = state.Items.Count != viewModel.Items.Count;
        Apply(state);
        if (countChanged && window is not null)
        {
            Position(window, state.Items.Count);
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
                viewModel.Items.Add(new PickerItemViewModel(item, GetThumbnail(item)));
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
                bitmap = Bitmap.DecodeToHeight(stream, (int)ItemHeight * 2);
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

    private void Position(Window picker, int itemCount)
    {
        var screen = FindScreen(picker.Screens);
        if (screen is null)
        {
            picker.Height = Math.Min(itemCount * ItemHeight + ChromeHeight, 600);
            return;
        }

        var scaling = screen.Scaling;
        var area = screen.WorkingArea;
        var maxHeight = area.Height / scaling * 0.8;
        var height = Math.Min((Math.Max(itemCount, 1) * ItemHeight) + ChromeHeight, maxHeight);
        picker.Height = height;
        var width = picker.Width;
        var x = area.X + (int)((area.Width - (width * scaling)) / 2);
        var y = area.Y + (int)((area.Height - (height * scaling)) / 3);
        picker.Position = new PixelPoint(x, y);
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
