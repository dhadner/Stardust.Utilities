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
    /// Info: The two-parameter [BitField(start, end)] constructor is confusing because the second parameter is an end bit position, but can be easily mistaken for a bit width. 
    /// This can lead to unexpected errors and misunderstandings about the field range. The generator will still process this syntax,
    /// and if the brevity is desired, disable the SD0015 message via .editorconfig or NoWarn.
    /// </summary>
    internal static readonly DiagnosticDescriptor ConfusingPositionalEndBit = new(
        id: "SD0015",
        title: "Potentially misunderstood two-parameter BitField constructor",
        messageFormat: "The two-parameter [BitField({1}, {2})] constructor on property '{0}' is potentially misunderstood. The second parameter '{2}' is the end bit position, but can be confused with 'Width' in bits.  Use [BitField({1}, End = {2})] or [BitField({1}, Width = {3})] for additional clarity.",
        category: CATEGORY,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The positional 'end' parameter is easily confused with a bit count. Use the named 'End' or 'Width' property syntax instead, which is self-documenting and unambiguous.");

    /// <summary>
    /// Warning: Both End and Width are specified and they are consistent (redundant).
    /// The field is still valid but the user should remove one.
    /// </summary>
    internal static readonly DiagnosticDescriptor RedundantEndBitAndWidth = new(
        id: "SD0016",
        title: "Redundant End and Width on BitField",
        messageFormat: "Property '{0}' specifies both 'End' ({1}) and 'Width' ({2}), which is redundant. Use one or the other.",
        category: CATEGORY,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Specifying both End and Width is redundant when they are consistent. Remove one to keep the declaration concise.");

    /// <summary>
    /// Error: Both End and Width are specified but they are inconsistent (contradictory).
    /// The generator cannot determine intent, so the field is skipped.
    /// </summary>
    internal static readonly DiagnosticDescriptor InconsistentEndBitAndWidth = new(
        id: "SD0017",
        title: "Inconsistent End and Width on BitField",
        messageFormat: "Property '{0}' specifies 'End' ({1}) and 'Width' ({2}) but they are inconsistent (End - Start + 1 = {3}, not {2}). Use one or the other.",
        category: CATEGORY,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When both End and Width are specified, they must agree. The expected width is End - Start + 1. Remove one or correct the values.");

    /// <summary>
    /// Error: A [BitField] attribute has a Start but neither End nor Width is specified.
    /// </summary>
    internal static readonly DiagnosticDescriptor MissingEndBitOrWidth = new(
        id: "SD0018",
        title: "BitField missing End or Width",
        messageFormat: "Property '{0}' in '{1}' uses [BitField({2})] without specifying 'End' or 'Width'. Use [BitField({2}, End = N)] or [BitField({2}, Width = N)].",
        category: CATEGORY,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A [BitField] attribute requires either an End (inclusive end position) or Width (bit count) to define the field range.");

    /// <summary>
    /// Error: A [BitField] attribute has End or Width but no Start.
    /// </summary>
    internal static readonly DiagnosticDescriptor MissingStartBit = new(
        id: "SD0019",
        title: "BitField missing Start",
        messageFormat: "Property '{0}' in '{1}' has a [BitField] attribute without a Start value. Use [BitField(start, End = N)] or [BitField(Start = N, Width = N)].",
        category: CATEGORY,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A [BitField] attribute requires a Start value. Provide it as a positional argument or as a named property.");

    /// <summary>
    /// Error: A floating-point or opaque property type (Half, float, double, decimal) is
    /// used with a field width that does not match the type's exact bit size.  Because
    /// these types are stored as opaque bit patterns, any width mismatch silently corrupts
    /// the value.
    /// </summary>
    internal static readonly DiagnosticDescriptor FloatPropertyWidthMismatch = new(
        id: "SD0020",
        title: "BitField width does not match floating-point/decimal property type size",
        messageFormat: "Property '{0}' in '{1}' has type '{2}' ({3} bits) but the field width is {4} bits. Floating-point and decimal types require an exact bit-width match because they are stored as opaque bit patterns.",
        category: CATEGORY,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Half requires exactly 16 bits, float requires 32 bits, double requires 64 bits, and decimal requires 128 bits. A mismatched width could silently corrupt the value.");

    /// <summary>
    /// Error: A property type is a [BitFields] struct whose declared bit width does not
    /// match the [BitField(start, End)] width allocated in the parent struct.
    /// The full storage must survive the round-trip; partial embedding silently truncates data.
    /// </summary>
    internal static readonly DiagnosticDescriptor EmbeddedStructWidthMismatch = new(
        id: "SD0021",
        title: "BitField width does not match embedded BitFields struct size",
        messageFormat: "Property '{0}' in '{1}' has type '{2}' ({3} bits) but the field width is {4} bits. Embedded [BitFields] structs require an exact bit-width match to avoid silent data truncation.",
        category: CATEGORY,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When a [BitFields] struct is used as a property type, the field width must exactly match the embedded type's declared bit width. Use [BitFields(N)] to declare a custom-width struct, or adjust the field range to match the embedded type's storage width.");

    /// <summary>
    /// Error: A record struct view ([BitFieldsView] or [BitFields] on a record struct)
    /// cannot be embedded as a property in a value-type [BitFields] struct.
    /// Views are backed by Memory&lt;byte&gt; and cannot be stored in a single integer value.
    /// </summary>
    internal static readonly DiagnosticDescriptor CannotEmbedViewInValueType = new(
        id: "SD0022",
        title: "Cannot embed a view record struct in a value-type BitFields struct",
        messageFormat: "Property '{0}' in '{1}' has type '{2}' which is a Memory<byte>-backed view (record struct). Views cannot be embedded in value-type [BitFields] structs. Use a [BitFields] value-type struct instead.",
        category: CATEGORY,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Record struct views are backed by Memory<byte> and require a buffer reference. They cannot be stored within a single integer backing field. Declare the embedded type as a [BitFields] value-type struct (not a record struct) instead.");

    /// <summary>
    /// Error: A multi-word [BitFields] struct (UInt128, Int128, decimal, or N &gt; 64)
    /// cannot be embedded in a single-word value-type parent. The parent struct's backing
    /// integer is too narrow to hold the embedded type's bits.
    /// </summary>
    internal static readonly DiagnosticDescriptor CannotEmbedMultiWordInSingleWord = new(
        id: "SD0023",
        title: "Cannot embed multi-word BitFields struct in a single-word value-type struct",
        messageFormat: "Property '{0}' in '{1}' has type '{2}' ({3} bits) which is a multi-word [BitFields] struct. It cannot be embedded in a single-word (≤ 64-bit) value-type struct. Use a multi-word parent ([BitFields(N)] where N ≥ {3}) or a record struct view instead.",
        category: CATEGORY,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Multi-word BitFields structs (UInt128, Int128, decimal, or [BitFields(N)] where N > 64) use multiple ulong words for storage and cannot be represented within a single-word integer backing field. Embed them in a multi-word parent struct or a record struct view instead.");
}
