# Live Speaker Teleprompter

Teleprompter professionale per presentazioni multi-schermo. Singolo eseguibile portable, nessuna installazione richiesta.  
Integrazione completa con **Bitfocus Companion**, **NDI** e **OSC** per il controllo remoto in produzione live.

> **Versione 2.3.3** — Self-contained .NET 8 / WPF — Windows 10/11 x64 — Italiano/English — Ultra fluido e stabile

---

## Indice

- [Funzionalità](#funzionalità)
- [Lingua e localizzazione](#lingua-e-localizzazione)
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
- [Licenza](#licenza)

---

## Funzionalità

### Editor e presentazione
- **Editor rich text** con grassetto, corsivo, sottolineato, allineamento e scelta rapida della dimensione font (36–120 pt).
- **Formato nativo `.rstp`** (XamlPackage) che conserva formattazione, colori, font e stile. Supporto anche RTF, testo semplice e sottotitoli.
- **Scorrimento automatico vsync-aligned** (CompositionTarget.Rendering) con compensazione delta-time. Velocità regolabile da **-80** a **+80** con step di 0,5.
- **Freccia guida draggable** con dimensione, colore e margine personalizzabili. Sincronizzata sul presenter.
- **Modalità modifica / presentazione** — toggle rapido che blocca la tastiera per evitare modifiche accidentali durante il live.
- **Drag & drop** di file di testo, RTF, Word (.docx) e sottotitoli direttamente nella finestra.

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
- **Eseguibile singolo self-contained** (~73 MB). Nessun runtime .NET richiesto.
- **Versioni per lingua** — `Live-Speaker-Teleprompter-Portable-ITA.exe` e `Live-Speaker-Teleprompter-Portable-ENG.exe` con lingua predefinita nel nome.
- **Preferenze e log** accanto all'exe quando esegui da USB/desktop (nessuna traccia sul PC host).
- **Installer** con scelta lingua (Italiano/English) per chi preferisce collegamenti Start/Desktop.
- **ReadyToRun** pre-compilato per startup ~40% più rapido.

---

## Lingua e localizzazione

L'app supporta **Italiano** e **English**. L'interfaccia è completamente tradotta (toolbar, messaggi, status, errori).

### Come si imposta la lingua
- **Installer**: alla prima schermata scegli Italiano o English. La lingua viene salvata e usata al primo avvio.
- **Portable ITA/ENG**: usa `Live-Speaker-Teleprompter-Portable-ITA.exe` o `Live-Speaker-Teleprompter-Portable-ENG.exe` — la lingua è predefinita.
- **Portable generico**: usa le preferenze salvate (default: Italiano) o la lingua dell'ultima installazione.

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

1. Scarica dalla cartella `portable/`:
   - `Live-Speaker-Teleprompter-Portable.exe` — lingua da preferenze
   - `Live-Speaker-Teleprompter-Portable-ITA.exe` — italiano predefinito
   - `Live-Speaker-Teleprompter-Portable-ENG.exe` — inglese predefinito
2. Esegui l'exe — estrazione automatica in temp e avvio (per ITA/ENG) oppure avvio diretto (portable generico).
3. Preferenze e log restano accanto all'exe (chiavetta USB = nessuna traccia sul PC host).

### Versione installer

1. Esegui `Live-Speaker-Teleprompter-Installer.exe`.
2. Scegli la lingua di installazione (Italiano / English).
3. Scegli la cartella di installazione (default: `%LOCALAPPDATA%\Live Speaker\Live Speaker Teleprompter`).
4. L'installer crea collegamenti sul desktop e nel menu Start. L'app appare in Impostazioni > App.

### Avvio

L'app si apre a **schermo intero** con velocità di scorrimento predefinita a **0,5** e modalità modifica attiva. Carica un file o inizia a scrivere direttamente nell'editor.

---

## Comandi da tastiera

| Tasto | Funzione |
|-------|----------|
| `Spazio` | Play / Pausa |
| `Freccia Su` | Aumenta velocità (+0,5) |
| `Freccia Giù` | Diminuisce velocità (-0,5) |
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

---

## Streaming NDI

### Attivazione

1. Installa il [NewTek NDI Tools/Runtime](https://www.ndi.tv/tools/) sul PC.
2. Nell'app, clicca il toggle **NDI** nel pannello "Schermi".
3. Il flusso apparirà come **"Live Speaker NDI"** su qualsiasi client NDI della rete locale.

### Controllo NDI via OSC

| Indirizzo OSC | Argomenti | Descrizione |
|---------------|-----------|-------------|
| `/ndi/start` | — | Avvia lo streaming NDI |
| `/ndi/stop` | — | Ferma lo streaming NDI |
| `/ndi/toggle` | — | Inverte lo stato NDI |
| `/ndi/resolution` | `int width`, `int height` | Imposta risoluzione (es. 1920, 1080) |
| `/ndi/framerate` | `int fps` | Imposta framerate (es. 30) |
| `/ndi/sourcename` | `string name` | Cambia il nome della sorgente NDI |

---

## Controllo remoto — HTTP REST API

L'app espone un endpoint HTTP su `http://localhost:3131/teleprompter/` avviato automaticamente. Compatibile con Bitfocus Companion, cURL, browser, script e qualsiasi client HTTP.

### Endpoint disponibili

| Comando | Metodo | URL | Descrizione |
|---------|--------|-----|-------------|
| Stato | GET | `/teleprompter/status` | JSON con stato completo |
| Play | GET/POST | `/teleprompter/play` | Avvia lo scorrimento |
| Pausa | GET/POST | `/teleprompter/pause` | Ferma lo scorrimento |
| Toggle | GET/POST | `/teleprompter/toggle` | Inverte play/pausa |
| Velocità + | GET/POST | `/teleprompter/speed/up` | +0,5 |
| Velocità − | GET/POST | `/teleprompter/speed/down` | −0,5 |
| Velocità 0 | GET/POST | `/teleprompter/speed/reset` | Azzera |
| Imposta velocità | GET/POST | `/teleprompter/speed/set?value=1.5` | Valore esatto |

### Esempio con cURL

```bash
curl http://localhost:3131/teleprompter/play
curl "http://localhost:3131/teleprompter/speed/set?value=2.0"
curl http://localhost:3131/teleprompter/status
```

---

## Controllo remoto — OSC

L'app riceve comandi OSC su UDP porta **8000** e invia feedback su porta **8001**.

### Comandi OSC principali

| Indirizzo | Argomenti | Descrizione |
|-----------|-----------|-------------|
| `/teleprompter/play` | — | Avvia scorrimento |
| `/teleprompter/pause` | — | Ferma scorrimento |
| `/teleprompter/reset` | — | Torna all'inizio del testo |
| `/teleprompter/speed` | `float` | Imposta velocità (-80 a +80) |
| `/teleprompter/position` | `float` (0.0–1.0) | Imposta posizione scroll |
| `/teleprompter/jump/top` | — | Salta all'inizio |
| `/teleprompter/jump/bottom` | — | Salta alla fine |
| `/teleprompter/mirror/toggle` | — | Inverte lo stato mirror |

---

## Integrazione Bitfocus Companion

Vedi la guida completa: [docs/COMPANION_SETUP_GUIDE.md](../docs/COMPANION_SETUP_GUIDE.md)

Il modulo si trova nella cartella `companion-module/` e offre:
- **23 azioni** (playback, velocità, font, navigazione, mirror, NDI)
- **5 feedback** (playing, speed, mirror, NDI active, NDI available)
- **4 variabili** (speed, playing, mirrored, ndi_active)
- **10 preset** pronti per Stream Deck
- **Auto-reconnect** ogni 5 secondi

---

## Formati file supportati

### Apertura

| Formato | Estensioni | Note |
|---------|-----------|------|
| Documento Teleprompter | `.rstp` | Formato nativo, conserva tutta la formattazione |
| Microsoft Word | `.docx`, `.doc` | Estrazione testo |
| Rich Text Format | `.rtf` | Formattazione base |
| Testo semplice | `.txt`, `.md`, `.log` | Convertito con font corrente |
| Sottotitoli | `.srt`, `.vtt` | Caricati come testo |

### Salvataggio

| Formato | Estensione | Conserva formattazione |
|---------|-----------|----------------------|
| Documento Teleprompter | `.rstp` | Completa |
| Rich Text Format | `.rtf` | Parziale |
| Testo semplice | `.txt` | Solo testo |

---

## Preferenze utente

Le preferenze vengono salvate automaticamente in:

```
%APPDATA%\Live Speaker Teleprompter\preferences.json
```

Per la versione portable (eseguita da USB), le preferenze restano accanto all'exe.

### Dati salvati

- Colore sfondo e testo, font, dimensione
- Velocità di scorrimento predefinita
- Stato mirror e sempre-in-primo-piano
- Monitor preferito
- Ultimo file aperto
- Posizione, dimensione e colore della freccia guida
- **Lingua** (it/en)

### Log errori

```
%APPDATA%\Live Speaker Teleprompter\logs\error-YYYYMMDD-HHmmss.log
```

---

## Build da sorgente

### Prerequisiti

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Windows 10/11 x64

### Comandi

```powershell
# Build completa (portable + installer + versioni ITA/ENG)
.\clean-and-build.ps1

# Oppure solo build installer
cd installer
.\build-installer.ps1
```

### Output del build

| File | Descrizione |
|------|-------------|
| `Live-Speaker-Teleprompter-Portable.exe` | Eseguibile portable generico |
| `Live-Speaker-Teleprompter-Portable-ITA.exe` | Portable italiano (self-extracting) |
| `Live-Speaker-Teleprompter-Portable-ENG.exe` | Portable inglese (self-extracting) |
| `Live-Speaker-Teleprompter-Installer.exe` | Installer con scelta lingua |

---

## Architettura del progetto

```
src/TeleprompterApp/           ← App WPF principale
  MainWindow.xaml/.cs            — Finestra principale
  PresenterWindow.xaml/.cs       — Finestra full-screen presenter
  Localization.cs                — Traduzioni IT/EN
  CompanionBridge.cs             — HTTP REST API (porta 3131)
  OscBridge.cs                   — OSC UDP (8000/8001)
  NDITransmitter.cs              — Streaming NDI
  Services/
    DisplayManager.cs            — Rilevamento schermi
    PresenterSyncService.cs      — Sync editor→presenter

installer/
  build-installer.ps1             — Build pipeline
  installer-template.ps1          — Installer con scelta lingua
  portable-extractor-template.ps1 — Template portable ITA/ENG

portable/                       ← Output build
```

---

## Licenza

MIT
