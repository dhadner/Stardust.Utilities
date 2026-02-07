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
public partial class BitFieldsGenerator : IIncrementalGenerator
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
        bool isMultiWord = false;
        int bitCount = 0;

        // Detect constructor form: [BitFields(typeof(T))] vs [BitFields(int)]
        if (bitFieldsAttr.ConstructorArguments.Length >= 1)
        {
            var firstArg = bitFieldsAttr.ConstructorArguments[0];
            if (firstArg.Value is INamedTypeSymbol storageTypeSymbol)
            {
                // Type-based constructor: [BitFields(typeof(byte))]
                storageType = storageTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            }
            else if (firstArg.Value is int bitCountValue)
            {
                // Int-based constructor: [BitFields(200)]
                isMultiWord = true;
                bitCount = bitCountValue;
                if (bitCount < 1 || bitCount > BitFieldsMultiWordGenerator.MAX_BIT_COUNT)
                    return null;
            }
        }

        if (storageType == null && !isMultiWord)
            return null;

        bool storageTypeIsSigned;
        string unsignedStorageType;
        string? floatingPointType = null;
        string? nativeWideType = null;

        if (isMultiWord)
        {
            // MultiWord mode: storage is multiple ulongs
            storageTypeIsSigned = false;
            unsignedStorageType = "ulong";
            storageType = "ulong"; // placeholder for compatibility
        }
        // Float/double: NativeFloat mode — stored as uint/ulong, user-facing type is float/double
        else if (storageType == "float")
        {
            storageTypeIsSigned = false;
            unsignedStorageType = "uint";
            floatingPointType = "float";
            storageType = "uint"; // internal storage is uint
        }
        else if (storageType == "double")
        {
            storageTypeIsSigned = false;
            unsignedStorageType = "ulong";
            floatingPointType = "double";
            storageType = "ulong"; // internal storage is ulong
        }
        // UInt128/Int128: route through multi-word (2 ulongs) with native type conversions
        else if (storageType == "UInt128")
        {
            isMultiWord = true;
            bitCount = 128;
            storageTypeIsSigned = false;
            unsignedStorageType = "ulong";
            nativeWideType = "UInt128";
            storageType = "ulong";
        }
        else if (storageType == "Int128")
        {
            isMultiWord = true;
            bitCount = 128;
            storageTypeIsSigned = false;
            unsignedStorageType = "ulong";
            nativeWideType = "Int128";
            storageType = "ulong";
        }
        else if (storageType == "decimal")
        {
            isMultiWord = true;
            bitCount = 128;
            storageTypeIsSigned = false;
            unsignedStorageType = "ulong";
            nativeWideType = "decimal";
            storageType = "ulong";
        }
        // Validate storage type
        else if (storageType == "byte" || storageType == "ushort" || storageType == "uint" || storageType == "ulong")
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
                    // [BitField(startBit, endBit, mustBe)] - Rust-style inclusive range
                    var startBit = (int)(attr.ConstructorArguments[0].Value ?? 0);
                    var endBit = (int)(attr.ConstructorArguments[1].Value ?? 0);
                    var width = endBit - startBit + 1;
                    var propType = member.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                    
                    // Read optional MustBe parameter (3rd constructor arg)
                    var valueOverride = MustBeValue.Any;
                    if (attr.ConstructorArguments.Length >= 3 && attr.ConstructorArguments[2].Value is int fieldMustBe)
                    {
                        valueOverride = (MustBeValue)fieldMustBe;
                    }
                    
                    fields.Add(new BitFieldInfo(member.Name, propType, startBit, width, valueOverride));
                }
                else if (attrName == "BitFlagAttribute" && attr.ConstructorArguments.Length >= 1)
                {
                    var bit = (int)(attr.ConstructorArguments[0].Value ?? 0);
                    
                    // Read optional MustBe parameter (2nd constructor arg)
                    var valueOverride = MustBeValue.Any;
                    if (attr.ConstructorArguments.Length >= 2 && attr.ConstructorArguments[1].Value is int flagMustBe)
                    {
                        valueOverride = (MustBeValue)flagMustBe;
                    }
                    
                    flags.Add(new BitFlagInfo(member.Name, bit, valueOverride));
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

        // Read UndefinedBitsMustBe from 2nd constructor argument (default is Any = 0)
        var undefinedBitsMode = MustBeValue.Any;
        if (bitFieldsAttr.ConstructorArguments.Length >= 2 && bitFieldsAttr.ConstructorArguments[1].Value is int modeValue)
        {
            undefinedBitsMode = (MustBeValue)modeValue;
        }

        // Determine storage mode, word count, and total bits
        StorageMode mode;
        int wordCount;
        int totalBits;

        if (isMultiWord)
        {
            mode = StorageMode.MultiWord;
            wordCount = (bitCount + 63) / 64;
            totalBits = bitCount;
        }
        else if (floatingPointType != null)
        {
            mode = StorageMode.NativeFloat;
            wordCount = 1;
            totalBits = GetStorageTypeBitWidth(storageType!);
        }
        else
        {
            mode = StorageMode.NativeInteger;
            wordCount = 1;
            totalBits = GetStorageTypeBitWidth(storageType!);
        }

        return new BitFieldsInfo(
            structSymbol.Name,
            ns,
            GetAccessibility(structSymbol),
            storageType!,
            storageTypeIsSigned,
            unsignedStorageType,
            fields,
            flags,
            containingTypes,
            undefinedBitsMode,
            mode,
            wordCount,
            totalBits,
            floatingPointType,
            nativeWideType);
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
        // Dispatch to the appropriate generator based on storage mode
        if (info.Mode == StorageMode.MultiWord)
        {
            BitFieldsMultiWordGenerator.Execute(context, info);
            return;
        }

        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// This code was generated by Stardust.Generators source generator.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable enable");
        // Suppress CS0675: Sign extension in bitwise ops is intentional for hardware register patterns.
        // When mixing signed int with ulong registers, sign extension to 64 bits is the expected behavior.
        sb.AppendLine("#pragma warning disable CS0675");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Buffers.Binary;");
        sb.AppendLine("using System.Globalization;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using System.Text.Json.Serialization;");
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

        sb.AppendLine($"{indent}[JsonConverter(typeof({info.TypeName}JsonConverter))]");
        sb.AppendLine($"{indent}{info.Accessibility} partial struct {info.TypeName} : IComparable, IComparable<{info.TypeName}>, IEquatable<{info.TypeName}>,");
        sb.AppendLine($"{indent}                             IFormattable, ISpanFormattable, IParsable<{info.TypeName}>, ISpanParsable<{info.TypeName}>");
        sb.AppendLine($"{indent}{{");

        string memberIndent = new string(' ', (indentLevel + 1) * 4);

        // Calculate the defined bits mask for handling undefined bits
        ulong definedBitsMask = CalculateDefinedBitsMask(info);
        int storageBits = GetStorageTypeBitWidth(info.StorageType);
        ulong allBitsMask = storageBits == 64 ? ulong.MaxValue : (1UL << storageBits) - 1;
        ulong undefinedBitsMask = allBitsMask & ~definedBitsMask;
        bool hasUndefinedBits = undefinedBitsMask != 0;
        string maskType = info.StorageTypeIsSigned ? info.UnsignedStorageType : info.StorageType;
        string definedMaskHex = FormatHex(definedBitsMask, maskType);
        string undefinedMaskHex = FormatHex(undefinedBitsMask, maskType);

        // Generate private Value field and constructor
        sb.AppendLine($"{memberIndent}private {info.StorageType} Value;");
        sb.AppendLine();
        int sizeInBytes = GetStorageTypeBitWidth(info.StorageType) / 8;
        sb.AppendLine($"{memberIndent}/// <summary>Size of this struct in bytes.</summary>");
        sb.AppendLine($"{memberIndent}public const int SizeInBytes = {sizeInBytes};");
        sb.AppendLine();
        sb.AppendLine($"{memberIndent}/// <summary>Returns a {info.TypeName} with all bits set to zero.</summary>");
        sb.AppendLine($"{memberIndent}public static {info.TypeName} Zero => default;");
        sb.AppendLine();
        sb.AppendLine($"{memberIndent}/// <summary>Creates a new {info.TypeName} with the specified raw bits value.</summary>");
        
        // Constructor applies undefined bits handling based on mode
        if (!hasUndefinedBits || info.UndefinedBitsMode == MustBeValue.Any)
        {
            sb.AppendLine($"{memberIndent}public {info.TypeName}({info.StorageType} value) {{ Value = value; }}");
        }
        else if (info.UndefinedBitsMode == MustBeValue.Zero)
        {
            if (info.StorageTypeIsSigned)
            {
                sb.AppendLine($"{memberIndent}public {info.TypeName}({info.StorageType} value) {{ Value = ({info.StorageType})((({info.UnsignedStorageType})value) & {definedMaskHex}); }}");
            }
            else
            {
                sb.AppendLine($"{memberIndent}public {info.TypeName}({info.StorageType} value) {{ Value = ({info.StorageType})(value & {definedMaskHex}); }}");
            }
        }
        else // One
        {
            if (info.StorageTypeIsSigned)
            {
                sb.AppendLine($"{memberIndent}public {info.TypeName}({info.StorageType} value) {{ Value = ({info.StorageType})((({info.UnsignedStorageType})value) | {undefinedMaskHex}); }}");
            }
            else
            {
                sb.AppendLine($"{memberIndent}public {info.TypeName}({info.StorageType} value) {{ Value = ({info.StorageType})(value | {undefinedMaskHex}); }}");
            }
        }
        sb.AppendLine();

        // NativeFloat: add floating-point constructor
        if (info.Mode == StorageMode.NativeFloat)
        {
            string fp = info.FloatingPointType!;
            string toBits = fp == "float" ? "BitConverter.SingleToUInt32Bits" : "BitConverter.DoubleToUInt64Bits";
            sb.AppendLine($"{memberIndent}/// <summary>Creates a new {info.TypeName} from a {fp} value.</summary>");
            sb.AppendLine($"{memberIndent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{memberIndent}public {info.TypeName}({fp} value) : this({toBits}(value)) {{ }}");
            sb.AppendLine();
        }

        // Generate property implementations with inline constants
        foreach (var field in info.Fields)
            GenerateBitFieldProperty(sb, info, field, memberIndent);
        foreach (var flag in info.Flags)
            GenerateBitFlagProperty(sb, info, flag, memberIndent);

        // Generate static Bit/Mask properties
        foreach (var flag in info.Flags)
            GenerateStaticBitProperty(sb, info, flag, memberIndent);
        foreach (var field in info.Fields)
            GenerateStaticMaskProperty(sb, info, field, memberIndent);

        // Generate With{Name} methods for fluent API
        foreach (var flag in info.Flags)
            GenerateWithBitFlagMethod(sb, info, flag, memberIndent);
        foreach (var field in info.Fields)
            GenerateWithBitFieldMethod(sb, info, field, memberIndent);

        // Generate operators
        GenerateBitwiseOperators(sb, info, memberIndent);
        GenerateArithmeticOperators(sb, info, memberIndent);
        GenerateShiftOperators(sb, info, memberIndent);
        GenerateComparisonOperators(sb, info, memberIndent);
        GenerateEqualityOperators(sb, info, memberIndent);

        // Generate conversions
        GenerateConversions(sb, info, memberIndent);

        // Generate byte span serialization methods
        GenerateByteSpanMethods(sb, info, memberIndent);

        // Generate parsing, formatting, and comparison interface methods
        GenerateParsingMethods(sb, info, memberIndent);
        GenerateFormattingMethods(sb, info, memberIndent);
        GenerateComparisonMethods(sb, info, memberIndent);

        // Generate JSON converter
        GenerateJsonConverter(sb, info, memberIndent);

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
}
