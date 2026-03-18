# Live Speaker Teleprompter — Companion Module Setup Guide

Guida completa per l'integrazione di **Live Speaker Teleprompter** con **Bitfocus Companion**.

Questa guida copre l'installazione del modulo, la configurazione della connessione OSC, la lista completa di azioni/feedback/variabili/preset e scenari d'uso in produzione.

---

## Indice

- [Panoramica](#panoramica)
- [Prerequisiti](#prerequisiti)
- [Installazione del modulo](#installazione-del-modulo)
- [Configurazione della connessione](#configurazione-della-connessione)
- [Azioni (Actions)](#azioni-actions)
- [Feedback](#feedback)
- [Variabili (Variables)](#variabili-variables)
- [Preset](#preset)
- [Protocollo OSC — Riferimento completo](#protocollo-osc--riferimento-completo)
- [Auto-reconnect e resilienza](#auto-reconnect-e-resilienza)
- [Scenari d'uso](#scenari-duso)
- [Risoluzione problemi](#risoluzione-problemi)

---

## Panoramica

Il modulo **Live Speaker Teleprompter** per Bitfocus Companion permette di controllare il teleprompter interamente da Stream Deck, Touch Portal o qualsiasi superficie supportata.

Il protocollo di comunicazione è **OSC over UDP**:

```
Companion  ──OSC──▶  Live Speaker Teleprompter (porta 8000)
                      ◀──OSC──  Live Speaker Teleprompter (porta 8001 = feedback)
```

La connessione è bidirezionale: Companion invia comandi all'app e riceve lo stato in tempo reale (playing, speed, mirror, NDI).

---

## Prerequisiti

| Componente | Versione | Note |
|-----------|---------|------|
| Bitfocus Companion | 3.0+ | Testato con 3.x |
| Live Speaker Teleprompter | 2.0.0+ | Deve essere in esecuzione sulla stessa macchina o rete |
| Node.js | 18+ | Fornito con Companion |
| NDI Tools | Opzionale | Solo se si usano le azioni NDI |

---

## Installazione del modulo

### Metodo 1 — Cartella moduli custom (consigliato)

1. Localizza la cartella dei **moduli custom** di Companion:
   - **Windows**: `%APPDATA%\companion-module-dev\` o `%APPDATA%\companion\modules\`
   - **macOS**: `~/Library/Application Support/companion-module-dev/`
   - **Linux**: `~/.config/companion-module-dev/`

2. Copia l'intera cartella `companion-module/` dal pacchetto Live Speaker Teleprompter nella cartella dei moduli custom:

   ```
   companion-module/
     ├── index.js
     ├── package.json
     ├── companion-config.json
     └── README.md
   ```

3. Apri un terminale nella cartella copiata e installa le dipendenze:

   ```bash
   cd companion-module
   npm install
   ```

4. Riavvia Companion.

### Metodo 2 — Development mode

1. In Companion, vai su **Connections** → **Developer** (o **Add Connection**).
2. Usa il path completo alla cartella `companion-module/` come **module path**.
3. Companion caricherà il modulo al volo.

### Verifica installazione

Dopo il riavvio, cerca **"Live Speaker Teleprompter"** nella lista delle connessioni disponibili. Se non compare, verifica:
- Che `package.json` e `index.js` siano nella cartella corretta
- Che `npm install` sia stato eseguito con successo
- I log di Companion per eventuali errori di caricamento

---

## Configurazione della connessione

Dopo aver aggiunto la connessione, configura i seguenti parametri:

| Parametro | Default | Descrizione |
|-----------|---------|-------------|
| **Target IP** | `127.0.0.1` | Indirizzo IP del PC dove gira Live Speaker Teleprompter |
| **OSC Port** | `8000` | Porta su cui Live Speaker Teleprompter ascolta i comandi |
| **Feedback Port** | `8001` | Porta locale su cui Companion riceve i feedback |

### Configurazione sulla stessa macchina

Se Companion e Live Speaker Teleprompter girano sullo stesso PC, usa i valori di default:
- Target IP: `127.0.0.1`
- OSC Port: `8000`
- Feedback Port: `8001`

### Configurazione in rete

Se Companion gira su un PC diverso da Live Speaker Teleprompter:
- **Target IP**: inserisci l'indirizzo IP del PC dove gira Live Speaker Teleprompter (es. `192.168.1.100`)
- **OSC Port**: `8000` (invariato)
- **Feedback Port**: `8001` (invariato, Companion ascolterà su questa porta)
- Assicurati che le porte UDP 8000 e 8001 siano aperte sul firewall Windows

### Stato della connessione

Dopo la configurazione, lo stato passa a:
- **Connecting** (giallo) — in attesa di apertura porta
- **Ok** (verde) — connesso e funzionante
- **Connection Failure** (rosso) — errore di connessione (l'auto-reconnect riproverà ogni 5 secondi)

---

## Azioni (Actions)

Il modulo offre **23 azioni** organizzate per categoria.

### Playback

| ID azione | Nome | Comando OSC | Descrizione |
|-----------|------|------------|-------------|
| `play` | Start/Play | `/teleprompter/start` | Avvia lo scorrimento |
| `stop` | Stop/Pause | `/teleprompter/stop` | Ferma lo scorrimento |
| `toggle` | Play/Pause Toggle | `/teleprompter/start` o `/stop` | Inverte lo stato (basato su feedback corrente) |
| `reset` | Reset | `/teleprompter/reset` | Riporta all'inizio del testo |

### Velocità

| ID azione | Nome | Comando OSC | Opzioni | Descrizione |
|-----------|------|------------|---------|-------------|
| `setSpeed` | Set Speed | `/teleprompter/speed` | `speed`: float da **-20** a **+20**, step 0,25 | Imposta velocità esatta |
| `speedUp` | Speed Increase | `/teleprompter/speed/increase` | — | +0,25 |
| `speedDown` | Speed Decrease | `/teleprompter/speed/decrease` | — | −0,25 |

> **Nota sul range velocità:** il range è **-20 a +20** con step di 0,25. Valori positivi = scorrimento verso il basso, negativi = verso l'alto. Il valore default è 0,5.

### Font

| ID azione | Nome | Comando OSC | Opzioni | Descrizione |
|-----------|------|------------|---------|-------------|
| `setFontSize` | Set Font Size | `/teleprompter/font/size` | `size`: int da 20 a 200 | Imposta dimensione font in punti |
| `fontUp` | Font Increase | `/teleprompter/font/increase` | — | +2 pt |
| `fontDown` | Font Decrease | `/teleprompter/font/decrease` | — | −2 pt |

### Navigazione

| ID azione | Nome | Comando OSC | Opzioni | Descrizione |
|-----------|------|------------|---------|-------------|
| `jumpTop` | Jump to Top | `/teleprompter/jump/top` | — | Salta all'inizio del documento |
| `jumpBottom` | Jump to Bottom | `/teleprompter/jump/bottom` | — | Salta alla fine del documento |
| `setPosition` | Set Position | `/teleprompter/position` | `position`: int 0–100 (%) | Imposta posizione scroll (convertito in 0.0–1.0) |

### Mirror

| ID azione | Nome | Comando OSC | Descrizione |
|-----------|------|------------|-------------|
| `mirrorToggle` | Mirror Toggle | `/teleprompter/mirror/toggle` | Inverte lo stato mirror |

### Script (predisposte)

| ID azione | Nome | Comando OSC | Opzioni | Descrizione |
|-----------|------|------------|---------|-------------|
| `nextScript` | Next Script | `/teleprompter/script/next` | — | Prossimo script nella playlist |
| `prevScript` | Previous Script | `/teleprompter/script/previous` | — | Script precedente |
| `loadScript` | Load Script | `/teleprompter/script/load` | `index`: int ≥ 0 | Carica script per indice |

> **Nota:** le azioni "Script" sono predisposte per una futura funzionalità playlist. Attualmente inviano i comandi OSC ma l'app non li gestisce.

### NDI

| ID azione | Nome | Comando OSC | Opzioni | Descrizione |
|-----------|------|------------|---------|-------------|
| `ndiStart` | NDI Start | `/ndi/start` | — | Avvia streaming NDI |
| `ndiStop` | NDI Stop | `/ndi/stop` | — | Ferma streaming NDI |
| `ndiToggle` | NDI Toggle | `/ndi/toggle` | — | Inverte lo stato NDI |
| `ndiResolution` | NDI Set Resolution | `/ndi/resolution` | Dropdown: Full HD, HD, 4K | Imposta risoluzione |
| `ndiFramerate` | NDI Set Framerate | `/ndi/framerate` | Dropdown: 25, 30, 50, 60 fps | Imposta framerate |

### Output Mode

| ID azione | Nome | Comando OSC | Opzioni | Descrizione |
|-----------|------|------------|---------|-------------|
| `outputMode` | Set Output Mode | `/output/{mode}` | Dropdown: display, ndi, both | Imposta modalità output |

---

## Feedback

Il modulo espone **5 feedback** che aggiornano automaticamente l'aspetto dei bottoni su Stream Deck.

| Feedback ID | Tipo | Nome | Stile default (attivo) | Descrizione |
|-------------|------|------|----------------------|-------------|
| `isPlaying` | boolean | Playing Status | Sfondo verde | `true` quando il teleprompter sta scorrendo |
| `currentSpeed` | advanced | Current Speed | Testo "Speed: X.XX" | Mostra la velocità corrente come overlay |
| `isMirrored` | boolean | Mirror Status | Sfondo blu | `true` quando il mirror è attivo |
| `ndiActive` | boolean | NDI Active | Sfondo rosso | `true` quando NDI sta trasmettendo |
| `ndiAvailable` | boolean | NDI Available | Sfondo verde | `true` quando il runtime NDI è disponibile |

### Come usare i feedback

1. In Companion, seleziona un bottone.
2. Vai alla tab **Feedbacks**.
3. Aggiungi il feedback desiderato (es. `isPlaying`).
4. Personalizza lo stile "attivo" (colore sfondo, testo, dimensione).
5. Il bottone cambierà aspetto automaticamente in base allo stato del teleprompter.

---

## Variabili (Variables)

Il modulo espone **4 variabili** utilizzabili in espressioni e testi dinamici.

| Variable ID | Nome | Tipo | Valori possibili | Esempio |
|-------------|------|------|------------------|---------|
| `speed` | Current Speed | string | Numerico con 2 decimali | `"1.50"`, `"-0.25"` |
| `playing` | Playing Status | string | `"Playing"` / `"Stopped"` | `"Playing"` |
| `mirrored` | Mirror Status | string | `"On"` / `"Off"` | `"Off"` |
| `ndi_active` | NDI Active | string | `"Active"` / `"Inactive"` | `"Active"` |

### Uso nei testi dei bottoni

Puoi inserire variabili nel testo di qualsiasi bottone:

```
$(rspeaker:speed)
$(rspeaker:playing)
$(rspeaker:mirrored)
$(rspeaker:ndi_active)
```

**Esempio di testo bottone:**
```
Speed
$(rspeaker:speed)
```
Questo mostra "Speed" sulla prima riga e il valore corrente (es. "1.50") sulla seconda.

---

## Preset

Il modulo include **10 preset** pronti per il drag & drop sulle pagine di Stream Deck.

### Categoria: Playback

| Preset | Testo | Colore | Azione | Feedback |
|--------|-------|--------|--------|----------|
| Play | `PLAY` | Verde (#00FF00) | `play` | `isPlaying` |
| Stop | `STOP` | Rosso (#FF0000) | `stop` | — |
| Reset | `RESET` | Blu (#0000FF) | `reset` | — |
| Toggle | `TOGGLE` | Grigio (#646464) | `toggle` | `isPlaying` |

### Categoria: Speed

| Preset | Testo | Colore | Azione |
|--------|-------|--------|--------|
| Speed Up | `SPEED+` | Arancione (#FFA500) | `speedUp` |
| Speed Down | `SPEED-` | Arancione (#FFA500) | `speedDown` |

### Categoria: NDI Output

| Preset | Testo | Colore | Azione | Feedback |
|--------|-------|--------|--------|----------|
| NDI Toggle | `NDI` | Rosso → Verde (attivo) | `ndiToggle` | `ndiActive` |
| Output: Display | `DISPLAY` | Grigio (#646464) | `outputMode` (display) | — |
| Output: NDI | `NDI ONLY` | Rosso (#FF0000) | `outputMode` (ndi) | — |
| Output: Both | `BOTH` | Verde scuro (#009600) | `outputMode` (both) | — |

---

## Protocollo OSC — Riferimento completo

### Comandi inviati da Companion → Live Speaker Teleprompter (porta 8000)

| # | Indirizzo OSC | Tipo argomenti | Descrizione |
|---|---------------|---------------|-------------|
| 1 | `/teleprompter/start` | — | Avvia scorrimento |
| 2 | `/teleprompter/stop` | — | Ferma scorrimento |
| 3 | `/teleprompter/reset` | — | Torna all'inizio |
| 4 | `/teleprompter/speed` | float | Imposta velocità (-20.0 ÷ +20.0) |
| 5 | `/teleprompter/speed/increase` | — | +0,25 |
| 6 | `/teleprompter/speed/decrease` | — | −0,25 |
| 7 | `/teleprompter/font/size` | int | Imposta font (20–200 pt) |
| 8 | `/teleprompter/font/increase` | — | +2 pt |
| 9 | `/teleprompter/font/decrease` | — | −2 pt |
| 10 | `/teleprompter/position` | float | Posizione scroll (0.0–1.0) |
| 11 | `/teleprompter/jump/top` | — | Salta all'inizio |
| 12 | `/teleprompter/jump/bottom` | — | Salta alla fine |
| 13 | `/teleprompter/mirror/toggle` | — | Inverte mirror |
| 14 | `/teleprompter/status/request` | — | Richiede stato completo |
| 15 | `/teleprompter/script/next` | — | Prossimo script * |
| 16 | `/teleprompter/script/previous` | — | Script precedente * |
| 17 | `/teleprompter/script/load` | int | Carica script per indice * |
| 18 | `/ndi/start` | — | Avvia NDI |
| 19 | `/ndi/stop` | — | Ferma NDI |
| 20 | `/ndi/toggle` | — | Inverte NDI |
| 21 | `/ndi/resolution` | int, int | Risoluzione (width, height) |
| 22 | `/ndi/framerate` | int | Framerate (fps) |
| 23 | `/ndi/status/request` | — | Richiede stato NDI |
| 24 | `/output/display` | — | Modalità solo display |
| 25 | `/output/ndi` | — | Modalità solo NDI |
| 26 | `/output/both` | — | Modalità display + NDI |

> \* Le azioni script (#15-17) sono predisposte. L'app attualmente non le gestisce.

### Feedback ricevuti da Live Speaker Teleprompter → Companion (porta 8001)

| # | Indirizzo OSC | Tipo | Valori | Descrizione |
|---|---------------|------|--------|-------------|
| 1 | `/teleprompter/status` | string | `"playing"` / `"stopped"` | Stato playback |
| 2 | `/teleprompter/speed/current` | string | `"1.50"` | Velocità corrente |
| 3 | `/teleprompter/mirror/status` | string | `"true"` / `"false"` | Stato mirror |
| 4 | `/teleprompter/position/current` | string | `"0.350"` | Posizione scroll (0.0–1.0) |
| 5 | `/teleprompter/font/size/current` | string | `"72"` | Dimensione font corrente |
| 6 | `/ndi/status` | string | `"active"` / `"inactive"` | Stato NDI |
| 7 | `/ndi/available` | string | `"yes"` / `"no"` | Runtime NDI disponibile |
| 8 | `/ndi/resolution/current` | string | `"1920x1080"` | Risoluzione NDI |
| 9 | `/ndi/framerate/current` | string | `"30.00"` | Framerate NDI |
| 10 | `/ndi/sourcename/current` | string | `"Live Speaker NDI"` | Nome sorgente NDI |

---

## Auto-reconnect e resilienza

Il modulo implementa un meccanismo di auto-reconnect:

1. **All'avvio**: il modulo apre la porta UDP e passa allo stato `Connecting`. Quando la porta è pronta, passa a `Ok`.
2. **Status request automatico**: 1 secondo dopo l'inizializzazione e ad ogni riconnessione, il modulo invia automaticamente `/teleprompter/status/request` e `/ndi/status/request` per sincronizzare tutti i feedback.
3. **In caso di errore**: passa allo stato `Connection Failure` e riprova la connessione ogni **5 secondi** automaticamente.
4. **Cleanup**: alla chiusura del modulo, tutti i timer e la porta UDP vengono rilasciati correttamente.

> L'ordine di avvio non importa: puoi avviare prima Companion o prima Live Speaker Teleprompter. Il modulo si sincronizzerà automaticamente appena entrambi sono online.

---

## Scenari d'uso

### Scenario 1 — Produzione con Stream Deck

1. Configura una pagina Stream Deck con i preset Playback:
   - Riga 1: `PLAY` | `STOP` | `RESET` | `TOGGLE`
   - Riga 2: `SPEED-` | Speed display (variabile) | `SPEED+`
   - Riga 3: `NDI` | `DISPLAY` | `NDI ONLY` | `BOTH`

2. Il bottone `PLAY` diventa verde quando il teleprompter sta scorrendo (feedback `isPlaying`).
3. Il bottone `NDI` diventa verde quando NDI è attivo (feedback `ndiActive`).

### Scenario 2 — Controllo remoto in rete

1. Live Speaker Teleprompter gira sul PC del presenter (es. `192.168.1.50`).
2. Companion gira sul PC della regia (es. `192.168.1.100`).
3. Nella configurazione del modulo, imposta Target IP = `192.168.1.50`.
4. Apri le porte 8000 e 8001 UDP sul firewall del PC presenter.
5. Controllo remoto operativo.

### Scenario 3 — NDI verso OBS Studio

1. Sul PC con OBS, aggiungi una sorgente **NDI Source** che punta a "Live Speaker NDI".
2. Da Stream Deck, premi `NDI` per avviare lo streaming.
3. Usa i preset risoluzione/framerate per regolare la qualità in base alla banda disponibile.

---

## Risoluzione problemi

### Il modulo non compare nella lista connessioni

- Verifica che `package.json`, `index.js` e `companion-config.json` siano nella stessa cartella.
- Esegui `npm install` nella cartella del modulo.
- Riavvia Companion completamente (chiudi e riapri).
- Controlla i log di Companion per errori di caricamento.

### Stato "Connection Failure" permanente

- Live Speaker Teleprompter è in esecuzione? Deve essere aperto e funzionante.
- La porta 8001 è già in uso? Verifica con `netstat -an | findstr 8001`.
- Il firewall blocca le connessioni UDP? Aggiungi eccezione per Companion e Live Speaker Teleprompter.

### I feedback non si aggiornano

- Live Speaker Teleprompter invia feedback solo sulla porta 8001 a `127.0.0.1`. Se Companion è su un'altra macchina, il feedback potrebbe non arrivare. In questo caso, verifica che la porta Feedback sia quella su cui Companion ascolta.
- Prova a premere un bottone: l'azione dovrebbe funzionare anche senza feedback.

### La velocità non cambia

- Verifica che Live Speaker Teleprompter sia in **modalità presentazione** (non in modalità modifica). In modalità modifica la velocità viene ignorata dal playback.

### NDI non disponibile

- Il runtime NDI deve essere installato sul PC dove gira Live Speaker Teleprompter ([download](https://www.ndi.tv/tools/)).
- Se il feedback `ndiAvailable` è `false`, la DLL NDI non è stata trovata.
