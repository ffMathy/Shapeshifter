using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Shapeshifter.Desktop.Services;

/// <summary>
/// Asks, the first time, whether images may be uploaded to an online service. Uploading is the one thing that sends
/// clipboard content off the computer, so it never happens without this "yes".
/// </summary>
internal static class UploadConsentDialog
{
    public static Task<bool> AskAsync(string serviceName, string modifierName, CancellationToken cancellationToken) =>
        Dispatcher.UIThread.InvokeAsync(() => ShowAsync(serviceName, modifierName, cancellationToken));

    private static async Task<bool> ShowAsync(string serviceName, string modifierName, CancellationToken cancellationToken)
    {
        var result = new TaskCompletionSource<bool>();
        var (window, upload, cancel) = Create(serviceName, modifierName);
        upload.Click += (_, _) =>
        {
            result.TrySetResult(true);
            window.Close();
        };
        cancel.Click += (_, _) => window.Close();
        window.Closed += (_, _) => result.TrySetResult(false);
        await using var registration = cancellationToken.Register(() => Dispatcher.UIThread.Post(window.Close)).ConfigureAwait(true);

        window.Show();
        window.Activate();
        return await result.Task.ConfigureAwait(true);
    }

    internal static (Window Window, Button Upload, Button Cancel) Create(string serviceName, string modifierName)
    {
        var upload = new Button { Content = $"Upload to {serviceName}", IsDefault = true };
        var cancel = new Button { Content = "Cancel", IsCancel = true };
        var window = new Window
        {
            Title = $"Upload to {serviceName}?",
            Width = 460,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            Topmost = true,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Icon = new WindowIcon(AssetLoader.Open(PlatformLook.Asset("Application.png"))),
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = $"Upload this image to {serviceName}?",
                        FontWeight = FontWeight.SemiBold,
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new TextBlock
                    {
                        Text = $"The image is sent to {serviceName} (catbox.moe), a free image host without ads, and anyone with "
                            + "the link can see it. Uploads can't be deleted. Nothing else you copy ever leaves your computer.",
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new TextBlock
                    {
                        Text = $"Shapeshifter won't ask again. This time the link is copied: press {modifierName}+V to paste it.",
                        TextWrapping = TextWrapping.Wrap,
                        Opacity = 0.7,
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { cancel, upload },
                    },
                },
            },
        };
        NativeMaterial.PrepareBackdrop(window);
        return (window, upload, cancel);
    }
}
