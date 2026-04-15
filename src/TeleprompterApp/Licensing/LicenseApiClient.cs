using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TeleprompterApp.Licensing;

/// <summary>
/// HTTP client per gli endpoint Live WORKS: <c>/api/activate|verify|deactivate</c>.
/// Timeout 45s come Ledwall; nessuna ritentativa automatica — il gate gestisce retry utente.
/// </summary>
internal sealed class LicenseApiClient : IDisposable
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public LicenseApiClient()
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(LicenseConstants.ApiBaseUrl + "/"),
            Timeout = TimeSpan.FromSeconds(LicenseConstants.HttpTimeoutSeconds),
        };
        _http.DefaultRequestHeaders.Add("User-Agent", $"LiveSpeakerTeleprompter/{AppVersion()}");
    }

    public async Task<ActivateResponse> ActivateAsync(ActivateRequest req, CancellationToken ct = default)
    {
        using var res = await _http.PostAsJsonAsync("activate", req, JsonOpts, ct).ConfigureAwait(false);
        var text = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        try
        {
            return JsonSerializer.Deserialize<ActivateResponse>(text, JsonOpts)
                   ?? throw new InvalidOperationException("Empty response");
        }
        catch (JsonException)
        {
            throw new InvalidOperationException($"Invalid server response (HTTP {(int)res.StatusCode})");
        }
    }

    public async Task<VerifyResponse> VerifyAsync(VerifyRequest req, CancellationToken ct = default)
    {
        using var res = await _http.PostAsJsonAsync("verify", req, JsonOpts, ct).ConfigureAwait(false);
        var text = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        try
        {
            return JsonSerializer.Deserialize<VerifyResponse>(text, JsonOpts)
                   ?? throw new InvalidOperationException("Empty response");
        }
        catch (JsonException)
        {
            throw new InvalidOperationException($"Invalid verify response (HTTP {(int)res.StatusCode})");
        }
    }

    public async Task DeactivateAsync(DeactivateRequest req, CancellationToken ct = default)
    {
        try
        {
            using var res = await _http.PostAsJsonAsync("deactivate", req, JsonOpts, ct).ConfigureAwait(false);
            _ = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            // Deactivate è best-effort: se la rete non c'è, cancelliamo comunque il file locale.
        }
    }

    public static string AppVersion()
    {
        var ver = typeof(LicenseApiClient).Assembly.GetName().Version;
        return ver != null ? $"{ver.Major}.{ver.Minor}.{ver.Build}" : "0.0.0";
    }

    public void Dispose() => _http.Dispose();
}
