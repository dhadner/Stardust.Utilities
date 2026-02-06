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
/// </code>
/// The generator creates:
/// <list type="bullet">
/// <item>A private Value field of the specified storage type</item>
/// <item>A constructor taking the storage type</item>
/// <item>Property implementations with inline bit manipulation</item>
/// <item>Implicit conversion operators to/from the storage type</item>
/// </list>
/// </remarks>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class BitFieldsAttribute : Attribute
{
    /// <summary>
    /// The storage type (byte, ushort, uint, ulong, sbyte, short, int, or long).
    /// </summary>
    public Type StorageType { get; }

    /// <summary>
    /// Specifies how undefined bits (bits not covered by any field or flag) are handled.
    /// Default is <see cref="UndefinedBitsMustBe.Any"/> which preserves raw data.
    /// </summary>
    public UndefinedBitsMustBe UndefinedBits { get; }

    /// <summary>
    /// Creates a BitFields attribute with the specified storage type.
    /// </summary>
    /// <param name="storageType">The storage type (byte, ushort, uint, ulong, sbyte, short, int, or long).</param>
    /// <param name="undefinedBits">Specifies how undefined bits are handled. Defaults to <see cref="UndefinedBitsMustBe.Any"/>.</param>
    public BitFieldsAttribute(Type storageType, UndefinedBitsMustBe undefinedBits = UndefinedBitsMustBe.Any)
    {
        StorageType = storageType;
        UndefinedBits = undefinedBits;
    }
}

