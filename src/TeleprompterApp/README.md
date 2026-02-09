# R-Speaker Teleprompter — Developer Notes

Technical reference for contributors working on the WPF application source.

## Tech Stack

- **.NET 8** / WPF / C# — `net8.0-windows`, self-contained single-file publish
- **Win32 Interop** — display detection via `WM_DISPLAYCHANGE`, DPI-aware positioning
- **NDI** — P/Invoke to `Processing.NDI.Lib.x64.dll` via `NdiInterop.cs`
- **OSC** — custom UDP parser (`Osc/OscPacket.cs`), ports 8000 (recv) / 8001 (send)
- **HTTP** — `HttpListener` on port 3131 with CORS (`CompanionBridge.cs`)

## Project Structure

```
MainWindow.xaml/.cs          — Main editor window, toolbar, file I/O, scrolling
PresenterWindow.xaml/.cs     — Full-screen output for external displays
App.xaml/.cs                 — Entry point, global exception handling, log cleanup
CompanionBridge.cs           — HTTP REST API server (port 3131)
OscBridge.cs                 — OSC UDP server/client (ports 8000/8001)
NDITransmitter.cs            — NDI frame capture and transmission
NdiInterop.cs                — P/Invoke declarations for NDI SDK
PreferencesService.cs        — JSON preferences read/write
UserPreferences.cs           — Preferences data model
Osc/OscPacket.cs             — OSC message/bundle parser
Services/
  DisplayManager.cs          — Real-time display detection (3-layer redundancy)
  DebouncedPreferencesService.cs — 500ms debounced save
  PresenterSyncService.cs    — 300ms debounced editor→presenter sync
```

## Build Commands

```powershell
dotnet build                          # Debug build
dotnet run                            # Run in development
dotnet publish -c Release             # Release publish (self-contained)
cd installer && .\build-installer.ps1 # Full pipeline: exe + zip + installer
```

## Key Optimizations (v2.0)

- FlowDocument cloning via XamlPackage (~30ms vs ~300ms with XamlWriter)
- Cached VisualBrush/RenderTargetBitmap/DrawingVisual for NDI (zero per-frame alloc)
- BitmapCache GPU rendering on editor and presenter
- Dead-zone scroll skip (< 0.1px)
- ServerGC + TieredPGO + ReadyToRun
- Auto log cleanup (keeps last 10 files)
