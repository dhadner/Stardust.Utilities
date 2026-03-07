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
}
