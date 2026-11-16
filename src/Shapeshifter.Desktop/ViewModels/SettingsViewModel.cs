using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Platform;
using Shapeshifter.Core.Settings;

namespace Shapeshifter.Desktop.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsStore settings;
    private readonly IAutostartService autostart;
    private readonly ClipboardHistory history;
    private readonly ILauncher launcher;
    private readonly AppPaths paths;

    [ObservableProperty]
    private int pasteDelayMilliseconds;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCopyNotifications))]
    private bool quietMode;

    [ObservableProperty]
    private int maxHistoryItems;

    [ObservableProperty]
    private bool startAtLogin;

    public SettingsViewModel(
        SettingsStore settings,
        IAutostartService autostart,
        ClipboardHistory history,
        ILauncher launcher,
        AppPaths paths,
        string modifierName,
        IReadOnlyList<string> warnings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(autostart);
        this.settings = settings;
        this.autostart = autostart;
        this.history = history;
        this.launcher = launcher;
        this.paths = paths;
        var current = settings.Current;
        pasteDelayMilliseconds = current.PasteDelayMilliseconds;
        quietMode = current.QuietMode;
        maxHistoryItems = current.MaxHistoryItems;
        startAtLogin = autostart.IsSupported && autostart.IsEnabled;
        DelayDescription = $"How long to hold {modifierName}+V before the history appears";
        Warnings = string.Join(Environment.NewLine + Environment.NewLine, warnings);
        Usage =
            $"Copy as usual. To paste an older item, hold {modifierName} and press V: keep {modifierName} held to see " +
            $"the history, press V again (or ↑/↓) to pick an item, ← to choose what to do with it, Delete to remove it " +
            $"and Esc to cancel. Release {modifierName} to paste.";
    }

    public string Usage { get; }

    /// <summary>The inverse of <see cref="QuietMode"/>, for a switch that reads "Copy notifications: on".</summary>
    public bool ShowCopyNotifications
    {
        get => !QuietMode;
        set => QuietMode = !value;
    }

    /// <summary>"Hold Ctrl+V this long before the history appears".</summary>
    public string DelayDescription { get; private set; } = string.Empty;

    public string Warnings { get; }

    public bool HasWarnings => Warnings.Length > 0;

    public bool IsAutostartSupported => autostart.IsSupported;

    public string Version { get; } = "Version " + (typeof(SettingsViewModel).Assembly.GetName().Version?.ToString(3) ?? "?");

    partial void OnPasteDelayMillisecondsChanged(int value) =>
        settings.Update(current => current with { PasteDelayMilliseconds = value });

    partial void OnQuietModeChanged(bool value) =>
        settings.Update(current => current with { QuietMode = value });

    partial void OnMaxHistoryItemsChanged(int value) =>
        settings.Update(current => current with { MaxHistoryItems = value });

    partial void OnStartAtLoginChanged(bool value)
    {
        if (autostart.IsSupported && autostart.IsEnabled != value)
        {
            autostart.SetEnabled(value);
        }
    }

    [RelayCommand]
    private void ClearHistory() => history.Clear(includePinned: false);

    [RelayCommand]
    private void OpenLogFolder() => launcher.OpenFolder(paths.LogDirectory);
}
