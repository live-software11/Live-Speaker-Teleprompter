# Live Speaker Teleprompter

Applicazione desktop portable per teleprompter professionale: editor script, output full-screen su monitor esterno, streaming NDI, controllo OSC e integrazione Bitfocus Companion.

**Stack:** .NET 8 WPF · C# 12 · OSC (UDP) · HTTP REST · NDI (P/Invoke) · Companion module API v2

---

## Build

```powershell
.\clean-and-build.ps1
# oppure doppio clic su clean-and-build.bat
```

**Output:** `release/` — Portable.exe (IT+EN), Setup.exe (2 file, ~73 MB ciascuno)

---

## Struttura progetto (sviluppatori)

```
Live Speaker Teleprompter/
├── src/TeleprompterApp/   ← App WPF (MainWindow, PresenterWindow, servizi)
├── companion-module/      ← Modulo Bitfocus Companion (Node.js, API v2)
├── installer/             ← Script build, template installer
├── scripts/               ← convert-logo.ps1, PngToIco
├── icons/                 ← Logo sorgente + app-icon.ico (generato)
├── docs/                  ← Documentazione architettura
├── release/               ← Output build (gitignored)
└── .cursor/rules/         ← Regole AI
```

## Quick start

```powershell
.\clean-and-build.ps1
# Output in release/
```

## Output build

Dopo `clean-and-build.ps1` (2 file):

| Output | Percorso |
|--------|----------|
| **Portable** (IT+EN, selezione in-app) | `release/Live_Speaker_Teleprompter_Portable.exe` |
| **Setup installer** | `release/Live_Speaker_Teleprompter_Setup.exe` |

## Documentazione

- [ARCHITETTURA_DEFINITIVA](docs/ARCHITETTURA_DEFINITIVA_Live_Speaker_Teleprompter.md) — Stack, struttura, vincoli, checklist
- [Guida_Refactoring_MainWindow](docs/Guida_Refactoring_MainWindow.md) — Estrazioni safe vs rischiose
- [Setup_Companion](docs/Setup_Companion_Live_Speaker_Teleprompter.md) — Setup Bitfocus Companion
- [System_Prompt_Claude](docs/System_Prompt_Claude_Live_Speaker_Teleprompter.md) — Prompt per Claude Desktop (architetto)
- [Primo_Prompt_Avvio](docs/Primo_Prompt_Avvio_Chat_Claude_Desktop_Live_Speaker_Teleprompter.md) — Prompt avvio chat Claude
