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

    [ObservableProperty]
    private int pasteDelayMilliseconds;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCopyNotifications))]
    private bool quietMode;

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
        string modifierName,
        IReadOnlyList<string> warnings,
        Updater? updater = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(autostart);
        this.settings = settings;
        this.autostart = autostart;
        this.history = history;
        this.launcher = launcher;
        this.paths = paths;
        this.updater = updater;
        var current = settings.Current;
        pasteDelayMilliseconds = current.PasteDelayMilliseconds;
        quietMode = current.QuietMode;
        maxHistoryItems = current.MaxHistoryItems;
        rememberHistory = current.RememberHistory;
        startAtLogin = autostart.IsSupported && autostart.IsEnabled;
        automaticUpdates = current.AutomaticUpdates;
        if (updater is not null)
        {
            updater.StateChanged += OnUpdaterStateChanged;
        }
        var modifier = Localizer.ModifierKey(modifierName);
        DelayDescription = Localizer.Format(Strings.SettingsDelayDescription, modifier);
        Warnings = string.Join(Environment.NewLine + Environment.NewLine, warnings);
        Usage = Localizer.Format(Strings.SettingsUsage, modifier);
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

    partial void OnQuietModeChanged(bool value) =>
        settings.Update(current => current with { QuietMode = value });

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
