# Live Speaker Teleprompter

Professional teleprompter for multi-screen presentations. Single portable executable, no installation required.  
Full integration with **Bitfocus Companion**, **NDI** and **OSC** for remote control in live production.

> **Version 2.3.3** — Self-contained .NET 8 / WPF — Windows 10/11 x64 — Italian/English — Smooth and stable

---

## Table of contents

- [Features](#features)
- [Language and localization](#language-and-localization)
- [System requirements](#system-requirements)
- [Installation and launch](#installation-and-launch)
- [Keyboard shortcuts](#keyboard-shortcuts)
- [External display management](#external-display-management)
- [NDI streaming](#ndi-streaming)
- [Remote control — HTTP REST API](#remote-control--http-rest-api)
- [Remote control — OSC](#remote-control--osc)
- [Bitfocus Companion integration](#bitfocus-companion-integration)
- [Supported file formats](#supported-file-formats)
- [User preferences](#user-preferences)
- [Build from source](#build-from-source)
- [Project architecture](#project-architecture)
- [License](#license)

---

## Features

### Editor and presentation
- **Rich text editor** with bold, italic, underline, alignment and quick font size selection (36–120 pt).
- **Native `.rstp` format** (XamlPackage) preserving formatting, colors, font and style. Also supports RTF, plain text and subtitles.
- **Vsync-aligned automatic scrolling** (CompositionTarget.Rendering) with delta-time compensation. Speed adjustable from **-80** to **+80** in 0.5 steps.
- **Draggable guide arrow** with customizable size, color and margin. Synced to the presenter view.
- **Edit / presentation mode** — quick toggle that locks the keyboard to prevent accidental changes during live use.
- **Drag & drop** of text files, RTF, Word (.docx) and subtitles directly into the window.

### Multi-screen
- **Real-time display detection** with triple redundancy: Win32 `WM_DISPLAYCHANGE`, `SystemEvents.DisplaySettingsChanged`, 3-second polling.
- **Full hotplug support** — connect/disconnect monitors while in use, the app responds instantly.
- **DPI-aware** — precise positioning on displays with different scaling (100%, 125%, 150%, 200%).
- **Automatic fallback** — if the presenter monitor is disconnected, the window moves to an alternative display.
- **Per-display toggle** — dedicated button for each connected display, with visual selection.

### Remote control
- **HTTP REST API** on port 3131 for Bitfocus Companion and external automation.
- **OSC UDP** on ports 8000 (receive) and 8001 (feedback) for hardware and software controllers.
- **CORS enabled** — the HTTP server accepts connections from any origin (network, browser, remote Companion).
- **Dedicated Companion module** with 23 actions, 5 feedbacks, 4 variables, ready presets and auto-reconnect.

### Video output
- **NewTek NDI** — direct streaming of the prompt to OBS, TriCaster, vMix or any NDI client.
- Vsync-aligned capture via `CompositionTarget.Rendering` with cached bitmaps and buffers.
- Configurable resolution and framerate via OSC (Full HD, HD, 4K, 25–60 fps).

### Portable
- **Single self-contained executable** (~73 MB). No .NET runtime required.
- **In-app language** — IT/EN ComboBox in toolbar, single Portable.exe with both languages.
- **Preferences and logs** stored next to the exe when running from USB/desktop (no traces on host PC).
- **Installer** with language selection (Italian/English) for those who prefer Start/Desktop shortcuts.
- **ReadyToRun** pre-compiled for ~40% faster cold startup.

---

## Language and localization

The app supports **Italian** and **English**. The interface is fully translated (toolbar, messages, status, errors).

### How to set the language
- **Installer**: on the first screen choose Italian or English. The language is saved and used on first launch.
- **Portable**: use `Live_Speaker_Teleprompter_Portable.exe` — IT+EN, in-app language selection.
- **Generic portable**: uses saved preferences (default: Italian) or the language from the last installation.

---

## System requirements

| Requirement | Details |
|-------------|---------|
| **Operating system** | Windows 10 (build 1903+) or Windows 11 — x64 |
| **RAM** | 4 GB minimum, 8 GB recommended |
| **Disk** | ~150 MB (app + preferences) |
| **Displays** | 1 minimum, 2+ recommended for production use |
| **.NET Runtime** | **Not required** — included in the executable |
| **NDI** (optional) | [NewTek NDI Tools/Runtime](https://www.ndi.tv/tools/) for video streaming |

---

## Installation and launch

### Portable version (recommended)

1. Download from the `release/` folder:
   - `Live_Speaker_Teleprompter_Portable.exe` — language from preferences
   - `Live_Speaker_Teleprompter_Portable.exe` — IT+EN, in-app selection
2. Run the exe — automatic extraction to temp and launch (for ITA/ENG) or direct launch (generic portable).
3. Preferences and logs stay next to the exe (USB drive = no traces on host PC).

### Installer version

1. Run `Live_Speaker_Teleprompter_Setup.exe`.
2. Choose the installation language (Italian / English).
3. Choose the installation folder (default: `%LOCALAPPDATA%\Live Speaker\Live Speaker Teleprompter`).
4. The installer creates desktop and Start menu shortcuts. The app appears in Settings > Apps.

### Launch

The app opens in **full screen** with default scroll speed **0.5** and edit mode active. Load a file or start typing directly in the editor.

---

## Keyboard shortcuts

| Key | Function |
|-----|----------|
| `Space` | Play / Pause |
| `Up Arrow` | Increase speed (+0.5) |
| `Down Arrow` | Decrease speed (-0.5) |
| `Left / Right Arrow` | Reset speed to zero |
| `Home` | Go to start of text |
| `End` | Go to end of text |
| `Mouse wheel` | Adjust speed (in presentation mode) |
| `Ctrl + O` | Open file |
| `Ctrl + S` | Save |
| `Ctrl + N` | New document |

> **Note:** Arrow keys and mouse wheel control speed only in **presentation mode**. In edit mode, standard editor behavior is preserved.

---

## External display management

### How it works

When you connect a second monitor (or projector, or TV), the app detects it automatically and shows a dedicated toggle in the "Screens" panel. Clicking the toggle opens the **Presenter** window in full screen on the chosen display.

### Triple redundancy detection

| Layer | Technology | Latency |
|-------|------------|---------|
| 1 | Win32 `WM_DISPLAYCHANGE` hook | ~50 ms |
| 2 | .NET `SystemEvents.DisplaySettingsChanged` | ~200 ms |
| 3 | `DispatcherTimer` polling | every 3 seconds |

This ensures detection on tablets, docking stations, USB-HDMI adapters or wireless connections (Miracast, AirPlay).

### Hotplug behavior

- **Monitor added**: a new toggle appears, selectable with one click.
- **Monitor removed**: if the Presenter was on that display, it is automatically moved to an alternative monitor. If there are no alternatives, it is hidden.
- **Preferred monitor saved**: the selection is remembered in preferences and restored on restart.

---

## NDI streaming

### Activation

1. Install [NewTek NDI Tools/Runtime](https://www.ndi.tv/tools/) on the PC.
2. In the app, click the **NDI** toggle in the "Screens" panel.
3. The stream will appear as **"Live Speaker NDI"** on any NDI client on the local network.

### NDI control via OSC

| OSC address | Arguments | Description |
|-------------|-----------|-------------|
| `/ndi/start` | — | Start NDI streaming |
| `/ndi/stop` | — | Stop NDI streaming |
| `/ndi/toggle` | — | Toggle NDI state |
| `/ndi/resolution` | `int width`, `int height` | Set resolution (e.g. 1920, 1080) |
| `/ndi/framerate` | `int fps` | Set framerate (e.g. 30) |
| `/ndi/sourcename` | `string name` | Change NDI source name |

---

## Remote control — HTTP REST API

The app exposes an HTTP endpoint at `http://localhost:3131/teleprompter/` started automatically. Compatible with Bitfocus Companion, cURL, browser, scripts and any HTTP client.

### Available endpoints

| Command | Method | URL | Description |
|---------|--------|-----|-------------|
| Status | GET | `/teleprompter/status` | Full JSON status |
| Play | GET/POST | `/teleprompter/play` | Start scrolling |
| Pause | GET/POST | `/teleprompter/pause` | Stop scrolling |
| Toggle | GET/POST | `/teleprompter/toggle` | Toggle play/pause |
| Speed + | GET/POST | `/teleprompter/speed/up` | +0.5 |
| Speed − | GET/POST | `/teleprompter/speed/down` | −0.5 |
| Speed 0 | GET/POST | `/teleprompter/speed/reset` | Reset to zero |
| Set speed | GET/POST | `/teleprompter/speed/set?value=1.5` | Exact value |

### cURL example

```bash
curl http://localhost:3131/teleprompter/play
curl "http://localhost:3131/teleprompter/speed/set?value=2.0"
curl http://localhost:3131/teleprompter/status
```

---

## Remote control — OSC

The app receives OSC commands on UDP port **8000** and sends feedback on port **8001**.

### Main OSC commands

| Address | Arguments | Description |
|---------|-----------|-------------|
| `/teleprompter/play` | — | Start scrolling |
| `/teleprompter/pause` | — | Stop scrolling |
| `/teleprompter/reset` | — | Go to start of text |
| `/teleprompter/speed` | `float` | Set speed (-80 to +80) |
| `/teleprompter/position` | `float` (0.0–1.0) | Set scroll position |
| `/teleprompter/jump/top` | — | Jump to start |
| `/teleprompter/jump/bottom` | — | Jump to end |
| `/teleprompter/mirror/toggle` | — | Toggle mirror state |

---

## Bitfocus Companion integration

See the full guide: [docs/Setup_Companion_Live_Speaker_Teleprompter.md](../docs/Setup_Companion_Live_Speaker_Teleprompter.md)

The module is in the `companion-module/` folder and offers:
- **23 actions** (playback, speed, font, navigation, mirror, NDI)
- **5 feedbacks** (playing, speed, mirror, NDI active, NDI available)
- **4 variables** (speed, playing, mirrored, ndi_active)
- **10 presets** ready for Stream Deck
- **Auto-reconnect** every 5 seconds

---

## Supported file formats

### Opening

| Format | Extensions | Notes |
|--------|------------|-------|
| Teleprompter document | `.rstp` | Native format, preserves all formatting |
| Microsoft Word | `.docx`, `.doc` | Text extraction |
| Rich Text Format | `.rtf` | Basic formatting |
| Plain text | `.txt`, `.md`, `.log` | Converted with current font |
| Subtitles | `.srt`, `.vtt` | Loaded as text |

### Saving

| Format | Extension | Preserves formatting |
|--------|-----------|----------------------|
| Teleprompter document | `.rstp` | Full |
| Rich Text Format | `.rtf` | Partial |
| Plain text | `.txt` | Text only |

---

## User preferences

Preferences are saved automatically to:

```
%APPDATA%\Live Speaker Teleprompter\preferences.json
```

For the portable version (run from USB), preferences stay next to the exe.

### Saved data

- Background and text color, font, size
- Default scroll speed
- Mirror and always-on-top state
- Preferred monitor
- Last opened file
- Arrow position, size and color
- **Language** (it/en)

### Error logs

```
%APPDATA%\Live Speaker Teleprompter\logs\error-YYYYMMDD-HHmmss.log
```

---

## Build from source

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Windows 10/11 x64

### Commands

```powershell
# Full build (portable + installer + ITA/ENG versions)
.\clean-and-build.ps1

# Or installer build only
cd installer
.\build-installer.ps1
```

### Build output

| File | Description |
|------|-------------|
| `Live_Speaker_Teleprompter_Portable.exe` | Generic portable executable |
| `Live_Speaker_Teleprompter_Portable.exe` | Portable IT+EN (in-app language selection) |
| `Live_Speaker_Teleprompter_Setup.exe` | Installer with language selection |

---

## Project architecture

```
src/TeleprompterApp/           ← Main WPF app
  MainWindow.xaml/.cs            — Main window
  PresenterWindow.xaml/.cs       — Full-screen presenter window
  Localization.cs                — IT/EN translations
  CompanionBridge.cs             — HTTP REST API (port 3131)
  OscBridge.cs                   — OSC UDP (8000/8001)
  NDITransmitter.cs              — NDI streaming
  Services/
    DisplayManager.cs            — Display detection
    PresenterSyncService.cs      — Editor→presenter sync

installer/
  build-installer.ps1             — Build pipeline
  installer-template.ps1         — Installer with language selection
  portable-extractor-template.ps1 — Portable ITA/ENG template

release/                        ← Build output
```

---

## License

MIT
