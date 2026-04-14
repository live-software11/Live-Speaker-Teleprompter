# Live Speaker Teleprompter — Architettura Software

> **Versione documento:** 2.3.4  
> **Aggiornato:** Marzo 2026  
> **Mantenuto da:** CTO / AI Agent — aggiornare ad ogni modifica strutturale al codice

---

## Indice

1. [Stack tecnologico](#1-stack-tecnologico)
2. [Struttura repository](#2-struttura-repository)
3. [Entry point e avvio applicazione](#3-entry-point-e-avvio-applicazione)
4. [Finestre principali](#4-finestre-principali)
5. [Servizi](#5-servizi)
6. [Modelli dati](#6-modelli-dati)
7. [Localizzazione](#7-localizzazione)
8. [Integrazione NDI](#8-integrazione-ndi)
9. [Integrazione OSC](#9-integrazione-osc)
10. [HTTP REST API (CompanionBridge)](#10-http-rest-api-companionbridge)
11. [Scroll engine](#11-scroll-engine)
12. [Sincronizzazione Presenter](#12-sincronizzazione-presenter)
13. [Gestione schermi (DisplayManager)](#13-gestione-schermi-displaymanager)
14. [Preferenze utente](#14-preferenze-utente)
15. [Layout Preset](#15-layout-preset)
16. [Percorsi file (AppPaths)](#16-percorsi-file-apppaths)
17. [Build e packaging](#17-build-e-packaging)
18. [Dipendenze e configurazione progetto](#18-dipendenze-e-configurazione-progetto)
19. [Principi architetturali e regole invarianti](#19-principi-architetturali-e-regole-invarianti)

---

## 1. Stack tecnologico

| Componente | Tecnologia |
|---|---|
| Framework UI | **.NET 8.0 WPF** (Windows Presentation Foundation) |
| Linguaggio | **C# 12+** con nullable enable, implicit usings |
| Pattern UI | **Code-behind** (no MVVM framework esterno) |
| Target OS | Windows 10 (build 1903+) / Windows 11 — x64 only |
| Deployment | **Self-contained single-file executable** (~73 MB) |
| Runtime .NET | **Incluso nel binario** — non richiesto sul PC host |
| Startup | **ReadyToRun** pre-compilazione AOT (~40% più rapido a freddo) |
| GC | **Server GC** — ottimizzato per workload real-time |
| JIT | **TieredPGO** — ottimizzazione progressiva hot path |
| Compressione | `EnableCompressionInSingleFile` — riduce dimensione ~30% |
| Integrazioni esterne | NDI (P/Invoke), OSC (UDP), HTTP REST, Windows Forms (dialog) |

---

## 2. Struttura repository

```
Live Speaker Teleprompter/
│
├── clean-and-build.ps1          ← Script principale build (root): pulizia + build completa
├── clean-and-build.bat          ← Wrapper .bat per doppio clic
│
├── src/
│   ├── TeleprompterApp/         ← Progetto WPF principale
│   │   ├── TeleprompterApp.csproj
│   │   ├── App.xaml / App.xaml.cs           ← Entry point, startup, error handling globale
│   │   ├── MainWindow.xaml / .cs            ← Finestra principale: editor + toolbar + statusbar
│   │   ├── PresenterWindow.xaml / .cs       ← Finestra full-screen monitor esterno
│   │   ├── Localization.cs                  ← Dizionari IT/EN, metodo Get(), Initialize()
│   │   ├── UserPreferences.cs               ← Modello dati preferenze utente
│   │   ├── PreferencesService.cs            ← I/O JSON preferenze (scrittura atomica)
│   │   ├── AppPaths.cs                      ← Risoluzione percorsi portable vs installato
│   │   ├── LayoutPreset.cs                  ← Modello snapshot layout (preset 1–4)
│   │   ├── CompanionBridge.cs               ← HTTP REST API porta 3131
│   │   ├── OscBridge.cs                     ← OSC UDP porte 8000 (rx) / 8001 (tx)
│   │   ├── NDITransmitter.cs                ← Streaming NDI vsync-aligned
│   │   ├── NdiInterop.cs                    ← P/Invoke wrapper NDI SDK (ProcessNDI4.dll)
│   │   ├── Osc/
│   │   │   └── OscPacket.cs                 ← Parser OSC Message + Bundle (ReadOnlySpan<byte>)
│   │   └── Services/
│   │       ├── DisplayManager.cs            ← Rilevamento schermi real-time (tripla ridondanza)
│   │       ├── PresenterSyncService.cs      ← Sync documento editor→presenter (debounce 300ms)
│   │       ├── DebouncedPreferencesService.cs ← Salvataggio preferenze (debounce 500ms)
│   │       └── LayoutPresetService.cs       ← Save/Load preset layout (layout-presets.json)
│
├── installer/
│   ├── build-installer.ps1                  ← Pipeline: publish + portable EXE + installer EXE
│   ├── installer-template.ps1               ← Template installer self-extracting (PowerShell)
│   └── portable-extractor-template.ps1      ← (legacy, non usato)
│
├── release/                                 ← Output build (gitignored) — 4 file
│   ├── Live_Speaker_Teleprompter_Setup.exe     ← Installer con scelta lingua e cartella
│   ├── Live_Speaker_Teleprompter_Portable.exe  ← IT+EN, selezione in-app
│   ├── README_ITA_Live_Speaker_Teleprompter.md ← Documentazione utente IT (copia da docs/)
│   └── README_ENG_Live_Speaker_Teleprompter.md ← Documentazione utente EN (copia da docs/)
│
├── icons/
│   ├── Logo Teleprompter.png                ← Sorgente logo (PNG)
│   └── app-icon.ico                         ← Icona generata da convert-logo.ps1
│
├── scripts/
│   ├── convert-logo.ps1                     ← Converte PNG → ICO (chiamato da clean-and-build)
│   └── PngToIco/                            ← Tool .NET per conversione PNG/WebP → ICO
│
├── docs/
│   ├── ARCHITETTURA_Live_Speaker_Teleprompter.md                    ← Questo documento
│   ├── BugFix_Refactor_Implementazioni_Live_Speaker_Teleprompter.md ← Changelog, bug, refactoring
│   ├── Setup_Companion_Live_Speaker_Teleprompter.md                 ← Guida setup Bitfocus Companion
│   ├── Istruzioni_Progetto_Claude_Live_Speaker_Teleprompter.md      ← System prompt Claude Desktop
│   ├── Primo_Prompt_Avvio_Chat_Claude_Desktop_Live_Speaker_Teleprompter.md ← Primo prompt Claude
│   ├── README_ITA_Live_Speaker_Teleprompter.md                      ← Documentazione utente IT
│   └── README_ENG_Live_Speaker_Teleprompter.md                      ← Documentazione utente EN
│
└── companion-module/                        ← Modulo Bitfocus Companion (Node.js)
    ├── index.js
    ├── package.json
    └── companion-config.json
```

---

## 3. Entry point e avvio applicazione

**File:** `App.xaml.cs`

### Sequenza `OnStartup`

```
App.OnStartup()
  │
  ├─ 1. RenderOptions.ProcessRenderMode = Default (GPU rendering)
  │
  ├─ 2. Localizzazione
  │     └─ Localization.Initialize(null, prefs.CultureName)
  │         → lingua da preferenze, nessun install-language.txt
  │
  ├─ 3. PreferencesService.Load() → carica UserPreferences da JSON
  │
  └─ 4. CleanupOldLogs() → mantiene solo gli ultimi 10 file di log
```

### Gestione errori non gestiti

`App.xaml.cs` registra:
- `AppDomain.CurrentDomain.UnhandledException` — eccezioni non catchate su qualsiasi thread
- `DispatcherUnhandledException` — eccezioni sul thread UI WPF

Entrambi scrivono su file di log in `AppPaths.LogDirectory` e mostrano `MessageBox` con `Localization.Get("Error_Unhandled", ...)`.

---

## 4. Finestre principali

### 4.1 MainWindow

**File:** `MainWindow.xaml` / `MainWindow.xaml.cs`

**Ruolo:** Unica fonte di verità per documento e stato dell'applicazione.

#### Struttura UI (XAML)

```
MainWindow (WindowStyle=None, Topmost opzionale)
  ├── Grid principale
  │   ├── Row 0 — Toolbar riga 1
  │   │   ├── OpenFileButton, NewDocumentButton, SaveButton
  │   │   ├── FontFamilyComboBox, FontSizeComboBox
  │   │   ├── BoldButton, ItalicButton, UnderlineButton
  │   │   ├── AlignLeft/Center/Right buttons
  │   │   ├── TextColorButton, BackgroundColorButton
  │   │   ├── SpeedSlider (-20 → +20, step 0.5)
  │   │   ├── PlayPauseButton (ToggleButton)
  │   │   ├── EditModeButton (ToggleButton)
  │   │   ├── OnAirButton (ToggleButton)
  │   │   ├── MirrorButton (ToggleButton)
  │   │   ├── NDIButton (ToggleButton)
  │   │   └── Monitor ToggleButtons (dinamici, generati da DisplayManager)
  │   │
  │   ├── Row 1 — Toolbar riga 2
  │   │   ├── MarginsLabel + 4 slider margini (Top/Right/Bottom/Left) + link toggle
  │   │   ├── ArrowLabel + ArrowColorButton + ArrowScaleSlider
  │   │   └── Preset S1–S4 (save) / L1–L4 (load) buttons
  │   │
  │   ├── Row 2 — Area editor/preview
  │   │   └── ScrollViewer (CanContentScroll=False)
  │   │       └── RichTextBox (FlowDocument — editor e anteprima)
  │   │
  │   └── Row 3 — StatusBar
  │       └── _statusText (TextBlock)
```

#### Campi privati principali

| Campo | Tipo | Scopo |
|---|---|---|
| `_scrollStopwatch` | `Stopwatch` | Delta-time compensation scroll |
| `_scrollAccumulator` | `double` | Accumulo pixel sub-frame |
| `_scrollSpeed` | `double` | Velocità corrente (px/s equivalente) |
| `_isScrollRenderingSubscribed` | `bool` | Guard per CompositionTarget.Rendering |
| `_screenInfos` | `List<ScreenInfo>` | Lista schermi rilevati |
| `_monitorToggleButtons` | `List<ToggleButton>` | Toggle dinamici per ogni monitor |
| `_displayManager` | `DisplayManager?` | Servizio rilevamento schermi |
| `_debouncedPrefs` | `DebouncedPreferencesService?` | Salvataggio preferenze debounced |
| `_presenterSync` | `PresenterSyncService?` | Sync documento al presenter |
| `_presenterWindow` | `PresenterWindow?` | Riferimento finestra presenter |
| `_preferences` | `UserPreferences` | Stato preferenze in memoria |
| `_supportedExtensions` | `HashSet<string>` | Estensioni file accettate |

#### Metodi chiave

| Metodo | Descrizione |
|---|---|
| `Window_Loaded` | Inizializza tutti i servizi, carica preferenze, avvia DisplayManager, CompanionBridge, OscBridge, applica localizzazione |
| `ApplyLocalization()` | Imposta ToolTip e Text di tutti gli elementi UI localizzabili dopo il caricamento |
| `OnScrollRendering` | Tick vsync-aligned: calcola delta-time, avanza scroll, chiama `SetVerticalOffset` sul presenter |
| `SyncPresenterDocument()` | Clone immediato FlowDocument via XamlPackage → presenter |
| `RequestPresenterSync()` | Marca dirty il PresenterSyncService (debounce 300ms) |
| `SyncPresenterAppearance()` | Sync completo sfondo, freccia (colore, scala, posizione) al presenter |
| `SavePreferences()` | Chiama `_debouncedPrefs.Save()` (debounce 500ms) |
| `LoadPreferences()` | Applica `UserPreferences` a tutti i controlli UI |
| `SetStatus(string)` | Aggiorna `_statusText.Text` sulla UI thread |
| `UpdatePlayPauseLabel()` | Aggiorna testo Play/Pausa in base allo stato |
| `UpdateOnAirLabel()` | Aggiorna testo On Air / Off Air |
| `Window_Closing` | Dispose di tutti i servizi: DisplayManager, PresenterSync, CompanionBridge, NDI, OSC |

---

### 4.2 PresenterWindow

**File:** `PresenterWindow.xaml` / `PresenterWindow.xaml.cs`

**Ruolo:** Clone read-only del documento, visualizzato full-screen su monitor esterno.

#### Struttura UI (costruita via codice in `LoadView()`)

```
PresenterWindow (WindowStyle=None, WindowState=Maximized)
  └── Border (sfondo configurabile)
      └── Grid
          ├── ScrollViewer (CanContentScroll=False)
          │   └── RichTextBox (_content) — documento clonato, read-only
          └── Canvas (_arrowCanvas)
              └── Grid (_arrowContainer) — freccia guida draggable
                  └── Polygon (_arrowShape) con ScaleTransform
```

#### Metodi pubblici (chiamati da MainWindow)

| Metodo | Descrizione |
|---|---|
| `SetDocument(FlowDocument)` | Sostituisce il documento clonato nel RichTextBox |
| `SetVerticalOffset(double)` | Sincronizza posizione scroll (pixel) |
| `SetArrowAbsoluteY(double)` | Posiziona la freccia a Y pixel dal top |
| `SetMirror(bool)` | Applica/rimuove ScaleTransform(-1,1) per mirror orizzontale |
| `SetBackgroundColor(Color)` | Cambia colore sfondo |
| `SetArrowColor(Color)` | Cambia colore freccia |
| `SetArrowScale(double)` | Scala la freccia |
| `MoveToScreen(Screen)` | Sposta la finestra sul monitor specificato (usa `Bounds` fisici) |
| `ApplyScreenBounds(Screen)` | Ricalcola posizione/dimensione rispettando DPI del monitor |

#### Gestione DPI

La finestra si registra su `HwndSource.DpiChanged` per riposizionarsi correttamente quando il monitor cambia DPI (es. spostamento tra display con scaling diverso).

---

## 5. Servizi

### 5.1 DisplayManager

**File:** `Services/DisplayManager.cs`

Rileva in tempo reale i monitor connessi con **tripla ridondanza**:

| Layer | Meccanismo | Latenza tipica |
|---|---|---|
| 1 | Win32 `WM_DISPLAYCHANGE` hook via `HwndSource` | ~50 ms |
| 2 | .NET `SystemEvents.DisplaySettingsChanged` | ~200 ms |
| 3 | `DispatcherTimer` polling ogni 3 secondi | max 3 s |

**Evento pubblico:** `ScreensChanged` — raised sul UI thread con `IReadOnlyList<ScreenInfo>`.

**`ScreenInfo`** (record interno):
- `Screen` — oggetto `WF.Screen` nativo
- `DisplayNumber` — numero progressivo (1, 2, ...)
- `IsPrimary` — true se schermo primario
- `DisplayLabel` — stringa localizzata (es. "Display 1 (Primary)" / "Display 1 (Principale)")

**Fingerprint:** confronta la stringa `DeviceName+Bounds` di tutti gli schermi per evitare notifiche duplicate.

---

### 5.2 PresenterSyncService

**File:** `Services/PresenterSyncService.cs`

Sincronizza il `FlowDocument` dall'editor al `PresenterWindow` con debounce 300ms.

**Flusso:**
1. `MarkDirty()` — chiamato da `TextChanged` dell'editor
2. `DispatcherTimer` (300ms, `Background` priority) — si resetta ad ogni chiamata
3. Alla scadenza: serializza il documento via `XamlPackage` (~30ms), crea clone, chiama `_applyToPresenter(clone)`

**Fallback:** se `XamlPackage` fallisce, usa `XamlWriter.Save` (~300ms).

**Importante:** mai clonare nel rendering tick o nel scroll tick.

---

### 5.3 DebouncedPreferencesService

**File:** `Services/DebouncedPreferencesService.cs`

Salva `UserPreferences` su disco con debounce 500ms per evitare micro-freeze durante il drag degli slider.

**Flusso:** ogni `SavePreferences()` in `MainWindow` → `_debouncedPrefs.Save(prefs)` → timer 500ms → `PreferencesService.Save(prefs)` (scrittura atomica).

---

### 5.4 LayoutPresetService

**File:** `Services/LayoutPresetService.cs`

Salva e carica fino a 4 snapshot di layout (`LayoutPreset`) in `layout-presets.json`.

- **`Load(slot)`** — legge slot 1–4 dal file JSON
- **`Save(slot, preset)`** — aggiorna lo slot nel file (legge tutti e 4, modifica, riscrive)
- Percorso: `AppPaths.BaseDirectory/layout-presets.json`
- Serializzazione: `System.Text.Json` con `camelCase`

---

## 6. Modelli dati

### 6.1 UserPreferences

**File:** `UserPreferences.cs`

Persistito in `preferences.json`. Tutti i campi sono nullable o hanno default.

| Proprietà | Tipo | Default | Descrizione |
|---|---|---|---|
| `CultureName` | `string?` | null | Lingua UI: "it" o "en" |
| `DocumentBackgroundHex` | `string?` | null | Colore sfondo hex |
| `TextForegroundHex` | `string?` | null | Colore testo hex |
| `FontFamily` | `string?` | null | Nome font |
| `FontSizePoints` | `double` | 72 | Dimensione font in pt |
| `IsBold` | `bool` | false | Grassetto |
| `IsItalic` | `bool` | false | Corsivo |
| `UseUnderline` | `bool` | false | Sottolineato |
| `DefaultScrollSpeed` | `double` | 0.5 | Velocità scroll iniziale |
| `MirrorEnabled` | `bool` | false | Mirror orizzontale |
| `TopMostEnabled` | `bool` | false | Sempre in primo piano |
| `PreferredDisplayNumber` | `int` | 0 | Monitor preferito |
| `LastScriptPath` | `string?` | null | Ultimo file aperto |
| `ArrowColorHex` | `string?` | null | Colore freccia |
| `ArrowScale` | `double` | 1.0 | Scala freccia |
| `ArrowHorizontalOffset` | `double` | 0.05 | Offset orizzontale freccia |
| `ArrowVerticalOffset` | `double` | 0.5 | Offset verticale freccia |
| `ArrowLeftPaddingExtra` | `double` | 12 | Padding extra sinistra freccia |
| `MarginTop/Right/Bottom/Left` | `double` | 40 | Margini documento (px) |
| `MarginsLinked` | `bool` | false | Margini collegati (cambio uniforme) |
| `EditModeEnabled` | `bool` | true | Modalità modifica attiva |
| `LastUpdatedUtc` | `DateTime` | now | Timestamp ultimo salvataggio |

---

### 6.2 LayoutPreset

**File:** `LayoutPreset.cs`

Snapshot del layout per i preset S1–S4 / L1–L4. Contiene un sottoinsieme di `UserPreferences` (no `CultureName`, `LastScriptPath`, `TopMostEnabled`, ecc.).

---

## 7. Localizzazione

**File:** `Localization.cs`

### Strategia

Sistema a dizionario statico, senza risorse RESX o binding XAML. Scelto per semplicità e stabilità (nessuna dipendenza esterna).

### Struttura

```csharp
static class Localization
{
    private static Dictionary<string, string> It = { ... };  // ~80 chiavi
    private static Dictionary<string, string> En = { ... };  // ~80 chiavi
    private static Dictionary<string, string> _current;      // punta a It o En

    static Initialize(string? fromInstaller, string? fromPrefs)
        → imposta _current e Thread.CurrentThread.CurrentUICulture

    static string Get(string key)
        → _current[key] ?? It[key] ?? key  (fallback IT, poi chiave raw)

    static string Get(string key, params object[] args)
        → string.Format(Get(key), args)
}
```

### Flusso determinazione lingua

```
App.OnStartup()
  │
  └─ Localization.Initialize(null, prefs.CultureName)
      ├─ UserPreferences.CultureName presente? → usa quella
      └─ No → default "it" (italiano)

Cambio lingua in-app: ComboBox IT/EN in toolbar → Localization.SwitchLanguage() → ApplyLocalization()
```

### Dove viene applicata

- **`App.xaml.cs`** — `Localization.Initialize()` all'avvio
- **`MainWindow.xaml.cs`** — `ApplyLocalization()` in `Window_Loaded`: imposta ToolTip e Text di tutti gli elementi UI
- **`MainWindow.xaml.cs`** — ogni `SetStatus(Localization.Get(...))` per messaggi di stato
- **`CompanionBridge.cs`** — messaggi di stato HTTP
- **`Services/DisplayManager.cs`** — `DisplayLabel` del `ScreenInfo`

### Aggiunta nuove chiavi

1. Aggiungere la chiave in entrambi i dizionari `It` e `En` in `Localization.cs`
2. Usare `Localization.Get("NuovaChiave")` nel codice
3. Se la chiave contiene placeholder: `Localization.Get("Chiave_{0}", valore)`

---

## 8. Integrazione NDI

**File:** `NDITransmitter.cs`, `NdiInterop.cs`

### Dipendenza runtime

Richiede **NewTek NDI SDK** (`ProcessNDI4.dll`) installato sul PC. Se non disponibile, il toggle NDI viene disabilitato senza crash.

### Architettura

```
CompositionTarget.Rendering (vsync)
  └── NDITransmitter.OnRendering()
        ├─ Stopwatch frame-rate limiter (default 30fps, min 5, max 120)
        ├─ RenderTargetBitmap (cached, riallocato solo a cambio risoluzione)
        ├─ VisualBrush (cached, aggiornato automaticamente da WPF)
        ├─ DrawingVisual (riusato, zero allocazioni per frame)
        └─ NdiInterop.SendFrame() → P/Invoke → ProcessNDI4.dll
```

### Ottimizzazioni

- **Buffer nativo pre-allocato** con `Marshal.AllocHGlobal` — pool crescente, mai riallocato se la risoluzione non cambia
- **VisualBrush cachato** — WPF aggiorna automaticamente il contenuto
- **Frame-rate limiter** via `Stopwatch` — evita oversending a monitor ad alto refresh rate

### Controllo via OSC

| Indirizzo | Azione |
|---|---|
| `/ndi/start` | Avvia streaming |
| `/ndi/stop` | Ferma streaming |
| `/ndi/toggle` | Inverte stato |
| `/ndi/resolution` | `int width, int height` |
| `/ndi/framerate` | `int fps` |
| `/ndi/sourcename` | `string name` |

---

## 9. Integrazione OSC

**File:** `OscBridge.cs`, `Osc/OscPacket.cs`

### Porte

| Porta | Direzione | Scopo |
|---|---|---|
| **8000** UDP | Ricezione | Comandi da controller OSC / Companion |
| **8001** UDP | Invio | Feedback di stato verso controller |

### Architettura

```
OscBridge
  ├─ UdpClient (porta 8000) — Task.Run(ListenLoopAsync)
  │   └─ OscPacket.Parse(ReadOnlySpan<byte>)
  │       ├─ OscMessage.TryParse() — singolo messaggio
  │       └─ OscBundle.TryParse() → lista di OscMessage
  │
  └─ _feedbackClient (UdpClient) — invia a 127.0.0.1:8001
      └─ SendFeedback(address, value)
```

### Parser OSC (`OscPacket.cs`)

Parser manuale su `ReadOnlySpan<byte>` senza allocazioni heap per messaggio. Supporta:
- Tipi: `i` (int32), `f` (float32), `s` (string), `b` (blob), `T`/`F` (bool)
- Bundle: `#bundle` con timestamp e lista di messaggi

### Comandi supportati (porta 8000)

| Indirizzo OSC | Argomenti | Azione |
|---|---|---|
| `/teleprompter/start` o `/play` | — | Avvia scroll |
| `/teleprompter/stop` o `/pause` | — | Ferma scroll |
| `/teleprompter/reset` | — | Torna all'inizio |
| `/teleprompter/speed` | `float` | Imposta velocità (-20 → +20) |
| `/teleprompter/speed/increase` | — | +0.25 |
| `/teleprompter/speed/decrease` | — | -0.25 |
| `/teleprompter/font/size` | `float` pt | Imposta dimensione font |
| `/teleprompter/font/increase` | — | +2 pt |
| `/teleprompter/font/decrease` | — | -2 pt |
| `/teleprompter/position` | `float` 0–1 | Imposta posizione scroll |
| `/teleprompter/jump/top` | — | Salta all'inizio |
| `/teleprompter/jump/bottom` | — | Salta alla fine |
| `/teleprompter/mirror` | `bool` | Imposta mirror |
| `/teleprompter/mirror/toggle` | — | Inverte mirror |
| `/teleprompter/status/request` | — | Richiede snapshot stato |
| `/ndi/*` | vari | Controllo NDI (vedi §8) |

### Feedback (porta 8001)

| Indirizzo | Tipo | Esempio |
|---|---|---|
| `/teleprompter/status` | string | `"playing"` / `"stopped"` |
| `/teleprompter/speed/current` | string | `"1.50"` |
| `/teleprompter/font/size/current` | string | `"72"` |
| `/teleprompter/position/current` | string | `"0.350"` |
| `/teleprompter/mirror/status` | string | `"true"` |
| `/ndi/status` | string | `"active"` / `"inactive"` |

---

## 10. HTTP REST API (CompanionBridge)

**File:** `CompanionBridge.cs`

### Endpoint base

`http://localhost:3131/teleprompter/`

### Avvio

`TryStart()` registra tre prefissi HTTP:
1. `http://localhost:3131/teleprompter/`
2. `http://127.0.0.1:3131/teleprompter/`
3. `http://+:3131/teleprompter/` (richiede admin o `netsh http add urlacl` — fallback silenzioso)

### CORS

Header `Access-Control-Allow-Origin: *` su tutte le risposte — compatibile con Companion remoto, browser, script.

### Endpoint

| Metodo | URL | Azione |
|---|---|---|
| GET | `/teleprompter/status` | JSON stato completo |
| GET/POST | `/teleprompter/play` | Avvia scroll |
| GET/POST | `/teleprompter/pause` | Ferma scroll |
| GET/POST | `/teleprompter/toggle` | Inverte play/pausa |
| GET/POST | `/teleprompter/speed/up` | +0.25 |
| GET/POST | `/teleprompter/speed/down` | -0.25 |
| GET/POST | `/teleprompter/speed/reset` | Azzera velocità |
| GET/POST | `/teleprompter/speed/set?value=X` | Imposta velocità esatta |

### Risposta JSON

```json
{
  "status": "ok",
  "message": "Playing",
  "isPlaying": true,
  "speed": 1.5,
  "editMode": false,
  "endpoint": "http://localhost:3131/teleprompter/"
}
```

### Loop asincrono

`Task.Run(ListenLoopAsync)` — ciclo `await _listener.GetContextAsync()` con `CancellationToken`. Ogni richiesta viene processata inline (nessun thread pool aggiuntivo — le richieste sono rare e veloci).

---

## 11. Scroll engine

**File:** `MainWindow.xaml.cs` — metodo `OnScrollRendering`

### Principio

Lo scroll usa `CompositionTarget.Rendering` (evento vsync-aligned, ~60Hz o refresh rate del monitor) invece di `DispatcherTimer` per eliminare micro-stutter.

### Algoritmo

```csharp
void OnScrollRendering(object sender, EventArgs e)
{
    // 1. Delta-time compensation
    var elapsed = _scrollStopwatch.Elapsed.TotalMilliseconds;
    _scrollStopwatch.Restart();
    if (elapsed > 500) elapsed = 16; // clamp: sistema in sleep/resume

    // 2. Accumulo pixel
    _scrollAccumulator += _scrollSpeed * elapsed / 1000.0;

    // 3. Dead zone 0.05px (non 1.0): scroll quasi ogni frame a velocità basse
    if (Math.Abs(_scrollAccumulator) < 0.05) return;

    // 4. Calcolo target e clamp
    var current = _contentScrollViewer.VerticalOffset;
    var target = current + _scrollAccumulator;
    var max = _contentScrollViewer.ScrollableHeight;
    var clamped = Math.Clamp(target, 0, max);

    // 5. Scroll fisico (pixel)
    _contentScrollViewer.ScrollToVerticalOffset(clamped);
    _scrollAccumulator = 0;

    // 6. Stop automatico a fine/inizio testo
    if (clamped >= max && _scrollSpeed > 0) StopScroll();
    if (clamped <= 0 && _scrollSpeed < 0) StopScroll();
}
```

### Regole invarianti

- **MAI** chiamare `UpdateLayout()` nel tick di scroll (causa micro-freeze 2–5ms/frame)
- **MAI** clonare il documento nel tick di scroll
- `SyncPresenterScroll` e `UpdateScrollProgressDisplay` solo in `ScrollChanged`, non nel tick
- `CanContentScroll="False"` su entrambi gli `ScrollViewer` — scroll fisico (pixel) non logico

---

## 12. Sincronizzazione Presenter

### Documento

```
MainWindow.TextChanged
  └─ RequestPresenterSync()
      └─ PresenterSyncService.MarkDirty()
          └─ DispatcherTimer 300ms
              └─ XamlPackage serialize → FlowDocument clone
                  └─ PresenterWindow.SetDocument(clone)
```

### Scroll

```
MainWindow.OnScrollRendering (ogni frame)
  └─ PresenterWindow.SetVerticalOffset(offset)
```

`ScrollChanged` ignora il sync se `_isAutoScrolling` è true (evita doppio sync).

### Aspetto

```
MainWindow (cambio colore/freccia/mirror)
  └─ SyncPresenterAppearance()
      └─ PresenterWindow.SetBackgroundColor()
         PresenterWindow.SetArrowColor()
         PresenterWindow.SetArrowScale()
         PresenterWindow.SetArrowAbsoluteY()
         PresenterWindow.SetMirror()
```

---

## 13. Gestione schermi (DisplayManager)

### Ciclo di vita

```
MainWindow.Window_Loaded
  └─ _displayManager = new DisplayManager(this)
      └─ _displayManager.ScreensChanged += OnScreensChanged
          └─ _displayManager.Start()
              ├─ Attach Win32 WM_DISPLAYCHANGE hook
              ├─ Subscribe SystemEvents.DisplaySettingsChanged
              └─ Start DispatcherTimer (3s)

MainWindow.Window_Closing
  └─ _displayManager.Dispose()
      ├─ Detach hook
      ├─ Unsubscribe SystemEvents
      └─ Stop timer
```

### `OnScreensChanged` (MainWindow)

1. Aggiorna `_screenInfos`
2. Rimuove i vecchi toggle button dal pannello
3. Crea nuovi `ToggleButton` per ogni schermo
4. Ripristina la selezione salvata in `_preferences.PreferredDisplayNumber`
5. Se il monitor del presenter è stato rimosso: sposta su alternativo o nasconde

---

## 14. Preferenze utente

### Persistenza

**Percorso:** `AppPaths.PreferencesPath` (vedi §16)  
**Formato:** JSON camelCase, indentato  
**Scrittura:** atomica via file `.tmp` + `File.Move(overwrite: true)` (NTFS atomic)

### Flusso lettura

```
PreferencesService.Load()
  ├─ Legge preferences.json → deserializza
  ├─ Se vuoto/corrotto: prova preferences.json.tmp (recovery)
  └─ Se tutto fallisce: new UserPreferences() (default)
```

### Flusso scrittura

```
MainWindow.SavePreferences()
  └─ DebouncedPreferencesService.Save(prefs)  [debounce 500ms]
      └─ PreferencesService.Save(prefs)
          ├─ Serializza JSON
          ├─ Scrive su preferences.json.tmp
          └─ File.Move(.tmp → preferences.json, overwrite: true)
```

---

## 15. Layout Preset

I preset S1–S4 (save) / L1–L4 (load) nella toolbar riga 2 permettono di salvare e ripristinare snapshot completi del layout.

**Dati salvati:** colori, font, dimensione, grassetto/corsivo/sottolineato, velocità, mirror, colore/scala/posizione freccia, margini.

**Non salvati nei preset:** lingua, file aperto, monitor preferito, topmost, modalità modifica.

**File:** `layout-presets.json` in `AppPaths.BaseDirectory` — array JSON di 4 elementi (slot 1–4).

---

## 16. Percorsi file (AppPaths)

**File:** `AppPaths.cs`

Determina la directory base per preferenze e log in base alla modalità di esecuzione:

| Modalità | Condizione | Directory base |
|---|---|---|
| **Portable** | exe fuori da `%LocalAppData%`, `Program Files` | Directory dell'exe (es. USB, desktop) |
| **Installato** | exe in `%LocalAppData%` o `Program Files` | `%APPDATA%\Live Speaker Teleprompter` |

**Percorsi derivati:**

| Percorso | Valore |
|---|---|
| `AppPaths.PreferencesPath` | `{BaseDirectory}\preferences.json` |
| `AppPaths.LogDirectory` | `{BaseDirectory}\logs\` |
| `LayoutPresetService.PresetsPath` | `{BaseDirectory}\layout-presets.json` |

**Comportamento portable:** nessuna traccia sul PC host — preferenze e log restano accanto all'exe (ideale per USB).

---

## 17. Build e packaging

### Script principale

`clean-and-build.ps1` (root):
1. Pulisce `bin/`, `obj/`, `release/` (e rimuove cartella `portable/` obsoleta se presente)
2. Genera icona: `scripts\convert-logo.ps1` (`icons\Logo Teleprompter.png` → `icons\app-icon.ico`)
3. `dotnet restore`
4. Chiama `installer\build-installer.ps1`

### Pipeline `build-installer.ps1`

**Step 1 — Publish:**
```
dotnet publish -c Release
→ src\TeleprompterApp\bin\Release\net8.0-windows\win-x64\publish\
  └─ TeleprompterApp.exe (rinominato in "Live Speaker Teleprompter.exe")
```

**Step 2 — Portable base:**
```
Copia "Live Speaker Teleprompter.exe"
→ release\Live_Speaker_Teleprompter_Portable.exe
```

**Step 3 — Installer:**

Funzione analoga con `installer-template.ps1`:
```
1. Base64(publish dir zip) → ##EMBEDDED_ZIP## nell'installer template
2. iexpress.exe → Live_Speaker_Teleprompter_Setup.exe
```

### Output finale (`release/`) — 2 file

| File | Dimensione | Descrizione |
|---|---|---|
| `Live_Speaker_Teleprompter_Portable.exe` | ~73 MB | Portable standalone IT+EN, selezione lingua in-app |
| `Live_Speaker_Teleprompter_Setup.exe` | ~73 MB | Installer con UI cartella + shortcuts |

### Installer (`installer-template.ps1`)

1. **Lingua UI installer** — usa lingua di sistema (Get-UICulture), nessuna scelta utente
2. **Selezione cartella** — default `%LocalAppData%\Live Speaker\Live Speaker Teleprompter`
3. **Estrazione** — `payload.zip` (base64 embedded) → cartella scelta
4. **Shortcuts** — Start menu e/o Desktop (opzionali)
5. **Uninstaller** — `Uninstall.ps1` nella cartella di installazione
6. **Registro Windows** — chiave `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\LiveSpeakerTeleprompter` per "Aggiungi/Rimuovi programmi"

---

## 18. Dipendenze e configurazione progetto

**File:** `TeleprompterApp.csproj`

### Dipendenze NuGet

Nessuna dipendenza NuGet esterna. Il progetto usa esclusivamente:
- `Microsoft.NET.Sdk` (WPF + Windows Forms)
- `System.Text.Json` (incluso in .NET 8)
- `System.Net.Http` (incluso in .NET 8)
- NDI SDK via P/Invoke (`NdiInterop.cs`) — DLL esterna opzionale

### Configurazione Release

```xml
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<SelfContained>true</SelfContained>
<PublishSingleFile>true</PublishSingleFile>
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
<PublishReadyToRun>true</PublishReadyToRun>
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
<ServerGarbageCollection>true</ServerGarbageCollection>
<TieredCompilation>true</TieredCompilation>
<TieredPGO>true</TieredPGO>
```

### Manifest applicazione

`app.manifest` con:
- `dpiAware: true/PM` — DPI awareness per-monitor v2
- `ApplicationHighDpiMode: PerMonitorV2` nel csproj

---

## 19. Principi architetturali e regole invarianti

### Regole fondamentali

1. **MainWindow = unica fonte di verità** — tutto lo stato dell'applicazione vive in MainWindow. PresenterWindow è un clone read-only.

2. **Stabilità live è priorità n.1** — mai crashare durante un evento. `try/catch` su ogni operazione I/O, di rete e di rendering. Fallback silenzioso dove possibile.

3. **Nessun MVVM framework** — la logica è nel code-behind con servizi separati. Aggiunge complessità senza benefici per questa architettura.

4. **Preferenze debounced** — ogni cambio UI chiama `SavePreferences()` → debounce 500ms → scrittura atomica. Mai scrivere su disco nel rendering tick.

5. **Scroll vsync-aligned** — `CompositionTarget.Rendering` con delta-time compensation. Mai `DispatcherTimer` per lo scroll. Mai `UpdateLayout()` nel tick.

6. **Dispose esplicito** — tutti i servizi implementano `IDisposable`. `Window_Closing` fa dispose di tutto: DisplayManager, PresenterSync, CompanionBridge, NDI, OSC.

7. **Scrittura atomica** — ogni file scritto su disco usa il pattern `.tmp` + `File.Move(overwrite: true)`.

8. **Localizzazione via dizionario** — nessun RESX, nessun binding XAML per le stringhe. `Localization.Get(key)` ovunque. Aggiungere sempre la chiave in entrambi i dizionari IT e EN.

9. **NDI opzionale** — se `ProcessNDI4.dll` non è presente, il toggle viene disabilitato. Mai crash per NDI mancante.

10. **Freeze dei Brush** — tutti i `SolidColorBrush` creati dinamicamente vanno freezati con `.Freeze()` per evitare memory leak.

### Checklist pre-release

- [ ] Testare con documento vuoto, 1 riga, 100+ righe
- [ ] Testare hot-plug monitor (connetti/disconnetti durante scroll)
- [ ] Testare velocità negative (scroll inverso)
- [ ] Testare con DPI diversi (100%, 125%, 150%, 200%)
- [ ] Testare mirror mode con freccia e margini
- [ ] Verificare che lo scroll si fermi correttamente a fine/inizio testo
- [ ] Testare installer ITA e ENG (lingua corretta al primo avvio)
- [ ] Testare portable ITA e ENG (lingua corretta al primo avvio)
- [ ] Verificare uninstaller in "Aggiungi/Rimuovi programmi"
- [ ] Verificare che NDI si disabiliti correttamente senza SDK installato
