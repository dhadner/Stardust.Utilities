using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Tests for the isBigEndian parameter added to ToBytes, WriteTo, and TryWriteTo on every
/// Int/UInt *Be and *Le type. The parameter defaults to the type's own storage endianness;
/// passing the opposite value reverses the bytes at the output boundary.
/// </summary>
public class EndianExportWithOrderTests
{
    // Canonical byte/numeric pairs used across every width.
    private static readonly byte[] BE16 = [0x12, 0x34];
    private static readonly byte[] LE16 = [0x34, 0x12];
    private const ushort U16 = 0x1234;
    private const short I16 = 0x1234;

    private static readonly byte[] BE32 = [0x12, 0x34, 0x56, 0x78];
    private static readonly byte[] LE32 = [0x78, 0x56, 0x34, 0x12];
    private const uint U32 = 0x12345678u;
    private const int I32 = 0x12345678;

    private static readonly byte[] BE64 = [0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF];
    private static readonly byte[] LE64 = [0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01];
    private const ulong U64 = 0x0123456789ABCDEFul;
    private const long I64 = 0x0123456789ABCDEFL;

    private static readonly byte[] BE128 =
    [
        0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
        0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF
    ];
    private static readonly byte[] LE128 =
    [
        0xFF, 0xEE, 0xDD, 0xCC, 0xBB, 0xAA, 0x99, 0x88,
        0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11, 0x00
    ];

    private static readonly byte[] BE256 =
    [
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
        0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
        0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
        0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F
    ];
    private static readonly byte[] LE256 =
    [
        0x1F, 0x1E, 0x1D, 0x1C, 0x1B, 0x1A, 0x19, 0x18,
        0x17, 0x16, 0x15, 0x14, 0x13, 0x12, 0x11, 0x10,
        0x0F, 0x0E, 0x0D, 0x0C, 0x0B, 0x0A, 0x09, 0x08,
        0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01, 0x00
    ];

    // ---------- UInt16Be / Int16Be ----------

    [Fact]
    public void UInt16Be_WriteTo_DefaultIsBigEndian()
    {
        UInt16Be v = new(U16);
        Span<byte> buf = stackalloc byte[2];
        v.WriteTo(buf);
        buf.ToArray().Should().Equal(BE16);
    }

    [Fact]
    public void UInt16Be_WriteTo_LittleEndianOutput()
    {
        UInt16Be v = new(U16);
        Span<byte> buf = stackalloc byte[2];
        v.WriteTo(buf, isBigEndian: false);
        buf.ToArray().Should().Equal(LE16);
    }

    [Fact]
    public void UInt16Be_TryWriteTo_BothOrders()
    {
        UInt16Be v = new(U16);
        Span<byte> big = stackalloc byte[2];
        Span<byte> little = stackalloc byte[2];
        v.TryWriteTo(big).Should().BeTrue();
        v.TryWriteTo(little, isBigEndian: false).Should().BeTrue();
        big.ToArray().Should().Equal(BE16);
        little.ToArray().Should().Equal(LE16);
    }

    [Fact]
    public void UInt16Be_TryWriteTo_TooShort_ReturnsFalse()
    {
        UInt16Be v = new(U16);
        byte[] buf = new byte[1];
        v.TryWriteTo(buf).Should().BeFalse();
        v.TryWriteTo(buf, isBigEndian: false).Should().BeFalse();
    }

    [Fact]
    public void UInt16Be_ToBytes_ByteArray_BothOrders()
    {
        UInt16Be v = new(U16);
        byte[] beOut = new byte[2];
        byte[] leOut = new byte[2];
        v.ToBytes(beOut);
        v.ToBytes(leOut, isBigEndian: false);
        beOut.Should().Equal(BE16);
        leOut.Should().Equal(LE16);
    }

    [Fact]
    public void UInt16Be_ToBytes_WithOffset_LittleEndian()
    {
        UInt16Be v = new(U16);
        byte[] buf = new byte[4]; // leading pad
        v.ToBytes(buf, offset: 2, isBigEndian: false);
        buf[2].Should().Be(LE16[0]);
        buf[3].Should().Be(LE16[1]);
        buf[0].Should().Be(0);
        buf[1].Should().Be(0);
    }

    [Fact]
    public void Int16Be_WriteTo_NegativeValue_BothOrders()
    {
        // 0xFFFE = -2 as a signed 16-bit value.
        Int16Be v = new((short)-2);
        Span<byte> big = stackalloc byte[2];
        Span<byte> little = stackalloc byte[2];
        v.WriteTo(big);
        v.WriteTo(little, isBigEndian: false);
        big.ToArray().Should().Equal([0xFF, 0xFE]);
        little.ToArray().Should().Equal([0xFE, 0xFF]);
    }

    // ---------- UInt32Be / Int32Be ----------

    [Fact]
    public void UInt32Be_WriteTo_BothOrders()
    {
        UInt32Be v = new(U32);
        Span<byte> big = stackalloc byte[4];
        Span<byte> little = stackalloc byte[4];
        v.WriteTo(big);
        v.WriteTo(little, isBigEndian: false);
        big.ToArray().Should().Equal(BE32);
        little.ToArray().Should().Equal(LE32);
    }

    [Fact]
    public void UInt32Be_TryWriteTo_LittleEndian()
    {
        UInt32Be v = new(U32);
        Span<byte> buf = stackalloc byte[4];
        v.TryWriteTo(buf, isBigEndian: false).Should().BeTrue();
        buf.ToArray().Should().Equal(LE32);
    }

    [Fact]
    public void UInt32Be_TryWriteTo_TooShort_ReturnsFalse()
    {
        UInt32Be v = new(U32);
        byte[] buf = new byte[3];
        v.TryWriteTo(buf).Should().BeFalse();
        v.TryWriteTo(buf, isBigEndian: false).Should().BeFalse();
    }

    [Fact]
    public void UInt32Be_ToBytes_IList_LittleEndian()
    {
        UInt32Be v = new(U32);
        List<byte> buf = [0, 0, 0, 0];
        v.ToBytes(buf, offset: 0, isBigEndian: false);
        buf.Should().Equal(LE32);
    }

    [Fact]
    public void Int32Be_WriteTo_NegativeValue()
    {
        // -256 = 0xFFFFFF00 big-endian; 00 FF FF FF little-endian.
        Int32Be v = new(-256);
        Span<byte> big = stackalloc byte[4];
        Span<byte> little = stackalloc byte[4];
        v.WriteTo(big);
        v.WriteTo(little, isBigEndian: false);
        big.ToArray().Should().Equal([0xFF, 0xFF, 0xFF, 0x00]);
        little.ToArray().Should().Equal([0x00, 0xFF, 0xFF, 0xFF]);
    }

    // ---------- UInt64Be / Int64Be ----------

    [Fact]
    public void UInt64Be_WriteTo_BothOrders()
    {
        UInt64Be v = new(U64);
        Span<byte> big = stackalloc byte[8];
        Span<byte> little = stackalloc byte[8];
        v.WriteTo(big);
        v.WriteTo(little, isBigEndian: false);
        big.ToArray().Should().Equal(BE64);
        little.ToArray().Should().Equal(LE64);
    }

    [Fact]
    public void UInt64Be_TryWriteTo_TooShort_ReturnsFalse()
    {
        UInt64Be v = new(U64);
        byte[] buf = new byte[7];
        v.TryWriteTo(buf).Should().BeFalse();
        v.TryWriteTo(buf, isBigEndian: false).Should().BeFalse();
    }

    [Fact]
    public void UInt64Be_ToBytes_IList_LittleEndian()
    {
        UInt64Be v = new(U64);
        List<byte> buf = new byte[8].ToList();
        v.ToBytes(buf, offset: 0, isBigEndian: false);
        buf.Should().Equal(LE64);
    }

    [Fact]
    public void Int64Be_WriteTo_NegativeValue()
    {
        Int64Be v = new(-2L);
        Span<byte> big = stackalloc byte[8];
        Span<byte> little = stackalloc byte[8];
        v.WriteTo(big);
        v.WriteTo(little, isBigEndian: false);
        big.ToArray().Should().Equal([0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFE]);
        little.ToArray().Should().Equal([0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]);
    }

    // ---------- UInt128Be / Int128Be ----------

    [Fact]
    public void UInt128Be_WriteTo_BothOrders()
    {
        UInt128Be v = new(BE128.AsSpan());
        Span<byte> big = stackalloc byte[16];
        Span<byte> little = stackalloc byte[16];
        v.WriteTo(big);
        v.WriteTo(little, isBigEndian: false);
        big.ToArray().Should().Equal(BE128);
        little.ToArray().Should().Equal(LE128);
    }

    [Fact]
    public void UInt128Be_TryWriteTo_TooShort_ReturnsFalse()
    {
        UInt128Be v = new(BE128.AsSpan());
        byte[] buf = new byte[15];
        v.TryWriteTo(buf).Should().BeFalse();
        v.TryWriteTo(buf, isBigEndian: false).Should().BeFalse();
    }

    [Fact]
    public void UInt128Be_ToBytes_IList_BothOrders()
    {
        UInt128Be v = new(BE128.AsSpan());
        List<byte> big = new byte[16].ToList();
        List<byte> little = new byte[16].ToList();
        v.ToBytes(big);
        v.ToBytes(little, offset: 0, isBigEndian: false);
        big.Should().Equal(BE128);
        little.Should().Equal(LE128);
    }

    [Fact]
    public void Int128Be_WriteTo_LittleEndian()
    {
        Int128Be v = new(BE128.AsSpan());
        Span<byte> buf = stackalloc byte[16];
        v.WriteTo(buf, isBigEndian: false);
        buf.ToArray().Should().Equal(LE128);
    }

    // ---------- UInt256Be / Int256Be ----------

    [Fact]
    public void UInt256Be_WriteTo_BothOrders()
    {
        UInt256Be v = new(BE256.AsSpan());
        Span<byte> big = stackalloc byte[32];
        Span<byte> little = stackalloc byte[32];
        v.WriteTo(big);
        v.WriteTo(little, isBigEndian: false);
        big.ToArray().Should().Equal(BE256);
        little.ToArray().Should().Equal(LE256);
    }

    [Fact]
    public void UInt256Be_ToBytes_ByteArray_LittleEndian()
    {
        UInt256Be v = new(BE256.AsSpan());
        byte[] buf = new byte[32];
        v.ToBytes(buf, offset: 0, isBigEndian: false);
        buf.Should().Equal(LE256);
    }

    [Fact]
    public void UInt256Be_TryWriteTo_TooShort_ReturnsFalse()
    {
        UInt256Be v = new(BE256.AsSpan());
        byte[] buf = new byte[31];
        v.TryWriteTo(buf).Should().BeFalse();
        v.TryWriteTo(buf, isBigEndian: false).Should().BeFalse();
    }

    [Fact]
    public void Int256Be_WriteTo_LittleEndian()
    {
        Int256Be v = new(BE256.AsSpan());
        Span<byte> buf = stackalloc byte[32];
        v.WriteTo(buf, isBigEndian: false);
        buf.ToArray().Should().Equal(LE256);
    }

    // ---------- UInt16Le / Int16Le ----------

    [Fact]
    public void UInt16Le_WriteTo_DefaultIsLittleEndian()
    {
        UInt16Le v = new(U16);
        Span<byte> buf = stackalloc byte[2];
        v.WriteTo(buf);
        buf.ToArray().Should().Equal(LE16);
    }

    [Fact]
    public void UInt16Le_WriteTo_BigEndianOutput()
    {
        UInt16Le v = new(U16);
        Span<byte> buf = stackalloc byte[2];
        v.WriteTo(buf, isBigEndian: true);
        buf.ToArray().Should().Equal(BE16);
    }

    [Fact]
    public void UInt16Le_TryWriteTo_BothOrders()
    {
        UInt16Le v = new(U16);
        Span<byte> little = stackalloc byte[2];
        Span<byte> big = stackalloc byte[2];
        v.TryWriteTo(little).Should().BeTrue();
        v.TryWriteTo(big, isBigEndian: true).Should().BeTrue();
        little.ToArray().Should().Equal(LE16);
        big.ToArray().Should().Equal(BE16);
    }

    [Fact]
    public void UInt16Le_TryWriteTo_TooShort_ReturnsFalse()
    {
        UInt16Le v = new(U16);
        byte[] buf = new byte[1];
        v.TryWriteTo(buf).Should().BeFalse();
        v.TryWriteTo(buf, isBigEndian: true).Should().BeFalse();
    }

    [Fact]
    public void UInt16Le_ToBytes_ByteArray_BothOrders()
    {
        UInt16Le v = new(U16);
        byte[] little = new byte[2];
        byte[] big = new byte[2];
        v.ToBytes(little);
        v.ToBytes(big, isBigEndian: true);
        little.Should().Equal(LE16);
        big.Should().Equal(BE16);
    }

    [Fact]
    public void Int16Le_WriteTo_NegativeValue_BothOrders()
    {
        Int16Le v = new((short)-2);
        Span<byte> little = stackalloc byte[2];
        Span<byte> big = stackalloc byte[2];
        v.WriteTo(little);
        v.WriteTo(big, isBigEndian: true);
        little.ToArray().Should().Equal([0xFE, 0xFF]);
        big.ToArray().Should().Equal([0xFF, 0xFE]);
    }

    // ---------- UInt32Le / Int32Le ----------

    [Fact]
    public void UInt32Le_WriteTo_BothOrders()
    {
        UInt32Le v = new(U32);
        Span<byte> little = stackalloc byte[4];
        Span<byte> big = stackalloc byte[4];
        v.WriteTo(little);
        v.WriteTo(big, isBigEndian: true);
        little.ToArray().Should().Equal(LE32);
        big.ToArray().Should().Equal(BE32);
    }

    [Fact]
    public void UInt32Le_ToBytes_ByteArray_BigEndian()
    {
        UInt32Le v = new(U32);
        byte[] buf = new byte[4];
        v.ToBytes(buf, isBigEndian: true);
        buf.Should().Equal(BE32);
    }

    [Fact]
    public void UInt32Le_TryWriteTo_TooShort_ReturnsFalse()
    {
        UInt32Le v = new(U32);
        byte[] buf = new byte[3];
        v.TryWriteTo(buf).Should().BeFalse();
        v.TryWriteTo(buf, isBigEndian: true).Should().BeFalse();
    }

    [Fact]
    public void Int32Le_WriteTo_NegativeValue()
    {
        Int32Le v = new(-256);
        Span<byte> little = stackalloc byte[4];
        Span<byte> big = stackalloc byte[4];
        v.WriteTo(little);
        v.WriteTo(big, isBigEndian: true);
        little.ToArray().Should().Equal([0x00, 0xFF, 0xFF, 0xFF]);
        big.ToArray().Should().Equal([0xFF, 0xFF, 0xFF, 0x00]);
    }

    // ---------- UInt64Le / Int64Le ----------

    [Fact]
    public void UInt64Le_WriteTo_BothOrders()
    {
        UInt64Le v = new(U64);
        Span<byte> little = stackalloc byte[8];
        Span<byte> big = stackalloc byte[8];
        v.WriteTo(little);
        v.WriteTo(big, isBigEndian: true);
        little.ToArray().Should().Equal(LE64);
        big.ToArray().Should().Equal(BE64);
    }

    [Fact]
    public void UInt64Le_ToBytes_ByteArray_BigEndian()
    {
        UInt64Le v = new(U64);
        byte[] buf = new byte[8];
        v.ToBytes(buf, isBigEndian: true);
        buf.Should().Equal(BE64);
    }

    [Fact]
    public void UInt64Le_TryWriteTo_TooShort_ReturnsFalse()
    {
        UInt64Le v = new(U64);
        byte[] buf = new byte[7];
        v.TryWriteTo(buf).Should().BeFalse();
        v.TryWriteTo(buf, isBigEndian: true).Should().BeFalse();
    }

    [Fact]
    public void Int64Le_WriteTo_NegativeValue()
    {
        Int64Le v = new(-2L);
        Span<byte> little = stackalloc byte[8];
        Span<byte> big = stackalloc byte[8];
        v.WriteTo(little);
        v.WriteTo(big, isBigEndian: true);
        little.ToArray().Should().Equal([0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]);
        big.ToArray().Should().Equal([0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFE]);
    }

    // ---------- UInt128Le / Int128Le ----------

    [Fact]
    public void UInt128Le_WriteTo_BothOrders()
    {
        UInt128Le v = new(LE128.AsSpan());
        Span<byte> little = stackalloc byte[16];
        Span<byte> big = stackalloc byte[16];
        v.WriteTo(little);
        v.WriteTo(big, isBigEndian: true);
        little.ToArray().Should().Equal(LE128);
        big.ToArray().Should().Equal(BE128);
    }

    [Fact]
    public void UInt128Le_ToBytes_ByteArray_BigEndian()
    {
        UInt128Le v = new(LE128.AsSpan());
        byte[] buf = new byte[16];
        v.ToBytes(buf, isBigEndian: true);
        buf.Should().Equal(BE128);
    }

    [Fact]
    public void UInt128Le_TryWriteTo_TooShort_ReturnsFalse()
    {
        UInt128Le v = new(LE128.AsSpan());
        byte[] buf = new byte[15];
        v.TryWriteTo(buf).Should().BeFalse();
        v.TryWriteTo(buf, isBigEndian: true).Should().BeFalse();
    }

    [Fact]
    public void Int128Le_WriteTo_BigEndian()
    {
        Int128Le v = new(LE128.AsSpan());
        Span<byte> buf = stackalloc byte[16];
        v.WriteTo(buf, isBigEndian: true);
        buf.ToArray().Should().Equal(BE128);
    }

    // ---------- UInt256Le / Int256Le ----------

    [Fact]
    public void UInt256Le_WriteTo_BothOrders()
    {
        UInt256Le v = new(LE256.AsSpan());
        Span<byte> little = stackalloc byte[32];
        Span<byte> big = stackalloc byte[32];
        v.WriteTo(little);
        v.WriteTo(big, isBigEndian: true);
        little.ToArray().Should().Equal(LE256);
        big.ToArray().Should().Equal(BE256);
    }

    [Fact]
    public void UInt256Le_ToBytes_ByteArray_BigEndian()
    {
        UInt256Le v = new(LE256.AsSpan());
        byte[] buf = new byte[32];
        v.ToBytes(buf, isBigEndian: true);
        buf.Should().Equal(BE256);
    }

    [Fact]
    public void UInt256Le_TryWriteTo_TooShort_ReturnsFalse()
    {
        UInt256Le v = new(LE256.AsSpan());
        byte[] buf = new byte[31];
        v.TryWriteTo(buf).Should().BeFalse();
        v.TryWriteTo(buf, isBigEndian: true).Should().BeFalse();
    }

    [Fact]
    public void Int256Le_WriteTo_BigEndian()
    {
        Int256Le v = new(LE256.AsSpan());
        Span<byte> buf = stackalloc byte[32];
        v.WriteTo(buf, isBigEndian: true);
        buf.ToArray().Should().Equal(BE256);
    }

    // ---------- Round-trip: constructor (with order) + exporter (with order) should
    //           preserve the original byte buffer regardless of the orientation used. ----------

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void UInt32Be_RoundTrip_AnyOrientation(bool ctorIsBigEndian, bool exportIsBigEndian)
    {
        byte[] src = ctorIsBigEndian ? BE32 : LE32;
        UInt32Be v = new(src.AsSpan(), isBigEndian: ctorIsBigEndian);
        byte[] round = new byte[4];
        v.ToBytes(round, offset: 0, isBigEndian: exportIsBigEndian);
        byte[] expected = exportIsBigEndian ? BE32 : LE32;
        round.Should().Equal(expected);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void UInt64Le_RoundTrip_AnyOrientation(bool ctorIsBigEndian, bool exportIsBigEndian)
    {
        byte[] src = ctorIsBigEndian ? BE64 : LE64;
        UInt64Le v = new(src.AsSpan(), isBigEndian: ctorIsBigEndian);
        byte[] round = new byte[8];
        v.ToBytes(round, offset: 0, isBigEndian: exportIsBigEndian);
        byte[] expected = exportIsBigEndian ? BE64 : LE64;
        round.Should().Equal(expected);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void UInt256Be_RoundTrip_WriteTo_AnyOrientation(bool ctorIsBigEndian, bool exportIsBigEndian)
    {
        byte[] src = ctorIsBigEndian ? BE256 : LE256;
        UInt256Be v = new(src.AsSpan(), isBigEndian: ctorIsBigEndian);
        Span<byte> round = stackalloc byte[32];
        v.WriteTo(round, isBigEndian: exportIsBigEndian);
        byte[] expected = exportIsBigEndian ? BE256 : LE256;
        round.ToArray().Should().Equal(expected);
    }

    // ---------- Cross-type: a Be value and an Le value constructed from the same logical
    //           numeric value must export identical bytes when asked for the same order. ----------

    [Fact]
    public void BeAndLe_ExportSameOrder_ProduceIdenticalBytes()
    {
        UInt32Be be = new(U32);
        UInt32Le le = new(U32);

        byte[] beBig = new byte[4];
        byte[] leBig = new byte[4];
        be.ToBytes(beBig);                         // default big-endian
        le.ToBytes(leBig, isBigEndian: true);      // explicit big-endian
        beBig.Should().Equal(leBig).And.Equal(BE32);

        byte[] beLittle = new byte[4];
        byte[] leLittle = new byte[4];
        be.ToBytes(beLittle, isBigEndian: false);
        le.ToBytes(leLittle);                      // default little-endian
        beLittle.Should().Equal(leLittle).And.Equal(LE32);
    }

    // ---------- Span-slice safety: passing an oversized destination must only touch the
    //           required prefix, leaving trailing bytes unchanged. ----------

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UInt64Be_WriteTo_OversizedSpan_LeavesTrailingBytesUntouched(bool isBigEndian)
    {
        UInt64Be v = new(U64);
        byte[] buf = new byte[32];
        for (int i = 0; i < 32; i++) buf[i] = 0xAA; // sentinel
        v.WriteTo(buf.AsSpan(), isBigEndian: isBigEndian);
        byte[] expected = isBigEndian ? BE64 : LE64;
        for (int i = 0; i < 8; i++) buf[i].Should().Be(expected[i]);
        for (int i = 8; i < 32; i++) buf[i].Should().Be(0xAA);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UInt128Le_ToBytes_OversizedArrayWithOffset_LeavesSurroundingBytesUntouched(bool isBigEndian)
    {
        UInt128Le v = new(LE128.AsSpan());
        byte[] buf = new byte[48];
        for (int i = 0; i < 48; i++) buf[i] = 0x77;
        v.ToBytes(buf, offset: 8, isBigEndian: isBigEndian);
        byte[] expected = isBigEndian ? BE128 : LE128;
        for (int i = 0; i < 8; i++) buf[i].Should().Be(0x77);
        for (int i = 0; i < 16; i++) buf[8 + i].Should().Be(expected[i]);
        for (int i = 24; i < 48; i++) buf[i].Should().Be(0x77);
    }

}
