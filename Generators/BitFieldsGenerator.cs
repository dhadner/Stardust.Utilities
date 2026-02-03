using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Stardust.Generators;

/// <summary>
/// Source generator for [BitFields] attributed structs.
/// Generates inline bit manipulation code with compile-time constants for maximum performance.
/// </summary>
[Generator]
public class BitFieldsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all structs with [BitFields] attribute
        var structDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Stardust.Utilities.BitFieldsAttribute",
                predicate: static (node, _) => node is StructDeclarationSyntax,
                transform: static (ctx, _) => GetBitFieldsInfo(ctx))
            .Where(static info => info is not null);

        // Generate source for each
        context.RegisterSourceOutput(structDeclarations,
            static (spc, info) => Execute(spc, info!));
    }

    private static BitFieldsInfo? GetBitFieldsInfo(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol structSymbol)
            return null;

        // Get [BitFields] attribute
        var bitFieldsAttr = structSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "BitFieldsAttribute");

        if (bitFieldsAttr == null)
            return null;

        string? storageType = null;

        // Get type from attribute: [BitFields(typeof(byte))]
        if (bitFieldsAttr.ConstructorArguments.Length >= 1)
        {
            var storageTypeArg = bitFieldsAttr.ConstructorArguments[0];
            if (storageTypeArg.Value is INamedTypeSymbol storageTypeSymbol)
            {
                storageType = storageTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            }
        }

        if (storageType == null)
            return null;

        bool storageTypeIsSigned;
        string unsignedStorageType;

        // Validate storage type
        if (storageType == "byte" || storageType == "ushort" || storageType == "uint" || storageType == "ulong")
        {
            storageTypeIsSigned = false;
            unsignedStorageType = storageType;
        }
        else if (storageType == "sbyte" || storageType == "short" || storageType == "int" || storageType == "long")
        {
            storageTypeIsSigned = true;
            unsignedStorageType = storageType switch
            {
                "sbyte" => "byte",
                "short" => "ushort",
                "int" => "uint",
                "long" => "ulong",
                _ => "CAN'T HAPPEN",
            };
        }
        else
        {
            return null;
        }


        var fields = new List<BitFieldInfo>();
        var flags = new List<BitFlagInfo>();


        foreach (var member in structSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            foreach (var attr in member.GetAttributes())
            {
                var attrName = attr.AttributeClass?.Name;
                if (attrName == "BitFieldAttribute" && attr.ConstructorArguments.Length >= 2)
                {
                    // New API: [BitField(startBit, endBit)] - Rust-style inclusive range
                    var startBit = (int)(attr.ConstructorArguments[0].Value ?? 0);
                    var endBit = (int)(attr.ConstructorArguments[1].Value ?? 0);
                    var width = endBit - startBit + 1;
                    var propType = member.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                    fields.Add(new BitFieldInfo(member.Name, propType, startBit, width));
                }
                else if (attrName == "BitFlagAttribute" && attr.ConstructorArguments.Length >= 1)
                {
                    var bit = (int)(attr.ConstructorArguments[0].Value ?? 0);
                    flags.Add(new BitFlagInfo(member.Name, bit));
                }
            }
        }

        if (fields.Count == 0 && flags.Count == 0)
            return null;

        var ns = structSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : structSymbol.ContainingNamespace.ToDisplayString();

        // Collect containing types (for nested structs)
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

        return new BitFieldsInfo(
            structSymbol.Name,
            ns,
            GetAccessibility(structSymbol),
            storageType,
            storageTypeIsSigned,
            unsignedStorageType,
            fields,
            flags,
            containingTypes);
    }

    private static string GetAccessibility(ISymbol symbol)
    {
        return symbol.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Protected => "protected",
            Accessibility.Private => "private",
            _ => "public"
        };
    }

    private static void Execute(SourceProductionContext context, BitFieldsInfo info)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// This code was generated by Stardust.Generators source generator.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Globalization;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(info.Namespace))
        {
            sb.AppendLine($"namespace {info.Namespace};");
            sb.AppendLine();
        }

        // Open containing types (for nested structs)
        int indentLevel = 0;
        foreach (var (kind, name, accessibility) in info.ContainingTypes)
        {
            sb.AppendLine($"{new string(' ', indentLevel * 4)}{accessibility} partial {kind} {name}");
            sb.AppendLine($"{new string(' ', indentLevel * 4)}{{");
            indentLevel++;
        }

        string indent = new string(' ', indentLevel * 4);

        sb.AppendLine($"{indent}{info.Accessibility} partial struct {info.TypeName} : IComparable, IComparable<{info.TypeName}>, IEquatable<{info.TypeName}>,");
        sb.AppendLine($"{indent}                             IFormattable, ISpanFormattable, IParsable<{info.TypeName}>, ISpanParsable<{info.TypeName}>");
        sb.AppendLine($"{indent}{{");

        string memberIndent = new string(' ', (indentLevel + 1) * 4);

        // Generate private Value field and constructor
        sb.AppendLine($"{memberIndent}private {info.StorageType} Value;");
        sb.AppendLine();
        sb.AppendLine($"{memberIndent}/// <summary>Creates a new {info.TypeName} with the specified raw value.</summary>");
        sb.AppendLine($"{memberIndent}public {info.TypeName}({info.StorageType} value) {{ Value = value; }}");
        sb.AppendLine();

        // Generate property implementations with inline constants
        foreach (var field in info.Fields)
        {
            GenerateBitFieldProperty(sb, info, field, memberIndent);
        }

        foreach (var flag in info.Flags)
        {
            GenerateBitFlagProperty(sb, info, flag, memberIndent);
        }

        // Generate static Bit properties for each BitFlag
        foreach (var flag in info.Flags)
        {
            GenerateStaticBitProperty(sb, info, flag, memberIndent);
        }

        // Generate static Mask properties for each BitField
        foreach (var field in info.Fields)
        {
            GenerateStaticMaskProperty(sb, info, field, memberIndent);
        }

        // Generate With{Name} methods for fluent API
        foreach (var flag in info.Flags)
        {
            GenerateWithBitFlagMethod(sb, info, flag, memberIndent);
        }

        foreach (var field in info.Fields)
        {
            GenerateWithBitFieldMethod(sb, info, field, memberIndent);
        }

        // Generate bitwise operators
        GenerateBitwiseOperators(sb, info, memberIndent);

        // Generate arithmetic operators
        GenerateArithmeticOperators(sb, info, memberIndent);

        // Generate shift operators
        GenerateShiftOperators(sb, info, memberIndent);

        // Generate comparison operators
        GenerateComparisonOperators(sb, info, memberIndent);

        // Generate equality operators
        GenerateEqualityOperators(sb, info, memberIndent);

        // Generate implicit conversions
        sb.AppendLine($"{memberIndent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{memberIndent}public static implicit operator {info.StorageType}({info.TypeName} value) => value.Value;");
        sb.AppendLine();
        sb.AppendLine($"{memberIndent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{memberIndent}public static implicit operator {info.TypeName}({info.StorageType} value) => new(value);");
        sb.AppendLine();

        // Generate parsing methods (IParsable<T> and ISpanParsable<T>)
        GenerateParsingMethods(sb, info, memberIndent);

        // Generate formatting methods (IFormattable and ISpanFormattable)
        GenerateFormattingMethods(sb, info, memberIndent);

        // Generate comparison and equality interface methods
        GenerateComparisonMethods(sb, info, memberIndent);

        sb.AppendLine($"{indent}}}");

        // Close containing types
        for (int i = info.ContainingTypes.Count - 1; i >= 0; i--)
        {
            indentLevel--;
            sb.AppendLine($"{new string(' ', indentLevel * 4)}}}");
        }

        // Generate unique filename for nested types
        string fileName = info.ContainingTypes.Count > 0
            ? $"{string.Join("_", info.ContainingTypes.Select(ct => ct.Name))}_{info.TypeName}.g.cs"
            : $"{info.TypeName}.g.cs";

        context.AddSource(fileName, SourceText.From(sb.ToString(), Encoding.UTF8));
    }




    /// <summary>
    /// Generates a BitField property with inline constants for maximum performance.
    /// </summary>
    private static void GenerateBitFieldProperty(StringBuilder sb, BitFieldsInfo info, BitFieldInfo field, string indent)
    {
        int shift = field.Shift;
        int width = field.Width;
        
        // Calculate masks as compile-time constants
        ulong mask = (1UL << width) - 1;
        ulong shiftedMask = mask << shift;
        ulong invertedShiftedMask = ~shiftedMask;

        // For signed types, use unsigned type for mask operations to avoid sign extension issues
        string maskType = info.StorageTypeIsSigned ? info.UnsignedStorageType : info.StorageType;
        
        // Format masks - always use unsigned representation for masks
        string maskHex = FormatHex(mask, maskType);
        string shiftedMaskHex = FormatHex(shiftedMask, maskType);
        string invertedMaskHex = FormatHex(invertedShiftedMask, maskType);

        sb.AppendLine($"{indent}public partial {field.PropertyType} {field.Name}");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        
        // Getter: For signed types, cast to unsigned first to avoid sign extension during shift
        if (info.StorageTypeIsSigned)
        {
            if (shift == 0)
            {
                sb.AppendLine($"{indent}    get => ({field.PropertyType})((({info.UnsignedStorageType})Value) & {maskHex});");
            }
            else
            {
                sb.AppendLine($"{indent}    get => ({field.PropertyType})(((({info.UnsignedStorageType})Value) >> {shift}) & {maskHex});");
            }
        }
        else
        {
            if (shift == 0)
            {
                sb.AppendLine($"{indent}    get => ({field.PropertyType})(Value & {maskHex});");
            }
            else
            {
                sb.AppendLine($"{indent}    get => ({field.PropertyType})((Value >> {shift}) & {maskHex});");
            }
        }

        sb.AppendLine($"{indent}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        
        // Setter: For signed types, do operations in unsigned space then cast back
        if (info.StorageTypeIsSigned)
        {
            if (shift == 0)
            {
                sb.AppendLine($"{indent}    set => Value = ({info.StorageType})(((({info.UnsignedStorageType})Value) & {invertedMaskHex}) | (({info.UnsignedStorageType})value & {shiftedMaskHex}));");
            }
            else
            {
                sb.AppendLine($"{indent}    set => Value = ({info.StorageType})(((({info.UnsignedStorageType})Value) & {invertedMaskHex}) | (((({info.UnsignedStorageType})value) << {shift}) & {shiftedMaskHex}));");
            }
        }
        else
        {
            if (shift == 0)
            {
                sb.AppendLine($"{indent}    set => Value = ({info.StorageType})((Value & {invertedMaskHex}) | (value & {shiftedMaskHex}));");
            }
            else
            {
                sb.AppendLine($"{indent}    set => Value = ({info.StorageType})((Value & {invertedMaskHex}) | ((({info.StorageType})value << {shift}) & {shiftedMaskHex}));");
            }
        }
        
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates a BitFlag property with inline constants for maximum performance.
    /// </summary>
    private static void GenerateBitFlagProperty(StringBuilder sb, BitFieldsInfo info, BitFlagInfo flag, string indent)
    {
        int bit = flag.Bit;
        
        // Calculate mask as compile-time constant
        ulong mask = 1UL << bit;
        ulong invertedMask = ~mask;

        // For signed types, use unsigned type for mask operations
        string maskType = info.StorageTypeIsSigned ? info.UnsignedStorageType : info.StorageType;
        
        string maskHex = FormatHex(mask, maskType);
        string invertedMaskHex = FormatHex(invertedMask, maskType);


        sb.AppendLine($"{indent}public partial bool {flag.Name}");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        
        if (info.StorageTypeIsSigned)
        {
            sb.AppendLine($"{indent}    get => ((({info.UnsignedStorageType})Value) & {maskHex}) != 0;");
            sb.AppendLine($"{indent}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}    set => Value = value ? ({info.StorageType})((({info.UnsignedStorageType})Value) | {maskHex}) : ({info.StorageType})((({info.UnsignedStorageType})Value) & {invertedMaskHex});");
        }
        else
        {
            sb.AppendLine($"{indent}    get => (Value & {maskHex}) != 0;");
            sb.AppendLine($"{indent}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}    set => Value = value ? ({info.StorageType})(Value | {maskHex}) : ({info.StorageType})(Value & {invertedMaskHex});");
        }
        
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates a static property that returns a struct with only the specified bit set.
    /// E.g., CA1_VblBit returns a new IFRFields with only the CA1_Vbl bit set.
    /// </summary>
    private static void GenerateStaticBitProperty(StringBuilder sb, BitFieldsInfo info, BitFlagInfo flag, string indent)
    {
        int bit = flag.Bit;
        ulong mask = 1UL << bit;
        string maskType = info.StorageTypeIsSigned ? info.UnsignedStorageType : info.StorageType;
        string maskHex = FormatHex(mask, maskType);

        // For signed types, we need unchecked for values that exceed signed max
        string castExpr = info.StorageTypeIsSigned 
            ? $"unchecked(({info.StorageType}){maskHex})"
            : $"({info.StorageType}){maskHex}";

        sb.AppendLine($"{indent}/// <summary>Returns a {info.TypeName} with only the {flag.Name} bit set.</summary>");
        sb.AppendLine($"{indent}public static {info.TypeName} {flag.Name}Bit => new({castExpr});");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates a static property that returns a struct with the mask for the specified field.
    /// E.g., SoundMask returns a new RegAFields with bits 0-2 set (0x07).
    /// </summary>
    private static void GenerateStaticMaskProperty(StringBuilder sb, BitFieldsInfo info, BitFieldInfo field, string indent)
    {
        int shift = field.Shift;
        int width = field.Width;
        ulong mask = ((1UL << width) - 1) << shift;
        string maskType = info.StorageTypeIsSigned ? info.UnsignedStorageType : info.StorageType;
        string maskHex = FormatHex(mask, maskType);

        // For signed types, we need unchecked for values that exceed signed max
        string castExpr = info.StorageTypeIsSigned 
            ? $"unchecked(({info.StorageType}){maskHex})"
            : $"({info.StorageType}){maskHex}";

        sb.AppendLine($"{indent}/// <summary>Returns a {info.TypeName} with the mask for the {field.Name} field (bits {shift}-{shift + width - 1}).</summary>");
        sb.AppendLine($"{indent}public static {info.TypeName} {field.Name}Mask => new({castExpr});");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates a With{Name} method for a BitFlag that returns a new struct with the flag set/cleared.
    /// E.g., WithReady(true) returns a new IFRFields with the Ready bit set.
    /// </summary>
    private static void GenerateWithBitFlagMethod(StringBuilder sb, BitFieldsInfo info, BitFlagInfo flag, string indent)
    {
        int bit = flag.Bit;
        ulong mask = 1UL << bit;
        ulong invertedMask = ~mask;

        string maskType = info.StorageTypeIsSigned ? info.UnsignedStorageType : info.StorageType;
        string maskHex = FormatHex(mask, maskType);
        string invertedMaskHex = FormatHex(invertedMask, maskType);

        sb.AppendLine($"{indent}/// <summary>Returns a new {info.TypeName} with the {flag.Name} flag set to the specified value.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");

        if (info.StorageTypeIsSigned)
        {
            sb.AppendLine($"{indent}public {info.TypeName} With{flag.Name}(bool value) => new(value ? ({info.StorageType})((({info.UnsignedStorageType})Value) | {maskHex}) : ({info.StorageType})((({info.UnsignedStorageType})Value) & {invertedMaskHex}));");
        }
        else
        {
            sb.AppendLine($"{indent}public {info.TypeName} With{flag.Name}(bool value) => new(value ? ({info.StorageType})(Value | {maskHex}) : ({info.StorageType})(Value & {invertedMaskHex}));");
        }
        sb.AppendLine();
    }

    /// <summary>
    /// Generates a With{Name} method for a BitField that returns a new struct with the field set to a value.
    /// E.g., WithSound(5) returns a new RegAFields with the Sound field set to 5.
    /// </summary>
    private static void GenerateWithBitFieldMethod(StringBuilder sb, BitFieldsInfo info, BitFieldInfo field, string indent)
    {
        int shift = field.Shift;
        int width = field.Width;

        ulong mask = (1UL << width) - 1;
        ulong shiftedMask = mask << shift;
        ulong invertedShiftedMask = ~shiftedMask;

        string maskType = info.StorageTypeIsSigned ? info.UnsignedStorageType : info.StorageType;
        string shiftedMaskHex = FormatHex(shiftedMask, maskType);
        string invertedMaskHex = FormatHex(invertedShiftedMask, maskType);

        sb.AppendLine($"{indent}/// <summary>Returns a new {info.TypeName} with the {field.Name} field set to the specified value.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");

        if (info.StorageTypeIsSigned)
        {
            if (shift == 0)
            {
                sb.AppendLine($"{indent}public {info.TypeName} With{field.Name}({field.PropertyType} value) => new(({info.StorageType})(((({info.UnsignedStorageType})Value) & {invertedMaskHex}) | (({info.UnsignedStorageType})value & {shiftedMaskHex})));");
            }
            else
            {
                sb.AppendLine($"{indent}public {info.TypeName} With{field.Name}({field.PropertyType} value) => new(({info.StorageType})(((({info.UnsignedStorageType})Value) & {invertedMaskHex}) | (((({info.UnsignedStorageType})value) << {shift}) & {shiftedMaskHex})));");
            }
        }
        else
        {
            if (shift == 0)
            {
                sb.AppendLine($"{indent}public {info.TypeName} With{field.Name}({field.PropertyType} value) => new(({info.StorageType})((Value & {invertedMaskHex}) | (value & {shiftedMaskHex})));");
            }
            else
            {
                sb.AppendLine($"{indent}public {info.TypeName} With{field.Name}({field.PropertyType} value) => new(({info.StorageType})((Value & {invertedMaskHex}) | ((({info.StorageType})value << {shift}) & {shiftedMaskHex})));");
            }
        }
        sb.AppendLine();
    }

    /// <summary>
    /// Generates bitwise operators: |, &amp;, ^, ~
    /// Also generates mixed-type operators with the storage type.
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

        // Binary OR |
        sb.AppendLine($"{indent}/// <summary>Bitwise OR operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator |({t} a, {t} b) => new(({s})(a.Value | b.Value));");
        sb.AppendLine();
        sb.AppendLine($"{indent}/// <summary>Bitwise OR operator with storage type.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator |({t} a, {s} b) => new(({s})(a.Value | b));");
        sb.AppendLine();
        sb.AppendLine($"{indent}/// <summary>Bitwise OR operator with storage type.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator |({s} a, {t} b) => new(({s})(a | b.Value));");
        sb.AppendLine();

        // Binary AND &
        sb.AppendLine($"{indent}/// <summary>Bitwise AND operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator &({t} a, {t} b) => new(({s})(a.Value & b.Value));");
        sb.AppendLine();
        sb.AppendLine($"{indent}/// <summary>Bitwise AND operator with storage type.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator &({t} a, {s} b) => new(({s})(a.Value & b));");
        sb.AppendLine();
        sb.AppendLine($"{indent}/// <summary>Bitwise AND operator with storage type.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator &({s} a, {t} b) => new(({s})(a & b.Value));");
        sb.AppendLine();

        // Binary XOR ^
        sb.AppendLine($"{indent}/// <summary>Bitwise XOR operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator ^({t} a, {t} b) => new(({s})(a.Value ^ b.Value));");
        sb.AppendLine();
        sb.AppendLine($"{indent}/// <summary>Bitwise XOR operator with storage type.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator ^({t} a, {s} b) => new(({s})(a.Value ^ b));");
        sb.AppendLine();
        sb.AppendLine($"{indent}/// <summary>Bitwise XOR operator with storage type.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator ^({s} a, {t} b) => new(({s})(a ^ b.Value));");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates arithmetic operators: +, -, *, /, %
    /// Note: Unary +/- are provided for all types for convenience, though native unsigned types
    /// don't support unary -. Behavior matches Int32Be pattern with two's complement semantics.
    /// </summary>
    private static void GenerateArithmeticOperators(StringBuilder sb, BitFieldsInfo info, string indent)
    {
        string t = info.TypeName;
        string s = info.StorageType;

        // Unary + (native types return int for byte/sbyte/short/ushort, but we return self for consistency)
        sb.AppendLine($"{indent}/// <summary>Unary plus operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator +({t} a) => a;");
        sb.AppendLine();

        // Unary - (native unsigned types don't support this, but we provide it for convenience like Int32Be)
        sb.AppendLine($"{indent}/// <summary>Unary negation operator. Returns two's complement negation.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        if (info.StorageTypeIsSigned)
        {
            // For signed types, use unchecked to match native wraparound behavior
            // e.g., -sbyte.MinValue wraps to sbyte.MinValue (-128)
            sb.AppendLine($"{indent}public static {t} operator -({t} a) => new(unchecked(({s})(-a.Value)));");
        }
        else
        {
            // For unsigned types, compute two's complement negation
            // This is an extension since native unsigned types don't support unary -
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
    /// Shift amount is always int, matching native behavior.
    /// </summary>
    private static void GenerateShiftOperators(StringBuilder sb, BitFieldsInfo info, string indent)
    {
        string t = info.TypeName;
        string s = info.StorageType;

        // Left shift <<
        sb.AppendLine($"{indent}/// <summary>Left shift operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator <<({t} a, int b) => new(unchecked(({s})(a.Value << b)));");
        sb.AppendLine();

        // Right shift >>
        sb.AppendLine($"{indent}/// <summary>Right shift operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator >>({t} a, int b) => new(unchecked(({s})(a.Value >> b)));");
        sb.AppendLine();

        // Unsigned right shift >>> (C# 11+)
        sb.AppendLine($"{indent}/// <summary>Unsigned right shift operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} operator >>>({t} a, int b) => new(unchecked(({s})(a.Value >>> b)));");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates comparison operators: &lt;, &gt;, &lt;=, &gt;=
    /// </summary>
    private static void GenerateComparisonOperators(StringBuilder sb, BitFieldsInfo info, string indent)
    {
        string t = info.TypeName;

        // < operator
        sb.AppendLine($"{indent}/// <summary>Less than operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static bool operator <({t} a, {t} b) => a.Value < b.Value;");
        sb.AppendLine();

        // > operator
        sb.AppendLine($"{indent}/// <summary>Greater than operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static bool operator >({t} a, {t} b) => a.Value > b.Value;");
        sb.AppendLine();

        // <= operator
        sb.AppendLine($"{indent}/// <summary>Less than or equal operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static bool operator <=({t} a, {t} b) => a.Value <= b.Value;");
        sb.AppendLine();

        // >= operator
        sb.AppendLine($"{indent}/// <summary>Greater than or equal operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static bool operator >=({t} a, {t} b) => a.Value >= b.Value;");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates equality operators: ==, !=
    /// Also generates Equals and GetHashCode overrides.
    /// </summary>
    private static void GenerateEqualityOperators(StringBuilder sb, BitFieldsInfo info, string indent)
    {
        string t = info.TypeName;
        string s = info.StorageType;

        // == operator
        sb.AppendLine($"{indent}/// <summary>Equality operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static bool operator ==({t} a, {t} b) => a.Value == b.Value;");
        sb.AppendLine();

        // != operator
        sb.AppendLine($"{indent}/// <summary>Inequality operator.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static bool operator !=({t} a, {t} b) => a.Value != b.Value;");
        sb.AppendLine();

        // Equals override
        sb.AppendLine($"{indent}/// <summary>Determines whether the specified object is equal to the current object.</summary>");
        sb.AppendLine($"{indent}public override bool Equals(object? obj) => obj is {t} other && Value == other.Value;");
        sb.AppendLine();

        // GetHashCode override
        sb.AppendLine($"{indent}/// <summary>Returns the hash code for this instance.</summary>");
        sb.AppendLine($"{indent}public override int GetHashCode() => Value.GetHashCode();");
        sb.AppendLine();

        // ToString override
        sb.AppendLine($"{indent}/// <summary>Returns a string representation of the value.</summary>");
        sb.AppendLine($"{indent}public override string ToString() => $\"0x{{Value:X}}\";");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates parsing methods implementing IParsable&lt;T&gt; and ISpanParsable&lt;T&gt;.
    /// Also generates convenience overloads without IFormatProvider.
    /// Handles decimal, hex (0x/0X prefix), and binary (0b/0B prefix) formats.
    /// Supports C#-style underscore digit separators in all formats.
    /// </summary>
    private static void GenerateParsingMethods(StringBuilder sb, BitFieldsInfo info, string indent)
    {
        string t = info.TypeName;
        string s = info.StorageType;

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

        // Convenience overload: Parse(string)
        sb.AppendLine($"{indent}/// <summary>Parses a string into a {t} using invariant culture. Supports decimal, hex (0x prefix), and binary (0b prefix) formats with optional underscores.</summary>");
        sb.AppendLine($"{indent}/// <param name=\"s\">The string to parse.</param>");
        sb.AppendLine($"{indent}/// <returns>The parsed {t} value.</returns>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static {t} Parse(string s) => Parse(s, CultureInfo.InvariantCulture);");
        sb.AppendLine();

        // Convenience overload: TryParse(string?, out T)
        sb.AppendLine($"{indent}/// <summary>Tries to parse a string into a {t} using invariant culture. Supports decimal, hex (0x prefix), and binary (0b prefix) formats with optional underscores.</summary>");
        sb.AppendLine($"{indent}/// <param name=\"s\">The string to parse.</param>");
        sb.AppendLine($"{indent}/// <param name=\"result\">When this method returns, contains the parsed value if successful.</param>");
        sb.AppendLine($"{indent}/// <returns>true if parsing succeeded; otherwise, false.</returns>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public static bool TryParse(string? s, out {t} result) => TryParse(s, CultureInfo.InvariantCulture, out result);");
        sb.AppendLine();
    }

    /// <summary>
    /// Gets the Convert.ToXxx method name for a given storage type.
    /// </summary>
    private static string GetConvertMethodName(string storageType)
    {
        return storageType switch
        {
            "byte" => "Byte",
            "sbyte" => "SByte",
            "short" => "Int16",
            "ushort" => "UInt16",
            "int" => "Int32",
            "uint" => "UInt32",
            "long" => "Int64",
            "ulong" => "UInt64",
            _ => "Int32"
        };
    }

    /// <summary>
    /// Generates formatting methods implementing IFormattable and ISpanFormattable.
    /// </summary>
    private static void GenerateFormattingMethods(StringBuilder sb, BitFieldsInfo info, string indent)
    {
        string t = info.TypeName;
        string s = info.StorageType;

        // IFormattable.ToString(string?, IFormatProvider?)
        sb.AppendLine($"{indent}/// <summary>Formats the value using the specified format and format provider.</summary>");
        sb.AppendLine($"{indent}/// <param name=\"format\">The format to use, or null for the default format.</param>");
        sb.AppendLine($"{indent}/// <param name=\"formatProvider\">The provider to use for culture-specific formatting.</param>");
        sb.AppendLine($"{indent}/// <returns>The formatted string representation of the value.</returns>");
        sb.AppendLine($"{indent}public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);");
        sb.AppendLine();

        // ISpanFormattable.TryFormat
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

        // IComparable.CompareTo(object?)
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

        // IComparable<T>.CompareTo(T)
        sb.AppendLine($"{indent}/// <summary>Compares this instance to another {t} and returns an integer indicating their relative order.</summary>");
        sb.AppendLine($"{indent}/// <param name=\"other\">A {t} to compare.</param>");
        sb.AppendLine($"{indent}/// <returns>A value indicating the relative order of the instances being compared.</returns>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public int CompareTo({t} other) => Value.CompareTo(other.Value);");
        sb.AppendLine();

        // IEquatable<T>.Equals(T)
        sb.AppendLine($"{indent}/// <summary>Indicates whether this instance is equal to another {t}.</summary>");
        sb.AppendLine($"{indent}/// <param name=\"other\">A {t} to compare with this instance.</param>");
        sb.AppendLine($"{indent}/// <returns>true if the two instances are equal; otherwise, false.</returns>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{indent}public bool Equals({t} other) => Value == other.Value;");
        sb.AppendLine();
    }

    /// <summary>
    /// Formats a value as a hex literal appropriate for the storage type.
    /// For signed types, uses unchecked cast from unsigned to avoid overflow issues.
    /// </summary>
    private static string FormatHex(ulong value, string storageType)
    {
        return storageType switch
        {
            "byte" => $"0x{(byte)value:X2}",
            "ushort" => $"0x{(ushort)value:X4}",
            "uint" => $"0x{(uint)value:X8}U",
            "ulong" => $"0x{value:X16}UL",
            // For signed types, we need unchecked casts because mask values can exceed signed max
            "sbyte" => $"unchecked((sbyte)0x{(byte)value:X2})",
            "short" => $"unchecked((short)0x{(ushort)value:X4})",
            "int" => $"unchecked((int)0x{(uint)value:X8}U)",
            "long" => $"unchecked((long)0x{value:X16}UL)",
            _ => $"0x{value:X}"
        };
    }
}

#region Info Classes


internal sealed class BitFieldsInfo
{
    public string TypeName { get; }
    public string? Namespace { get; }
    public string Accessibility { get; }
    public string StorageType { get; }
    public bool StorageTypeIsSigned { get; }
    public string UnsignedStorageType { get; }
    public List<BitFieldInfo> Fields { get; }
    public List<BitFlagInfo> Flags { get; }
    /// <summary>
    /// List of containing types from outermost to innermost (closest to target struct).
    /// Each tuple contains (TypeKind, TypeName, Accessibility).
    /// </summary>
    public List<(string Kind, string Name, string Accessibility)> ContainingTypes { get; }

    public BitFieldsInfo(string typeName, string? ns, string accessibility, string storageType, bool storageTypeIsSigned, string unsignedStorageType, List<BitFieldInfo> fields, List<BitFlagInfo> flags, List<(string Kind, string Name, string Accessibility)> containingTypes)
    {
        TypeName = typeName;
        Namespace = ns;
        Accessibility = accessibility;
        StorageType = storageType;
        UnsignedStorageType = unsignedStorageType;
        StorageTypeIsSigned = storageTypeIsSigned;
        Fields = fields;
        Flags = flags;
        ContainingTypes = containingTypes;
    }
}

internal sealed class BitFieldInfo
{
    public string Name { get; }
    public string PropertyType { get; }
    public int Shift { get; }
    public int Width { get; }

    public BitFieldInfo(string name, string propertyType, int shift, int width)
    {
        Name = name;
        PropertyType = propertyType;
        Shift = shift;
        Width = width;
    }
}

internal sealed class BitFlagInfo
{
    public string Name { get; }
    public int Bit { get; }

    public BitFlagInfo(string name, int bit)
    {
        Name = name;
        Bit = bit;
    }
}

#endregion
