using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Stardust.Utilities.Generators
{
    /// <summary>
    /// Source generator that creates discriminated union types from [EnhancedEnum] attributed records.
    /// Generates nested sealed record types for each variant with proper pattern matching support.
    /// </summary>
    [Generator]
    public class EnhancedEnumGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Find all records with [EnhancedEnum] attribute
            var enumDeclarations = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    "Stardust.Utilities.EnhancedEnumAttribute",
                    predicate: static (node, _) => node is RecordDeclarationSyntax,
                    transform: static (ctx, _) => GetEnhancedEnumInfo(ctx))
                .Where(static info => info is not null);

            // Generate source for each
            context.RegisterSourceOutput(enumDeclarations,
                static (spc, info) => Execute(spc, info!));
        }

        private static EnhancedEnumInfo? GetEnhancedEnumInfo(GeneratorAttributeSyntaxContext context)
        {
            if (context.TargetSymbol is not INamedTypeSymbol recordSymbol)
                return null;

            var recordSyntax = (RecordDeclarationSyntax)context.TargetNode;

            // Find the nested enum with [EnumKind] attribute
            var kindEnum = recordSymbol.GetTypeMembers()
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

                if (enumValueAttr != null && enumValueAttr.ConstructorArguments.Length > 0)
                {
                    var typeArg = enumValueAttr.ConstructorArguments[0];
                    if (typeArg.Value is INamedTypeSymbol payloadType)
                    {
                        payloadTypeName = GetFullTypeName(payloadType);
                    }
                }

                variants.Add(new VariantInfo(member.Name, payloadTypeName));
            }

            if (variants.Count == 0)
                return null;

            // Get namespace
            var ns = recordSymbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : recordSymbol.ContainingNamespace.ToDisplayString();

            // Check if partial
            bool isPartial = recordSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

            return new EnhancedEnumInfo(
                recordSymbol.Name,
                ns,
                isPartial,
                GetAccessibility(recordSymbol),
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
                    // Include element name if it's explicitly named (not Item1, Item2, etc.)
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
                Accessibility.ProtectedOrInternal => "protected internal",
                Accessibility.ProtectedAndInternal => "private protected",
                _ => "public"
            };
        }

        private static void Execute(SourceProductionContext context, EnhancedEnumInfo info)
        {
            var sb = new StringBuilder();

            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(info.Namespace))
            {
                sb.AppendLine($"namespace {info.Namespace}");
                sb.AppendLine("{");
            }

            var indent = string.IsNullOrEmpty(info.Namespace) ? "" : "    ";

            // Generate abstract partial record
            sb.AppendLine($"{indent}{info.Accessibility} abstract partial record {info.TypeName}");
            sb.AppendLine($"{indent}{{");

            // Generate nested sealed records for each variant
            foreach (var variant in info.Variants)
            {
                GenerateVariant(sb, indent + "    ", info.TypeName, variant);
                sb.AppendLine();
            }

            // Generate Is properties for convenient checking
            sb.AppendLine($"{indent}    #region Is Properties");
            sb.AppendLine();

            foreach (var variant in info.Variants)
            {
                GenerateIsMethod(sb, indent + "    ", variant);
            }

            sb.AppendLine($"{indent}    #endregion");

            sb.AppendLine($"{indent}}}");

            if (!string.IsNullOrEmpty(info.Namespace))
            {
                sb.AppendLine("}");
            }

            context.AddSource($"{info.TypeName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        private static void GenerateVariant(StringBuilder sb, string indent, string parentType, VariantInfo variant)
        {
            if (variant.PayloadType is null)
            {
                // Unit variant (no payload)
                sb.AppendLine($"{indent}/// <summary>");
                sb.AppendLine($"{indent}/// Variant '{variant.Name}' with no associated data.");
                sb.AppendLine($"{indent}/// </summary>");
                sb.AppendLine($"{indent}public sealed record {variant.Name}() : {parentType};");
            }
            else
            {
                // Variant with payload
                sb.AppendLine($"{indent}/// <summary>");
                sb.AppendLine($"{indent}/// Variant '{variant.Name}' with payload of type <see cref=\"{EscapeXmlComment(variant.PayloadType)}\"/>.");
                sb.AppendLine($"{indent}/// </summary>");
                sb.AppendLine($"{indent}/// <param name=\"Value\">The payload value.</param>");
                sb.AppendLine($"{indent}public sealed record {variant.Name}({variant.PayloadType} Value) : {parentType}");
                sb.AppendLine($"{indent}{{");
                sb.AppendLine($"{indent}    /// <summary>");
                sb.AppendLine($"{indent}    /// Deconstructs the variant for pattern matching.");
                sb.AppendLine($"{indent}    /// </summary>");
                sb.AppendLine($"{indent}    /// <param name=\"value\">The extracted payload value.</param>");
                sb.AppendLine($"{indent}    public void Deconstruct(out {variant.PayloadType} value) => value = Value;");
                sb.AppendLine($"{indent}}}");
            }
        }

        private static void GenerateIsMethod(StringBuilder sb, string indent, VariantInfo variant)
        {
            sb.AppendLine($"{indent}/// <summary>");
            sb.AppendLine($"{indent}/// Returns true if this is the {variant.Name} variant.");
            sb.AppendLine($"{indent}/// </summary>");
            sb.AppendLine($"{indent}public bool Is{variant.Name} => this is {variant.Name};");
            sb.AppendLine();
        }

        private static string EscapeXmlComment(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }
    }

    // Using classes instead of records for netstandard2.0 compatibility
    internal sealed class EnhancedEnumInfo
    {
        public string TypeName { get; }
        public string? Namespace { get; }
        public bool IsPartial { get; }
        public string Accessibility { get; }
        public List<VariantInfo> Variants { get; }

        public EnhancedEnumInfo(string typeName, string? ns, bool isPartial, string accessibility, List<VariantInfo> variants)
        {
            TypeName = typeName;
            Namespace = ns;
            IsPartial = isPartial;
            Accessibility = accessibility;
            Variants = variants;
        }
    }

    internal sealed class VariantInfo
    {
        public string Name { get; }
        public string? PayloadType { get; }

        public VariantInfo(string name, string? payloadType)
        {
            Name = name;
            PayloadType = payloadType;
        }
    }
}
