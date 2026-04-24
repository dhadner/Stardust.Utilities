using System;
using System.Globalization;
using System.Text;

namespace Stardust.Utilities
{
    /// <summary>
    /// Shared user-input parsing for the Be/Le integer TypeConverters. Accepts:
    /// decimal (culture-aware, optional sign), hex prefixed with "0x"/"0X",
    /// binary prefixed with "0b"/"0B", "_" digit separators, and leading/trailing
    /// whitespace. Wraps any underlying parse failure in a FormatException whose
    /// message includes the original input.
    /// </summary>
    internal static class ConverterParsing
    {
        /// <summary>
        /// Parses <paramref name="input"/> using the supplied decimal and hex parsers.
        /// </summary>
        /// <typeparam name="T">The target integer type.</typeparam>
        /// <param name="input">The raw user input string.</param>
        /// <param name="culture">Culture for decimal parsing. Hex and binary always use invariant. Null means invariant.</param>
        /// <param name="parseDecimal">Delegate that parses a decimal-formatted string with the supplied provider.</param>
        /// <param name="parseHex">Delegate that parses an unprefixed hex-digit string (invariant).</param>
        /// <returns>The parsed value of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="input"/> is null.</exception>
        /// <exception cref="FormatException">If the input is empty, has only a prefix, or fails to parse.</exception>
        internal static T Parse<T>(string input,
                                   CultureInfo? culture,
                                   Func<string, IFormatProvider, T> parseDecimal,
                                   Func<string, T> parseHex)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            string trimmed = input.Trim();
            if (trimmed.Length == 0)
                throw new FormatException("Input string was empty or whitespace.");

            try
            {
                if (HasPrefix(trimmed, 'x'))
                {
                    string digits = StripUnderscores(trimmed, 2);
                    if (digits.Length == 0)
                        throw new FormatException("Hex prefix with no digits.");
                    return parseHex(digits);
                }
                if (HasPrefix(trimmed, 'b'))
                {
                    string bits = StripUnderscores(trimmed, 2);
                    if (bits.Length == 0)
                        throw new FormatException("Binary prefix with no digits.");
                    return parseHex(BinaryToHex(bits));
                }
                string dec = StripUnderscores(trimmed, 0);
                return parseDecimal(dec, culture ?? CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException)
            {
                throw new FormatException(
                    $"Could not convert \"{input}\": {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Non-throwing TryParse counterpart to <see cref="Parse{T}"/>. Returns false
        /// on any input that would cause <see cref="Parse{T}"/> to throw (null, empty,
        /// invalid syntax, overflow).
        /// </summary>
        /// <typeparam name="T">The target integer type.</typeparam>
        /// <param name="input">The raw user input string.</param>
        /// <param name="culture">Culture for decimal parsing. Hex and binary always use invariant. Null means invariant.</param>
        /// <param name="parseDecimal">Delegate that parses a decimal-formatted string with the supplied provider.</param>
        /// <param name="parseHex">Delegate that parses an unprefixed hex-digit string (invariant).</param>
        /// <param name="result">On success, the parsed value; otherwise default.</param>
        /// <returns>True if parsing succeeded; otherwise false.</returns>
        internal static bool TryParse<T>(string? input,
                                         CultureInfo? culture,
                                         Func<string, IFormatProvider, T> parseDecimal,
                                         Func<string, T> parseHex,
                                         out T result)
        {
            if (input is null) { result = default!; return false; }
            try
            {
                result = Parse(input, culture, parseDecimal, parseHex);
                return true;
            }
            catch (Exception ex) when (ex is FormatException || ex is ArgumentException)
            {
                result = default!;
                return false;
            }
        }

        /// <summary>
        /// Returns true if <paramref name="s"/> starts with "0" followed by the given tag letter (case-insensitive).
        /// </summary>
        /// <param name="s">The (already-trimmed) input.</param>
        /// <param name="lowerTag">The lower-case tag letter, e.g. 'x' for hex or 'b' for binary.</param>
        /// <returns>True when the prefix matches.</returns>
        private static bool HasPrefix(string s, char lowerTag)
        {
            if (s.Length < 2) return false;
            if (s[0] != '0') return false;
            char c = s[1];
            return c == lowerTag || c == char.ToUpperInvariant(lowerTag);
        }

        /// <summary>
        /// Returns <paramref name="s"/> starting at <paramref name="start"/> with all '_' characters removed.
        /// </summary>
        /// <param name="s">The source string.</param>
        /// <param name="start">Inclusive start index.</param>
        /// <returns>The cleaned substring.</returns>
        private static string StripUnderscores(string s, int start)
        {
            int u = s.IndexOf('_', start);
            if (u < 0) return start == 0 ? s : s.Substring(start);
            var sb = new StringBuilder(s.Length - start);
            for (int i = start; i < s.Length; i++)
            {
                char c = s[i];
                if (c != '_') sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Converts a string of '0'/'1' digits to an unprefixed lower-case hex string, left-padding
        /// to a nibble boundary. Throws FormatException if any non-binary digit is present.
        /// </summary>
        /// <param name="bits">The binary-digit string (no prefix).</param>
        /// <returns>A lower-case hex string.</returns>
        /// <exception cref="FormatException">If <paramref name="bits"/> contains a non-binary character.</exception>
        private static string BinaryToHex(string bits)
        {
            for (int i = 0; i < bits.Length; i++)
            {
                char c = bits[i];
                if (c != '0' && c != '1')
                    throw new FormatException($"Invalid binary digit '{c}'.");
            }
            int pad = (4 - (bits.Length & 3)) & 3;
            int totalBits = bits.Length + pad;
            var sb = new StringBuilder(totalBits / 4);
            int bitIndex = -pad;
            const string HEX_DIGITS = "0123456789abcdef";
            for (int nib = 0; nib < totalBits / 4; nib++)
            {
                int nibble = 0;
                for (int b = 0; b < 4; b++)
                {
                    nibble <<= 1;
                    if (bitIndex >= 0) nibble |= bits[bitIndex] - '0';
                    bitIndex++;
                }
                sb.Append(HEX_DIGITS[nibble]);
            }
            return sb.ToString();
        }
    }
}
