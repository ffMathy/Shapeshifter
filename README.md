# Shapeshifter

[![CI](https://github.com/ffMathy/Shapeshifter/actions/workflows/ci.yml/badge.svg)](https://github.com/ffMathy/Shapeshifter/actions/workflows/ci.yml)
[![Website](https://github.com/ffMathy/Shapeshifter/actions/workflows/website.yml/badge.svg)](https://github.com/ffMathy/Shapeshifter/actions/workflows/website.yml)
[![Join the chat at https://gitter.im/ffMathy/Shapeshifter](https://badges.gitter.im/ffMathy/Shapeshifter.svg)](https://gitter.im/ffMathy/Shapeshifter?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Financial Contributors on Open Collective](https://opencollective.com/shapeshifterclipboard/all/badge.svg?label=financial+contributors)](https://opencollective.com/shapeshifterclipboard)

<p align="center"><img src="assets/screenshots/cover@2x.PNG" alt="Shapeshifter: copy many things, paste any of them. A free, open-source clipboard manager for Windows, macOS and Linux, shown next to its picker with five copied items"></p>

<p align="center"><b>Copy many things. Paste any of them.</b><br>A clipboard manager for Windows, macOS and Linux.</p>

<p align="center"><a href="https://shapeshifter.app"><b>shapeshifter.app</b></a> · <a href="https://github.com/ffMathy/Shapeshifter/releases">Download</a></p>

## Hold <kbd>Ctrl</kbd>+<kbd>V</kbd>

Tap it and it pastes as always, even while you keep <kbd>Ctrl</kbd> held to paste again. Keep holding both and your history appears. Then, with <kbd>Ctrl</kbd> still held, press <kbd>V</kbd> to move and release to paste.

| Pick an item | <kbd>→</kbd> to choose what to do with it |
|---|---|
| ![The history, with the newest item selected](assets/screenshots/windows.PNG) | ![The actions for the selected text, with "Paste as plain text" chosen](assets/screenshots/actions.PNG) |

<kbd>1</kbd>–<kbd>9</kbd> jump to that item, <kbd>Delete</kbd> removes an item, <kbd>Esc</kbd> cancels. To look around without holding <kbd>Ctrl</kbd>, press <kbd>Space</kbd>: the history stays open until you press <kbd>Enter</kbd> to paste or <kbd>Esc</kbd>. On macOS it's <kbd>Cmd</kbd>+<kbd>V</kbd>. In Settings you can choose another shortcut instead: <kbd>Win</kbd>+<kbd>V</kbd> on Windows, in place of the Windows clipboard history, or <kbd>Ctrl</kbd>+<kbd>V</kbd> on macOS, leaving the usual paste keys alone.

Looking for something you copied a while ago? Press <kbd>S</kbd> while the history is open and type a few words:
<kbd>Enter</kbd> pastes what you found.

## At home on every platform

| Windows | macOS | Linux |
|---|---|---|
| ![The picker on Windows](assets/screenshots/windows.PNG) | ![The picker on macOS](assets/screenshots/macos.PNG) | ![The picker on Linux](assets/screenshots/linux.PNG) |
| ![Settings on Windows](assets/screenshots/settings-windows.PNG) | ![Settings on macOS](assets/screenshots/settings-macos.PNG) | ![Settings on Linux](assets/screenshots/settings-linux.PNG) |
| ![Copy notification on Windows](assets/screenshots/toast-windows.PNG) | ![Copy notification on macOS](assets/screenshots/toast-macos.PNG) | ![Copy notification on Linux](assets/screenshots/toast-linux.PNG) |

## In your accent colour

| | | | |
|---|---|---|---|
| ![Blue accent](assets/screenshots/blue.PNG) | ![Green accent](assets/screenshots/green.PNG) | ![Purple accent](assets/screenshots/purple.PNG) | ![Red accent](assets/screenshots/red.PNG) |

## Good to know

- **Everything is kept:** rich text, HTML, images, files and app-specific formats are restored exactly.
- **Passwords are skipped:** content that password managers mark as sensitive is never recorded.
- **Pinned items** survive restarts; turn on *Remember history after restart* in Settings to keep the rest of the
  history too.
- **Nothing you copy leaves your computer**, unless you choose **Upload and paste link** for an image. That one
  image is uploaded to [Catbox](https://catbox.moe), a free image host without ads, and a public link to it is pasted.
- **Installs and updates itself:** the first start installs Shapeshifter for your user (no administrator rights) and
  starts it at login. It checks GitHub for new releases every few hours, verifies the download's SHA-256 and restarts
  into it when you're not using it. Switch this off in Settings; on Windows, uninstall it from Apps or Control Panel.
  Starting a newer version while an older one runs closes the older one.
- **In your language:** Shapeshifter is translated into 21 languages and follows your system's language.
- **Something wrong?** *Report a problem…* in the tray menu opens a GitHub issue with your version and system filled in.
- **macOS** needs Accessibility access. **Linux** works fully on X11; on Wayland only while an X11 app is focused.

<details>
<summary>Where your data lives</summary>

| | Settings | Pinned items, saved history and logs |
|---|---|---|
| Windows | `%APPDATA%\Shapeshifter` | `%LOCALAPPDATA%\Shapeshifter` |
| macOS | `~/Library/Application Support/Shapeshifter` | same, logs in `~/Library/Logs/Shapeshifter` |
| Linux | `$XDG_CONFIG_HOME/shapeshifter` | `$XDG_DATA_HOME/shapeshifter` |

</details>

<details>
<summary>Where Shapeshifter installs itself</summary>

| | Installed to | Also |
|---|---|---|
| Windows | `%LOCALAPPDATA%\Programs\Shapeshifter` | Start menu shortcut, entry in Apps and Control Panel |
| macOS | `~/Applications/Shapeshifter.app` | |
| Linux | `~/.local/bin/shapeshifter` | desktop entry in `~/.local/share/applications` |

Run it with `--no-install` (or set `SHAPESHIFTER_NO_INSTALL=1`) to keep a copy where it is, `--uninstall` to remove the
installation (your settings and pinned items stay), and set `SHAPESHIFTER_NO_UPDATE=1` to never check for updates.

</details>

# Contributing
Feel free to contribute! See [CONTRIBUTING.md](CONTRIBUTING.md) for how to build, test and send a pull request, [docs/architecture.md](docs/architecture.md) for how the pieces fit together, and look for issues labelled [`good first issue`](https://github.com/ffMathy/Shapeshifter/labels/good%20first%20issue) to get started.

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
