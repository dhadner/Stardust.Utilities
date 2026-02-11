using Stardust.Utilities;

namespace BitFields.DemoApp;

[BitFieldsView(ByteOrder.LittleEndian, BitOrder.BitZeroIsLsb)]
public partial record struct DosHeaderView
{
    [BitField(0, 15)] public partial ushort Magic { get; set; }
    [BitField(480, 511)] public partial uint Lfanew { get; set; }
}

public static class PeHeader
{
    public const uint Signature = 0x00004550;
}

[BitFieldsView(ByteOrder.LittleEndian, BitOrder.BitZeroIsLsb)]
public partial record struct CoffHeaderView
{
    [BitField(0, 15)] public partial ushort Machine { get; set; }
    [BitField(16, 31)] public partial ushort NumberOfSections { get; set; }
    [BitField(32, 63)] public partial uint TimeDateStamp { get; set; }
    [BitField(64, 95)] public partial uint PointerToSymbolTable { get; set; }
    [BitField(96, 127)] public partial uint NumberOfSymbols { get; set; }
    [BitField(128, 143)] public partial ushort SizeOfOptionalHeader { get; set; }
    [BitField(144, 159)] public partial ushort Characteristics { get; set; }
    [BitField(256, 287)] public partial uint OptionalHeaderEntryPoint { get; set; }
}
