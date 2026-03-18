# Guida Refactoring MainWindow — Live Speaker Teleprompter

> **Obiettivo:** Ridurre la MainWindow God Class (3.400 righe) estraendo servizi in modo **sicuro**, senza rischi per il funzionamento live.

---

## Principio guida

**Stabilità live = priorità n.1.** Ogni modifica deve essere reversibile e testabile manualmente prima del deploy.

---

## Fase 0 — Prerequisiti (consigliato)

Prima di qualsiasi refactoring:

1. **Checkpoint Git** — `git commit` con messaggio chiaro (es. "Pre-refactoring MainWindow")
2. **Test manuale baseline** — Verifica: scroll fluido, OSC, Companion, NDI, preset, margini, freccia, drag & drop
3. **Build Release** — `dotnet build -c Release` deve dare 0 errori, 0 warning

---

## Fase 1 — Estrazioni SAFE (rischio basso)

Queste estrazioni non toccano path real-time né rendering. Si possono fare subito.

### 1.1 OscCommandHandler

**Cosa estrarre:** La logica di `HandleOscMessage` — parsing indirizzi OSC e dispatch comandi.

**Rischio:** Basso. Logica pura, nessun tick rendering.

**Passi:**

1. Creare interfaccia `ITeleprompterController` con i metodi usati da OSC:
   - `SetPlayState(bool)`, `SetSpeed(double, bool)`, `AdjustSpeed(double)`
   - `IsPlaying`, `CurrentScrollSpeed`, `IsEditMode`
   - `HandleOscMessage(string, List<object>)` — oppure i metodi granulari che già esistono
2. Creare classe `OscCommandHandler` che riceve `(address, args)` e chiama l'interfaccia
3. MainWindow implementa `ITeleprompterController`, passa `this` a OscCommandHandler
4. OscBridge passa i messaggi a OscCommandHandler invece che a MainWindow.HandleOscMessage

**Verifica post-estrazione:** Inviare comandi OSC da Companion o controller — play, stop, speed, font, NDI. Tutto deve funzionare identico.

---

### 1.2 DocumentFileService (solo I/O)

**Cosa estrarre:** Lettura/scrittura file — `File.ReadAllText`, `File.WriteAllText`, logica formati (txt, rtf, rstp).

**Rischio:** Basso. L'I/O è isolabile; l'applicazione del contenuto all'editor resta in MainWindow.

**Passi:**

1. Creare `DocumentFileService` con metodi:
   - `byte[] ReadBytes(string path)` o `string ReadText(string path)`
   - `void WriteText(string path, string content)`
   - `bool IsSupportedExtension(string ext)`
   - `string? DetectFormat(string path)` — per decidere come caricare
2. MainWindow chiama DocumentFileService per leggere/scrivere, poi applica il risultato a `_contentEditor`
3. Non spostare `LoadDocument`/`SaveDocument` interi — solo la parte file I/O

**Verifica:** Aprire .txt, .rtf, .rstp, salvare in vari formati. Drag & drop file.

---

### 1.3 Type parsing helpers → classe statica

**Cosa estrarre:** `TryGetDouble`, `TryGetInt`, `TryGetBool`, `TryGetString` (righe ~3239–3328).

**Rischio:** Nullo. Funzioni pure senza side-effect.

**Passi:**

1. Creare `OscTypeParser` (o `TypeConversionHelper`) statico
2. Spostare i 4 metodi
3. Sostituire chiamate `TryGetDouble(...)` con `OscTypeParser.TryGetDouble(...)`

**Verifica:** Build + test OSC con valori int, float, string, bool.

---

## Fase 2 — Estrazioni MEDIE (richiedono attenzione)

Solo dopo aver completato Fase 1 e verificato che tutto funziona.

### 2.1 FormatController

**Cosa estrarre:** `ApplyFont`, `ApplyBackgroundColor`, `SetDocumentForeground`, `ApplyToDocument`, logica colori/font.

**Rischio:** Medio. Molte chiamate a `_contentEditor`, `_presenterWindow`, `SavePreferences`.

**Passi:**

1. Creare `FormatController` che riceve nel costruttore:
   - `RichTextBox contentEditor`
   - `Action<FlowDocument>? syncToPresenter`
   - `Action savePreferences`
2. Spostare i metodi di formattazione
3. MainWindow delega i click (FontButton_Click, ForegroundButton_Click, ecc.) al FormatController

**Verifica:** Cambiare font, colore testo, sfondo. Verificare sync presenter e persistenza preferenze.

---

### 2.2 ArrowController

**Cosa estrarre:** `ApplyNormalizedArrowPosition`, `MoveArrowTo`, `UpdateArrowScale`, `UpdateArrowNormalizedFromCurrent`, `SetArrowColor`, logica drag.

**Rischio:** Medio-alto. Eventi MouseDown/Move/Up, sync con PresenterWindow.

**Passi:**

1. Creare `ArrowController` che riceve:
   - `Canvas arrowCanvas`, `Grid arrowContainer`, `Polygon arrowShape`, `ScaleTransform arrowScaleTransform`
   - `Action<double, double> syncToPresenter` (left, top)
   - `Action savePreferences`
2. Gli event handler (ArrowCanvas_MouseDown, ecc.) restano in MainWindow ma chiamano `_arrowController.OnMouseDown(...)`
3. Oppure: registrare gli eventi dentro ArrowController passando i riferimenti ai controlli

**Verifica:** Trascinare freccia, cambiare scala, colore. Verificare che la posizione sia corretta su presenter e dopo riavvio.

---

## Fase 3 — NON estrarre (rischio alto)

### 3.1 ScrollEngine

**Perché non estrarlo ora:** È sul path `CompositionTarget.Rendering` — viene eseguito ogni frame (~60 Hz). Qualsiasi modifica può introdurre:
- Micro-stutter
- Jitter
- Scroll che non si ferma correttamente a fine/inizio testo

**Quando considerarlo:** Solo dopo aver aggiunto test che verificano il comportamento dello scroll (es. test che simula tick e controlla `VerticalOffset`). Richiede infrastruttura di test non banale per WPF.

**Alternativa conservativa:** Aggiungere commenti di sezione (`#region`) per rendere il codice più navigabile, senza spostare logica.

---

## Checklist pre-commit (ogni fase)

- [ ] `dotnet build -c Release` — 0 errori, 0 warning
- [ ] Test manuale: scroll, play/pause, speed, OSC, Companion, NDI
- [ ] Test: caricare/salvare file, drag & drop
- [ ] Test: freccia, margini, preset layout
- [ ] Test: hot-plug monitor (se possibile)
- [ ] `git diff --stat` — verificare che le modifiche siano coerenti con il piano

---

## Ordine consigliato

1. **OscTypeParser** (5 min) — zero rischio
2. **OscCommandHandler** (30–45 min) — basso rischio
3. **DocumentFileService** (20–30 min) — basso rischio
4. Pausa, verifica completa, commit
5. **FormatController** (45–60 min) — medio rischio
6. **ArrowController** (60–90 min) — medio-alto rischio
7. Pausa, verifica completa, commit

**ScrollEngine:** Non toccare senza test automatici.

---

## Riferimenti

- `docs/Architettura_Live_Speaker_Teleprompter.md` — sezione 11 (Scroll engine), 19 (Principi)
- `.cursor/rules/project-architecture.mdc` — regole invarianti
- `.cursor/rules/performance-stability.mdc` — priorità stabilità live
