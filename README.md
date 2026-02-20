# R-Speaker Teleprompter

Teleprompter professionale per presentazioni multi-schermo. Singolo eseguibile portable, nessuna installazione richiesta.  
Integrazione completa con **Bitfocus Companion**, **NDI** e **OSC** per il controllo remoto in produzione live.

> **Versione 2.0.0** — Self-contained .NET 8 / WPF — Windows 10/11 x64

---

## Indice

- [Funzionalità](#funzionalità)
- [Requisiti di sistema](#requisiti-di-sistema)
- [Installazione e avvio](#installazione-e-avvio)
- [Comandi da tastiera](#comandi-da-tastiera)
- [Gestione schermi esterni](#gestione-schermi-esterni)
- [Streaming NDI](#streaming-ndi)
- [Controllo remoto — HTTP REST API](#controllo-remoto--http-rest-api)
- [Controllo remoto — OSC](#controllo-remoto--osc)
- [Integrazione Bitfocus Companion](#integrazione-bitfocus-companion)
- [Formati file supportati](#formati-file-supportati)
- [Preferenze utente](#preferenze-utente)
- [Build da sorgente](#build-da-sorgente)
- [Architettura del progetto](#architettura-del-progetto)
- [Ottimizzazioni performance](#ottimizzazioni-performance)
- [Licenza](#licenza)

---

## Funzionalità

### Editor e presentazione
- **Editor rich text** con grassetto, corsivo, sottolineato, allineamento e scelta rapida della dimensione font (36–120 pt).
- **Formato nativo `.rstp`** (XamlPackage) che conserva formattazione, colori, font e stile. Supporto anche RTF, testo semplice e sottotitoli.
- **Scorrimento automatico fluido** con compensazione delta-time (Stopwatch). Velocità regolabile da **-20** a **+20** con step di 0,25.
- **Freccia guida draggable** con dimensione, colore e margine personalizzabili. Sincronizzata sul presenter.
- **Modalità modifica / presentazione** — toggle rapido che blocca la tastiera per evitare modifiche accidentali durante il live.
- **Drag & drop** di file di testo, RTF e sottotitoli direttamente nella finestra.

### Multi-schermo
- **Rilevamento real-time degli schermi** con tripla ridondanza: Win32 `WM_DISPLAYCHANGE`, `SystemEvents.DisplaySettingsChanged`, polling ogni 3 secondi.
- **Hotplug completo** — collega/scollega monitor durante l'uso, l'app reagisce istantaneamente.
- **DPI-aware** — posizionamento preciso su display con scaling diversi (100%, 125%, 150%, 200%).
- **Fallback automatico** — se il monitor del presenter viene scollegato, la finestra si sposta su uno alternativo.
- **Toggle per schermo** — un bottone dedicato per ogni display collegato, con selezione visiva.

### Controllo remoto
- **HTTP REST API** su porta 3131 per Bitfocus Companion e automazione esterna.
- **OSC UDP** su porte 8000 (ricezione) e 8001 (feedback) per controller hardware e software.
- **CORS abilitato** — il server HTTP accetta connessioni da qualsiasi origine (network, browser, Companion remoto).
- **Modulo Companion dedicato** con 23 azioni, 5 feedback, 4 variabili, preset pronti e auto-reconnect.

### Output video
- **NewTek NDI** — streaming diretto del prompt verso OBS, TriCaster, vMix o qualsiasi client NDI.
- Cattura vsync-aligned tramite `CompositionTarget.Rendering` con bitmap e buffer cachati.
- Risoluzione e framerate configurabili via OSC (Full HD, HD, 4K, 25–60 fps).

### Portable
- **Eseguibile singolo self-contained** (~67 MB). Nessun runtime .NET richiesto.
- **Solo exe** — nessun file da estrarre, nessuna installazione. Lancia e usa.
- **Preferenze e log** accanto all'exe quando esegui da USB/desktop (nessuna traccia sul PC host).
- **Installer** per chi preferisce collegamenti Start/Desktop.
- **ReadyToRun** pre-compilato per startup ~40% più rapido.

---

## Requisiti di sistema

| Requisito | Dettaglio |
|-----------|----------|
| **Sistema operativo** | Windows 10 (build 1903+) o Windows 11 — x64 |
| **RAM** | 4 GB minimo, 8 GB consigliati |
| **Disco** | ~150 MB (app + preferenze) |
| **Schermi** | 1 minimo, 2+ consigliati per l'uso in produzione |
| **.NET Runtime** | **Non richiesto** — incluso nell'eseguibile |
| **NDI** (opzionale) | [NewTek NDI Tools/Runtime](https://www.ndi.tv/tools/) per lo streaming video |

---

## Installazione e avvio

### Versione portable (consigliata)

1. Scarica `R-Speaker-Teleprompter-Portable.exe` dalla cartella `portable/` (oppure lo ZIP).
2. Esegui l'exe — nessuna estrazione, nessuna installazione, nessuna dipendenza.
3. Preferenze e log restano accanto all'exe (chiavetta USB = nessuna traccia sul PC host).

### Versione installer

1. Esegui `R-Speaker-Teleprompter-Installer.exe`.
2. Scegli la cartella di installazione (default: `%LOCALAPPDATA%\R-Speaker\R-Speaker Teleprompter`).
3. L'installer crea un collegamento sul desktop e nel menu Start.

### Avvio

L'app si apre a **schermo intero** con velocità di scorrimento predefinita a **0,5** e modalità modifica attiva. Carica un file o inizia a scrivere direttamente nell'editor.

---

## Comandi da tastiera

| Tasto | Funzione |
|-------|----------|
| `Spazio` | Play / Pausa |
| `Freccia Su` | Aumenta velocità (+0,25) |
| `Freccia Giù` | Diminuisce velocità (-0,25) |
| `Freccia Sinistra / Destra` | Azzera la velocità |
| `Home` | Vai all'inizio del testo |
| `End` | Vai alla fine del testo |
| `Rotellina mouse` | Regola velocità (in modalità presentazione) |
| `Ctrl + O` | Apri file |
| `Ctrl + S` | Salva |
| `Ctrl + N` | Nuovo documento |

> **Nota:** i tasti freccia e rotellina controllano la velocità solo in **modalità presentazione**. In modalità modifica, il comportamento standard dell'editor è preservato.

---

## Gestione schermi esterni

### Come funziona

Quando colleghi un secondo monitor (o proiettore, o TV), l'app lo rileva automaticamente e mostra un toggle dedicato nel pannello "Schermi". Cliccando il toggle, la finestra **Presenter** si apre a schermo intero sul display scelto.

### Tripla ridondanza di rilevamento

| Layer | Tecnologia | Latenza |
|-------|-----------|---------|
| 1 | Win32 `WM_DISPLAYCHANGE` hook | ~50 ms |
| 2 | .NET `SystemEvents.DisplaySettingsChanged` | ~200 ms |
| 3 | Polling `DispatcherTimer` | ogni 3 secondi |

Questo garantisce il rilevamento anche su tablet, docking station, adattatori USB-HDMI o connessioni wireless (Miracast, AirPlay).

### Comportamento hotplug

- **Monitor aggiunto**: compare un nuovo toggle, selezionabile con un click.
- **Monitor rimosso**: se il Presenter era su quel display, viene automaticamente spostato su un monitor alternativo. Se non ci sono alternative, viene nascosto.
- **Monitor preferito salvato**: la selezione viene ricordata nelle preferenze e ripristinata al riavvio.

### DPI e scaling

La finestra Presenter usa le coordinate fisiche (`Bounds`) del display per posizionarsi correttamente indipendentemente dallo scaling Windows (100%, 125%, 150%, 200%). Se il `PresentationSource` non è ancora disponibile al momento dell'apertura, viene schedulato un re-apply dopo il primo render.

---

## Streaming NDI

### Attivazione

1. Installa il [NewTek NDI Tools/Runtime](https://www.ndi.tv/tools/) sul PC.
2. Nell'app, clicca il toggle **NDI** nel pannello "Schermi".
3. Il flusso apparirà come **"R-Speaker NDI"** su qualsiasi client NDI della rete locale.

### Caratteristiche tecniche

| Parametro | Valore default | Intervallo |
|-----------|---------------|------------|
| Nome sorgente | `R-Speaker NDI` | Configurabile via OSC |
| Risoluzione | Segue la finestra | Configurabile: 1280×720, 1920×1080, 3840×2160 |
| Framerate | 30 fps | 5–120 fps |
| Formato pixel | BGRA 32-bit | Progressivo |
| Sync | CompositionTarget.Rendering (vsync) | Frame-rate limiter via Stopwatch |

### Controllo NDI via OSC

| Indirizzo OSC | Argomenti | Descrizione |
|---------------|-----------|-------------|
| `/ndi/start` | — | Avvia lo streaming NDI |
| `/ndi/stop` | — | Ferma lo streaming NDI |
| `/ndi/toggle` | — | Inverte lo stato NDI |
| `/ndi/resolution` | `int width`, `int height` | Imposta risoluzione (es. 1920, 1080) |
| `/ndi/framerate` | `int fps` | Imposta framerate (es. 30) |
| `/ndi/sourcename` | `string name` | Cambia il nome della sorgente NDI |
| `/ndi/status/request` | — | Richiede lo stato corrente |

### Feedback NDI (porta 8001)

| Indirizzo | Tipo | Esempio |
|-----------|------|---------|
| `/ndi/status` | string | `"active"` / `"inactive"` |
| `/ndi/available` | string | `"yes"` / `"no"` |
| `/ndi/resolution/current` | string | `"1920x1080"` |
| `/ndi/framerate/current` | string | `"30.00"` |
| `/ndi/sourcename/current` | string | `"R-Speaker NDI"` |

---

## Controllo remoto — HTTP REST API

L'app espone un endpoint HTTP su `http://localhost:3131/teleprompter/` avviato automaticamente. Compatibile con Bitfocus Companion, cURL, browser, script e qualsiasi client HTTP.

### CORS

Le intestazioni CORS sono abilitate (`Access-Control-Allow-Origin: *`), quindi l'API è utilizzabile anche da pagine web, Companion remoto e applicazioni cross-origin.

### Endpoint disponibili

| Comando | Metodo | URL | Descrizione |
|---------|--------|-----|-------------|
| Stato | GET | `/teleprompter/status` | JSON con stato completo |
| Play | GET/POST | `/teleprompter/play` | Avvia lo scorrimento |
| Pausa | GET/POST | `/teleprompter/pause` | Ferma lo scorrimento |
| Toggle | GET/POST | `/teleprompter/toggle` | Inverte play/pausa |
| Velocità + | GET/POST | `/teleprompter/speed/up` | +0,25 |
| Velocità − | GET/POST | `/teleprompter/speed/down` | −0,25 |
| Velocità 0 | GET/POST | `/teleprompter/speed/reset` | Azzera |
| Imposta velocità | GET/POST | `/teleprompter/speed/set?value=1.5` | Valore esatto (punto decimale) |

### Formato risposta JSON

```json
{
  "status": "ok",
  "message": "Riproduzione avviata",
  "isPlaying": true,
  "speed": 1.5,
  "editMode": false,
  "endpoint": "http://localhost:3131/teleprompter/"
}
```

### Esempio con cURL

```bash
# Avvia lo scorrimento
curl http://localhost:3131/teleprompter/play

# Imposta velocità a 2.0
curl "http://localhost:3131/teleprompter/speed/set?value=2.0"

# Ottieni lo stato
curl http://localhost:3131/teleprompter/status
```

---

## Controllo remoto — OSC

L'app riceve comandi OSC su UDP porta **8000** e invia feedback su porta **8001** (indirizzo 127.0.0.1 di default).

### Comandi OSC (porta 8000)

| Indirizzo | Argomenti | Descrizione |
|-----------|-----------|-------------|
| `/teleprompter/start` | — | Avvia scorrimento |
| `/teleprompter/play` | — | Avvia scorrimento (alias) |
| `/teleprompter/stop` | — | Ferma scorrimento |
| `/teleprompter/pause` | — | Ferma scorrimento (alias) |
| `/teleprompter/reset` | — | Torna all'inizio del testo |
| `/teleprompter/speed` | `float` | Imposta velocità (range: -20.0 a +20.0) |
| `/teleprompter/speed/increase` | — | +0,25 |
| `/teleprompter/speed/decrease` | — | −0,25 |
| `/teleprompter/font/size` | `float` (pt) | Imposta dimensione font (20–200) |
| `/teleprompter/font/increase` | — | +2 pt |
| `/teleprompter/font/decrease` | — | −2 pt |
| `/teleprompter/position` | `float` (0.0–1.0) | Imposta posizione scroll (0=inizio, 1=fine) |
| `/teleprompter/jump/top` | — | Salta all'inizio |
| `/teleprompter/jump/bottom` | — | Salta alla fine |
| `/teleprompter/mirror` | `bool` | Imposta stato mirror |
| `/teleprompter/mirror/toggle` | — | Inverte lo stato mirror |
| `/teleprompter/status/request` | — | Richiede snapshot completo di stato |

### Feedback OSC (porta 8001)

| Indirizzo | Tipo | Esempio |
|-----------|------|---------|
| `/teleprompter/status` | string | `"playing"` / `"stopped"` |
| `/teleprompter/speed/current` | string | `"1.50"` |
| `/teleprompter/font/size/current` | string | `"72"` |
| `/teleprompter/position/current` | string | `"0.350"` |
| `/teleprompter/mirror/status` | string | `"true"` / `"false"` |

---

## Integrazione Bitfocus Companion

Vedi la guida completa: [docs/COMPANION_SETUP_GUIDE.md](docs/COMPANION_SETUP_GUIDE.md)

Il modulo si trova nella cartella `companion-module/` e offre:
- **23 azioni** (playback, velocità, font, navigazione, mirror, NDI, output mode)
- **5 feedback** (playing, speed, mirror, NDI active, NDI available)
- **4 variabili** (speed, playing, mirrored, ndi_active)
- **10 preset** pronti per Stream Deck (Play, Stop, Reset, Toggle, Speed+/−, NDI Toggle, Output modes)
- **Auto-reconnect** ogni 5 secondi in caso di disconnessione
- **Status request** automatico alla connessione

### Installazione rapida

1. Copia la cartella `companion-module/` nel path dei moduli custom di Companion.
2. Riavvia Companion, aggiungi connessione **"R-Speaker Teleprompter"**.
3. Inserisci la porta OSC (default: `8000`) e l'indirizzo della macchina (default: `127.0.0.1`).
4. I preset appaiono nella libreria, pronti per il drag & drop.

---

## Formati file supportati

### Apertura

| Formato | Estensioni | Note |
|---------|-----------|------|
| Documento Teleprompter | `.rstp` | Formato nativo, conserva tutta la formattazione |
| Rich Text Format | `.rtf` | Formattazione base |
| FlowDocument XAML | `.xaml`, `.xamlpackage` | Formato WPF nativo |
| Testo semplice | `.txt`, `.md`, `.log` | Convertito con font corrente |
| Sottotitoli | `.srt`, `.vtt` | Caricati come testo |
| Dati strutturati | `.csv`, `.json`, `.xml`, `.yaml`, `.yml` | Caricati come testo |
| Markup | `.html`, `.htm` | Caricato come testo |
| Script | `.bat`, `.ps1`, `.ini`, `.cfg` | Caricati come testo |

### Salvataggio

| Formato | Estensione | Conserva formattazione |
|---------|-----------|----------------------|
| Documento Teleprompter | `.rstp` | Completa (colori, font, stile) |
| Rich Text Format | `.rtf` | Parziale |
| FlowDocument XAML | `.xaml` | Completa |
| Testo semplice | `.txt` | Solo testo |

> **Suggerimento:** Salva sempre in `.rstp` per preservare la formattazione completa. Se scegli un formato senza formattazione, l'app chiederà se vuoi convertire in `.rstp`.

---

## Preferenze utente

Le preferenze vengono salvate automaticamente (con debounce 500 ms) in:

```
%APPDATA%\R-Speaker Teleprompter\preferences.json
```

### Dati salvati

- Colore sfondo e testo
- Font, dimensione, grassetto, corsivo, sottolineato
- Velocità di scorrimento predefinita
- Stato mirror e sempre-in-primo-piano
- Monitor preferito (numero display)
- Ultimo file aperto (ripristinato al riavvio)
- Posizione, dimensione e colore della freccia guida
- Margine sinistro personalizzato
- Stato modalità modifica

### Reset

Per ripristinare le impostazioni predefinite, elimina il file `preferences.json`.

### Log errori

I log degli errori vengono salvati in:

```
%APPDATA%\R-Speaker Teleprompter\logs\error-YYYYMMDD-HHmmss.log
```

Vengono mantenuti solo gli ultimi 10 file di log. I log più vecchi vengono eliminati automaticamente all'avvio.

---

## Build da sorgente

### Prerequisiti per lo sviluppo

- [.NET 8 SDK](https://dotnet.microsoft.com/download) (solo per compilare, non richiesto dall'utente finale)
- Windows 10/11 x64

### Comandi

```powershell
# Compilazione rapida
cd src\TeleprompterApp
dotnet build

# Avvio in Development
dotnet run

# Publish Release (self-contained, ReadyToRun, compresso)
dotnet publish -c Release

# Build completa (portable ZIP + installer EXE)
cd installer
.\build-installer.ps1
```

### Output del build

Il build script produce tre file nella cartella `portable/`:

| File | Dimensione | Descrizione |
|------|-----------|-------------|
| `R-Speaker Teleprompter.exe` | ~73 MB | Eseguibile diretto (copia dalla publish) |
| `R-Speaker-Teleprompter-Portable.zip` | ~67 MB | ZIP portable (stesso contenuto) |
| `R-Speaker-Teleprompter-Installer.exe` | ~67 MB | Installer self-extracting con scelta cartella |

Tutti e tre contengono la **stessa identica app** self-contained. Nessun runtime .NET richiesto.

---

## Architettura del progetto

```
src/TeleprompterApp/                 ← App WPF principale (.NET 8, self-contained)
  MainWindow.xaml/.cs                  — Finestra principale con editor e toolbar
  PresenterWindow.xaml/.cs             — Finestra full-screen per il secondo schermo
  App.xaml/.cs                         — Entry point, error handling globale
  CompanionBridge.cs                   — HTTP REST API (porta 3131, CORS abilitato)
  OscBridge.cs                         — Server/client OSC UDP (porte 8000/8001)
  NDITransmitter.cs                    — Streaming NDI (CompositionTarget.Rendering)
  NdiInterop.cs                        — P/Invoke wrapper per NDI SDK
  PreferencesService.cs                — Lettura/scrittura preferenze su disco
  UserPreferences.cs                   — Modello dati preferenze
  Osc/
    OscPacket.cs                       — Parser pacchetti OSC (Message + Bundle)
  Services/
    DisplayManager.cs                  — Rilevamento schermi real-time (3 layer)
    DebouncedPreferencesService.cs     — Salvataggio preferenze con debounce 500ms
    PresenterSyncService.cs            — Sync editor→presenter con debounce 300ms

companion-module/                    ← Modulo Bitfocus Companion
  index.js                             — Logica del modulo (azioni, feedback, preset)
  package.json                         — Dipendenze e metadata
  companion-config.json                — Configurazione per Companion
  README.md                            — Documentazione del modulo

installer/                           ← Script di build e packaging
  build-installer.ps1                  — Build pipeline (publish + ZIP + installer)
  installer-template.ps1               — Template installer self-extracting

portable/                            ← Output del build
  R-Speaker-Teleprompter-Portable.exe  — Eseguibile portable (solo exe, nessun altro file)
  R-Speaker-Teleprompter-Portable.zip  — ZIP con solo l'exe
  R-Speaker-Teleprompter-Installer.exe — Installer self-extracting

icons/                               — Icone dell'applicazione
docs/                                — Documentazione aggiuntiva
```

---

## Ottimizzazioni performance

### Rendering e schermi esterni
- **BitmapCache** sull'editor e sul presenter per rendering GPU-accelerato.
- **RenderOptions.ClearTypeHint** e **TextOptions.TextFormattingMode=Display** per testo nitido.
- **SetVerticalOffset ottimizzato** — skip se delta < 0,1 px per evitare layout pass ridondanti.
- **Scroll delta-time** — `Stopwatch` compensa variazioni di framerate per scorrimento sempre fluido.

### NDI
- **VisualBrush cachato** — riusato tra frame, WPF aggiorna automaticamente quando il contenuto cambia.
- **RenderTargetBitmap cachato** — riallocato solo al cambio risoluzione.
- **DrawingVisual riusato** — zero allocazioni per frame.
- **Buffer nativo pre-allocato** — pool crescente con `Marshal.AllocHGlobal`.

### Documento
- **FlowDocument cloning via XamlPackage** (~30 ms) anziché XamlWriter/XamlReader (~300 ms).
- **Debounce 300 ms** sulla sincronizzazione editor→presenter per evitare cloning ad ogni keystroke.
- **Debounce 500 ms** sulle preferenze per evitare micro-freeze durante il drag degli slider.

### Startup
- **ReadyToRun** — pre-compilazione AOT per startup a freddo ~40% più rapido.
- **Compression** nel single-file per dimensione ridotta.
- **TieredPGO** — JIT ottimizza progressivamente il codice hot path.
- **Server GC** — garbage collector ottimizzato per workload in tempo reale.

---

## Licenza

MIT
