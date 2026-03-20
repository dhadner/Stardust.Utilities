using Stardust.Utilities;

namespace Stardust.Utilities.Protocols;

/// <summary>
/// IPv4 header (RFC 791). Fixed 20-byte header (no options).
/// <code>
///  0                   1                   2                   3
///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |Version|  IHL  |Type of Service|          Total Length         |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |         Identification        |Flags|      Fragment Offset    |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |  Time to Live |    Protocol   |         Header Checksum       |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |                       Source Address                          |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |                    Destination Address                        |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// </code>
/// </summary>
[BitFieldsView(ByteOrder.NetworkEndian, BitOrder.BitZeroIsMsb)]
public partial record struct IPv4HeaderView
{
    [BitField(0, EndBit = 3)]     public partial byte Version { get; set; }
    [BitField(4, EndBit = 7)]     public partial byte Ihl { get; set; }
    [BitField(8, EndBit = 13)]    public partial byte Dscp { get; set; }
    [BitField(14, EndBit = 15)]   public partial byte Ecn { get; set; }
    [BitField(16, EndBit = 31)]   public partial ushort TotalLength { get; set; }
    [BitField(32, EndBit = 47)]   public partial ushort Identification { get; set; }
    [BitFlag(48)]        public partial bool ReservedFlag { get; set; }
    [BitFlag(49)]        public partial bool DontFragment { get; set; }
    [BitFlag(50)]        public partial bool MoreFragments { get; set; }
    [BitField(51, EndBit = 63)]   public partial ushort FragmentOffset { get; set; }
    [BitField(64, EndBit = 71)]   public partial byte TimeToLive { get; set; }
    [BitField(72, EndBit = 79)]   public partial byte Protocol { get; set; }
    [BitField(80, EndBit = 95)]   public partial ushort HeaderChecksum { get; set; }
    [BitField(96, EndBit = 127)]  public partial uint SourceAddress { get; set; }
    [BitField(128, EndBit = 159)] public partial uint DestinationAddress { get; set; }

    /// <summary>Header length in bytes (IHL * 4).</summary>
    public int HeaderLengthBytes => Ihl * 4;
}
