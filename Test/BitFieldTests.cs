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
    [BitField(2, 4)] public partial OpMode Mode { get; set; }    // bits 2..=4 (3 bits)
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
