using System.Runtime.CompilerServices;
using FluentAssertions;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Unit tests that exercise <see cref="BitFieldsAttribute(StorageType, UndefinedBitsMustBe, BitOrder, ByteOrder)"/>
/// for every value of the <see cref="StorageType"/> enum. This ensures that the
/// enum-to-<see cref="System.Type"/> mapping inside
/// <c>BitFieldsAttribute.MapToType</c> and the generator's handling of each backing
/// type are exercised end-to-end, not just via <c>typeof(T)</c>.
/// </summary>
public partial class StorageTypeEnumTests
{
    #region Test Struct Definitions — Small Integer Backings

    /// <summary>8-bit signed backing (sbyte) via the StorageType enum constructor.</summary>
    [BitFields(StorageType.SByte)]
    public partial struct EnumRegSByte
    {
        [BitFlag(0)] public partial bool Low { get; set; }
        [BitField(1, End = 6)] public partial byte Mid { get; set; }  // bits 1..=6 (6 bits)
        [BitFlag(7)] public partial bool Sign { get; set; }
    }

    /// <summary>16-bit signed backing (short) via the StorageType enum constructor.</summary>
    [BitFields(StorageType.Int16)]
    public partial struct EnumRegInt16
    {
        [BitFlag(0)] public partial bool Low { get; set; }
        [BitField(1, End = 14)] public partial ushort Mid { get; set; }  // bits 1..=14 (14 bits)
        [BitFlag(15)] public partial bool Sign { get; set; }
    }

    /// <summary>16-bit unsigned backing (ushort) via the StorageType enum constructor.</summary>
    [BitFields(StorageType.UInt16)]
    public partial struct EnumRegUInt16
    {
        [BitField(0, End = 7)] public partial byte Low { get; set; }
        [BitField(8, End = 15)] public partial byte High { get; set; }
    }

    /// <summary>32-bit signed backing (int) via the StorageType enum constructor.</summary>
    [BitFields(StorageType.Int32)]
    public partial struct EnumRegInt32
    {
        [BitField(0, End = 15)] public partial ushort Low { get; set; }
        [BitField(16, End = 30)] public partial ushort High { get; set; }  // bits 16..=30 (15 bits)
        [BitFlag(31)] public partial bool Sign { get; set; }
    }

    /// <summary>64-bit signed backing (long) via the StorageType enum constructor.</summary>
    [BitFields(StorageType.Int64)]
    public partial struct EnumRegInt64
    {
        [BitField(0, End = 31)] public partial uint Low { get; set; }
        [BitField(32, End = 62)] public partial uint High { get; set; }  // bits 32..=62 (31 bits)
        [BitFlag(63)] public partial bool Sign { get; set; }
    }

    #endregion

    #region Test Struct Definitions — Native Integer Backings

    /// <summary>Platform-dependent signed native integer (nint) via the StorageType enum constructor.</summary>
    [BitFields(StorageType.NInt)]
    public partial struct EnumRegNInt
    {
        [BitField(0, End = 15)] public partial ushort Low { get; set; }
        [BitFlag(16)] public partial bool Flag { get; set; }
    }

    /// <summary>Platform-dependent unsigned native integer (nuint) via the StorageType enum constructor.</summary>
    [BitFields(StorageType.NUInt)]
    public partial struct EnumRegNUInt
    {
        [BitField(0, End = 15)] public partial ushort Low { get; set; }
        [BitFlag(16)] public partial bool Flag { get; set; }
    }

    #endregion

    #region Test Struct Definitions — Floating-Point Backings

    /// <summary>
    /// IEEE 754 half-precision float (16-bit) via the StorageType enum constructor.
    /// </summary>
    [BitFields(StorageType.Half)]
    public partial struct EnumRegHalf
    {
        [BitField(0, End = 9)] public partial ushort Mantissa { get; set; }
        [BitField(10, End = 14)] public partial byte Exponent { get; set; }
        [BitFlag(15)] public partial bool Sign { get; set; }
    }

    /// <summary>
    /// IEEE 754 single-precision float (32-bit) via the StorageType enum constructor.
    /// </summary>
    [BitFields(StorageType.Single)]
    public partial struct EnumRegSingle
    {
        [BitField(0, End = 22)] public partial uint Mantissa { get; set; }
        [BitField(23, End = 30)] public partial byte Exponent { get; set; }
        [BitFlag(31)] public partial bool Sign { get; set; }
    }

    /// <summary>
    /// IEEE 754 double-precision float (64-bit) via the StorageType enum constructor.
    /// </summary>
    [BitFields(StorageType.Double)]
    public partial struct EnumRegDouble
    {
        [BitField(0, End = 51)] public partial ulong Mantissa { get; set; }
        [BitField(52, End = 62)] public partial ushort Exponent { get; set; }
        [BitFlag(63)] public partial bool Sign { get; set; }
    }

    /// <summary>
    /// .NET decimal (128 bits) via the StorageType enum constructor.
    /// Layout matches decimal.GetBits() canonical order.
    /// </summary>
    [BitFields(StorageType.Decimal)]
    public partial struct EnumRegDecimal
    {
        [BitField(0, End = 95)] public partial UInt128 Coefficient { get; set; }
        [BitField(112, End = 118)] public partial byte Scale { get; set; }
        [BitFlag(127)] public partial bool Sign { get; set; }
    }

    #endregion

    #region Test Struct Definitions — Wide (128/256-bit) Integer Backings

    /// <summary>128-bit signed (Int128) via the StorageType enum constructor.</summary>
    [BitFields(StorageType.Int128)]
    public partial struct EnumRegInt128
    {
        [BitField(0, End = 63)] public partial ulong Low { get; set; }
        [BitField(64, End = 127)] public partial ulong High { get; set; }
    }

    /// <summary>128-bit unsigned (UInt128) via the StorageType enum constructor.</summary>
    [BitFields(StorageType.UInt128)]
    public partial struct EnumRegUInt128
    {
        [BitField(0, End = 63)] public partial ulong Low { get; set; }
        [BitField(64, End = 127)] public partial ulong High { get; set; }
    }

    /// <summary>256-bit signed (Int256) via the StorageType enum constructor.</summary>
    [BitFields(StorageType.Int256)]
    public partial struct EnumRegInt256
    {
        [BitField(0, End = 63)] public partial ulong W0 { get; set; }
        [BitField(64, End = 127)] public partial ulong W1 { get; set; }
        [BitField(128, End = 191)] public partial ulong W2 { get; set; }
        [BitField(192, End = 255)] public partial ulong W3 { get; set; }
    }

    /// <summary>256-bit unsigned (UInt256) via the StorageType enum constructor.</summary>
    [BitFields(StorageType.UInt256)]
    public partial struct EnumRegUInt256
    {
        [BitField(0, End = 63)] public partial ulong W0 { get; set; }
        [BitField(64, End = 127)] public partial ulong W1 { get; set; }
        [BitField(128, End = 191)] public partial ulong W2 { get; set; }
        [BitField(192, End = 255)] public partial ulong W3 { get; set; }
    }

    #endregion

    #region SByte

    [Fact]
    public void EnumRegSByte_RoundTrip_PreservesRawBits()
    {
        EnumRegSByte reg = unchecked((sbyte)0xA5);  // 1010_0101
        reg.Low.Should().BeTrue();
        reg.Sign.Should().BeTrue();
        reg.Mid.Should().Be(0b010010);  // bits 1..=6
        ((sbyte)reg).Should().Be(unchecked((sbyte)0xA5));
    }

    [Fact]
    public void EnumRegSByte_SetFields_ComposesRawValue()
    {
        EnumRegSByte reg = default;
        reg.Low = true;
        reg.Mid = 0b010010;
        reg.Sign = true;
        ((sbyte)reg).Should().Be(unchecked((sbyte)0xA5));
    }

    [Fact]
    public void EnumRegSByte_Size_Is1Byte()
    {
        Unsafe.SizeOf<EnumRegSByte>().Should().Be(1);
    }

    #endregion

    #region Int16

    [Fact]
    public void EnumRegInt16_RoundTrip_PreservesRawBits()
    {
        EnumRegInt16 reg = unchecked((short)0xBEEF);
        reg.Low.Should().BeTrue();
        reg.Sign.Should().BeTrue();
        reg.Mid.Should().Be((ushort)((0xBEEF >> 1) & 0x3FFF));
        ((short)reg).Should().Be(unchecked((short)0xBEEF));
    }

    [Fact]
    public void EnumRegInt16_Size_Is2Bytes()
    {
        Unsafe.SizeOf<EnumRegInt16>().Should().Be(2);
    }

    #endregion

    #region UInt16

    [Fact]
    public void EnumRegUInt16_RoundTrip_PreservesRawBits()
    {
        EnumRegUInt16 reg = (ushort)0xABCD;
        reg.Low.Should().Be(0xCD);
        reg.High.Should().Be(0xAB);
        ((ushort)reg).Should().Be((ushort)0xABCD);
    }

    [Fact]
    public void EnumRegUInt16_SetFields_ComposesRawValue()
    {
        EnumRegUInt16 reg = default;
        reg.Low = 0xEF;
        reg.High = 0x12;
        ((ushort)reg).Should().Be((ushort)0x12EF);
    }

    #endregion

    #region Int32

    [Fact]
    public void EnumRegInt32_RoundTrip_PreservesRawBits()
    {
        EnumRegInt32 reg = unchecked((int)0x80001234);
        reg.Low.Should().Be(0x1234);
        reg.Sign.Should().BeTrue("bit 31 is set on 0x80001234");
        ((int)reg).Should().Be(unchecked((int)0x80001234));
    }

    [Fact]
    public void EnumRegInt32_Negative_RoundTrips()
    {
        EnumRegInt32 reg = -1;
        ((int)reg).Should().Be(-1);
        reg.Sign.Should().BeTrue();
    }

    [Fact]
    public void EnumRegInt32_Size_Is4Bytes()
    {
        Unsafe.SizeOf<EnumRegInt32>().Should().Be(4);
    }

    #endregion

    #region Int64

    [Fact]
    public void EnumRegInt64_RoundTrip_PreservesRawBits()
    {
        EnumRegInt64 reg = unchecked((long)0x8000000012345678UL);
        reg.Low.Should().Be(0x12345678);
        reg.Sign.Should().BeTrue();
        ((long)reg).Should().Be(unchecked((long)0x8000000012345678UL));
    }

    [Fact]
    public void EnumRegInt64_MinValue_RoundTrips()
    {
        EnumRegInt64 reg = long.MinValue;
        ((long)reg).Should().Be(long.MinValue);
        reg.Sign.Should().BeTrue();
    }

    [Fact]
    public void EnumRegInt64_Size_Is8Bytes()
    {
        Unsafe.SizeOf<EnumRegInt64>().Should().Be(8);
    }

    #endregion

    #region NInt / NUInt

    [Fact]
    public void EnumRegNInt_RoundTrip_PreservesFields()
    {
        EnumRegNInt reg = default;
        reg.Low = 0xABCD;
        reg.Flag = true;
        reg.Low.Should().Be(0xABCD);
        reg.Flag.Should().BeTrue();
        ((nint)reg).Should().Be((nint)0x1ABCD);
    }

    [Fact]
    public void EnumRegNUInt_RoundTrip_PreservesFields()
    {
        EnumRegNUInt reg = default;
        reg.Low = 0xABCD;
        reg.Flag = true;
        reg.Low.Should().Be(0xABCD);
        reg.Flag.Should().BeTrue();
        ((nuint)reg).Should().Be((nuint)0x1ABCD);
    }

    #endregion

    #region Half

    [Fact]
    public void EnumRegHalf_FromHalf_DecomposesBits()
    {
        EnumRegHalf reg = (Half)1.5;
        reg.Sign.Should().BeFalse();
        reg.Exponent.Should().Be(15);    // bias for 2^0
        reg.Mantissa.Should().Be(0x200); // bit 9 set -> 0.5 contribution
        ((Half)reg).Should().Be((Half)1.5);
    }

    [Fact]
    public void EnumRegHalf_FromHalf_Negative()
    {
        EnumRegHalf reg = (Half)(-2.0);
        reg.Sign.Should().BeTrue();
        ((Half)reg).Should().Be((Half)(-2.0));
    }

    [Fact]
    public void EnumRegHalf_Size_Is2Bytes()
    {
        Unsafe.SizeOf<EnumRegHalf>().Should().Be(2);
    }

    #endregion

    #region Single

    [Fact]
    public void EnumRegSingle_FromFloat_DecomposesBits()
    {
        EnumRegSingle reg = 1.0f;
        reg.Sign.Should().BeFalse();
        reg.Exponent.Should().Be(127);   // IEEE 754 bias for 2^0
        reg.Mantissa.Should().Be(0u);    // implied leading 1
        ((float)reg).Should().Be(1.0f);
    }

    [Fact]
    public void EnumRegSingle_SetFromBits_ProducesOne()
    {
        EnumRegSingle reg = default;
        reg.Exponent = 127;
        ((float)reg).Should().Be(1.0f);
    }

    [Fact]
    public void EnumRegSingle_Size_Is4Bytes()
    {
        Unsafe.SizeOf<EnumRegSingle>().Should().Be(4);
    }

    #endregion

    #region Double

    [Fact]
    public void EnumRegDouble_FromDouble_DecomposesBits()
    {
        EnumRegDouble reg = 1.0;
        reg.Sign.Should().BeFalse();
        reg.Exponent.Should().Be(1023);  // IEEE 754 bias for 2^0
        reg.Mantissa.Should().Be(0UL);
        ((double)reg).Should().Be(1.0);
    }

    [Fact]
    public void EnumRegDouble_NegativePi_RoundTrips()
    {
        EnumRegDouble reg = -System.Math.PI;
        reg.Sign.Should().BeTrue();
        ((double)reg).Should().Be(-System.Math.PI);
    }

    [Fact]
    public void EnumRegDouble_Size_Is8Bytes()
    {
        Unsafe.SizeOf<EnumRegDouble>().Should().Be(8);
    }

    #endregion

    #region Decimal

    [Fact]
    public void EnumRegDecimal_FromDecimal_RoundTrips()
    {
        EnumRegDecimal reg = 3.14m;
        ((decimal)reg).Should().Be(3.14m);
        reg.Scale.Should().Be(2);
        reg.Sign.Should().BeFalse();
    }

    [Fact]
    public void EnumRegDecimal_FromNegative_SetsSign()
    {
        EnumRegDecimal reg = -1.5m;
        reg.Sign.Should().BeTrue();
        ((decimal)reg).Should().Be(-1.5m);
    }

    [Fact]
    public void EnumRegDecimal_Size_Is16Bytes()
    {
        Unsafe.SizeOf<EnumRegDecimal>().Should().Be(16);
    }

    #endregion

    #region Int128

    [Fact]
    public void EnumRegInt128_RoundTrip_PreservesFields()
    {
        Int128 value = (Int128)(((UInt128)0xDEADBEEFCAFEBABEUL << 64) | 0x0123456789ABCDEFUL);
        EnumRegInt128 reg = value;
        reg.Low.Should().Be(0x0123456789ABCDEFUL);
        reg.High.Should().Be(0xDEADBEEFCAFEBABEUL);
        ((Int128)reg).Should().Be(value);
    }

    [Fact]
    public void EnumRegInt128_NegativeOne_RoundTrips()
    {
        EnumRegInt128 reg = (Int128)(-1);
        ((Int128)reg).Should().Be((Int128)(-1));
        reg.Low.Should().Be(0xFFFFFFFFFFFFFFFFUL);
        reg.High.Should().Be(0xFFFFFFFFFFFFFFFFUL);
    }

    [Fact]
    public void EnumRegInt128_Size_Is16Bytes()
    {
        Unsafe.SizeOf<EnumRegInt128>().Should().Be(16);
    }

    #endregion

    #region UInt128

    [Fact]
    public void EnumRegUInt128_RoundTrip_PreservesFields()
    {
        UInt128 value = ((UInt128)0xAAAAAAAAAAAAAAAAUL << 64) | 0xBBBBBBBBBBBBBBBBUL;
        EnumRegUInt128 reg = value;
        reg.Low.Should().Be(0xBBBBBBBBBBBBBBBBUL);
        reg.High.Should().Be(0xAAAAAAAAAAAAAAAAUL);
        ((UInt128)reg).Should().Be(value);
    }

    [Fact]
    public void EnumRegUInt128_MaxValue_RoundTrips()
    {
        EnumRegUInt128 reg = UInt128.MaxValue;
        ((UInt128)reg).Should().Be(UInt128.MaxValue);
    }

    [Fact]
    public void EnumRegUInt128_Size_Is16Bytes()
    {
        Unsafe.SizeOf<EnumRegUInt128>().Should().Be(16);
    }

    #endregion

    #region Int256

    [Fact]
    public void EnumRegInt256_RoundTrip_PreservesFields()
    {
        Int256 value = (Int256)new UInt256(
            0xDEADBEEF01020304UL,
            0xCAFEBABE05060708UL,
            0xFEEDFACE090A0B0CUL,
            0xBAAAAAAD0D0E0F10UL);
        EnumRegInt256 reg = value;
        reg.W0.Should().Be(0xBAAAAAAD0D0E0F10UL);
        reg.W1.Should().Be(0xFEEDFACE090A0B0CUL);
        reg.W2.Should().Be(0xCAFEBABE05060708UL);
        reg.W3.Should().Be(0xDEADBEEF01020304UL);
        ((Int256)reg).Should().Be(value);
    }

    [Fact]
    public void EnumRegInt256_NegativeOne_RoundTrips()
    {
        EnumRegInt256 reg = (Int256)(-1);
        ((Int256)reg).Should().Be((Int256)(-1));
        reg.W3.Should().Be(0xFFFFFFFFFFFFFFFFUL);
    }

    [Fact]
    public void EnumRegInt256_MinValue_RoundTrips()
    {
        EnumRegInt256 reg = Int256.MinValue;
        ((Int256)reg).Should().Be(Int256.MinValue);
    }

    [Fact]
    public void EnumRegInt256_Size_Is32Bytes()
    {
        Unsafe.SizeOf<EnumRegInt256>().Should().Be(32);
    }

    #endregion

    #region UInt256

    [Fact]
    public void EnumRegUInt256_RoundTrip_PreservesFields()
    {
        UInt256 value = new UInt256(
            0x1111111111111111UL,
            0x2222222222222222UL,
            0x3333333333333333UL,
            0x4444444444444444UL);
        EnumRegUInt256 reg = value;
        reg.W0.Should().Be(0x4444444444444444UL);
        reg.W1.Should().Be(0x3333333333333333UL);
        reg.W2.Should().Be(0x2222222222222222UL);
        reg.W3.Should().Be(0x1111111111111111UL);
        ((UInt256)reg).Should().Be(value);
    }

    [Fact]
    public void EnumRegUInt256_MaxValue_RoundTrips()
    {
        EnumRegUInt256 reg = UInt256.MaxValue;
        ((UInt256)reg).Should().Be(UInt256.MaxValue);
    }

    [Fact]
    public void EnumRegUInt256_SetFields_ComposesRawValue()
    {
        EnumRegUInt256 reg = default;
        reg.W0 = 0x1234567890ABCDEFUL;
        reg.W1 = 0xFEDCBA0987654321UL;
        reg.W2 = 0x0123456789ABCDEFUL;
        reg.W3 = 0x1032547698BADCFEUL;
        UInt256 expected = new UInt256(
            0x1032547698BADCFEUL,
            0x0123456789ABCDEFUL,
            0xFEDCBA0987654321UL,
            0x1234567890ABCDEFUL);
        ((UInt256)reg).Should().Be(expected);
    }

    [Fact]
    public void EnumRegUInt256_Size_Is32Bytes()
    {
        Unsafe.SizeOf<EnumRegUInt256>().Should().Be(32);
    }

    #endregion

    #region Enum vs typeof Equivalence

    /// <summary>
    /// Verifies that every StorageType enum value maps to a non-null Type via the
    /// attribute's internal mapping. This guards against accidentally dropping a case
    /// from MapToType when new enum values are added.
    /// </summary>
    [Fact]
    public void StorageType_EveryEnumValue_ProducesTypedAttribute()
    {
        foreach (StorageType value in System.Enum.GetValues<StorageType>())
        {
            var attr = new BitFieldsAttribute(value);
            attr.StorageType.Should().NotBeNull($"StorageType.{value} must map to a concrete System.Type");
        }
    }

    #endregion
}
