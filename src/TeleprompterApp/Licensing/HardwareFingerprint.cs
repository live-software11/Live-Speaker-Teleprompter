using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace TeleprompterApp.Licensing;

/// <summary>
/// Fingerprint hardware Windows (WMI) — allineato a GUIDA_INTEGRAZIONE_LICENZA_APP §3
/// e al modulo Ledwall <c>src-tauri/src/license/fingerprint.rs</c>.
/// SHA-256( MB_SERIAL | CPU_ID | DISK_SERIAL ) in hex 64 caratteri.
/// </summary>
internal readonly struct HardwareFingerprint
{
    public string FingerprintHex { get; init; }
    public string HardwareDetails { get; init; }

    public static HardwareFingerprint Compute()
    {
        var mb = QueryFirst("SELECT SerialNumber FROM Win32_BaseBoard", "SerialNumber");
        var cpu = QueryFirst("SELECT ProcessorId FROM Win32_Processor", "ProcessorId");
        var disk = QueryFirst("SELECT SerialNumber FROM Win32_DiskDrive WHERE Index=0", "SerialNumber");

        var missing = new System.Collections.Generic.List<string>();
        if (mb == null) missing.Add("MB");
        if (cpu == null) missing.Add("CPU");
        if (disk == null) missing.Add("DISK");
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Hardware fingerprint incomplete — missing: {string.Join(", ", missing)}. " +
                "WMI query failed; license activation requires real hardware identifiers.");

        var pipe = $"{mb}|{cpu}|{disk}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(pipe));
        var hex = Convert.ToHexString(hash).ToLowerInvariant();

        return new HardwareFingerprint
        {
            FingerprintHex = hex,
            HardwareDetails = $"MB:{mb}|CPU:{cpu}|DISK:{disk}",
        };
    }

    private static string? QueryFirst(string wql, string field)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(wql);
            foreach (ManagementObject obj in searcher.Get())
            {
                using (obj)
                {
                    var val = obj[field]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(val)) return val;
                }
            }
        }
        catch
        {
            // WMI può fallire su VM / ambienti ristretti — fallback a sentinel.
        }
        return null;
    }
}
