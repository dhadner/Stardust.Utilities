using System;
using FluentAssertions;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Tests for the (ReadOnlySpan&lt;byte&gt; bytes, bool isBigEndian) constructors on each
/// Int/UInt *Be and *Le endian-qualified integer type. These constructors let callers
/// build an endian-typed value from a buffer whose order may differ from the type's
/// storage order, reversing the bytes when the source order does not match.
/// </summary>
public class EndianSpanWithOrderConstructorTests
{
    // Reference byte pattern used in every width. When interpreted big-endian the
    // "BE bytes" listed produce the natural numeric value; the "LE bytes" listed are
    // the same value with the byte order flipped.
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

    private static byte[] Reverse(byte[] source)
    {
        byte[] result = new byte[source.Length];
        for (int i = 0; i < source.Length; i++) result[i] = source[source.Length - 1 - i];
        return result;
    }

    // ---------- UInt16Be / Int16Be ----------

    [Fact]
    public void UInt16Be_SpanCtor_BigEndianDefault_MatchesBigEndianBuffer()
    {
        UInt16Be value = new(BE16.AsSpan());
        ((ushort)value).Should().Be(U16);
    }

    [Fact]
    public void UInt16Be_SpanCtor_BigEndianExplicit()
    {
        UInt16Be value = new(BE16.AsSpan(), isBigEndian: true);
        ((ushort)value).Should().Be(U16);
    }

    [Fact]
    public void UInt16Be_SpanCtor_LittleEndianSource_ReversesBytes()
    {
        UInt16Be value = new(LE16.AsSpan(), isBigEndian: false);
        ((ushort)value).Should().Be(U16);
    }

    [Fact]
    public void UInt16Be_SpanCtor_StorageOrderIsBigEndian_BigEndianSource()
    {
        UInt16Be value = new(BE16.AsSpan(), isBigEndian: true);
        Span<byte> dst = stackalloc byte[2];
        value.WriteTo(dst);
        dst.ToArray().Should().Equal(BE16);
    }

    [Fact]
    public void UInt16Be_SpanCtor_StorageOrderIsBigEndian_LittleEndianSource()
    {
        UInt16Be value = new(LE16.AsSpan(), isBigEndian: false);
        Span<byte> dst = stackalloc byte[2];
        value.WriteTo(dst);
        dst.ToArray().Should().Equal(BE16);
    }

    [Fact]
    public void UInt16Be_SpanCtor_TooShort_Throws()
    {
        byte[] tooShort = [0x00];
        Action act = () => { _ = new UInt16Be(tooShort.AsSpan(), isBigEndian: true); };
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Int16Be_SpanCtor_BigEndianDefault()
    {
        Int16Be value = new(BE16.AsSpan());
        ((short)value).Should().Be(I16);
    }

    [Fact]
    public void Int16Be_SpanCtor_LittleEndianSource()
    {
        Int16Be value = new(LE16.AsSpan(), isBigEndian: false);
        ((short)value).Should().Be(I16);
    }

    [Fact]
    public void Int16Be_SpanCtor_NegativeValue_BigEndianSource()
    {
        // 0xFFFE = -2 as a signed 16-bit big-endian value.
        byte[] be = [0xFF, 0xFE];
        Int16Be value = new(be.AsSpan(), isBigEndian: true);
        ((short)value).Should().Be(-2);
    }

    [Fact]
    public void Int16Be_SpanCtor_NegativeValue_LittleEndianSource()
    {
        byte[] le = [0xFE, 0xFF]; // same -2 value but in LE byte order
        Int16Be value = new(le.AsSpan(), isBigEndian: false);
        ((short)value).Should().Be(-2);
    }

    [Fact]
    public void Int16Be_SpanCtor_TooShort_Throws()
    {
        byte[] tooShort = [0x00];
        Action act = () => { _ = new Int16Be(tooShort.AsSpan(), isBigEndian: false); };
        act.Should().Throw<ArgumentException>();
    }

    // ---------- UInt32Be / Int32Be ----------

    [Fact]
    public void UInt32Be_SpanCtor_BigEndianDefault()
    {
        UInt32Be value = new(BE32.AsSpan());
        ((uint)value).Should().Be(U32);
    }

    [Fact]
    public void UInt32Be_SpanCtor_LittleEndianSource_ReversesBytes()
    {
        UInt32Be value = new(LE32.AsSpan(), isBigEndian: false);
        ((uint)value).Should().Be(U32);
    }

    [Fact]
    public void UInt32Be_SpanCtor_StorageOrderIsBigEndian()
    {
        UInt32Be value = new(LE32.AsSpan(), isBigEndian: false);
        Span<byte> dst = stackalloc byte[4];
        value.WriteTo(dst);
        dst.ToArray().Should().Equal(BE32);
    }

    [Fact]
    public void UInt32Be_SpanCtor_TooShort_Throws()
    {
        byte[] tooShort = new byte[3];
        Action act = () => { _ = new UInt32Be(tooShort.AsSpan(), isBigEndian: true); };
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Int32Be_SpanCtor_BigEndianDefault()
    {
        Int32Be value = new(BE32.AsSpan());
        ((int)value).Should().Be(I32);
    }

    [Fact]
    public void Int32Be_SpanCtor_LittleEndianSource()
    {
        Int32Be value = new(LE32.AsSpan(), isBigEndian: false);
        ((int)value).Should().Be(I32);
    }

    [Fact]
    public void Int32Be_SpanCtor_NegativeValue_BothOrders()
    {
        // -1 in 32-bit = 0xFFFFFFFF in either endianness (palindrome). Use a distinct negative:
        // -256 = 0xFFFFFF00 in BE order; bytes 00 FF FF FF in LE order.
        byte[] be = [0xFF, 0xFF, 0xFF, 0x00];
        byte[] le = [0x00, 0xFF, 0xFF, 0xFF];
        ((int)new Int32Be(be.AsSpan(), isBigEndian: true)).Should().Be(-256);
        ((int)new Int32Be(le.AsSpan(), isBigEndian: false)).Should().Be(-256);
    }

    // ---------- UInt64Be / Int64Be ----------

    [Fact]
    public void UInt64Be_SpanCtor_BigEndianDefault()
    {
        UInt64Be value = new(BE64.AsSpan());
        ((ulong)value).Should().Be(U64);
    }

    [Fact]
    public void UInt64Be_SpanCtor_LittleEndianSource_ReversesBytes()
    {
        UInt64Be value = new(LE64.AsSpan(), isBigEndian: false);
        ((ulong)value).Should().Be(U64);
    }

    [Fact]
    public void UInt64Be_SpanCtor_StorageOrderIsBigEndian()
    {
        UInt64Be value = new(LE64.AsSpan(), isBigEndian: false);
        Span<byte> dst = stackalloc byte[8];
        value.WriteTo(dst);
        dst.ToArray().Should().Equal(BE64);
    }

    [Fact]
    public void UInt64Be_SpanCtor_TooShort_Throws()
    {
        byte[] tooShort = new byte[7];
        Action act = () => { _ = new UInt64Be(tooShort.AsSpan(), isBigEndian: false); };
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Int64Be_SpanCtor_BigEndianDefault()
    {
        Int64Be value = new(BE64.AsSpan());
        ((long)value).Should().Be(I64);
    }

    [Fact]
    public void Int64Be_SpanCtor_LittleEndianSource()
    {
        Int64Be value = new(LE64.AsSpan(), isBigEndian: false);
        ((long)value).Should().Be(I64);
    }

    [Fact]
    public void Int64Be_SpanCtor_NegativeValue_BothOrders()
    {
        // -1 in 64-bit = all 0xFF (palindrome). Use -2: 0xFFFF...FE in BE, FE FF FF... in LE.
        byte[] be = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFE];
        byte[] le = [0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
        ((long)new Int64Be(be.AsSpan(), isBigEndian: true)).Should().Be(-2L);
        ((long)new Int64Be(le.AsSpan(), isBigEndian: false)).Should().Be(-2L);
    }

    // ---------- UInt128Be / Int128Be ----------

    [Fact]
    public void UInt128Be_SpanCtor_BigEndianDefault()
    {
        UInt128Be be = new(BE128.AsSpan());
        Span<byte> dst = stackalloc byte[16];
        be.WriteTo(dst);
        dst.ToArray().Should().Equal(BE128);
    }

    [Fact]
    public void UInt128Be_SpanCtor_LittleEndianSource_ReversesBytes()
    {
        UInt128Be be = new(LE128.AsSpan(), isBigEndian: false);
        Span<byte> dst = stackalloc byte[16];
        be.WriteTo(dst);
        dst.ToArray().Should().Equal(BE128);
    }

    [Fact]
    public void UInt128Be_SpanCtor_TooShort_Throws()
    {
        byte[] tooShort = new byte[15];
        Action act = () => { _ = new UInt128Be(tooShort.AsSpan(), isBigEndian: true); };
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Int128Be_SpanCtor_BigEndianDefault()
    {
        Int128Be be = new(BE128.AsSpan());
        Span<byte> dst = stackalloc byte[16];
        be.WriteTo(dst);
        dst.ToArray().Should().Equal(BE128);
    }

    [Fact]
    public void Int128Be_SpanCtor_LittleEndianSource()
    {
        Int128Be be = new(LE128.AsSpan(), isBigEndian: false);
        Span<byte> dst = stackalloc byte[16];
        be.WriteTo(dst);
        dst.ToArray().Should().Equal(BE128);
    }

    // ---------- UInt256Be / Int256Be ----------

    [Fact]
    public void UInt256Be_SpanCtor_BigEndianDefault()
    {
        UInt256Be be = new(BE256.AsSpan());
        Span<byte> dst = stackalloc byte[32];
        be.WriteTo(dst);
        dst.ToArray().Should().Equal(BE256);
    }

    [Fact]
    public void UInt256Be_SpanCtor_LittleEndianSource_ReversesBytes()
    {
        UInt256Be be = new(LE256.AsSpan(), isBigEndian: false);
        Span<byte> dst = stackalloc byte[32];
        be.WriteTo(dst);
        dst.ToArray().Should().Equal(BE256);
    }

    [Fact]
    public void UInt256Be_SpanCtor_TooShort_Throws()
    {
        byte[] tooShort = new byte[31];
        Action act = () => { _ = new UInt256Be(tooShort.AsSpan(), isBigEndian: false); };
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Int256Be_SpanCtor_BigEndianDefault()
    {
        Int256Be be = new(BE256.AsSpan());
        Span<byte> dst = stackalloc byte[32];
        be.WriteTo(dst);
        dst.ToArray().Should().Equal(BE256);
    }

    [Fact]
    public void Int256Be_SpanCtor_LittleEndianSource()
    {
        Int256Be be = new(LE256.AsSpan(), isBigEndian: false);
        Span<byte> dst = stackalloc byte[32];
        be.WriteTo(dst);
        dst.ToArray().Should().Equal(BE256);
    }

    // ---------- UInt16Le / Int16Le ----------

    [Fact]
    public void UInt16Le_SpanCtor_LittleEndianDefault_MatchesLittleEndianBuffer()
    {
        UInt16Le value = new(LE16.AsSpan());
        ((ushort)value).Should().Be(U16);
    }

    [Fact]
    public void UInt16Le_SpanCtor_LittleEndianExplicit()
    {
        UInt16Le value = new(LE16.AsSpan(), isBigEndian: false);
        ((ushort)value).Should().Be(U16);
    }

    [Fact]
    public void UInt16Le_SpanCtor_BigEndianSource_ReversesBytes()
    {
        UInt16Le value = new(BE16.AsSpan(), isBigEndian: true);
        ((ushort)value).Should().Be(U16);
    }

    [Fact]
    public void UInt16Le_SpanCtor_StorageOrderIsLittleEndian_LittleEndianSource()
    {
        UInt16Le value = new(LE16.AsSpan(), isBigEndian: false);
        Span<byte> dst = stackalloc byte[2];
        value.WriteTo(dst);
        dst.ToArray().Should().Equal(LE16);
    }

    [Fact]
    public void UInt16Le_SpanCtor_StorageOrderIsLittleEndian_BigEndianSource()
    {
        UInt16Le value = new(BE16.AsSpan(), isBigEndian: true);
        Span<byte> dst = stackalloc byte[2];
        value.WriteTo(dst);
        dst.ToArray().Should().Equal(LE16);
    }

    [Fact]
    public void UInt16Le_SpanCtor_TooShort_Throws()
    {
        byte[] tooShort = [0x00];
        Action act = () => { _ = new UInt16Le(tooShort.AsSpan(), isBigEndian: false); };
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Int16Le_SpanCtor_LittleEndianDefault()
    {
        Int16Le value = new(LE16.AsSpan());
        ((short)value).Should().Be(I16);
    }

    [Fact]
    public void Int16Le_SpanCtor_BigEndianSource()
    {
        Int16Le value = new(BE16.AsSpan(), isBigEndian: true);
        ((short)value).Should().Be(I16);
    }

    [Fact]
    public void Int16Le_SpanCtor_NegativeValue_BothOrders()
    {
        byte[] le = [0xFE, 0xFF]; // -2 in LE
        byte[] be = [0xFF, 0xFE]; // -2 in BE
        ((short)new Int16Le(le.AsSpan(), isBigEndian: false)).Should().Be(-2);
        ((short)new Int16Le(be.AsSpan(), isBigEndian: true)).Should().Be(-2);
    }

    // ---------- UInt32Le / Int32Le ----------

    [Fact]
    public void UInt32Le_SpanCtor_LittleEndianDefault()
    {
        UInt32Le value = new(LE32.AsSpan());
        ((uint)value).Should().Be(U32);
    }

    [Fact]
    public void UInt32Le_SpanCtor_BigEndianSource_ReversesBytes()
    {
        UInt32Le value = new(BE32.AsSpan(), isBigEndian: true);
        ((uint)value).Should().Be(U32);
    }

    [Fact]
    public void UInt32Le_SpanCtor_StorageOrderIsLittleEndian()
    {
        UInt32Le value = new(BE32.AsSpan(), isBigEndian: true);
        Span<byte> dst = stackalloc byte[4];
        value.WriteTo(dst);
        dst.ToArray().Should().Equal(LE32);
    }

    [Fact]
    public void UInt32Le_SpanCtor_TooShort_Throws()
    {
        byte[] tooShort = new byte[3];
        Action act = () => { _ = new UInt32Le(tooShort.AsSpan(), isBigEndian: true); };
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Int32Le_SpanCtor_LittleEndianDefault()
    {
        Int32Le value = new(LE32.AsSpan());
        ((int)value).Should().Be(I32);
    }

    [Fact]
    public void Int32Le_SpanCtor_BigEndianSource()
    {
        Int32Le value = new(BE32.AsSpan(), isBigEndian: true);
        ((int)value).Should().Be(I32);
    }

    [Fact]
    public void Int32Le_SpanCtor_NegativeValue_BothOrders()
    {
        byte[] be = [0xFF, 0xFF, 0xFF, 0x00]; // -256 in BE
        byte[] le = [0x00, 0xFF, 0xFF, 0xFF]; // -256 in LE
        ((int)new Int32Le(le.AsSpan(), isBigEndian: false)).Should().Be(-256);
        ((int)new Int32Le(be.AsSpan(), isBigEndian: true)).Should().Be(-256);
    }

    // ---------- UInt64Le / Int64Le ----------

    [Fact]
    public void UInt64Le_SpanCtor_LittleEndianDefault()
    {
        UInt64Le value = new(LE64.AsSpan());
        ((ulong)value).Should().Be(U64);
    }

    [Fact]
    public void UInt64Le_SpanCtor_BigEndianSource_ReversesBytes()
    {
        UInt64Le value = new(BE64.AsSpan(), isBigEndian: true);
        ((ulong)value).Should().Be(U64);
    }

    [Fact]
    public void UInt64Le_SpanCtor_StorageOrderIsLittleEndian()
    {
        UInt64Le value = new(BE64.AsSpan(), isBigEndian: true);
        Span<byte> dst = stackalloc byte[8];
        value.WriteTo(dst);
        dst.ToArray().Should().Equal(LE64);
    }

    [Fact]
    public void UInt64Le_SpanCtor_TooShort_Throws()
    {
        byte[] tooShort = new byte[7];
        Action act = () => { _ = new UInt64Le(tooShort.AsSpan(), isBigEndian: false); };
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Int64Le_SpanCtor_LittleEndianDefault()
    {
        Int64Le value = new(LE64.AsSpan());
        ((long)value).Should().Be(I64);
    }

    [Fact]
    public void Int64Le_SpanCtor_BigEndianSource()
    {
        Int64Le value = new(BE64.AsSpan(), isBigEndian: true);
        ((long)value).Should().Be(I64);
    }

    [Fact]
    public void Int64Le_SpanCtor_NegativeValue_BothOrders()
    {
        byte[] be = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFE];
        byte[] le = [0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
        ((long)new Int64Le(le.AsSpan(), isBigEndian: false)).Should().Be(-2L);
        ((long)new Int64Le(be.AsSpan(), isBigEndian: true)).Should().Be(-2L);
    }

    // ---------- UInt128Le / Int128Le ----------

    [Fact]
    public void UInt128Le_SpanCtor_LittleEndianDefault()
    {
        UInt128Le le = new(LE128.AsSpan());
        Span<byte> dst = stackalloc byte[16];
        le.WriteTo(dst);
        dst.ToArray().Should().Equal(LE128);
    }

    [Fact]
    public void UInt128Le_SpanCtor_BigEndianSource_ReversesBytes()
    {
        UInt128Le le = new(BE128.AsSpan(), isBigEndian: true);
        Span<byte> dst = stackalloc byte[16];
        le.WriteTo(dst);
        dst.ToArray().Should().Equal(LE128);
    }

    [Fact]
    public void UInt128Le_SpanCtor_TooShort_Throws()
    {
        byte[] tooShort = new byte[15];
        Action act = () => { _ = new UInt128Le(tooShort.AsSpan(), isBigEndian: true); };
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Int128Le_SpanCtor_LittleEndianDefault()
    {
        Int128Le le = new(LE128.AsSpan());
        Span<byte> dst = stackalloc byte[16];
        le.WriteTo(dst);
        dst.ToArray().Should().Equal(LE128);
    }

    [Fact]
    public void Int128Le_SpanCtor_BigEndianSource()
    {
        Int128Le le = new(BE128.AsSpan(), isBigEndian: true);
        Span<byte> dst = stackalloc byte[16];
        le.WriteTo(dst);
        dst.ToArray().Should().Equal(LE128);
    }

    // ---------- UInt256Le / Int256Le ----------

    [Fact]
    public void UInt256Le_SpanCtor_LittleEndianDefault()
    {
        UInt256Le le = new(LE256.AsSpan());
        Span<byte> dst = stackalloc byte[32];
        le.WriteTo(dst);
        dst.ToArray().Should().Equal(LE256);
    }

    [Fact]
    public void UInt256Le_SpanCtor_BigEndianSource_ReversesBytes()
    {
        UInt256Le le = new(BE256.AsSpan(), isBigEndian: true);
        Span<byte> dst = stackalloc byte[32];
        le.WriteTo(dst);
        dst.ToArray().Should().Equal(LE256);
    }

    [Fact]
    public void UInt256Le_SpanCtor_TooShort_Throws()
    {
        byte[] tooShort = new byte[31];
        Action act = () => { _ = new UInt256Le(tooShort.AsSpan(), isBigEndian: false); };
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Int256Le_SpanCtor_LittleEndianDefault()
    {
        Int256Le le = new(LE256.AsSpan());
        Span<byte> dst = stackalloc byte[32];
        le.WriteTo(dst);
        dst.ToArray().Should().Equal(LE256);
    }

    [Fact]
    public void Int256Le_SpanCtor_BigEndianSource()
    {
        Int256Le le = new(BE256.AsSpan(), isBigEndian: true);
        Span<byte> dst = stackalloc byte[32];
        le.WriteTo(dst);
        dst.ToArray().Should().Equal(LE256);
    }

    // ---------- Cross-type equivalence: Be and Le types built from matched source buffers
    //           must represent the same numeric value. ----------

    [Fact]
    public void Be16AndLe16_FromOppositeOrderBuffers_AgreeNumerically()
    {
        UInt16Be be = new(LE16.AsSpan(), isBigEndian: false);
        UInt16Le le = new(BE16.AsSpan(), isBigEndian: true);
        ((ushort)be).Should().Be((ushort)le);
        ((ushort)be).Should().Be(U16);
    }

    [Fact]
    public void Be32AndLe32_FromOppositeOrderBuffers_AgreeNumerically()
    {
        UInt32Be be = new(LE32.AsSpan(), isBigEndian: false);
        UInt32Le le = new(BE32.AsSpan(), isBigEndian: true);
        ((uint)be).Should().Be((uint)le);
        ((uint)be).Should().Be(U32);
    }

    [Fact]
    public void Be64AndLe64_FromOppositeOrderBuffers_AgreeNumerically()
    {
        UInt64Be be = new(LE64.AsSpan(), isBigEndian: false);
        UInt64Le le = new(BE64.AsSpan(), isBigEndian: true);
        ((ulong)be).Should().Be((ulong)le);
        ((ulong)be).Should().Be(U64);
    }

    [Fact]
    public void Int64SignedRoundTrip_LE_to_Int64Be_IsBigEndian_False()
    {
        // Write a native long as little-endian bytes, build Int64Be from those LE bytes,
        // and assert the signed value is preserved. Exercises both the explicit
        // isBigEndian:false path and a wider range of byte patterns than the constants.
        foreach (long v in new[] { long.MinValue, -1L, 0L, 1L, 0x00DEADBEEFCAFE01L, long.MaxValue })
        {
            byte[] le = BitConverter.GetBytes(v);
            if (!BitConverter.IsLittleEndian) Array.Reverse(le);
            Int64Be via = new(le.AsSpan(), isBigEndian: false);
            ((long)via).Should().Be(v);
        }
    }

    [Fact]
    public void Int32SignedRoundTrip_BE_to_Int32Le_IsBigEndian_True()
    {
        foreach (int v in new[] { int.MinValue, -1, 0, 1, 0x0DEADBEE, int.MaxValue })
        {
            byte[] be = BitConverter.GetBytes(v);
            if (BitConverter.IsLittleEndian) Array.Reverse(be);
            Int32Le via = new(be.AsSpan(), isBigEndian: true);
            ((int)via).Should().Be(v);
        }
    }

    [Fact]
    public void UInt256_RoundTrip_ReversedBuffer_PreservesValue()
    {
        // Feeding LE bytes to the Be ctor (isBigEndian: false) and feeding BE bytes to
        // the Le ctor (isBigEndian: true) must produce the same numeric value as
        // reading the same bytes through the natural-order ctor.
        UInt256Be beNatural = new(BE256.AsSpan());
        UInt256Be beReversed = new(LE256.AsSpan(), isBigEndian: false);
        UInt256Le leNatural = new(LE256.AsSpan());
        UInt256Le leReversed = new(BE256.AsSpan(), isBigEndian: true);

        ((UInt256)beNatural).Should().Be((UInt256)beReversed);
        ((UInt256)leNatural).Should().Be((UInt256)leReversed);
        ((UInt256)beNatural).Should().Be((UInt256)leNatural);
    }

    // ---------- Span-slice safety: passing an oversized buffer must copy only the
    //           required prefix so trailing bytes never leak into the value. ----------

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UInt64Be_SpanCtor_OversizedBuffer_ReadsOnlyFirstEightBytes(bool isBigEndian)
    {
        byte[] oversized = new byte[32];
        byte[] src = isBigEndian ? BE64 : LE64;
        Array.Copy(src, 0, oversized, 0, 8);
        for (int i = 8; i < 32; i++) oversized[i] = 0xAA; // trailing noise

        UInt64Be value = new(oversized.AsSpan(), isBigEndian: isBigEndian);
        ((ulong)value).Should().Be(U64);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UInt64Le_SpanCtor_OversizedBuffer_ReadsOnlyFirstEightBytes(bool isBigEndian)
    {
        byte[] oversized = new byte[32];
        byte[] src = isBigEndian ? BE64 : LE64;
        Array.Copy(src, 0, oversized, 0, 8);
        for (int i = 8; i < 32; i++) oversized[i] = 0x55;

        UInt64Le value = new(oversized.AsSpan(), isBigEndian: isBigEndian);
        ((ulong)value).Should().Be(U64);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UInt128Be_SpanCtor_OversizedBuffer_ReadsOnlyFirst16Bytes(bool isBigEndian)
    {
        byte[] oversized = new byte[48];
        byte[] src = isBigEndian ? BE128 : LE128;
        Array.Copy(src, 0, oversized, 0, 16);
        for (int i = 16; i < 48; i++) oversized[i] = 0xCC;

        UInt128Be value = new(oversized.AsSpan(), isBigEndian: isBigEndian);
        Span<byte> dst = stackalloc byte[16];
        value.WriteTo(dst);
        dst.ToArray().Should().Equal(BE128);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UInt256Le_SpanCtor_OversizedBuffer_ReadsOnlyFirst32Bytes(bool isBigEndian)
    {
        byte[] oversized = new byte[64];
        byte[] src = isBigEndian ? BE256 : LE256;
        Array.Copy(src, 0, oversized, 0, 32);
        for (int i = 32; i < 64; i++) oversized[i] = 0x99;

        UInt256Le value = new(oversized.AsSpan(), isBigEndian: isBigEndian);
        Span<byte> dst = stackalloc byte[32];
        value.WriteTo(dst);
        dst.ToArray().Should().Equal(LE256);
    }

    // ---------- Exhaustive: reverse helper correctness against a manually-reversed
    //           buffer for every width, so a future implementation change that
    //           silently shuffles a different number of bytes would fail a test. ----------

    [Fact]
    public void AllBeTypes_FromReversedBuffer_MatchNativeOrder()
    {
        ((ushort)new UInt16Be(Reverse(BE16).AsSpan(), isBigEndian: false))
            .Should().Be((ushort)new UInt16Be(BE16.AsSpan()));

        ((short)new Int16Be(Reverse(BE16).AsSpan(), isBigEndian: false))
            .Should().Be((short)new Int16Be(BE16.AsSpan()));

        ((uint)new UInt32Be(Reverse(BE32).AsSpan(), isBigEndian: false))
            .Should().Be((uint)new UInt32Be(BE32.AsSpan()));

        ((int)new Int32Be(Reverse(BE32).AsSpan(), isBigEndian: false))
            .Should().Be((int)new Int32Be(BE32.AsSpan()));

        ((ulong)new UInt64Be(Reverse(BE64).AsSpan(), isBigEndian: false))
            .Should().Be((ulong)new UInt64Be(BE64.AsSpan()));

        ((long)new Int64Be(Reverse(BE64).AsSpan(), isBigEndian: false))
            .Should().Be((long)new Int64Be(BE64.AsSpan()));

        new UInt128Be(Reverse(BE128).AsSpan(), isBigEndian: false)
            .Should().Be(new UInt128Be(BE128.AsSpan()));

        new Int128Be(Reverse(BE128).AsSpan(), isBigEndian: false)
            .Should().Be(new Int128Be(BE128.AsSpan()));

        new UInt256Be(Reverse(BE256).AsSpan(), isBigEndian: false)
            .Should().Be(new UInt256Be(BE256.AsSpan()));

        new Int256Be(Reverse(BE256).AsSpan(), isBigEndian: false)
            .Should().Be(new Int256Be(BE256.AsSpan()));
    }

    [Fact]
    public void AllLeTypes_FromReversedBuffer_MatchNativeOrder()
    {
        ((ushort)new UInt16Le(Reverse(LE16).AsSpan(), isBigEndian: true))
            .Should().Be((ushort)new UInt16Le(LE16.AsSpan()));

        ((short)new Int16Le(Reverse(LE16).AsSpan(), isBigEndian: true))
            .Should().Be((short)new Int16Le(LE16.AsSpan()));

        ((uint)new UInt32Le(Reverse(LE32).AsSpan(), isBigEndian: true))
            .Should().Be((uint)new UInt32Le(LE32.AsSpan()));

        ((int)new Int32Le(Reverse(LE32).AsSpan(), isBigEndian: true))
            .Should().Be((int)new Int32Le(LE32.AsSpan()));

        ((ulong)new UInt64Le(Reverse(LE64).AsSpan(), isBigEndian: true))
            .Should().Be((ulong)new UInt64Le(LE64.AsSpan()));

        ((long)new Int64Le(Reverse(LE64).AsSpan(), isBigEndian: true))
            .Should().Be((long)new Int64Le(LE64.AsSpan()));

        new UInt128Le(Reverse(LE128).AsSpan(), isBigEndian: true)
            .Should().Be(new UInt128Le(LE128.AsSpan()));

        new Int128Le(Reverse(LE128).AsSpan(), isBigEndian: true)
            .Should().Be(new Int128Le(LE128.AsSpan()));

        new UInt256Le(Reverse(LE256).AsSpan(), isBigEndian: true)
            .Should().Be(new UInt256Le(LE256.AsSpan()));

        new Int256Le(Reverse(LE256).AsSpan(), isBigEndian: true)
            .Should().Be(new Int256Le(LE256.AsSpan()));
    }
}
