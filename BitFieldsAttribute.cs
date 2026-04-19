namespace Stardust.Utilities;

/// <summary>
/// Marks a struct as a bit register, enabling source generation for bitfield properties.
/// On a <c>partial struct</c>, generates a value type with inline storage.
/// On a <c>partial record struct</c>, generates a zero-copy view over <c>Memory&lt;byte&gt;</c>.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// // Value type (private fields, operators, implicit conversions)
/// [BitFields(typeof(byte))]
/// public partial struct MyRegister
/// {
///     [BitField(0, End = 3)] public partial byte LowNibble { get; set; }  // bits 0..=3
///     [BitFlag(7)] public partial bool Flag { get; set; }
/// }
/// 
/// // Zero-copy buffer view (reads/writes directly in the buffer)
/// [BitFields(ByteOrder.BigEndian, BitOrder.BitZeroIsMsb)]
/// public partial record struct IPv6Header
/// {
///     [BitField(0, End = 3)]   public partial byte Version { get; set; }
///     [BitField(4, End = 11)]  public partial byte TrafficClass { get; set; }
///     [BitField(12, End = 31)] public partial uint FlowLabel { get; set; }
/// }
/// 
/// // Equivalent: use the StorageType enum for compile-time safety
/// [BitFields(StorageType.Byte)]
/// public partial struct MyRegister2 { ... }
/// 
/// // Usage with implicit conversions
/// MyRegister reg = 0xFF;       // From byte
/// byte raw = reg;              // To byte
/// var reg2 = new MyRegister(0x55);  // Constructor
/// 
/// // With undefined bits handling:
/// [BitFields(typeof(ushort), UndefinedBitsMustBe.Zeroes)]
/// public partial struct ProtocolHeader { ... }
/// 
/// // Auto-sized: generator picks the smallest backing type for the declared fields
/// [BitFields]
/// public partial struct AutoReg
/// {
///     [BitField(0, End = 3)] public partial byte LowNibble { get; set; }
///     [BitFlag(7)] public partial bool Flag { get; set; }
/// }
/// // LowNibble ends at bit 3, Flag at bit 7 → 8 bits needed → byte backing
/// 
/// // Arbitrary-size bitfields (multi-word backing store):
/// [BitFields(200)]  // 200 bits backed by 4 x ulong
/// public partial struct WideRegister { ... }
/// 
/// // MSB-first bit numbering (for datasheets that number from the MSB):
/// [BitFields(typeof(byte), bitOrder: BitOrder.BitZeroIsMsb)]
/// public partial struct MsbRegister
/// {
///     [BitField(0, End = 3)] public partial byte HighNibble { get; set; }  // top 4 bits
/// }
/// </code>
/// The generator creates for value types (<c>struct</c>):
/// <list type="bullet">
/// <item>A private <c>__value</c> field of the specified storage type (or multiple <c>__w0</c>, <c>__w1</c>, ... fields for arbitrary sizes)</item>
/// <item>A constructor taking the storage type</item>
/// <item>Property implementations with inline bit manipulation</item>
/// <item>Implicit conversion operators to/from the storage type</item>
/// </list>
/// The generator creates for views (<c>record struct</c>):
/// <list type="bullet">
/// <item>A <c>Memory&lt;byte&gt;</c> field referencing the external buffer</item>
/// <item>Constructors accepting <c>Memory&lt;byte&gt;</c> and <c>byte[]</c></item>
/// <item>Property implementations that read/write directly through the span</item>
/// <item>A <c>SIZE_IN_BYTES</c> constant for the minimum required buffer size</item>
/// </list>
/// </remarks>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class BitFieldsAttribute : Attribute
{
    /// <summary>
    /// The storage type (byte, ushort, uint, ulong, nint, nuint, Half, float, double, decimal, UInt128, Int128, UInt256, Int256, sbyte, short, int, or long).
    /// Null when using the bit-count constructor for arbitrary-size bitfields.
    /// </summary>
    public Type? StorageType { get; }

    /// <summary>
    /// The total number of bits in the bitfield. Set when using the bit-count constructor.
    /// Zero when using the storage type constructor (bit count is inferred from the type).
    /// </summary>
    public int BitCount { get; }

    /// <summary>
    /// Specifies how undefined bits (bits not covered by any field or flag) are handled.
    /// Default is <see cref="UndefinedBitsMustBe.Any"/> which preserves raw data.
    /// Can be set as a named argument: <c>[BitFields(UndefinedBits = UndefinedBitsMustBe.Zeroes)]</c>.
    /// </summary>
    public UndefinedBitsMustBe UndefinedBits { get; set; }

    /// <summary>
    /// The byte order used for serialization (ReadFrom/WriteTo/ToByteArray).
    /// Default is <see cref="ByteOrder.LittleEndian"/> (x86 native).
    /// Use <see cref="ByteOrder.BigEndian"/> for network protocols or big-endian hardware.
    /// </summary>
    /// <remarks>
    /// This controls the byte order of <c>ReadFrom</c>, <c>WriteTo</c>, <c>TryWriteTo</c>, and <c>ToByteArray</c>.
    /// When a <c>[BitFields]</c> struct is nested inside a record struct view, the struct's
    /// <c>ByteOrder</c> overrides the view's default byte order for that field.
    /// </remarks>
    public ByteOrder ByteOrder { get; }

    /// <summary>
    /// The bit numbering convention used for field positions.
    /// Default is <see cref="BitOrder.BitZeroIsLsb"/> (hardware register convention: bit 0 = least significant bit).
    /// </summary>
    public BitOrder BitOrder { get; }

    /// <summary>
    /// An optional description of the struct, used as a section label in
    /// <see cref="BitFieldDiagram"/> multi-struct diagrams.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// An optional resource type for the Description property, allowing localization of struct descriptions in BitFieldDiagram.
    /// </summary>
    public Type? DescriptionResourceType { get; set; }

    /// <summary>
    /// Creates a BitFields attribute with the specified storage type.
    /// </summary>
    /// <param name="storageType">The storage type (byte, ushort, uint, ulong, nint, nuint, Half, float, double, decimal, UInt128, Int128, UInt256, Int256, sbyte, short, int, or long).</param>
    /// <param name="undefinedBits">Specifies how undefined bits are handled. Defaults to <see cref="UndefinedBitsMustBe.Any"/>.</param>
    /// <param name="bitOrder">Bit numbering convention. Defaults to <see cref="BitOrder.BitZeroIsLsb"/>.</param>
    /// <param name="byteOrder">Byte order for serialization. Defaults to <see cref="ByteOrder.LittleEndian"/>.</param>
    public BitFieldsAttribute(Type storageType, UndefinedBitsMustBe undefinedBits = UndefinedBitsMustBe.Any, BitOrder bitOrder = BitOrder.BitZeroIsLsb, ByteOrder byteOrder = ByteOrder.LittleEndian)
    {
        StorageType = storageType;
        UndefinedBits = undefinedBits;
        BitOrder = bitOrder;
        ByteOrder = byteOrder;
    }

    /// <summary>
    /// Creates an auto-sized BitFields attribute.
    /// When applied to a <c>partial struct</c>, the generator automatically selects the smallest
    /// unsigned primitive (<c>byte</c>, <c>ushort</c>, <c>uint</c>, <c>ulong</c>) that can hold
    /// all declared <c>[BitField]</c> and <c>[BitFlag]</c> properties. If the fields require more
    /// than 64 bits, a multi-word backing store is used.
    /// When applied to a <c>partial record struct</c>, the generator produces a zero-copy view
    /// over <c>Memory&lt;byte&gt;</c>.
    /// </summary>
    /// <param name="byteOrder">Byte order for serialization / multi-byte field access. Defaults to <see cref="ByteOrder.LittleEndian"/>.</param>
    /// <param name="bitOrder">Bit numbering convention. Defaults to <see cref="BitOrder.BitZeroIsLsb"/>.</param>
    public BitFieldsAttribute(ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.BitZeroIsLsb)
    {
        ByteOrder = byteOrder;
        BitOrder = bitOrder;
    }

    /// <summary>
    /// Creates a BitFields attribute with the specified storage type using the <see cref="Utilities.StorageType"/> enum.
    /// This overload provides compile-time safety and discoverability for the supported storage types.
    /// </summary>
    /// <param name="storageType">The storage type as an enum value.</param>
    /// <param name="undefinedBits">Specifies how undefined bits are handled. Defaults to <see cref="UndefinedBitsMustBe.Any"/>.</param>
    /// <param name="bitOrder">Bit numbering convention. Defaults to <see cref="BitOrder.BitZeroIsLsb"/>.</param>
    /// <param name="byteOrder">Byte order for serialization. Defaults to <see cref="ByteOrder.LittleEndian"/>.</param>
    public BitFieldsAttribute(StorageType storageType, UndefinedBitsMustBe undefinedBits = UndefinedBitsMustBe.Any, BitOrder bitOrder = BitOrder.BitZeroIsLsb, ByteOrder byteOrder = ByteOrder.LittleEndian)
    {
        StorageType = MapToType(storageType);
        UndefinedBits = undefinedBits;
        BitOrder = bitOrder;
        ByteOrder = byteOrder;
    }

    /// <summary>
    /// Creates a BitFields attribute with an arbitrary bit count.
    /// The backing store is generated as multiple ulong fields, rounded up to the next 64-bit boundary.
    /// Maximum supported size is 16,384 bits (2,048 bytes).
    /// </summary>
    /// <param name="bitCount">The number of bits in the bitfield (1 to 16,384).</param>
    /// <param name="undefinedBits">Specifies how undefined bits are handled. Defaults to <see cref="UndefinedBitsMustBe.Any"/>.</param>
    /// <param name="bitOrder">Bit numbering convention. Defaults to <see cref="BitOrder.BitZeroIsLsb"/>.</param>
    /// <param name="byteOrder">Byte order for serialization. Defaults to <see cref="ByteOrder.LittleEndian"/>.</param>
    public BitFieldsAttribute(int bitCount, UndefinedBitsMustBe undefinedBits = UndefinedBitsMustBe.Any, BitOrder bitOrder = BitOrder.BitZeroIsLsb, ByteOrder byteOrder = ByteOrder.LittleEndian)
    {
        BitCount = bitCount;
        UndefinedBits = undefinedBits;
        BitOrder = bitOrder;
        ByteOrder = byteOrder;
    }

    /// <summary>
    /// Maps a <see cref="Utilities.StorageType"/> enum value to the corresponding <see cref="System.Type"/>.
    /// </summary>
    private static Type? MapToType(StorageType storageType) => storageType switch
    {
        Utilities.StorageType.Byte => typeof(byte),
        Utilities.StorageType.SByte => typeof(sbyte),
        Utilities.StorageType.Int16 => typeof(short),
        Utilities.StorageType.UInt16 => typeof(ushort),
        Utilities.StorageType.Int32 => typeof(int),
        Utilities.StorageType.UInt32 => typeof(uint),
        Utilities.StorageType.Int64 => typeof(long),
        Utilities.StorageType.UInt64 => typeof(ulong),
        Utilities.StorageType.NInt => typeof(nint),
        Utilities.StorageType.NUInt => typeof(nuint),
        Utilities.StorageType.Half => typeof(Half),
        Utilities.StorageType.Single => typeof(float),
        Utilities.StorageType.Double => typeof(double),
        Utilities.StorageType.Decimal => typeof(decimal),
        Utilities.StorageType.Int128 => typeof(Int128),
        Utilities.StorageType.UInt128 => typeof(UInt128),
        _ => null,
    };
}

