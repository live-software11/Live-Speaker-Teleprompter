using System;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace TeleprompterApp.Licensing;

/// <summary>
/// T-04: HMAC <c>appId|appVersion|ts|hardwareFingerprint</c> come
/// <c>functions/src/lib/app-challenge.ts</c>. Opzionale: niente header se il secret
/// manca; con <c>APP_CHALLENGE_ENFORCED=true</c> su Cloud Functions il valore deve
/// combaciare con <c>APP_CHALLENGE_SECRET_SPEAKER_TELEPROMPTER</c>.
/// Secret: (1) <see cref="AssemblyMetadataAttribute" /> cotto in build, (2) env
/// <c>LIVEWORKS_APP_CHALLENGE_SECRET</c> a runtime.
/// </summary>
internal static class LicenseAppChallenge
{
    public static void TryAttach(
        HttpRequestMessage request,
        string productId,
        string appVersion,
        string hardwareFingerprint)
    {
        var secret = ResolveSecret();
        if (string.IsNullOrEmpty(secret) || secret.Length < 16)
        {
            return;
        }

        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var payload = $"{productId}|{appVersion}|{ts}|{hardwareFingerprint}";
        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = h.ComputeHash(Encoding.UTF8.GetBytes(payload));
        // Node crypto.digest("hex") è lower-case; il server accetta entrambe le forme.
        var challengeHex = Convert.ToHexString(hash).ToLowerInvariant();
        request.Headers.TryAddWithoutValidation("X-App-Id", productId);
        request.Headers.TryAddWithoutValidation("X-App-Version", appVersion);
        request.Headers.TryAddWithoutValidation("X-App-Challenge-Ts", ts.ToString());
        request.Headers.TryAddWithoutValidation("X-App-Challenge", challengeHex);
    }

    private static string? ResolveSecret()
    {
        foreach (var a in typeof(LicenseApiClient).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (a.Key == "LiveWorksAppChallengeSecret" && !string.IsNullOrWhiteSpace(a.Value))
            {
                return a.Value.Trim();
            }
        }

        return Environment.GetEnvironmentVariable("LIVEWORKS_APP_CHALLENGE_SECRET")?.Trim();
    }
}
