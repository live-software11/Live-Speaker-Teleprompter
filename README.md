# R-Speaker Teleprompter

Teleprompter moderno e leggero pensato per presentazioni su più schermi. Permette di aprire qualsiasi file di testo, personalizzare stile e colori, regolare la velocità in tempo reale e lavorare in modalità mirror per specchi.

## Funzionalità principali

- Apertura rapida di file di testo (`.txt`, `.md`, `.rtf`, `.srt`, `.vtt`, `.csv`, `.json`, `.xml`, ecc.).
- Interfaccia tecnologica con toolbar modulare: controlli play/pausa, mirror, topmost, palette colori e selezione font.
- Editor rich text in stile Word con grassetto/corsivo/sottolineato, allineamenti e scelta rapida della dimensione.
- **Rilevamento real-time degli schermi** con triple ridondanza (Win32 hook, SystemEvents, polling) e switch istantaneo — hotplug supportato.
- Scorrimento automatico fluido con **compensazione delta-time** (Stopwatch), regolazione da slider, frecce, rotellina e barra spaziatrice.
- Salvataggio automatico delle preferenze utente con **debounce 500ms** (velocità, colori, font, posizione freccia, monitor preferito).
- Mirroring orizzontale per utilizzo con teleprompter fisici.
- Avvio a schermo intero con velocità predefinita a 0,5 per avere subito un flusso pronto alla lettura.
- Streaming diretto via **NewTek NDI** con cattura ottimizzata (CompositionTarget.Rendering, bitmap riutilizzate).
- Controllo remoto via **OSC** (porte UDP 8000/8001) e **HTTP REST API** (porta 3131) per Bitfocus Companion.
- Sincronizzazione editor–presenter con **debounce 300ms** e cloning ottimizzato (XamlPackage).

## Stack tecnologico

| Componente | Tecnologia | Versione |
|---|---|---|
| App desktop (primaria) | WPF / C# / .NET | 8.0 (`net8.0-windows`) |
| Web frontend (secondaria) | React + TypeScript | React 19 / TS 5.7 |
| Build web | Vite | 6.x |
| Linting web | ESLint (flat config) | 9.x |
| Comunicazione remota | OSC (UDP) + HTTP REST | - |
| Video output | NewTek NDI (P/Invoke) | - |
| Companion | Bitfocus Companion module | Module API 1.0 |

## Requisiti

- Windows 10 o superiore.
- [.NET 8 SDK](https://dotnet.microsoft.com/it-it/download) per compilare il progetto WPF.
- [Node.js 18+](https://nodejs.org/) per compilare il frontend web (opzionale).

## Avvio rapido

### App WPF (principale)

```powershell
cd src\TeleprompterApp
dotnet run
```

### Frontend web (opzionale)

```powershell
npm install
npm run dev
```

## Creare un eseguibile portable

Per ottenere un eseguibile leggero che si avvii rapidamente:

1. **Versione dipendente dal runtime (peso ridotto)** – richiede il runtime .NET 8 sul PC di destinazione:

   ```powershell
   cd src\TeleprompterApp
   dotnet publish -c Release -p:SelfContained=false
   ```

   L'eseguibile sarà in `bin\Release\net8.0-windows\win-x64\publish\R-Speaker Teleprompter.exe`.

2. **Versione completamente portabile** – non richiede installazione del runtime (~70 MB). Questa è la modalità di default nel .csproj:

   ```powershell
   cd src\TeleprompterApp
   dotnet publish -c Release
   ```

3. **Build completa (portable ZIP + installer EXE)** — usa lo script unificato:

   ```powershell
   cd installer
   .\build-installer.ps1
   ```

   Genera nella cartella `portable/` sia lo ZIP che l'installer. Entrambi contengono la stessa identica app.

## Comandi rapidi da tastiera

- `Freccia su / giù`: aumenta o diminuisce la velocità di scorrimento.
- `Rotellina mouse`: varia la velocità (scroll avanti = più veloce, indietro = più lento) quando il focus non è sull'editor oppure durante il play; mentre scrivi resta disponibile lo scroll classico del documento.
- `Barra spaziatrice`: play/pausa.
- `Home` / `End`: vai all'inizio o alla fine del testo.
- `Freccia sinistra / destra`: azzera immediatamente la velocità.
- `Ctrl+O`: apri file.
- `Ctrl+S`: salva.
- `Ctrl+N`: nuovo documento.

## Controllo remoto con Bitfocus Companion

L'app espone un piccolo endpoint HTTP locale pensato per Bitfocus Companion (o qualsiasi controller capace di inviare richieste HTTP). Il listener è attivo su `http://localhost:3131/teleprompter/`.

| Comando Companion | Metodo | URL / Parametri | Effetto |
|-------------------|--------|-----------------|---------|
| Play              | GET/POST | `/teleprompter/play` | Avvia lo scorrimento |
| Pausa             | GET/POST | `/teleprompter/pause` | Ferma lo scorrimento |
| Toggle            | GET/POST | `/teleprompter/toggle` | Inverte play/pausa |
| Velocità +        | GET/POST | `/teleprompter/speed/up` | Aumenta di uno step (0,25) |
| Velocità -        | GET/POST | `/teleprompter/speed/down` | Diminuisce di uno step |
| Velocità 0        | GET/POST | `/teleprompter/speed/reset` | Azzera la velocità |
| Imposta velocità  | GET/POST | `/teleprompter/speed/set?value=0.8` | Imposta direttamente il valore (usa il punto come separatore decimale) |
| Stato             | GET      | `/teleprompter/status` | Restituisce JSON con velocità, stato play/pausa e modalità |

Ogni comando restituisce un JSON di conferma. In Companion puoi usare l'azione HTTP (GET o POST) puntando agli URL sopra; non è necessario autenticarsi. Assicurati che nessun altro servizio usi la porta `3131`.

### Controllo remoto via OSC

Oltre all'HTTP, l'app accetta comandi OSC su UDP porta **8000** e invia feedback su porta **8001**:

| Comando | Indirizzo OSC | Argomenti |
|---------|---------------|-----------|
| Play | `/teleprompter/play` | nessuno |
| Pausa | `/teleprompter/pause` | nessuno |
| Velocità | `/teleprompter/speed` | float |
| Velocità + | `/teleprompter/speed/increase` | nessuno |
| Velocità - | `/teleprompter/speed/decrease` | nessuno |
| Font size | `/teleprompter/font/size` | float (pt) |
| Posizione | `/teleprompter/position` | float (0.0–1.0) |
| Mirror | `/teleprompter/mirror/toggle` | nessuno |
| NDI start | `/ndi/start` | nessuno |
| NDI stop | `/ndi/stop` | nessuno |
| Status | `/teleprompter/status/request` | nessuno |

### Uscita video tramite NewTek NDI

Nel pannello "Schermi" è presente lo switch **NDI**: quando attivo, il contenuto del prompt viene inviato come flusso NDI denominato "R-Speaker NDI". Per funzionare è necessario aver installato sul PC il **NewTek NDI Tools/Runtime**. Il flusso può essere ricevuto da OBS, TriCaster, vMix o qualsiasi client compatibile.

## Suggerimenti d'uso

- Mantieni attivo "Sempre in primo piano" quando utilizzi il teleprompter sopra altre app.
- Se usi un teleprompter fisico con specchio, attiva l'opzione "Specchio" per ribaltare il testo.
- La velocità viene mostrata e controllata in tempo reale da slider e indicatori; valori negativi scorrono verso l'alto.

## Struttura del progetto

```
src/
  TeleprompterApp/           ← App WPF principale (.NET 8)
    Services/
      DisplayManager.cs        — Rilevamento schermi real-time (Win32 + polling)
      DebouncedPreferencesService.cs — Salvataggio preferenze con debounce
      PresenterSyncService.cs  — Sync editor→presenter debounced
    CompanionBridge.cs         — HTTP REST API (porta 3131)
    NDITransmitter.cs          — Streaming NDI ottimizzato
    NdiInterop.cs              — P/Invoke NDI SDK
    OscBridge.cs               — Server/client OSC UDP
    MainWindow.xaml/.cs        — Finestra principale
    PresenterWindow.xaml/.cs   — Finestra presenter (secondo schermo)
    PreferencesService.cs      — I/O preferenze su disco
    UserPreferences.cs         — Modello preferenze
    Osc/                       — Parser pacchetti OSC
  components/                ← Componenti React (frontend web)
    TextDisplay.tsx            — Display testo virtualizzato
    ControlPanel.tsx           — Slider con debounce indipendente
companion-module/            ← Modulo Bitfocus Companion
icons/
mobile/                      ← PWA mobile (sperimentale)
```

> **Nota:** Le cartelle `src/main/` e `src/renderer/` contengono codice Electron legacy non più attivo. La piattaforma desktop è interamente WPF/.NET 8.

## Licenza

MIT
