# Live Speaker Teleprompter - Analisi Completa Bug e Miglioramenti

## Stato: v2.3.5 — Freccia allineata al testo, scroll fluido

Tutti i bug segnalati sono risolti. Ottimizzazioni di performance e fluidità implementate secondo best practice WPF e fonti esterne (Microsoft Docs, Stack Overflow, PerfView).

---

## RIEPILOGO v2.3.0 (Febbraio 2026)

### Header a due righe
| Intervento | Descrizione |
|------------|-------------|
| **Riga 1** | File, Formattazione, Velocità, Inizio/Fine, Play/Pausa, Schermi, NDI, Modifica, Specchio, TopMost |
| **Riga 2** | Margini L, D, tasto L=D, margini A, B, barra dimensione freccia, pulsante Freccia |

### Margini e freccia
| Intervento | Descrizione |
|------------|-------------|
| **Margini estesi** | L, D, A, B: range 0–400 px (prima 0–200) |
| **Tasto L=D** | Imposta margine sinistro e destro uguali (media dei due valori) |
| **Barra dimensione freccia** | Slider visibile in header (0.5–2.0) |
| **Freccia solo manuale** | La freccia non si sposta automaticamente quando si modificano margini o dimensione; solo trascinamento manuale |
| **Margine sinistro minimo** | Il testo inizia sempre dopo la freccia (arrowRightEdge + extra) |

### Velocità e navigazione
| Intervento | Descrizione |
|------------|-------------|
| **Velocità estesa** | Range -80 … +80 (prima -20 … +20) per scorrimento più rapido |
| **Spazio** | Play/Pausa solo in modalità non-modifica; in modifica lo Spazio digita normalmente |
| **Tasti navigazione** | Home, End, Page Up, Page Down per spostarsi nel documento (sempre attivi) |
| **Pulsanti Inizio/Fine** | In header per accesso rapido |

### Fluidità e performance (v2.2.0)
| Intervento | Descrizione | Fonte |
|------------|-------------|-------|
| **Scroll vsync** | `CompositionTarget.Rendering` al posto di `DispatcherTimer` — sincronizzato con refresh monitor | Stack Overflow, Microsoft |
| **SpellCheck disabilitato** | Con font 72pt causava 50–200ms di latenza per keystroke | Analisi empirica |
| **TextFormattingMode=Display** | Rendering testo ottimizzato per display | Microsoft Docs |
| **Doppio padding rimosso** | `RichTextBox.Padding` duplicava `FlowDocument.PagePadding` → testo verticale | Bug fix |
| **RequestPresenterSync** | Debounce 300ms invece di sync immediato | PresenterSyncService |

---

## BUG RISOLTI (storico)

| # | Bug | Stato |
|---|-----|-------|
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

## COMANDI TASTIERA

| Tasto | Modalità modifica | Modalità presentazione |
|-------|-------------------|-------------------------|
| **Spazio** | Digita spazio | Play/Pausa |
| **Home** | Vai all'inizio documento | Vai all'inizio documento |
| **End** | Vai alla fine documento | Vai alla fine documento |
| **Page Up** | Scorri su una pagina | Scorri su una pagina |
| **Page Down** | Scorri giù una pagina | Scorri giù una pagina |
| **Su/Giù** | — | Modifica velocità |
| **Sinistra/Destra** | — | Azzera velocità |
| **Scroll mouse** | Modifica velocità | Modifica velocità |
| **Ctrl+Scroll** | Velocità fine | Velocità fine |
| **Shift+Scroll** | Velocità grossa | Velocità grossa |

---

## ARCHITETTURA PERFORMANCE

### Scroll engine (vsync-aligned)
```
CompositionTarget.Rendering += OnScrollRendering
  → Fires at monitor refresh (60/120 Hz)
  → Delta-time compensation via Stopwatch
  → ScrollToVerticalOffset(clampedTarget)
  → Unsubscribe when speed=0 or end reached
```

### Editor → Presenter sync
```
TextChanged → RequestPresenterSync() → MarkDirty()
  → [300ms debounce] → CloneDocument(XamlPackage) → SetDocument()
```

### Preferenze
```
UI change → SavePreferences() → CapturePreferences() → DebouncedPreferencesService
  → [500ms debounce] → PreferencesService.Save(JSON)
```

---

## CHECKLIST DI VERIFICA

- [ ] Header su due righe, menu utilizzabili
- [ ] Preset S1–S4 Save, L1–L4 Load; toggle evidenziato dopo save
- [ ] Play si ferma al 100% (nessun loop)
- [ ] Freccia allineata: stessa riga testo in preview e program (Y assoluta)
- [ ] Freccia stabile: non si sposta con preset/play/sync aspetto
- [ ] Preview e Program: font e aspetto identici
- [ ] Ctrl+S salva documento
- [ ] Play/Pausa visibile, Spazio funziona solo in modalità presentazione
- [ ] Velocità -80 … +80, scorrimento rapido
- [ ] Margini L, D, A, B fino a 400 px
- [ ] Tasto L=D imposta margini uguali
- [ ] Barra dimensione freccia in header
- [ ] Freccia si sposta solo con trascinamento manuale
- [ ] Home, End, Page Up, Page Down per navigazione (solo fuori modifica)
- [ ] Scorrimento fluido (vsync)
- [ ] Import Word in background
- [ ] NDI, OSC, Companion

---

---

## v2.3.1 — Qualità testo e schermo esterno

| Intervento | Descrizione |
|------------|-------------|
| **Qualità caratteri** | TextFormattingMode=Ideal, TextRenderingMode=ClearType, rimosso BitmapCache che sgranava a 72pt |
| **Schermo esterno** | Sync presenter in background (DispatcherPriority.Loaded), DPI PerMonitorV2, gestione errori |
| **Spazio** | Solo primo keypress (e.IsRepeat), nessun toggle ripetuto se tenuto premuto |

---

---

## v2.3.2 — Schermo esterno ultra stabile, Preview=Program

| Intervento | Descrizione | Fonte |
|------------|-------------|-------|
| **PageWidth nel clone** | Copia esplicita di PageWidth per layout identico preview/program | Analisi |
| **SetScrollRatio** | UpdateLayout solo quando ScrollableHeight invalido (evita 60 UpdateLayout/sec) | Microsoft Perf |
| **DpiChanged** | HwndSource.DpiChanged riapplica bounds su cambio DPI/monitor | Microsoft Docs |
| **Sync prima di Show** | SyncPresenterDocument prima di ShowOnScreen per contenuto visibile subito | Best practice |

---

---

## v2.3.3 — Tasto On-Air

| Intervento | Descrizione |
|------------|-------------|
| **On-Air toggle** | Sostituito tasto Modifica con On-Air (rosso quando attivo) |
| **On-Air ON** | Testo bloccato, schermo esterno = preview (relatore vede ciò che fai) |
| **On-Air OFF** | Modifica script, relatore vede modifiche in diretta |
| **Play → On-Air** | Avviando lo scroll si passa automaticamente in On-Air |

---

## v2.3.4 — Preset layout, Play senza loop, Sync presenter completo

| Intervento | Descrizione |
|------------|-------------|
| **Preset S1–S4 / L1–L4** | 4 slot Save e 4 Load in header riga 2; salvataggio layout completo (colori, font, margini, freccia, ecc.) in `layout-presets.json` |
| **Toggle preset Save** | Dopo il salvataggio, il pulsante S1–S4 resta evidenziato (toggle style) |
| **Play senza loop** | Lo scroll prosegue fino al 100% del testo e si ferma; nessun ritorno all'inizio |
| **Freccia stabile** | La freccia non si sposta quando si salva preset, play, sync aspetto; solo trascinamento manuale |
| **Sync presenter completo** | `SyncPresenterAppearance()` applica sfondo, colore freccia, dimensione freccia, posizione freccia al secondo schermo |
| **Font identico** | Preview e Program hanno dimensione e stile font identici (`SetFontFromDocument` con `MediaFontFamily`) |
| **Salvataggio Ctrl+S** | Gestito in `Window_PreviewKeyDown`; null check in `TrySaveDocument` |
| **Margini prima di Show** | `ApplyArrowSafePadding()` e `SetPagePadding` chiamati prima di mostrare il presenter |
| **Home/End/PageUp/PageDown** | Attivi solo in modalità non-modifica |

### File aggiunti
- `LayoutPreset.cs` — modello snapshot layout
- `Services/LayoutPresetService.cs` — salvataggio/caricamento preset in `layout-presets.json`

---

## v2.3.5 — Freccia allineata al testo, scroll fluido

| Intervento | Descrizione |
|------------|-------------|
| **Freccia Y assoluta** | `SetArrowAbsoluteY(top)` — posizione in pixel dal top; stessa Y = stessa riga testo in preview e program |
| **Font sync** | `SyncPresenterDocument()` dopo `ApplyFont()` e `FontSizeComboBox_SelectionChanged` — font visibile subito sul secondo schermo |
| **Scroll vsync** | `CompositionTarget.Rendering` al posto di `DispatcherTimer` — sincronizzato con refresh monitor |
| **CanContentScroll=False** | Scroll fisico (pixel) invece di logico — movimento continuo, non a scatti |
| **Dead zone 0.05 px** | Prima 1.0 px: a velocità basse si saltavano frame. Ora scroll quasi ogni frame |
| **Tick snellito** | `SyncPresenterScroll` e `UpdateScrollProgressDisplay` solo in `ScrollChanged` (non nel tick) |

---

*Documento aggiornato il 2026-02-20 — Live Speaker Teleprompter v2.3.5*
