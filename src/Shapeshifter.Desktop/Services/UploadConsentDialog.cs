using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using Shapeshifter.Core.Localization;

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
        var upload = new Button { Content = Localizer.Format(Strings.UploadConsentUpload, serviceName), IsDefault = true };
        var cancel = new Button { Content = Strings.DialogCancel, IsCancel = true };
        var window = new Window
        {
            Title = Localizer.Format(Strings.UploadConsentTitle, serviceName),
            FlowDirection = Localizer.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
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
                        Text = Localizer.Format(Strings.UploadConsentQuestion, serviceName),
                        FontWeight = FontWeight.SemiBold,
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new TextBlock
                    {
                        Text = Localizer.Format(Strings.UploadConsentExplanation, serviceName),
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new TextBlock
                    {
                        Text = Localizer.Format(Strings.UploadConsentAskOnce, Localizer.ModifierKey(modifierName)),
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
