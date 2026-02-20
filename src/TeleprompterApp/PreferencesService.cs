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
                var preferences = JsonSerializer.Deserialize<UserPreferences>(json, SerializerOptions);
                if (preferences != null)
                {
                    return preferences;
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
            File.WriteAllText(PreferencesPath, json);
        }
        catch
        {
            // ignored - avoid crashing if disk is not writable
        }
    }
}
