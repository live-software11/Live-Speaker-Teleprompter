# AGENTS.md — Live Speaker Teleprompter

> **Entry-point unico per agenti AI** (Cursor, Claude Code, Codex, Continue, ecc.) su questo workspace.
> Compatibile con il formato standard `AGENTS.md` (2026). Letto a inizio di ogni sessione.
>
> **Ultimo aggiornamento:** 4 luglio 2026 (audit CTO performance/stabilità: hot-plug monitor, focus-steal, flicker presenter — ARCHITETTURA 2.3.6).

---

## 0. Cosa è questo workspace

- **Prodotto:** Live Speaker Teleprompter — applicazione **desktop** (.NET 8 WPF, C# 12, Windows x64) per teleprompter professionale multi-monitor con scroll vsync-aligned, OSC, NDI, Companion HTTP.
- **Funzionalità:** editor testo + presenter window full-screen su monitor secondario; scroll engine `CompositionTarget.Rendering` con delta-time compensation; mirror mode; freccia personalizzabile; preset layout S1-S4 / L1-L4; OSC remote control; Companion HTTP REST API; NDI broadcast (opzionale, P/Invoke su `ProcessNDI4.dll`).
- **Target:** Speaker, conduttori TV, podcast host, eventi live, broadcaster.
- **Tipo:** Distribuito in **due binari distinti** dal singolo codebase via `LicenseEnabled` MSBuild — *portable* `.exe` self-contained (~73 MB, no licenza, IT+EN selezionabile in-app) e *installer* IExpress (~73 MB, gate licenza Live WORKS APP).
- **Cartella app:** root del workspace. Solution `.sln` + `src/TeleprompterApp/` + `installer/` + `companion-module/` + `scripts/` + `docs/`.
- **Lingua agente ↔ utente:** SEMPRE italiano. Tono CTO che parla a un imprenditore (Andrea Rizzari).
- **Account ufficiali (mai incrociare):**
  - **GitHub:** `live-software11` · remote `https://github.com/live-software11/Live-Speaker-Teleprompter` · branch `master`
  - **Firebase:** **nessuno** per questa app (backend = Live WORKS APP esterno via HTTPS)
  - **Sistema licenze:** Live WORKS APP (`live-works-app.web.app/api`) · `productId = speaker-teleprompter` · AppData `com.livesoftware.live-speaker-teleprompter`

---

## 1. Mappa documentazione (dove guardare cosa)

| Vuoi sapere… | Apri questo file |
| --- | --- |
| **Sintesi viva per Claude Desktop/Code** | [`CLAUDE.md`](./CLAUDE.md) (root) |
| **Architettura completa: stack, services, OSC/Companion, build, licensing** | [`docs/ARCHITETTURA_Live_Speaker_Teleprompter.md`](./docs/ARCHITETTURA_Live_Speaker_Teleprompter.md) |
| **Indice canonico documenti** | [`docs/README.md`](./docs/README.md) |
| **Audit pre-vendita (verdetto VERDE — pronto)** | [`docs/AUDIT_PRE_VENDITA.md`](./docs/AUDIT_PRE_VENDITA.md) |
| **Bug, refactor, roadmap implementazioni** | [`docs/BugFix_Refactor_Implementazioni_Live_Speaker_Teleprompter.md`](./docs/BugFix_Refactor_Implementazioni_Live_Speaker_Teleprompter.md) |
| **Integrazione client API licenze LiveWorks** | [`docs/GUIDA_INTEGRAZIONE_LICENZA_APP.md`](./docs/GUIDA_INTEGRAZIONE_LICENZA_APP.md) |
| **Setup Bitfocus Companion** | [`docs/Setup_Companion_Live_Speaker_Teleprompter.md`](./docs/Setup_Companion_Live_Speaker_Teleprompter.md) |
| **Documentazione utente IT** | [`docs/README_ITA_Live_Speaker_Teleprompter.md`](./docs/README_ITA_Live_Speaker_Teleprompter.md) |
| **Documentazione utente EN** | [`docs/README_ENG_Live_Speaker_Teleprompter.md`](./docs/README_ENG_Live_Speaker_Teleprompter.md) |
| **System prompt Claude Desktop (architetto, legacy)** | [`docs/Istruzioni_Progetto_Claude_Live_Speaker_Teleprompter.md`](./docs/Istruzioni_Progetto_Claude_Live_Speaker_Teleprompter.md) |
| **Primo prompt avvio chat Claude (legacy)** | [`docs/Primo_Prompt_Avvio_Chat_Claude_Desktop_Live_Speaker_Teleprompter.md`](./docs/Primo_Prompt_Avvio_Chat_Claude_Desktop_Live_Speaker_Teleprompter.md) |
| **Regole Cursor modulari (12 file)** | [`.cursor/rules/*.mdc`](./.cursor/rules) |
| **Modulo licensing C#** | [`src/TeleprompterApp/Licensing/`](./src/TeleprompterApp/Licensing) (incluso solo con `-p:LicenseEnabled=true`) |
| **Modulo Bitfocus Companion (Node.js)** | [`companion-module/`](./companion-module) |

> **Prima di scrivere codice nuovo:** apri sempre `docs/ARCHITETTURA_Live_Speaker_Teleprompter.md`. È il single source of truth tecnico.

---

## 2. Stack tecnologico (NON cambiare versioni senza approvazione)

### Framework

| Componente | Versione | Ruolo |
| --- | --- | --- |
| .NET | 8.0 (`net8.0-windows`) | Runtime |
| WPF + WinForms | inclusi | UI principale + dialog (`UseWPF=true` + `UseWindowsForms=true`) |
| C# | 12+ | `Nullable` enable, implicit usings |
| `System.Text.Json` | inclusa | Serializzazione preferenze, licenza |
| `System.Net.Http` | inclusa | HTTP licenze, OSC bundle parsing |
| `System.Management` | 8.0.0 | **Solo build setup** (`Condition='$(LicenseEnabled)'=='true'`) — WMI fingerprint |

### Configurazione build (`TeleprompterApp.csproj`)

```xml
<TargetFramework>net8.0-windows</TargetFramework>
<UseWPF>true</UseWPF>
<UseWindowsForms>true</UseWindowsForms>
<ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>

<!-- Performance -->
<ServerGarbageCollection>true</ServerGarbageCollection>
<TieredCompilation>true</TieredCompilation>
<TieredPGO>true</TieredPGO>

<!-- Release: self-contained single-file -->
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<SelfContained>true</SelfContained>
<PublishSingleFile>true</PublishSingleFile>
<PublishReadyToRun>true</PublishReadyToRun>
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
```

### Dual-build via `LicenseEnabled`

| Flag MSBuild | Output | Caratteristiche |
| --- | --- | --- |
| `-p:LicenseEnabled=false` (default) | `Live_Speaker_Teleprompter_Portable.exe` | Cartella `Licensing/` esclusa; `System.Management` rimosso; nessun `LICENSE_ENABLED` symbol; ~73 MB |
| `-p:LicenseEnabled=true` | (poi wrap IExpress in) `Live_Speaker_Teleprompter_Setup.exe` | Cartella `Licensing/` inclusa; `System.Management` ref; `#if LICENSE_ENABLED` attivo; gate `LicenseGateWindow` all'avvio |

### T-04 HMAC App Challenge (csproj)

```xml
<!-- Iniezione secret a build-time per setup licenziato -->
<ItemGroup Condition="'$(LicenseEnabled)' == 'true' and '$(LiveWorksAppChallengeSecret)' != ''">
  <AssemblyAttribute Include="System.Reflection.AssemblyMetadataAttribute">
    <_Parameter1>LiveWorksAppChallengeSecret</_Parameter1>
    <_Parameter2>$(LiveWorksAppChallengeSecret)</_Parameter2>
  </AssemblyAttribute>
</ItemGroup>
```

A runtime, `LicenseApiClient` legge `AssemblyMetadata` `LiveWorksAppChallengeSecret` e (se ≥16 char) emette header HMAC-SHA256: `X-App-Id`, `X-App-Version`, `X-App-Challenge-Ts`, `X-App-Challenge` (payload = `productId|version|ts|fingerprint`). Backend match: `APP_CHALLENGE_SECRET_SPEAKER_TELEPROMPTER` su Cloud Functions WORKS APP.

### Integrazioni esterne

| Tecnologia | Implementazione | Note |
| --- | --- | --- |
| NDI | `NdiInterop.cs` (P/Invoke `ProcessNDI4.dll`) + `NDITransmitter.cs` | Opzionale: se DLL assente, toggle disabilitato |
| OSC | `Osc/OscPacket.cs` (parser puro) + `OscBridge.cs` UDP rx 8000 / tx 8001 — **bind `IPAddress.Loopback`** | |
| Companion HTTP | `CompanionBridge.cs` — `HttpListener` su `localhost:3131` (no wildcard `+`) | |
| WMI fingerprint | `HardwareFingerprint.cs` — SHA-256 su `MB_SERIAL\|CPU_ID\|DISK_SERIAL` | Solo build setup |

---

## 3. Le 5 invarianti SACRE (mai violare)

1. **Account GitHub immutabile.** `origin` punta a `live-software11/Live-Speaker-Teleprompter`. Prima di `git push`: `gh auth status` → account attivo deve essere **`live-software11`**. Mai pushare con `Andraven11`. Branch principale = **`master`**.

2. **Doppia build dal singolo codebase via MSBuild** (`-p:LicenseEnabled=true|false`). Il binario portable **NON deve contenere** codice Licensing né riferimento a `System.Management` (verificare che le `ItemGroup Condition` in csproj NON siano alterate). Pipeline standard: `clean-and-build.ps1` → `installer/build-installer.ps1` (dual publish + SHA-256 diff check + IExpress wrap del setup).

3. **Companion HTTP + OSC LOOPBACK ONLY.** `CompanionBridge.cs` HTTP usa `http://localhost:3131/` e `http://127.0.0.1:3131/` (mai wildcard `http://+:3131/`); CORS `Access-Control-Allow-Origin: http://localhost`. `OscBridge.cs` UDP usa `new UdpClient(new IPEndPoint(IPAddress.Loopback, port))`. Nessun bind su 0.0.0.0 / `IPAddress.Any`. Errori HTTP 500 mostrano "Internal server error" (mai `ex.Message` nel response).

4. **Sistema licenze come zona protetta.** `src/TeleprompterApp/Licensing/`:
   - `License.ApiBaseUrl = https://live-works-app.web.app/api` (immutabile salvo `license.liveworksapp.com` post DNS)
   - `License.ProductId = speaker-teleprompter`
   - `License.AesKey` 32 byte hardcoded — limite intrinseco DRM, mitigato da verify online + grace 30gg
   - `LicenseStorage`: AES-256-GCM su `%LOCALAPPDATA%\com.livesoftware.live-speaker-teleprompter\license.enc` (layout `nonce||cipher||tag`, **compatibile con il modulo Rust** di Ledwall/Timer)
   - `verify_before` assente = **rifiuto** (richiede verifica online)
   - `pending_activation.json` cifrato AES-GCM (con fallback legacy clear-text)
   - **T-04 HMAC** opzionale (vedi §2): secret in `AssemblyMetadata` `LiveWorksAppChallengeSecret` ↔ backend `APP_CHALLENGE_SECRET_SPEAKER_TELEPROMPTER`. Modifiche richiedono replica nel backend.
   - **CLI `--deactivate`**: invocato da uninstaller IExpress prima di cancellare la cartella (timeout 30s) → `LicenseManager.DeactivateAsync("uninstall")` → `Shutdown(0)`.
   - **`ShutdownMode = OnExplicitShutdown`** durante license gate (fix commit `7e36ca4`): evita exit dopo `ShowDialog()` se utente chiude la finestra senza valori.

5. **Stabilità live = priorità #1.** Mai crashare durante un evento. `try/catch` su ogni I/O, rete, rendering. Fallback silenzioso. Scroll **vsync-aligned** (`CompositionTarget.Rendering` con delta-time compensation, MAI `DispatcherTimer` per scroll, MAI `UpdateLayout()` nel tick). Tutti i servizi `IDisposable`; `Window_Closing` fa dispose esplicito di DisplayManager, PresenterSync, CompanionBridge, NDI, OSC. Tutti i `SolidColorBrush` dinamici vanno **freezati** con `.Freeze()`.

---

## 4. Invarianti tecniche derivate (sempre verificare)

- **MainWindow = unica fonte di verità.** `PresenterWindow` è clone read-only via `PresenterSyncService` (debounce 300ms).
- **Code-behind, no MVVM framework.** Logica nel code-behind con servizi separati (no Caliburn/Prism/MVVM Toolkit).
- **Preferenze debounced** (`DebouncedPreferencesService`, 500ms) → `PreferencesService.Save()` con scrittura atomica `.tmp` + `File.Move(overwrite: true)`. Mai scrivere su disco nel rendering tick.
- **Localizzazione via dizionario** (`Localization.cs`): no RESX, no binding XAML per stringhe. `Localization.Get(key)` ovunque. Aggiungere chiave in entrambi i dizionari `It` **e** `En`. `ApplyLocalization()` per nuovi controlli WPF. Setup IExpress **usa lingua di sistema** (Get-UICulture); portable ha selezione in-app.
- **Percorsi via `AppPaths.cs`:**
  - Portable (exe fuori da `%LocalAppData%` / `Program Files`): directory dell'exe (USB-friendly, no traccia sul host).
  - Installato: `%APPDATA%\Live Speaker Teleprompter`.
  - `PreferencesPath`, `LogDirectory`, `LayoutPresetService.PresetsPath` derivati da `BaseDirectory`.
- **Layout preset (S1-S4/L1-L4):** salva colori, font, dim, B/I/U, velocità, mirror, freccia, margini. NON salva: lingua, file, monitor, topmost, modalità modifica. File `layout-presets.json` in `BaseDirectory`.
- **NDI opzionale:** se `ProcessNDI4.dll` non c'è → toggle disabilitato. Mai crash. `NDIlib_send_create_t.clock_video = false` (pacing già gestito dal rate-limiter interno; `true` bloccherebbe il thread UI).
- **Hot-plug monitor:** `DisplayManager` coalizza gli eventi push con debounce 300ms + re-check di assestamento 1.5s (mai reagire diretto a `WM_DISPLAYCHANGE`/`SystemEvents`). L'intento dell'operatore (schermo scelto, presenter nascosto per scelta) è distinto dallo stato transitorio dei toggle e sopravvive agli hot-plug. `PresenterWindow`: `ShowActivated="False"`, mai `Activate()`, nessun `Owner`, `ShowOnScreen` no-op se già a schermo intero sullo stesso device.
- **`AssemblyName = TeleprompterApp`** non rinominare (XAML pack URIs).
- **DPI awareness:** `ApplicationHighDpiMode = PerMonitorV2`, manifest `dpiAware: true/PM`.

---

## 5. Comportamento autonomo vs. conferma

### L'agente procede AUTONOMAMENTE

- Bug fix isolati, refactor di un singolo file, aggiunta UI minore
- Aggiunta/aggiornamento stringhe i18n in `Localization.cs` (sempre IT+EN)
- Fix lint C# (warning compiler), test (anche se non c'è suite formale)
- Aggiornamento docs (ARCHITETTURA, BugFix datato, README utente)
- Aggiornamento Cursor rules dopo modifica strutturale
- Aggiunta colori XAML (con `.Freeze()`)

### L'agente si FERMA e chiede conferma (formato 3 righe: Cosa / Rischio / Beneficio)

- Modifiche a `src/TeleprompterApp/Licensing/` (zona protetta — gemello WORKS APP backend)
- Modifiche a `License.ApiBaseUrl`, `License.ProductId`, `License.AesKey`, AppData identifier
- Modifiche al binding di `CompanionBridge` o `OscBridge` (loopback non negoziabile)
- Modifiche a `csproj` `ItemGroup Condition` su `LicenseEnabled`
- Modifiche a `App.xaml.cs` `ShutdownMode` o sequenza license gate
- Modifiche a NDI P/Invoke (`NdiInterop.cs`) — DLL esterna critica
- Cambio versione major .NET / aggiornamento `System.Management`
- Refactor che tocca >10 file
- Operazioni distruttive

### MAI fare

- Push su repo diverso da `live-software11/Live-Speaker-Teleprompter`
- Bind HTTP/OSC su wildcard `+` o `0.0.0.0`
- CORS `*` su Companion HTTP
- Restituire `ex.Message` in HTTP response (info disclosure)
- Rinominare `AssemblyName` (rompe XAML pack URIs)
- Includere `release/*.exe`, `bin/`, `obj/` nel commit
- Bypassare `gh auth switch --user live-software11` prima di push
- Modificare il binario portable per includere codice Licensing

---

## 6. Checklist obbligatoria per nuove feature / funzioni

- [ ] **Stack invariato?** `csproj` non modificato senza approvazione (TargetFramework, dependencies, `LicenseEnabled` conditions)?
- [ ] **i18n?** Nuove stringhe in `Localization.cs` dizionari `It` e `En`, terminologia broadcast?
- [ ] **Code-behind WPF:** `Localization.Get(key)` + `ApplyLocalization()` chiamato per nuovi controlli?
- [ ] **Loopback?** Se tocchi rete/HTTP/OSC, bind locale preservato?
- [ ] **CORS?** `http://localhost` non `*`?
- [ ] **No info disclosure?** Errori HTTP 500 generici, dettagli solo in log?
- [ ] **Build dual?** `dotnet publish -c Release -p:LicenseEnabled=false` e `=true` compilano entrambi?
- [ ] **Portable senza Licensing?** `Live_Speaker_Teleprompter_Portable.exe` non contiene `System.Management.dll` (verificare con `Get-FileHash` differenza vs Setup)?
- [ ] **Licensing?** Se tocchi `Licensing/`, rispetti contratto API e impatto su WORKS APP backend?
- [ ] **T-04?** Se modifichi `LicenseApiClient`, lettura `AssemblyMetadata` resta opzionale (no break per build interne)?
- [ ] **Dispose?** Nuovi servizi implementano `IDisposable` e sono dispose-ati in `Window_Closing`?
- [ ] **Brush freeze?** `SolidColorBrush` dinamici hanno `.Freeze()`?
- [ ] **Scroll engine?** Se modifichi `MainWindow.cs` scroll, vsync-aligned (`CompositionTarget.Rendering`) preservato?
- [ ] **Scrittura atomica?** Nuove scritture file usano pattern `.tmp` + `File.Move(overwrite: true)`?
- [ ] **Docs?** Aggiornato `docs/ARCHITETTURA_Live_Speaker_Teleprompter.md` (versione + sezioni rilevanti) e `BugFix`?
- [ ] **Cursor rules?** Se cambia stack/workflow, aggiornato `.cursor/rules/*.mdc` rilevante?
- [ ] **WORKS APP backend?** Se cambia contratto API o webhook, replica in repo `live-works-app`?

---

## 7. Comandi essenziali

```powershell
# DEV
dotnet restore
dotnet build src/TeleprompterApp/TeleprompterApp.csproj
dotnet run --project src/TeleprompterApp/TeleprompterApp.csproj

# QUALITY
dotnet build -c Release                          # warning come errori (se configurato)

# BUILD PORTABLE
dotnet publish src/TeleprompterApp/TeleprompterApp.csproj -c Release -p:LicenseEnabled=false

# BUILD SETUP (con licenza)
dotnet publish src/TeleprompterApp/TeleprompterApp.csproj -c Release -p:LicenseEnabled=true `
  -p:LiveWorksAppChallengeSecret=<≥16char>     # T-04 secret opzionale

# RELEASE COMPLETA (orchestrato)
.\clean-and-build.ps1                           # pulisce + restore + dual publish + IExpress wrap
.\clean-and-build.bat                           # wrapper doppio clic

# INSTALLER
.\installer\build-installer.ps1                 # dual publish + SHA-256 diff + IExpress wrap

# GIT (account live-software11)
gh auth status
gh auth switch --user live-software11
git remote -v                                    # github.com/live-software11/Live-Speaker-Teleprompter
git push origin master
```

**PowerShell:** usa `;` al posto di `&&`.
**T-04 secret:** `$env:LiveWorksAppChallengeSecret = "..."` prima di `clean-and-build.ps1` per iniettare HMAC nel binario setup.
**Output finale (`release/`):** `Live_Speaker_Teleprompter_Portable.exe` + `Live_Speaker_Teleprompter_Setup.exe` + `README_ITA_*.md` + `README_ENG_*.md` (4 file).

---

## 8. Ecosistema (per orientamento)

| Progetto | Stack | Account / Repo | productId licenze |
| --- | --- | --- | --- |
| **Live Speaker Teleprompter** (qui) | C# WPF .NET 8 | `live-software11/Live-Speaker-Teleprompter` | `speaker-teleprompter` |
| Live Speaker Timer | Tauri 2 + Rust + React + Axum | `live-software11/Live-Speaker-Timer` | `speaker-timer` |
| Live 3d Ledwall Render | Tauri 2 + Rust + React + Three.js | `live-software11/Live-3d-Ledwall-Render` | `ledwall-render` |
| Live Video Composer | Python + Tkinter | `live-software11/Live-Video-Composer` | `video-composer` |
| **Live WORKS APP** (backend licenze) | React 19 + Firebase Functions Node 22 (Blaze) | `live-software11/live-works-app` | sorgente di verità |
| Live PLAN / CREW | Web SaaS Firebase Blaze | `live-software11/...` | `live-plan` / `live-crew` |
| Live SLIDE CENTER | React + Tauri 2 + Supabase | `live-software11/...` | (HMAC bidirezionale) |
| SITO www.liveworksapp.com | React 19 + Vite 8 + Tailwind 4 (Vercel) | `live-software11/liveworks-site` | nessuno (marketing) |
| Preventivi DHS / Gestionale FREELANCE | React + Firebase Spark | `Andraven11/...` | nessuno |

**Mai incrociare account.** Per repo `Andraven11/*` usa altro workspace + `gh auth switch --user Andraven11`.

**Compatibilità AES-GCM:** il file `license.enc` di Teleprompter è leggibile/scrivibile dal modulo Rust `aes-gcm 0.10` di Ledwall/Timer (stesso layout `nonce||cipher||tag`, stesso `productId|fingerprint` payload). Mai cambiare layout senza allineamento ecosistema.

---

## 9. Storia documentale

- **4 luglio 2026 (audit corrente)** — Audit CTO completo performance/stabilità, focus su gestione monitor esterni e comportamento hot-plug (collega/scollega a runtime). Fix: presenter che riappariva da solo dopo hot-plug se nascosto dall'operatore; `Activate()` che rubava il focus alla finestra di controllo su ogni show; flicker Normal→Maximized quando cambiava *un altro* schermo; doppio re-home con doppia serializzazione documento; `Owner` rimosso da `PresenterWindow` (minimize accidentale non spegne più l'uscita live). Perf: coalescing eventi `DisplayManager` (debounce 300ms + settle 1.5s + resume standby), hot path `CapturePreferences` senza scansione documento, NDI `clock_video=false` (no blocking UI thread), skip serializzazione ridondante su cambio schermo con presenter già visibile. Aggiornato `docs/ARCHITETTURA_Live_Speaker_Teleprompter.md` a v2.3.6, `docs/BugFix_Refactor_*.md`, `.cursor/rules/performance-stability.mdc`, `.cursor/rules/project-architecture.mdc`. Nessuna modifica a licensing, Companion/OSC binding, csproj, ShutdownMode. Build Debug/Setup/Release verificate 0 errori 0 warning.
- **6 maggio 2026** — Creato `AGENTS.md` (questo file) e `CLAUDE.md` come entry-point standard 2026 per agenti AI; aggiunto indice `docs/README.md`. Aggiornato `docs/ARCHITETTURA_Live_Speaker_Teleprompter.md` a v2.3.5 con riferimenti AGENTS.md/CLAUDE.md/docs/README.md. Cursor rules `ecosystem-context`, `doc-sync` aggiornate per puntare a `AGENTS.md` come prima fonte. Nessuna modifica al codice.
- **24 aprile 2026** — T-04 LiveWorks App Challenge HMAC client (commit `19ecd96`): secret in `AssemblyMetadata` (`LiveWorksAppChallengeSecret`), header `X-App-*` opzionali su API. Allineato con backend WORKS APP T-04 chiuso 24/04/2026.
- **Aprile 2026** — Audit pre-vendita chiuso (commit `3b9c366`): Companion/OSC loopback, CORS restrittivo, info disclosure rimossa, fingerprint WMI strict, pending cifrato AES-GCM. Verdetto: **VERDE — pronto per la vendita** (vedi `docs/AUDIT_PRE_VENDITA.md`).
- **Aprile 2026** — Fix `ShutdownMode = OnExplicitShutdown` durante license gate (commit `7e36ca4`): evita exit dopo `ShowDialog()`.
- **Marzo 2026** — Versione 2.3.x (csproj 2.3.3): merge feature/license-integration, installer upgrade, docs.

---

**Regola d'oro per tutta la sessione:**

> Prima di scrivere codice nuovo, controlla se esiste un pattern simile nel codebase. Riusa, non duplicare. Per la zona licenze: ogni modifica al `LicenseApiClient` o al manager che cambia il contratto API verso `live-works-app.web.app` richiede replica nel backend WORKS APP (`live-software11/live-works-app`). Per Companion/OSC: il bind loopback è non negoziabile.

## Test, audit e debug a fondo

Test, audit, campo e debug vanno fatti **a fondo**, con l'obiettivo di trovare i problemi **prima** del cliente e di Andrea. Mai uno sguardo veloce: CI e unit verdi **non bastano**.

### Cosa coprire (quando applicabile)

- Percorso felice
- Write o chiamata fallita a metà (niente stato parziale nascosto né successo finto)
- Reload e sessione
- Import e azioni di massa (righe errate, duplicati, limiti di batch)
- Ruoli, permessi e rules
- Concorrenza e race (listener prima della promise, modifiche ravvicinate, due utenti)
- Dati limite (vuoti, null/undefined, date e Timestamp serializzati, caratteri speciali)
- Regressioni sui flussi che condividono file, hook o servizi
- Effetti esterni (calendari, email, Functions) **senza** effetti reali
- Coerenza finale tra UI e dati salvati

### Come lavorare

- Leggere il **diff** e i **chiamanti** dei file toccati; quando si scopre un bug simile, cercare gli altri punti con lo stesso schema.
- **Dati reali intoccabili:** test solo con fixture fittizie poi eliminate, o su emulatore; le azioni che potrebbero toccare dati reali non si eseguono sui dati veri.
- Ogni esito verde dichiara in chiaro **cosa è stato provato** e **cosa resta fuori**, con il motivo.
- UI e campo a **1920×1080**.
