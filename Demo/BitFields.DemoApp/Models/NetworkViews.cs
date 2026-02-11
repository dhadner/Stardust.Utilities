using Stardust.Utilities;

namespace BitFields.DemoApp;

[BitFieldsView(ByteOrder.BigEndian, BitOrder.BitZeroIsMsb)]
public partial record struct IPv4HeaderView
{
    [BitField(0, 3)] public partial byte Version { get; set; }
    [BitField(4, 7)] public partial byte Ihl { get; set; }
    [BitField(16, 31)] public partial ushort TotalLength { get; set; }
    [BitField(72, 79)] public partial byte Protocol { get; set; }
    [BitField(96, 127)] public partial uint SourceAddress { get; set; }
    [BitField(128, 159)] public partial uint DestinationAddress { get; set; }

    public int HeaderLengthBytes => Ihl * 4;
}

[BitFieldsView(ByteOrder.BigEndian, BitOrder.BitZeroIsMsb)]
public partial record struct TcpHeaderView
{
    [BitField(0, 15)] public partial ushort SourcePort { get; set; }
    [BitField(16, 31)] public partial ushort DestinationPort { get; set; }
    [BitField(32, 63)] public partial uint SequenceNumber { get; set; }
    [BitField(64, 95)] public partial uint AcknowledgmentNumber { get; set; }
    [BitField(96, 99)] public partial byte DataOffset { get; set; }
    [BitFlag(103)] public partial bool SYN { get; set; }
    [BitFlag(104)] public partial bool FIN { get; set; }

    public int HeaderLengthBytes => DataOffset * 4;
}
