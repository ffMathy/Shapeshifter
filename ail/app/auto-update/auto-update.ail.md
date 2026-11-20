# Updating itself

Described from the code and PR #627; no real release was downloaded.

```ail
node AutoUpdate : fn   level: system
  why   Most people never look for a new version; fixes only reach them if the app
        fetches them itself, safely and without interrupting their work.   <- App
  what
    Updater(app: AppIdentity, running: RunningApp, layout: InstallLayout or nothing, http: HttpClient, log: IUpdateLog, isEnabled: () -> boolean, current: ReleaseVersion or nothing = nothing, runtimeIdentifier: text or nothing = nothing) -> Updater
    Updater.Start(firstDelay: duration, interval: duration) -> nothing
    Updater.CheckNowAsync() -> later boolean
    Updater.Restart(arguments: list of text) -> nothing
    type UpdateState = "Idle" | "Unsupported" | "Disabled" | "Checking" | "UpToDate" | "Downloading" | "ReadyToRestart" | "Failed"
    Updater.State: UpdateState
    Updater.StatusText: text   — the description of the "Automatic updates" row
    Updater.IsSupported: boolean
    Updater.PendingVersion: ReleaseVersion or nothing
    GitHubReleases.GetLatestAsync() -> later (ReleaseInfo or nothing)
    GitHubReleases.GetSha256Async(release: ReleaseInfo, asset: ReleaseAsset) -> later (text or nothing)   — the hash in hex
    The schedule (App.StartUpdater): first check 1 minute after start, then every 6 hours, while "Automatic updates" is on;
      a check while one runs does nothing; after ReadyToRestart no more checks
    Updating is unsupported in these cases, checked in this order (the reason is the row's description and the switch is greyed):
      SHAPESHIFTER_NO_UPDATE=1 => "Updates are turned off (SHAPESHIFTER_NO_UPDATE)"
      not a single-file build => "Only release builds update themselves"
      a pull request build => "Pull request builds do not update themselves"
      the folder of the file (or the .app) is not writable => "Shapeshifter cannot write to {folder}; update it the way it was installed"
    A check:
      GET https://api.github.com/repos/ffMathy/Shapeshifter/releases/latest (Accept application/vnd.github+json, X-GitHub-Api-Version 2022-11-28),
        30 s timeout; 404 => up to date; drafts and tags that are not versions are ignored; prereleases never come from /latest
      release.Version ≤ current => UpToDate
      else the asset "Shapeshifter-<rid>" ("Shapeshifter-win-x64.exe", "Shapeshifter-linux-arm64", …; on macOS from a bundle,
        the bundle asset when releases have one); none => Failed "Version {v} has no build for {rid} ({asset})"
      the expected SHA-256: the asset's GitHub digest "sha256:<64 hex>", else its line in SHA256SUMS.txt; neither => Failed, nothing downloaded
      download next to the running file under a temporary name (1 hour limit), hashing as it goes; a size or hash mismatch => Failed, file deleted
      made executable, then swapped in: rename over the file on Linux and macOS (the running process keeps its copy),
        on Windows the old file is renamed ".old-…" and removed later; install.json gets the new version when installed
      then ReadyToRestart, and UpdateReady is raised
    Restarting (App.RestartWhenIdle): when neither the picker nor the settings window is open, start the new file with
      --replaces <this pid> and quit; otherwise try again every 15 s. A failed start leaves the new version for the next start.
    The status texts:
      Idle/UpToDate/Disabled with the switch off => "Automatic updates are off";  Idle => "Updates are checked automatically"
      Checking => "Checking for updates…";  UpToDate => "Up to date (checked 14:05)";  Downloading => "Downloading version 7.2.0…"
      ReadyToRestart => "Version 7.2.0 is installed and starts the next time Shapeshifter does";  Failed => "Could not update: {reason}"
    examples
      GitHubReleases.Sha256FromDigest("sha256:" + 64 hex digits in lower case) => the same digits in upper case
      Sha256FromDigest("sha512:…") => nothing;  Sha256FromDigest("sha256:abc") => nothing
      FindChecksum("<64 hex>  Shapeshifter-linux-x64\n", "Shapeshifter-linux-x64") => the hex, upper case;  "<hex> *name" also matches
      RunningApp.RuntimeIdentifier(Windows, Arm64) => "win-arm64";  (MacOS, X64) => "osx-x64";  (Linux, X64) => "linux-x64"
      driven, SHAPESHIFTER_NO_UPDATE=1: StatusText => "Updates are turned off (SHAPESHIFTER_NO_UPDATE)", the switch greyed and on
  how
    decide verify every download against a published SHA-256 and refuse a release without one
      because "unknown, confirm with the owner: PR #627 states the rule, not the threat it guards against"
    decide restart only when no window of the app is in use
      because "unknown, confirm with the owner: commit 608e752 says 'once no window of the app is in use', not why"
    decide pull request builds and builds from source never update
      because builds from source must stay where they are (RunningApp); for pull request builds "unknown, confirm with the owner" (PR #635)
    decide check every 6 hours, the first a minute after start
      because the first check must not compete with starting up; "unknown, confirm with the owner: why 6 hours"
    decide latest stable releases only
      because "unknown, confirm with the owner: prereleases were updated to in the WPF version (#467, #540)"
  sources
    PR #627; commits 608e752, e9b8fc1, cf3fd75; issues #283, #346, #456, #467, #540, #590
    src/Shapeshifter.Core/Updates/Updater.cs, GitHubReleases.cs, FileSwap.cs, AppIdentity.cs
    src/Shapeshifter.Desktop/App.axaml.cs, StartUpdater, CheckForUpdates, RestartWhenIdle
    src/Shapeshifter.Desktop/Services/Installation.cs, ExecutableAssetName
    not driven: no real release was downloaded; SHAPESHIFTER_NO_UPDATE=1 in every driven run
```
