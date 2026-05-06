# Documentazione — Live Speaker Teleprompter

> **Indice canonico** della documentazione tecnica e di prodotto.
> **Ultima revisione:** 6 maggio 2026.

---

## Ripresa veloce

1. **[`../AGENTS.md`](../AGENTS.md)** — Entry-point standard 2026 per Cursor / Codex / Continue. **Prima fonte** per agenti automatici.
2. **[`../CLAUDE.md`](../CLAUDE.md)** — Sintesi viva per Claude Desktop / Code (gemello di `AGENTS.md`).
3. **[`ARCHITETTURA_Live_Speaker_Teleprompter.md`](./ARCHITETTURA_Live_Speaker_Teleprompter.md)** — Architettura completa: stack, services, OSC, Companion HTTP, NDI, build, licensing. **Single source of truth tecnico.**
4. **[`../README.md`](../README.md)** — README repository (overview).

---

## Documentazione tecnica

| File | Contenuto | Quando aggiornarlo |
| --- | --- | --- |
| [`ARCHITETTURA_Live_Speaker_Teleprompter.md`](./ARCHITETTURA_Live_Speaker_Teleprompter.md) | Stack, struttura repo, services (DisplayManager, PresenterSync, CompanionBridge, OscBridge, NDITransmitter), data model, build dual-binary, licensing, principi invarianti | Ogni modifica strutturale |
| [`BugFix_Refactor_Implementazioni_Live_Speaker_Teleprompter.md`](./BugFix_Refactor_Implementazioni_Live_Speaker_Teleprompter.md) | Tracciamento bug, refactor, implementazioni future | Ogni bug fix / refactor / nuova feature |
| [`GUIDA_INTEGRAZIONE_LICENZA_APP.md`](./GUIDA_INTEGRAZIONE_LICENZA_APP.md) | Integrazione client API LiveWorks (endpoint, fingerprint, T-04 HMAC) | Ogni cambio contratto API |
| [`Setup_Companion_Live_Speaker_Teleprompter.md`](./Setup_Companion_Live_Speaker_Teleprompter.md) | Setup Bitfocus Companion (azioni, feedback, variabili) | Cambi rotte HTTP / azioni Companion |

## Audit e sign-off

| File | Tipo | Esito |
| --- | --- | --- |
| [`AUDIT_PRE_VENDITA.md`](./AUDIT_PRE_VENDITA.md) | Audit pre-vendita (security, stabilità) | **VERDE — pronto per la vendita** (Aprile 2026) |
| [`TASK_BATCH_2026-04-14.md`](./TASK_BATCH_2026-04-14.md) | Batch composer task (storico, integrazione licenze) | Eseguito 14/04/2026 |

## Documentazione utente finale

| File | Lingua |
| --- | --- |
| [`README_ITA_Live_Speaker_Teleprompter.md`](./README_ITA_Live_Speaker_Teleprompter.md) | Italiano — guida utente per portable + installer (copia in `release/`) |
| [`README_ENG_Live_Speaker_Teleprompter.md`](./README_ENG_Live_Speaker_Teleprompter.md) | English — user guide (copy in `release/`) |

## AI assistant

| File | Destinatario | Ruolo |
| --- | --- | --- |
| [`../AGENTS.md`](../AGENTS.md) | Cursor / Codex / Continue | **Entry-point standard 2026** (prima fonte) |
| [`../CLAUDE.md`](../CLAUDE.md) | Claude Desktop / Code | Sintesi viva (gemello AGENTS) |
| [`Istruzioni_Progetto_Claude_Live_Speaker_Teleprompter.md`](./Istruzioni_Progetto_Claude_Live_Speaker_Teleprompter.md) | Claude Desktop (architetto) | System prompt — **legacy**, ancora valido per chat profonde |
| [`Primo_Prompt_Avvio_Chat_Claude_Desktop_Live_Speaker_Teleprompter.md`](./Primo_Prompt_Avvio_Chat_Claude_Desktop_Live_Speaker_Teleprompter.md) | Claude Desktop (architetto) | Primo prompt avvio chat — **legacy** |

> **Precedenza:** `AGENTS.md` > `ARCHITETTURA_Live_Speaker_Teleprompter.md` > `CLAUDE.md` > legacy.

## Configurazione AI / MCP

- **Cursor rules modulari** in [`../.cursor/rules/`](../.cursor/rules/) — 12 file `.mdc` (alwaysApply / globs):
  - `project-architecture.mdc`, `wpf-xaml-standards.mdc`, `csharp-codebehind.mdc`
  - `build-and-release.mdc`, `installer-modern.mdc`, `windows-installer.mdc` *(non più presente in repo Teleprompter — vedi cartella)*
  - `presenter-sync.mdc`, `preferences-persistence.mdc`, `performance-stability.mdc`
  - `i18n-installer.mdc`
  - `ecosystem-context.mdc`, `doc-sync.mdc`
  - `github-account-live-software11.mdc`
- **Claude Code config** in [`../.claude/`](../.claude/).

---

## Storia overhaul docs

| Data | Cosa |
| --- | --- |
| **6 maggio 2026** | Audit completo: creati `AGENTS.md` + `CLAUDE.md` + questo indice. Aggiornata ARCHITETTURA a v2.3.5 (riferimenti AGENTS/CLAUDE/docs/README; nessuna modifica strutturale al codice). Cursor rules `ecosystem-context`, `doc-sync` aggiornate. |
| 24/04/2026 | T-04 LiveWorks App Challenge HMAC: `AssemblyMetadata` `LiveWorksAppChallengeSecret`, header `X-App-*`. |
| Aprile 2026 | Audit pre-vendita chiuso (VERDE): Companion/OSC loopback, CORS restrittivo, info disclosure fix. Fix `ShutdownMode = OnExplicitShutdown` durante license gate. |
| Marzo 2026 | Versione 2.3.3 commerciale; merge feature/license-integration; installer IExpress upgrade. Documentazione consolidata in 4 docs core + setup Companion + README utente IT/EN. |

---

> **Regola d'oro:** la documentazione è parte del codice. Ogni modifica significativa al codebase richiede aggiornamento del file canonico corrispondente nello stesso commit (regola `.cursor/rules/doc-sync.mdc`).
