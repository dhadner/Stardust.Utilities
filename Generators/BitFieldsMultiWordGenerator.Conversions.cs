using System.Linq;
using System.Text;

namespace Stardust.Generators;

internal static partial class BitFieldsMultiWordGenerator
{
    private static void GenerateConversions(StringBuilder sb, BitFieldsInfo info, WordLayout layout, string ind)
    {
        string t = info.TypeName;

        sb.AppendLine($"{ind}/// <summary>Implicit conversion from ulong (zero-extended).</summary>");
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}public static implicit operator {t}(ulong value) => new(value);");
        sb.AppendLine();
        sb.AppendLine($"{ind}/// <summary>Implicit conversion from int.</summary>");
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}public static implicit operator {t}(int value) => new(value);");
        sb.AppendLine();
        sb.AppendLine($"{ind}/// <summary>Explicit conversion to BigInteger.</summary>");
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}public static explicit operator BigInteger({t} value) => value.ToBigInteger();");
        sb.AppendLine();
        sb.AppendLine($"{ind}/// <summary>Explicit conversion from BigInteger.</summary>");
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}public static explicit operator {t}(BigInteger value) => FromBigInteger(value);");
        sb.AppendLine();

        // NativeWide: add UInt128/Int128 conversion operators for 128-bit multi-word structs
        if (info.NativeWideType != null && layout.WordCount == 2)
        {
            string wt = info.NativeWideType; // "UInt128" or "Int128"
            bool isSigned = wt == "Int128";

            sb.AppendLine($"{ind}/// <summary>Implicit conversion to {wt}.</summary>");
            sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            if (isSigned)
                sb.AppendLine($"{ind}public static implicit operator {wt}({t} value) => (Int128)(((UInt128)value._w1 << 64) | value._w0);");
            else
                sb.AppendLine($"{ind}public static implicit operator {wt}({t} value) => ((UInt128)value._w1 << 64) | value._w0;");
            sb.AppendLine();

            sb.AppendLine($"{ind}/// <summary>Implicit conversion from {wt}.</summary>");
            sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            if (isSigned)
                sb.AppendLine($"{ind}public static implicit operator {t}({wt} value) {{ var u = (UInt128)value; return new((ulong)(u & ulong.MaxValue), (ulong)(u >> 64)); }}");
            else
                sb.AppendLine($"{ind}public static implicit operator {t}({wt} value) => new((ulong)(value & ulong.MaxValue), (ulong)(value >> 64));");
            sb.AppendLine();
        }
    }

    private static void GenerateBigIntegerHelpers(StringBuilder sb, BitFieldsInfo info, WordLayout layout, string ind)
    {
        string t = info.TypeName;
        int wc = layout.WordCount;

        sb.AppendLine($"{ind}/// <summary>Converts this value to a BigInteger.</summary>");
        sb.AppendLine($"{ind}public BigInteger ToBigInteger()");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}    BigInteger result = {layout.Read("", wc - 1)};");
        for (int i = wc - 2; i >= 0; i--)
            sb.AppendLine($"{ind}    result = (result << 64) | _w{i};");
        sb.AppendLine($"{ind}    return result;");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();

        sb.AppendLine($"{ind}/// <summary>Creates a {t} from a BigInteger (truncated to {info.TotalBits} bits).</summary>");
        sb.AppendLine($"{ind}public static {t} FromBigInteger(BigInteger value)");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}    if (value.Sign < 0) value = (BigInteger.One << TotalBits) + value;");
        for (int i = 0; i < wc; i++)
        {
            if (i == 0)
                sb.AppendLine($"{ind}    ulong w0 = (ulong)(value & ulong.MaxValue);");
            else
            {
                sb.AppendLine($"{ind}    value >>= 64;");
                sb.AppendLine($"{ind}    ulong w{i} = (ulong)(value & ulong.MaxValue);");
            }
        }
        var ctorExprs = Enumerable.Range(0, wc).Select(i =>
            layout.IsRemainder(i) ? $"({layout.RemainderType})w{i}" : $"w{i}").ToArray();
        EmitReturnConstruction(sb, t, layout, $"{ind}    ", ctorExprs);
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();
    }

    private static void GenerateParsingMethods(StringBuilder sb, BitFieldsInfo info, string ind)
    {
        string t = info.TypeName;

        sb.AppendLine($"{ind}private static bool IsHexPrefix(ReadOnlySpan<char> s) => s.Length >= 2 && s[0] == '0' && (s[1] == 'x' || s[1] == 'X');");
        sb.AppendLine($"{ind}private static bool IsBinaryPrefix(ReadOnlySpan<char> s) => s.Length >= 2 && s[0] == '0' && (s[1] == 'b' || s[1] == 'B');");
        sb.AppendLine();
        sb.AppendLine($"{ind}private static string RemoveUnderscores(ReadOnlySpan<char> s)");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}    bool hasUnderscore = false;");
        sb.AppendLine($"{ind}    foreach (var c in s) {{ if (c == '_') {{ hasUnderscore = true; break; }} }}");
        sb.AppendLine($"{ind}    if (!hasUnderscore) return s.ToString();");
        sb.AppendLine($"{ind}    var sb = new System.Text.StringBuilder(s.Length);");
        sb.AppendLine($"{ind}    foreach (var c in s) {{ if (c != '_') sb.Append(c); }}");
        sb.AppendLine($"{ind}    return sb.ToString();");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();

        sb.AppendLine($"{ind}/// <summary>Parses a string into a {t}. Supports decimal, hex (0x prefix), and binary (0b prefix) formats with optional underscores.</summary>");
        sb.AppendLine($"{ind}public static {t} Parse(string s, IFormatProvider? provider)");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}    ArgumentNullException.ThrowIfNull(s);");
        sb.AppendLine($"{ind}    var span = s.AsSpan();");
        sb.AppendLine($"{ind}    if (IsBinaryPrefix(span))");
        sb.AppendLine($"{ind}    {{");
        sb.AppendLine($"{ind}        BigInteger val = 0;");
        sb.AppendLine($"{ind}        foreach (var c in RemoveUnderscores(span.Slice(2)))");
        sb.AppendLine($"{ind}            val = (val << 1) | (c == '1' ? 1 : 0);");
        sb.AppendLine($"{ind}        return FromBigInteger(val);");
        sb.AppendLine($"{ind}    }}");
        sb.AppendLine($"{ind}    if (IsHexPrefix(span))");
        sb.AppendLine($"{ind}        return FromBigInteger(BigInteger.Parse(\"0\" + RemoveUnderscores(span.Slice(2)), NumberStyles.HexNumber, provider));");
        sb.AppendLine($"{ind}    return FromBigInteger(BigInteger.Parse(RemoveUnderscores(span), NumberStyles.Integer, provider));");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();

        sb.AppendLine($"{ind}/// <summary>Tries to parse a string into a {t}.</summary>");
        sb.AppendLine($"{ind}public static bool TryParse(string? s, IFormatProvider? provider, out {t} result)");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}    if (s is null) {{ result = default; return false; }}");
        sb.AppendLine($"{ind}    try {{ result = Parse(s, provider); return true; }}");
        sb.AppendLine($"{ind}    catch {{ result = default; return false; }}");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();

        sb.AppendLine($"{ind}/// <summary>Parses a span of characters into a {t}.</summary>");
        sb.AppendLine($"{ind}public static {t} Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s.ToString(), provider);");
        sb.AppendLine();
        sb.AppendLine($"{ind}/// <summary>Tries to parse a span of characters into a {t}.</summary>");
        sb.AppendLine($"{ind}public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out {t} result) => TryParse(s.ToString(), provider, out result);");
        sb.AppendLine();
        sb.AppendLine($"{ind}/// <summary>Parses a string using invariant culture.</summary>");
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}public static {t} Parse(string s) => Parse(s, CultureInfo.InvariantCulture);");
        sb.AppendLine();
        sb.AppendLine($"{ind}/// <summary>Tries to parse a string using invariant culture.</summary>");
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}public static bool TryParse(string? s, out {t} result) => TryParse(s, CultureInfo.InvariantCulture, out result);");
        sb.AppendLine();
    }

    private static void GenerateFormattingMethods(StringBuilder sb, BitFieldsInfo info, WordLayout layout, string ind)
    {
        sb.AppendLine($"{ind}/// <summary>Formats the value using the specified format and format provider.</summary>");
        sb.AppendLine($"{ind}public string ToString(string? format, IFormatProvider? formatProvider) => ToBigInteger().ToString(format, formatProvider);");
        sb.AppendLine();

        sb.AppendLine($"{ind}/// <summary>Tries to format the value into the provided span of characters.</summary>");
        sb.AppendLine($"{ind}public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}    var str = ToString(format.ToString(), provider);");
        sb.AppendLine($"{ind}    if (str.Length <= destination.Length)");
        sb.AppendLine($"{ind}    {{");
        sb.AppendLine($"{ind}        str.AsSpan().CopyTo(destination);");
        sb.AppendLine($"{ind}        charsWritten = str.Length;");
        sb.AppendLine($"{ind}        return true;");
        sb.AppendLine($"{ind}    }}");
        sb.AppendLine($"{ind}    charsWritten = 0;");
        sb.AppendLine($"{ind}    return false;");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();
    }

    private static void GenerateComparisonMethods(StringBuilder sb, BitFieldsInfo info, WordLayout layout, string ind)
    {
        string t = info.TypeName;
        int wc = layout.WordCount;

        sb.AppendLine($"{ind}/// <summary>Compares this instance to a specified object.</summary>");
        sb.AppendLine($"{ind}public int CompareTo(object? obj)");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}    if (obj is null) return 1;");
        sb.AppendLine($"{ind}    if (obj is {t} other) return CompareTo(other);");
        sb.AppendLine($"{ind}    throw new ArgumentException(\"Object must be of type {t}\", nameof(obj));");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();

        sb.AppendLine($"{ind}/// <summary>Compares this instance to another {t}.</summary>");
        sb.AppendLine($"{ind}public int CompareTo({t} other)");
        sb.AppendLine($"{ind}{{");
        for (int i = wc - 1; i >= 0; i--)
        {
            string rdThis = layout.Read("", i);
            string rdOther = layout.Read("other.", i);
            sb.AppendLine($"{ind}    if ({rdThis} != {rdOther}) return {rdThis}.CompareTo({rdOther});");
        }
        sb.AppendLine($"{ind}    return 0;");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();

        sb.AppendLine($"{ind}/// <summary>Indicates whether this instance is equal to another {t}.</summary>");
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}public bool Equals({t} other) => this == other;");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates byte-span construction, serialization, and round-trip methods for multi-word BitFields.
    /// </summary>
    private static void GenerateByteSpanMethods(StringBuilder sb, BitFieldsInfo info, WordLayout layout, string ind)
    {
        string t = info.TypeName;
        int wc = layout.WordCount;

        // ReadOnlySpan<byte> constructor
        sb.AppendLine($"{ind}/// <summary>Creates a new {t} from a little-endian byte span.</summary>");
        sb.AppendLine($"{ind}/// <param name=\"bytes\">The source span. Must contain at least <see cref=\"SizeInBytes\"/> bytes.</param>");
        sb.AppendLine($"{ind}/// <exception cref=\"ArgumentException\">The span is too short.</exception>");
        sb.AppendLine($"{ind}public {t}(ReadOnlySpan<byte> bytes)");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}    if (bytes.Length < SizeInBytes)");
        sb.AppendLine($"{ind}        throw new ArgumentException($\"Span must contain at least {{SizeInBytes}} bytes.\", nameof(bytes));");
        for (int i = 0; i < wc; i++)
        {
            int offset = i * 8;
            if (layout.IsRemainder(i))
            {
                // Read the remainder using the smallest appropriate method
                switch (layout.RemainderType)
                {
                    case "byte":
                        sb.AppendLine($"{ind}    _w{i} = bytes[{offset}];");
                        break;
                    case "ushort":
                        sb.AppendLine($"{ind}    _w{i} = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice({offset}));");
                        break;
                    case "uint":
                        sb.AppendLine($"{ind}    _w{i} = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice({offset}));");
                        break;
                    default: // ulong remainder
                        sb.AppendLine($"{ind}    _w{i} = BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice({offset}));");
                        break;
                }
            }
            else
            {
                sb.AppendLine($"{ind}    _w{i} = BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice({offset}));");
            }
        }
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();

        // Static ReadFrom factory
        sb.AppendLine($"{ind}/// <summary>Creates a new {t} by reading <see cref=\"SizeInBytes\"/> bytes from a little-endian byte span.</summary>");
        sb.AppendLine($"{ind}/// <param name=\"bytes\">The source span. Must contain at least <see cref=\"SizeInBytes\"/> bytes.</param>");
        sb.AppendLine($"{ind}/// <returns>The deserialized {t}.</returns>");
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}public static {t} ReadFrom(ReadOnlySpan<byte> bytes) => new(bytes);");
        sb.AppendLine();

        // WriteTo
        sb.AppendLine($"{ind}/// <summary>Writes the value as little-endian bytes into the destination span.</summary>");
        sb.AppendLine($"{ind}/// <param name=\"destination\">The destination span. Must contain at least <see cref=\"SizeInBytes\"/> bytes.</param>");
        sb.AppendLine($"{ind}/// <exception cref=\"ArgumentException\">The span is too short.</exception>");
        sb.AppendLine($"{ind}public void WriteTo(Span<byte> destination)");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}    if (destination.Length < SizeInBytes)");
        sb.AppendLine($"{ind}        throw new ArgumentException($\"Span must contain at least {{SizeInBytes}} bytes.\", nameof(destination));");
        for (int i = 0; i < wc; i++)
        {
            int offset = i * 8;
            if (layout.IsRemainder(i))
            {
                switch (layout.RemainderType)
                {
                    case "byte":
                        sb.AppendLine($"{ind}    destination[{offset}] = _w{i};");
                        break;
                    case "ushort":
                        sb.AppendLine($"{ind}    BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice({offset}), _w{i});");
                        break;
                    case "uint":
                        sb.AppendLine($"{ind}    BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice({offset}), _w{i});");
                        break;
                    default:
                        sb.AppendLine($"{ind}    BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice({offset}), _w{i});");
                        break;
                }
            }
            else
            {
                sb.AppendLine($"{ind}    BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice({offset}), _w{i});");
            }
        }
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();

        // TryWriteTo
        sb.AppendLine($"{ind}/// <summary>Attempts to write the value as little-endian bytes into the destination span.</summary>");
        sb.AppendLine($"{ind}/// <param name=\"destination\">The destination span.</param>");
        sb.AppendLine($"{ind}/// <param name=\"bytesWritten\">The number of bytes written on success.</param>");
        sb.AppendLine($"{ind}/// <returns>true if the destination span was large enough; otherwise, false.</returns>");
        sb.AppendLine($"{ind}public bool TryWriteTo(Span<byte> destination, out int bytesWritten)");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}    if (destination.Length < SizeInBytes)");
        sb.AppendLine($"{ind}    {{");
        sb.AppendLine($"{ind}        bytesWritten = 0;");
        sb.AppendLine($"{ind}        return false;");
        sb.AppendLine($"{ind}    }}");
        sb.AppendLine($"{ind}    WriteTo(destination);");
        sb.AppendLine($"{ind}    bytesWritten = SizeInBytes;");
        sb.AppendLine($"{ind}    return true;");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();

        // ToByteArray
        sb.AppendLine($"{ind}/// <summary>Returns the value as a new little-endian byte array.</summary>");
        sb.AppendLine($"{ind}/// <returns>A byte array of length <see cref=\"SizeInBytes\"/>.</returns>");
        sb.AppendLine($"{ind}public byte[] ToByteArray()");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}    var bytes = new byte[SizeInBytes];");
        sb.AppendLine($"{ind}    WriteTo(bytes);");
        sb.AppendLine($"{ind}    return bytes;");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates a nested JsonConverter class for multi-word BitFields that round-trips via ToString/Parse.
    /// </summary>
    private static void GenerateJsonConverter(StringBuilder sb, BitFieldsInfo info, string ind)
    {
        string t = info.TypeName;

        sb.AppendLine($"{ind}/// <summary>JSON converter that serializes {t} as a hex string.</summary>");
        sb.AppendLine($"{ind}private sealed class {t}JsonConverter : JsonConverter<{t}>");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}    /// <summary>Reads a {t} from a JSON string.</summary>");
        sb.AppendLine($"{ind}    public override {t} Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)");
        sb.AppendLine($"{ind}    {{");
        sb.AppendLine($"{ind}        var s = reader.GetString();");
        sb.AppendLine($"{ind}        return s is null ? default : {t}.Parse(s);");
        sb.AppendLine($"{ind}    }}");
        sb.AppendLine();
        sb.AppendLine($"{ind}    /// <summary>Writes a {t} to JSON as a hex string.</summary>");
        sb.AppendLine($"{ind}    public override void Write(Utf8JsonWriter writer, {t} value, JsonSerializerOptions options)");
        sb.AppendLine($"{ind}    {{");
        sb.AppendLine($"{ind}        writer.WriteStringValue(value.ToString());");
        sb.AppendLine($"{ind}    }}");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();
    }
}
