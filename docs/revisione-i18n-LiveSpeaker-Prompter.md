# Revisione i18n — Live Speaker Teleprompter

> **Data:** 19 Marzo 2026
> **Fase:** 2 — REVISIONE (modifiche approvate)
> **Riferimento:** `docs/audit-i18n-LiveSpeaker-Prompter.md`
> **Prossimo step:** Applicazione (FASE 3)

---

## 1. MODIFICHE APPROVATE

### 1.1 DisplayManager.cs — Stringa hardcoded "Display {n}"

| Campo | Valore |
|-------|--------|
| **File** | `src/TeleprompterApp/Services/DisplayManager.cs` |
| **Riga** | 181 |
| **Problema** | `$"Display {DisplayNumber}"` non localizzato |
| **Soluzione** | Nuova chiave `Display_Number` + `Localization.Get("Display_Number", DisplayNumber)` |

**Nuova chiave in Localization.cs:**

| Chiave | IT | EN |
|--------|----|----|
| `Display_Number` | Schermo {0} | Display {0} |

**Codice prima:**
```csharp
? $"Display {DisplayNumber}{(IsPrimary ? Localization.Get("Display_Primary") : string.Empty)}"
```

**Codice dopo:**
```csharp
? $"{Localization.Get("Display_Number", DisplayNumber)}{(IsPrimary ? Localization.Get("Display_Primary") : string.Empty)}"
```

---

### 1.2 MainWindow.xaml — Titolo finestra

| Campo | Valore |
|-------|--------|
| **File** | `MainWindow.xaml` |
| **Problema** | `Title="Live Speaker Teleprompter"` hardcoded |
| **Decisione** | **Nessuna modifica.** Coerente con Live Speaker Timer: titolo = nome prodotto/brand, invariato IT/EN. |

---

## 2. RIEPILOGO FILE DA MODIFICARE

| File | Azione |
|------|--------|
| `src/TeleprompterApp/Localization.cs` | Aggiungere `Display_Number` in It e En |
| `src/TeleprompterApp/Services/DisplayManager.cs` | Sostituire stringa hardcoded con `Localization.Get("Display_Number", DisplayNumber)` |

---

## 3. VERIFICA POST-APPLICAZIONE

- [ ] Nessuna stringa UI hardcoded in DisplayManager
- [ ] Chiave `Display_Number` presente in entrambi i dizionari
- [ ] Build progetto OK
- [ ] Test rapido: cambio lingua IT/EN → etichetta display corretta

---

**Stato:** ✅ Revisione completata — pronto per FASE 3 (Applicazione)
