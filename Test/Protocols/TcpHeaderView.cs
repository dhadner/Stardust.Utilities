using Stardust.Utilities;

namespace Stardust.Utilities.Protocols;

/// <summary>
/// TCP header (RFC 793, updated by RFC 3168 ECN, RFC 3540 NS). Fixed 20-byte header (no options).
/// <code>
///  0                   1                   2                   3
///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |          Source Port          |       Destination Port        |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |                        Sequence Number                        |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |                    Acknowledgment Number                      |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |  Data |       |N|C|E|U|A|P|R|S|F|                             |
/// | Offset| Rsvd  |S|W|C|R|C|S|S|Y|I|         Window Size         |
/// |       |       | |R|E|G|K|H|T|N|N|                             |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |           Checksum            |         Urgent Pointer        |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// </code>
/// </summary>
[BitFieldsView(ByteOrder.NetworkEndian, BitOrder.MsbIsBitZero)]
public partial record struct TcpHeaderView
{
    [BitField(0, 15)]    public partial ushort SourcePort { get; set; }
    [BitField(16, 31)]   public partial ushort DestinationPort { get; set; }
    [BitField(32, 63)]   public partial uint SequenceNumber { get; set; }
    [BitField(64, 95)]   public partial uint AcknowledgmentNumber { get; set; }
    [BitField(96, 99)]   public partial byte DataOffset { get; set; }
    [BitField(100, 102)] public partial byte Reserved { get; set; }
    [BitFlag(103)]       public partial bool NS { get; set; }
    [BitFlag(104)]       public partial bool CWR { get; set; }
    [BitFlag(105)]       public partial bool ECE { get; set; }
    [BitFlag(106)]       public partial bool URG { get; set; }
    [BitFlag(107)]       public partial bool ACK { get; set; }
    [BitFlag(108)]       public partial bool PSH { get; set; }
    [BitFlag(109)]       public partial bool RST { get; set; }
    [BitFlag(110)]       public partial bool SYN { get; set; }
    [BitFlag(111)]       public partial bool FIN { get; set; }
    [BitField(112, 127)] public partial ushort WindowSize { get; set; }
    [BitField(128, 143)] public partial ushort Checksum { get; set; }
    [BitField(144, 159)] public partial ushort UrgentPointer { get; set; }

    /// <summary>Header length in bytes (DataOffset * 4).</summary>
    public int HeaderLengthBytes => DataOffset * 4;
}
