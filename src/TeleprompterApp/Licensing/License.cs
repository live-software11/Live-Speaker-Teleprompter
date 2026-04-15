using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TeleprompterApp.Licensing;

/// <summary>
/// Costanti sistema licenze. Allineate al contratto di Live WORKS APP
/// (`functions/src/api/{activate,verify,deactivate}.ts`) e al modulo di Live 3D Ledwall Render
/// (`src-tauri/src/license/manager.rs`).
/// </summary>
internal static class LicenseConstants
{
    public const string ApiBaseUrl = "https://live-works-app.web.app/api";
    public const string ProductId = "speaker-teleprompter";
    public const string AppDataDir = "com.livesoftware.live-speaker-teleprompter";
    public const int NonceLength = 12;
    public const int HttpTimeoutSeconds = 45;
    public const int OfflineGraceDays = 30;
    public const int PendingPollIntervalMs = 30_000;

    /// <summary>
    /// Chiave AES-256-GCM per cifratura file licenza locale. Rigenerare per fork prodotto.
    /// (Stesso meccanismo anti-tamper basilare usato da Live 3D Ledwall Render; chiave distinta
    /// perché ogni app deve avere la propria — da sistema licenze Live WORKS).
    /// </summary>
    public static readonly byte[] LicenseAesKey =
    {
        0x5a, 0x3e, 0x91, 0xcc, 0x4b, 0x27, 0xa6, 0xf1,
        0x88, 0x02, 0x9d, 0xe4, 0x7b, 0x15, 0x3a, 0x66,
        0xd8, 0xb3, 0x44, 0x0f, 0x92, 0x61, 0xce, 0x23,
        0x77, 0xfa, 0x18, 0x85, 0x4e, 0xb9, 0xd1, 0x0c,
    };
}

/// <summary>
/// Dati licenza persistiti localmente in AES-256-GCM.
/// Schema allineato a Ledwall `LicenseData` (rust).
/// </summary>
internal sealed class LicenseData
{
    [JsonPropertyName("license_key")]
    public string LicenseKey { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("fingerprint")]
    public string Fingerprint { get; set; } = string.Empty;

    [JsonPropertyName("expires_at")]
    public string ExpiresAt { get; set; } = string.Empty;

    [JsonPropertyName("verify_before")]
    public string VerifyBefore { get; set; } = string.Empty;

    [JsonPropertyName("customer_name")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("product_ids")]
    public List<string>? ProductIds { get; set; }

    [JsonPropertyName("hardware_details")]
    public string? HardwareDetails { get; set; }
}

/// <summary>
/// Richiesta pending salvata a disco (plain JSON) tra un'attivazione rifiutata e un retry.
/// </summary>
internal sealed class PendingActivation
{
    [JsonPropertyName("license_key")]
    public string LicenseKey { get; set; } = string.Empty;

    [JsonPropertyName("fingerprint")]
    public string Fingerprint { get; set; } = string.Empty;
}

/// <summary>
/// Stati UI del gate. Mirror dell'enum `LicenseStatus` in Ledwall `manager.rs`.
/// </summary>
internal enum LicenseStatusKind
{
    NotActivated,
    PendingApproval,
    Licensed,
    Expired,
    WrongMachine,
    NeedsOnlineVerify,
    Error,
}

internal sealed class LicenseStatus
{
    public LicenseStatusKind Kind { get; init; }
    public string? Message { get; init; }
    public string? CustomerName { get; init; }
    public IReadOnlyList<string>? ProductIds { get; init; }

    public static LicenseStatus NotActivated() => new() { Kind = LicenseStatusKind.NotActivated };
    public static LicenseStatus Pending(string? message = null) => new() { Kind = LicenseStatusKind.PendingApproval, Message = message };
    public static LicenseStatus LicensedOk(string? customer, IReadOnlyList<string>? productIds) => new() { Kind = LicenseStatusKind.Licensed, CustomerName = customer, ProductIds = productIds };
    public static LicenseStatus ExpiredStatus(string? message = null) => new() { Kind = LicenseStatusKind.Expired, Message = message };
    public static LicenseStatus WrongMachine() => new() { Kind = LicenseStatusKind.WrongMachine };
    public static LicenseStatus NeedsOnlineVerify() => new() { Kind = LicenseStatusKind.NeedsOnlineVerify };
    public static LicenseStatus Error(string message) => new() { Kind = LicenseStatusKind.Error, Message = message };
}

/// <summary>
/// DTO per POST /activate. camelCase allineato al backend Cloud Functions.
/// </summary>
internal sealed class ActivateRequest
{
    [JsonPropertyName("licenseKey")] public string LicenseKey { get; set; } = string.Empty;
    [JsonPropertyName("hardwareFingerprint")] public string HardwareFingerprint { get; set; } = string.Empty;
    [JsonPropertyName("hardwareDetails")] public string? HardwareDetails { get; set; }
    [JsonPropertyName("productId")] public string ProductId { get; set; } = string.Empty;
    [JsonPropertyName("appVersion")] public string AppVersion { get; set; } = string.Empty;
}

internal sealed class ActivateResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("pendingApproval")] public bool PendingApproval { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
    [JsonPropertyName("token")] public string? Token { get; set; }
    [JsonPropertyName("expiresAt")] public string? ExpiresAt { get; set; }
    [JsonPropertyName("verifyBeforeDate")] public string? VerifyBeforeDate { get; set; }
    [JsonPropertyName("productIds")] public List<string>? ProductIds { get; set; }
    [JsonPropertyName("customerName")] public string? CustomerName { get; set; }
}

internal sealed class VerifyRequest
{
    [JsonPropertyName("licenseKey")] public string LicenseKey { get; set; } = string.Empty;
    [JsonPropertyName("hardwareFingerprint")] public string HardwareFingerprint { get; set; } = string.Empty;
    [JsonPropertyName("token")] public string? Token { get; set; }
    [JsonPropertyName("productId")] public string ProductId { get; set; } = string.Empty;
    [JsonPropertyName("appVersion")] public string AppVersion { get; set; } = string.Empty;
}

internal sealed class VerifyResponse
{
    [JsonPropertyName("valid")] public bool Valid { get; set; }
    [JsonPropertyName("pendingApproval")] public bool PendingApproval { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
    [JsonPropertyName("expiresAt")] public string? ExpiresAt { get; set; }
    [JsonPropertyName("nextVerifyDate")] public string? NextVerifyDate { get; set; }
    [JsonPropertyName("newToken")] public string? NewToken { get; set; }
}

internal sealed class DeactivateRequest
{
    [JsonPropertyName("licenseKey")] public string LicenseKey { get; set; } = string.Empty;
    [JsonPropertyName("hardwareFingerprint")] public string HardwareFingerprint { get; set; } = string.Empty;
    [JsonPropertyName("token")] public string Token { get; set; } = string.Empty;
    [JsonPropertyName("reason")] public string Reason { get; set; } = string.Empty;
}
