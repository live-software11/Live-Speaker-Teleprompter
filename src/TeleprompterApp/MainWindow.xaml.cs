using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Markup;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using WF = System.Windows.Forms;
using SD = System.Drawing;
using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaFontFamily = System.Windows.Media.FontFamily;
using MediaMessageBox = System.Windows.MessageBox;
using MediaDataFormats = System.Windows.DataFormats;
using MediaPoint = System.Windows.Point;
using MediaCursors = System.Windows.Input.Cursors;
using WpfApplication = System.Windows.Application;
using WpfRichTextBox = System.Windows.Controls.RichTextBox;
using WpfScrollViewer = System.Windows.Controls.ScrollViewer;
using WpfToggleButton = System.Windows.Controls.Primitives.ToggleButton;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfSlider = System.Windows.Controls.Slider;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfCanvas = System.Windows.Controls.Canvas;
using WpfGrid = System.Windows.Controls.Grid;
using WpfScaleTransform = System.Windows.Media.ScaleTransform;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfPolygon = System.Windows.Shapes.Polygon;
using System.Xml;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TeleprompterApp.Services;

namespace TeleprompterApp
{
    public partial class MainWindow : Window
    {
    // ── Scroll ──
    private readonly DispatcherTimer _scrollTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly Stopwatch _scrollStopwatch = new();
    private double _scrollSpeed;
    private const double SpeedStep = 0.25;
    private const double MaxSpeed = 20;

    // ── Monitors ──
    private readonly List<ScreenInfo> _screenInfos = new();
    private readonly List<ToggleButton> _monitorToggleButtons = new();

    // ── Services ──
    private DisplayManager? _displayManager;
    private DebouncedPreferencesService? _debouncedPrefs;
    private PresenterSyncService? _presenterSync;

    private readonly HashSet<string> _supportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".rtf", ".srt", ".vtt", ".log", ".csv", ".json", ".xml", ".html", ".htm", ".yaml", ".yml", ".ini", ".cfg", ".bat", ".ps1", ".xaml", ".xamlpackage", ".rstp"
    };
    private PresenterWindow? _presenterWindow;
    private UserPreferences _preferences = new();
    private bool _isApplyingPreferences;
    private bool _isSyncingMonitorToggle;
    private bool _isUpdatingSpeedSlider;
    private bool _isUpdatingFontSizeSelection;
    private string? _currentDocumentPath;
    private bool _isDraggingArrow;
    private bool _isUpdatingArrowSize;
    private MediaPoint _arrowDragStart;
    private MediaPoint _arrowInitialOffset;
    private MediaPoint _arrowNormalizedPosition = new(0, 0.5);
    private double _arrowScale = 1.0;
    private bool _isUpdatingEditToggle;
    private bool _pendingEditMode = true;
    private bool _isUpdatingLeftMargin;
    private const double BasePagePadding = 72;
    private double _arrowPaddingExtra = 12;
    private const double ArrowLeftOffset = 12;
    private const double ArrowBaseWidth = 72;
    private const int CompanionPort = 3131;
    private CompanionBridge? _companionBridge;
    private NDITransmitter? _ndiTransmitter;
    private OscBridge? _oscBridge;
    private string _ndiSourceName = "R-Speaker NDI";
    private int? _ndiTargetWidth;
    private int? _ndiTargetHeight;
    private double _ndiFrameRate = 30.0;

    private const int OscListenPort = 8000;
    private const int OscFeedbackPort = 8001;

    // ── Public API for CompanionBridge / OscBridge ──
    public bool IsPlaying => _playPauseToggle?.IsChecked == true;
    public double CurrentScrollSpeed => _scrollSpeed;

    private WpfRichTextBox _contentEditor = null!;
    private WpfScrollViewer _contentScrollViewer = null!;
    private WpfToggleButton _editModeToggle = null!;
    private WpfToggleButton _playPauseToggle = null!;
    private WpfToggleButton _mirrorToggle = null!;
    private WpfToggleButton _topMostToggle = null!;
    private WpfToggleButton _ndiToggle = null!;
    private WpfComboBox _fontSizeComboBox = null!;
    private WpfSlider _speedSlider = null!;
    private WpfTextBlock _speedText = null!;
    private WpfSlider _arrowSizeSlider = null!;
    private WpfSlider _leftMarginSlider = null!;
    private WpfTextBlock _leftMarginValueText = null!;
    private WpfCanvas _arrowCanvas = null!;
    private WpfGrid _arrowContainer = null!;
    private WpfScaleTransform _arrowScaleTransform = null!;
    private WpfPolygon _arrowShape = null!;
    private WpfTextBlock _statusText = null!;
    private WpfStackPanel _monitorTogglePanel = null!;

    public MainWindow()
    {
        LoadView();
        ResolveNamedElements();
        AttachRuntimeHandlers();
        _scrollTimer.Tick += OnScrollTimerTick;
    }

    private void LoadView()
    {
        var resourceLocator = new Uri("/TeleprompterApp;component/MainWindow.xaml", UriKind.Relative);
        WpfApplication.LoadComponent(this, resourceLocator);
    }

    private void ResolveNamedElements()
    {
    _contentEditor = GetRequiredElement<WpfRichTextBox>("ContentEditor");
    _contentScrollViewer = GetRequiredElement<WpfScrollViewer>("ContentScrollViewer");
    _editModeToggle = GetRequiredElement<WpfToggleButton>("EditModeToggle");
    _playPauseToggle = GetRequiredElement<WpfToggleButton>("PlayPauseToggle");
    _mirrorToggle = GetRequiredElement<WpfToggleButton>("MirrorToggle");
    _topMostToggle = GetRequiredElement<WpfToggleButton>("TopMostToggle");
    _ndiToggle = GetRequiredElement<WpfToggleButton>("NdiToggle");
    _fontSizeComboBox = GetRequiredElement<WpfComboBox>("FontSizeComboBox");
    _speedSlider = GetRequiredElement<WpfSlider>("SpeedSlider");
    _speedText = GetRequiredElement<WpfTextBlock>("SpeedText");
    _arrowSizeSlider = GetRequiredElement<WpfSlider>("ArrowSizeSlider");
    _leftMarginSlider = GetRequiredElement<WpfSlider>("LeftMarginSlider");
    _leftMarginValueText = GetRequiredElement<WpfTextBlock>("LeftMarginValueText");
    _arrowCanvas = GetRequiredElement<WpfCanvas>("ArrowCanvas");
    _arrowContainer = GetRequiredElement<WpfGrid>("ArrowContainer");
    _arrowScaleTransform = GetRequiredElement<WpfScaleTransform>("ArrowScaleTransform");
    _arrowShape = GetRequiredElement<WpfPolygon>("ArrowShape");
    _statusText = GetRequiredElement<WpfTextBlock>("StatusText");
    _monitorTogglePanel = GetRequiredElement<WpfStackPanel>("MonitorTogglePanel");
    }

    private void AttachRuntimeHandlers()
    {
        _contentEditor.TextChanged += ContentEditor_TextChanged;
        _contentScrollViewer.ScrollChanged += ContentScrollViewer_ScrollChanged;
    }

    private T GetRequiredElement<T>(string name) where T : class
    {
        if (FindName(name) is T element)
        {
            return element;
        }

        throw new InvalidOperationException($"Elemento XAML '{name}' non trovato.");
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Maximized;

        _isApplyingPreferences = true;
        _preferences = PreferencesService.Load();

        // Initialize services
        _debouncedPrefs = new DebouncedPreferencesService(Dispatcher);
        _presenterSync = new PresenterSyncService(Dispatcher);

        var placeholder = string.IsNullOrWhiteSpace(_preferences.LastScriptPath)
            ? "Carica un file di testo o inizia a scrivere..."
            : null;

        ApplyDocumentDefaults(placeholder);

        // Real-time display manager instead of one-shot PopulateMonitorList
        _displayManager = new DisplayManager(this);
        RebuildMonitorToggles(_displayManager.GetCurrentScreens());
        _displayManager.ScreensChanged += OnScreensChanged;
        _displayManager.Start();

        EnsurePresenterWindow();

        // Configure presenter sync service
        _presenterSync.Configure(
            () => _contentEditor?.Document,
            clone =>
            {
                if (_presenterWindow == null) return;
                _presenterWindow.SetDocument(clone);
                var presenterPadding = GetPresenterPagePadding(_contentEditor.Document.PagePadding);
                _presenterWindow.SetPagePadding(presenterPadding);
                _presenterWindow.SetVerticalOffset(_contentScrollViewer.VerticalOffset);
                UpdatePresenterArrowAppearance();
                UpdatePresenterMirror();
            });

        ApplyPreferences();
        ApplyMirrorState();
        StartCompanionIntegration();
    StartOscIntegration();
        _isApplyingPreferences = false;

        _pendingEditMode = _editModeToggle?.IsChecked ?? _pendingEditMode;
        ApplyEditMode(_pendingEditMode);

        if (_pendingEditMode)
        {
            _contentEditor.Focus();
        }
        else
        {
            FocusManager.SetFocusedElement(this, this);
        }
    }

    private void StartOscIntegration()
    {
        if (_oscBridge != null)
        {
            return;
        }

        var bridge = new OscBridge(this, OscListenPort, "127.0.0.1", OscFeedbackPort);
        if (bridge.Start())
        {
            _oscBridge = bridge;
            SendOscStatusSnapshot();
        }
        else
        {
            SetStatus($"OSC non disponibile (porta {OscListenPort}).");
        }
    }

    private void StartCompanionIntegration()
    {
        if (_companionBridge != null)
        {
            return;
        }

        var bridge = new CompanionBridge(this, CompanionPort);
        if (bridge.TryStart())
        {
            _companionBridge = bridge;
        }
        else
        {
            SetStatus($"Companion non disponibile (porta {CompanionPort}).");
        }
    }

    private void StartNdiStreaming()
    {
        if (_contentScrollViewer == null)
        {
            SetStatus("NDI non disponibile: contenuto non pronto.");
            if (_ndiToggle != null)
            {
                _ndiToggle.IsChecked = false;
            }
            NotifyOscNdiStatus();
            return;
        }

        _ndiTransmitter ??= new NDITransmitter(_contentScrollViewer, _ndiSourceName);
        _ndiTransmitter.SetSourceName(_ndiSourceName);
        _ndiTransmitter.SetTargetResolution(_ndiTargetWidth, _ndiTargetHeight);
        _ndiTransmitter.SetFrameRate(_ndiFrameRate);

        if (_ndiTransmitter.TryStart())
        {
            SetStatus("Trasmissione NDI attiva.");
        }
        else
        {
            SetStatus("Impossibile avviare NDI: installa il runtime NewTek.");
            if (_ndiToggle != null)
            {
                _ndiToggle.IsChecked = false;
            }
        }

        NotifyOscNdiStatus();
    }

    private void StopNdiStreaming()
    {
        if (_ndiTransmitter == null)
        {
            return;
        }

        _ndiTransmitter.Stop();
        SetStatus("Trasmissione NDI disattivata.");
        NotifyOscNdiStatus();
    }

    private void ApplyDocumentDefaults(string? placeholder = "Carica un file di testo o inizia a scrivere...")
    {
        var document = _contentEditor.Document;
        document.Blocks.Clear();
        document.PagePadding = new Thickness(BasePagePadding);
        document.LineHeight = _contentEditor.FontSize * 1.2;
        document.TextAlignment = TextAlignment.Center;
        document.Background = MediaBrushes.Transparent;

        if (!string.IsNullOrWhiteSpace(placeholder))
        {
            var paragraph = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                LineHeight = document.LineHeight
            };
            paragraph.Inlines.Add(new Run(placeholder));
            document.Blocks.Add(paragraph);
        }

        SetDocumentForeground(_contentEditor.Foreground ?? MediaBrushes.White);
        ApplyArrowSafePadding();
    }

    private void ApplyPreferences()
    {
        if (_preferences is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(_preferences.DocumentBackgroundHex))
        {
            var background = CreateBrushFromHex(_preferences.DocumentBackgroundHex);
            if (background != null)
            {
                _contentEditor.Document.Background = background;
                _contentEditor.Background = MediaBrushes.Transparent;
            }
        }

        if (!string.IsNullOrWhiteSpace(_preferences.TextForegroundHex))
        {
            var foreground = CreateBrushFromHex(_preferences.TextForegroundHex);
            if (foreground != null)
            {
                SetDocumentForeground(foreground);
            }
        }

        if (!string.IsNullOrWhiteSpace(_preferences.FontFamily))
        {
            ApplyFont(_preferences.FontFamily!, _preferences.FontSizePoints <= 0 ? 72 : _preferences.FontSizePoints, _preferences.IsBold, _preferences.IsItalic, _preferences.UseUnderline, strikeout: false);
            _isUpdatingFontSizeSelection = true;
            _fontSizeComboBox.SelectedValue = Math.Round(_preferences.FontSizePoints <= 0 ? 72 : _preferences.FontSizePoints).ToString("F0");
            _isUpdatingFontSizeSelection = false;
        }

        _topMostToggle.IsChecked = _preferences.TopMostEnabled;
        _mirrorToggle.IsChecked = _preferences.MirrorEnabled;

        if (_editModeToggle != null)
        {
            _isUpdatingEditToggle = true;
            _editModeToggle.IsChecked = _preferences.EditModeEnabled;
            _isUpdatingEditToggle = false;
        }

        _pendingEditMode = _preferences.EditModeEnabled;

        var preferredToggle = _monitorToggleButtons.FirstOrDefault(button => button.Tag is ScreenInfo si && si.DisplayNumber == _preferences.PreferredDisplayNumber);
        ScreenInfo? selectedScreenInfo = null;
        if (preferredToggle != null)
        {
            preferredToggle.IsChecked = true;
            selectedScreenInfo = preferredToggle.Tag as ScreenInfo;
        }
        else if (_monitorToggleButtons.Count > 0)
        {
            var fallbackToggle = _monitorToggleButtons.FirstOrDefault(b => b.IsChecked == true) ?? _monitorToggleButtons[0];
            if (fallbackToggle.IsChecked != true)
            {
                fallbackToggle.IsChecked = true;
            }

            selectedScreenInfo = fallbackToggle.Tag as ScreenInfo;
        }

        if (selectedScreenInfo != null)
        {
            MoveWindowToScreen(selectedScreenInfo);
        }

        var preferredSpeed = Math.Abs(_preferences.DefaultScrollSpeed) < 0.01 ? 0.5 : _preferences.DefaultScrollSpeed;
        if (Math.Abs(_preferences.DefaultScrollSpeed - preferredSpeed) > 0.001)
        {
            _preferences.DefaultScrollSpeed = preferredSpeed;
        }
        SetSpeed(preferredSpeed, fromSlider: false);

        if (!string.IsNullOrWhiteSpace(_preferences.ArrowColorHex))
        {
            var arrowBrush = CreateBrushFromHex(_preferences.ArrowColorHex);
            if (arrowBrush is SolidColorBrush solid)
            {
                SetArrowColor(solid.Color, persist: false);
            }
        }

        var requestedScale = _preferences.ArrowScale <= 0 ? 1.0 : Math.Clamp(_preferences.ArrowScale, 0.5, 2.0);
        UpdateArrowScale(requestedScale, fromSlider: false, persist: false);

        var requestedMargin = _preferences.ArrowLeftPaddingExtra;
        if (Math.Abs(requestedMargin - 260) < 0.001)
        {
            requestedMargin = 12;
        }
    requestedMargin = Math.Clamp(requestedMargin, 0, 640);
        SetArrowPaddingExtra(requestedMargin, fromSlider: false, persist: false);

        _arrowNormalizedPosition = new MediaPoint(
            0,
            Clamp01(_preferences.ArrowVerticalOffset));

        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(ApplyNormalizedArrowPosition));

        if (!string.IsNullOrWhiteSpace(_preferences.LastScriptPath) && File.Exists(_preferences.LastScriptPath))
        {
            try
            {
                LoadDocument(_preferences.LastScriptPath);
                _currentDocumentPath = _preferences.LastScriptPath;
                SetStatus($"Ripristinato: {Path.GetFileName(_currentDocumentPath)}");
            }
            catch
            {
                SetStatus("Pronto: carica un file o scrivi il tuo copione.");
            }
        }
        else
        {
            SetStatus("Pronto: carica un file o scrivi il tuo copione.");
        }
    }

    /// <summary>
    /// Called by DisplayManager whenever screens are added, removed, or reconfigured.
    /// </summary>
    private void OnScreensChanged(IReadOnlyList<ScreenInfo> screens)
    {
        // Remember which screen was selected before rebuild
        var previousScreenName = _presenterWindow?.CurrentScreenDeviceName;

        RebuildMonitorToggles(screens, previousScreenName);

        // If presenter window was on a now-gone screen, re-home it
        if (_presenterWindow != null && !string.IsNullOrEmpty(previousScreenName) &&
            screens.All(s => !string.Equals(s.Screen.DeviceName, previousScreenName, StringComparison.OrdinalIgnoreCase)))
        {
            // Screen was removed — select best alternative
            var alt = screens.FirstOrDefault(s => !IsMainWindowScreen(s.Screen));
            if (alt != null)
            {
                MoveWindowToScreen(alt);
            }
            else
            {
                _presenterWindow.HideIfNeeded();
            }
        }
    }

    private void RebuildMonitorToggles(IReadOnlyList<ScreenInfo> screens, string? preferScreenDeviceName = null)
    {
        _screenInfos.Clear();
        _screenInfos.AddRange(screens);

        _monitorTogglePanel.Children.Clear();
        _monitorToggleButtons.Clear();

        var mainScreen = GetCurrentMainScreen();

        foreach (var info in _screenInfos)
        {
            var toggle = new ToggleButton
            {
                Content = info.DisplayLabel,
                Style = (Style)FindResource("MonitorToggleStyle"),
                Margin = _monitorTogglePanel.Children.Count == 0 ? new Thickness(0) : new Thickness(8, 0, 0, 0),
                Tag = info,
                MinWidth = 150,
                Padding = new Thickness(22, 10, 22, 10)
            };

            toggle.Checked += MonitorToggle_Checked;
            toggle.Unchecked += MonitorToggle_Unchecked;

            _monitorTogglePanel.Children.Add(toggle);
            _monitorToggleButtons.Add(toggle);
        }

        if (_monitorToggleButtons.Count > 0)
        {
            ToggleButton? targetToggle = null;

            // Try to preserve the previously selected screen
            if (!string.IsNullOrEmpty(preferScreenDeviceName))
            {
                targetToggle = _monitorToggleButtons.FirstOrDefault(button =>
                    button.Tag is ScreenInfo opt &&
                    string.Equals(opt.Screen.DeviceName, preferScreenDeviceName, StringComparison.OrdinalIgnoreCase));
            }

            // Fallback: pick first non-primary, non-main screen
            if (targetToggle == null && _screenInfos.Count > 1)
            {
                targetToggle = _monitorToggleButtons.FirstOrDefault(button =>
                    button.Tag is ScreenInfo opt && !opt.IsPrimary && opt.Screen.DeviceName != mainScreen.DeviceName);
            }

            targetToggle ??= _monitorToggleButtons[0];
            targetToggle.IsChecked = true;
        }

        if (_screenInfos.Count > 1)
        {
            SetStatus($"Schermi rilevati: {string.Join(", ", _screenInfos.Select(o => o.DisplayLabel))}");
        }
        else if (_screenInfos.Count == 1)
        {
            SetStatus($"Schermo attivo: {_screenInfos[0].DisplayLabel}");
        }
        else
        {
            SetStatus("Nessun monitor rilevato");
        }
    }

    private void MonitorToggle_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton toggle || toggle.Tag is not ScreenInfo option)
        {
            return;
        }

        _isSyncingMonitorToggle = true;
        foreach (var other in _monitorToggleButtons)
        {
            if (!ReferenceEquals(other, toggle))
            {
                other.IsChecked = false;
            }
        }
        _isSyncingMonitorToggle = false;

        var presenterShown = MoveWindowToScreen(option);
        if (presenterShown)
        {
            SetStatus($"Presenter su {option.DisplayLabel}");
        }
        else
        {
            SetStatus("Presenter nascosto: usa un display esterno per attivarlo.");
        }
    }

    private void MonitorToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_isSyncingMonitorToggle)
        {
            return;
        }

        if (_monitorToggleButtons.All(button => button.IsChecked != true) && sender is ToggleButton toggle)
        {
            toggle.IsChecked = true;
        }
    }

    private void NdiToggle_Checked(object sender, RoutedEventArgs e)
    {
        StartNdiStreaming();
    }

    private void NdiToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        StopNdiStreaming();
    }

    private bool MoveWindowToScreen(ScreenInfo option)
    {
        EnsurePresenterWindow();
        if (_presenterWindow == null)
        {
            return false;
        }

        if (IsMainWindowScreen(option.Screen))
        {
            _presenterWindow.HideIfNeeded();
            return false;
        }

        _presenterWindow.ShowOnScreen(option.Screen);
        SyncPresenterDocument();
        ApplyNormalizedArrowPosition();
        return true;
    }

    private bool IsMainWindowScreen(WF.Screen screen)
    {
        var current = GetCurrentMainScreen();
        return string.Equals(current.DeviceName, screen.DeviceName, StringComparison.OrdinalIgnoreCase);
    }

    private WF.Screen GetCurrentMainScreen()
    {
        var helper = new WindowInteropHelper(this);
        if (helper.Handle != IntPtr.Zero)
        {
            return WF.Screen.FromHandle(helper.Handle);
        }

        return WF.Screen.PrimaryScreen ?? WF.Screen.AllScreens.FirstOrDefault() ?? throw new InvalidOperationException("Nessun monitor disponibile.");
    }

    private void OpenFileButton_Click(object sender, RoutedEventArgs e)
    {
        OpenDocument();
    }

    private void OpenDocument()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Apri copione",
            Filter = "Documento Teleprompter (*.rstp)|*.rstp|Formati ricchi (*.rtf;*.xaml;*.xamlpackage)|*.rtf;*.xaml;*.xamlpackage|Testo e sottotitoli|*.txt;*.md;*.srt;*.vtt;*.log;*.csv;*.json;*.xml;*.html;*.htm;*.yaml;*.yml;*.ini;*.cfg;*.bat;*.ps1|Tutti i file|*.*",
            DefaultExt = ".rstp",
            AddExtension = true
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                LoadDocument(dialog.FileName);
                _contentScrollViewer.ScrollToTop();
                SetStatus($"Caricato: {Path.GetFileName(dialog.FileName)}");
            }
            catch (Exception ex)
            {
                MediaMessageBox.Show(this, $"Impossibile aprire il file.\n{ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void LoadDocument(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var range = new TextRange(_contentEditor.Document.ContentStart, _contentEditor.Document.ContentEnd);

        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        if (extension == ".rtf")
        {
            range.Load(fileStream, MediaDataFormats.Rtf);
        }
        else if (extension == ".xaml" || extension == ".xamlpackage" || extension == ".rstp")
        {
            range.Load(fileStream, MediaDataFormats.XamlPackage);
        }
        else
        {
            using var reader = new StreamReader(fileStream);
            var text = reader.ReadToEnd();
            SetPlainTextDocument(text);
        }

        _contentEditor.CaretPosition = _contentEditor.Document.ContentStart;
        _currentDocumentPath = filePath;
        if (_preferences != null)
        {
            _preferences.LastScriptPath = filePath;
        }
        ApplyArrowSafePadding();
        SavePreferences();
    }

    private void SaveDocumentButton_Click(object sender, RoutedEventArgs e)
    {
        SaveDocument();
    }

    private void SaveDocument()
    {
        var initialDirectory = !string.IsNullOrEmpty(_currentDocumentPath) ? Path.GetDirectoryName(_currentDocumentPath) : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Salva copione",
            Filter = "Documento Teleprompter (*.rstp)|*.rstp|Rich Text Format (*.rtf)|*.rtf|FlowDocument XAML (*.xaml)|*.xaml|Testo semplice (*.txt)|*.txt|Tutti i file|*.*",
            DefaultExt = ".rstp",
            AddExtension = true,
            FileName = !string.IsNullOrEmpty(_currentDocumentPath) ? Path.GetFileName(_currentDocumentPath) : "Copione.rstp",
            InitialDirectory = initialDirectory
        };

        if (!string.IsNullOrEmpty(_currentDocumentPath))
        {
            dialog.FilterIndex = Path.GetExtension(_currentDocumentPath)?.ToLowerInvariant() switch
            {
                ".rstp" => 1,
                ".rtf" => 2,
                ".xaml" => 3,
                ".txt" => 4,
                _ => 1
            };
        }

        if (dialog.ShowDialog(this) == true)
        {
            TrySaveDocument(dialog.FileName);
        }
        else
        {
            SetStatus("Salvataggio annullato.");
        }
    }

    private void TrySaveDocument(string filePath)
    {
        try
        {
            var extension = NormalizeExtension(filePath);

            if (string.IsNullOrWhiteSpace(extension))
            {
                filePath = Path.ChangeExtension(filePath, ".rstp");
                extension = ".rstp";
            }

            var preserveFormatting = SupportsRichFormatting(extension);
            var forcedRichFormat = false;

            if (!preserveFormatting)
            {
                var question = "Il formato scelto non conserva la formattazione. Vuoi salvarlo come Documento Teleprompter (.rstp) per mantenere colori, font e stile?";
                var choice = MediaMessageBox.Show(this, question, "Formato limitato", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (choice == MessageBoxResult.Yes)
                {
                    filePath = Path.ChangeExtension(filePath, ".rstp");
                    extension = ".rstp";
                    preserveFormatting = true;
                    forcedRichFormat = true;
                }
            }

            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            var range = new TextRange(_contentEditor.Document.ContentStart, _contentEditor.Document.ContentEnd);

            switch (extension)
            {
                case ".rstp":
                case ".xamlpackage":
                    range.Save(fileStream, MediaDataFormats.XamlPackage);
                    break;
                case ".xaml":
                    range.Save(fileStream, MediaDataFormats.Xaml);
                    break;
                case ".rtf":
                    range.Save(fileStream, MediaDataFormats.Rtf);
                    break;
                default:
                    var text = ExtractPlainText(range);
                    using (var writer = new StreamWriter(fileStream, Encoding.UTF8, leaveOpen: false))
                    {
                        writer.Write(text);
                    }
                    preserveFormatting = false;
                    break;
            }

            _currentDocumentPath = filePath;
            if (_preferences != null)
            {
                _preferences.LastScriptPath = filePath;
            }

            if (forcedRichFormat)
            {
                SetStatus($"Salvato come documento completo: {Path.GetFileName(filePath)}");
            }
            else if (!preserveFormatting)
            {
                SetStatus($"Salvato (solo testo): {Path.GetFileName(filePath)}");
            }
            else
            {
                SetStatus($"Salvato: {Path.GetFileName(filePath)}");
            }
            SavePreferences();
        }
        catch (Exception ex)
        {
            MediaMessageBox.Show(this, $"Impossibile salvare il file.\n{ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string NormalizeExtension(string path)
    {
        var extension = Path.GetExtension(path);
        return string.IsNullOrWhiteSpace(extension) ? string.Empty : extension.ToLowerInvariant();
    }

    private static bool SupportsRichFormatting(string extension)
    {
        return extension is ".rstp" or ".xaml" or ".xamlpackage" or ".rtf";
    }

    private static string ExtractPlainText(TextRange range)
    {
        var text = range.Text ?? string.Empty;
        return text.TrimEnd('\r', '\n');
    }

    private void SaveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = true;
        e.Handled = true;
    }

    private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentDocumentPath))
        {
            TrySaveDocument(_currentDocumentPath);
        }
        else
        {
            SaveDocument();
        }
    }

    private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        OpenDocument();
    }

    private void NewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        NewDocumentButton_Click(sender, new RoutedEventArgs());
    }

    private void SetPlainTextDocument(string text)
    {
        var document = _contentEditor.Document;
    document.Blocks.Clear();
    document.PagePadding = new Thickness(BasePagePadding);
        document.TextAlignment = TextAlignment.Center;
        document.LineHeight = _contentEditor.FontSize * 1.2;

        using var reader = new StringReader(text);
        string? line;
        Paragraph? paragraph = null;

        while ((line = reader.ReadLine()) is not null)
        {
            if (paragraph is null)
            {
                paragraph = CreateParagraph();
            }

            if (line.Length == 0)
            {
                document.Blocks.Add(paragraph);
                paragraph = null;
                continue;
            }

            if (paragraph.Inlines.Count > 0)
            {
                paragraph.Inlines.Add(new LineBreak());
            }

            paragraph.Inlines.Add(new Run(line));
        }

        if (paragraph is not null)
        {
            document.Blocks.Add(paragraph);
        }

        if (!document.Blocks.Any())
        {
            document.Blocks.Add(CreateParagraph());
        }

        ApplyArrowSafePadding();
    }

    private Paragraph CreateParagraph()
    {
        return new Paragraph
        {
            TextAlignment = TextAlignment.Center,
            LineHeight = _contentEditor.Document.LineHeight
        };
    }

    private void NewDocumentButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyDocumentDefaults();
        _contentScrollViewer.ScrollToTop();
        SetStatus("Nuovo copione pronto.");
        _currentDocumentPath = null;
        SavePreferences();
    }

    private void BackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        using var colorDialog = new WF.ColorDialog { FullOpen = true };
        if (colorDialog.ShowDialog() == WF.DialogResult.OK)
        {
            var brush = new SolidColorBrush(MediaColor.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
            _contentEditor.Document.Background = brush;
            _contentEditor.Background = MediaBrushes.Transparent;
            SavePreferences();
                SyncPresenterDocument();
        }
    }

    private void ForegroundButton_Click(object sender, RoutedEventArgs e)
    {
        using var colorDialog = new WF.ColorDialog { FullOpen = true };
        if (colorDialog.ShowDialog() == WF.DialogResult.OK)
        {
            var brush = new SolidColorBrush(MediaColor.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
            SetDocumentForeground(brush);
        }
    }

    private void FontButton_Click(object sender, RoutedEventArgs e)
    {
        using var fontDialog = new WF.FontDialog
        {
            ShowEffects = true,
            FontMustExist = true
        };

        if (fontDialog.ShowDialog() == WF.DialogResult.OK)
        {
            ApplyFont(fontDialog.Font);
        }
    }

    private void ApplyFont(SD.Font font)
    {
        ApplyFont(font.Name, font.SizeInPoints, font.Bold, font.Italic, font.Underline, font.Strikeout);
    }

    private void ApplyFont(string fontFamilyName, double fontSizePoints, bool isBold, bool isItalic, bool underline, bool strikeout)
    {
        var points = fontSizePoints <= 0 ? 72 : fontSizePoints;
        var wpfSize = ConvertPointsToWpf(points);
        var family = new MediaFontFamily(fontFamilyName);

        _contentEditor.FontFamily = family;
        _contentEditor.FontSize = wpfSize;
        _contentEditor.FontWeight = isBold ? FontWeights.Bold : FontWeights.Regular;
        _contentEditor.FontStyle = isItalic ? FontStyles.Italic : FontStyles.Normal;

        TextDecorationCollection? decorations = null;
        if (underline)
        {
            decorations = TextDecorations.Underline;
        }
        else if (strikeout)
        {
            decorations = TextDecorations.Strikethrough;
        }

        ApplyToDocument(range =>
        {
            range.ApplyPropertyValue(TextElement.FontFamilyProperty, family);
            range.ApplyPropertyValue(TextElement.FontSizeProperty, wpfSize);
            range.ApplyPropertyValue(TextElement.FontWeightProperty, _contentEditor.FontWeight);
            range.ApplyPropertyValue(TextElement.FontStyleProperty, _contentEditor.FontStyle);
            range.ApplyPropertyValue(Inline.TextDecorationsProperty, decorations);
        });

        _contentEditor.Document.LineHeight = wpfSize * 1.2;
        SyncFontSizeSelectionFromEditor();
        SavePreferences();
    }

    private void SyncFontSizeSelectionFromEditor()
    {
        if (!_fontSizeComboBox.IsLoaded)
        {
            return;
        }

        var points = Math.Round(ConvertWpfToPoints(_contentEditor.FontSize));
        var target = points.ToString("F0");

        _isUpdatingFontSizeSelection = true;
        _fontSizeComboBox.SelectedValue = target;
        _isUpdatingFontSizeSelection = false;
    }

    private void SetDocumentForeground(MediaBrush brush)
    {
        _contentEditor.Foreground = brush;
        ApplyToDocument(range => range.ApplyPropertyValue(TextElement.ForegroundProperty, brush));
        SavePreferences();
        SyncPresenterDocument();
    }

    private void ApplyArrowSafePadding()
    {
        if (_contentEditor?.Document is not FlowDocument document)
        {
            return;
        }

    var mainPadding = ComputeMainDocumentPadding();
    document.PagePadding = mainPadding;
    _contentEditor.Padding = mainPadding;

    var presenterPadding = GetPresenterPagePadding(mainPadding);
    _presenterWindow?.SetPagePadding(presenterPadding);

        UpdateLeftMarginDisplay();
        ApplyNormalizedArrowPosition();
        _contentEditor.UpdateLayout();
        _contentScrollViewer.UpdateLayout();
        SyncPresenterDocument();
    }

    private Thickness ComputeMainDocumentPadding()
    {
        var basePadding = BasePagePadding;
        var arrowPadding = ComputeArrowSidePadding();

        return new Thickness(arrowPadding, basePadding, basePadding, basePadding);
    }

    private Thickness GetPresenterPagePadding(Thickness mainPadding)
    {
        if (_mirrorToggle?.IsChecked == true)
        {
            // In mirror mode: swap left ↔ right so the arrow margin becomes the right edge
            var arrowPadding = ComputeArrowSidePadding();
            return new Thickness(BasePagePadding, mainPadding.Top, arrowPadding, mainPadding.Bottom);
        }

        return mainPadding;
    }

    private double ComputeArrowSidePadding()
    {
        var arrowRightEdge = ArrowLeftOffset + GetArrowEffectiveWidth();
        var leftPadding = arrowRightEdge + _arrowPaddingExtra;
        if (double.IsNaN(leftPadding) || double.IsInfinity(leftPadding))
        {
            leftPadding = 0;
        }

        return Math.Max(0, leftPadding);
    }

    private void EnsurePresenterWindow()
    {
        if (_presenterWindow != null)
        {
            return;
        }

        _presenterWindow = new PresenterWindow
        {
            Owner = this
        };

        _presenterWindow.Hide();
        UpdatePresenterMirror();
        UpdatePresenterArrowAppearance();
        SyncPresenterDocument();
    }

    /// <summary>
    /// For explicit sync (e.g., loading new document, showing on new screen) — immediate.
    /// </summary>
    private void SyncPresenterDocument()
    {
        if (_presenterSync != null)
        {
            _presenterSync.SyncNow();
            return;
        }

        // Fallback if service not yet initialized
        if (_presenterWindow == null || _contentEditor?.Document is null)
        {
            return;
        }

        try
        {
            var clone = CloneDocument(_contentEditor.Document);
            _presenterWindow.SetDocument(clone);
            var presenterPadding = GetPresenterPagePadding(_contentEditor.Document.PagePadding);
            _presenterWindow.SetPagePadding(presenterPadding);
            _presenterWindow.SetVerticalOffset(_contentScrollViewer.VerticalOffset);
            UpdatePresenterArrowAppearance();
            UpdatePresenterMirror();
        }
        catch
        {
            // Ignore serialization issues to avoid disrupting the operator
        }
    }

    /// <summary>
    /// For typing — debounced via PresenterSyncService (300ms).
    /// </summary>
    private void RequestPresenterSync()
    {
        if (_presenterSync != null)
        {
            _presenterSync.MarkDirty();
        }
        else
        {
            SyncPresenterDocument();
        }
    }

    private static FlowDocument CloneDocument(FlowDocument source)
    {
        var xaml = XamlWriter.Save(source);
        using var stringReader = new StringReader(xaml);
        using var xmlReader = XmlReader.Create(stringReader);
        return (FlowDocument)XamlReader.Load(xmlReader);
    }

    private void UpdatePresenterMirror()
    {
        _presenterWindow?.SetMirror(_mirrorToggle.IsChecked == true);
    }

    private void UpdatePresenterArrowAppearance()
    {
        if (_presenterWindow == null)
        {
            return;
        }

        var fill = (_arrowShape.Fill as SolidColorBrush)?.Color ?? MediaColor.FromRgb(234, 179, 8);
        var stroke = (_arrowShape.Stroke as SolidColorBrush)?.Color ?? MediaColor.FromRgb(255, 240, 138);
        _presenterWindow.SetArrowColor(fill, stroke);
        _presenterWindow.SetArrowScale(_arrowScale);
        _presenterWindow.SetArrowNormalizedY(_arrowNormalizedPosition.Y);
    }

    private double GetArrowEffectiveWidth()
    {
        var baseWidth = _arrowContainer?.Width ?? ArrowBaseWidth;
        return baseWidth * _arrowScale;
    }

    internal bool IsEditMode => _editModeToggle?.IsChecked != false;

    private void ApplyEditMode(bool isEditMode)
    {
        if (_contentEditor is null)
        {
            return;
        }

        _contentEditor.IsReadOnly = !isEditMode;
        _contentEditor.IsReadOnlyCaretVisible = isEditMode;
        _contentEditor.IsHitTestVisible = isEditMode;
        _contentEditor.Focusable = isEditMode;
    _contentEditor.Cursor = isEditMode ? MediaCursors.IBeam : MediaCursors.Arrow;

        if (!isEditMode)
        {
            Keyboard.ClearFocus();
            FocusManager.SetFocusedElement(this, this);
        }
    }

    private static SolidColorBrush? CreateBrushFromHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return null;
        }

        try
        {
            if (MediaColorConverter.ConvertFromString(hex) is MediaColor color)
            {
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                return brush;
            }
        }
        catch
        {
            // ignore invalid color strings
        }

        return null;
    }

    private void ApplyToDocument(Action<TextRange> action)
    {
        var document = _contentEditor.Document;
        var range = new TextRange(document.ContentStart, document.ContentEnd);
        var caret = _contentEditor.CaretPosition;

        action(range);

        if (caret != null)
        {
            _contentEditor.CaretPosition = caret;
        }
    }

    private void ContentEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isApplyingPreferences)
        {
            return;
        }

        RequestPresenterSync();
    }

    private void ContentScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        _presenterWindow?.SetVerticalOffset(e.VerticalOffset);
        NotifyOscPosition();
    }

    private void PlayPauseToggle_Checked(object sender, RoutedEventArgs e)
    {
        if (_editModeToggle?.IsChecked == true)
        {
            _editModeToggle.IsChecked = false;
        }

        var desiredSpeed = _scrollSpeed;
        if (Math.Abs(desiredSpeed) < 0.05)
        {
            desiredSpeed = _speedSlider?.Value ?? 0;
        }

        if (Math.Abs(desiredSpeed) < 0.05)
        {
            desiredSpeed = 0.5;
        }

        SetSpeed(desiredSpeed, fromSlider: false);

        _scrollTimer.Stop();
        _scrollStopwatch.Restart();
        _scrollTimer.Start();

        _contentScrollViewer?.Focus();
        SetStatus("Scorrimento attivo");
        NotifyOscPlaybackState();
    }

    private void PlayPauseToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        _scrollTimer.Stop();
        _scrollStopwatch.Stop();
        SetStatus("In pausa");
        NotifyOscPlaybackState();
    }

    private void MirrorToggle_Checked(object sender, RoutedEventArgs e)
    {
        ApplyMirrorState();
        SavePreferences();
    }

    private void MirrorToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        ApplyMirrorState();
        SavePreferences();
    }

    private void TopMostToggle_Checked(object sender, RoutedEventArgs e)
    {
        Topmost = true;
        SavePreferences();
    }

    private void TopMostToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        Topmost = false;
        SavePreferences();
    }

    private void OnScrollTimerTick(object? sender, EventArgs e)
    {
        if (_scrollSpeed == 0 || _contentScrollViewer == null)
        {
            return;
        }

        // Delta-time compensation: scale scroll by actual elapsed time vs ideal 16ms
        var elapsed = _scrollStopwatch.Elapsed.TotalMilliseconds;
        _scrollStopwatch.Restart();

        // Avoid extreme jumps after long stalls (e.g. system sleep)
        if (elapsed <= 0 || elapsed > 500)
        {
            elapsed = 16;
        }

        var dtFactor = elapsed / 16.0;
        var scrollDelta = _scrollSpeed * dtFactor;

        var previousOffset = _contentScrollViewer.VerticalOffset;
        var maxOffset = _contentScrollViewer.ScrollableHeight;
        if (double.IsNaN(maxOffset) || maxOffset < 0)
        {
            maxOffset = 0;
        }

        var clampedTarget = maxOffset <= 0
            ? previousOffset + scrollDelta
            : Math.Clamp(previousOffset + scrollDelta, 0, maxOffset);

        _contentScrollViewer.ScrollToVerticalOffset(clampedTarget);
        _contentScrollViewer.UpdateLayout();

        var actualOffset = _contentScrollViewer.VerticalOffset;
        if (Math.Abs(actualOffset - previousOffset) < 0.05)
        {
            _scrollTimer.Stop();
            _scrollStopwatch.Stop();
            _playPauseToggle.IsChecked = false;
            SetStatus(_scrollSpeed > 0 ? "Fine del testo" : "Inizio del testo");
        }
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        HandlePresentationKey(e);
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        HandlePresentationKey(e);
    }

    private void HandlePresentationKey(System.Windows.Input.KeyEventArgs e)
    {
        if (IsEditMode)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Up:
            case Key.Down:
            case Key.Left:
            case Key.Right:
                e.Handled = true;
                break;
        }

        switch (e.Key)
        {
            case Key.Up:
                AdjustSpeed(SpeedStep);
                break;
            case Key.Down:
                AdjustSpeed(-SpeedStep);
                break;
            case Key.Left:
            case Key.Right:
                if (Math.Abs(_scrollSpeed) > 0.01)
                {
                    SetSpeed(0, fromSlider: false);
                    SetStatus("Velocità azzerata");
                }
                break;
            case Key.Space:
                _playPauseToggle.IsChecked = !_playPauseToggle.IsChecked;
                e.Handled = true;
                break;
            case Key.Home:
                _contentScrollViewer.ScrollToTop();
                e.Handled = true;
                break;
            case Key.End:
                _contentScrollViewer.ScrollToEnd();
                e.Handled = true;
                break;
        }
    }

    private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (IsEditMode)
        {
            return;
        }

        if (_contentEditor.IsKeyboardFocusWithin && _playPauseToggle.IsChecked != true)
        {
            return;
        }

        var delta = e.Delta > 0 ? SpeedStep : -SpeedStep;
        AdjustSpeed(delta);
        e.Handled = true;
    }

    internal void AdjustSpeed(double delta)
    {
        SetSpeed(_scrollSpeed + delta, fromSlider: false);
    }

    internal void SetSpeed(double newSpeed, bool fromSlider)
    {
        _scrollSpeed = Math.Clamp(newSpeed, -MaxSpeed, MaxSpeed);
        if (Math.Abs(_scrollSpeed) < 0.05)
        {
            _scrollSpeed = 0;
        }

        UpdateSpeedText();

        if (!fromSlider && _speedSlider != null)
        {
            _isUpdatingSpeedSlider = true;
            _speedSlider.Value = _scrollSpeed;
            _isUpdatingSpeedSlider = false;
        }

        if (_scrollSpeed == 0)
        {
            if (_playPauseToggle.IsChecked == true)
            {
                _scrollTimer.Stop();
                SetStatus("Velocità 0: pausa");
            }
        }
        else if (_playPauseToggle.IsChecked == true)
        {
            if (!_scrollTimer.IsEnabled)
            {
                _scrollTimer.Start();
            }

            SetStatus(_scrollSpeed > 0 ? "Scorrimento attivo" : "Scorrimento inverso");
        }

        NotifyOscSpeed();

        SavePreferences();
    }

    private void UpdateSpeedText()
    {
        if (_speedText == null)
        {
            return;
        }

        _speedText.Text = $"Velocità: {_scrollSpeed:F2}";
    }

    private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingSpeedSlider || !IsLoaded || sender is not Slider slider || !ReferenceEquals(slider, _speedSlider))
        {
            return;
        }

        SetSpeed(e.NewValue, fromSlider: true);
    }

    private void ArrowSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingArrowSize)
        {
            return;
        }

        if (!IsLoaded)
        {
            _arrowScale = Math.Clamp(e.NewValue, 0.5, 2.0);
            ApplyArrowSafePadding();
            return;
        }

        UpdateArrowScale(e.NewValue, fromSlider: true);
    }

    private void LeftMarginSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingLeftMargin)
        {
            return;
        }

        SetArrowPaddingExtra(e.NewValue, fromSlider: true);
    }

    private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingFontSizeSelection)
        {
            return;
        }

        if (_fontSizeComboBox.SelectedValue is string value && double.TryParse(value, out var points))
        {
            var wpfSize = ConvertPointsToWpf(points);
            _contentEditor.FontSize = wpfSize;
            ApplyToDocument(range => range.ApplyPropertyValue(TextElement.FontSizeProperty, wpfSize));
            _contentEditor.Document.LineHeight = wpfSize * 1.2;
            SavePreferences();
        }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        _displayManager?.Dispose();
        _presenterSync?.Dispose();
        _companionBridge?.Dispose();
        _ndiTransmitter?.Dispose();
        _oscBridge?.Stop();

        CapturePreferences();

        // Cancel any pending debounced save and save the freshly captured preferences directly.
        // Flush() would save stale _pending data, not the just-captured _preferences.
        _debouncedPrefs?.Dispose();
        PreferencesService.Save(_preferences);
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(MediaDataFormats.FileDrop))
        {
            var files = e.Data.GetData(MediaDataFormats.FileDrop) as string[] ?? Array.Empty<string>();
            e.Effects = GetFirstSupportedFile(files) != null ? DragDropEffects.Copy : DragDropEffects.None;
        }
        else if (e.Data.GetDataPresent(MediaDataFormats.UnicodeText))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(MediaDataFormats.FileDrop))
        {
            var files = e.Data.GetData(MediaDataFormats.FileDrop) as string[] ?? Array.Empty<string>();
            var path = GetFirstSupportedFile(files);
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    LoadDocument(path);
                    _contentScrollViewer.ScrollToTop();
                    SetStatus($"Caricato da drag & drop: {Path.GetFileName(path)}");
                }
                catch (Exception ex)
                {
                    MediaMessageBox.Show(this, $"Impossibile aprire il file trascinato.\n{ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            e.Handled = true;
            return;
        }

        if (e.Data.GetDataPresent(MediaDataFormats.UnicodeText))
        {
            if (e.Data.GetData(MediaDataFormats.UnicodeText) is string text && !string.IsNullOrWhiteSpace(text))
            {
                SetPlainTextDocument(text);
                _currentDocumentPath = null;
                SetStatus("Testo applicato dal trascinamento.");
                SavePreferences();
            }
        }

        e.Handled = true;
    }

    private void ArrowCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_arrowCanvas == null || _arrowContainer == null)
        {
            return;
        }

        if (e.NewSize.Width <= 0 || e.NewSize.Height <= 0)
        {
            return;
        }

        ApplyArrowSafePadding();
        ApplyNormalizedArrowPosition();
        UpdatePresenterArrowAppearance();
    }

    private void ArrowContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingArrow = true;
        _arrowDragStart = e.GetPosition(_arrowCanvas);
        _arrowInitialOffset = new MediaPoint(double.IsNaN(Canvas.GetLeft(_arrowContainer)) ? 0 : Canvas.GetLeft(_arrowContainer),
            double.IsNaN(Canvas.GetTop(_arrowContainer)) ? 0 : Canvas.GetTop(_arrowContainer));
        _arrowContainer.CaptureMouse();
        e.Handled = true;
    }

    private void ArrowContainer_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingArrow)
        {
            return;
        }

        if (_arrowCanvas == null || _arrowContainer == null)
        {
            return;
        }

        var current = e.GetPosition(_arrowCanvas);
        var delta = new MediaPoint(current.X - _arrowDragStart.X, current.Y - _arrowDragStart.Y);
        var targetLeft = _arrowInitialOffset.X + delta.X;
        var targetTop = _arrowInitialOffset.Y + delta.Y;
        MoveArrowTo(targetLeft, targetTop);
        e.Handled = true;
    }

    private void ArrowContainer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDraggingArrow)
        {
            return;
        }

        _isDraggingArrow = false;
        _arrowContainer.ReleaseMouseCapture();
        UpdateArrowNormalizedFromCurrent();
        UpdatePresenterArrowAppearance();
        SetStatus("Freccia aggiornata");
        SavePreferences();
        e.Handled = true;
    }

    private void ArrowColorButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WF.ColorDialog
        {
            AllowFullOpen = true,
            FullOpen = true
        };

        if (_arrowShape?.Fill is SolidColorBrush existing)
        {
            dialog.Color = SD.Color.FromArgb(existing.Color.A, existing.Color.R, existing.Color.G, existing.Color.B);
        }

        if (dialog.ShowDialog() == WF.DialogResult.OK)
        {
            var mediaColor = MediaColor.FromArgb(dialog.Color.A, dialog.Color.R, dialog.Color.G, dialog.Color.B);
            SetArrowColor(mediaColor);
            SetStatus("Colore freccia aggiornato");
        }
    }

    private void ApplyMirrorState()
    {
        ApplyArrowSafePadding();
        UpdatePresenterMirror();
        NotifyOscMirror();
        UpdateLeftMarginDisplay();
    }

    private void EditModeToggle_Checked(object sender, RoutedEventArgs e)
    {
        OnEditModeToggleChanged(true);
    }

    private void EditModeToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        OnEditModeToggleChanged(false);
    }

    private void OnEditModeToggleChanged(bool isChecked)
    {
        _pendingEditMode = isChecked;

        if (_contentEditor is null)
        {
            return;
        }

        ApplyEditMode(isChecked);

        if (_isUpdatingEditToggle || _isApplyingPreferences)
        {
            return;
        }

        if (isChecked)
        {
            _contentEditor.Focus();
            SetStatus("Modalità modifica attiva");
        }
        else
        {
            SetStatus("Modalità presentazione attiva");
        }

        SavePreferences();
    }

    private string? GetFirstSupportedFile(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            var extension = Path.GetExtension(path);
            if (!string.IsNullOrWhiteSpace(extension) && _supportedExtensions.Contains(extension))
            {
                return path;
            }
        }

        return paths.FirstOrDefault();
    }

    private void CapturePreferences()
    {
        if (_contentEditor == null)
        {
            return;
        }

        _preferences ??= new UserPreferences();

        UpdateArrowNormalizedFromCurrent();

        if (_contentEditor.Document.Background is SolidColorBrush docBackground)
        {
            _preferences.DocumentBackgroundHex = docBackground.Color.ToString();
        }

        if (_contentEditor.Foreground is SolidColorBrush foreground)
        {
            _preferences.TextForegroundHex = foreground.Color.ToString();
        }

        _preferences.FontFamily = _contentEditor.FontFamily?.Source;
        _preferences.FontSizePoints = ConvertWpfToPoints(_contentEditor.FontSize);
        _preferences.IsBold = _contentEditor.FontWeight == FontWeights.Bold;
        _preferences.IsItalic = _contentEditor.FontStyle == FontStyles.Italic;

        try
        {
            var range = new TextRange(_contentEditor.Document.ContentStart, _contentEditor.Document.ContentEnd);
            var decorations = range.GetPropertyValue(Inline.TextDecorationsProperty);
            _preferences.UseUnderline = decorations is TextDecorationCollection collection && collection == TextDecorations.Underline;
        }
        catch
        {
            _preferences.UseUnderline = false;
        }

        _preferences.DefaultScrollSpeed = _scrollSpeed;
        _preferences.MirrorEnabled = _mirrorToggle.IsChecked == true;
        _preferences.TopMostEnabled = _topMostToggle.IsChecked == true;
        _preferences.LastScriptPath = _currentDocumentPath;

        var selectedMonitor = _monitorToggleButtons.FirstOrDefault(button => button.IsChecked == true);
        _preferences.PreferredDisplayNumber = selectedMonitor?.Tag is ScreenInfo screenInfo ? screenInfo.DisplayNumber : 0;
        _preferences.EditModeEnabled = IsEditMode;
        if (_arrowShape != null && _arrowShape.Fill is SolidColorBrush arrowBrush)
        {
            _preferences.ArrowColorHex = arrowBrush.Color.ToString();
        }

        _preferences.ArrowScale = _arrowScale;
        _preferences.ArrowHorizontalOffset = 0;
        _preferences.ArrowVerticalOffset = _arrowNormalizedPosition.Y;
        _preferences.ArrowLeftPaddingExtra = _arrowPaddingExtra;
    }

    private void SavePreferences()
    {
        if (_isApplyingPreferences)
        {
            return;
        }

        CapturePreferences();

        if (_debouncedPrefs != null)
        {
            _debouncedPrefs.SaveDebounced(_preferences);
        }
        else
        {
            PreferencesService.Save(_preferences);
        }
    }

    private static double ConvertPointsToWpf(double points) => points * 96.0 / 72.0;
    private static double ConvertWpfToPoints(double wpfSize) => wpfSize * 72.0 / 96.0;

    internal void SetStatus(string message)
    {
        _statusText.Text = message;
    }

    /// <summary>
    /// Sets the play/pause state from external sources (Companion, OSC).
    /// </summary>
    internal void SetPlayState(bool playing)
    {
        if (_playPauseToggle != null)
        {
            _playPauseToggle.IsChecked = playing;
        }
    }

    private void ApplyNormalizedArrowPosition()
    {
        if (_arrowCanvas == null || _arrowContainer == null)
        {
            return;
        }

        if (_arrowCanvas.ActualWidth <= 0 || _arrowCanvas.ActualHeight <= 0)
        {
            return;
        }

        var arrowHeight = _arrowContainer.ActualHeight > 0 ? _arrowContainer.ActualHeight : _arrowContainer.Height;
        var maxTop = Math.Max(0, _arrowCanvas.ActualHeight - arrowHeight);
        var top = maxTop * Clamp01(_arrowNormalizedPosition.Y);

        MoveArrowTo(ArrowLeftOffset, top);
        _presenterWindow?.SetArrowNormalizedY(_arrowNormalizedPosition.Y);
    }

    private void MoveArrowTo(double left, double top)
    {
        if (_arrowContainer == null || _arrowCanvas == null)
        {
            return;
        }

        var arrowHeight = _arrowContainer.ActualHeight > 0 ? _arrowContainer.ActualHeight : _arrowContainer.Height;
        var arrowWidth = _arrowContainer.ActualWidth > 0 ? _arrowContainer.ActualWidth : _arrowContainer.Width;
        var maxTop = Math.Max(0, _arrowCanvas.ActualHeight - arrowHeight);
        top = double.IsNaN(top) ? 0 : Math.Clamp(top, 0, maxTop);

        var maxLeft = Math.Max(ArrowLeftOffset, _arrowCanvas.ActualWidth - arrowWidth - ArrowLeftOffset);
        var arrowLeft = double.IsNaN(left) ? ArrowLeftOffset : Math.Clamp(left, ArrowLeftOffset, maxLeft);

        if (_mirrorToggle?.IsChecked == true)
        {
            arrowLeft = ArrowLeftOffset;
        }

        Canvas.SetLeft(_arrowContainer, arrowLeft);
        Canvas.SetTop(_arrowContainer, top);

        var normalized = maxTop > 0 ? top / maxTop : 0;
        _presenterWindow?.SetArrowNormalizedY(Clamp01(normalized));
    }

    private void UpdateArrowScale(double scale, bool fromSlider, bool persist = true)
    {
        var clamped = Math.Clamp(scale, 0.5, 2.0);
        _arrowScale = clamped;

        if (_arrowScaleTransform != null)
        {
            _arrowScaleTransform.ScaleX = clamped;
            _arrowScaleTransform.ScaleY = clamped;
        }

        if (!fromSlider && _arrowSizeSlider != null)
        {
            _isUpdatingArrowSize = true;
            _arrowSizeSlider.Value = clamped;
            _isUpdatingArrowSize = false;
        }

        if (persist && IsLoaded)
        {
            SavePreferences();
        }

        ApplyArrowSafePadding();
        UpdatePresenterArrowAppearance();
    }

    private void SetArrowPaddingExtra(double value, bool fromSlider, bool persist = true)
    {
        var clamped = Math.Clamp(value, 0, 640);
        _arrowPaddingExtra = clamped;

        if (!fromSlider && _leftMarginSlider != null)
        {
            _isUpdatingLeftMargin = true;
            _leftMarginSlider.Value = clamped;
            _isUpdatingLeftMargin = false;
        }

        if (persist)
        {
            _preferences.ArrowLeftPaddingExtra = _arrowPaddingExtra;
        }

        UpdateLeftMarginDisplay();
        ApplyArrowSafePadding();

        if (persist && IsLoaded)
        {
            SavePreferences();
        }
    }

    private void UpdateLeftMarginDisplay()
    {
        if (_leftMarginValueText != null)
        {
            var padding = ComputeArrowSidePadding();
            var label = _mirrorToggle?.IsChecked == true ? "Margine destro" : "Margine sinistro";
            _leftMarginValueText.Text = $"{label}: {Math.Round(padding)} px";
        }
    }

    internal void HandleOscMessage(string address, IList<object> args)
    {
        switch (address)
        {
            case "/teleprompter/start":
            case "/teleprompter/play":
                if (_playPauseToggle != null)
                {
                    _playPauseToggle.IsChecked = true;
                }
                break;

            case "/teleprompter/stop":
            case "/teleprompter/pause":
                if (_playPauseToggle != null)
                {
                    _playPauseToggle.IsChecked = false;
                }
                break;

            case "/teleprompter/reset":
                _contentScrollViewer?.ScrollToTop();
                NotifyOscPosition();
                SetStatus("OSC: riposizionamento all'inizio");
                break;

            case "/teleprompter/speed":
                if (args.Count > 0 && TryGetDouble(args[0], out var absoluteSpeed))
                {
                    SetSpeed(absoluteSpeed, fromSlider: false);
                }
                break;

            case "/teleprompter/speed/increase":
                AdjustSpeed(SpeedStep);
                break;

            case "/teleprompter/speed/decrease":
                AdjustSpeed(-SpeedStep);
                break;

            case "/teleprompter/font/size":
                if (args.Count > 0 && TryGetDouble(args[0], out var fontPoints))
                {
                    SetFontSizePoints(fontPoints);
                }
                break;

            case "/teleprompter/font/increase":
                SetFontSizePoints(GetCurrentFontPoints() + 2);
                break;

            case "/teleprompter/font/decrease":
                SetFontSizePoints(Math.Max(20, GetCurrentFontPoints() - 2));
                break;

            case "/teleprompter/position":
                if (args.Count > 0 && TryGetDouble(args[0], out var ratio))
                {
                    SetScrollRatio(ratio);
                }
                break;

            case "/teleprompter/jump/top":
                _contentScrollViewer?.ScrollToTop();
                NotifyOscPosition();
                break;

            case "/teleprompter/jump/bottom":
                _contentScrollViewer?.ScrollToBottom();
                NotifyOscPosition();
                break;

            case "/teleprompter/mirror":
                if (args.Count > 0 && TryGetBool(args[0], out var enabled))
                {
                    if (_mirrorToggle != null)
                    {
                        _mirrorToggle.IsChecked = enabled;
                    }
                }
                break;

            case "/teleprompter/mirror/toggle":
                if (_mirrorToggle != null)
                {
                    _mirrorToggle.IsChecked = !(_mirrorToggle.IsChecked == true);
                }
                break;

            case "/teleprompter/status/request":
                SendOscStatusSnapshot();
                break;

            case "/ndi/start":
                if (_ndiToggle != null)
                {
                    _ndiToggle.IsChecked = true;
                }
                StartNdiStreaming();
                break;

            case "/ndi/stop":
                if (_ndiToggle != null)
                {
                    _ndiToggle.IsChecked = false;
                }
                StopNdiStreaming();
                break;

            case "/ndi/toggle":
                if (_ndiToggle != null)
                {
                    _ndiToggle.IsChecked = !(_ndiToggle.IsChecked == true);
                }
                else if (_ndiTransmitter?.IsRunning == true)
                {
                    StopNdiStreaming();
                }
                else
                {
                    StartNdiStreaming();
                }
                break;

            case "/ndi/resolution":
                if (args.Count >= 2 && TryGetInt(args[0], out var width) && TryGetInt(args[1], out var height))
                {
                    _ndiTargetWidth = width > 0 ? width : null;
                    _ndiTargetHeight = height > 0 ? height : null;
                    if (_ndiTransmitter != null)
                    {
                        _ndiTransmitter.SetTargetResolution(_ndiTargetWidth, _ndiTargetHeight);
                    }
                    RestartNdiWithCurrentSettings();
                    NotifyOscNdiStatus();
                }
                break;

            case "/ndi/framerate":
                if (args.Count > 0 && TryGetDouble(args[0], out var fps))
                {
                    _ndiFrameRate = Math.Clamp(fps, 5.0, 120.0);
                    if (_ndiTransmitter != null)
                    {
                        _ndiTransmitter.SetFrameRate(_ndiFrameRate);
                    }
                    RestartNdiWithCurrentSettings();
                    NotifyOscNdiStatus();
                }
                break;

            case "/ndi/sourcename":
                if (args.Count > 0 && TryGetString(args[0], out var name) && !string.IsNullOrWhiteSpace(name))
                {
                    _ndiSourceName = name.Trim();
                    if (_ndiTransmitter != null)
                    {
                        _ndiTransmitter.SetSourceName(_ndiSourceName);
                    }
                    RestartNdiWithCurrentSettings();
                    NotifyOscNdiStatus();
                }
                break;

            case "/ndi/status/request":
                NotifyOscNdiStatus();
                break;

            case "/output/ndi":
                if (_ndiToggle != null)
                {
                    _ndiToggle.IsChecked = true;
                }
                StartNdiStreaming();
                break;

            case "/output/display":
                if (_ndiToggle != null)
                {
                    _ndiToggle.IsChecked = false;
                }
                StopNdiStreaming();
                break;

            case "/output/both":
                if (_ndiToggle != null)
                {
                    _ndiToggle.IsChecked = true;
                }
                StartNdiStreaming();
                break;

            default:
                break;
        }
    }

    private void SendOscStatusSnapshot()
    {
        NotifyOscPlaybackState();
        NotifyOscSpeed();
        NotifyOscFontSize();
        NotifyOscPosition();
        NotifyOscMirror();
        NotifyOscNdiStatus();
    }

    private void NotifyOscPlaybackState()
    {
        _oscBridge?.SendFeedback("/teleprompter/status", _playPauseToggle?.IsChecked == true ? "playing" : "stopped");
    }

    private void NotifyOscSpeed()
    {
        _oscBridge?.SendFeedback("/teleprompter/speed/current", _scrollSpeed.ToString("F2", CultureInfo.InvariantCulture));
    }

    private void NotifyOscFontSize()
    {
        _oscBridge?.SendFeedback("/teleprompter/font/size/current", GetCurrentFontPoints().ToString("F0", CultureInfo.InvariantCulture));
    }

    private void NotifyOscPosition()
    {
        _oscBridge?.SendFeedback("/teleprompter/position/current", GetScrollRatio().ToString("F3", CultureInfo.InvariantCulture));
    }

    private void NotifyOscMirror()
    {
        _oscBridge?.SendFeedback("/teleprompter/mirror/status", _mirrorToggle?.IsChecked == true ? "true" : "false");
    }

    private void NotifyOscNdiStatus()
    {
        var isActive = _ndiTransmitter?.IsRunning == true;
        _oscBridge?.SendFeedback("/ndi/status", isActive ? "active" : "inactive");
        _oscBridge?.SendFeedback("/ndi/available", NdiInterop.IsAvailable ? "yes" : "no");

        if (_ndiTargetWidth.HasValue && _ndiTargetHeight.HasValue)
        {
            _oscBridge?.SendFeedback("/ndi/resolution/current", $"{_ndiTargetWidth.Value}x{_ndiTargetHeight.Value}");
        }
        else if (_contentScrollViewer is not null)
        {
            var width = (int)Math.Max(1, _contentScrollViewer.ActualWidth);
            var height = (int)Math.Max(1, _contentScrollViewer.ActualHeight);
            if (width > 0 && height > 0)
            {
                _oscBridge?.SendFeedback("/ndi/resolution/current", $"{width}x{height}");
            }
        }

        _oscBridge?.SendFeedback("/ndi/framerate/current", _ndiFrameRate.ToString("F2", CultureInfo.InvariantCulture));
        _oscBridge?.SendFeedback("/ndi/sourcename/current", _ndiSourceName);
    }

    private void RestartNdiWithCurrentSettings()
    {
        if (_ndiTransmitter == null || !_ndiTransmitter.IsRunning)
        {
            return;
        }

        _ndiTransmitter.Stop();
        _ndiTransmitter.SetSourceName(_ndiSourceName);
        _ndiTransmitter.SetTargetResolution(_ndiTargetWidth, _ndiTargetHeight);
        _ndiTransmitter.SetFrameRate(_ndiFrameRate);

        if (!_ndiTransmitter.TryStart())
        {
            SetStatus("Impossibile riavviare NDI con le impostazioni correnti.");
            if (_ndiToggle != null)
            {
                _ndiToggle.IsChecked = false;
            }
        }
    }

    private double GetCurrentFontPoints()
    {
        return ConvertWpfToPoints(_contentEditor.FontSize);
    }

    private void SetFontSizePoints(double points)
    {
        points = Math.Clamp(points, 20, 200);
        var wpfSize = ConvertPointsToWpf(points);

        _contentEditor.FontSize = wpfSize;
        ApplyToDocument(range => range.ApplyPropertyValue(TextElement.FontSizeProperty, wpfSize));
        _contentEditor.Document.LineHeight = wpfSize * 1.2;
        SyncFontSizeSelectionFromEditor();
        SavePreferences();
        NotifyOscFontSize();
    }

    private double GetScrollRatio()
    {
        if (_contentScrollViewer == null)
        {
            return 0;
        }

        var max = _contentScrollViewer.ScrollableHeight;
        if (double.IsNaN(max) || max <= 0)
        {
            return 0;
        }

        return Math.Clamp(_contentScrollViewer.VerticalOffset / max, 0, 1);
    }

    private void SetScrollRatio(double ratio)
    {
        if (_contentScrollViewer == null)
        {
            return;
        }

        ratio = Math.Clamp(ratio, 0, 1);
        var max = _contentScrollViewer.ScrollableHeight;
        if (double.IsNaN(max) || max <= 0)
        {
            return;
        }

        _contentScrollViewer.ScrollToVerticalOffset(max * ratio);
        NotifyOscPosition();
    }

    private static bool TryGetDouble(object value, out double result)
    {
        switch (value)
        {
            case double d:
                result = d;
                return true;
            case float f:
                result = f;
                return true;
            case int i:
                result = i;
                return true;
            case long l:
                result = l;
                return true;
            case string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed):
                result = parsed;
                return true;
            default:
                result = 0;
                return false;
        }
    }

    private static bool TryGetInt(object value, out int result)
    {
        switch (value)
        {
            case int i:
                result = i;
                return true;
            case long l:
                result = (int)l;
                return true;
            case double d:
                result = (int)Math.Round(d);
                return true;
            case float f:
                result = (int)Math.Round(f);
                return true;
            case string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed):
                result = parsed;
                return true;
            default:
                result = 0;
                return false;
        }
    }

    private static bool TryGetBool(object value, out bool result)
    {
        switch (value)
        {
            case bool b:
                result = b;
                return true;
            case int i:
                result = i != 0;
                return true;
            case long l:
                result = l != 0;
                return true;
            case double d:
                result = Math.Abs(d) > 0.5;
                return true;
            case float f:
                result = Math.Abs(f) > 0.5f;
                return true;
            case string s when bool.TryParse(s, out var parsed):
                result = parsed;
                return true;
            default:
                result = false;
                return false;
        }
    }

    private static bool TryGetString(object value, out string? result)
    {
        switch (value)
        {
            case string s:
                result = s;
                return true;
            default:
                result = value?.ToString();
                return result != null;
        }
    }

    private void SetArrowColor(MediaColor color, bool persist = true)
    {
        if (_arrowShape == null)
        {
            _preferences.ArrowColorHex = color.ToString();
            var strokeFallback = LightenColor(color, 0.35);
            _presenterWindow?.SetArrowColor(color, strokeFallback);
            return;
        }

        var fillBrush = new SolidColorBrush(color);
        fillBrush.Freeze();
        _arrowShape.Fill = fillBrush;

        var strokeColor = LightenColor(color, 0.35);
        var strokeBrush = new SolidColorBrush(strokeColor);
        strokeBrush.Freeze();
        _arrowShape.Stroke = strokeBrush;

        _presenterWindow?.SetArrowColor(color, strokeColor);
        UpdatePresenterArrowAppearance();

        if (persist)
        {
            SavePreferences();
        }
    }

    private void UpdateArrowNormalizedFromCurrent()
    {
        if (_arrowCanvas == null || _arrowContainer == null)
        {
            return;
        }

        if (_arrowCanvas.ActualWidth <= 0 || _arrowCanvas.ActualHeight <= 0)
        {
            return;
        }

        var arrowHeight = _arrowContainer.ActualHeight > 0 ? _arrowContainer.ActualHeight : _arrowContainer.Height;
        var maxTop = Math.Max(1, _arrowCanvas.ActualHeight - arrowHeight);

        var currentTop = double.IsNaN(Canvas.GetTop(_arrowContainer)) ? 0 : Canvas.GetTop(_arrowContainer);

        _arrowNormalizedPosition = new MediaPoint(
            0,
            Clamp01(currentTop / maxTop));

    _presenterWindow?.SetArrowNormalizedY(_arrowNormalizedPosition.Y);
    }

    private static double Clamp01(double value) => Math.Max(0, Math.Min(1, value));

    private static MediaColor LightenColor(MediaColor color, double factor)
    {
        factor = Clamp01(factor);
        byte Lighten(byte channel) => (byte)Math.Clamp(channel + (255 - channel) * factor, 0, 255);
        return MediaColor.FromArgb(color.A, Lighten(color.R), Lighten(color.G), Lighten(color.B));
    }
    }
}
