using Stardust.Utilities;

namespace BitFields.DemoApp;

[BitFieldsView(ByteOrder.BigEndian, BitOrder.BitZeroIsMsb)]
public partial record struct IPv4HeaderView
{
    [BitField(0, 3, Description = "IP protocol version (always 4 for IPv4)")]
    public partial byte Version { get; set; }

    [BitField(4, 7, Description = "Internet Header Length in 32-bit words (min 5 = 20 bytes)")]
    public partial byte Ihl { get; set; }

    [BitField(16, 31, Description = "Total packet length in bytes, including header and payload")]
    public partial ushort TotalLength { get; set; }

    [BitField(72, 79, Description = "Upper-layer protocol number (6=TCP, 17=UDP, 1=ICMP)")]
    public partial byte Protocol { get; set; }

    [BitField(96, 127, Description = "32-bit source IPv4 address")]
    public partial uint SourceAddress { get; set; }

    [BitField(128, 159, Description = "32-bit destination IPv4 address")]
    public partial uint DestinationAddress { get; set; }

    public int HeaderLengthBytes => Ihl * 4;
}

[BitFieldsView(ByteOrder.BigEndian, BitOrder.BitZeroIsMsb)]
public partial record struct TcpHeaderView
{
    [BitField(0, 15, Description = "Source port number identifying the sending application")]
    public partial ushort SourcePort { get; set; }

    [BitField(16, 31, Description = "Destination port number identifying the receiving application")]
    public partial ushort DestinationPort { get; set; }

    [BitField(32, 63, Description = "Sequence number of the first data byte in this segment")]
    public partial uint SequenceNumber { get; set; }

    [BitField(64, 95, Description = "Next sequence number the sender expects to receive")]
    public partial uint AcknowledgmentNumber { get; set; }

    [BitField(96, 99, Description = "TCP header length in 32-bit words (min 5 = 20 bytes)")]
    public partial byte DataOffset { get; set; }

    [BitFlag(103, Description = "Synchronize flag - initiates a TCP connection")]
    public partial bool SYN { get; set; }

    [BitFlag(104, Description = "Finish flag - signals the sender has finished sending data")]
    public partial bool FIN { get; set; }

    public int HeaderLengthBytes => DataOffset * 4;
}
