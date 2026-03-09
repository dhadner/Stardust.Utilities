using FluentAssertions;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Unit tests for BitFields operator support.
/// Tests arithmetic, shift, comparison, formatting, and interface implementations.
/// </summary>
public class BitFieldOperatorTests
{
    #region Arithmetic Operator Tests

    /// <summary>
    /// Tests unary plus operator.
    /// </summary>
    [Fact]
    public void UnaryPlus_ReturnsOriginalValue()
    {
        GeneratedStatusReg8 a = 0x42;
        var result = +a;
        ((byte)result).Should().Be(0x42);
    }

    /// <summary>
    /// Tests unary negation operator for unsigned types.
    /// </summary>
    [Fact]
    public void UnaryNegation_UnsignedType_WrapsAround()
    {
        GeneratedStatusReg8 a = 1;
        var result = -a;
        // -1 in byte should wrap to 0xFF
        ((byte)result).Should().Be(0xFF);

        GeneratedStatusReg8 b = 0;
        var result2 = -b;
        ((byte)result2).Should().Be(0);
    }

    /// <summary>
    /// Tests addition operator between two BitFields.
    /// </summary>
    [Fact]
    public void Addition_TwoBitFields()
    {
        GeneratedStatusReg8 a = 10;
        GeneratedStatusReg8 b = 20;
        var result = a + b;
        ((byte)result).Should().Be(30);
    }

    /// <summary>
    /// Tests addition operator with storage type on right.
    /// </summary>
    [Fact]
    public void Addition_BitFieldPlusStorageType()
    {
        GeneratedStatusReg8 a = 10;
        var result = a + (byte)5;
        ((byte)result).Should().Be(15);
    }

    /// <summary>
    /// Tests addition operator with storage type on left.
    /// </summary>
    [Fact]
    public void Addition_StorageTypePlusBitField()
    {
        GeneratedStatusReg8 b = 20;
        var result = (byte)5 + b;
        ((byte)result).Should().Be(25);
    }

    /// <summary>
    /// Tests addition overflow wraps around for byte.
    /// </summary>
    [Fact]
    public void Addition_Overflow_WrapsAround()
    {
        GeneratedStatusReg8 a = 0xFF;
        GeneratedStatusReg8 b = 1;
        var result = a + b;
        ((byte)result).Should().Be(0); // 255 + 1 wraps to 0
    }

    /// <summary>
    /// Tests subtraction operator between two BitFields.
    /// </summary>
    [Fact]
    public void Subtraction_TwoBitFields()
    {
        GeneratedStatusReg8 a = 30;
        GeneratedStatusReg8 b = 10;
        var result = a - b;
        ((byte)result).Should().Be(20);
    }

    /// <summary>
    /// Tests subtraction with storage type.
    /// </summary>
    [Fact]
    public void Subtraction_WithStorageType()
    {
        GeneratedStatusReg8 a = 50;
        var result = a - (byte)25;
        ((byte)result).Should().Be(25);

        result = (byte)100 - a;
        ((byte)result).Should().Be(50);
    }

    /// <summary>
    /// Tests subtraction underflow wraps around.
    /// </summary>
    [Fact]
    public void Subtraction_Underflow_WrapsAround()
    {
        GeneratedStatusReg8 a = 0;
        GeneratedStatusReg8 b = 1;
        var result = a - b;
        ((byte)result).Should().Be(0xFF); // 0 - 1 wraps to 255
    }

    /// <summary>
    /// Tests multiplication operator.
    /// </summary>
    [Fact]
    public void Multiplication_TwoBitFields()
    {
        GeneratedStatusReg8 a = 5;
        GeneratedStatusReg8 b = 6;
        var result = a * b;
        ((byte)result).Should().Be(30);
    }

    /// <summary>
    /// Tests multiplication with storage type.
    /// </summary>
    [Fact]
    public void Multiplication_WithStorageType()
    {
        GeneratedStatusReg8 a = 7;
        var result = a * (byte)8;
        ((byte)result).Should().Be(56);

        result = (byte)3 * a;
        ((byte)result).Should().Be(21);
    }

    /// <summary>
    /// Tests division operator.
    /// </summary>
    [Fact]
    public void Division_TwoBitFields()
    {
        GeneratedStatusReg8 a = 100;
        GeneratedStatusReg8 b = 5;
        var result = a / b;
        ((byte)result).Should().Be(20);
    }

    /// <summary>
    /// Tests division with storage type.
    /// </summary>
    [Fact]
    public void Division_WithStorageType()
    {
        GeneratedStatusReg8 a = 50;
        var result = a / (byte)5;
        ((byte)result).Should().Be(10);

        result = (byte)100 / a;
        ((byte)result).Should().Be(2);
    }

    /// <summary>
    /// Tests division by zero throws.
    /// </summary>
    [Fact]
    public void Division_ByZero_Throws()
    {
        GeneratedStatusReg8 a = 10;
        GeneratedStatusReg8 zero = 0;

        FluentActions.Invoking(() => { var _ = a / zero; })
            .Should().Throw<DivideByZeroException>();
    }

    /// <summary>
    /// Tests modulus operator.
    /// </summary>
    [Fact]
    public void Modulus_TwoBitFields()
    {
        GeneratedStatusReg8 a = 17;
        GeneratedStatusReg8 b = 5;
        var result = a % b;
        ((byte)result).Should().Be(2);
    }

    /// <summary>
    /// Tests modulus with storage type.
    /// </summary>
    [Fact]
    public void Modulus_WithStorageType()
    {
        GeneratedStatusReg8 a = 23;
        var result = a % (byte)7;
        ((byte)result).Should().Be(2);

        result = (byte)100 % a;
        ((byte)result).Should().Be(8); // 100 % 23 = 8
    }

    #endregion

    #region Shift Operator Tests

    /// <summary>
    /// Tests left shift operator.
    /// </summary>
    [Fact]
    public void LeftShift_ShiftsBitsLeft()
    {
        GeneratedStatusReg8 a = 0x01;
        var result = a << 4;
        ((byte)result).Should().Be(0x10);
    }

    /// <summary>
    /// Tests left shift with overflow truncation.
    /// </summary>
    [Fact]
    public void LeftShift_Overflow_Truncates()
    {
        GeneratedStatusReg8 a = 0x80;
        var result = a << 1;
        ((byte)result).Should().Be(0x00); // Bit shifted out
    }

    /// <summary>
    /// Tests right shift operator.
    /// </summary>
    [Fact]
    public void RightShift_ShiftsBitsRight()
    {
        GeneratedStatusReg8 a = 0x80;
        var result = a >> 4;
        ((byte)result).Should().Be(0x08);
    }

    /// <summary>
    /// Tests right shift with zero fill.
    /// </summary>
    [Fact]
    public void RightShift_ZeroFill()
    {
        GeneratedStatusReg8 a = 0xFF;
        var result = a >> 4;
        ((byte)result).Should().Be(0x0F);
    }

    /// <summary>
    /// Tests unsigned right shift operator (>>>).
    /// </summary>
    [Fact]
    public void UnsignedRightShift_AlwaysZeroFills()
    {
        GeneratedStatusReg8 a = 0xFF;
        var result = a >>> 4;
        ((byte)result).Should().Be(0x0F);
    }

    /// <summary>
    /// Tests shift operators on 16-bit type.
    /// </summary>
    [Fact]
    public void Shift_16Bit()
    {
        GeneratedKeyboardReg16 a = 0x00FF;
        var left = a << 8;
        ((ushort)left).Should().Be(0xFF00);

        var right = left >> 8;
        ((ushort)right).Should().Be(0x00FF);
    }

    /// <summary>
    /// Tests that shift-then-mask pattern works intuitively with integer literals.
    /// For small types (byte, ushort, etc.), shift returns int to allow:
    /// <c>(bits >> n) &amp; 1</c> without ambiguity.
    /// </summary>
    [Fact]
    public void ShiftThenMask_WorksWithIntegerLiterals()
    {
        // Test with byte-backed BitFields
        GeneratedStatusReg8 bits8 = 0b0000_1110; // bits 1,2,3 set
        int lsb8 = (bits8 >> 1) & 1;
        lsb8.Should().Be(1, "bit 1 is set, so (bits >> 1) & 1 should be 1");
        
        int bit2 = (bits8 >> 2) & 1;
        bit2.Should().Be(1, "bit 2 is set");
        
        int bit0 = (bits8 >> 0) & 1;
        bit0.Should().Be(0, "bit 0 is not set");

        // Test with ushort-backed BitFields
        GeneratedKeyboardReg16 bits16 = 0b0000_0000_0111_0000; // bits 4,5,6 set
        int bit4 = (bits16 >> 4) & 1;
        bit4.Should().Be(1, "bit 4 is set");
        
        int bit5 = (bits16 >> 5) & 1;
        bit5.Should().Be(1, "bit 5 is set");
        
        int bit7 = (bits16 >> 7) & 1;
        bit7.Should().Be(0, "bit 7 is not set");

        // Test that shift result can be directly assigned back to BitFields type
        // This works because we have implicit conversion from int to small BitFields types
        GeneratedKeyboardReg16 c = 0b0000_0000_0111_0000;
        GeneratedKeyboardReg16 d = (c >> 4) & 1;  // Implicit int -> GeneratedKeyboardReg16
        d.Should().Be((GeneratedKeyboardReg16)1);

        GeneratedStatusReg8 e = (bits8 >> 1) & 0xFF;  // Implicit int -> GeneratedStatusReg8
        ((byte)e).Should().Be(0x07);
    }

    /// <summary>
    /// Tests that shift result can be used with various bitwise operators.
    /// </summary>
    [Fact]
    public void ShiftResult_CanUseWithBitwiseOperators()
    {
        GeneratedStatusReg8 bits = 0b1010_1010;
        
        // Shift then AND with literal
        int andResult = (bits >> 2) & 0x0F;
        andResult.Should().Be(0x0A); // 0b1010_1010 >> 2 = 0b0010_1010 = 42, & 0x0F = 10
        
        // Shift then OR with literal
        int orResult = (bits >> 4) | 0xF0;
        orResult.Should().Be(0xFA); // 0b1010_1010 >> 4 = 0b0000_1010 = 10, | 0xF0 = 250
        
        // Shift then XOR with literal
        int xorResult = (bits >> 4) ^ 0x0F;
        xorResult.Should().Be(0x05); // 0b0000_1010 ^ 0b0000_1111 = 0b0000_0101 = 5
    }

    #endregion

    #region Comparison Operator Tests

    /// <summary>
    /// Tests less than operator.
    /// </summary>
    [Fact]
    public void LessThan_ComparesTwoValues()
    {
        GeneratedStatusReg8 a = 10;
        GeneratedStatusReg8 b = 20;
        GeneratedStatusReg8 a2 = 10; // Same value as 'a' for reflexivity test

        (a < b).Should().BeTrue();
        (b < a).Should().BeFalse();
        (a < a2).Should().BeFalse(); // Reflexivity: equal values are not less than each other
    }

    /// <summary>
    /// Tests greater than operator.
    /// </summary>
    [Fact]
    public void GreaterThan_ComparesTwoValues()
    {
        GeneratedStatusReg8 a = 10;
        GeneratedStatusReg8 b = 20;
        GeneratedStatusReg8 a2 = 10; // Same value as 'a' for reflexivity test

        (a > b).Should().BeFalse();
        (b > a).Should().BeTrue();
        (a > a2).Should().BeFalse(); // Reflexivity: equal values are not greater than each other
    }

    /// <summary>
    /// Tests less than or equal operator.
    /// </summary>
    [Fact]
    public void LessThanOrEqual_ComparesTwoValues()
    {
        GeneratedStatusReg8 a = 10;
        GeneratedStatusReg8 b = 20;
        GeneratedStatusReg8 c = 10;

        (a <= b).Should().BeTrue();
        (b <= a).Should().BeFalse();
        (a <= c).Should().BeTrue();
    }

    /// <summary>
    /// Tests greater than or equal operator.
    /// </summary>
    [Fact]
    public void GreaterThanOrEqual_ComparesTwoValues()
    {
        GeneratedStatusReg8 a = 10;
        GeneratedStatusReg8 b = 20;
        GeneratedStatusReg8 c = 10;

        (a >= b).Should().BeFalse();
        (b >= a).Should().BeTrue();
        (a >= c).Should().BeTrue();
    }

    /// <summary>
    /// Tests comparison with boundary values.
    /// </summary>
    [Fact]
    public void Comparison_BoundaryValues()
    {
        GeneratedStatusReg8 zero = 0;
        GeneratedStatusReg8 zero2 = 0; // Same value for reflexivity test
        GeneratedStatusReg8 max = 0xFF;
        GeneratedStatusReg8 max2 = 0xFF; // Same value for reflexivity test

        (zero < max).Should().BeTrue();
        (max > zero).Should().BeTrue();
        (zero <= zero2).Should().BeTrue(); // Reflexivity at boundary
        (max >= max2).Should().BeTrue();   // Reflexivity at boundary
    }

    #endregion

    #region IComparable Tests

    /// <summary>
    /// Tests IComparable.CompareTo with same type.
    /// </summary>
    [Fact]
    public void CompareTo_SameType_ReturnsCorrectOrder()
    {
        GeneratedStatusReg8 a = 10;
        GeneratedStatusReg8 b = 20;
        GeneratedStatusReg8 c = 10;

        a.CompareTo(b).Should().BeNegative();
        b.CompareTo(a).Should().BePositive();
        a.CompareTo(c).Should().Be(0);
    }

    /// <summary>
    /// Tests IComparable.CompareTo with null.
    /// </summary>
    [Fact]
    public void CompareTo_Null_ReturnsPositive()
    {
        GeneratedStatusReg8 a = 10;
        a.CompareTo(null).Should().BePositive();
    }

    /// <summary>
    /// Tests IComparable.CompareTo with wrong type throws.
    /// </summary>
    [Fact]
    public void CompareTo_WrongType_ThrowsArgumentException()
    {
        GeneratedStatusReg8 a = 10;
        FluentActions.Invoking(() => a.CompareTo("not a BitField"))
            .Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests IComparable.CompareTo with boxed same type.
    /// </summary>
    [Fact]
    public void CompareTo_BoxedSameType_Works()
    {
        GeneratedStatusReg8 a = 10;
        GeneratedStatusReg8 b = 20;
        object boxedB = b;

        a.CompareTo(boxedB).Should().BeNegative();
    }

    #endregion

    #region IEquatable Tests

    /// <summary>
    /// Tests IEquatable&lt;T&gt;.Equals with same type.
    /// </summary>
    [Fact]
    public void EquatableEquals_SameType()
    {
        GeneratedStatusReg8 a = 0x42;
        GeneratedStatusReg8 b = 0x42;
        GeneratedStatusReg8 c = 0x24;

        a.Equals(b).Should().BeTrue();
        a.Equals(c).Should().BeFalse();
    }

    /// <summary>
    /// Tests that equal values have equal hash codes.
    /// </summary>
    [Fact]
    public void EqualValues_HaveEqualHashCodes()
    {
        GeneratedStatusReg8 a = 0x42;
        GeneratedStatusReg8 b = 0x42;

        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    #endregion

    #region Formatting Tests (IFormattable, ISpanFormattable)

    /// <summary>
    /// Tests ToString with format string.
    /// </summary>
    [Fact]
    public void ToString_WithFormat_FormatsCorrectly()
    {
        GeneratedStatusReg8 a = 255;

        // Hex format
        a.ToString("X", null).Should().Be("FF");
        a.ToString("X2", null).Should().Be("FF");
        a.ToString("X4", null).Should().Be("00FF");

        // Decimal format
        a.ToString("D", null).Should().Be("255");
        a.ToString("D3", null).Should().Be("255");
        a.ToString("D5", null).Should().Be("00255");
    }

    /// <summary>
    /// Tests ToString with format provider.
    /// </summary>
    [Fact]
    public void ToString_WithFormatProvider()
    {
        GeneratedStatusReg8 a = 42;
        a.ToString(null, System.Globalization.CultureInfo.InvariantCulture).Should().Be("42");
    }

    /// <summary>
    /// Tests TryFormat writes to span correctly.
    /// </summary>
    [Fact]
    public void TryFormat_WritesToSpan()
    {
        GeneratedStatusReg8 a = 255;
        Span<char> buffer = stackalloc char[10];

        var success = a.TryFormat(buffer, out int charsWritten, "X2", null);

        success.Should().BeTrue();
        charsWritten.Should().Be(2);
        new string(buffer[..charsWritten]).Should().Be("FF");
    }

    /// <summary>
    /// Tests TryFormat fails gracefully when buffer too small.
    /// </summary>
    [Fact]
    public void TryFormat_BufferTooSmall_ReturnsFalse()
    {
        GeneratedStatusReg8 a = 255;
        Span<char> buffer = stackalloc char[1]; // Too small for "FF"

        var success = a.TryFormat(buffer, out int charsWritten, "X2", null);

        success.Should().BeFalse();
    }

    #endregion

    #region Binary Parsing Tests

    /// <summary>
    /// Tests parsing binary strings with 0b prefix.
    /// </summary>
    [Fact]
    public void Parse_BinaryString_LowerCase()
    {
        var result = GeneratedStatusReg8.Parse("0b11111111");
        ((byte)result).Should().Be(255);

        result = GeneratedStatusReg8.Parse("0b00000001");
        ((byte)result).Should().Be(1);

        result = GeneratedStatusReg8.Parse("0b10101010");
        ((byte)result).Should().Be(0xAA);
    }

    /// <summary>
    /// Tests parsing binary strings with 0B prefix (uppercase).
    /// </summary>
    [Fact]
    public void Parse_BinaryString_UpperCase()
    {
        var result = GeneratedStatusReg8.Parse("0B11111111");
        ((byte)result).Should().Be(255);

        result = GeneratedStatusReg8.Parse("0B00001111");
        ((byte)result).Should().Be(15);
    }

    /// <summary>
    /// Tests TryParse with binary strings.
    /// </summary>
    [Fact]
    public void TryParse_BinaryString_ReturnsTrue()
    {
        var success = GeneratedStatusReg8.TryParse("0b10000000", out var result);
        success.Should().BeTrue();
        ((byte)result).Should().Be(128);
    }

    /// <summary>
    /// Tests TryParse with invalid binary string returns false.
    /// </summary>
    [Fact]
    public void TryParse_InvalidBinaryString_ReturnsFalse()
    {
        var success = GeneratedStatusReg8.TryParse("0b12345", out var result);
        success.Should().BeFalse();
        ((byte)result).Should().Be(0);
    }

    /// <summary>
    /// Tests binary parsing for 16-bit type.
    /// </summary>
    [Fact]
    public void Parse_Binary_16Bit()
    {
        var result = GeneratedKeyboardReg16.Parse("0b1111111111111111");
        ((ushort)result).Should().Be(0xFFFF);

        result = GeneratedKeyboardReg16.Parse("0b0000000100000000");
        ((ushort)result).Should().Be(256);
    }

    /// <summary>
    /// Tests binary parsing for 32-bit type.
    /// </summary>
    [Fact]
    public void Parse_Binary_32Bit()
    {
        var result = GeneratedControlReg32.Parse("0b11111111000000001111111100000000");
        ((uint)result).Should().Be(0xFF00FF00);
    }

    /// <summary>
    /// Tests binary parsing for 64-bit type.
    /// </summary>
    [Fact]
    public void Parse_Binary_64Bit()
    {
        var result = GeneratedWideReg64.Parse("0b1010101010101010101010101010101010101010101010101010101010101010");
        ((ulong)result).Should().Be(0xAAAAAAAAAAAAAAAA);
    }

    #endregion

    #region Underscore Digit Separator Tests

    /// <summary>
    /// Tests parsing decimal numbers with underscores.
    /// </summary>
    [Fact]
    public void Parse_DecimalWithUnderscores()
    {
        var result = GeneratedStatusReg8.Parse("2_5_5");
        ((byte)result).Should().Be(255);

        result = GeneratedStatusReg8.Parse("1_0_0");
        ((byte)result).Should().Be(100);
    }

    /// <summary>
    /// Tests parsing hex numbers with underscores.
    /// </summary>
    [Fact]
    public void Parse_HexWithUnderscores()
    {
        var result = GeneratedStatusReg8.Parse("0xF_F");
        ((byte)result).Should().Be(255);

        result = GeneratedStatusReg8.Parse("0xA_B");
        ((byte)result).Should().Be(0xAB);
    }

    /// <summary>
    /// Tests parsing binary numbers with underscores.
    /// </summary>
    [Fact]
    public void Parse_BinaryWithUnderscores()
    {
        // Common pattern: grouping by nibbles
        var result = GeneratedStatusReg8.Parse("0b1111_1111");
        ((byte)result).Should().Be(255);

        result = GeneratedStatusReg8.Parse("0b1010_1010");
        ((byte)result).Should().Be(0xAA);

        result = GeneratedStatusReg8.Parse("0b0000_0001");
        ((byte)result).Should().Be(1);
    }


    /// <summary>
    /// Tests TryParse with underscores.
    /// </summary>
    [Fact]
    public void TryParse_WithUnderscores_ReturnsTrue()
    {
        GeneratedStatusReg8.TryParse("1_2_8", out var decResult).Should().BeTrue();
        ((byte)decResult).Should().Be(128);

        GeneratedStatusReg8.TryParse("0xFF", out var hexResult).Should().BeTrue();
        ((byte)hexResult).Should().Be(255);

        GeneratedStatusReg8.TryParse("0b1111_0000", out var binResult).Should().BeTrue();
        ((byte)binResult).Should().Be(0xF0);
    }

    /// <summary>
    /// Tests parsing 16-bit numbers with underscores.
    /// </summary>
    [Fact]
    public void Parse_16Bit_WithUnderscores()
    {
        var result = GeneratedKeyboardReg16.Parse("0xFF_FF");
        ((ushort)result).Should().Be(0xFFFF);

        result = GeneratedKeyboardReg16.Parse("0b1111_1111_0000_0000");
        ((ushort)result).Should().Be(0xFF00);

        result = GeneratedKeyboardReg16.Parse("65_535");
        ((ushort)result).Should().Be(65535);
    }

    /// <summary>
    /// Tests parsing 32-bit numbers with underscores.
    /// </summary>
    [Fact]
    public void Parse_32Bit_WithUnderscores()
    {
        var result = GeneratedControlReg32.Parse("0xDEAD_BEEF");
        ((uint)result).Should().Be(0xDEADBEEF);

        result = GeneratedControlReg32.Parse("0b1111_1111_1111_1111_0000_0000_0000_0000");
        ((uint)result).Should().Be(0xFFFF0000);

        result = GeneratedControlReg32.Parse("1_000_000");
        ((uint)result).Should().Be(1_000_000);
    }

    /// <summary>
    /// Tests parsing 64-bit numbers with underscores.
    /// </summary>
    [Fact]
    public void Parse_64Bit_WithUnderscores()
    {
        var result = GeneratedWideReg64.Parse("0xDEAD_BEEF_CAFE_BABE");
        ((ulong)result).Should().Be(0xDEADBEEFCAFEBABE);

        result = GeneratedWideReg64.Parse("1_000_000_000_000");
        ((ulong)result).Should().Be(1_000_000_000_000);
    }

    /// <summary>
    /// Tests that strings without underscores still work (fast path).
    /// </summary>
    [Fact]
    public void Parse_NoUnderscores_StillWorks()
    {
        ((byte)GeneratedStatusReg8.Parse("255")).Should().Be(255);
        ((byte)GeneratedStatusReg8.Parse("0xFF")).Should().Be(255);
        ((byte)GeneratedStatusReg8.Parse("0b11111111")).Should().Be(255);
    }

    #endregion

    #region 16-bit Operator Tests

    /// <summary>
    /// Tests arithmetic operators on 16-bit type.
    /// </summary>
    [Fact]
    public void Arithmetic_16Bit()
    {
        GeneratedKeyboardReg16 a = 1000;
        GeneratedKeyboardReg16 b = 500;

        var sum = a + b;
        ((ushort)sum).Should().Be(1500);

        var diff = a - b;
        ((ushort)diff).Should().Be(500);

        var prod = a * (ushort)2;
        ((ushort)prod).Should().Be(2000);

        var quot = a / b;
        ((ushort)quot).Should().Be(2);
    }

    /// <summary>
    /// Tests comparison operators on 16-bit type.
    /// </summary>
    [Fact]
    public void Comparison_16Bit()
    {
        GeneratedKeyboardReg16 a = 0x1000;
        GeneratedKeyboardReg16 a2 = 0x1000; // Same value for reflexivity test
        GeneratedKeyboardReg16 b = 0x2000;
        GeneratedKeyboardReg16 b2 = 0x2000; // Same value for reflexivity test

        (a < b).Should().BeTrue();
        (b > a).Should().BeTrue();
        (a <= a2).Should().BeTrue(); // Reflexivity
        (b >= b2).Should().BeTrue(); // Reflexivity
    }

    #endregion

    #region 32-bit Operator Tests

    /// <summary>
    /// Tests arithmetic operators on 32-bit type.
    /// </summary>
    [Fact]
    public void Arithmetic_32Bit()
    {
        GeneratedControlReg32 a = 1_000_000;
        GeneratedControlReg32 b = 500_000;

        var sum = a + b;
        ((uint)sum).Should().Be(1_500_000);

        var diff = a - b;
        ((uint)diff).Should().Be(500_000);
    }

    /// <summary>
    /// Tests comparison operators on 32-bit type.
    /// </summary>
    [Fact]
    public void Comparison_32Bit()
    {
        GeneratedControlReg32 a = 0x10000000;
        GeneratedControlReg32 b = 0x20000000;

        (a < b).Should().BeTrue();
        (b > a).Should().BeTrue();
        a.CompareTo(b).Should().BeNegative();
    }

    #endregion

    #region 64-bit Operator Tests

    /// <summary>
    /// Tests arithmetic operators on 64-bit type.
    /// </summary>
    [Fact]
    public void Arithmetic_64Bit()
    {
        GeneratedWideReg64 a = 10_000_000_000UL;
        GeneratedWideReg64 b = 5_000_000_000UL;

        var sum = a + b;
        ((ulong)sum).Should().Be(15_000_000_000UL);

        var diff = a - b;
        ((ulong)diff).Should().Be(5_000_000_000UL);
    }

    /// <summary>
    /// Tests shift operators on 64-bit type.
    /// </summary>
    [Fact]
    public void Shift_64Bit()
    {
        GeneratedWideReg64 a = 1;
        var result = a << 32;
        ((ulong)result).Should().Be(0x100000000UL);

        result = result >> 16;
        ((ulong)result).Should().Be(0x10000UL);
    }

    /// <summary>
    /// Tests comparison operators on 64-bit type.
    /// </summary>
    [Fact]
    public void Comparison_64Bit()
    {
        GeneratedWideReg64 a = 0x1000000000000000UL;
        GeneratedWideReg64 b = 0x2000000000000000UL;

        (a < b).Should().BeTrue();
        (b > a).Should().BeTrue();
        a.CompareTo(b).Should().BeNegative();
    }

    #endregion

    #region Combined Expression Tests

    /// <summary>
    /// Tests complex arithmetic expressions.
    /// </summary>
    [Fact]
    public void ComplexExpression_Arithmetic()
    {
        GeneratedStatusReg8 a = 10;
        GeneratedStatusReg8 b = 5;
        GeneratedStatusReg8 c = 2;

        // (10 + 5) * 2 = 30
        var result = (a + b) * c;
        ((byte)result).Should().Be(30);

        // 10 + 5 * 2 = 20 (operator precedence)
        result = a + b * c;
        ((byte)result).Should().Be(20);
    }

    /// <summary>
    /// Tests combining bitwise and arithmetic operators.
    /// </summary>
    [Fact]
    public void ComplexExpression_BitwiseAndArithmetic()
    {
        GeneratedStatusReg8 a = 0x0F;
        GeneratedStatusReg8 b = 0x10;

        // OR then add
        var result = (a | b) + a;
        ((byte)result).Should().Be(0x2E); // 0x1F + 0x0F = 0x2E

        // Shift then compare
        var shifted = a << 4;
        (shifted > a).Should().BeTrue();
    }

    /// <summary>
    /// Tests using operators with properties still work.
    /// </summary>
    [Fact]
    public void Operators_WithProperties()
    {
        GeneratedStatusReg8 reg = 0;
        reg.Mode = OpMode.Mode5;
        reg.Priority = 2;

        // Add a value to the register
        var result = reg + (byte)1;
        result.Mode.Should().Be(OpMode.Mode5); // Mode should still be 5 (adding 1 to byte doesn't change bits 2-4)
        ((byte)result).Should().Be((byte)((byte)reg + 1));
    }

    #endregion

    #region Sorting and Ordering Tests

    /// <summary>
    /// Tests that BitFields can be sorted using comparison.
    /// </summary>
    [Fact]
    public void Sorting_WorksWithComparison()
    {
        var values = new GeneratedStatusReg8[] { 50, 10, 30, 20, 40 };
        
        Array.Sort(values);
        
        values[0].Should().Be((GeneratedStatusReg8)10);
        values[1].Should().Be((GeneratedStatusReg8)20);
        values[2].Should().Be((GeneratedStatusReg8)30);
        values[3].Should().Be((GeneratedStatusReg8)40);
        values[4].Should().Be((GeneratedStatusReg8)50);
    }

    /// <summary>
    /// Tests that BitFields work with LINQ ordering.
    /// </summary>
    [Fact]
    public void LinqOrdering_Works()
    {
        var values = new GeneratedStatusReg8[] { 50, 10, 30, 20, 40 };
        
        var sorted = values.OrderBy(v => v).ToArray();
        
        ((byte)sorted[0]).Should().Be(10);
        ((byte)sorted[4]).Should().Be(50);

        var descending = values.OrderByDescending(v => v).ToArray();
        ((byte)descending[0]).Should().Be(50);
        ((byte)descending[4]).Should().Be(10);
    }

    #endregion

    #region Native Type Parity Tests

    /// <summary>
    /// Tests that overflow behavior matches native byte arithmetic.
    /// </summary>
    [Fact]
    public void Parity_Byte_OverflowBehavior()
    {
        // Compare BitFields overflow with native byte overflow
        byte nativeA = byte.MaxValue;
        byte nativeB = 1;
        byte nativeResult = unchecked((byte)(nativeA + nativeB));

        GeneratedStatusReg8 bfA = byte.MaxValue;
        GeneratedStatusReg8 bfB = 1;
        var bfResult = bfA + bfB;

        ((byte)bfResult).Should().Be(nativeResult, "BitFields overflow should match native byte overflow");
    }

    /// <summary>
    /// Tests that underflow behavior matches native byte arithmetic.
    /// </summary>
    [Fact]
    public void Parity_Byte_UnderflowBehavior()
    {
        byte nativeA = 0;
        byte nativeB = 1;
        byte nativeResult = unchecked((byte)(nativeA - nativeB));

        GeneratedStatusReg8 bfA = 0;
        GeneratedStatusReg8 bfB = 1;
        var bfResult = bfA - bfB;

        ((byte)bfResult).Should().Be(nativeResult, "BitFields underflow should match native byte underflow");
    }

    /// <summary>
    /// Tests that multiplication overflow matches native behavior.
    /// </summary>
    [Fact]
    public void Parity_Byte_MultiplicationOverflow()
    {
        byte nativeA = 200;
        byte nativeB = 2;
        byte nativeResult = unchecked((byte)(nativeA * nativeB));

        GeneratedStatusReg8 bfA = 200;
        GeneratedStatusReg8 bfB = 2;
        var bfResult = bfA * bfB;

        // 200 * 2 = 400 = 0x190, truncated to byte = 0x90 = 144
        ((byte)bfResult).Should().Be(nativeResult, "BitFields multiplication overflow should match native");
        ((byte)bfResult).Should().Be(144);
    }

    /// <summary>
    /// Tests that division by zero throws DivideByZeroException like native types.
    /// </summary>
    [Fact]
    public void Parity_DivisionByZero_ThrowsSameException()
    {
        GeneratedStatusReg8 a = 10;
        GeneratedStatusReg8 zero = 0;

        // Native behavior
        FluentActions.Invoking(() => { byte x = 10; byte y = 0; var _ = x / y; })
            .Should().Throw<DivideByZeroException>();

        // BitFields should match
        FluentActions.Invoking(() => { var _ = a / zero; })
            .Should().Throw<DivideByZeroException>();
    }

    /// <summary>
    /// Tests that modulus by zero throws DivideByZeroException like native types.
    /// </summary>
    [Fact]
    public void Parity_ModulusByZero_ThrowsSameException()
    {
        GeneratedStatusReg8 a = 10;
        GeneratedStatusReg8 zero = 0;

        // BitFields should match native
        FluentActions.Invoking(() => { var _ = a % zero; })
            .Should().Throw<DivideByZeroException>();
    }

    /// <summary>
    /// Tests two's complement negation behavior for unsigned types.
    /// Note: Native unsigned types don't support unary -, but we provide it for convenience.
    /// This tests that the implementation produces correct two's complement results.
    /// </summary>
    [Fact]
    public void Parity_UnaryNegation_TwosComplement()
    {
        // Two's complement of 1 should be MAX (all bits set except bit 0)
        GeneratedStatusReg8 one = 1;
        var negOne = -one;
        ((byte)negOne).Should().Be(0xFF); // -1 in two's complement = 255

        // Two's complement of 0 should be 0
        GeneratedStatusReg8 zero = 0;
        var negZero = -zero;
        ((byte)negZero).Should().Be(0);

        // Two's complement of MAX should be 1
        GeneratedStatusReg8 max = 0xFF;
        var negMax = -max;
        ((byte)negMax).Should().Be(1); // -(255) in two's complement = 1

        // Verify via formula: -x = ~x + 1
        for (byte i = 0; i < 10; i++)
        {
            GeneratedStatusReg8 bf = i;
            var neg = -bf;
            byte expected = unchecked((byte)(~i + 1));
            ((byte)neg).Should().Be(expected, $"Two's complement of {i} should be {expected}");
        }
    }

    /// <summary>
    /// Tests shift operators match native behavior.
    /// </summary>
    [Fact]
    public void Parity_ShiftOperators()
    {
        byte nativeValue = 0x0F;
        GeneratedStatusReg8 bfValue = 0x0F;

        // Left shift
        byte nativeLeft = (byte)(nativeValue << 4);
        var bfLeft = bfValue << 4;
        ((byte)bfLeft).Should().Be(nativeLeft);

        // Right shift
        byte nativeRight = (byte)(nativeValue >> 2);
        var bfRight = bfValue >> 2;
        ((byte)bfRight).Should().Be(nativeRight);

        // Shift with overflow
        byte nativeOverflow = (byte)(nativeValue << 8);
        var bfOverflow = bfValue << 8;
        ((byte)bfOverflow).Should().Be(nativeOverflow);
    }

    /// <summary>
    /// Tests 16-bit type parity with native ushort.
    /// </summary>
    [Fact]
    public void Parity_UShort_Overflow()
    {
        ushort nativeMax = ushort.MaxValue;
        ushort nativeResult = unchecked((ushort)(nativeMax + 1));

        GeneratedKeyboardReg16 bfMax = ushort.MaxValue;
        var bfResult = bfMax + (ushort)1;

        ((ushort)bfResult).Should().Be(nativeResult);
        ((ushort)bfResult).Should().Be(0);
    }

    /// <summary>
    /// Tests 32-bit type parity with native uint.
    /// </summary>
    [Fact]
    public void Parity_UInt_Overflow()
    {
        uint nativeMax = uint.MaxValue;
        uint nativeResult = unchecked(nativeMax + 1);

        GeneratedControlReg32 bfMax = uint.MaxValue;
        var bfResult = bfMax + (uint)1;

        ((uint)bfResult).Should().Be(nativeResult);
        ((uint)bfResult).Should().Be(0);
    }

    /// <summary>
    /// Tests 64-bit type parity with native ulong.
    /// </summary>
    [Fact]
    public void Parity_ULong_Overflow()
    {
        ulong nativeMax = ulong.MaxValue;
        ulong nativeResult = unchecked(nativeMax + 1);

        GeneratedWideReg64 bfMax = ulong.MaxValue;
        var bfResult = bfMax + (ulong)1;

        ((ulong)bfResult).Should().Be(nativeResult);
        ((ulong)bfResult).Should().Be(0);
    }

    #endregion
}
