# Guida aggiornamenti — Live Speaker Teleprompter

> Documento di riferimento per aggiornare dipendenze .NET e Companion module in modo sicuro.  
> **Ultimo aggiornamento:** Marzo 2026

---

## 1. Principi generali

- **Stabilità prioritaria:** Per eventi live, evitare aggiornamenti major senza test approfonditi.
- **Aggiornamenti safe:** Patch e minor sono generalmente sicuri.
- **Pre-commit:** Dopo ogni aggiornamento eseguire build completo e test manuale.

---

## 2. Stack attuale

| Componente | Versione | Note |
|------------|----------|------|
| .NET | 8 | WPF, C# 12 |
| Companion module | API v2 | Node.js |
| NDI | P/Invoke | — |

---

## 3. Aggiornamenti .NET

```powershell
dotnet restore
dotnet build
```

Verificare compatibilità WPF e dipendenze NuGet.

---

## 4. Companion module

```bash
cd companion-module
npm update
```

Verificare compatibilità API v2 Bitfocus Companion.

---

## 5. Build release

```powershell
.\clean-and-build.ps1
```

Output in `release/` — verificare Portable.exe e Setup.exe.

---

*Aggiornare questo documento ogni volta che si modificano versioni stack o procedure.*
