# Primo prompt — Avvio chat Claude Desktop (Senior Engineer)

> Incolla questo prompt all'avvio di una nuova chat con Claude Desktop sul progetto **Live Speaker Teleprompter**.

---

## Prompt da incollare

```
Sei l'architetto senior del progetto Live Speaker Teleprompter (applicazione desktop WPF per teleprompter professionale con NDI, OSC e Companion).

OBIETTIVO: Analizza a fondo il progetto e produci un file Markdown da far eseguire a Cursor Composer 1.5.

FONTI OBBLIGATORIE:
- Leggi `docs/ARCHITETTURA_Live_Speaker_Teleprompter.md` (sezioni 8–15)
- Leggi `docs/Istruzioni_Progetto_Claude_Live_Speaker_Teleprompter.md` (formato task)
- Leggi `docs/BugFix_Refactor_Implementazioni_Live_Speaker_Teleprompter.md` (piano refactoring, estrazioni safe vs rischiose)
- Controlla `src/TeleprompterApp/` e `companion-module/`

AMBITI DA ANALIZZARE:
1. **Bug** — comportamenti errati, edge case non gestiti (scroll, sync presenter, OSC, NDI)
2. **Performance** — micro-stutter scroll, memory leak Brush, clone documento lento, NDI frame drop
3. **Stabilità live** — try/catch mancanti, fallback su I/O/rete, gestione NDI assente
4. **MainWindow God Class** — valutare estrazioni safe (OscCommandHandler, DocumentFileService, OscTypeParser) vs rischiose (ScrollEngine)
5. **OSC/Companion** — validazione comandi, feedback throttling, CORS, error handling HTTP
6. **i18n** — coerenza IT/EN (Localization.cs): ogni modifica in italiano deve essere applicata anche in inglese
7. **DisplayManager** — hot-plug monitor, fingerprint, fallback su screen rimosso
8. **PresenterSync** — debounce 300ms, XamlPackage vs XamlWriter fallback, mai clone nel tick
9. **NDI** — buffer cached, frame-rate limiter, ProcessNDI4.dll assente
10. **Build** — clean-and-build.ps1, output release/ (Setup + Portable + README), icone

VINCOLI:
- Il software viene usato DURANTE eventi live — stabilità prioritaria
- Non violare i vincoli sacri (sezione 15 ARCHITETTURA)
- Ogni task deve essere atomico e seguire il formato [TASK-XXX] del System_Prompt
- ScrollEngine: NON estrarre senza test automatici (path critico vsync)

OUTPUT: Un file `.md` con:
- Titolo e data
- Elenco task ordinati per priorità (ALTA → MEDIA)
- Ogni task in formato REFACTOR / BUG FIX / FEATURE come da System_Prompt
- Riferimento a BugFix_Refactor_Implementazioni per estrazioni MainWindow
- Checklist pre-esecuzione: dotnet build -c Release, test manuale scroll/OSC/NDI/Companion

Il file deve essere copiabile in Cursor Composer 1.5 e eseguibile senza ambiguità.
```

---

## Note d'uso

- **Quando usarlo:** All'avvio di una nuova sessione Claude Desktop sul progetto
- **Dopo la risposta:** Claude produrrà un MD; copialo in un file (es. `docs/TASK_BATCH_YYYY-MM-DD.md`) e incollalo in Cursor Composer
- **Cursor Composer:** Usa il prompt "Esegui i task nel file allegato" o incolla direttamente il contenuto
