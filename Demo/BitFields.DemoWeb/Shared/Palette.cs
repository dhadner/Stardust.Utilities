using Stardust.Utilities;

namespace BitFields.DemoWeb;

/// <summary>
/// Shared color palette and formatting helpers for the Blazor demo pages.
/// </summary>
public static class Palette
{
    public static readonly string[] Colors =
    [
        "#61AFEF", "#C678DD", "#7AC04A", "#D4A034",
        "#E06C75", "#4CC8D9", "#D09040", "#E06856",
        "#4CC8DD", "#E868B0", "#50C878", "#B48AE0",
        "#BD93F9", "#E0A040", "#4CBCD8", "#FF6666",
        "#8CC870", "#C088C0", "#60B860", "#A0A4AA",
    ];

    public static string Get(int index) => Colors[index % Colors.Length];

    public static string WithAlpha(string hex, double alpha)
    {
        var a = (int)(alpha * 255);
        return $"{hex}{a:X2}";  // CSS format: #RRGGBBAA
    }

    public static string FormatField<T>(T view, BitFieldInfo fieldInfo) where T : struct
    {
        var prop = typeof(T).GetProperty(fieldInfo.Name);
        if (prop == null) return "?";
        var val = prop.GetValue(view);
        if (val == null) return "?";

        if (fieldInfo.IsFlag) return (bool)val ? "1" : "0";

        return fieldInfo.PropertyType switch
        {
            "byte" => $"0x{val:X2}",
            "ushort" => $"0x{(ushort)val:X4}",
            "uint" => $"0x{(uint)val:X8}",
            "ulong" => $"0x{(ulong)val:X16}",
            _ => val?.ToString() ?? "?"
        };
    }
}
