namespace Stardust.Utilities;

/// <summary>
/// Marks a <c>partial record struct</c> as a zero-copy view over a <see cref="Memory{T}">Memory&lt;byte&gt;</see> buffer,
/// enabling source generation for bitfield properties that read and write directly into the underlying memory.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="BitFieldsAttribute"/> which generates value-type structs with inline storage,
/// <c>[BitFieldsView]</c> generates a <c>record struct</c> that wraps a <c>Memory&lt;byte&gt;</c> reference.
/// This enables zero-copy access to protocol headers, file formats, and other binary data of arbitrary size.
/// </para>
/// <para>
/// The generated struct includes:
/// <list type="bullet">
/// <item>A <c>Memory&lt;byte&gt;</c> field referencing the external buffer</item>
/// <item>Constructors accepting <c>Memory&lt;byte&gt;</c> and <c>byte[]</c></item>
/// <item>Property implementations that read/write directly through the span</item>
/// <item>A <c>SizeInBytes</c> constant for the minimum required buffer size</item>
/// </list>
/// </para>
/// <example>
/// <code>
/// // Default: little-endian, LSB-first (matches [BitFields] convention)
/// [BitFieldsView]
/// public partial record struct RegisterView
/// {
///     [BitField(0, 7)] public partial byte LowByte { get; set; }
///     [BitField(8, 15)] public partial byte HighByte { get; set; }
/// }
///
/// // Network protocol: big-endian, MSB-first (RFC convention)
/// [BitFieldsView(ByteOrder.BigEndian, BitOrder.MsbFirst)]
/// public partial record struct IPv6Header
/// {
///     [BitField(0, 3)]   public partial byte Version { get; set; }
///     [BitField(4, 11)]  public partial byte TrafficClass { get; set; }
///     [BitField(12, 31)] public partial uint FlowLabel { get; set; }
/// }
/// </code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class BitFieldsViewAttribute : Attribute
{
    /// <summary>
    /// The byte order used for multi-byte field access.
    /// Default is <see cref="ByteOrder.LittleEndian"/> (native byte order), matching <see cref="BitFieldsAttribute"/>.
    /// Use <see cref="ByteOrder.BigEndian"/> for network protocols.
    /// </summary>
    public ByteOrder ByteOrder { get; }

    /// <summary>
    /// The bit numbering convention used for field positions.
    /// Default is <see cref="BitOrder.LsbFirst"/> (bit 0 = least significant), matching <see cref="BitFieldsAttribute"/>.
    /// Use <see cref="BitOrder.MsbFirst"/> for RFC/network protocol conventions.
    /// </summary>
    public BitOrder BitOrder { get; }

    /// <summary>
    /// Creates a BitFieldsView attribute with the specified byte order and bit order.
    /// </summary>
    /// <param name="byteOrder">Byte order for multi-byte field access. Defaults to <see cref="ByteOrder.LittleEndian"/>.</param>
    /// <param name="bitOrder">Bit numbering convention. Defaults to <see cref="BitOrder.LsbFirst"/>.</param>
    public BitFieldsViewAttribute(ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.LsbFirst)
    {
        ByteOrder = byteOrder;
        BitOrder = bitOrder;
    }
}
