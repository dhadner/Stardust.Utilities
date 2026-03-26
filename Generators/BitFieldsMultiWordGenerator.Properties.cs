using System.Linq;
using System.Text;
using static Stardust.Generators.GeneratorUtils;

namespace Stardust.Generators;

internal static partial class BitFieldsMultiWordGenerator
{
    private static void GenerateBitFieldProperty(StringBuilder sb, BitFieldsInfo info, WordLayout layout, BitFieldInfo field, string ind)
    {
        // Span-backed embedded multi-word struct: use WriteTo/ReadFrom
        if (field.IsSpanBacked)
        {
            GenerateSpanBackedProperty(sb, info, layout, field, ind);
            return;
        }

        int shift = field.Shift;
        int width = field.Width;
        int startWord = shift / 64;
        int localShift = shift % 64;
        int end = shift + width - 1;
        int endWord = end / 64;
        bool crossWord = startWord != endWord;

        // Floating-point property types need BitConverter wrapping
        bool isFloatProp = IsFloatingPointPropertyType(field.PropertyType);
        bool isWideFloat = isFloatProp && width > 64; // decimal (128-bit) needs UInt128 arithmetic

        string getterPrefix = isFloatProp ? $"{FloatPropertyFromBits(field.PropertyType)}(({FloatPropertyUnsignedType(field.PropertyType)})" : $"({field.PropertyType})";
        string getterSuffix = isFloatProp ? ")" : "";
        // For the setter, convert float value to raw bits before inserting.
        // For wide float types (decimal→UInt128), the local variable __bits is used instead.
        string setterOpen = isWideFloat ? "__bits"
            : isFloatProp ? $"(ulong){FloatPropertyToBits(field.PropertyType)}(value)"
            : "(ulong)value";

        sb.AppendLine($"{ind}public partial {field.PropertyType} {field.Name}");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");

        if (!crossWord)
        {
            ulong mask = (width == 64) ? ulong.MaxValue : (1UL << width) - 1;
            string rd = layout.Read("", startWord);
            if (localShift == 0 && width == 64)
                sb.AppendLine($"{ind}    get => {getterPrefix}{rd}{getterSuffix};");
            else if (localShift == 0)
                sb.AppendLine($"{ind}    get => {getterPrefix}({rd} & 0x{mask:X}UL){getterSuffix};");
            else
                sb.AppendLine($"{ind}    get => {getterPrefix}(({rd} >> {localShift}) & 0x{mask:X}UL){getterSuffix};");

            sb.AppendLine($"{ind}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");

            ulong shiftedMask = mask << localShift;
            // Enforce MustBe constraints
            if (field.ValueOverride == MustBe.Zero)
            {
                sb.AppendLine($"{ind}    set => _w{startWord} = {layout.Store(startWord, $"({rd} & 0x{~shiftedMask:X16}UL)")};" );
            }
            else if (field.ValueOverride == MustBe.One)
            {
                sb.AppendLine($"{ind}    set => _w{startWord} = {layout.Store(startWord, $"({rd} | 0x{shiftedMask:X16}UL)")};" );
            }
            else if (localShift == 0 && width == 64)
                sb.AppendLine($"{ind}    set => _w{startWord} = {layout.Store(startWord, setterOpen)};");
            else if (localShift == 0)
                sb.AppendLine($"{ind}    set => _w{startWord} = {layout.Store(startWord, $"({rd} & 0x{~shiftedMask:X16}UL) | ({setterOpen} & 0x{shiftedMask:X16}UL)")};");
            else
                sb.AppendLine($"{ind}    set => _w{startWord} = {layout.Store(startWord, $"({rd} & 0x{~shiftedMask:X16}UL) | (({setterOpen} << {localShift}) & 0x{shiftedMask:X16}UL)")};");
        }
        else if (isWideFloat)
        {
            // Wide float property (decimal → 128 bits): uses UInt128 arithmetic to
            // combine / split words.  Only decimal triggers this path; Half/float/double
            // continue to use the fast ulong path below.
            string rdS = layout.Read("", startWord);
            string rdE = layout.Read("", endWord);
            string toBits = FloatPropertyToBits(field.PropertyType);

            if (localShift == 0)
            {
                // Word-aligned: low 64 bits in startWord, high 64 bits in endWord
                sb.AppendLine($"{ind}    get => {getterPrefix}(((UInt128){rdE} << 64) | (UInt128){rdS}){getterSuffix};");
            }
            else
            {
                // Non-aligned: field spans 3 words.
                // startWord has (64 - localShift) bits, middleWord has 64 bits,
                // endWord has the remaining bits.
                int midWord = startWord + 1;
                string rdM = layout.Read("", midWord);
                int bitsHigh = width - (64 - localShift) - 64; // bits in endWord
                ulong maskHigh = (bitsHigh >= 64) ? ulong.MaxValue : (1UL << bitsHigh) - 1;
                sb.AppendLine($"{ind}    get => {getterPrefix}(((UInt128)({rdE} & 0x{maskHigh:X}UL) << {128 - bitsHigh}) | ((UInt128){rdM} << {64 - localShift}) | (UInt128)({rdS} >> {localShift})){getterSuffix};");
            }

            sb.AppendLine($"{ind}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");

            if (field.ValueOverride == MustBe.Zero)
            {
                sb.AppendLine($"{ind}    set");
                sb.AppendLine($"{ind}    {{");
                if (localShift == 0)
                {
                    sb.AppendLine($"{ind}        _w{startWord} = {layout.Store(startWord, "0UL")};");
                    sb.AppendLine($"{ind}        _w{endWord} = {layout.Store(endWord, "0UL")};");
                }
                else
                {
                    int midWord = startWord + 1;
                    ulong clearS = (1UL << localShift) - 1;
                    int bitsHigh = width - (64 - localShift) - 64;
                    ulong clearE = (bitsHigh >= 64) ? 0UL : ~((1UL << bitsHigh) - 1);
                    sb.AppendLine($"{ind}        _w{startWord} = {layout.Store(startWord, $"({rdS} & 0x{clearS:X16}UL)")};");
                    sb.AppendLine($"{ind}        _w{midWord} = {layout.Store(midWord, "0UL")};");
                    sb.AppendLine($"{ind}        _w{endWord} = {layout.Store(endWord, $"({rdE} & 0x{clearE:X16}UL)")};");
                }
                sb.AppendLine($"{ind}    }}");
            }
            else if (field.ValueOverride == MustBe.One)
            {
                sb.AppendLine($"{ind}    set");
                sb.AppendLine($"{ind}    {{");
                if (localShift == 0)
                {
                    sb.AppendLine($"{ind}        _w{startWord} = {layout.Store(startWord, "0xFFFFFFFFFFFFFFFFUL")};");
                    sb.AppendLine($"{ind}        _w{endWord} = {layout.Store(endWord, "0xFFFFFFFFFFFFFFFFUL")};");
                }
                else
                {
                    int midWord = startWord + 1;
                    ulong setS = ~((1UL << localShift) - 1);
                    int bitsHigh = width - (64 - localShift) - 64;
                    ulong setE = (bitsHigh >= 64) ? ulong.MaxValue : (1UL << bitsHigh) - 1;
                    sb.AppendLine($"{ind}        _w{startWord} = {layout.Store(startWord, $"({rdS} | 0x{setS:X16}UL)")};");
                    sb.AppendLine($"{ind}        _w{midWord} = {layout.Store(midWord, "0xFFFFFFFFFFFFFFFFUL")};");
                    sb.AppendLine($"{ind}        _w{endWord} = {layout.Store(endWord, $"({rdE} | 0x{setE:X16}UL)")};");
                }
                sb.AppendLine($"{ind}    }}");
            }
            else
            {
                sb.AppendLine($"{ind}    set");
                sb.AppendLine($"{ind}    {{");
                sb.AppendLine($"{ind}        UInt128 __bits = {toBits}(value);");
                if (localShift == 0)
                {
                    sb.AppendLine($"{ind}        _w{startWord} = {layout.Store(startWord, "(ulong)__bits")};");
                    sb.AppendLine($"{ind}        _w{endWord} = {layout.Store(endWord, "(ulong)(__bits >> 64)")};");
                }
                else
                {
                    int midWord = startWord + 1;
                    int bitsHigh = width - (64 - localShift) - 64;
                    ulong keepLow = (1UL << localShift) - 1;
                    ulong clearHigh = (bitsHigh >= 64) ? 0UL : ~((1UL << bitsHigh) - 1);
                    sb.AppendLine($"{ind}        _w{startWord} = {layout.Store(startWord, $"({rdS} & 0x{keepLow:X16}UL) | ((ulong)__bits << {localShift})")};");
                    sb.AppendLine($"{ind}        _w{midWord} = {layout.Store(midWord, $"(ulong)(__bits >> {64 - localShift})")};");
                    sb.AppendLine($"{ind}        _w{endWord} = {layout.Store(endWord, $"({rdE} & 0x{clearHigh:X16}UL) | (ulong)(__bits >> {128 - bitsHigh})")};");
                }
                sb.AppendLine($"{ind}    }}");
            }
        }
        else
        {
            int bitsInStart = 64 - localShift;
            int bitsInEnd = width - bitsInStart;
            ulong maskEnd = (1UL << bitsInEnd) - 1;
            string rdS = layout.Read("", startWord);
            string rdE = layout.Read("", endWord);

            sb.AppendLine($"{ind}    get => {getterPrefix}(({rdS} >> {localShift}) | (({rdE} & 0x{maskEnd:X}UL) << {bitsInStart})){getterSuffix};");

            sb.AppendLine($"{ind}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            ulong maskStartShifted = ((1UL << bitsInStart) - 1) << localShift;
            if (field.ValueOverride == MustBe.Zero)
            {
                sb.AppendLine($"{ind}    set");
                sb.AppendLine($"{ind}    {{");
                sb.AppendLine($"{ind}        _w{startWord} = {layout.Store(startWord, $"({rdS} & 0x{~maskStartShifted:X16}UL)")};" );
                sb.AppendLine($"{ind}        _w{endWord} = {layout.Store(endWord, $"({rdE} & 0x{~maskEnd:X16}UL)")};" );
                sb.AppendLine($"{ind}    }}");
            }
            else if (field.ValueOverride == MustBe.One)
            {
                sb.AppendLine($"{ind}    set");
                sb.AppendLine($"{ind}    {{");
                sb.AppendLine($"{ind}        _w{startWord} = {layout.Store(startWord, $"({rdS} | 0x{maskStartShifted:X16}UL)")};" );
                sb.AppendLine($"{ind}        _w{endWord} = {layout.Store(endWord, $"({rdE} | 0x{maskEnd:X16}UL)")};" );
                sb.AppendLine($"{ind}    }}");
            }
            else
            {
                sb.AppendLine($"{ind}    set");
                sb.AppendLine($"{ind}    {{");
                ulong maskStart = (1UL << bitsInStart) - 1;
                sb.AppendLine($"{ind}        _w{startWord} = {layout.Store(startWord, $"({rdS} & 0x{(1UL << localShift) - 1:X16}UL) | (({setterOpen} & 0x{maskStart:X}UL) << {localShift})")};");
                sb.AppendLine($"{ind}        _w{endWord} = {layout.Store(endWord, $"({rdE} & 0x{~maskEnd:X16}UL) | (({setterOpen} >> {bitsInStart}) & 0x{maskEnd:X}UL)")};");
                sb.AppendLine($"{ind}    }}");
            }
        }

        sb.AppendLine($"{ind}}}");
        sb.AppendLine();
    }

    private static void GenerateBitFlagProperty(StringBuilder sb, BitFieldsInfo info, WordLayout layout, BitFlagInfo flag, string ind)
    {
        int wi = flag.Bit / 64;
        int localBit = flag.Bit % 64;
        ulong mask = 1UL << localBit;
        string rd = layout.Read("", wi);

        sb.AppendLine($"{ind}public partial bool {flag.Name}");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");

        if (flag.ValueOverride == MustBe.Zero)
        {
            sb.AppendLine($"{ind}    get => false;");
            sb.AppendLine($"{ind}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{ind}    set => _w{wi} = {layout.Store(wi, $"({rd} & 0x{~mask:X16}UL)")};");
        }
        else if (flag.ValueOverride == MustBe.One)
        {
            sb.AppendLine($"{ind}    get => true;");
            sb.AppendLine($"{ind}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{ind}    set => _w{wi} = {layout.Store(wi, $"({rd} | 0x{mask:X}UL)")};");
        }
        else
        {
            sb.AppendLine($"{ind}    get => ({rd} & 0x{mask:X}UL) != 0;");
            sb.AppendLine($"{ind}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{ind}    set => _w{wi} = {layout.Store(wi, $"value ? ({rd} | 0x{mask:X}UL) : ({rd} & 0x{~mask:X16}UL)")};");
        }

        sb.AppendLine($"{ind}}}");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates a property accessor for a span-backed embedded multi-word struct.
    /// Uses <c>WriteTo</c> / <c>ReadFrom</c> on a temp byte span.
    /// When the field starts at a byte-aligned position the span is a simple slice;
    /// otherwise byte-level bit-shifting is emitted to support arbitrary bit offsets.
    /// </summary>
    private static void GenerateSpanBackedProperty(StringBuilder sb, BitFieldsInfo info, WordLayout layout, BitFieldInfo field, string ind)
    {
        int startByte = field.Shift / 8;
        int bitOffset = field.Shift % 8;
        int sizeBytes = field.StructSizeBytes;
        bool aligned = bitOffset == 0;

        sb.AppendLine($"{ind}public partial {field.PropertyType} {field.Name}");
        sb.AppendLine($"{ind}{{");

        // ── Getter ──────────────────────────────────────────────────────
        sb.AppendLine($"{ind}    get");
        sb.AppendLine($"{ind}    {{");
        sb.AppendLine($"{ind}        Span<byte> __buf = stackalloc byte[SIZE_IN_BYTES];");
        sb.AppendLine($"{ind}        WriteTo(__buf);");
        if (aligned)
        {
            sb.AppendLine($"{ind}        return {field.PropertyType}.ReadFrom(__buf.Slice({startByte}, {sizeBytes}));");
        }
        else
        {
            sb.AppendLine($"{ind}        Span<byte> __ebuf = stackalloc byte[{sizeBytes}];");
            sb.AppendLine($"{ind}        for (int __i = 0; __i < {sizeBytes}; __i++)");
            sb.AppendLine($"{ind}            __ebuf[__i] = (byte)((__buf[{startByte} + __i] >> {bitOffset}) | (({startByte} + __i + 1 < SIZE_IN_BYTES) ? (__buf[{startByte} + __i + 1] << {8 - bitOffset}) : 0));");
            sb.AppendLine($"{ind}        return {field.PropertyType}.ReadFrom(__ebuf);");
        }
        sb.AppendLine($"{ind}    }}");

        // ── Setter ──────────────────────────────────────────────────────
        sb.AppendLine($"{ind}    set");
        sb.AppendLine($"{ind}    {{");
        sb.AppendLine($"{ind}        Span<byte> __buf = stackalloc byte[SIZE_IN_BYTES];");
        sb.AppendLine($"{ind}        WriteTo(__buf);");
        if (aligned)
        {
            sb.AppendLine($"{ind}        value.WriteTo(__buf.Slice({startByte}, {sizeBytes}));");
        }
        else
        {
            int lowMask = (1 << bitOffset) - 1;
            int hiMask = ~lowMask & 0xFF;
            sb.AppendLine($"{ind}        Span<byte> __ebuf = stackalloc byte[{sizeBytes}];");
            sb.AppendLine($"{ind}        value.WriteTo(__ebuf);");
            sb.AppendLine($"{ind}        for (int __i = 0; __i < {sizeBytes}; __i++)");
            sb.AppendLine($"{ind}        {{");
            sb.AppendLine($"{ind}            __buf[{startByte} + __i] = (byte)((__buf[{startByte} + __i] & 0x{lowMask:X2}) | (__ebuf[__i] << {bitOffset}));");
            sb.AppendLine($"{ind}            if ({startByte} + __i + 1 < SIZE_IN_BYTES)");
            sb.AppendLine($"{ind}                __buf[{startByte} + __i + 1] = (byte)((__buf[{startByte} + __i + 1] & 0x{hiMask:X2}) | (__ebuf[__i] >> {8 - bitOffset}));");
            sb.AppendLine($"{ind}        }}");
        }
        sb.AppendLine($"{ind}        this = ReadFrom(__buf);");
        sb.AppendLine($"{ind}    }}");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();
    }

    private static void GenerateStaticBitProperty(StringBuilder sb, BitFieldsInfo info, WordLayout layout, BitFlagInfo flag, string ind)
    {
        int wi = flag.Bit / 64;
        int localBit = flag.Bit % 64;
        ulong mask = 1UL << localBit;

        var wordExprs = Enumerable.Range(0, layout.WordCount).Select(i =>
            i == wi ? layout.Literal(i, mask) : layout.Zero(i)).ToArray();
        var inline = InlineNew(layout, wordExprs);

        sb.AppendLine($"{ind}/// <summary>Returns a {info.TypeName} with only the {flag.Name} bit set.</summary>");
        if (inline != null)
        {
            sb.AppendLine($"{ind}public static {info.TypeName} {flag.Name}Bit => {inline};");
        }
        else
        {
            sb.AppendLine($"{ind}public static {info.TypeName} {flag.Name}Bit");
            sb.AppendLine($"{ind}{{");
            sb.AppendLine($"{ind}    get");
            sb.AppendLine($"{ind}    {{");
            EmitBlockConstruction(sb, info.TypeName, layout, $"{ind}        ", wordExprs);
            sb.AppendLine($"{ind}    }}");
            sb.AppendLine($"{ind}}}");
        }
        sb.AppendLine();
    }

    private static void GenerateStaticMaskProperty(StringBuilder sb, BitFieldsInfo info, WordLayout layout, BitFieldInfo field, string ind)
    {
        int wc = layout.WordCount;
        var words = new ulong[wc];
        for (int b = field.Shift; b < field.Shift + field.Width && b < wc * 64; b++)
            words[b / 64] |= 1UL << (b % 64);

        var wordExprs = Enumerable.Range(0, wc).Select(i =>
            words[i] != 0 ? layout.Literal(i, words[i]) : layout.Zero(i)).ToArray();
        var inline = InlineNew(layout, wordExprs);

        sb.AppendLine($"{ind}/// <summary>Returns a {info.TypeName} with the mask for the {field.Name} field (bits {field.Shift}-{field.Shift + field.Width - 1}).</summary>");
        if (inline != null)
        {
            sb.AppendLine($"{ind}public static {info.TypeName} {field.Name}Mask => {inline};");
        }
        else
        {
            sb.AppendLine($"{ind}public static {info.TypeName} {field.Name}Mask");
            sb.AppendLine($"{ind}{{");
            sb.AppendLine($"{ind}    get");
            sb.AppendLine($"{ind}    {{");
            EmitBlockConstruction(sb, info.TypeName, layout, $"{ind}        ", wordExprs);
            sb.AppendLine($"{ind}    }}");
            sb.AppendLine($"{ind}}}");
        }
        sb.AppendLine();
    }

    private static void GenerateWithFlagMethod(StringBuilder sb, BitFieldsInfo info, WordLayout layout, BitFlagInfo flag, string ind)
    {
        int wi = flag.Bit / 64;
        int localBit = flag.Bit % 64;
        ulong mask = 1UL << localBit;
        int wc = layout.WordCount;

        sb.AppendLine($"{ind}/// <summary>Returns a new {info.TypeName} with the {flag.Name} flag set to the specified value.</summary>");
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");

        var wordExprs = Enumerable.Range(0, wc).Select(i =>
        {
            if (i != wi) return $"_w{i}";
            string rd = layout.Read("", i);
            return layout.Store(i, $"value ? ({rd} | 0x{mask:X}UL) : ({rd} & 0x{~mask:X16}UL)");
        }).ToArray();
        var inline = InlineNew(layout, wordExprs);

        if (inline != null)
        {
            sb.AppendLine($"{ind}public {info.TypeName} With{flag.Name}(bool value) => {inline};");
        }
        else
        {
            // Use copy+modify for >4 words
            sb.AppendLine($"{ind}public {info.TypeName} With{flag.Name}(bool value) {{ var copy = this; copy.{flag.Name} = value; return copy; }}");
        }
        sb.AppendLine();
    }

    private static void GenerateWithFieldMethod(StringBuilder sb, BitFieldsInfo info, WordLayout layout, BitFieldInfo field, string ind)
    {
        sb.AppendLine($"{ind}/// <summary>Returns a new {info.TypeName} with the {field.Name} field set to the specified value.</summary>");
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}public {info.TypeName} With{field.Name}({field.PropertyType} value) {{ var copy = this; copy.{field.Name} = value; return copy; }}");
        sb.AppendLine();
    }
}
