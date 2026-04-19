using System;
using System.Numerics;
using FluentAssertions;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Correctness tests for the batched <see cref="UInt256Vector"/> API.
/// Every output element is cross-checked against the scalar operator on
/// <see cref="UInt256"/>, which itself is covered against <see cref="BigInteger"/>
/// elsewhere. Fuzz tests exercise 512 random operand pairs to catch
/// multi-limb multiply column-accumulation edge cases.
/// </summary>
public class UInt256VectorTests
{
    private const int FUZZ_COUNT = 512;

    [Fact]
    public void Multiply_MatchesScalar_Fuzz()
    {
        var (a, b) = RandomPairs(FUZZ_COUNT, seed: 0xDEADBEEFU);
        var expected = new UInt256[FUZZ_COUNT];
        for (int i = 0; i < FUZZ_COUNT; i++) expected[i] = a[i] * b[i];

        var actual = new UInt256[FUZZ_COUNT];
        UInt256Vector.Multiply(a, b, actual);

        actual.Should().Equal(expected);
    }

    [Fact]
    public void Multiply_BoundaryValues_MatchesScalar()
    {
        UInt256[] a = { UInt256.Zero, UInt256.One, UInt256.MaxValue, new UInt256(1, 0, 0, 0), UInt256.MaxValue };
        UInt256[] b = { UInt256.MaxValue, UInt256.MaxValue, UInt256.One, new UInt256(0, 0, 0, 0x100), UInt256.MaxValue };
        var expected = new UInt256[a.Length];
        for (int i = 0; i < a.Length; i++) expected[i] = a[i] * b[i];

        var actual = new UInt256[a.Length];
        UInt256Vector.Multiply(a, b, actual);

        actual.Should().Equal(expected);
    }

    [Fact]
    public void Multiply_LengthMismatch_Throws()
    {
        var a = new UInt256[4];
        var b = new UInt256[3];
        var r = new UInt256[4];
        Action act = () => UInt256Vector.Multiply(a, b, r);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Multiply_EmptySpan_NoOp()
    {
        var a = Array.Empty<UInt256>();
        var b = Array.Empty<UInt256>();
        var r = Array.Empty<UInt256>();
        Action act = () => UInt256Vector.Multiply(a, b, r);
        act.Should().NotThrow();
    }

    /// <summary>
    /// Produces a pair of random UInt256 arrays using a small LCG so the
    /// tests are deterministic and cheap to execute.
    /// </summary>
    private static (UInt256[] a, UInt256[] b) RandomPairs(int count, uint seed)
    {
        var a = new UInt256[count];
        var b = new UInt256[count];
        ulong s0 = seed, s1 = ~(ulong)seed, s2 = ((ulong)seed << 32) | seed, s3 = 0xC0FFEE00_DEADBEEFUL;
        for (int i = 0; i < count; i++)
        {
            s0 = unchecked(s0 * 6364136223846793005UL + 1442695040888963407UL);
            s1 = unchecked(s1 * 6364136223846793005UL + 1442695040888963407UL);
            s2 = unchecked(s2 * 6364136223846793005UL + 1442695040888963407UL);
            s3 = unchecked(s3 * 6364136223846793005UL + 1442695040888963407UL);
            a[i] = new UInt256(s3, s2, s1, s0);
            s0 = unchecked(s0 * 6364136223846793005UL + 1442695040888963407UL);
            s1 = unchecked(s1 * 6364136223846793005UL + 1442695040888963407UL);
            s2 = unchecked(s2 * 6364136223846793005UL + 1442695040888963407UL);
            s3 = unchecked(s3 * 6364136223846793005UL + 1442695040888963407UL);
            b[i] = new UInt256(s3, s2, s1, s0);
        }
        return (a, b);
    }
}
