namespace Stardust.Utilities;

/// <summary>
/// Marks a <c>partial record struct</c> as a zero-copy view over a <see cref="Memory{T}">Memory&lt;byte&gt;</see> buffer,
/// enabling source generation for bitfield properties that read and write directly into the underlying memory.
/// </summary>
/// <remarks>
/// <para>
/// <b>Deprecated:</b> Use <see cref="BitFieldsAttribute"/> on a <c>partial record struct</c> instead.
/// The generator detects the <c>record</c> keyword and produces identical view code.
/// </para>
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
/// // Preferred (1.0+): use [BitFields] on a record struct
/// [BitFields(ByteOrder.BigEndian, BitOrder.BitZeroIsMsb)]
/// public partial record struct IPv6Header
/// {
///     [BitField(0, End = 3)]   public partial byte Version { get; set; }
///     [BitField(4, End = 11)]  public partial byte TrafficClass { get; set; }
///     [BitField(12, End = 31)] public partial uint FlowLabel { get; set; }
/// }
/// </code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Struct)]
[Obsolete("Use [BitFields] on a partial record struct instead. The generator detects the record keyword and produces identical view code. BitFieldsViewAttribute will be removed in a future version.")]
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
    /// Default is <see cref="BitOrder.BitZeroIsLsb"/> (bit 0 = least significant), matching <see cref="BitFieldsAttribute"/>.
    /// Use <see cref="BitOrder.BitZeroIsMsb"/> for RFC/network protocol conventions.
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
    /// Creates a BitFieldsView attribute with the specified byte order and bit order.
    /// </summary>
    /// <param name="byteOrder">Byte order for multi-byte field access. Defaults to <see cref="ByteOrder.LittleEndian"/>.</param>
    /// <param name="bitOrder">Bit numbering convention. Defaults to <see cref="BitOrder.BitZeroIsLsb"/>.</param>
    public BitFieldsViewAttribute(ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.BitZeroIsLsb)
    {
        ByteOrder = byteOrder;
        BitOrder = bitOrder;
    }
}
