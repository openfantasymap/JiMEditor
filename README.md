# JiME Scenario Editor

A scenario and campaign editor for the *Journeys in Middle-earth* board game. You build custom
adventures in it: map tiles, tokens, events, enemies, objectives, triggers and story text. The
[Your Journey](https://github.com/openfantasymap/your-journey) companion app then plays them at the
table.

The editor runs on **Windows, macOS (Apple Silicon and Intel) and Linux**. It is built with
[Avalonia UI](https://avaloniaui.net/) 12 on .NET 10.

Originally created by [GlowPuff](https://github.com/GlowPuff/JiMEditor) as a Windows-only WPF app.
This fork ports it to Avalonia so it runs everywhere, keeping the same screens and the same file format.

> **Status:** early alpha. Documentation for the editor's features is in the
> [original wiki](https://github.com/GlowPuff/JiMEditor/wiki), and there is a
> [demonstration video](https://www.youtube.com/watch?v=J5u6YwjxIgU).

---

## Download & install

Get the latest build from the [Releases page](https://github.com/openfantasymap/JiMEditor/releases).
The builds are self-contained, so no .NET install is needed.

| Platform | File | How to run |
|---|---|---|
| Windows 10/11 (x64) | `JiME-Editor-windows-x64.zip` | Unzip, run `JiME.exe` |
| macOS 14+ Apple Silicon (M1–M4) | `JiME-Editor-macos-arm64.zip` | Unzip, move **JiME Editor** to *Applications* |
| macOS 14+ Intel | `JiME-Editor-macos-x64.zip` | Same as above |
| Linux x64 | `JiME-Editor-linux-x64.tar.gz` | `tar xzf …`, run `./JiME` |

**macOS first launch:** the app is ad-hoc signed but not notarized, so Gatekeeper blocks the first
launch. Either right-click the app, choose *Open*, then *Open*, or clear the quarantine flag once:

```bash
xattr -dr "com.apple.quarantine" "/Applications/JiME Editor.app"
```

## Where your files live

The editor and the companion app share one project folder, **`Your Journey`**, inside your Documents folder:

| OS | Folder |
|---|---|
| Windows | `%USERPROFILE%\Documents\Your Journey` |
| macOS | `~/Documents/Your Journey` |
| Linux | `$XDG_DOCUMENTS_DIR/Your Journey`, default `~/Documents/Your Journey` |

To use a different folder, set the `JIME_DATA_DIR` environment variable to its full path. The
companion app reads the same variable.

```
Your Journey/
├── My Scenario.jime              standalone scenarios
└── <campaign-guid>/              one folder per campaign
    ├── <campaign-guid>.json      campaign metadata (name, story, scenario order, campaign triggers)
    ├── Chapter 1.jime …          the campaign's scenarios
    └── <Campaign Name>.zip       package created with "Create Package", to share or to play
```

To play a campaign, copy its `.zip` into the `Your Journey` folder on the machine running the
companion app. The app unpacks it on startup.

## Using the editor

1. **Start screen:** create a *New Campaign* or a *New Standalone Scenario*, or open a recent project.
2. **Main window:**
   - Sidebars list the scenario's **Objectives**, **Events** and **Triggers**. Use them to add,
     remove and edit entries.
   - The centre shows the scenario's **Tile Blocks** (chapters).
   - The toolbar holds the scenario settings (threat, resolutions, intro text, rewards) and
     save/open.
3. **Tile Editor:**
   - Place up to 5 hex tiles per block, from a list or from the tile gallery.
   - Drag tiles to move them. **Page Up/Down** rotates the selected tile, **Delete** (or
     **Backspace** on a Mac) removes it.
   - Double-click a tile to open the **Token Editor**, where you place Search, Person, Threat and
     Darkness tokens and link them to events.
4. **Events:** text, stat tests, decisions, story branches, enemy threats, dialogs, multi-events,
   persistent/conditional events, token replacement and rewards. Each has its own editor.
5. **Campaigns:** order scenarios, set campaign triggers, then **Create Package** to share.

### Keyboard shortcuts (main window)

| Action | Windows / Linux | macOS |
|---|---|---|
| New project / open project | Ctrl+N / Ctrl+O | ⌘N / ⌘O |
| Save / Save As | Ctrl+S / Ctrl+Alt+S | ⌘S / ⌘⌥S |
| New objective / event / trigger / tile block | Alt+O / Alt+E / Alt+T / Alt+C | ⌥O / ⌥E / ⌥T / ⌥C |
| Scenario settings | Alt+S | ⌥S |
| Exit | Alt+X | ⌥X (or ⌘Q) |

---

## How it works

```
JiMEditor/
├── src/JiME.Core/        UI-free model and file format (net10.0 class library)
│   ├── Models/           Scenario, Chapter, HexTile, Token, Trigger, Objective, Threat, Monster, Campaign
│   │   └── Interaction Events/   one class per event type (Text, Test, Decision, Branch, Threat, …)
│   ├── Common/
│   │   ├── FileManager.cs        .jime (de)serialization, project listing, campaign loading
│   │   ├── AppPaths.cs           the shared "Your Journey" folder on every OS
│   │   ├── Vector.cs             2D vector that keeps WPF's "x,y" JSON format
│   │   └── …Converter.cs         polymorphic JSON for events and tiles
│   └── Assets/*.json     tile ids, hex tile shapes (A/B sides), default enemy stats
├── src/JiME/             Avalonia desktop app
│   ├── App.axaml, Styles/        theme (dark Fluent + the original editor's styles)
│   ├── ProjectWindow, MainWindow start screen and main editor
│   ├── Views/                    all editor windows (tiles, tokens, events, campaign, …)
│   ├── Canvas/                   drawing of hex tiles and tokens on the editor canvases
│   ├── Common/                   converters, dialogs (message box, file pickers), tile artwork cache
│   └── Assets/                   icons and tile artwork (TilesA/, TilesB/)
├── tests/JiME.Tests/     xUnit tests: file format, project folder, headless UI smoke tests
└── build/macos/          Info.plist template for the macOS .app bundle
```

* **File format.** A `.jime` file is the JSON form of `FileManager`: scenario settings plus lists of
  interactions, triggers, objectives, resolutions, threats and chapters. Chapters contain hex tiles,
  and hex tiles contain tokens. Events and tiles are polymorphic, keyed by `interactionType` and
  `tileType`. Positions are written as `"x,y"` strings, which is what the WPF editor produced and
  what the companion app parses. The tests pin this shape down.
* **Compatibility.** Scenarios made with the old Windows editor (format 1.9) open unchanged, and files
  saved by this version load in the old editor and in the companion app.

## Developing

You need the **.NET 10 SDK**. For editing, JetBrains Rider or VS Code with the Avalonia extension give
XAML previews.

```bash
dotnet run --project src/JiME              # run the editor
dotnet test tests/JiME.Tests               # run the tests
JIME_SCREENSHOTS=./screenshots dotnet test tests/JiME.Tests   # also save a PNG of every window
```

Or build and test without installing .NET, in Docker:

```bash
docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 \
  bash -c "apt-get update -qq && apt-get install -y -qq libfontconfig1 >/dev/null && dotnet test tests/JiME.Tests"
```

Self-contained builds:

```bash
dotnet publish src/JiME -c Release -r osx-arm64 --self-contained -o publish   # or win-x64, linux-x64, osx-x64
```

## Building on GitHub

`.github/workflows/build.yml` does the following:

| Trigger | Jobs |
|---|---|
| any push or pull request | **build + tests**. Screenshots of every window are uploaded as the `window-screenshots` artifact |
| push to `master`, manual run | then **self-contained builds**: Windows x64, Linux x64 (Ubuntu runner), macOS arm64 and x64 as signed `.app` bundles (macOS runner) |
| tag `v*` (e.g. `v0.19.0`) | then a **GitHub Release** with all four downloads. Tags with a hyphen (`v0.20.0-beta.1`) are pre-releases |

No secrets are needed. To release:

```bash
git tag v0.19.0
git push origin v0.19.0
```

The tag (without the `v`) becomes the app version shown in the editor's status bar.

## Related

* **Companion app:** [openfantasymap/your-journey](https://github.com/openfantasymap/your-journey)
* Original project: [GlowPuff/JiMEditor](https://github.com/GlowPuff/JiMEditor),
  [BoardGameGeek thread](https://boardgamegeek.com/thread/2488415/custom-scenario-editor-and-companion-app-create-yo)

## License

See [LICENSE](LICENSE). *Journeys in Middle-earth* is a trademark of Fantasy Flight Games. This is
an unofficial fan project.
