using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stardust.Generators;

internal static partial class BitFieldsMultiWordGenerator
{
    private static void GenerateBitwiseOperators(StringBuilder sb, BitFieldsInfo info, WordLayout layout, string ind)
    {
        string t = info.TypeName;
        int wc = layout.WordCount;

        // Helper: emit either expression-bodied or block-bodied binary op
        void EmitBinaryOp(string op, string opName, System.Func<int, string> exprForWord)
        {
            var wordExprs = Enumerable.Range(0, wc).Select(i => layout.Store(i, exprForWord(i))).ToArray();
            var inline = InlineNew(layout, wordExprs);
            sb.AppendLine($"{ind}/// <summary>Bitwise {opName} operator.</summary>");
            sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            if (inline != null)
            {
                sb.AppendLine($"{ind}public static {t} operator {op}({t} a, {t} b) => {inline};");
            }
            else
            {
                sb.AppendLine($"{ind}public static {t} operator {op}({t} a, {t} b)");
                sb.AppendLine($"{ind}{{");
                EmitBlockConstruction(sb, t, layout, $"{ind}    ", wordExprs);
                sb.AppendLine($"{ind}}}");
            }
            sb.AppendLine();
        }

        // ~
        {
            var wordExprs = Enumerable.Range(0, wc).Select(i =>
                layout.Store(i, $"~{layout.Read("a.", i)}")).ToArray();
            var inline = InlineNew(layout, wordExprs);
            sb.AppendLine($"{ind}/// <summary>Bitwise complement operator.</summary>");
            sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            if (inline != null)
            {
                sb.AppendLine($"{ind}public static {t} operator ~({t} a) => {inline};");
            }
            else
            {
                sb.AppendLine($"{ind}public static {t} operator ~({t} a)");
                sb.AppendLine($"{ind}{{");
                EmitBlockConstruction(sb, t, layout, $"{ind}    ", wordExprs);
                sb.AppendLine($"{ind}}}");
            }
            sb.AppendLine();
        }

        // |, &, ^
        EmitBinaryOp("|", "OR", i => $"{layout.Read("a.", i)} | {layout.Read("b.", i)}");
        EmitBinaryOp("&", "AND", i => $"{layout.Read("a.", i)} & {layout.Read("b.", i)}");
        EmitBinaryOp("^", "XOR", i => $"{layout.Read("a.", i)} ^ {layout.Read("b.", i)}");

        // & with ulong (applied to lowest word only)
        {
            var wordExprs = new string[wc];
            wordExprs[0] = "a._w0 & b";
            for (int i = 1; i < wc; i++) wordExprs[i] = layout.Zero(i);
            var inline = InlineNew(layout, wordExprs);
            sb.AppendLine($"{ind}/// <summary>Bitwise AND operator with ulong (applied to lowest word).</summary>");
            sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            if (inline != null)
            {
                sb.AppendLine($"{ind}public static {t} operator &({t} a, ulong b) => {inline};");
            }
            else
            {
                sb.AppendLine($"{ind}public static {t} operator &({t} a, ulong b)");
                sb.AppendLine($"{ind}{{");
                EmitBlockConstruction(sb, t, layout, $"{ind}    ", wordExprs);
                sb.AppendLine($"{ind}}}");
            }
            sb.AppendLine();
        }
    }

    private static void GenerateArithmeticOperators(StringBuilder sb, BitFieldsInfo info, WordLayout layout, string ind)
    {
        string t = info.TypeName;
        int wc = layout.WordCount;

        // Decimal: generate arithmetic via native decimal operators
        if (info.NativeWideType == "decimal")
        {
            sb.AppendLine($"{ind}/// <summary>Unary plus operator.</summary>");
            sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{ind}public static {t} operator +({t} a) => a;");
            sb.AppendLine();

            sb.AppendLine($"{ind}/// <summary>Unary negation operator.</summary>");
            sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{ind}public static {t} operator -({t} a) => -(decimal)a;");
            sb.AppendLine();

            foreach (var (op, name) in new[] { ("+", "Addition"), ("-", "Subtraction"), ("*", "Multiplication"), ("/", "Division"), ("%", "Modulus") })
            {
                sb.AppendLine($"{ind}/// <summary>{name} operator (decimal arithmetic).</summary>");
                sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                sb.AppendLine($"{ind}public static {t} operator {op}({t} a, {t} b) => (decimal)a {op} (decimal)b;");
                sb.AppendLine();

                sb.AppendLine($"{ind}/// <summary>{name} operator with decimal.</summary>");
                sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                sb.AppendLine($"{ind}public static {t} operator {op}({t} a, decimal b) => (decimal)a {op} b;");
                sb.AppendLine();

                sb.AppendLine($"{ind}/// <summary>{name} operator with decimal.</summary>");
                sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                sb.AppendLine($"{ind}public static {t} operator {op}(decimal a, {t} b) => a {op} (decimal)b;");
                sb.AppendLine();
            }
            return;
        }

        // Unary +
        sb.AppendLine($"{ind}/// <summary>Unary plus operator.</summary>");
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}public static {t} operator +({t} a) => a;");
        sb.AppendLine();

        // Unary - (two's complement via ~a + 1)
        sb.AppendLine($"{ind}/// <summary>Unary negation operator. Returns two's complement negation.</summary>");
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}public static {t} operator -({t} a) => ~a + new {t}(1UL);");
        sb.AppendLine();

        // Binary + with carry chain
        sb.AppendLine($"{ind}/// <summary>Addition operator with carry propagation.</summary>");
        sb.AppendLine($"{ind}public static {t} operator +({t} a, {t} b)");
        sb.AppendLine($"{ind}{{");
        for (int i = 0; i < wc; i++)
        {
            string rdA = layout.Read("a.", i);
            string rdB = layout.Read("b.", i);
            if (i == 0)
            {
                sb.AppendLine($"{ind}    ulong w0 = {rdA} + {rdB};");
                sb.AppendLine($"{ind}    ulong c0 = (w0 < {rdA}) ? 1UL : 0UL;");
            }
            else
            {
                sb.AppendLine($"{ind}    ulong w{i} = {rdA} + {rdB} + c{i - 1};");
                if (i < wc - 1)
                    sb.AppendLine($"{ind}    ulong c{i} = (w{i} < {rdA} || (c{i - 1} != 0 && w{i} == {rdA})) ? 1UL : 0UL;");
            }
        }
        var addExprs = Enumerable.Range(0, wc).Select(i =>
            layout.IsRemainder(i) ? $"({layout.RemainderType})w{i}" : $"w{i}").ToArray();
        EmitReturnConstruction(sb, t, layout, $"{ind}    ", addExprs);
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();

        // + with ulong
        sb.AppendLine($"{ind}/// <summary>Addition operator with ulong.</summary>");
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}public static {t} operator +({t} a, ulong b) => a + new {t}(b);");
        sb.AppendLine();

        // Binary - with borrow chain
        sb.AppendLine($"{ind}/// <summary>Subtraction operator with borrow propagation.</summary>");
        sb.AppendLine($"{ind}public static {t} operator -({t} a, {t} b)");
        sb.AppendLine($"{ind}{{");
        for (int i = 0; i < wc; i++)
        {
            string rdA = layout.Read("a.", i);
            string rdB = layout.Read("b.", i);
            if (i == 0)
            {
                sb.AppendLine($"{ind}    ulong w0 = {rdA} - {rdB};");
                sb.AppendLine($"{ind}    ulong borrow0 = ({rdA} < {rdB}) ? 1UL : 0UL;");
            }
            else
            {
                sb.AppendLine($"{ind}    ulong diff{i} = {rdA} - {rdB};");
                sb.AppendLine($"{ind}    ulong w{i} = diff{i} - borrow{i - 1};");
                if (i < wc - 1)
                    sb.AppendLine($"{ind}    ulong borrow{i} = ({rdA} < {rdB} || (borrow{i - 1} != 0 && diff{i} == 0)) ? 1UL : 0UL;");
            }
        }
        var subExprs = Enumerable.Range(0, wc).Select(i =>
            layout.IsRemainder(i) ? $"({layout.RemainderType})w{i}" : $"w{i}").ToArray();
        EmitReturnConstruction(sb, t, layout, $"{ind}    ", subExprs);
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();

        // - with ulong
        sb.AppendLine($"{ind}/// <summary>Subtraction operator with ulong.</summary>");
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}public static {t} operator -({t} a, ulong b) => a - new {t}(b);");
        sb.AppendLine();

        // *, /, % via BigInteger
        sb.AppendLine($"{ind}/// <summary>Multiplication operator (via BigInteger).</summary>");
        sb.AppendLine($"{ind}public static {t} operator *({t} a, {t} b) => FromBigInteger(a.ToBigInteger() * b.ToBigInteger());");
        sb.AppendLine();
        sb.AppendLine($"{ind}/// <summary>Multiplication operator with ulong.</summary>");
        sb.AppendLine($"{ind}public static {t} operator *({t} a, ulong b) => FromBigInteger(a.ToBigInteger() * b);");
        sb.AppendLine();
        sb.AppendLine($"{ind}/// <summary>Multiplication operator with ulong.</summary>");
        sb.AppendLine($"{ind}public static {t} operator *(ulong a, {t} b) => FromBigInteger(a * b.ToBigInteger());");
        sb.AppendLine();
        sb.AppendLine($"{ind}/// <summary>Division operator (via BigInteger).</summary>");
        sb.AppendLine($"{ind}public static {t} operator /({t} a, {t} b) => FromBigInteger(a.ToBigInteger() / b.ToBigInteger());");
        sb.AppendLine();
        sb.AppendLine($"{ind}/// <summary>Division operator with ulong.</summary>");
        sb.AppendLine($"{ind}public static {t} operator /({t} a, ulong b) => FromBigInteger(a.ToBigInteger() / b);");
        sb.AppendLine();
        sb.AppendLine($"{ind}/// <summary>Modulus operator (via BigInteger).</summary>");
        sb.AppendLine($"{ind}public static {t} operator %({t} a, {t} b) => FromBigInteger(a.ToBigInteger() % b.ToBigInteger());");
        sb.AppendLine();
        sb.AppendLine($"{ind}/// <summary>Modulus operator with ulong.</summary>");
        sb.AppendLine($"{ind}public static {t} operator %({t} a, ulong b) => FromBigInteger(a.ToBigInteger() % b);");
        sb.AppendLine();
    }

    private static void GenerateShiftOperators(StringBuilder sb, BitFieldsInfo info, WordLayout layout, string ind)
    {
        string t = info.TypeName;
        int wc = layout.WordCount;

        // Left shift
        sb.AppendLine($"{ind}/// <summary>Left shift operator.</summary>");
        sb.AppendLine($"{ind}public static {t} operator <<({t} a, int amount)");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}    if (amount <= 0) return a;");
        sb.AppendLine($"{ind}    if (amount >= TotalBits) return default;");
        sb.AppendLine($"{ind}    int wordShift = amount / 64;");
        sb.AppendLine($"{ind}    int bitShift = amount % 64;");
        sb.AppendLine($"{ind}    var result = default({t});");
        sb.AppendLine($"{ind}    for (int dst = WordCount - 1; dst >= 0; dst--)");
        sb.AppendLine($"{ind}    {{");
        sb.AppendLine($"{ind}        int src = dst - wordShift;");
        sb.AppendLine($"{ind}        if (src < 0) continue;");
        sb.AppendLine($"{ind}        ulong val = GetWord(a, src);");
        sb.AppendLine($"{ind}        if (bitShift == 0)");
        sb.AppendLine($"{ind}            SetWord(ref result, dst, val);");
        sb.AppendLine($"{ind}        else");
        sb.AppendLine($"{ind}        {{");
        sb.AppendLine($"{ind}            SetWord(ref result, dst, GetWord(result, dst) | (val << bitShift));");
        sb.AppendLine($"{ind}            if (src > 0)");
        sb.AppendLine($"{ind}                SetWord(ref result, dst, GetWord(result, dst) | (GetWord(a, src - 1) >> (64 - bitShift)));");
        sb.AppendLine($"{ind}        }}");
        sb.AppendLine($"{ind}    }}");
        sb.AppendLine($"{ind}    return result;");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();

        // Right shift (unsigned)
        sb.AppendLine($"{ind}/// <summary>Right shift operator (unsigned).</summary>");
        sb.AppendLine($"{ind}public static {t} operator >>({t} a, int amount)");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}    if (amount <= 0) return a;");
        sb.AppendLine($"{ind}    if (amount >= TotalBits) return default;");
        sb.AppendLine($"{ind}    int wordShift = amount / 64;");
        sb.AppendLine($"{ind}    int bitShift = amount % 64;");
        sb.AppendLine($"{ind}    var result = default({t});");
        sb.AppendLine($"{ind}    for (int dst = 0; dst < WordCount; dst++)");
        sb.AppendLine($"{ind}    {{");
        sb.AppendLine($"{ind}        int src = dst + wordShift;");
        sb.AppendLine($"{ind}        if (src >= WordCount) break;");
        sb.AppendLine($"{ind}        ulong val = GetWord(a, src);");
        sb.AppendLine($"{ind}        if (bitShift == 0)");
        sb.AppendLine($"{ind}            SetWord(ref result, dst, val);");
        sb.AppendLine($"{ind}        else");
        sb.AppendLine($"{ind}        {{");
        sb.AppendLine($"{ind}            SetWord(ref result, dst, val >> bitShift);");
        sb.AppendLine($"{ind}            if (src + 1 < WordCount)");
        sb.AppendLine($"{ind}                SetWord(ref result, dst, GetWord(result, dst) | (GetWord(a, src + 1) << (64 - bitShift)));");
        sb.AppendLine($"{ind}        }}");
        sb.AppendLine($"{ind}    }}");
        sb.AppendLine($"{ind}    return result;");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();

        // >>>
        sb.AppendLine($"{ind}/// <summary>Unsigned right shift operator.</summary>");
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}public static {t} operator >>>({t} a, int amount) => a >> amount;");
        sb.AppendLine();

        // GetWord helper
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}private static ulong GetWord({t} v, int index)");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}    return index switch");
        sb.AppendLine($"{ind}    {{");
        for (int i = 0; i < wc; i++)
            sb.AppendLine($"{ind}        {i} => {layout.Read("v.", i)},");
        sb.AppendLine($"{ind}        _ => 0UL,");
        sb.AppendLine($"{ind}    }};");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();

        // SetWord helper
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}private static void SetWord(ref {t} v, int index, ulong value)");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}    switch (index)");
        sb.AppendLine($"{ind}    {{");
        for (int i = 0; i < wc; i++)
            sb.AppendLine($"{ind}        case {i}: v._w{i} = {layout.Store(i, "value")}; break;");
        sb.AppendLine($"{ind}    }}");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();
    }

    private static void GenerateComparisonOperators(StringBuilder sb, BitFieldsInfo info, WordLayout layout, string ind)
    {
        string t = info.TypeName;
        int wc = layout.WordCount;

        // Decimal: compare via native decimal comparison
        if (info.NativeWideType == "decimal")
        {
            sb.AppendLine($"{ind}/// <summary>Less than operator (decimal comparison).</summary>");
            sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{ind}public static bool operator <({t} a, {t} b) => (decimal)a < (decimal)b;");
            sb.AppendLine();
            sb.AppendLine($"{ind}/// <summary>Greater than operator (decimal comparison).</summary>");
            sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{ind}public static bool operator >({t} a, {t} b) => (decimal)a > (decimal)b;");
            sb.AppendLine();
            sb.AppendLine($"{ind}/// <summary>Less than or equal operator (decimal comparison).</summary>");
            sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{ind}public static bool operator <=({t} a, {t} b) => (decimal)a <= (decimal)b;");
            sb.AppendLine();
            sb.AppendLine($"{ind}/// <summary>Greater than or equal operator (decimal comparison).</summary>");
            sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{ind}public static bool operator >=({t} a, {t} b) => (decimal)a >= (decimal)b;");
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"{ind}/// <summary>Less than operator.</summary>");
        sb.AppendLine($"{ind}public static bool operator <({t} a, {t} b)");
        sb.AppendLine($"{ind}{{");
        for (int i = wc - 1; i >= 0; i--)
        {
            string rdA = layout.Read("a.", i);
            string rdB = layout.Read("b.", i);
            sb.AppendLine($"{ind}    if ({rdA} != {rdB}) return {rdA} < {rdB};");
        }
        sb.AppendLine($"{ind}    return false;");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();

        sb.AppendLine($"{ind}/// <summary>Greater than operator.</summary>");
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}public static bool operator >({t} a, {t} b) => b < a;");
        sb.AppendLine();
        sb.AppendLine($"{ind}/// <summary>Less than or equal operator.</summary>");
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}public static bool operator <=({t} a, {t} b) => !(b < a);");
        sb.AppendLine();
        sb.AppendLine($"{ind}/// <summary>Greater than or equal operator.</summary>");
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}public static bool operator >=({t} a, {t} b) => !(a < b);");
        sb.AppendLine();
    }

    private static void GenerateEqualityOperators(StringBuilder sb, BitFieldsInfo info, WordLayout layout, string ind)
    {
        string t = info.TypeName;
        int wc = layout.WordCount;

        var eqExpr = string.Join(" && ", Enumerable.Range(0, wc).Select(i => $"a._w{i} == b._w{i}"));
        sb.AppendLine($"{ind}/// <summary>Equality operator.</summary>");
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}public static bool operator ==({t} a, {t} b) => {eqExpr};");
        sb.AppendLine();
        sb.AppendLine($"{ind}/// <summary>Inequality operator.</summary>");
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}public static bool operator !=({t} a, {t} b) => !(a == b);");
        sb.AppendLine();
        sb.AppendLine($"{ind}/// <summary>Determines whether the specified object is equal to the current object.</summary>");
        sb.AppendLine($"{ind}public override bool Equals(object? obj) => obj is {t} other && this == other;");
        sb.AppendLine();

        sb.AppendLine($"{ind}/// <summary>Returns the hash code for this instance.</summary>");
        sb.AppendLine($"{ind}public override int GetHashCode()");
        sb.AppendLine($"{ind}{{");
        if (wc <= 8)
        {
            sb.Append($"{ind}    return HashCode.Combine(");
            sb.Append(string.Join(", ", Enumerable.Range(0, wc).Select(i => $"_w{i}")));
            sb.AppendLine(");");
        }
        else
        {
            sb.AppendLine($"{ind}    var hash = new HashCode();");
            for (int i = 0; i < wc; i++)
                sb.AppendLine($"{ind}    hash.Add(_w{i});");
            sb.AppendLine($"{ind}    return hash.ToHashCode();");
        }
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();

        if (info.NativeWideType == "decimal")
        {
            sb.AppendLine($"{ind}/// <summary>Returns the decimal string representation of the value.</summary>");
            sb.AppendLine($"{ind}public override string ToString() => ((decimal)this).ToString();");
        }
        else
        {
            sb.AppendLine($"{ind}/// <summary>Returns a hex string representation of the value.</summary>");
            sb.AppendLine($"{ind}public override string ToString() => \"0x\" + ToBigInteger().ToString(\"X\");");
        }
        sb.AppendLine();
    }
}
