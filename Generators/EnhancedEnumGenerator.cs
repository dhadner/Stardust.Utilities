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
    /// Source generator that creates zero-allocation discriminated union structs from [EnhancedEnum] attributed structs.
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

            var structSyntax = (StructDeclarationSyntax)context.TargetNode;

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

            // Get namespace
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
                Accessibility.ProtectedOrInternal => "protected internal",
                Accessibility.ProtectedAndInternal => "private protected",
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
            // Keep PascalCase for Match parameters since they correspond to variant names
            // Only escape C# keywords
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

            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#nullable enable");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Runtime.CompilerServices;");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(info.Namespace))
            {
                sb.AppendLine($"namespace {info.Namespace}");
                sb.AppendLine("{");
            }

            var indent = string.IsNullOrEmpty(info.Namespace) ? "" : "    ";
            var variantsWithPayload = info.Variants.Where(v => v.PayloadType != null).ToList();

            // Generate readonly partial struct
            sb.AppendLine($"{indent}{info.Accessibility} readonly partial struct {info.TypeName} : IEquatable<{info.TypeName}>");
            sb.AppendLine($"{indent}{{");

            // Note: The Kind enum is already defined by the user with [EnumKind] attribute

            // Generate fields
            sb.AppendLine($"{indent}    private readonly Kind _tag;");
            foreach (var v in variantsWithPayload)
            {
                var fieldName = $"_{ToCamelCase(v.Name)}Payload";
                var nullableSuffix = v.IsReferenceType ? "?" : "";
                sb.AppendLine($"{indent}    private readonly {v.PayloadType}{nullableSuffix} {fieldName};");
            }
            sb.AppendLine();

            // Generate Tag property
            sb.AppendLine($"{indent}    /// <summary>Gets the discriminant tag indicating which variant this is.</summary>");
            sb.AppendLine($"{indent}    public Kind Tag => _tag;");
            sb.AppendLine();

            // Generate private constructor
            sb.AppendLine($"{indent}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.Append($"{indent}    private {info.TypeName}(Kind tag");
            foreach (var v in variantsWithPayload)
            {
                var nullableSuffix = v.IsReferenceType ? "?" : "";
                sb.Append($", {v.PayloadType}{nullableSuffix} {ToCamelCase(v.Name)}Payload");
            }
            sb.AppendLine(")");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        _tag = tag;");
            foreach (var v in variantsWithPayload)
            {
                var fieldName = $"_{ToCamelCase(v.Name)}Payload";
                sb.AppendLine($"{indent}        {fieldName} = {ToCamelCase(v.Name)}Payload;");
            }
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();

            // Generate static factory methods
            sb.AppendLine($"{indent}    #region Factory Methods");
            sb.AppendLine();
            foreach (var v in info.Variants)
            {
                GenerateFactoryMethod(sb, indent + "    ", info, v, variantsWithPayload);
            }
            sb.AppendLine($"{indent}    #endregion");
            sb.AppendLine();

            // Generate Is properties
            sb.AppendLine($"{indent}    #region Is Properties");
            sb.AppendLine();
            foreach (var v in info.Variants)
            {
                sb.AppendLine($"{indent}    /// <summary>Returns true if this is the {v.Name} variant.</summary>");
                sb.AppendLine($"{indent}    public bool Is{v.Name} => _tag == Kind.{v.Name};");
                sb.AppendLine();
            }
            sb.AppendLine($"{indent}    #endregion");
            sb.AppendLine();

            // Generate TryGet methods
            sb.AppendLine($"{indent}    #region TryGet Methods");
            sb.AppendLine();
            foreach (var v in variantsWithPayload)
            {
                GenerateTryGetMethod(sb, indent + "    ", v);
            }
            sb.AppendLine($"{indent}    #endregion");
            sb.AppendLine();

            // Generate Match method
            GenerateMatchMethod(sb, indent + "    ", info);
            sb.AppendLine();

            // Generate equality members
            GenerateEqualityMembers(sb, indent + "    ", info, variantsWithPayload);

            sb.AppendLine($"{indent}}}");

            if (!string.IsNullOrEmpty(info.Namespace))
            {
                sb.AppendLine("}");
            }

            context.AddSource($"{info.TypeName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        private static void GenerateFactoryMethod(StringBuilder sb, string indent, EnhancedEnumInfo info, VariantInfo variant, List<VariantInfo> allWithPayload)
        {
            sb.AppendLine($"{indent}/// <summary>Creates a {variant.Name} variant.</summary>");
            if (variant.PayloadType != null)
            {
                sb.AppendLine($"{indent}/// <param name=\"value\">The payload value.</param>");
                sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                sb.Append($"{indent}public static {info.TypeName} {variant.Name}({variant.PayloadType} value) => new(Kind.{variant.Name}");
            }
            else
            {
                sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                sb.Append($"{indent}public static {info.TypeName} {variant.Name}() => new(Kind.{variant.Name}");
            }

            foreach (var v in allWithPayload)
            {
                if (v.Name == variant.Name)
                    sb.Append(", value");
                else
                    sb.Append(", default");
            }
            sb.AppendLine(");");
            sb.AppendLine();
        }

        private static void GenerateTryGetMethod(StringBuilder sb, string indent, VariantInfo variant)
        {
            var fieldName = $"_{ToCamelCase(variant.Name)}Payload";
            sb.AppendLine($"{indent}/// <summary>Attempts to get the {variant.Name} payload.</summary>");
            sb.AppendLine($"{indent}/// <param name=\"value\">The payload value if this is a {variant.Name} variant.</param>");
            sb.AppendLine($"{indent}/// <returns>True if this is a {variant.Name} variant.</returns>");
            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public bool TryGet{variant.Name}(out {variant.PayloadType} value)");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    if (_tag == Kind.{variant.Name})");
            sb.AppendLine($"{indent}    {{");
            if (variant.IsReferenceType)
                sb.AppendLine($"{indent}        value = {fieldName}!;");
            else
                sb.AppendLine($"{indent}        value = {fieldName};");
            sb.AppendLine($"{indent}        return true;");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}    value = default!;");
            sb.AppendLine($"{indent}    return false;");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
        }

        private static void GenerateMatchMethod(StringBuilder sb, string indent, EnhancedEnumInfo info)
        {
            sb.AppendLine($"{indent}#region Match");
            sb.AppendLine();
            sb.AppendLine($"{indent}/// <summary>Exhaustively matches all variants and returns a result.</summary>");

            // Build parameter list
            var paramList = new List<string>();
            foreach (var v in info.Variants)
            {
                var paramName = ToSafeParameterName(v.Name);
                if (v.PayloadType != null)
                    paramList.Add($"Func<{v.PayloadType}, TResult> {paramName}");
                else
                    paramList.Add($"Func<TResult> {paramName}");
            }

            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public TResult Match<TResult>(");
            for (int i = 0; i < paramList.Count; i++)
            {
                var comma = i < paramList.Count - 1 ? "," : ")";
                sb.AppendLine($"{indent}    {paramList[i]}{comma}");
            }
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    return _tag switch");
            sb.AppendLine($"{indent}    {{");
            foreach (var v in info.Variants)
            {
                var paramName = ToSafeParameterName(v.Name);
                if (v.PayloadType != null)
                {
                    var fieldName = $"_{ToCamelCase(v.Name)}Payload";
                    var nullForgiving = v.IsReferenceType ? "!" : "";
                    sb.AppendLine($"{indent}        Kind.{v.Name} => {paramName}({fieldName}{nullForgiving}),");
                }
                else
                {
                    sb.AppendLine($"{indent}        Kind.{v.Name} => {paramName}(),");
                }
            }
            sb.AppendLine($"{indent}        _ => throw new InvalidOperationException($\"Unknown tag: {{_tag}}\")");
            sb.AppendLine($"{indent}    }};");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();

            // Generate void Match overload
            sb.AppendLine($"{indent}/// <summary>Exhaustively matches all variants and performs an action.</summary>");
            var actionParamList = new List<string>();
            foreach (var v in info.Variants)
            {
                var paramName = ToSafeParameterName(v.Name);
                if (v.PayloadType != null)
                    actionParamList.Add($"Action<{v.PayloadType}> {paramName}");
                else
                    actionParamList.Add($"Action {paramName}");
            }

            sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}public void Match(");
            for (int i = 0; i < actionParamList.Count; i++)
            {
                var comma = i < actionParamList.Count - 1 ? "," : ")";
                sb.AppendLine($"{indent}    {actionParamList[i]}{comma}");
            }
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    switch (_tag)");
            sb.AppendLine($"{indent}    {{");
            foreach (var v in info.Variants)
            {
                var paramName = ToSafeParameterName(v.Name);
                if (v.PayloadType != null)
                {
                    var fieldName = $"_{ToCamelCase(v.Name)}Payload";
                    var nullForgiving = v.IsReferenceType ? "!" : "";
                    sb.AppendLine($"{indent}        case Kind.{v.Name}: {paramName}({fieldName}{nullForgiving}); break;");
                }
                else
                {
                    sb.AppendLine($"{indent}        case Kind.{v.Name}: {paramName}(); break;");
                }
            }
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
            sb.AppendLine($"{indent}#endregion");
        }

        private static void GenerateEqualityMembers(StringBuilder sb, string indent, EnhancedEnumInfo info, List<VariantInfo> variantsWithPayload)
        {
            sb.AppendLine($"{indent}#region Equality");
            sb.AppendLine();

            // Equals(T other)
            sb.AppendLine($"{indent}/// <inheritdoc/>");
            sb.AppendLine($"{indent}public bool Equals({info.TypeName} other)");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    if (_tag != other._tag) return false;");
            sb.AppendLine($"{indent}    return _tag switch");
            sb.AppendLine($"{indent}    {{");
            foreach (var v in info.Variants)
            {
                if (v.PayloadType != null)
                {
                    var fieldName = $"_{ToCamelCase(v.Name)}Payload";
                    sb.AppendLine($"{indent}        Kind.{v.Name} => EqualityComparer<{v.PayloadType}>.Default.Equals({fieldName}, other.{fieldName}),");
                }
                else
                {
                    sb.AppendLine($"{indent}        Kind.{v.Name} => true,");
                }
            }
            sb.AppendLine($"{indent}        _ => false");
            sb.AppendLine($"{indent}    }};");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();

            // Equals(object)
            sb.AppendLine($"{indent}/// <inheritdoc/>");
            sb.AppendLine($"{indent}public override bool Equals(object? obj) => obj is {info.TypeName} other && Equals(other);");
            sb.AppendLine();

            // GetHashCode
            sb.AppendLine($"{indent}/// <inheritdoc/>");
            sb.AppendLine($"{indent}public override int GetHashCode()");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    return _tag switch");
            sb.AppendLine($"{indent}    {{");
            foreach (var v in info.Variants)
            {
                if (v.PayloadType != null)
                {
                    var fieldName = $"_{ToCamelCase(v.Name)}Payload";
                    sb.AppendLine($"{indent}        Kind.{v.Name} => HashCode.Combine(_tag, {fieldName}),");
                }
                else
                {
                    sb.AppendLine($"{indent}        Kind.{v.Name} => HashCode.Combine(_tag),");
                }
            }
            sb.AppendLine($"{indent}        _ => 0");
            sb.AppendLine($"{indent}    }};");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();

            // Operators
            sb.AppendLine($"{indent}/// <summary>Equality operator.</summary>");
            sb.AppendLine($"{indent}public static bool operator ==({info.TypeName} left, {info.TypeName} right) => left.Equals(right);");
            sb.AppendLine();
            sb.AppendLine($"{indent}/// <summary>Inequality operator.</summary>");
            sb.AppendLine($"{indent}public static bool operator !=({info.TypeName} left, {info.TypeName} right) => !left.Equals(right);");
            sb.AppendLine();

            sb.AppendLine($"{indent}#endregion");
        }
    }

    // Using classes instead of records for netstandard2.0 compatibility
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
}
