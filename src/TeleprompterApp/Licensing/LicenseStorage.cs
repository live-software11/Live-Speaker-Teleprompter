using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace TeleprompterApp.Licensing;

/// <summary>
/// Persistenza licenza in <c>%LOCALAPPDATA%\{AppDataDir}\license.enc</c> cifrata AES-256-GCM
/// (formato: 12 byte nonce || ciphertext || 16 byte tag, allineato a Ledwall
/// <c>src-tauri/src/license/manager.rs</c>).
/// Il pending (non cifrato) sta in <c>pending_activation.json</c> nella stessa dir.
/// </summary>
internal static class LicenseStorage
{
    private const string LicenseFile = "license.enc";
    private const string PendingFile = "pending_activation.json";

    public static string AppDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(localAppData))
            throw new InvalidOperationException("LOCALAPPDATA non disponibile");
        var dir = Path.Combine(localAppData, LicenseConstants.AppDataDir);
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string LicensePath() => Path.Combine(AppDirectory(), LicenseFile);
    private static string PendingPath() => Path.Combine(AppDirectory(), PendingFile);

    public static LicenseData? LoadLicense()
    {
        var path = LicensePath();
        if (!File.Exists(path)) return null;
        try
        {
            var bytes = File.ReadAllBytes(path);
            return Decrypt(bytes);
        }
        catch
        {
            return null;
        }
    }

    public static void SaveLicense(LicenseData data)
    {
        var encrypted = Encrypt(data);
        var tmp = LicensePath() + ".tmp";
        File.WriteAllBytes(tmp, encrypted);
        File.Move(tmp, LicensePath(), overwrite: true);
        ClearPending();
    }

    public static void DeleteLicense()
    {
        var path = LicensePath();
        if (File.Exists(path))
        {
            try { File.Delete(path); } catch { }
        }
        ClearPending();
    }

    public static PendingActivation? LoadPending()
    {
        var path = PendingPath();
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<PendingActivation>(json);
        }
        catch
        {
            return null;
        }
    }

    public static void SavePending(string licenseKey, string fingerprint)
    {
        var pending = new PendingActivation { LicenseKey = licenseKey, Fingerprint = fingerprint };
        var json = JsonSerializer.Serialize(pending, new JsonSerializerOptions { WriteIndented = true });
        var path = PendingPath();
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, json);
        File.Move(tmp, path, overwrite: true);
    }

    public static void ClearPending()
    {
        var path = PendingPath();
        if (File.Exists(path))
        {
            try { File.Delete(path); } catch { }
        }
    }

    // -- Cifratura AES-256-GCM (stesso schema di Ledwall) --------------------

    private static byte[] Encrypt(LicenseData data)
    {
        var plain = JsonSerializer.SerializeToUtf8Bytes(data);
        var nonce = RandomNumberGenerator.GetBytes(LicenseConstants.NonceLength);
        var cipher = new byte[plain.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(LicenseConstants.LicenseAesKey, 16);
        aes.Encrypt(nonce, plain, cipher, tag);

        // Output: nonce || cipher || tag  (stesso layout di Rust aes-gcm che inline il tag in coda)
        var output = new byte[nonce.Length + cipher.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, output, 0, nonce.Length);
        Buffer.BlockCopy(cipher, 0, output, nonce.Length, cipher.Length);
        Buffer.BlockCopy(tag, 0, output, nonce.Length + cipher.Length, tag.Length);
        return output;
    }

    private static LicenseData? Decrypt(byte[] bytes)
    {
        if (bytes.Length < LicenseConstants.NonceLength + 16) return null;
        var nonce = new byte[LicenseConstants.NonceLength];
        Buffer.BlockCopy(bytes, 0, nonce, 0, nonce.Length);

        var cipherLen = bytes.Length - nonce.Length - 16;
        if (cipherLen <= 0) return null;
        var cipher = new byte[cipherLen];
        Buffer.BlockCopy(bytes, nonce.Length, cipher, 0, cipherLen);

        var tag = new byte[16];
        Buffer.BlockCopy(bytes, nonce.Length + cipherLen, tag, 0, 16);

        var plain = new byte[cipherLen];
        using var aes = new AesGcm(LicenseConstants.LicenseAesKey, 16);
        try
        {
            aes.Decrypt(nonce, cipher, tag, plain);
        }
        catch (CryptographicException)
        {
            return null;
        }
        return JsonSerializer.Deserialize<LicenseData>(plain);
    }
}
