using System;
using System.Collections.Generic;
using TeleprompterApp;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Win32;
using WF = System.Windows.Forms;

namespace TeleprompterApp.Services;

/// <summary>
/// Manages real-time screen detection with triple-redundancy:
/// 1. Win32 WM_DISPLAYCHANGE hook (instant, ~50ms)
/// 2. SystemEvents.DisplaySettingsChanged (.NET backup)
/// 3. Polling timer every 3 seconds (safety net for edge cases)
/// </summary>
internal sealed class DisplayManager : IDisposable
{
    private const int WM_DISPLAYCHANGE = 0x007E;
    private const int WM_DEVICECHANGE = 0x0219;
    private const double PollingIntervalSeconds = 3.0;

    private readonly Window _owner;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _pollingTimer;

    private HwndSource? _hwndSource;
    private string _lastFingerprint = string.Empty;
    private bool _disposed;

    /// <summary>
    /// Raised on the UI thread whenever the set of connected screens changes.
    /// Provides the updated list of <see cref="ScreenInfo"/>.
    /// </summary>
    public event Action<IReadOnlyList<ScreenInfo>>? ScreensChanged;

    public DisplayManager(Window owner)
    {
        _owner = owner;
        _dispatcher = owner.Dispatcher;

        _pollingTimer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher)
        {
            Interval = TimeSpan.FromSeconds(PollingIntervalSeconds)
        };
        _pollingTimer.Tick += OnPollingTick;
    }

    /// <summary>
    /// Must be called after the window is loaded (has an HWND).
    /// Attaches Win32 hook, .NET event, and starts polling.
    /// </summary>
    public void Start()
    {
        // 1. Win32 hook
        var helper = new WindowInteropHelper(_owner);
        if (helper.Handle != IntPtr.Zero)
        {
            _hwndSource = HwndSource.FromHwnd(helper.Handle);
            _hwndSource?.AddHook(WndProc);
        }

        // 2. .NET SystemEvents
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

        // 3. Polling timer
        _pollingTimer.Start();

        // Take initial snapshot
        _lastFingerprint = BuildFingerprint();
    }

    /// <summary>
    /// Returns the current list of screens.
    /// </summary>
    public IReadOnlyList<ScreenInfo> GetCurrentScreens()
    {
        return WF.Screen.AllScreens
            .Select(s => new ScreenInfo(s, GetDisplayNumber(s)))
            .OrderBy(s => s.DisplayNumber == 0 ? int.MaxValue : s.DisplayNumber)
            .ToList();
    }

    /// <summary>
    /// Force a screen refresh check (e.g., after waking from sleep).
    /// </summary>
    public void ForceRefresh()
    {
        CheckForChanges();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg is WM_DISPLAYCHANGE or WM_DEVICECHANGE)
        {
            // WM_DISPLAYCHANGE fires when resolution or monitor count changes
            // Slight delay to let Windows settle the new configuration
            _dispatcher.BeginInvoke(DispatcherPriority.Background, () => CheckForChanges());
        }

        return IntPtr.Zero;
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        _dispatcher.BeginInvoke(DispatcherPriority.Background, () => CheckForChanges());
    }

    private void OnPollingTick(object? sender, EventArgs e)
    {
        CheckForChanges();
    }

    private void CheckForChanges()
    {
        if (_disposed)
        {
            return;
        }

        var newFingerprint = BuildFingerprint();
        if (string.Equals(newFingerprint, _lastFingerprint, StringComparison.Ordinal))
        {
            return;
        }

        _lastFingerprint = newFingerprint;
        var screens = GetCurrentScreens();
        ScreensChanged?.Invoke(screens);
    }

    /// <summary>
    /// Builds a deterministic fingerprint of the current screen configuration.
    /// Changes in count, bounds, device name, or primary status will produce a different fingerprint.
    /// </summary>
    private static string BuildFingerprint()
    {
        var screens = WF.Screen.AllScreens
            .OrderBy(s => s.DeviceName, StringComparer.Ordinal);

        return string.Join("|", screens.Select(s =>
            $"{s.DeviceName}:{s.Bounds.X},{s.Bounds.Y},{s.Bounds.Width},{s.Bounds.Height}:{s.Primary}"));
    }

    private static int GetDisplayNumber(WF.Screen screen)
    {
        var digits = new string(screen.DeviceName.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var number) ? number : 0;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _pollingTimer.Stop();

        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;

        if (_hwndSource != null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource = null;
        }
    }
}

/// <summary>
/// Immutable record describing a connected screen.
/// </summary>
internal sealed record ScreenInfo(WF.Screen Screen, int DisplayNumber)
{
    public bool IsPrimary => Screen.Primary;

    public string DisplayLabel => DisplayNumber > 0
        ? $"{Localization.Get("Display_Number", DisplayNumber)}{(IsPrimary ? Localization.Get("Display_Primary") : string.Empty)}"
        : Localization.Get("Display_Screen", Screen.Bounds.Width, Screen.Bounds.Height);
}
