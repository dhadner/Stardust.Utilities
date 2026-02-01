using FluentAssertions;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Tests for the Big-Endian integer types.
/// </summary>
public class BigEndianTests
{
    #region UInt16Be Tests

    [Fact]
    public void UInt16Be_Constructor_FromUshort()
    {
        UInt16Be value = new(0x1234);
        ((ushort)value).Should().Be(0x1234);
    }

    [Fact]
    public void UInt16Be_Constructor_FromSpan()
    {
        ReadOnlySpan<byte> bytes = stackalloc byte[] { 0x12, 0x34 };
        var value = new UInt16Be(bytes);
        ((ushort)value).Should().Be(0x1234);
    }

    [Fact]
    public void UInt16Be_WriteTo_Span()
    {
        UInt16Be value = 0x1234;
        Span<byte> buffer = stackalloc byte[2];
        value.WriteTo(buffer);
        buffer[0].Should().Be(0x12);
        buffer[1].Should().Be(0x34);
    }

    [Fact]
    public void UInt16Be_TryWriteTo_Span_Success()
    {
        UInt16Be value = 0xABCD;
        Span<byte> buffer = stackalloc byte[4];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeTrue();
        buffer[0].Should().Be(0xAB);
        buffer[1].Should().Be(0xCD);
    }

    [Fact]
    public void UInt16Be_TryWriteTo_Span_TooSmall()
    {
        UInt16Be value = 0xABCD;
        Span<byte> buffer = stackalloc byte[1];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeFalse();
    }

    [Fact]
    public void UInt16Be_ReadFrom_Span()
    {
        ReadOnlySpan<byte> bytes = stackalloc byte[] { 0xAB, 0xCD };
        var value = UInt16Be.ReadFrom(bytes);
        ((ushort)value).Should().Be(0xABCD);
    }

    [Fact]
    public void UInt16Be_Parse_IFormatProvider()
    {
        var value = UInt16Be.Parse("1234", null);
        ((ushort)value).Should().Be(1234);
    }

    [Fact]
    public void UInt16Be_TryParse_String_Success()
    {
        bool result = UInt16Be.TryParse("5678", null, out var value);
        result.Should().BeTrue();
        ((ushort)value).Should().Be(5678);
    }

    [Fact]
    public void UInt16Be_TryParse_String_Failure()
    {
        bool result = UInt16Be.TryParse("invalid", null, out var value);
        result.Should().BeFalse();
    }

    [Fact]
    public void UInt16Be_TryParse_Span_Success()
    {
        bool result = UInt16Be.TryParse("9999".AsSpan(), null, out var value);
        result.Should().BeTrue();
        ((ushort)value).Should().Be(9999);
    }

    [Fact]
    public void UInt16Be_TryFormat_Success()
    {
        UInt16Be value = 0x00FF;
        Span<char> buffer = stackalloc char[10];
        bool result = value.TryFormat(buffer, out int charsWritten, "X4", null);
        result.Should().BeTrue();
        new string(buffer[..charsWritten]).Should().Be("00FF");
    }

    [Fact]
    public void UInt16Be_ToString_Format()
    {
        UInt16Be value = 0x1234;
        value.ToString("X4", null).Should().Be("1234");
        value.ToString("D", null).Should().Be("4660");
    }

    #endregion

    #region UInt32Be Tests

    [Fact]
    public void UInt32Be_Constructor_FromSpan()
    {
        ReadOnlySpan<byte> bytes = stackalloc byte[] { 0x12, 0x34, 0x56, 0x78 };
        var value = new UInt32Be(bytes);
        ((uint)value).Should().Be(0x12345678);
    }

    [Fact]
    public void UInt32Be_WriteTo_Span()
    {
        UInt32Be value = 0x12345678;
        Span<byte> buffer = stackalloc byte[4];
        value.WriteTo(buffer);
        buffer[0].Should().Be(0x12);
        buffer[1].Should().Be(0x34);
        buffer[2].Should().Be(0x56);
        buffer[3].Should().Be(0x78);
    }

    [Fact]
    public void UInt32Be_TryWriteTo_Success()
    {
        UInt32Be value = 0xAABBCCDD;
        Span<byte> buffer = stackalloc byte[8];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeTrue();
        buffer[0].Should().Be(0xAA);
        buffer[3].Should().Be(0xDD);
    }

    [Fact]
    public void UInt32Be_TryFormat()
    {
        UInt32Be value = 0xDEADBEEF;
        Span<char> buffer = stackalloc char[16];
        bool result = value.TryFormat(buffer, out int charsWritten, "X8", null);
        result.Should().BeTrue();
        new string(buffer[..charsWritten]).Should().Be("DEADBEEF");
    }

    [Fact]
    public void UInt32Be_Parse_IParsable()
    {
        var value = UInt32Be.Parse("12345678".AsSpan(), null);
        ((uint)value).Should().Be(12345678);
    }

    #endregion

    #region Int16Be Tests

    [Fact]
    public void Int16Be_Constructor_FromSpan()
    {
        ReadOnlySpan<byte> bytes = stackalloc byte[] { 0xFF, 0xFE }; // -2 in big-endian
        var value = new Int16Be(bytes);
        ((short)value).Should().Be(-2);
    }

    [Fact]
    public void Int16Be_WriteTo_Span()
    {
        Int16Be value = -1;
        Span<byte> buffer = stackalloc byte[2];
        value.WriteTo(buffer);
        buffer[0].Should().Be(0xFF);
        buffer[1].Should().Be(0xFF);
    }

    [Fact]
    public void Int16Be_TryParse()
    {
        bool result = Int16Be.TryParse("-100", null, out var value);
        result.Should().BeTrue();
        ((short)value).Should().Be(-100);
    }

    #endregion

    #region Int32Be Tests

    [Fact]
    public void Int32Be_Constructor_FromSpan()
    {
        ReadOnlySpan<byte> bytes = stackalloc byte[] { 0xFF, 0xFF, 0xFF, 0xFE }; // -2 in big-endian
        var value = new Int32Be(bytes);
        ((int)value).Should().Be(-2);
    }

    [Fact]
    public void Int32Be_WriteTo_Span()
    {
        Int32Be value = -1;
        Span<byte> buffer = stackalloc byte[4];
        value.WriteTo(buffer);
        buffer[0].Should().Be(0xFF);
        buffer[1].Should().Be(0xFF);
        buffer[2].Should().Be(0xFF);
        buffer[3].Should().Be(0xFF);
    }

    [Fact]
    public void Int32Be_SignExtension_FromInt16Be()
    {
        Int16Be small = -100;
        Int32Be large = (Int32Be)small;
        ((int)large).Should().Be(-100);
    }

    #endregion

    #region UInt64Be Tests

    [Fact]
    public void UInt64Be_Constructor_FromUlong()
    {
        UInt64Be value = new(0x123456789ABCDEF0UL);
        ((ulong)value).Should().Be(0x123456789ABCDEF0UL);
    }

    [Fact]
    public void UInt64Be_Constructor_FromSpan()
    {
        ReadOnlySpan<byte> bytes = stackalloc byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 };
        var value = new UInt64Be(bytes);
        ((ulong)value).Should().Be(0x123456789ABCDEF0UL);
    }

    [Fact]
    public void UInt64Be_WriteTo_Span()
    {
        UInt64Be value = 0x0102030405060708UL;
        Span<byte> buffer = stackalloc byte[8];
        value.WriteTo(buffer);
        buffer[0].Should().Be(0x01);
        buffer[1].Should().Be(0x02);
        buffer[2].Should().Be(0x03);
        buffer[3].Should().Be(0x04);
        buffer[4].Should().Be(0x05);
        buffer[5].Should().Be(0x06);
        buffer[6].Should().Be(0x07);
        buffer[7].Should().Be(0x08);
    }

    [Fact]
    public void UInt64Be_TryWriteTo_Success()
    {
        UInt64Be value = 0xAABBCCDDEEFF0011UL;
        Span<byte> buffer = stackalloc byte[16];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeTrue();
        buffer[0].Should().Be(0xAA);
        buffer[7].Should().Be(0x11);
    }

    [Fact]
    public void UInt64Be_TryWriteTo_TooSmall()
    {
        UInt64Be value = 0x1234UL;
        Span<byte> buffer = stackalloc byte[4];
        bool result = value.TryWriteTo(buffer);
        result.Should().BeFalse();
    }

    [Fact]
    public void UInt64Be_ReadFrom_Span()
    {
        ReadOnlySpan<byte> bytes = stackalloc byte[] { 0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10 };
        var value = UInt64Be.ReadFrom(bytes);
        ((ulong)value).Should().Be(0xFEDCBA9876543210UL);
    }

    [Fact]
    public void UInt64Be_Arithmetic_Add()
    {
        UInt64Be a = 100UL;
        UInt64Be b = 200UL;
        UInt64Be c = a + b;
        ((ulong)c).Should().Be(300UL);
    }

    [Fact]
    public void UInt64Be_Arithmetic_Subtract()
    {
        UInt64Be a = 500UL;
        UInt64Be b = 200UL;
        UInt64Be c = a - b;
        ((ulong)c).Should().Be(300UL);
    }

    [Fact]
    public void UInt64Be_Arithmetic_Multiply()
    {
        UInt64Be a = 100UL;
        UInt64Be b = 200UL;
        UInt64Be c = a * b;
        ((ulong)c).Should().Be(20000UL);
    }

    [Fact]
    public void UInt64Be_Arithmetic_Divide()
    {
        UInt64Be a = 1000UL;
        UInt64Be b = 10UL;
        UInt64Be c = a / b;
        ((ulong)c).Should().Be(100UL);
    }

    [Fact]
    public void UInt64Be_Bitwise_And()
    {
        UInt64Be a = 0xFF00FF00FF00FF00UL;
        UInt64Be b = 0x00FF00FF00FF00FFUL;
        UInt64Be c = a & b;
        ((ulong)c).Should().Be(0UL);
    }

    [Fact]
    public void UInt64Be_Bitwise_Or()
    {
        UInt64Be a = 0xFF00FF00FF00FF00UL;
        UInt64Be b = 0x00FF00FF00FF00FFUL;
        UInt64Be c = a | b;
        ((ulong)c).Should().Be(0xFFFFFFFFFFFFFFFFUL);
    }

    [Fact]
    public void UInt64Be_Bitwise_Xor()
    {
        UInt64Be a = 0x123456789ABCDEF0UL;
        UInt64Be b = 0x123456789ABCDEF0UL;
        UInt64Be c = a ^ b;
        ((ulong)c).Should().Be(0UL);
    }

    [Fact]
    public void UInt64Be_Bitwise_Not()
    {
        UInt64Be a = 0UL;
        UInt64Be b = ~a;
        ((ulong)b).Should().Be(0xFFFFFFFFFFFFFFFFUL);
    }

    [Fact]
    public void UInt64Be_Shift_Left()
    {
        UInt64Be a = 1UL;
        UInt64Be b = a << 63;
        ((ulong)b).Should().Be(0x8000000000000000UL);
    }

    [Fact]
    public void UInt64Be_Shift_Right()
    {
        UInt64Be a = 0x8000000000000000UL;
        UInt64Be b = a >> 63;
        ((ulong)b).Should().Be(1UL);
    }

    [Fact]
    public void UInt64Be_Comparison_LessThan()
    {
        UInt64Be a = 100UL;
        UInt64Be b = 200UL;
        (a < b).Should().BeTrue();
        (b < a).Should().BeFalse();
    }

    [Fact]
    public void UInt64Be_Comparison_GreaterThan()
    {
        UInt64Be a = 200UL;
        UInt64Be b = 100UL;
        (a > b).Should().BeTrue();
        (b > a).Should().BeFalse();
    }

    [Fact]
    public void UInt64Be_Comparison_Equality()
    {
        UInt64Be a = 12345UL;
        UInt64Be b = 12345UL;
        UInt64Be c = 54321UL;
        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
    }

    [Fact]
    public void UInt64Be_Parse_Decimal()
    {
        var value = UInt64Be.Parse("18446744073709551615"); // Max ulong
        ((ulong)value).Should().Be(ulong.MaxValue);
    }

    [Fact]
    public void UInt64Be_Parse_Hex()
    {
        var value = UInt64Be.Parse("FFFFFFFFFFFFFFFF", System.Globalization.NumberStyles.HexNumber);
        ((ulong)value).Should().Be(ulong.MaxValue);
    }

    [Fact]
    public void UInt64Be_TryParse_String_Success()
    {
        bool result = UInt64Be.TryParse("1234567890123456789", null, out var value);
        result.Should().BeTrue();
        ((ulong)value).Should().Be(1234567890123456789UL);
    }

    [Fact]
    public void UInt64Be_TryParse_Span_Success()
    {
        bool result = UInt64Be.TryParse("9876543210".AsSpan(), null, out var value);
        result.Should().BeTrue();
        ((ulong)value).Should().Be(9876543210UL);
    }

    [Fact]
    public void UInt64Be_TryFormat()
    {
        UInt64Be value = 0xDEADBEEFCAFEBABEUL;
        Span<char> buffer = stackalloc char[20];
        bool result = value.TryFormat(buffer, out int charsWritten, "X16", null);
        result.Should().BeTrue();
        new string(buffer[..charsWritten]).Should().Be("DEADBEEFCAFEBABE");
    }

    [Fact]
    public void UInt64Be_ToString_Default()
    {
        UInt64Be value = 0x1234567890ABCDEFUL;
        value.ToString().Should().Be("0x1234567890abcdef");
    }

    [Fact]
    public void UInt64Be_Equals_Object()
    {
        UInt64Be a = 12345UL;
        object b = (UInt64Be)12345UL;
        object c = (UInt64Be)54321UL;
        a.Equals(b).Should().BeTrue();
        a.Equals(c).Should().BeFalse();
    }

    [Fact]
    public void UInt64Be_GetHashCode_Consistent()
    {
        UInt64Be a = 0x123456789ABCDEF0UL;
        UInt64Be b = 0x123456789ABCDEF0UL;
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void UInt64Be_Increment()
    {
        UInt64Be value = 99UL;
        value++;
        ((ulong)value).Should().Be(100UL);
    }

    [Fact]
    public void UInt64Be_Decrement()
    {
        UInt64Be value = 100UL;
        value--;
        ((ulong)value).Should().Be(99UL);
    }

    [Fact]
    public void UInt64Be_ImplicitConversion_FromUInt32Be()
    {
        UInt32Be small = 0x12345678U;
        UInt64Be large = small;
        ((ulong)large).Should().Be(0x12345678UL);
    }

    [Fact]
    public void UInt64Be_ExplicitConversion_ToUInt32Be()
    {
        UInt64Be large = 0x123456789ABCDEF0UL;
        UInt32Be small = (UInt32Be)large;
        ((uint)small).Should().Be(0x9ABCDEF0U);
    }

    #endregion

    #region Int64Be Tests

    [Fact]
    public void Int64Be_Constructor_FromLong()
    {
        Int64Be value = new(-1234567890123456789L);
        ((long)value).Should().Be(-1234567890123456789L);
    }

    [Fact]
    public void Int64Be_Constructor_FromSpan()
    {
        // -1 in big-endian is all 0xFF bytes
        ReadOnlySpan<byte> bytes = stackalloc byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        var value = new Int64Be(bytes);
        ((long)value).Should().Be(-1L);
    }

    [Fact]
    public void Int64Be_Constructor_FromSpan_Negative()
    {
        // -2 in big-endian
        ReadOnlySpan<byte> bytes = stackalloc byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFE };
        var value = new Int64Be(bytes);
        ((long)value).Should().Be(-2L);
    }

    [Fact]
    public void Int64Be_WriteTo_Span()
    {
        Int64Be value = 0x0102030405060708L;
        Span<byte> buffer = stackalloc byte[8];
        value.WriteTo(buffer);
        buffer[0].Should().Be(0x01);
        buffer[7].Should().Be(0x08);
    }

    [Fact]
    public void Int64Be_WriteTo_Span_Negative()
    {
        Int64Be value = -1L;
        Span<byte> buffer = stackalloc byte[8];
        value.WriteTo(buffer);
        foreach (var b in buffer)
        {
            b.Should().Be(0xFF);
        }
    }

    [Fact]
    public void Int64Be_Arithmetic_Add()
    {
        Int64Be a = -100L;
        Int64Be b = 200L;
        Int64Be c = a + b;
        ((long)c).Should().Be(100L);
    }

    [Fact]
    public void Int64Be_Arithmetic_Negate()
    {
        Int64Be a = 12345L;
        Int64Be b = -a;
        ((long)b).Should().Be(-12345L);
    }

    [Fact]
    public void Int64Be_Comparison_Signed()
    {
        Int64Be positive = 100L;
        Int64Be negative = -100L;
        (negative < positive).Should().BeTrue();
        (positive > negative).Should().BeTrue();
    }

    [Fact]
    public void Int64Be_SignExtension_FromInt32Be()
    {
        Int32Be small = -1000;
        Int64Be large = small;
        ((long)large).Should().Be(-1000L);
    }

    [Fact]
    public void Int64Be_Parse_Negative()
    {
        var value = Int64Be.Parse("-9223372036854775808"); // long.MinValue
        ((long)value).Should().Be(long.MinValue);
    }

    [Fact]
    public void Int64Be_TryParse_Success()
    {
        bool result = Int64Be.TryParse("-123456789", null, out var value);
        result.Should().BeTrue();
        ((long)value).Should().Be(-123456789L);
    }

    [Fact]
    public void Int64Be_ToString_Format()
    {
        Int64Be value = -12345L;
        value.ToString("D", null).Should().Be("-12345");
    }

    [Fact]
    public void Int64Be_ShiftRight_SignExtends()
    {
        Int64Be negative = -8L;
        Int64Be shifted = negative >> 2;
        ((long)shifted).Should().Be(-2L);
    }

    #endregion

    #region Cross-Type Conversion Tests

    [Fact]
    public void Conversion_UInt16Be_To_UInt64Be()
    {
        UInt16Be small = 0xABCD;
        UInt64Be large = small;
        ((ulong)large).Should().Be(0xABCDUL);
    }

    [Fact]
    public void Conversion_UInt32Be_To_UInt64Be()
    {
        UInt32Be small = 0x12345678U;
        UInt64Be large = small;
        ((ulong)large).Should().Be(0x12345678UL);
    }

    [Fact]
    public void Conversion_Int16Be_To_Int64Be_Positive()
    {
        Int16Be small = 1000;
        Int64Be large = small;
        ((long)large).Should().Be(1000L);
    }

    [Fact]
    public void Conversion_Int16Be_To_Int64Be_Negative()
    {
        Int16Be small = -1000;
        Int64Be large = small;
        ((long)large).Should().Be(-1000L);
    }

    [Fact]
    public void Conversion_Int32Be_To_Int64Be_Negative()
    {
        Int32Be small = -1000000;
        Int64Be large = small;
        ((long)large).Should().Be(-1000000L);
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public void UInt64Be_Roundtrip_WriteRead()
    {
        UInt64Be original = 0x123456789ABCDEF0UL;
        Span<byte> buffer = stackalloc byte[8];
        original.WriteTo(buffer);
        var restored = UInt64Be.ReadFrom(buffer);
        restored.Should().Be(original);
    }

    [Fact]
    public void Int64Be_Roundtrip_WriteRead()
    {
        Int64Be original = -1234567890123456789L;
        Span<byte> buffer = stackalloc byte[8];
        original.WriteTo(buffer);
        var restored = Int64Be.ReadFrom(buffer);
        restored.Should().Be(original);
    }

    [Fact]
    public void UInt32Be_Roundtrip_WriteRead()
    {
        UInt32Be original = 0xDEADBEEF;
        Span<byte> buffer = stackalloc byte[4];
        original.WriteTo(buffer);
        var restored = UInt32Be.ReadFrom(buffer);
        restored.Should().Be(original);
    }

    [Fact]
    public void UInt16Be_Roundtrip_WriteRead()
    {
        UInt16Be original = 0xCAFE;
        Span<byte> buffer = stackalloc byte[2];
        original.WriteTo(buffer);
        var restored = UInt16Be.ReadFrom(buffer);
        restored.Should().Be(original);
    }

    #endregion

    #region Hi/Lo Extension Method Tests

    [Fact]
    public void UInt64Be_Hi_ReturnsUpperUInt32Be()
    {
        UInt64Be value = 0x123456789ABCDEF0UL;
        UInt32Be hi = value.Hi();
        ((uint)hi).Should().Be(0x12345678U);
    }

    [Fact]
    public void UInt64Be_Lo_ReturnsLowerUInt32Be()
    {
        UInt64Be value = 0x123456789ABCDEF0UL;
        UInt32Be lo = value.Lo();
        ((uint)lo).Should().Be(0x9ABCDEF0U);
    }

    [Fact]
    public void Int64Be_Hi_ReturnsUpperUInt32Be()
    {
        Int64Be value = 0x123456789ABCDEF0L;
        UInt32Be hi = value.Hi();
        ((uint)hi).Should().Be(0x12345678U);
    }

    [Fact]
    public void Int64Be_Lo_ReturnsLowerUInt32Be()
    {
        Int64Be value = 0x123456789ABCDEF0L;
        UInt32Be lo = value.Lo();
        ((uint)lo).Should().Be(0x9ABCDEF0U);
    }

    [Fact]
    public void Ulong_Hi_ReturnsUpperUint()
    {
        ulong value = 0x123456789ABCDEF0UL;
        uint hi = value.Hi();
        hi.Should().Be(0x12345678U);
    }

    [Fact]
    public void Ulong_Lo_ReturnsLowerUint()
    {
        ulong value = 0x123456789ABCDEF0UL;
        uint lo = value.Lo();
        lo.Should().Be(0x9ABCDEF0U);
    }

    [Fact]
    public void Long_Hi_ReturnsUpperUint()
    {
        long value = 0x123456789ABCDEF0L;
        uint hi = value.Hi();
        hi.Should().Be(0x12345678U);
    }

    [Fact]
    public void Long_Lo_ReturnsLowerUint()
    {
        long value = 0x123456789ABCDEF0L;
        uint lo = value.Lo();
        lo.Should().Be(0x9ABCDEF0U);
    }

    [Fact]
    public void UInt64Be_SetHi_SetsUpperUInt32Be()
    {
        UInt64Be value = 0x123456789ABCDEF0UL;
        UInt64Be result = value.SetHi((UInt32Be)0xDEADBEEFU);
        ((ulong)result).Should().Be(0xDEADBEEF9ABCDEF0UL);
    }

    [Fact]
    public void UInt64Be_SetLo_SetsLowerUInt32Be()
    {
        UInt64Be value = 0x123456789ABCDEF0UL;
        UInt64Be result = value.SetLo((UInt32Be)0xCAFEBABEU);
        ((ulong)result).Should().Be(0x12345678CAFEBABEU);
    }

    [Fact]
    public void Int64Be_SetHi_SetsUpperUInt32Be()
    {
        Int64Be value = 0x123456789ABCDEF0L;
        Int64Be result = value.SetHi((UInt32Be)0x00000001U);
        ((long)result).Should().Be(0x000000019ABCDEF0L);
    }

    [Fact]
    public void Int64Be_SetLo_SetsLowerUInt32Be()
    {
        Int64Be value = 0x123456789ABCDEF0L;
        Int64Be result = value.SetLo((UInt32Be)0x00000001U);
        ((long)result).Should().Be(0x1234567800000001L);
    }

    [Fact]
    public void Ulong_SetHi_SetsUpperUint()
    {
        ulong value = 0x123456789ABCDEF0UL;
        ulong result = value.SetHi(0xDEADBEEFU);
        result.Should().Be(0xDEADBEEF9ABCDEF0UL);
    }

    [Fact]
    public void Ulong_SetLo_SetsLowerUint()
    {
        ulong value = 0x123456789ABCDEF0UL;
        ulong result = value.SetLo(0xCAFEBABEU);
        result.Should().Be(0x12345678CAFEBABEU);
    }

    [Fact]
    public void Long_SetHi_SetsUpperUint()
    {
        long value = 0x123456789ABCDEF0L;
        long result = value.SetHi(0x00000001U);
        result.Should().Be(0x000000019ABCDEF0L);
    }

    [Fact]
    public void Long_SetLo_SetsLowerUint()
    {
        long value = 0x123456789ABCDEF0L;
        long result = value.SetLo(0x00000001U);
        result.Should().Be(0x1234567800000001L);
    }

    #endregion
}
