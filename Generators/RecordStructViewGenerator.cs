using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Stardust.Generators;

/// <summary>
/// Source generator for [BitFields]-attributed record structs.
/// Generates a Memory&lt;byte&gt;-backed record struct with property accessors
/// that read/write directly into the underlying buffer.
/// </summary>
[Generator]
public class RecordStructViewGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // [BitFields] on a record struct (no storage type = view mode)
        var declarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Stardust.Utilities.BitFieldsAttribute",
                predicate: static (node, _) => node is RecordDeclarationSyntax,
                transform: static (ctx, _) => GetViewInfo(ctx))
            .Where(static info => info is not null);

        context.RegisterSourceOutput(declarations,
            static (spc, info) => Execute(spc, info!));
    }

    private static RecordStructViewInfo? GetViewInfo(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol structSymbol)
            return null;

        var attr = structSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name is "BitFieldsAttribute");
        if (attr == null)
            return null;

        // For [BitFields] on a record struct, the ByteOrder/BitOrder constructor
        // has the same parameter layout.
        // For [BitFields(typeof(T), ...)] or [BitFields(StorageType, ...)] on a record struct,
        // skip -- the value-type generator should handle it (or error).
        if (attr.ConstructorArguments.Length >= 1)
        {
            // If a storage type or bit count was provided, this is a value-type usage on a record struct.
            // Skip it -- the view generator doesn't handle storage types.
            if (attr.ConstructorArguments.Length >= 1)
            {
                var firstArg = attr.ConstructorArguments[0];
                // ByteOrder is an enum with underlying int; StorageType is also an int enum.
                // Type arguments come as INamedTypeSymbol. int bit count comes as int.
                // ByteOrder enum values: LittleEndian=1, BigEndian=0
                // We need to distinguish ByteOrder from StorageType/Type/int.
                // The (ByteOrder, BitOrder) constructor has first param of type ByteOrder.
                if (firstArg.Type?.Name != "ByteOrder")
                    return null;
            }
        }

        // Read ByteOrder (arg 0, default LittleEndian = 1)
        var byteOrder = ByteOrder.LittleEndian;
        if (attr.ConstructorArguments.Length >= 1 && attr.ConstructorArguments[0].Value is int bo)
            byteOrder = (ByteOrder)bo;

        // Read BitOrder (arg 1, default BitZeroIsLsb = 1)
        var bitOrder = BitOrder.BitZeroIsLsb;
        if (attr.ConstructorArguments.Length >= 2 && attr.ConstructorArguments[1].Value is int bi)
            bitOrder = (BitOrder)bi;

        // Read optional struct-level Description from named argument
        string? structDescription = null;
        foreach (var named in attr.NamedArguments)
        {
            if (named.Key == "Description" && named.Value.Value is string sd)
                structDescription = sd;
        }

        // Discover fields, flags, and sub-views (reuses [BitField] and [BitFlag] attributes)
        var fields = new List<BitFieldInfo>();
        var flags = new List<BitFlagInfo>();
        var subViews = new List<SubViewInfo>();
        var nonPartialProperties = new List<NonPartialPropertyInfo>();
        var propertyDiagnostics = new List<PropertyDiagnosticInfo>();

        foreach (var member in structSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            foreach (var memberAttr in member.GetAttributes())
            {
                var attrName = memberAttr.AttributeClass?.Name;
                if (attrName == "BitFieldAttribute")
                {
                    // Check for missing 'partial' keyword before processing
                    if (!GeneratorUtils.IsPartialProperty(member))
                    {
                        var propType = member.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                        nonPartialProperties.Add(new NonPartialPropertyInfo(member.Name, propType, "BitField", member.Locations.Length > 0 ? member.Locations[0] : null));
                        continue;
                    }

                    // Resolve [BitField] parameters across all constructor forms (SD0015�SD0019)
                    var memberLocation = member.Locations.Length > 0 ? member.Locations[0] : null;
                    var (resolved, fieldDiags) = GeneratorUtils.ResolveBitFieldAttribute(
                        memberAttr, member.Name, structSymbol.Name, memberLocation);
                    propertyDiagnostics.AddRange(fieldDiags);
                    if (resolved == null) continue;

                    var start = resolved.Value.Start;
                    var end = resolved.Value.End;

                    // Check if the property type is itself a [BitFields] record struct (sub-view)
                    bool isSubView = member.Type.GetAttributes()
                        .Any(a => a.AttributeClass?.Name is "BitFieldsAttribute")
                        && member.Type is INamedTypeSymbol propTypeSymbol
                        && propTypeSymbol.IsRecord;

                    if (isSubView)
                    {
                        var propType = member.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        subViews.Add(new SubViewInfo(member.Name, propType, start, end));
                    }
                    else
                    {
                        var width = resolved.Value.Width;
                        var qualifiedName = member.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                        // SD0020: Floating-point / opaque property type width mismatch
                        var fpDiag = GeneratorUtils.ValidateFloatPropertyWidth(
                            qualifiedName, width, member.Name, structSymbol.Name,
                            member.Locations.Length > 0 ? member.Locations[0] : null);
                        if (fpDiag != null)
                        {
                            propertyDiagnostics.Add(fpDiag);
                            continue;
                        }

                        var (nativeType, fieldByteOrder) = ResolveEndianType(qualifiedName);

                        // If no endian-type override, check if the property type is a [BitFields]
                        // struct � use its ByteOrder as a per-field override and its
                        // StorageType to resolve the native type for cast logic.
                        if (fieldByteOrder == null)
                        {
                            var bitFieldsAttr = member.Type.GetAttributes()
                                .FirstOrDefault(a => a.AttributeClass?.Name == "BitFieldsAttribute");
                            if (bitFieldsAttr != null)
                            {
                                if (bitFieldsAttr.ConstructorArguments.Length >= 4 &&
                                    bitFieldsAttr.ConstructorArguments[3].Value is int bfByteOrder)
                                {
                                    // ByteOrder enum: BigEndian = 0, LittleEndian = 1
                                    fieldByteOrder = bfByteOrder == 0
                                        ? ByteOrder.BigEndian
                                        : ByteOrder.LittleEndian;
                                }

                                // Resolve the underlying native type from the [BitFields] StorageType
                                // so that cast logic can convert through the native type.
                                var resolvedNative = ResolveBitFieldsNativeType(bitFieldsAttr);
                                if (resolvedNative != null)
                                {
                                    nativeType = resolvedNative;

                                    // Validate width for float-backed BitFields structs (SD0020)
                                    int reqWidth = GeneratorUtils.GetRequiredFloatBitWidth(resolvedNative);
                                    if (reqWidth >= 0 && width != reqWidth)
                                    {
                                        var loc = member.Locations.Length > 0 ? member.Locations[0] : null;
                                        propertyDiagnostics.Add(new PropertyDiagnosticInfo(
                                            BitFieldsDiagnostics.FloatPropertyWidthMismatch,
                                            loc, member.Name, structSymbol.Name, qualifiedName, reqWidth, width));
                                        continue;
                                    }
                                }

                                // SD0021: Validate width for embedded [BitFields] structs.
                                // For [BitFields(N)] N<=64 where ResolveBitFieldsNativeType returned null,
                                // also resolve the native type for cast logic.
                                var embeddedInfo = GeneratorUtils.ResolveEmbeddedBitFieldsInfo(bitFieldsAttr);
                                if (embeddedInfo != null)
                                {
                                    if (resolvedNative == null)
                                        nativeType = embeddedInfo.Value.NativeType;

                                    // Skip width validation for float-backed types (already handled as SD0020)
                                    // Only enforce exact width match for [BitFields(N)] types, not typeof(T) partial-width
                                    if (embeddedInfo.Value.IsExactWidth)
                                    {
                                        int reqFloat = GeneratorUtils.GetRequiredFloatBitWidth(nativeType ?? qualifiedName);
                                        if (reqFloat < 0)
                                        {
                                            var widthDiag = GeneratorUtils.ValidateEmbeddedStructWidth(
                                                qualifiedName, width, embeddedInfo.Value.BitWidth,
                                                member.Name, structSymbol.Name,
                                                member.Locations.Length > 0 ? member.Locations[0] : null);
                                            if (widthDiag != null)
                                            {
                                                propertyDiagnostics.Add(widthDiag);
                                                continue;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // Check for multi-word embedded type (UInt128, Int128, decimal, [BitFields(N)] N>64)
                                    var multiWordInfo = GeneratorUtils.ResolveMultiWordEmbeddedInfo(bitFieldsAttr);
                                    if (multiWordInfo != null)
                                    {
                                        // SD0021: Width validation for exact-width multi-word types
                                        if (multiWordInfo.Value.IsExactWidth)
                                        {
                                            var widthDiag = GeneratorUtils.ValidateEmbeddedStructWidth(
                                                qualifiedName, width, multiWordInfo.Value.BitWidth,
                                                member.Name, structSymbol.Name,
                                                member.Locations.Length > 0 ? member.Locations[0] : null);
                                            if (widthDiag != null)
                                            {
                                                propertyDiagnostics.Add(widthDiag);
                                                continue;
                                            }
                                        }

                                        var (desc2, descResType2) = ReadDescriptionArgs(memberAttr);
                                        fields.Add(new BitFieldInfo(member.Name, qualifiedName, start, width, fieldByteOrder: fieldByteOrder, nativeType: nativeType, description: desc2, descriptionResourceType: descResType2, isSpanBacked: true, structSizeBytes: multiWordInfo.Value.SizeInBytes));
                                        continue;
                                    }
                                }
                            }
                        }

                        var (desc, descResType) = ReadDescriptionArgs(memberAttr);

                        fields.Add(new BitFieldInfo(member.Name, qualifiedName, start, width, fieldByteOrder: fieldByteOrder, nativeType: nativeType, description: desc, descriptionResourceType: descResType, saturating: resolved.Value.Saturating));
                    }
                }
                else if (attrName == "BitFlagAttribute" && memberAttr.ConstructorArguments.Length >= 1)
                {
                    // Check for missing 'partial' keyword before processing
                    if (!GeneratorUtils.IsPartialProperty(member))
                    {
                        nonPartialProperties.Add(new NonPartialPropertyInfo(member.Name, "bool", "BitFlag", member.Locations.Length > 0 ? member.Locations[0] : null));
                        continue;
                    }

                    var bit = (int)(memberAttr.ConstructorArguments[0].Value ?? 0);
                    var (desc, descResType) = ReadDescriptionArgs(memberAttr);
                    flags.Add(new BitFlagInfo(member.Name, bit, description: desc, descriptionResourceType: descResType));
                }
            }
        }

        if (fields.Count == 0 && flags.Count == 0 && subViews.Count == 0 && nonPartialProperties.Count == 0 && propertyDiagnostics.Count == 0)
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
            int end = f.Shift + f.Width - 1;
            if (end > maxBit) maxBit = end;
        }
        foreach (var f in flags)
        {
            if (f.Bit > maxBit) maxBit = f.Bit;
        }
        foreach (var sv in subViews)
        {
            if (sv.End > maxBit) maxBit = sv.End;
        }
        int minBytes = (maxBit / 8) + 1;

        string accessibility = GetAccessibility(structSymbol);

        return new RecordStructViewInfo(
            structSymbol.Name, ns, accessibility,
            byteOrder, bitOrder,
            fields, flags, subViews, containingTypes, minBytes, structDescription,
            nonPartialProperties: nonPartialProperties,
            propertyDiagnostics: propertyDiagnostics);
    }

    /// <summary>
    /// Reads the optional Description and DescriptionResourceType named arguments
    /// from a [BitField] or [BitFlag] attribute.
    /// </summary>
    private static (string? description, string? descriptionResourceType) ReadDescriptionArgs(AttributeData attr)
    {
        string? desc = null;
        string? descResType = null;

        foreach (var named in attr.NamedArguments)
        {
            if (named.Key == "Description" && named.Value.Value is string d)
                desc = d;
            else if (named.Key == "DescriptionResourceType" && named.Value.Value is INamedTypeSymbol resType)
                descResType = resType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        return (desc, descResType);
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

    private static void Execute(SourceProductionContext context, RecordStructViewInfo info)
    {
        // Report property-level diagnostics (SD0015�SD0019)
        foreach (var pd in info.PropertyDiagnostics)
        {
            context.ReportDiagnostic(Diagnostic.Create(pd.Descriptor, pd.Location, pd.MessageArgs));
        }

        // Report diagnostics for non-partial properties (SD0004)
        foreach (var np in info.NonPartialProperties)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                BitFieldsDiagnostics.PropertyMustBePartial,
                np.Location,
                np.PropertyName,
                info.TypeName,
                np.AttributeName,
                np.PropertyType));
        }

        // If all properties were non-partial, skip code generation entirely
        if (info.Fields.Count == 0 && info.Flags.Count == 0 && info.SubViews.Count == 0)
            return;

        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// This code was generated by Stardust.Generators source generator.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Buffers.Binary;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine("using Stardust.Utilities;");
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

        sb.AppendLine($"{ind}[JsonConverter(typeof({t}JsonConverter))]");
        sb.AppendLine($"{ind}{info.Accessibility} partial record struct {t}");
        sb.AppendLine($"{ind}{{");

        // Backing field
        sb.AppendLine($"{mind}private readonly Memory<byte> _data;");
        sb.AppendLine($"{mind}private readonly byte _bitOffset;");
        sb.AppendLine();

        // SIZE_IN_BYTES constant (accounts for read widths, not just field byte spans)
        (int bitWidth, int computedMinBytes) = ComputeMinBytes(info);
        sb.AppendLine($"{mind}/// <summary>Minimum number of bytes required in the backing buffer.</summary>");
        sb.AppendLine($"{mind}public const int SIZE_IN_BYTES = {computedMinBytes};");
        sb.AppendLine($"{mind}public const int BIT_WIDTH = {bitWidth};");
        sb.AppendLine();

        // Constructors
        sb.AppendLine($"{mind}/// <summary>Creates a view over the specified memory buffer.</summary>");
        sb.AppendLine($"{mind}/// <param name=\"data\">The buffer to view. Must contain at least <see cref=\"SIZE_IN_BYTES\"/> bytes.</param>");
        sb.AppendLine($"{mind}/// <exception cref=\"ArgumentException\">The buffer is too short.</exception>");
        sb.AppendLine($"{mind}public {t}(Memory<byte> data)");
        sb.AppendLine($"{mind}{{");
        sb.AppendLine($"{mind}    if (data.Length < SIZE_IN_BYTES)");
        sb.AppendLine($"{mind}        throw new ArgumentException($\"Buffer must contain at least {{SIZE_IN_BYTES}} bytes, but was {{data.Length}}.\", nameof(data));");
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

        // Generate field metadata
        GenerateFieldMetadata(sb, info, mind);

        // Generate JSON converter
        GenerateJsonConverter(sb, info, mind);

        // Emit opaque bit-reinterpretation helpers for decimal property types
        if (info.Fields.Any(f => f.NativeType == "decimal"))
            EmitDecimalHelpers(sb, mind);

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
    private static (int bitWidth, int minBytes) ComputeMinBytes(RecordStructViewInfo info)
    {
        int minBytes = 0;
        int minStartBit = int.MaxValue;
        int maxEndBit = int.MinValue;
        foreach (var f in info.Fields)
        {
            int start = f.Shift;
            int end = start + f.Width - 1;
            minStartBit = Math.Min(minStartBit, start);
            maxEndBit = Math.Max(maxEndBit, end);

            int firstByte = start / 8;
            int needed;
            if (f.IsSpanBacked)
            {
                // Span-backed multi-word field: exact byte range
                needed = firstByte + f.StructSizeBytes;
            }
            else
            {
                int lastByte = end / 8;
                int byteSpan = lastByte - firstByte + 1;
                int readWidth = byteSpan <= 1 ? 1 : byteSpan <= 2 ? 2 : byteSpan <= 4 ? 4 : byteSpan <= 8 ? 8 : 16;
                needed = firstByte + readWidth;
            }
            if (needed > minBytes) minBytes = needed;
        }
        foreach (var f in info.Flags)
        {
            int byteIdx = f.Bit / 8;
            if (byteIdx + 1 > minBytes) minBytes = byteIdx + 1;
            minStartBit = Math.Min(minStartBit, f.Bit);
            maxEndBit = Math.Max(maxEndBit, f.Bit);
        }
        foreach (var sv in info.SubViews)
        {
            // Sub-view extends to at least (end + 1) / 8 bytes, rounded up
            int needed = (sv.End / 8) + 1;
            if (needed > minBytes) minBytes = needed;
            minStartBit = Math.Min(minStartBit, sv.Start);
            maxEndBit = Math.Max(maxEndBit, sv.End);
        }
        int bitWidth = maxEndBit - minStartBit + 1;
        return (bitWidth, minBytes);
    }

    /// <summary>
    /// Generates a property accessor for a multi-bit field.
    /// Emits a fast path when _bitOffset == 0 (compile-time constants) and a
    /// general path with runtime bit offset for nested sub-view scenarios.
    /// </summary>
    private static void GenerateFieldProperty(StringBuilder sb, RecordStructViewInfo info, BitFieldInfo field, string ind)
    {
        // Span-backed embedded multi-word struct: use ReadFrom/WriteTo on the byte buffer
        if (field.IsSpanBacked)
        {
            GenerateSpanBackedViewProperty(sb, field, ind);
            return;
        }

        int start = field.Shift;
        int width = field.Width;
        int end = start + width - 1;
        ulong mask = (width >= 64) ? ulong.MaxValue : (1UL << width) - 1;

        // === Compute values for the fast path (_bitOffset == 0) ===
        int firstByte = start / 8;
        int lastByte = end / 8;
        int byteSpan = lastByte - firstByte + 1;
        int readWidth = byteSpan <= 1 ? 1 : byteSpan <= 2 ? 2 : byteSpan <= 4 ? 4 : byteSpan <= 8 ? 8 : 16;
        int readBits = readWidth * 8;

        int rightShift;
        if (info.BitOrder == BitOrder.BitZeroIsMsb)
        {
            int fieldEndInWindow = end - firstByte * 8;
            rightShift = readBits - 1 - fieldEndInWindow;
        }
        else
        {
            rightShift = start % 8;
        }

        string readType, readMethod, writeMethod;
        GetReadWriteMethods(info, readWidth, out readType, out readMethod, out writeMethod, field.FieldByteOrder);

        string maskLiteral = FormatMask(mask, readType);

        // === Compute values for the offset-aware path ===
        // Worst-case byte span with any bit offset 0-7
        int maxSpan = (width + 6) / 8 + 1;
        if (width <= 1) maxSpan = 1; // single bit never spans two bytes
        int oReadWidth = maxSpan <= 1 ? 1 : maxSpan <= 2 ? 2 : maxSpan <= 4 ? 4 : maxSpan <= 8 ? 8 : 16;
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
        EmitOffsetGetter(sb, info, field, ind + "        ", start, width, oReadWidth, oReadType, oReadMethod, oMaskLiteral);
        sb.AppendLine($"{ind}    }}");

        // ---- Setter ----
        sb.AppendLine($"{ind}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}    set");
        sb.AppendLine($"{ind}    {{");

        // Saturation clamp: emitted as const locals at the top of the setter body so the
        // existing fast/offset paths pick up the clamped value via the 'value' parameter.
        bool isViewSat = field.Saturating
            && !IsFloatingPointPropertyType(field.PropertyType)
            && field.NativeType == field.PropertyType  // not an endian-aware / embedded type
            && GeneratorUtils.IsSaturatablePropertyType(field.PropertyType)
            && width < GeneratorUtils.GetTypeBitWidth(field.PropertyType);
        if (isViewSat)
        {
            bool isSigned = GeneratorUtils.IsSignedType(field.PropertyType);
            string satConstType = GeneratorUtils.GetSatConstType(field.PropertyType);
            if (isSigned)
            {
                long satMinVal = -(1L << (width - 1));
                long satMaxVal = (1L << (width - 1)) - 1;
                sb.AppendLine($"{ind}        const {satConstType} SAT_MIN = {GeneratorUtils.FormatSignedSatLiteral(satMinVal, satConstType)};  // saturating: min for {width}-bit signed field");
                sb.AppendLine($"{ind}        const {satConstType} SAT_MAX = {GeneratorUtils.FormatSignedSatLiteral(satMaxVal, satConstType)};  // saturating: max for {width}-bit signed field");
                string clampExpr = GeneratorUtils.BuildSatClampExpr(field.PropertyType, true, "SAT_MIN", "SAT_MAX");
                sb.AppendLine($"{ind}        value = ({field.PropertyType})({clampExpr});");
            }
            else
            {
                ulong satMaxVal = (1UL << width) - 1;
                sb.AppendLine($"{ind}        const {satConstType} SAT_MAX = {GeneratorUtils.FormatUnsignedSatLiteral(satMaxVal, satConstType)};  // saturating: max for {width}-bit unsigned field");
                string clampExpr = GeneratorUtils.BuildSatClampExpr(field.PropertyType, false, null!, "SAT_MAX");
                sb.AppendLine($"{ind}        value = ({field.PropertyType})({clampExpr});");
            }
        }

        sb.AppendLine($"{ind}        var s = _data.Span;");
        sb.AppendLine($"{ind}        if (_bitOffset == 0)");
        sb.AppendLine($"{ind}        {{");
        EmitFastSetter(sb, info, field, ind + "            ", firstByte, rightShift, readWidth, readType, readMethod, writeMethod, mask, maskLiteral, width, byteSpan);
        sb.AppendLine($"{ind}        }}");
        sb.AppendLine($"{ind}        else");
        sb.AppendLine($"{ind}        {{");
        EmitOffsetSetter(sb, info, field, ind + "            ", start, width, oReadWidth, oReadType, oReadMethod, oWriteMethod, mask, oMaskLiteral);
        sb.AppendLine($"{ind}        }}");
        sb.AppendLine($"{ind}    }}");

        sb.AppendLine($"{ind}}}");
        sb.AppendLine();
    }

    private static void EmitFastGetter(StringBuilder sb, RecordStructViewInfo info, BitFieldInfo field,
        string ind, int firstByte, int rightShift, int readWidth, string readType,
        string readMethod, string maskLiteral, int width, int byteSpan)
    {
        string cast = GetterCast(field);
        string gSuffix = GetterSuffix(field);
        if (byteSpan == 1)
        {
            if (rightShift == 0 && width == 8)
                sb.AppendLine($"{ind}return {cast}s[{firstByte}]{gSuffix};");
            else if (rightShift == 0)
                sb.AppendLine($"{ind}return {cast}(s[{firstByte}] & {maskLiteral}){gSuffix};");
            else
                sb.AppendLine($"{ind}return {cast}((s[{firstByte}] >> {rightShift}) & {maskLiteral}){gSuffix};");
        }
        else
        {
            int readBits = readWidth * 8;
            if (rightShift == 0 && width == readBits)
                sb.AppendLine($"{ind}return {cast}{readMethod}(s.Slice({firstByte})){gSuffix};");
            else if (rightShift == 0)
                sb.AppendLine($"{ind}return {cast}({readMethod}(s.Slice({firstByte})) & {maskLiteral}){gSuffix};");
            else
                sb.AppendLine($"{ind}return {cast}(({readMethod}(s.Slice({firstByte})) >> {rightShift}) & {maskLiteral}){gSuffix};");
        }
    }

    private static void EmitOffsetGetter(StringBuilder sb, RecordStructViewInfo info, BitFieldInfo field,
        string ind, int start, int width, int oReadWidth, string oReadType,
        string oReadMethod, string oMaskLiteral)
    {
        bool isMsb = info.BitOrder == BitOrder.BitZeroIsMsb;
        int oReadBits = oReadWidth * 8;
        string cast = GetterCast(field);
        string gSuffix = GetterSuffix(field);

        sb.AppendLine($"{ind}int ep = {start} + _bitOffset;");
        sb.AppendLine($"{ind}int bi = ep >> 3;");

        if (oReadWidth == 1)
        {
            if (isMsb)
            {
                sb.AppendLine($"{ind}int sh = 7 - (ep & 7);");
                sb.AppendLine($"{ind}return {cast}((s[bi] >> sh) & {oMaskLiteral}){gSuffix};");
            }
            else
            {
                sb.AppendLine($"{ind}int sh = ep & 7;");
                sb.AppendLine($"{ind}return {cast}((s[bi] >> sh) & {oMaskLiteral}){gSuffix};");
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
            sb.AppendLine($"{ind}return {cast}(({oReadMethod}(s.Slice(bi)) >> sh) & {oMaskLiteral}){gSuffix};");
        }
    }

    private static void EmitFastSetter(StringBuilder sb, RecordStructViewInfo info, BitFieldInfo field,
        string ind, int firstByte, int rightShift, int readWidth, string readType,
        string readMethod, string writeMethod, ulong mask, string maskLiteral, int width, int byteSpan)
    {
        string sCast = SetterCast(field, "byte");
        string vSuffix = SetterValueSuffix(field);
        if (byteSpan == 1)
        {
            ulong shiftedMask = mask << rightShift;
            string invMask = $"0x{(byte)~shiftedMask & 0xFF:X2}";
            string smask = $"0x{(byte)shiftedMask:X2}";
            if (rightShift == 0 && width == 8)
                sb.AppendLine($"{ind}s[{firstByte}] = {sCast}value{vSuffix};");
            else if (rightShift == 0)
                sb.AppendLine($"{ind}s[{firstByte}] = (byte)((s[{firstByte}] & {invMask}) | ({sCast}value{vSuffix} & {smask}));");
            else
                sb.AppendLine($"{ind}s[{firstByte}] = (byte)((s[{firstByte}] & {invMask}) | (({sCast}value{vSuffix} << {rightShift}) & {smask}));");
        }
        else
        {
            int readBits = readWidth * 8;
            string sCastR = SetterCast(field, readType);
            if (rightShift == 0 && width == readBits)
            {
                // Full-width write: no masking needed
                sb.AppendLine($"{ind}var slice = s.Slice({firstByte});");
                sb.AppendLine($"{ind}{writeMethod}(slice, {sCastR}value{vSuffix});");
            }
            else
            {
                string invMaskLit, smaskLit;
                if (readType == "UInt128")
                {
                    // For UInt128, the shifted mask can exceed 64 bits, so emit
                    // the shift as a runtime expression instead of a precomputed literal.
                    FormatShiftedMasks128(maskLiteral, rightShift, out invMaskLit, out smaskLit);
                }
                else
                {
                    ulong shiftedMask = mask << rightShift;
                    FormatShiftedMasks(readType, shiftedMask, out invMaskLit, out smaskLit);
                }
                sb.AppendLine($"{ind}var slice = s.Slice({firstByte});");
                sb.AppendLine($"{ind}{readType} raw = {readMethod}(slice);");
                if (rightShift == 0)
                    sb.AppendLine($"{ind}raw = ({readType})((raw & {invMaskLit}) | ({sCastR}value{vSuffix} & {smaskLit}));");
                else
                    sb.AppendLine($"{ind}raw = ({readType})((raw & {invMaskLit}) | (({sCastR}value{vSuffix} << {rightShift}) & {smaskLit}));");
                sb.AppendLine($"{ind}{writeMethod}(slice, raw);");
            }
        }
    }

    private static void EmitOffsetSetter(StringBuilder sb, RecordStructViewInfo info, BitFieldInfo field,
        string ind, int start, int width, int oReadWidth, string oReadType,
        string oReadMethod, string oWriteMethod, ulong mask, string oMaskLiteral)
    {
        bool isMsb = info.BitOrder == BitOrder.BitZeroIsMsb;
        int oReadBits = oReadWidth * 8;
        string vSuffix = SetterValueSuffix(field);

        sb.AppendLine($"{ind}int ep = {start} + _bitOffset;");
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
            sb.AppendLine($"{ind}s[bi] = (byte)((s[bi] & ~m) | (({sCast}value{vSuffix} << sh) & m));");
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
            sb.AppendLine($"{ind}raw = ({oReadType})((raw & ({oReadType})~m) | (({sCast}value{vSuffix} << sh) & m));");
            sb.AppendLine($"{ind}{oWriteMethod}(slice, raw);");
        }
    }

    /// <summary>
    /// Generates a property accessor for a span-backed embedded multi-word struct
    /// inside a record struct view. Uses <c>ReadFrom</c>/<c>WriteTo</c> on the
    /// underlying <c>Memory&lt;byte&gt;</c> buffer.
    /// When the field starts at a byte-aligned position the span is a simple slice;
    /// otherwise byte-level bit-shifting is emitted to support arbitrary bit offsets.
    /// </summary>
    private static void GenerateSpanBackedViewProperty(StringBuilder sb, BitFieldInfo field, string ind)
    {
        int startByte = field.Shift / 8;
        int bitOffset = field.Shift % 8;
        int sizeBytes = field.StructSizeBytes;
        bool aligned = bitOffset == 0;

        sb.AppendLine($"{ind}public partial {field.PropertyType} {field.Name}");
        sb.AppendLine($"{ind}{{");

        if (aligned)
        {
            sb.AppendLine($"{ind}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{ind}    get => {field.PropertyType}.ReadFrom(_data.Span.Slice({startByte}, {sizeBytes}));");
            sb.AppendLine($"{ind}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{ind}    set => value.WriteTo(_data.Span.Slice({startByte}, {sizeBytes}));");
        }
        else
        {
            int lowMask = (1 << bitOffset) - 1;
            int hiMask = ~lowMask & 0xFF;

            // Getter: extract bits with byte-level shifting
            sb.AppendLine($"{ind}    get");
            sb.AppendLine($"{ind}    {{");
            sb.AppendLine($"{ind}        ReadOnlySpan<byte> __src = _data.Span;");
            sb.AppendLine($"{ind}        Span<byte> __ebuf = stackalloc byte[{sizeBytes}];");
            sb.AppendLine($"{ind}        for (int __i = 0; __i < {sizeBytes}; __i++)");
            sb.AppendLine($"{ind}            __ebuf[__i] = (byte)((__src[{startByte} + __i] >> {bitOffset}) | (({startByte} + __i + 1 < __src.Length) ? (__src[{startByte} + __i + 1] << {8 - bitOffset}) : 0));");
            sb.AppendLine($"{ind}        return {field.PropertyType}.ReadFrom(__ebuf);");
            sb.AppendLine($"{ind}    }}");

            // Setter: insert bits with byte-level shifting
            sb.AppendLine($"{ind}    set");
            sb.AppendLine($"{ind}    {{");
            sb.AppendLine($"{ind}        Span<byte> __dst = _data.Span;");
            sb.AppendLine($"{ind}        Span<byte> __ebuf = stackalloc byte[{sizeBytes}];");
            sb.AppendLine($"{ind}        value.WriteTo(__ebuf);");
            sb.AppendLine($"{ind}        for (int __i = 0; __i < {sizeBytes}; __i++)");
            sb.AppendLine($"{ind}        {{");
            sb.AppendLine($"{ind}            __dst[{startByte} + __i] = (byte)((__dst[{startByte} + __i] & 0x{lowMask:X2}) | (__ebuf[__i] << {bitOffset}));");
            sb.AppendLine($"{ind}            if ({startByte} + __i + 1 < __dst.Length)");
            sb.AppendLine($"{ind}                __dst[{startByte} + __i + 1] = (byte)((__dst[{startByte} + __i + 1] & 0x{hiMask:X2}) | (__ebuf[__i] >> {8 - bitOffset}));");
            sb.AppendLine($"{ind}        }}");
            sb.AppendLine($"{ind}    }}");
        }

        sb.AppendLine($"{ind}}}");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates a property accessor for a single-bit boolean flag.
    /// Fast path for _bitOffset == 0, offset-aware path for nested views.
    /// </summary>
    private static void GenerateFlagProperty(StringBuilder sb, RecordStructViewInfo info, BitFlagInfo flag, string ind)
    {
        int bitPos = flag.Bit;
        bool isMsb = info.BitOrder == BitOrder.BitZeroIsMsb;

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
    /// Generates a property accessor for a nested record struct sub-view.
    /// The getter constructs a new view over the same memory at the appropriate byte/bit offset.
    /// The setter copies bytes from the source view into the buffer.
    /// </summary>
    private static void GenerateSubViewProperty(StringBuilder sb, RecordStructViewInfo info, SubViewInfo sv, string ind)
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
            sb.AppendLine($"{ind}    set {{ value.Data.Span.Slice(0, {sv.ViewTypeName}.SIZE_IN_BYTES).CopyTo(_data.Span.Slice({byteOff})); }}");
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
            sb.AppendLine($"{ind}        src.Slice(0, {sv.ViewTypeName}.SIZE_IN_BYTES).CopyTo(_data.Span.Slice({byteOff}));");
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
    /// For floating-point property types (Half, float, double), wraps with BitConverter.*BitsTo*
    /// so the raw unsigned bits are reinterpreted as the float type.
    /// </summary>
    private static string GetterCast(BitFieldInfo field)
    {
        // Floating-point property types need BitConverter wrapping instead of a C-style cast.
        if (IsFloatingPointPropertyType(field.PropertyType))
        {
            string fromBits = FloatPropertyFromBits(field.PropertyType);
            string unsignedType = FloatPropertyUnsignedType(field.PropertyType);
            return $"{fromBits}(({unsignedType})";
        }
        // BitFields struct wrapping a float type (e.g., IEEE754Double wraps double):
        // reinterpret raw bits via BitConverter, then rely on the struct's implicit conversion.
        if (IsFloatingPointPropertyType(field.NativeType) && field.PropertyType != field.NativeType)
        {
            string fromBits = FloatPropertyFromBits(field.NativeType);
            string unsignedType = FloatPropertyUnsignedType(field.NativeType);
            return $"({field.PropertyType}){fromBits}(({unsignedType})";
        }
        if (field.PropertyType != field.NativeType)
            return $"({field.PropertyType})({field.NativeType})";
        return $"({field.PropertyType})";
    }

    /// <summary>
    /// Produces the cast for converting a property value to the unsigned read type for setter logic.
    /// For endian-aware types, chains through the native type: (uint)(int)value.
    /// For plain types, casts directly: (uint)value.
    /// For floating-point property types, converts via BitConverter first:
    /// (uint)BitConverter.SingleToUInt32Bits(value) instead of (uint)value.
    /// </summary>
    private static string SetterCast(BitFieldInfo field, string readType)
    {
        // Floating-point property types: convert to raw bits, then cast to the read type.
        if (IsFloatingPointPropertyType(field.PropertyType))
        {
            string toBits = FloatPropertyToBits(field.PropertyType);
            return $"({readType}){toBits}(";
        }
        // BitFields struct wrapping a float type: convert to native float via implicit conversion,
        // then to raw bits via BitConverter.
        if (IsFloatingPointPropertyType(field.NativeType) && field.PropertyType != field.NativeType)
        {
            string toBits = FloatPropertyToBits(field.NativeType);
            return $"({readType}){toBits}(({field.NativeType})";
        }
        if (field.PropertyType != field.NativeType)
            return $"({readType})({field.NativeType})";
        return $"({readType})";
    }

    /// <summary>
    /// Returns the suffix to append after <c>value</c> in setter expressions.
    /// For floating-point property types the <see cref="SetterCast"/> opens a
    /// <c>BitConverter.*To*Bits(</c> call that needs a closing <c>)</c>.
    /// For all other types, returns an empty string.
    /// </summary>
    private static string SetterValueSuffix(BitFieldInfo field)
    {
        return IsFloatingPointPropertyType(field.PropertyType)
            || (IsFloatingPointPropertyType(field.NativeType) && field.PropertyType != field.NativeType)
            ? ")" : "";
    }

    /// <summary>
    /// Returns the suffix to append after the getter expression.
    /// For floating-point property types the <see cref="GetterCast"/> opens a
    /// <c>BitConverter.*BitsTo*(</c> call that needs a closing <c>)</c>.
    /// For all other types, returns an empty string.
    /// </summary>
    private static string GetterSuffix(BitFieldInfo field)
    {
        return IsFloatingPointPropertyType(field.PropertyType)
            || (IsFloatingPointPropertyType(field.NativeType) && field.PropertyType != field.NativeType)
            ? ")" : "";
    }

    /// <summary>
    /// Determines if a property type name represents a floating-point type.
    /// </summary>
    private static bool IsFloatingPointPropertyType(string typeName)
    {
        return typeName is "float" or "double" or "decimal"
            or "Half" or "global::System.Half" or "System.Half";
    }

    private static string FloatPropertyUnsignedType(string typeName) => typeName switch
    {
        "Half" or "global::System.Half" or "System.Half" => "ushort",
        "float" => "uint",
        "double" => "ulong",
        "decimal" => "UInt128",
        _ => throw new System.ArgumentException($"Not a floating-point property type: {typeName}")
    };

    private static string FloatPropertyFromBits(string typeName) => typeName switch
    {
        "Half" or "global::System.Half" or "System.Half" => "BitConverter.UInt16BitsToHalf",
        "float" => "BitConverter.UInt32BitsToSingle",
        "double" => "BitConverter.UInt64BitsToDouble",
        "decimal" => "UInt128BitsToDecimal",
        _ => throw new System.ArgumentException($"Not a floating-point property type: {typeName}")
    };

    private static string FloatPropertyToBits(string typeName) => typeName switch
    {
        "Half" or "global::System.Half" or "System.Half" => "BitConverter.HalfToUInt16Bits",
        "float" => "BitConverter.SingleToUInt32Bits",
        "double" => "BitConverter.DoubleToUInt64Bits",
        "decimal" => "DecimalToUInt128Bits",
        _ => throw new System.ArgumentException($"Not a floating-point property type: {typeName}")
    };

    /// <summary>
    /// Emits private static helper methods for opaque decimal ? UInt128 bit reinterpretation.
    /// Uses <c>Unsafe.As</c> which is a zero-cost JIT intrinsic on .NET 7+.
    /// </summary>
    private static void EmitDecimalHelpers(StringBuilder sb, string ind)
    {
        sb.AppendLine();
        sb.AppendLine($"{ind}// ?? Opaque decimal ? UInt128 bit reinterpretation ??");
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}private static decimal UInt128BitsToDecimal(UInt128 bits) => Unsafe.As<UInt128, decimal>(ref bits);");
        sb.AppendLine($"{ind}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}private static UInt128 DecimalToUInt128Bits(decimal value) => Unsafe.As<decimal, UInt128>(ref value);");
    }

    /// <summary>
    /// Maps endian-aware property types (UInt16Be, Int32Be, UInt64Le, etc.) to
    /// the underlying native type and the byte order they imply.
    /// Returns the original type unchanged if it is not an endian-aware type.
    /// </summary>
    private static (string resolvedType, ByteOrder? byteOrder) ResolveEndianType(string qualifiedType)
    {
        return qualifiedType switch
        {
            "global::Stardust.Utilities.UInt16Be" => ("ushort", ByteOrder.BigEndian),
            "global::Stardust.Utilities.Int16Be"  => ("short",  ByteOrder.BigEndian),
            "global::Stardust.Utilities.UInt32Be" => ("uint",   ByteOrder.BigEndian),
            "global::Stardust.Utilities.Int32Be"  => ("int",    ByteOrder.BigEndian),
            "global::Stardust.Utilities.UInt64Be" => ("ulong",  ByteOrder.BigEndian),
            "global::Stardust.Utilities.Int64Be"  => ("long",   ByteOrder.BigEndian),
            "global::Stardust.Utilities.UInt16Le" => ("ushort", ByteOrder.LittleEndian),
            "global::Stardust.Utilities.Int16Le"  => ("short",  ByteOrder.LittleEndian),
            "global::Stardust.Utilities.UInt32Le" => ("uint",   ByteOrder.LittleEndian),
            "global::Stardust.Utilities.Int32Le"  => ("int",    ByteOrder.LittleEndian),
            "global::Stardust.Utilities.UInt64Le" => ("ulong",  ByteOrder.LittleEndian),
            "global::Stardust.Utilities.Int64Le"  => ("long",   ByteOrder.LittleEndian),
            _ => (qualifiedType, null)
        };
    }

    /// <summary>
    /// Extracts the native storage type name from a <c>[BitFields]</c> attribute.
    /// Returns <c>null</c> when the attribute does not declare a storage type
    /// (e.g., the record-struct view constructor or the bit-count constructor).
    /// </summary>
    private static string? ResolveBitFieldsNativeType(AttributeData bitFieldsAttr)
    {
        if (bitFieldsAttr.ConstructorArguments.Length == 0)
            return null;

        var firstArg = bitFieldsAttr.ConstructorArguments[0];

        // StorageType enum constructor: BitFieldsAttribute(StorageType, ...)
        if (firstArg.Kind == TypedConstantKind.Enum &&
            firstArg.Type?.Name == "StorageType" &&
            firstArg.Value is int storageTypeValue)
        {
            return StorageTypeIntToNativeName(storageTypeValue);
        }

        // Type constructor: BitFieldsAttribute(Type, ...)
        if (firstArg.Kind == TypedConstantKind.Type && firstArg.Value is ITypeSymbol typeSymbol)
        {
            return TypeSymbolToNativeName(typeSymbol);
        }

        return null;
    }

    /// <summary>
    /// Maps a <c>StorageType</c> enum value (its underlying int) to the C# type keyword.
    /// </summary>
    private static string? StorageTypeIntToNativeName(int value) => value switch
    {
        0  => "sbyte",                  // StorageType.SByte
        1  => "byte",                   // StorageType.Byte
        2  => "short",                  // StorageType.Int16
        3  => "ushort",                 // StorageType.UInt16
        4  => "int",                    // StorageType.Int32
        5  => "uint",                   // StorageType.UInt32
        6  => "long",                   // StorageType.Int64
        7  => "ulong",                  // StorageType.UInt64
        8  => "nint",                   // StorageType.NInt
        9  => "nuint",                  // StorageType.NUInt
        10 => "global::System.Half",    // StorageType.Half
        11 => "float",                  // StorageType.Single
        12 => "double",                 // StorageType.Double
        13 => "decimal",               // StorageType.Decimal
        14 => "Int128",                 // StorageType.Int128
        15 => "UInt128",                // StorageType.UInt128
        _  => null
    };

    /// <summary>
    /// Maps an <see cref="ITypeSymbol"/> (from a <c>typeof(T)</c> attribute argument)
    /// to the C# type keyword.
    /// </summary>
    private static string? TypeSymbolToNativeName(ITypeSymbol type) => type.SpecialType switch
    {
        SpecialType.System_SByte   => "sbyte",
        SpecialType.System_Byte    => "byte",
        SpecialType.System_Int16   => "short",
        SpecialType.System_UInt16  => "ushort",
        SpecialType.System_Int32   => "int",
        SpecialType.System_UInt32  => "uint",
        SpecialType.System_Int64   => "long",
        SpecialType.System_UInt64  => "ulong",
        SpecialType.System_Single  => "float",
        SpecialType.System_Double  => "double",
        SpecialType.System_Decimal => "decimal",
        SpecialType.System_IntPtr  => "nint",
        SpecialType.System_UIntPtr => "nuint",
        _ => type.Name switch
        {
            "Half"    => "global::System.Half",
            "Int128"  => "Int128",
            "UInt128" => "UInt128",
            _         => null
        }
    };

    /// <summary>
    /// Resolves the effective byte order for a field: per-field override wins,
    /// otherwise falls back to the struct-level default.
    /// </summary>
    private static bool IsEffectiveBigEndian(RecordStructViewInfo info, ByteOrder? fieldOverride)
    {
        if (fieldOverride.HasValue)
            return fieldOverride.Value == ByteOrder.BigEndian;
        return info.ByteOrder == ByteOrder.BigEndian;
    }

    private static void GetReadWriteMethods(RecordStructViewInfo info, int readWidth,
        out string readType, out string readMethod, out string writeMethod,
        ByteOrder? fieldOverride = null)
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
        else if (readWidth == 8)
        {
            readType = "ulong";
            readMethod = isBE
                ? "BinaryPrimitives.ReadUInt64BigEndian"
                : "BinaryPrimitives.ReadUInt64LittleEndian";
            writeMethod = isBE
                ? "BinaryPrimitives.WriteUInt64BigEndian"
                : "BinaryPrimitives.WriteUInt64LittleEndian";
        }
        else
        {
            readType = "UInt128";
            readMethod = isBE
                ? "BinaryPrimitives.ReadUInt128BigEndian"
                : "BinaryPrimitives.ReadUInt128LittleEndian";
            writeMethod = isBE
                ? "BinaryPrimitives.WriteUInt128BigEndian"
                : "BinaryPrimitives.WriteUInt128LittleEndian";
        }
    }

    private static string FormatMask(ulong mask, string readType)
    {
        return readType switch
        {
            "byte" => $"0x{(byte)mask:X2}",
            "ushort" => $"0x{(ushort)mask:X4}",
            "uint" => $"0x{(uint)mask:X}U",
            "UInt128" => $"(UInt128)0x{mask:X}UL",
            _ => $"0x{mask:X}UL"
        };
    }

    /// <summary>
    /// Generates a static <c>Fields</c> property that returns a
    /// <see cref="System.ReadOnlySpan{T}"/> of <c>BitFieldInfo</c> describing
    /// every field and flag declared on this view struct.
    /// </summary>
    private static void GenerateFieldMetadata(StringBuilder sb, RecordStructViewInfo info, string ind)
    {
        string structBitOrder = info.BitOrder == BitOrder.BitZeroIsMsb
            ? "BitOrder.BitZeroIsMsb" : "BitOrder.BitZeroIsLsb";
        (int _, int totalBytes) = ComputeMinBytes(info);
        int totalBits = totalBytes * 8;
        string structDescArg = info.Description != null
            ? $", StructDescription: \"{GeneratorUtils.EscapeStringLiteral(info.Description)}\""
            : "";

        sb.AppendLine($"{ind}/// <summary>Metadata for every field and flag declared on this view, in declaration order.</summary>");
        sb.AppendLine($"{ind}public static ReadOnlySpan<BitFieldInfo> Fields => new BitFieldInfo[]");
        sb.AppendLine($"{ind}{{");

        foreach (var f in info.Fields)
        {
            var qualifiedType = StripGlobalPrefix(f.PropertyType);
            string effectiveByteOrder = IsEffectiveBigEndian(info, f.FieldByteOrder)
                ? "ByteOrder.BigEndian" : "ByteOrder.LittleEndian";
            var descArgs = FormatDescriptionArgs(f.Description, f.DescriptionResourceType);
            sb.AppendLine($"{ind}    new(\"{f.Name}\", {f.Shift}, {f.Width}, \"{qualifiedType}\", false, {effectiveByteOrder}, {structBitOrder}{descArgs}, StructTotalBits: {totalBits}, FieldMustBe: {(int)f.ValueOverride}{structDescArg}),");
        }

        foreach (var f in info.Flags)
        {
            string structByteOrder = info.ByteOrder == ByteOrder.BigEndian
                ? "ByteOrder.BigEndian" : "ByteOrder.LittleEndian";
            var descArgs = FormatDescriptionArgs(f.Description, f.DescriptionResourceType);
            sb.AppendLine($"{ind}    new(\"{f.Name}\", {f.Bit}, 1, \"bool\", true, {structByteOrder}, {structBitOrder}{descArgs}, StructTotalBits: {totalBits}, FieldMustBe: {(int)f.ValueOverride}{structDescArg}),");
        }

        sb.AppendLine($"{ind}}};");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates a private nested <c>JsonConverter&lt;T&gt;</c> that serializes the view
    /// as a <c>"0x..."</c> hex string of the underlying bytes, matching the format
    /// used by <c>[BitFields]</c> types.
    /// </summary>
    private static void GenerateJsonConverter(StringBuilder sb, RecordStructViewInfo info, string ind)
    {
        string t = info.TypeName;

        sb.AppendLine($"{ind}private sealed class {t}JsonConverter : JsonConverter<{t}>");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}    /// <summary>Reads a {t} from a JSON hex string.</summary>");
        sb.AppendLine($"{ind}    public override {t} Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)");
        sb.AppendLine($"{ind}    {{");
        sb.AppendLine($"{ind}        var s = reader.GetString();");
        sb.AppendLine($"{ind}        if (s is null) return new {t}(new byte[SIZE_IN_BYTES]);");
        sb.AppendLine($"{ind}        ReadOnlySpan<char> hex = s.AsSpan();");
        sb.AppendLine($"{ind}        if (hex.Length >= 2 && hex[0] == '0' && (hex[1] == 'x' || hex[1] == 'X'))");
        sb.AppendLine($"{ind}            hex = hex.Slice(2);");
        sb.AppendLine($"{ind}        var bytes = new byte[SIZE_IN_BYTES];");
        sb.AppendLine($"{ind}        int hexLen = hex.Length;");
        sb.AppendLine($"{ind}        for (int i = 0; i < SIZE_IN_BYTES && (hexLen - i * 2) > 0; i++)");
        sb.AppendLine($"{ind}        {{");
        sb.AppendLine($"{ind}            int hi = hexLen - (i + 1) * 2;");
        sb.AppendLine($"{ind}            if (hi >= 0)");
        sb.AppendLine($"{ind}                bytes[i] = (byte)((HexVal(hex[hi]) << 4) | HexVal(hex[hi + 1]));");
        sb.AppendLine($"{ind}            else if (hi == -1)");
        sb.AppendLine($"{ind}                bytes[i] = (byte)HexVal(hex[0]);");
        sb.AppendLine($"{ind}        }}");
        sb.AppendLine($"{ind}        return new {t}(bytes);");
        sb.AppendLine($"{ind}    }}");
        sb.AppendLine();
        sb.AppendLine($"{ind}    /// <summary>Writes a {t} to JSON as a hex string.</summary>");
        sb.AppendLine($"{ind}    public override void Write(Utf8JsonWriter writer, {t} value, JsonSerializerOptions options)");
        sb.AppendLine($"{ind}    {{");
        sb.AppendLine($"{ind}        var s = value._data.Span;");
        sb.AppendLine($"{ind}        // Find highest non-zero byte for minimal hex output");
        sb.AppendLine($"{ind}        int top = SIZE_IN_BYTES - 1;");
        sb.AppendLine($"{ind}        while (top > 0 && s[top] == 0) top--;");
        sb.AppendLine($"{ind}        // Build hex string from most-significant to least-significant byte");
        sb.AppendLine($"{ind}        var chars = new char[2 + (top + 1) * 2];");
        sb.AppendLine($"{ind}        chars[0] = '0';");
        sb.AppendLine($"{ind}        chars[1] = 'x';");
        sb.AppendLine($"{ind}        const string HEX_CHARS = \"0123456789ABCDEF\";");
        sb.AppendLine($"{ind}        // First byte (top) may have leading zero nibble -- include both for simplicity");
        sb.AppendLine($"{ind}        for (int i = top; i >= 0; i--)");
        sb.AppendLine($"{ind}        {{");
        sb.AppendLine($"{ind}            int pos = 2 + (top - i) * 2;");
        sb.AppendLine($"{ind}            chars[pos] = HEX_CHARS[s[i] >> 4];");
        sb.AppendLine($"{ind}            chars[pos + 1] = HEX_CHARS[s[i] & 0xF];");
        sb.AppendLine($"{ind}        }}");
        sb.AppendLine($"{ind}        writer.WriteStringValue(new string(chars));");
        sb.AppendLine($"{ind}    }}");
        sb.AppendLine();
        sb.AppendLine($"{ind}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{ind}    private static int HexVal(char c) => c <= '9' ? c - '0' : (c <= 'F' ? c - 'A' + 10 : c - 'a' + 10);");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine();
    }

    /// <summary>
    /// Formats the optional Description and DescriptionResourceType arguments for a BitFieldInfo constructor call.
    /// </summary>
    private static string FormatDescriptionArgs(string? description, string? descriptionResourceType)
    {
        if (description is null)
            return "";

        var escaped = GeneratorUtils.EscapeStringLiteral(description);
        if (descriptionResourceType is null)
            return $", \"{escaped}\"";

        var resType = StripGlobalPrefix(descriptionResourceType);
        return $", \"{escaped}\", typeof({resType})";
    }

    /// <summary>
    /// Strips only the <c>global::</c> prefix, preserving the full namespace-qualified type name.
    /// </summary>
    private static string StripGlobalPrefix(string qualifiedName)
    {
        const string GLOBAL_PREFIX = "global::";
        return qualifiedName.StartsWith(GLOBAL_PREFIX)
            ? qualifiedName.Substring(GLOBAL_PREFIX.Length)
            : qualifiedName;
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

    /// <summary>
    /// Overload for UInt128 shifted masks where the shift can push a 64-bit mask
    /// beyond 64 bits.  Emits the mask and inverted mask as runtime expressions.
    /// </summary>
    private static void FormatShiftedMasks128(string maskLiteral, int rightShift,
        out string invMaskLit, out string smaskLit)
    {
        smaskLit = $"({maskLiteral} << {rightShift})";
        invMaskLit = $"~({maskLiteral} << {rightShift})";
    }
}
