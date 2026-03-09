using FluentAssertions;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Tests for the Little-Endian integer types.
/// </summary>
public class LittleEndianTests
{
    #region UInt16Le Tests

    [Fact]
    public void UInt16Le_Constructor_FromUshort()
    {
        UInt16Le value = new(0x1234);
        ((ushort)value).Should().Be(0x1234);
    }

    [Fact]
    public void UInt16Le_Constructor_FromSpan()
    {
        ReadOnlySpan<byte> bytes = stackalloc byte[] { 0x34, 0x12 }; // LE: lo first
        var value = new UInt16Le(bytes);
        ((ushort)value).Should().Be(0x1234);
    }

    [Fact]
    public void UInt16Le_Constructor_FromByteArray()
    {
        byte[] bytes = [0xCD, 0xAB];
        var value = new UInt16Le(bytes);
        ((ushort)value).Should().Be(0xABCD);
    }

    [Fact]
    public void UInt16Le_Constructor_FromByteArray_WithOffset()
    {
        byte[] bytes = [0x00, 0x34, 0x12];
        var value = new UInt16Le(bytes, 1);
        ((ushort)value).Should().Be(0x1234);
    }

    [Fact]
    public void UInt16Le_WriteTo_Span()
    {
        UInt16Le value = 0x1234;
        Span<byte> buffer = stackalloc byte[2];
        value.WriteTo(buffer);
        buffer[0].Should().Be(0x34); // lo first
        buffer[1].Should().Be(0x12);
    }

    [Fact]
    public void UInt16Le_TryWriteTo_Span_Success()
    {
        UInt16Le value = 0xABCD;
        Span<byte> buffer = stackalloc byte[4];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeTrue();
        buffer[0].Should().Be(0xCD);
        buffer[1].Should().Be(0xAB);
    }

    [Fact]
    public void UInt16Le_TryWriteTo_Span_ExactSize()
    {
        UInt16Le value = 0xABCD;
        Span<byte> buffer = stackalloc byte[2];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeTrue();
        buffer[0].Should().Be(0xCD);
        buffer[1].Should().Be(0xAB);
    }

    [Fact]
    public void UInt16Le_TryWriteTo_Span_TooSmall()
    {
        UInt16Le value = 0xABCD;
        Span<byte> buffer = stackalloc byte[1];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeFalse();
    }

    [Fact]
    public void UInt16Le_ReadFrom_Span()
    {
        ReadOnlySpan<byte> bytes = stackalloc byte[] { 0xCD, 0xAB };
        var value = UInt16Le.ReadFrom(bytes);
        ((ushort)value).Should().Be(0xABCD);
    }

    [Fact]
    public void UInt16Le_MemoryLayout_IsLittleEndian()
    {
        UInt16Le value = 0x0102;
        Span<byte> buffer = stackalloc byte[2];
        value.WriteTo(buffer);
        buffer[0].Should().Be(0x02, "least significant byte comes first in LE");
        buffer[1].Should().Be(0x01, "most significant byte comes last in LE");
    }

    [Fact]
    public void UInt16Le_Parse_IFormatProvider()
    {
        var value = UInt16Le.Parse("1234", null);
        ((ushort)value).Should().Be(1234);
    }

    [Fact]
    public void UInt16Le_Parse_Hex()
    {
        var value = UInt16Le.Parse("ABCD", System.Globalization.NumberStyles.HexNumber);
        ((ushort)value).Should().Be(0xABCD);
    }

    [Fact]
    public void UInt16Le_TryParse_String_Success()
    {
        bool result = UInt16Le.TryParse("5678", null, out var value);
        result.Should().BeTrue();
        ((ushort)value).Should().Be(5678);
    }

    [Fact]
    public void UInt16Le_TryParse_String_Failure()
    {
        bool result = UInt16Le.TryParse("invalid", null, out var value);
        result.Should().BeFalse();
    }

    [Fact]
    public void UInt16Le_TryParse_Span_Success()
    {
        bool result = UInt16Le.TryParse("9999".AsSpan(), null, out var value);
        result.Should().BeTrue();
        ((ushort)value).Should().Be(9999);
    }

    [Fact]
    public void UInt16Le_TryFormat_Success()
    {
        UInt16Le value = 0x00FF;
        Span<char> buffer = stackalloc char[10];
        bool result = value.TryFormat(buffer, out int charsWritten, "X4", null);
        result.Should().BeTrue();
        new string(buffer[..charsWritten]).Should().Be("00FF");
    }

    [Fact]
    public void UInt16Le_ToString_Default()
    {
        UInt16Le value = 0x1234;
        value.ToString().Should().Be("0x1234");
    }

    [Fact]
    public void UInt16Le_ToString_Format()
    {
        UInt16Le value = 0x1234;
        value.ToString("X4", null).Should().Be("1234");
        value.ToString("D", null).Should().Be("4660");
    }

    [Fact]
    public void UInt16Le_Arithmetic_Add()
    {
        UInt16Le a = 100;
        UInt16Le b = 200;
        UInt16Le c = a + b;
        ((ushort)c).Should().Be(300);
    }

    [Fact]
    public void UInt16Le_Arithmetic_Subtract()
    {
        UInt16Le a = 500;
        UInt16Le b = 200;
        UInt16Le c = a - b;
        ((ushort)c).Should().Be(300);
    }

    [Fact]
    public void UInt16Le_Arithmetic_Multiply()
    {
        UInt16Le a = 100;
        UInt16Le b = 200;
        UInt16Le c = a * b;
        ((ushort)c).Should().Be(20000);
    }

    [Fact]
    public void UInt16Le_Arithmetic_Divide()
    {
        UInt16Le a = 1000;
        UInt16Le b = 10;
        UInt16Le c = a / b;
        ((ushort)c).Should().Be(100);
    }

    [Fact]
    public void UInt16Le_Arithmetic_DivideByZero_Throws()
    {
        UInt16Le a = 100;
        UInt16Le b = 0;
        var act = () => { var _ = a / b; };
        act.Should().Throw<DivideByZeroException>();
    }

    [Fact]
    public void UInt16Le_Bitwise_And()
    {
        UInt16Le a = 0xFF00;
        UInt16Le b = 0x0FF0;
        UInt16Le c = a & b;
        ((ushort)c).Should().Be(0x0F00);
    }

    [Fact]
    public void UInt16Le_Bitwise_Or()
    {
        UInt16Le a = 0xFF00;
        UInt16Le b = 0x00FF;
        UInt16Le c = a | b;
        ((ushort)c).Should().Be(0xFFFF);
    }

    [Fact]
    public void UInt16Le_Bitwise_Not()
    {
        UInt16Le a = 0;
        UInt16Le b = ~a;
        ((ushort)b).Should().Be(0xFFFF);
    }

    [Fact]
    public void UInt16Le_Shift_Left()
    {
        UInt16Le a = 1;
        UInt16Le b = a << 15;
        ((ushort)b).Should().Be(0x8000);
    }

    [Fact]
    public void UInt16Le_Shift_Right()
    {
        UInt16Le a = 0x8000;
        UInt16Le b = a >> 15;
        ((ushort)b).Should().Be(1);
    }

    [Fact]
    public void UInt16Le_Comparison_LessThan()
    {
        UInt16Le a = 100;
        UInt16Le b = 200;
        (a < b).Should().BeTrue();
        (b < a).Should().BeFalse();
    }

    [Fact]
    public void UInt16Le_Comparison_Equality()
    {
        UInt16Le a = 12345;
        UInt16Le b = 12345;
        UInt16Le c = 54321;
        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
    }

    [Fact]
    public void UInt16Le_Increment()
    {
        UInt16Le value = 99;
        value++;
        ((ushort)value).Should().Be(100);
    }

    [Fact]
    public void UInt16Le_Decrement()
    {
        UInt16Le value = 100;
        value--;
        ((ushort)value).Should().Be(99);
    }

    [Fact]
    public void UInt16Le_Equals_Object()
    {
        UInt16Le a = 12345;
        object b = (UInt16Le)12345;
        object c = (UInt16Le)54321;
        a.Equals(b).Should().BeTrue();
        a.Equals(c).Should().BeFalse();
    }

    [Fact]
    public void UInt16Le_GetHashCode_Consistent()
    {
        UInt16Le a = 0x1234;
        UInt16Le b = 0x1234;
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    #endregion

    #region UInt32Le Tests

    [Fact]
    public void UInt32Le_Constructor_FromUint()
    {
        UInt32Le value = new(0x12345678U);
        ((uint)value).Should().Be(0x12345678U);
    }

    [Fact]
    public void UInt32Le_Constructor_FromSpan()
    {
        ReadOnlySpan<byte> bytes = stackalloc byte[] { 0x78, 0x56, 0x34, 0x12 }; // LE: lo first
        var value = new UInt32Le(bytes);
        ((uint)value).Should().Be(0x12345678U);
    }

    [Fact]
    public void UInt32Le_Constructor_FromByteArray()
    {
        byte[] bytes = [0xEF, 0xBE, 0xAD, 0xDE];
        var value = new UInt32Le(bytes);
        ((uint)value).Should().Be(0xDEADBEEFU);
    }

    [Fact]
    public void UInt32Le_Constructor_FromByteArray_WithOffset()
    {
        byte[] bytes = [0x00, 0x00, 0x78, 0x56, 0x34, 0x12];
        var value = new UInt32Le(bytes, 2);
        ((uint)value).Should().Be(0x12345678U);
    }

    [Fact]
    public void UInt32Le_WriteTo_Span()
    {
        UInt32Le value = 0x12345678;
        Span<byte> buffer = stackalloc byte[4];
        value.WriteTo(buffer);
        buffer[0].Should().Be(0x78); // lo first
        buffer[1].Should().Be(0x56);
        buffer[2].Should().Be(0x34);
        buffer[3].Should().Be(0x12);
    }

    [Fact]
    public void UInt32Le_TryWriteTo_Success()
    {
        UInt32Le value = 0xAABBCCDD;
        Span<byte> buffer = stackalloc byte[8];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeTrue();
        buffer[0].Should().Be(0xDD); // lo first
        buffer[3].Should().Be(0xAA);
    }

    [Fact]
    public void UInt32Le_TryWriteTo_ExactSize()
    {
        UInt32Le value = 0xAABBCCDD;
        Span<byte> buffer = stackalloc byte[4];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeTrue();
        buffer[0].Should().Be(0xDD);
        buffer[1].Should().Be(0xCC);
        buffer[2].Should().Be(0xBB);
        buffer[3].Should().Be(0xAA);
    }

    [Fact]
    public void UInt32Le_TryWriteTo_TooSmall()
    {
        UInt32Le value = 0x12345678;
        Span<byte> buffer = stackalloc byte[2];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeFalse();
    }

    [Fact]
    public void UInt32Le_ReadFrom_Span()
    {
        ReadOnlySpan<byte> bytes = stackalloc byte[] { 0xEF, 0xBE, 0xAD, 0xDE };
        var value = UInt32Le.ReadFrom(bytes);
        ((uint)value).Should().Be(0xDEADBEEFU);
    }

    [Fact]
    public void UInt32Le_MemoryLayout_IsLittleEndian()
    {
        UInt32Le value = 0x01020304;
        Span<byte> buffer = stackalloc byte[4];
        value.WriteTo(buffer);
        buffer[0].Should().Be(0x04, "least significant byte comes first in LE");
        buffer[1].Should().Be(0x03);
        buffer[2].Should().Be(0x02);
        buffer[3].Should().Be(0x01, "most significant byte comes last in LE");
    }

    [Fact]
    public void UInt32Le_Parse_IParsable()
    {
        var value = UInt32Le.Parse("12345678".AsSpan(), null);
        ((uint)value).Should().Be(12345678U);
    }

    [Fact]
    public void UInt32Le_Parse_Hex()
    {
        var value = UInt32Le.Parse("DEADBEEF", System.Globalization.NumberStyles.HexNumber);
        ((uint)value).Should().Be(0xDEADBEEFU);
    }

    [Fact]
    public void UInt32Le_TryFormat()
    {
        UInt32Le value = 0xDEADBEEF;
        Span<char> buffer = stackalloc char[16];
        bool result = value.TryFormat(buffer, out int charsWritten, "X8", null);
        result.Should().BeTrue();
        new string(buffer[..charsWritten]).Should().Be("DEADBEEF");
    }

    [Fact]
    public void UInt32Le_ToString_Default()
    {
        UInt32Le value = 0x12345678;
        value.ToString().Should().Be("0x12345678");
    }

    [Fact]
    public void UInt32Le_Arithmetic_Add()
    {
        UInt32Le a = 100U;
        UInt32Le b = 200U;
        UInt32Le c = a + b;
        ((uint)c).Should().Be(300U);
    }

    [Fact]
    public void UInt32Le_Arithmetic_Subtract()
    {
        UInt32Le a = 500U;
        UInt32Le b = 200U;
        UInt32Le c = a - b;
        ((uint)c).Should().Be(300U);
    }

    [Fact]
    public void UInt32Le_Arithmetic_Multiply()
    {
        UInt32Le a = 100U;
        UInt32Le b = 200U;
        UInt32Le c = a * b;
        ((uint)c).Should().Be(20000U);
    }

    [Fact]
    public void UInt32Le_Arithmetic_Divide()
    {
        UInt32Le a = 1000U;
        UInt32Le b = 10U;
        UInt32Le c = a / b;
        ((uint)c).Should().Be(100U);
    }

    [Fact]
    public void UInt32Le_Arithmetic_DivideByZero_Throws()
    {
        UInt32Le a = 100U;
        UInt32Le b = 0U;
        var act = () => { var _ = a / b; };
        act.Should().Throw<DivideByZeroException>();
    }

    [Fact]
    public void UInt32Le_Bitwise_And()
    {
        UInt32Le a = 0xFF00FF00U;
        UInt32Le b = 0x00FF00FFU;
        UInt32Le c = a & b;
        ((uint)c).Should().Be(0U);
    }

    [Fact]
    public void UInt32Le_Bitwise_Or()
    {
        UInt32Le a = 0xFF00FF00U;
        UInt32Le b = 0x00FF00FFU;
        UInt32Le c = a | b;
        ((uint)c).Should().Be(0xFFFFFFFFU);
    }

    [Fact]
    public void UInt32Le_Bitwise_Not()
    {
        UInt32Le a = 0U;
        UInt32Le b = ~a;
        ((uint)b).Should().Be(0xFFFFFFFFU);
    }

    [Fact]
    public void UInt32Le_Shift_Left()
    {
        UInt32Le a = 1U;
        UInt32Le b = a << 31;
        ((uint)b).Should().Be(0x80000000U);
    }

    [Fact]
    public void UInt32Le_Shift_Right()
    {
        UInt32Le a = 0x80000000U;
        UInt32Le b = a >> 31;
        ((uint)b).Should().Be(1U);
    }

    [Fact]
    public void UInt32Le_Comparison_LessThan()
    {
        UInt32Le a = 100U;
        UInt32Le b = 200U;
        (a < b).Should().BeTrue();
        (b < a).Should().BeFalse();
    }

    [Fact]
    public void UInt32Le_Comparison_Equality()
    {
        UInt32Le a = 12345U;
        UInt32Le b = 12345U;
        UInt32Le c = 54321U;
        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
    }

    [Fact]
    public void UInt32Le_Increment()
    {
        UInt32Le value = 99U;
        value++;
        ((uint)value).Should().Be(100U);
    }

    [Fact]
    public void UInt32Le_Decrement()
    {
        UInt32Le value = 100U;
        value--;
        ((uint)value).Should().Be(99U);
    }

    [Fact]
    public void UInt32Le_Equals_Object()
    {
        UInt32Le a = 12345U;
        object b = (UInt32Le)12345U;
        object c = (UInt32Le)54321U;
        a.Equals(b).Should().BeTrue();
        a.Equals(c).Should().BeFalse();
    }

    [Fact]
    public void UInt32Le_GetHashCode_Consistent()
    {
        UInt32Le a = 0x12345678U;
        UInt32Le b = 0x12345678U;
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    #endregion

    #region UInt64Le Tests

    [Fact]
    public void UInt64Le_Constructor_FromUlong()
    {
        UInt64Le value = new(0x123456789ABCDEF0UL);
        ((ulong)value).Should().Be(0x123456789ABCDEF0UL);
    }

    [Fact]
    public void UInt64Le_Constructor_FromSpan()
    {
        ReadOnlySpan<byte> bytes = stackalloc byte[] { 0xF0, 0xDE, 0xBC, 0x9A, 0x78, 0x56, 0x34, 0x12 };
        var value = new UInt64Le(bytes);
        ((ulong)value).Should().Be(0x123456789ABCDEF0UL);
    }

    [Fact]
    public void UInt64Le_WriteTo_Span()
    {
        UInt64Le value = 0x0102030405060708UL;
        Span<byte> buffer = stackalloc byte[8];
        value.WriteTo(buffer);
        buffer[0].Should().Be(0x08); // lo first
        buffer[1].Should().Be(0x07);
        buffer[2].Should().Be(0x06);
        buffer[3].Should().Be(0x05);
        buffer[4].Should().Be(0x04);
        buffer[5].Should().Be(0x03);
        buffer[6].Should().Be(0x02);
        buffer[7].Should().Be(0x01);
    }

    [Fact]
    public void UInt64Le_TryWriteTo_Success()
    {
        UInt64Le value = 0xAABBCCDDEEFF0011UL;
        Span<byte> buffer = stackalloc byte[16];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeTrue();
        buffer[0].Should().Be(0x11); // lo first
        buffer[7].Should().Be(0xAA);
    }

    [Fact]
    public void UInt64Le_TryWriteTo_ExactSize()
    {
        UInt64Le value = 0x0102030405060708UL;
        Span<byte> buffer = stackalloc byte[8];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeTrue();
        buffer[0].Should().Be(0x08);
        buffer[1].Should().Be(0x07);
        buffer[2].Should().Be(0x06);
        buffer[3].Should().Be(0x05);
        buffer[4].Should().Be(0x04);
        buffer[5].Should().Be(0x03);
        buffer[6].Should().Be(0x02);
        buffer[7].Should().Be(0x01);
    }

    [Fact]
    public void UInt64Le_TryWriteTo_TooSmall()
    {
        UInt64Le value = 0x1234UL;
        Span<byte> buffer = stackalloc byte[4];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeFalse();
    }

    [Fact]
    public void UInt64Le_ReadFrom_Span()
    {
        ReadOnlySpan<byte> bytes = stackalloc byte[] { 0x10, 0x32, 0x54, 0x76, 0x98, 0xBA, 0xDC, 0xFE };
        var value = UInt64Le.ReadFrom(bytes);
        ((ulong)value).Should().Be(0xFEDCBA9876543210UL);
    }

    [Fact]
    public void UInt64Le_MemoryLayout_IsLittleEndian()
    {
        UInt64Le value = 0x0102030405060708UL;
        Span<byte> buffer = stackalloc byte[8];
        value.WriteTo(buffer);
        buffer[0].Should().Be(0x08, "least significant byte comes first in LE");
        buffer[7].Should().Be(0x01, "most significant byte comes last in LE");
    }

    [Fact]
    public void UInt64Le_Arithmetic_Add()
    {
        UInt64Le a = 100UL;
        UInt64Le b = 200UL;
        UInt64Le c = a + b;
        ((ulong)c).Should().Be(300UL);
    }

    [Fact]
    public void UInt64Le_Arithmetic_Subtract()
    {
        UInt64Le a = 500UL;
        UInt64Le b = 200UL;
        UInt64Le c = a - b;
        ((ulong)c).Should().Be(300UL);
    }

    [Fact]
    public void UInt64Le_Arithmetic_Multiply()
    {
        UInt64Le a = 100UL;
        UInt64Le b = 200UL;
        UInt64Le c = a * b;
        ((ulong)c).Should().Be(20000UL);
    }

    [Fact]
    public void UInt64Le_Arithmetic_Divide()
    {
        UInt64Le a = 1000UL;
        UInt64Le b = 10UL;
        UInt64Le c = a / b;
        ((ulong)c).Should().Be(100UL);
    }

    [Fact]
    public void UInt64Le_Arithmetic_DivideByZero_Throws()
    {
        UInt64Le a = 100UL;
        UInt64Le b = 0UL;
        var act = () => { var _ = a / b; };
        act.Should().Throw<DivideByZeroException>();
    }

    [Fact]
    public void UInt64Le_Bitwise_And()
    {
        UInt64Le a = 0xFF00FF00FF00FF00UL;
        UInt64Le b = 0x00FF00FF00FF00FFUL;
        UInt64Le c = a & b;
        ((ulong)c).Should().Be(0UL);
    }

    [Fact]
    public void UInt64Le_Bitwise_Or()
    {
        UInt64Le a = 0xFF00FF00FF00FF00UL;
        UInt64Le b = 0x00FF00FF00FF00FFUL;
        UInt64Le c = a | b;
        ((ulong)c).Should().Be(0xFFFFFFFFFFFFFFFFUL);
    }

    [Fact]
    public void UInt64Le_Bitwise_Xor()
    {
        UInt64Le a = 0x123456789ABCDEF0UL;
        UInt64Le b = 0x123456789ABCDEF0UL;
        UInt64Le c = a ^ b;
        ((ulong)c).Should().Be(0UL);
    }

    [Fact]
    public void UInt64Le_Bitwise_Not()
    {
        UInt64Le a = 0UL;
        UInt64Le b = ~a;
        ((ulong)b).Should().Be(0xFFFFFFFFFFFFFFFFUL);
    }

    [Fact]
    public void UInt64Le_Shift_Left()
    {
        UInt64Le a = 1UL;
        UInt64Le b = a << 63;
        ((ulong)b).Should().Be(0x8000000000000000UL);
    }

    [Fact]
    public void UInt64Le_Shift_Right()
    {
        UInt64Le a = 0x8000000000000000UL;
        UInt64Le b = a >> 63;
        ((ulong)b).Should().Be(1UL);
    }

    [Fact]
    public void UInt64Le_Comparison_LessThan()
    {
        UInt64Le a = 100UL;
        UInt64Le b = 200UL;
        (a < b).Should().BeTrue();
        (b < a).Should().BeFalse();
    }

    [Fact]
    public void UInt64Le_Comparison_GreaterThan()
    {
        UInt64Le a = 200UL;
        UInt64Le b = 100UL;
        (a > b).Should().BeTrue();
        (b > a).Should().BeFalse();
    }

    [Fact]
    public void UInt64Le_Comparison_Equality()
    {
        UInt64Le a = 12345UL;
        UInt64Le b = 12345UL;
        UInt64Le c = 54321UL;
        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
    }

    [Fact]
    public void UInt64Le_Parse_Decimal()
    {
        var value = UInt64Le.Parse("18446744073709551615"); // ulong.MaxValue
        ((ulong)value).Should().Be(ulong.MaxValue);
    }

    [Fact]
    public void UInt64Le_Parse_Hex()
    {
        var value = UInt64Le.Parse("FFFFFFFFFFFFFFFF", System.Globalization.NumberStyles.HexNumber);
        ((ulong)value).Should().Be(ulong.MaxValue);
    }

    [Fact]
    public void UInt64Le_TryParse_String_Success()
    {
        bool result = UInt64Le.TryParse("1234567890123456789", null, out var value);
        result.Should().BeTrue();
        ((ulong)value).Should().Be(1234567890123456789UL);
    }

    [Fact]
    public void UInt64Le_TryParse_Span_Success()
    {
        bool result = UInt64Le.TryParse("9876543210".AsSpan(), null, out var value);
        result.Should().BeTrue();
        ((ulong)value).Should().Be(9876543210UL);
    }

    [Fact]
    public void UInt64Le_TryFormat()
    {
        UInt64Le value = 0xDEADBEEFCAFEBABEUL;
        Span<char> buffer = stackalloc char[20];
        bool result = value.TryFormat(buffer, out int charsWritten, "X16", null);
        result.Should().BeTrue();
        new string(buffer[..charsWritten]).Should().Be("DEADBEEFCAFEBABE");
    }

    [Fact]
    public void UInt64Le_ToString_Default()
    {
        UInt64Le value = 0x1234567890ABCDEFUL;
        value.ToString().Should().Be("0x1234567890abcdef");
    }

    [Fact]
    public void UInt64Le_Equals_Object()
    {
        UInt64Le a = 12345UL;
        object b = (UInt64Le)12345UL;
        object c = (UInt64Le)54321UL;
        a.Equals(b).Should().BeTrue();
        a.Equals(c).Should().BeFalse();
    }

    [Fact]
    public void UInt64Le_GetHashCode_Consistent()
    {
        UInt64Le a = 0x123456789ABCDEF0UL;
        UInt64Le b = 0x123456789ABCDEF0UL;
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void UInt64Le_Increment()
    {
        UInt64Le value = 99UL;
        value++;
        ((ulong)value).Should().Be(100UL);
    }

    [Fact]
    public void UInt64Le_Decrement()
    {
        UInt64Le value = 100UL;
        value--;
        ((ulong)value).Should().Be(99UL);
    }

    #endregion

    #region Int16Le Tests

    [Fact]
    public void Int16Le_Constructor_FromShort()
    {
        Int16Le value = new((short)-1234);
        ((short)value).Should().Be(-1234);
    }

    [Fact]
    public void Int16Le_Constructor_FromSpan()
    {
        ReadOnlySpan<byte> bytes = stackalloc byte[] { 0xFE, 0xFF }; // -2 in little-endian
        var value = new Int16Le(bytes);
        ((short)value).Should().Be(-2);
    }

    [Fact]
    public void Int16Le_WriteTo_Span()
    {
        Int16Le value = -1;
        Span<byte> buffer = stackalloc byte[2];
        value.WriteTo(buffer);
        buffer[0].Should().Be(0xFF);
        buffer[1].Should().Be(0xFF);
    }

    [Fact]
    public void Int16Le_WriteTo_Span_Positive()
    {
        Int16Le value = 0x0102;
        Span<byte> buffer = stackalloc byte[2];
        value.WriteTo(buffer);
        buffer[0].Should().Be(0x02); // lo first
        buffer[1].Should().Be(0x01);
    }

    [Fact]
    public void Int16Le_MemoryLayout_IsLittleEndian()
    {
        Int16Le value = 0x0102;
        Span<byte> buffer = stackalloc byte[2];
        value.WriteTo(buffer);
        buffer[0].Should().Be(0x02, "least significant byte comes first in LE");
        buffer[1].Should().Be(0x01, "most significant byte comes last in LE");
    }

    [Fact]
    public void Int16Le_TryWriteTo_Span_Success()
    {
        Int16Le value = 0x0102;
        Span<byte> buffer = stackalloc byte[4];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeTrue();
        buffer[0].Should().Be(0x02);
        buffer[1].Should().Be(0x01);
    }

    [Fact]
    public void Int16Le_TryWriteTo_Span_ExactSize()
    {
        Int16Le value = 0x0102;
        Span<byte> buffer = stackalloc byte[2];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeTrue();
        buffer[0].Should().Be(0x02);
        buffer[1].Should().Be(0x01);
    }

    [Fact]
    public void Int16Le_TryWriteTo_Span_TooSmall()
    {
        Int16Le value = 0x0102;
        Span<byte> buffer = stackalloc byte[1];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeFalse();
    }

    [Fact]
    public void Int16Le_TryWriteTo_Span_Negative()
    {
        Int16Le value = -2;
        Span<byte> buffer = stackalloc byte[2];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeTrue();
        buffer[0].Should().Be(0xFE);
        buffer[1].Should().Be(0xFF);
    }

    [Fact]
    public void Int16Le_TryParse_Success()
    {
        bool result = Int16Le.TryParse("-100", null, out var value);
        result.Should().BeTrue();
        ((short)value).Should().Be(-100);
    }

    [Fact]
    public void Int16Le_TryParse_Failure()
    {
        bool result = Int16Le.TryParse("invalid", null, out _);
        result.Should().BeFalse();
    }

    [Fact]
    public void Int16Le_Parse_Hex()
    {
        var value = Int16Le.Parse("7FFF", System.Globalization.NumberStyles.HexNumber);
        ((short)value).Should().Be(short.MaxValue);
    }

    [Fact]
    public void Int16Le_Arithmetic_Add()
    {
        Int16Le a = -100;
        Int16Le b = 200;
        Int16Le c = a + b;
        ((short)c).Should().Be(100);
    }

    [Fact]
    public void Int16Le_Arithmetic_Negate()
    {
        Int16Le a = 12345;
        Int16Le b = -a;
        ((short)b).Should().Be(-12345);
    }

    [Fact]
    public void Int16Le_Comparison_Signed()
    {
        Int16Le positive = 100;
        Int16Le negative = -100;
        (negative < positive).Should().BeTrue();
        (positive > negative).Should().BeTrue();
    }

    [Fact]
    public void Int16Le_Increment()
    {
        Int16Le value = -1;
        value++;
        ((short)value).Should().Be(0);
    }

    [Fact]
    public void Int16Le_Decrement()
    {
        Int16Le value = 0;
        value--;
        ((short)value).Should().Be(-1);
    }

    [Fact]
    public void Int16Le_Equals_Object()
    {
        Int16Le a = -100;
        object b = (Int16Le)(-100);
        object c = (Int16Le)100;
        a.Equals(b).Should().BeTrue();
        a.Equals(c).Should().BeFalse();
    }

    [Fact]
    public void Int16Le_ImplicitConversion_ToInt()
    {
        Int16Le small = -1000;
        int wide = small;
        wide.Should().Be(-1000);
    }

    #endregion

    #region Int32Le Tests

    [Fact]
    public void Int32Le_Constructor_FromInt()
    {
        Int32Le value = new(-12345678);
        ((int)value).Should().Be(-12345678);
    }

    [Fact]
    public void Int32Le_Constructor_FromSpan()
    {
        ReadOnlySpan<byte> bytes = stackalloc byte[] { 0xFE, 0xFF, 0xFF, 0xFF }; // -2 in little-endian
        var value = new Int32Le(bytes);
        ((int)value).Should().Be(-2);
    }

    [Fact]
    public void Int32Le_WriteTo_Span()
    {
        Int32Le value = -1;
        Span<byte> buffer = stackalloc byte[4];
        value.WriteTo(buffer);
        buffer[0].Should().Be(0xFF);
        buffer[1].Should().Be(0xFF);
        buffer[2].Should().Be(0xFF);
        buffer[3].Should().Be(0xFF);
    }

    [Fact]
    public void Int32Le_WriteTo_Span_Positive()
    {
        Int32Le value = 0x01020304;
        Span<byte> buffer = stackalloc byte[4];
        value.WriteTo(buffer);
        buffer[0].Should().Be(0x04); // lo first
        buffer[1].Should().Be(0x03);
        buffer[2].Should().Be(0x02);
        buffer[3].Should().Be(0x01);
    }

    [Fact]
    public void Int32Le_MemoryLayout_IsLittleEndian()
    {
        Int32Le value = 0x01020304;
        Span<byte> buffer = stackalloc byte[4];
        value.WriteTo(buffer);
        buffer[0].Should().Be(0x04, "least significant byte comes first in LE");
        buffer[3].Should().Be(0x01, "most significant byte comes last in LE");
    }

    [Fact]
    public void Int32Le_TryWriteTo_Span_Success()
    {
        Int32Le value = 0x01020304;
        Span<byte> buffer = stackalloc byte[8];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeTrue();
        buffer[0].Should().Be(0x04);
        buffer[1].Should().Be(0x03);
        buffer[2].Should().Be(0x02);
        buffer[3].Should().Be(0x01);
    }

    [Fact]
    public void Int32Le_TryWriteTo_Span_ExactSize()
    {
        Int32Le value = 0x01020304;
        Span<byte> buffer = stackalloc byte[4];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeTrue();
        buffer[0].Should().Be(0x04);
        buffer[1].Should().Be(0x03);
        buffer[2].Should().Be(0x02);
        buffer[3].Should().Be(0x01);
    }

    [Fact]
    public void Int32Le_TryWriteTo_Span_TooSmall()
    {
        Int32Le value = 0x01020304;
        Span<byte> buffer = stackalloc byte[2];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeFalse();
    }

    [Fact]
    public void Int32Le_TryWriteTo_Span_Negative()
    {
        Int32Le value = -2;
        Span<byte> buffer = stackalloc byte[4];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeTrue();
        buffer[0].Should().Be(0xFE);
        buffer[1].Should().Be(0xFF);
        buffer[2].Should().Be(0xFF);
        buffer[3].Should().Be(0xFF);
    }

    [Fact]
    public void Int32Le_Parse_Hex()
    {
        var value = Int32Le.Parse("7FFFFFFF", System.Globalization.NumberStyles.HexNumber);
        ((int)value).Should().Be(int.MaxValue);
    }

    [Fact]
    public void Int32Le_TryParse_Success()
    {
        bool result = Int32Le.TryParse("-123456", null, out var value);
        result.Should().BeTrue();
        ((int)value).Should().Be(-123456);
    }

    [Fact]
    public void Int32Le_Arithmetic_Add()
    {
        Int32Le a = -100;
        Int32Le b = 200;
        Int32Le c = a + b;
        ((int)c).Should().Be(100);
    }

    [Fact]
    public void Int32Le_Arithmetic_Negate()
    {
        Int32Le a = 12345;
        Int32Le b = -a;
        ((int)b).Should().Be(-12345);
    }

    [Fact]
    public void Int32Le_Comparison_Signed()
    {
        Int32Le positive = 100;
        Int32Le negative = -100;
        (negative < positive).Should().BeTrue();
        (positive > negative).Should().BeTrue();
    }

    [Fact]
    public void Int32Le_Increment()
    {
        Int32Le value = -1;
        value++;
        ((int)value).Should().Be(0);
    }

    [Fact]
    public void Int32Le_Decrement()
    {
        Int32Le value = 0;
        value--;
        ((int)value).Should().Be(-1);
    }

    [Fact]
    public void Int32Le_Equals_Object()
    {
        Int32Le a = -12345;
        object b = (Int32Le)(-12345);
        object c = (Int32Le)12345;
        a.Equals(b).Should().BeTrue();
        a.Equals(c).Should().BeFalse();
    }

    [Fact]
    public void Int32Le_ImplicitConversion_ToLong()
    {
        Int32Le small = -1000000;
        long wide = small;
        wide.Should().Be(-1000000L);
    }

    #endregion

    #region Int64Le Tests

    [Fact]
    public void Int64Le_Constructor_FromLong()
    {
        Int64Le value = new(-1234567890123456789L);
        ((long)value).Should().Be(-1234567890123456789L);
    }

    [Fact]
    public void Int64Le_Constructor_FromSpan()
    {
        // -1 in little-endian is all 0xFF bytes
        ReadOnlySpan<byte> bytes = stackalloc byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        var value = new Int64Le(bytes);
        ((long)value).Should().Be(-1L);
    }

    [Fact]
    public void Int64Le_Constructor_FromSpan_Negative()
    {
        // -2 in little-endian
        ReadOnlySpan<byte> bytes = stackalloc byte[] { 0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        var value = new Int64Le(bytes);
        ((long)value).Should().Be(-2L);
    }

    [Fact]
    public void Int64Le_WriteTo_Span()
    {
        Int64Le value = 0x0102030405060708L;
        Span<byte> buffer = stackalloc byte[8];
        value.WriteTo(buffer);
        buffer[0].Should().Be(0x08); // lo first
        buffer[7].Should().Be(0x01);
    }

    [Fact]
    public void Int64Le_WriteTo_Span_Negative()
    {
        Int64Le value = -1L;
        Span<byte> buffer = stackalloc byte[8];
        value.WriteTo(buffer);
        foreach (var b in buffer)
        {
            b.Should().Be(0xFF);
        }
    }

    [Fact]
    public void Int64Le_MemoryLayout_IsLittleEndian()
    {
        Int64Le value = 0x0102030405060708L;
        Span<byte> buffer = stackalloc byte[8];
        value.WriteTo(buffer);
        buffer[0].Should().Be(0x08, "least significant byte comes first in LE");
        buffer[7].Should().Be(0x01, "most significant byte comes last in LE");
    }

    [Fact]
    public void Int64Le_TryWriteTo_Span_Success()
    {
        Int64Le value = 0x0102030405060708L;
        Span<byte> buffer = stackalloc byte[16];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeTrue();
        buffer[0].Should().Be(0x08);
        buffer[7].Should().Be(0x01);
    }

    [Fact]
    public void Int64Le_TryWriteTo_Span_ExactSize()
    {
        Int64Le value = 0x0102030405060708L;
        Span<byte> buffer = stackalloc byte[8];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeTrue();
        buffer[0].Should().Be(0x08);
        buffer[1].Should().Be(0x07);
        buffer[2].Should().Be(0x06);
        buffer[3].Should().Be(0x05);
        buffer[4].Should().Be(0x04);
        buffer[5].Should().Be(0x03);
        buffer[6].Should().Be(0x02);
        buffer[7].Should().Be(0x01);
    }

    [Fact]
    public void Int64Le_TryWriteTo_Span_TooSmall()
    {
        Int64Le value = 0x0102030405060708L;
        Span<byte> buffer = stackalloc byte[4];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeFalse();
    }

    [Fact]
    public void Int64Le_TryWriteTo_Span_Negative()
    {
        Int64Le value = -2L;
        Span<byte> buffer = stackalloc byte[8];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeTrue();
        buffer[0].Should().Be(0xFE);
        buffer[1].Should().Be(0xFF);
        buffer[2].Should().Be(0xFF);
        buffer[3].Should().Be(0xFF);
        buffer[4].Should().Be(0xFF);
        buffer[5].Should().Be(0xFF);
        buffer[6].Should().Be(0xFF);
        buffer[7].Should().Be(0xFF);
    }

    [Fact]
    public void Int64Le_Arithmetic_Add()
    {
        Int64Le a = -100L;
        Int64Le b = 200L;
        Int64Le c = a + b;
        ((long)c).Should().Be(100L);
    }

    [Fact]
    public void Int64Le_Arithmetic_Negate()
    {
        Int64Le a = 12345L;
        Int64Le b = -a;
        ((long)b).Should().Be(-12345L);
    }

    [Fact]
    public void Int64Le_Comparison_Signed()
    {
        Int64Le positive = 100L;
        Int64Le negative = -100L;
        (negative < positive).Should().BeTrue();
        (positive > negative).Should().BeTrue();
    }

    [Fact]
    public void Int64Le_Parse_Negative()
    {
        var value = Int64Le.Parse("-9223372036854775808"); // long.MinValue
        ((long)value).Should().Be(long.MinValue);
    }

    [Fact]
    public void Int64Le_Parse_Hex()
    {
        var value = Int64Le.Parse("7FFFFFFFFFFFFFFF", System.Globalization.NumberStyles.HexNumber);
        ((long)value).Should().Be(long.MaxValue);
    }

    [Fact]
    public void Int64Le_TryParse_Success()
    {
        bool result = Int64Le.TryParse("-123456789", null, out var value);
        result.Should().BeTrue();
        ((long)value).Should().Be(-123456789L);
    }

    [Fact]
    public void Int64Le_TryParse_Failure()
    {
        bool result = Int64Le.TryParse("not_a_number", null, out _);
        result.Should().BeFalse();
    }

    [Fact]
    public void Int64Le_ToString_Format()
    {
        Int64Le value = -12345L;
        value.ToString("D", null).Should().Be("-12345");
    }

    [Fact]
    public void Int64Le_Increment()
    {
        Int64Le value = -1L;
        value++;
        ((long)value).Should().Be(0L);
    }

    [Fact]
    public void Int64Le_Decrement()
    {
        Int64Le value = 0L;
        value--;
        ((long)value).Should().Be(-1L);
    }

    [Fact]
    public void Int64Le_Equals_Object()
    {
        Int64Le a = -12345L;
        object b = (Int64Le)(-12345L);
        object c = (Int64Le)12345L;
        a.Equals(b).Should().BeTrue();
        a.Equals(c).Should().BeFalse();
    }

    [Fact]
    public void Int64Le_GetHashCode_Consistent()
    {
        Int64Le a = -9999999L;
        Int64Le b = -9999999L;
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public void UInt16Le_Roundtrip_WriteRead()
    {
        UInt16Le original = 0xCAFE;
        Span<byte> buffer = stackalloc byte[2];
        original.WriteTo(buffer);
        var restored = UInt16Le.ReadFrom(buffer);
        restored.Should().Be(original);
    }

    [Fact]
    public void UInt32Le_Roundtrip_WriteRead()
    {
        UInt32Le original = 0xDEADBEEF;
        Span<byte> buffer = stackalloc byte[4];
        original.WriteTo(buffer);
        var restored = UInt32Le.ReadFrom(buffer);
        restored.Should().Be(original);
    }

    [Fact]
    public void UInt64Le_Roundtrip_WriteRead()
    {
        UInt64Le original = 0x123456789ABCDEF0UL;
        Span<byte> buffer = stackalloc byte[8];
        original.WriteTo(buffer);
        var restored = UInt64Le.ReadFrom(buffer);
        restored.Should().Be(original);
    }

    [Fact]
    public void Int64Le_Roundtrip_WriteRead()
    {
        Int64Le original = -1234567890123456789L;
        Span<byte> buffer = stackalloc byte[8];
        original.WriteTo(buffer);
        var restored = Int64Le.ReadFrom(buffer);
        restored.Should().Be(original);
    }

    #endregion

    #region Endian Contrast Tests (Le vs Be produce different byte layouts)

    [Fact]
    public void UInt16_Le_Vs_Be_ByteOrder()
    {
        UInt16Le le = 0x1234;
        UInt16Be be = 0x1234;
        Span<byte> leBuf = stackalloc byte[2];
        Span<byte> beBuf = stackalloc byte[2];
        le.WriteTo(leBuf);
        be.WriteTo(beBuf);

        // Same value, reversed byte order
        leBuf[0].Should().Be(0x34); // LE: lo first
        leBuf[1].Should().Be(0x12);
        beBuf[0].Should().Be(0x12); // BE: hi first
        beBuf[1].Should().Be(0x34);
    }

    [Fact]
    public void UInt32_Le_Vs_Be_ByteOrder()
    {
        UInt32Le le = 0x12345678;
        UInt32Be be = 0x12345678;
        Span<byte> leBuf = stackalloc byte[4];
        Span<byte> beBuf = stackalloc byte[4];
        le.WriteTo(leBuf);
        be.WriteTo(beBuf);

        leBuf[0].Should().Be(0x78); // LE: lo first
        leBuf[3].Should().Be(0x12);
        beBuf[0].Should().Be(0x12); // BE: hi first
        beBuf[3].Should().Be(0x78);
    }

    [Fact]
    public void UInt64_Le_Vs_Be_ByteOrder()
    {
        UInt64Le le = 0x0102030405060708UL;
        UInt64Be be = 0x0102030405060708UL;
        Span<byte> leBuf = stackalloc byte[8];
        Span<byte> beBuf = stackalloc byte[8];
        le.WriteTo(leBuf);
        be.WriteTo(beBuf);

        leBuf[0].Should().Be(0x08); // LE: lo first
        leBuf[7].Should().Be(0x01);
        beBuf[0].Should().Be(0x01); // BE: hi first
        beBuf[7].Should().Be(0x08);
    }

    [Fact]
    public void Le_And_Be_ProduceSameNativeValue()
    {
        UInt32Le le = 0xDEADBEEF;
        UInt32Be be = 0xDEADBEEF;
        ((uint)le).Should().Be((uint)be, "same native value regardless of storage order");
    }

    [Fact]
    public void Le_ReadFrom_Be_WriteTo_CrossDecodes()
    {
        // Write as BE, read back as LE -- should get byte-swapped value
        UInt32Be be = 0x12345678;
        Span<byte> buffer = stackalloc byte[4];
        be.WriteTo(buffer);

        // Reading BE bytes as LE interprets them reversed
        var le = UInt32Le.ReadFrom(buffer);
        ((uint)le).Should().Be(0x78563412U);
    }

    #endregion
}
