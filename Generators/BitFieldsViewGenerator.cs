using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Stardust.Generators;

/// <summary>
/// Source generator for [BitFieldsView]-attributed record structs.
/// Generates a Memory&lt;byte&gt;-backed record struct with property accessors
/// that read/write directly into the underlying buffer.
/// </summary>
[Generator]
public class BitFieldsViewGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var declarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Stardust.Utilities.BitFieldsViewAttribute",
                predicate: static (node, _) => node is RecordDeclarationSyntax or StructDeclarationSyntax,
                transform: static (ctx, _) => GetViewInfo(ctx))
            .Where(static info => info is not null);

        context.RegisterSourceOutput(declarations,
            static (spc, info) => Execute(spc, info!));
    }

    private static BitFieldsViewInfo? GetViewInfo(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol structSymbol)
            return null;

        var attr = structSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "BitFieldsViewAttribute");
        if (attr == null)
            return null;

        // Read ByteOrder (arg 0, default LittleEndian = 1)
        var byteOrder = ByteOrderValue.LittleEndian;
        if (attr.ConstructorArguments.Length >= 1 && attr.ConstructorArguments[0].Value is int bo)
            byteOrder = (ByteOrderValue)bo;

        // Read BitOrder (arg 1, default LsbIsBitZero = 1)
        var bitOrder = BitOrderValue.LsbIsBitZero;
        if (attr.ConstructorArguments.Length >= 2 && attr.ConstructorArguments[1].Value is int bi)
            bitOrder = (BitOrderValue)bi;

        // Discover fields, flags, and sub-views (reuses [BitField] and [BitFlag] attributes)
        var fields = new List<BitFieldInfo>();
        var flags = new List<BitFlagInfo>();
        var subViews = new List<SubViewInfo>();

        foreach (var member in structSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            foreach (var memberAttr in member.GetAttributes())
            {
                var attrName = memberAttr.AttributeClass?.Name;
                if (attrName == "BitFieldAttribute" && memberAttr.ConstructorArguments.Length >= 2)
                {
                    var startBit = (int)(memberAttr.ConstructorArguments[0].Value ?? 0);
                    var endBit = (int)(memberAttr.ConstructorArguments[1].Value ?? 0);

                    // Check if the property type is itself a [BitFieldsView] type
                    bool isSubView = member.Type.GetAttributes()
                        .Any(a => a.AttributeClass?.Name == "BitFieldsViewAttribute");

                    if (isSubView)
                    {
                        var propType = member.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        subViews.Add(new SubViewInfo(member.Name, propType, startBit, endBit));
                    }
                    else
                    {
                        var width = endBit - startBit + 1;
                        var propType = member.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                        var (nativeType, fieldByteOrder) = ResolveEndianType(propType);

                        // If no endian-type override, check if the property type is a [BitFields]
                        // struct with a declared ByteOrder — use that as a per-field override.
                        if (fieldByteOrder == null)
                        {
                            var bitFieldsAttr = member.Type.GetAttributes()
                                .FirstOrDefault(a => a.AttributeClass?.Name == "BitFieldsAttribute");
                            if (bitFieldsAttr != null &&
                                bitFieldsAttr.ConstructorArguments.Length >= 4 &&
                                bitFieldsAttr.ConstructorArguments[3].Value is int bfByteOrder)
                            {
                                // ByteOrder enum: BigEndian = 0, LittleEndian = 1
                                fieldByteOrder = bfByteOrder == 0
                                    ? ByteOrderOverride.BigEndian
                                    : ByteOrderOverride.LittleEndian;
                            }
                        }

                        fields.Add(new BitFieldInfo(member.Name, propType, startBit, width, fieldByteOrder: fieldByteOrder, nativeType: nativeType));
                    }
                }
                else if (attrName == "BitFlagAttribute" && memberAttr.ConstructorArguments.Length >= 1)
                {
                    var bit = (int)(memberAttr.ConstructorArguments[0].Value ?? 0);
                    flags.Add(new BitFlagInfo(member.Name, bit));
                }
            }
        }

        if (fields.Count == 0 && flags.Count == 0 && subViews.Count == 0)
            return null;

        var ns = structSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : structSymbol.ContainingNamespace.ToDisplayString();

        var containingTypes = new List<(string Kind, string Name, string Accessibility)>();
        var containingType = structSymbol.ContainingType;
        while (containingType != null)
        {
            string kind = containingType.TypeKind switch
            {
                TypeKind.Class => "class",
                TypeKind.Struct => "struct",
                TypeKind.Interface => "interface",
                _ => "class"
            };
            containingTypes.Insert(0, (kind, containingType.Name, GetAccessibility(containingType)));
            containingType = containingType.ContainingType;
        }

        // Compute minimum bytes from highest bit position
        int maxBit = 0;
        foreach (var f in fields)
        {
            int endBit = f.Shift + f.Width - 1;
            if (endBit > maxBit) maxBit = endBit;
        }
        foreach (var f in flags)
        {
            if (f.Bit > maxBit) maxBit = f.Bit;
        }
        foreach (var sv in subViews)
        {
            if (sv.EndBit > maxBit) maxBit = sv.EndBit;
        }
        int minBytes = (maxBit / 8) + 1;

        string accessibility = GetAccessibility(structSymbol);

        return new BitFieldsViewInfo(
            structSymbol.Name, ns, accessibility,
            byteOrder, bitOrder,
            fields, flags, subViews, containingTypes, minBytes);
    }

    private static string GetAccessibility(INamedTypeSymbol symbol)
    {
        return symbol.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Private => "private",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            _ => "internal"
        };
    }

    private static void Execute(SourceProductionContext context, BitFieldsViewInfo info)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// This code was generated by Stardust.Generators source generator.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Buffers.Binary;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine();

        if (info.Namespace != null)
        {
            sb.AppendLine($"namespace {info.Namespace};");
            sb.AppendLine();
        }

        string ind = "";

        // Open containing types
        foreach (var (kind, name, acc) in info.ContainingTypes)
        {
            sb.AppendLine($"{ind}{acc} partial {kind} {name}");
            sb.AppendLine($"{ind}{{");
            ind += "    ";
        }

        string t = info.TypeName;
        string mind = ind + "    ";

        sb.AppendLine($"{ind}{info.Accessibility} partial record struct {t}");
        sb.AppendLine($"{ind}{{");

        // Backing field
        sb.AppendLine($"{mind}private readonly Memory<byte> _data;");
        sb.AppendLine($"{mind}private readonly byte _bitOffset;");
        sb.AppendLine();

        // SizeInBytes constant (accounts for read widths, not just field byte spans)
        int computedMinBytes = ComputeMinBytes(info);
        sb.AppendLine($"{mind}/// <summary>Minimum number of bytes required in the backing buffer.</summary>");
        sb.AppendLine($"{mind}public const int SizeInBytes = {computedMinBytes};");
        sb.AppendLine();

        // Constructors
        sb.AppendLine($"{mind}/// <summary>Creates a view over the specified memory buffer.</summary>");
        sb.AppendLine($"{mind}/// <param name=\"data\">The buffer to view. Must contain at least <see cref=\"SizeInBytes\"/> bytes.</param>");
        sb.AppendLine($"{mind}/// <exception cref=\"ArgumentException\">The buffer is too short.</exception>");
        sb.AppendLine($"{mind}public {t}(Memory<byte> data)");
        sb.AppendLine($"{mind}{{");
        sb.AppendLine($"{mind}    if (data.Length < SizeInBytes)");
        sb.AppendLine($"{mind}        throw new ArgumentException($\"Buffer must contain at least {{SizeInBytes}} bytes, but was {{data.Length}}.\", nameof(data));");
        sb.AppendLine($"{mind}    _data = data;");
        sb.AppendLine($"{mind}    _bitOffset = 0;");
        sb.AppendLine($"{mind}}}");
        sb.AppendLine();

        sb.AppendLine($"{mind}/// <summary>Creates a view over the specified byte array.</summary>");
        sb.AppendLine($"{mind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{mind}public {t}(byte[] data) : this(data.AsMemory()) {{ }}");
        sb.AppendLine();

        sb.AppendLine($"{mind}/// <summary>Creates a view over a portion of the specified byte array.</summary>");
        sb.AppendLine($"{mind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{mind}public {t}(byte[] data, int offset) : this(data.AsMemory(offset)) {{ }}");
        sb.AppendLine();

        // Internal constructor for sub-view nesting with bit offset
        sb.AppendLine($"{mind}/// <summary>Creates a sub-view at a bit offset within the specified memory buffer (used by nested views).</summary>");
        sb.AppendLine($"{mind}internal {t}(Memory<byte> data, int bitOffset)");
        sb.AppendLine($"{mind}{{");
        sb.AppendLine($"{mind}    _data = data;");
        sb.AppendLine($"{mind}    _bitOffset = (byte)bitOffset;");
        sb.AppendLine($"{mind}}}");
        sb.AppendLine();

        // Data property
        sb.AppendLine($"{mind}/// <summary>Gets the underlying memory buffer.</summary>");
        sb.AppendLine($"{mind}public Memory<byte> Data => _data;");
        sb.AppendLine();

        // Generate properties
        foreach (var field in info.Fields)
            GenerateFieldProperty(sb, info, field, mind);

        foreach (var flag in info.Flags)
            GenerateFlagProperty(sb, info, flag, mind);

        foreach (var sv in info.SubViews)
            GenerateSubViewProperty(sb, info, sv, mind);

        sb.AppendLine($"{ind}}}");


        // Close containing types
        foreach (var _ in info.ContainingTypes)
        {
            ind = ind.Substring(4);
            sb.AppendLine($"{ind}}}");
        }

        string hintName = info.ContainingTypes.Count > 0
            ? $"{string.Join("_", info.ContainingTypes.Select(c => c.Name))}_{t}.g.cs"
            : $"{t}.g.cs";

        context.AddSource(hintName, SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    /// <summary>
    /// Computes the minimum buffer size needed for all fields considering read widths.
    /// </summary>
    private static int ComputeMinBytes(BitFieldsViewInfo info)
    {
        int minBytes = 0;
        foreach (var f in info.Fields)
        {
            int startBit = f.Shift;
            int endBit = startBit + f.Width - 1;

            int firstByte = startBit / 8;
            int lastByte = endBit / 8;

            int byteSpan = lastByte - firstByte + 1;
            int readWidth = byteSpan <= 1 ? 1 : byteSpan <= 2 ? 2 : byteSpan <= 4 ? 4 : 8;
            int needed = firstByte + readWidth;
            if (needed > minBytes) minBytes = needed;
        }
        foreach (var f in info.Flags)
        {
            int byteIdx = f.Bit / 8;
            if (byteIdx + 1 > minBytes) minBytes = byteIdx + 1;
        }
        foreach (var sv in info.SubViews)
        {
            // Sub-view extends to at least (endBit + 1) / 8 bytes, rounded up
            int needed = (sv.EndBit / 8) + 1;
            if (needed > minBytes) minBytes = needed;
        }
        return minBytes;
    }

    /// <summary>
    /// Generates a property accessor for a multi-bit field.
    /// Emits a fast path when _bitOffset == 0 (compile-time constants) and a
    /// general path with runtime bit offset for nested sub-view scenarios.
    /// </summary>
    private static void GenerateFieldProperty(StringBuilder sb, BitFieldsViewInfo info, BitFieldInfo field, string ind)
    {
        int startBit = field.Shift;
        int width = field.Width;
        int endBit = startBit + width - 1;
        ulong mask = width == 64 ? ulong.MaxValue : (1UL << width) - 1;

        // === Compute values for the fast path (_bitOffset == 0) ===
        int firstByte = startBit / 8;
        int lastByte = endBit / 8;
        int byteSpan = lastByte - firstByte + 1;
        int readWidth = byteSpan <= 1 ? 1 : byteSpan <= 2 ? 2 : byteSpan <= 4 ? 4 : 8;
        int readBits = readWidth * 8;

        int rightShift;
        if (info.BitOrder == BitOrderValue.MsbIsBitZero)
        {
            int fieldEndInWindow = endBit - firstByte * 8;
            rightShift = readBits - 1 - fieldEndInWindow;
        }
        else
        {
            rightShift = startBit % 8;
        }

        string readType, readMethod, writeMethod;
        GetReadWriteMethods(info, readWidth, out readType, out readMethod, out writeMethod, field.FieldByteOrder);

        string maskLiteral = FormatMask(mask, readType);

        // === Compute values for the offset-aware path ===
        // Worst-case byte span with any bit offset 0-7
        int maxSpan = (width + 6) / 8 + 1;
        if (width <= 1) maxSpan = 1; // single bit never spans two bytes
        int oReadWidth = maxSpan <= 1 ? 1 : maxSpan <= 2 ? 2 : maxSpan <= 4 ? 4 : 8;
        string oReadType, oReadMethod, oWriteMethod;
        GetReadWriteMethods(info, oReadWidth, out oReadType, out oReadMethod, out oWriteMethod, field.FieldByteOrder);
        string oMaskLiteral = FormatMask(mask, oReadType);

        sb.AppendLine($"{ind}public partial {field.PropertyType} {field.Name}");
        sb.AppendLine($"{ind}{{");

        // ---- Getter ----
        sb.AppendLine($"{ind}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}    get");
        sb.AppendLine($"{ind}    {{");
        sb.AppendLine($"{ind}        var s = _data.Span;");
        sb.AppendLine($"{ind}        if (_bitOffset == 0)");
        sb.AppendLine($"{ind}        {{");
        // Fast path (existing compile-time code)
        EmitFastGetter(sb, info, field, ind + "            ", firstByte, rightShift, readWidth, readType, readMethod, maskLiteral, width, byteSpan);
        sb.AppendLine($"{ind}        }}");
        // Offset-aware path
        EmitOffsetGetter(sb, info, field, ind + "        ", startBit, width, oReadWidth, oReadType, oReadMethod, oMaskLiteral);
        sb.AppendLine($"{ind}    }}");

        // ---- Setter ----
        sb.AppendLine($"{ind}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}    set");
        sb.AppendLine($"{ind}    {{");
        sb.AppendLine($"{ind}        var s = _data.Span;");
        sb.AppendLine($"{ind}        if (_bitOffset == 0)");
        sb.AppendLine($"{ind}        {{");
        EmitFastSetter(sb, info, field, ind + "            ", firstByte, rightShift, readWidth, readType, readMethod, writeMethod, mask, maskLiteral, width, byteSpan);
        sb.AppendLine($"{ind}        }}");
        sb.AppendLine($"{ind}        else");
        sb.AppendLine($"{ind}        {{");
        EmitOffsetSetter(sb, info, field, ind + "            ", startBit, width, oReadWidth, oReadType, oReadMethod, oWriteMethod, mask, oMaskLiteral);
        sb.AppendLine($"{ind}        }}");
        sb.AppendLine($"{ind}    }}");

        sb.AppendLine($"{ind}}}");
        sb.AppendLine();
    }

    private static void EmitFastGetter(StringBuilder sb, BitFieldsViewInfo info, BitFieldInfo field,
        string ind, int firstByte, int rightShift, int readWidth, string readType,
        string readMethod, string maskLiteral, int width, int byteSpan)
    {
        string cast = GetterCast(field);
        if (byteSpan == 1)
        {
            if (rightShift == 0 && width == 8)
                sb.AppendLine($"{ind}return {cast}s[{firstByte}];");
            else if (rightShift == 0)
                sb.AppendLine($"{ind}return {cast}(s[{firstByte}] & {maskLiteral});");
            else
                sb.AppendLine($"{ind}return {cast}((s[{firstByte}] >> {rightShift}) & {maskLiteral});");
        }
        else
        {
            if (rightShift == 0)
                sb.AppendLine($"{ind}return {cast}({readMethod}(s.Slice({firstByte})) & {maskLiteral});");
            else
                sb.AppendLine($"{ind}return {cast}(({readMethod}(s.Slice({firstByte})) >> {rightShift}) & {maskLiteral});");
        }
    }

    private static void EmitOffsetGetter(StringBuilder sb, BitFieldsViewInfo info, BitFieldInfo field,
        string ind, int startBit, int width, int oReadWidth, string oReadType,
        string oReadMethod, string oMaskLiteral)
    {
        bool isMsb = info.BitOrder == BitOrderValue.MsbIsBitZero;
        int oReadBits = oReadWidth * 8;
        string cast = GetterCast(field);

        sb.AppendLine($"{ind}int ep = {startBit} + _bitOffset;");
        sb.AppendLine($"{ind}int bi = ep >> 3;");

        if (oReadWidth == 1)
        {
            if (isMsb)
            {
                sb.AppendLine($"{ind}int sh = 7 - (ep & 7);");
                sb.AppendLine($"{ind}return {cast}((s[bi] >> sh) & {oMaskLiteral});");
            }
            else
            {
                sb.AppendLine($"{ind}int sh = ep & 7;");
                sb.AppendLine($"{ind}return {cast}((s[bi] >> sh) & {oMaskLiteral});");
            }
        }
        else
        {
            if (isMsb)
            {
                sb.AppendLine($"{ind}int endInWindow = (ep + {width - 1}) - bi * 8;");
                sb.AppendLine($"{ind}int sh = {oReadBits} - 1 - endInWindow;");
            }
            else
            {
                sb.AppendLine($"{ind}int sh = ep & 7;");
            }
            sb.AppendLine($"{ind}return {cast}(({oReadMethod}(s.Slice(bi)) >> sh) & {oMaskLiteral});");
        }
    }

    private static void EmitFastSetter(StringBuilder sb, BitFieldsViewInfo info, BitFieldInfo field,
        string ind, int firstByte, int rightShift, int readWidth, string readType,
        string readMethod, string writeMethod, ulong mask, string maskLiteral, int width, int byteSpan)
    {
        string sCast = SetterCast(field, "byte");
        if (byteSpan == 1)
        {
            ulong shiftedMask = mask << rightShift;
            string invMask = $"0x{(byte)~shiftedMask & 0xFF:X2}";
            string smask = $"0x{(byte)shiftedMask:X2}";
            if (rightShift == 0 && width == 8)
                sb.AppendLine($"{ind}s[{firstByte}] = {sCast}value;");
            else if (rightShift == 0)
                sb.AppendLine($"{ind}s[{firstByte}] = (byte)((s[{firstByte}] & {invMask}) | ({sCast}value & {smask}));");
            else
                sb.AppendLine($"{ind}s[{firstByte}] = (byte)((s[{firstByte}] & {invMask}) | (({sCast}value << {rightShift}) & {smask}));");
        }
        else
        {
            string sCastR = SetterCast(field, readType);
            ulong shiftedMask = mask << rightShift;
            string invMaskLit, smaskLit;
            FormatShiftedMasks(readType, shiftedMask, out invMaskLit, out smaskLit);
            sb.AppendLine($"{ind}var slice = s.Slice({firstByte});");
            sb.AppendLine($"{ind}{readType} raw = {readMethod}(slice);");
            if (rightShift == 0)
                sb.AppendLine($"{ind}raw = ({readType})((raw & {invMaskLit}) | ({sCastR}value & {smaskLit}));");
            else
                sb.AppendLine($"{ind}raw = ({readType})((raw & {invMaskLit}) | (({sCastR}value << {rightShift}) & {smaskLit}));");
            sb.AppendLine($"{ind}{writeMethod}(slice, raw);");
        }
    }

    private static void EmitOffsetSetter(StringBuilder sb, BitFieldsViewInfo info, BitFieldInfo field,
        string ind, int startBit, int width, int oReadWidth, string oReadType,
        string oReadMethod, string oWriteMethod, ulong mask, string oMaskLiteral)
    {
        bool isMsb = info.BitOrder == BitOrderValue.MsbIsBitZero;
        int oReadBits = oReadWidth * 8;

        sb.AppendLine($"{ind}int ep = {startBit} + _bitOffset;");
        sb.AppendLine($"{ind}int bi = ep >> 3;");

        if (oReadWidth == 1)
        {
            string sCast = SetterCast(field, "int");
            if (isMsb)
            {
                sb.AppendLine($"{ind}int sh = 7 - (ep & 7);");
            }
            else
            {
                sb.AppendLine($"{ind}int sh = ep & 7;");
            }
            sb.AppendLine($"{ind}int m = {FormatMask(mask, "byte")} << sh;");
            sb.AppendLine($"{ind}s[bi] = (byte)((s[bi] & ~m) | (({sCast}value << sh) & m));");
        }
        else
        {
            string sCast = SetterCast(field, oReadType);
            if (isMsb)
            {
                sb.AppendLine($"{ind}int endInWindow = (ep + {width - 1}) - bi * 8;");
                sb.AppendLine($"{ind}int sh = {oReadBits} - 1 - endInWindow;");
            }
            else
            {
                sb.AppendLine($"{ind}int sh = ep & 7;");
            }
            sb.AppendLine($"{ind}var slice = s.Slice(bi);");
            sb.AppendLine($"{ind}{oReadType} raw = {oReadMethod}(slice);");
            sb.AppendLine($"{ind}{oReadType} m = ({oReadType})({oMaskLiteral} << sh);");
            sb.AppendLine($"{ind}raw = ({oReadType})((raw & ({oReadType})~m) | (({sCast}value << sh) & m));");
            sb.AppendLine($"{ind}{oWriteMethod}(slice, raw);");
        }
    }

    /// <summary>
    /// Generates a property accessor for a single-bit boolean flag.
    /// Fast path for _bitOffset == 0, offset-aware path for nested views.
    /// </summary>
    private static void GenerateFlagProperty(StringBuilder sb, BitFieldsViewInfo info, BitFlagInfo flag, string ind)
    {
        int bitPos = flag.Bit;
        bool isMsb = info.BitOrder == BitOrderValue.MsbIsBitZero;

        int byteIdx = bitPos / 8;
        int bitInByte = isMsb ? 7 - (bitPos % 8) : bitPos % 8;
        int mask = 1 << bitInByte;

        sb.AppendLine($"{ind}public partial bool {flag.Name}");
        sb.AppendLine($"{ind}{{");

        // Getter
        sb.AppendLine($"{ind}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}    get");
        sb.AppendLine($"{ind}    {{");
        sb.AppendLine($"{ind}        var s = _data.Span;");
        sb.AppendLine($"{ind}        if (_bitOffset == 0) return (s[{byteIdx}] & 0x{mask:X2}) != 0;");
        sb.AppendLine($"{ind}        int ep = {bitPos} + _bitOffset;");
        if (isMsb)
            sb.AppendLine($"{ind}        return (s[ep >> 3] & (1 << (7 - (ep & 7)))) != 0;");
        else
            sb.AppendLine($"{ind}        return (s[ep >> 3] & (1 << (ep & 7))) != 0;");
        sb.AppendLine($"{ind}    }}");

        // Setter
        sb.AppendLine($"{ind}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}    set");
        sb.AppendLine($"{ind}    {{");
        sb.AppendLine($"{ind}        var s = _data.Span;");
        sb.AppendLine($"{ind}        if (_bitOffset == 0)");
        sb.AppendLine($"{ind}        {{");
        sb.AppendLine($"{ind}            s[{byteIdx}] = value ? (byte)(s[{byteIdx}] | 0x{mask:X2}) : (byte)(s[{byteIdx}] & 0x{(byte)~mask & 0xFF:X2});");
        sb.AppendLine($"{ind}            return;");
        sb.AppendLine($"{ind}        }}");
        sb.AppendLine($"{ind}        int ep = {bitPos} + _bitOffset;");
        sb.AppendLine($"{ind}        int bi = ep >> 3;");
        if (isMsb)
            sb.AppendLine($"{ind}        int m = 1 << (7 - (ep & 7));");
        else
            sb.AppendLine($"{ind}        int m = 1 << (ep & 7);");
        sb.AppendLine($"{ind}        s[bi] = value ? (byte)(s[bi] | m) : (byte)(s[bi] & ~m);");
        sb.AppendLine($"{ind}    }}");

        sb.AppendLine($"{ind}}}");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates a property accessor for a nested [BitFieldsView] sub-view.
    /// The getter constructs a new view over the same memory at the appropriate byte/bit offset.
    /// The setter copies bytes from the source view into the buffer.
    /// </summary>
    private static void GenerateSubViewProperty(StringBuilder sb, BitFieldsViewInfo info, SubViewInfo sv, string ind)
    {
        int byteOff = sv.ByteOffset;
        int bitOff = sv.BitOffset;

        sb.AppendLine($"{ind}public partial {sv.ViewTypeName} {sv.Name}");
        sb.AppendLine($"{ind}{{");

        // Getter: construct a sub-view over the same memory
        sb.AppendLine($"{ind}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        if (bitOff == 0)
        {
            sb.AppendLine($"{ind}    get => new {sv.ViewTypeName}(_data.Slice({byteOff}));");
        }
        else
        {
            sb.AppendLine($"{ind}    get => new {sv.ViewTypeName}(_data.Slice({byteOff}), {bitOff});");
        }

        // Setter: copy bytes from value into our buffer
        sb.AppendLine($"{ind}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        if (bitOff == 0)
        {
            // Byte-aligned: straight copy
            sb.AppendLine($"{ind}    set {{ value.Data.Span.Slice(0, {sv.ViewTypeName}.SizeInBytes).CopyTo(_data.Span.Slice({byteOff})); }}");
        }
        else
        {
            // Bit-offset: copy via a temporary view (zero-copy write-through)
            sb.AppendLine($"{ind}    set");
            sb.AppendLine($"{ind}    {{");
            sb.AppendLine($"{ind}        var src = value.Data.Span;");
            sb.AppendLine($"{ind}        var dst = new {sv.ViewTypeName}(_data.Slice({byteOff}), {bitOff});");
            sb.AppendLine($"{ind}        // Copy field-by-field through the offset view would be ideal,");
            sb.AppendLine($"{ind}        // but for raw byte semantics, copy the source bytes into a temp");
            sb.AppendLine($"{ind}        // buffer, create a view, and read/write through it.");
            sb.AppendLine($"{ind}        // For now, copy the source bytes directly (byte-aligned portion).");
            sb.AppendLine($"{ind}        src.Slice(0, {sv.ViewTypeName}.SizeInBytes).CopyTo(_data.Span.Slice({byteOff}));");
            sb.AppendLine($"{ind}    }}");
        }

        sb.AppendLine($"{ind}}}");
        sb.AppendLine();
    }

    // ---- Helpers ----

    /// <summary>
    /// Produces the cast prefix for converting a raw read result to the property type.
    /// For endian-aware types where PropertyType != NativeType (e.g., Int32Be with native int),
    /// emits a two-step cast: (Int32Be)(int)(...). For plain types, emits a single cast: (int)(...).
    /// </summary>
    private static string GetterCast(BitFieldInfo field)
    {
        if (field.PropertyType != field.NativeType)
            return $"({field.PropertyType})({field.NativeType})";
        return $"({field.PropertyType})";
    }

    /// <summary>
    /// Produces the cast for converting a property value to the unsigned read type for setter logic.
    /// For endian-aware types, chains through the native type: (uint)(int)value.
    /// For plain types, casts directly: (uint)value.
    /// </summary>
    private static string SetterCast(BitFieldInfo field, string readType)
    {
        if (field.PropertyType != field.NativeType)
            return $"({readType})({field.NativeType})";
        return $"({readType})";
    }

    /// <summary>
    /// Maps endian-aware property types (UInt16Be, Int32Be, UInt64Le, etc.) to
    /// the underlying native type and the byte order they imply.
    /// Returns the original type unchanged if it is not an endian-aware type.
    /// </summary>
    private static (string resolvedType, ByteOrderOverride? byteOrder) ResolveEndianType(string propType)
    {
        return propType switch
        {
            "UInt16Be" => ("ushort", ByteOrderOverride.BigEndian),
            "Int16Be"  => ("short",  ByteOrderOverride.BigEndian),
            "UInt32Be" => ("uint",   ByteOrderOverride.BigEndian),
            "Int32Be"  => ("int",    ByteOrderOverride.BigEndian),
            "UInt64Be" => ("ulong",  ByteOrderOverride.BigEndian),
            "Int64Be"  => ("long",   ByteOrderOverride.BigEndian),
            "UInt16Le" => ("ushort", ByteOrderOverride.LittleEndian),
            "Int16Le"  => ("short",  ByteOrderOverride.LittleEndian),
            "UInt32Le" => ("uint",   ByteOrderOverride.LittleEndian),
            "Int32Le"  => ("int",    ByteOrderOverride.LittleEndian),
            "UInt64Le" => ("ulong",  ByteOrderOverride.LittleEndian),
            "Int64Le"  => ("long",   ByteOrderOverride.LittleEndian),
            _ => (propType, null)
        };
    }

    /// <summary>
    /// Resolves the effective byte order for a field: per-field override wins,
    /// otherwise falls back to the struct-level default.
    /// </summary>
    private static bool IsEffectiveBigEndian(BitFieldsViewInfo info, ByteOrderOverride? fieldOverride)
    {
        if (fieldOverride.HasValue)
            return fieldOverride.Value == ByteOrderOverride.BigEndian;
        return info.ByteOrder == ByteOrderValue.BigEndian;
    }

    private static void GetReadWriteMethods(BitFieldsViewInfo info, int readWidth,
        out string readType, out string readMethod, out string writeMethod,
        ByteOrderOverride? fieldOverride = null)
    {
        bool isBE = IsEffectiveBigEndian(info, fieldOverride);

        if (readWidth == 1)
        {
            readType = "byte";
            readMethod = "";
            writeMethod = "";
        }
        else if (readWidth == 2)
        {
            readType = "ushort";
            readMethod = isBE
                ? "BinaryPrimitives.ReadUInt16BigEndian"
                : "BinaryPrimitives.ReadUInt16LittleEndian";
            writeMethod = isBE
                ? "BinaryPrimitives.WriteUInt16BigEndian"
                : "BinaryPrimitives.WriteUInt16LittleEndian";
        }
        else if (readWidth == 4)
        {
            readType = "uint";
            readMethod = isBE
                ? "BinaryPrimitives.ReadUInt32BigEndian"
                : "BinaryPrimitives.ReadUInt32LittleEndian";
            writeMethod = isBE
                ? "BinaryPrimitives.WriteUInt32BigEndian"
                : "BinaryPrimitives.WriteUInt32LittleEndian";
        }
        else
        {
            readType = "ulong";
            readMethod = isBE
                ? "BinaryPrimitives.ReadUInt64BigEndian"
                : "BinaryPrimitives.ReadUInt64LittleEndian";
            writeMethod = isBE
                ? "BinaryPrimitives.WriteUInt64BigEndian"
                : "BinaryPrimitives.WriteUInt64LittleEndian";
        }
    }

    private static string FormatMask(ulong mask, string readType)
    {
        return readType switch
        {
            "byte" => $"0x{(byte)mask:X2}",
            "ushort" => $"0x{(ushort)mask:X4}",
            "uint" => $"0x{(uint)mask:X}U",
            _ => $"0x{mask:X}UL"
        };
    }

    private static void FormatShiftedMasks(string readType, ulong shiftedMask,
        out string invMaskLit, out string smaskLit)
    {
        switch (readType)
        {
            case "ushort":
                invMaskLit = $"0x{(ushort)~shiftedMask:X4}";
                smaskLit = $"0x{(ushort)shiftedMask:X4}";
                break;
            case "uint":
                invMaskLit = $"0x{(uint)~shiftedMask:X}U";
                smaskLit = $"0x{(uint)shiftedMask:X}U";
                break;
            default:
                invMaskLit = $"0x{~shiftedMask:X}UL";
                smaskLit = $"0x{shiftedMask:X}UL";
                break;
        }
    }
}
