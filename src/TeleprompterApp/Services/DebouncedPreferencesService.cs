using System;
using System.IO;
using System.Threading;
using System.Windows.Threading;

namespace TeleprompterApp.Services;

/// <summary>
/// Wraps <see cref="PreferencesService"/> with a 500ms debounce
/// and atomic file writes (temp file + rename) to avoid corruption
/// and eliminate micro-freezes during slider dragging.
/// </summary>
internal sealed class DebouncedPreferencesService : IDisposable
{
    private const int DebounceMs = 500;

    private readonly Dispatcher _dispatcher;
    private DispatcherTimer? _debounceTimer;
    private UserPreferences? _pending;
    private bool _disposed;

    public DebouncedPreferencesService(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// Schedules a debounced save. Multiple calls within 500ms are coalesced into one write.
    /// </summary>
    public void SaveDebounced(UserPreferences preferences)
    {
        if (_disposed)
        {
            return;
        }

        _pending = preferences;

        if (_debounceTimer == null)
        {
            _debounceTimer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(DebounceMs)
            };
            _debounceTimer.Tick += OnDebounceElapsed;
        }

        // Reset the timer on each call
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    /// <summary>
    /// Forces an immediate save, bypassing the debounce. Use on app shutdown.
    /// </summary>
    public void Flush()
    {
        _debounceTimer?.Stop();

        if (_pending != null)
        {
            PreferencesService.Save(_pending);
            _pending = null;
        }
    }

    private void OnDebounceElapsed(object? sender, EventArgs e)
    {
        _debounceTimer?.Stop();

        if (_pending != null)
        {
            PreferencesService.Save(_pending);
            _pending = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Flush();

        if (_debounceTimer != null)
        {
            _debounceTimer.Stop();
            _debounceTimer.Tick -= OnDebounceElapsed;
            _debounceTimer = null;
        }
    }
}
