# Live Speaker Teleprompter — Companion Module

Modulo per [Bitfocus Companion](https://bitfocus.io/companion) che permette il controllo completo di Live Speaker Teleprompter via **OSC over UDP**.

## Funzionalità

- **23 azioni**: playback, velocità, font, navigazione, mirror, NDI, output mode
- **5 feedback**: playing, speed, mirror, NDI active, NDI available
- **4 variabili**: speed, playing, mirrored, ndi_active
- **10 preset**: pronti per Stream Deck con feedback visivi
- **Auto-reconnect**: riconnessione automatica ogni 5 secondi
- **Status sync**: richiesta stato automatica alla connessione

## Requisiti

- Bitfocus Companion 3.0+
- Live Speaker Teleprompter 2.0.0+

## Installazione

1. Copia questa cartella nel path dei moduli custom di Companion.
2. Installa le dipendenze:

   ```bash
   npm install
   ```

3. Riavvia Companion.
4. Aggiungi la connessione **"Live Speaker Teleprompter"**.

## Configurazione

| Parametro | Default | Descrizione |
|-----------|---------|-------------|
| Target IP | `127.0.0.1` | IP del PC con Live Speaker Teleprompter |
| OSC Port | `8000` | Porta comandi |
| Feedback Port | `8001` | Porta feedback (locale) |

## Azioni disponibili

### Playback
- **Start/Play** — avvia lo scorrimento
- **Stop/Pause** — ferma lo scorrimento
- **Play/Pause Toggle** — inverte lo stato
- **Reset** — torna all'inizio del testo

### Velocità
- **Set Speed** — imposta velocità esatta (range: **-20** a **+20**, step 0,25)
- **Speed Increase** — +0,25
- **Speed Decrease** — −0,25

### Font
- **Set Font Size** — imposta dimensione (20–200 pt)
- **Font Increase** — +2 pt
- **Font Decrease** — −2 pt

### Navigazione
- **Jump to Top** — inizio documento
- **Jump to Bottom** — fine documento
- **Set Position** — posizione percentuale (0–100%)

### Mirror
- **Mirror Toggle** — inverte stato mirror

### Script (predisposte)
- **Next Script** / **Previous Script** / **Load Script** — predisposte per futura playlist

### NDI
- **NDI Start** / **NDI Stop** / **NDI Toggle**
- **NDI Set Resolution** — Full HD, HD, 4K
- **NDI Set Framerate** — 25, 30, 50, 60 fps

### Output
- **Set Output Mode** — Display, NDI, Both

## Variabili

| ID | Esempio | Uso nel testo bottone |
|----|---------|----------------------|
| `speed` | `"1.50"` | `$(rspeaker:speed)` |
| `playing` | `"Playing"` | `$(rspeaker:playing)` |
| `mirrored` | `"Off"` | `$(rspeaker:mirrored)` |
| `ndi_active` | `"Active"` | `$(rspeaker:ndi_active)` |

## Protocollo OSC

Comandi: porta **8000** (UDP). Feedback: porta **8001** (UDP).

Riferimento completo: vedi [Setup_Companion_Live_Speaker_Teleprompter.md](../docs/Setup_Companion_Live_Speaker_Teleprompter.md#protocollo-osc--riferimento-completo)

## Dipendenze

- `@companion-module/base` ^1.12.0
- `osc` ^2.4.5

## Licenza

MIT
