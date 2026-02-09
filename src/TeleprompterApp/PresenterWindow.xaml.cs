using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using MediaColor = System.Windows.Media.Color;
using WpfApplication = System.Windows.Application;
using WpfRichTextBox = System.Windows.Controls.RichTextBox;

namespace TeleprompterApp
{
    public partial class PresenterWindow : Window
    {
        private const double ArrowLeftOffset = 12;
        private bool _isMirrored;
        private ScrollViewer? _scrollViewer;
        private WpfRichTextBox? _content;
        private Canvas? _arrowCanvas;
        private Grid? _arrowContainer;
        private ScaleTransform? _arrowScaleTransform;
        private Polygon? _arrowShape;

        /// <summary>
        /// Tracks which screen this window is currently displayed on.
        /// Used by DisplayManager to detect if the screen was removed.
        /// </summary>
        public string? CurrentScreenDeviceName { get; private set; }

        public PresenterWindow()
        {
            LoadView();
            ResolveNamedElements();
            Loaded += PresenterWindow_Loaded;
        }

        private void PresenterWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateArrowPosition();
        }

        private void PresenterArrowCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateArrowPosition();
        }

        public void SetDocument(FlowDocument document)
        {
            if (_content != null)
            {
                _content.Document = document;
            }
        }

        public void SetPagePadding(Thickness padding)
        {
            if (_content?.Document is FlowDocument doc)
            {
                // When mirrored, the ScaleTransform flips the coordinate system,
                // so we also need to swap left↔right padding to match visual layout
                var effectivePadding = _isMirrored
                    ? new Thickness(padding.Right, padding.Top, padding.Left, padding.Bottom)
                    : padding;

                doc.PagePadding = effectivePadding;
                _content.Padding = effectivePadding;
            }
        }

        public void SetMirror(bool isMirrored)
        {
            _isMirrored = isMirrored;
            if (_content != null)
            {
                _content.LayoutTransform = isMirrored
                    ? new ScaleTransform(-1, 1)
                    : Transform.Identity;
            }

            if (_arrowContainer != null)
            {
                _arrowContainer.LayoutTransform = Transform.Identity;
            }

            if (_arrowShape != null)
            {
                _arrowShape.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                _arrowShape.RenderTransform = isMirrored
                    ? new ScaleTransform(-1, 1)
                    : Transform.Identity;
            }
            UpdateArrowPosition();
        }

        public void SetVerticalOffset(double offset)
        {
            _scrollViewer?.ScrollToVerticalOffset(offset);
        }

        private double _arrowNormalizedY = 0.5;

        public void SetArrowNormalizedY(double normalizedY)
        {
            _arrowNormalizedY = Math.Clamp(normalizedY, 0, 1);
            UpdateArrowPosition();
        }

        public void SetArrowScale(double scale)
        {
                if (_arrowScaleTransform != null)
                {
                    _arrowScaleTransform.ScaleX = scale;
                    _arrowScaleTransform.ScaleY = scale;
                }
            UpdateArrowPosition();
        }

        public void SetArrowColor(MediaColor fill, MediaColor stroke)
        {
                if (_arrowShape != null)
                {
                    _arrowShape.Fill = new SolidColorBrush(fill);
                    _arrowShape.Stroke = new SolidColorBrush(stroke);
                }
        }

        private void UpdateArrowPosition()
        {
                if (_arrowCanvas == null || _arrowContainer == null)
                {
                    return;
                }

                if (_arrowCanvas.ActualHeight <= 0)
            {
                return;
            }

                var arrowHeight = _arrowContainer.ActualHeight > 0 ? _arrowContainer.ActualHeight : _arrowContainer.Height;
                var arrowWidth = _arrowContainer.ActualWidth > 0 ? _arrowContainer.ActualWidth : _arrowContainer.Width;
                var maxTop = Math.Max(0, _arrowCanvas.ActualHeight - arrowHeight);
            var top = maxTop * _arrowNormalizedY;

            var left = ArrowLeftOffset;
                var canvasWidth = _arrowCanvas.ActualWidth;
            if (_isMirrored && canvasWidth > 0)
            {
                left = Math.Max(ArrowLeftOffset, canvasWidth - arrowWidth - ArrowLeftOffset);
            }

                Canvas.SetLeft(_arrowContainer, left);
                Canvas.SetTop(_arrowContainer, top);
        }

        public void ShowOnScreen(System.Windows.Forms.Screen screen)
        {
            CurrentScreenDeviceName = screen.DeviceName;
            WindowState = WindowState.Normal;
            WindowStartupLocation = WindowStartupLocation.Manual;

            var targetOpacity = Opacity;
            var shouldRestoreOpacity = !IsVisible && targetOpacity > 0;
            if (shouldRestoreOpacity)
            {
                Opacity = 0;
            }

            if (!IsVisible)
            {
                Show();
            }

            ApplyScreenBounds(screen);
            Activate();

            if (shouldRestoreOpacity)
            {
                Opacity = targetOpacity;
            }

            WindowState = WindowState.Maximized;
        }

        private void ApplyScreenBounds(System.Windows.Forms.Screen screen)
        {
            // Use Bounds (full screen area) instead of WorkingArea (excludes taskbar)
            // since the presenter should cover the entire display.
            var bounds = screen.Bounds;
            var source = PresentationSource.FromVisual(this);
            var compositionTarget = source?.CompositionTarget;

            if (compositionTarget != null)
            {
                var transform = compositionTarget.TransformFromDevice;
                var topLeft = transform.Transform(new System.Windows.Point(bounds.Left, bounds.Top));
                var bottomRight = transform.Transform(new System.Windows.Point(bounds.Right, bounds.Bottom));

                Left = topLeft.X;
                Top = topLeft.Y;
                Width = Math.Max(0, bottomRight.X - topLeft.X);
                Height = Math.Max(0, bottomRight.Y - topLeft.Y);
            }
            else
            {
                // PresentationSource not yet available (window not rendered).
                // Use raw device pixels — will be corrected on first render via DpiChanged.
                Left = bounds.Left;
                Top = bounds.Top;
                Width = bounds.Width;
                Height = bounds.Height;

                // Schedule a re-apply after the window has a PresentationSource
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
                {
                    if (!string.IsNullOrEmpty(CurrentScreenDeviceName))
                    {
                        var currentScreen = System.Windows.Forms.Screen.AllScreens
                            .FirstOrDefault(s => s.DeviceName == CurrentScreenDeviceName);
                        if (currentScreen != null)
                        {
                            ApplyScreenBounds(currentScreen);
                        }
                    }
                });
            }
        }

        public void HideIfNeeded()
        {
            if (IsVisible)
            {
                Hide();
            }
        }

        private void LoadView()
        {
            var resourceLocator = new Uri("/TeleprompterApp;component/PresenterWindow.xaml", UriKind.Relative);
            WpfApplication.LoadComponent(this, resourceLocator);
        }

        private void ResolveNamedElements()
        {
            _scrollViewer = FindElement<ScrollViewer>("PresenterScrollViewer");
            _content = FindElement<WpfRichTextBox>("PresenterContent");
            _arrowCanvas = FindElement<Canvas>("PresenterArrowCanvas");
            _arrowContainer = FindElement<Grid>("PresenterArrowContainer");
            _arrowScaleTransform = FindElement<ScaleTransform>("PresenterArrowScaleTransform");
            _arrowShape = FindElement<Polygon>("PresenterArrowShape");

            if (_arrowCanvas != null)
            {
                _arrowCanvas.SizeChanged += PresenterArrowCanvas_SizeChanged;
            }
        }

        private T? FindElement<T>(string name) where T : class
        {
            return FindName(name) as T;
        }
    }
}
