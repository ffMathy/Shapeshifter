# Where the data lives

Everything Shapeshifter keeps stays in the user's own folders, in each
platform's usual places, and is private to the user.

```ail
node DataLocations : data   level: system
  why   People must be able to find, back up or delete what a clipboard manager keeps,
        and other users of the computer must not read it.   <- App
  what
    type AppPaths = { ConfigDirectory: text, DataDirectory: text, LogDirectory: text, TempDirectory: text }
    AppPaths.CreateDefault() -> AppPaths
    AppPaths.ForRoot(root: text) -> AppPaths
    AppPaths.EnsureCreated() -> nothing
    the files and folders an AppPaths derives, each a path as text:
      SettingsFile = Config/settings.json;  PinnedDirectory = Data/Pinned;  HistoryDirectory = Data/History
      InstanceLockFile = Data/instance.lock;  InstanceInfoFile = Data/instance.json;  install.json = Data/install.json
      logs: Log/shapeshifter-<yyyyMMdd>.log, one file a day, the last 7 kept; Debug and up in the file, Information and up on the console
    per platform (CreateDefault, for home H)
      SHAPESHIFTER_HOME=R set => ForRoot(R): R/config, R/data, R/logs, R/temp (for tests and the end-to-end runner)
      Windows => config %APPDATA%\Shapeshifter; data %LOCALAPPDATA%\Shapeshifter; logs %LOCALAPPDATA%\Shapeshifter\Logs; temp %TEMP%\Shapeshifter
      macOS => config and data H/Library/Application Support/Shapeshifter; logs H/Library/Logs/Shapeshifter; temp $TMPDIR/Shapeshifter
      Linux => config $XDG_CONFIG_HOME/shapeshifter (H/.config when unset); data $XDG_DATA_HOME/shapeshifter (H/.local/share);
        logs data/logs; temp $XDG_RUNTIME_DIR/shapeshifter when that folder exists, else data/temp
    examples
      Linux, H=/home/u, no XDG variables, no runtime folder => { ConfigDirectory: "/home/u/.config/shapeshifter", DataDirectory: "/home/u/.local/share/shapeshifter", LogDirectory: "/home/u/.local/share/shapeshifter/logs", TempDirectory: "/home/u/.local/share/shapeshifter/temp" }
      Linux, XDG_RUNTIME_DIR=/run/user/1000 (exists) => temp /run/user/1000/shapeshifter
      ForRoot("/tmp/s").SettingsFile => "/tmp/s/config/settings.json"
      driven, Linux, throwaway HOME and XDG_* => .config/shapeshifter (0700) and .local/share/shapeshifter (0700) holding install.json, instance.json, instance.lock, logs/, temp/
    on Unix every folder is created with mode 0700, since they hold clipboard contents; Windows uses its per-user folders as they are
    the temp folder is emptied at every start by deleting and re-creating it, which re-creates it with the default
      mode (driven: 0755 inside the 0700 data folder); actions put files there (text files, zip archives)
    --uninstall removes none of these; nothing is ever sent elsewhere, except an image the user chooses to upload
  how
    decide follow each platform's conventions (XDG on Linux, Application Support on macOS, AppData on Windows)
      because "unknown, confirm with the owner"
    decide on Linux, temp files go to the per-user runtime folder rather than /tmp
      because /tmp is shared and the files contain copied text (AppPaths)
    decide only pinned items are on disk unless "Remember history after restart" is on
      because some people chose Shapeshifter because it does not store clipboard contents on disk (#459)
    decide the temp folder is re-created without the 0700 mode
      because "unknown, confirm with the owner: keep this? its parent is private, but $XDG_RUNTIME_DIR/shapeshifter is not"
  sources
    README.md, "Where your data lives"; issue #459
    src/Shapeshifter.Core/Settings/AppPaths.cs; src/Shapeshifter.Desktop/Program.cs (the log); src/Shapeshifter.Desktop/App.axaml.cs, CleanTemporaryFiles
    driven on Linux 2026-11-20 in a throwaway HOME (.ail/app/install): the folders the installed copy created
```
