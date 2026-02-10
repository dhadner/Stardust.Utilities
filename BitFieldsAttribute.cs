namespace Stardust.Utilities;

/// <summary>
/// Marks a struct as a bit register, enabling source generation for bitfield properties.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [BitFields(typeof(byte))]
/// public partial struct MyRegister
/// {
///     [BitField(0, 3)] public partial byte LowNibble { get; set; }  // bits 0..=3
///     [BitFlag(7)] public partial bool Flag { get; set; }
/// }
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
/// // Arbitrary-size bitfields (multi-word backing store):
/// [BitFields(200)]  // 200 bits backed by 4 x ulong
/// public partial struct WideRegister { ... }
/// 
/// // MSB-first bit numbering (for datasheets that number from the MSB):
/// [BitFields(typeof(byte), bitOrder: BitOrder.MsbFirst)]
/// public partial struct MsbRegister
/// {
///     [BitField(0, 3)] public partial byte HighNibble { get; set; }  // top 4 bits
/// }
/// </code>
/// The generator creates:
/// <list type="bullet">
/// <item>A private Value field of the specified storage type (or multiple ulong fields for arbitrary sizes)</item>
/// <item>A constructor taking the storage type</item>
/// <item>Property implementations with inline bit manipulation</item>
/// <item>Implicit conversion operators to/from the storage type</item>
/// </list>
/// </remarks>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class BitFieldsAttribute : Attribute
{
    /// <summary>
    /// The storage type (byte, ushort, uint, ulong, Half, float, double, decimal, UInt128, Int128, sbyte, short, int, or long).
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
    /// </summary>
    public UndefinedBitsMustBe UndefinedBits { get; }

    /// <summary>
    /// The byte order used for serialization (ReadFrom/WriteTo/ToByteArray).
    /// Default is <see cref="ByteOrder.LittleEndian"/> (x86 native).
    /// Use <see cref="ByteOrder.BigEndian"/> for network protocols or big-endian hardware.
    /// </summary>
    /// <remarks>
    /// This controls the byte order of <c>ReadFrom</c>, <c>WriteTo</c>, <c>TryWriteTo</c>, and <c>ToByteArray</c>.
    /// When a <c>[BitFields]</c> struct is nested inside a <c>[BitFieldsView]</c>, the struct's
    /// <c>ByteOrder</c> overrides the view's default byte order for that field.
    /// </remarks>
    public ByteOrder ByteOrder { get; }

    /// <summary>
    /// The bit numbering convention used for field positions.
    /// Default is <see cref="BitOrder.LsbFirst"/> (hardware register convention: bit 0 = least significant bit).
    /// </summary>
    public BitOrder BitOrder { get; }

    /// <summary>
    /// Creates a BitFields attribute with the specified storage type.
    /// </summary>
    /// <param name="storageType">The storage type (byte, ushort, uint, ulong, Half, float, double, decimal, UInt128, Int128, sbyte, short, int, or long).</param>
    /// <param name="undefinedBits">Specifies how undefined bits are handled. Defaults to <see cref="UndefinedBitsMustBe.Any"/>.</param>
    /// <param name="bitOrder">Bit numbering convention. Defaults to <see cref="BitOrder.LsbFirst"/>.</param>
    /// <param name="byteOrder">Byte order for serialization. Defaults to <see cref="ByteOrder.LittleEndian"/>.</param>
    public BitFieldsAttribute(Type storageType, UndefinedBitsMustBe undefinedBits = UndefinedBitsMustBe.Any, BitOrder bitOrder = BitOrder.LsbFirst, ByteOrder byteOrder = ByteOrder.LittleEndian)
    {
        StorageType = storageType;
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
    /// <param name="bitOrder">Bit numbering convention. Defaults to <see cref="BitOrder.LsbFirst"/>.</param>
    /// <param name="byteOrder">Byte order for serialization. Defaults to <see cref="ByteOrder.LittleEndian"/>.</param>
    public BitFieldsAttribute(int bitCount, UndefinedBitsMustBe undefinedBits = UndefinedBitsMustBe.Any, BitOrder bitOrder = BitOrder.LsbFirst, ByteOrder byteOrder = ByteOrder.LittleEndian)
    {
        BitCount = bitCount;
        UndefinedBits = undefinedBits;
        BitOrder = bitOrder;
        ByteOrder = byteOrder;
    }
}

