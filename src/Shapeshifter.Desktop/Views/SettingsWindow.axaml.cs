using Avalonia.Controls;
using Shapeshifter.Core.Localization;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Shapeshifter.Desktop.Services;
using Shapeshifter.Desktop.ViewModels;

namespace Shapeshifter.Desktop.Views;

public partial class SettingsWindow : Window
{
    private IDisposable? copiedTimer;

    public SettingsWindow()
    {
        InitializeComponent();
        if (Localizer.IsRightToLeft)
        {
            FlowDirection = Avalonia.Media.FlowDirection.RightToLeft;
        }

        NativeMaterial.PrepareBackdrop(this);
    }

    /// <summary>Copies the version, operating system and architecture, ready to paste into a bug report.</summary>
    private async void OnCopyVersion(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel viewModel || Clipboard is not { } clipboard)
        {
            return;
        }

        try
        {
            await clipboard.SetTextAsync(viewModel.EnvironmentDescription).ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            Serilog.Log.Warning(exception, "Could not copy the version");
            return;
        }

        CopyVersionLink.Content = Strings.SettingsCopied;
        copiedTimer?.Dispose();
        copiedTimer = DispatcherTimer.RunOnce(() => CopyVersionLink.Content = Strings.SettingsCopyVersion, TimeSpan.FromSeconds(2));
    }
}
