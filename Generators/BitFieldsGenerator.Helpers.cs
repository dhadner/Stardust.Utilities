namespace Stardust.Generators;

public partial class BitFieldsGenerator
{
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
    /// Calculates the maximum bit index that is defined by any field or flag.
    /// Returns -1 if no fields or flags are defined.
    /// </summary>
    private static int CalculateMaxDefinedBit(BitFieldsInfo info)
    {
        int maxBit = -1;

        foreach (var field in info.Fields)
        {
            int fieldMaxBit = field.Shift + field.Width - 1;
            if (fieldMaxBit > maxBit)
                maxBit = fieldMaxBit;
        }

        foreach (var flag in info.Flags)
        {
            if (flag.Bit > maxBit)
                maxBit = flag.Bit;
        }

        return maxBit;
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
    private static string? GetBinaryPrimitivesReadMethod(string storageType)
    {
        return storageType switch
        {
            "short" => "ReadInt16LittleEndian",
            "ushort" => "ReadUInt16LittleEndian",
            "int" => "ReadInt32LittleEndian",
            "uint" => "ReadUInt32LittleEndian",
            "long" => "ReadInt64LittleEndian",
            "ulong" => "ReadUInt64LittleEndian",
            _ => null
        };
    }

    /// <summary>
    /// Gets the BinaryPrimitives write method name for a given storage type.
    /// Returns null for byte/sbyte since those don't need BinaryPrimitives.
    /// </summary>
    private static string? GetBinaryPrimitivesWriteMethod(string storageType)
    {
        return storageType switch
        {
            "short" => "WriteInt16LittleEndian",
            "ushort" => "WriteUInt16LittleEndian",
            "int" => "WriteInt32LittleEndian",
            "uint" => "WriteUInt32LittleEndian",
            "long" => "WriteInt64LittleEndian",
            "ulong" => "WriteUInt64LittleEndian",
            _ => null
        };
    }
}
