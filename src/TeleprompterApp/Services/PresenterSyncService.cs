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
/// Avoids the expensive XamlWriter.Save + XamlReader.Load on every keystroke.
/// Uses a 300ms debounce: marks dirty on TextChanged, syncs after typing stops.
/// </summary>
internal sealed class PresenterSyncService : IDisposable
{
    private const int DebounceMs = 300;

    private readonly Dispatcher _dispatcher;
    private DispatcherTimer? _debounceTimer;
    private bool _isDirty;
    private bool _disposed;

    private Func<FlowDocument?>? _getSourceDocument;
    private Action<FlowDocument>? _applyToPresenter;

    public PresenterSyncService(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// Configures the source and target for document synchronization.
    /// </summary>
    public void Configure(Func<FlowDocument?> getSourceDocument, Action<FlowDocument> applyToPresenter)
    {
        _getSourceDocument = getSourceDocument;
        _applyToPresenter = applyToPresenter;
    }

    /// <summary>
    /// Marks the document as dirty. The actual sync will happen after the debounce period.
    /// </summary>
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

    /// <summary>
    /// Forces an immediate sync, bypassing the debounce (e.g., when loading a new document).
    /// </summary>
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

        var source = _getSourceDocument();
        if (source == null)
        {
            return;
        }

        try
        {
            var clone = CloneDocument(source);
            _applyToPresenter(clone);
        }
        catch
        {
            // Ignore serialization issues to avoid disrupting the operator
        }
    }

    /// <summary>
    /// Clones a FlowDocument via XamlPackage serialization (faster than XamlWriter/XamlReader).
    /// </summary>
    private static FlowDocument CloneDocument(FlowDocument source)
    {
        // Try the faster XamlPackage approach first
        try
        {
            using var stream = new MemoryStream();
            var sourceRange = new TextRange(source.ContentStart, source.ContentEnd);
            sourceRange.Save(stream, System.Windows.DataFormats.XamlPackage);
            stream.Position = 0;

            var clone = new FlowDocument();
            var cloneRange = new TextRange(clone.ContentStart, clone.ContentEnd);
            cloneRange.Load(stream, System.Windows.DataFormats.XamlPackage);

            // Copy document-level properties (PageWidth critico per preview=program identico)
            clone.PagePadding = source.PagePadding;
            clone.PageWidth = source.PageWidth;
            clone.LineHeight = source.LineHeight;
            clone.TextAlignment = source.TextAlignment;
            clone.Background = source.Background;
            clone.FontFamily = source.FontFamily;
            clone.FontSize = source.FontSize;
            clone.Foreground = source.Foreground;
            clone.FontWeight = source.FontWeight;
            clone.FontStyle = source.FontStyle;

            return clone;
        }
        catch
        {
            // Fall back to XamlWriter/XamlReader
            var xaml = XamlWriter.Save(source);
            using var stringReader = new StringReader(xaml);
            using var xmlReader = XmlReader.Create(stringReader);
            return (FlowDocument)XamlReader.Load(xmlReader);
        }
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
