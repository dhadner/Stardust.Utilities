using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Stardust.Generators;

/// <summary>
/// Source generator for [EnhancedEnum] attributed structs.
/// This generator is a FALLBACK - it generates code that the disk-based tool would create.
/// The disk-based tool creates .Generated.cs files that are committed to Git.
/// If those files exist, they are compiled directly and this generator's output is unused.
/// </summary>
[Generator]
public class EnhancedEnumGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all structs with [EnhancedEnum] attribute
        var enumDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Stardust.Utilities.EnhancedEnumAttribute",
                predicate: static (node, _) => node is StructDeclarationSyntax,
                transform: static (ctx, _) => GetEnhancedEnumInfo(ctx))
            .Where(static info => info is not null);

        // Generate source for each
        context.RegisterSourceOutput(enumDeclarations,
            static (spc, info) => Execute(spc, info!));
    }

    private static EnhancedEnumInfo? GetEnhancedEnumInfo(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol structSymbol)
            return null;

        // Find the nested enum with [EnumKind] attribute
        var kindEnum = structSymbol.GetTypeMembers()
            .FirstOrDefault(t => t.TypeKind == TypeKind.Enum &&
                t.GetAttributes().Any(a => a.AttributeClass?.Name == "EnumKindAttribute"));

        if (kindEnum is null)
            return null;

        var variants = new List<VariantInfo>();

        foreach (var member in kindEnum.GetMembers().OfType<IFieldSymbol>())
        {
            if (!member.HasConstantValue)
                continue;

            // Find [EnumValue] attribute
            var enumValueAttr = member.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "EnumValueAttribute");

            string? payloadTypeName = null;
            bool isReferenceType = false;

            if (enumValueAttr != null && enumValueAttr.ConstructorArguments.Length > 0)
            {
                var typeArg = enumValueAttr.ConstructorArguments[0];
                if (typeArg.Value is ITypeSymbol payloadType)
                {
                    payloadTypeName = GetFullTypeName(payloadType);
                    isReferenceType = payloadType.IsReferenceType;
                }
            }

            variants.Add(new VariantInfo(member.Name, payloadTypeName, isReferenceType));
        }

        if (variants.Count == 0)
            return null;

        var ns = structSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : structSymbol.ContainingNamespace.ToDisplayString();

        return new EnhancedEnumInfo(
            structSymbol.Name,
            ns,
            GetAccessibility(structSymbol),
            variants);
    }

    private static string GetFullTypeName(ITypeSymbol type)
    {
        // Handle tuple types specially
        if (type is INamedTypeSymbol namedType && namedType.IsTupleType)
        {
            var elements = namedType.TupleElements;
            var parts = elements.Select(e =>
            {
                var typeName = GetFullTypeName(e.Type);
                if (!string.IsNullOrEmpty(e.Name) && !e.Name.StartsWith("Item"))
                    return $"{typeName} {e.Name}";
                return typeName;
            });
            return $"({string.Join(", ", parts)})";
        }

        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", "");
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

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private static string ToSafeParameterName(string name)
    {
        var lower = name.ToLowerInvariant();
        return lower switch
        {
            "continue" or "break" or "return" or "if" or "else" or "switch" or "case" or
            "default" or "for" or "foreach" or "while" or "do" or "try" or "catch" or
            "finally" or "throw" or "new" or "this" or "base" or "null" or "true" or
            "false" or "void" or "int" or "string" or "bool" or "object" or "class" or
            "struct" or "enum" or "interface" or "delegate" or "event" or "namespace" or
            "using" or "static" or "const" or "readonly" or "volatile" or "public" or
            "private" or "protected" or "internal" or "abstract" or "virtual" or "override" or
            "sealed" or "partial" or "async" or "await" or "ref" or "out" or "in" or
            "params" or "lock" or "checked" or "unchecked" or "fixed" or "sizeof" or
            "typeof" or "is" or "as" or "goto" or "stackalloc" => $"@{name}",
            _ => name
        };
    }

    private static void Execute(SourceProductionContext context, EnhancedEnumInfo info)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// This code was generated by Stardust.Generators (fallback source generator).");
        sb.AppendLine("// For better IntelliSense, run the tool to create .Generated.cs files and commit them.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(info.Namespace))
        {
            sb.AppendLine($"namespace {info.Namespace};");
            sb.AppendLine();
        }

        var variantsWithPayload = info.Variants.Where(v => v.PayloadType != null).ToList();

        sb.AppendLine($"{info.Accessibility} readonly partial struct {info.TypeName} : IEquatable<{info.TypeName}>");
        sb.AppendLine("{");

        // Fields
        sb.AppendLine("    private readonly Kind _tag;");
        foreach (var v in variantsWithPayload)
        {
            var fieldName = $"_{ToCamelCase(v.Name)}Payload";
            var nullableSuffix = v.IsReferenceType ? "?" : "";
            sb.AppendLine($"    private readonly {v.PayloadType}{nullableSuffix} {fieldName};");
        }
        sb.AppendLine();

        // Tag property
        sb.AppendLine("    public Kind Tag => _tag;");
        sb.AppendLine();

        // Private constructor
        sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.Append($"    private {info.TypeName}(Kind tag");
        foreach (var v in variantsWithPayload)
        {
            var nullableSuffix = v.IsReferenceType ? "?" : "";
            sb.Append($", {v.PayloadType}{nullableSuffix} {ToCamelCase(v.Name)}Payload");
        }
        sb.AppendLine(")");
        sb.AppendLine("    {");
        sb.AppendLine("        _tag = tag;");
        foreach (var v in variantsWithPayload)
        {
            var fieldName = $"_{ToCamelCase(v.Name)}Payload";
            sb.AppendLine($"        {fieldName} = {ToCamelCase(v.Name)}Payload;");
        }
        sb.AppendLine("    }");
        sb.AppendLine();

        // Factory methods
        foreach (var v in info.Variants)
        {
            sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            if (v.PayloadType != null)
            {
                sb.Append($"    public static {info.TypeName} {v.Name}({v.PayloadType} value) => new(Kind.{v.Name}");
            }
            else
            {
                sb.Append($"    public static {info.TypeName} {v.Name}() => new(Kind.{v.Name}");
            }
            foreach (var vp in variantsWithPayload)
            {
                if (vp.Name == v.Name)
                    sb.Append(", value");
                else
                    sb.Append(", default");
            }
            sb.AppendLine(");");
        }
        sb.AppendLine();

        // Is properties
        foreach (var v in info.Variants)
        {
            sb.AppendLine($"    public bool Is{v.Name} => _tag == Kind.{v.Name};");
        }
        sb.AppendLine();

        // TryGet methods
        foreach (var v in variantsWithPayload)
        {
            var fieldName = $"_{ToCamelCase(v.Name)}Payload";
            sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"    public bool TryGet{v.Name}(out {v.PayloadType} value)");
            sb.AppendLine("    {");
            sb.AppendLine($"        if (_tag == Kind.{v.Name})");
            sb.AppendLine("        {");
            if (v.IsReferenceType)
                sb.AppendLine($"            value = {fieldName}!;");
            else
                sb.AppendLine($"            value = {fieldName};");
            sb.AppendLine("            return true;");
            sb.AppendLine("        }");
            sb.AppendLine("        value = default!;");
            sb.AppendLine("        return false;");
            sb.AppendLine("    }");
        }
        sb.AppendLine();

        // Match method
        sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine("    public TResult Match<TResult>(");
        for (int i = 0; i < info.Variants.Count; i++)
        {
            var v = info.Variants[i];
            var paramName = ToSafeParameterName(v.Name);
            var comma = i < info.Variants.Count - 1 ? "," : ")";
            if (v.PayloadType != null)
                sb.AppendLine($"        Func<{v.PayloadType}, TResult> {paramName}{comma}");
            else
                sb.AppendLine($"        Func<TResult> {paramName}{comma}");
        }
        sb.AppendLine("    {");
        sb.AppendLine("        return _tag switch");
        sb.AppendLine("        {");
        foreach (var v in info.Variants)
        {
            var paramName = ToSafeParameterName(v.Name);
            if (v.PayloadType != null)
            {
                var fieldName = $"_{ToCamelCase(v.Name)}Payload";
                var nullForgiving = v.IsReferenceType ? "!" : "";
                sb.AppendLine($"            Kind.{v.Name} => {paramName}({fieldName}{nullForgiving}),");
            }
            else
            {
                sb.AppendLine($"            Kind.{v.Name} => {paramName}(),");
            }
        }
        sb.AppendLine("            _ => throw new InvalidOperationException($\"Unknown tag: {_tag}\")");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Equality
        sb.AppendLine($"    public bool Equals({info.TypeName} other)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (_tag != other._tag) return false;");
        sb.AppendLine("        return _tag switch");
        sb.AppendLine("        {");
        foreach (var v in info.Variants)
        {
            if (v.PayloadType != null)
            {
                var fieldName = $"_{ToCamelCase(v.Name)}Payload";
                sb.AppendLine($"            Kind.{v.Name} => EqualityComparer<{v.PayloadType}>.Default.Equals({fieldName}, other.{fieldName}),");
            }
            else
            {
                sb.AppendLine($"            Kind.{v.Name} => true,");
            }
        }
        sb.AppendLine("            _ => false");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine($"    public override bool Equals(object? obj) => obj is {info.TypeName} other && Equals(other);");
        sb.AppendLine();

        sb.AppendLine("    public override int GetHashCode()");
        sb.AppendLine("    {");
        sb.AppendLine("        return _tag switch");
        sb.AppendLine("        {");
        foreach (var v in info.Variants)
        {
            if (v.PayloadType != null)
            {
                var fieldName = $"_{ToCamelCase(v.Name)}Payload";
                sb.AppendLine($"            Kind.{v.Name} => HashCode.Combine(_tag, {fieldName}),");
            }
            else
            {
                sb.AppendLine($"            Kind.{v.Name} => HashCode.Combine(_tag),");
            }
        }
        sb.AppendLine("            _ => 0");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine($"    public static bool operator ==({info.TypeName} left, {info.TypeName} right) => left.Equals(right);");
        sb.AppendLine($"    public static bool operator !=({info.TypeName} left, {info.TypeName} right) => !left.Equals(right);");

        sb.AppendLine("}");

        context.AddSource($"{info.TypeName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }
}

#region Info Classes

internal sealed class EnhancedEnumInfo
{
    public string TypeName { get; }
    public string? Namespace { get; }
    public string Accessibility { get; }
    public List<VariantInfo> Variants { get; }

    public EnhancedEnumInfo(string typeName, string? ns, string accessibility, List<VariantInfo> variants)
    {
        TypeName = typeName;
        Namespace = ns;
        Accessibility = accessibility;
        Variants = variants;
    }
}

internal sealed class VariantInfo
{
    public string Name { get; }
    public string? PayloadType { get; }
    public bool IsReferenceType { get; }

    public VariantInfo(string name, string? payloadType, bool isReferenceType)
    {
        Name = name;
        PayloadType = payloadType;
        IsReferenceType = isReferenceType;
    }
}

#endregion
