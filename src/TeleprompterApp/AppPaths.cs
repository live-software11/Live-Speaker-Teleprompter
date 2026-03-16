using System;
using System.IO;

namespace TeleprompterApp;

/// <summary>
/// Determines the base directory for preferences and logs.
/// Portable mode: exe outside Program Files / LocalAppData → use exe directory (no traces on host).
/// Installed mode: exe in AppData or Program Files → use %APPDATA% (standard installed behavior).
/// </summary>
internal static class AppPaths
{
    private static readonly Lazy<string> BaseDir = new(ComputeBaseDirectory);

    public static string BaseDirectory => BaseDir.Value;
    public static string PreferencesPath => Path.Combine(BaseDirectory, "preferences.json");
    public static string LogDirectory => Path.Combine(BaseDirectory, "logs");

    private static string ComputeBaseDirectory()
    {
        try
        {
            var exePath = AppDomain.CurrentDomain.BaseDirectory;
            var exeDir = Path.GetFullPath(exePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            var isInstalled = exeDir.StartsWith(localAppData, StringComparison.OrdinalIgnoreCase)
                || exeDir.StartsWith(programFiles, StringComparison.OrdinalIgnoreCase)
                || exeDir.StartsWith(programFilesX86, StringComparison.OrdinalIgnoreCase);

            if (isInstalled)
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Live Speaker Teleprompter");
            }

            // Portable: use exe directory (USB, desktop, etc.) — no traces on host PC
            return exeDir;
        }
        catch
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Live Speaker Teleprompter");
        }
    }
}
