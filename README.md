# Shapeshifter

[![CI](https://github.com/ffMathy/Shapeshifter/actions/workflows/ci.yml/badge.svg)](https://github.com/ffMathy/Shapeshifter/actions/workflows/ci.yml)
[![Website](https://github.com/ffMathy/Shapeshifter/actions/workflows/website.yml/badge.svg)](https://github.com/ffMathy/Shapeshifter/actions/workflows/website.yml)
[![Join the chat at https://gitter.im/ffMathy/Shapeshifter](https://badges.gitter.im/ffMathy/Shapeshifter.svg)](https://gitter.im/ffMathy/Shapeshifter?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Financial Contributors on Open Collective](https://opencollective.com/shapeshifterclipboard/all/badge.svg?label=financial+contributors)](https://opencollective.com/shapeshifterclipboard)

Shapeshifter is a clipboard manager that lets you copy multiple items and choose which one to paste later, without losing any of them. It runs on **Windows, macOS and Linux** and is built with .NET 11 and [Avalonia](https://avaloniaui.net).

<b>To get Shapeshifter or to see more information, go to the website:</b> [https://shapeshifter.app](https://shapeshifter.app). Builds for every platform are attached to the [GitHub releases](https://github.com/ffMathy/Shapeshifter/releases) and to every CI run.

Shapeshifter looks at home on each platform: Fluent 2 on Windows, Liquid Glass on macOS, and GNOME's libadwaita on Linux. Each look follows your system accent colour.

| Windows | macOS | Linux |
|---|---|---|
| ![The picker on Windows](assets/screenshots/windows.PNG) | ![The picker on macOS](assets/screenshots/macos.PNG) | ![The picker on Linux](assets/screenshots/linux.PNG) |

## How it works

Copy as usual. Shapeshifter remembers what you copied, including every clipboard format (rich text, HTML, images, files and application-specific data), so an item is restored exactly when you paste it.

| You do | Shapeshifter does |
|---|---|
| <kbd>Ctrl</kbd>+<kbd>V</kbd> (tap) | Pastes normally. |
| Hold <kbd>Ctrl</kbd>, press <kbd>V</kbd> and keep holding <kbd>Ctrl</kbd> | Shows your clipboard history after a short delay (300 ms by default). |
| Press <kbd>V</kbd> again, or <kbd>↑</kbd>/<kbd>↓</kbd> | Selects the next (older) or previous item. |
| <kbd>←</kbd> / <kbd>→</kbd> | Switches between the items and the actions for the selected item. |
| <kbd>Delete</kbd> | Removes the selected item from the history. |
| <kbd>Esc</kbd> | Cancels; nothing is pasted. |
| Release <kbd>Ctrl</kbd> | Pastes the selected item (or performs the selected action). |

On macOS, use <kbd>Cmd</kbd> instead of <kbd>Ctrl</kbd>. Shortcuts that add another modifier, such as <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>V</kbd> in terminals or <kbd>AltGr</kbd>+<kbd>V</kbd>, are left to the application.

Shapeshifter does not record clipboard content that password managers mark as sensitive (Windows `ExcludeClipboardContentFromMonitorProcessing` / `CanIncludeInClipboardHistory`, macOS `org.nspasteboard.ConcealedType` and related types, KDE `x-kde-passwordManagerHint`).

Actions available for an item, depending on its content: **Paste**, **Copy as file** (saves text to a `.txt` file), **Paste as plain text**, **Copy as compressed folder** (zips copied files), **Copy image from link**, **Open links**, and **Pin to clipboard / Unpin from clipboard** (pinned items are kept across restarts).

Settings (tray icon → Settings) include the delay before the history appears, the number of items to keep, copy notifications and starting at login.

## Platform support

| Platform | Status | Notes |
|---|---|---|
| Windows 10/11 (x64, Arm64) | Supported | No special permissions needed. |
| macOS (Apple silicon, Intel) | Supported | Grant **Accessibility** access (System Settings → Privacy & Security → Accessibility). macOS requires it for any app that observes or sends keystrokes. |
| Linux, X11 session (x64, Arm64) | Supported | Works on any X11 desktop. A system tray (StatusNotifierItem or XEmbed) is optional. |
| Linux, Wayland session | Limited | Wayland does not let applications observe or send global keystrokes. Shapeshifter runs through XWayland, so the paste gesture only works while an X11 application is focused. Log in to an X11 session for full support. |

Where data lives:

| | Settings | Pinned items and logs |
|---|---|---|
| Windows | `%APPDATA%\Shapeshifter` | `%LOCALAPPDATA%\Shapeshifter` |
| macOS | `~/Library/Application Support/Shapeshifter` | same, logs in `~/Library/Logs/Shapeshifter` |
| Linux | `$XDG_CONFIG_HOME/shapeshifter` | `$XDG_DATA_HOME/shapeshifter` |

## Building from source

Install the [.NET 11 SDK](https://dotnet.microsoft.com/download), then:

```bash
dotnet build Shapeshifter.slnx
dotnet run --project src/Shapeshifter.Desktop
```

To produce a self-contained, single-file build for a platform (the output folder holds only the executable; debug symbols are embedded) (`win-x64`, `win-arm64`, `osx-arm64`, `osx-x64`, `linux-x64`, `linux-arm64`):

```bash
dotnet publish src/Shapeshifter.Desktop -c Release -r linux-x64 -o publish
```

### Screenshots

The screenshots in `assets/screenshots` (also used by the website) are rendered from the real picker of each look with sample content. To run the app in another platform's look, set `SHAPESHIFTER_LOOK` to `windows`, `macos` or `linux`. After changing how the picker looks, regenerate them with:

```bash
scripts/generate-screenshots.sh
```

CI does the same on every pull request and commits changed screenshots to the pull request's branch.

### Tests

```bash
dotnet test --project tests/Shapeshifter.Core.Tests
```

The end-to-end tests start Shapeshifter and a separate target application, copy through the real operating system clipboard and paste with real keystrokes. They need a desktop session:

```bash
# Linux (starts Xvfb and openbox when no display is available)
scripts/run-e2e-linux.sh

# Windows, or any machine with a desktop session
SHAPESHIFTER_E2E=1 dotnet test --project tests/Shapeshifter.EndToEnd.Tests
```

On macOS the terminal running the tests needs Accessibility access.

### Website

The website, [shapeshifter.app](https://shapeshifter.app), is the static site in `website/`: plain HTML, CSS and a little JavaScript, with no build step. The [Website workflow](.github/workflows/website.yml) deploys it to GitHub Pages whenever it, the screenshots or the app icons change on `master`. At build time the workflow copies `assets/screenshots` and the app icons into the site, renders each look's settings window and notification with the screenshot generator, checks that every local link and image exists (`scripts/check-website.py`) and deploys the result, so the site always shows the current app.

To preview it locally (renders the settings and notification screenshots too when the .NET SDK is installed):

```bash
website/preview.sh    # then open http://localhost:8000
```

### Project layout

| Project | Purpose |
|---|---|
| `src/Shapeshifter.Core` | Platform-independent logic: clipboard history, the paste gesture state machine, actions, persistence, settings. |
| `src/Shapeshifter.Platform` | Operating system integration: global keyboard hook and paste simulation ([SharpHook](https://github.com/TolikPylypchuk/SharpHook)), X11 key grabs and clipboard notifications, autostart. |
| `src/Shapeshifter.Desktop` | The Avalonia application: picker, notifications, settings, tray icon and clipboard access. |
| `website/` | The static website, [shapeshifter.app](https://shapeshifter.app) (see [Website](#website)). |
| `tests/` | Unit tests and the end-to-end suite. |

See [docs/architecture.md](docs/architecture.md) for how the pieces fit together.

### Releasing

Every merge to `master` is released (merges that land while a release is being built are released together). CI builds and tests the merge, publishes all six platforms and creates a GitHub release with the archives attached and release notes generated from the merged pull requests.

The version is worked out by `scripts/next-version.sh`: the newest `v*` tag with its patch number increased (v7.1.0 → v7.1.1), or the `<Version>` in `Directory.Build.props` when that is newer. To release a new minor or major version, raise `<Version>` in the pull request (for example to `7.2.0` or `8.0.0`).

For a prerelease, push a tag with a suffix instead:

```bash
git tag v7.2.0-beta.1
git push origin v7.2.0-beta.1
```

You can also start the CI workflow manually on `master` (Actions → CI → Run workflow) with **release_tag** set to a version, such as `v7.2.0`.

# Contributing
Feel free to contribute! Create a pull request, and we'll look at it.

# Can Shapeshifter be trusted?
Shapeshifter can be fully trusted, and hopefully having it 100% open-source helps showing that. Shapeshifter does not send your clipboard data anywhere. Pinned items are stored only on your own computer, in the folders listed above.

## Contributors

### Code Contributors

This project exists thanks to all the people who contribute. [[Contribute](CONTRIBUTING.md)].
<a href="https://github.com/ffMathy/Shapeshifter/graphs/contributors"><img src="https://opencollective.com/shapeshifterclipboard/contributors.svg?width=890&button=false" /></a>

### Financial Contributors

Become a financial contributor and help us sustain our community. [[Contribute](https://opencollective.com/shapeshifterclipboard/contribute)]

#### Individuals

<a href="https://opencollective.com/shapeshifterclipboard"><img src="https://opencollective.com/shapeshifterclipboard/individuals.svg?width=890"></a>

#### Organizations

Support this project with your organization. Your logo will show up here with a link to your website. [[Contribute](https://opencollective.com/shapeshifterclipboard/contribute)]

<a href="https://opencollective.com/shapeshifterclipboard/organization/0/website"><img src="https://opencollective.com/shapeshifterclipboard/organization/0/avatar.svg"></a>
<a href="https://opencollective.com/shapeshifterclipboard/organization/1/website"><img src="https://opencollective.com/shapeshifterclipboard/organization/1/avatar.svg"></a>
<a href="https://opencollective.com/shapeshifterclipboard/organization/2/website"><img src="https://opencollective.com/shapeshifterclipboard/organization/2/avatar.svg"></a>
<a href="https://opencollective.com/shapeshifterclipboard/organization/3/website"><img src="https://opencollective.com/shapeshifterclipboard/organization/3/avatar.svg"></a>
<a href="https://opencollective.com/shapeshifterclipboard/organization/4/website"><img src="https://opencollective.com/shapeshifterclipboard/organization/4/avatar.svg"></a>
<a href="https://opencollective.com/shapeshifterclipboard/organization/5/website"><img src="https://opencollective.com/shapeshifterclipboard/organization/5/avatar.svg"></a>
<a href="https://opencollective.com/shapeshifterclipboard/organization/6/website"><img src="https://opencollective.com/shapeshifterclipboard/organization/6/avatar.svg"></a>
<a href="https://opencollective.com/shapeshifterclipboard/organization/7/website"><img src="https://opencollective.com/shapeshifterclipboard/organization/7/avatar.svg"></a>
<a href="https://opencollective.com/shapeshifterclipboard/organization/8/website"><img src="https://opencollective.com/shapeshifterclipboard/organization/8/avatar.svg"></a>
<a href="https://opencollective.com/shapeshifterclipboard/organization/9/website"><img src="https://opencollective.com/shapeshifterclipboard/organization/9/avatar.svg"></a>

# License
Shapeshifter is licensed under the
[Creative Commons Attribution NonCommercial](https://tldrlegal.com/license/creative-commons-attribution-noncommercial-(cc-nc)) license, which means that it's free to use
for personal use.
If you want to use Shapeshifter for commercial use, feel free to contact us.
