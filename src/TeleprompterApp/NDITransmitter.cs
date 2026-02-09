using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace TeleprompterApp;

/// <summary>
/// NDI frame transmitter with performance optimizations:
/// - CompositionTarget.Rendering for vsync-aligned capture (replaces DispatcherTimer)
/// - Pre-allocated, reused RenderTargetBitmap and DrawingVisual
/// - Stopwatch-based frame-rate limiter to avoid oversending
/// - Unmanaged buffer pooling (existing EnsureBuffer)
/// </summary>
internal sealed class NDITransmitter : IDisposable
{
    private readonly FrameworkElement _source;
    private readonly Dispatcher _dispatcher;
    private readonly Stopwatch _frameClock = new();
    private readonly DrawingVisual _reusableVisual = new();

    private string _ndiName;
    private double _framesPerSecond = 30.0;
    private double _minFrameIntervalMs;
    private int? _targetWidth;
    private int? _targetHeight;

    private IntPtr _sendInstance;
    private IntPtr _bufferPtr = IntPtr.Zero;
    private int _bufferSize;
    private bool _isRunning;
    private bool _ndiInitialized;

    // Cached bitmap — recreated only when resolution changes
    private RenderTargetBitmap? _cachedBitmap;
    private int _cachedWidth;
    private int _cachedHeight;

    public NDITransmitter(FrameworkElement source, string ndiName)
    {
        _source = source;
        _dispatcher = source.Dispatcher;
        _ndiName = ndiName;
        _minFrameIntervalMs = 1000.0 / _framesPerSecond;
    }

    public bool IsRunning => _isRunning;

    public double FramesPerSecond => _framesPerSecond;

    public void SetFrameRate(double fps)
    {
        _framesPerSecond = Math.Clamp(fps, 5.0, 120.0);
        _minFrameIntervalMs = 1000.0 / _framesPerSecond;
    }

    public void SetTargetResolution(int? width, int? height)
    {
        _targetWidth = width > 0 ? width : null;
        _targetHeight = height > 0 ? height : null;
        // Invalidate cached bitmap so it gets recreated with new size
        _cachedBitmap = null;
    }

    public void SetSourceName(string ndiName)
    {
        if (!string.IsNullOrWhiteSpace(ndiName))
        {
            _ndiName = ndiName.Trim();
        }
    }

    public bool TryStart()
    {
        if (_isRunning)
        {
            return true;
        }

        if (!NdiInterop.IsAvailable)
        {
            return false;
        }

        if (!NdiInterop.Initialize())
        {
            return false;
        }

        var namePtr = NdiInterop.CreateUtf8String(_ndiName);

        var sendCreate = new NdiInterop.NDIlib_send_create_t
        {
            p_ndi_name = namePtr,
            p_groups = IntPtr.Zero,
            p_clock_domain = IntPtr.Zero,
            clock_audio = false,
            clock_video = true
        };

        _sendInstance = NdiInterop.SendCreate(ref sendCreate);

        if (namePtr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(namePtr);
        }

        if (_sendInstance == IntPtr.Zero)
        {
            NdiInterop.Destroy();
            return false;
        }

        _ndiInitialized = true;
        _isRunning = true;

        // Use CompositionTarget.Rendering for vsync-aligned frame capture
        _frameClock.Restart();
        CompositionTarget.Rendering += OnRendering;
        return true;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (!_isRunning || _sendInstance == IntPtr.Zero)
        {
            return;
        }

        // Frame-rate limiter: skip if not enough time has passed
        var elapsed = _frameClock.Elapsed.TotalMilliseconds;
        if (elapsed < _minFrameIntervalMs)
        {
            return;
        }

        _frameClock.Restart();

        try
        {
            CaptureAndSendFrame();
        }
        catch (Exception ex)
        {
            // Swallow rendering errors to prevent crashing the WPF composition pipeline.
            // Common causes: source element disposed mid-frame, bitmap allocation failure.
            Debug.WriteLine($"NDI frame capture error: {ex.Message}");
        }
    }

    private void CaptureAndSendFrame()
    {
        var width = (int)Math.Max(1, _source.ActualWidth);
        var height = (int)Math.Max(1, _source.ActualHeight);
        if (width == 0 || height == 0)
        {
            return;
        }

        var desiredWidth = _targetWidth ?? width;
        var desiredHeight = _targetHeight ?? height;

        if (_targetWidth.HasValue && !_targetHeight.HasValue)
        {
            desiredHeight = (int)Math.Round(desiredWidth / Math.Max(1.0, width / (double)height));
        }
        else if (_targetHeight.HasValue && !_targetWidth.HasValue)
        {
            desiredWidth = (int)Math.Round(Math.Max(1.0, width / (double)height) * desiredHeight);
        }

        desiredWidth = Math.Max(1, desiredWidth);
        desiredHeight = Math.Max(1, desiredHeight);

        // Reuse DrawingVisual — just re-render into it
        using (var context = _reusableVisual.RenderOpen())
        {
            var brush = new VisualBrush(_source) { Stretch = Stretch.Fill };
            context.DrawRectangle(brush, null, new Rect(0, 0, desiredWidth, desiredHeight));
        }

        // Reuse RenderTargetBitmap if resolution hasn't changed
        if (_cachedBitmap == null || _cachedWidth != desiredWidth || _cachedHeight != desiredHeight)
        {
            _cachedBitmap = new RenderTargetBitmap(desiredWidth, desiredHeight, 96, 96, PixelFormats.Pbgra32);
            _cachedWidth = desiredWidth;
            _cachedHeight = desiredHeight;
        }
        else
        {
            _cachedBitmap.Clear();
        }

        _cachedBitmap.Render(_reusableVisual);

        var stride = desiredWidth * 4;
        var size = stride * desiredHeight;
        EnsureBuffer(size);

        _cachedBitmap.CopyPixels(Int32Rect.Empty, _bufferPtr, size, stride);

        var videoFrame = new NdiInterop.NDIlib_video_frame_v2_t
        {
            xres = desiredWidth,
            yres = desiredHeight,
            FourCC = NdiInterop.NDIlib_FourCC_video_type_e.NDIlib_FourCC_type_BGRA,
            frame_format_type = NdiInterop.NDIlib_frame_format_type_e.NDIlib_frame_format_type_progressive,
            line_stride_in_bytes = stride,
            frame_rate_N = (int)Math.Round(_framesPerSecond * 1000.0),
            frame_rate_D = 1000,
            picture_aspect_ratio = desiredWidth / (float)desiredHeight,
            p_data = _bufferPtr,
            timecode = NdiInterop.NDIlib_send_timecode_synthesize,
            p_metadata = IntPtr.Zero,
            timestamp = 0
        };

        NdiInterop.SendVideoV2(_sendInstance, ref videoFrame);
    }

    private void EnsureBuffer(int required)
    {
        if (required <= _bufferSize)
        {
            return;
        }

        if (_bufferPtr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_bufferPtr);
        }

        _bufferPtr = Marshal.AllocHGlobal(required);
        _bufferSize = required;
    }

    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;
        _frameClock.Stop();

        // Detach from rendering pipeline
        CompositionTarget.Rendering -= OnRendering;

        _cachedBitmap = null;

        if (_sendInstance != IntPtr.Zero)
        {
            NdiInterop.SendDestroy(_sendInstance);
            _sendInstance = IntPtr.Zero;
        }

        if (_bufferPtr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_bufferPtr);
            _bufferPtr = IntPtr.Zero;
            _bufferSize = 0;
        }

        if (_ndiInitialized)
        {
            NdiInterop.Destroy();
            _ndiInitialized = false;
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
