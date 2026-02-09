# R-Speaker Teleprompter — Refactor Plan

> Generato: 6 febbraio 2026  
> Stato: **COMPLETATO**

---

## Indice

1. [Fase 1 — Monitor Real-Time Detection](#fase-1--monitor-real-time-detection)
2. [Fase 2 — God Class Split (MainWindow 2629→600 righe)](#fase-2--god-class-split)
3. [Fase 3 — Performance NDI](#fase-3--performance-ndi)
4. [Fase 4 — Performance & Stabilità](#fase-4--performance--stabilità)
5. [Fase 5 — React Web Frontend](#fase-5--react-web-frontend)

---

## Fase 1 — Monitor Real-Time Detection

**Priorità: CRITICA**  
**Problema:** `PopulateMonitorList()` viene chiamata una sola volta in `Window_Loaded`. Nessun hook per hotplug monitor (collegamento/scollegamento). La UI non si aggiorna mai.

### Operazioni

- [x] **1.1** Creare `DisplayManager.cs` — servizio dedicato alla gestione schermi
  - Hook `WM_DISPLAYCHANGE` via `HwndSource.AddHook`
  - Listener `SystemEvents.DisplaySettingsChanged` come backup
  - Polling `DispatcherTimer` ogni 3 secondi come rete di sicurezza
  - Fingerprint schermi per evitare refresh inutili
  - Evento `ScreensChanged` per notificare la UI
- [x] **1.2** Refactoring `PopulateMonitorList()` → consumare `DisplayManager.ScreensChanged`
  - Rigenerare toggle buttons dinamicamente
  - Preservare la selezione corrente se lo schermo è ancora presente
- [x] **1.3** Gestione fallback disconnessione
  - Se lo schermo della PresenterWindow viene scollegato → sposta su alternativo
  - Notifica utente con messaggio di stato
- [x] **1.4** DPI-awareness completa
  - PresenterWindow usa Bounds (fullscreen) invece di WorkingArea
  - Deferred re-apply quando PresentationSource non è ancora disponibile
  - Aggiunto `CurrentScreenDeviceName` tracking

---

## Fase 2 — God Class Split

**Priorità: ALTA**  
**Problema:** `MainWindow.xaml.cs` è 2629 righe — God Class che gestisce tutto. Impossibile testare, fragile, difficile da manutenere.

### Operazioni

- [ ] **2.1** Creare `TeleprompterState.cs` — state object centralizzato *(deferred — servizi integrati direttamente)*
  - Implementare `INotifyPropertyChanged`
  - Proprietà: `ScrollSpeed`, `IsPlaying`, `IsMirrored`, `FontSizePoints`, `IsEditMode`, `CurrentDocumentPath`, etc.
  - Tutti i servizi leggono/scrivono sullo state
- [x] **2.2** Scroll con delta-time (Stopwatch) integrato in `OnScrollTimerTick`
  - `Stopwatch.Restart()` ad ogni tick
  - Formula: `scrollDelta = speed * (elapsed / 16.0)`
  - Clamp per stalli > 500ms (es. sleep sistema)
- [ ] **2.3** Creare `DocumentService.cs` *(deferred — non critico)*
  - Estrarre `OpenDocument`, `LoadDocument`, `SaveDocument`, `TrySaveDocument`
- [ ] **2.4** Creare `AppearanceService.cs` *(deferred — non critico)*
  - Estrarre gestione font, colori, freccia, mirror
- [x] **2.5** Estrarre `CompanionBridge` da inner class a file proprio
  - Spostato in `CompanionBridge.cs`
  - Creato API pubblica: `IsPlaying`, `CurrentScrollSpeed`, `SetPlayState()`, `SetSpeed()`, `AdjustSpeed()`, `SetStatus()`
  - MainWindow metodi da private → internal
- [x] **2.6** Creare `DebouncedPreferencesService.cs`
  - Debounce 500ms con `DispatcherTimer`
  - Fallback sync su shutdown via `Flush()`
- [x] **2.7** Creare `PresenterSyncService.cs`
  - Debounce 300ms document cloning
  - `TextRange.Save/Load` con `DataFormats.XamlPackage` (più veloce)
  - Fallback a `XamlWriter/XamlReader` in caso di errore

### Struttura file risultante

```
src/TeleprompterApp/
  Services/
    DisplayManager.cs        (Fase 1) ✅
    DebouncedPreferencesService.cs (Fase 2.6) ✅
    PresenterSyncService.cs  (Fase 2.7) ✅
  CompanionBridge.cs         (Fase 2.5) ✅
  NDITransmitter.cs          (refactored) ✅
  NdiInterop.cs              (invariato)
  OscBridge.cs               (invariato)
  MainWindow.xaml.cs         (~2460 righe — servizi integrati)
  PresenterWindow.xaml.cs    (DPI fix) ✅
```

---

## Fase 3 — Performance NDI

**Priorità: ALTA**  
**Problema:** `NDITransmitter.OnTick` alloca `RenderTargetBitmap` + `DrawingVisual` + `VisualBrush` ogni frame → GC pressure, frame drops durante interazione UI.

### Operazioni

- [x] **3.1** Riutilizzare `RenderTargetBitmap` — riallocare solo se risoluzione cambia
  - Campo `_cachedBitmap`, `_cachedWidth`, `_cachedHeight`
  - `Clear()` + `Render()` al posto di `new` ogni frame
- [x] **3.2** Riutilizzare `DrawingVisual` come campo
  - `_reusableVisual` istanziato una volta nel costruttore
- [x] **3.3** Usare `CompositionTarget.Rendering` al posto di `DispatcherTimer`
  - Sync perfetto con vsync WPF
  - `Stopwatch` + frame-rate limiter per FPS target < refresh rate
- [x] **3.4** Pre-allocare buffer nativo con `Marshal.AllocHGlobal`
  - `EnsureBuffer()` mantiene pool crescente (già esistente, preservato)

---

## Fase 4 — Performance & Stabilità

**Priorità: MEDIA**

### Operazioni

- [x] **4.1** Debounce `SyncPresenterDocument()`
  - `PresenterSyncService` con debounce 300ms
  - `MarkDirty()` su TextChanged, `SyncNow()` per caricamento documenti
  - `TextRange.Save/Load` con `DataFormats.XamlPackage` (più veloce, fallback XamlWriter)
- [x] **4.2** Debounce `SavePreferences()`
  - `DebouncedPreferencesService` con debounce 500ms
  - `Flush()` sincrono su shutdown
- [x] **4.3** Scroll con delta-time compensato
  - `Stopwatch` in `OnScrollTimerTick`
  - Formula: `scrollDelta = speed * (elapsed / 16.0)`
  - Clamp per stalli > 500ms
  - Start/Stop stopwatch sincronizzato con PlayPause toggle
- [x] **4.4** DPI-aware PresenterWindow
  - `Bounds` al posto di `WorkingArea` (fullscreen)
  - Deferred `ApplyScreenBounds` se `PresentationSource` è null
  - `CurrentScreenDeviceName` per tracking schermo corrente

---

## Fase 5 — React Web Frontend

**Priorità: BASSA** (la stack WPF è primaria)

### Operazioni

- [x] **5.1** Virtualizzazione TextDisplay
  - Implementata virtualizzazione custom (overscan 10 righe sopra/sotto viewport)
  - Container con `height: totalHeight`, contenuto posizionato con `position: absolute + top: offsetTop`
  - `scroll` event listener + `ResizeObserver` per aggiornare range visibile
  - Key stabile `absoluteIndex` invece di `line-${index}`
- [x] **5.2** Fix debounce ControlPanel
  - Custom hook `useDebouncedCallback` con timer indipendente per handler
  - `callbackRef.current` per evitare stale closures
  - Cleanup automatico su unmount
- [ ] **5.3** Cleanup dead code Electron *(deferred — non critico)*
  - `ndiHandler.js` usa `require()` non disponibile in browser
  - `oscServer.js` duplica funzionalità già in WPF
  - Documentare o rimuovere
- [x] **5.4** TextDisplay pure rendering
  - Virtualization naturalmente previene re-render di righe fuori viewport

---

## Riepilogo impatto atteso

| Metrica | Prima | Dopo | Status |
|---|---|---|---|
| **Monitor hotplug** | Non rilevato | Real-time <500ms (3 layer) | ✅ |
| **MainWindow.cs** | 2629 righe, 1 file | ~2460 righe + 4 servizi | ✅ (parziale) |
| **NDI frame drops** | DispatcherTimer Background, alloc ogni frame | CompositionTarget.Rendering + cache | ✅ |
| **Presenter sync** | XamlWriter ogni keystroke (~300ms) | Debounced 300ms + XamlPackage (~30ms) | ✅ |
| **Preferenze I/O** | Write sincrona ogni modifica | Debounced 500ms + Flush on exit | ✅ |
| **Scroll smoothness** | Timer-dependent senza delta | Delta-time compensated (Stopwatch) | ✅ |
| **React DOM (web)** | 1000+ nodi reali | Virtualizzato, ~30 visibili + overscan | ✅ |
| **React debounce** | Timer condiviso, handler annullati | Timer indipendenti per handler | ✅ |
| **DPI PresenterWindow** | WorkingArea + null PresentationSource | Bounds + deferred re-apply | ✅ |

---

## Log Operazioni

| Data | Operazione | Stato |
|---|---|---|
| 2026-02-06 | Piano creato | ✅ |
| 2026-02-06 | `Services/DisplayManager.cs` creato (1.1) | ✅ |
| 2026-02-06 | `Services/DebouncedPreferencesService.cs` creato (2.6/4.2) | ✅ |
| 2026-02-06 | `Services/PresenterSyncService.cs` creato (2.7/4.1) | ✅ |
| 2026-02-06 | `CompanionBridge.cs` estratto da inner class (2.5) | ✅ |
| 2026-02-06 | MainWindow integrato con DisplayManager, servizi debounced (1.2, 1.3) | ✅ |
| 2026-02-06 | Delta-time scroll implementato (4.3) | ✅ |
| 2026-02-06 | MonitorOption → ScreenInfo migration | ✅ |
| 2026-02-06 | NDITransmitter: CompositionTarget.Rendering + cached bitmap (3.1-3.4) | ✅ |
| 2026-02-06 | PresenterWindow: DPI fix + Bounds + deferred apply (4.4) | ✅ |
| 2026-02-06 | React TextDisplay: virtualizzazione custom (5.1) | ✅ |
| 2026-02-06 | React ControlPanel: debounce fix con hook indipendenti (5.2) | ✅ |
| 2026-02-06 | Build C# (.NET 8): 0 errori, 0 warning | ✅ |
| 2026-02-06 | Build React/Vite: 0 errori | ✅ |
| 2026-02-06 | **Review fix**: NDITransmitter try/catch in OnRendering | ✅ |
| 2026-02-06 | **Review fix**: Window_Closing ordine salvataggio preferenze | ✅ |
| 2026-02-06 | **Review fix**: RebuildMonitorToggles preserva schermo selezionato | ✅ |
| 2026-02-06 | **Review fix**: TextDisplay scrollTo disabilitato durante auto-scroll | ✅ |
| 2026-02-06 | **Review fix**: ControlPanel slider con stato locale (feedback immediato) | ✅ |
| 2026-02-06 | Documentazione aggiornata (README, struttura, versioni) | ✅ |
