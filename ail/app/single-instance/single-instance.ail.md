# One copy per user

Two copies would both grab Ctrl+V and both record every copy. So one runs, and
a newer one takes over from an older one.

```ail
node SingleInstance : fn   level: system
  why   Two running copies fight over the paste keys; and after a download or an
        update, the newest copy must be the one that runs.   <- App
  what
    type Lease = { Release() -> nothing }
      held while the app runs and released on exit (below)
    SingleInstance.Acquire(paths: AppPaths, layout: InstallLayout, version: ReleaseVersion) -> Lease or nothing — nothing: exit
    type RunningInstance = { ProcessId: integer, Version: ReleaseVersion, ExecutablePath: text }
    RunningInstance.ShouldReplace(launching: ReleaseVersion, running: RunningInstance or nothing) -> boolean — running is not nothing and launching > running.Version
    RunningInstance.Read(path: text) -> RunningInstance or nothing
    RunningInstance.Write(path: text) -> nothing
    RunningInstance.Remove(path: text, processId: integer) -> nothing
    ReleaseVersion.TryParse(text: text or nothing) -> ReleaseVersion or nothing — ordered like semantic versions
    the files it keeps, in the data folder (DataLocations):
      instance.lock   held open with no sharing and deleted on close; holds the process id
      instance.json   {"pid":14039,"version":"7.1.0","path":"/home/u/.local/bin/shapeshifter"}
    what happens in Program.Main, after the installer and before the app starts:
      the lock is free => take it, write instance.json, run
      the lock is held => find the running copy: instance.json when its process still runs that executable
        (or one named like it: Windows may run an updated copy as "….old-…"), else, for releases that do not
        write instance.json, a process running the installed executable, with the version from install.json
      ShouldReplace(this version, running) => kill the running process, wait up to 10 s for it to exit and up to
        5 s for the lock, then run; anything fails => exit
      otherwise => log and print "Shapeshifter is already running." to standard error and exit with code 1
      on exit the lease removes instance.json only when it still names this process, then releases the lock
    examples
      ShouldReplace(7.2.0, running 7.1.0) => true
      ShouldReplace(7.1.0, running 7.1.0) => false;  ShouldReplace(7.1.0, running 7.2.0) => false;  ShouldReplace(7.2.0, nothing) => false
      ShouldReplace(7.1.17-pr.635.98, running 7.1.16) => true;  ShouldReplace(7.1.17, running 7.1.17-pr.635.98) => true
      ReleaseVersion.TryParse("v7.2.0-beta.1+build") => 7.2.0-beta.1;  TryParse("7.2") => 7.2.0;  TryParse("7") => nothing
      7.2.0-beta.2 < 7.2.0-beta.10 => true (numeric parts compare as numbers);  7.2.0-beta < 7.2.0 => true
      driven: installed 7.1.0 runs, start it again => "Shapeshifter is already running.", exit code 1
      driven: 7.1.0 runs with --no-install, start 7.2.0 with --no-install => "Shapeshifter 7.1.0 is running (process 14478); ending it to run this newer copy, 7.2.0", then 7.2.0 runs
      driven: 7.2.0 runs, start 7.1.0 with --no-install => exit code 1
  how
    decide a newer copy ends the older one instead of exiting
      because a newer download or a pull request build must take over from whichever copy runs (PR #635)
    decide the older copy is killed, not asked to quit
      because Shapeshifter has no channel to ask a running copy to quit; pinned items are on disk, and the unpinned
        history is kept only with "Remember history after restart"
    decide the same version never replaces itself
      because "unknown, confirm with the owner: the running copy stays and the new one exits, as before PR #635"
    decide one lock per user, in the user's data folder
      because more than one copy could be started (#46, "Not a singleton application")
  sources
    issue #46 "More instances of the exe could be started"; commit cf3fd75; PR #635
    driven on Linux 2026-11-20 with single-file builds 7.1.0 and 7.2.0 (published locally), HOME and XDG_* in a throwaway folder
    src/Shapeshifter.Desktop/SingleInstance.cs; src/Shapeshifter.Desktop/Program.cs
    src/Shapeshifter.Core/Updates/RunningInstance.cs and ReleaseVersion.cs
```
