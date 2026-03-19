# BugFix, Refactor e Implementazioni — Live Speaker Teleprompter

> **Ultimo aggiornamento:** Marzo 2026 — v2.3.5  
> **Scopo:** Registro unico di bug risolti, changelog per versione, piano refactoring e guida aggiornamenti

---

## CHANGELOG PER VERSIONE

### v2.3.5 — Freccia allineata al testo, scroll fluido

| Intervento | Descrizione |
|---|---|
| **Freccia Y assoluta** | `SetArrowAbsoluteY(top)` — posizione in pixel dal top; stessa riga in preview e program |
| **Font sync** | `SyncPresenterDocument()` dopo `ApplyFont()` — font visibile subito sul presenter |
| **Scroll vsync** | `CompositionTarget.Rendering` — sincronizzato con refresh monitor |
| **CanContentScroll=False** | Scroll fisico (pixel) — movimento continuo, non a scatti |
| **Dead zone 0.05 px** | Prima 1.0 px: a velocità basse si saltavano frame |
| **Tick snellito** | `SyncPresenterScroll` e `UpdateScrollProgressDisplay` solo in `ScrollChanged` |

### v2.3.4 — Preset layout, Play senza loop, Sync presenter completo

| Intervento | Descrizione |
|---|---|
| **Preset S1–S4 / L1–L4** | 4 slot Save e 4 Load; layout completo in `layout-presets.json` |
| **Play senza loop** | Scroll fino al 100% e stop; nessun ritorno all'inizio |
| **Freccia stabile** | Non si sposta con preset/play/sync; solo trascinamento manuale |
| **Sync presenter completo** | `SyncPresenterAppearance()` — sfondo, colore/dimensione/posizione freccia |
| **Font identico** | Preview e Program: dimensione e stile identici via `SetFontFromDocument` |

**File aggiunti:** `LayoutPreset.cs`, `Services/LayoutPresetService.cs`

### v2.3.3 — Tasto On-Air, Localizzazione IT/EN

| Intervento | Descrizione |
|---|---|
| **On-Air toggle** | Sostituito tasto Modifica con On-Air (rosso quando attivo) |
| **Localizzazione** | `Localization.cs` — dizionari IT/EN ~125 chiavi, ComboBox lingua in toolbar |
| **Installer bilingue** | Scelta lingua IT/EN al primo schermo, UI installer localizzata |
| **Portable unico** | Un solo EXE con switch lingua in-app |

**File aggiunti:** `Localization.cs`

### v2.3.2 — Schermo esterno ultra stabile

| Intervento | Descrizione |
|---|---|
| **PageWidth nel clone** | Copia esplicita per layout identico preview/program |
| **DpiChanged** | `HwndSource.DpiChanged` riapplica bounds su cambio DPI/monitor |
| **Sync prima di Show** | `SyncPresenterDocument` prima di `ShowOnScreen` |

### v2.3.1 — Qualità testo

| Intervento | Descrizione |
|---|---|
| **Qualità caratteri** | TextFormattingMode=Ideal, TextRenderingMode=ClearType, rimosso BitmapCache |
| **Spazio** | Solo primo keypress (`e.IsRepeat`), nessun toggle ripetuto |

### v2.3.0 — Header a due righe, margini estesi

| Intervento | Descrizione |
|---|---|
| **Riga 1** | File, Formattazione, Velocità, Play/Pausa, Schermi, NDI, On-Air, Specchio, TopMost |
| **Riga 2** | Margini L/D/A/B (0–400 px), barra dimensione freccia, preset S1–S4 / L1–L4 |
| **Velocità estesa** | Range -80 … +80 |
| **Navigazione** | Home, End, Page Up, Page Down sempre attivi |

### v2.2.0 — Fluidità e performance

| Intervento | Descrizione |
|---|---|
| **Scroll vsync** | `CompositionTarget.Rendering` al posto di `DispatcherTimer` |
| **SpellCheck off** | 50–200ms latenza per keystroke a 72pt → eliminata |
| **TextFormattingMode=Display** | Rendering testo ottimizzato |
| **Debounce** | PresenterSync 300ms, Preferenze 500ms |

---

## BUG RISOLTI (storico)

| # | Bug | Stato |
|---|---|---|
| 1 | File import verticale + non editabile | RISOLTO |
| 2 | Freccia non allineata tra preview e monitor | RISOLTO |
| 3 | Colori sfondo/testo non funzionano | RISOLTO |
| 4 | Mouse scroll velocità non reattivo | RISOLTO |
| 5 | Manca tasto per tornare all'inizio | RISOLTO |
| 6 | Impossibile impostare margini esterni | RISOLTO |
| 7 | Preview e Program non corrispondono | RISOLTO |
| N1 | Brush non frozen (memory leak) | RISOLTO |
| N2 | CornerRadius visibili su fullscreen | RISOLTO |
| N3 | SpeedSlider SmallChange incoerente | RISOLTO |
| N4 | IsHitTestVisible bloccava interazione | RISOLTO |

---

## PIANO REFACTORING MAINWINDOW

> **Obiettivo:** Ridurre MainWindow (~3.400 righe) estraendo servizi in modo sicuro.
> **Principio:** Stabilità live = priorità n.1. Ogni modifica reversibile e testabile.

### Fase 1 — Estrazioni SAFE (rischio basso)

**1.1 OscTypeParser** (~5 min, rischio nullo)
- Estrarre `TryGetDouble`, `TryGetInt`, `TryGetBool`, `TryGetString` in classe statica
- Funzioni pure senza side-effect

**1.2 OscCommandHandler** (~30-45 min, rischio basso)
- Creare interfaccia `ITeleprompterController` con i metodi usati da OSC
- `OscCommandHandler` riceve `(address, args)` e chiama l'interfaccia
- MainWindow implementa `ITeleprompterController`

**1.3 DocumentFileService** (~20-30 min, rischio basso)
- Solo I/O: `ReadBytes`, `WriteText`, `IsSupportedExtension`, `DetectFormat`
- L'applicazione del contenuto all'editor resta in MainWindow

### Fase 2 — Estrazioni MEDIE (dopo Fase 1 verificata)

**2.1 FormatController** (~45-60 min, rischio medio)
- `ApplyFont`, `ApplyBackgroundColor`, `SetDocumentForeground`, logica colori/font
- Riceve `RichTextBox`, `Action syncToPresenter`, `Action savePreferences`

**2.2 ArrowController** (~60-90 min, rischio medio-alto)
- Posizionamento, scala, colore, drag. Sync con PresenterWindow
- Event handler restano in MainWindow, delegano a `_arrowController`

### Fase 3 — NON ESTRARRE (rischio alto)

**ScrollEngine** — eseguito ogni frame (60 Hz). Qualsiasi modifica può introdurre micro-stutter. Solo dopo test automatici che verificano il comportamento dello scroll.

### Ordine consigliato

1. OscTypeParser → 2. OscCommandHandler → 3. DocumentFileService → **commit**
4. FormatController → 5. ArrowController → **commit**
6. ScrollEngine → **non toccare senza test automatici**

---

## GUIDA AGGIORNAMENTI DIPENDENZE

### Principi

- **Stabilità prioritaria:** per eventi live, evitare aggiornamenti major senza test
- **Aggiornamenti safe:** patch e minor generalmente sicuri
- **Pre-commit:** build completo + test manuale dopo ogni aggiornamento

### Comandi

```powershell
# Aggiornamento .NET
dotnet restore
dotnet build -c Release

# Companion module
cd companion-module
npm update

# Build release completa
.\clean-and-build.ps1
```

### Stack attuale

| Componente | Versione | Note |
|---|---|---|
| .NET | 8 | WPF, C# 12 |
| Companion module | API v2 | Node.js |
| NDI | P/Invoke | ProcessNDI4.dll opzionale |

---

## CHECKLIST DI VERIFICA

- [ ] Header su due righe, menu utilizzabili
- [ ] Preset S1–S4 Save, L1–L4 Load
- [ ] Play si ferma al 100% (nessun loop)
- [ ] Freccia allineata: stessa riga in preview e program
- [ ] Preview e Program: font e aspetto identici
- [ ] Ctrl+S salva documento
- [ ] Play/Pausa, Spazio funziona solo in On-Air
- [ ] Velocità -80 … +80
- [ ] Margini L/D/A/B fino a 400 px
- [ ] Home, End, Page Up, Page Down
- [ ] Scorrimento fluido (vsync)
- [ ] NDI, OSC, Companion
- [ ] Hot-plug monitor
- [ ] Lingua IT/EN switch in-app
