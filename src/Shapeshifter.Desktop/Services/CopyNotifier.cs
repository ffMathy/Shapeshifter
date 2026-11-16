using Avalonia;
using Avalonia.Threading;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Engine;
using Shapeshifter.Core.Settings;
using Shapeshifter.Desktop.Views;

namespace Shapeshifter.Desktop.Services;

/// <summary>
/// Shows a small, non-activating "copied" notification for 1.5 seconds where the platform shows its own: bottom right
/// on Windows, top right under the menu bar on macOS, bottom centre on Linux (the look's <c>ToastPlacement</c>).
/// </summary>
internal sealed class CopyNotifier(SettingsStore settings) : ICopyNotifier
{
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(1.5);

    /// <summary>Distance from the edges of the working area; the looks' shadow margins add to it.</summary>
    private const int Margin = 8;
    private ToastWindow? window;
    private IDisposable? hideTimer;

    public void NotifyCopied(ClipboardItem item, int historyCount)
    {
        if (settings.Current.QuietMode)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            var toast = window ??= CreateWindow();
            toast.SetContent(historyCount, item.PreviewText);
            toast.Show();
            toast.UpdateLayout();
            Position(toast);

            hideTimer?.Dispose();
            hideTimer = DispatcherTimer.RunOnce(() => toast.Hide(), Duration);
        });
    }

    private static ToastWindow CreateWindow()
    {
        var toast = new ToastWindow();

        // The window sizes itself to its content, which is only final after layout: place it again once it is.
        toast.SizeChanged += (_, _) => Position(toast);
        return toast;
    }

    private static void Position(ToastWindow toast)
    {
        var screen = toast.Screens.Primary ?? (toast.Screens.All.Count > 0 ? toast.Screens.All[0] : null);
        if (screen is null)
        {
            return;
        }

        // As in PickerPresenter: on macOS windows are positioned in points, not in backing pixels.
        var scaling = OperatingSystem.IsMacOS() ? toast.DesktopScaling : screen.Scaling;
        var placement = PlatformLook.Resource(Application.Current!, "ToastPlacement", "BottomRight");
        toast.Position = Place(screen.WorkingArea, toast.Bounds.Size * scaling, (int)(Margin * scaling), placement);
    }

    internal static PixelPoint Place(PixelRect area, Size size, int margin, string placement) => placement switch
    {
        "TopRight" => new PixelPoint(area.Right - (int)size.Width - margin, area.Y + margin),
        "BottomCenter" => new PixelPoint(area.X + (int)((area.Width - size.Width) / 2), area.Bottom - (int)size.Height - margin),
        _ => new PixelPoint(area.Right - (int)size.Width - margin, area.Bottom - (int)size.Height - margin),
    };
}
