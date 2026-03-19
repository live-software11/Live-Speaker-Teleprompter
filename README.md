# Live Speaker Teleprompter

Teleprompter professionale per presentazioni multi-schermo con integrazione NDI, OSC e Bitfocus Companion.

> **Versione 2.3.5** — .NET 8 WPF — Windows 10/11 x64 — Italiano/English

---

## Release

Dopo `.\clean-and-build.ps1`, la cartella `release/` contiene:

| File | Descrizione |
|---|---|
| `Live_Speaker_Teleprompter_Setup.exe` | Installer con scelta lingua e cartella |
| `Live_Speaker_Teleprompter_Portable.exe` | Eseguibile portable (switch lingua in-app) |
| `README_ITA_Live_Speaker_Teleprompter.md` | Documentazione utente italiana |
| `README_ENG_Live_Speaker_Teleprompter.md` | Documentazione utente inglese |

## Documentazione

| Documento | Percorso |
|---|---|
| Architettura software | [docs/ARCHITETTURA_Live_Speaker_Teleprompter.md](docs/ARCHITETTURA_Live_Speaker_Teleprompter.md) |
| BugFix, Refactor, Changelog | [docs/BugFix_Refactor_Implementazioni_Live_Speaker_Teleprompter.md](docs/BugFix_Refactor_Implementazioni_Live_Speaker_Teleprompter.md) |
| Setup Companion | [docs/Setup_Companion_Live_Speaker_Teleprompter.md](docs/Setup_Companion_Live_Speaker_Teleprompter.md) |
| Documentazione utente ITA | [docs/README_ITA_Live_Speaker_Teleprompter.md](docs/README_ITA_Live_Speaker_Teleprompter.md) |
| Documentazione utente ENG | [docs/README_ENG_Live_Speaker_Teleprompter.md](docs/README_ENG_Live_Speaker_Teleprompter.md) |
| Claude Desktop — Istruzioni progetto | [docs/Istruzioni_Progetto_Claude_Live_Speaker_Teleprompter.md](docs/Istruzioni_Progetto_Claude_Live_Speaker_Teleprompter.md) |
| Claude Desktop — Primo prompt | [docs/Primo_Prompt_Avvio_Chat_Claude_Desktop_Live_Speaker_Teleprompter.md](docs/Primo_Prompt_Avvio_Chat_Claude_Desktop_Live_Speaker_Teleprompter.md) |

## Build da sorgente

```powershell
.\clean-and-build.ps1
```

Richiede [.NET 8 SDK](https://dotnet.microsoft.com/download). Output in `release/`.

## Licenza

MIT
