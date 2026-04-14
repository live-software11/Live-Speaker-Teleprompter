namespace TeleprompterApp.Osc;

/// <summary>
/// TASK-009: facade controller esposto da <see cref="MainWindow"/> al
/// <see cref="OscCommandHandler"/>. Permette al command handler di restare puro
/// (no dipendenze UI dirette). Ogni metodo è pensato per essere già marshalled
/// sull'UI thread tramite Dispatcher (il dispatch avviene in OscBridge).
/// </summary>
internal interface ITeleprompterController
{
    // Playback
    void SetPlayState(bool playing);

    // Speed
    void AdjustSpeed(double delta);
    void SetSpeed(double value, bool fromSlider);
    double SpeedStep { get; }

    // Font
    void AdjustFontSize(double deltaPoints);
    void SetFontSize(double points);

    // Position / scroll
    void SetScrollPosition(double normalized);
    void JumpToTop();
    void JumpToBottom();
    void ResetScroll();

    // Mirror
    void SetMirror(bool enabled);
    void ToggleMirror();

    // NDI
    void NdiStart();
    void NdiStop();
    void NdiToggle();
    void SetNdiResolution(int width, int height);
    void SetNdiFrameRate(double fps);
    void SetNdiSourceName(string name);
    void SendNdiStatusSnapshot();

    // Status
    void SendStatusSnapshot();
}
