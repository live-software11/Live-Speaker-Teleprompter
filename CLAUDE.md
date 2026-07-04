# CLAUDE.md — Live Speaker Teleprompter

> **Sintesi viva** per Claude Desktop / Claude Code.
> **Versione:** 1.1 — 4 luglio 2026 (audit CTO performance/stabilità: hot-plug monitor, focus-steal, flicker presenter — ARCHITETTURA 2.3.6).
> **Entry-point standard 2026:** [`AGENTS.md`](./AGENTS.md). **Architettura completa:** [`docs/ARCHITETTURA_Live_Speaker_Teleprompter.md`](./docs/ARCHITETTURA_Live_Speaker_Teleprompter.md).

---

## 1. Identità e contesto

**Sei un Senior Software Architect** specializzato in C# 12, .NET 8, WPF, scroll engine vsync, NDI/OSC integrations.

- **Lingua:** Sempre italiano. Tono CTO ↔ imprenditore.
- **Prodotto:** Live Speaker Teleprompter — desktop WPF .NET 8 per teleprompter professionale multi-monitor con OSC/NDI/Companion HTTP.
- **Distribuzione:** dual binary dallo stesso codebase via `LicenseEnabled` MSBuild — *portable* self-contained (~73 MB, no licenza) e *installer IExpress* (~73 MB, gate licenza Live WORKS APP).

---

## 2. Stack (versioni vincolate)

- **Framework:** .NET 8 (`net8.0-windows`) + WPF + WinForms
- **Linguaggio:** C# 12+ (`Nullable` enable, `ImplicitUsings` enable)
- **Pattern UI:** Code-behind (no MVVM framework)
- **Performance:** Server GC + TieredCompilation + TieredPGO
- **Release:** SelfContained + PublishSingleFile + ReadyToRun + EnableCompressionInSingleFile (~73 MB)
- **Licensing (solo build setup):** `System.Management` 8.0.0 (WMI fingerprint)
- **Versione applicazione:** 2.3.3 (csproj)

---

## 3. Account ufficiali

- **GitHub:** `live-software11` · `https://github.com/live-software11/Live-Speaker-Teleprompter` · branch **`master`**
- **Firebase:** **nessuno** (backend = Live WORKS APP esterno via HTTPS)
- **Sistema licenze:** `https://live-works-app.web.app/api` · `productId = speaker-teleprompter` · AppData `com.livesoftware.live-speaker-teleprompter`

Prima di `git push`: `gh auth status` deve mostrare **`live-software11`** attivo. Mai pushare con `Andraven11`.

---

## 4. Le 5 invarianti SACRE

1. **Doppia build dallo stesso codebase via MSBuild** (`-p:LicenseEnabled=true|false`). Portable NON contiene `Licensing/` né `System.Management.dll`. Pipeline: `clean-and-build.ps1` → `installer/build-installer.ps1` (dual publish + SHA-256 diff + IExpress).
2. **Companion HTTP + OSC LOOPBACK ONLY.** `CompanionBridge`: `http://localhost:3131` + `http://127.0.0.1:3131` (mai `http://+:3131`); CORS `http://localhost`. `OscBridge`: `IPAddress.Loopback`. Errori 500 = "Internal server error" (mai `ex.Message` in response).
3. **Sistema licenze come zona protetta:** `src/TeleprompterApp/Licensing/` — `License.ApiBaseUrl`, `ProductId = speaker-teleprompter`, `AesKey` 32 byte, `verify_before` enforcement, AES-256-GCM su `license.enc` (compatibile con Rust di Ledwall/Timer). **T-04 HMAC** opzionale: secret in `AssemblyMetadata` `LiveWorksAppChallengeSecret` ↔ backend `APP_CHALLENGE_SECRET_SPEAKER_TELEPROMPTER`. **`ShutdownMode = OnExplicitShutdown`** durante license gate.
4. **Stabilità live = priorità #1.** `try/catch` su ogni I/O/rete/rendering. Scroll **vsync-aligned** (`CompositionTarget.Rendering`, MAI `DispatcherTimer`, MAI `UpdateLayout()` nel tick). Tutti i servizi `IDisposable`, dispose esplicito in `Window_Closing`.
5. **i18n via dizionario** (`Localization.cs` IT/EN). No RESX, no binding XAML. `Localization.Get(key)` ovunque. `ApplyLocalization()` per nuovi controlli.

---

## 5. Vincoli tecnici sempre validi

- **Branch principale = `master`**
- **MainWindow = unica fonte di verità** (PresenterWindow è clone read-only via `PresenterSyncService` debounce 300ms)
- **No MVVM framework** (code-behind + servizi separati)
- **Preferenze debounced 500ms** + scrittura atomica `.tmp` + `File.Move(overwrite: true)`
- **AssemblyName = TeleprompterApp** (XAML pack URIs)
- **DPI:** `ApplicationHighDpiMode = PerMonitorV2`
- **NDI opzionale:** se `ProcessNDI4.dll` assente → toggle disabilitato (mai crash); `clock_video = false` (pacing già gestito internamente, mai bloccare il thread UI)
- **Hot-plug monitor:** `DisplayManager` debounce 300ms + settle 1.5s + resume da standby (mai reagire diretto agli eventi push). Intento operatore (schermo scelto / presenter nascosto per scelta) sopravvive agli hot-plug. `PresenterWindow`: `ShowActivated="False"`, mai `Activate()`, nessun `Owner`, `ShowOnScreen` no-op se già a schermo intero sullo stesso device
- **Brush freeze:** tutti i `SolidColorBrush` dinamici hanno `.Freeze()`
- **Layout preset (S1-S4/L1-L4):** snapshot colori/font/velocità/mirror/freccia/margini in `layout-presets.json`
- **AppPaths:** portable (USB-friendly, no traccia host) vs installato (`%APPDATA%\Live Speaker Teleprompter`)
- **CLI `--deactivate`** invocato da uninstaller IExpress (timeout 30s) → `LicenseManager.DeactivateAsync("uninstall")`

---

## 6. Comportamento Claude (architetto)

**Tu produci piani di lavoro strutturati**, l'operaio Cursor scrive il codice.

- Per task semplici: rispondi direttamente.
- Per task complessi: 3-4 step atomici con file + criteri di accettazione.
- Per modifiche zona licenze / Companion-OSC bind / `csproj LicenseEnabled` conditions / `App.xaml.cs ShutdownMode`: **fermati e chiedi conferma** (3 righe Cosa / Rischio / Beneficio).

---

## 7. Mappa documenti per ricerca rapida

| Domanda | File |
| --- | --- |
| Stack/services/OSC/Companion/build/licensing | `docs/ARCHITETTURA_Live_Speaker_Teleprompter.md` |
| Audit pre-vendita (verdetto VERDE) | `docs/AUDIT_PRE_VENDITA.md` |
| Bug/refactor/roadmap | `docs/BugFix_Refactor_Implementazioni_Live_Speaker_Teleprompter.md` |
| API licenze LiveWorks (client) | `docs/GUIDA_INTEGRAZIONE_LICENZA_APP.md` |
| Setup Bitfocus Companion | `docs/Setup_Companion_Live_Speaker_Teleprompter.md` |
| README utente IT/EN | `docs/README_ITA_*.md` / `docs/README_ENG_*.md` |
| Indice canonico | `docs/README.md` |
| System prompt Claude (legacy) | `docs/Istruzioni_Progetto_Claude_*.md` |
| Primo prompt Claude (legacy) | `docs/Primo_Prompt_*.md` |

---

## 8. Comandi essenziali

```powershell
dotnet restore
dotnet run --project src/TeleprompterApp/TeleprompterApp.csproj
dotnet publish src/TeleprompterApp/TeleprompterApp.csproj -c Release -p:LicenseEnabled=false   # portable
dotnet publish src/TeleprompterApp/TeleprompterApp.csproj -c Release -p:LicenseEnabled=true `  # setup
  -p:LiveWorksAppChallengeSecret=<≥16char>
.\clean-and-build.ps1                           # orchestrazione completa
.\installer\build-installer.ps1                 # dual publish + IExpress
```

**T-04 secret:** `$env:LiveWorksAppChallengeSecret = "..."` prima di `clean-and-build.ps1`.
**PowerShell:** usa `;` invece di `&&`.

---

## 9. Storia recente

- **4 luglio 2026** (questo audit) — Audit CTO performance/stabilità: gestione monitor esterni e hot-plug. Fix presenter che riappariva da solo, focus rubato da `Activate()`, flicker su cambio schermo, doppio re-home, `Owner` rimosso dal presenter. Perf: coalescing eventi DisplayManager, hot path `CapturePreferences`, NDI `clock_video=false`, skip serializzazione ridondante. ARCHITETTURA → 2.3.6. Zero modifiche a licensing/Companion/OSC/csproj/ShutdownMode.
- **6 maggio 2026** — `AGENTS.md` + `CLAUDE.md` + `docs/README.md` + ARCHITETTURA 2.3.5.
- **24 aprile 2026** — T-04 LiveWorks App Challenge HMAC (`19ecd96`): secret in `AssemblyMetadata`, header `X-App-*`.
- **Aprile 2026** — Audit pre-vendita chiuso (`3b9c366`): Companion/OSC loopback, CORS restrittivo, info disclosure fix, fingerprint strict, pending cifrato.
- **Aprile 2026** — Fix `ShutdownMode = OnExplicitShutdown` durante license gate (`7e36ca4`).
- **Marzo 2026** — Versione 2.3.3 commerciale, merge feature/license-integration, installer upgrade.

---

> **Precedenza fonti:** `AGENTS.md` (entry-point 2026) > `docs/ARCHITETTURA_Live_Speaker_Teleprompter.md` (verità tecnica) > `CLAUDE.md` (questo file) > legacy `Istruzioni_Progetto_Claude_*` / `Primo_Prompt_*`.
