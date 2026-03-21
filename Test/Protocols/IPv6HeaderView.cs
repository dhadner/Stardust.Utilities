using Stardust.Utilities;

namespace Stardust.Utilities.Protocols;

/// <summary>
/// IPv6 header (RFC 2460). Fixed 40-byte header.
/// <code>
///  0                   1                   2                   3
///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |Version| Traffic Class |           Flow Label                  |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |         Payload Length        |  Next Header  |   Hop Limit   |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |                                                               |
/// +                         Source Address                        +
/// |                         (128 bits)                            |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |                                                               |
/// +                      Destination Address                      +
/// |                         (128 bits)                            |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// </code>
/// </summary>
[BitFieldsView(ByteOrder.NetworkEndian, BitOrder.BitZeroIsMsb)]
public partial record struct IPv6FullHeaderView
{
    [BitField(0, End = 3)]     public partial byte Version { get; set; }
    [BitField(4, End = 11)]    public partial byte TrafficClass { get; set; }
    [BitField(12, End = 31)]   public partial uint FlowLabel { get; set; }
    [BitField(32, End = 47)]   public partial ushort PayloadLength { get; set; }
    [BitField(48, End = 55)]   public partial byte NextHeader { get; set; }
    [BitField(56, End = 63)]   public partial byte HopLimit { get; set; }
    [BitField(64, End = 127)]  public partial ulong SourceAddressHigh { get; set; }
    [BitField(128, End = 191)] public partial ulong SourceAddressLow { get; set; }
    [BitField(192, End = 255)] public partial ulong DestinationAddressHigh { get; set; }
    [BitField(256, End = 319)] public partial ulong DestinationAddressLow { get; set; }
}
