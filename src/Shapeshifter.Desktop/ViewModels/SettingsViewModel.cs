using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shapeshifter.Core.Clipboard;
using Shapeshifter.Core.Platform;
using Shapeshifter.Core.Settings;
using Shapeshifter.Core.Support;
using Shapeshifter.Core.Updates;
using Shapeshifter.Desktop.Services;
using Shapeshifter.Core.Localization;

namespace Shapeshifter.Desktop.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsStore settings;
    private readonly IAutostartService autostart;
    private readonly ClipboardHistory history;
    private readonly ILauncher launcher;
    private readonly AppPaths paths;
    private readonly Updater? updater;
    private readonly Func<string> modifierName;

    [ObservableProperty]
    private int pasteDelayMilliseconds;

    /// <summary>The index of the chosen <see cref="Core.Settings.PasteShortcut"/> in <see cref="PasteShortcutOptions"/>.</summary>
    [ObservableProperty]
    private int pasteShortcutIndex;

    /// <summary>"Hold Ctrl+V this long before the history appears".</summary>
    [ObservableProperty]
    private string delayDescription = string.Empty;

    [ObservableProperty]
    private string usage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCopyNotifications))]
    private bool quietMode;

    [ObservableProperty]
    private bool coachingTips;

    [ObservableProperty]
    private int maxHistoryItems;

    [ObservableProperty]
    private bool rememberHistory;

    [ObservableProperty]
    private bool startAtLogin;

    [ObservableProperty]
    private bool automaticUpdates;

    public SettingsViewModel(
        SettingsStore settings,
        IAutostartService autostart,
        ClipboardHistory history,
        ILauncher launcher,
        AppPaths paths,
        Func<string> modifierName,
        IReadOnlyList<string> warnings,
        Updater? updater = null,
        IReadOnlyList<string>? shortcutModifierNames = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(autostart);
        ArgumentNullException.ThrowIfNull(modifierName);
        this.modifierName = modifierName;
        shortcutModifierNames ??= [];
        CanChoosePasteShortcut = shortcutModifierNames.Count > 1;
        PasteShortcutOptions = shortcutModifierNames.Select(name => Localizer.ModifierKey(name) + "+V").ToArray();

        // Win+V needs explaining on Windows (it replaces the clipboard history); Ctrl+V on macOS leaves Cmd+V alone.
        PasteShortcutDescription = shortcutModifierNames is [_, "Win"]
            ? Strings.SettingsShortcutDescription
            : Strings.SettingsShortcutDescriptionMac;
        this.settings = settings;
        this.autostart = autostart;
        this.history = history;
        this.launcher = launcher;
        this.paths = paths;
        this.updater = updater;
        var current = settings.Current;
        pasteDelayMilliseconds = current.PasteDelayMilliseconds;
        pasteShortcutIndex = (int)current.PasteShortcut;
        quietMode = current.QuietMode;
        coachingTips = current.CoachingTips;
        maxHistoryItems = current.MaxHistoryItems;
        rememberHistory = current.RememberHistory;
        startAtLogin = autostart.IsSupported && autostart.IsEnabled;
        automaticUpdates = current.AutomaticUpdates;
        if (updater is not null)
        {
            updater.StateChanged += OnUpdaterStateChanged;
        }
        Warnings = string.Join(Environment.NewLine + Environment.NewLine, warnings);
        UpdateModifierTexts();
    }

    /// <summary>Whether the user can choose the shortcut: Ctrl+V or Win+V on Windows, Cmd+V or Ctrl+V on macOS.</summary>
    public bool CanChoosePasteShortcut { get; }

    /// <summary>The choices of the shortcut, in the order of <see cref="Core.Settings.PasteShortcut"/>.</summary>
    public IReadOnlyList<string> PasteShortcutOptions { get; }

    /// <summary>What choosing the alternative shortcut does on this platform.</summary>
    public string PasteShortcutDescription { get; }

    /// <summary>The inverse of <see cref="QuietMode"/>, for a switch that reads "Copy notifications: on".</summary>
    public bool ShowCopyNotifications
    {
        get => !QuietMode;
        set => QuietMode = !value;
    }

    public string Warnings { get; }

    public bool HasWarnings => Warnings.Length > 0;

    public bool IsAutostartSupported => autostart.IsSupported;

    public string Version { get; } = Localizer.Format(Strings.SettingsVersion, typeof(SettingsViewModel).Assembly.GetName().Version?.ToString(3) ?? "?");

    /// <summary>The version, operating system and architecture, as "Copy version" puts them on the clipboard.</summary>
    public string EnvironmentDescription { get; } = ProblemReport.DescribeCurrentEnvironment(Installation.App.Name);

    /// <summary>What the updater is doing, or why this copy cannot update itself.</summary>
    public string UpdateStatus => updater?.StatusText ?? Strings.UpdateAutomatic;

    /// <summary>Whether this copy can download and install updates (single-file release builds in a writable folder).</summary>
    public bool CanUpdate => updater?.IsSupported == true;

    /// <summary>Stops following the updater; called when the window closes.</summary>
    public void Detach()
    {
        if (updater is not null)
        {
            updater.StateChanged -= OnUpdaterStateChanged;
        }
    }

    private void OnUpdaterStateChanged(object? sender, EventArgs e) =>
        Avalonia.Threading.Dispatcher.UIThread.Post(() => OnPropertyChanged(nameof(UpdateStatus)));

    partial void OnAutomaticUpdatesChanged(bool value)
    {
        settings.Update(current => current with { AutomaticUpdates = value });
        if (value && updater is { IsSupported: true, State: UpdateState.Idle or UpdateState.Disabled })
        {
            _ = updater.CheckNowAsync();
        }
        else
        {
            OnPropertyChanged(nameof(UpdateStatus));
        }
    }

    partial void OnPasteDelayMillisecondsChanged(int value) =>
        settings.Update(current => current with { PasteDelayMilliseconds = value });

    partial void OnPasteShortcutIndexChanged(int value)
    {
        settings.Update(current => current with { PasteShortcut = (PasteShortcut)value });

        // The keyboard hook follows the setting as it is saved, and now names the other modifier.
        UpdateModifierTexts();
    }

    private void UpdateModifierTexts()
    {
        var modifier = Localizer.ModifierKey(modifierName());
        DelayDescription = Localizer.Format(Strings.SettingsDelayDescription, modifier);
        Usage = Localizer.Format(Strings.SettingsUsage, modifier) + Localizer.Format(Strings.SettingsUsageHoldOpen, modifier);
    }

    partial void OnQuietModeChanged(bool value) =>
        settings.Update(current => current with { QuietMode = value });

    partial void OnCoachingTipsChanged(bool value) =>
        settings.Update(current => current with { CoachingTips = value });

    partial void OnMaxHistoryItemsChanged(int value) =>
        settings.Update(current => current with { MaxHistoryItems = value });

    partial void OnRememberHistoryChanged(bool value) =>
        settings.Update(current => current with { RememberHistory = value });

    partial void OnStartAtLoginChanged(bool value)
    {
        if (autostart.IsSupported && autostart.IsEnabled != value)
        {
            autostart.SetEnabled(value);
        }
    }

    [RelayCommand]
    private void ClearHistory() => history.Clear(includePinned: false);

    /// <summary>Checks GitHub now; builds that cannot update themselves open the releases page instead.</summary>
    [RelayCommand]
    private void CheckForUpdates()
    {
        if (updater is { IsSupported: true })
        {
            _ = updater.CheckNowAsync();
        }
        else
        {
            launcher.OpenUrl(Installation.App.ReleasesPage);
        }
    }

    [RelayCommand]
    private void OpenLogFolder() => launcher.OpenFolder(paths.LogDirectory);

    /// <summary>Opens a new GitHub issue with the version and operating system filled in.</summary>
    [RelayCommand]
    private void ReportProblem() => launcher.OpenUrl(ProblemReport.CreateIssueUrl(Installation.App, EnvironmentDescription));

    [RelayCommand]
    private void OpenProjectPage() => launcher.OpenUrl(Installation.App.ProjectPage);
}
