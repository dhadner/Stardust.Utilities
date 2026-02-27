using System.Globalization;
using System.Resources;

namespace Stardust.Utilities;
using static Result<string>;

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
/// <param name="StructTotalBits">
/// The total number of bits in the containing struct (e.g., 16 for <c>typeof(ushort)</c>,
/// 256 for <c>[BitFields(256)]</c>). Used by diagram renderers to show undefined trailing bits.
/// </param>
/// <param name="FieldMustBe">
/// Per-field MustBe constraint: Any = no constraint, Zero = must be zero, Ones = must be all ones.
/// </param>
/// <param name="StructUndefinedMustBe">
/// Struct-level UndefinedBitsMustBe: Any = any, Zeroes = zeroes, Ones = ones.
/// </param>
/// <param name="StructDescription">
/// An optional description of the containing struct, from the <c>[BitFields]</c> or <c>[BitFieldsView]</c>
/// attribute's <c>Description</c> property. Used as a section label in multi-struct diagram rendering.
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
    Type? DescriptionResourceType = null,
    int StructTotalBits = 0,
    MustBe FieldMustBe = MustBe.Any,
    UndefinedBitsMustBe StructUndefinedMustBe = UndefinedBitsMustBe.Any,
    string? StructDescription = null)
{
    public static Result<BitFieldInfo, string> Create(Type type, string? field = null, bool inherit = true)
    {
        string name;
        int startBit;
        int bitLength;
        string propertyType;
        bool isFlag;
        ByteOrder byteOrder = ByteOrder.LittleEndian;
        BitOrder bitOrder = BitOrder.BitZeroIsLsb;
        string? description = null;
        Type? descriptionResourceType = null;
        int structTotalBits;
        MustBe fieldMustBe = MustBe.Any;
        UndefinedBitsMustBe structUndefinedMustBe = UndefinedBitsMustBe.Any;
        string? structDescription = null;

        if (type == null)
        {
            return Err("Type cannot be null.");
        }

        if (!type.IsBitsType())
        {
            return Err($"Type '{type.FullName}' is not a valid bitfield struct type.");
        }
        var fieldInfo = field == null ? null : type.GetProperty(field);
        if (field != null && fieldInfo == null)
        {
            return Err($"Field '{field}' not found in type '{type.FullName}'.");
        }

        // At this point, we have a valid type and (if specified) a valid field.
        // We can proceed to extract all the relevant information.

        // Description is the same for either struct or field
        var descRes = type.GetBitsDescription(field, inherit).Match(
            onSuccess: desc => {
                description = desc.description;
                descriptionResourceType = desc.descriptionResourceType;
                return Ok(desc);
            },
            onFailure: error => Err(error)
        );
        if (descRes.IsFailure) return Err(descRes.Error);
        if (field == null)
        {
            // For struct-level description, also set structDescription for convenience
            structDescription = description;
        }
        else
        {
            // For field-level description, get struct-level description separately for convenience
            var fldDescRes = type.GetBitsDescription(null, inherit).Match(
                onSuccess: desc => {
                    structDescription = desc.description;
                    return Ok(desc);
                },
                onFailure: error => Err(error)
            );
            if (fldDescRes.IsFailure) return Err(fldDescRes.Error);
        }
        var byteOrderRes = type.GetBitAndByteOrder(inherit);

        // Undefined bits defined at the struct level.
        type.GetUndefinedBitsMustBe(inherit).Match(
            onSuccess: mustBe => structUndefinedMustBe = mustBe,
            onFailure: _ => structUndefinedMustBe = UndefinedBitsMustBe.Any
        );
        var bitLengthRes = type.GetBitLength(field, inherit);
        if (bitLengthRes.IsFailure) return Err(bitLengthRes.Error);
        bitLength = bitLengthRes.Value;

        if (field != null)
        {
            var structTotalBitsRes = type.GetBitLength(null, inherit);
            if (structTotalBitsRes.IsFailure) return Err(structTotalBitsRes.Error);
            structTotalBits = structTotalBitsRes.Value;
        }
        else
        {
            structTotalBits = bitLength;
        }

        if (field != null)
        {
            name = field;
            propertyType = fieldInfo!.PropertyType.FullName ?? fieldInfo.PropertyType.Name;

            var seBitsRes = type.GetStartAndEndBits(field, inherit);
            if (seBitsRes.IsFailure) return Err(seBitsRes.Error);

            startBit = seBitsRes.Value.startBit;
            bitLength = seBitsRes.Value.endBit - startBit + 1;
            type.GetFieldValueOverride(field, inherit).OnSuccess(mustBe => fieldMustBe = mustBe);
            var fieldAttr = type.GetAttribute<BitFieldAttribute>(field, inherit);
            if (fieldAttr != null)
            {
                isFlag = false;
            }
            else
            {
                var flagAttr = type.GetAttribute<BitFlagAttribute>(field, inherit);
                if (flagAttr != null)
                {
                    isFlag = true;
                }
                else
                {
                    return Err($"Field '{field}' in type '{type.FullName}' is missing both [BitField] and [BitFlag] attributes.");
                }
            }
            structDescription = description;
        }
        else
        {
            // Dealing with the struct itself
            name = type.Name;
            propertyType = type.FullName ?? type.Name;
            startBit = 0;
            isFlag = false;
        }

        return Ok(new BitFieldInfo(
            Name: name,
            StartBit: startBit,
            BitLength: bitLength,
            PropertyType: propertyType,
            IsFlag: isFlag,
            ByteOrder: byteOrder,
            BitOrder: bitOrder,
            Description: description,
            DescriptionResourceType: descriptionResourceType,
            StructTotalBits: structTotalBits,
            FieldMustBe: fieldMustBe,
            StructUndefinedMustBe: structUndefinedMustBe,
            StructDescription: structDescription
            ));
    }

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
