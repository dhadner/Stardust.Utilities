using FluentAssertions;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Unit tests for the [BitFields] source generator.
/// </summary>
public partial class BitFieldTests
{
    #region Generated 8-bit BitFields Struct Tests

    public partial class Reg8_InternalClass
    {
        [BitFields(typeof(byte))]
        public partial struct InternalReg8
        {
            [BitFlag(0)] public partial bool FlagA { get; set; }
            [BitFlag(1)] public partial bool FlagB { get; set; }
            [BitField(3, 4)] public partial byte FieldC { get; set; }  // bits 3..=4 (2 bits)
        }

        public void RunTest()
        {
            var reg = new InternalReg8();

            // Test default values
            reg.FlagA.Should().BeFalse();
            reg.FlagB.Should().BeFalse();
            reg.FieldC.Should().Be(0);

            // Test setting values
            reg.FlagA = true;
            reg.FlagB = true;
            reg.FieldC = 3;  // Max value for 2-bit field

            // Test getting values
            reg.FlagA.Should().BeTrue();
            reg.FlagB.Should().BeTrue();
            reg.FieldC.Should().Be(3);

            // FlagA (bit 0) = 1, FlagB (bit 1) = 1, FieldC (bits 3-4) = 3 (0b11 << 3 = 0x18)
            // Expected: 0x01 | 0x02 | 0x18 = 0x1B
            ((byte)reg).Should().Be(0x1B);
        }
    }

    [Fact]
    public void Reg8_InternalClass_Works()
    {
        new Reg8_InternalClass().RunTest();
    }

    /// <summary>
    /// Tests generated 8-bit BitFields struct - flags.
    /// </summary>
    [Fact]
    public void GeneratedStatusReg8_Flags_GetAndSet()
    {
        GeneratedStatusReg8 reg = 0;

        // Initial state
        ((byte)reg).Should().Be(0);
        reg.Ready.Should().BeFalse();
        reg.Error.Should().BeFalse();
        reg.Busy.Should().BeFalse();

        // Set flags
        reg.Ready = true;
        reg.Ready.Should().BeTrue();
        ((byte)reg).Should().Be(0x01);

        reg.Error = true;
        reg.Error.Should().BeTrue();
        ((byte)reg).Should().Be(0x03);

        reg.Busy = true;
        reg.Busy.Should().BeTrue();
        ((byte)reg).Should().Be(0x83);

        // Clear flags
        reg.Ready = false;
        reg.Ready.Should().BeFalse();
        ((byte)reg).Should().Be(0x82);
    }

    /// <summary>
    /// Tests generated 8-bit BitFields struct - multi-bit fields.
    /// </summary>
    [Fact]
    public void GeneratedStatusReg8_Fields_GetAndSet()
    {
        GeneratedStatusReg8 reg = 0;

        // Set Mode (bits 2-4, 3 bits wide)
        reg.Mode = 5;
        reg.Mode.Should().Be(5);
        ((byte)reg).Should().Be(0x14); // 5 << 2 = 0x14

        // Set Priority (bits 5-6, 2 bits wide)
        reg.Priority = 3;
        reg.Priority.Should().Be(3);
        ((byte)reg).Should().Be(0x74); // 0x14 | (3 << 5) = 0x74

        // Verify fields don't interfere with each other
        reg.Mode = 0;
        reg.Mode.Should().Be(0);
        reg.Priority.Should().Be(3);
    }

    /// <summary>
    /// Tests generated 8-bit BitFields struct - implicit conversion.
    /// </summary>
    [Fact]
    public void GeneratedStatusReg8_ImplicitConversion()
    {
        // From byte
        GeneratedStatusReg8 reg = 0xFF;
        ((byte)reg).Should().Be(0xFF);
        reg.Ready.Should().BeTrue();
        reg.Error.Should().BeTrue();
        reg.Busy.Should().BeTrue();

        // To byte
        byte value = reg;
        value.Should().Be(0xFF);
    }

    #endregion

    #region Generated 16-bit BitFields Struct Tests

    /// <summary>
    /// Tests generated 16-bit BitFields struct.
    /// </summary>
    [Fact]
    public void GeneratedKeyboardReg16_GetAndSet()
    {
        var reg = new GeneratedKeyboardReg16();

        // Set first key code (bits 8-14, 7 bits)
        reg.FirstKeyCode = 0x1A; // 'Z' key
        reg.FirstKeyCode.Should().Be(0x1A);

        // Set first key up flag (bit 15)
        reg.FirstKeyUp = true;
        reg.FirstKeyUp.Should().BeTrue();

        // Set second key code (bits 0-6, 7 bits)
        reg.SecondKeyCode = 0x00; // 'A' key
        reg.SecondKeyCode.Should().Be(0x00);

        // Set second key up flag (bit 7)
        reg.SecondKeyUp = false;
        reg.SecondKeyUp.Should().BeFalse();

        // Verify the combined value
        // FirstKeyUp (bit 15) = 1, FirstKeyCode (bits 8-14) = 0x1A
        // SecondKeyUp (bit 7) = 0, SecondKeyCode (bits 0-6) = 0x00
        // Expected: 0x9A00
        ((ushort)reg).Should().Be(0x9A00);
    }

    /// <summary>
    /// Tests generated 16-bit BitFields struct - implicit conversion.
    /// </summary>
    [Fact]
    public void GeneratedKeyboardReg16_ImplicitConversion()
    {
        // From ushort
        GeneratedKeyboardReg16 reg = 0xFFFF;
        ((ushort)reg).Should().Be(0xFFFF);
        reg.FirstKeyUp.Should().BeTrue();
        reg.SecondKeyUp.Should().BeTrue();
        reg.FirstKeyCode.Should().Be(0x7F);
        reg.SecondKeyCode.Should().Be(0x7F);

        // To ushort
        ushort value = reg;
        value.Should().Be(0xFFFF);
    }

    #endregion

    #region Generated 32-bit BitFields Struct Tests

    /// <summary>
    /// Tests generated 32-bit BitFields struct.
    /// </summary>
    [Fact]
    public void GeneratedControlReg32_GetAndSet()
    {
        var reg = new GeneratedControlReg32();

        // Set address (bits 0-23, 24 bits)
        reg.Address = 0x00ABCDEF;
        reg.Address.Should().Be(0x00ABCDEF);

        // Set command (bits 24-27, 4 bits)
        reg.Command = 0x0F;
        reg.Command.Should().Be(0x0F);

        // Set flags
        reg.Enable = true;
        reg.Enable.Should().BeTrue();

        reg.Interrupt = true;
        reg.Interrupt.Should().BeTrue();

        // Verify combined value
        // Address = 0x00ABCDEF, Command = 0x0F, Enable = 1 (bit 28), Interrupt = 1 (bit 29)
        ((uint)reg).Should().Be(0x3FABCDEF);
    }

    /// <summary>
    /// Tests generated 32-bit BitFields struct - implicit conversion.
    /// </summary>
    [Fact]
    public void GeneratedControlReg32_ImplicitConversion()
    {
        // From uint
        GeneratedControlReg32 reg = 0xFFFFFFFF;
        ((uint)reg).Should().Be(0xFFFFFFFF);
        reg.Enable.Should().BeTrue();
        reg.Interrupt.Should().BeTrue();
        reg.Command.Should().Be(0x0F);
        reg.Address.Should().Be(0x00FFFFFF);

        // To uint
        uint value = reg;
        value.Should().Be(0xFFFFFFFF);
    }

    #endregion

    #region Generated 64-bit BitFields Struct Tests

    /// <summary>
    /// Tests generated 64-bit BitFields struct.
    /// </summary>
    [Fact]
    public void GeneratedWideReg64_GetAndSet()
    {
        var reg = new GeneratedWideReg64();

        // Set status byte (bits 0-7)
        reg.Status = 0xAB;
        reg.Status.Should().Be(0xAB);

        // Set data word (bits 8-23, 16 bits)
        reg.Data = 0xCDEF;
        reg.Data.Should().Be(0xCDEF);

        // Set address dword (bits 24-55, 32 bits)
        reg.Address = 0x12345678;
        reg.Address.Should().Be(0x12345678);

        // Set high flags
        reg.Valid = true;
        reg.Valid.Should().BeTrue();

        reg.Ready = true;
        reg.Ready.Should().BeTrue();

        // Verify combined value
        ulong expected = 0xAB
            | ((ulong)0xCDEF << 8)
            | ((ulong)0x12345678 << 24)
            | (1UL << 56)  // Valid
            | (1UL << 57); // Ready
        ((ulong)reg).Should().Be(expected);
    }

    /// <summary>
    /// Tests generated 64-bit BitFields struct - implicit conversion.
    /// </summary>
    [Fact]
    public void GeneratedWideReg64_ImplicitConversion()
    {
        // From ulong
        GeneratedWideReg64 reg = 0xFFFFFFFFFFFFFFFF;
        ((ulong)reg).Should().Be(0xFFFFFFFFFFFFFFFF);
        reg.Valid.Should().BeTrue();
        reg.Ready.Should().BeTrue();
        reg.Status.Should().Be(0xFF);
        reg.Data.Should().Be(0xFFFF);
        reg.Address.Should().Be(0xFFFFFFFF);

        // To ulong
        ulong value = reg;
        value.Should().Be(0xFFFFFFFFFFFFFFFF);
    }

    #endregion

    #region Edge Case Tests

    /// <summary>
    /// Tests that setting a field doesn't affect adjacent fields.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_FieldIsolation()
    {
        GeneratedStatusReg8 reg = 0xFF;

        // Clear Mode field (bits 2-4)
        reg.Mode = 0;
        
        // Other fields should be unchanged
        reg.Ready.Should().BeTrue();    // bit 0
        reg.Error.Should().BeTrue();    // bit 1
        reg.Priority.Should().Be(3);    // bits 5-6
        reg.Busy.Should().BeTrue();     // bit 7
        
        // Only bits 2-4 should be cleared
        ((byte)reg).Should().Be(0xE3); // 0xFF & ~0x1C = 0xE3
    }

    /// <summary>
    /// Tests boundary values for multi-bit fields.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_BoundaryValues()
    {
        GeneratedStatusReg8 reg = 0;

        // Mode is 3 bits (max value 7)
        reg.Mode = 7;
        reg.Mode.Should().Be(7);

        // Priority is 2 bits (max value 3)
        reg.Priority = 3;
        reg.Priority.Should().Be(3);

        // Values exceeding field width should be truncated
        reg.Mode = 0xFF;  // Should become 7 (0b111)
        reg.Mode.Should().Be(7);
    }

    #endregion
}

#region Generated BitFields Test Structs

/// <summary>
/// 8-bit status register for testing code generation.
/// </summary>
[BitFields(typeof(byte))]
public partial struct GeneratedStatusReg8
{
    [BitFlag(0)] public partial bool Ready { get; set; }
    [BitFlag(1)] public partial bool Error { get; set; }
    [BitFlag(7)] public partial bool Busy { get; set; }
    [BitField(2, 4)] public partial byte Mode { get; set; }      // bits 2..=4 (3 bits)
    [BitField(5, 6)] public partial byte Priority { get; set; }  // bits 5..=6 (2 bits)
}

/// <summary>
/// 16-bit keyboard register for testing code generation.
/// </summary>
[BitFields(typeof(ushort))]
public partial struct GeneratedKeyboardReg16
{
    [BitField(0, 6)] public partial byte SecondKeyCode { get; set; }  // bits 0..=6 (7 bits)
    [BitFlag(7)] public partial bool SecondKeyUp { get; set; }
    [BitField(8, 14)] public partial byte FirstKeyCode { get; set; }  // bits 8..=14 (7 bits)
    [BitFlag(15)] public partial bool FirstKeyUp { get; set; }
}

/// <summary>
/// 32-bit control register for testing code generation.
/// </summary>
[BitFields(typeof(uint))]
public partial struct GeneratedControlReg32
{
    [BitField(0, 23)] public partial uint Address { get; set; }   // bits 0..=23 (24 bits)
    [BitField(24, 27)] public partial byte Command { get; set; }  // bits 24..=27 (4 bits)
    [BitFlag(28)] public partial bool Enable { get; set; }
    [BitFlag(29)] public partial bool Interrupt { get; set; }
}

/// <summary>
/// 64-bit wide register for testing code generation.
/// </summary>
[BitFields(typeof(ulong))]
public partial struct GeneratedWideReg64
{
    [BitField(0, 7)] public partial byte Status { get; set; }     // bits 0..=7 (8 bits)
    [BitField(8, 23)] public partial ushort Data { get; set; }    // bits 8..=23 (16 bits)
    [BitField(24, 55)] public partial uint Address { get; set; }  // bits 24..=55 (32 bits)
    [BitFlag(56)] public partial bool Valid { get; set; }
    [BitFlag(57)] public partial bool Ready { get; set; }
}

#endregion
