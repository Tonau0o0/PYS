namespace PYS.Core.Common;

public static class ColorPalette
{
    public static readonly IReadOnlyList<string> Defaults = new[]
    {
        "#2196F3", // Blue
        "#F44336", // Red
        "#4CAF50", // Green
        "#FF9800", // Orange
        "#9C27B0", // Purple
        "#E91E63", // Pink
        "#00BCD4", // Cyan
        "#FFC107", // Amber
        "#3F51B5", // Indigo
        "#009688", // Teal
        "#FF5722", // Deep Orange
        "#795548"  // Brown
    };

    public static string PickFor(string email)
    {
        var hash = 0;
        foreach (var c in email) hash = (hash * 31 + c) & 0x7FFFFFFF;
        return Defaults[hash % Defaults.Count];
    }

    public static bool IsValid(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return false;
        if (!hex.StartsWith('#')) return false;
        if (hex.Length is not (4 or 7 or 9)) return false;
        for (int i = 1; i < hex.Length; i++)
        {
            var c = hex[i];
            var ok = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
            if (!ok) return false;
        }
        return true;
    }
}
