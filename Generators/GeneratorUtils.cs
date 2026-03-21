using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Stardust.Generators;

/// <summary>
/// Shared utility methods used by all BitFields source generators.
/// </summary>
internal static class GeneratorUtils
{
    /// <summary>
    /// Escapes a string value so it can be safely embedded in a C# string literal
    /// (regular quoted string, not verbatim). Handles all characters that would break
    /// compilation or alter semantics: backslash, quote, newline, carriage return, tab,
    /// null, and all other control characters (U+0000..U+001F, U+007F..U+009F).
    /// </summary>
    internal static string EscapeStringLiteral(string value)
    {
        var sb = new System.Text.StringBuilder(value.Length + 8);
        foreach (char c in value)
        {
            switch (c)
            {
                case '\\': sb.Append(@"\\"); break;
                case '"':  sb.Append(@"\"""); break;
                case '\n': sb.Append(@"\n"); break;
                case '\r': sb.Append(@"\r"); break;
                case '\t': sb.Append(@"\t"); break;
                case '\0': sb.Append(@"\0"); break;
                case '\a': sb.Append(@"\a"); break;
                case '\b': sb.Append(@"\b"); break;
                case '\f': sb.Append(@"\f"); break;
                case '\v': sb.Append(@"\v"); break;
                default:
                    // Escape any remaining control characters as \uXXXX
                    if (char.IsControl(c))
                        sb.Append($"\\u{(int)c:X4}");
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Returns true if the given property symbol is declared with the <c>partial</c> keyword.
    /// Used by both <see cref="BitFieldsGenerator"/> and <see cref="BitFieldsViewGenerator"/>
    /// to detect non-partial properties and emit SD0004 diagnostics.
    /// </summary>
    internal static bool IsPartialProperty(IPropertySymbol property)
    {
        foreach (var syntaxRef in property.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is PropertyDeclarationSyntax propertySyntax &&
                propertySyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Result of resolving [BitField] attribute parameters across all constructor forms.
    /// </summary>
    internal readonly struct BitFieldResolveResult
    {
        public int Start { get; }
        public int End { get; }
        public int Width { get; }
        public MustBe ValueOverride { get; }

        public BitFieldResolveResult(int start, int end, int width, MustBe valueOverride)
        {
            Start = start;
            End = end;
            Width = width;
            ValueOverride = valueOverride;
        }
    }

    /// <summary>
    /// Resolves [BitField] attribute parameters across all constructor forms
    /// (parameterless, single-param, deprecated two-param) and produces any
    /// diagnostics for deprecated, redundant, or invalid usage.
    /// </summary>
    /// <returns>
    /// A resolved result (null if errors prevent resolution) and a list of diagnostics.
    /// Warning-severity diagnostics are reported but the field is still valid.
    /// Error-severity diagnostics indicate the field should be skipped.
    /// </returns>
    internal static (BitFieldResolveResult? result, List<PropertyDiagnosticInfo> diagnostics) ResolveBitFieldAttribute(
        AttributeData attr, string propertyName, string structName, Location? location)
    {
        var diagnostics = new List<PropertyDiagnosticInfo>();

        // Determine which constructor was used by checking the second parameter type.
        // The deprecated 2-param ctor has (int start, int end, ...) where the 2nd param is System.Int32.
        // The 1-param ctor has (int start, MustBe mustBe = ...) where the 2nd param is MustBe (enum).
        var ctor = attr.AttributeConstructor;
        bool isDeprecated2ParamCtor = ctor != null && ctor.Parameters.Length >= 2
            && ctor.Parameters[1].Type.SpecialType == SpecialType.System_Int32;

        int start = -1;
        int ctorEndBit = -1;
        var valueOverride = MustBe.Any;

        if (isDeprecated2ParamCtor)
        {
            // Deprecated 2-param constructor: (int start, int end, MustBe mustBe = MustBe.Any)
            start = (int)(attr.ConstructorArguments[0].Value ?? 0);
            ctorEndBit = (int)(attr.ConstructorArguments[1].Value ?? 0);
            if (attr.ConstructorArguments.Length >= 3 && attr.ConstructorArguments[2].Value is int mb2)
                valueOverride = (MustBe)mb2;
        }
        else if (attr.ConstructorArguments.Length >= 1)
        {
            // 1-param constructor: (int start, MustBe mustBe = MustBe.Any)
            start = (int)(attr.ConstructorArguments[0].Value ?? 0);
            if (attr.ConstructorArguments.Length >= 2 && attr.ConstructorArguments[1].Value is int mb1)
                valueOverride = (MustBe)mb1;
        }
        // else: 0-param constructor, start remains -1

        // Read named arguments (these override/supplement constructor values)
        int namedEndBit = -1;
        int namedWidth = -1;

        foreach (var named in attr.NamedArguments)
        {
            switch (named.Key)
            {
                case "Start" when named.Value.Value is int sb:
                    start = sb;
                    break;
                case "End" when named.Value.Value is int eb:
                    namedEndBit = eb;
                    break;
                case "Width" when named.Value.Value is int w:
                    namedWidth = w;
                    break;
                case "ValueOverride" when named.Value.Value is int vo:
                    valueOverride = (MustBe)vo;
                    break;
            }
        }

        // SD0019: No Start specified
        if (start < 0)
        {
            diagnostics.Add(new PropertyDiagnosticInfo(
                BitFieldsDiagnostics.MissingStartBit, location, propertyName, structName));
            return (null, diagnostics);
        }

        // Resolve End/Width
        int resolvedEndBit;
        int resolvedWidth;

        if (isDeprecated2ParamCtor)
        {
            // Named End overrides constructor end if present
            int effectiveEndBit = namedEndBit >= 0 ? namedEndBit : ctorEndBit;
            int effectiveWidth = effectiveEndBit - start + 1;

            // SD0015: deprecated constructor
            diagnostics.Add(new PropertyDiagnosticInfo(
                BitFieldsDiagnostics.DeprecatedPositionalEndBit, location,
                propertyName, start, effectiveEndBit, effectiveWidth));

            // Check if Width is also specified
            if (namedWidth >= 0)
            {
                if (effectiveWidth == namedWidth)
                {
                    diagnostics.Add(new PropertyDiagnosticInfo(
                        BitFieldsDiagnostics.RedundantEndBitAndWidth, location,
                        propertyName, effectiveEndBit, namedWidth));
                }
                else
                {
                    diagnostics.Add(new PropertyDiagnosticInfo(
                        BitFieldsDiagnostics.InconsistentEndBitAndWidth, location,
                        propertyName, effectiveEndBit, namedWidth, effectiveWidth));
                    return (null, diagnostics);
                }
            }

            resolvedEndBit = effectiveEndBit;
            resolvedWidth = effectiveWidth;
        }
        else
        {
            // 0-param or 1-param constructor: End/Width come from named arguments
            bool hasEndBit = namedEndBit >= 0;
            bool hasWidth = namedWidth >= 0;

            if (hasEndBit && hasWidth)
            {
                int expectedWidth = namedEndBit - start + 1;
                if (expectedWidth == namedWidth)
                {
                    diagnostics.Add(new PropertyDiagnosticInfo(
                        BitFieldsDiagnostics.RedundantEndBitAndWidth, location,
                        propertyName, namedEndBit, namedWidth));
                    resolvedEndBit = namedEndBit;
                    resolvedWidth = namedWidth;
                }
                else
                {
                    diagnostics.Add(new PropertyDiagnosticInfo(
                        BitFieldsDiagnostics.InconsistentEndBitAndWidth, location,
                        propertyName, namedEndBit, namedWidth, expectedWidth));
                    return (null, diagnostics);
                }
            }
            else if (hasEndBit)
            {
                resolvedEndBit = namedEndBit;
                resolvedWidth = namedEndBit - start + 1;
            }
            else if (hasWidth)
            {
                resolvedWidth = namedWidth;
                resolvedEndBit = start + namedWidth - 1;
            }
            else
            {
                // SD0018: Neither End nor Width specified
                diagnostics.Add(new PropertyDiagnosticInfo(
                    BitFieldsDiagnostics.MissingEndBitOrWidth, location, propertyName, structName, start));
                return (null, diagnostics);
            }
        }

        return (new BitFieldResolveResult(start, resolvedEndBit, resolvedWidth, valueOverride), diagnostics);
    }
}
