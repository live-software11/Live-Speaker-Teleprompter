# 📡 R-Speaker Teleprompter + Bitfocus Companion
## Guida Completa all'Integrazione

---

## 🚀 Installazione Rapida

### Prerequisiti
- **R-Speaker Teleprompter** installato e funzionante
- **Bitfocus Companion** v3.0 o superiore installato
- Connessione di rete attiva (anche localhost)

---

## 📋 PARTE 1: Preparazione R-Speaker Teleprompter

### 1.1 Avviare l'applicazione
1. Apri **R-Speaker Teleprompter**
2. L'applicazione avvia automaticamente il server OSC sulla porta **8000** e l'API HTTP sulla porta **3131**
3. Verifica nella barra di stato che l'app sia pronta

### 1.2 Verifica delle porte
- **Porta HTTP Companion**: 3131 (API REST, attiva automaticamente)
- **Porta OSC principale**: 8000 (riceve comandi OSC/UDP)
- **Porta feedback OSC**: 8001 (invia status a Companion)

> ⚠️ **Nota**: Se queste porte sono già in uso, puoi modificarle nel file di configurazione

---

## 📦 PARTE 2: Installazione Modulo Companion

### Metodo 1: Installazione Manuale (Consigliato)

1. **Localizza la cartella dei moduli di Companion**:
   - Windows: `%APPDATA%\Companion\module-local`
   - macOS: `~/Library/Application Support/Companion/module-local`
   - Linux: `~/.config/Companion/module-local`

2. **Copia il modulo**:
   ```bash
   cd "C:\Users\andre\Desktop\R-Speaker- Telepromper"
   cp -r companion-module "%APPDATA%\Companion\module-local\r-speaker-teleprompter"
   ```

3. **Riavvia Companion**:
   - Chiudi completamente Companion
   - Riavvialo

### Metodo 2: Installazione via Developer Mode

1. In Companion, vai su **Settings** → **Developer**
2. Abilita **Developer Mode**
3. Clicca **Scan for modules**
4. Naviga fino a `C:\Users\andre\Desktop\R-Speaker- Telepromper\companion-module`
5. Clicca **Add Module**

---

## ⚙️ PARTE 3: Configurazione in Companion

### 3.1 Aggiungere la connessione

1. **Apri Companion** e vai su **Connections**
2. Clicca **Add connection** (+ in alto a destra)
3. Cerca **"R-Speaker"** o **"Teleprompter"**
4. Seleziona **R-Speaker Teleprompter**
5. Clicca **Add**

### 3.2 Configurare i parametri

Nella schermata di configurazione, imposta:

| Parametro | Valore | Note |
|-----------|--------|------|
| **Label** | R-Speaker | Nome identificativo |
| **Target IP** | 127.0.0.1 | Se su stesso PC |
| **OSC Port** | 8000 | Porta comandi |
| **Feedback Port** | 8001 | Porta status |

> 💡 **Suggerimento**: Se usi Companion su un altro computer, inserisci l'IP del PC con R-Speaker

### 3.3 Verificare la connessione

- Il pallino accanto alla connessione dovrebbe diventare **verde** ✅
- Se rimane **rosso** ❌, verifica:
  - R-Speaker Teleprompter è in esecuzione?
  - Le porte sono corrette?
  - Il firewall blocca le connessioni?

---

## 🎮 PARTE 4: Configurazione dei Controlli

### 4.1 Usare i Preset

1. Vai su **Buttons**
2. Nel pannello di destra, trova **R-Speaker Teleprompter**
3. Espandi le categorie:
   - **Playback**: Play, Stop, Reset
   - **Speed**: Speed Up, Speed Down
   - **Navigation**: Next/Previous Script
   - **Display**: Font Size, Mirror Mode

4. **Trascina i preset** sulla tua griglia di pulsanti

### 4.2 Creare pulsanti personalizzati

1. Clicca su un pulsante vuoto
2. Vai su **Actions** → **Add action**
3. Seleziona **R-Speaker Teleprompter**
4. Scegli l'azione desiderata

#### Azioni disponibili:

| Categoria | Azione | Descrizione | Parametri |
|-----------|--------|-------------|-----------|
| **Playback** | Start/Play | Avvia lo scorrimento | Nessuno |
| | Stop/Pause | Ferma lo scorrimento | Nessuno |
| | Reset | Torna all'inizio | Nessuno |
| **Speed** | Set Speed | Imposta velocità | 1-10 |
| | Speed Increase | Aumenta velocità | Nessuno |
| | Speed Decrease | Diminuisci velocità | Nessuno |
| **Font** | Set Font Size | Imposta dimensione | 20-200 px |
| | Font Increase | Aumenta font | Nessuno |
| | Font Decrease | Diminuisci font | Nessuno |
| **Script** | Next Script | Script successivo | Nessuno |
| | Previous Script | Script precedente | Nessuno |
| | Load Script | Carica script specifico | Index (0+) |
| **Navigation** | Jump to Top | Vai all'inizio | Nessuno |
| | Jump to Bottom | Vai alla fine | Nessuno |
| | Set Position | Imposta posizione | 0-100% |
| **Display** | Mirror Toggle | Attiva/Disattiva specchio | Nessuno |

### 4.3 Configurare i Feedback

I feedback mostrano lo stato in tempo reale:

1. Clicca su un pulsante
2. Vai su **Feedbacks** → **Add feedback**
3. Seleziona **R-Speaker Teleprompter**
4. Scegli il feedback:
   - **Playing Status**: Verde quando in riproduzione
   - **Current Speed**: Mostra velocità attuale
   - **Mirror Status**: Blu quando specchiato

---

## 🎯 PARTE 5: Esempi di Configurazione

### Esempio 1: Pulsante Play/Stop Toggle

```
Actions:
- On Press: Start/Play
Feedbacks:
- Playing Status (Verde quando attivo)
Appearance:
- Text: ▶/⏸
- Size: 24
- Default Color: Bianco su Nero
```

### Esempio 2: Controllo Velocità con Display

```
Button 1 (Speed Display):
- Feedback: Current Speed
- Text: Speed: $(r-speaker:currentSpeed)

Button 2 (Speed Up):
- Action: Speed Increase
- Text: Speed +
- Color: Arancione

Button 3 (Speed Down):
- Action: Speed Decrease
- Text: Speed -
- Color: Arancione
```

### Esempio 3: Navigazione Script

```
Row Layout:
[Previous] [Script Name] [Next]

Previous Button:
- Action: Previous Script
- Text: ◀ PREV

Next Button:
- Action: Next Script
- Text: NEXT ▶
```

---

## 🔧 PARTE 6: Troubleshooting

### Problema: Connessione non si stabilisce

**Soluzioni**:
1. Verifica che R-Speaker sia in esecuzione
2. Controlla il firewall Windows:
   ```powershell
   netsh advfirewall firewall add rule name="OSC 8000" dir=in action=allow protocol=UDP localport=8000
   netsh advfirewall firewall add rule name="OSC 8001" dir=in action=allow protocol=UDP localport=8001
   ```
3. Test con `127.0.0.1` invece dell'IP di rete

### Problema: I comandi non funzionano

**Soluzioni**:
1. Verifica nella console di R-Speaker se arrivano i messaggi OSC
2. Controlla che non ci siano altri software che usano le porte 8000/8001
3. Riavvia sia R-Speaker che Companion

### Problema: Feedback non si aggiornano

**Soluzioni**:
1. Verifica che la porta feedback (8001) sia configurata
2. Controlla che il feedback sia abilitato nel pulsante
3. Ricarica la connessione in Companion

---

## 📊 PARTE 7: Test di Funzionamento

### Checklist di verifica:

- [ ] R-Speaker Teleprompter si avvia correttamente
- [ ] Il server OSC mostra "listening on port 8000"
- [ ] Companion mostra connessione verde
- [ ] Il pulsante Play avvia lo scorrimento
- [ ] Il pulsante Stop ferma lo scorrimento
- [ ] Speed Up/Down modificano la velocità
- [ ] I feedback mostrano lo stato corretto
- [ ] Mirror Toggle funziona
- [ ] La navigazione script funziona

---

## 🌐 PARTE 8: Configurazione Avanzata

### Uso con Stream Deck

1. Installa **Companion Bridge** per Stream Deck
2. Configura i pulsanti in Companion
3. Appariranno automaticamente su Stream Deck

### Controllo da Remoto

Per controllare da un altro computer:

1. **Su PC con R-Speaker**:
   - Trova il tuo IP: `ipconfig` (Windows)
   - Esempio: `192.168.1.100`

2. **Su PC con Companion**:
   - Target IP: `192.168.1.100`
   - OSC Port: `8000`

### Automazione con Trigger

Crea automazioni:
1. Vai su **Triggers**
2. Crea nuovo trigger
3. Condizione: Time of Day / Variable / etc.
4. Azione: R-Speaker command

---

## 💡 PARTE 9: Best Practices

### Layout Consigliato per Stream Deck

```
[PLAY]  [STOP]  [RESET]  [SPEED]
[◀PREV] [PAUSE] [NEXT▶]  [5]
[TOP↑]  [MIRROR][BOT↓]   [FONT+]
[SCRIPT1][SCRIPT2][SCRIPT3][FONT-]
```

### Shortcuts Utili

| Funzione | Shortcut Suggerito |
|----------|-------------------|
| Play/Stop Toggle | `F1` |
| Reset | `F2` |
| Speed Up | `+` |
| Speed Down | `-` |
| Mirror Toggle | `M` |

---

## 📞 PARTE 10: Supporto

### Risorse Utili

- **Documentazione Companion**: https://docs.bitfocus.io/
- **R-Speaker Repository**: [GitHub Link]
- **Community Discord**: [Discord Link]

### Log e Debug

Per debug avanzato:

1. **R-Speaker Logs**:
   - Verifica la barra di stato dell'applicazione per messaggi OSC
   - Controlla gli eventi nella finestra principale

2. **Companion Logs**:
   - Settings → Log → Set to Debug
   - Controlla i messaggi OSC

### Contatti

- **Email supporto**: support@r-speaker.com
- **Issue tracker**: GitHub Issues
- **Forum**: community.r-speaker.com

---

## 🎉 Conclusione

Ora sei pronto per controllare R-Speaker Teleprompter con Bitfocus Companion!

**Funzionalità principali disponibili**:
- ✅ Controllo completo playback
- ✅ Regolazione velocità in tempo reale
- ✅ Gestione font e display
- ✅ Navigazione script
- ✅ Feedback visivi
- ✅ Automazione
- ✅ Controllo remoto

**Happy Teleprompting!** 🎬

---

*Versione documento: 1.1.0*
*Ultimo aggiornamento: febbraio 2026*
