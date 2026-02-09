using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TeleprompterApp;

internal static class NdiInterop
{
    private const string LibraryName = "Processing.NDI.Lib.x64.dll";

    private static readonly IntPtr LibraryHandle;
    private static readonly bool LibraryLoaded;

    private static readonly InitializeDelegate? InitializeFunc;
    private static readonly DestroyDelegate? DestroyFunc;
    private static readonly SendCreateDelegate? SendCreateFunc;
    private static readonly SendDestroyDelegate? SendDestroyFunc;
    private static readonly SendVideoV2Delegate? SendVideoV2Func;

    internal const long NDIlib_send_timecode_synthesize = unchecked((long)0x8000000000000000);

    internal static bool IsAvailable => LibraryLoaded;

    static NdiInterop()
    {
        if (TryLoadLibrary(out var handle))
        {
            LibraryHandle = handle;
            InitializeFunc = GetDelegate<InitializeDelegate>("NDIlib_initialize");
            DestroyFunc = GetDelegate<DestroyDelegate>("NDIlib_destroy");
            SendCreateFunc = GetDelegate<SendCreateDelegate>("NDIlib_send_create_v2");
            SendDestroyFunc = GetDelegate<SendDestroyDelegate>("NDIlib_send_destroy");
            SendVideoV2Func = GetDelegate<SendVideoV2Delegate>("NDIlib_send_send_video_v2");

            LibraryLoaded = InitializeFunc != null
                             && DestroyFunc != null
                             && SendCreateFunc != null
                             && SendDestroyFunc != null
                             && SendVideoV2Func != null;
        }
        else
        {
            LibraryLoaded = false;
        }
    }

    internal static bool Initialize()
    {
        return InitializeFunc?.Invoke() ?? false;
    }

    internal static void Destroy()
    {
        DestroyFunc?.Invoke();
    }

    internal static IntPtr SendCreate(ref NDIlib_send_create_t create)
    {
        return SendCreateFunc?.Invoke(ref create) ?? IntPtr.Zero;
    }

    internal static void SendDestroy(IntPtr instance)
    {
        SendDestroyFunc?.Invoke(instance);
    }

    internal static void SendVideoV2(IntPtr instance, ref NDIlib_video_frame_v2_t frame)
    {
        SendVideoV2Func?.Invoke(instance, ref frame);
    }

    internal static IntPtr CreateUtf8String(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return IntPtr.Zero;
        }

        var bytes = Encoding.UTF8.GetBytes(value + '\0');
        var ptr = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, ptr, bytes.Length);
        return ptr;
    }

    private static TDelegate? GetDelegate<TDelegate>(string export)
        where TDelegate : Delegate
    {
        if (LibraryHandle == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var proc = NativeLibrary.GetExport(LibraryHandle, export);
            return Marshal.GetDelegateForFunctionPointer<TDelegate>(proc);
        }
        catch
        {
            return null;
        }
    }

    private static bool TryLoadLibrary(out IntPtr handle)
    {
        if (NativeLibrary.TryLoad(LibraryName, out handle))
        {
            return true;
        }

        foreach (var candidatePath in EnumeratePotentialLibraryLocations())
        {
            if (string.IsNullOrWhiteSpace(candidatePath))
            {
                continue;
            }

            try
            {
                if (!File.Exists(candidatePath))
                {
                    continue;
                }

                if (NativeLibrary.TryLoad(candidatePath, out handle))
                {
                    return true;
                }
            }
            catch
            {
                // Ignore invalid paths and continue checking the remaining options.
            }
        }

        handle = IntPtr.Zero;
        return false;
    }

    private static IEnumerable<string> EnumeratePotentialLibraryLocations()
    {
        static IEnumerable<string> SafeEnumerateFiles(string? root)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                yield break;
            }

            var pending = new Stack<string>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                var current = pending.Pop();

                string[] files;
                try
                {
                    files = Directory.GetFiles(current, LibraryName, SearchOption.TopDirectoryOnly);
                }
                catch
                {
                    continue;
                }

                foreach (var file in files)
                {
                    yield return file;
                }

                string[] subdirectories;
                try
                {
                    subdirectories = Directory.GetDirectories(current);
                }
                catch
                {
                    continue;
                }

                foreach (var subDir in subdirectories)
                {
                    try
                    {
                        var dirInfo = new DirectoryInfo(subDir);
                        if ((dirInfo.Attributes & FileAttributes.ReparsePoint) != 0)
                        {
                            continue;
                        }
                    }
                    catch
                    {
                        // If metadata cannot be read, skip this entry.
                        continue;
                    }

                    pending.Push(subDir);
                }
            }
        }

        var directoriesToSearch = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            AppContext.BaseDirectory,
        };

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrWhiteSpace(programFiles))
        {
            directoriesToSearch.Add(programFiles);
            directoriesToSearch.Add(Path.Combine(programFiles, "NDI"));
        }

        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if (!string.IsNullOrWhiteSpace(programFilesX86))
        {
            directoriesToSearch.Add(programFilesX86);
            directoriesToSearch.Add(Path.Combine(programFilesX86, "NDI"));
        }

        foreach (var envVar in new[] { "NDI_RUNTIME_DIR_V2", "NDI_RUNTIME_DIR_V3", "NDI_RUNTIME_DIR_V4", "NDI_RUNTIME_DIR_V5", "NDI_RUNTIME_DIR_V6" })
        {
            var value = Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrWhiteSpace(value))
            {
                directoriesToSearch.Add(value);
            }
        }

        foreach (var root in directoriesToSearch)
        {
            foreach (var file in SafeEnumerateFiles(root))
            {
                yield return file;
            }
        }
    }

    private delegate bool InitializeDelegate();
    private delegate void DestroyDelegate();
    private delegate IntPtr SendCreateDelegate(ref NDIlib_send_create_t create);
    private delegate void SendDestroyDelegate(IntPtr instance);
    private delegate void SendVideoV2Delegate(IntPtr instance, ref NDIlib_video_frame_v2_t frame);

    [StructLayout(LayoutKind.Sequential)]
    internal struct NDIlib_send_create_t
    {
        public IntPtr p_ndi_name;
        public IntPtr p_groups;
        public IntPtr p_clock_domain;
        [MarshalAs(UnmanagedType.I1)] public bool clock_video;
        [MarshalAs(UnmanagedType.I1)] public bool clock_audio;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NDIlib_video_frame_v2_t
    {
        public int xres;
        public int yres;
        public NDIlib_FourCC_video_type_e FourCC;
        public NDIlib_frame_format_type_e frame_format_type;
        public int frame_rate_N;
        public int frame_rate_D;
        public float picture_aspect_ratio;
        public long timecode;
        public IntPtr p_data;
        public int line_stride_in_bytes;
        public IntPtr p_metadata;
        public long timestamp;
    }

    internal enum NDIlib_FourCC_video_type_e : uint
    {
        NDIlib_FourCC_type_BGRA = 0x41524742,
        NDIlib_FourCC_type_BGRX = 0x58524742,
        NDIlib_FourCC_type_RGBA = 0x41424752,
        NDIlib_FourCC_type_RGBX = 0x58424752,
    }

    internal enum NDIlib_frame_format_type_e
    {
        NDIlib_frame_format_type_interleaved = 0,
        NDIlib_frame_format_type_progressive = 1,
        NDIlib_frame_format_type_field_0 = 2,
        NDIlib_frame_format_type_field_1 = 3
    }
}
