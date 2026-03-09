using FluentAssertions;
using System.Globalization;
using System.Text;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Fuzz tests for parsing methods to ensure robustness against malformed,
/// malicious, or unexpected input. These tests verify that parsing methods
/// do not crash, throw unexpected exceptions, or exhibit security vulnerabilities.
/// </summary>
public partial class ParsingFuzzTests
{
    #region Malformed Input Tests

    /// <summary>
    /// Tests that TryParse handles null input gracefully.
    /// </summary>
    [Theory]
    [InlineData(null)]
    public void TryParse_NullInput_ReturnsFalse(string? input)
    {
        UInt16Be.TryParse(input, null, out _).Should().BeFalse();
        UInt32Be.TryParse(input, null, out _).Should().BeFalse();
        UInt64Be.TryParse(input, null, out _).Should().BeFalse();
        Int16Be.TryParse(input, null, out _).Should().BeFalse();
        Int32Be.TryParse(input, null, out _).Should().BeFalse();
        Int64Be.TryParse(input, null, out _).Should().BeFalse();
    }

    /// <summary>
    /// Tests that TryParse handles empty strings gracefully.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void TryParse_EmptyOrWhitespace_ReturnsFalse(string input)
    {
        UInt16Be.TryParse(input, null, out _).Should().BeFalse();
        UInt32Be.TryParse(input, null, out _).Should().BeFalse();
        UInt64Be.TryParse(input, null, out _).Should().BeFalse();
        Int16Be.TryParse(input, null, out _).Should().BeFalse();
        Int32Be.TryParse(input, null, out _).Should().BeFalse();
        Int64Be.TryParse(input, null, out _).Should().BeFalse();
    }

    /// <summary>
    /// Tests that TryParse handles various non-numeric strings gracefully.
    /// </summary>
    [Theory]
    [InlineData("abc")]
    [InlineData("hello")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("null")]
    [InlineData("undefined")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("()")]
    public void TryParse_NonNumericStrings_ReturnsFalse(string input)
    {
        UInt16Be.TryParse(input, null, out _).Should().BeFalse();
        UInt32Be.TryParse(input, null, out _).Should().BeFalse();
        UInt64Be.TryParse(input, null, out _).Should().BeFalse();
        Int16Be.TryParse(input, null, out _).Should().BeFalse();
        Int32Be.TryParse(input, null, out _).Should().BeFalse();
        Int64Be.TryParse(input, null, out _).Should().BeFalse();
    }

    #endregion

    #region Overflow and Boundary Tests

    /// <summary>
    /// Tests that TryParse handles values exceeding type limits gracefully.
    /// </summary>
    [Fact]
    public void TryParse_UInt16Be_Overflow_ReturnsFalse()
    {
        // Values larger than ushort.MaxValue (65535)
        UInt16Be.TryParse("65536", null, out _).Should().BeFalse();
        UInt16Be.TryParse("100000", null, out _).Should().BeFalse();
        UInt16Be.TryParse("999999999999999999", null, out _).Should().BeFalse();
        UInt16Be.TryParse("-1", null, out _).Should().BeFalse(); // Negative for unsigned
    }

    /// <summary>
    /// Tests that TryParse handles values exceeding type limits gracefully.
    /// </summary>
    [Fact]
    public void TryParse_UInt32Be_Overflow_ReturnsFalse()
    {
        // Values larger than uint.MaxValue (4294967295)
        UInt32Be.TryParse("4294967296", null, out _).Should().BeFalse();
        UInt32Be.TryParse("99999999999999999999", null, out _).Should().BeFalse();
        UInt32Be.TryParse("-1", null, out _).Should().BeFalse(); // Negative for unsigned
    }

    /// <summary>
    /// Tests that TryParse handles values exceeding type limits gracefully.
    /// </summary>
    [Fact]
    public void TryParse_UInt64Be_Overflow_ReturnsFalse()
    {
        // Values larger than ulong.MaxValue (18446744073709551615)
        UInt64Be.TryParse("18446744073709551616", null, out _).Should().BeFalse();
        UInt64Be.TryParse("999999999999999999999999999999", null, out _).Should().BeFalse();
        UInt64Be.TryParse("-1", null, out _).Should().BeFalse(); // Negative for unsigned
    }

    /// <summary>
    /// Tests that TryParse handles signed type overflow correctly.
    /// </summary>
    [Fact]
    public void TryParse_Int16Be_Overflow_ReturnsFalse()
    {
        // Values outside short range (-32768 to 32767)
        Int16Be.TryParse("32768", null, out _).Should().BeFalse();
        Int16Be.TryParse("-32769", null, out _).Should().BeFalse();
        Int16Be.TryParse("100000", null, out _).Should().BeFalse();
    }

    /// <summary>
    /// Tests boundary values parse correctly.
    /// </summary>
    [Fact]
    public void TryParse_BoundaryValues_Success()
    {
        // UInt16Be boundaries
        UInt16Be.TryParse("0", null, out var u16).Should().BeTrue();
        ((ushort)u16).Should().Be(0);
        UInt16Be.TryParse("65535", null, out u16).Should().BeTrue();
        ((ushort)u16).Should().Be(ushort.MaxValue);

        // UInt32Be boundaries
        UInt32Be.TryParse("0", null, out var u32).Should().BeTrue();
        ((uint)u32).Should().Be(0);
        UInt32Be.TryParse("4294967295", null, out u32).Should().BeTrue();
        ((uint)u32).Should().Be(uint.MaxValue);

        // Int16Be boundaries
        Int16Be.TryParse("-32768", null, out var i16).Should().BeTrue();
        ((short)i16).Should().Be(short.MinValue);
        Int16Be.TryParse("32767", null, out i16).Should().BeTrue();
        ((short)i16).Should().Be(short.MaxValue);
    }

    #endregion

    #region Injection and Security Tests

    /// <summary>
    /// Tests that TryParse rejects format string injection attempts.
    /// </summary>
    [Theory]
    [InlineData("{0}")]
    [InlineData("{{0}}")]
    [InlineData("{0:X}")]
    [InlineData("%s")]
    [InlineData("%d")]
    [InlineData("%n")]
    [InlineData("%x")]
    [InlineData("${{7*7}}")]  // Template injection
    [InlineData("${7*7}")]
    [InlineData("#{7*7}")]
    public void TryParse_FormatStringInjection_ReturnsFalse(string input)
    {
        UInt16Be.TryParse(input, null, out _).Should().BeFalse();
        UInt32Be.TryParse(input, null, out _).Should().BeFalse();
        UInt64Be.TryParse(input, null, out _).Should().BeFalse();
    }

    /// <summary>
    /// Tests that TryParse rejects SQL injection patterns.
    /// </summary>
    [Theory]
    [InlineData("1; DROP TABLE users;")]
    [InlineData("1 OR 1=1")]
    [InlineData("1' OR '1'='1")]
    [InlineData("1; SELECT * FROM users")]
    [InlineData("1 UNION SELECT 1")]
    [InlineData("1--")]
    [InlineData("1/*comment*/")]
    public void TryParse_SqlInjection_ReturnsFalse(string input)
    {
        UInt16Be.TryParse(input, null, out _).Should().BeFalse();
        UInt32Be.TryParse(input, null, out _).Should().BeFalse();
        UInt64Be.TryParse(input, null, out _).Should().BeFalse();
    }

    /// <summary>
    /// Tests that TryParse rejects command injection patterns.
    /// </summary>
    [Theory]
    [InlineData("1; ls")]
    [InlineData("1 && cat /etc/passwd")]
    [InlineData("1 | rm -rf /")]
    [InlineData("`whoami`")]
    [InlineData("$(whoami)")]
    [InlineData("1\n ls")]
    [InlineData("1\r\nls")]
    public void TryParse_CommandInjection_ReturnsFalse(string input)
    {
        UInt16Be.TryParse(input, null, out _).Should().BeFalse();
        UInt32Be.TryParse(input, null, out _).Should().BeFalse();
        UInt64Be.TryParse(input, null, out _).Should().BeFalse();
    }

    /// <summary>
    /// Tests that TryParse rejects XSS patterns.
    /// </summary>
    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("javascript:alert(1)")]
    [InlineData("1<script>")]
    [InlineData("1</script>")]
    [InlineData("<1>")]
    public void TryParse_XssInjection_ReturnsFalse(string input)
    {
        UInt16Be.TryParse(input, null, out _).Should().BeFalse();
        UInt32Be.TryParse(input, null, out _).Should().BeFalse();
        UInt64Be.TryParse(input, null, out _).Should().BeFalse();
    }

    /// <summary>
    /// Tests that TryParse rejects path traversal patterns.
    /// </summary>
    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\..\\windows\\system32")]
    [InlineData("....//....//")]
    [InlineData("1/../2")]
    public void TryParse_PathTraversal_ReturnsFalse(string input)
    {
        UInt16Be.TryParse(input, null, out _).Should().BeFalse();
        UInt32Be.TryParse(input, null, out _).Should().BeFalse();
        UInt64Be.TryParse(input, null, out _).Should().BeFalse();
    }

    #endregion

    #region Special Characters and Unicode Tests

    /// <summary>
    /// Tests that TryParse handles special characters gracefully.
    /// Note: Some inputs with embedded nulls may still parse if the .NET parser
    /// treats the null as a string terminator.
    /// </summary>
    [Theory]
    [InlineData("\0")]           // Null character alone
    [InlineData("\x00\x00")]     // Multiple nulls
    [InlineData("\x7F")]         // DEL character
    [InlineData("\x1B")]         // ESC character
    public void TryParse_ControlCharacters_ReturnsFalse(string input)
    {
        UInt16Be.TryParse(input, null, out _).Should().BeFalse();
        UInt32Be.TryParse(input, null, out _).Should().BeFalse();
        UInt64Be.TryParse(input, null, out _).Should().BeFalse();
    }

    /// <summary>
    /// Tests that TryParse with embedded nulls behaves consistently.
    /// The behavior may vary - the parser might see the string up to the null.
    /// </summary>
    [Fact]
    public void TryParse_EmbeddedNulls_DoesNotCrash()
    {
        // These should not crash regardless of whether they parse or not
        var action = () =>
        {
            UInt16Be.TryParse("\0123", null, out _);  // Null before digits
            UInt16Be.TryParse("123\0", null, out _);  // Null after digits  
            UInt16Be.TryParse("12\03", null, out _);  // Embedded null (note: \03 is octal for ETX)
        };
        action.Should().NotThrow();
    }

    /// <summary>
    /// Provides Unicode digit variant test data with descriptive names.
    /// </summary>
    public static TheoryData<string, string> UnicodeDigitVariantsData => new()
    {
        { "???", "Full-width digits" },
        { "???", "Circled digits" },
        { "???", "Parenthesized digits" },
        { "???", "Subscript digits" },
        { "¹²³", "Superscript digits" },
        { "???", "Devanagari digits" },
        { "????", "Arabic-Indic digits" },
        { "????", "Extended Arabic-Indic digits" },
    };

    /// <summary>
    /// Tests that TryParse handles Unicode confusables/homoglyphs correctly.
    /// </summary>
    [Theory]
    [MemberData(nameof(UnicodeDigitVariantsData))]
    public void TryParse_UnicodeDigitVariants_ReturnsFalse(string input, string description)
    {
        // These should fail because they're not ASCII digits
        _ = description; // Used for test identification
        UInt16Be.TryParse(input, null, out _).Should().BeFalse();
        UInt32Be.TryParse(input, null, out _).Should().BeFalse();
        UInt64Be.TryParse(input, null, out _).Should().BeFalse();
    }

    /// <summary>
    /// Tests that TryParse handles Unicode BOM and zero-width characters.
    /// </summary>
    [Theory]
    [InlineData("\uFEFF123")]    // BOM prefix
    [InlineData("123\uFEFF")]    // BOM suffix
    [InlineData("\u200B123")]    // Zero-width space prefix
    [InlineData("1\u200B23")]    // Embedded zero-width space
    [InlineData("\u200C123")]    // Zero-width non-joiner
    [InlineData("\u200D123")]    // Zero-width joiner
    [InlineData("\u2060123")]    // Word joiner
    public void TryParse_InvisibleCharacters_ReturnsFalse(string input)
    {
        UInt16Be.TryParse(input, null, out _).Should().BeFalse();
        UInt32Be.TryParse(input, null, out _).Should().BeFalse();
        UInt64Be.TryParse(input, null, out _).Should().BeFalse();
    }

    /// <summary>
    /// Tests that TryParse handles RTL override characters.
    /// </summary>
    [Theory]
    [InlineData("\u202E123")]    // Right-to-left override
    [InlineData("\u202D123")]    // Left-to-right override
    [InlineData("1\u202E23")]    // Embedded RTL override
    public void TryParse_BidiOverrideCharacters_ReturnsFalse(string input)
    {
        UInt16Be.TryParse(input, null, out _).Should().BeFalse();
        UInt32Be.TryParse(input, null, out _).Should().BeFalse();
        UInt64Be.TryParse(input, null, out _).Should().BeFalse();
    }

    #endregion

    #region Extreme Length Tests

    /// <summary>
    /// Tests that TryParse handles extremely long strings gracefully (no crash or hang).
    /// </summary>
    [Fact]
    public void TryParse_ExtremelyLongString_DoesNotCrash()
    {
        // 1 million characters of '1'
        string longInput = new('1', 1_000_000);
        
        // Should not throw, should just return false (overflow)
        var action16 = () => UInt16Be.TryParse(longInput, null, out _);
        var action32 = () => UInt32Be.TryParse(longInput, null, out _);
        var action64 = () => UInt64Be.TryParse(longInput, null, out _);

        action16.Should().NotThrow();
        action32.Should().NotThrow();
        action64.Should().NotThrow();
    }

    /// <summary>
    /// Tests that TryParse handles repeated patterns gracefully.
    /// </summary>
    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void TryParse_RepeatedPatterns_DoesNotCrash(int repeatCount)
    {
        string repeatedZeros = new('0', repeatCount);
        string repeatedNines = new('9', repeatCount);
        string repeatedMixed = string.Concat(Enumerable.Repeat("12", repeatCount));

        // Should not throw
        var action = () =>
        {
            UInt64Be.TryParse(repeatedZeros, null, out _);
            UInt64Be.TryParse(repeatedNines, null, out _);
            UInt64Be.TryParse(repeatedMixed, null, out _);
        };

        action.Should().NotThrow();
    }

    #endregion

    #region BitField Parsing Fuzz Tests

    /// <summary>
    /// Test struct for BitField parsing tests.
    /// </summary>
    [BitFields(typeof(byte))]
    public partial struct FuzzTestReg8
    {
        [BitFlag(0)] public partial bool Flag0 { get; set; }
        [BitField(1, 4)] public partial byte Field1 { get; set; }
    }

    /// <summary>
    /// Tests that BitField TryParse handles malformed hex input.
    /// </summary>
    [Theory]
    [InlineData("0x")]           // Empty hex
    [InlineData("0xG")]          // Invalid hex digit
    [InlineData("0xZZ")]         // Invalid hex digits
    [InlineData("0x12345678901234567890")]  // Too long
    [InlineData("0X")]           // Empty hex uppercase
    [InlineData("0x ")]          // Hex with space
    [InlineData(" 0x1")]         // Leading space before hex
    public void BitField_TryParse_MalformedHex_ReturnsFalse(string input)
    {
        FuzzTestReg8.TryParse(input, null, out _).Should().BeFalse();
    }

    /// <summary>
    /// Tests that BitField TryParse handles malformed binary input.
    /// </summary>
    [Theory]
    [InlineData("0b")]           // Empty binary
    [InlineData("0b2")]          // Invalid binary digit
    [InlineData("0b123")]        // Contains non-binary
    [InlineData("0bABC")]        // Contains letters
    [InlineData("0B")]           // Empty binary uppercase
    [InlineData("0b ")]          // Binary with space
    public void BitField_TryParse_MalformedBinary_ReturnsFalse(string input)
    {
        FuzzTestReg8.TryParse(input, null, out _).Should().BeFalse();
    }

    /// <summary>
    /// Tests that BitField TryParse handles underscore edge cases.
    /// Note: The parser is permissive with underscores - it strips them before parsing.
    /// This matches the behavior of stripping underscores for C#-style digit separators.
    /// </summary>
    [Theory]
    [InlineData("___")]          // Only underscores - becomes empty after stripping
    public void BitField_TryParse_MalformedUnderscores_ReturnsFalse(string input)
    {
        FuzzTestReg8.TryParse(input, null, out _).Should().BeFalse();
    }

    /// <summary>
    /// Tests that BitField TryParse accepts permissive underscore patterns.
    /// The parser strips all underscores before parsing, which is intentionally permissive.
    /// </summary>
    [Theory]
    [InlineData("_123", 123)]        // Leading underscore stripped
    [InlineData("123_", 123)]        // Trailing underscore stripped
    [InlineData("1__23", 123)]       // Double underscore stripped
    [InlineData("_1_2_3_", 123)]     // Multiple underscores stripped
    [InlineData("0x_FF", 255)]       // Underscore after hex prefix stripped
    [InlineData("0b_1010", 10)]      // Underscore after binary prefix stripped
    public void BitField_TryParse_PermissiveUnderscores_Succeeds(string input, byte expected)
    {
        FuzzTestReg8.TryParse(input, null, out var result).Should().BeTrue();
        ((byte)result).Should().Be(expected);
    }

    /// <summary>
    /// Tests that valid BitField inputs still parse correctly after fuzz testing.
    /// </summary>
    [Theory]
    [InlineData("0", 0)]
    [InlineData("255", 255)]
    [InlineData("0xFF", 255)]
    [InlineData("0b11111111", 255)]
    [InlineData("1_2_3", 123)]
    [InlineData("0x1_F", 31)]
    [InlineData("0b1111_0000", 240)]
    public void BitField_TryParse_ValidInputs_Success(string input, byte expected)
    {
        FuzzTestReg8.TryParse(input, null, out var result).Should().BeTrue();
        ((byte)result).Should().Be(expected);
    }

    #endregion

    #region Parse (throwing) Fuzz Tests

    /// <summary>
    /// Tests that Parse throws appropriate exceptions for invalid input.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("12.34")]
    [InlineData("1e10")]
    public void Parse_InvalidInput_ThrowsFormatException(string input)
    {
        var action16 = () => UInt16Be.Parse(input, null);
        var action32 = () => UInt32Be.Parse(input, null);
        var action64 = () => UInt64Be.Parse(input, null);

        action16.Should().Throw<FormatException>();
        action32.Should().Throw<FormatException>();
        action64.Should().Throw<FormatException>();
    }

    /// <summary>
    /// Tests that Parse throws OverflowException for values too large.
    /// </summary>
    [Fact]
    public void Parse_Overflow_ThrowsOverflowException()
    {
        var action16 = () => UInt16Be.Parse("65536", null);
        var action32 = () => UInt32Be.Parse("4294967296", null);

        action16.Should().Throw<OverflowException>();
        action32.Should().Throw<OverflowException>();
    }

    #endregion

    #region Random Fuzzing Tests

    /// <summary>
    /// Tests parsing with random garbage data.
    /// </summary>
    [Fact]
    public void TryParse_RandomGarbage_DoesNotCrash()
    {
        var random = new Random(42); // Fixed seed for reproducibility

        for (int i = 0; i < 1000; i++)
        {
            // Generate random garbage string
            int length = random.Next(0, 100);
            var chars = new char[length];
            for (int j = 0; j < length; j++)
            {
                chars[j] = (char)random.Next(0, 65536);
            }
            string garbage = new(chars);

            // Should not throw - just return true or false
            var action = () =>
            {
                UInt16Be.TryParse(garbage, null, out _);
                UInt32Be.TryParse(garbage, null, out _);
                UInt64Be.TryParse(garbage, null, out _);
                Int16Be.TryParse(garbage, null, out _);
                Int32Be.TryParse(garbage, null, out _);
                Int64Be.TryParse(garbage, null, out _);
            };

            action.Should().NotThrow($"input was: {EscapeForDiagnostics(garbage)}");
        }
    }

    /// <summary>
    /// Tests parsing with random byte sequences interpreted as UTF-8.
    /// </summary>
    [Fact]
    public void TryParse_RandomBytes_DoesNotCrash()
    {
        var random = new Random(42); // Fixed seed for reproducibility

        for (int i = 0; i < 1000; i++)
        {
            // Generate random bytes
            int length = random.Next(0, 100);
            var bytes = new byte[length];
            random.NextBytes(bytes);
            
            // Try to interpret as string (may produce invalid Unicode)
            string input;
            try
            {
                input = Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                continue; // Skip if can't convert to string
            }

            // Should not throw
            var action = () =>
            {
                UInt16Be.TryParse(input, null, out _);
                UInt32Be.TryParse(input, null, out _);
                UInt64Be.TryParse(input, null, out _);
            };

            action.Should().NotThrow();
        }
    }

    /// <summary>
    /// Tests parsing with mutations of valid inputs.
    /// </summary>
    [Theory]
    [InlineData("12345")]
    [InlineData("0xABCD")]
    [InlineData("65535")]
    public void TryParse_MutatedValidInput_DoesNotCrash(string validInput)
    {
        var random = new Random(42);

        for (int i = 0; i < 100; i++)
        {
            string mutated = MutateString(validInput, random);

            // Should not throw
            var action = () =>
            {
                UInt16Be.TryParse(mutated, null, out _);
                UInt32Be.TryParse(mutated, null, out _);
                UInt64Be.TryParse(mutated, null, out _);
            };

            action.Should().NotThrow($"mutated input was: {EscapeForDiagnostics(mutated)}");
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Mutates a string randomly for fuzzing purposes.
    /// </summary>
    private static string MutateString(string input, Random random)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var chars = input.ToCharArray();
        int mutationType = random.Next(6);

        switch (mutationType)
        {
            case 0: // Bit flip in a random character
                int pos = random.Next(chars.Length);
                chars[pos] = (char)(chars[pos] ^ (1 << random.Next(8)));
                break;

            case 1: // Insert random character
                pos = random.Next(chars.Length + 1);
                char newChar = (char)random.Next(256);
                return input.Insert(pos, newChar.ToString());

            case 2: // Delete random character
                pos = random.Next(chars.Length);
                return input.Remove(pos, 1);

            case 3: // Replace with random character
                pos = random.Next(chars.Length);
                chars[pos] = (char)random.Next(256);
                break;

            case 4: // Swap two characters
                if (chars.Length > 1)
                {
                    int pos1 = random.Next(chars.Length);
                    int pos2 = random.Next(chars.Length);
                    (chars[pos1], chars[pos2]) = (chars[pos2], chars[pos1]);
                }
                break;

            case 5: // Duplicate string
                return input + input;
        }

        return new string(chars);
    }

    /// <summary>
    /// Escapes a string for diagnostic output.
    /// </summary>
    private static string EscapeForDiagnostics(string input)
    {
        if (input.Length > 50)
            input = input[..50] + "...";
        
        var sb = new StringBuilder();
        foreach (char c in input)
        {
            if (c < 32 || c > 126)
                sb.Append($"\\u{(int)c:X4}");
            else
                sb.Append(c);
        }
        return sb.ToString();
    }

    #endregion
}
