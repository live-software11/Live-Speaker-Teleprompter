using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TeleprompterApp.Services;

/// <summary>
/// TASK-010: estratto da MainWindow. Concentra la parte pura di I/O documenti:
/// detection formato, lettura bytes/testo, scrittura atomica (pattern .tmp).
/// La conversione bytes → FlowDocument resta in MainWindow perché richiede
/// UI thread e il RichTextBox.
/// </summary>
internal static class DocumentFileService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".rtf", ".srt", ".vtt", ".log", ".csv", ".json",
        ".xml", ".html", ".htm", ".yaml", ".yml", ".ini", ".cfg",
        ".bat", ".ps1", ".xaml", ".xamlpackage", ".rstp", ".docx", ".doc"
    };

    public static bool IsSupportedExtension(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        var ext = Path.GetExtension(path);
        return !string.IsNullOrEmpty(ext) && SupportedExtensions.Contains(ext);
    }

    public static byte[] ReadBytes(string path) => File.ReadAllBytes(path);

    public static string ReadAllText(string path, Encoding? encoding = null)
        => encoding != null ? File.ReadAllText(path, encoding) : File.ReadAllText(path);

    /// <summary>
    /// Scrittura atomica (vincolo sacro #6): scrive prima su <c>path.tmp</c>
    /// poi sostituisce il file finale con <c>File.Move(overwrite:true)</c>.
    /// </summary>
    public static void WriteText(string path, string content)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, content);
        File.Move(tmp, path, overwrite: true);
    }

    public static DocumentFormat DetectFormat(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".rstp" or ".xamlpackage" => DocumentFormat.XamlPackage,
            ".xaml" => DocumentFormat.Xaml,
            ".rtf" => DocumentFormat.Rtf,
            ".docx" or ".doc" => DocumentFormat.Word,
            _ => DocumentFormat.PlainText
        };
    }
}

internal enum DocumentFormat
{
    PlainText,
    Rtf,
    Xaml,
    XamlPackage,
    Word
}
