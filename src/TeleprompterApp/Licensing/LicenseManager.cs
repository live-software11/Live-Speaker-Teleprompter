using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TeleprompterApp.Licensing;

/// <summary>
/// Orchestratore del sistema licenze per Live Speaker Teleprompter.
/// Mirror funzionale di <c>src-tauri/src/license/manager.rs</c> (Ledwall).
///
/// Flow:
///   <list type="number">
///     <item>GetStatus() — check locale senza rete (fingerprint + file + grace period).</item>
///     <item>ActivateAsync(key) — prima attivazione via POST /activate.</item>
///     <item>VerifyOnlineAsync() — verifica periodica / recupero pending approval.</item>
///     <item>DeactivateAsync() — deattivazione esplicita o da uninstall hook.</item>
///   </list>
/// </summary>
internal static class LicenseManager
{
    private static readonly Regex KeyRegex = new(
        @"^LIVE-[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}$",
        RegexOptions.Compiled);

    /// <summary>
    /// Normalizza input utente: rimuove spazi, uppercase, auto-formatta 16 char alfanumerici
    /// senza trattini in LIVE-XXXX-XXXX-XXXX-XXXX.
    /// </summary>
    public static string NormalizeKey(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        var cleaned = Regex.Replace(raw, @"\s+", "").ToUpperInvariant();
        var onlyAlnum = Regex.Replace(cleaned, @"[^A-Z0-9]", "");
        if (Regex.IsMatch(onlyAlnum, @"^[A-HJ-NP-Z2-9]{16}$"))
        {
            return $"LIVE-{onlyAlnum[..4]}-{onlyAlnum.Substring(4, 4)}-{onlyAlnum.Substring(8, 4)}-{onlyAlnum.Substring(12, 4)}";
        }
        return cleaned;
    }

    public static bool IsValidKeyFormat(string key) => KeyRegex.IsMatch(key);

    /// <summary>
    /// Check istantaneo senza rete. Usato dal gate al boot.
    /// </summary>
    public static LicenseStatus GetStatus()
    {
        HardwareFingerprint fp;
        try { fp = HardwareFingerprint.Compute(); }
        catch (Exception ex) { return LicenseStatus.Error(ex.Message); }

        var pending = LicenseStorage.LoadPending();
        if (pending != null && pending.Fingerprint == fp.FingerprintHex)
        {
            return LicenseStatus.Pending();
        }

        var data = LicenseStorage.LoadLicense();
        if (data == null) return LicenseStatus.NotActivated();

        if (data.Fingerprint != fp.FingerprintHex)
            return LicenseStatus.WrongMachine();

        if (string.IsNullOrEmpty(data.Token))
            return LicenseStatus.Pending();

        if (OfflineGraceOk(data))
            return LicenseStatus.LicensedOk(data.CustomerName, data.ProductIds);

        return LicenseStatus.NeedsOnlineVerify();
    }

    public static async Task<LicenseStatus> ActivateAsync(string rawKey, CancellationToken ct = default)
    {
        var key = NormalizeKey(rawKey);
        if (!IsValidKeyFormat(key))
            return LicenseStatus.Error("Invalid license key format");

        HardwareFingerprint fp;
        try { fp = HardwareFingerprint.Compute(); }
        catch (Exception ex) { return LicenseStatus.Error(ex.Message); }

        using var api = new LicenseApiClient();
        ActivateResponse parsed;
        try
        {
            parsed = await api.ActivateAsync(new ActivateRequest
            {
                LicenseKey = key,
                HardwareFingerprint = fp.FingerprintHex,
                HardwareDetails = fp.HardwareDetails,
                ProductId = LicenseConstants.ProductId,
                AppVersion = LicenseApiClient.AppVersion(),
            }, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return LicenseStatus.Error($"Network: {ex.Message}");
        }

        if (parsed.PendingApproval)
        {
            LicenseStorage.SavePending(key, fp.FingerprintHex);
            return LicenseStatus.Pending(parsed.Error);
        }

        if (!parsed.Success)
        {
            LicenseStorage.ClearPending();
            return LicenseStatus.Error(parsed.Error ?? "Activation failed");
        }

        if (string.IsNullOrEmpty(parsed.Token) || string.IsNullOrEmpty(parsed.ExpiresAt))
            return LicenseStatus.Error("Server: invalid response (missing token or expiresAt)");

        var data = new LicenseData
        {
            LicenseKey = key,
            Token = parsed.Token!,
            Fingerprint = fp.FingerprintHex,
            ExpiresAt = parsed.ExpiresAt!,
            VerifyBefore = parsed.VerifyBeforeDate ?? parsed.ExpiresAt!,
            CustomerName = parsed.CustomerName,
            ProductIds = parsed.ProductIds,
            HardwareDetails = fp.HardwareDetails,
        };
        LicenseStorage.SaveLicense(data);
        return LicenseStatus.LicensedOk(data.CustomerName, data.ProductIds);
    }

    /// <summary>
    /// Verifica online: se c'è un pending per questa macchina ritenta activate; altrimenti verify.
    /// </summary>
    public static async Task<LicenseStatus> VerifyOnlineAsync(CancellationToken ct = default)
    {
        HardwareFingerprint fp;
        try { fp = HardwareFingerprint.Compute(); }
        catch (Exception ex) { return LicenseStatus.Error(ex.Message); }

        var pending = LicenseStorage.LoadPending();
        if (pending != null && pending.Fingerprint == fp.FingerprintHex)
        {
            return await ActivateAsync(pending.LicenseKey, ct).ConfigureAwait(false);
        }

        var data = LicenseStorage.LoadLicense();
        if (data == null) return LicenseStatus.NotActivated();

        if (data.Fingerprint != fp.FingerprintHex)
            return LicenseStatus.WrongMachine();

        using var api = new LicenseApiClient();
        VerifyResponse parsed;
        try
        {
            parsed = await api.VerifyAsync(new VerifyRequest
            {
                LicenseKey = data.LicenseKey,
                HardwareFingerprint = fp.FingerprintHex,
                Token = string.IsNullOrEmpty(data.Token) ? null : data.Token,
                ProductId = LicenseConstants.ProductId,
                AppVersion = LicenseApiClient.AppVersion(),
            }, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return LicenseStatus.Error($"Network: {ex.Message}");
        }

        if (parsed.PendingApproval)
        {
            LicenseStorage.SavePending(data.LicenseKey, fp.FingerprintHex);
            LicenseStorage.DeleteLicense();
            return LicenseStatus.Pending(parsed.Error);
        }

        if (!parsed.Valid)
            return LicenseStatus.ExpiredStatus(parsed.Error ?? "License invalid");

        if (!string.IsNullOrEmpty(parsed.NewToken)) data.Token = parsed.NewToken!;
        if (!string.IsNullOrEmpty(parsed.ExpiresAt)) data.ExpiresAt = parsed.ExpiresAt!;
        if (!string.IsNullOrEmpty(parsed.NextVerifyDate)) data.VerifyBefore = parsed.NextVerifyDate!;
        LicenseStorage.SaveLicense(data);
        return LicenseStatus.LicensedOk(data.CustomerName, data.ProductIds);
    }

    /// <summary>
    /// Deattivazione esplicita (manuale) o da <c>--deactivate</c> durante uninstall NSIS/Iexpress.
    /// Non lancia eccezioni: best-effort.
    /// </summary>
    public static async Task DeactivateAsync(string reason, CancellationToken ct = default)
    {
        HardwareFingerprint fp;
        try { fp = HardwareFingerprint.Compute(); }
        catch
        {
            LicenseStorage.DeleteLicense();
            return;
        }

        var data = LicenseStorage.LoadLicense();
        if (data == null)
        {
            LicenseStorage.ClearPending();
            return;
        }

        if (string.IsNullOrEmpty(data.Token) || data.Fingerprint != fp.FingerprintHex)
        {
            LicenseStorage.DeleteLicense();
            return;
        }

        using var api = new LicenseApiClient();
        await api.DeactivateAsync(new DeactivateRequest
        {
            LicenseKey = data.LicenseKey,
            HardwareFingerprint = fp.FingerprintHex,
            Token = data.Token,
            Reason = reason,
        }, ct).ConfigureAwait(false);

        LicenseStorage.DeleteLicense();
    }

    /// <summary>
    /// Grace period offline: mirror della logica di Ledwall.
    /// Se expires_at è scaduto → false; se verify_before + 30 giorni è scaduto → false.
    /// </summary>
    public static bool OfflineGraceOk(LicenseData data)
    {
        var now = DateTimeOffset.UtcNow;
        if (!string.IsNullOrEmpty(data.ExpiresAt)
            && DateTimeOffset.TryParse(data.ExpiresAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var exp)
            && now > exp.ToUniversalTime())
        {
            return false;
        }
        if (string.IsNullOrEmpty(data.VerifyBefore)) return true;
        if (DateTimeOffset.TryParse(data.VerifyBefore, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var vb))
        {
            var deadline = vb.ToUniversalTime().AddDays(LicenseConstants.OfflineGraceDays);
            if (now > deadline) return false;
        }
        return true;
    }

    public static string FingerprintForSupport()
    {
        try { return HardwareFingerprint.Compute().FingerprintHex; }
        catch { return string.Empty; }
    }
}
