using System.ComponentModel;
using Stardust.Utilities;

namespace BitFields.DemoApp;

// ── Condition Code Register (lower byte of SR) ────────────────
/// <summary>
/// Motorola 68020 CCR -- 16-bit with only bits 0-4 defined.
/// Upper 8 bits forced to zero; when composed as 8-bit field in SR,
/// only the relevant lower byte is embedded.
/// </summary>
[BitFields(typeof(ushort), UndefinedBitsMustBe.Zeroes)]
public partial struct M68020CCR
{
    [BitFlag(0, Description = "Carry/borrow flag")]
    public partial bool C { get; set; }

    [BitFlag(1, Description = "Overflow flag")]
    public partial bool V { get; set; }

    [BitFlag(2, Description = "Zero result flag")]
    public partial bool Z { get; set; }

    [BitFlag(3, Description = "Negative result flag")]
    public partial bool N { get; set; }

    [BitFlag(4, Description = "Extend flag (multi-precision arithmetic)")]
    public partial bool X { get; set; }
}

// ── Status Register (CCR + system byte) ───────────────────────
/// <summary>Motorola 68020 SR -- 16-bit. CCR at bits 0-7, system byte at 8-15.</summary>
[BitFields(typeof(ushort))]
public partial struct M68020SR
{
    [BitField(0, 7, Description = "Condition Code Register")]
    public partial M68020CCR CCR { get; set; }

    [BitField(8, 10, Description = "Interrupt priority mask (0-7)")]
    public partial byte IPM { get; set; }

    [BitFlag(12, Description = "Master/Interrupt state (1=Master Stack)")]
    public partial bool M { get; set; }

    [BitFlag(13, Description = "Supervisor/User state (1=Supervisor)")]
    public partial bool S { get; set; }

    [BitFlag(14, Description = "Trace enable bit 0")]
    public partial bool T0 { get; set; }

    [BitFlag(15, Description = "Trace enable bit 1")]
    public partial bool T1 { get; set; }
}

// ── Cache Control Register ────────────────────────────────────
/// <summary>Motorola 68020 CACR -- 32-bit, bits 0-3 defined.</summary>
[BitFields(typeof(uint))]
public partial struct M68020CACR
{
    [BitFlag(0, Description = "Enable instruction cache")]
    public partial bool E { get; set; }

    [BitFlag(1, Description = "Freeze instruction cache")]
    public partial bool F { get; set; }

    [BitFlag(2, Description = "Clear entry in instruction cache")]
    public partial bool CE { get; set; }

    [BitFlag(3, Description = "Clear entire instruction cache")]
    public partial bool C { get; set; }
}

// ── Source Function Code Register ─────────────────────────────
/// <summary>Motorola 68020 SFC -- 32-bit, only bits 0-2 used.</summary>
[BitFields(typeof(uint))]
public partial struct M68020SFC
{
    [BitField(0, 2, Description = "Source function code")]
    public partial byte FC { get; set; }
}

// ── Destination Function Code Register ────────────────────────
/// <summary>Motorola 68020 DFC -- 32-bit, only bits 0-2 used.</summary>
[BitFields(typeof(uint))]
public partial struct M68020DFC
{
    [BitField(0, 2, Description = "Destination function code")]
    public partial byte FC { get; set; }
}

// ── Simple 32-bit registers (no internal bit structure) ───────

[BitFields(typeof(uint))]
public partial struct M68020PC
{
    [BitField(0, 31, Description = "Program Counter")]
    public partial uint PC { get; set; }
}

[BitFields(typeof(uint))]
public partial struct M68020VBR
{
    [BitField(0, 31, Description = "Vector Base Register")]
    public partial uint VBR { get; set; }
}

[BitFields(typeof(uint))]
public partial struct M68020CAAR
{
    [BitField(0, 31, Description = "Cache Address Register")]
    public partial uint CAAR { get; set; }
}

[BitFields(typeof(uint))]
public partial struct M68020MSP
{
    [BitField(0, 31, Description = "A7* ()Master Stack Pointer")]
    public partial uint MSP { get; set; }
}

[BitFields(typeof(uint))]
public partial struct M68020ISP
{
    [BitField(0, 31, Description = "A7* ()Interrupt Stack Pointer")]
    public partial uint ISP { get; set; }
}

[BitFields(typeof(uint))]
public partial struct M68020USP
{
    [BitField(0, 31, Description = "A7 (User Stack Pointer)")]
    public partial uint USP { get; set; }
}

// ── Composed register groups ──────────────────────────────────

/// <summary>Motorola 68020 Data Registers D0-D7 -- 8 x 32-bit.</summary>
[BitFields(256)]
public partial struct M68020DataRegisters
{
    [BitField(0, 31, Description = "Data register 0")] public partial uint D0 { get; set; }
    [BitField(32, 63, Description = "Data register 1")] public partial uint D1 { get; set; }
    [BitField(64, 95, Description = "Data register 2")] public partial uint D2 { get; set; }
    [BitField(96, 127, Description = "Data register 3")] public partial uint D3 { get; set; }
    [BitField(128, 159, Description = "Data register 4")] public partial uint D4 { get; set; }
    [BitField(160, 191, Description = "Data register 5")] public partial uint D5 { get; set; }
    [BitField(192, 223, Description = "Data register 6")] public partial uint D6 { get; set; }
    [BitField(224, 255, Description = "Data register 7")] public partial uint D7 { get; set; }
}

/// <summary>Motorola 68020 Address Registers A0-A6 -- 7 x 32-bit.</summary>
[BitFields(256-32)]
public partial struct M68020AddressRegisters
{
    [BitField(0, 31, Description = "Address register 0")] public partial uint A0 { get; set; }
    [BitField(32, 63, Description = "Address register 1")] public partial uint A1 { get; set; }
    [BitField(64, 95, Description = "Address register 2")] public partial uint A2 { get; set; }
    [BitField(96, 127, Description = "Address register 3")] public partial uint A3 { get; set; }
    [BitField(128, 159, Description = "Address register 4")] public partial uint A4 { get; set; }
    [BitField(160, 191, Description = "Address register 5")] public partial uint A5 { get; set; }
    [BitField(192, 223, Description = "Address register 6 (often used as a Frame Pointer)")] public partial uint A6 { get; set; }
}
