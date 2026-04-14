# TASK BATCH — Live Speaker Teleprompter

**Data:** 14 Aprile 2026  
**Versione base:** 2.3.5  
**Autore analisi:** Senior Software Architect (Claude Desktop)  
**Destinatario esecuzione:** Cursor Composer 1.5  
**Riferimenti:**
- `docs/ARCHITETTURA_Live_Speaker_Teleprompter.md` (§§ 8–15)
- `docs/BugFix_Refactor_Implementazioni_Live_Speaker_Teleprompter.md` (piano refactoring)
- `docs/Istruzioni_Progetto_Claude_Live_Speaker_Teleprompter.md` (formato task)

---

## 📊 STATO ESECUZIONE

**Branch:** `task-batch-2026-04-14`
**Build baseline:** ✅ 0 errori, 0 warning
**Ultimo aggiornamento:** 2026-04-14

| # | ID | Stato | Commit |
|---|---|---|---|
| 1 | TASK-001 | ✅ COMPLETATO | 9c6dfa1 |
| 2 | TASK-002 | ✅ COMPLETATO | 53bcdeb |
| 3 | TASK-003 | ✅ COMPLETATO | 53bcdeb |
| 4 | TASK-004 | ✅ COMPLETATO | 9754ea0 |
| 5 | TASK-005 | ✅ COMPLETATO | 122d353 |
| 6 | TASK-006 | ✅ COMPLETATO | 122d353 |
| 7 | TASK-007 | ✅ COMPLETATO | 950e356 |
| 8 | TASK-008 | ✅ COMPLETATO | 9ce0d7e |
| 9 | TASK-009 | ✅ COMPLETATO | 9ce0d7e |
| 10 | TASK-010 | ✅ COMPLETATO | 91cfa31 |
| 11 | TASK-011 | ✅ COMPLETATO | f04ffbd |

**Legenda:** ⬜ TODO · ⏳ IN CORSO · ✅ COMPLETATO · ⚠️ PARZIALE · ❌ BLOCCATO

### ✅ Batch completato — 14 Aprile 2026

**Tutti gli 11 task chiusi in 8 commit su branch `task-batch-2026-04-14`:**

| Commit | Hash | Task |
|---|---|---|
| 1 | 9c6dfa1 | TASK-001 |
| 2 | 53bcdeb | TASK-002 + TASK-003 |
| 3 | 9754ea0 | TASK-004 |
| 4 | 122d353 | TASK-005 + TASK-006 |
| 5 | 950e356 | TASK-007 |
| 6 | 9ce0d7e | TASK-008 + TASK-009 |
| 7 | 91cfa31 | TASK-010 |
| 8 | f04ffbd | TASK-011 |

**Build finale:** `dotnet build -c Release` → 0 errori, 0 warning.

**Follow-up residui:**
- `companion-module/index.js` (Node.js, fuori dal batch) ancora dichiara range `-20/+20` step `0.25`. Da allineare a `-80/+80` step `0.5` in task successivo (vedi TASK-002).
- Test manuali (OSC monitor, Companion HTTP, NDI, hot-plug monitor, switch lingua IT/EN, stabilità live) da eseguire con l'eseguibile di release prima del merge (checklist post-esecuzione a fondo documento).
- Aggiornare `docs/BugFix_Refactor_Implementazioni_Live_Speaker_Teleprompter.md` con un blocco `CHANGELOG v2.3.6` elencando i task completati (come da strategia di commit).

---

## Sintesi esecutiva

L'analisi del codice sorgente (`src/TeleprompterApp/`) ha evidenziato **11 problemi concreti** divisi tra:

- **3 BUG FIX ad alta priorità** che possono causare crash o comportamento errato durante un evento live
- **2 incoerenze** tra `CompanionBridge` e `MainWindow` che rompono il contratto di velocità
- **3 problemi di stabilità/performance** (i18n hardcoded, feedback OSC non throttled, fallback clone mancante)
- **3 refactor SAFE** per alleggerire MainWindow (God Class ~3400 righe, file da 121 KB)

I task sono ordinati **per priorità decrescente**. Il primo blocco (ALTA) deve essere eseguito e committato prima di procedere al secondo. **Nessun task tocca lo ScrollEngine** (vincolo sacro #10).

---

## Indice task

| # | ID | Tipo | Priorità | Titolo | Effort |
|---|---|---|---|---|---|
| 1 | TASK-001 | BUG FIX | 🔴 ALTA | Rimuovere `Shutdown(-1)` da `OnDispatcherUnhandledException` | 15 min |
| 2 | TASK-002 | BUG FIX | 🔴 ALTA | Allineare `SpeedStep`/`MaxSpeed` di `CompanionBridge` a `MainWindow` | 10 min |
| 3 | TASK-003 | BUG FIX | 🔴 ALTA | Localizzare messaggi di errore hardcoded in `CompanionBridge` | 20 min |
| 4 | TASK-004 | BUG FIX | 🟡 MEDIA | Fallback `XamlWriter` in `PresenterSyncService` | 25 min |
| 5 | TASK-005 | REFACTOR | 🟡 MEDIA | Throttle feedback OSC durante scroll | 30 min |
| 6 | TASK-006 | BUG FIX | 🟡 MEDIA | Validazione argomenti OSC in `OscBridge.DispatchMessage` | 30 min |
| 7 | TASK-007 | BUG FIX | 🟡 MEDIA | Visibilità errori NDI in Release (`OnRendering` swallow) | 20 min |
| 8 | TASK-008 | REFACTOR | 🟡 MEDIA | Estrarre `OscTypeParser` (safe) | 15 min |
| 9 | TASK-009 | REFACTOR | 🟡 MEDIA | Estrarre `OscCommandHandler` (safe) | 60 min |
| 10 | TASK-010 | REFACTOR | 🟢 BASSA | Estrarre `DocumentFileService` (safe) | 45 min |
| 11 | TASK-011 | BUG FIX | 🟢 BASSA | `DisplayManager.GetDisplayNumber` — collisione digit extraction | 15 min |

---

# PRIORITÀ ALTA 🔴

---

## [TASK-001] BUG FIX: Rimuovere `Shutdown(-1)` da `OnDispatcherUnhandledException`

**FILE:** `src/TeleprompterApp/App.xaml.cs`

### SINTOMO
Durante un evento live, una qualsiasi eccezione non gestita sul dispatcher UI (es. errore di sync presenter, eccezione RichTextBox su documento complesso, glitch NDI) causa il **completo shutdown dell'applicazione** con `Shutdown(-1)`, interrompendo la produzione.

### CAUSA ROOT
In `App.xaml.cs` il handler chiude l'app:

```csharp
private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
{
    LogException("Dispatcher", e.Exception);
    MessageBox.Show(Localization.Get("Error_Unhandled", e.Exception.Message), ...);
    e.Handled = true;
    Shutdown(-1);  // ← VIOLA "stabilità live priorità n.1"
}
```

Questo **viola il vincolo sacro #2** (`docs/ARCHITETTURA §19`): "Stabilità live è priorità n.1 — mai crashare durante un evento".

### FIX
1. Rimuovere la chiamata `Shutdown(-1)` — dopo `e.Handled = true` l'app deve **continuare a girare**.
2. Mantenere il logging e il MessageBox (non invasivo — l'operatore può ignorarlo).
3. Aggiungere un commento esplicito che richiama il vincolo sacro:

```csharp
private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
{
    LogException("Dispatcher", e.Exception);
    try
    {
        MessageBox.Show(
            Localization.Get("Error_Unhandled", e.Exception.Message),
            Localization.Get("Error_Title"),
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
    catch { /* non-fatal */ }

    // SACRED RULE #2: stabilità live. MAI Shutdown in un handler globale:
    // durante un evento live l'app deve restare viva anche dopo un'eccezione.
    // L'operatore chiuderà manualmente a fine show se necessario.
    e.Handled = true;
}
```

### VINCOLI (non toccare)
- Non rimuovere il `LogException`
- Non rimuovere il `MessageBox` (serve all'operatore per capire che qualcosa è andato storto)
- Non toccare `OnUnhandledException` (domain-level, già non chiude)

### TEST
- `dotnet build -c Release` → 0 errori, 0 warning nuovi
- Test manuale: provocare un'eccezione di test (es. in un handler di un bottone, lanciare `throw new InvalidOperationException("test")`). Verificare che appaia il MessageBox e l'app **rimanga aperta** dopo OK.
- Verificare che il file di log in `%APPDATA%\Live Speaker Teleprompter\logs\` contenga l'eccezione.

---

## [TASK-002] BUG FIX: Allineare `SpeedStep` e `MaxSpeed` di `CompanionBridge` a `MainWindow`

**FILE:** `src/TeleprompterApp/CompanionBridge.cs`

### SINTOMO
I comandi HTTP `/teleprompter/speed/up`, `/speed/down` e `/speed/set?value=X` non rispettano il range di velocità usato dal resto dell'app. Un utente Companion che manda `speed/up` vede +0.25 invece di +0.5, e un `speed/set?value=60` viene clampato a 20 anche se l'app supporta fino a 80.

### CAUSA ROOT
In `CompanionBridge.cs` ci sono costanti **disallineate** rispetto a `MainWindow`:

```csharp
// CompanionBridge.cs
private const double SpeedStep = 0.25;
private const double MaxSpeed = 20;
```

```csharp
// MainWindow.xaml.cs
private const double SpeedStep = 0.5;
private const double MaxSpeed = 80;
```

Il contratto documentato in `ARCHITETTURA §10` e nei README dichiara range **-80 ÷ +80 step 0.5**. CompanionBridge usa valori vecchi (ereditati da v2.2). Questo **spezza l'integrazione Companion in produzione**: lo Stream Deck sembra funzionare ma incrementa con step diverso rispetto al panello fisico/OSC.

> Nota: anche il modulo Node `companion-module/index.js` (vedi `docs/Setup_Companion_Live_Speaker_Teleprompter.md`) dichiara range -20/+20 step 0.25. **Dopo questo task, va aggiornato anche quello** (vedi VINCOLI).

### FIX
1. In `CompanionBridge.cs`, allineare le costanti:

```csharp
private const double SpeedStep = 0.5;   // allineato a MainWindow
private const double MaxSpeed = 80;     // allineato a MainWindow
```

2. Verificare che `HandleSpeedCommandAsync` usi queste costanti (già OK — usa `SpeedStep` e `MaxSpeed`, non letterali).

3. Aggiornare il commento di testa del file menzionando l'origine del range (single source of truth = MainWindow).

4. **Opzionale (consigliato)**: esporre le costanti come proprietà pubbliche di `MainWindow` (`MainWindow.SpeedStep`, `MainWindow.MaxSpeed`) e farle leggere a `CompanionBridge` via `_owner.SpeedStep`/`_owner.MaxSpeed`. Questo elimina la duplicazione alla radice.

### VINCOLI (non toccare)
- Non modificare `MainWindow.SpeedStep` / `MainWindow.MaxSpeed` (sono il riferimento corretto)
- Non modificare il range OSC in `OscBridge.cs` (già corretto, passa per `MainWindow.SetSpeed`)
- **Segnalare all'imprenditore che `companion-module/index.js` va aggiornato in un task successivo** (è Node.js separato, non rientra in questo task atomico)

### TEST
- `dotnet build -c Release` → 0 errori
- Test manuale HTTP:
  ```bash
  curl http://localhost:3131/teleprompter/speed/set?value=60
  curl http://localhost:3131/teleprompter/status
  ```
  Verificare che `"speed": 60.0` compaia nello status (prima veniva clampato a 20).
- Test manuale: `/speed/up` → verificare che il `SpeedSlider` avanzi di 0.5 (non 0.25).

---

## [TASK-003] BUG FIX: Localizzare messaggi di errore hardcoded in `CompanionBridge`

**FILE:** `src/TeleprompterApp/CompanionBridge.cs`

### SINTOMO
I messaggi di errore HTTP restituiti a Companion contengono **stringhe italiane hardcoded** che non rispettano la lingua selezionata dall'utente e violano il workflow i18n (vincolo sacro #9).

Esempi concreti nel codice:
```csharp
throw new NotSupportedException("Endpoint non trovato.");
throw new NotSupportedException($"Comando '{command}' non supportato.");
throw new ArgumentException("Parametro 'value' mancante.");
throw new ArgumentException("Valore velocità non valido.");
throw new NotSupportedException($"Comando velocità '{subCommand}' non supportato.");

responseMessage = "Riproduzione avviata";
responseMessage = "Riproduzione in pausa";
responseMessage = "Stato play/pausa invertito";
message = "Velocità aumentata";
message = "Velocità diminuita";
message = "Velocità azzerata";
message = $"Velocità impostata a {target:F2}";
```

### CAUSA ROOT
Il codice è stato scritto prima dell'introduzione di `Localization.cs` (v2.3.3) e **non è mai stato portato al nuovo sistema i18n**. Questo viola il vincolo sacro #9 (ogni stringa IT deve avere pair EN).

### FIX
1. Aggiungere le seguenti chiavi in `src/TeleprompterApp/Localization.cs` **sia nel dizionario `It` che in `En`**:

| Chiave | IT | EN |
|---|---|---|
| `Companion_EndpointNotFound` | `"Endpoint non trovato."` | `"Endpoint not found."` |
| `Companion_CommandNotSupported` | `"Comando '{0}' non supportato."` | `"Command '{0}' not supported."` |
| `Companion_SpeedValueMissing` | `"Parametro 'value' mancante."` | `"Missing 'value' parameter."` |
| `Companion_SpeedValueInvalid` | `"Valore velocità non valido."` | `"Invalid speed value."` |
| `Companion_SpeedCommandNotSupported` | `"Comando velocità '{0}' non supportato."` | `"Speed command '{0}' not supported."` |
| `Companion_PlayStarted` | `"Riproduzione avviata"` | `"Playback started"` |
| `Companion_PlayPaused` | `"Riproduzione in pausa"` | `"Playback paused"` |
| `Companion_PlayToggled` | `"Stato play/pausa invertito"` | `"Play/pause toggled"` |
| `Companion_SpeedIncreased` | `"Velocità aumentata"` | `"Speed increased"` |
| `Companion_SpeedDecreased` | `"Velocità diminuita"` | `"Speed decreased"` |
| `Companion_SpeedReset` | `"Velocità azzerata"` | `"Speed reset to zero"` |
| `Companion_SpeedSet` | `"Velocità impostata a {0:F2}"` | `"Speed set to {0:F2}"` |
| `Companion_StatusTitle` | `"Stato teleprompter"` | `"Teleprompter status"` |

2. In `CompanionBridge.cs`, sostituire TUTTE le stringhe hardcoded con `Localization.Get("...")` o `Localization.Get("...", arg)`.

Esempi:
```csharp
// PRIMA
throw new NotSupportedException("Endpoint non trovato.");
// DOPO
throw new NotSupportedException(Localization.Get("Companion_EndpointNotFound"));

// PRIMA
responseMessage = "Riproduzione avviata";
// DOPO
responseMessage = Localization.Get("Companion_PlayStarted");

// PRIMA
message = $"Velocità impostata a {target:F2}";
// DOPO
message = Localization.Get("Companion_SpeedSet", target);
```

3. Verificare che `return BuildResponse(message: "Stato teleprompter");` usi `Localization.Get("Companion_StatusTitle")`.

### VINCOLI (non toccare)
- **Aggiornare IT e EN insieme** nello stesso commit (vincolo sacro #9)
- Non cambiare la struttura JSON della risposta (Companion legge `status`, `message`, `isPlaying`, `speed`, `editMode`, `endpoint`)
- Non cambiare gli status code HTTP (200/400/404/500 restano com'è)
- Terminologia EN professionale (broadcast/teleprompter) — non traduzioni letterali

### TEST
- `dotnet build -c Release` → 0 errori
- Test manuale con lingua impostata a EN:
  ```bash
  curl http://localhost:3131/teleprompter/play
  # Aspettato: {"status":"ok","message":"Playback started",...}
  curl http://localhost:3131/teleprompter/wrong
  # Aspettato: status 404 con message "Command 'wrong' not supported."
  ```
- Test con lingua IT: verificare stessi endpoint restituiscono messaggi italiani.
- Switch lingua a runtime tramite ComboBox toolbar → nuova richiesta HTTP deve riflettere la nuova lingua.

---

# PRIORITÀ MEDIA 🟡

---

## [TASK-004] BUG FIX: Fallback `XamlWriter` in `PresenterSyncService`

**FILE:** `src/TeleprompterApp/Services/PresenterSyncService.cs`

### SINTOMO
Il documento `ARCHITETTURA §5.2` dichiara:
> **Fallback:** se `XamlPackage` fallisce, usa `XamlWriter.Save` (~300ms).

**Ma nel codice attuale questo fallback non esiste.** Se `XamlPackage` fallisce (documenti con oggetti non serializzabili, embedded images rotti, etc.), il sync al Presenter viene silenziosamente abortito e la finestra esterna mostra contenuto stale. Durante un live, il presenter potrebbe restare "congelato" sull'ultimo documento sincronizzato senza che l'operatore se ne accorga.

### CAUSA ROOT
In `PresenterSyncService.SerializeDocument`:

```csharp
private static (byte[] data, DocProps props)? SerializeDocument(FlowDocument source)
{
    try
    {
        // ...tenta XamlPackage...
        return (stream.ToArray(), props);
    }
    catch
    {
        return null;  // ← nessun fallback, sync perso silenziosamente
    }
}
```

E `DoSync` tratta `null` come "ignora e basta":
```csharp
var serialized = SerializeDocument(source);
if (serialized == null) return;  // ← operatore non sa nulla
```

### FIX
1. Aggiungere un metodo privato `SerializeDocumentXamlFallback` che usa `XamlWriter.Save` + `XamlReader.Parse` per ricreare il FlowDocument:

```csharp
private static FlowDocument? TryDeserializeWithXamlWriter(FlowDocument source, DocProps props)
{
    try
    {
        var xaml = System.Windows.Markup.XamlWriter.Save(source);
        if (System.Windows.Markup.XamlReader.Parse(xaml) is not FlowDocument clone)
            return null;

        clone.PagePadding = props.PagePadding;
        clone.PageWidth = props.PageWidth;
        clone.LineHeight = props.LineHeight;
        clone.TextAlignment = props.TextAlignment;
        clone.Background = props.Background;
        clone.FontFamily = props.FontFamily;
        clone.FontSize = props.FontSize;
        clone.Foreground = props.Foreground;
        clone.FontWeight = props.FontWeight;
        clone.FontStyle = props.FontStyle;
        return clone;
    }
    catch
    {
        return null;
    }
}
```

2. In `DoSync`, dopo il tentativo XamlPackage, se il risultato è `null` provare il fallback:

```csharp
_isSyncing = true;
try
{
    FlowDocument? clone = null;
    var serialized = SerializeDocument(source);
    if (serialized != null)
    {
        clone = DeserializeDocument(serialized.Value.data, serialized.Value.props);
    }
    else
    {
        // Fallback XamlWriter — più lento (~300ms) ma più robusto
        var props = CaptureProps(source);
        clone = TryDeserializeWithXamlWriter(source, props);
    }

    if (clone != null)
    {
        _applyToPresenter(clone);
    }
    // else: entrambi i metodi falliti — sync skippato (log non disponibile qui, 
    //       MainWindow può mostrare status "Presenter sync failed")
}
```

3. Estrarre la costruzione di `DocProps` in un metodo privato `CaptureProps(FlowDocument)` per evitare duplicazione.

4. **Opzionale (consigliato)**: aggiungere un evento `public event Action<string>? SyncFailed;` che MainWindow può sottoscrivere per mostrare `SetStatus(Localization.Get("Status_PresenterSyncFailed"))`. Aggiungere le chiavi IT/EN corrispondenti.

### VINCOLI (non toccare)
- **MAI** chiamare `DoSync` dal rendering tick (il debounce 300ms è sacro — vincolo #4)
- Non cambiare il debounce da 300ms
- Non toccare `MarkDirty` / `OnDebounceElapsed`
- Il fallback XamlWriter è ~10x più lento: usarlo solo se XamlPackage fallisce

### TEST
- `dotnet build -c Release` → 0 errori
- Test manuale normale: scrivere testo, cambiare font, colori. Il sync deve funzionare come prima (XamlPackage path).
- Test di stress: caricare un `.rstp` con formattazione complessa, verificare che il presenter si aggiorni entro 300-600ms.
- **Test di fallback (difficile da provocare):** modificare temporaneamente `SerializeDocument` per lanciare un'eccezione e verificare che il fallback XamlWriter entri in azione. Poi ripristinare.

---

## [TASK-005] REFACTOR: Throttle feedback OSC durante scroll

**FILE:** `src/TeleprompterApp/OscBridge.cs` (+ integrazione in `MainWindow.xaml.cs`)

### PROBLEMA
Il feedback OSC di posizione scroll (`/teleprompter/position/current`) è chiamato potenzialmente **ad ogni frame** del rendering tick (60 Hz) o almeno ad ogni `ScrollChanged`. Questo:

1. Satura la rete locale durante lo scroll continuo (60 pacchetti UDP/secondo per client)
2. Costringe il client OSC (Companion, Touch OSC, X-Air Edit, ecc.) a processare aggiornamenti inutili
3. Con più controller connessi, amplifica il problema
4. Non è previsto in architettura (`ARCHITETTURA §9` non menziona feedback throttling ma è una best practice OSC)

### SOLUZIONE
1. In `OscBridge.cs`, aggiungere un meccanismo di throttling basato su `Stopwatch` e un dizionario di "ultimo invio per indirizzo":

```csharp
private readonly System.Collections.Concurrent.ConcurrentDictionary<string, long> _lastFeedbackTicks = new();
private const long MinFeedbackIntervalMs = 50; // 20 Hz max per indirizzo

public void SendFeedbackThrottled(string address, params object[] values)
{
    var now = Environment.TickCount64;
    var last = _lastFeedbackTicks.GetOrAdd(address, 0L);
    if (now - last < MinFeedbackIntervalMs)
        return;

    _lastFeedbackTicks[address] = now;
    SendFeedback(address, values);
}
```

2. Mantenere `SendFeedback` invariato per cambi di stato **booleani** (play/pause, mirror, NDI) che vanno trasmessi sempre. Usare `SendFeedbackThrottled` **solo per valori continui**:
   - `/teleprompter/speed/current`
   - `/teleprompter/position/current`
   - `/teleprompter/font/size/current`
   - `/ndi/framerate/current`

3. In `MainWindow.xaml.cs`, trovare tutte le chiamate `_oscBridge?.SendFeedback(...)` per questi indirizzi e sostituirle con `SendFeedbackThrottled`.

### VINCOLI (non toccare)
- **Non throttlare i feedback di stato discreti** (`/teleprompter/status` playing/stopped, `/teleprompter/mirror/status`, `/ndi/status`)
- Non chiamare `SendFeedback` dal rendering tick direttamente
- Non toccare il path di ricezione (`ListenLoopAsync`, `DispatchPacket`)
- Mantenere thread-safety: `ConcurrentDictionary` è obbligatorio

### TEST
- `dotnet build -c Release` → 0 errori
- Test manuale: avviare scroll a velocità 5, monitorare porta 8001 con un OSC monitor (es. Protokol, OSC Monitor di Hexler). Verificare che `/teleprompter/position/current` arrivi al massimo ~20 volte/secondo (non 60).
- Test OSC: inviare `/teleprompter/play`, verificare che `/teleprompter/status` arrivi **immediatamente** (non throttlato).
- Test Companion: i feedback del pulsante PLAY devono continuare a funzionare senza ritardi percepibili.

---

## [TASK-006] BUG FIX: Validazione argomenti OSC in `OscBridge.DispatchMessage`

**FILE:** `src/TeleprompterApp/OscBridge.cs`

### SINTOMO
`OscBridge.DispatchMessage` inoltra i messaggi OSC a `MainWindow.HandleOscMessage` **senza alcuna validazione** del numero o tipo di argomenti. Un pacchetto OSC malformato (es. `/teleprompter/speed` senza float, o con stringa invece di numero) può causare:
- Eccezioni nel handler di MainWindow (gestite silenziosamente da `App.OnDispatcherUnhandledException` → log ma UX compromessa)
- Valori indefiniti passati a `SetSpeed` / `SetPosition`
- Potenziale blocco dello UI thread se il handler è sincrono

### CAUSA ROOT
```csharp
private void DispatchMessage(OscMessage message)
{
    var args = message.Arguments ?? Array.Empty<object>();
    _owner.Dispatcher.InvokeAsync(() => _owner.HandleOscMessage(message.Address, args.ToList()));
}
```

Nessun type-check, nessun log di pacchetti scartati.

### FIX
1. Aggiungere una `LoggingAction` (opzionale tramite callback) per notificare pacchetti scartati. Alternativa: usare `Debug.WriteLine` in Debug build.

2. Sanitizzare gli argomenti prima della dispatch, convertendo tipi numerici in `double` per uniformità:

```csharp
private void DispatchMessage(OscMessage message)
{
    if (string.IsNullOrWhiteSpace(message.Address))
        return;

    // Sanitize args: null-safe list, nothing else
    var args = (message.Arguments ?? Array.Empty<object>())
        .Where(a => a != null)
        .ToList();

    _owner.Dispatcher.InvokeAsync(() =>
    {
        try
        {
            _owner.HandleOscMessage(message.Address, args);
        }
        catch (Exception ex)
        {
            // Log but never crash — questo è il punto di entry di input esterno
            Debug.WriteLine($"OSC handler error [{message.Address}]: {ex.Message}");
        }
    });
}
```

3. La validazione **type-safe dei singoli argomenti** va fatta dentro `HandleOscMessage` o nell'estraendo `OscCommandHandler` del TASK-009. Qui ci limitiamo al null-safety e all'exception-safety.

### VINCOLI (non toccare)
- Non modificare `OscPacket.Parse` (zero-allocation path critico)
- Non modificare le porte 8000/8001
- Non rimuovere `InvokeAsync` (marshalling UI thread obbligatorio)
- La validazione semantica dei valori (range, clamp) resta in `MainWindow` / `OscCommandHandler`

### TEST
- `dotnet build -c Release` → 0 errori
- Test manuale con un client OSC (es. TouchOSC):
  - Inviare `/teleprompter/speed` senza argomenti → app non crasha, status resta stabile
  - Inviare `/teleprompter/speed` con argomento stringa `"abc"` → app non crasha
  - Inviare `/teleprompter/play` (nessun argomento, già funzionante) → play parte normalmente
- Verificare che il log non esploda con pacchetti malformati

---

## [TASK-007] BUG FIX: Visibilità errori NDI in Release (`OnRendering` swallow)

**FILE:** `src/TeleprompterApp/NDITransmitter.cs`

### SINTOMO
Se durante uno streaming NDI live si verifica un errore (es. buffer corruption, source disposed, `ProcessNDI4.dll` che restituisce errore), il problema viene **completamente invisibile in Release build** perché `Debug.WriteLine` non scrive da nessuna parte.

### CAUSA ROOT
```csharp
private void OnRendering(object? sender, EventArgs e)
{
    // ...
    try { CaptureAndSendFrame(); }
    catch (Exception ex)
    {
        Debug.WriteLine($"NDI frame capture error: {ex.Message}");  // ← invisibile in Release
    }
}
```

In Release build, `Debug.WriteLine` è un no-op. L'operatore non ha modo di accorgersi che NDI sta perdendo frame o crashando internamente finché il toggle NDI non diventa inutile.

### FIX
1. Introdurre un contatore di errori e un callback opzionale per notificare MainWindow:

```csharp
private int _consecutiveErrors;
private long _lastErrorLogTicks;
private const int MaxConsecutiveErrorsBeforeStop = 30;  // ~1 secondo a 30 fps
private const long ErrorLogThrottleMs = 5000;            // log max ogni 5s

public event Action<string>? FrameError;

private void OnRendering(object? sender, EventArgs e)
{
    if (!_isRunning || _sendInstance == IntPtr.Zero) return;

    var elapsed = _frameClock.Elapsed.TotalMilliseconds;
    if (elapsed < _minFrameIntervalMs) return;

    _frameClock.Restart();

    try
    {
        CaptureAndSendFrame();
        _consecutiveErrors = 0;  // reset su successo
    }
    catch (Exception ex)
    {
        _consecutiveErrors++;
        
        // Throttled error notification
        var now = Environment.TickCount64;
        if (now - _lastErrorLogTicks > ErrorLogThrottleMs)
        {
            _lastErrorLogTicks = now;
            Debug.WriteLine($"NDI frame capture error: {ex.Message}");
            try { FrameError?.Invoke(ex.Message); } catch { }
        }

        // Auto-stop se troppi errori consecutivi (protegge da loop fatali)
        if (_consecutiveErrors >= MaxConsecutiveErrorsBeforeStop)
        {
            Debug.WriteLine("NDI: too many consecutive errors, stopping");
            _dispatcher.BeginInvoke(() => Stop());
        }
    }
}
```

2. In `MainWindow`, sottoscrivere l'evento e mostrare status localizzato:

```csharp
_ndiTransmitter.FrameError += msg =>
    Dispatcher.BeginInvoke(() => SetStatus(Localization.Get("Status_NdiError", msg)));
```

3. Aggiungere chiavi `Status_NdiError` in IT/EN in `Localization.cs`:
   - IT: `"Errore NDI: {0}"`
   - EN: `"NDI error: {0}"`

### VINCOLI (non toccare)
- **MAI** lanciare eccezioni da `OnRendering` (rompe la composition pipeline — vincolo di base WPF)
- Non rimuovere il try/catch esterno
- Non loggare ogni frame (flood). Il throttling 5s è critico
- Non chiamare `Stop()` direttamente dal tick — usare `Dispatcher.BeginInvoke`

### TEST
- `dotnet build -c Release` → 0 errori
- Test manuale: avviare NDI, trasmettere ~30 secondi normalmente. Nessun errore atteso.
- Test provocato: temporaneamente rinominare `ProcessNDI4.dll` mentre l'app gira (se fattibile) oppure modificare `CaptureAndSendFrame` per lanciare eccezione random 10% del tempo. Verificare che lo status bar mostri "Errore NDI: ..." e che dopo 30 errori consecutivi il toggle NDI si spenga automaticamente.
- Verificare assenza di crash dell'app o del rendering WPF durante gli errori.

---

## [TASK-008] REFACTOR: Estrarre `OscTypeParser`

**FILE:** `src/TeleprompterApp/MainWindow.xaml.cs` → `src/TeleprompterApp/Osc/OscTypeParser.cs` (nuovo)

### PROBLEMA
`MainWindow.HandleOscMessage` contiene funzioni di parsing di argomenti OSC (`TryGetDouble`, `TryGetInt`, `TryGetBool`, `TryGetString`) inline. Sono **funzioni pure senza side-effect** e appesantiscono MainWindow senza motivo. Vedi `docs/BugFix_Refactor_Implementazioni §PIANO REFACTORING MAINWINDOW — 1.1`.

### SOLUZIONE
1. Creare il file `src/TeleprompterApp/Osc/OscTypeParser.cs` nella stessa cartella di `OscPacket.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;

namespace TeleprompterApp.Osc;

internal static class OscTypeParser
{
    public static bool TryGetDouble(IReadOnlyList<object> args, int index, out double value)
    {
        value = 0;
        if (args == null || index < 0 || index >= args.Count) return false;
        var raw = args[index];
        switch (raw)
        {
            case double d: value = d; return true;
            case float f: value = f; return true;
            case int i: value = i; return true;
            case long l: value = l; return true;
            case string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed):
                value = parsed; return true;
            default: return false;
        }
    }

    public static bool TryGetInt(IReadOnlyList<object> args, int index, out int value)
    {
        if (TryGetDouble(args, index, out var d))
        {
            value = (int)Math.Round(d);
            return true;
        }
        value = 0;
        return false;
    }

    public static bool TryGetBool(IReadOnlyList<object> args, int index, out bool value)
    {
        value = false;
        if (args == null || index < 0 || index >= args.Count) return false;
        var raw = args[index];
        switch (raw)
        {
            case bool b: value = b; return true;
            case int i: value = i != 0; return true;
            case float f: value = f != 0f; return true;
            case double d: value = d != 0d; return true;
            case string s:
                if (bool.TryParse(s, out var parsed)) { value = parsed; return true; }
                if (s == "1" || s.Equals("on", StringComparison.OrdinalIgnoreCase)) { value = true; return true; }
                if (s == "0" || s.Equals("off", StringComparison.OrdinalIgnoreCase)) { value = false; return true; }
                return false;
            default: return false;
        }
    }

    public static bool TryGetString(IReadOnlyList<object> args, int index, out string value)
    {
        value = string.Empty;
        if (args == null || index < 0 || index >= args.Count) return false;
        value = Convert.ToString(args[index], CultureInfo.InvariantCulture) ?? string.Empty;
        return !string.IsNullOrEmpty(value);
    }
}
```

2. In `MainWindow.xaml.cs`, trovare le funzioni esistenti `TryGetDouble`, `TryGetInt`, `TryGetBool`, `TryGetString` (probabilmente private nella sezione OSC) e **rimuoverle**.

3. Sostituire tutte le chiamate con `OscTypeParser.TryGetDouble(args, ...)` ecc. Aggiungere `using TeleprompterApp.Osc;` in cima se non già presente.

### VINCOLI (non toccare)
- Funzioni **pure** — zero side-effect, zero dipendenze da MainWindow
- Non cambiare la firma (stessa signature che c'era inline)
- Non toccare `OscPacket.cs` (path zero-allocation già ottimizzato)
- Questo refactor è **SAFE** per definizione — vedi `docs/BugFix_Refactor_Implementazioni §1.1`

### TEST
- `dotnet build -c Release` → 0 errori
- Test manuale OSC: inviare tutti i comandi con argomenti (speed float, position float, font int, mirror bool). Verificare identico comportamento a prima.
- Diff test: compilare prima e dopo, verificare che `MainWindow.xaml.cs` sia più corto di ~50-80 righe.

---

## [TASK-009] REFACTOR: Estrarre `OscCommandHandler`

**FILE:** `src/TeleprompterApp/MainWindow.xaml.cs` → `src/TeleprompterApp/Osc/OscCommandHandler.cs` + `src/TeleprompterApp/Osc/ITeleprompterController.cs` (nuovi)

### PROBLEMA
`MainWindow.HandleOscMessage` contiene uno switch enorme (decine di case) che dispatch i comandi OSC. Appesantisce MainWindow e **viene chiamato dal thread pool** (via `InvokeAsync`), quindi è un punto critico per stabilità. Vedi `docs/BugFix_Refactor_Implementazioni §PIANO REFACTORING — 1.2`.

### SOLUZIONE
Refactor in 3 step atomici.

**Step 1:** Creare l'interfaccia `ITeleprompterController` che espone i metodi che OSC chiama su MainWindow:

```csharp
// src/TeleprompterApp/Osc/ITeleprompterController.cs
namespace TeleprompterApp.Osc;

internal interface ITeleprompterController
{
    bool IsPlaying { get; }
    void SetPlayState(bool playing);
    void AdjustSpeed(double delta);
    void SetSpeed(double value, bool fromSlider);
    void AdjustFontSize(double delta);
    void SetFontSize(double size);
    void SetScrollPosition(double normalized);
    void JumpToTop();
    void JumpToBottom();
    void ToggleMirror();
    void SetMirror(bool enabled);
    // NDI
    void NdiStart();
    void NdiStop();
    void NdiToggle();
    void SetNdiResolution(int width, int height);
    void SetNdiFrameRate(double fps);
    void SetNdiSourceName(string name);
    // Status
    void SendStatusSnapshot();
}
```

**Step 2:** Creare `OscCommandHandler` che riceve `(address, args)` e l'`ITeleprompterController`:

```csharp
// src/TeleprompterApp/Osc/OscCommandHandler.cs
namespace TeleprompterApp.Osc;

internal sealed class OscCommandHandler
{
    private readonly ITeleprompterController _controller;

    public OscCommandHandler(ITeleprompterController controller)
    {
        _controller = controller;
    }

    public void Handle(string address, IReadOnlyList<object> args)
    {
        switch (address)
        {
            case "/teleprompter/play":
            case "/teleprompter/start":
                _controller.SetPlayState(true); break;
            case "/teleprompter/pause":
            case "/teleprompter/stop":
                _controller.SetPlayState(false); break;
            case "/teleprompter/speed":
                if (OscTypeParser.TryGetDouble(args, 0, out var s))
                    _controller.SetSpeed(s, fromSlider: false);
                break;
            case "/teleprompter/speed/increase":
                _controller.AdjustSpeed(+0.5); break;
            case "/teleprompter/speed/decrease":
                _controller.AdjustSpeed(-0.5); break;
            // ... tutti gli altri case da MainWindow.HandleOscMessage
        }
    }
}
```

**Step 3:** In `MainWindow`:
1. Implementare `ITeleprompterController` — la maggior parte dei metodi già esiste, basta aggiungere `: ITeleprompterController` alla classe e marcare internal quelli mancanti.
2. Istanziare `_oscCommandHandler = new OscCommandHandler(this);` in `Window_Loaded` dopo `StartOscIntegration`.
3. Modificare `HandleOscMessage` per delegare:
   ```csharp
   public void HandleOscMessage(string address, IReadOnlyList<object> args)
   {
       _oscCommandHandler?.Handle(address, args);
   }
   ```
4. Rimuovere lo switch gigante (ora vive in `OscCommandHandler`).

### VINCOLI (non toccare)
- **MAI** spostare logica UI (es. aggiornamento slider, status bar) dentro `OscCommandHandler` — deve restare in MainWindow. Il handler deve **solo** chiamare metodi dell'interfaccia
- **MAI** toccare lo ScrollEngine (vincolo sacro #10)
- Non cambiare `OscBridge.DispatchMessage` — continua a chiamare `MainWindow.HandleOscMessage`
- Questo è un refactor **SAFE medio** — va eseguito **solo dopo TASK-008**

### TEST
- `dotnet build -c Release` → 0 errori
- Test manuale OSC completo (tutti i 20+ comandi documentati in `ARCHITETTURA §9`):
  - play, pause, reset, speed, speed/increase, speed/decrease, font/size, font/increase, font/decrease, position, jump/top, jump/bottom, mirror, mirror/toggle, status/request
  - ndi/start, ndi/stop, ndi/toggle, ndi/resolution, ndi/framerate, ndi/sourcename
- Verificare identico comportamento a pre-refactor
- Verificare che `MainWindow.xaml.cs` sia ~200-400 righe più corto

---

## [TASK-010] REFACTOR: Estrarre `DocumentFileService`

**FILE:** `src/TeleprompterApp/MainWindow.xaml.cs` → `src/TeleprompterApp/Services/DocumentFileService.cs` (nuovo)

### PROBLEMA
MainWindow contiene logica I/O per aprire/salvare documenti (`.rstp`, `.rtf`, `.txt`, `.docx`, etc.) mescolata con logica UI. La parte I/O è **pura** e può essere estratta. Vedi `docs/BugFix_Refactor_Implementazioni §PIANO REFACTORING — 1.3`.

### SOLUZIONE
1. Creare `src/TeleprompterApp/Services/DocumentFileService.cs`:

```csharp
namespace TeleprompterApp.Services;

internal static class DocumentFileService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".rtf", ".srt", ".vtt", ".log", ".csv", ".json",
        ".xml", ".html", ".htm", ".yaml", ".yml", ".ini", ".cfg",
        ".bat", ".ps1", ".xaml", ".xamlpackage", ".rstp", ".docx", ".doc"
    };

    public static bool IsSupportedExtension(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        return SupportedExtensions.Contains(Path.GetExtension(path));
    }

    public static byte[] ReadBytes(string path) => File.ReadAllBytes(path);

    public static string ReadAllText(string path, Encoding? encoding = null)
        => encoding != null ? File.ReadAllText(path, encoding) : File.ReadAllText(path);

    public static void WriteText(string path, string content)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, content);
        File.Move(tmp, path, overwrite: true);
    }

    public static DocumentFormat DetectFormat(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".rstp" or ".xamlpackage" => DocumentFormat.XamlPackage,
            ".rtf" => DocumentFormat.Rtf,
            ".docx" or ".doc" => DocumentFormat.Word,
            _ => DocumentFormat.PlainText
        };
    }
}

internal enum DocumentFormat { PlainText, Rtf, XamlPackage, Word }
```

2. In `MainWindow`, sostituire:
   - `_supportedExtensions` (HashSet locale) con `DocumentFileService.IsSupportedExtension`
   - Le letture `File.ReadAllBytes`/`File.ReadAllText` con i metodi del servizio
   - Le scritture con `DocumentFileService.WriteText` (pattern .tmp atomico)

3. **NON spostare** la logica di applicazione del contenuto al `RichTextBox` (questa resta in MainWindow perché richiede UI thread e accesso a `FlowDocument`).

### VINCOLI (non toccare)
- La conversione bytes→FlowDocument resta in MainWindow
- Non spostare il drag&drop (dipende da MainWindow.OnDrop)
- Non spostare la gestione `SaveFileDialog`/`OpenFileDialog` (UI)
- Scrittura **atomica** obbligatoria (vincolo sacro #6)

### TEST
- `dotnet build -c Release` → 0 errori
- Test manuale: aprire file .txt, .rtf, .rstp, .docx — tutti devono caricarsi come prima
- Test salvataggio: Ctrl+S su documento modificato, verificare che il file sia scritto atomicamente
- Test drag&drop: trascinare file supportato → deve aprirsi; file non supportato → deve essere rifiutato

---

## [TASK-011] BUG FIX: `DisplayManager.GetDisplayNumber` — collisione digit extraction

**FILE:** `src/TeleprompterApp/Services/DisplayManager.cs`

### SINTOMO
Su PC con più di 9 display (caso raro ma reale in configurazioni broadcast multi-wall) o con device name anomali (es. `\\.\DISPLAY10` vs `\\.\DISPLAY1`), il metodo `GetDisplayNumber` può assegnare lo **stesso numero** a due schermi diversi, causando:
- Duplicati nel pannello ToggleButton monitor
- Salvataggio errato del `PreferredDisplayNumber`
- Spostamento Presenter su display sbagliato dopo hotplug

### CAUSA ROOT
```csharp
private static int GetDisplayNumber(WF.Screen screen)
{
    var digits = new string(screen.DeviceName.Where(char.IsDigit).ToArray());
    return int.TryParse(digits, out var number) ? number : 0;
}
```

Questo concatena **tutte** le cifre del `DeviceName`. Se `DeviceName` è `\\.\DISPLAY10`, digits = `"10"` → numero 10. OK. Ma se fosse `\\.\DISPLAY1\Monitor0` (raro, ma esistono), diventa `"10"` → collide con `\\.\DISPLAY10`.

Inoltre, il numero estratto viene usato come **identificatore primario** nel salvataggio preferenze (`PreferredDisplayNumber`), rendendo la scelta del monitor fragile.

### FIX
1. Usare una regex che estrae solo il **primo gruppo di digit** dopo `DISPLAY`:

```csharp
private static int GetDisplayNumber(WF.Screen screen)
{
    if (string.IsNullOrEmpty(screen.DeviceName))
        return 0;

    // DeviceName format: \\.\DISPLAY1, \\.\DISPLAY2, ...
    var match = System.Text.RegularExpressions.Regex.Match(
        screen.DeviceName, @"DISPLAY(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    return match.Success && int.TryParse(match.Groups[1].Value, out var n) ? n : 0;
}
```

2. **Opzionale (consigliato):** aggiungere fallback basato sull'indice di `WF.Screen.AllScreens` per evitare zeri o duplicati:

```csharp
public IReadOnlyList<ScreenInfo> GetCurrentScreens()
{
    var all = WF.Screen.AllScreens;
    var result = new List<ScreenInfo>(all.Length);
    for (int i = 0; i < all.Length; i++)
    {
        var num = GetDisplayNumber(all[i]);
        if (num == 0) num = i + 1;  // fallback: indice 1-based
        result.Add(new ScreenInfo(all[i], num));
    }
    return result.OrderBy(s => s.DisplayNumber).ToList();
}
```

### VINCOLI (non toccare)
- Non cambiare la tripla ridondanza di rilevamento (Win32 hook + SystemEvents + polling)
- Non toccare `BuildFingerprint` (chiave di dedup per lo stato)
- Non cambiare `ScreensChanged` event signature
- Testare con 1, 2 e 3+ monitor fisici (il fallback indice-based è safety net)

### TEST
- `dotnet build -c Release` → 0 errori
- Test manuale con 1 monitor: funzionamento normale
- Test manuale con 2 monitor: numeri 1, 2 correttamente assegnati
- Test hot-plug: scollegare monitor 2 durante l'uso, ricollegarlo → stesso numero
- Test (se possibile con configurazione a 3+ monitor): verificare assenza di collisioni

---

# CHECKLIST PRE-ESECUZIONE

Prima di iniziare il batch, verificare:

- [ ] Repository su branch pulito (`git status` clean), preferibilmente `main` aggiornato
- [ ] Creato branch di lavoro: `git checkout -b task-batch-2026-04-14`
- [ ] `.NET 8 SDK` installato (`dotnet --version` → `8.x.x`)
- [ ] Build pulita di partenza: `dotnet build -c Release` → **0 errori, 0 warning nuovi**
- [ ] NewTek NDI Tools/Runtime installato (per test TASK-007)
- [ ] Bitfocus Companion 3.x disponibile per test TASK-002, TASK-003
- [ ] Almeno 1 monitor esterno fisico per test TASK-011 (o simulatore VirtualDisplay)
- [ ] Client OSC (Protokol, TouchOSC, OSC Monitor) per test TASK-005, TASK-006, TASK-009

# CHECKLIST POST-ESECUZIONE (per OGNI task completato)

- [ ] `dotnet build -c Release` → 0 errori
- [ ] `.\clean-and-build.ps1` → `release/Live_Speaker_Teleprompter_Portable.exe` e `_Setup.exe` generati
- [ ] **Scroll:** avviare l'exe di release, caricare un documento, testare scroll fluido a velocità 0.5, 5, 20, -5. Nessun micro-stutter percepibile.
- [ ] **Fine/inizio testo:** verificare stop automatico a 100% (Play) e a 0% (velocità negativa)
- [ ] **OSC:** testare `/teleprompter/play`, `/speed 5.0`, `/mirror/toggle`, `/ndi/start` con client OSC
- [ ] **Companion HTTP:** `curl http://localhost:3131/teleprompter/play`, `curl http://localhost:3131/teleprompter/status` — risposta JSON valida
- [ ] **NDI:** avviare streaming, verificare in OBS (o NDI Studio Monitor) che "Live Speaker NDI" appaia e scorra correttamente
- [ ] **Hot-plug monitor:** collegare/scollegare monitor esterno → Presenter si sposta correttamente
- [ ] **Lingua IT/EN:** switch in-app via ComboBox toolbar → tutti i messaggi cambiano lingua
- [ ] **Preferenze:** chiudere e riaprire l'app → preferenze ripristinate correttamente

# COMMIT STRATEGY

- **Commit 1:** TASK-001 (hotfix critico) — `fix: never shutdown on dispatcher exception (live safety)`
- **Commit 2:** TASK-002 + TASK-003 (Companion contract + i18n) — `fix(companion): align speed range to 0.5/80 and localize HTTP messages`
- **Commit 3:** TASK-004 (fallback sync) — `fix(presenter-sync): add XamlWriter fallback path`
- **Commit 4:** TASK-005 + TASK-006 (OSC robustness) — `refactor(osc): throttle continuous feedback and sanitize dispatch`
- **Commit 5:** TASK-007 (NDI visibility) — `feat(ndi): surface frame errors and auto-stop on fatal loop`
- **Commit 6:** TASK-008 + TASK-009 (extract OSC) — `refactor(main-window): extract OscTypeParser and OscCommandHandler`
- **Commit 7:** TASK-010 (extract DocumentFileService) — `refactor(main-window): extract DocumentFileService`
- **Commit 8:** TASK-011 (display manager hardening) — `fix(display-manager): regex-based DISPLAY number extraction`

Dopo l'ultimo commit, aggiornare `docs/BugFix_Refactor_Implementazioni_Live_Speaker_Teleprompter.md` con un nuovo blocco **CHANGELOG v2.3.6** che elenca i task completati.

---

# RIFERIMENTI DOCUMENTALI

| Documento | Quando consultarlo |
|---|---|
| `docs/ARCHITETTURA_Live_Speaker_Teleprompter.md` §§ 8–15 | Vincoli sacri, scroll engine, PresenterSync, DisplayManager |
| `docs/BugFix_Refactor_Implementazioni_Live_Speaker_Teleprompter.md` § PIANO REFACTORING | Classificazione SAFE/MEDIO/RISCHIOSO delle estrazioni |
| `docs/Istruzioni_Progetto_Claude_Live_Speaker_Teleprompter.md` § FORMATO TASK | Template `[TASK-XXX]` |
| `docs/Setup_Companion_Live_Speaker_Teleprompter.md` | Protocollo OSC e HTTP per test Companion |

**FINE DEL BATCH. Nessun task tocca lo ScrollEngine (vincolo sacro #10).**
