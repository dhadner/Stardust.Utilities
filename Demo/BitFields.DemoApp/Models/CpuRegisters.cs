using Stardust.Utilities;

namespace BitFields.DemoApp;

[BitFields(typeof(ushort))]
public partial struct CpuStatusRegister
{
    [BitFlag(0, Description = "Set when an arithmetic operation produces a carry or borrow out of the MSB")]
    public partial bool Carry { get; set; }

    [BitFlag(1, Description = "Set when the result of an operation is zero")]
    public partial bool ZeroFlag { get; set; }

    [BitFlag(2, Description = "When set, the processor ignores maskable interrupt requests")]
    public partial bool InterruptDisable { get; set; }

    [BitFlag(3, Description = "Selects BCD arithmetic mode for ADC and SBC instructions")]
    public partial bool Decimal { get; set; }

    [BitFlag(6, Description = "Set when an arithmetic operation produces a signed overflow")]
    public partial bool Overflow { get; set; }

    [BitFlag(7, Description = "Set when the result of an operation has bit 7 set (negative in signed arithmetic)")]
    public partial bool Negative { get; set; }

    [BitField(8, 10, Description = "Processor operating mode (0-7)")]
    public partial byte Mode { get; set; }
}

[BitFields(typeof(uint), bitOrder: BitOrder.BitZeroIsMsb, byteOrder: ByteOrder.BigEndian)]
public partial struct MixedEndianRegister
{
    [BitField(0, 15)] public partial UInt16Be BigValue { get; set; }
    [BitField(16, 31)] public partial UInt16Le LittleValue { get; set; }
}

public static class MixedEndianDemo
{
    public static string Summarize()
    {
        MixedEndianRegister reg = 0;
        reg.BigValue = new UInt16Be(0xCAFE);
        reg.LittleValue = new UInt16Le(0xBEEF);
        return $"BigValue=0x{(reg.BigValue):X4}, LittleValue=0x{(reg.LittleValue):X4}, Raw=0x{(reg):X8}";
    }
}
