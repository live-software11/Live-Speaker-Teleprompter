using System;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TeleprompterApp;

internal sealed class CompanionBridge : IDisposable
{
    // Allineati a MainWindow (single source of truth per il contratto di velocità).
    // Range documentato in ARCHITETTURA §10: -80 ÷ +80, step 0.5.
    private const double SpeedStep = 0.5;
    private const double MaxSpeed = 80;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly MainWindow _owner;
    private readonly int _port;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;

    public CompanionBridge(MainWindow owner, int port)
    {
        _owner = owner;
        _port = port;
    }

    public string Endpoint => $"http://localhost:{_port}/teleprompter/";

    public bool TryStart()
    {
        if (!HttpListener.IsSupported)
        {
            return false;
        }

        Stop();

        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(Endpoint);
            try
            {
                _listener.Prefixes.Add($"http://127.0.0.1:{_port}/teleprompter/");
            }
            catch
            {
                // alcuni sistemi potrebbero non consentire il binding doppio: ignoriamo l'errore.
            }

            _listener.Start();

            _cts = new CancellationTokenSource();
            _ = Task.Run(() => ListenLoopAsync(_cts.Token));
            return true;
        }
        catch
        {
            Stop();
            return false;
        }
    }

    private async Task ListenLoopAsync(CancellationToken token)
    {
        var listener = _listener;
        if (listener == null)
        {
            return;
        }

        while (!token.IsCancellationRequested)
        {
            HttpListenerContext? context = null;
            try
            {
                context = await listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (HttpListenerException)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                continue;
            }

            if (context != null)
            {
                _ = Task.Run(() => HandleRequestAsync(context, token), token);
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken token)
    {
        var statusCode = 200;
        string payload;

        try
        {
            payload = await ProcessRequestAsync(context).ConfigureAwait(false);
        }
        catch (ArgumentException ex)
        {
            statusCode = 400;
            payload = BuildResponse(error: ex.Message);
        }
        catch (NotSupportedException ex)
        {
            statusCode = 404;
            payload = BuildResponse(error: ex.Message);
        }
        catch (Exception ex)
        {
            statusCode = 500;
            payload = BuildResponse(error: "Internal server error");
            _owner.Dispatcher.Invoke(() => _owner.SetStatus(Localization.Get("Error_Companion", ex.Message)));
        }

        try
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            context.Response.Headers.Add("Access-Control-Allow-Origin", "http://localhost");
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
            context.Response.KeepAlive = false;

            var buffer = Encoding.UTF8.GetBytes(payload);
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
        }
        catch
        {
            // Ignoriamo errori di scrittura verso il client Companion.
        }
        finally
        {
            try
            {
                context.Response.OutputStream.Close();
            }
            catch
            {
                // ignorato
            }
        }
    }

    private async Task<string> ProcessRequestAsync(HttpListenerContext context)
    {
        // Handle CORS preflight requests
        if (string.Equals(context.Request.HttpMethod, "OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            return BuildResponse(message: "OK");
        }

        var path = context.Request.Url?.AbsolutePath ?? "/";
        var segments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0 || !segments[0].Equals("teleprompter", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(Localization.Get("Companion_EndpointNotFound"));
        }

        if (segments.Length == 1 || segments[1].Equals("status", StringComparison.OrdinalIgnoreCase))
        {
            return BuildResponse(message: Localization.Get("Companion_StatusTitle"));
        }

        var command = segments[1].ToLowerInvariant();
        string responseMessage = command;

        switch (command)
        {
            case "play":
                await _owner.Dispatcher.InvokeAsync(() =>
                {
                    _owner.SetPlayState(true);
                    _owner.SetStatus(Localization.Get("Status_CompanionPlay"));
                });
                responseMessage = Localization.Get("Companion_PlayStarted");
                break;
            case "pause":
                await _owner.Dispatcher.InvokeAsync(() =>
                {
                    _owner.SetPlayState(false);
                    _owner.SetStatus(Localization.Get("Status_CompanionPause"));
                });
                responseMessage = Localization.Get("Companion_PlayPaused");
                break;
            case "toggle":
                await _owner.Dispatcher.InvokeAsync(() =>
                {
                    var current = _owner.IsPlaying;
                    _owner.SetPlayState(!current);
                    _owner.SetStatus(current ? Localization.Get("Status_CompanionPause") : Localization.Get("Status_CompanionPlay"));
                });
                responseMessage = Localization.Get("Companion_PlayToggled");
                break;
            case "speed":
                responseMessage = await HandleSpeedCommandAsync(context, segments).ConfigureAwait(false);
                break;
            default:
                throw new NotSupportedException(Localization.Get("Companion_CommandNotSupported", command));
        }

        return BuildResponse(responseMessage);
    }

    private async Task<string> HandleSpeedCommandAsync(HttpListenerContext context, string[] segments)
    {
        var subCommand = segments.Length >= 3 ? segments[2].ToLowerInvariant() : "set";
        string message;

        switch (subCommand)
        {
            case "up":
                await _owner.Dispatcher.InvokeAsync(() =>
                {
                    _owner.AdjustSpeed(SpeedStep);
                    _owner.SetStatus(Localization.Get("Status_CompanionSpeedUp"));
                });
                message = Localization.Get("Companion_SpeedIncreased");
                break;
            case "down":
                await _owner.Dispatcher.InvokeAsync(() =>
                {
                    _owner.AdjustSpeed(-SpeedStep);
                    _owner.SetStatus(Localization.Get("Status_CompanionSpeedDown"));
                });
                message = Localization.Get("Companion_SpeedDecreased");
                break;
            case "reset":
                await _owner.Dispatcher.InvokeAsync(() =>
                {
                    _owner.SetSpeed(0, fromSlider: false);
                    _owner.SetStatus(Localization.Get("Status_CompanionSpeedZero"));
                });
                message = Localization.Get("Companion_SpeedReset");
                break;
            case "set":
                var valueText = context.Request.QueryString?["value"];
                if (string.IsNullOrWhiteSpace(valueText))
                {
                    throw new ArgumentException(Localization.Get("Companion_SpeedValueMissing"));
                }

                if (!double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var target))
                {
                    throw new ArgumentException(Localization.Get("Companion_SpeedValueInvalid"));
                }

                target = Math.Clamp(target, -MaxSpeed, MaxSpeed);

                await _owner.Dispatcher.InvokeAsync(() =>
                {
                    _owner.SetSpeed(target, fromSlider: false);
                    _owner.SetStatus(Localization.Get("Status_CompanionSpeed", target));
                });
                message = Localization.Get("Companion_SpeedSet", target);
                break;
            default:
                throw new NotSupportedException(Localization.Get("Companion_SpeedCommandNotSupported", subCommand));
        }

        return message;
    }

    private string BuildResponse(string message = "OK", string? error = null)
    {
        try
        {
            if (_owner.Dispatcher.HasShutdownStarted || _owner.Dispatcher.HasShutdownFinished)
            {
                return JsonSerializer.Serialize(new { status = "shutdown", message = "Application closing" }, JsonOptions);
            }

            var snapshot = _owner.Dispatcher.Invoke(() => new
            {
                status = error == null ? "ok" : "error",
                message = error ?? message,
                isPlaying = _owner.IsPlaying,
                speed = Math.Round(_owner.CurrentScrollSpeed, 3),
                editMode = _owner.IsEditMode,
                endpoint = Endpoint
            });

            return JsonSerializer.Serialize(snapshot, JsonOptions);
        }
        catch (TaskCanceledException)
        {
            return JsonSerializer.Serialize(new { status = "shutdown", message = "Application closing" }, JsonOptions);
        }
    }

    public void Dispose()
    {
        Stop();
    }

    private void Stop()
    {
        try
        {
            _cts?.Cancel();
        }
        catch
        {
            // ignored
        }

        try
        {
            _listener?.Stop();
            _listener?.Close();
        }
        catch
        {
            // ignored
        }

        _listener = null;
        _cts?.Dispose();
        _cts = null;
    }
}
