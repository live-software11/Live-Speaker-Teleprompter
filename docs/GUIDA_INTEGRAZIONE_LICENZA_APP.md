# Guida integrazione modulo licenza — app desktop LiveWorks

**EN:** Desktop app integration guide for the LiveWorks license API (Firebase Hosting + Cloud Functions).

Documento **autonomo** da allegare o copiare nei repository delle singole app (Tauri, WPF, Python). Per il codice completo dei moduli, resta fonte di verità [`GUIDA_SISTEMA_LICENZE_LIVEWORKS.md`](./GUIDA_SISTEMA_LICENZE_LIVEWORKS.md) (sezioni 10–13).

---

## 1. Configurazione canonica

| Parametro | Valore |
|-----------|--------|
| **Sorgente backend/dashboard (Git)** | https://github.com/live-software11/live-works-app (privato; non necessario per l’integrazione client, solo riferimento). **EN:** *Backend/dashboard Git source:* same URL (private; optional reference for desktop apps). |
| **API base URL** | `https://live-works-app.web.app/api` |
| **Dominio alternativo** | `https://license.liveworksapp.com/api` (dopo DNS custom) |
| **Formato chiave** | `LIVE-XXXX-XXXX-XXXX-XXXX` (senza I, O, 0, 1) |
| **Verifica periodica consigliata** | ogni **14 giorni** (allineare a `settings/global` → `verificationIntervalDays`) |
| **Grace period offline** | **30 giorni** oltre `verifyBefore` / `nextVerifyDate` (`gracePeriodDays`) |
| **Binding** | **1 PC** per licenza (fingerprint hardware); cambio PC tramite API `deactivate` con `reason: pc_change` (limite annuale, vedi `maxPcChanges` su documento licenza) |

---

## 2. Endpoint REST

Tutte le richieste sono **HTTPS POST** (tranne health). **Non** usare Firestore SDK nelle app desktop per i dati licenza.

Se l’API risponde con errore di **configurazione server** (es. messaggio generico lato client dopo deploy), in produzione di solito manca o non è collegato **`LICENSE_TOKEN_SECRET`** sulle Cloud Functions — intervento lato Firebase (Secret Manager), non nell’app desktop. Vedi `docs/SECRETS_E_DEPLOY_LOCALE.md`.

### 2.1 `GET /api/health`

Controllo raggiungibilità (nessun body).

### 2.2 `POST /api/activate`

**Body JSON (camelCase):**

| Campo | Tipo | Obbligatorio |
|-------|------|--------------|
| `licenseKey` | string | sì |
| `hardwareFingerprint` | string (64 hex SHA-256) | sì |
| `hardwareDetails` | string | no (consigliato per supporto admin) |
| `productId` | string slug | sì |
| `appVersion` | string | sì |

**Product ID ufficiali:** `ledwall-render`, `speaker-teleprompter`, `speaker-timer`, `video-composer`, `live-plan`, `live-crew`

**Risposta 200 (attivazione riuscita):** `success: true`, `token`, `expiresAt`, `verifyBeforeDate`, `productIds`, `customerName`  
**Risposta 200 (in attesa approvazione admin):** `success: false`, `pendingApproval: true`, `error` (es. `"Activation pending admin approval"`), `productIds`, `customerName` — **nessun token**. L’app deve mostrare una schermata di attesa e **ripetere `POST /activate`** ogni 30–60 s (stessa richiesta non spamma l’admin). Dopo l’approvazione dalla dashboard, la stessa chiamata restituirà `success: true` e il token.  
**Risposta 400:** altri errori (`success: false`, senza `pendingApproval`)

### 2.3 `POST /api/verify`

| Campo | Obbligatorio |
|-------|--------------|
| `licenseKey` | sì |
| `hardwareFingerprint` | sì |
| `token` | sì, **tranne** stato **in attesa approvazione** (`pendingActivation` uguale al fingerprint richiesto): in quel caso il server accetta anche token assente/non valido e risponde `pendingApproval: true` |
| `productId` | sì |
| `appVersion` | sì |

**Risposta 200 (valido):** `valid: true`, `expiresAt`, `nextVerifyDate`, `newToken?`  
**Risposta 200 (in attesa approvazione):** se la licenza non ha ancora `hardwareFingerprint` ma esiste `pendingActivation` per **questo** `hardwareFingerprint` e `productId`, il server risponde `valid: false`, `pendingApproval: true` anche **senza token** (utile per polling durante l’attesa).  
**Altri casi:** `valid: false` e `error`; il token nel body deve corrispondere alla **stessa** coppia licenza + fingerprint (binding lato server).

### 2.4 `POST /api/deactivate`

| Campo | Obbligatorio |
|-------|--------------|
| `licenseKey` | sì |
| `hardwareFingerprint` | sì |
| `token` | sì |
| `reason` | `pc_change` \| `uninstall` |

- **`pc_change`:** azzera binding su questo PC (conteggio annuale e audit lato server).  
- **`uninstall`:** audit; non sblocca automaticamente la licenza su altro PC senza `pc_change`.

---

## 3. Fingerprint hardware (Windows)

**EN:** Composite fingerprint = SHA-256 of `BASEBOARD_SERIAL|CPU_ID|DISK0_SERIAL` (WMI). See guide §10 for WMI classes and fallbacks (`UNKNOWN_MB`, `NO_DISK`, ecc.).

```
FINGERPRINT = SHA256( MB_SERIAL + "|" + CPU_ID + "|" + DISK_SERIAL )
```

Inviare sempre anche `hardwareDetails` leggibile, es.: `MB:…|CPU:…|DISK:…` (salvato in dashboard per diagnosi).

**Evoluzione futura (opzionale):** TPM 2.0 come ancoraggio crittografico (migliore su macchine gestite); oggi lo stack LiveWorks è allineato a WMI come in guida.

---

## 4. Flusso consigliato nell’app

1. **Avvio:** caricare file licenza locale (cifrato).  
2. **Offline:** verificare fingerprint + `expiresAt` + grace su `verify_before` / `nextVerifyDate`.  
3. Se offline non valido o scaduta finestra di verifica: chiamare **`/verify`**; aggiornare token locale se presente `newToken`.  
4. Se non c’è licenza: schermata attivazione → **`/activate`**.  
5. **Disinstallazione / cambio PC:** chiamare **`/deactivate`** con token valido; poi su nuovo PC `activate` (se licenza libera o dopo `pc_change`).

### 4.1 Approvazione manuale (licenze con gate admin)

**EN:** Some licenses require the admin to approve the **first** PC binding. Until then, the app should not start the protected features.

- Dopo `POST /activate`, se ricevi **`pendingApproval: true`**, mostra messaggio chiaro (“In attesa di approvazione”) e continua a chiamare **`/activate`** (o **`/verify`** senza token, vedi §2.3) a intervalli regolari.  
- Quando l’admin approva, **`/activate`** con lo stesso fingerprint restituisce **`success: true`** e il token: salvalo e prosegui il flusso normale.  
- Le licenze da checkout Lemon Squeezy usano in genere **`requiresApproval: false`** (nessun gate).

---

## 5. Costanti da impostare per ogni progetto

| Costante | Esempio | Note |
|-----------|---------|------|
| `API_BASE_URL` | `https://live-works-app.web.app/api` | Allineare a [`CONFIG_CANONICA.md`](./CONFIG_CANONICA.md) |
| `PRODUCT_ID` | es. `speaker-timer` | Slug prodotto in Firestore |
| `APP_NAME` | nome cartella AppData | Univoco per app |
| `APP_VERSION` | da `Cargo.toml` / assembly / `pyproject` | Inviato alle API |

---

## 6. Stack e file di riferimento (guida completa)

### 6.1 Rust / Tauri 2

- **Fingerprint:** `src-tauri/src/license/fingerprint.rs`  
- **Manager HTTP + file locale:** `src-tauri/src/license/manager.rs`  
- **Comandi Tauri:** `src-tauri/src/license/commands.rs`  
- **UI gate:** `LicenseGate.tsx` (guida §11.6)  
- **CSP:** `connect-src` verso dominio Hosting (guida §11.7)

### 6.2 C# / .NET 8 WPF

- **Fingerprint:** `HardwareFingerprint.cs`  
- **HTTP + DPAPI:** `LicenseManager.cs`  
- **UI:** `ActivationWindow` (guida §12.4)  
- **Bootstrap:** `App.xaml.cs` (guida §12.5)

### 6.3 Python

- **Fingerprint:** `src/license/fingerprint.py`  
- **Manager:** `src/license/manager.py` (Fernet + `requests`)  
- Variabile ambiente **`LIVEWORKS_FERNET_KEY`** (mai in repo)

---

## 7. Sicurezza — checklist pre-rilascio

- [ ] Nessun segreto (`LICENSE_TOKEN_SECRET`, Fernet, webhook LS) committato nel repo app.  
- [ ] `API_BASE_URL` in produzione punta all’hosting corretto.  
- [ ] `PRODUCT_ID` coincide con documento `products/{id}` in Firestore.  
- [ ] File licenza locale **cifrato** (AES-GCM Rust, DPAPI .NET, Fernet Python).  
- [ ] Gestire errori di rete senza esporre stack trace all’utente finale.  
- [ ] Test manuale: activate → verify → deactivate (`uninstall` / `pc_change`) su VM reale.

---

## 8. Riferimenti

- Architettura e dashboard: [`README.md`](./README.md), [`CONFIG_CANONICA.md`](./CONFIG_CANONICA.md)  
- Implementazione estesa: [`GUIDA_SISTEMA_LICENZE_LIVEWORKS.md`](./GUIDA_SISTEMA_LICENZE_LIVEWORKS.md) §7 (API), §8 (webhook), §10–13 (client)

---

## 9. English summary

Use a single HTTPS API under `/api/*`. Bind each license to one machine via SHA-256(WMI components). Store the activation token securely; send it on every `verify` and `deactivate`. Refresh the token when `newToken` is returned. Implement offline checks for expiry and verification grace window, then revalidate online periodically. Per-stack code templates live in `GUIDA_SISTEMA_LICENZE_LIVEWORKS.md` sections 11–13.
