# Contributing to Shapeshifter

Thanks for helping out! Shapeshifter is a small project, and bug reports, ideas and pull requests are all welcome.

## Finding something to work on

- Issues labelled [`good first issue`](https://github.com/ffMathy/Shapeshifter/labels/good%20first%20issue) are small and self-contained, a good way to get to know the code.
- Issues labelled [`help wanted`](https://github.com/ffMathy/Shapeshifter/labels/help%20wanted) are ones we would especially like a hand with.
- For anything bigger, or a change to how Shapeshifter looks or behaves, open an issue (or comment on an existing one) first so we can agree on the approach before you spend time on it.

## Reporting bugs and suggesting features

Use the issue templates. For a bug, include:

- your operating system and version, and on Linux whether you use X11 or Wayland;
- the Shapeshifter version (in Settings);
- the applications you copied from and pasted into;
- the log file, if anything went wrong.

Logs are in `%LOCALAPPDATA%\Shapeshifter\Logs` on Windows, `~/Library/Logs/Shapeshifter` on macOS and `~/.local/share/shapeshifter/logs` (or `$XDG_DATA_HOME/shapeshifter/logs`) on Linux. Remove anything private from them first, because they can contain window titles.

## Building and running

Install the [.NET 11 SDK](https://dotnet.microsoft.com/download), then:

```bash
dotnet build Shapeshifter.slnx
dotnet run --project src/Shapeshifter.Desktop
```

Start with [docs/architecture.md](docs/architecture.md), which explains how the pieces fit together:

| Project | What it holds |
|---|---|
| `src/Shapeshifter.Core` | All behaviour: the clipboard history, the paste gesture, actions, settings. Plain C#, no UI or operating system code. |
| `src/Shapeshifter.Platform` | The thin layer that talks to Windows, macOS and X11. |
| `src/Shapeshifter.Desktop` | The Avalonia application: the picker, the notifications and the settings window. |
| `tests/` | Unit tests, and end-to-end tests that drive the real app. |
| `tools/Shapeshifter.ScreenshotGenerator` | Renders `assets/screenshots` from the real picker. |

## Tests

Every change to behaviour should come with tests. Most logic lives in `Shapeshifter.Core`, where it can be tested with fakes:

```bash
dotnet test --project tests/Shapeshifter.Core.Tests
```

The end-to-end tests start Shapeshifter and a separate target application, copy through the real clipboard and press real keys. They need a desktop session:

- **Linux:** `scripts/run-e2e-linux.sh` runs them under Xvfb with openbox (install `xvfb openbox x11-utils`).
- **Windows:** set `SHAPESHIFTER_E2E=1`, then run `dotnet test --project tests/Shapeshifter.EndToEnd.Tests`. Don't touch the keyboard or mouse while they run.

CI runs the unit tests on Windows, macOS and Linux, and the end-to-end tests on Linux and Windows, including a Remote Desktop session on Windows.

## Changing how the picker looks

The screenshots in `assets/screenshots` (also used on the website) are rendered from the real picker. After a visual change, regenerate them with `scripts/generate-screenshots.sh` and check the result. CI also does this on pull requests and commits changed screenshots to the branch.

## Code style

- The build treats warnings as errors, with the .NET analyzers on, so a clean `dotnet build` is the first check.
- Follow the style of the surrounding code: nullable reference types, file-scoped namespaces, `var`, and XML documentation comments on types and public members that explain *why* rather than *what*.
- Keep operating-system code in `Shapeshifter.Platform` and as thin as possible; put decisions in `Shapeshifter.Core` where they can be tested.
- Write commit messages as a short sentence about what changes for the user ("Show images copied from Firefox as images"), followed by a paragraph explaining why when it isn't obvious.

## Pull requests

- Keep a pull request to one topic, and link the issue it fixes.
- Make sure CI is green. If a test fails, fix the cause rather than skipping or disabling the test.
- Every merge to `master` is released automatically, so `master` should always be ready to ship.

By contributing, you agree that your contributions are licensed under the same license as Shapeshifter (see [License](README.md#license) in the README).
