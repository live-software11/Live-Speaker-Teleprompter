# R-Speaker Teleprompter - Analisi Completa Bug e Miglioramenti

## Stato: v2.3.0 — Header a due righe, margini estesi, navigazione rapida

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
- [ ] Play/Pausa visibile, Spazio funziona solo in modalità presentazione
- [ ] Velocità -80 … +80, scorrimento rapido
- [ ] Margini L, D, A, B fino a 400 px
- [ ] Tasto L=D imposta margini uguali
- [ ] Barra dimensione freccia in header
- [ ] Freccia si sposta solo con trascinamento
- [ ] Home, End, Page Up, Page Down per navigazione
- [ ] Scorrimento fluido (vsync)
- [ ] Import Word in background
- [ ] NDI, OSC, Companion

---

*Documento aggiornato il 2026-02-20 — R-Speaker Teleprompter v2.3.0*
