# Live Speaker Teleprompter — Documentazione Definitiva

> **Versione:** 2.3.4 — 18/03/2026
> **Sostituisce:** Architettura_Live_Speaker_Teleprompter.md
> **Scopo:** Documento unico di riferimento per sviluppo, manutenzione e operatività live

---

## INDICE

1. [Panoramica e Obiettivo](#1-panoramica-e-obiettivo)
2. [Stack Tecnologico](#2-stack-tecnologico)
3. [Struttura Progetto e Percorsi](#3-struttura-progetto-e-percorsi)
4. [Entry Point e Avvio](#4-entry-point-e-avvio)
5. [Finestre Principali](#5-finestre-principali)
6. [Servizi](#6-servizi)
7. [Modelli Dati](#7-modelli-dati)
8. [Integrazione OSC](#8-integrazione-osc)
9. [HTTP REST API (CompanionBridge)](#9-http-rest-api-companionbridge)
10. [Integrazione NDI](#10-integrazione-ndi)
11. [Scroll Engine](#11-scroll-engine)
12. [Sincronizzazione Presenter](#12-sincronizzazione-presenter)
13. [Companion Module (Node.js)](#13-companion-module-nodejs)
14. [Build e Packaging](#14-build-e-packaging)
15. [Vincoli Sacri e Regole Invarianti](#15-vincoli-sacri-e-regole-invarianti)
16. [Changelog](#16-changelog)

---

## 1. PANORAMICA E OBIETTIVO

**Live Speaker Teleprompter** è un'applicazione desktop **portable** (singolo `.exe`, nessuna installazione) per teleprompter professionale con supporto multi-schermo, NDI, OSC e Bitfocus Companion.

L'utente carica uno script, configura font/colori/velocità, e l'app produce:

- **Editor + Preview** nella finestra principale (MainWindow)
- **Output full-screen** su monitor esterno (PresenterWindow) con freccia guida
- **Streaming NDI** (opzionale, richiede NewTek SDK)
- **Controllo OSC** (porte 8000 rx / 8001 tx) per controller hardware e software
- **HTTP REST API** (porta 3131) per Bitfocus Companion
- **Modulo Companion** (Node.js, API v2) per integrazione nativa Companion 4.3+

**Target utente:** Presentatori, speaker, operatori live, studi TV, eventi corporate.

### Flusso Utente

```
┌───────────────────────────────────────────────────────────┐
│                 LIVE SPEAKER TELEPROMPTER                   │
│                                                             │
│  ┌──────────────────┐   ┌────────────────────────────────┐ │
│  │  TOOLBAR RIGA 1   │   │       EDITOR / PREVIEW          │ │
│  │  Open Save Font   │   │   ┌────────────────────────┐    │ │
│  │  Speed Play/Pause │   │   │  RichTextBox FlowDoc   │    │ │
│  │  OnAir Mirror NDI │   │   │  (editor + anteprima)  │    │ │
│  │  Monitor Toggles  │   │   └────────────────────────┘    │ │
│  └──────────────────┘   └────────────────────────────────┘ │
│  ┌──────────────────┐   ┌────────────────────────────────┐ │
│  │  TOOLBAR RIGA 2   │   │  PresenterWindow (monitor ext)  │ │
│  │  Margini Freccia  │   │  Full-screen, documento clone   │ │
│  │  Preset S1–L4     │   │  Freccia guida, mirror mode    │ │
│  └──────────────────┘   └────────────────────────────────┘ │
└───────────────────────────────────────────────────────────┘
```

---

## 2. STACK TECNOLOGICO

### 2.1 Applicazione Principale (.NET)

| Tecnologia | Versione | Ruolo |
|---|---|---|
| **.NET** | 8.0 LTS | Framework runtime |
| **WPF** | net8.0-windows | UI, XAML, FlowDocument |
| **C#** | 12+ | Linguaggio, nullable enable, implicit usings |
| **Windows Forms** | (integrazione) | ColorDialog, FontDialog |
| **System.Text.Json** | (incluso .NET 8) | Serializzazione preferenze, layout preset |

### 2.2 Integrazioni Esterne

| Integrazione | Protocollo | Porte |
|---|---|---|
| **OSC** | UDP | 8000 (rx), 8001 (tx feedback) |
| **CompanionBridge** | HTTP REST | 3131 |
| **NDI** | P/Invoke ProcessNDI4.dll | Streaming video |

### 2.3 Companion Module (Node.js)

| Pacchetto | Versione | Ruolo |
|---|---|---|
| **@companion-module/base** | ^2.0.3 | API v2, runtime Node 22 |
| **osc** | ^2.4.5 | Client OSC UDP |

**Requisito:** Bitfocus Companion 4.3+ per modulo API v2.

### 2.4 Build & Tooling

| Strumento | Ruolo |
|---|---|
| **dotnet publish** | Build Release, self-contained single-file |
| **PowerShell** | clean-and-build.ps1, build-installer.ps1 |
| **IExpress** | Packaging installer Setup (self-extracting) |
| **PngToIco** | Conversione logo PNG → ICO |

---

## 3. STRUTTURA PROGETTO E PERCORSI

```
Live Speaker Teleprompter/
├── clean-and-build.ps1          ← Script principale build (root)
├── clean-and-build.bat          ← Wrapper .bat per doppio clic
│
├── src/
│   ├── TeleprompterApp/         ← Progetto WPF principale
│   │   ├── TeleprompterApp.csproj
│   │   ├── App.xaml / App.xaml.cs
│   │   ├── MainWindow.xaml / .cs
│   │   ├── PresenterWindow.xaml / .cs
│   │   ├── Localization.cs
│   │   ├── UserPreferences.cs
│   │   ├── PreferencesService.cs
│   │   ├── AppPaths.cs
│   │   ├── LayoutPreset.cs
│   │   ├── CompanionBridge.cs
│   │   ├── OscBridge.cs
│   │   ├── NDITransmitter.cs
│   │   ├── NdiInterop.cs
│   │   ├── Osc/OscPacket.cs
│   │   └── Services/
│   │       ├── DisplayManager.cs
│   │       ├── PresenterSyncService.cs
│   │       ├── DebouncedPreferencesService.cs
│   │       └── LayoutPresetService.cs
│   ├── README-ITA.md
│   └── README-ENG.md
│
├── installer/
│   ├── build-installer.ps1
│   ├── installer-template.ps1
│   └── portable-extractor-template.ps1
│
├── release/                     ← Output build (gitignored)
├── icons/
├── scripts/
│   ├── convert-logo.ps1
│   └── PngToIco/
│
├── docs/
│   ├── ARCHITETTURA_Live_Speaker_Teleprompter.md  ← QUESTO FILE
│   ├── Guida_Refactoring_MainWindow.md
│   ├── Setup_Companion_Live_Speaker_Teleprompter.md
│   ├── Primo_Prompt_Avvio_Chat_Claude_Desktop_Live_Speaker_Teleprompter.md
│   └── Istruzioni_Progetto_Claude_Live_Speaker_Teleprompter.md
│
└── companion-module/            ← Modulo Bitfocus Companion (Node.js)
    ├── index.js
    ├── package.json
    └── companion-config.json
```

### Percorsi critici

| Tipo | Percorso |
|---|---|
| **Exe portable** | `release/Live_Speaker_Teleprompter_Portable.exe` (IT+EN, selezione in-app) |
| **Setup installer** | `release/Live_Speaker_Teleprompter_Setup.exe` |
| **Preferenze** | `AppPaths.PreferencesPath` (portable = accanto exe, installato = %APPDATA%) |
| **Layout preset** | `AppPaths.BaseDirectory/layout-presets.json` |
| **Log errori** | `AppPaths.LogDirectory/error-*.log` |

---

## 4. ENTRY POINT E AVVIO

**File:** `App.xaml.cs`

### Sequenza OnStartup

1. `RenderOptions.ProcessRenderMode = Default` (GPU rendering)
2. Localizzazione: `Localization.Initialize(null, prefs.CultureName)` — lingua da preferenze
3. `PreferencesService.Load()` → `UserPreferences`
4. `CleanupOldLogs()` — mantiene ultimi 10 file di log

### Gestione errori

- `DispatcherUnhandledException` — UI thread, MessageBox + Shutdown
- `AppDomain.UnhandledException` — altri thread, log
- `TaskScheduler.UnobservedTaskException` — log + SetObserved

---

## 5. FINESTRE PRINCIPALI

### 5.1 MainWindow

**Ruolo:** Unica fonte di verità per documento e stato.

- Toolbar riga 1: Open, Save, Font, Speed, Play/Pause, OnAir, Mirror, TopMost, **IT/EN** (selezione lingua), NDI, Monitor toggles
- Toolbar riga 2: Margini, Freccia, Preset S1–L4
- Area editor: RichTextBox con FlowDocument (editor + anteprima)
- StatusBar: messaggi di stato

### 5.2 PresenterWindow

**Ruolo:** Clone read-only full-screen su monitor esterno.

- ScrollViewer + RichTextBox (documento clonato)
- Canvas con freccia guida draggable
- Supporto mirror mode (ScaleTransform -1,1)
- Gestione DPI (HwndSource.DpiChanged)

---

## 6. SERVIZI

| Servizio | File | Ruolo |
|---|---|---|
| **DisplayManager** | Services/DisplayManager.cs | Rilevamento schermi (Win32 + SystemEvents + polling 3s) |
| **PresenterSyncService** | Services/PresenterSyncService.cs | Sync documento editor→presenter (debounce 300ms, XamlPackage) |
| **DebouncedPreferencesService** | Services/DebouncedPreferencesService.cs | Salvataggio preferenze (debounce 500ms) |
| **LayoutPresetService** | Services/LayoutPresetService.cs | Save/Load preset S1–L4 (layout-presets.json) |
| **PreferencesService** | PreferencesService.cs | I/O JSON preferenze (scrittura atomica) |
| **CompanionBridge** | CompanionBridge.cs | HTTP REST API porta 3131 |
| **OscBridge** | OscBridge.cs | OSC UDP 8000/8001 |
| **NDITransmitter** | NDITransmitter.cs | Streaming NDI via CompositionTarget.Rendering |

---

## 7. MODELLI DATI

### UserPreferences

Persistito in `preferences.json`. Campi: CultureName, DocumentBackgroundHex, TextForegroundHex, FontFamily, FontSizePoints, IsBold, IsItalic, UseUnderline, DefaultScrollSpeed, MirrorEnabled, TopMostEnabled, PreferredDisplayNumber, LastScriptPath, ArrowColorHex, ArrowScale, ArrowHorizontalOffset, ArrowVerticalOffset, ArrowLeftPaddingExtra, MarginTop/Right/Bottom/Left, MarginsLinked, EditModeEnabled, LastUpdatedUtc.

### LayoutPreset

Snapshot layout per preset S1–S4 / L1–L4. Sottoinsieme di UserPreferences (no lingua, file, topmost, edit mode).

---

## 8. INTEGRAZIONE OSC

### Porte

| Porta | Direzione | Scopo |
|---|---|---|
| 8000 | Ricezione | Comandi da controller OSC / Companion |
| 8001 | Invio | Feedback di stato verso controller |

### Comandi supportati

`/teleprompter/start`, `/stop`, `/reset`, `/speed`, `/speed/increase`, `/speed/decrease`, `/font/size`, `/font/increase`, `/font/decrease`, `/position`, `/jump/top`, `/jump/bottom`, `/mirror`, `/mirror/toggle`, `/status/request`, `/ndi/*`

### Feedback

`/teleprompter/status`, `/teleprompter/speed/current`, `/teleprompter/font/size/current`, `/teleprompter/position/current`, `/teleprompter/mirror/status`, `/ndi/status`

---

## 9. HTTP REST API (COMPANIONBRIDGE)

**Endpoint base:** `http://localhost:3131/teleprompter/`

**Comandi:** GET/POST `/play`, `/pause`, `/toggle`, `/speed/up`, `/speed/down`, `/speed/reset`, `/speed/set?value=X`

**Risposta JSON:** `{ status, message, isPlaying, speed, editMode, endpoint }`

**CORS:** `Access-Control-Allow-Origin: *` per Companion remoto.

---

## 10. INTEGRAZIONE NDI

**Dipendenze:** NewTek NDI SDK (ProcessNDI4.dll). Se assente: toggle disabilitato, nessun crash.

**Architettura:** CompositionTarget.Rendering → frame-rate limiter → RenderTargetBitmap cached → VisualBrush → NdiInterop.SendFrame().

**Controllo OSC:** `/ndi/start`, `/ndi/stop`, `/ndi/toggle`, `/ndi/resolution`, `/ndi/framerate`, `/ndi/sourcename`.

---

## 11. SCROLL ENGINE

**Principio:** `CompositionTarget.Rendering` (vsync-aligned) invece di DispatcherTimer.

**Algoritmo:** Delta-time compensation → accumulo pixel → dead zone 0.05px → ScrollToVerticalOffset → stop automatico a fine/inizio testo.

**Regole invarianti:**
- MAI `UpdateLayout()` nel tick
- MAI clonare documento nel tick
- `CanContentScroll="False"` su entrambi gli ScrollViewer

---

## 12. SINCRONIZZAZIONE PRESENTER

- **Documento:** TextChanged → RequestPresenterSync → debounce 300ms → XamlPackage clone → SetDocument
- **Scroll:** OnScrollRendering → SetVerticalOffset (ogni frame)
- **Aspetto:** SyncPresenterAppearance → SetBackgroundColor, SetArrowColor, SetArrowScale, SetArrowAbsoluteY, SetMirror

---

## 13. COMPANION MODULE (NODE.JS)

**Percorso:** `companion-module/`

**API:** @companion-module/base v2.0.3 (Companion 4.3+)

**Manifest:** `type: "connection"`, `module_api: "2.0.0"`, `runtime: "node22"`

**Export:** `module.exports = RSpeakerTeleprompterInstance` (default export, no runEntrypoint)

**Variabili:** speed, playing, mirrored, ndi_active (setVariableDefinitions come oggetto)

**Presets:** setPresetDefinitions(structure, presets) con type: 'simple'

---

## 14. BUILD E PACKAGING

**Script:** `clean-and-build.ps1` → pulizia → convert-logo.ps1 → dotnet restore → build-installer.ps1

**Output (2 file):**
- `Live_Speaker_Teleprompter_Portable.exe` — eseguibile standalone con IT+EN, selezione lingua in-app
- `Live_Speaker_Teleprompter_Setup.exe` — installer self-extracting (stesso exe, collegamenti Start/Desktop)

**Configurazione Release:** SelfContained, PublishSingleFile, PublishReadyToRun, EnableCompressionInSingleFile, ServerGarbageCollection, TieredPGO.

---

## 15. VINCOLI SACRI E REGOLE INVARIANTI

1. **MainWindow = unica fonte di verità** — PresenterWindow è clone read-only
2. **Stabilità live priorità n.1** — try/catch su ogni I/O e rete
3. **Nessun MVVM framework** — code-behind con servizi separati
4. **Preferenze debounced** — 500ms, mai nel rendering tick
5. **Scroll vsync-aligned** — CompositionTarget.Rendering, mai UpdateLayout nel tick
6. **Dispose esplicito** — tutti i servizi IDisposable, Window_Closing fa dispose di tutto
7. **Scrittura atomica** — `.tmp` + File.Move(overwrite: true)
8. **Localizzazione** — `Localization.Get(key)`, aggiungere sempre in `It` e `En`. Terminologia EN professionale teleprompter/broadcast. Primo avvio in inglese (`DefaultCulture = "en"`). Lingua salvata in `preferences.json` alla chiusura. Installer in inglese. Vedi `docs/Istruzioni_Traduzione_i18n_Live_Speaker_Teleprompter.md`.
9. **NDI opzionale** — mai crash se SDK assente
10. **Freeze Brush** — SolidColorBrush creati dinamicamente → .Freeze()

### Checklist pre-release

- [ ] Documento vuoto, 1 riga, 100+ righe
- [ ] Hot-plug monitor
- [ ] Velocità negative (scroll inverso)
- [ ] DPI diversi (100%, 125%, 150%, 200%)
- [ ] Mirror mode con freccia e margini
- [ ] Scroll si ferma a fine/inizio testo
- [ ] Installer e portable (lingua IT/EN selezionabile in-app)
- [ ] Uninstaller in Aggiungi/Rimuovi programmi
- [ ] NDI disabilitato senza SDK

---

## 16. CHANGELOG

| Data | Versione | Modifiche |
|---|---|---|
| 18/03/2026 | 2.3.4 | Lingua: selezione in-app (ComboBox IT/EN in toolbar), nessuna scelta durante installazione. Release: solo 2 file (Portable.exe + Setup.exe). Traduzione completa: dialog Apri/Salva, PresenterWindow, errori I/O. |
| 18/03/2026 | 2.3.3 | Creazione ARCHITETTURA. Companion module migrato a API v2 (@companion-module/base ^2.0.3). Aggiunta Guida_Refactoring_MainWindow, Primo_Prompt, System_Prompt Claude. Rules Cursor aggiornate con doc-sync. |
| — | 2.3.3 | Stack: .NET 8 LTS, WPF, C# 12. Zero NuGet esterni. Companion module: Node 22, osc 2.4.5. |
