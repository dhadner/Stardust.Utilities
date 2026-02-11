using System.Globalization;
using System.Resources;

namespace Stardust.Utilities;

/// <summary>
/// Describes a single field or flag within a bitfield struct, providing its name,
/// bit position, width, type, effective endianness, and optional description at runtime.
/// </summary>
/// <param name="Name">The property name of the field.</param>
/// <param name="StartBit">The starting bit position (inclusive, 0-based, as declared by the user).</param>
/// <param name="BitLength">The number of bits in the field (1 for flags).</param>
/// <param name="PropertyType">The fully qualified CLR type name of the property (e.g., "byte", "bool", "ushort").</param>
/// <param name="IsFlag">True if this is a single-bit <see cref="BitFlagAttribute"/> flag; false for a <see cref="BitFieldAttribute"/> field.</param>
/// <param name="ByteOrder">The effective byte order for this field (struct-level default or per-field override).</param>
/// <param name="BitOrder">The effective bit ordering for this field (struct-level).</param>
/// <param name="Description">
/// An optional description string or resource key.
/// When <paramref name="DescriptionResourceType"/> is null, this is a literal string.
/// When set, this is a resource key resolved by <see cref="GetDescription(CultureInfo?)"/>.
/// </param>
/// <param name="DescriptionResourceType">
/// When set, <paramref name="Description"/> is treated as a resource key and resolved
/// via this type's static <c>ResourceManager</c> property.
/// </param>
public sealed record BitFieldInfo(
    string Name,
    int StartBit,
    int BitLength,
    string PropertyType,
    bool IsFlag,
    ByteOrder ByteOrder = ByteOrder.LittleEndian,
    BitOrder BitOrder = BitOrder.BitZeroIsLsb,
    string? Description = null,
    Type? DescriptionResourceType = null)
{
    /// <summary>The ending bit position (inclusive, 0-based, as declared by the user).</summary>
    public int EndBit => StartBit + BitLength - 1;

    /// <summary>
    /// Returns the resolved description string. When <see cref="DescriptionResourceType"/> is set,
    /// looks up <see cref="Description"/> as a resource key using the type's <c>ResourceManager</c>.
    /// Otherwise returns <see cref="Description"/> as a literal string.
    /// </summary>
    /// <param name="culture">
    /// The culture to use for resource lookup. Defaults to <see cref="CultureInfo.CurrentUICulture"/> when null.
    /// </param>
    /// <returns>The resolved description, or null if no description was specified.</returns>
    public string? GetDescription(CultureInfo? culture = null)
    {
        if (Description is null)
            return null;

        if (DescriptionResourceType is null)
            return Description;

        var prop = DescriptionResourceType.GetProperty(
            "ResourceManager",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        if (prop?.GetValue(null) is ResourceManager rm)
            return rm.GetString(Description, culture ?? CultureInfo.CurrentUICulture) ?? Description;

        return Description;
    }
}
