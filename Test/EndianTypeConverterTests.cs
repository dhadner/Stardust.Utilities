using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Linq;
using FluentAssertions;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Unit tests for TypeConverters on all endian types.
/// Verifies PropertyGrid-style string-to-value and value-to-string conversion,
/// including decimal input, hex input with 0x prefix, binary input with 0b prefix,
/// digit separators, whitespace, culture handling, round-tripping, and conversion
/// from the native primitive integer type.
/// </summary>
public class EndianTypeConverterTests
{
    // ===================================================================
    // UInt16Be
    // ===================================================================

    [Fact]
    public void UInt16Be_TypeConverterAttribute_IsWired()
    {
        var attr = TypeDescriptor.GetConverter(typeof(UInt16Be));
        attr.Should().BeOfType<UInt16BeTypeConverter>();
    }

    [Fact]
    public void UInt16Be_ConvertFrom_DecimalString()
    {
        var converter = new UInt16BeTypeConverter();
        var result = converter.ConvertFrom(null, null, "4660");
        result.Should().BeOfType<UInt16Be>();
        ((ushort)(UInt16Be)result!).Should().Be(0x1234);
    }

    [Fact]
    public void UInt16Be_ConvertFrom_HexString()
    {
        var converter = new UInt16BeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0xABCD");
        ((ushort)(UInt16Be)result!).Should().Be(0xABCD);
    }

    [Fact]
    public void UInt16Be_ConvertTo_String()
    {
        var converter = new UInt16BeTypeConverter();
        UInt16Be value = 0x00FF;
        var result = converter.ConvertTo(null, null, value, typeof(string));
        result.Should().Be("0x00ff");
    }

    [Fact]
    public void UInt16Be_RoundTrip()
    {
        var converter = new UInt16BeTypeConverter();
        UInt16Be original = 0x1234;
        var str = (string)converter.ConvertTo(null, null, original, typeof(string))!;
        var restored = (UInt16Be)converter.ConvertFrom(null, null, str)!;
        ((ushort)restored).Should().Be((ushort)original);
    }

    // ===================================================================
    // UInt32Be
    // ===================================================================

    [Fact]
    public void UInt32Be_TypeConverterAttribute_IsWired()
    {
        TypeDescriptor.GetConverter(typeof(UInt32Be)).Should().BeOfType<UInt32BeTypeConverter>();
    }

    [Fact]
    public void UInt32Be_ConvertFrom_DecimalString()
    {
        var converter = new UInt32BeTypeConverter();
        var result = converter.ConvertFrom(null, null, "305419896");
        ((uint)(UInt32Be)result!).Should().Be(0x12345678U);
    }

    [Fact]
    public void UInt32Be_ConvertFrom_HexString()
    {
        var converter = new UInt32BeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0xDEADBEEF");
        ((uint)(UInt32Be)result!).Should().Be(0xDEADBEEFU);
    }

    [Fact]
    public void UInt32Be_ConvertTo_String()
    {
        var converter = new UInt32BeTypeConverter();
        UInt32Be value = 0x0000FFFF;
        var result = converter.ConvertTo(null, null, value, typeof(string));
        result.Should().Be("0x0000ffff");
    }

    [Fact]
    public void UInt32Be_RoundTrip()
    {
        var converter = new UInt32BeTypeConverter();
        UInt32Be original = 0xCAFEBABE;
        var str = (string)converter.ConvertTo(null, null, original, typeof(string))!;
        var restored = (UInt32Be)converter.ConvertFrom(null, null, str)!;
        ((uint)restored).Should().Be((uint)original);
    }

    // ===================================================================
    // UInt64Be
    // ===================================================================

    [Fact]
    public void UInt64Be_TypeConverterAttribute_IsWired()
    {
        TypeDescriptor.GetConverter(typeof(UInt64Be)).Should().BeOfType<UInt64BeTypeConverter>();
    }

    [Fact]
    public void UInt64Be_ConvertFrom_HexString()
    {
        var converter = new UInt64BeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0x0123456789ABCDEF");
        ((ulong)(UInt64Be)result!).Should().Be(0x0123456789ABCDEFUL);
    }

    [Fact]
    public void UInt64Be_ConvertTo_String()
    {
        var converter = new UInt64BeTypeConverter();
        UInt64Be value = 0x00000000DEADBEEF;
        var result = converter.ConvertTo(null, null, value, typeof(string));
        result.Should().Be("0x00000000deadbeef");
    }

    // ===================================================================
    // Int16Be
    // ===================================================================

    [Fact]
    public void Int16Be_TypeConverterAttribute_IsWired()
    {
        TypeDescriptor.GetConverter(typeof(Int16Be)).Should().BeOfType<Int16BeTypeConverter>();
    }

    [Fact]
    public void Int16Be_ConvertFrom_DecimalString()
    {
        var converter = new Int16BeTypeConverter();
        var result = converter.ConvertFrom(null, null, "256");
        ((short)(Int16Be)result!).Should().Be(256);
    }

    [Fact]
    public void Int16Be_ConvertFrom_HexString()
    {
        var converter = new Int16BeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0x7FFF");
        ((short)(Int16Be)result!).Should().Be(short.MaxValue);
    }

    [Fact]
    public void Int16Be_ConvertTo_String()
    {
        var converter = new Int16BeTypeConverter();
        Int16Be value = 0x00FF;
        var result = converter.ConvertTo(null, null, value, typeof(string));
        result.Should().Be("0x00ff");
    }

    [Fact]
    public void Int16Be_NegativeHex_RoundTrip()
    {
        // Two's-complement hex representation of -1 (16-bit) is 0xffff.
        var converter = new Int16BeTypeConverter();
        Int16Be original = (Int16Be)(short)-1;
        var str = (string)converter.ConvertTo(null, null, original, typeof(string))!;
        str.Should().Be("0xffff");
        var restored = (Int16Be)converter.ConvertFrom(null, null, str)!;
        ((short)restored).Should().Be(-1);
    }

    [Fact]
    public void Int16Be_NegativeDecimal_Parses()
    {
        var converter = new Int16BeTypeConverter();
        var result = converter.ConvertFrom(null, null, "-1");
        ((short)(Int16Be)result!).Should().Be(-1);
    }

    // ===================================================================
    // Int32Be
    // ===================================================================

    [Fact]
    public void Int32Be_TypeConverterAttribute_IsWired()
    {
        TypeDescriptor.GetConverter(typeof(Int32Be)).Should().BeOfType<Int32BeTypeConverter>();
    }

    [Fact]
    public void Int32Be_ConvertFrom_DecimalString()
    {
        var converter = new Int32BeTypeConverter();
        var result = converter.ConvertFrom(null, null, "12345");
        ((int)(Int32Be)result!).Should().Be(12345);
    }

    [Fact]
    public void Int32Be_ConvertFrom_HexString()
    {
        var converter = new Int32BeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0x7FFFFFFF");
        ((int)(Int32Be)result!).Should().Be(int.MaxValue);
    }

    [Fact]
    public void Int32Be_ConvertTo_String()
    {
        var converter = new Int32BeTypeConverter();
        Int32Be value = 0x000000FF;
        var result = converter.ConvertTo(null, null, value, typeof(string));
        result.Should().Be("0x000000ff");
    }

    // ===================================================================
    // Int64Be
    // ===================================================================

    [Fact]
    public void Int64Be_TypeConverterAttribute_IsWired()
    {
        TypeDescriptor.GetConverter(typeof(Int64Be)).Should().BeOfType<Int64BeTypeConverter>();
    }

    [Fact]
    public void Int64Be_ConvertFrom_HexString()
    {
        var converter = new Int64BeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0x7FFFFFFFFFFFFFFF");
        ((long)(Int64Be)result!).Should().Be(long.MaxValue);
    }

    [Fact]
    public void Int64Be_ConvertTo_String()
    {
        var converter = new Int64BeTypeConverter();
        Int64Be value = 1L;
        var result = converter.ConvertTo(null, null, value, typeof(string));
        result.Should().Be("0x0000000000000001");
    }

    // ===================================================================
    // UInt16Le
    // ===================================================================

    [Fact]
    public void UInt16Le_TypeConverterAttribute_IsWired()
    {
        TypeDescriptor.GetConverter(typeof(UInt16Le)).Should().BeOfType<UInt16LeTypeConverter>();
    }

    [Fact]
    public void UInt16Le_ConvertFrom_DecimalString()
    {
        var converter = new UInt16LeTypeConverter();
        var result = converter.ConvertFrom(null, null, "4660");
        ((ushort)(UInt16Le)result!).Should().Be(0x1234);
    }

    [Fact]
    public void UInt16Le_ConvertFrom_HexString()
    {
        var converter = new UInt16LeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0xABCD");
        ((ushort)(UInt16Le)result!).Should().Be(0xABCD);
    }

    [Fact]
    public void UInt16Le_ConvertTo_String()
    {
        var converter = new UInt16LeTypeConverter();
        UInt16Le value = 0x00FF;
        var result = converter.ConvertTo(null, null, value, typeof(string));
        result.Should().Be("0x00ff");
    }

    [Fact]
    public void UInt16Le_RoundTrip()
    {
        var converter = new UInt16LeTypeConverter();
        UInt16Le original = 0x1234;
        var str = (string)converter.ConvertTo(null, null, original, typeof(string))!;
        var restored = (UInt16Le)converter.ConvertFrom(null, null, str)!;
        ((ushort)restored).Should().Be((ushort)original);
    }

    // ===================================================================
    // UInt32Le
    // ===================================================================

    [Fact]
    public void UInt32Le_TypeConverterAttribute_IsWired()
    {
        TypeDescriptor.GetConverter(typeof(UInt32Le)).Should().BeOfType<UInt32LeTypeConverter>();
    }

    [Fact]
    public void UInt32Le_ConvertFrom_DecimalString()
    {
        var converter = new UInt32LeTypeConverter();
        var result = converter.ConvertFrom(null, null, "305419896");
        ((uint)(UInt32Le)result!).Should().Be(0x12345678U);
    }

    [Fact]
    public void UInt32Le_ConvertFrom_HexString()
    {
        var converter = new UInt32LeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0xDEADBEEF");
        ((uint)(UInt32Le)result!).Should().Be(0xDEADBEEFU);
    }

    [Fact]
    public void UInt32Le_ConvertTo_String()
    {
        var converter = new UInt32LeTypeConverter();
        UInt32Le value = 0x0000FFFF;
        var result = converter.ConvertTo(null, null, value, typeof(string));
        result.Should().Be("0x0000ffff");
    }

    [Fact]
    public void UInt32Le_RoundTrip()
    {
        var converter = new UInt32LeTypeConverter();
        UInt32Le original = 0xCAFEBABE;
        var str = (string)converter.ConvertTo(null, null, original, typeof(string))!;
        var restored = (UInt32Le)converter.ConvertFrom(null, null, str)!;
        ((uint)restored).Should().Be((uint)original);
    }

    // ===================================================================
    // UInt64Le
    // ===================================================================

    [Fact]
    public void UInt64Le_TypeConverterAttribute_IsWired()
    {
        TypeDescriptor.GetConverter(typeof(UInt64Le)).Should().BeOfType<UInt64LeTypeConverter>();
    }

    [Fact]
    public void UInt64Le_ConvertFrom_HexString()
    {
        var converter = new UInt64LeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0x0123456789ABCDEF");
        ((ulong)(UInt64Le)result!).Should().Be(0x0123456789ABCDEFUL);
    }

    [Fact]
    public void UInt64Le_ConvertTo_String()
    {
        var converter = new UInt64LeTypeConverter();
        UInt64Le value = 0x00000000DEADBEEF;
        var result = converter.ConvertTo(null, null, value, typeof(string));
        result.Should().Be("0x00000000deadbeef");
    }

    // ===================================================================
    // Int16Le
    // ===================================================================

    [Fact]
    public void Int16Le_TypeConverterAttribute_IsWired()
    {
        TypeDescriptor.GetConverter(typeof(Int16Le)).Should().BeOfType<Int16LeTypeConverter>();
    }

    [Fact]
    public void Int16Le_ConvertFrom_DecimalString()
    {
        var converter = new Int16LeTypeConverter();
        var result = converter.ConvertFrom(null, null, "256");
        ((short)(Int16Le)result!).Should().Be(256);
    }

    [Fact]
    public void Int16Le_ConvertFrom_HexString()
    {
        var converter = new Int16LeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0x7FFF");
        ((short)(Int16Le)result!).Should().Be(short.MaxValue);
    }

    [Fact]
    public void Int16Le_ConvertTo_String()
    {
        var converter = new Int16LeTypeConverter();
        Int16Le value = 0x00FF;
        var result = converter.ConvertTo(null, null, value, typeof(string));
        result.Should().Be("0x00ff");
    }

    // ===================================================================
    // Int32Le
    // ===================================================================

    [Fact]
    public void Int32Le_TypeConverterAttribute_IsWired()
    {
        TypeDescriptor.GetConverter(typeof(Int32Le)).Should().BeOfType<Int32LeTypeConverter>();
    }

    [Fact]
    public void Int32Le_ConvertFrom_DecimalString()
    {
        var converter = new Int32LeTypeConverter();
        var result = converter.ConvertFrom(null, null, "12345");
        ((int)(Int32Le)result!).Should().Be(12345);
    }

    [Fact]
    public void Int32Le_ConvertFrom_HexString()
    {
        var converter = new Int32LeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0x7FFFFFFF");
        ((int)(Int32Le)result!).Should().Be(int.MaxValue);
    }

    [Fact]
    public void Int32Le_ConvertTo_String()
    {
        var converter = new Int32LeTypeConverter();
        Int32Le value = 0x000000FF;
        var result = converter.ConvertTo(null, null, value, typeof(string));
        result.Should().Be("0x000000ff");
    }

    // ===================================================================
    // Int64Le
    // ===================================================================

    [Fact]
    public void Int64Le_TypeConverterAttribute_IsWired()
    {
        TypeDescriptor.GetConverter(typeof(Int64Le)).Should().BeOfType<Int64LeTypeConverter>();
    }

    [Fact]
    public void Int64Le_ConvertFrom_HexString()
    {
        var converter = new Int64LeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0x7FFFFFFFFFFFFFFF");
        ((long)(Int64Le)result!).Should().Be(long.MaxValue);
    }

    [Fact]
    public void Int64Le_ConvertTo_String()
    {
        var converter = new Int64LeTypeConverter();
        Int64Le value = 1L;
        var result = converter.ConvertTo(null, null, value, typeof(string));
        result.Should().Be("0x0000000000000001");
    }

    // ===================================================================
    // UInt128Be
    // ===================================================================

    [Fact]
    public void UInt128Be_TypeConverterAttribute_IsWired()
    {
        TypeDescriptor.GetConverter(typeof(UInt128Be)).Should().BeOfType<UInt128BeTypeConverter>();
    }

    [Fact]
    public void UInt128Be_ConvertFrom_HexString()
    {
        var converter = new UInt128BeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0x00000000000000010000000000000002");
        UInt128 expected = ((UInt128)1UL << 64) | 2UL;
        ((UInt128)(UInt128Be)result!).Should().Be(expected);
    }

    [Fact]
    public void UInt128Be_ConvertTo_String()
    {
        var converter = new UInt128BeTypeConverter();
        UInt128Be value = new((UInt128)1);
        var result = converter.ConvertTo(null, null, value, typeof(string));
        result.Should().Be("0x00000000000000000000000000000001");
    }

    // ===================================================================
    // Int128Be
    // ===================================================================

    [Fact]
    public void Int128Be_TypeConverterAttribute_IsWired()
    {
        TypeDescriptor.GetConverter(typeof(Int128Be)).Should().BeOfType<Int128BeTypeConverter>();
    }

    [Fact]
    public void Int128Be_ConvertFrom_HexString()
    {
        var converter = new Int128BeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0x7FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
        ((Int128)(Int128Be)result!).Should().Be(Int128.MaxValue);
    }

    [Fact]
    public void Int128Be_ConvertTo_String()
    {
        var converter = new Int128BeTypeConverter();
        Int128Be value = new((Int128)1);
        var result = converter.ConvertTo(null, null, value, typeof(string));
        result.Should().Be("0x00000000000000000000000000000001");
    }

    // ===================================================================
    // UInt128Le
    // ===================================================================

    [Fact]
    public void UInt128Le_TypeConverterAttribute_IsWired()
    {
        TypeDescriptor.GetConverter(typeof(UInt128Le)).Should().BeOfType<UInt128LeTypeConverter>();
    }

    [Fact]
    public void UInt128Le_ConvertFrom_HexString()
    {
        var converter = new UInt128LeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0x00000000000000010000000000000002");
        UInt128 expected = ((UInt128)1UL << 64) | 2UL;
        ((UInt128)(UInt128Le)result!).Should().Be(expected);
    }

    [Fact]
    public void UInt128Le_ConvertTo_String()
    {
        var converter = new UInt128LeTypeConverter();
        UInt128Le value = new((UInt128)1);
        var result = converter.ConvertTo(null, null, value, typeof(string));
        result.Should().Be("0x00000000000000000000000000000001");
    }

    // ===================================================================
    // Int128Le
    // ===================================================================

    [Fact]
    public void Int128Le_TypeConverterAttribute_IsWired()
    {
        TypeDescriptor.GetConverter(typeof(Int128Le)).Should().BeOfType<Int128LeTypeConverter>();
    }

    [Fact]
    public void Int128Le_ConvertFrom_HexString()
    {
        var converter = new Int128LeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0x7FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
        ((Int128)(Int128Le)result!).Should().Be(Int128.MaxValue);
    }

    [Fact]
    public void Int128Le_ConvertTo_String()
    {
        var converter = new Int128LeTypeConverter();
        Int128Le value = new((Int128)1);
        var result = converter.ConvertTo(null, null, value, typeof(string));
        result.Should().Be("0x00000000000000000000000000000001");
    }

    // ===================================================================
    // UInt256Be / UInt256Le
    // ===================================================================

    [Fact]
    public void UInt256Be_TypeConverterAttribute_IsWired()
    {
        TypeDescriptor.GetConverter(typeof(UInt256Be)).Should().BeOfType<UInt256BeTypeConverter>();
    }

    [Fact]
    public void UInt256Be_ConvertTo_String()
    {
        var converter = new UInt256BeTypeConverter();
        UInt256Be value = new((UInt256)1);
        var result = converter.ConvertTo(null, null, value, typeof(string));
        result.Should().Be("0x0000000000000000000000000000000000000000000000000000000000000001");
    }

    [Fact]
    public void UInt256Be_RoundTrip_HexString()
    {
        var converter = new UInt256BeTypeConverter();
        UInt256Be original = new((UInt256)0xDEADBEEFCAFEBABEUL);
        var str = (string)converter.ConvertTo(null, null, original, typeof(string))!;
        var restored = (UInt256Be)converter.ConvertFrom(null, null, str)!;
        ((UInt256)restored).Should().Be((UInt256)original);
    }

    [Fact]
    public void UInt256Le_TypeConverterAttribute_IsWired()
    {
        TypeDescriptor.GetConverter(typeof(UInt256Le)).Should().BeOfType<UInt256LeTypeConverter>();
    }

    [Fact]
    public void UInt256Le_ConvertTo_String()
    {
        var converter = new UInt256LeTypeConverter();
        UInt256Le value = new((UInt256)1);
        var result = converter.ConvertTo(null, null, value, typeof(string));
        result.Should().Be("0x0000000000000000000000000000000000000000000000000000000000000001");
    }

    // ===================================================================
    // Int256Be / Int256Le
    // ===================================================================

    [Fact]
    public void Int256Be_TypeConverterAttribute_IsWired()
    {
        TypeDescriptor.GetConverter(typeof(Int256Be)).Should().BeOfType<Int256BeTypeConverter>();
    }

    [Fact]
    public void Int256Be_ConvertFrom_NegativeDecimal()
    {
        var converter = new Int256BeTypeConverter();
        var result = converter.ConvertFrom(null, null, "-1");
        ((Int256)(Int256Be)result!).Should().Be(Int256.NegativeOne);
    }

    [Fact]
    public void Int256Be_NegativeOne_HexRoundTrip()
    {
        var converter = new Int256BeTypeConverter();
        Int256Be original = new(Int256.NegativeOne);
        var str = (string)converter.ConvertTo(null, null, original, typeof(string))!;
        str.Should().Be("0xffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff");
        var restored = (Int256Be)converter.ConvertFrom(null, null, str)!;
        ((Int256)restored).Should().Be(Int256.NegativeOne);
    }

    [Fact]
    public void Int256Le_TypeConverterAttribute_IsWired()
    {
        TypeDescriptor.GetConverter(typeof(Int256Le)).Should().BeOfType<Int256LeTypeConverter>();
    }

    [Fact]
    public void Int256Le_ConvertTo_String()
    {
        var converter = new Int256LeTypeConverter();
        Int256Le value = new((Int256)1);
        var result = converter.ConvertTo(null, null, value, typeof(string));
        result.Should().Be("0x0000000000000000000000000000000000000000000000000000000000000001");
    }

    // ===================================================================
    // CanConvertFrom / CanConvertTo : string is always supported
    // ===================================================================

    public static TheoryData<Type> AllConverterTypes => new()
    {
        typeof(UInt16BeTypeConverter),  typeof(UInt32BeTypeConverter),  typeof(UInt64BeTypeConverter),
        typeof(Int16BeTypeConverter),   typeof(Int32BeTypeConverter),   typeof(Int64BeTypeConverter),
        typeof(UInt16LeTypeConverter),  typeof(UInt32LeTypeConverter),  typeof(UInt64LeTypeConverter),
        typeof(Int16LeTypeConverter),   typeof(Int32LeTypeConverter),   typeof(Int64LeTypeConverter),
        typeof(UInt128BeTypeConverter), typeof(Int128BeTypeConverter),
        typeof(UInt128LeTypeConverter), typeof(Int128LeTypeConverter),
        typeof(UInt256BeTypeConverter), typeof(Int256BeTypeConverter),
        typeof(UInt256LeTypeConverter), typeof(Int256LeTypeConverter),
    };

    [Theory]
    [MemberData(nameof(AllConverterTypes))]
    public void AllConverters_CanConvertFrom_String(Type converterType)
    {
        var converter = (TypeConverter)Activator.CreateInstance(converterType)!;
        converter.CanConvertFrom(null, typeof(string)).Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(AllConverterTypes))]
    public void AllConverters_CanConvertTo_String(Type converterType)
    {
        var converter = (TypeConverter)Activator.CreateInstance(converterType)!;
        converter.CanConvertTo(null, typeof(string)).Should().BeTrue();
    }

    // ===================================================================
    // CanConvertFrom: native primitive of each type is supported
    // ===================================================================

    [Fact]
    public void UInt16Be_CanConvertFrom_UShort() =>
        new UInt16BeTypeConverter().CanConvertFrom(null, typeof(ushort)).Should().BeTrue();

    [Fact]
    public void Int16Be_CanConvertFrom_Short() =>
        new Int16BeTypeConverter().CanConvertFrom(null, typeof(short)).Should().BeTrue();

    [Fact]
    public void UInt32Be_CanConvertFrom_UInt() =>
        new UInt32BeTypeConverter().CanConvertFrom(null, typeof(uint)).Should().BeTrue();

    [Fact]
    public void Int32Be_CanConvertFrom_Int() =>
        new Int32BeTypeConverter().CanConvertFrom(null, typeof(int)).Should().BeTrue();

    [Fact]
    public void UInt64Be_CanConvertFrom_ULong() =>
        new UInt64BeTypeConverter().CanConvertFrom(null, typeof(ulong)).Should().BeTrue();

    [Fact]
    public void Int64Be_CanConvertFrom_Long() =>
        new Int64BeTypeConverter().CanConvertFrom(null, typeof(long)).Should().BeTrue();

    [Fact]
    public void UInt128Be_CanConvertFrom_UInt128() =>
        new UInt128BeTypeConverter().CanConvertFrom(null, typeof(UInt128)).Should().BeTrue();

    [Fact]
    public void Int128Be_CanConvertFrom_Int128() =>
        new Int128BeTypeConverter().CanConvertFrom(null, typeof(Int128)).Should().BeTrue();

    [Fact]
    public void UInt256Be_CanConvertFrom_UInt256() =>
        new UInt256BeTypeConverter().CanConvertFrom(null, typeof(UInt256)).Should().BeTrue();

    [Fact]
    public void Int256Be_CanConvertFrom_Int256() =>
        new Int256BeTypeConverter().CanConvertFrom(null, typeof(Int256)).Should().BeTrue();

    // UInt32Be does NOT accept int (requires exact uint) — confirms the scope stays tight.
    [Fact]
    public void UInt32Be_CannotConvertFrom_Int() =>
        new UInt32BeTypeConverter().CanConvertFrom(null, typeof(int)).Should().BeFalse();

    // ===================================================================
    // ConvertFrom / ConvertTo : native primitive round-trip
    // ===================================================================

    [Fact]
    public void UInt32Be_ConvertFrom_UInt()
    {
        var converter = new UInt32BeTypeConverter();
        var result = converter.ConvertFrom(null, null, 0xDEADBEEFU);
        ((uint)(UInt32Be)result!).Should().Be(0xDEADBEEFU);
    }

    [Fact]
    public void UInt32Be_ConvertTo_UInt()
    {
        var converter = new UInt32BeTypeConverter();
        UInt32Be value = 0x12345678U;
        var result = converter.ConvertTo(null, null, value, typeof(uint));
        result.Should().Be(0x12345678U);
    }

    [Fact]
    public void Int32Be_ConvertFrom_Int_NegativeValue()
    {
        var converter = new Int32BeTypeConverter();
        var result = converter.ConvertFrom(null, null, -42);
        ((int)(Int32Be)result!).Should().Be(-42);
    }

    // ===================================================================
    // ConvertTo null value : explicit null for string (existing contract)
    // ===================================================================

    [Fact]
    public void UInt32Be_ConvertTo_NullValue_ReturnsNull()
    {
        var converter = new UInt32BeTypeConverter();
        var result = converter.ConvertTo(null, null, null, typeof(string));
        result.Should().BeNull();
    }

    [Fact]
    public void UInt32Le_ConvertTo_NullValue_ReturnsNull()
    {
        var converter = new UInt32LeTypeConverter();
        var result = converter.ConvertTo(null, null, null, typeof(string));
        result.Should().BeNull();
    }

    // ===================================================================
    // Hex prefix case insensitivity
    // ===================================================================

    [Fact]
    public void UInt32Be_ConvertFrom_HexPrefix_Lowercase()
    {
        var converter = new UInt32BeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0xff");
        ((uint)(UInt32Be)result!).Should().Be(0xFF);
    }

    [Fact]
    public void UInt32Be_ConvertFrom_HexPrefix_Uppercase()
    {
        var converter = new UInt32BeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0XFF");
        ((uint)(UInt32Be)result!).Should().Be(0xFF);
    }

    [Fact]
    public void UInt32Le_ConvertFrom_HexPrefix_Lowercase()
    {
        var converter = new UInt32LeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0xff");
        ((uint)(UInt32Le)result!).Should().Be(0xFF);
    }

    [Fact]
    public void UInt32Le_ConvertFrom_HexPrefix_Uppercase()
    {
        var converter = new UInt32LeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0XFF");
        ((uint)(UInt32Le)result!).Should().Be(0xFF);
    }

    // ===================================================================
    // Binary prefix (0b)
    // ===================================================================

    [Fact]
    public void UInt32Be_ConvertFrom_Binary()
    {
        var converter = new UInt32BeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0b1010_0101");
        ((uint)(UInt32Be)result!).Should().Be(0xA5);
    }

    [Fact]
    public void UInt16Be_ConvertFrom_Binary_FullWidth()
    {
        var converter = new UInt16BeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0b1111_1111_1111_1111");
        ((ushort)(UInt16Be)result!).Should().Be(0xFFFF);
    }

    [Fact]
    public void UInt32Be_ConvertFrom_Binary_Uppercase_B()
    {
        var converter = new UInt32BeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0B10");
        ((uint)(UInt32Be)result!).Should().Be(2);
    }

    [Fact]
    public void UInt32Be_ConvertFrom_Binary_InvalidDigit_Throws()
    {
        var converter = new UInt32BeTypeConverter();
        Action act = () => converter.ConvertFrom(null, null, "0b10201");
        act.Should().Throw<FormatException>();
    }

    // ===================================================================
    // Digit separators '_'
    // ===================================================================

    [Fact]
    public void UInt32Be_ConvertFrom_Hex_WithUnderscores()
    {
        var converter = new UInt32BeTypeConverter();
        var result = converter.ConvertFrom(null, null, "0xDEAD_BEEF");
        ((uint)(UInt32Be)result!).Should().Be(0xDEADBEEFU);
    }

    [Fact]
    public void UInt32Be_ConvertFrom_Decimal_WithUnderscores()
    {
        var converter = new UInt32BeTypeConverter();
        var result = converter.ConvertFrom(null, null, "1_000_000");
        ((uint)(UInt32Be)result!).Should().Be(1_000_000U);
    }

    // ===================================================================
    // Whitespace trimming
    // ===================================================================

    [Fact]
    public void UInt32Be_ConvertFrom_LeadingAndTrailingWhitespace()
    {
        var converter = new UInt32BeTypeConverter();
        var result = converter.ConvertFrom(null, null, "  0xFF  ");
        ((uint)(UInt32Be)result!).Should().Be(0xFF);
    }

    // ===================================================================
    // Empty / whitespace-only input
    // ===================================================================

    [Fact]
    public void UInt32Be_ConvertFrom_EmptyString_Throws()
    {
        var converter = new UInt32BeTypeConverter();
        Action act = () => converter.ConvertFrom(null, null, "");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void UInt32Be_ConvertFrom_WhitespaceOnly_Throws()
    {
        var converter = new UInt32BeTypeConverter();
        Action act = () => converter.ConvertFrom(null, null, "   ");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void UInt32Be_ConvertFrom_HexPrefixOnly_Throws()
    {
        var converter = new UInt32BeTypeConverter();
        Action act = () => converter.ConvertFrom(null, null, "0x");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void UInt32Be_ConvertFrom_BinaryPrefixOnly_Throws()
    {
        var converter = new UInt32BeTypeConverter();
        Action act = () => converter.ConvertFrom(null, null, "0b");
        act.Should().Throw<FormatException>();
    }

    // ===================================================================
    // Error message includes original input
    // ===================================================================

    [Fact]
    public void UInt32Be_ConvertFrom_BadHex_ErrorIncludesInput()
    {
        var converter = new UInt32BeTypeConverter();
        Action act = () => converter.ConvertFrom(null, null, "0xZZZZ");
        act.Should().Throw<FormatException>()
           .WithMessage("*0xZZZZ*");
    }

    [Fact]
    public void UInt32Be_ConvertFrom_Overflow_ErrorIncludesInput()
    {
        var converter = new UInt32BeTypeConverter();
        Action act = () => converter.ConvertFrom(null, null, "0x1_0000_0000");
        act.Should().Throw<FormatException>()
           .WithMessage("*0x1_0000_0000*");
    }

    // ===================================================================
    // Culture: hex is always invariant; decimal honors supplied culture
    // ===================================================================

    [Fact]
    public void UInt32Be_ConvertFrom_Hex_InvariantRegardlessOfCulture()
    {
        // de-DE uses '.' as group separator — hex parsing must ignore culture and use invariant.
        var converter = new UInt32BeTypeConverter();
        var result = converter.ConvertFrom(null, new CultureInfo("de-DE"), "0xDEADBEEF");
        ((uint)(UInt32Be)result!).Should().Be(0xDEADBEEFU);
    }

    [Fact]
    public void Int32Be_ConvertFrom_NegativeDecimal_InvariantCulture()
    {
        var converter = new Int32BeTypeConverter();
        var result = converter.ConvertFrom(null, CultureInfo.InvariantCulture, "-42");
        ((int)(Int32Be)result!).Should().Be(-42);
    }

    // ===================================================================
    // IsValid (item 11)
    // ===================================================================

    [Theory]
    [InlineData("0xDEADBEEF")]
    [InlineData("0XDEADBEEF")]
    [InlineData("0b1010_0101")]
    [InlineData("305419896")]
    [InlineData("0xDEAD_BEEF")]
    [InlineData("  0xFF  ")]
    public void UInt32Be_IsValid_AcceptsGoodString(string input)
    {
        new UInt32BeTypeConverter().IsValid(null, input).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0x")]
    [InlineData("0b")]
    [InlineData("0xZZ")]
    [InlineData("0b1020")]
    [InlineData("0x1_0000_0000")] // overflows uint
    [InlineData("not-a-number")]
    public void UInt32Be_IsValid_RejectsBadString(string input)
    {
        new UInt32BeTypeConverter().IsValid(null, input).Should().BeFalse();
    }

    [Fact]
    public void UInt32Be_IsValid_AcceptsNativePrimitive()
    {
        new UInt32BeTypeConverter().IsValid(null, 0xDEADBEEFU).Should().BeTrue();
    }

    [Fact]
    public void UInt32Be_IsValid_AcceptsSameType()
    {
        var value = new UInt32Be(1U);
        new UInt32BeTypeConverter().IsValid(null, value).Should().BeTrue();
    }

    [Fact]
    public void Int16Be_IsValid_RejectsOverflowHex()
    {
        // "0x10000" = 65536, overflows short.
        new Int16BeTypeConverter().IsValid(null, "0x10000").Should().BeFalse();
    }

    // ===================================================================
    // GetStandardValues (item 12)
    // ===================================================================

    [Fact]
    public void UInt32Be_GetStandardValues_HasZeroAndMax()
    {
        var converter = new UInt32BeTypeConverter();
        converter.GetStandardValuesSupported(null).Should().BeTrue();
        converter.GetStandardValuesExclusive(null).Should().BeFalse();
        var values = converter.GetStandardValues(null)!;
        values.Count.Should().Be(2);
        values.Cast<UInt32Be>().Select(v => (uint)v).Should().BeEquivalentTo(new[] { 0U, uint.MaxValue });
    }

    [Fact]
    public void Int32Be_GetStandardValues_HasMinZeroMax()
    {
        var converter = new Int32BeTypeConverter();
        converter.GetStandardValuesSupported(null).Should().BeTrue();
        converter.GetStandardValuesExclusive(null).Should().BeFalse();
        var values = converter.GetStandardValues(null)!;
        values.Count.Should().Be(3);
        values.Cast<Int32Be>().Select(v => (int)v).Should().BeEquivalentTo(new[] { int.MinValue, 0, int.MaxValue });
    }

    [Fact]
    public void UInt256Be_GetStandardValues_HasZeroAndMax()
    {
        var converter = new UInt256BeTypeConverter();
        converter.GetStandardValuesSupported(null).Should().BeTrue();
        var values = converter.GetStandardValues(null)!;
        values.Count.Should().Be(2);
        var arr = values.Cast<UInt256Be>().Select(v => (UInt256)v).ToArray();
        arr[0].Should().Be(UInt256.Zero);
        arr[1].Should().Be(UInt256.MaxValue);
    }

    [Fact]
    public void Int256Be_GetStandardValues_HasMinZeroMax()
    {
        var converter = new Int256BeTypeConverter();
        var values = converter.GetStandardValues(null)!;
        values.Count.Should().Be(3);
        var arr = values.Cast<Int256Be>().Select(v => (Int256)v).ToArray();
        arr[0].Should().Be(Int256.MinValue);
        arr[1].Should().Be(Int256.Zero);
        arr[2].Should().Be(Int256.MaxValue);
    }

    // ===================================================================
    // InstanceDescriptor (item 13)
    // ===================================================================

    [Fact]
    public void UInt16Be_CanConvertTo_InstanceDescriptor() =>
        new UInt16BeTypeConverter().CanConvertTo(null, typeof(InstanceDescriptor)).Should().BeTrue();

    [Fact]
    public void UInt16Be_ConvertTo_InstanceDescriptor_InvokesReproducesValue()
    {
        var converter = new UInt16BeTypeConverter();
        UInt16Be original = (UInt16Be)(ushort)0x1234;
        var descriptor = (InstanceDescriptor)converter.ConvertTo(null, null, original, typeof(InstanceDescriptor))!;
        descriptor.MemberInfo.Should().NotBeNull();
        descriptor.Arguments!.Cast<object>().Should().ContainSingle().Which.Should().Be((ushort)0x1234);
        var rebuilt = (UInt16Be)descriptor.Invoke()!;
        ((ushort)rebuilt).Should().Be(0x1234);
    }

    [Fact]
    public void Int32Be_ConvertTo_InstanceDescriptor_PreservesNegative()
    {
        var converter = new Int32BeTypeConverter();
        Int32Be original = new(-42);
        var descriptor = (InstanceDescriptor)converter.ConvertTo(null, null, original, typeof(InstanceDescriptor))!;
        var rebuilt = (Int32Be)descriptor.Invoke()!;
        ((int)rebuilt).Should().Be(-42);
    }

    [Fact]
    public void UInt256Be_ConvertTo_InstanceDescriptor_PreservesValue()
    {
        var converter = new UInt256BeTypeConverter();
        UInt256Be original = new((UInt256)0xDEADBEEFCAFEBABEUL);
        var descriptor = (InstanceDescriptor)converter.ConvertTo(null, null, original, typeof(InstanceDescriptor))!;
        var rebuilt = (UInt256Be)descriptor.Invoke()!;
        ((UInt256)rebuilt).Should().Be((UInt256)0xDEADBEEFCAFEBABEUL);
    }

    // ===================================================================
    // UInt256 / Int256 ToString hex width (previously broken; now produces
    // a string of correct length regardless of width specifier)
    // ===================================================================

    [Fact]
    public void UInt256_ToString_x64_IsExactly64Chars_ForOne()
    {
        ((UInt256)1).ToString("x64", CultureInfo.InvariantCulture)
            .Should().Be("0000000000000000000000000000000000000000000000000000000000000001");
    }

    [Fact]
    public void UInt256_ToString_x64_IsExactly64Chars_ForMaxValue()
    {
        UInt256.MaxValue.ToString("x64", CultureInfo.InvariantCulture)
            .Should().Be(new string('f', 64));
    }

    [Fact]
    public void UInt256_ToString_x_NaturalLength_ForSmallValue()
    {
        // "x" with no width → natural length, no leading zeros.
        ((UInt256)0xDEADBEEFU).ToString("x", CultureInfo.InvariantCulture)
            .Should().Be("deadbeef");
    }

    [Fact]
    public void Int256_ToString_x64_NegativeOne_IsAllFs()
    {
        Int256.NegativeOne.ToString("x64", CultureInfo.InvariantCulture)
            .Should().Be(new string('f', 64));
    }

    [Fact]
    public void Int256_ToString_x_SmallPositive_NaturalLength()
    {
        ((Int256)0x10).ToString("x", CultureInfo.InvariantCulture).Should().Be("10");
    }

    [Fact]
    public void UInt256Be_Default_ToString_RoundTripsThroughConverter()
    {
        // UInt256Be.ToString() uses "$"0x{(UInt256)this:x64}"" — verify that the library
        // type's own display form is valid input back through the converter.
        UInt256Be value = new((UInt256)42);
        string s = value.ToString();
        s.Length.Should().Be(66); // "0x" + 64 hex
        var converter = new UInt256BeTypeConverter();
        var restored = (UInt256Be)converter.ConvertFrom(null, null, s)!;
        ((UInt256)restored).Should().Be((UInt256)42);
    }

    // ===================================================================
    // Raw UInt256 / Int256 TypeConverters (not the Be/Le wrappers)
    // ===================================================================

    [Fact]
    public void UInt256_TypeConverterAttribute_IsWired()
    {
        TypeDescriptor.GetConverter(typeof(UInt256)).Should().BeOfType<UInt256TypeConverter>();
    }

    [Fact]
    public void UInt256_ConvertFrom_DecimalString()
    {
        var converter = new UInt256TypeConverter();
        var result = converter.ConvertFrom(null, null, "305419896");
        result.Should().BeOfType<UInt256>();
        ((UInt256)result!).Should().Be((UInt256)0x12345678UL);
    }

    [Fact]
    public void UInt256_ConvertFrom_HexString()
    {
        var converter = new UInt256TypeConverter();
        var result = converter.ConvertFrom(null, null, "0xDEADBEEFCAFEBABE");
        ((UInt256)result!).Should().Be((UInt256)0xDEADBEEFCAFEBABEUL);
    }

    [Fact]
    public void UInt256_ConvertFrom_BinaryString()
    {
        var converter = new UInt256TypeConverter();
        var result = converter.ConvertFrom(null, null, "0b1111_0000");
        ((UInt256)result!).Should().Be((UInt256)0xF0UL);
    }

    [Fact]
    public void UInt256_ConvertTo_String_IsDecimal()
    {
        // Matches System.ComponentModel.Int32Converter and friends: numeric TypeConverters
        // emit culture-aware decimal strings, not hex.
        var converter = new UInt256TypeConverter();
        var result = converter.ConvertTo(null, CultureInfo.InvariantCulture, (UInt256)42, typeof(string));
        result.Should().Be("42");
    }

    [Fact]
    public void UInt256_ConvertTo_String_MaxValueIsFullDecimal()
    {
        var converter = new UInt256TypeConverter();
        var result = converter.ConvertTo(null, CultureInfo.InvariantCulture, UInt256.MaxValue, typeof(string));
        result.Should().Be("115792089237316195423570985008687907853269984665640564039457584007913129639935");
    }

    [Fact]
    public void UInt256_RoundTrip_DefaultDecimalOutput()
    {
        // ConvertTo emits decimal; ConvertFrom parses decimal → value round-trips exactly.
        var converter = new UInt256TypeConverter();
        UInt256 original = (UInt256)0xDEADBEEFCAFEBABEUL;
        var str = (string)converter.ConvertTo(null, CultureInfo.InvariantCulture, original, typeof(string))!;
        var restored = (UInt256)converter.ConvertFrom(null, CultureInfo.InvariantCulture, str)!;
        restored.Should().Be(original);
    }

    [Fact]
    public void UInt256_ConvertFrom_HexInput_StillRoundTripsToDecimalOutput()
    {
        // Input grammar is rich (dec/hex/binary with underscores); output is always decimal.
        var converter = new UInt256TypeConverter();
        UInt256 viaHex = (UInt256)converter.ConvertFrom(null, CultureInfo.InvariantCulture, "0xDEAD_BEEF")!;
        var decimalOut = (string)converter.ConvertTo(null, CultureInfo.InvariantCulture, viaHex, typeof(string))!;
        decimalOut.Should().Be("3735928559");
    }

    [Fact]
    public void UInt256_ConvertTo_InstanceDescriptor_PreservesValue()
    {
        var converter = new UInt256TypeConverter();
        UInt256 original = new(0x1111_2222_3333_4444UL, 0x5555_6666_7777_8888UL,
                               0x9999_AAAA_BBBB_CCCCUL, 0xDDDD_EEEE_FFFF_0000UL);
        var descriptor = (InstanceDescriptor)converter.ConvertTo(null, null, original, typeof(InstanceDescriptor))!;
        var rebuilt = (UInt256)descriptor.Invoke()!;
        rebuilt.Should().Be(original);
    }

    [Fact]
    public void UInt256_GetStandardValues_HasZeroAndMax()
    {
        var converter = new UInt256TypeConverter();
        converter.GetStandardValuesSupported(null).Should().BeTrue();
        converter.GetStandardValuesExclusive(null).Should().BeFalse();
        var values = converter.GetStandardValues(null)!;
        values.Count.Should().Be(2);
        var arr = values.Cast<UInt256>().ToArray();
        arr[0].Should().Be(UInt256.Zero);
        arr[1].Should().Be(UInt256.MaxValue);
    }

    [Fact]
    public void UInt256_IsValid_AcceptsParsableString()
    {
        var converter = new UInt256TypeConverter();
        converter.IsValid(null, "0xFF").Should().BeTrue();
        converter.IsValid(null, "not a number").Should().BeFalse();
    }

    [Fact]
    public void Int256_TypeConverterAttribute_IsWired()
    {
        TypeDescriptor.GetConverter(typeof(Int256)).Should().BeOfType<Int256TypeConverter>();
    }

    [Fact]
    public void Int256_ConvertFrom_NegativeDecimal()
    {
        var converter = new Int256TypeConverter();
        var result = converter.ConvertFrom(null, null, "-1");
        ((Int256)result!).Should().Be(Int256.NegativeOne);
    }

    [Fact]
    public void Int256_NegativeOne_DecimalRoundTrip()
    {
        var converter = new Int256TypeConverter();
        Int256 original = Int256.NegativeOne;
        var str = (string)converter.ConvertTo(null, CultureInfo.InvariantCulture, original, typeof(string))!;
        str.Should().Be("-1");
        var restored = (Int256)converter.ConvertFrom(null, CultureInfo.InvariantCulture, str)!;
        restored.Should().Be(Int256.NegativeOne);
    }

    [Fact]
    public void Int256_ConvertTo_String_PositiveIsDecimal()
    {
        // Matches System.ComponentModel.Int32Converter: decimal output, not hex.
        var converter = new Int256TypeConverter();
        var result = converter.ConvertTo(null, CultureInfo.InvariantCulture, (Int256)42, typeof(string));
        result.Should().Be("42");
    }

    [Fact]
    public void Int256_ConvertFrom_HexNegativeBitPattern_FormatsAsNegativeDecimal()
    {
        // Hex input is interpreted as two's-complement bits, so the all-Fs hex pattern
        // is -1, which formats back as "-1" in decimal.
        var converter = new Int256TypeConverter();
        var hexIn = "0x" + new string('f', 64);
        var v = (Int256)converter.ConvertFrom(null, CultureInfo.InvariantCulture, hexIn)!;
        var decOut = (string)converter.ConvertTo(null, CultureInfo.InvariantCulture, v, typeof(string))!;
        decOut.Should().Be("-1");
    }

    [Fact]
    public void Int256_ConvertTo_InstanceDescriptor_PreservesNegative()
    {
        var converter = new Int256TypeConverter();
        Int256 original = (Int256)(-42);
        var descriptor = (InstanceDescriptor)converter.ConvertTo(null, null, original, typeof(InstanceDescriptor))!;
        var rebuilt = (Int256)descriptor.Invoke()!;
        rebuilt.Should().Be(original);
    }

    [Fact]
    public void Int256_GetStandardValues_HasMinZeroMax()
    {
        var converter = new Int256TypeConverter();
        var values = converter.GetStandardValues(null)!;
        values.Count.Should().Be(3);
        var arr = values.Cast<Int256>().ToArray();
        arr[0].Should().Be(Int256.MinValue);
        arr[1].Should().Be(Int256.Zero);
        arr[2].Should().Be(Int256.MaxValue);
    }

    [Fact]
    public void Int256_IsValid_AcceptsParsableString()
    {
        var converter = new Int256TypeConverter();
        converter.IsValid(null, "-1").Should().BeTrue();
        converter.IsValid(null, "garbage").Should().BeFalse();
    }
}
