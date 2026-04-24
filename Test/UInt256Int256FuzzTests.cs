using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using FluentAssertions;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Randomized / boundary coverage for the full <see cref="UInt256"/> and
/// <see cref="Int256"/> API surface added to mirror BCL <see cref="UInt128"/>/<see cref="Int128"/>.
///
/// Each test cross-checks against <see cref="BigInteger"/> (source of truth for arbitrary-
/// precision integer math) or against the BCL <c>Int128</c>/<c>UInt128</c> types directly
/// (for operations that fit in 128 bits). The goal is to prove the new surface is not just
/// syntactically present but semantically correct across the range. All seeds are fixed
/// integer literals so failures reproduce deterministically.
/// </summary>
public class UInt256Int256FuzzTests
{
    private const int FUZZ_ITERATIONS = 256;

    // Per-test seed base. Each test adds a unique offset so they explore different inputs.
    private const int SEED_BASE = 0x12345670;

    private static UInt256 RandomUInt256(Random r)
    {
        Span<byte> buf = stackalloc byte[32];
        r.NextBytes(buf);
        return new UInt256(buf, isBigEndian: false);
    }

    private static Int256 RandomInt256(Random r)
    {
        Span<byte> buf = stackalloc byte[32];
        r.NextBytes(buf);
        return new Int256(buf, isBigEndian: false);
    }

    private static BigInteger ToBig(UInt256 v) => v.ToBigInteger();
    private static BigInteger ToBig(Int256 v) => v.ToBigInteger();

    // Surface the bit-counting results as a plain int via the public (ulong) cast.
    private static int AsInt(UInt256 v) => (int)(ulong)v;
    private static int AsInt(Int256 v) => (int)(long)v;

    private static readonly BigInteger POW2_256 = BigInteger.One << 256;
    private static readonly BigInteger POW2_255 = BigInteger.One << 255;
    private static readonly BigInteger INT256_MIN = -POW2_255;
    private static readonly BigInteger INT256_MAX = POW2_255 - 1;

    // ─── Checked arithmetic fuzz ─────────────────────────────────────────

    [Fact]
    public void UInt256_CheckedAdd_Fuzz_MatchesBigInteger()
    {
        Random r = new(SEED_BASE + 0);
        for (int i = 0; i < FUZZ_ITERATIONS; i++)
        {
            UInt256 a = RandomUInt256(r);
            UInt256 b = RandomUInt256(r);
            BigInteger sum = ToBig(a) + ToBig(b);
            if (sum >= POW2_256)
            {
                Action act = () => { UInt256 _ = checked(a + b); };
                act.Should().Throw<OverflowException>($"iter {i}");
            }
            else
            {
                ToBig(checked(a + b)).Should().Be(sum, $"iter {i}");
            }
        }
    }

    [Fact]
    public void UInt256_CheckedSub_Fuzz_MatchesBigInteger()
    {
        Random r = new(SEED_BASE + 1);
        for (int i = 0; i < FUZZ_ITERATIONS; i++)
        {
            UInt256 a = RandomUInt256(r);
            UInt256 b = RandomUInt256(r);
            BigInteger diff = ToBig(a) - ToBig(b);
            if (diff < 0)
            {
                Action act = () => { UInt256 _ = checked(a - b); };
                act.Should().Throw<OverflowException>($"iter {i}");
            }
            else
            {
                ToBig(checked(a - b)).Should().Be(diff, $"iter {i}");
            }
        }
    }

    [Fact]
    public void UInt256_CheckedMul_Fuzz_MatchesBigInteger()
    {
        Random r = new(SEED_BASE + 2);
        for (int i = 0; i < FUZZ_ITERATIONS; i++)
        {
            UInt256 a = RandomUInt256(r);
            UInt256 b = RandomUInt256(r);
            BigInteger prod = ToBig(a) * ToBig(b);
            if (prod >= POW2_256)
            {
                Action act = () => { UInt256 _ = checked(a * b); };
                act.Should().Throw<OverflowException>($"iter {i}");
            }
            else
            {
                ToBig(checked(a * b)).Should().Be(prod, $"iter {i}");
            }
        }
    }

    [Fact]
    public void Int256_CheckedAdd_Fuzz_MatchesBigInteger()
    {
        Random r = new(SEED_BASE + 3);
        for (int i = 0; i < FUZZ_ITERATIONS; i++)
        {
            Int256 a = RandomInt256(r);
            Int256 b = RandomInt256(r);
            BigInteger sum = ToBig(a) + ToBig(b);
            if (sum < INT256_MIN || sum > INT256_MAX)
            {
                Action act = () => { Int256 _ = checked(a + b); };
                act.Should().Throw<OverflowException>($"iter {i}");
            }
            else
            {
                ToBig(checked(a + b)).Should().Be(sum, $"iter {i}");
            }
        }
    }

    [Fact]
    public void Int256_CheckedSub_Fuzz_MatchesBigInteger()
    {
        Random r = new(SEED_BASE + 4);
        for (int i = 0; i < FUZZ_ITERATIONS; i++)
        {
            Int256 a = RandomInt256(r);
            Int256 b = RandomInt256(r);
            BigInteger diff = ToBig(a) - ToBig(b);
            if (diff < INT256_MIN || diff > INT256_MAX)
            {
                Action act = () => { Int256 _ = checked(a - b); };
                act.Should().Throw<OverflowException>($"iter {i}");
            }
            else
            {
                ToBig(checked(a - b)).Should().Be(diff, $"iter {i}");
            }
        }
    }

    [Fact]
    public void Int256_CheckedMul_Fuzz_MatchesBigInteger()
    {
        Random r = new(SEED_BASE + 5);
        for (int i = 0; i < FUZZ_ITERATIONS; i++)
        {
            Int256 a = RandomInt256(r);
            Int256 b = RandomInt256(r);
            BigInteger prod = ToBig(a) * ToBig(b);
            if (prod < INT256_MIN || prod > INT256_MAX)
            {
                Action act = () => { Int256 _ = checked(a * b); };
                act.Should().Throw<OverflowException>($"iter {i}");
            }
            else
            {
                ToBig(checked(a * b)).Should().Be(prod, $"iter {i}");
            }
        }
    }

    // ─── Boundary tests around MaxValue/MinValue ─────────────────────────

    [Fact]
    public void UInt256_CheckedAdd_ExactOverflowBoundary()
    {
        (UInt256.MaxValue + UInt256.Zero).Should().Be(UInt256.MaxValue);
        UInt256 x = checked(UInt256.MaxValue - UInt256.One + UInt256.One);
        x.Should().Be(UInt256.MaxValue);
    }

    [Fact]
    public void Int256_CheckedAdd_ExactOverflowBoundary()
    {
        Int256 x = checked(Int256.MaxValue - Int256.One + Int256.One);
        x.Should().Be(Int256.MaxValue);
        Int256 y = checked(Int256.MinValue + Int256.One - Int256.One);
        y.Should().Be(Int256.MinValue);
    }

    [Fact]
    public void Int256_CheckedMul_MinValueTimesOne_IsIdentity()
    {
        checked(Int256.MinValue * Int256.One).Should().Be(Int256.MinValue);
        checked(Int256.MaxValue * Int256.One).Should().Be(Int256.MaxValue);
    }

    // ─── Bit-level fuzz ──────────────────────────────────────────────────

    [Fact]
    public void UInt256_PopCount_Fuzz_MatchesBigInteger()
    {
        Random r = new(SEED_BASE + 6);
        for (int i = 0; i < FUZZ_ITERATIONS; i++)
        {
            UInt256 v = RandomUInt256(r);
            int expected = 0;
            foreach (byte b in ToBig(v).ToByteArray(isUnsigned: true, isBigEndian: false))
                expected += System.Numerics.BitOperations.PopCount(b);
            AsInt(UInt256.PopCount(v)).Should().Be(expected, $"iter {i}");
        }
    }

    [Fact]
    public void UInt256_LeadingZeroCount_Fuzz_MatchesBigInteger()
    {
        Random r = new(SEED_BASE + 7);
        for (int i = 0; i < FUZZ_ITERATIONS; i++)
        {
            UInt256 v = RandomUInt256(r);
            int expected = v == UInt256.Zero ? 256 : (int)(256 - ToBig(v).GetBitLength());
            AsInt(UInt256.LeadingZeroCount(v)).Should().Be(expected, $"iter {i}");
        }
    }

    [Fact]
    public void UInt256_TrailingZeroCount_Fuzz_MatchesBigInteger()
    {
        Random r = new(SEED_BASE + 8);
        for (int i = 0; i < FUZZ_ITERATIONS; i++)
        {
            UInt256 v = RandomUInt256(r);
            int expected;
            if (v == UInt256.Zero) expected = 256;
            else
            {
                expected = 0;
                BigInteger big = ToBig(v);
                while ((big & 1) == 0) { big >>= 1; expected++; }
            }
            AsInt(UInt256.TrailingZeroCount(v)).Should().Be(expected, $"iter {i}");
        }
    }

    [Fact]
    public void UInt256_Log2_Fuzz_MatchesBigIntegerBitLength()
    {
        Random r = new(SEED_BASE + 9);
        for (int i = 0; i < FUZZ_ITERATIONS; i++)
        {
            UInt256 v = RandomUInt256(r);
            if (v == UInt256.Zero) continue;
            int expected = (int)(ToBig(v).GetBitLength() - 1);
            AsInt(UInt256.Log2(v)).Should().Be(expected, $"iter {i}");
        }
    }

    [Fact]
    public void UInt256_Rotate_Fuzz_RoundTrips()
    {
        Random r = new(SEED_BASE + 10);
        for (int i = 0; i < FUZZ_ITERATIONS; i++)
        {
            UInt256 v = RandomUInt256(r);
            int amt = r.Next(0, 512);
            UInt256.RotateRight(UInt256.RotateLeft(v, amt), amt).Should().Be(v, $"iter {i}");
        }
    }

    [Fact]
    public void UInt256_RotateLeft_PreservesPopCount()
    {
        Random r = new(SEED_BASE + 11);
        for (int i = 0; i < 64; i++)
        {
            UInt256 v = RandomUInt256(r);
            UInt256 rotated = UInt256.RotateLeft(v, r.Next(0, 256));
            UInt256.PopCount(rotated).Should().Be(UInt256.PopCount(v));
        }
    }

    [Fact]
    public void Int256_PopCount_Fuzz_MatchesUnsigned()
    {
        Random r = new(SEED_BASE + 12);
        for (int i = 0; i < FUZZ_ITERATIONS; i++)
        {
            Int256 v = RandomInt256(r);
            UInt256 u = (UInt256)v;
            AsInt(Int256.PopCount(v)).Should().Be(AsInt(UInt256.PopCount(u)), $"iter {i}");
        }
    }

    [Fact]
    public void Int256_Log2_NonNegativeOnly()
    {
        Int256.Log2(Int256.One).Should().Be((Int256)0);
        Int256.Log2((Int256)1024).Should().Be((Int256)10);
        Action a = () => Int256.Log2(Int256.NegativeOne);
        a.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ─── Endian Read/Write round-trip fuzz ───────────────────────────────

    [Fact]
    public void UInt256_EndianRoundTrip_Fuzz()
    {
        Random r = new(SEED_BASE + 13);
        Span<byte> be = stackalloc byte[32];
        Span<byte> le = stackalloc byte[32];
        for (int i = 0; i < FUZZ_ITERATIONS; i++)
        {
            UInt256 v = RandomUInt256(r);
            v.WriteBigEndian(be).Should().Be(32);
            v.WriteLittleEndian(le).Should().Be(32);
            UInt256.ReadBigEndian(be, isUnsigned: true).Should().Be(v);
            UInt256.ReadLittleEndian(le, isUnsigned: true).Should().Be(v);
            for (int j = 0; j < 32; j++) be[j].Should().Be(le[31 - j]);
        }
    }

    [Fact]
    public void Int256_EndianRoundTrip_Fuzz_PreservesSign()
    {
        Random r = new(SEED_BASE + 14);
        Span<byte> be = stackalloc byte[32];
        Span<byte> le = stackalloc byte[32];
        for (int i = 0; i < FUZZ_ITERATIONS; i++)
        {
            Int256 v = RandomInt256(r);
            v.WriteBigEndian(be);
            v.WriteLittleEndian(le);
            Int256.ReadBigEndian(be, isUnsigned: false).Should().Be(v, $"iter {i} BE");
            Int256.ReadLittleEndian(le, isUnsigned: false).Should().Be(v, $"iter {i} LE");
        }
    }

    [Fact]
    public void UInt256_ReadBigEndian_VariousLengths()
    {
        UInt256.ReadBigEndian(new byte[] { 0xAB }, isUnsigned: true).Should().Be((UInt256)0xAB);
        UInt256.ReadBigEndian(new byte[] { 0x01, 0x02 }, isUnsigned: true).Should().Be((UInt256)0x0102);
        UInt256.ReadBigEndian(Array.Empty<byte>(), isUnsigned: true).Should().Be(UInt256.Zero);
        // 33 bytes with leading zero is accepted.
        byte[] padded = new byte[33];
        padded[32] = 0xFF;
        UInt256.ReadBigEndian(padded, isUnsigned: true).Should().Be((UInt256)0xFF);
        // 33 bytes with leading non-zero is rejected.
        padded[0] = 0x01;
        UInt256.TryReadBigEndian(padded, isUnsigned: true, out _).Should().BeFalse();
    }

    [Fact]
    public void Int256_ReadBigEndian_SignExtendsShortNegative()
    {
        Int256.ReadBigEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: false).Should().Be(Int256.NegativeOne);
        Int256.ReadBigEndian(new byte[] { 0xFF, 0xFF }, isUnsigned: true).Should().Be((Int256)65535);
        // 32-byte unsigned source with top bit set overflows Int256.
        byte[] tooBig = new byte[32];
        tooBig[0] = 0x80;
        Int256.TryReadBigEndian(tooBig, isUnsigned: true, out _).Should().BeFalse();
    }

    [Fact]
    public void Int256_ReadBigEndian_ExcessBytesMustMatchSignByte()
    {
        // 33-byte signed source. For negative value, excess leading bytes must all be 0xFF.
        byte[] extNeg = new byte[33];
        Array.Fill(extNeg, (byte)0xFF);
        Int256.ReadBigEndian(extNeg, isUnsigned: false).Should().Be(Int256.NegativeOne);
        // One byte wrong → reject.
        extNeg[0] = 0xFE;
        Int256.TryReadBigEndian(extNeg, isUnsigned: false, out _).Should().BeFalse();
    }

    // ─── Conversion edge cases ───────────────────────────────────────────

    [Fact]
    public void UInt256_DoubleConversion_NaNAndNegativeReturnZero()
    {
        ((UInt256)double.NaN).Should().Be(UInt256.Zero);
        ((UInt256)(-1.0)).Should().Be(UInt256.Zero);
        ((UInt256)(-0.0)).Should().Be(UInt256.Zero);
    }

    [Fact]
    public void UInt256_CheckedDoubleConversion_RejectsNaNInfNegative()
    {
        Action[] bad =
        {
            () => { UInt256 _ = checked((UInt256)double.NaN); },
            () => { UInt256 _ = checked((UInt256)double.PositiveInfinity); },
            () => { UInt256 _ = checked((UInt256)double.NegativeInfinity); },
            () => { UInt256 _ = checked((UInt256)(-1.0)); },
        };
        foreach (Action a in bad) a.Should().Throw<OverflowException>();
    }

    [Fact]
    public void Int256_DoubleConversion_RoundTripForExactMantissaValues()
    {
        long[] exacts = { 0, 1, -1, 42, -42, 1L << 30, -(1L << 30), (1L << 53) - 1, -((1L << 53) - 1) };
        foreach (long v in exacts)
        {
            ((Int256)(double)v).Should().Be((Int256)v);
            ((double)(Int256)v).Should().Be((double)v);
        }
    }

    [Fact]
    public void UInt256_DecimalConversion_RoundTripForInRangeValues()
    {
        decimal[] samples = { 0m, 1m, decimal.MaxValue, 123456789012345m };
        foreach (decimal d in samples)
        {
            UInt256 v = (UInt256)d;
            ((decimal)v).Should().Be(d);
        }
    }

    [Fact]
    public void UInt256_DecimalConversion_OverflowThrows()
    {
        UInt256 tooBig = ((UInt256)1) << 128;
        Action a = () => { decimal _ = (decimal)tooBig; };
        a.Should().Throw<OverflowException>();
    }

    [Fact]
    public void Int256_DecimalConversion_NegativeRoundTrip()
    {
        Int256 v = (Int256)(-123456789012345L);
        ((decimal)v).Should().Be(-123456789012345m);
        ((Int256)(-123456789012345m)).Should().Be(v);
    }

    // ─── CreateChecked/Saturating/Truncating matrix ──────────────────────

    [Theory]
    [InlineData((byte)0)]
    [InlineData((byte)255)]
    [InlineData((byte)42)]
    public void UInt256_CreateChecked_FromByte(byte v)
        => UInt256.CreateChecked(v).Should().Be((UInt256)v);

    [Theory]
    [InlineData(0)]
    [InlineData(int.MaxValue)]
    [InlineData(1)]
    public void UInt256_CreateChecked_FromPositiveInt(int v)
        => UInt256.CreateChecked(v).Should().Be((UInt256)(uint)v);

    [Fact]
    public void UInt256_CreateChecked_FromNegativeInt_Throws()
    {
        Action a = () => UInt256.CreateChecked(-1);
        a.Should().Throw<OverflowException>();
    }

    [Fact]
    public void UInt256_CreateSaturating_FromNegative_ClampsToZero()
    {
        UInt256.CreateSaturating(-1).Should().Be(UInt256.Zero);
        UInt256.CreateSaturating(int.MinValue).Should().Be(UInt256.Zero);
        UInt256.CreateSaturating(-1L).Should().Be(UInt256.Zero);
        UInt256.CreateSaturating(Int128.MinValue).Should().Be(UInt256.Zero);
    }

    [Fact]
    public void UInt256_CreateTruncating_FromSignedWrapsToTwosComplement()
    {
        UInt256.CreateTruncating((sbyte)-1).Should().Be((UInt256)byte.MaxValue);
        UInt256.CreateTruncating((short)-1).Should().Be((UInt256)ushort.MaxValue);
        UInt256.CreateTruncating(-1).Should().Be((UInt256)uint.MaxValue);
        UInt256.CreateTruncating(-1L).Should().Be((UInt256)ulong.MaxValue);
        UInt256.CreateTruncating(Int128.NegativeOne).Should().Be(new UInt256(UInt128.MaxValue, UInt128.MaxValue));
    }

    [Fact]
    public void Int256_CreateChecked_FromInt128_MatchesWiden()
    {
        Int256.CreateChecked(Int128.MaxValue).Should().Be((Int256)Int128.MaxValue);
        Int256.CreateChecked(Int128.MinValue).Should().Be((Int256)Int128.MinValue);
        Int256.CreateChecked(Int128.Zero).Should().Be(Int256.Zero);
    }

    [Fact]
    public void Int256_CreateChecked_FromUInt128_MatchesWiden()
    {
        Int256.CreateChecked(UInt128.MaxValue).Should().Be((Int256)UInt128.MaxValue);
        Int256.CreateChecked(UInt128.Zero).Should().Be(Int256.Zero);
    }

    [Fact]
    public void Int256_CreateTruncating_DoubleOutOfRange_WrapsToMinValue()
    {
        // Matches BCL semantics for explicit (Int128)double when value >= 2^127.
        Int256.CreateTruncating(1e78).Should().Be(Int256.MinValue);
    }

    [Fact]
    public void Int256_CreateSaturating_DoubleExtremes()
    {
        Int256.CreateSaturating(double.PositiveInfinity).Should().Be(Int256.MaxValue);
        Int256.CreateSaturating(double.NegativeInfinity).Should().Be(Int256.MinValue);
    }

    // ─── GetShortestBitLength parity with Int128 / UInt128 ───────────────

    [Fact]
    public void Int256_GetShortestBitLength_MatchesInt128_ForSmallValues()
    {
        Int128[] values =
        {
            0, 1, -1, 2, -2, 127, -128, 255, -256,
            Int128.MaxValue, Int128.MinValue,
            (Int128)long.MaxValue, (Int128)long.MinValue,
        };
        foreach (Int128 v128 in values)
        {
            Int256 v256 = (Int256)v128;
            int expected = ((IBinaryInteger<Int128>)v128).GetShortestBitLength();
            v256.GetShortestBitLength().Should().Be(expected, $"for value {v128}");
        }
    }

    [Fact]
    public void UInt256_GetShortestBitLength_MatchesUInt128_ForSmallValues()
    {
        UInt128[] values = { 0, 1, 255, 0x1_0000_0000UL, UInt128.MaxValue };
        foreach (UInt128 v128 in values)
        {
            UInt256 v256 = v128;
            int expected = ((IBinaryInteger<UInt128>)v128).GetShortestBitLength();
            v256.GetShortestBitLength().Should().Be(expected, $"for value {v128}");
        }
    }

    // ─── UTF-8 Format / Parse ────────────────────────────────────────────

#if NET8_0_OR_GREATER
    [Fact]
    public void UInt256_Utf8Format_BufferTooSmall_ReturnsFalse()
    {
        UInt256 v = UInt256.MaxValue;
        Span<byte> buf = stackalloc byte[4];
        v.TryFormat(buf, out int n, "G", CultureInfo.InvariantCulture).Should().BeFalse();
        n.Should().Be(0);
    }

    [Fact]
    public void Int256_Utf8_RoundTrip_Fuzz()
    {
        Random r = new(SEED_BASE + 15);
        Span<byte> buf = stackalloc byte[90];
        for (int i = 0; i < FUZZ_ITERATIONS; i++)
        {
            Int256 v = RandomInt256(r);
            v.TryFormat(buf, out int n, "G", CultureInfo.InvariantCulture).Should().BeTrue();
            Int256 round = Int256.Parse(buf.Slice(0, n), NumberStyles.Integer, CultureInfo.InvariantCulture);
            round.Should().Be(v, $"iter {i}");
        }
    }

    [Fact]
    public void UInt256_Utf8_HexFormat_RoundTrip()
    {
        UInt256 v = UInt256.Parse("DEADBEEFCAFEBABE", NumberStyles.HexNumber);
        Span<byte> buf = stackalloc byte[80];
        v.TryFormat(buf, out int n, "X", CultureInfo.InvariantCulture).Should().BeTrue();
        string s = Encoding.UTF8.GetString(buf.Slice(0, n));
        UInt256.Parse(s, NumberStyles.HexNumber).Should().Be(v);
    }
#endif

    // ─── Sign / Max / Min boundaries ─────────────────────────────────────

    [Fact]
    public void Int256_Sign_AllBoundaries()
    {
        Int256.Sign(Int256.MinValue).Should().Be(-1);
        Int256.Sign((Int256)(-1)).Should().Be(-1);
        Int256.Sign(Int256.Zero).Should().Be(0);
        Int256.Sign((Int256)1).Should().Be(1);
        Int256.Sign(Int256.MaxValue).Should().Be(1);
    }

    [Fact]
    public void Int256_Max_Min_Fuzz_AgainstBigInteger()
    {
        Random r = new(SEED_BASE + 16);
        for (int i = 0; i < FUZZ_ITERATIONS; i++)
        {
            Int256 a = RandomInt256(r);
            Int256 b = RandomInt256(r);
            BigInteger ba = ToBig(a), bb = ToBig(b);
            ToBig(Int256.Max(a, b)).Should().Be(BigInteger.Max(ba, bb), $"iter {i} Max");
            ToBig(Int256.Min(a, b)).Should().Be(BigInteger.Min(ba, bb), $"iter {i} Min");
        }
    }

    [Fact]
    public void Int256_MaxMagnitude_Fuzz_MatchesBigInteger()
    {
        Random r = new(SEED_BASE + 17);
        for (int i = 0; i < FUZZ_ITERATIONS; i++)
        {
            Int256 a = RandomInt256(r);
            Int256 b = RandomInt256(r);
            BigInteger ba = ToBig(a), bb = ToBig(b);
            BigInteger expected;
            if (BigInteger.Abs(ba) > BigInteger.Abs(bb)) expected = ba;
            else if (BigInteger.Abs(bb) > BigInteger.Abs(ba)) expected = bb;
            else expected = ba >= 0 ? ba : bb; // tie → positive
            ToBig(Int256.MaxMagnitude(a, b)).Should().Be(expected, $"iter {i}");
        }
    }

    // ─── DivRem parity ───────────────────────────────────────────────────

    [Fact]
    public void UInt256_DivRem_Fuzz_MatchesSeparateDivAndMod()
    {
        Random r = new(SEED_BASE + 18);
        for (int i = 0; i < FUZZ_ITERATIONS; i++)
        {
            UInt256 a = RandomUInt256(r);
            UInt256 b = RandomUInt256(r);
            if (b == UInt256.Zero) b = UInt256.One;
            var (q, rem) = UInt256.DivRem(a, b);
            q.Should().Be(a / b, $"iter {i} q");
            rem.Should().Be(a % b, $"iter {i} r");
            (q * b + rem).Should().Be(a, $"iter {i} identity");
        }
    }

    [Fact]
    public void Int256_DivRem_Fuzz_MatchesBigInteger()
    {
        Random r = new(SEED_BASE + 19);
        for (int i = 0; i < FUZZ_ITERATIONS; i++)
        {
            Int256 a = RandomInt256(r);
            Int256 b = RandomInt256(r);
            if (b == Int256.Zero) b = Int256.One;
            if (a == Int256.MinValue && b == Int256.NegativeOne) continue; // undefined
            var (q, rem) = Int256.DivRem(a, b);
            BigInteger expectedQ = BigInteger.DivRem(ToBig(a), ToBig(b), out BigInteger expectedR);
            ToBig(q).Should().Be(expectedQ, $"iter {i} q");
            ToBig(rem).Should().Be(expectedR, $"iter {i} r");
        }
    }

    // ─── Clamp edge cases ────────────────────────────────────────────────

    [Fact]
    public void UInt256_Clamp_ValueAtExactBoundary()
    {
        UInt256.Clamp(10, 10, 100).Should().Be((UInt256)10);
        UInt256.Clamp(100, 10, 100).Should().Be((UInt256)100);
        UInt256.Clamp(50, 50, 50).Should().Be((UInt256)50);
    }

    [Fact]
    public void Int256_Clamp_NegativeRange()
    {
        Int256.Clamp((Int256)(-50), (Int256)(-100), (Int256)(-10)).Should().Be((Int256)(-50));
        Int256.Clamp((Int256)(-5), (Int256)(-100), (Int256)(-10)).Should().Be((Int256)(-10));
        Int256.Clamp((Int256)(-200), (Int256)(-100), (Int256)(-10)).Should().Be((Int256)(-100));
    }

    // ─── Is* predicate parity with Int128/UInt128 ───────────────────────

    [Fact]
    public void UInt256_IsPow2_MatchesUInt128()
    {
        for (int shift = 0; shift < 128; shift++)
        {
            UInt128 u128 = (UInt128)1 << shift;
            UInt256 u256 = u128;
            UInt256.IsPow2(u256).Should().Be(UInt128.IsPow2(u128), $"1 << {shift}");
        }
        UInt256.IsPow2(UInt256.Zero).Should().BeFalse();
        UInt256.IsPow2((UInt256)3).Should().BeFalse();
        UInt256.IsPow2(((UInt256)1) << 200).Should().BeTrue();
    }

    [Fact]
    public void Int256_IsPow2_NegativeNeverPow2()
    {
        Int256.IsPow2(Int256.NegativeOne).Should().BeFalse();
        Int256.IsPow2(Int256.MinValue).Should().BeFalse();
        Int256.IsPow2((Int256)(-4)).Should().BeFalse();
        Int256.IsPow2((Int256)4).Should().BeTrue();
    }

    [Fact]
    public void Int256_IsNegative_IsPositive_AllBoundaries()
    {
        (Int256, bool, bool)[] cases =
        {
            (Int256.MinValue, true, false),
            ((Int256)(-1), true, false),
            (Int256.Zero, false, true),     // BCL: 0 is positive
            ((Int256)1, false, true),
            (Int256.MaxValue, false, true),
        };
        foreach (var (v, neg, pos) in cases)
        {
            Int256.IsNegative(v).Should().Be(neg);
            Int256.IsPositive(v).Should().Be(pos);
        }
    }
}
