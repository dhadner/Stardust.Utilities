using FluentAssertions;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Unit tests for the BitField types.
/// </summary>
public class BitFieldTests
{
    #region BitFlagDef<TStorage> Tests

    /// <summary>
    /// Tests BitFlagDef with byte storage.
    /// </summary>
    [Fact]
    public void BitFlagDef_Byte_GetAndSet()
    {
        // Arrange
        var flag = new BitFlagDef<byte>(3); // bit position 3

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
    /// Tests BitFlagDef with ushort storage.
    /// </summary>
    [Fact]
    public void BitFlagDef_UShort_GetAndSet()
    {
        // Arrange
        var flag = new BitFlagDef<ushort>(10); // bit position 10

        // Assert initial state
        flag.Shift.Should().Be(10);
        flag[(ushort)0x0000].Should().BeFalse();
        flag[(ushort)0x0400].Should().BeTrue(); // bit 10 set

        // Test Set
        flag.Set(0x0000, true).Should().Be((ushort)0x0400);
        flag.Set(0x0400, false).Should().Be((ushort)0x0000);
    }

    /// <summary>
    /// Tests BitFlagDef with uint storage.
    /// </summary>
    [Fact]
    public void BitFlagDef_UInt_GetAndSet()
    {
        // Arrange
        var flag = new BitFlagDef<uint>(20); // bit position 20

        // Assert initial state
        flag.Shift.Should().Be(20);
        flag[0x00000000U].Should().BeFalse();
        flag[0x00100000U].Should().BeTrue(); // bit 20 set

        // Test Set
        flag.Set(0x00000000U, true).Should().Be(0x00100000U);
        flag.Set(0x00100000U, false).Should().Be(0x00000000U);
    }

    /// <summary>
    /// Tests BitFlagDef with ulong storage.
    /// </summary>
    [Fact]
    public void BitFlagDef_ULong_GetAndSet()
    {
        // Arrange
        var flag = new BitFlagDef<ulong>(40); // bit position 40

        // Assert initial state
        flag.Shift.Should().Be(40);
        flag[0x0000000000000000UL].Should().BeFalse();
        flag[0x0000010000000000UL].Should().BeTrue(); // bit 40 set

        // Test Set
        flag.Set(0x0000000000000000UL, true).Should().Be(0x0000010000000000UL);
        flag.Set(0x0000010000000000UL, false).Should().Be(0x0000000000000000UL);
    }

    /// <summary>
    /// Tests BitFlagDef constructor throws for invalid shift.
    /// </summary>
    [Fact]
    public void BitFlagDef_InvalidShift_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var act = () => new BitFlagDef<byte>(8); // byte only has 8 bits (0-7)
        act.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => new BitFlagDef<byte>(-1);
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region BitFieldDef<TStorage, TField> Tests

    /// <summary>
    /// Tests BitFieldDef extraction and setting.
    /// </summary>
    [Fact]
    public void BitFieldDef_ExtractAndSet()
    {
        // Arrange - 4-bit field starting at bit 4
        var field = new BitFieldDef<ushort, byte>(4, 4);

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
    /// Tests BitFieldDef with larger fields.
    /// </summary>
    [Fact]
    public void BitFieldDef_LargerField_ExtractAndSet()
    {
        // Arrange - 16-bit field starting at bit 8 in uint
        var field = new BitFieldDef<uint, ushort>(8, 16);

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
    /// Tests BitFieldDef constructor throws for invalid parameters.
    /// </summary>
    [Fact]
    public void BitFieldDef_InvalidParameters_ThrowsArgumentOutOfRangeException()
    {
        // Negative shift
        var act1 = () => new BitFieldDef<uint, byte>(-1, 4);
        act1.Should().Throw<ArgumentOutOfRangeException>();

        // Zero width
        var act2 = () => new BitFieldDef<uint, byte>(0, 0);
        act2.Should().Throw<ArgumentOutOfRangeException>();

        // Exceeds storage size
        var act3 = () => new BitFieldDef<byte, byte>(4, 8); // 4 + 8 > 8
        act3.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region BitFieldDef64 Tests

    /// <summary>
    /// Tests BitFieldDef64 Get methods.
    /// </summary>
    [Fact]
    public void BitFieldDef64_GetMethods()
    {
        // Arrange - 8-bit field starting at bit 16
        var field = new BitFieldDef64(16, 8);

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
    /// Tests BitFieldDef64 Set methods.
    /// </summary>
    [Fact]
    public void BitFieldDef64_SetMethods()
    {
        // Arrange - 8-bit field starting at bit 16
        var field = new BitFieldDef64(16, 8);
        ulong initial = 0x00000000_00000000UL;

        // Test all Set methods
        field.Set(initial, (byte)0xAB).Should().Be(0x00000000_00AB0000UL);
        field.Set(initial, (ushort)0xCD).Should().Be(0x00000000_00CD0000UL);
        field.Set(initial, 0xEFU).Should().Be(0x00000000_00EF0000UL);
        field.Set(initial, 0x12UL).Should().Be(0x00000000_00120000UL);
    }

    /// <summary>
    /// Tests BitFieldDef64 constructor validation.
    /// </summary>
    [Fact]
    public void BitFieldDef64_InvalidParameters_ThrowsArgumentOutOfRangeException()
    {
        var act1 = () => new BitFieldDef64(-1, 8);
        act1.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => new BitFieldDef64(64, 1);
        act2.Should().Throw<ArgumentOutOfRangeException>();

        var act3 = () => new BitFieldDef64(60, 8); // 60 + 8 > 64
        act3.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region BitFlagDef64 Tests

    /// <summary>
    /// Tests BitFlagDef64 operations.
    /// </summary>
    [Fact]
    public void BitFlagDef64_Operations()
    {
        // Arrange
        var flag = new BitFlagDef64(32);

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
    /// Tests BitFlagDef64 constructor validation.
    /// </summary>
    [Fact]
    public void BitFlagDef64_InvalidShift_ThrowsArgumentOutOfRangeException()
    {
        var act1 = () => new BitFlagDef64(-1);
        act1.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => new BitFlagDef64(64);
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region BitFieldDef32 Tests

    /// <summary>
    /// Tests BitFieldDef32 Get methods.
    /// </summary>
    [Fact]
    public void BitFieldDef32_GetMethods()
    {
        // Arrange - 8-bit field starting at bit 8
        var field = new BitFieldDef32(8, 8);

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
    /// Tests BitFieldDef32 Set methods.
    /// </summary>
    [Fact]
    public void BitFieldDef32_SetMethods()
    {
        // Arrange - 8-bit field starting at bit 8
        var field = new BitFieldDef32(8, 8);
        uint initial = 0x00000000U;

        // Test Set methods
        field.Set(initial, (byte)0xAB).Should().Be(0x0000AB00U);
        field.Set(initial, (ushort)0xCD).Should().Be(0x0000CD00U);
        field.Set(initial, 0xEFU).Should().Be(0x0000EF00U);
    }

    /// <summary>
    /// Tests BitFieldDef32 constructor validation.
    /// </summary>
    [Fact]
    public void BitFieldDef32_InvalidParameters_ThrowsArgumentOutOfRangeException()
    {
        var act1 = () => new BitFieldDef32(-1, 8);
        act1.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => new BitFieldDef32(32, 1);
        act2.Should().Throw<ArgumentOutOfRangeException>();

        var act3 = () => new BitFieldDef32(28, 8); // 28 + 8 > 32
        act3.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region BitFlagDef32 Tests

    /// <summary>
    /// Tests BitFlagDef32 operations.
    /// </summary>
    [Fact]
    public void BitFlagDef32_Operations()
    {
        // Arrange
        var flag = new BitFlagDef32(16);

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
    /// Tests BitFlagDef32 constructor validation.
    /// </summary>
    [Fact]
    public void BitFlagDef32_InvalidShift_ThrowsArgumentOutOfRangeException()
    {
        var act1 = () => new BitFlagDef32(-1);
        act1.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => new BitFlagDef32(32);
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region BitFieldDef16 Tests

    /// <summary>
    /// Tests BitFieldDef16 Get methods.
    /// </summary>
    [Fact]
    public void BitFieldDef16_GetMethods()
    {
        // Arrange - 4-bit field starting at bit 4
        var field = new BitFieldDef16(4, 4);

        // Assert properties
        field.Shift.Should().Be(4);
        field.Width.Should().Be(4);

        // Test Get methods
        ushort value = 0x00A0;
        field.GetByte(value).Should().Be((byte)0x0A);
        field.GetUShort(value).Should().Be((ushort)0x0A);
    }

    /// <summary>
    /// Tests BitFieldDef16 Set methods.
    /// </summary>
    [Fact]
    public void BitFieldDef16_SetMethods()
    {
        // Arrange - 4-bit field starting at bit 4
        var field = new BitFieldDef16(4, 4);
        ushort initial = 0x0000;

        // Test Set methods
        field.Set(initial, (byte)0x0A).Should().Be((ushort)0x00A0);
        field.Set(initial, (ushort)0x0B).Should().Be((ushort)0x00B0);
    }

    /// <summary>
    /// Tests BitFieldDef16 constructor validation.
    /// </summary>
    [Fact]
    public void BitFieldDef16_InvalidParameters_ThrowsArgumentOutOfRangeException()
    {
        var act1 = () => new BitFieldDef16(-1, 4);
        act1.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => new BitFieldDef16(16, 1);
        act2.Should().Throw<ArgumentOutOfRangeException>();

        var act3 = () => new BitFieldDef16(12, 8); // 12 + 8 > 16
        act3.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region BitFlagDef16 Tests

    /// <summary>
    /// Tests BitFlagDef16 operations.
    /// </summary>
    [Fact]
    public void BitFlagDef16_Operations()
    {
        // Arrange
        var flag = new BitFlagDef16(8);

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
    /// Tests BitFlagDef16 constructor validation.
    /// </summary>
    [Fact]
    public void BitFlagDef16_InvalidShift_ThrowsArgumentOutOfRangeException()
    {
        var act1 = () => new BitFlagDef16(-1);
        act1.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => new BitFlagDef16(16);
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region BitFieldDef8 Tests

    /// <summary>
    /// Tests BitFieldDef8 Get method.
    /// </summary>
    [Fact]
    public void BitFieldDef8_GetByte()
    {
        // Arrange - 4-bit field starting at bit 2
        var field = new BitFieldDef8(2, 4);

        // Assert properties
        field.Shift.Should().Be(2);
        field.Width.Should().Be(4);

        // Test GetByte
        byte value = 0x3C; // binary: 00111100
        field.GetByte(value).Should().Be((byte)0x0F);
    }

    /// <summary>
    /// Tests BitFieldDef8 Set method.
    /// </summary>
    [Fact]
    public void BitFieldDef8_SetByte()
    {
        // Arrange - 4-bit field starting at bit 2
        var field = new BitFieldDef8(2, 4);
        byte initial = 0x00;

        // Test Set method
        field.Set(initial, (byte)0x0A).Should().Be((byte)0x28); // 0x0A << 2 = 0x28
    }

    /// <summary>
    /// Tests BitFieldDef8 constructor validation.
    /// </summary>
    [Fact]
    public void BitFieldDef8_InvalidParameters_ThrowsArgumentOutOfRangeException()
    {
        var act1 = () => new BitFieldDef8(-1, 4);
        act1.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => new BitFieldDef8(8, 1);
        act2.Should().Throw<ArgumentOutOfRangeException>();

        var act3 = () => new BitFieldDef8(4, 8); // 4 + 8 > 8
        act3.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region BitFlagDef8 Tests

    /// <summary>
    /// Tests BitFlagDef8 operations.
    /// </summary>
    [Fact]
    public void BitFlagDef8_Operations()
    {
        // Arrange
        var flag = new BitFlagDef8(4);

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
    /// Tests BitFlagDef8 constructor validation.
    /// </summary>
    [Fact]
    public void BitFlagDef8_InvalidShift_ThrowsArgumentOutOfRangeException()
    {
        var act1 = () => new BitFlagDef8(-1);
        act1.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => new BitFlagDef8(8);
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion
}
