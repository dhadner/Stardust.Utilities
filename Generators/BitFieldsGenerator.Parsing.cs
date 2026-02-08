using System.Text;

namespace Stardust.Generators;

public partial class BitFieldsGenerator
{
    /// <summary>
    /// Generates implicit/explicit conversion operators between the BitFields type and its storage/user-facing types.
    /// </summary>
    private static void GenerateConversions(StringBuilder sb, BitFieldsInfo info, string indent)
    {
        if (info.Mode == StorageMode.NativeFloat)
        {
            string fp = info.FloatingPointType!;
            string toBits = ToBitsMethod(fp);
            string fromBits = FromBitsMethod(fp);

            // Implicit float/double/Half conversions
            sb.AppendLine($"{indent}/// <summary>Implicit conversion to {fp}.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static implicit operator {fp}({info.TypeName} value) => {fromBits}(value.Value);");
            sb.AppendLine();

            sb.AppendLine($"{indent}/// <summary>Implicit conversion from {fp}.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static implicit operator {info.TypeName}({fp} value) => new({toBits}(value));");
            sb.AppendLine();

            // Explicit raw-bits conversions (for low-level access)
            sb.AppendLine($"{indent}/// <summary>Explicit conversion to raw bits ({info.StorageType}).</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static explicit operator {info.StorageType}({info.TypeName} value) => value.Value;");
            sb.AppendLine();

            sb.AppendLine($"{indent}/// <summary>Explicit conversion from raw bits ({info.StorageType}).</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static explicit operator {info.TypeName}({info.StorageType} value) => new(value);");
            sb.AppendLine();
        }
        else
        {
            // NativeInteger: standard implicit conversions
            // Note: The constructor handles undefined bits masking, so conversions just call new()
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static implicit operator {info.StorageType}({info.TypeName} value) => value.Value;");
            sb.AppendLine();
            
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static implicit operator {info.TypeName}({info.StorageType} value) => new(value);");
            sb.AppendLine();
        }

        // For small types, add implicit conversion from int to allow intuitive patterns like:
        //   MyBitFields x = (bits >> n) & 1;
        if (info.StorageType == "byte" || info.StorageType == "sbyte" || 
            info.StorageType == "short" || info.StorageType == "ushort")
        {
            sb.AppendLine($"{indent}/// <summary>Implicit conversion from int. Truncates to storage type.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static implicit operator {info.TypeName}(int value) => new(unchecked(({info.StorageType})value));");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates parsing methods implementing IParsable&lt;T&gt; and ISpanParsable&lt;T&gt;.
    /// Handles decimal, hex (0x/0X prefix), and binary (0b/0B prefix) formats.
    /// Supports C#-style underscore digit separators in all formats.
    /// </summary>
    private static void GenerateParsingMethods(StringBuilder sb, BitFieldsInfo info, string indent)
    {
        string t = info.TypeName;
        string s = info.StorageType;

        if (info.Mode == StorageMode.NativeFloat)
        {
            string fp = info.FloatingPointType!;
            string toBits = ToBitsMethod(fp);

            // For NativeFloat: parse as Half/float/double, convert to bits
            sb.AppendLine($"{indent}/// <summary>Parses a string into a {t} by parsing a {fp} value.</summary>");
            sb.AppendLine($"{indent}public static {t} Parse(string s, IFormatProvider? provider)");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    ArgumentNullException.ThrowIfNull(s);");
            sb.AppendLine($"{indent}    return new({toBits}({fp}.Parse(s, provider)));");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();

            sb.AppendLine($"{indent}/// <summary>Tries to parse a string into a {t}.</summary>");
            sb.AppendLine($"{indent}public static bool TryParse(string? s, IFormatProvider? provider, out {t} result)");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    if (s is not null && {fp}.TryParse(s, provider, out var fpValue))");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        result = new({toBits}(fpValue));");
            sb.AppendLine($"{indent}        return true;");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}    result = default;");
            sb.AppendLine($"{indent}    return false;");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();

            sb.AppendLine($"{indent}/// <summary>Parses a span of characters into a {t}.</summary>");
            sb.AppendLine($"{indent}public static {t} Parse(ReadOnlySpan<char> s, IFormatProvider? provider)");
            sb.AppendLine($"{indent}    => new({toBits}({fp}.Parse(s, provider)));");
            sb.AppendLine();

            sb.AppendLine($"{indent}/// <summary>Tries to parse a span of characters into a {t}.</summary>");
            sb.AppendLine($"{indent}public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out {t} result)");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    if ({fp}.TryParse(s, provider, out var fpValue))");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        result = new({toBits}(fpValue));");
            sb.AppendLine($"{indent}        return true;");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}    result = default;");
            sb.AppendLine($"{indent}    return false;");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();

            sb.AppendLine($"{indent}/// <summary>Parses a string using invariant culture.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static {t} Parse(string s) => Parse(s, CultureInfo.InvariantCulture);");
            sb.AppendLine();

            sb.AppendLine($"{indent}/// <summary>Tries to parse a string using invariant culture.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static bool TryParse(string? s, out {t} result) => TryParse(s, CultureInfo.InvariantCulture, out result);");
            sb.AppendLine();
            return;
        }

        // Helper methods for detecting prefixes and removing underscores
        sb.AppendLine($"{indent}private static bool IsHexPrefix(ReadOnlySpan<char> s) => s.Length >= 2 && s[0] == '0' && (s[1] == 'x' || s[1] == 'X');");
        sb.AppendLine($"{indent}private static bool IsBinaryPrefix(ReadOnlySpan<char> s) => s.Length >= 2 && s[0] == '0' && (s[1] == 'b' || s[1] == 'B');");
        sb.AppendLine();
        sb.AppendLine($"{indent}private static string RemoveUnderscores(ReadOnlySpan<char> s)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    // Fast path: no underscores");
        sb.AppendLine($"{indent}    bool hasUnderscore = false;");
        sb.AppendLine($"{indent}    foreach (var c in s) {{ if (c == '_') {{ hasUnderscore = true; break; }} }}");
        sb.AppendLine($"{indent}    if (!hasUnderscore) return s.ToString();");
        sb.AppendLine();
        sb.AppendLine($"{indent}    // Remove underscores");
        sb.AppendLine($"{indent}    var sb = new System.Text.StringBuilder(s.Length);");
        sb.AppendLine($"{indent}    foreach (var c in s) {{ if (c != '_') sb.Append(c); }}");
        sb.AppendLine($"{indent}    return sb.ToString();");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
        sb.AppendLine($"{indent}private static {s} ParseBinary(ReadOnlySpan<char> s)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    var clean = RemoveUnderscores(s);");
        sb.AppendLine($"{indent}    return Convert.To{GetConvertMethodName(s)}(clean, 2);");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
        sb.AppendLine($"{indent}private static bool TryParseBinary(ReadOnlySpan<char> s, out {s} result)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    try");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        result = ParseBinary(s);");
        sb.AppendLine($"{indent}        return true;");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}    catch");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        result = default;");
        sb.AppendLine($"{indent}        return false;");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();

        // IParsable<T>.Parse(string, IFormatProvider?)
        sb.AppendLine($"{indent}/// <summary>Parses a string into a {t}. Supports decimal, hex (0x prefix), and binary (0b prefix) formats with optional underscores.</summary>");
        sb.AppendLine($"{indent}/// <param name=\"s\">The string to parse.</param>");
        sb.AppendLine($"{indent}/// <param name=\"provider\">An object that provides culture-specific formatting information.</param>");
        sb.AppendLine($"{indent}/// <returns>The parsed {t} value.</returns>");
        sb.AppendLine($"{indent}/// <exception cref=\"ArgumentNullException\">s is null.</exception>");
        sb.AppendLine($"{indent}public static {t} Parse(string s, IFormatProvider? provider)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    ArgumentNullException.ThrowIfNull(s);");
        sb.AppendLine($"{indent}    var span = s.AsSpan();");
        sb.AppendLine($"{indent}    if (IsBinaryPrefix(span))");
        sb.AppendLine($"{indent}        return new(ParseBinary(span.Slice(2)));");
        sb.AppendLine($"{indent}    if (IsHexPrefix(span))");
        sb.AppendLine($"{indent}        return new({s}.Parse(RemoveUnderscores(span.Slice(2)), NumberStyles.HexNumber, provider));");
        sb.AppendLine($"{indent}    return new({s}.Parse(RemoveUnderscores(span), NumberStyles.Integer, provider));");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();

        // IParsable<T>.TryParse(string?, IFormatProvider?, out T)
        sb.AppendLine($"{indent}/// <summary>Tries to parse a string into a {t}. Supports decimal, hex (0x prefix), and binary (0b prefix) formats with optional underscores.</summary>");
        sb.AppendLine($"{indent}/// <param name=\"s\">The string to parse.</param>");
        sb.AppendLine($"{indent}/// <param name=\"provider\">An object that provides culture-specific formatting information.</param>");
        sb.AppendLine($"{indent}/// <param name=\"result\">When this method returns, contains the parsed value if successful.</param>");
        sb.AppendLine($"{indent}/// <returns>true if parsing succeeded; otherwise, false.</returns>");
        sb.AppendLine($"{indent}public static bool TryParse(string? s, IFormatProvider? provider, out {t} result)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    if (s is null) {{ result = default; return false; }}");
        sb.AppendLine($"{indent}    var span = s.AsSpan();");
        sb.AppendLine($"{indent}    if (IsBinaryPrefix(span))");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        if (TryParseBinary(span.Slice(2), out var binValue))");
        sb.AppendLine($"{indent}        {{");
        sb.AppendLine($"{indent}            result = new(binValue);");
        sb.AppendLine($"{indent}            return true;");
        sb.AppendLine($"{indent}        }}");
        sb.AppendLine($"{indent}        result = default;");
        sb.AppendLine($"{indent}        return false;");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}    if (IsHexPrefix(span))");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        if ({s}.TryParse(RemoveUnderscores(span.Slice(2)), NumberStyles.HexNumber, provider, out var hexValue))");
        sb.AppendLine($"{indent}        {{");
        sb.AppendLine($"{indent}            result = new(hexValue);");
        sb.AppendLine($"{indent}            return true;");
        sb.AppendLine($"{indent}        }}");
        sb.AppendLine($"{indent}        result = default;");
        sb.AppendLine($"{indent}        return false;");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}    if ({s}.TryParse(RemoveUnderscores(span), NumberStyles.Integer, provider, out var value))");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        result = new(value);");
        sb.AppendLine($"{indent}        return true;");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}    result = default;");
        sb.AppendLine($"{indent}    return false;");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();

        // ISpanParsable<T>.Parse(ReadOnlySpan<char>, IFormatProvider?)
        sb.AppendLine($"{indent}/// <summary>Parses a span of characters into a {t}. Supports decimal, hex (0x prefix), and binary (0b prefix) formats with optional underscores.</summary>");
        sb.AppendLine($"{indent}/// <param name=\"s\">The span of characters to parse.</param>");
        sb.AppendLine($"{indent}/// <param name=\"provider\">An object that provides culture-specific formatting information.</param>");
        sb.AppendLine($"{indent}/// <returns>The parsed {t} value.</returns>");
        sb.AppendLine($"{indent}public static {t} Parse(ReadOnlySpan<char> s, IFormatProvider? provider)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    if (IsBinaryPrefix(s))");
        sb.AppendLine($"{indent}        return new(ParseBinary(s.Slice(2)));");
        sb.AppendLine($"{indent}    if (IsHexPrefix(s))");
        sb.AppendLine($"{indent}        return new({s}.Parse(RemoveUnderscores(s.Slice(2)), NumberStyles.HexNumber, provider));");
        sb.AppendLine($"{indent}    return new({s}.Parse(RemoveUnderscores(s), NumberStyles.Integer, provider));");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();

        // ISpanParsable<T>.TryParse(ReadOnlySpan<char>, IFormatProvider?, out T)
        sb.AppendLine($"{indent}/// <summary>Tries to parse a span of characters into a {t}. Supports decimal, hex (0x prefix), and binary (0b prefix) formats with optional underscores.</summary>");
        sb.AppendLine($"{indent}/// <param name=\"s\">The span of characters to parse.</param>");
        sb.AppendLine($"{indent}/// <param name=\"provider\">An object that provides culture-specific formatting information.</param>");
        sb.AppendLine($"{indent}/// <param name=\"result\">When this method returns, contains the parsed value if successful.</param>");
        sb.AppendLine($"{indent}/// <returns>true if parsing succeeded; otherwise, false.</returns>");
        sb.AppendLine($"{indent}public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out {t} result)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    if (IsBinaryPrefix(s))");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        if (TryParseBinary(s.Slice(2), out var binValue))");
        sb.AppendLine($"{indent}        {{");
        sb.AppendLine($"{indent}            result = new(binValue);");
        sb.AppendLine($"{indent}            return true;");
        sb.AppendLine($"{indent}        }}");
        sb.AppendLine($"{indent}        result = default;");
        sb.AppendLine($"{indent}        return false;");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}    if (IsHexPrefix(s))");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        if ({s}.TryParse(RemoveUnderscores(s.Slice(2)), NumberStyles.HexNumber, provider, out var hexValue))");
        sb.AppendLine($"{indent}        {{");
        sb.AppendLine($"{indent}            result = new(hexValue);");
        sb.AppendLine($"{indent}            return true;");
        sb.AppendLine($"{indent}        }}");
        sb.AppendLine($"{indent}        result = default;");
        sb.AppendLine($"{indent}        return false;");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}    if ({s}.TryParse(RemoveUnderscores(s), NumberStyles.Integer, provider, out var value))");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        result = new(value);");
        sb.AppendLine($"{indent}        return true;");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}    result = default;");
        sb.AppendLine($"{indent}    return false;");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();

        // Convenience overloads
        sb.AppendLine($"{indent}/// <summary>Parses a string into a {t} using invariant culture. Supports decimal, hex (0x prefix), and binary (0b prefix) formats with optional underscores.</summary>");
        sb.AppendLine($"{indent}/// <param name=\"s\">The string to parse.</param>");
        sb.AppendLine($"{indent}/// <returns>The parsed {t} value.</returns>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} Parse(string s) => Parse(s, CultureInfo.InvariantCulture);");
        sb.AppendLine();

        sb.AppendLine($"{indent}/// <summary>Tries to parse a string into a {t} using invariant culture. Supports decimal, hex (0x prefix), and binary (0b prefix) formats with optional underscores.</summary>");
        sb.AppendLine($"{indent}/// <param name=\"s\">The string to parse.</param>");
        sb.AppendLine($"{indent}/// <param name=\"result\">When this method returns, contains the parsed value if successful.</param>");
        sb.AppendLine($"{indent}/// <returns>true if parsing succeeded; otherwise, false.</returns>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static bool TryParse(string? s, out {t} result) => TryParse(s, CultureInfo.InvariantCulture, out result);");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates formatting methods implementing IFormattable and ISpanFormattable.
    /// </summary>
    private static void GenerateFormattingMethods(StringBuilder sb, BitFieldsInfo info, string indent)
    {
        string t = info.TypeName;

        // For NativeFloat, format the floating-point value rather than raw bits
        string fmtExpr = info.Mode == StorageMode.NativeFloat
            ? $"{FromBitsMethod(info.FloatingPointType!)}(Value)"
            : "Value";

        sb.AppendLine($"{indent}/// <summary>Formats the value using the specified format and format provider.</summary>");
        sb.AppendLine($"{indent}/// <param name=\"format\">The format to use, or null for the default format.</param>");
        sb.AppendLine($"{indent}/// <param name=\"formatProvider\">The provider to use for culture-specific formatting.</param>");
        sb.AppendLine($"{indent}/// <returns>The formatted string representation of the value.</returns>");
        sb.AppendLine($"{indent}public string ToString(string? format, IFormatProvider? formatProvider) => {fmtExpr}.ToString(format, formatProvider);");
        sb.AppendLine();

        sb.AppendLine($"{indent}/// <summary>Tries to format the value into the provided span of characters.</summary>");
        sb.AppendLine($"{indent}/// <param name=\"destination\">The span to write to.</param>");
        sb.AppendLine($"{indent}/// <param name=\"charsWritten\">The number of characters written.</param>");
        sb.AppendLine($"{indent}/// <param name=\"format\">The format to use.</param>");
        sb.AppendLine($"{indent}/// <param name=\"provider\">The provider to use for culture-specific formatting.</param>");
        sb.AppendLine($"{indent}/// <returns>true if the formatting was successful; otherwise, false.</returns>");
        sb.AppendLine($"{indent}public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)");
        sb.AppendLine($"{indent}    => Value.TryFormat(destination, out charsWritten, format, provider);");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates comparison and equality interface methods: IComparable, IComparable&lt;T&gt;, IEquatable&lt;T&gt;.
    /// </summary>
    private static void GenerateComparisonMethods(StringBuilder sb, BitFieldsInfo info, string indent)
    {
        string t = info.TypeName;

        sb.AppendLine($"{indent}/// <summary>Compares this instance to a specified object and returns an integer indicating their relative order.</summary>");
        sb.AppendLine($"{indent}/// <param name=\"obj\">An object to compare, or null.</param>");
        sb.AppendLine($"{indent}/// <returns>A value indicating the relative order of the objects being compared.</returns>");
        sb.AppendLine($"{indent}/// <exception cref=\"ArgumentException\">obj is not a {t}.</exception>");
        sb.AppendLine($"{indent}public int CompareTo(object? obj)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    if (obj is null) return 1;");
        sb.AppendLine($"{indent}    if (obj is {t} other) return CompareTo(other);");
        sb.AppendLine($"{indent}    throw new ArgumentException(\"Object must be of type {t}\", nameof(obj));");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();

        sb.AppendLine($"{indent}/// <summary>Compares this instance to another {t} and returns an integer indicating their relative order.</summary>");
        sb.AppendLine($"{indent}/// <param name=\"other\">A {t} to compare.</param>");
        sb.AppendLine($"{indent}/// <returns>A value indicating the relative order of the instances being compared.</returns>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        if (info.Mode == StorageMode.NativeFloat)
        {
            string fromBits = FromBitsMethod(info.FloatingPointType!);
            sb.AppendLine($"{indent}public int CompareTo({t} other) => {fromBits}(Value).CompareTo({fromBits}(other.Value));");
        }
        else
        {
            sb.AppendLine($"{indent}public int CompareTo({t} other) => Value.CompareTo(other.Value);");
        }
        sb.AppendLine();

        sb.AppendLine($"{indent}/// <summary>Indicates whether this instance is equal to another {t}.</summary>");
        sb.AppendLine($"{indent}/// <param name=\"other\">A {t} to compare with this instance.</param>");
        sb.AppendLine($"{indent}/// <returns>true if the two instances are equal; otherwise, false.</returns>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public bool Equals({t} other) => Value == other.Value;");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates byte-span construction and serialization methods for single-word BitFields.
    /// </summary>
    private static void GenerateByteSpanMethods(StringBuilder sb, BitFieldsInfo info, string indent)
    {
        string t = info.TypeName;
        string s = info.StorageType;
        int sizeInBytes = GetStorageTypeBitWidth(s) / 8;
        string? readMethod = GetBinaryPrimitivesReadMethod(s);
        string? writeMethod = GetBinaryPrimitivesWriteMethod(s);
        bool isByte = s == "byte" || s == "sbyte";

        // For NativeFloat types, use the unsigned storage type for binary reads/writes
        string binaryType = s; // The type used with BinaryPrimitives
        if (info.Mode == StorageMode.NativeFloat)
            binaryType = info.StorageType; // already uint or ulong

        // ReadOnlySpan<byte> constructor
        sb.AppendLine($"{indent}/// <summary>Creates a new {t} from a little-endian byte span.</summary>");
        sb.AppendLine($"{indent}/// <param name=\"bytes\">The source span. Must contain at least <see cref=\"SizeInBytes\"/> bytes.</param>");
        sb.AppendLine($"{indent}/// <exception cref=\"ArgumentException\">The span is too short.</exception>");
        sb.AppendLine($"{indent}public {t}(ReadOnlySpan<byte> bytes)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    if (bytes.Length < SizeInBytes)");
        sb.AppendLine($"{indent}        throw new ArgumentException($\"Span must contain at least {{SizeInBytes}} bytes.\", nameof(bytes));");
        if (isByte)
        {
            if (s == "sbyte")
                sb.AppendLine($"{indent}    Value = unchecked((sbyte)bytes[0]);");
            else
                sb.AppendLine($"{indent}    Value = bytes[0];");
        }
        else
            sb.AppendLine($"{indent}    Value = BinaryPrimitives.{readMethod}(bytes);");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();

        // Static ReadFrom factory
        sb.AppendLine($"{indent}/// <summary>Creates a new {t} by reading <see cref=\"SizeInBytes\"/> bytes from a little-endian byte span.</summary>");
        sb.AppendLine($"{indent}/// <param name=\"bytes\">The source span. Must contain at least <see cref=\"SizeInBytes\"/> bytes.</param>");
        sb.AppendLine($"{indent}/// <returns>The deserialized {t}.</returns>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} ReadFrom(ReadOnlySpan<byte> bytes) => new(bytes);");
        sb.AppendLine();

        // WriteTo
        sb.AppendLine($"{indent}/// <summary>Writes the value as little-endian bytes into the destination span.</summary>");
        sb.AppendLine($"{indent}/// <param name=\"destination\">The destination span. Must contain at least <see cref=\"SizeInBytes\"/> bytes.</param>");
        sb.AppendLine($"{indent}/// <exception cref=\"ArgumentException\">The span is too short.</exception>");
        sb.AppendLine($"{indent}public void WriteTo(Span<byte> destination)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    if (destination.Length < SizeInBytes)");
        sb.AppendLine($"{indent}        throw new ArgumentException($\"Span must contain at least {{SizeInBytes}} bytes.\", nameof(destination));");
        if (isByte)
            sb.AppendLine($"{indent}    destination[0] = unchecked((byte)Value);");
        else
            sb.AppendLine($"{indent}    BinaryPrimitives.{writeMethod}(destination, Value);");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();

        // TryWriteTo
        sb.AppendLine($"{indent}/// <summary>Attempts to write the value as little-endian bytes into the destination span.</summary>");
        sb.AppendLine($"{indent}/// <param name=\"destination\">The destination span.</param>");
        sb.AppendLine($"{indent}/// <param name=\"bytesWritten\">The number of bytes written on success.</param>");
        sb.AppendLine($"{indent}/// <returns>true if the destination span was large enough; otherwise, false.</returns>");
        sb.AppendLine($"{indent}public bool TryWriteTo(Span<byte> destination, out int bytesWritten)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    if (destination.Length < SizeInBytes)");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        bytesWritten = 0;");
        sb.AppendLine($"{indent}        return false;");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}    WriteTo(destination);");
        sb.AppendLine($"{indent}    bytesWritten = SizeInBytes;");
        sb.AppendLine($"{indent}    return true;");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();

        // ToByteArray
        sb.AppendLine($"{indent}/// <summary>Returns the value as a new little-endian byte array.</summary>");
        sb.AppendLine($"{indent}/// <returns>A byte array of length <see cref=\"SizeInBytes\"/>.</returns>");
        sb.AppendLine($"{indent}public byte[] ToByteArray()");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    var bytes = new byte[SizeInBytes];");
        sb.AppendLine($"{indent}    WriteTo(bytes);");
        sb.AppendLine($"{indent}    return bytes;");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates a nested JsonConverter class for single-word BitFields that round-trips via ToString/Parse.
    /// </summary>
    private static void GenerateJsonConverter(StringBuilder sb, BitFieldsInfo info, string indent)
    {
        string t = info.TypeName;

        sb.AppendLine($"{indent}/// <summary>JSON converter that serializes {t} as a string.</summary>");
        sb.AppendLine($"{indent}private sealed class {t}JsonConverter : JsonConverter<{t}>");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    /// <summary>Reads a {t} from a JSON string.</summary>");
        sb.AppendLine($"{indent}    public override {t} Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        var s = reader.GetString();");
        sb.AppendLine($"{indent}        return s is null ? default : {t}.Parse(s);");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine();
        sb.AppendLine($"{indent}    /// <summary>Writes a {t} to JSON as a string.</summary>");
        sb.AppendLine($"{indent}    public override void Write(Utf8JsonWriter writer, {t} value, JsonSerializerOptions options)");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        writer.WriteStringValue(value.ToString());");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }
}
