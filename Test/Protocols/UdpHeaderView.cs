using Stardust.Utilities;

namespace Stardust.Utilities.Protocols;

/// <summary>
/// UDP header (RFC 768). Fixed 8-byte header.
/// <code>
///  0                   1                   2                   3
///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |          Source Port          |       Destination Port        |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |            Length             |           Checksum            |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// </code>
/// </summary>
[BitFieldsView(ByteOrder.NetworkEndian, BitOrder.MsbFirst)]
public partial record struct UdpHeaderView
{
    [BitField(0, 15)]  public partial ushort SourcePort { get; set; }
    [BitField(16, 31)] public partial ushort DestinationPort { get; set; }
    [BitField(32, 47)] public partial ushort Length { get; set; }
    [BitField(48, 63)] public partial ushort Checksum { get; set; }
}
