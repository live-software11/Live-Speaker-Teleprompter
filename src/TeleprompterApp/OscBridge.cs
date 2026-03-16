using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TeleprompterApp.Osc;

namespace TeleprompterApp;

internal sealed class OscBridge : IDisposable
{
    private readonly MainWindow _owner;
    private readonly int _listenPort;
    private readonly UdpClient _feedbackClient;
    private readonly IPEndPoint _feedbackEndpoint;

    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;

    public OscBridge(MainWindow owner, int listenPort, string feedbackHost, int feedbackPort)
    {
        _owner = owner;
        _listenPort = listenPort;
    _feedbackClient = new UdpClient();
    _feedbackEndpoint = new IPEndPoint(IPAddress.Parse(feedbackHost), feedbackPort);
    }

    public bool Start()
    {
        Stop();

        try
        {
            _udpClient = new UdpClient(_listenPort);
        }
        catch (SocketException)
        {
            Stop();
            return false;
        }

        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ListenLoopAsync(_cts.Token));
        return true;
    }

    private async Task ListenLoopAsync(CancellationToken token)
    {
        var client = _udpClient;
        if (client == null)
        {
            return;
        }

        while (!token.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try
            {
                result = await client.ReceiveAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                continue;
            }

            try
            {
                var packet = OscPacket.Parse(result.Buffer);
                if (packet != null)
                {
                    DispatchPacket(packet);
                }
            }
            catch
            {
                // Ignore malformed packets
            }
        }
    }

    private void DispatchPacket(OscPacket packet)
    {
        switch (packet)
        {
            case OscMessage message:
                DispatchMessage(message);
                break;
            case OscBundle bundle:
                foreach (var inner in bundle.Packets.OfType<OscMessage>())
                {
                    DispatchMessage(inner);
                }
                break;
        }
    }

    private void DispatchMessage(OscMessage message)
    {
        var args = message.Arguments ?? Array.Empty<object>();
        _owner.Dispatcher.InvokeAsync(() => _owner.HandleOscMessage(message.Address, args.ToList()));
    }

    public void SendFeedback(string address, params object[] values)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return;
        }

        try
        {
            var payload = BuildMessagePayload(address, values);
            if (payload != null)
            {
                _feedbackClient.Send(payload, payload.Length, _feedbackEndpoint);
            }
        }
        catch
        {
            // Feedback errors are non-fatal; ignore
        }
    }

    private static byte[]? BuildMessagePayload(string address, IReadOnlyList<object> values)
    {
        var sanitized = new List<object>(values.Count);
        foreach (var value in values)
        {
            if (value is null)
            {
                sanitized.Add(string.Empty);
                continue;
            }

            switch (value)
            {
                case bool or int or long or float or double or byte[]:
                    sanitized.Add(value);
                    break;
                default:
                    sanitized.Add(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
                    break;
            }
        }

        var message = new OscMessage(address, sanitized);
        return message.ToByteArray();
    }

    public void Stop()
    {
        try
        {
            _cts?.Cancel();
        }
        catch
        {
        }

        try
        {
            _udpClient?.Close();
        }
        catch
        {
        }

        try { _udpClient?.Dispose(); } catch { }
        _udpClient = null;

        try { _cts?.Dispose(); } catch { }
        _cts = null;
    }

    public void Dispose()
    {
        Stop();
        try { _feedbackClient.Dispose(); } catch { }
    }
}
