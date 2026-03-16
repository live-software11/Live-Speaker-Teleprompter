using System;
using System.IO;
using System.Text.Json;

namespace TeleprompterApp.Services;

internal static class LayoutPresetService
{
    private static string PresetsPath => Path.Combine(AppPaths.BaseDirectory, "layout-presets.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static LayoutPreset? Load(int slot)
    {
        if (slot < 1 || slot > 4) return null;
        try
        {
            if (!File.Exists(PresetsPath)) return null;
            var json = File.ReadAllText(PresetsPath);
            var all = JsonSerializer.Deserialize<LayoutPreset[]>(json, Options);
            var index = slot - 1;
            if (all != null && index < all.Length) return all[index];
        }
        catch { /* ignore */ }
        return null;
    }

    public static void Save(int slot, LayoutPreset preset)
    {
        if (slot < 1 || slot > 4) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PresetsPath)!);
            var all = new LayoutPreset[4];
            if (File.Exists(PresetsPath))
            {
                var json = File.ReadAllText(PresetsPath);
                var existing = JsonSerializer.Deserialize<LayoutPreset[]>(json, Options);
                if (existing != null)
                {
                    for (var i = 0; i < Math.Min(4, existing.Length); i++)
                        all[i] = existing[i] ?? new LayoutPreset();
                }
            }
            for (var i = 0; i < 4; i++)
                all[i] ??= new LayoutPreset();
            all[slot - 1] = preset;
            var outJson = JsonSerializer.Serialize(all, Options);
            var tempPath = PresetsPath + ".tmp";
            File.WriteAllText(tempPath, outJson);
            File.Move(tempPath, PresetsPath, overwrite: true);
        }
        catch { /* ignore */ }
    }
}
