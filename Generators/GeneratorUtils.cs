namespace Stardust.Generators;

/// <summary>
/// Shared utility methods used by all BitFields source generators.
/// </summary>
internal static class GeneratorUtils
{
    /// <summary>
    /// Escapes a string value so it can be safely embedded in a C# string literal
    /// (regular quoted string, not verbatim). Handles all characters that would break
    /// compilation or alter semantics: backslash, quote, newline, carriage return, tab,
    /// null, and all other control characters (U+0000..U+001F, U+007F..U+009F).
    /// </summary>
    internal static string EscapeStringLiteral(string value)
    {
        var sb = new System.Text.StringBuilder(value.Length + 8);
        foreach (char c in value)
        {
            switch (c)
            {
                case '\\': sb.Append(@"\\"); break;
                case '"':  sb.Append(@"\"""); break;
                case '\n': sb.Append(@"\n"); break;
                case '\r': sb.Append(@"\r"); break;
                case '\t': sb.Append(@"\t"); break;
                case '\0': sb.Append(@"\0"); break;
                case '\a': sb.Append(@"\a"); break;
                case '\b': sb.Append(@"\b"); break;
                case '\f': sb.Append(@"\f"); break;
                case '\v': sb.Append(@"\v"); break;
                default:
                    // Escape any remaining control characters as \uXXXX
                    if (char.IsControl(c))
                        sb.Append($"\\u{(int)c:X4}");
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }
}
