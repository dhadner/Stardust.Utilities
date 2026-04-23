using System;
using System.Globalization;
using System.Numerics;
using FluentAssertions;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Coverage for the extended <see cref="UInt256"/> API surface added to mirror
/// the BCL <see cref="UInt128"/> type: math helpers, bit operations, endian
/// read/write, checked operators, conversions, and generic-math interface
/// members.
/// </summary>
public class UInt256ApiTests
{
    // ── A. Math helpers ──────────────────────────────────────────────

    [Fact]
    public void Abs_IsIdentity()
    {
        UInt256.Abs(UInt256.Zero).Should().Be(UInt256.Zero);
        UInt256.Abs(UInt256.One).Should().Be(UInt256.One);
        UInt256.Abs(UInt256.MaxValue).Should().Be(UInt256.MaxValue);
    }

    [Fact]
    public void Clamp_WorksInBothDirections()
    {
        UInt256 min = 10, max = 100;
        UInt256.Clamp(5, min, max).Should().Be((UInt256)10);
        UInt256.Clamp(50, min, max).Should().Be((UInt256)50);
        UInt256.Clamp(200, min, max).Should().Be((UInt256)100);
    }

    [Fact]
    public void Clamp_ThrowsWhenMinGreaterThanMax()
    {
        Action a = () => UInt256.Clamp(5, 100, 10);
        a.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CopySign_Unsigned_IsIdentity()
    {
        UInt256.CopySign(42, 99).Should().Be((UInt256)42);
    }

    [Fact]
    public void DivRem_ReturnsQuotientAndRemainder()
    {
        var (q, r) = UInt256.DivRem(17, 5);
        q.Should().Be((UInt256)3);
        r.Should().Be((UInt256)2);
    }

    [Fact]
    public void Max_Min_WorkCorrectly()
    {
        UInt256.Max(3, 7).Should().Be((UInt256)7);
        UInt256.Min(3, 7).Should().Be((UInt256)3);
        UInt256.MaxMagnitude(3, 7).Should().Be((UInt256)7);
        UInt256.MinMagnitude(3, 7).Should().Be((UInt256)3);
    }

    [Fact]
    public void Sign_ZeroVsNonZero()
    {
        UInt256.Sign(UInt256.Zero).Should().Be(0);
        UInt256.Sign(UInt256.One).Should().Be(1);
        UInt256.Sign(UInt256.MaxValue).Should().Be(1);
    }

    // ── B. Bit-level ─────────────────────────────────────────────────

    [Fact]
    public void LeadingZeroCount_CoversFullRange()
    {
        UInt256.LeadingZeroCount(UInt256.Zero).Should().Be((UInt256)256);
        UInt256.LeadingZeroCount(UInt256.One).Should().Be((UInt256)255);
        UInt256.LeadingZeroCount(UInt256.MaxValue).Should().Be(UInt256.Zero);
        UInt256.LeadingZeroCount(((UInt256)1) << 128).Should().Be((UInt256)127);
        UInt256.LeadingZeroCount(((UInt256)1) << 200).Should().Be((UInt256)55);
    }

    [Fact]
    public void TrailingZeroCount_CoversFullRange()
    {
        UInt256.TrailingZeroCount(UInt256.Zero).Should().Be((UInt256)256);
        UInt256.TrailingZeroCount(UInt256.One).Should().Be(UInt256.Zero);
        UInt256.TrailingZeroCount(((UInt256)1) << 200).Should().Be((UInt256)200);
    }

    [Fact]
    public void PopCount_CoversFullRange()
    {
        UInt256.PopCount(UInt256.Zero).Should().Be(UInt256.Zero);
        UInt256.PopCount(UInt256.MaxValue).Should().Be((UInt256)256);
        UInt256.PopCount((UInt256)0x5555555555555555UL).Should().Be((UInt256)32);
    }

    [Fact]
    public void Log2_MatchesBigInteger()
    {
        for (int i = 0; i < 256; i++)
        {
            UInt256 v = ((UInt256)1) << i;
            UInt256.Log2(v).Should().Be((UInt256)i);
        }
    }

    [Fact]
    public void Rotate_IsRoundTrippable()
    {
        UInt256 v = (UInt256)0xCAFEBABE_DEADBEEFUL;
        UInt256.RotateRight(UInt256.RotateLeft(v, 37), 37).Should().Be(v);
        UInt256.RotateLeft(v, 0).Should().Be(v);
        UInt256.RotateLeft(v, 256).Should().Be(v); // 256 & 255 == 0
    }

    [Fact]
    public void GetByteCount_IsAlways32()
    {
        UInt256.Zero.GetByteCount().Should().Be(32);
        UInt256.MaxValue.GetByteCount().Should().Be(32);
    }

    [Fact]
    public void GetShortestBitLength_MatchesExpected()
    {
        UInt256.Zero.GetShortestBitLength().Should().Be(0);
        UInt256.One.GetShortestBitLength().Should().Be(1);
        ((UInt256)255).GetShortestBitLength().Should().Be(8);
        UInt256.MaxValue.GetShortestBitLength().Should().Be(256);
    }

    // ── C. Is-predicates ─────────────────────────────────────────────

    [Fact]
    public void IsPredicates_MatchBclSemantics()
    {
        UInt256.IsZero(UInt256.Zero).Should().BeTrue();
        UInt256.IsZero(UInt256.One).Should().BeFalse();
        UInt256.IsEvenInteger((UInt256)4).Should().BeTrue();
        UInt256.IsOddInteger((UInt256)5).Should().BeTrue();
        UInt256.IsPow2(UInt256.Zero).Should().BeFalse();
        UInt256.IsPow2(UInt256.One).Should().BeTrue();
        UInt256.IsPow2(((UInt256)1) << 200).Should().BeTrue();
        UInt256.IsPow2((UInt256)3).Should().BeFalse();
        UInt256.IsNegative(UInt256.MaxValue).Should().BeFalse();
        UInt256.IsPositive(UInt256.MaxValue).Should().BeTrue();
        UInt256.IsPositive(UInt256.Zero).Should().BeTrue(); // matches BCL
        UInt256.IsInteger(UInt256.One).Should().BeTrue();
        UInt256.IsFinite(UInt256.One).Should().BeTrue();
        UInt256.IsNaN(UInt256.One).Should().BeFalse();
    }

    // ── D. Endian Read/Write ─────────────────────────────────────────

    [Fact]
    public void ReadBigEndian_ExactLength_WorksForUnsignedAndSigned()
    {
        byte[] buf = new byte[32];
        buf[0] = 0x80; buf[31] = 0x01;
        // Unsigned: reads top bit set as just a large value.
        UInt256 u = UInt256.ReadBigEndian(buf, isUnsigned: true);
        UInt256.PopCount(u).Should().Be((UInt256)2);
        // Signed: top bit set → overflow for unsigned target.
        Action a = () => UInt256.ReadBigEndian(buf, isUnsigned: false);
        a.Should().Throw<OverflowException>();
    }

    [Fact]
    public void ReadBigEndian_RejectsExcessNonzeroBytes()
    {
        byte[] buf = new byte[40];
        buf[7] = 0x01; // within the excess region
        UInt256.TryReadBigEndian(buf, isUnsigned: true, out _).Should().BeFalse();
    }

    [Fact]
    public void WriteBigEndian_RoundTrip()
    {
        UInt256 v = UInt256.Parse("0x12345678_9ABCDEF0_11223344_55667788_DEADBEEF_CAFEBABE_00112233_44556677".Replace("_", ""), NumberStyles.HexNumber);
        byte[] buf = new byte[32];
        v.WriteBigEndian(buf).Should().Be(32);
        UInt256 round = UInt256.ReadBigEndian(buf, isUnsigned: true);
        round.Should().Be(v);
    }

    [Fact]
    public void WriteLittleEndian_RoundTrip()
    {
        UInt256 v = UInt256.Parse("1234567890123456789012345678901234567890", NumberStyles.Integer);
        byte[] buf = new byte[32];
        v.WriteLittleEndian(buf).Should().Be(32);
        UInt256 round = UInt256.ReadLittleEndian(buf, isUnsigned: true);
        round.Should().Be(v);
    }

    [Fact]
    public void TryWriteBigEndian_FailsOnShortDestination()
    {
        byte[] buf = new byte[31];
        UInt256.One.TryWriteBigEndian(buf, out int n).Should().BeFalse();
        n.Should().Be(0);
    }

    // ── E. Checked operators ─────────────────────────────────────────

    [Fact]
    public void CheckedAdd_OverflowThrows()
    {
        Action a = () => { UInt256 _ = checked(UInt256.MaxValue + UInt256.One); };
        a.Should().Throw<OverflowException>();
    }

    [Fact]
    public void CheckedSub_UnderflowThrows()
    {
        Action a = () => { UInt256 _ = checked(UInt256.Zero - UInt256.One); };
        a.Should().Throw<OverflowException>();
    }

    [Fact]
    public void CheckedNegate_ThrowsUnlessZero()
    {
        UInt256 z = checked(-UInt256.Zero);
        z.Should().Be(UInt256.Zero);
        Action a = () => { UInt256 _ = checked(-UInt256.One); };
        a.Should().Throw<OverflowException>();
    }

    [Fact]
    public void CheckedMul_OverflowThrows()
    {
        UInt256 big = (UInt256)1 << 200;
        Action a = () => { UInt256 _ = checked(big * big); };
        a.Should().Throw<OverflowException>();
    }

    [Fact]
    public void CheckedIncDec_Boundaries()
    {
        Action a = () => { UInt256 x = UInt256.MaxValue; x = checked(++x); };
        a.Should().Throw<OverflowException>();
        Action b = () => { UInt256 x = UInt256.Zero; x = checked(--x); };
        b.Should().Throw<OverflowException>();
    }

    // ── F. Parsing with NumberStyles / provider ──────────────────────

    [Fact]
    public void Parse_HexWithProvider()
    {
        UInt256 v = UInt256.Parse("FF", NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        v.Should().Be((UInt256)255);
    }

    [Fact]
    public void TryParse_WithStyle()
    {
        UInt256.TryParse("1000", NumberStyles.Integer, CultureInfo.InvariantCulture, out UInt256 r).Should().BeTrue();
        r.Should().Be((UInt256)1000);
        UInt256.TryParse("not-a-number", NumberStyles.Integer, null, out _).Should().BeFalse();
    }

    // ── G. UTF-8 parsing/formatting ──────────────────────────────────

#if NET8_0_OR_GREATER
    [Fact]
    public void Utf8_FormatAndParseRoundTrip()
    {
        UInt256 v = UInt256.Parse("123456789012345678901234567890", NumberStyles.Integer);
        Span<byte> buf = stackalloc byte[64];
        v.TryFormat(buf, out int n, "G", CultureInfo.InvariantCulture).Should().BeTrue();
        UInt256 round = UInt256.Parse(buf[..n], CultureInfo.InvariantCulture);
        round.Should().Be(v);
    }
#endif

    // ── H. Conversions ───────────────────────────────────────────────

    [Fact]
    public void CharConversion_Implicit()
    {
        UInt256 v = 'A';
        v.Should().Be((UInt256)65);
    }

    [Fact]
    public void DoubleConversion_RoundTrip_ForSmallValues()
    {
        UInt256 v = 42;
        ((double)v).Should().Be(42.0);
        ((UInt256)42.7).Should().Be((UInt256)42);
    }

    [Fact]
    public void DoubleConversion_LargeValueApproximatesCorrectly()
    {
        UInt256 v = ((UInt256)1) << 200;
        double d = (double)v;
        d.Should().BeApproximately(Math.Pow(2, 200), Math.Pow(2, 200) * 1e-15);
    }

    [Fact]
    public void DecimalConversion_FitsWhenSmall()
    {
        UInt256 v = (UInt256)123456789UL;
        ((decimal)v).Should().Be(123456789m);
    }

    [Fact]
    public void DecimalConversion_ThrowsWhenTooLarge()
    {
        UInt256 v = ((UInt256)1) << 128;
        Action a = () => { decimal _ = (decimal)v; };
        a.Should().Throw<OverflowException>();
    }

    [Fact]
    public void CheckedNarrowing_ToByte_ThrowsOnOverflow()
    {
        Action a = () => { byte _ = checked((byte)(UInt256)256); };
        a.Should().Throw<OverflowException>();
    }

    [Fact]
    public void CheckedNarrowing_ToUInt128_ThrowsOnOverflow()
    {
        UInt256 big = ((UInt256)1) << 128;
        Action a = () => { UInt128 _ = checked((UInt128)big); };
        a.Should().Throw<OverflowException>();
    }

    [Fact]
    public void CheckedNarrowing_ToLong_ThrowsOnSignOverflow()
    {
        UInt256 big = ((UInt256)1) << 63; // long.MinValue bit pattern when cast
        Action a = () => { long _ = checked((long)big); };
        a.Should().Throw<OverflowException>();
    }

    // ── I. Identity / generic-math ───────────────────────────────────

    [Fact]
    public void AdditiveAndMultiplicativeIdentity()
    {
        UInt256.AdditiveIdentity.Should().Be(UInt256.Zero);
        UInt256.MultiplicativeIdentity.Should().Be(UInt256.One);
        UInt256.Radix.Should().Be(2);
    }

#if NET8_0_OR_GREATER
    [Fact]
    public void AllBitsSet_EqualsMaxValue()
    {
        UInt256.AllBitsSet.Should().Be(UInt256.MaxValue);
    }
#endif

    // ── Generic-math interface surface via open generic ──────────────

    private static T Add<T>(T a, T b) where T : INumber<T> => a + b;
    private static T FromInt<T>(int v) where T : INumber<T> => T.CreateChecked(v);

    [Fact]
    public void INumber_ArithmeticViaGenerics()
    {
        UInt256 r = Add<UInt256>(40, 2);
        r.Should().Be((UInt256)42);
    }

    [Fact]
    public void INumber_CreateChecked()
    {
        UInt256 r = FromInt<UInt256>(42);
        r.Should().Be((UInt256)42);
    }

    [Fact]
    public void INumber_CreateChecked_ThrowsOnNegative()
    {
        Action a = () => { UInt256 _ = UInt256.CreateChecked(-1); };
        a.Should().Throw<OverflowException>();
    }

    [Fact]
    public void INumber_CreateSaturating_ClampsNegativeToZero()
    {
        UInt256 r = UInt256.CreateSaturating(-1);
        r.Should().Be(UInt256.Zero);
    }

    [Fact]
    public void INumber_CreateTruncating_WrapsNegativeInt()
    {
        // Truncating from int: take the 32-bit two's complement pattern of -1 (= 0xFFFFFFFF).
        UInt256 r = UInt256.CreateTruncating(-1);
        r.Should().Be((UInt256)uint.MaxValue);
    }

    [Fact]
    public void INumber_CreateTruncating_WrapsNegativeLong()
    {
        // Truncating from long: take the 64-bit two's complement pattern of -1 (= 0xFFFFFFFFFFFFFFFF).
        UInt256 r = UInt256.CreateTruncating(-1L);
        r.Should().Be((UInt256)ulong.MaxValue);
    }
}
