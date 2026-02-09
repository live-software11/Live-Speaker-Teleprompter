# R-Speaker Teleprompter

A modern WPF teleprompter designed to be portable, responsive, and delightful for live presentations.

## ✨ Highlights
- **Rich text editor** with formatting controls, mirror mode, and responsive preview.
- **Persistent preferences** with debounced auto-save (500ms) covering playback speed, colors, fonts, and active monitors.
- **Real-time display detection** via triple-redundancy (Win32 `WM_DISPLAYCHANGE`, `SystemEvents`, 3s polling) with instant hotplug support.
- **Quick actions** via keyboard shortcuts: `Ctrl+O` (open), `Ctrl+S` (save), `Ctrl+N` (new document).
- **Drag & drop** support for text, RTF, and subtitle files straight into the editor.
- **Multi-monitor routing** with per-display toggles, DPI-aware positioning, and automatic re-homing on screen removal.
- **Self-contained publishing** for a portable executable that runs without additional installs.
- **Fullscreen startup & ready speed**: launches maximized with a 0.5 default scroll speed so you're show-ready instantly.
- **Direct NDI output**: stream the prompt over NewTek NDI with optimized capture (CompositionTarget.Rendering, cached bitmaps).
- **Delta-time scroll**: Stopwatch-based compensation for smooth scrolling regardless of system load.
- **Debounced presenter sync**: editor→presenter document cloning via XamlPackage with 300ms debounce.

## 🏗️ Architecture

```
Services/
  DisplayManager.cs              — Real-time screen detection (3-layer redundancy)
  DebouncedPreferencesService.cs  — 500ms debounced preference I/O
  PresenterSyncService.cs         — 300ms debounced document cloning
CompanionBridge.cs               — HTTP REST API on port 3131
NDITransmitter.cs                — NDI frame capture (CompositionTarget.Rendering)
NdiInterop.cs                    — P/Invoke wrapper for NDI SDK
OscBridge.cs                     — OSC UDP server/client (ports 8000/8001)
MainWindow.xaml/.cs              — Main application window
PresenterWindow.xaml/.cs         — Secondary display with DPI-aware positioning
PreferencesService.cs            — Preferences file I/O
UserPreferences.cs               — Preferences model
Osc/                             — OSC packet parser
```

## 🚀 Getting Started
1. Install the [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download).
2. Build and launch:
   ```pwsh
   dotnet build
   dotnet run
   ```
3. Use the toolbar or keyboard shortcuts to load content, tweak formatting, and start scrolling.

## 🗂️ File Support
Drag or open any of the following formats:
- Plain text: `.txt`, `.md`, `.log`, `.csv` and more
- Rich text: `.rtf`
- Subtitles & captions: `.srt`, `.vtt`
- Markup: `.html`, `.xml`, `.yaml`

The app will detect the best format automatically. When saving, choose between plain text, RTF, or FlowDocument XAML to preserve layout.

## 💾 Preference Storage
User preferences are saved to `%APPDATA%/R-Speaker Teleprompter/preferences.json`. Delete the file to reset defaults.

## 📦 Portable Build
Create a standalone executable with:
```pwsh
dotnet publish -c Release
```
The output lives under `bin/Release/net8.0-windows/win-x64/publish/` as a single `R-Speaker Teleprompter.exe` file (~70 MB) that can be copied to any Windows 10+ machine without installing .NET.

Alternatively, use the unified build script to create both a portable ZIP and an installer:
```pwsh
cd ..\..\installer
.\build-installer.ps1
```

## 🧪 Quality Checks
Before distributing a build, run:
```pwsh
 dotnet build
```
Add unit tests in a separate project when you're ready, then include `dotnet test` in your release checklist.

## 🎛️ Companion Remote Control

Bitfocus Companion (or any tool capable of making HTTP calls) can drive the teleprompter through a lightweight local API. The listener is available at `http://localhost:3131/teleprompter/` as soon as the app starts.

| Action | Method | URL / Parameters | Description |
|--------|--------|------------------|-------------|
| Play | GET/POST | `/teleprompter/play` | Starts scrolling |
| Pause | GET/POST | `/teleprompter/pause` | Stops scrolling |
| Toggle | GET/POST | `/teleprompter/toggle` | Flips play/pause |
| Speed + | GET/POST | `/teleprompter/speed/up` | Increases speed by 0.25 |
| Speed - | GET/POST | `/teleprompter/speed/down` | Decreases speed by 0.25 |
| Reset speed | GET/POST | `/teleprompter/speed/reset` | Sets speed to 0 |
| Set speed | GET/POST | `/teleprompter/speed/set?value=0.8` | Applies an explicit speed (use `.` for decimals) |
| Status | GET | `/teleprompter/status` | Returns JSON with play state, speed, and edit mode |

Every response comes back as JSON (`status`, `message`, `speed`, etc.), making it easy to feed feedback into Companion buttons or stream decks. Make sure no other service uses port `3131` when enabling the integration.

### 📡 NewTek NDI output

The **NDI** toggle in the "Schermi" header section publishes the current prompt as an NDI source named *"R-Speaker NDI"*. Install the [NDI Tools/runtime](https://www.ndi.tv/tools/) on the machine to expose the stream, then pick it up from OBS, TriCaster, vMix, or any NDI-compatible receiver.

## 🤝 Contributions
Issues and pull requests are welcome! Let us know if you discover an edge case or have feature ideas.
