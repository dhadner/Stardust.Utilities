using System.Text.Json;
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
            [BitField(3, End = 4)] public partial byte FieldC { get; set; }  // bits 3..=4 (2 bits)
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
        reg.Mode = OpMode.Mode5;
        reg.Mode.Should().Be(OpMode.Mode5);
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
    /// Note: GeneratedControlReg32 uses Native mode (default), so undefined bits are preserved.
    /// </summary>
    [Fact]
    public void GeneratedControlReg32_ImplicitConversion()
    {
        // From uint - Native mode preserves all bits including undefined
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
    /// Note: GeneratedWideReg64 uses Native mode (default), so undefined bits are preserved.
    /// </summary>
    [Fact]
    public void GeneratedWideReg64_ImplicitConversion()
    {
        // From ulong - Native mode preserves all bits
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
        reg.Mode = OpMode.Mode7;
        reg.Mode.Should().Be(OpMode.Mode7);

        // Priority is 2 bits (max value 3)
        reg.Priority = 3;
        reg.Priority.Should().Be(3);

        // Values exceeding field width should be truncated
        reg.Mode = (OpMode)0xFF;  // Should become 7 (0b111)
        reg.Mode.Should().Be(OpMode.Mode7);
    }

    #endregion

    #region Static Bit/Mask Property Tests

    /// <summary>
    /// Tests static Bit properties for BitFlags.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_StaticBitProperties()
    {
        // Each static Bit property should return a struct with only that bit set
        ((byte)GeneratedStatusReg8.ReadyBit).Should().Be(0x01);  // bit 0
        ((byte)GeneratedStatusReg8.ErrorBit).Should().Be(0x02);  // bit 1
        ((byte)GeneratedStatusReg8.BusyBit).Should().Be(0x80);   // bit 7

        // Using static Bit properties to set/clear flags
        GeneratedStatusReg8 reg = 0;
        reg = reg | GeneratedStatusReg8.ReadyBit;
        reg.Ready.Should().BeTrue();
        ((byte)reg).Should().Be(0x01);

        // Combine multiple flags
        reg = GeneratedStatusReg8.ReadyBit | GeneratedStatusReg8.ErrorBit;
        ((byte)reg).Should().Be(0x03);
    }

    /// <summary>
    /// Tests static Mask properties for BitFields.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_StaticMaskProperties()
    {
        // Each static Mask property should return a struct with the mask for that field
        ((byte)GeneratedStatusReg8.ModeMask).Should().Be(0x1C);     // bits 2-4
        ((byte)GeneratedStatusReg8.PriorityMask).Should().Be(0x60); // bits 5-6

        // Using masks to clear a field
        GeneratedStatusReg8 reg = 0xFF;
        reg = reg & ~GeneratedStatusReg8.ModeMask;
        ((byte)reg).Should().Be(0xE3);  // 0xFF & ~0x1C
        reg.Mode.Should().Be(0);
        reg.Ready.Should().BeTrue();
        reg.Busy.Should().BeTrue();
    }

    #endregion

    #region Bitwise Operator Tests

    /// <summary>
    /// Tests bitwise OR operator.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_BitwiseOr()
    {
        GeneratedStatusReg8 a = 0x01;
        GeneratedStatusReg8 b = 0x02;

        // Struct | Struct
        GeneratedStatusReg8 result = a | b;
        ((byte)result).Should().Be(0x03);

        // Struct | byte
        result = a | (byte)0x80;
        ((byte)result).Should().Be(0x81);

        // byte | Struct
        result = (byte)0x40 | b;
        ((byte)result).Should().Be(0x42);
    }

    /// <summary>
    /// Tests bitwise AND operator.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_BitwiseAnd()
    {
        GeneratedStatusReg8 a = 0xFF;
        GeneratedStatusReg8 b = 0x0F;

        // Struct & Struct
        GeneratedStatusReg8 result = a & b;
        ((byte)result).Should().Be(0x0F);

        // Struct & byte
        result = a & (byte)0xF0;
        ((byte)result).Should().Be(0xF0);

        // byte & Struct
        result = (byte)0x55 & b;
        ((byte)result).Should().Be(0x05);
    }

    /// <summary>
    /// Tests bitwise XOR operator.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_BitwiseXor()
    {
        GeneratedStatusReg8 a = 0xAA;
        GeneratedStatusReg8 b = 0x55;

        // Struct ^ Struct
        GeneratedStatusReg8 result = a ^ b;
        ((byte)result).Should().Be(0xFF);

        // Struct ^ byte
        result = a ^ (byte)0xFF;
        ((byte)result).Should().Be(0x55);

        // byte ^ Struct
        result = (byte)0xFF ^ b;
        ((byte)result).Should().Be(0xAA);
    }

    /// <summary>
    /// Tests bitwise complement operator.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_BitwiseComplement()
    {
        GeneratedStatusReg8 a = 0x0F;

        // ~Struct
        GeneratedStatusReg8 result = ~a;
        ((byte)result).Should().Be(0xF0);

        // Double complement should give original
        result = ~~a;
        ((byte)result).Should().Be(0x0F);
    }

    /// <summary>
    /// Tests combining bitwise operators in complex expressions.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_CombinedBitwiseOperations()
    {
        GeneratedStatusReg8 reg = 0xFF;

        // Clear Ready flag using complement and AND
        reg = reg & ~GeneratedStatusReg8.ReadyBit;
        reg.Ready.Should().BeFalse();
        ((byte)reg).Should().Be(0xFE);

        // Set multiple flags using OR
        reg = 0;
        reg = reg | GeneratedStatusReg8.ReadyBit | GeneratedStatusReg8.BusyBit;
        ((byte)reg).Should().Be(0x81);

        // Toggle a flag using XOR
        reg = reg ^ GeneratedStatusReg8.ReadyBit;
        reg.Ready.Should().BeFalse();
        ((byte)reg).Should().Be(0x80);
    }

    #endregion

    #region Equality Operator Tests

    /// <summary>
    /// Tests equality operator.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_EqualityOperator()
    {
        GeneratedStatusReg8 a = 0x42;
        GeneratedStatusReg8 b = 0x42;
        GeneratedStatusReg8 c = 0x24;

        (a == b).Should().BeTrue();
        (a == c).Should().BeFalse();
        (b == c).Should().BeFalse();

        // Default value comparison
        GeneratedStatusReg8 zero1 = 0;
        GeneratedStatusReg8 zero2 = default;
        (zero1 == zero2).Should().BeTrue();
    }

    /// <summary>
    /// Tests inequality operator.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_InequalityOperator()
    {
        GeneratedStatusReg8 a = 0x42;
        GeneratedStatusReg8 b = 0x42;
        GeneratedStatusReg8 c = 0x24;

        (a != b).Should().BeFalse();
        (a != c).Should().BeTrue();
        (b != c).Should().BeTrue();
    }

    /// <summary>
    /// Tests Equals method.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_EqualsMethod()
    {
        GeneratedStatusReg8 a = 0x42;
        GeneratedStatusReg8 b = 0x42;
        GeneratedStatusReg8 c = 0x24;

        a.Equals(b).Should().BeTrue();
        a.Equals(c).Should().BeFalse();
        a.Equals(null).Should().BeFalse();
        a.Equals("not a struct").Should().BeFalse();
    }

    /// <summary>
    /// Tests GetHashCode method.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_GetHashCode()
    {
        GeneratedStatusReg8 a = 0x42;
        GeneratedStatusReg8 b = 0x42;
        GeneratedStatusReg8 c = 0x24;


        a.GetHashCode().Should().Be(b.GetHashCode());
        a.GetHashCode().Should().NotBe(c.GetHashCode());
    }

    /// <summary>
    /// Tests ToString method.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_ToString()
    {
        GeneratedStatusReg8 a = 0x42;
        a.ToString().Should().Be("0x42");

        GeneratedStatusReg8 b = 0xFF;
        b.ToString().Should().Be("0xFF");
    }

    #endregion



    #region Parsing Tests (IParsable<T> and ISpanParsable<T>)

    /// <summary>
    /// Tests Parse method with decimal string.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_Parse_Decimal()
    {
        var result = GeneratedStatusReg8.Parse("66");
        ((byte)result).Should().Be(66);
        result.ToString().Should().Be("0x42");
    }

    /// <summary>
    /// Tests Parse method with hex string (0x prefix).
    /// </summary>
    [Fact]
    public void GeneratedBitFields_Parse_HexWith0xPrefix()
    {
        var result = GeneratedStatusReg8.Parse("0x42");
        ((byte)result).Should().Be(0x42);
    }

    /// <summary>
    /// Tests Parse method with hex string (0X prefix, uppercase).
    /// </summary>
    [Fact]
    public void GeneratedBitFields_Parse_HexWith0XPrefix()
    {
        var result = GeneratedStatusReg8.Parse("0XFF");
        ((byte)result).Should().Be(0xFF);
    }

    /// <summary>
    /// Tests Parse method throws on invalid input.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_Parse_ThrowsOnInvalidInput()
    {
        FluentActions.Invoking(() => GeneratedStatusReg8.Parse("not a number"))
            .Should().Throw<FormatException>();
    }

    /// <summary>
    /// Tests Parse method throws on null input.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_Parse_ThrowsOnNull()
    {
        FluentActions.Invoking(() => GeneratedStatusReg8.Parse(null!))
            .Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests TryParse returns true and outputs correct value for valid input.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_TryParse_ValidInput()
    {
        var success = GeneratedStatusReg8.TryParse("66", out var result);
        success.Should().BeTrue();
        ((byte)result).Should().Be(66);
    }

    /// <summary>
    /// Tests TryParse with hex input.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_TryParse_HexInput()
    {
        var success = GeneratedStatusReg8.TryParse("0xFF", out var result);
        success.Should().BeTrue();
        ((byte)result).Should().Be(0xFF);
    }

    /// <summary>
    /// Tests TryParse returns false for invalid input.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_TryParse_InvalidInput()
    {
        var success = GeneratedStatusReg8.TryParse("not a number", out var result);
        success.Should().BeFalse();
        ((byte)result).Should().Be(0); // default value
    }

    /// <summary>
    /// Tests TryParse returns false for null input.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_TryParse_NullInput()
    {
        var success = GeneratedStatusReg8.TryParse(null, out var result);
        success.Should().BeFalse();
        ((byte)result).Should().Be(0);
    }

    /// <summary>
    /// Tests Parse with IFormatProvider.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_Parse_WithFormatProvider()
    {
        var result = GeneratedStatusReg8.Parse("42", System.Globalization.CultureInfo.InvariantCulture);
        ((byte)result).Should().Be(42);
    }

    /// <summary>
    /// Tests TryParse with IFormatProvider.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_TryParse_WithFormatProvider()
    {
        var success = GeneratedStatusReg8.TryParse("255", System.Globalization.CultureInfo.InvariantCulture, out var result);
        success.Should().BeTrue();
        ((byte)result).Should().Be(255);
    }

    /// <summary>
    /// Tests Parse with ReadOnlySpan&lt;char&gt;.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_Parse_Span()
    {
        var input = "0xAB".AsSpan();
        var result = GeneratedStatusReg8.Parse(input, null);
        ((byte)result).Should().Be(0xAB);
    }

    /// <summary>
    /// Tests TryParse with ReadOnlySpan&lt;char&gt;.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_TryParse_Span()
    {
        var input = "123".AsSpan();
        var success = GeneratedStatusReg8.TryParse(input, null, out var result);
        success.Should().BeTrue();
        ((byte)result).Should().Be(123);
    }

    /// <summary>
    /// Tests parsing for 16-bit BitFields struct.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_Parse_16Bit()
    {
        var result = GeneratedKeyboardReg16.Parse("0xABCD");
        ((ushort)result).Should().Be(0xABCD);
        result.FirstKeyCode.Should().Be(0x2B); // bits 8-14
        result.SecondKeyCode.Should().Be(0x4D); // bits 0-6
    }

    /// <summary>
    /// Tests parsing for 32-bit BitFields struct.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_Parse_32Bit()
    {
        var result = GeneratedControlReg32.Parse("0x12345678");
        ((uint)result).Should().Be(0x12345678);
    }

    /// <summary>
    /// Tests parsing for 64-bit BitFields struct.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_Parse_64Bit()
    {
        var result = GeneratedWideReg64.Parse("0x123456789ABCDEF0");
        ((ulong)result).Should().Be(0x123456789ABCDEF0);
    }

    /// <summary>
    /// Tests that parsed value can be used with properties.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_Parse_ThenUseProperties()
    {
        var reg = GeneratedStatusReg8.Parse("0x83");
        reg.Ready.Should().BeTrue();   // bit 0
        reg.Error.Should().BeTrue();   // bit 1
        reg.Busy.Should().BeTrue();    // bit 7
        reg.Mode.Should().Be(0);       // bits 2-4
        reg.Priority.Should().Be(0);   // bits 5-6
    }

    /// <summary>
    /// Tests round-trip: Parse then ToString.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_Parse_RoundTrip()
    {
        var original = "0xFF";
        var parsed = GeneratedStatusReg8.Parse(original);
        parsed.ToString().Should().Be(original);
    }

    #endregion


    #region With{Name} Fluent Method Tests

    /// <summary>
    /// Tests With{Name} methods for BitFlags.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_WithBitFlagMethods()
    {
        GeneratedStatusReg8 reg = 0;

        // Set a flag using With method
        var result = reg.WithReady(true);
        result.Ready.Should().BeTrue();
        ((byte)result).Should().Be(0x01);

        // Original should be unchanged (immutable pattern)
        reg.Ready.Should().BeFalse();
        ((byte)reg).Should().Be(0x00);

        // Clear a flag
        GeneratedStatusReg8 full = 0xFF;
        var cleared = full.WithReady(false);
        cleared.Ready.Should().BeFalse();
        ((byte)cleared).Should().Be(0xFE);
    }

    /// <summary>
    /// Tests With{Name} methods for BitFields.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_WithBitFieldMethods()
    {
        GeneratedStatusReg8 reg = 0;

        // Set a multi-bit field using With method
        var result = reg.WithMode(OpMode.Mode5);
        result.Mode.Should().Be(OpMode.Mode5);
        ((byte)result).Should().Be(0x14); // 5 << 2 = 0x14

        // Original should be unchanged
        reg.Mode.Should().Be(0);

        // Change a field in an existing register
        GeneratedStatusReg8 existing = 0xFF;
        var changed = existing.WithMode(0);
        changed.Mode.Should().Be(0);
        ((byte)changed).Should().Be(0xE3); // 0xFF & ~0x1C
    }

    /// <summary>
    /// Tests chaining multiple With{Name} methods.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_WithMethodChaining()
    {
        GeneratedStatusReg8 reg = 0;

        // Chain multiple With methods
        var result = reg
            .WithReady(true)
            .WithError(true)
            .WithMode(OpMode.Mode5)
            .WithPriority(2);

        result.Ready.Should().BeTrue();
        result.Error.Should().BeTrue();
        result.Mode.Should().Be(OpMode.Mode5);
        result.Priority.Should().Be(2);

        // Ready (bit 0) = 1, Error (bit 1) = 1, Mode (bits 2-4) = 5, Priority (bits 5-6) = 2
        // Expected: 0x01 | 0x02 | 0x14 | 0x40 = 0x57
        ((byte)result).Should().Be(0x57);
    }

    /// <summary>
    /// Tests that With methods work for enum property types at bit 0 (shift == 0).
    /// Regression: the generator must cast the value to the storage type before
    /// masking, otherwise the generated code produces a compile error for enums.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_WithEnumAtBitZero()
    {
        EnumAtBitZeroReg reg = 0;

        // With on the shift-0 enum field
        var r1 = reg.WithCommand(OpMode.Mode5);
        r1.Command.Should().Be(OpMode.Mode5);
        ((byte)r1).Should().Be(0x05);

        // With on the shifted enum field (shift 3) -- should also work
        var r2 = reg.WithStatus(OpMode.Mode3);
        r2.Status.Should().Be(OpMode.Mode3);
        ((byte)r2).Should().Be(0x18); // 3 << 3 = 0x18

        // Both together via chaining
        var r3 = reg.WithCommand(OpMode.Mode7).WithStatus(OpMode.Mode2).WithFlags(3);
        r3.Command.Should().Be(OpMode.Mode7);
        r3.Status.Should().Be(OpMode.Mode2);
        r3.Flags.Should().Be(3);
        // 7 | (2 << 3) | (3 << 6) = 0x07 | 0x10 | 0xC0 = 0xD7
        ((byte)r3).Should().Be(0xD7);

        // Setter round-trip at bit 0
        reg.Command = OpMode.Mode6;
        reg.Command.Should().Be(OpMode.Mode6);
    }

    /// <summary>
    /// Tests using With{Name} methods with properties (the main use case).
    /// </summary>
    [Fact]
    public void GeneratedBitFields_WithMethodsOnProperties()
    {
        // Simulate a class with a BitFields property
        var container = new TestContainer();
        container.Status = 0;

        // This is the pattern that works with properties
        container.Status = container.Status.WithReady(true);
        container.Status.Ready.Should().BeTrue();

        // Chain multiple changes
        container.Status = container.Status.WithError(true).WithMode(OpMode.Mode3);
        container.Status.Ready.Should().BeTrue();
        container.Status.Error.Should().BeTrue();
        container.Status.Mode.Should().Be(OpMode.Mode3);
    }

    /// <summary>
    /// Tests With{Name} methods on 16-bit registers.
    /// </summary>
    [Fact]
    public void GeneratedBitFields_WithMethods16Bit()
    {
        GeneratedKeyboardReg16 reg = 0;

        var result = reg
            .WithFirstKeyCode(0x1A)
            .WithFirstKeyUp(true)
            .WithSecondKeyCode(0x00);

        result.FirstKeyCode.Should().Be(0x1A);
        result.FirstKeyUp.Should().BeTrue();
        result.SecondKeyCode.Should().Be(0x00);

        // FirstKeyUp (bit 15) = 1, FirstKeyCode (bits 8-14) = 0x1A
        // SecondKeyCode (bits 0-6) = 0x00
        // Expected: 0x9A00
        ((ushort)result).Should().Be(0x9A00);
    }

    #endregion

    #region Byte Span Tests

    [Fact]
    public void Reg8_SizeInBytes_Is1()
    {
        GeneratedStatusReg8.SIZE_IN_BYTES.Should().Be(1);
    }

    [Fact]
    public void Reg8_SpanConstructor_RoundTrips()
    {
        GeneratedStatusReg8 original = 0xAB;
        var bytes = original.ToByteArray();
        bytes.Length.Should().Be(1);
        var restored = new GeneratedStatusReg8((ReadOnlySpan<byte>)bytes);
        ((byte)restored).Should().Be(0xAB);
    }

    [Fact]
    public void Reg16_SpanConstructor_RoundTrips()
    {
        GeneratedKeyboardReg16 original = 0x9A00;
        var bytes = original.ToByteArray();
        bytes.Length.Should().Be(2);
        var restored = new GeneratedKeyboardReg16((ReadOnlySpan<byte>)bytes);
        ((ushort)restored).Should().Be(0x9A00);
    }

    [Fact]
    public void Reg32_SpanConstructor_RoundTrips()
    {
        GeneratedControlReg32 original = 0xDEADBEEF;
        var bytes = original.ToByteArray();
        bytes.Length.Should().Be(4);
        var restored = new GeneratedControlReg32((ReadOnlySpan<byte>)bytes);
        ((uint)restored).Should().Be(0xDEADBEEF);
    }

    [Fact]
    public void Reg64_SpanConstructor_RoundTrips()
    {
        GeneratedWideReg64 original = 0xCAFEBABE_DEADBEEF;
        var bytes = original.ToByteArray();
        bytes.Length.Should().Be(8);
        var restored = new GeneratedWideReg64((ReadOnlySpan<byte>)bytes);
        ((ulong)restored).Should().Be(0xCAFEBABE_DEADBEEF);
    }

    [Fact]
    public void Reg32_WriteTo_LittleEndian()
    {
        GeneratedControlReg32 value = 0x01020304;
        Span<byte> buf = stackalloc byte[GeneratedControlReg32.SIZE_IN_BYTES];
        value.WriteTo(buf);
        buf[0].Should().Be(0x04);
        buf[1].Should().Be(0x03);
        buf[2].Should().Be(0x02);
        buf[3].Should().Be(0x01);
    }

    [Fact]
    public void Reg64_TryWriteTo_SucceedsWithExactSize()
    {
        GeneratedWideReg64 value = 42;
        Span<byte> buf = stackalloc byte[GeneratedWideReg64.SIZE_IN_BYTES];
        value.TryWriteTo(buf, out int written).Should().BeTrue();
        written.Should().Be(GeneratedWideReg64.SIZE_IN_BYTES);
    }

    [Fact]
    public void Reg64_TryWriteTo_FailsWithTooSmallSpan()
    {
        GeneratedWideReg64 value = 42;
        Span<byte> buf = stackalloc byte[GeneratedWideReg64.SIZE_IN_BYTES - 1];
        value.TryWriteTo(buf, out int written).Should().BeFalse();
        written.Should().Be(0);
    }

    [Fact]
    public void Reg8_SpanConstructor_ThrowsOnEmpty()
    {
        var act = () => new GeneratedStatusReg8(ReadOnlySpan<byte>.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Reg8_ReadFrom_MatchesConstructor()
    {
        GeneratedStatusReg8 original = 0x42;
        var bytes = original.ToByteArray();
        var fromReadFrom = GeneratedStatusReg8.ReadFrom(bytes);
        ((byte)fromReadFrom).Should().Be(0x42);
    }

    #endregion

    #region JSON Serialization Tests

    [Fact]
    public void Reg8_JsonRoundTrip()
    {
        GeneratedStatusReg8 original = 0xAB;
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<GeneratedStatusReg8>(json);
        ((byte)restored).Should().Be(0xAB);
    }

    [Fact]
    public void Reg32_JsonRoundTrip()
    {
        GeneratedControlReg32 original = 0xDEADBEEF;
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<GeneratedControlReg32>(json);
        ((uint)restored).Should().Be(0xDEADBEEF);
    }

    [Fact]
    public void Reg64_JsonRoundTrip()
    {
        GeneratedWideReg64 original = 0xCAFEBABE_DEADBEEF;
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<GeneratedWideReg64>(json);
        ((ulong)restored).Should().Be(0xCAFEBABE_DEADBEEF);
    }

    [Fact]
    public void Reg8_JsonSerializesAsString()
    {
        GeneratedStatusReg8 value = 0xFF;
        var json = JsonSerializer.Serialize(value);
        // Should be a JSON string, not a number
        json.Should().StartWith("\"");
    }

    [Fact]
    public void Reg32_JsonDefaultRoundTrip()
    {
        GeneratedControlReg32 original = default;
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<GeneratedControlReg32>(json);
        ((uint)restored).Should().Be(0);
    }

    [Fact]
    public void Reg8_JsonDeserializeInContainer()
    {
        var container = new TestContainer { Status = 0xAB };
        var json = JsonSerializer.Serialize(container);
        var restored = JsonSerializer.Deserialize<TestContainer>(json);
        ((byte)restored!.Status).Should().Be(0xAB);
    }

    [Fact]
    public void Reg8_JsonDeserializesFromHex()
    {
        var json = "\"0xFF\"";
        var restored = JsonSerializer.Deserialize<GeneratedStatusReg8>(json);
        ((byte)restored).Should().Be(0xFF);
    }

    [Fact]
    public void Reg8_JsonDeserializesFromBinary()
    {
        var json = "\"0b10101010\"";
        var restored = JsonSerializer.Deserialize<GeneratedStatusReg8>(json);
        ((byte)restored).Should().Be(0xAA);
    }

    [Fact]
    public void Reg8_JsonDeserializesFromDecimal()
    {
        var json = "\"42\"";
        var restored = JsonSerializer.Deserialize<GeneratedStatusReg8>(json);
        ((byte)restored).Should().Be(42);
    }

    [Fact]
    public void Reg8_JsonNullDeserializesToDefault()
    {
        var json = "null";
        var restored = JsonSerializer.Deserialize<GeneratedStatusReg8>(json);
        ((byte)restored).Should().Be(0);
    }

    [Fact]
    public void Reg16_JsonRoundTrip()
    {
        GeneratedKeyboardReg16 original = 0xABCD;
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<GeneratedKeyboardReg16>(json);
        ((ushort)restored).Should().Be(0xABCD);
    }

    #endregion

    #region StorageType Enum Constructor Tests

    /// <summary>
    /// Tests that [BitFields(StorageType.Byte)] produces the same behavior as [BitFields(typeof(byte))].
    /// </summary>
    [Fact]
    public void EnumReg8_MatchesTypeofBehavior()
    {
        // Set identical values on both structs
        GeneratedStatusReg8 typeofReg = 0;
        typeofReg.Ready = true;
        typeofReg.Error = true;
        typeofReg.Mode = OpMode.Mode5;
        typeofReg.Priority = 2;

        EnumReg8 enumReg = 0;
        enumReg.Ready = true;
        enumReg.Error = true;
        enumReg.Mode = OpMode.Mode5;
        enumReg.Priority = 2;

        // Both should produce the same raw value
        ((byte)typeofReg).Should().Be((byte)enumReg);
    }

    /// <summary>
    /// Tests flags on an enum-constructed 8-bit register.
    /// </summary>
    [Fact]
    public void EnumReg8_Flags_GetAndSet()
    {
        EnumReg8 reg = 0;

        reg.Ready = true;
        reg.Ready.Should().BeTrue();
        ((byte)reg).Should().Be(0x01);

        reg.Busy = true;
        reg.Busy.Should().BeTrue();
        ((byte)reg).Should().Be(0x81);
    }

    /// <summary>
    /// Tests implicit conversion with the enum-constructed struct.
    /// </summary>
    [Fact]
    public void EnumReg8_ImplicitConversion()
    {
        EnumReg8 reg = 0xFF;
        ((byte)reg).Should().Be(0xFF);
        reg.Ready.Should().BeTrue();

        byte raw = reg;
        raw.Should().Be(0xFF);
    }

    /// <summary>
    /// Tests that [BitFields(StorageType.UInt32)] works correctly.
    /// </summary>
    [Fact]
    public void EnumReg32_GetAndSet()
    {
        EnumReg32 reg = 0;

        reg.Address = 0x00ABCDEF;
        reg.Address.Should().Be(0x00ABCDEF);

        reg.Command = 0x0F;
        reg.Command.Should().Be(0x0F);

        reg.Enable = true;
        reg.Interrupt = true;

        ((uint)reg).Should().Be(0x3FABCDEF);
    }

    /// <summary>
    /// Tests that [BitFields(StorageType.UInt64)] works correctly.
    /// </summary>
    [Fact]
    public void EnumReg64_GetAndSet()
    {
        EnumReg64 reg = 0;

        reg.Status = 0xAB;
        reg.Status.Should().Be(0xAB);

        reg.Data = 0xCDEF;
        reg.Data.Should().Be(0xCDEF);

        reg.Valid = true;
        reg.Valid.Should().BeTrue();

        ulong expected = 0xAB | ((ulong)0xCDEF << 8) | (1UL << 56);
        ((ulong)reg).Should().Be(expected);
    }

    /// <summary>
    /// Tests SIZE_IN_BYTES on an enum-constructed struct.
    /// </summary>
    [Fact]
    public void EnumReg8_SizeInBytes()
    {
        EnumReg8.SIZE_IN_BYTES.Should().Be(1);
    }

    /// <summary>
    /// Tests byte span round-trip on an enum-constructed struct.
    /// </summary>
    [Fact]
    public void EnumReg32_SpanRoundTrip()
    {
        EnumReg32 original = 0xDEADBEEF;
        var bytes = original.ToByteArray();
        bytes.Length.Should().Be(4);
        var restored = new EnumReg32((ReadOnlySpan<byte>)bytes);
        ((uint)restored).Should().Be(0xDEADBEEF);
    }

    /// <summary>
    /// Tests parsing on an enum-constructed struct.
    /// </summary>
    [Fact]
    public void EnumReg8_Parse()
    {
        var result = EnumReg8.Parse("0xFF");
        ((byte)result).Should().Be(0xFF);
    }

    /// <summary>
    /// Tests JSON round-trip on an enum-constructed struct.
    /// </summary>
    [Fact]
    public void EnumReg8_JsonRoundTrip()
    {
        EnumReg8 original = 0xAB;
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<EnumReg8>(json);
        ((byte)restored).Should().Be(0xAB);
    }

    /// <summary>
    /// Tests JSON round-trip on a 32-bit enum-constructed struct.
    /// </summary>
    [Fact]
    public void EnumReg32_JsonRoundTrip()
    {
        EnumReg32 original = 0xDEADBEEF;
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<EnumReg32>(json);
        ((uint)restored).Should().Be(0xDEADBEEF);
    }

    /// <summary>
    /// Tests JSON round-trip on a 64-bit enum-constructed struct.
    /// </summary>
    [Fact]
    public void EnumReg64_JsonRoundTrip()
    {
        EnumReg64 original = 0x0100_0000_0000_00AB;
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<EnumReg64>(json);
        ((ulong)restored).Should().Be(0x0100_0000_0000_00AB);
    }

    /// <summary>
    /// Tests With{Name} fluent methods on an enum-constructed struct.
    /// </summary>
    [Fact]
    public void EnumReg8_WithMethods()
    {
        EnumReg8 reg = 0;
        var result = reg.WithReady(true).WithMode(OpMode.Mode5).WithPriority(2);

        result.Ready.Should().BeTrue();
        result.Mode.Should().Be(OpMode.Mode5);
        result.Priority.Should().Be(2);

        // Ready (bit 0) = 1, Mode (bits 2-4) = 5, Priority (bits 5-6) = 2
        // Expected: 0x01 | 0x14 | 0x40 = 0x55
        ((byte)result).Should().Be(0x55);
    }

    /// <summary>
    /// Tests static bit/mask properties on an enum-constructed struct.
    /// </summary>
    [Fact]
    public void EnumReg8_StaticBitAndMaskProperties()
    {
        ((byte)EnumReg8.ReadyBit).Should().Be(0x01);
        ((byte)EnumReg8.BusyBit).Should().Be(0x80);
        ((byte)EnumReg8.ModeMask).Should().Be(0x1C);
    }

    #endregion

    #region Reversed Start/End Bit Order Tests

    /// <summary>
    /// BitFieldAttribute(int start, int end) constructor silently swaps when end &lt; start.
    /// </summary>
    [Fact]
    public void BitFieldAttribute_Constructor_SwapsWhenEndLessThanStart()
    {
        var attr = new BitFieldAttribute(7, 3);
        attr.Start.Should().Be(3);
        attr.End.Should().Be(7);
    }

    /// <summary>
    /// BitFieldAttribute(int start, int end) constructor leaves values unchanged when start &lt;= end.
    /// </summary>
    [Fact]
    public void BitFieldAttribute_Constructor_NoSwapWhenStartLessThanEnd()
    {
        var attr = new BitFieldAttribute(3, 7);
        attr.Start.Should().Be(3);
        attr.End.Should().Be(7);
    }

    /// <summary>
    /// BitFieldAttribute(int start, int end) constructor leaves equal values unchanged.
    /// </summary>
    [Fact]
    public void BitFieldAttribute_Constructor_NoSwapWhenEqual()
    {
        var attr = new BitFieldAttribute(5, 5);
        attr.Start.Should().Be(5);
        attr.End.Should().Be(5);
    }

    /// <summary>
    /// A field declared with reversed positional syntax [BitField(7, 0)] covers all 8 bits.
    /// The generator silently swaps to Start=0, End=7.
    /// </summary>
    [Fact]
    public void ReversedPositionalReg_GetSet_WorksCorrectly()
    {
        ReversedPositionalReg reg = 0;
        reg.AllBits = 0xAB;
        reg.AllBits.Should().Be(0xAB);
        ((byte)reg).Should().Be(0xAB);

        reg.AllBits = 0x00;
        ((byte)reg).Should().Be(0x00);
    }

    /// <summary>
    /// Fields declared with reversed fully-named syntax [BitField(Start = high, End = low)]
    /// behave identically to the same fields declared in canonical order.
    /// </summary>
    [Fact]
    public void ReversedNamedSyntaxReg_GetSet_WorksCorrectly()
    {
        ReversedNamedSyntaxReg reg = 0;

        reg.UpperFive = 0b11111;   // 31 -- bits 3-7 all set
        ((byte)reg).Should().Be(0xF8);
        reg.UpperFive.Should().Be(31);

        reg = 0;
        reg.LowerThree = 0b111;   // 7 -- bits 0-2 all set
        ((byte)reg).Should().Be(0x07);
        reg.LowerThree.Should().Be(7);

        reg = 0;
        reg.UpperFive = 31;
        reg.LowerThree = 7;
        ((byte)reg).Should().Be(0xFF);
    }

    /// <summary>
    /// Fields declared with reversed mixed syntax [BitField(highBit, End = lowBit)]
    /// behave identically to the same fields declared in canonical order.
    /// </summary>
    [Fact]
    public void ReversedMixedSyntaxReg_GetSet_WorksCorrectly()
    {
        ReversedMixedSyntaxReg reg = 0;

        reg.UpperFive = 0b11111;  // 31 -- bits 3-7 all set
        ((byte)reg).Should().Be(0xF8);
        reg.UpperFive.Should().Be(31);

        reg = 0;
        reg.LowerThree = 0b111;  // 7 -- bits 0-2 all set
        ((byte)reg).Should().Be(0x07);
        reg.LowerThree.Should().Be(7);

        reg = 0;
        reg.UpperFive = 31;
        reg.LowerThree = 7;
        ((byte)reg).Should().Be(0xFF);
    }

    #endregion

    #region End + Width derives Start Tests

    /// <summary>
    /// [BitField(End = N, Width = W)] with no Start derives Start = End - Width + 1.
    /// Upper nibble: End=7, Width=4 -> Start=4, covering bits 4-7.
    /// Lower nibble: End=3, Width=4 -> Start=0, covering bits 0-3.
    /// </summary>
    [Fact]
    public void EndAndWidthOnlyReg_GetSet_WorksCorrectly()
    {
        EndAndWidthOnlyReg reg = 0;

        reg.UpperNibble = 0xA;   // bits 4-7 = 10
        ((byte)reg).Should().Be(0xA0);
        reg.UpperNibble.Should().Be(0xA);

        reg = 0;
        reg.LowerNibble = 0x5;   // bits 0-3 = 5
        ((byte)reg).Should().Be(0x05);
        reg.LowerNibble.Should().Be(0x5);

        reg = 0;
        reg.UpperNibble = 0xA;
        reg.LowerNibble = 0x5;
        ((byte)reg).Should().Be(0xA5);
    }

    /// <summary>
    /// [BitField(End = N, Width = W)] round-trips all 256 byte values correctly,
    /// verifying that Start derivation produces the same result as explicit [BitField(Start, End)].
    /// </summary>
    [Fact]
    public void EndAndWidthOnlyReg_AllValues_RoundTrip()
    {
        for (byte upper = 0; upper <= 0xF; upper++)
        {
            for (byte lower = 0; lower <= 0xF; lower++)
            {
                EndAndWidthOnlyReg reg = 0;
                reg.UpperNibble = upper;
                reg.LowerNibble = lower;
                byte expected = (byte)((upper << 4) | lower);
                ((byte)reg).Should().Be(expected, $"upper={upper}, lower={lower}");
                reg.UpperNibble.Should().Be(upper);
                reg.LowerNibble.Should().Be(lower);
            }
        }
    }

    #endregion

    #region Auto-Sized [BitFields] Tests

    /// <summary>
    /// Auto-sized struct resolves to byte (max bit = 7).
    /// Verifies identical behavior to the explicit [BitFields(typeof(byte))] GeneratedStatusReg8.
    /// </summary>
    [Fact]
    public void AutoSized8_GetAndSet()
    {
        AutoSizedReg8 reg = 0;

        reg.Ready.Should().BeFalse();
        reg.Error.Should().BeFalse();
        reg.Busy.Should().BeFalse();
        reg.Mode.Should().Be(OpMode.Mode0);
        reg.Priority.Should().Be(0);

        reg.Ready = true;
        reg.Error = true;
        reg.Busy = true;
        reg.Mode = OpMode.Mode5;
        reg.Priority = 3;

        reg.Ready.Should().BeTrue();
        reg.Error.Should().BeTrue();
        reg.Busy.Should().BeTrue();
        reg.Mode.Should().Be(OpMode.Mode5);
        reg.Priority.Should().Be(3);

        // FlagA(0)=1, FlagB(1)=1, Mode(2-4)=5, Priority(5-6)=3, Busy(7)=1
        // = 0x01 | 0x02 | (5<<2) | (3<<5) | 0x80 = 0x01|0x02|0x14|0x60|0x80 = 0xF7
        ((byte)reg).Should().Be(0xF7);
    }

    /// <summary>
    /// Auto-sized struct resolves to byte when SIZE_IN_BYTES = 1.
    /// </summary>
    [Fact]
    public void AutoSized8_SizeInBytes()
    {
        AutoSizedReg8.SIZE_IN_BYTES.Should().Be(1);
    }

    /// <summary>
    /// Auto-sized struct with max bit 15 resolves to ushort.
    /// </summary>
    [Fact]
    public void AutoSized16_GetAndSet()
    {
        AutoSizedReg16 reg = 0;

        reg.SecondKeyCode = 0x1A;
        reg.SecondKeyUp = true;
        reg.FirstKeyCode = 0x3F;
        reg.FirstKeyUp = true;

        reg.SecondKeyCode.Should().Be(0x1A);
        reg.SecondKeyUp.Should().BeTrue();
        reg.FirstKeyCode.Should().Be(0x3F);
        reg.FirstKeyUp.Should().BeTrue();

        AutoSizedReg16.SIZE_IN_BYTES.Should().Be(2);
    }

    /// <summary>
    /// Auto-sized struct with max bit 29 resolves to uint.
    /// </summary>
    [Fact]
    public void AutoSized32_GetAndSet()
    {
        AutoSizedReg32 reg = 0;

        reg.Address = 0xABCDEF;
        reg.Command = 0xF;
        reg.Enable = true;
        reg.Interrupt = true;

        reg.Address.Should().Be(0xABCDEF);
        reg.Command.Should().Be(0xF);
        reg.Enable.Should().BeTrue();
        reg.Interrupt.Should().BeTrue();

        AutoSizedReg32.SIZE_IN_BYTES.Should().Be(4);
    }

    /// <summary>
    /// Auto-sized struct with max bit 57 resolves to ulong.
    /// </summary>
    [Fact]
    public void AutoSized64_GetAndSet()
    {
        AutoSizedReg64 reg = 0;

        reg.Status = 0xAB;
        reg.Data = 0x1234;
        reg.Address = 0xDEADBEEF;
        reg.Valid = true;
        reg.Ready = true;

        reg.Status.Should().Be(0xAB);
        reg.Data.Should().Be(0x1234);
        reg.Address.Should().Be(0xDEADBEEF);
        reg.Valid.Should().BeTrue();
        reg.Ready.Should().BeTrue();

        AutoSizedReg64.SIZE_IN_BYTES.Should().Be(8);
    }

    /// <summary>
    /// Auto-sized matches explicit typeof(byte) behavior: same raw value for same field values.
    /// </summary>
    [Fact]
    public void AutoSized8_MatchesExplicit()
    {
        GeneratedStatusReg8 explicitReg = 0;
        AutoSizedReg8 autoReg = 0;

        explicitReg.Ready = true;
        explicitReg.Mode = OpMode.Mode3;
        explicitReg.Priority = 2;

        autoReg.Ready = true;
        autoReg.Mode = OpMode.Mode3;
        autoReg.Priority = 2;

        ((byte)explicitReg).Should().Be((byte)autoReg);
    }

    /// <summary>
    /// Auto-sized struct with 5 bits (bits 0-4) resolves to byte (5 bits fit in 8).
    /// Verifies right-sized behavior matches [BitFields(5)].
    /// </summary>
    [Fact]
    public void AutoSized5Bits_GetAndSet()
    {
        AutoSizedReg5 reg = 0;

        reg.Low = 0x7;  // 3 bits max
        reg.High = 0x3; // 2 bits max

        reg.Low.Should().Be(0x7);
        reg.High.Should().Be(0x3);

        // Low(0-2)=7, High(3-4)=3 → 0x07 | (3<<3) = 0x07 | 0x18 = 0x1F
        ((byte)reg).Should().Be(0x1F);
        AutoSizedReg5.SIZE_IN_BYTES.Should().Be(1);
    }

    /// <summary>
    /// Auto-sized struct with 10 bits (bits 0-9) resolves to ushort (10 bits > 8 so not byte).
    /// </summary>
    [Fact]
    public void AutoSized10Bits_UsesUshort()
    {
        AutoSizedReg10 reg = 0;

        reg.Lo = 0xFF;  // 8-bit field
        reg.Hi = 0x3;   // 2-bit field

        reg.Lo.Should().Be(0xFF);
        reg.Hi.Should().Be(0x3);

        AutoSizedReg10.SIZE_IN_BYTES.Should().Be(2);
    }

    /// <summary>
    /// Auto-sized struct can use implicit conversion round-trip.
    /// </summary>
    [Fact]
    public void AutoSized8_ImplicitConversion()
    {
        AutoSizedReg8 reg = 0xFF;
        ((byte)reg).Should().Be(0xFF);
        reg.Ready.Should().BeTrue();
        reg.Error.Should().BeTrue();
        reg.Busy.Should().BeTrue();

        byte value = reg;
        value.Should().Be(0xFF);
    }

    /// <summary>
    /// Auto-sized struct JSON round-trips correctly.
    /// </summary>
    [Fact]
    public void AutoSized8_JsonRoundTrip()
    {
        AutoSizedReg8 reg = 0;
        reg.Ready = true;
        reg.Mode = OpMode.Mode5;

        var json = System.Text.Json.JsonSerializer.Serialize(reg);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<AutoSizedReg8>(json);

        ((byte)deserialized).Should().Be((byte)reg);
    }

    /// <summary>
    /// Auto-sized struct supports With methods.
    /// </summary>
    [Fact]
    public void AutoSized8_WithMethods()
    {
        AutoSizedReg8 reg = 0;
        var result = reg.WithReady(true).WithMode(OpMode.Mode7).WithBusy(true);

        result.Ready.Should().BeTrue();
        result.Mode.Should().Be(OpMode.Mode7);
        result.Busy.Should().BeTrue();
    }

    /// <summary>
    /// Auto-sized struct with UndefinedBits = Zeroes enforces zeroed undefined bits.
    /// </summary>
    [Fact]
    public void AutoSizedWithUndefinedBitsZeroes_EnforcesZeroes()
    {
        // Bits 0-4 are defined, bits 5-7 are undefined.
        // Assigning 0xFF should clear the undefined bits.
        AutoSizedUndefinedZeroes reg = 0xFF;
        ((byte)reg).Should().Be(0x1F); // only bits 0-4 preserved
        reg.AllDefined.Should().Be(0x1F);
    }

    /// <summary>
    /// Auto-sized struct with flags-only (1-bit fields) resolves to byte.
    /// </summary>
    [Fact]
    public void AutoSizedFlagsOnly_GetAndSet()
    {
        AutoSizedFlagsOnly reg = 0;

        reg.A.Should().BeFalse();
        reg.B.Should().BeFalse();
        reg.C.Should().BeFalse();

        reg.A = true;
        reg.B = true;
        reg.C = true;

        reg.A.Should().BeTrue();
        reg.B.Should().BeTrue();
        reg.C.Should().BeTrue();

        // A(0)=1, B(3)=1, C(7)=1 → 0x01 | 0x08 | 0x80 = 0x89
        ((byte)reg).Should().Be(0x89);
        AutoSizedFlagsOnly.SIZE_IN_BYTES.Should().Be(1);
    }

    /// <summary>
    /// Auto-sized struct with 33 bits resolves to ulong (>32 bits).
    /// </summary>
    [Fact]
    public void AutoSized33Bits_UsesUlong()
    {
        AutoSizedReg33 reg = 0;

        reg.Lo = 0xFFFFFFFF; // 32-bit field
        reg.Hi = true;       // bit 32

        reg.Lo.Should().Be(0xFFFFFFFF);
        reg.Hi.Should().BeTrue();

        AutoSizedReg33.SIZE_IN_BYTES.Should().Be(8);
    }

    /// <summary>
    /// Auto-sized struct supports parse/format round-trip.
    /// </summary>
    [Fact]
    public void AutoSized8_ParseRoundTrip()
    {
        AutoSizedReg8 reg = 0;
        reg.Ready = true;
        reg.Busy = true;
        reg.Mode = OpMode.Mode3;

        string s = reg.ToString();
        AutoSizedReg8 parsed = AutoSizedReg8.Parse(s, null);

        ((byte)parsed).Should().Be((byte)reg);
    }

    #endregion

    #region Default Static Property Tests

    /// <summary>
    /// Default on an unconstrained type returns all-zero value.
    /// </summary>
    [Fact]
    public void Default_Unconstrained_ReturnsZero()
    {
        var reg = GeneratedStatusReg8.Default;
        ((byte)reg).Should().Be(0x00);
        reg.Ready.Should().BeFalse();
        reg.Error.Should().BeFalse();
        reg.Mode.Should().Be(0);
    }

    /// <summary>
    /// Default on a constrained type applies MustBe normalization.
    /// MustBeTestReg: bit 7 = MustBe.One, bits 1-2 = MustBe.Zero.
    /// Default should read Sync = true (bit 7 forced on).
    /// </summary>
    [Fact]
    public void Default_Constrained_MustBe_AppliesNormalization()
    {
        var reg = MustBeTestReg.Default;
        reg.Sync.Should().BeTrue("bit 7 is MustBe.One so Default must read as true");
        reg.Reserved.Should().Be(0, "bits 1-2 are MustBe.Zero so Default must read as 0");
        reg.Active.Should().BeFalse();
        reg.Data.Should().Be(0);
    }

    /// <summary>
    /// Default on a 16-bit unconstrained type returns all-zero.
    /// </summary>
    [Fact]
    public void Default_16Bit_ReturnsZero()
    {
        var reg = GeneratedKeyboardReg16.Default;
        ((ushort)reg).Should().Be(0x0000);
    }

    /// <summary>
    /// Default on a 32-bit unconstrained type returns all-zero.
    /// </summary>
    [Fact]
    public void Default_32Bit_ReturnsZero()
    {
        var reg = GeneratedControlReg32.Default;
        ((uint)reg).Should().Be(0x00000000);
    }

    /// <summary>
    /// Default on a 64-bit unconstrained type returns all-zero.
    /// </summary>
    [Fact]
    public void Default_64Bit_ReturnsZero()
    {
        var reg = GeneratedWideReg64.Default;
        ((ulong)reg).Should().Be(0UL);
    }

    /// <summary>
    /// Default on a CombinedMustBeReg (UndefinedBitsMustBe.Zeroes + MustBe.One on bit 3)
    /// reads AlwaysHigh = true and all undefined bits zeroed.
    /// </summary>
    [Fact]
    public void Default_CombinedConstraints_AppliesNormalization()
    {
        var reg = CombinedMustBeReg.Default;
        reg.AlwaysHigh.Should().BeTrue("bit 3 is MustBe.One");
        reg.Flags.Should().Be(0);
    }

    /// <summary>
    /// Default used as fluent builder base produces correct results.
    /// </summary>
    [Fact]
    public void Default_FluentBuilder_ProducesCorrectResult()
    {
        var reg = GeneratedStatusReg8.Default.WithReady(true).WithMode(OpMode.Mode1);
        reg.Ready.Should().BeTrue();
        reg.Mode.Should().Be(OpMode.Mode1);
        reg.Error.Should().BeFalse();
    }

    #endregion
}

/// <summary>
/// Helper class to test BitFields properties.
/// </summary>
public class TestContainer
{
    public GeneratedStatusReg8 Status { get; set; }
}

#region Generated BitFields Test Structs

public enum OpMode : byte
{
    Mode0 = 0,
    Mode1 = 1,
    Mode2 = 2,
    Mode3 = 3,
    Mode4 = 4,
    Mode5 = 5,
    Mode6 = 6,
    Mode7 = 7
}
/// <summary>
/// 8-bit status register for testing code generation.
/// </summary>
[BitFields(typeof(byte))]
public partial struct GeneratedStatusReg8
{
    [BitFlag(0)] public partial bool Ready { get; set; }
    [BitFlag(1)] public partial bool Error { get; set; }
    [BitFlag(7)] public partial bool Busy { get; set; }
    [BitField(2, End = 4)] public partial OpMode Mode { get; set; }    // bits 2..=4 (3 bits)
    [BitField(5, End = 6)] public partial byte Priority { get; set; }  // bits 5..=6 (2 bits)
}

/// <summary>
/// 8-bit register with an enum field starting at bit 0 (shift == 0).
/// Regression test: the generated With method must cast the enum value to the
/// storage type before applying the mask, otherwise C# rejects enum &amp; int.
/// </summary>
[BitFields(typeof(byte))]
public partial struct EnumAtBitZeroReg
{
    [BitField(0, End = 2)] public partial OpMode Command { get; set; }   // bits 0..=2 (3 bits, shift 0)
    [BitField(3, End = 5)] public partial OpMode Status { get; set; }    // bits 3..=5 (3 bits, shift 3)
    [BitField(6, End = 7)] public partial byte Flags { get; set; }       // bits 6..=7 (2 bits)
}

/// <summary>
/// 16-bit keyboard register for testing code generation.
/// </summary>
[BitFields(typeof(ushort))]
public partial struct GeneratedKeyboardReg16
{
    [BitField(0, End = 6)] public partial byte SecondKeyCode { get; set; }  // bits 0..=6 (7 bits)
    [BitFlag(7)] public partial bool SecondKeyUp { get; set; }
    [BitField(8, End = 14)] public partial byte FirstKeyCode { get; set; }  // bits 8..=14 (7 bits)
    [BitFlag(15)] public partial bool FirstKeyUp { get; set; }
}

/// <summary>
/// 32-bit control register for testing code generation.
/// </summary>
[BitFields(typeof(uint))]
public partial struct GeneratedControlReg32
{
    [BitField(0, End = 23)] public partial uint Address { get; set; }   // bits 0..=23 (24 bits)
    [BitField(24, End = 27)] public partial byte Command { get; set; }  // bits 24..=27 (4 bits)
    [BitFlag(28)] public partial bool Enable { get; set; }
    [BitFlag(29)] public partial bool Interrupt { get; set; }
}

/// <summary>
/// 64-bit wide register for testing code generation.
/// </summary>
[BitFields(typeof(ulong))]
public partial struct GeneratedWideReg64
{
    [BitField(0, End = 7)] public partial byte Status { get; set; }     // bits 0..=7 (8 bits)
    [BitField(8, End = 23)] public partial ushort Data { get; set; }    // bits 8..=23 (16 bits)
    [BitField(24, End = 55)] public partial uint Address { get; set; }  // bits 24..=55 (32 bits)
    [BitFlag(56)] public partial bool Valid { get; set; }
    [BitFlag(57)] public partial bool Ready { get; set; }
}

/// <summary>
/// 64-bit wide register for testing code generation.
/// Intentionally uses bits above 31 to test 64-bit nint behavior.
/// </summary>
[BitFields(typeof(nint))]
public partial struct GeneratedWideRegNint
{
#pragma warning disable SD0002 // Intentional: testing 64-bit nint with high-bit fields
    [BitField(0, End = 7)] public partial byte Status { get; set; }     // bits 0..=7 (8 bits)
    [BitField(8, End = 23)] public partial ushort Data { get; set; }    // bits 8..=23 (16 bits)
    [BitField(24, End = 55)] public partial uint Address { get; set; }  // bits 24..=55 (32 bits)
    [BitFlag(56)] public partial bool Valid { get; set; }
    [BitFlag(57)] public partial bool Ready { get; set; }
#pragma warning restore SD0002
}

/// <summary>
/// 64-bit wide register for testing code generation.
/// Intentionally uses bits above 31 to test 64-bit nuint behavior.
/// </summary>
[BitFields(typeof(nuint))]
public partial struct GeneratedWideRegNuint
{
#pragma warning disable SD0002 // Intentional: testing 64-bit nuint with high-bit fields
    [BitField(0, End = 7)] public partial byte Status { get; set; }     // bits 0..=7 (8 bits)
    [BitField(8, End = 23)] public partial ushort Data { get; set; }    // bits 8..=23 (16 bits)
    [BitField(24, End = 55)] public partial uint Address { get; set; }  // bits 24..=55 (32 bits)
    [BitFlag(56)] public partial bool Valid { get; set; }
    [BitFlag(57)] public partial bool Ready { get; set; }
#pragma warning restore SD0002
}

/// <summary>
/// 8-bit register using StorageType enum constructor.
/// Verifies the enum-based constructor produces identical generated code to typeof(byte).
/// </summary>
[BitFields(StorageType.Byte)]
public partial struct EnumReg8
{
    [BitFlag(0)] public partial bool Ready { get; set; }
    [BitFlag(1)] public partial bool Error { get; set; }
    [BitFlag(7)] public partial bool Busy { get; set; }
    [BitField(2, End = 4)] public partial OpMode Mode { get; set; }    // bits 2..=4 (3 bits)
    [BitField(5, End = 6)] public partial byte Priority { get; set; }  // bits 5..=6 (2 bits)
}

/// <summary>
/// 32-bit register using StorageType enum constructor.
/// Verifies the enum-based constructor works with larger types.
/// </summary>
[BitFields(StorageType.UInt32)]
public partial struct EnumReg32
{
    [BitField(0, End = 23)] public partial uint Address { get; set; }   // bits 0..=23 (24 bits)
    [BitField(24, End = 27)] public partial byte Command { get; set; }  // bits 24..=27 (4 bits)
    [BitFlag(28)] public partial bool Enable { get; set; }
    [BitFlag(29)] public partial bool Interrupt { get; set; }
}

/// <summary>
/// 64-bit register using StorageType enum constructor.
/// </summary>
[BitFields(StorageType.UInt64)]
public partial struct EnumReg64
{
    [BitField(0, End = 7)] public partial byte Status { get; set; }
    [BitField(8, End = 23)] public partial ushort Data { get; set; }
    [BitFlag(56)] public partial bool Valid { get; set; }
}

/// <summary>
/// Byte register with a single field declared using reversed positional syntax [BitField(7, 0)].
/// After silent swap this covers all 8 bits.
/// </summary>
[BitFields(typeof(byte))]
public partial struct ReversedPositionalReg
{
#pragma warning disable SD0015
    [BitField(7, 0)] public partial byte AllBits { get; set; }
#pragma warning restore SD0015
}

/// <summary>
/// Byte register with fields declared using reversed fully-named syntax [BitField(Start = end, End = start)].
/// After silent swap: UpperFive covers bits 3-7 (5 bits), LowerThree covers bits 0-2 (3 bits).
/// </summary>
[BitFields(typeof(byte))]
public partial struct ReversedNamedSyntaxReg
{
    [BitField(Start = 7, End = 3)] public partial byte UpperFive { get; set; }
    [BitField(Start = 2, End = 0)] public partial byte LowerThree { get; set; }
}

/// <summary>
/// Byte register with fields declared using reversed mixed syntax [BitField(endValue, End = startValue)].
/// After silent swap: UpperFive covers bits 3-7 (5 bits), LowerThree covers bits 0-2 (3 bits).
/// </summary>
[BitFields(typeof(byte))]
public partial struct ReversedMixedSyntaxReg
{
    [BitField(7, End = 3)] public partial byte UpperFive { get; set; }
    [BitField(2, End = 0)] public partial byte LowerThree { get; set; }
}

/// <summary>
/// Byte register with fields using End+Width-only syntax (no Start).
/// [BitField(End = 7, Width = 4)] -> Start derived as 7 - 4 + 1 = 4, covering bits 4-7.
/// [BitField(End = 3, Width = 4)] -> Start derived as 3 - 4 + 1 = 0, covering bits 0-3.
/// </summary>
[BitFields(typeof(byte))]
public partial struct EndAndWidthOnlyReg
{
    [BitField(End = 7, Width = 4)] public partial byte UpperNibble { get; set; }
    [BitField(End = 3, Width = 4)] public partial byte LowerNibble { get; set; }
}

// ── Auto-sized [BitFields] test structs ─────────────────────────────

/// <summary>
/// Auto-sized: max bit = 7 (Busy flag) → byte backing.
/// Same layout as GeneratedStatusReg8 to verify identical behavior.
/// </summary>
[BitFields]
public partial struct AutoSizedReg8
{
    [BitFlag(0)] public partial bool Ready { get; set; }
    [BitFlag(1)] public partial bool Error { get; set; }
    [BitFlag(7)] public partial bool Busy { get; set; }
    [BitField(2, End = 4)] public partial OpMode Mode { get; set; }    // bits 2..=4 (3 bits)
    [BitField(5, End = 6)] public partial byte Priority { get; set; }  // bits 5..=6 (2 bits)
}

/// <summary>
/// Auto-sized: max bit = 15 (FirstKeyUp flag) → ushort backing.
/// </summary>
[BitFields]
public partial struct AutoSizedReg16
{
    [BitField(0, End = 6)] public partial byte SecondKeyCode { get; set; }  // bits 0..=6 (7 bits)
    [BitFlag(7)] public partial bool SecondKeyUp { get; set; }
    [BitField(8, End = 14)] public partial byte FirstKeyCode { get; set; }  // bits 8..=14 (7 bits)
    [BitFlag(15)] public partial bool FirstKeyUp { get; set; }
}

/// <summary>
/// Auto-sized: max bit = 29 (Interrupt flag) → uint backing.
/// </summary>
[BitFields]
public partial struct AutoSizedReg32
{
    [BitField(0, End = 23)] public partial uint Address { get; set; }   // bits 0..=23 (24 bits)
    [BitField(24, End = 27)] public partial byte Command { get; set; }  // bits 24..=27 (4 bits)
    [BitFlag(28)] public partial bool Enable { get; set; }
    [BitFlag(29)] public partial bool Interrupt { get; set; }
}

/// <summary>
/// Auto-sized: max bit = 57 (Ready flag) → ulong backing.
/// </summary>
[BitFields]
public partial struct AutoSizedReg64
{
    [BitField(0, End = 7)] public partial byte Status { get; set; }     // bits 0..=7 (8 bits)
    [BitField(8, End = 23)] public partial ushort Data { get; set; }    // bits 8..=23 (16 bits)
    [BitField(24, End = 55)] public partial uint Address { get; set; }  // bits 24..=55 (32 bits)
    [BitFlag(56)] public partial bool Valid { get; set; }
    [BitFlag(57)] public partial bool Ready { get; set; }
}

/// <summary>
/// Auto-sized: max bit = 4 → byte backing (5 bits needed, fits in 8).
/// </summary>
[BitFields]
public partial struct AutoSizedReg5
{
    [BitField(0, End = 2)] public partial byte Low { get; set; }   // bits 0..=2 (3 bits)
    [BitField(3, End = 4)] public partial byte High { get; set; }  // bits 3..=4 (2 bits)
}

/// <summary>
/// Auto-sized: max bit = 9 → ushort backing (10 bits needed, > 8).
/// </summary>
[BitFields]
public partial struct AutoSizedReg10
{
    [BitField(0, End = 7)] public partial byte Lo { get; set; }   // bits 0..=7 (8 bits)
    [BitField(8, End = 9)] public partial byte Hi { get; set; }   // bits 8..=9 (2 bits)
}

/// <summary>
/// Auto-sized with UndefinedBits set via named property.
/// Max bit = 4, undefined bits 5-7 must be zeroes.
/// </summary>
[BitFields(UndefinedBits = UndefinedBitsMustBe.Zeroes)]
public partial struct AutoSizedUndefinedZeroes
{
    [BitField(0, End = 2)] public partial byte Low { get; set; }
    [BitField(3, End = 4)] public partial byte High { get; set; }
    [BitField(0, End = 4)] public partial byte AllDefined { get; set; }
}

/// <summary>
/// Auto-sized with flags only (no multi-bit fields). Max bit = 7 → byte.
/// </summary>
[BitFields]
public partial struct AutoSizedFlagsOnly
{
    [BitFlag(0)] public partial bool A { get; set; }
    [BitFlag(3)] public partial bool B { get; set; }
    [BitFlag(7)] public partial bool C { get; set; }
}

/// <summary>
/// Auto-sized: max bit = 32 → ulong backing (33 bits needed, > 32).
/// </summary>
[BitFields]
public partial struct AutoSizedReg33
{
    [BitField(0, End = 31)] public partial uint Lo { get; set; }  // bits 0..=31 (32 bits)
    [BitFlag(32)] public partial bool Hi { get; set; }            // bit 32
}

#endregion
