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

        sb.AppendLine($"{indent}{info.Accessibility} partial struct {info.TypeName}");
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

        // Generate equality operators
        GenerateEqualityOperators(sb, info, memberIndent);

        // Generate implicit conversions
        sb.AppendLine($"{memberIndent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{memberIndent}public static implicit operator {info.StorageType}({info.TypeName} value) => value.Value;");
        sb.AppendLine();
        sb.AppendLine($"{memberIndent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{memberIndent}public static implicit operator {info.TypeName}({info.StorageType} value) => new(value);");

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
