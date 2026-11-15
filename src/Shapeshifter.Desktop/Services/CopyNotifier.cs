using Avalonia;
using Avalonia.Threading;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Engine;
using Shapeshifter.Core.Settings;
using Shapeshifter.Desktop.Views;

namespace Shapeshifter.Desktop.Services;

/// <summary>Shows a small, non-activating "copied" toast in the bottom-right corner for 1.5 seconds.</summary>
internal sealed class CopyNotifier(SettingsStore settings) : ICopyNotifier
{
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(1.5);
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
            var toast = window ??= new ToastWindow();
            toast.SetContent(historyCount, item.PreviewText);
            toast.Show();
            var screen = toast.Screens.Primary ?? (toast.Screens.All.Count > 0 ? toast.Screens.All[0] : null);
            if (screen is not null)
            {
                var size = toast.Bounds.Size * screen.Scaling;
                var margin = (int)(40 * screen.Scaling);
                toast.Position = new PixelPoint(
                    screen.WorkingArea.Right - (int)size.Width - margin,
                    screen.WorkingArea.Bottom - (int)size.Height - margin);
            }

            hideTimer?.Dispose();
            hideTimer = DispatcherTimer.RunOnce(() => toast.Hide(), Duration);
        });
    }
}
