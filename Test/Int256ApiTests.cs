using System;
using System.Globalization;
using System.Numerics;
using FluentAssertions;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Coverage for the extended <see cref="Int256"/> API surface added to mirror
/// the BCL <see cref="Int128"/> type: math helpers, bit operations, endian
/// read/write, checked operators, conversions, and generic-math interface
/// members.
/// </summary>
public class Int256ApiTests
{
    // ── A. Math helpers ──────────────────────────────────────────────

    [Fact]
    public void Abs_Positive_IsIdentity()
    {
        Int256.Abs((Int256)42).Should().Be((Int256)42);
    }

    [Fact]
    public void Abs_Negative_Negates()
    {
        Int256.Abs((Int256)(-42)).Should().Be((Int256)42);
    }

    [Fact]
    public void Abs_MinValue_Throws()
    {
        Action a = () => Int256.Abs(Int256.MinValue);
        a.Should().Throw<OverflowException>();
    }

    [Fact]
    public void CopySign_ChangesSign()
    {
        Int256.CopySign(42, -1).Should().Be((Int256)(-42));
        Int256.CopySign(-42, 1).Should().Be((Int256)42);
        Int256.CopySign(-42, -1).Should().Be((Int256)(-42));
    }

    [Fact]
    public void Clamp_WorksOverSignedRange()
    {
        Int256.Clamp(-5, -3, 3).Should().Be((Int256)(-3));
        Int256.Clamp(5, -3, 3).Should().Be((Int256)3);
        Int256.Clamp(0, -3, 3).Should().Be((Int256)0);
    }

    [Fact]
    public void DivRem_TruncatingToward_Zero()
    {
        var (q, r) = Int256.DivRem(-17, 5);
        q.Should().Be((Int256)(-3));
        r.Should().Be((Int256)(-2));
    }

    [Fact]
    public void Max_Min_RespectSignedOrdering()
    {
        Int256.Max(-10, 5).Should().Be((Int256)5);
        Int256.Min(-10, 5).Should().Be((Int256)(-10));
    }

    [Fact]
    public void MaxMagnitude_PrefersLargerAbsoluteValue()
    {
        Int256.MaxMagnitude(-10, 5).Should().Be((Int256)(-10));
        Int256.MaxMagnitude(3, -3).Should().Be((Int256)3); // tie → positive
        Int256.MinMagnitude(-10, 5).Should().Be((Int256)5);
    }

    [Fact]
    public void Sign_ReturnsMinus1_0_1()
    {
        Int256.Sign(Int256.Zero).Should().Be(0);
        Int256.Sign((Int256)5).Should().Be(1);
        Int256.Sign((Int256)(-5)).Should().Be(-1);
    }

    // ── B. Bit-level ─────────────────────────────────────────────────

    [Fact]
    public void LeadingZeroCount_NegativeHasZero()
    {
        Int256.LeadingZeroCount((Int256)(-1)).Should().Be(Int256.Zero);
        Int256.LeadingZeroCount(Int256.Zero).Should().Be((Int256)256);
        Int256.LeadingZeroCount((Int256)1).Should().Be((Int256)255);
    }

    [Fact]
    public void Log2_ThrowsOnNegative()
    {
        Action a = () => Int256.Log2((Int256)(-1));
        a.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Log2_MatchesBigInteger()
    {
        for (int i = 0; i < 255; i++)
        {
            Int256 v = (Int256)1 << i;
            Int256.Log2(v).Should().Be((Int256)i);
        }
    }

    [Fact]
    public void Rotate_RoundTrips()
    {
        Int256 v = (Int256)0x1234567890ABCDEFL;
        Int256.RotateRight(Int256.RotateLeft(v, 33), 33).Should().Be(v);
    }

    [Fact]
    public void GetByteCount_IsAlways32()
    {
        Int256.Zero.GetByteCount().Should().Be(32);
    }

    [Fact]
    public void GetShortestBitLength_MatchesBclSemantics()
    {
        // BCL Int128 semantics:
        //   non-negative: 256 - LeadingZeroCount(value)
        //   negative:     257 - LeadingZeroCount(~value)
        Int256.Zero.GetShortestBitLength().Should().Be(0);
        ((Int256)1).GetShortestBitLength().Should().Be(1);
        ((Int256)127).GetShortestBitLength().Should().Be(7);
        ((Int256)(-1)).GetShortestBitLength().Should().Be(1);
        Int256.MinValue.GetShortestBitLength().Should().Be(256);
    }

    // ── C. Is-predicates ─────────────────────────────────────────────

    [Fact]
    public void IsPredicates_MatchBclSemantics()
    {
        Int256.IsZero(Int256.Zero).Should().BeTrue();
        Int256.IsNegative((Int256)(-1)).Should().BeTrue();
        Int256.IsNegative(Int256.Zero).Should().BeFalse();
        Int256.IsPositive(Int256.Zero).Should().BeTrue();   // BCL: 0 is non-negative → positive
        Int256.IsPositive((Int256)(-1)).Should().BeFalse();
        Int256.IsPow2((Int256)(-4)).Should().BeFalse();
        Int256.IsPow2((Int256)4).Should().BeTrue();
        Int256.IsEvenInteger((Int256)(-4)).Should().BeTrue();
        Int256.IsOddInteger((Int256)(-3)).Should().BeTrue();
    }

    // ── D. Endian Read/Write ─────────────────────────────────────────

    [Fact]
    public void ReadBigEndian_Signed_SignExtends()
    {
        byte[] buf = new byte[] { 0xFF }; // -1 as signed
        Int256 v = Int256.ReadBigEndian(buf, isUnsigned: false);
        v.Should().Be((Int256)(-1));
    }

    [Fact]
    public void ReadBigEndian_Unsigned_ZeroExtends()
    {
        byte[] buf = new byte[] { 0xFF };
        Int256 v = Int256.ReadBigEndian(buf, isUnsigned: true);
        v.Should().Be((Int256)255);
    }

    [Fact]
    public void ReadBigEndian_Unsigned32Bytes_WithTopBit_Overflows()
    {
        byte[] buf = new byte[32];
        buf[0] = 0x80;
        Int256.TryReadBigEndian(buf, isUnsigned: true, out _).Should().BeFalse();
    }

    [Fact]
    public void WriteBigEndian_RoundTrip_Negative()
    {
        Int256 v = (Int256)(-123456789L);
        byte[] buf = new byte[32];
        v.WriteBigEndian(buf).Should().Be(32);
        Int256 round = Int256.ReadBigEndian(buf, isUnsigned: false);
        round.Should().Be(v);
    }

    [Fact]
    public void WriteLittleEndian_RoundTrip_MinValue()
    {
        byte[] buf = new byte[32];
        Int256.MinValue.WriteLittleEndian(buf).Should().Be(32);
        Int256 round = Int256.ReadLittleEndian(buf, isUnsigned: false);
        round.Should().Be(Int256.MinValue);
    }

    // ── E. Checked operators ─────────────────────────────────────────

    [Fact]
    public void CheckedAdd_Overflow()
    {
        Action a = () => { Int256 _ = checked(Int256.MaxValue + (Int256)1); };
        a.Should().Throw<OverflowException>();
    }

    [Fact]
    public void CheckedSub_Overflow()
    {
        Action a = () => { Int256 _ = checked(Int256.MinValue - (Int256)1); };
        a.Should().Throw<OverflowException>();
    }

    [Fact]
    public void CheckedNegate_MinValue_Throws()
    {
        Action a = () => { Int256 _ = checked(-Int256.MinValue); };
        a.Should().Throw<OverflowException>();
    }

    [Fact]
    public void CheckedMul_Overflow()
    {
        Int256 big = (Int256)1 << 130;
        Action a = () => { Int256 _ = checked(big * big); };
        a.Should().Throw<OverflowException>();
    }

    [Fact]
    public void CheckedMul_MinValueTimesMinusOne_Throws()
    {
        Action a = () => { Int256 _ = checked(Int256.MinValue * Int256.NegativeOne); };
        a.Should().Throw<OverflowException>();
    }

    [Fact]
    public void CheckedDiv_MinValueOverMinusOne_Throws()
    {
        Action a = () => { Int256 _ = checked(Int256.MinValue / Int256.NegativeOne); };
        a.Should().Throw<OverflowException>();
    }

    // ── F. Parsing with NumberStyles / provider ──────────────────────

    [Fact]
    public void Parse_NegativeWithProvider()
    {
        Int256 v = Int256.Parse("-42", NumberStyles.Integer, CultureInfo.InvariantCulture);
        v.Should().Be((Int256)(-42));
    }

    [Fact]
    public void TryParse_SpanStyle()
    {
        Int256.TryParse("99".AsSpan(), NumberStyles.Integer, null, out Int256 r).Should().BeTrue();
        r.Should().Be((Int256)99);
    }

    // ── G. UTF-8 parsing/formatting ──────────────────────────────────

#if NET8_0_OR_GREATER
    [Fact]
    public void Utf8_FormatAndParseRoundTrip_Negative()
    {
        Int256 v = (Int256)(-1234567890123456789L);
        Span<byte> buf = stackalloc byte[64];
        v.TryFormat(buf, out int n, "G", CultureInfo.InvariantCulture).Should().BeTrue();
        Int256 round = Int256.Parse(buf[..n], CultureInfo.InvariantCulture);
        round.Should().Be(v);
    }
#endif

    // ── H. Conversions ───────────────────────────────────────────────

    [Fact]
    public void CharConversion_Implicit()
    {
        Int256 v = 'A';
        v.Should().Be((Int256)65);
    }

    [Fact]
    public void ByteConversion_Implicit()
    {
        Int256 v = (byte)200;
        v.Should().Be((Int256)200);
    }

    [Fact]
    public void DoubleConversion_Negative()
    {
        Int256 v = (Int256)(-42);
        ((double)v).Should().Be(-42.0);
        ((Int256)(-42.7)).Should().Be((Int256)(-42));
    }

    [Fact]
    public void DecimalConversion_PositiveAndNegative()
    {
        ((decimal)(Int256)123456789L).Should().Be(123456789m);
        ((decimal)(Int256)(-123456789L)).Should().Be(-123456789m);
    }

    [Fact]
    public void CheckedNarrowing_ToInt_Overflow()
    {
        Int256 big = (Int256)1 << 40;
        Action a = () => { int _ = checked((int)big); };
        a.Should().Throw<OverflowException>();
    }

    [Fact]
    public void CheckedNarrowing_ToUnsigned_ThrowsOnNegative()
    {
        Action a = () => { byte _ = checked((byte)(Int256)(-1)); };
        a.Should().Throw<OverflowException>();
    }

    [Fact]
    public void CheckedNarrowing_ToInt128_WorksForInRange()
    {
        Int256 v = (Int256)42;
        ((Int128)v).Should().Be((Int128)42);
        Int256 bigNeg = (Int256)Int128.MinValue;
        ((Int128)bigNeg).Should().Be(Int128.MinValue);
    }

    // ── I. Identity / generic-math ───────────────────────────────────

    [Fact]
    public void AdditiveAndMultiplicativeIdentity()
    {
        Int256.AdditiveIdentity.Should().Be(Int256.Zero);
        Int256.MultiplicativeIdentity.Should().Be(Int256.One);
        Int256.Radix.Should().Be(2);
    }

#if NET8_0_OR_GREATER
    [Fact]
    public void AllBitsSet_EqualsNegativeOne()
    {
        Int256.AllBitsSet.Should().Be(Int256.NegativeOne);
    }
#endif

    private static T Negate<T>(T a) where T : INumber<T> => -a;

    [Fact]
    public void INumber_GenericNegation()
    {
        Int256 r = Negate<Int256>((Int256)5);
        r.Should().Be((Int256)(-5));
    }

    [Fact]
    public void INumber_CreateChecked_FromNegativeInt()
    {
        Int256 r = Int256.CreateChecked(-42);
        r.Should().Be((Int256)(-42));
    }

    [Fact]
    public void INumber_CreateSaturating_LargeDouble()
    {
        Int256 r = Int256.CreateSaturating(1e78);
        r.Should().Be(Int256.MaxValue);
    }

    [Fact]
    public void INumber_CreateTruncating_UInt128()
    {
        Int256 r = Int256.CreateTruncating(UInt128.MaxValue);
        // UInt128.MaxValue truncated to 128-bit container, then zero-extended to Int256.
        r.Should().Be(new Int256(UInt128.Zero, UInt128.MaxValue));
    }
}
