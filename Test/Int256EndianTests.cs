using System;
using System.Globalization;
using FluentAssertions;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Tests for <see cref="Int256Be"/> and <see cref="Int256Le"/> — the signed 256-bit
/// big-endian and little-endian wire types. Each test verifies that the byte layout
/// is correct for the stated endianness and that round-tripping through the native
/// <see cref="Int256"/> type preserves the value exactly.
/// </summary>
public class Int256EndianTests
{
    // ── Helpers ──────────────────────────────────────────────────────────

    private static Int256 MakeInt256(long hi3, long hi2, long hi1, long lo0)
        => (Int256)new UInt256((ulong)hi3, (ulong)hi2, (ulong)hi1, (ulong)lo0);

    // ── Int256Be ─────────────────────────────────────────────────────────

    [Fact]
    public void Int256Be_PositiveValue_ByteLayoutIsBigEndian()
    {
        // Value = 1 should have the last byte = 0x01 and all others 0x00 (big-endian)
        Int256Be be = (Int256Be)(Int256)UInt256.One;
        Span<byte> buf = stackalloc byte[32];
        be.WriteTo(buf);

        buf[31].Should().Be(0x01);
        for (int i = 0; i < 31; i++) buf[i].Should().Be(0x00);
    }

    [Fact]
    public void Int256Be_MinusOne_AllBytesFf()
    {
        // -1 in two's complement is all 0xFF bytes
        Int256Be be = (Int256Be)Int256.NegativeOne;
        Span<byte> buf = stackalloc byte[32];
        be.WriteTo(buf);

        for (int i = 0; i < 32; i++) buf[i].Should().Be(0xFF);
    }

    [Fact]
    public void Int256Be_RoundTrip_ThroughBytes()
    {
        Int256 original = MakeInt256(0x0102030405060708L, 0x090A0B0C0D0E0F10L,
                                     0x1112131415161718L, 0x191A1B1C1D1E1F20L);
        Int256Be be = (Int256Be)original;
        Span<byte> buf = stackalloc byte[32];
        be.WriteTo(buf);

        Int256Be restored = new Int256Be(buf);
        ((Int256)restored).Should().Be(original);
    }

    [Fact]
    public void Int256Be_RoundTrip_NegativeValue()
    {
        Int256 original = Int256.MinValue;
        Int256Be be = (Int256Be)original;
        Span<byte> buf = stackalloc byte[32];
        be.WriteTo(buf);

        // MinValue is 1000...0 in binary; big-endian: first byte is 0x80, rest 0x00
        buf[0].Should().Be(0x80);
        for (int i = 1; i < 32; i++) buf[i].Should().Be(0x00);

        Int256Be restored = new Int256Be(buf);
        ((Int256)restored).Should().Be(original);
    }

    [Fact]
    public void Int256Be_ByteOrderVsLeByteOrder_AreMirrored()
    {
        // A positive value's big-endian bytes should be the reverse of the little-endian bytes.
        Int256 value = (Int256)new UInt256(0x0102030405060708UL, 0x090A0B0C0D0E0F10UL,
                                           0x1112131415161718UL, 0x191A1B1C1D1E1F20UL);
        Span<byte> beBuf = stackalloc byte[32];
        Span<byte> leBuf = stackalloc byte[32];
        ((Int256Be)value).WriteTo(beBuf);
        ((Int256Le)value).WriteTo(leBuf);

        for (int i = 0; i < 32; i++)
            beBuf[i].Should().Be(leBuf[31 - i], $"byte {i} should be mirrored");
    }

    [Fact]
    public void Int256Be_Arithmetic_DelegatesSignedSemantics()
    {
        Int256Be a = (Int256Be)(Int256)(-10);
        Int256Be b = (Int256Be)(Int256)3;
        ((Int256)(a + b)).Should().Be((Int256)(-7));
        ((Int256)(a - b)).Should().Be((Int256)(-13));
        ((Int256)(a * b)).Should().Be((Int256)(-30));
        ((Int256)(a / b)).Should().Be((Int256)(-3));
        ((Int256)(a % b)).Should().Be((Int256)(-1));
    }

    [Fact]
    public void Int256Be_Comparison_IsSignedNotUnsigned()
    {
        Int256Be neg = (Int256Be)Int256.NegativeOne;   // -1
        Int256Be pos = (Int256Be)(Int256)UInt256.One; // +1

        (neg < pos).Should().BeTrue("signed: -1 < 1");
        (pos > neg).Should().BeTrue();
        (neg <= pos).Should().BeTrue();
        (neg == neg).Should().BeTrue();
    }

    [Fact]
    public void Int256Be_ShiftRight_IsArithmetic()
    {
        // -256 >> 4 should give -16 (arithmetic, sign-extending)
        Int256Be val = (Int256Be)(Int256)(-256);
        Int256Be shifted = val >> 4;
        ((Int256)shifted).Should().Be((Int256)(-16));
    }

    [Fact]
    public void Int256Be_ShiftRightUnsigned_IsLogical()
    {
        // -1 >>> 1 should give MaxValue / 2 (logical, zero-filling)
        Int256Be val = (Int256Be)Int256.NegativeOne;
        Int256Be shifted = val >>> 1;
        // Result should be Int256.MaxValue (all bits set except sign bit)
        ((Int256)shifted).Should().Be(Int256.MaxValue);
    }

    [Fact]
    public void Int256Be_Parse_DecimalRoundTrip()
    {
        string s = "-57896044618658097711785492504343953926634992332820282019728792003956564819968";
        Int256Be val = Int256Be.Parse(s);
        val.ToString().Should().Be(s);
    }

    [Fact]
    public void Int256Be_Parse_PositiveDecimalRoundTrip()
    {
        string s = "42";
        Int256Be val = Int256Be.Parse(s);
        val.ToString().Should().Be(s);
    }

    [Fact]
    public void Int256Be_TryWriteTo_ReturnsFalseWhenTooShort()
    {
        Int256Be be = (Int256Be)(Int256)UInt256.One;
        Span<byte> tooShort = stackalloc byte[16];
        be.TryWriteTo(tooShort).Should().BeFalse();
    }

    [Fact]
    public void Int256Be_Negation_IsCorrect()
    {
        Int256Be val = (Int256Be)(Int256)100;
        Int256Be neg = -val;
        ((Int256)neg).Should().Be((Int256)(-100));
    }

    [Fact]
    public void Int256Be_IncrementDecrement()
    {
        Int256Be val = (Int256Be)Int256.NegativeOne;
        ((Int256)(val + (Int256Be)(Int256)UInt256.One)).Should().Be(Int256.Zero);
        Int256Be v2 = (Int256Be)(Int256)UInt256.One;
        ((Int256)(--v2)).Should().Be(Int256.Zero);
    }

    [Fact]
    public void Int256Be_WideningFromInt128Be()
    {
        Int128Be narrow = new Int128Be((Int128)(-42));
        Int256Be wide = narrow; // implicit widening
        ((Int256)wide).Should().Be((Int256)(-42));
    }

    [Fact]
    public void Int256Be_NarrowingToInt128Be()
    {
        Int256Be wide = (Int256Be)(Int256)(-42);
        Int128Be narrow = (Int128Be)wide;
        ((Int128)narrow).Should().Be((Int128)(-42));
    }

    [Fact]
    public void Int256Be_ExplicitToUInt256Be_IsReinterpret()
    {
        Int256Be signed = (Int256Be)Int256.NegativeOne;
        UInt256Be unsigned = (UInt256Be)signed;
        ((UInt256)unsigned).Should().Be(UInt256.MaxValue);
    }

    [Fact]
    public void Int256Be_ExplicitFromUInt256Be_IsReinterpret()
    {
        UInt256Be unsigned = (UInt256Be)UInt256.MaxValue;
        Int256Be signed = (Int256Be)unsigned;
        ((Int256)signed).Should().Be(Int256.NegativeOne);
    }

    [Fact]
    public void Int256Be_ReadFrom_Span()
    {
        Int256 original = (Int256)(-999);
        Int256Be expected = (Int256Be)original;
        Span<byte> buf = stackalloc byte[32];
        expected.WriteTo(buf);
        Int256Be restored = Int256Be.ReadFrom(buf);
        ((Int256)restored).Should().Be(original);
    }

    // ── Int256Le ─────────────────────────────────────────────────────────

    [Fact]
    public void Int256Le_PositiveValue_ByteLayoutIsLittleEndian()
    {
        // Value = 1 should have the first byte = 0x01 and all others 0x00 (little-endian)
        Int256Le le = (Int256Le)(Int256)UInt256.One;
        Span<byte> buf = stackalloc byte[32];
        le.WriteTo(buf);

        buf[0].Should().Be(0x01);
        for (int i = 1; i < 32; i++) buf[i].Should().Be(0x00);
    }

    [Fact]
    public void Int256Le_MinusOne_AllBytesFf()
    {
        Int256Le le = (Int256Le)Int256.NegativeOne;
        Span<byte> buf = stackalloc byte[32];
        le.WriteTo(buf);

        for (int i = 0; i < 32; i++) buf[i].Should().Be(0xFF);
    }

    [Fact]
    public void Int256Le_RoundTrip_ThroughBytes()
    {
        Int256 original = MakeInt256(0x0102030405060708L, 0x090A0B0C0D0E0F10L,
                                     0x1112131415161718L, 0x191A1B1C1D1E1F20L);
        Int256Le le = (Int256Le)original;
        Span<byte> buf = stackalloc byte[32];
        le.WriteTo(buf);

        Int256Le restored = new Int256Le(buf);
        ((Int256)restored).Should().Be(original);
    }

    [Fact]
    public void Int256Le_RoundTrip_NegativeValue()
    {
        Int256 original = Int256.MinValue;
        Int256Le le = (Int256Le)original;
        Span<byte> buf = stackalloc byte[32];
        le.WriteTo(buf);

        // MinValue = 1000...0; little-endian: byte[31] = 0x80, rest 0x00
        for (int i = 0; i < 31; i++) buf[i].Should().Be(0x00);
        buf[31].Should().Be(0x80);

        Int256Le restored = new Int256Le(buf);
        ((Int256)restored).Should().Be(original);
    }

    [Fact]
    public void Int256Le_Arithmetic_DelegatesSignedSemantics()
    {
        Int256Le a = (Int256Le)(Int256)(-10);
        Int256Le b = (Int256Le)(Int256)3;
        ((Int256)(a + b)).Should().Be((Int256)(-7));
        ((Int256)(a - b)).Should().Be((Int256)(-13));
        ((Int256)(a * b)).Should().Be((Int256)(-30));
        ((Int256)(a / b)).Should().Be((Int256)(-3));
        ((Int256)(a % b)).Should().Be((Int256)(-1));
    }

    [Fact]
    public void Int256Le_Comparison_IsSignedNotUnsigned()
    {
        Int256Le neg = (Int256Le)Int256.NegativeOne;
        Int256Le pos = (Int256Le)(Int256)UInt256.One;

        (neg < pos).Should().BeTrue("signed: -1 < 1");
        (pos > neg).Should().BeTrue();
    }

    [Fact]
    public void Int256Le_ShiftRight_IsArithmetic()
    {
        Int256Le val = (Int256Le)(Int256)(-256);
        Int256Le shifted = val >> 4;
        ((Int256)shifted).Should().Be((Int256)(-16));
    }

    [Fact]
    public void Int256Le_ShiftRightUnsigned_IsLogical()
    {
        Int256Le val = (Int256Le)Int256.NegativeOne;
        Int256Le shifted = val >>> 1;
        ((Int256)shifted).Should().Be(Int256.MaxValue);
    }

    [Fact]
    public void Int256Le_Parse_DecimalRoundTrip()
    {
        string s = "-42";
        Int256Le val = Int256Le.Parse(s);
        val.ToString().Should().Be(s);
    }

    [Fact]
    public void Int256Le_TryWriteTo_ReturnsFalseWhenTooShort()
    {
        Int256Le le = (Int256Le)(Int256)UInt256.One;
        Span<byte> tooShort = stackalloc byte[16];
        le.TryWriteTo(tooShort).Should().BeFalse();
    }

    [Fact]
    public void Int256Le_WideningFromInt128Le()
    {
        Int128Le narrow = new Int128Le((Int128)(-999));
        Int256Le wide = narrow;
        ((Int256)wide).Should().Be((Int256)(-999));
    }

    [Fact]
    public void Int256Le_NarrowingToInt128Le()
    {
        Int256Le wide = (Int256Le)(Int256)(-999);
        Int128Le narrow = (Int128Le)wide;
        ((Int128)narrow).Should().Be((Int128)(-999));
    }

    [Fact]
    public void Int256Le_ExplicitToUInt256Le_IsReinterpret()
    {
        Int256Le signed = (Int256Le)Int256.NegativeOne;
        UInt256Le unsigned = (UInt256Le)signed;
        ((UInt256)unsigned).Should().Be(UInt256.MaxValue);
    }

    [Fact]
    public void Int256Le_ExplicitFromUInt256Le_IsReinterpret()
    {
        UInt256Le unsigned = (UInt256Le)UInt256.MaxValue;
        Int256Le signed = (Int256Le)unsigned;
        ((Int256)signed).Should().Be(Int256.NegativeOne);
    }

    [Fact]
    public void Int256Le_ReadFrom_Span()
    {
        Int256 original = (Int256)(-1234567);
        Int256Le expected = (Int256Le)original;
        Span<byte> buf = stackalloc byte[32];
        expected.WriteTo(buf);
        Int256Le restored = Int256Le.ReadFrom(buf);
        ((Int256)restored).Should().Be(original);
    }

    // ── Cross-type ───────────────────────────────────────────────────────

    [Fact]
    public void Int256BeAndLe_SameValue_SameBitsInNativeInt256()
    {
        Int256 native = (Int256)(-123456789);
        ((Int256)(Int256Be)(native)).Should().Be(native);
        ((Int256)(Int256Le)(native)).Should().Be(native);
    }

    [Fact]
    public void Int256BeAndLe_Fuzz_RoundTrip()
    {
        var rng = new Random(unchecked((int)0xDEAD_BEEF));
        Span<byte> raw   = stackalloc byte[32];
        Span<byte> beOut = stackalloc byte[32];
        Span<byte> leOut = stackalloc byte[32];
        for (int i = 0; i < 200; i++)
        {
            rng.NextBytes(raw);

            Int256Be be = new Int256Be(raw);
            Int256 nativeBe = (Int256)be;
            be.WriteTo(beOut);
            new Int256Be(beOut).Should().Be(be);

            Int256Le le = new Int256Le(raw);
            Int256 nativeLe = (Int256)le;
            le.WriteTo(leOut);
            new Int256Le(leOut).Should().Be(le);

            // The two types over the same bytes represent different values (unless value is 0).
            // But both must round-trip through native correctly.
            ((Int256)(Int256Be)nativeBe).Should().Be(nativeBe);
            ((Int256)(Int256Le)nativeLe).Should().Be(nativeLe);
        }
    }
}
