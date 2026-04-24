using System;
using System.Globalization;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Stardust.Utilities.Benchmarks;

/// <summary>
/// Head-to-head comparison of the signed <see cref="Int256"/> type in this package
/// against the same operation on Nethermind.Int256.Int256, MissingValues.Int256, and
/// (for reference) <see cref="BigInteger"/>. Same operand values across libraries so
/// differences reflect implementation cost alone, not input distribution.
///
/// Stardust.Int256 stores its value as two <see cref="UInt128"/> halves; both
/// competitors store as four raw <see cref="ulong"/> limbs. This benchmark exists to
/// measure whether that layout difference costs us measurable performance on the
/// signed type (the unsigned UInt256 already uses 4 ulongs and matches both
/// competitors).
/// </summary>
[MemoryDiagnoser(true)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class Int256SignedComparisonBenchmarks
{
    private const int ITERATIONS = 10_000;

    // Operand values — same bit pattern across libraries.

    private static readonly Int256 OUR_A = (Int256)
        new UInt256(0x0123456789ABCDEFUL, 0x1122334455667788UL, 0xAABBCCDDEEFF0011UL, 0x0123456789ABCDEFUL);
    private static readonly Int256 OUR_B = (Int256)
        new UInt256(0x0000000000000000UL, 0x00000000DEADBEEFUL, 0xCAFEBABEF00DBA11UL, 0x1234567890ABCDEFUL);

    // Nethermind: ctor takes (u0..u3) least-significant first.
    private static readonly Nethermind.Int256.Int256 NETH_A = new Nethermind.Int256.Int256(
        new Nethermind.Int256.UInt256(0x0123456789ABCDEFUL, 0xAABBCCDDEEFF0011UL, 0x1122334455667788UL, 0x0123456789ABCDEFUL));
    private static readonly Nethermind.Int256.Int256 NETH_B = new Nethermind.Int256.Int256(
        new Nethermind.Int256.UInt256(0x1234567890ABCDEFUL, 0xCAFEBABEF00DBA11UL, 0x00000000DEADBEEFUL, 0x0000000000000000UL));

    // MissingValues: Int256(upper UInt128, lower UInt128).
    private static readonly MissingValues.Int256 MV_A = (MissingValues.Int256)new MissingValues.UInt256(
        new UInt128(0x0123456789ABCDEFUL, 0x1122334455667788UL),
        new UInt128(0xAABBCCDDEEFF0011UL, 0x0123456789ABCDEFUL));
    private static readonly MissingValues.Int256 MV_B = (MissingValues.Int256)new MissingValues.UInt256(
        new UInt128(0x0000000000000000UL, 0x00000000DEADBEEFUL),
        new UInt128(0xCAFEBABEF00DBA11UL, 0x1234567890ABCDEFUL));

    private static readonly BigInteger BIG_A = OUR_A.ToBigInteger();
    private static readonly BigInteger BIG_B = OUR_B.ToBigInteger();
    // Mask the BigInteger result back to 256 signed bits to keep the comparison fair
    // (otherwise BigInteger grows unboundedly each iteration and the benchmark
    //  degenerates into measuring allocation size, not operation cost).
    private static readonly BigInteger BIG_MASK_256 = (BigInteger.One << 256) - BigInteger.One;
    private static readonly BigInteger BIG_SIGN_BIT = BigInteger.One << 255;

    private static BigInteger Wrap256(BigInteger v)
    {
        v &= BIG_MASK_256;
        return (v & BIG_SIGN_BIT) != 0 ? v - (BigInteger.One << 256) : v;
    }

    private static readonly string DECIMAL_A = OUR_A.ToString("D", CultureInfo.InvariantCulture);

    // ════════════════════════════════════════════════════════════
    //  Add
    // ════════════════════════════════════════════════════════════

    [BenchmarkCategory("Add"), Benchmark(Baseline = true)]
    public Int256 Ours_Add()
    {
        Int256 acc = Int256.Zero, a = OUR_A, b = OUR_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a + b; a++; }
        return acc;
    }

    [BenchmarkCategory("Add"), Benchmark]
    public Nethermind.Int256.Int256 Nethermind_Add()
    {
        Nethermind.Int256.Int256 acc = default, a = NETH_A, b = NETH_B;
        Nethermind.Int256.Int256 one = Nethermind.Int256.Int256.One;
        for (int i = 0; i < ITERATIONS; i++)
        {
            Nethermind.Int256.Int256.Add(a, b, out var sum);
            Nethermind.Int256.Int256.Add(acc, sum, out acc);
            Nethermind.Int256.Int256.Add(a, one, out a);
        }
        return acc;
    }

    [BenchmarkCategory("Add"), Benchmark]
    public MissingValues.Int256 MissingValues_Add()
    {
        MissingValues.Int256 acc = MissingValues.Int256.Zero, a = MV_A, b = MV_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a + b; a++; }
        return acc;
    }

    [BenchmarkCategory("Add"), Benchmark]
    public BigInteger BigInteger_Add()
    {
        BigInteger acc = BigInteger.Zero, a = BIG_A, b = BIG_B, one = BigInteger.One;
        for (int i = 0; i < ITERATIONS; i++) { acc = Wrap256(acc + (a + b)); a = Wrap256(a + one); }
        return acc;
    }

    // ════════════════════════════════════════════════════════════
    //  Subtract
    // ════════════════════════════════════════════════════════════

    [BenchmarkCategory("Sub"), Benchmark(Baseline = true)]
    public Int256 Ours_Sub()
    {
        Int256 acc = Int256.Zero, a = OUR_A, b = OUR_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a - b; a++; }
        return acc;
    }

    [BenchmarkCategory("Sub"), Benchmark]
    public Nethermind.Int256.Int256 Nethermind_Sub()
    {
        Nethermind.Int256.Int256 acc = default, a = NETH_A, b = NETH_B;
        Nethermind.Int256.Int256 one = Nethermind.Int256.Int256.One;
        for (int i = 0; i < ITERATIONS; i++)
        {
            Nethermind.Int256.Int256.Subtract(a, b, out var diff);
            Nethermind.Int256.Int256.Add(acc, diff, out acc);
            Nethermind.Int256.Int256.Add(a, one, out a);
        }
        return acc;
    }

    [BenchmarkCategory("Sub"), Benchmark]
    public MissingValues.Int256 MissingValues_Sub()
    {
        MissingValues.Int256 acc = MissingValues.Int256.Zero, a = MV_A, b = MV_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a - b; a++; }
        return acc;
    }

    [BenchmarkCategory("Sub"), Benchmark]
    public BigInteger BigInteger_Sub()
    {
        BigInteger acc = BigInteger.Zero, a = BIG_A, b = BIG_B, one = BigInteger.One;
        for (int i = 0; i < ITERATIONS; i++) { acc = Wrap256(acc + (a - b)); a = Wrap256(a + one); }
        return acc;
    }

    // ════════════════════════════════════════════════════════════
    //  Multiply
    // ════════════════════════════════════════════════════════════

    [BenchmarkCategory("Mul"), Benchmark(Baseline = true)]
    public Int256 Ours_Mul()
    {
        Int256 acc = Int256.Zero, a = OUR_A, b = OUR_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a * b; a++; }
        return acc;
    }

    [BenchmarkCategory("Mul"), Benchmark]
    public Nethermind.Int256.Int256 Nethermind_Mul()
    {
        Nethermind.Int256.Int256 acc = default, a = NETH_A, b = NETH_B;
        Nethermind.Int256.Int256 one = Nethermind.Int256.Int256.One;
        for (int i = 0; i < ITERATIONS; i++)
        {
            Nethermind.Int256.Int256.Multiply(a, b, out var prod);
            Nethermind.Int256.Int256.Add(acc, prod, out acc);
            Nethermind.Int256.Int256.Add(a, one, out a);
        }
        return acc;
    }

    [BenchmarkCategory("Mul"), Benchmark]
    public MissingValues.Int256 MissingValues_Mul()
    {
        MissingValues.Int256 acc = MissingValues.Int256.Zero, a = MV_A, b = MV_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a * b; a++; }
        return acc;
    }

    [BenchmarkCategory("Mul"), Benchmark]
    public BigInteger BigInteger_Mul()
    {
        BigInteger acc = BigInteger.Zero, a = BIG_A, b = BIG_B, one = BigInteger.One;
        for (int i = 0; i < ITERATIONS; i++) { acc = Wrap256(acc + (a * b)); a = Wrap256(a + one); }
        return acc;
    }

    // ════════════════════════════════════════════════════════════
    //  Divide
    // ════════════════════════════════════════════════════════════

    [BenchmarkCategory("Div"), Benchmark(Baseline = true)]
    public Int256 Ours_Div()
    {
        Int256 acc = Int256.Zero, a = OUR_A, b = OUR_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a / b; a++; }
        return acc;
    }

    [BenchmarkCategory("Div"), Benchmark]
    public Nethermind.Int256.Int256 Nethermind_Div()
    {
        Nethermind.Int256.Int256 acc = default, a = NETH_A, b = NETH_B;
        Nethermind.Int256.Int256 one = Nethermind.Int256.Int256.One;
        for (int i = 0; i < ITERATIONS; i++)
        {
            Nethermind.Int256.Int256.Divide(a, b, out var q);
            Nethermind.Int256.Int256.Add(acc, q, out acc);
            Nethermind.Int256.Int256.Add(a, one, out a);
        }
        return acc;
    }

    [BenchmarkCategory("Div"), Benchmark]
    public MissingValues.Int256 MissingValues_Div()
    {
        MissingValues.Int256 acc = MissingValues.Int256.Zero, a = MV_A, b = MV_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a / b; a++; }
        return acc;
    }

    [BenchmarkCategory("Div"), Benchmark]
    public BigInteger BigInteger_Div()
    {
        BigInteger acc = BigInteger.Zero, a = BIG_A, b = BIG_B, one = BigInteger.One;
        for (int i = 0; i < ITERATIONS; i++) { acc = Wrap256(acc + (a / b)); a = Wrap256(a + one); }
        return acc;
    }

    // ════════════════════════════════════════════════════════════
    //  Modulo
    // ════════════════════════════════════════════════════════════

    [BenchmarkCategory("Mod"), Benchmark(Baseline = true)]
    public Int256 Ours_Mod()
    {
        Int256 acc = Int256.Zero, a = OUR_A, b = OUR_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a % b; a++; }
        return acc;
    }

    [BenchmarkCategory("Mod"), Benchmark]
    public Nethermind.Int256.Int256 Nethermind_Mod()
    {
        Nethermind.Int256.Int256 acc = default, a = NETH_A, b = NETH_B;
        Nethermind.Int256.Int256 one = Nethermind.Int256.Int256.One;
        for (int i = 0; i < ITERATIONS; i++)
        {
            Nethermind.Int256.Int256.Mod(a, b, out var r);
            Nethermind.Int256.Int256.Add(acc, r, out acc);
            Nethermind.Int256.Int256.Add(a, one, out a);
        }
        return acc;
    }

    [BenchmarkCategory("Mod"), Benchmark]
    public MissingValues.Int256 MissingValues_Mod()
    {
        MissingValues.Int256 acc = MissingValues.Int256.Zero, a = MV_A, b = MV_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a % b; a++; }
        return acc;
    }

    [BenchmarkCategory("Mod"), Benchmark]
    public BigInteger BigInteger_Mod()
    {
        BigInteger acc = BigInteger.Zero, a = BIG_A, b = BIG_B, one = BigInteger.One;
        for (int i = 0; i < ITERATIONS; i++) { acc = Wrap256(acc + (a % b)); a = Wrap256(a + one); }
        return acc;
    }

    // ════════════════════════════════════════════════════════════
    //  ToString (decimal)
    // ════════════════════════════════════════════════════════════

    [BenchmarkCategory("ToString"), Benchmark(Baseline = true)]
    public int Ours_ToString()
    {
        int total = 0;
        Int256 a = OUR_A;
        for (int i = 0; i < ITERATIONS; i++) { total += a.ToString("D", CultureInfo.InvariantCulture).Length; a++; }
        return total;
    }

    [BenchmarkCategory("ToString"), Benchmark]
    public int Nethermind_ToString()
    {
        int total = 0;
        Nethermind.Int256.Int256 a = NETH_A;
        Nethermind.Int256.Int256 one = Nethermind.Int256.Int256.One;
        for (int i = 0; i < ITERATIONS; i++)
        {
            total += a.ToString().Length;
            Nethermind.Int256.Int256.Add(a, one, out a);
        }
        return total;
    }

    [BenchmarkCategory("ToString"), Benchmark]
    public int MissingValues_ToString()
    {
        int total = 0;
        MissingValues.Int256 a = MV_A;
        for (int i = 0; i < ITERATIONS; i++) { total += a.ToString("D", CultureInfo.InvariantCulture).Length; a++; }
        return total;
    }

    [BenchmarkCategory("ToString"), Benchmark]
    public int BigInteger_ToString()
    {
        int total = 0;
        BigInteger a = BIG_A, one = BigInteger.One;
        for (int i = 0; i < ITERATIONS; i++) { total += a.ToString(CultureInfo.InvariantCulture).Length; a = Wrap256(a + one); }
        return total;
    }

    // ════════════════════════════════════════════════════════════
    //  Parse (decimal)
    // ════════════════════════════════════════════════════════════

    [BenchmarkCategory("Parse"), Benchmark(Baseline = true)]
    public Int256 Ours_Parse()
    {
        Int256 acc = Int256.Zero;
        string s = DECIMAL_A;
        for (int i = 0; i < ITERATIONS; i++) { acc += Int256.Parse(s); }
        return acc;
    }

    [BenchmarkCategory("Parse"), Benchmark]
    public Nethermind.Int256.Int256 Nethermind_Parse()
    {
        // Nethermind.Int256.Int256 has no Parse — only BigInteger ctor. This is the
        // only path they expose for parsing a decimal string.
        Nethermind.Int256.Int256 acc = default;
        string s = DECIMAL_A;
        for (int i = 0; i < ITERATIONS; i++)
        {
            var v = new Nethermind.Int256.Int256(BigInteger.Parse(s, CultureInfo.InvariantCulture));
            Nethermind.Int256.Int256.Add(acc, v, out acc);
        }
        return acc;
    }

    [BenchmarkCategory("Parse"), Benchmark]
    public MissingValues.Int256 MissingValues_Parse()
    {
        MissingValues.Int256 acc = MissingValues.Int256.Zero;
        string s = DECIMAL_A;
        for (int i = 0; i < ITERATIONS; i++) { acc += MissingValues.Int256.Parse(s, CultureInfo.InvariantCulture); }
        return acc;
    }

    [BenchmarkCategory("Parse"), Benchmark]
    public BigInteger BigInteger_Parse()
    {
        BigInteger acc = BigInteger.Zero;
        string s = DECIMAL_A;
        for (int i = 0; i < ITERATIONS; i++) { acc = Wrap256(acc + BigInteger.Parse(s, CultureInfo.InvariantCulture)); }
        return acc;
    }
}
