using System;
using System.IO;
using System.Text.Json;

namespace TeleprompterApp;

internal static class PreferencesService
{
    private static string AppFolder => Path.GetDirectoryName(AppPaths.PreferencesPath) ?? AppPaths.BaseDirectory;
    private static string PreferencesPath => AppPaths.PreferencesPath;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static UserPreferences Load()
    {
        try
        {
            if (File.Exists(PreferencesPath))
            {
                var json = File.ReadAllText(PreferencesPath);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var preferences = JsonSerializer.Deserialize<UserPreferences>(json, SerializerOptions);
                    if (preferences != null)
                    {
                        return preferences;
                    }
                }
            }

            // If main file is missing/empty, try recovering from temp file
            var tempPath = PreferencesPath + ".tmp";
            if (File.Exists(tempPath))
            {
                var json = File.ReadAllText(tempPath);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var preferences = JsonSerializer.Deserialize<UserPreferences>(json, SerializerOptions);
                    if (preferences != null)
                    {
                        // Recover: promote temp to main
                        try { File.Move(tempPath, PreferencesPath, overwrite: true); } catch { }
                        return preferences;
                    }
                }
            }
        }
        catch
        {
            // ignored - fall back to defaults
        }

        return new UserPreferences();
    }

    public static void Save(UserPreferences preferences)
    {
        try
        {
            Directory.CreateDirectory(AppFolder);
            preferences.LastUpdatedUtc = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(preferences, SerializerOptions);

            // Atomic write: write to temp file, then rename to avoid corruption
            var tempPath = PreferencesPath + ".tmp";
            File.WriteAllText(tempPath, json);
            // File.Move with overwrite is atomic on NTFS
            File.Move(tempPath, PreferencesPath, overwrite: true);
        }
        catch
        {
            // ignored - avoid crashing if disk is not writable
        }
    }
}
