using System;

namespace TeleprompterApp;

public class UserPreferences
{
    public string? DocumentBackgroundHex { get; set; }
    public string? TextForegroundHex { get; set; }
    public string? FontFamily { get; set; }
    public double FontSizePoints { get; set; } = 72;
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
    public bool UseUnderline { get; set; }
    public double DefaultScrollSpeed { get; set; } = 0.5;
    public bool MirrorEnabled { get; set; }
    public bool TopMostEnabled { get; set; }
    public int PreferredDisplayNumber { get; set; }
    public string? LastScriptPath { get; set; }
    public string? ArrowColorHex { get; set; }
    public double ArrowScale { get; set; } = 1.0;
    public double ArrowHorizontalOffset { get; set; } = 0.05;
    public double ArrowVerticalOffset { get; set; } = 0.5;
    public double ArrowLeftPaddingExtra { get; set; } = 12;
    public double MarginTop { get; set; } = 40;
    public double MarginRight { get; set; } = 40;
    public double MarginBottom { get; set; } = 40;
    public double MarginLeft { get; set; } = 40;
    public bool EditModeEnabled { get; set; } = true;
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}
