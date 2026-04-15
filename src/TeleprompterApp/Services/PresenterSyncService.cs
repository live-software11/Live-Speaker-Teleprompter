using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Xml;

namespace TeleprompterApp.Services;

/// <summary>
/// Debounced synchronization of FlowDocument content from the editor to the PresenterWindow.
/// Serialization happens on UI thread (required by WPF), but is coalesced via debounce.
/// Uses a 300ms debounce: marks dirty on TextChanged, syncs after typing stops.
/// </summary>
internal sealed class PresenterSyncService : IDisposable
{
    private const int DebounceMs = 300;

    private readonly Dispatcher _dispatcher;
    private DispatcherTimer? _debounceTimer;
    private bool _isDirty;
    private bool _disposed;
    private bool _isSyncing;

    private Func<FlowDocument?>? _getSourceDocument;
    private Action<FlowDocument>? _applyToPresenter;

    /// <summary>
    /// Raised when both serialization strategies (XamlPackage + XamlWriter fallback) fail.
    /// Consumers (typically MainWindow) should surface a non-intrusive status message.
    /// </summary>
    public event Action<string>? SyncFailed;

    public PresenterSyncService(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void Configure(Func<FlowDocument?> getSourceDocument, Action<FlowDocument> applyToPresenter)
    {
        _getSourceDocument = getSourceDocument;
        _applyToPresenter = applyToPresenter;
    }

    public void MarkDirty()
    {
        if (_disposed)
        {
            return;
        }

        _isDirty = true;

        if (_debounceTimer == null)
        {
            _debounceTimer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(DebounceMs)
            };
            _debounceTimer.Tick += OnDebounceElapsed;
        }

        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    public void SyncNow()
    {
        _debounceTimer?.Stop();
        _isDirty = false;
        DoSync();
    }

    private void OnDebounceElapsed(object? sender, EventArgs e)
    {
        _debounceTimer?.Stop();

        if (_isDirty)
        {
            _isDirty = false;
            DoSync();
        }
    }

    private void DoSync()
    {
        if (_getSourceDocument == null || _applyToPresenter == null)
        {
            return;
        }

        if (_isSyncing)
        {
            _isDirty = true;
            return;
        }

        var source = _getSourceDocument();
        if (source == null)
        {
            return;
        }

        _isSyncing = true;
        try
        {
            // Serialize on UI thread (required by WPF), then apply
            FlowDocument? clone = null;

            var serialized = SerializeDocument(source);
            if (serialized != null)
            {
                clone = DeserializeDocument(serialized.Value.data, serialized.Value.props);
            }
            else
            {
                // Fallback XamlWriter — più lento (~300ms) ma più robusto per documenti
                // con elementi non serializzabili da XamlPackage (es. embedded media rotti).
                var props = CaptureProps(source);
                clone = TryDeserializeWithXamlWriter(source, props);
            }

            if (clone != null)
            {
                _applyToPresenter(clone);
            }
            else
            {
                // Entrambe le strategie fallite: notifichiamo MainWindow per mostrare uno status.
                try { SyncFailed?.Invoke("XamlPackage+XamlWriter failed"); } catch { }
            }
        }
        catch (Exception ex)
        {
            // Ignore serialization issues to avoid disrupting the operator
            try { SyncFailed?.Invoke(ex.Message); } catch { }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private static DocProps CaptureProps(FlowDocument source)
    {
        return new DocProps(
            source.PagePadding, source.PageWidth, source.LineHeight,
            source.TextAlignment, source.Background,
            source.FontFamily, source.FontSize,
            source.Foreground, source.FontWeight, source.FontStyle);
    }

    private static FlowDocument? TryDeserializeWithXamlWriter(FlowDocument source, DocProps props)
    {
        try
        {
            var xaml = XamlWriter.Save(source);
            if (XamlReader.Parse(xaml) is not FlowDocument clone)
                return null;

            clone.PagePadding = props.PagePadding;
            clone.PageWidth = props.PageWidth;
            clone.LineHeight = props.LineHeight;
            clone.TextAlignment = props.TextAlignment;
            clone.Background = props.Background;
            clone.FontFamily = props.FontFamily;
            clone.FontSize = props.FontSize;
            clone.Foreground = props.Foreground;
            clone.FontWeight = props.FontWeight;
            clone.FontStyle = props.FontStyle;
            return clone;
        }
        catch
        {
            return null;
        }
    }

    private record struct DocProps(
        Thickness PagePadding, double PageWidth, double LineHeight,
        TextAlignment TextAlignment, System.Windows.Media.Brush? Background,
        System.Windows.Media.FontFamily? FontFamily, double FontSize,
        System.Windows.Media.Brush? Foreground, FontWeight FontWeight,
        System.Windows.FontStyle FontStyle);

    private static (byte[] data, DocProps props)? SerializeDocument(FlowDocument source)
    {
        try
        {
            using var stream = new MemoryStream();
            var sourceRange = new TextRange(source.ContentStart, source.ContentEnd);
            sourceRange.Save(stream, System.Windows.DataFormats.XamlPackage);

            var props = new DocProps(
                source.PagePadding, source.PageWidth, source.LineHeight,
                source.TextAlignment, source.Background,
                source.FontFamily, source.FontSize,
                source.Foreground, source.FontWeight, source.FontStyle);

            return (stream.ToArray(), props);
        }
        catch
        {
            return null;
        }
    }

    private static FlowDocument DeserializeDocument(byte[] data, DocProps props)
    {
        var clone = new FlowDocument();
        using var stream = new MemoryStream(data);
        var cloneRange = new TextRange(clone.ContentStart, clone.ContentEnd);
        cloneRange.Load(stream, System.Windows.DataFormats.XamlPackage);

        clone.PagePadding = props.PagePadding;
        clone.PageWidth = props.PageWidth;
        clone.LineHeight = props.LineHeight;
        clone.TextAlignment = props.TextAlignment;
        clone.Background = props.Background;
        clone.FontFamily = props.FontFamily;
        clone.FontSize = props.FontSize;
        clone.Foreground = props.Foreground;
        clone.FontWeight = props.FontWeight;
        clone.FontStyle = props.FontStyle;

        return clone;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_debounceTimer != null)
        {
            _debounceTimer.Stop();
            _debounceTimer.Tick -= OnDebounceElapsed;
            _debounceTimer = null;
        }
    }
}
