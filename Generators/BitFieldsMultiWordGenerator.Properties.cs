using System.Linq;
using System.Text;

namespace Stardust.Generators;

internal static partial class BitFieldsMultiWordGenerator
{
    private static void GenerateBitFieldProperty(StringBuilder sb, BitFieldsInfo info, WordLayout layout, BitFieldInfo field, string ind)
    {
        int shift = field.Shift;
        int width = field.Width;
        int startWord = shift / 64;
        int localShift = shift % 64;
        int endBit = shift + width - 1;
        int endWord = endBit / 64;
        bool crossWord = startWord != endWord;

        sb.AppendLine($"{ind}public partial {field.PropertyType} {field.Name}");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");

        if (!crossWord)
        {
            ulong mask = (width == 64) ? ulong.MaxValue : (1UL << width) - 1;
            string rd = layout.Read("", startWord);
            if (localShift == 0 && width == 64)
                sb.AppendLine($"{ind}    get => ({field.PropertyType}){rd};");
            else if (localShift == 0)
                sb.AppendLine($"{ind}    get => ({field.PropertyType})({rd} & 0x{mask:X}UL);");
            else
                sb.AppendLine($"{ind}    get => ({field.PropertyType})(({rd} >> {localShift}) & 0x{mask:X}UL);");

            sb.AppendLine($"{ind}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");

            ulong shiftedMask = mask << localShift;
            if (localShift == 0 && width == 64)
                sb.AppendLine($"{ind}    set => _w{startWord} = {layout.Store(startWord, "(ulong)value")};");
            else if (localShift == 0)
                sb.AppendLine($"{ind}    set => _w{startWord} = {layout.Store(startWord, $"({rd} & 0x{~shiftedMask:X16}UL) | ((ulong)value & 0x{shiftedMask:X16}UL)")};");
            else
                sb.AppendLine($"{ind}    set => _w{startWord} = {layout.Store(startWord, $"({rd} & 0x{~shiftedMask:X16}UL) | (((ulong)value << {localShift}) & 0x{shiftedMask:X16}UL)")};");
        }
        else
        {
            int bitsInStart = 64 - localShift;
            int bitsInEnd = width - bitsInStart;
            ulong maskEnd = (1UL << bitsInEnd) - 1;
            string rdS = layout.Read("", startWord);
            string rdE = layout.Read("", endWord);

            sb.AppendLine($"{ind}    get => ({field.PropertyType})(({rdS} >> {localShift}) | (({rdE} & 0x{maskEnd:X}UL) << {bitsInStart}));");

            sb.AppendLine($"{ind}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{ind}    set");
            sb.AppendLine($"{ind}    {{");
            ulong maskStart = (1UL << bitsInStart) - 1;
            sb.AppendLine($"{ind}        _w{startWord} = {layout.Store(startWord, $"({rdS} & 0x{(1UL << localShift) - 1:X16}UL) | (((ulong)value & 0x{maskStart:X}UL) << {localShift})")};");
            sb.AppendLine($"{ind}        _w{endWord} = {layout.Store(endWord, $"({rdE} & 0x{~maskEnd:X16}UL) | (((ulong)value >> {bitsInStart}) & 0x{maskEnd:X}UL)")};");
            sb.AppendLine($"{ind}    }}");
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
        sb.AppendLine($"{ind}    get => ({rd} & 0x{mask:X}UL) != 0;");
        sb.AppendLine($"{ind}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}    set => _w{wi} = {layout.Store(wi, $"value ? ({rd} | 0x{mask:X}UL) : ({rd} & 0x{~mask:X16}UL)")};");
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
