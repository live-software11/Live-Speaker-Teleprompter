# Istruzioni Traduzione i18n — Live Speaker Teleprompter

> **Ultimo aggiornamento:** 19 Marzo 2026
> **Progetto:** Live Speaker Teleprompter (Desktop .NET 8 WPF)
> **Lingua default app:** Inglese al primo avvio. Salvataggio scelta alla chiusura.
> **Stato workflow:** ✅ Completato 19/03/2026 (audit, revisione, applicazione)

---

## Contesto tecnico

| Campo | Valore |
|-------|--------|
| **Tecnologia i18n** | Custom: `Localization.cs` (Dictionary<string, string> statico, It e En) |
| **File traduzioni** | `src/TeleprompterApp/Localization.cs` |
| **Uso nel codice** | `Localization.Get("chiave")` o `Localization.Get("chiave", arg0, arg1)` |
| **Interpolazione** | `{0}`, `{1}` (string.Format standard C#) |
| **Chiavi** | ~125, flat con prefisso categoria (Status_, Tooltip_, Btn_, Label_, Error_, Filter_, Title_, Display_) |
| **Persistenza lingua** | `preferences.json` → campo `CultureName` ("it" o "en") |
| **Toggle lingua** | ComboBox IT/EN nella toolbar → `Localization.SwitchLanguage()` → `ApplyLocalization()` |
| **Default** | `DefaultCulture = "en"` — primo avvio in inglese |
| **Portable vs Installed** | Portable: preferences nella cartella exe. Installed: `%APPDATA%\Live Speaker Teleprompter` |

---

## Dominio terminologico

Terminologia professionale per:
- **Teleprompter:** script, scrolling, scroll speed, cue, on-air, mirror, presenter view
- **Display:** external display, primary, screen, monitor, presenter window
- **Formattazione:** font, font size, bold, italic, underline, margins, arrow
- **Controllo remoto:** Companion, OSC, NDI, play, pause, speed
- **File:** script, document, import, export, drag & drop, rich text

---

## Regole specifiche

1. **Chiavi con prefisso:** `Status_Ready`, `Tooltip_Open`, `Btn_Play`, `Label_Margins`. Mantenere la convenzione.
2. **Placeholder C#:** `{0}`, `{1}` — non cambiare l'ordine tra IT e EN se possibile.
3. **Filtri file dialog:** Tradurre le etichette ma non le estensioni (`.rstp`, `.rtf`, `.txt`).
4. **Companion/OSC messages:** Status bar — devono essere concisi.
5. **NDI messages:** Specifici per trasmissione broadcast.
6. **ApplyLocalization():** Dopo ogni SwitchLanguage, tutti i controlli WPF vengono aggiornati. Verificare che nessun tooltip/label sia dimenticato.

---

## Workflow

```bash
git checkout -b revisione-i18n-LiveSpeaker-Prompter
```

### FASE 1 — AUDIT

```
Analizza tutti i file del progetto Live Speaker Teleprompter.

File i18n: src/TeleprompterApp/Localization.cs (dizionari It e En)
Uso: Localization.Get("chiave") con placeholder {0}

Genera un report strutturato con:

1. STRINGHE HARDCODED
   → file e riga dove il testo è nel codice C# e non in Localization.cs
   → Cerca in: MainWindow.xaml.cs, MainWindow.xaml, PresenterWindow.xaml.cs,
     App.xaml.cs, CompanionBridge.cs, OscBridge.cs, NDITransmitter.cs,
     Services/DisplayManager.cs, Services/*.cs

2. COPERTURA i18n
   → chiavi presenti in It ma mancanti in En
   → chiavi presenti in En ma mancanti in It

3. CHIAVI IN ITALIANO
   → chiavi con nome in italiano (es. "Stato_Pronto" invece di "Status_Ready")

4. TRADUZIONI SOSPETTE
   → EN identico all'IT
   → traduzioni letterali di termini tecnici teleprompter/broadcast

5. INCONSISTENZE DI TONO

Salva come docs/audit-i18n-LiveSpeaker-Prompter.md
Non correggere — aspetta OK.
```

### FASE 2 — REVISIONE

```
Leggi docs/audit-i18n-LiveSpeaker-Prompter.md

Step 1: leggi Localization.cs dal disco (dizionari It e En)
Step 2: verifica termini teleprompter/broadcast
        (Wikipedia EN "Teleprompter", manuali Autoscript, glossari broadcast)
Step 3: documenta errori con chiave, IT attuale, EN attuale, problema, proposta
Step 4: salva come docs/revisione-i18n-LiveSpeaker-Prompter.md
        Aspetta OK.
```

### FASE 3 — APPLICAZIONE

```
Leggi docs/revisione-i18n-LiveSpeaker-Prompter.md e applica.

Step 1: modifica Localization.cs (entrambi i dizionari It e En)
Step 2: se chiavi rinominate → docs/mappa-chiavi-LiveSpeaker-Prompter.md
Step 3: dotnet build per verificare
```

### FASE 4 — REFACTOR (solo se chiavi rinominate)

```
Leggi docs/mappa-chiavi-LiveSpeaker-Prompter.md
Sostituisci Localization.Get("vecchia") in tutto il codice C#.
Cerca in: MainWindow.xaml.cs, PresenterWindow.xaml.cs, CompanionBridge.cs, OscBridge.cs
dotnet build e verifica.
```

---

## Checklist finale

- [x] Tutte le chiavi It hanno corrispondente En (e viceversa)
- [x] Nessuna stringa hardcoded nel codice C#/XAML
- [x] Terminologia EN professionale teleprompter/broadcast
- [x] Build pulita (`dotnet build`)
- [x] Primo avvio in inglese (DefaultCulture = "en")
- [x] Lingua salvata in preferences.json alla chiusura

**Nota:** Workflow completato 19/03/2026. Documenti: `audit-i18n-LiveSpeaker-Prompter.md`, `revisione-i18n-LiveSpeaker-Prompter.md`.
