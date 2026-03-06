namespace Stardust.Generators;

public partial class BitFieldsGenerator
{
    /// <summary>
    /// Returns the BitConverter method name that converts a floating-point value to its raw bits.
    /// E.g., "BitConverter.HalfToUInt16Bits" for Half.
    /// </summary>
    private static string ToBitsMethod(string floatingPointType) => floatingPointType switch
    {
        "Half" => "BitConverter.HalfToUInt16Bits",
        "float" => "BitConverter.SingleToUInt32Bits",
        "double" => "BitConverter.DoubleToUInt64Bits",
        _ => throw new System.ArgumentException($"Unknown floating-point type: {floatingPointType}")
    };

    /// <summary>
    /// Returns the BitConverter method name that converts raw bits to a floating-point value.
    /// E.g., "BitConverter.UInt16BitsToHalf" for Half.
    /// </summary>
    private static string FromBitsMethod(string floatingPointType) => floatingPointType switch
    {
        "Half" => "BitConverter.UInt16BitsToHalf",
        "float" => "BitConverter.UInt32BitsToSingle",
        "double" => "BitConverter.UInt64BitsToDouble",
        _ => throw new System.ArgumentException($"Unknown floating-point type: {floatingPointType}")
    };

    /// <summary>
    /// Formats a value as a hex literal appropriate for the storage type.
    /// For signed types, uses unchecked cast from unsigned to avoid overflow issues.
    /// </summary>
    private static string FormatHex(ulong value, string storageType)
    {
        return storageType switch
        {
            "byte" => $"0x{(byte)value:X2}",
            "ushort" => $"0x{(ushort)value:X4}",
            "uint" => $"0x{(uint)value:X8}U",
            "ulong" => $"0x{value:X16}UL",
            // For signed types, we need unchecked casts because mask values can exceed signed max
            "sbyte" => $"unchecked((sbyte)0x{(byte)value:X2})",
            "short" => $"unchecked((short)0x{(ushort)value:X4})",
            "int" => $"unchecked((int)0x{(uint)value:X8}U)",
            "long" => $"unchecked((long)0x{value:X16}UL)",
            _ => $"0x{value:X}"
        };
    }

    /// <summary>
    /// Determines if a type name represents a signed integer type.
    /// </summary>
    private static bool IsSignedType(string typeName)
    {
        return typeName == "sbyte" || typeName == "short" || typeName == "int" || typeName == "long";
    }

    /// <summary>
    /// Gets the bit width of a numeric type.
    /// </summary>
    private static int GetTypeBitWidth(string typeName)
    {
        return typeName switch
        {
            "sbyte" or "byte" => 8,
            "short" or "ushort" => 16,
            "int" or "uint" => 32,
            "long" or "ulong" => 64,
            _ => 32 // Default to int size for unknown types
        };
    }

    /// <summary>
    /// Gets the signed intermediate type to use for sign extension.
    /// For sbyte/short, we use int. For int, we use int. For long, we use long.
    /// </summary>
    private static string GetSignExtendIntermediateType(string propertyType)
    {
        return propertyType switch
        {
            "sbyte" or "short" or "int" => "int",
            "long" => "long",
            _ => "int"
        };
    }

    /// <summary>
    /// Gets the bit width of a storage type.
    /// </summary>
    private static int GetStorageTypeBitWidth(string storageType)
    {
        return storageType switch
        {
            "sbyte" or "byte" => 8,
            "short" or "ushort" => 16,
            "int" or "uint" => 32,
            "long" or "ulong" => 64,
            _ => 32
        };
    }

    /// <summary>
    /// Calculates a bitmask of ALL defined bits (union of all field and flag bit positions).
    /// This handles sparse undefined bits (e.g., bits 0, 3, 7 undefined with gaps).
    /// </summary>
    private static ulong CalculateDefinedBitsMask(BitFieldsInfo info)
    {
        ulong mask = 0;

        foreach (var field in info.Fields)
        {
            ulong fieldMask = ((1UL << field.Width) - 1) << field.Shift;
            mask |= fieldMask;
        }

        foreach (var flag in info.Flags)
        {
            mask |= 1UL << flag.Bit;
        }

        return mask;
    }

    /// <summary>
    /// Calculates combined enforcement masks for the constructor normalizer.
    /// Combines per-field <see cref="MustBe"/> constraints with the struct-level
    /// <see cref="UndefinedBitsMustBe"/> mode into two masks:
    /// <list type="bullet">
    /// <item><c>mustClearMask</c> – bits that must be forced to 0 (AND with <c>~mask</c>).</item>
    /// <item><c>mustSetMask</c> – bits that must be forced to 1 (OR with mask).</item>
    /// </list>
    /// </summary>
    private static (ulong mustClearMask, ulong mustSetMask) CalculateNormalizationMasks(BitFieldsInfo info, ulong undefinedBitsMask)
    {
        ulong clearMask = 0;
        ulong setMask = 0;

        // Per-field MustBe constraints
        foreach (var field in info.Fields)
        {
            ulong fieldMask = ((1UL << field.Width) - 1) << field.Shift;
            if (field.ValueOverride == MustBe.Zero)
                clearMask |= fieldMask;
            else if (field.ValueOverride == MustBe.One)
                setMask |= fieldMask;
        }

        foreach (var flag in info.Flags)
        {
            ulong flagMask = 1UL << flag.Bit;
            if (flag.ValueOverride == MustBe.Zero)
                clearMask |= flagMask;
            else if (flag.ValueOverride == MustBe.One)
                setMask |= flagMask;
        }

        // Struct-level UndefinedBitsMustBe
        if (info.UndefinedBitsMode == UndefinedBitsMustBe.Zeroes)
            clearMask |= undefinedBitsMask;
        else if (info.UndefinedBitsMode == UndefinedBitsMustBe.Ones)
            setMask |= undefinedBitsMask;

        return (clearMask, setMask);
    }

    /// <summary>
    /// Generates a static <c>Fields</c> property that returns a
    /// <see cref="System.ReadOnlySpan{T}"/> of <c>BitFieldInfo</c> describing
    /// every field and flag declared on this struct, using the original user-declared bit positions.
    /// </summary>
    private static void GenerateFieldMetadata(System.Text.StringBuilder sb, BitFieldsInfo info, string indent)
    {
        string structByteOrder = info.ByteOrder == ByteOrder.BigEndian
            ? "ByteOrder.BigEndian" : "ByteOrder.LittleEndian";
        string structBitOrder = "BitOrder.BitZeroIsLsb"; // [BitFields] default; MSB conversion happens internally
        string structDescArg = info.Description != null
            ? $", StructDescription: \"{GeneratorUtils.EscapeStringLiteral(info.Description)}\""
            : "";

        sb.AppendLine($"{indent}/// <summary>Optional description (title) for this struct.</summary>");
        sb.AppendLine($"{indent}public static string? StructDescription => {(info.Description != null ? $"\"{GeneratorUtils.EscapeStringLiteral(info.Description)}\"" : "null")};");
        sb.AppendLine($"{indent}/// <summary>Optional resource type for the struct description.</summary>");
        sb.AppendLine($"{indent}public static Type? StructDescriptionResourceType => {(info.DescriptionResourceType != null ? $"typeof({StripGlobalPrefix(info.DescriptionResourceType.FullName)})" : "null")};");
        sb.AppendLine($"{indent}/// <summary>Metadata for every field and flag declared on this struct, in declaration order.</summary>");
        sb.AppendLine($"{indent}public static ReadOnlySpan<BitFieldInfo> Fields => new BitFieldInfo[]");
        sb.AppendLine($"{indent}{{");

        foreach (var f in info.DeclaredFields)
        {
            var qualifiedType = StripGlobalPrefix(f.PropertyType);
            var descArgs = FormatDescriptionArgs(f.Description, f.DescriptionResourceType);
            sb.AppendLine($"{indent}    new(\"{f.Name}\", {f.Shift}, {f.Width}, \"{qualifiedType}\", false, {structByteOrder}, {structBitOrder}{descArgs}, StructTotalBits: {info.TotalBits}, FieldMustBe: MustBe.{f.ValueOverride}, StructUndefinedMustBe: UndefinedBitsMustBe.{info.UndefinedBitsMode}{structDescArg}),");
        }

        foreach (var f in info.DeclaredFlags)
        {
            var descArgs = FormatDescriptionArgs(f.Description, f.DescriptionResourceType);
            sb.AppendLine($"{indent}    new(\"{f.Name}\", {f.Bit}, 1, \"bool\", true, {structByteOrder}, {structBitOrder}{descArgs}, StructTotalBits: {info.TotalBits}, FieldMustBe: MustBe.{f.ValueOverride}, StructUndefinedMustBe: UndefinedBitsMustBe.{info.UndefinedBitsMode}{structDescArg}),");
        }

        sb.AppendLine($"{indent}}};");
        sb.AppendLine();
    }

    /// <summary>
    /// Formats the optional Description and DescriptionResourceType arguments for a BitFieldInfo constructor call.
    /// Returns an empty string when no description is set, or the trailing named arguments otherwise.
    /// </summary>
    private static string FormatDescriptionArgs(string? description, string? descriptionResourceType)
    {
        if (description is null)
            return "";

        var escaped = GeneratorUtils.EscapeStringLiteral(description);
        if (descriptionResourceType is null)
            return $", \"{escaped}\"";

        var resType = StripGlobalPrefix(descriptionResourceType);
        return $", \"{escaped}\", typeof({resType})";
    }

    /// <summary>
    /// Strips only the <c>global::</c> prefix, preserving the full namespace-qualified type name.
    /// </summary>
    private static string StripGlobalPrefix(string qualifiedName)
    {
        const string globalPrefix = "global::";
        return qualifiedName.StartsWith(globalPrefix)
            ? qualifiedName.Substring(globalPrefix.Length)
            : qualifiedName;
    }

    /// <summary>
    /// Gets the Convert.ToXxx method name for a given storage type.
    /// </summary>
    private static string GetConvertMethodName(string storageType)
    {
        return storageType switch
        {
            "byte" => "Byte",
            "sbyte" => "SByte",
            "short" => "Int16",
            "ushort" => "UInt16",
            "int" => "Int32",
            "uint" => "UInt32",
            "long" => "Int64",
            "ulong" => "UInt64",
            _ => "Int32"
        };
    }

    /// <summary>
    /// Gets the BinaryPrimitives read method name for a given storage type.
    /// Returns null for byte/sbyte since those don't need BinaryPrimitives.
    /// </summary>
    private static string? GetBinaryPrimitivesReadMethod(string storageType, bool bigEndian = false)
    {
        return (storageType, bigEndian) switch
        {
            ("short", false)  => "ReadInt16LittleEndian",
            ("short", true)   => "ReadInt16BigEndian",
            ("ushort", false) => "ReadUInt16LittleEndian",
            ("ushort", true)  => "ReadUInt16BigEndian",
            ("int", false)    => "ReadInt32LittleEndian",
            ("int", true)     => "ReadInt32BigEndian",
            ("uint", false)   => "ReadUInt32LittleEndian",
            ("uint", true)    => "ReadUInt32BigEndian",
            ("long", false)   => "ReadInt64LittleEndian",
            ("long", true)    => "ReadInt64BigEndian",
            ("ulong", false)  => "ReadUInt64LittleEndian",
            ("ulong", true)   => "ReadUInt64BigEndian",
            _ => null
        };
    }

    /// <summary>
    /// Gets the BinaryPrimitives write method name for a given storage type.
    /// Returns null for byte/sbyte since those don't need BinaryPrimitives.
    /// </summary>
    private static string? GetBinaryPrimitivesWriteMethod(string storageType, bool bigEndian = false)
    {
        return (storageType, bigEndian) switch
        {
            ("short", false)  => "WriteInt16LittleEndian",
            ("short", true)   => "WriteInt16BigEndian",
            ("ushort", false) => "WriteUInt16LittleEndian",
            ("ushort", true)  => "WriteUInt16BigEndian",
            ("int", false)    => "WriteInt32LittleEndian",
            ("int", true)     => "WriteInt32BigEndian",
            ("uint", false)   => "WriteUInt32LittleEndian",
            ("uint", true)    => "WriteUInt32BigEndian",
            ("long", false)   => "WriteInt64LittleEndian",
            ("long", true)    => "WriteInt64BigEndian",
            ("ulong", false)  => "WriteUInt64LittleEndian",
            ("ulong", true)   => "WriteUInt64BigEndian",
            _ => null
        };
    }
}
