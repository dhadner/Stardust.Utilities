using Microsoft.CodeAnalysis;

namespace Stardust.Generators;

/// <summary>
/// Diagnostic descriptors for the BitFields source generator.
/// </summary>
internal static class BitFieldsDiagnostics
{
    private const string CATEGORY = "Stardust.BitFields";

    /// <summary>
    /// Error: nint/nuint struct uses bit positions above 31 on a build targeting x86 (32-bit only).
    /// Fields above bit 31 are unreachable because nint/nuint is always 4 bytes on x86.
    /// </summary>
    internal static readonly DiagnosticDescriptor NativeIntExceedsBits32Error = new(
        id: "SD0001",
        title: "BitField exceeds 32-bit nint/nuint range on 32-bit platform",
        messageFormat: "Field '{0}' in '{1}' accesses bit {2} which exceeds the 31-bit maximum for nint/nuint on a 32-bit (x86) build. Move the field to bits 0-31, or change the storage type to ulong/long.",
        category: CATEGORY,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When PlatformTarget is x86, nint/nuint is always 32 bits. Fields or flags that reference bits 32 and above are unreachable and will cause data corruption at runtime.");

    /// <summary>
    /// Warning: nint/nuint struct uses bit positions above 31 on an AnyCPU build.
    /// The struct will work on 64-bit but silently lose data on 32-bit.
    /// </summary>
    internal static readonly DiagnosticDescriptor NativeIntExceedsBits32Warning = new(
        id: "SD0002",
        title: "BitField may exceed 32-bit nint/nuint range on AnyCPU build",
        messageFormat: "Field '{0}' in '{1}' accesses bit {2} which exceeds the 31-bit limit of nint/nuint on 32-bit platforms. This struct will silently lose data if run on a 32-bit process. Consider using ulong/long, or restrict the build to x64.",
        category: CATEGORY,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When PlatformTarget is AnyCPU (or unset), the binary may run on 32-bit platforms where nint/nuint is 32 bits. Fields or flags above bit 31 will be unreachable, causing silent data corruption.");

    /// <summary>
    /// Error: The storage type passed to [BitFields(typeof(T))] is not a supported type.
    /// The generator only supports a fixed set of primitive/numeric types.
    /// </summary>
    internal static readonly DiagnosticDescriptor UnsupportedStorageType = new(
        id: "SD0003",
        title: "Unsupported BitFields storage type",
        messageFormat: "Type '{0}' is not a supported storage type for [BitFields] on struct '{1}'. Supported types: byte, sbyte, short, ushort, int, uint, long, ulong, nint, nuint, Half, float, double, decimal, UInt128, Int128.",
        category: CATEGORY,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The [BitFields] attribute requires a specific storage type that the generator knows how to manipulate at the bit level. Use one of the supported primitive types, or specify an integer bit count for arbitrary-size bitfields.");

    /// <summary>
    /// Error: A property with [BitField] or [BitFlag] is not declared as partial.
    /// The source generator emits a partial property implementation, so the user's
    /// declaration must also be partial. Without it the compiler produces confusing
    /// CS9248 and CS0102 errors from the generated code instead of pointing at the
    /// real problem in the user's source file.
    /// </summary>
    internal static readonly DiagnosticDescriptor PropertyMustBePartial = new(
        id: "SD0004",
        title: "BitField/BitFlag property must be declared partial",
        messageFormat: "Property '{0}' in '{1}' has a [{2}] attribute but is not declared 'partial'; add the 'partial' keyword to the declaration: partial {3} {0} {{ get; set; }}",
        category: CATEGORY,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Properties decorated with [BitField] or [BitFlag] must include the 'partial' keyword so the source generator can provide the implementation. Without 'partial', the compiler produces confusing CS9248 or CS0102 errors from the generated file instead of identifying the actual problem.");

    /// <summary>
    /// Warning: The two-parameter [BitField(startBit, endBit)] constructor is deprecated
    /// and will be removed before v1.0. Users should migrate to named property syntax.
    /// </summary>
    internal static readonly DiagnosticDescriptor DeprecatedPositionalEndBit = new(
        id: "SD0015",
        title: "Deprecated two-parameter BitField constructor",
        messageFormat: "The two-parameter [BitField({1}, {2})] constructor on property '{0}' is deprecated and will be removed before v1.0. Use [BitField({1}, EndBit = {2})] or [BitField({1}, Width = {3})].",
        category: CATEGORY,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The positional 'endBit' parameter is easily confused with a bit count. Use the named 'EndBit' or 'Width' property syntax instead, which is self-documenting and unambiguous.");

    /// <summary>
    /// Warning: Both EndBit and Width are specified and they are consistent (redundant).
    /// The field is still valid but the user should remove one.
    /// </summary>
    internal static readonly DiagnosticDescriptor RedundantEndBitAndWidth = new(
        id: "SD0016",
        title: "Redundant EndBit and Width on BitField",
        messageFormat: "Property '{0}' specifies both 'EndBit' ({1}) and 'Width' ({2}), which is redundant. Use one or the other.",
        category: CATEGORY,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Specifying both EndBit and Width is redundant when they are consistent. Remove one to keep the declaration concise.");

    /// <summary>
    /// Error: Both EndBit and Width are specified but they are inconsistent (contradictory).
    /// The generator cannot determine intent, so the field is skipped.
    /// </summary>
    internal static readonly DiagnosticDescriptor InconsistentEndBitAndWidth = new(
        id: "SD0017",
        title: "Inconsistent EndBit and Width on BitField",
        messageFormat: "Property '{0}' specifies 'EndBit' ({1}) and 'Width' ({2}) but they are inconsistent (EndBit - StartBit + 1 = {3}, not {2}). Use one or the other.",
        category: CATEGORY,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When both EndBit and Width are specified, they must agree. The expected width is EndBit - StartBit + 1. Remove one or correct the values.");

    /// <summary>
    /// Error: A [BitField] attribute has a StartBit but neither EndBit nor Width is specified.
    /// </summary>
    internal static readonly DiagnosticDescriptor MissingEndBitOrWidth = new(
        id: "SD0018",
        title: "BitField missing EndBit or Width",
        messageFormat: "Property '{0}' in '{1}' uses [BitField({2})] without specifying 'EndBit' or 'Width'. Use [BitField({2}, EndBit = N)] or [BitField({2}, Width = N)].",
        category: CATEGORY,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A [BitField] attribute requires either an EndBit (inclusive end position) or Width (bit count) to define the field range.");

    /// <summary>
    /// Error: A [BitField] attribute has EndBit or Width but no StartBit.
    /// </summary>
    internal static readonly DiagnosticDescriptor MissingStartBit = new(
        id: "SD0019",
        title: "BitField missing StartBit",
        messageFormat: "Property '{0}' in '{1}' has a [BitField] attribute without a StartBit value. Use [BitField(startBit, EndBit = N)] or [BitField(StartBit = N, Width = N)].",
        category: CATEGORY,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A [BitField] attribute requires a StartBit value. Provide it as a positional argument or as a named property.");
}
