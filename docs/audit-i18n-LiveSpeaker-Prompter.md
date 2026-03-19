# Audit i18n — Live Speaker Teleprompter

> **Data:** 19 Marzo 2026
> **Fase:** 1 — AUDIT (nessuna modifica effettuata)
> **Prossimo step:** Revisione (FASE 2) dopo OK utente

---

## 1. STRINGHE HARDCODED

Testo presente nel codice ma non in `Localization.cs`:

| File | Riga | Testo | Contesto |
|------|------|-------|----------|
| `Services/DisplayManager.cs` | 181 | `"Display "` | Prefisso hardcoded in DisplayLabel quando DisplayNumber > 0 |
| `MainWindow.xaml` | 8 | `Title="Live Speaker Teleprompter"` | Titolo finestra principale — brand, invariato in IT/EN |

**Nota:** Il titolo MainWindow è il nome prodotto. Coerente con Live Speaker Timer (app.title invariato). Accettabile lasciare hardcoded.

**Azione DisplayManager:** Aggiungere chiave `Display_Number` IT="Schermo {0}" EN="Display {0}" e usare `Localization.Get("Display_Number", DisplayNumber)` al posto di `$"Display {DisplayNumber}"`.

---

## 2. COPERTURA i18n

**Verifica chiavi It vs En:** ✅ **Simmetrica**

Tutte le 124 chiavi presenti in `It` hanno corrispondente in `En` e viceversa. Nessuna chiave mancante.

---

## 3. CHIAVI IN ITALIANO

**Nessuna.** Tutte le chiavi usano nomi in inglese (`Status_Ready`, `Tooltip_Open`, `Btn_Play`, ecc.).

---

## 4. TRADUZIONI SOSPETTE (EN identico a IT)

Chiavi dove il valore EN è identico al valore IT:

| Chiave | Valore | Note |
|--------|--------|------|
| `Btn_OnAir` | ON AIR | Termine broadcast invariato — OK |
| `Btn_Reset` | Reset | Termine tecnico — OK |
| `Btn_Arrow` | Arrow / Freccia | Diversi — OK |
| `Label_SaveShortcut` | Ctrl+S | Scorciatoia invariata — OK |
| `Title_Presenter` | Presenter | Termine invariato — OK |

**Nessuna copia IT→EN non tradotta.**

---

## 5. INCONSISTENZE DI TONO

- **IT:** Formale, impersonale, professionale
- **EN:** Stesso tono
- **Coerenza:** ✅ Buona. Terminologia teleprompter/broadcast professionale.

---

## 6. ELEMENTI AGGIUNTIVI

### ApplyLocalization

`MainWindow.xaml.cs` → `ApplyLocalization()` aggiorna tutti i controlli (tooltip, label, pulsanti) dopo `SwitchLanguage()`. Copertura completa verificata.

### PresenterWindow

Il titolo Presenter viene impostato da `Localization.Get("Title_Presenter")` in ApplyLocalization. ✅

### CompanionBridge, OscBridge, NDITransmitter

Nessuna stringa UI hardcoded. Tutti i messaggi di status usano `Localization.Get()`.

---

## RIEPILOGO AZIONI CONSIGLIATE

| Priorità | Azione |
|----------|--------|
| **Alta** | DisplayManager.cs: aggiungere `Display_Number` e sostituire `"Display "` con `Localization.Get("Display_Number", DisplayNumber)` |
| **Bassa** | MainWindow.xaml Title: lasciare "Live Speaker Teleprompter" (brand) oppure aggiungere `Title_MainWindow` se si vuole titolo tradotto |

---

## PROSSIMO STEP

**FASE 2 — REVISIONE:** Generare `docs/revisione-i18n-LiveSpeaker-Prompter.md` con proposte di correzione dettagliate.

**⏸ Attendere OK utente prima di procedere.**
