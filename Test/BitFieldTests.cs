using FluentAssertions;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Unit tests for the BitField types.
/// </summary>
public class BitFieldTests
{
    #region BitFlag<TStorage> Tests

    /// <summary>
    /// Tests BitFlag with byte storage.
    /// </summary>
    [Fact]
    public void BitFlag_Byte_GetAndSet()
    {
        // Arrange
        var flag = new BitFlag<byte>(3); // bit position 3

        // Assert initial state
        flag.Shift.Should().Be(3);
        flag[(byte)0x00].Should().BeFalse();
        flag[(byte)0x08].Should().BeTrue(); // bit 3 set
        flag[(byte)0xFF].Should().BeTrue();

        // Test Set
        flag.Set(0x00, true).Should().Be(0x08);
        flag.Set(0xFF, false).Should().Be(0xF7);

        // Test Toggle
        flag.Toggle(0x00).Should().Be(0x08);
        flag.Toggle(0x08).Should().Be(0x00);
    }

    /// <summary>
    /// Tests BitFlag with ushort storage.
    /// </summary>
    [Fact]
    public void BitFlag_UShort_GetAndSet()
    {
        // Arrange
        var flag = new BitFlag<ushort>(10); // bit position 10

        // Assert initial state
        flag.Shift.Should().Be(10);
        flag[(ushort)0x0000].Should().BeFalse();
        flag[(ushort)0x0400].Should().BeTrue(); // bit 10 set

        // Test Set
        flag.Set(0x0000, true).Should().Be((ushort)0x0400);
        flag.Set(0x0400, false).Should().Be((ushort)0x0000);
    }

    /// <summary>
    /// Tests BitFlag with uint storage.
    /// </summary>
    [Fact]
    public void BitFlag_UInt_GetAndSet()
    {
        // Arrange
        var flag = new BitFlag<uint>(20); // bit position 20

        // Assert initial state
        flag.Shift.Should().Be(20);
        flag[0x00000000U].Should().BeFalse();
        flag[0x00100000U].Should().BeTrue(); // bit 20 set

        // Test Set
        flag.Set(0x00000000U, true).Should().Be(0x00100000U);
        flag.Set(0x00100000U, false).Should().Be(0x00000000U);
    }

    /// <summary>
    /// Tests BitFlag with ulong storage.
    /// </summary>
    [Fact]
    public void BitFlag_ULong_GetAndSet()
    {
        // Arrange
        var flag = new BitFlag<ulong>(40); // bit position 40

        // Assert initial state
        flag.Shift.Should().Be(40);
        flag[0x0000000000000000UL].Should().BeFalse();
        flag[0x0000010000000000UL].Should().BeTrue(); // bit 40 set

        // Test Set
        flag.Set(0x0000000000000000UL, true).Should().Be(0x0000010000000000UL);
        flag.Set(0x0000010000000000UL, false).Should().Be(0x0000000000000000UL);
    }

    /// <summary>
    /// Tests BitFlag constructor throws for invalid shift.
    /// </summary>
    [Fact]
    public void BitFlag_InvalidShift_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var act = () => new BitFlag<byte>(8); // byte only has 8 bits (0-7)
        act.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => new BitFlag<byte>(-1);
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region BitField<TStorage, TField> Tests

    /// <summary>
    /// Tests BitField extraction and setting.
    /// </summary>
    [Fact]
    public void BitField_ExtractAndSet()
    {
        // Arrange - 4-bit field starting at bit 4
        var field = new BitField<ushort, byte>(4, 4);

        // Assert properties
        field.Shift.Should().Be(4);
        field.Width.Should().Be(4);

        // Test extraction
        field[(ushort)0x00F0].Should().Be((byte)0x0F);
        field[(ushort)0x0050].Should().Be((byte)0x05);
        field[(ushort)0xFFFF].Should().Be((byte)0x0F);

        // Test setting
        field.Set(0x0000, (byte)0x0A).Should().Be((ushort)0x00A0);
        field.Set(0xFF0F, (byte)0x05).Should().Be((ushort)0xFF5F);
    }

    /// <summary>
    /// Tests BitField with larger fields.
    /// </summary>
    [Fact]
    public void BitField_LargerField_ExtractAndSet()
    {
        // Arrange - 16-bit field starting at bit 8 in uint
        var field = new BitField<uint, ushort>(8, 16);

        // Assert properties
        field.Shift.Should().Be(8);
        field.Width.Should().Be(16);

        // Test extraction
        field[0x00ABCD00U].Should().Be((ushort)0xABCD);
        field[0x12345600U].Should().Be((ushort)0x3456);

        // Test setting
        field.Set(0x00000000U, (ushort)0x1234).Should().Be(0x00123400U);
        field.Set(0xFF0000FFU, (ushort)0xABCD).Should().Be(0xFFABCDFFU);
    }

    /// <summary>
    /// Tests BitField constructor throws for invalid parameters.
    /// </summary>
    [Fact]
    public void BitField_InvalidParameters_ThrowsArgumentOutOfRangeException()
    {
        // Negative shift
        var act1 = () => new BitField<uint, byte>(-1, 4);
        act1.Should().Throw<ArgumentOutOfRangeException>();

        // Zero width
        var act2 = () => new BitField<uint, byte>(0, 0);
        act2.Should().Throw<ArgumentOutOfRangeException>();

        // Exceeds storage size
        var act3 = () => new BitField<byte, byte>(4, 8); // 4 + 8 > 8
        act3.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region BitField64 Tests

    /// <summary>
    /// Tests BitField64 Get methods.
    /// </summary>
    [Fact]
    public void BitField64_GetMethods()
    {
        // Arrange - 8-bit field starting at bit 16
        var field = new BitField64(16, 8);

        // Assert properties
        field.Shift.Should().Be(16);
        field.Width.Should().Be(8);

        // Test all Get methods
        ulong value = 0x00000000_00AB0000UL;
        field.GetByte(value).Should().Be((byte)0xAB);
        field.GetUShort(value).Should().Be((ushort)0xAB);
        field.GetUInt(value).Should().Be(0xABU);
        field.GetULong(value).Should().Be(0xABUL);
    }

    /// <summary>
    /// Tests BitField64 Set methods.
    /// </summary>
    [Fact]
    public void BitField64_SetMethods()
    {
        // Arrange - 8-bit field starting at bit 16
        var field = new BitField64(16, 8);
        ulong initial = 0x00000000_00000000UL;

        // Test all Set methods
        field.Set(initial, (byte)0xAB).Should().Be(0x00000000_00AB0000UL);
        field.Set(initial, (ushort)0xCD).Should().Be(0x00000000_00CD0000UL);
        field.Set(initial, 0xEFU).Should().Be(0x00000000_00EF0000UL);
        field.Set(initial, 0x12UL).Should().Be(0x00000000_00120000UL);
    }

    /// <summary>
    /// Tests BitField64 constructor validation.
    /// </summary>
    [Fact]
    public void BitField64_InvalidParameters_ThrowsArgumentOutOfRangeException()
    {
        var act1 = () => new BitField64(-1, 8);
        act1.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => new BitField64(64, 1);
        act2.Should().Throw<ArgumentOutOfRangeException>();

        var act3 = () => new BitField64(60, 8); // 60 + 8 > 64
        act3.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region BitFlag64 Tests

    /// <summary>
    /// Tests BitFlag64 operations.
    /// </summary>
    [Fact]
    public void BitFlag64_Operations()
    {
        // Arrange
        var flag = new BitFlag64(32);

        // Assert properties
        flag.Shift.Should().Be(32);

        // Test IsSet
        flag.IsSet(0x0000000000000000UL).Should().BeFalse();
        flag.IsSet(0x0000000100000000UL).Should().BeTrue();

        // Test Set
        flag.Set(0x0000000000000000UL, true).Should().Be(0x0000000100000000UL);
        flag.Set(0x0000000100000000UL, false).Should().Be(0x0000000000000000UL);

        // Test Toggle
        flag.Toggle(0x0000000000000000UL).Should().Be(0x0000000100000000UL);
        flag.Toggle(0x0000000100000000UL).Should().Be(0x0000000000000000UL);
    }

    /// <summary>
    /// Tests BitFlag64 constructor validation.
    /// </summary>
    [Fact]
    public void BitFlag64_InvalidShift_ThrowsArgumentOutOfRangeException()
    {
        var act1 = () => new BitFlag64(-1);
        act1.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => new BitFlag64(64);
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region BitField32 Tests

    /// <summary>
    /// Tests BitField32 Get methods.
    /// </summary>
    [Fact]
    public void BitField32_GetMethods()
    {
        // Arrange - 8-bit field starting at bit 8
        var field = new BitField32(8, 8);

        // Assert properties
        field.Shift.Should().Be(8);
        field.Width.Should().Be(8);

        // Test Get methods
        uint value = 0x0000AB00U;
        field.GetByte(value).Should().Be((byte)0xAB);
        field.GetUShort(value).Should().Be((ushort)0xAB);
        field.GetUInt(value).Should().Be(0xABU);
    }

    /// <summary>
    /// Tests BitField32 Set methods.
    /// </summary>
    [Fact]
    public void BitField32_SetMethods()
    {
        // Arrange - 8-bit field starting at bit 8
        var field = new BitField32(8, 8);
        uint initial = 0x00000000U;

        // Test Set methods
        field.Set(initial, (byte)0xAB).Should().Be(0x0000AB00U);
        field.Set(initial, (ushort)0xCD).Should().Be(0x0000CD00U);
        field.Set(initial, 0xEFU).Should().Be(0x0000EF00U);
    }

    /// <summary>
    /// Tests BitField32 constructor validation.
    /// </summary>
    [Fact]
    public void BitField32_InvalidParameters_ThrowsArgumentOutOfRangeException()
    {
        var act1 = () => new BitField32(-1, 8);
        act1.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => new BitField32(32, 1);
        act2.Should().Throw<ArgumentOutOfRangeException>();

        var act3 = () => new BitField32(28, 8); // 28 + 8 > 32
        act3.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region BitFlag32 Tests

    /// <summary>
    /// Tests BitFlag32 operations.
    /// </summary>
    [Fact]
    public void BitFlag32_Operations()
    {
        // Arrange
        var flag = new BitFlag32(16);

        // Assert properties
        flag.Shift.Should().Be(16);

        // Test IsSet
        flag.IsSet(0x00000000U).Should().BeFalse();
        flag.IsSet(0x00010000U).Should().BeTrue();

        // Test Set
        flag.Set(0x00000000U, true).Should().Be(0x00010000U);
        flag.Set(0x00010000U, false).Should().Be(0x00000000U);

        // Test Toggle
        flag.Toggle(0x00000000U).Should().Be(0x00010000U);
        flag.Toggle(0x00010000U).Should().Be(0x00000000U);
    }

    /// <summary>
    /// Tests BitFlag32 constructor validation.
    /// </summary>
    [Fact]
    public void BitFlag32_InvalidShift_ThrowsArgumentOutOfRangeException()
    {
        var act1 = () => new BitFlag32(-1);
        act1.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => new BitFlag32(32);
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region BitField16 Tests

    /// <summary>
    /// Tests BitField16 Get methods.
    /// </summary>
    [Fact]
    public void BitField16_GetMethods()
    {
        // Arrange - 4-bit field starting at bit 4
        var field = new BitField16(4, 4);

        // Assert properties
        field.Shift.Should().Be(4);
        field.Width.Should().Be(4);

        // Test Get methods
        ushort value = 0x00A0;
        field.GetByte(value).Should().Be((byte)0x0A);
        field.GetUShort(value).Should().Be((ushort)0x0A);
    }

    /// <summary>
    /// Tests BitField16 Set methods.
    /// </summary>
    [Fact]
    public void BitField16_SetMethods()
    {
        // Arrange - 4-bit field starting at bit 4
        var field = new BitField16(4, 4);
        ushort initial = 0x0000;

        // Test Set methods
        field.Set(initial, (byte)0x0A).Should().Be((ushort)0x00A0);
        field.Set(initial, (ushort)0x0B).Should().Be((ushort)0x00B0);
    }

    /// <summary>
    /// Tests BitField16 constructor validation.
    /// </summary>
    [Fact]
    public void BitField16_InvalidParameters_ThrowsArgumentOutOfRangeException()
    {
        var act1 = () => new BitField16(-1, 4);
        act1.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => new BitField16(16, 1);
        act2.Should().Throw<ArgumentOutOfRangeException>();

        var act3 = () => new BitField16(12, 8); // 12 + 8 > 16
        act3.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region BitFlag16 Tests

    /// <summary>
    /// Tests BitFlag16 operations.
    /// </summary>
    [Fact]
    public void BitFlag16_Operations()
    {
        // Arrange
        var flag = new BitFlag16(8);

        // Assert properties
        flag.Shift.Should().Be(8);

        // Test IsSet
        flag.IsSet(0x0000).Should().BeFalse();
        flag.IsSet(0x0100).Should().BeTrue();

        // Test Set
        flag.Set(0x0000, true).Should().Be((ushort)0x0100);
        flag.Set(0x0100, false).Should().Be((ushort)0x0000);

        // Test Toggle
        flag.Toggle(0x0000).Should().Be((ushort)0x0100);
        flag.Toggle(0x0100).Should().Be((ushort)0x0000);
    }

    /// <summary>
    /// Tests BitFlag16 constructor validation.
    /// </summary>
    [Fact]
    public void BitFlag16_InvalidShift_ThrowsArgumentOutOfRangeException()
    {
        var act1 = () => new BitFlag16(-1);
        act1.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => new BitFlag16(16);
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region BitField8 Tests

    /// <summary>
    /// Tests BitField8 Get method.
    /// </summary>
    [Fact]
    public void BitField8_GetByte()
    {
        // Arrange - 4-bit field starting at bit 2
        var field = new BitField8(2, 4);

        // Assert properties
        field.Shift.Should().Be(2);
        field.Width.Should().Be(4);

        // Test GetByte
        byte value = 0x3C; // binary: 00111100
        field.GetByte(value).Should().Be((byte)0x0F);
    }

    /// <summary>
    /// Tests BitField8 Set method.
    /// </summary>
    [Fact]
    public void BitField8_SetByte()
    {
        // Arrange - 4-bit field starting at bit 2
        var field = new BitField8(2, 4);
        byte initial = 0x00;

        // Test Set method
        field.Set(initial, (byte)0x0A).Should().Be((byte)0x28); // 0x0A << 2 = 0x28
    }

    /// <summary>
    /// Tests BitField8 constructor validation.
    /// </summary>
    [Fact]
    public void BitField8_InvalidParameters_ThrowsArgumentOutOfRangeException()
    {
        var act1 = () => new BitField8(-1, 4);
        act1.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => new BitField8(8, 1);
        act2.Should().Throw<ArgumentOutOfRangeException>();

        var act3 = () => new BitField8(4, 8); // 4 + 8 > 8
        act3.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region BitFlag8 Tests

    /// <summary>
    /// Tests BitFlag8 operations.
    /// </summary>
    [Fact]
    public void BitFlag8_Operations()
    {
        // Arrange
        var flag = new BitFlag8(4);

        // Assert properties
        flag.Shift.Should().Be(4);

        // Test IsSet
        flag.IsSet(0x00).Should().BeFalse();
        flag.IsSet(0x10).Should().BeTrue();

        // Test Set
        flag.Set(0x00, true).Should().Be((byte)0x10);
        flag.Set(0x10, false).Should().Be((byte)0x00);

        // Test Toggle
        flag.Toggle(0x00).Should().Be((byte)0x10);
        flag.Toggle(0x10).Should().Be((byte)0x00);
    }

    /// <summary>
    /// Tests BitFlag8 constructor validation.
    /// </summary>
    [Fact]
    public void BitFlag8_InvalidShift_ThrowsArgumentOutOfRangeException()
    {
        var act1 = () => new BitFlag8(-1);
        act1.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => new BitFlag8(8);
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Generated BitFields Struct Tests

    /// <summary>
    /// Tests generated 8-bit BitFields struct - flags.
    /// </summary>
    [Fact]
    public void GeneratedStatusReg8_Flags_GetAndSet()
    {
        var reg = new GeneratedStatusReg8();

        // Initial state
        reg.Value.Should().Be(0);
        reg.Ready.Should().BeFalse();
        reg.Error.Should().BeFalse();
        reg.Busy.Should().BeFalse();

        // Set flags
        reg.Ready = true;
        reg.Ready.Should().BeTrue();
        reg.Value.Should().Be(0x01);

        reg.Error = true;
        reg.Error.Should().BeTrue();
        reg.Value.Should().Be(0x03);

        reg.Busy = true;
        reg.Busy.Should().BeTrue();
        reg.Value.Should().Be(0x83);

        // Clear flags
        reg.Ready = false;
        reg.Ready.Should().BeFalse();
        reg.Value.Should().Be(0x82);
    }

    /// <summary>
    /// Tests generated 8-bit BitFields struct - multi-bit fields.
    /// </summary>
    [Fact]
    public void GeneratedStatusReg8_Fields_GetAndSet()
    {
        var reg = new GeneratedStatusReg8();

        // Set Mode (bits 2-4, 3 bits wide)
        reg.Mode = 5;
        reg.Mode.Should().Be(5);
        reg.Value.Should().Be(0x14); // 5 << 2 = 0x14

        // Set Priority (bits 5-6, 2 bits wide)
        reg.Priority = 3;
        reg.Priority.Should().Be(3);
        reg.Value.Should().Be(0x74); // 0x14 | (3 << 5) = 0x74

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
        reg.Value.Should().Be(0xFF);
        reg.Ready.Should().BeTrue();
        reg.Error.Should().BeTrue();
        reg.Busy.Should().BeTrue();

        // To byte
        byte value = reg;
        value.Should().Be(0xFF);
    }

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
        reg.Value.Should().Be(0x9A00);
    }

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
        reg.Value.Should().Be(0x3FABCDEF);
    }

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
        reg.Value.Should().Be(expected);
    }

    #endregion
}

#region Generated BitFields Test Structs

/// <summary>
/// 8-bit status register for testing code generation.
/// </summary>
[BitFields]
public partial struct GeneratedStatusReg8
{
    public byte Value;

    [BitFlag(0)] public partial bool Ready { get; set; }
    [BitFlag(1)] public partial bool Error { get; set; }
    [BitFlag(7)] public partial bool Busy { get; set; }
    [BitField(2, 3)] public partial byte Mode { get; set; }
    [BitField(5, 2)] public partial byte Priority { get; set; }
}

/// <summary>
/// 16-bit keyboard register for testing code generation.
/// </summary>
[BitFields]
public partial struct GeneratedKeyboardReg16
{
    public ushort Value;

    [BitField(0, 7)] public partial byte SecondKeyCode { get; set; }
    [BitFlag(7)] public partial bool SecondKeyUp { get; set; }
    [BitField(8, 7)] public partial byte FirstKeyCode { get; set; }
    [BitFlag(15)] public partial bool FirstKeyUp { get; set; }
}

/// <summary>
/// 32-bit control register for testing code generation.
/// </summary>
[BitFields]
public partial struct GeneratedControlReg32
{
    public uint Value;

    [BitField(0, 24)] public partial uint Address { get; set; }
    [BitField(24, 4)] public partial byte Command { get; set; }
    [BitFlag(28)] public partial bool Enable { get; set; }
    [BitFlag(29)] public partial bool Interrupt { get; set; }
}

/// <summary>
/// 64-bit wide register for testing code generation.
/// </summary>
[BitFields]
public partial struct GeneratedWideReg64
{
    public ulong Value;

    [BitField(0, 8)] public partial byte Status { get; set; }
    [BitField(8, 16)] public partial ushort Data { get; set; }
    [BitField(24, 32)] public partial uint Address { get; set; }
    [BitFlag(56)] public partial bool Valid { get; set; }
    [BitFlag(57)] public partial bool Ready { get; set; }
}

#endregion
