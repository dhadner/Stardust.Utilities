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
                    var shift = (int)(attr.ConstructorArguments[0].Value ?? 0);
                    var width = (int)(attr.ConstructorArguments[1].Value ?? 1);
                    var propType = member.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                    fields.Add(new BitFieldInfo(member.Name, propType, shift, width));
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

        return new BitFieldsInfo(
            structSymbol.Name,
            ns,
            GetAccessibility(structSymbol),
            storageType,
            storageTypeIsSigned,
            unsignedStorageType,
            fields,
            flags);
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

        sb.AppendLine($"{info.Accessibility} partial struct {info.TypeName}");
        sb.AppendLine("{");

        // Generate private Value field and constructor
        sb.AppendLine($"    private {info.StorageType} Value;");
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>Creates a new {info.TypeName} with the specified raw value.</summary>");
        sb.AppendLine($"    public {info.TypeName}({info.StorageType} value) {{ Value = value; }}");
        sb.AppendLine();

        // Generate property implementations with inline constants
        foreach (var field in info.Fields)
        {
            GenerateBitFieldProperty(sb, info, field);
        }

        foreach (var flag in info.Flags)
        {
            GenerateBitFlagProperty(sb, info, flag);
        }

        // Generate implicit conversions
        sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"    public static implicit operator {info.StorageType}({info.TypeName} value) => value.Value;");
        sb.AppendLine();
        sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"    public static implicit operator {info.TypeName}({info.StorageType} value) => new(value);");

        sb.AppendLine("}");

        context.AddSource($"{info.TypeName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }




    /// <summary>
    /// Generates a BitField property with inline constants for maximum performance.
    /// </summary>
    private static void GenerateBitFieldProperty(StringBuilder sb, BitFieldsInfo info, BitFieldInfo field)
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

        sb.AppendLine($"    public partial {field.PropertyType} {field.Name}");
        sb.AppendLine("    {");
        sb.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        
        // Getter: For signed types, cast to unsigned first to avoid sign extension during shift
        if (info.StorageTypeIsSigned)
        {
            if (shift == 0)
            {
                sb.AppendLine($"        get => ({field.PropertyType})((({info.UnsignedStorageType})Value) & {maskHex});");
            }
            else
            {
                sb.AppendLine($"        get => ({field.PropertyType})(((({info.UnsignedStorageType})Value) >> {shift}) & {maskHex});");
            }
        }
        else
        {
            if (shift == 0)
            {
                sb.AppendLine($"        get => ({field.PropertyType})(Value & {maskHex});");
            }
            else
            {
                sb.AppendLine($"        get => ({field.PropertyType})((Value >> {shift}) & {maskHex});");
            }
        }

        sb.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        
        // Setter: For signed types, do operations in unsigned space then cast back
        if (info.StorageTypeIsSigned)
        {
            if (shift == 0)
            {
                sb.AppendLine($"        set => Value = ({info.StorageType})(((({info.UnsignedStorageType})Value) & {invertedMaskHex}) | (({info.UnsignedStorageType})value & {shiftedMaskHex}));");
            }
            else
            {
                sb.AppendLine($"        set => Value = ({info.StorageType})(((({info.UnsignedStorageType})Value) & {invertedMaskHex}) | (((({info.UnsignedStorageType})value) << {shift}) & {shiftedMaskHex}));");
            }
        }
        else
        {
            if (shift == 0)
            {
                sb.AppendLine($"        set => Value = ({info.StorageType})((Value & {invertedMaskHex}) | (value & {shiftedMaskHex}));");
            }
            else
            {
                sb.AppendLine($"        set => Value = ({info.StorageType})((Value & {invertedMaskHex}) | ((({info.StorageType})value << {shift}) & {shiftedMaskHex}));");
            }
        }
        
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates a BitFlag property with inline constants for maximum performance.
    /// </summary>
    private static void GenerateBitFlagProperty(StringBuilder sb, BitFieldsInfo info, BitFlagInfo flag)
    {
        int bit = flag.Bit;
        
        // Calculate mask as compile-time constant
        ulong mask = 1UL << bit;
        ulong invertedMask = ~mask;

        // For signed types, use unsigned type for mask operations
        string maskType = info.StorageTypeIsSigned ? info.UnsignedStorageType : info.StorageType;
        
        string maskHex = FormatHex(mask, maskType);
        string invertedMaskHex = FormatHex(invertedMask, maskType);

        sb.AppendLine($"    public partial bool {flag.Name}");
        sb.AppendLine("    {");
        sb.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        
        if (info.StorageTypeIsSigned)
        {
            sb.AppendLine($"        get => ((({info.UnsignedStorageType})Value) & {maskHex}) != 0;");
            sb.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"        set => Value = value ? ({info.StorageType})((({info.UnsignedStorageType})Value) | {maskHex}) : ({info.StorageType})((({info.UnsignedStorageType})Value) & {invertedMaskHex});");
        }
        else
        {
            sb.AppendLine($"        get => (Value & {maskHex}) != 0;");
            sb.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"        set => Value = value ? ({info.StorageType})(Value | {maskHex}) : ({info.StorageType})(Value & {invertedMaskHex});");
        }
        
        sb.AppendLine("    }");
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

    public BitFieldsInfo(string typeName, string? ns, string accessibility, string storageType, bool storageTypeIsSigned, string unsignedStorageType, List<BitFieldInfo> fields, List<BitFlagInfo> flags)
    {
        TypeName = typeName;
        Namespace = ns;
        Accessibility = accessibility;
        StorageType = storageType;
        UnsignedStorageType = unsignedStorageType;
        StorageTypeIsSigned = storageTypeIsSigned;
        Fields = fields;
        Flags = flags;
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
