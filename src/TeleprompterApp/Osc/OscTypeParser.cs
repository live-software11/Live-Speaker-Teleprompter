using System;
using System.Collections.Generic;
using System.Globalization;

namespace TeleprompterApp.Osc;

/// <summary>
/// TASK-008: parser puro (no side-effect, nessuna dipendenza UI) per gli argomenti OSC.
/// Estratto da MainWindow per alleggerire la God Class. Le firme principali
/// <c>(object value, out T result)</c> mantengono retrocompatibilità con i call site
/// di MainWindow; gli overload <c>(IReadOnlyList&lt;object&gt; args, int index, out T)</c>
/// sono usati da OscCommandHandler (TASK-009).
/// </summary>
internal static class OscTypeParser
{
    public static bool TryGetDouble(object value, out double result)
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

    public static bool TryGetInt(object value, out int result)
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

    public static bool TryGetBool(object value, out bool result)
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

    public static bool TryGetString(object value, out string? result)
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

    // Overload index-based per OscCommandHandler (TASK-009)
    public static bool TryGetDouble(IReadOnlyList<object> args, int index, out double result)
    {
        result = 0;
        if (args == null || index < 0 || index >= args.Count) return false;
        return TryGetDouble(args[index], out result);
    }

    public static bool TryGetInt(IReadOnlyList<object> args, int index, out int result)
    {
        result = 0;
        if (args == null || index < 0 || index >= args.Count) return false;
        return TryGetInt(args[index], out result);
    }

    public static bool TryGetBool(IReadOnlyList<object> args, int index, out bool result)
    {
        result = false;
        if (args == null || index < 0 || index >= args.Count) return false;
        return TryGetBool(args[index], out result);
    }

    public static bool TryGetString(IReadOnlyList<object> args, int index, out string? result)
    {
        result = null;
        if (args == null || index < 0 || index >= args.Count) return false;
        return TryGetString(args[index], out result);
    }
}
