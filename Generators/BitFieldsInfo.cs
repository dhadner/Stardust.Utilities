using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Stardust.Generators;

/// <summary>
/// Mirrors the ByteOrder enum from the public API.
/// </summary>
internal enum ByteOrder
{
    BigEndian = 0,
    LittleEndian = 1
}

/// <summary>
/// Mirrors the BitOrder enum from the public API.
/// </summary>
internal enum BitOrder
{
    BitZeroIsMsb = 0,
    BitZeroIsLsb = 1
}

/// <summary>
/// Mirrors the MustBe enum values from the public API.
/// </summary>
internal enum MustBe
{
    Any = 0,
    Zero = 1,
    One = 2
}

/// <summary>
/// Mirrors the UndefinedBitsMustBe enum values from the public API.
/// </summary>
internal enum UndefinedBitsMustBe
{
    Any = 0,
    Zeroes = 1,
    Ones = 2
}

/// <summary>
/// Describes the storage architecture used by the generated struct.
/// </summary>
internal enum StorageMode
{
    /// <summary>Single native integer field (byte, ushort, uint, ulong, sbyte, short, int, long).</summary>
    NativeInteger,
    /// <summary>Single native float field (float ? uint internally, double ? ulong internally).</summary>
    NativeFloat,
    /// <summary>Multiple ulong fields for arbitrary bit widths.</summary>
    MultiWord,
}

/// <summary>
/// Describes a [BitFields]-attributed struct and its fields/flags.
/// </summary>
internal sealed class BitFieldsInfo
{
    public string TypeName { get; }
    public string? Namespace { get; }
    public string Accessibility { get; }
    /// <summary>The native storage type name (e.g., "byte", "ulong"). Null for MultiWord mode.</summary>
    public string StorageType { get; }
    public bool StorageTypeIsSigned { get; }
    public string UnsignedStorageType { get; }
    public List<BitFieldInfo> Fields { get; }
    public List<BitFlagInfo> Flags { get; }
    /// <summary>
    /// Specifies how undefined bits (bits not covered by any field or flag) are handled.
    /// </summary>
    public UndefinedBitsMustBe UndefinedBitsMode { get; }
    /// <summary>
    /// List of containing types from outermost to innermost (closest to target struct).
    /// Each tuple contains (TypeKind, TypeName, Accessibility).
    /// </summary>
    public List<(string Kind, string Name, string Accessibility)> ContainingTypes { get; }
    /// <summary>The storage architecture.</summary>
    public StorageMode Mode { get; }
    /// <summary>Number of 64-bit words (for MultiWord mode). 1 for native types.</summary>
    public int WordCount { get; }
    /// <summary>Total number of bits in the struct.</summary>
    public int TotalBits { get; }
    /// <summary>The user-facing floating-point type ("float" or "double"). Null for non-FP modes.</summary>
    public string? FloatingPointType { get; }
    /// <summary>If non-null, the multi-word struct generates implicit conversions to/from this native wide type ("UInt128" or "Int128").</summary>
    public string? NativeWideType { get; }
    /// <summary>The byte order for serialization (ReadFrom/WriteTo). Default is LittleEndian.</summary>
    public ByteOrder ByteOrder { get; }
    /// <summary>Whether generated code needs to cast Value through UnsignedStorageType for bit operations.
    /// True for signed types and for nint/nuint (whose UnsignedStorageType is ulong).</summary>
    public bool NeedsUnsignedCast => StorageTypeIsSigned || UnsignedStorageType != StorageType;
    /// <summary>True when the storage type is nint or nuint (platform-dependent width).</summary>
    public bool IsNativeIntegerType => StorageType == "nint" || StorageType == "nuint";
    /// <summary>The source location of the struct declaration, for diagnostic reporting.</summary>
    public Location? Location { get; }
    /// <summary>Optional struct-level description from the [BitFields] attribute.</summary>
    public string? Description { get; }
    /// <summary>
    /// An optional resource type for the Description property, allowing localization of struct descriptions in BitFieldDiagram.
    /// </summary>
    public Type? DescriptionResourceType { get; set; }
    /// <summary>
    /// Fields with original user-declared bit positions (before any MSB-to-LSB conversion).
    /// Used for metadata generation only.
    /// </summary>
    public List<BitFieldInfo> DeclaredFields { get; }
    /// <summary>
    /// Flags with original user-declared bit positions (before any MSB-to-LSB conversion).
    /// Used for metadata generation only.
    /// </summary>
    public List<BitFlagInfo> DeclaredFlags { get; }

    public BitFieldsInfo(string typeName, string? ns, string accessibility, string storageType, bool storageTypeIsSigned, string unsignedStorageType, List<BitFieldInfo> fields, List<BitFlagInfo> flags, List<(string Kind, string Name, string Accessibility)> containingTypes, UndefinedBitsMustBe undefinedBitsMode = UndefinedBitsMustBe.Any, StorageMode mode = StorageMode.NativeInteger, int wordCount = 1, int totalBits = 0, string? floatingPointType = null, string? nativeWideType = null, ByteOrder byteOrder = ByteOrder.LittleEndian, List<BitFieldInfo>? declaredFields = null, List<BitFlagInfo>? declaredFlags = null, string? description = null, Type? descriptionResourceType = null, Location? location = null)
    {
        TypeName = typeName;
        Namespace = ns;
        Accessibility = accessibility;
        StorageType = storageType;
        UnsignedStorageType = unsignedStorageType;
        StorageTypeIsSigned = storageTypeIsSigned;
        Fields = fields;
        Flags = flags;
        ContainingTypes = containingTypes;
        UndefinedBitsMode = undefinedBitsMode;
        Mode = mode;
        WordCount = wordCount;
        TotalBits = totalBits;
        FloatingPointType = floatingPointType;
        NativeWideType = nativeWideType;
        ByteOrder = byteOrder;
        DeclaredFields = declaredFields ?? fields;
        DeclaredFlags = declaredFlags ?? flags;
        Description = description;
        DescriptionResourceType = descriptionResourceType;
        Location = location;
    }
}

/// <summary>
/// Describes a [BitField] property (multi-bit field with shift and width).
/// </summary>
internal sealed class BitFieldInfo
{
    public string Name { get; }
    public string PropertyType { get; }
    /// <summary>
    /// The underlying native CLR type used for BinaryPrimitives read/write and cast logic.
    /// For endian-aware types (e.g., UInt32Be -> uint), this differs from <see cref="PropertyType"/>.
    /// For plain types, this equals <see cref="PropertyType"/>.
    /// </summary>
    public string NativeType { get; }
    public int Shift { get; }
    public int Width { get; }
    /// <summary>
    /// Override for this specific field's bits.
    /// </summary>
    public MustBe ValueOverride { get; }
    /// <summary>
    /// Per-field byte order override inferred from endian-aware property types
    /// (e.g., UInt32Be forces BigEndian). Null means use the struct-level default.
    /// </summary>
    public ByteOrder? FieldByteOrder { get; }
    /// <summary>Optional description string or resource key.</summary>
    public string? Description { get; }
    /// <summary>Fully qualified type name for resource lookup, or null for literal descriptions.</summary>
    public string? DescriptionResourceType { get; }

    /// <summary>The source location of the property declaration, for diagnostic reporting.</summary>
    public Location? Location { get; }

    public BitFieldInfo(string name, string propertyType, int shift, int width, MustBe valueOverride = MustBe.Any, ByteOrder? fieldByteOrder = null, string? nativeType = null, string? description = null, string? descriptionResourceType = null, Location? location = null)
    {
        Name = name;
        PropertyType = propertyType;
        NativeType = nativeType ?? propertyType;
        Shift = shift;
        Width = width;
        ValueOverride = valueOverride;
        FieldByteOrder = fieldByteOrder;
        Description = description;
        DescriptionResourceType = descriptionResourceType;
        Location = location;
    }
}

/// <summary>
/// Describes a [BitFlag] property (single-bit boolean flag).
/// </summary>
internal sealed class BitFlagInfo
{
    public string Name { get; }
    public int Bit { get; }
    /// <summary>
    /// Override for this specific flag's bit.
    /// </summary>
    public MustBe ValueOverride { get; }
    /// <summary>Optional description string or resource key.</summary>
    public string? Description { get; }
    /// <summary>Fully qualified type name for resource lookup, or null for literal descriptions.</summary>
    public string? DescriptionResourceType { get; }

    /// <summary>The source location of the property declaration, for diagnostic reporting.</summary>
    public Location? Location { get; }

    public BitFlagInfo(string name, int bit, MustBe valueOverride = MustBe.Any, string? description = null, string? descriptionResourceType = null, Location? location = null)
    {
        Name = name;
        Bit = bit;
        ValueOverride = valueOverride;
        Description = description;
        DescriptionResourceType = descriptionResourceType;
        Location = location;
    }
}
