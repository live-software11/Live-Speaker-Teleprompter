using System;
using System.Collections.Generic;

namespace TeleprompterApp.Osc;

/// <summary>
/// TASK-009: estratto dallo switch gigante di MainWindow.HandleOscMessage.
/// Riceve address+args già sanitized da OscBridge e delega operazioni atomiche
/// al controller. Nessuna logica UI qui — il controller è l'unico touchpoint.
/// </summary>
internal sealed class OscCommandHandler
{
    private readonly ITeleprompterController _controller;

    public OscCommandHandler(ITeleprompterController controller)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    public void Handle(string address, IReadOnlyList<object> args)
    {
        if (string.IsNullOrEmpty(address))
            return;

        switch (address)
        {
            // ----- Playback -----
            case "/teleprompter/start":
            case "/teleprompter/play":
                _controller.SetPlayState(true);
                break;

            case "/teleprompter/stop":
            case "/teleprompter/pause":
                _controller.SetPlayState(false);
                break;

            case "/teleprompter/reset":
                _controller.ResetScroll();
                break;

            // ----- Speed -----
            case "/teleprompter/speed":
                if (OscTypeParser.TryGetDouble(args, 0, out var absoluteSpeed))
                    _controller.SetSpeed(absoluteSpeed, fromSlider: false);
                break;

            case "/teleprompter/speed/increase":
                _controller.AdjustSpeed(_controller.SpeedStep);
                break;

            case "/teleprompter/speed/decrease":
                _controller.AdjustSpeed(-_controller.SpeedStep);
                break;

            // ----- Font -----
            case "/teleprompter/font/size":
                if (OscTypeParser.TryGetDouble(args, 0, out var fontPoints))
                    _controller.SetFontSize(fontPoints);
                break;

            case "/teleprompter/font/increase":
                _controller.AdjustFontSize(+2);
                break;

            case "/teleprompter/font/decrease":
                _controller.AdjustFontSize(-2);
                break;

            // ----- Position -----
            case "/teleprompter/position":
                if (OscTypeParser.TryGetDouble(args, 0, out var ratio))
                    _controller.SetScrollPosition(ratio);
                break;

            case "/teleprompter/jump/top":
                _controller.JumpToTop();
                break;

            case "/teleprompter/jump/bottom":
                _controller.JumpToBottom();
                break;

            // ----- Mirror -----
            case "/teleprompter/mirror":
                if (OscTypeParser.TryGetBool(args, 0, out var enabled))
                    _controller.SetMirror(enabled);
                break;

            case "/teleprompter/mirror/toggle":
                _controller.ToggleMirror();
                break;

            // ----- Status -----
            case "/teleprompter/status/request":
                _controller.SendStatusSnapshot();
                break;

            // ----- NDI -----
            case "/ndi/start":
            case "/output/ndi":
            case "/output/both":
                _controller.NdiStart();
                break;

            case "/ndi/stop":
            case "/output/display":
                _controller.NdiStop();
                break;

            case "/ndi/toggle":
                _controller.NdiToggle();
                break;

            case "/ndi/resolution":
                if (OscTypeParser.TryGetInt(args, 0, out var width) && OscTypeParser.TryGetInt(args, 1, out var height))
                    _controller.SetNdiResolution(width, height);
                break;

            case "/ndi/framerate":
                if (OscTypeParser.TryGetDouble(args, 0, out var fps))
                    _controller.SetNdiFrameRate(fps);
                break;

            case "/ndi/sourcename":
                if (OscTypeParser.TryGetString(args, 0, out var name) && !string.IsNullOrWhiteSpace(name))
                    _controller.SetNdiSourceName(name!.Trim());
                break;

            case "/ndi/status/request":
                _controller.SendNdiStatusSnapshot();
                break;

            default:
                // Comando non riconosciuto: ignore (resta per compat futura).
                break;
        }
    }
}
