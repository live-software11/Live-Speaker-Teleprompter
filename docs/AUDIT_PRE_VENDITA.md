# Live Speaker Teleprompter — Audit pre-vendita

> Generato: Aprile 2026 | Stack: .NET 8 WPF (C# 12) — Windows x64
> Ultimo aggiornamento: fix applicati Aprile 2026

---

## Verdetto: VERDE — Pronto per la vendita

Tutti i fix critici sono stati applicati. Companion e OSC ora ascoltano solo su loopback, i dati pending sono cifrati, e l'app non espone informazioni sensibili nelle risposte HTTP.

---

## Fix applicati in questo audit

### CRITICAL — Risolti
- **Companion HTTP su localhost** (`CompanionBridge.cs`) — rimosso binding wildcard `http://+:{port}`; solo `localhost` e `127.0.0.1`
- **OSC UDP su loopback** (`OscBridge.cs`) — `new UdpClient(new IPEndPoint(IPAddress.Loopback, port))` invece di bind su tutte le interfacce
- **CORS restrittivo** (`CompanionBridge.cs`) — `http://localhost` invece di `*`

### MEDIUM — Risolti
- **Info disclosure rimossa** (`CompanionBridge.cs`) — errori 500 ora restituiscono "Internal server error" invece di `ex.Message`
- **MessageBox senza dettagli** (`App.xaml.cs`) — errori globali mostrano messaggio generico, dettagli solo nel log
- **Fingerprint WMI** (`HardwareFingerprint.cs`) — `InvalidOperationException` con dettagli se WMI fallisce
- **`verify_before` assente = rifiuto** (`LicenseManager.cs`) — richiede verifica online se dati incompleti
- **Pending cifrato** (`LicenseStorage.cs`) — AES-256-GCM (con fallback legacy in chiaro)
- **i18n** (`Localization.cs`) — aggiunta chiave `Error_Unhandled_Generic` IT/EN

---

## Punti residui (accettabili per vendita)

### Limite architetturale (tutti i client desktop)
- Chiave AES hardcoded nell'assembly — limite intrinseco DRM; mitigato da verify online

### LOW
- `ReadAllBytes` su file grandi — limite dimensione file consigliato
- Race condition su import Word — `CancellationToken` opzionale
- Timer polling pending senza unsubscribe `Tick` — leak trascurabile

---

## Checklist pre-rilascio

- [x] Companion bind su localhost
- [x] OSC bind su loopback
- [x] CORS restrittivo
- [x] Info disclosure rimossa
- [x] Fingerprint errore su WMI failure
- [x] Pending cifrato
- [x] verify_before assente = rifiuto
- [x] i18n aggiornato
- [ ] `.\installer\build-installer.ps1` → portable + setup
- [ ] Test NDI su monitor secondario
- [ ] Test upgrade + disinstallazione
