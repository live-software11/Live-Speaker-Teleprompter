using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace TeleprompterApp.Licensing;

/// <summary>
/// Finestra modale di attivazione licenza. Mirror funzionale di
/// <c>src/components/LicenseGate.tsx</c> (Ledwall).
/// DialogResult = true ⇒ licenza valida, main window può partire.
/// DialogResult = false / null ⇒ utente ha chiuso ⇒ app esce.
/// </summary>
public partial class LicenseGateWindow : Window
{
    private LicenseStatus? _status;
    private bool _busy;
    private DispatcherTimer? _pendingPollTimer;
    private CancellationTokenSource? _cts;

    public LicenseGateWindow()
    {
        InitializeComponent();
        ApplyStaticLocalization();
    }

    private void ApplyStaticLocalization()
    {
        Title = Localization.Get("License_WindowTitle");
        TitleText.Text = Localization.Get("License_Title");
        SubtitleText.Text = Localization.Get("License_Subtitle");
        KeyLabel.Text = Localization.Get("License_KeyLabel");
        KeyInput.ToolTip = Localization.Get("License_KeyPlaceholder");
        FingerprintLabel.Text = Localization.Get("License_FingerprintLabel");
        CopyFpButton.Content = Localization.Get("License_CopyFingerprint");
        UninstallHint.Text = Localization.Get("License_DeactivateUninstall");
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _cts = new CancellationTokenSource();
        try
        {
            FingerprintText.Text = LicenseManager.FingerprintForSupport();
        }
        catch { FingerprintText.Text = string.Empty; }

        await LoadStatusAsync();
    }

    protected override void OnClosed(EventArgs e)
    {
        _pendingPollTimer?.Stop();
        _pendingPollTimer = null;
        _cts?.Cancel();
        _cts?.Dispose();
        base.OnClosed(e);
    }

    private async System.Threading.Tasks.Task LoadStatusAsync()
    {
        var s = LicenseManager.GetStatus();
        ApplyStatus(s);

        // Se il file locale dice NeedsOnlineVerify ⇒ prova subito online.
        if (s.Kind == LicenseStatusKind.NeedsOnlineVerify)
        {
            await RunVerifyAsync();
        }
    }

    private void ApplyStatus(LicenseStatus status)
    {
        _status = status;
        LocalError.Visibility = Visibility.Collapsed;
        LocalError.Text = string.Empty;

        if (status.Kind == LicenseStatusKind.Licensed)
        {
            DialogResult = true;
            Close();
            return;
        }

        StatusBanner.Visibility = Visibility.Collapsed;
        VerifyButton.Visibility = Visibility.Collapsed;
        StopPendingPolling();

        switch (status.Kind)
        {
            case LicenseStatusKind.PendingApproval:
                ShowBanner(
                    Localization.Get("License_PendingApproval"),
                    status.Message ?? Localization.Get("License_PendingApprovalBody"),
                    Localization.Get("License_PendingHint"));
                PrimaryButton.Content = Localization.Get("License_Activate");
                VerifyButton.Content = Localization.Get("License_VerifyNow");
                VerifyButton.Visibility = Visibility.Visible;
                StartPendingPolling();
                break;

            case LicenseStatusKind.WrongMachine:
                ShowBanner(
                    Localization.Get("License_WrongMachineTitle"),
                    Localization.Get("License_WrongMachineBody"),
                    null);
                PrimaryButton.Content = Localization.Get("License_Activate");
                break;

            case LicenseStatusKind.Expired:
                ShowBanner(
                    Localization.Get("License_ExpiredTitle"),
                    status.Message ?? Localization.Get("License_ExpiredBody"),
                    null);
                PrimaryButton.Content = Localization.Get("License_Activate");
                break;

            case LicenseStatusKind.NeedsOnlineVerify:
                ShowBanner(
                    Localization.Get("License_NeedsOnlineVerifyTitle"),
                    Localization.Get("License_NeedsOnlineVerifyBody"),
                    null);
                PrimaryButton.Content = Localization.Get("License_Activate");
                VerifyButton.Content = Localization.Get("License_VerifyNow");
                VerifyButton.Visibility = Visibility.Visible;
                break;

            case LicenseStatusKind.Error:
                ShowBanner(
                    Localization.Get("License_ErrorTitle"),
                    status.Message ?? Localization.Get("License_ErrorGeneric"),
                    null);
                PrimaryButton.Content = Localization.Get("License_Activate");
                break;

            case LicenseStatusKind.NotActivated:
            default:
                PrimaryButton.Content = Localization.Get("License_Activate");
                break;
        }
    }

    private void ShowBanner(string title, string body, string? hint)
    {
        StatusBannerTitle.Text = title;
        StatusBannerBody.Text = body;
        if (string.IsNullOrEmpty(hint))
        {
            StatusBannerHint.Visibility = Visibility.Collapsed;
        }
        else
        {
            StatusBannerHint.Visibility = Visibility.Visible;
            StatusBannerHint.Text = hint;
        }
        StatusBanner.Visibility = Visibility.Visible;
    }

    private void StartPendingPolling()
    {
        if (_pendingPollTimer != null) return;
        _pendingPollTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(LicenseConstants.PendingPollIntervalMs),
        };
        _pendingPollTimer.Tick += async (_, _) => await RunVerifyAsync(silent: true);
        _pendingPollTimer.Start();
    }

    private void StopPendingPolling()
    {
        _pendingPollTimer?.Stop();
        _pendingPollTimer = null;
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        PrimaryButton.IsEnabled = !busy;
        VerifyButton.IsEnabled = !busy;
        KeyInput.IsEnabled = !busy;
        PrimaryButton.Content = busy
            ? Localization.Get("License_Activating")
            : Localization.Get("License_Activate");
    }

    private async void OnPrimaryClick(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        var raw = KeyInput.Text?.Trim() ?? string.Empty;
        var normalized = LicenseManager.NormalizeKey(raw);
        if (!LicenseManager.IsValidKeyFormat(normalized))
        {
            ShowLocalError(Localization.Get("License_InvalidFormat"));
            return;
        }
        KeyInput.Text = normalized;
        LocalError.Visibility = Visibility.Collapsed;
        SetBusy(true);
        try
        {
            var ct = _cts?.Token ?? CancellationToken.None;
            var result = await LicenseManager.ActivateAsync(normalized, ct).ConfigureAwait(true);
            ApplyStatus(result);
            if (result.Kind == LicenseStatusKind.Error)
            {
                ShowLocalError(MapServerError(result.Message));
            }
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnVerifyClick(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        await RunVerifyAsync();
    }

    private async System.Threading.Tasks.Task RunVerifyAsync(bool silent = false)
    {
        SetBusy(true);
        try
        {
            var ct = _cts?.Token ?? CancellationToken.None;
            var result = await LicenseManager.VerifyOnlineAsync(ct).ConfigureAwait(true);
            ApplyStatus(result);
            if (!silent && result.Kind == LicenseStatusKind.Error)
            {
                ShowLocalError(MapServerError(result.Message));
            }
        }
        catch (Exception ex) when (silent)
        {
            // Rete non disponibile durante polling silenzioso — riproviamo al prossimo giro.
            _ = ex;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void OnCopyFingerprint(object sender, RoutedEventArgs e)
    {
        try
        {
            var fp = FingerprintText.Text;
            if (!string.IsNullOrEmpty(fp))
                System.Windows.Clipboard.SetText(fp);
        }
        catch { /* clipboard occasionalmente bloccata */ }
    }

    private void ShowLocalError(string msg)
    {
        LocalError.Text = msg;
        LocalError.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Mapping errori server → chiavi localizzate (mirror di LicenseGate.tsx).
    /// </summary>
    private static string MapServerError(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return Localization.Get("License_ErrorGeneric");
        var s = raw.ToLowerInvariant();
        if (s.Contains("not found")) return Localization.Get("License_NotFound");
        if (s.Contains("already activated on a different")) return Localization.Get("License_BoundToOtherPc");
        if (s.Contains("expired")) return Localization.Get("License_ExpiredBody");
        if (s.Contains("invalid license key format")) return Localization.Get("License_InvalidFormat");
        if (s.Contains("is not covered")) return Localization.Get("License_ProductNotCovered");
        return raw;
    }
}
