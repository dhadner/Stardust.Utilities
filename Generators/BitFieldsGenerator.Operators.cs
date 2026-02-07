using System.Text;

namespace Stardust.Generators;

public partial class BitFieldsGenerator
{
    /// <summary>
    /// Generates bitwise operators: |, &amp;, ^, ~
    /// For small types (byte, sbyte, short, ushort), generates widening operators with int.
    /// For larger types, generates mixed operators with the storage type.
    /// </summary>
    private static void GenerateBitwiseOperators(StringBuilder sb, BitFieldsInfo info, string indent)
    {
        string t = info.TypeName;
        string s = info.StorageType;

        // Unary complement ~
        sb.AppendLine($"{indent}/// <summary>Bitwise complement operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator ~({t} a) => new(({s})~a.Value);");
        sb.AppendLine();

        // Binary OR | (same type)
        sb.AppendLine($"{indent}/// <summary>Bitwise OR operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator |({t} a, {t} b) => new(({s})(a.Value | b.Value));");
        sb.AppendLine();

        // Binary AND & (same type)
        sb.AppendLine($"{indent}/// <summary>Bitwise AND operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator &({t} a, {t} b) => new(({s})(a.Value & b.Value));");
        sb.AppendLine();

        // Binary XOR ^ (same type)
        sb.AppendLine($"{indent}/// <summary>Bitwise XOR operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator ^({t} a, {t} b) => new(({s})(a.Value ^ b.Value));");
        sb.AppendLine();

        // For small types (byte, sbyte, short, ushort), DON'T generate mixed int operators.
        if (s == "byte" || s == "sbyte" || s == "short" || s == "ushort")
        {
            // No mixed operators - shift returns int, use native int operators for `& 1` etc.
        }
        else if (s == "int")
        {
            GenerateMixedBitwiseOps(sb, t, "int", indent);
        }
        else if (s == "uint")
        {
            GenerateMixedBitwiseOps(sb, t, "uint", indent);

            // Widening int operators (return long)
            GenerateWideningBitwiseOps(sb, t, "int", "long", "(long)", indent);
        }
        else if (s == "long")
        {
            GenerateMixedBitwiseOps(sb, t, "long", indent);

            // Widening int operators (return long)
            sb.AppendLine($"{indent}/// <summary>Bitwise AND operator with int (widening). Returns long.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static long operator &({t} a, int b) => a.Value & b;");
            sb.AppendLine();
            sb.AppendLine($"{indent}/// <summary>Bitwise AND operator with int (widening). Returns long.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static long operator &(int a, {t} b) => a & b.Value;");
            sb.AppendLine();

            sb.AppendLine($"{indent}/// <summary>Bitwise OR operator with int (widening). Returns long.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static long operator |({t} a, int b) => a.Value | b;");
            sb.AppendLine();
            sb.AppendLine($"{indent}/// <summary>Bitwise OR operator with int (widening). Returns long.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static long operator |(int a, {t} b) => a | b.Value;");
            sb.AppendLine();

            sb.AppendLine($"{indent}/// <summary>Bitwise XOR operator with int (widening). Returns long.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static long operator ^({t} a, int b) => a.Value ^ b;");
            sb.AppendLine();
            sb.AppendLine($"{indent}/// <summary>Bitwise XOR operator with int (widening). Returns long.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static long operator ^(int a, {t} b) => a ^ b.Value;");
            sb.AppendLine();
        }
        else if (s == "ulong")
        {
            GenerateMixedBitwiseOps(sb, t, "ulong", indent);

            // Widening int operators (return ulong via cast)
            GenerateWideningBitwiseOps(sb, t, "int", "ulong", "(ulong)", indent);
        }
    }

    /// <summary>
    /// Generates mixed bitwise operators (AND, OR, XOR) for a given storage type that return the BitFields type.
    /// </summary>
    private static void GenerateMixedBitwiseOps(StringBuilder sb, string t, string mixType, string indent)
    {
        foreach (var (op, name) in new[] { ("&", "AND"), ("|", "OR"), ("^", "XOR") })
        {
            sb.AppendLine($"{indent}/// <summary>Bitwise {name} operator with {mixType}.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static {t} operator {op}({t} a, {mixType} b) => new(a.Value {op} b);");
            sb.AppendLine();
            sb.AppendLine($"{indent}/// <summary>Bitwise {name} operator with {mixType}.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static {t} operator {op}({mixType} a, {t} b) => new(a {op} b.Value);");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates widening bitwise operators that return a wider type (e.g., int with uint ? long).
    /// </summary>
    private static void GenerateWideningBitwiseOps(StringBuilder sb, string t, string narrowType, string wideReturn, string cast, string indent)
    {
        foreach (var (op, name) in new[] { ("&", "AND"), ("|", "OR"), ("^", "XOR") })
        {
            sb.AppendLine($"{indent}/// <summary>Bitwise {name} operator with {narrowType} (widening). Returns {wideReturn} for correct semantics.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static {wideReturn} operator {op}({t} a, {narrowType} b) => a.Value {op} {cast}b;");
            sb.AppendLine();
            sb.AppendLine($"{indent}/// <summary>Bitwise {name} operator with {narrowType} (widening). Returns {wideReturn} for correct semantics.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static {wideReturn} operator {op}({narrowType} a, {t} b) => {cast}a {op} b.Value;");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates arithmetic operators: +, -, *, /, %
    /// For NativeFloat mode, uses floating-point arithmetic via BitConverter.
    /// </summary>
    private static void GenerateArithmeticOperators(StringBuilder sb, BitFieldsInfo info, string indent)
    {
        string t = info.TypeName;
        string s = info.StorageType;

        if (info.Mode == StorageMode.NativeFloat)
        {
            string fp = info.FloatingPointType!;
            string toBits = fp == "float" ? "BitConverter.SingleToUInt32Bits" : "BitConverter.DoubleToUInt64Bits";
            string fromBits = fp == "float" ? "BitConverter.UInt32BitsToSingle" : "BitConverter.UInt64BitsToDouble";

            sb.AppendLine($"{indent}/// <summary>Unary plus operator.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static {t} operator +({t} a) => a;");
            sb.AppendLine();

            sb.AppendLine($"{indent}/// <summary>Unary negation operator.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static {t} operator -({t} a) => new({toBits}(-{fromBits}(a.Value)));");
            sb.AppendLine();

            // Binary arithmetic operators via float/double
            foreach (var (op, name) in new[] { ("+", "Addition"), ("-", "Subtraction"), ("*", "Multiplication"), ("/", "Division"), ("%", "Modulus") })
            {
                sb.AppendLine($"{indent}/// <summary>{name} operator (floating-point arithmetic).</summary>");
                sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                sb.AppendLine($"{indent}public static {t} operator {op}({t} a, {t} b) => new({toBits}({fromBits}(a.Value) {op} {fromBits}(b.Value)));");
                sb.AppendLine();

                sb.AppendLine($"{indent}/// <summary>{name} operator with {fp}.</summary>");
                sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                sb.AppendLine($"{indent}public static {t} operator {op}({t} a, {fp} b) => new({toBits}({fromBits}(a.Value) {op} b));");
                sb.AppendLine();

                sb.AppendLine($"{indent}/// <summary>{name} operator with {fp}.</summary>");
                sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                sb.AppendLine($"{indent}public static {t} operator {op}({fp} a, {t} b) => new({toBits}(a {op} {fromBits}(b.Value)));");
                sb.AppendLine();
            }
            return;
        }

        // Unary +
        sb.AppendLine($"{indent}/// <summary>Unary plus operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator +({t} a) => a;");
        sb.AppendLine();

        // Unary -
        sb.AppendLine($"{indent}/// <summary>Unary negation operator. Returns two's complement negation.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        if (info.StorageTypeIsSigned)
        {
            sb.AppendLine($"{indent}public static {t} operator -({t} a) => new(unchecked(({s})(-a.Value)));");
        }
        else
        {
            sb.AppendLine($"{indent}public static {t} operator -({t} a) => new(unchecked(({s})(0 - a.Value)));");
        }
        sb.AppendLine();

        // Binary + (use unchecked to match native wraparound behavior)
        sb.AppendLine($"{indent}/// <summary>Addition operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator +({t} a, {t} b) => new(unchecked(({s})(a.Value + b.Value)));");
        sb.AppendLine();
        sb.AppendLine($"{indent}/// <summary>Addition operator with storage type.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator +({t} a, {s} b) => new(unchecked(({s})(a.Value + b)));");
        sb.AppendLine();
        sb.AppendLine($"{indent}/// <summary>Addition operator with storage type.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator +({s} a, {t} b) => new(unchecked(({s})(a + b.Value)));");
        sb.AppendLine();

        // Binary -
        sb.AppendLine($"{indent}/// <summary>Subtraction operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator -({t} a, {t} b) => new(unchecked(({s})(a.Value - b.Value)));");
        sb.AppendLine();
        sb.AppendLine($"{indent}/// <summary>Subtraction operator with storage type.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator -({t} a, {s} b) => new(unchecked(({s})(a.Value - b)));");
        sb.AppendLine();
        sb.AppendLine($"{indent}/// <summary>Subtraction operator with storage type.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator -({s} a, {t} b) => new(unchecked(({s})(a - b.Value)));");
        sb.AppendLine();

        // Binary *
        sb.AppendLine($"{indent}/// <summary>Multiplication operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator *({t} a, {t} b) => new(unchecked(({s})(a.Value * b.Value)));");
        sb.AppendLine();
        sb.AppendLine($"{indent}/// <summary>Multiplication operator with storage type.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator *({t} a, {s} b) => new(unchecked(({s})(a.Value * b)));");
        sb.AppendLine();
        sb.AppendLine($"{indent}/// <summary>Multiplication operator with storage type.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator *({s} a, {t} b) => new(unchecked(({s})(a * b.Value)));");
        sb.AppendLine();

        // Binary /
        sb.AppendLine($"{indent}/// <summary>Division operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator /({t} a, {t} b) => new(({s})(a.Value / b.Value));");
        sb.AppendLine();
        sb.AppendLine($"{indent}/// <summary>Division operator with storage type.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator /({t} a, {s} b) => new(({s})(a.Value / b));");
        sb.AppendLine();
        sb.AppendLine($"{indent}/// <summary>Division operator with storage type.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator /({s} a, {t} b) => new(({s})(a / b.Value));");
        sb.AppendLine();

        // Binary %
        sb.AppendLine($"{indent}/// <summary>Modulus operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator %({t} a, {t} b) => new(({s})(a.Value % b.Value));");
        sb.AppendLine();
        sb.AppendLine($"{indent}/// <summary>Modulus operator with storage type.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator %({t} a, {s} b) => new(({s})(a.Value % b));");
        sb.AppendLine();
        sb.AppendLine($"{indent}/// <summary>Modulus operator with storage type.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator %({s} a, {t} b) => new(({s})(a % b.Value));");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates shift operators: &lt;&lt;, &gt;&gt;, &gt;&gt;&gt;
    /// For small types (byte, sbyte, short, ushort), returns int to enable intuitive use like `(bits >> 1) &amp; 1`.
    /// </summary>
    private static void GenerateShiftOperators(StringBuilder sb, BitFieldsInfo info, string indent)
    {
        string t = info.TypeName;
        string s = info.StorageType;

        if (s == "byte" || s == "sbyte" || s == "short" || s == "ushort")
        {
            // Small types: shift returns int
            sb.AppendLine($"{indent}/// <summary>Left shift operator. Returns int for intuitive bitwise operations with literals.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static int operator <<({t} a, int b) => a.Value << b;");
            sb.AppendLine();

            sb.AppendLine($"{indent}/// <summary>Right shift operator. Returns int for intuitive bitwise operations with literals.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static int operator >>({t} a, int b) => a.Value >> b;");
            sb.AppendLine();

            sb.AppendLine($"{indent}/// <summary>Unsigned right shift operator. Returns int for intuitive bitwise operations with literals.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static int operator >>>({t} a, int b) => a.Value >>> b;");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine($"{indent}/// <summary>Left shift operator.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static {t} operator <<({t} a, int b) => new(unchecked(({s})(a.Value << b)));");
            sb.AppendLine();

            sb.AppendLine($"{indent}/// <summary>Right shift operator.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static {t} operator >>({t} a, int b) => new(unchecked(({s})(a.Value >> b)));");
            sb.AppendLine();

            sb.AppendLine($"{indent}/// <summary>Unsigned right shift operator.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static {t} operator >>>({t} a, int b) => new(unchecked(({s})(a.Value >>> b)));");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates comparison operators: &lt;, &gt;, &lt;=, &gt;=
    /// For NativeFloat mode, uses floating-point comparison via BitConverter.
    /// </summary>
    private static void GenerateComparisonOperators(StringBuilder sb, BitFieldsInfo info, string indent)
    {
        string t = info.TypeName;

        if (info.Mode == StorageMode.NativeFloat)
        {
            string fp = info.FloatingPointType!;
            string fromBits = fp == "float" ? "BitConverter.UInt32BitsToSingle" : "BitConverter.UInt64BitsToDouble";

            foreach (var (op, name) in new[] { ("<", "Less than"), (">", "Greater than"), ("<=", "Less than or equal"), (">=", "Greater than or equal") })
            {
                sb.AppendLine($"{indent}/// <summary>{name} operator (floating-point comparison).</summary>");
                sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                sb.AppendLine($"{indent}public static bool operator {op}({t} a, {t} b) => {fromBits}(a.Value) {op} {fromBits}(b.Value);");
                sb.AppendLine();
            }
            return;
        }

        foreach (var (op, name) in new[] { ("<", "Less than"), (">", "Greater than"), ("<=", "Less than or equal"), (">=", "Greater than or equal") })
        {
            sb.AppendLine($"{indent}/// <summary>{name} operator.</summary>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public static bool operator {op}({t} a, {t} b) => a.Value {op} b.Value;");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates equality operators: ==, !=
    /// Also generates Equals, GetHashCode, and ToString overrides.
    /// </summary>
    private static void GenerateEqualityOperators(StringBuilder sb, BitFieldsInfo info, string indent)
    {
        string t = info.TypeName;

        sb.AppendLine($"{indent}/// <summary>Equality operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static bool operator ==({t} a, {t} b) => a.Value == b.Value;");
        sb.AppendLine();

        sb.AppendLine($"{indent}/// <summary>Inequality operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static bool operator !=({t} a, {t} b) => a.Value != b.Value;");
        sb.AppendLine();

        sb.AppendLine($"{indent}/// <summary>Determines whether the specified object is equal to the current object.</summary>");
        sb.AppendLine($"{indent}public override bool Equals(object? obj) => obj is {t} other && Value == other.Value;");
        sb.AppendLine();

        sb.AppendLine($"{indent}/// <summary>Returns the hash code for this instance.</summary>");
        sb.AppendLine($"{indent}public override int GetHashCode() => Value.GetHashCode();");
        sb.AppendLine();

        sb.AppendLine($"{indent}/// <summary>Returns a string representation of the value.</summary>");
        if (info.Mode == StorageMode.NativeFloat)
        {
            string fromBits = info.FloatingPointType == "float" ? "BitConverter.UInt32BitsToSingle" : "BitConverter.UInt64BitsToDouble";
            sb.AppendLine($"{indent}public override string ToString() => {fromBits}(Value).ToString();");
        }
        else
        {
            sb.AppendLine($"{indent}public override string ToString() => $\"0x{{Value:X}}\";");
        }
        sb.AppendLine();
    }
}
