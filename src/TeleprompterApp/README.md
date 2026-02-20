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
  LayoutPresetService.cs     — Save/Load preset layout S1–S4 / L1–L4 (layout-presets.json)
```

## Build Commands

```powershell
dotnet build                          # Debug build
dotnet run                            # Run in development
dotnet publish -c Release             # Release publish (self-contained)
cd installer && .\build-installer.ps1 # Full pipeline: exe + zip + installer
```

## Key Features (v2.3.5)

- **Header a due righe** — File, formattazione, velocità, play, navigazione, margini, freccia, preset
- **Preset S1–S4 / L1–L4** — Salvataggio e caricamento layout completo (colori, font, margini, freccia) in header riga 2
- **Play senza loop** — Lo scroll prosegue fino al 100% del testo e si ferma
- **Freccia allineata** — Stessa riga di testo in preview e program (Y assoluta, riferimento testo)
- **Margini estesi** — L, D, A, B fino a 400 px; tasto L=D per margini uguali
- **Velocità -80 … +80** — Scorrimento più rapido
- **Navigazione** — Home, End, Page Up, Page Down; pulsanti Inizio/Fine in header
- **Spazio** — Play/Pausa solo in modalità presentazione (non-modifica)
- **Preview = Program** — Font e aspetto identici tra MainWindow e PresenterWindow

## Key Optimizations (v2.3.5)

- CompositionTarget.Rendering per scroll vsync-aligned (60/120 Hz)
- CanContentScroll=False per scroll fisico (pixel) fluido
- Dead-zone 0.05 px per scroll quasi ogni frame
- FlowDocument cloning via XamlPackage (~30ms vs ~300ms with XamlWriter)
- TextFormattingMode=Ideal + ClearType per testo nitido a 72pt
- ServerGC + TieredPGO + ReadyToRun
- Auto log cleanup (keeps last 10 files)
