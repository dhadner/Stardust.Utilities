using System;
using System.Globalization;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Stardust.Utilities.Benchmarks;

/// <summary>
/// Head-to-head comparison of the <see cref="UInt256"/> / <see cref="Int256"/>
/// types in this package against widely-used 256-bit alternatives:
///   * Nethermind.Numerics.Int256         (namespace: Nethermind.Int256)
///   * MissingValues                      (namespace: MissingValues)
///   * System.Numerics.BigInteger         (BCL arbitrary-precision reference
///                                         -- gives architects a clear view
///                                         of the heap-allocation / variable-
///                                         width tax relative to a fixed-
///                                         32-byte value type).
///
/// Operations exercised: Add, Subtract, Multiply, Divide, Modulo,
/// ToString (decimal) and Parse (decimal). All benchmarks use the same operand
/// values across libraries so differences reflect implementation cost alone,
/// not input distribution.
/// </summary>
[MemoryDiagnoser(true)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class Int256LibraryComparisonBenchmarks
{
    private const int ITERATIONS = 10_000;

    // ── Operand values (identical bits across all three libraries) ───────

    // Stardust.Utilities
    private static readonly UInt256 OUR_A =
        new UInt256(0xFEDCBA9876543210UL, 0x1122334455667788UL, 0xAABBCCDDEEFF0011UL, 0x0123456789ABCDEFUL);
    private static readonly UInt256 OUR_B =
        new UInt256(0x0000000000000000UL, 0x00000000DEADBEEFUL, 0xCAFEBABEF00DBA11UL, 0x1234567890ABCDEFUL);
    private static readonly Int256 OUR_IA = (Int256)OUR_A;
    private static readonly Int256 OUR_IB = (Int256)OUR_B;

    // Nethermind.Int256 - ctor takes ulong limbs (u0 = least-significant).
    private static readonly Nethermind.Int256.UInt256 NETH_A =
        new Nethermind.Int256.UInt256(0x0123456789ABCDEFUL, 0xAABBCCDDEEFF0011UL, 0x1122334455667788UL, 0xFEDCBA9876543210UL);
    private static readonly Nethermind.Int256.UInt256 NETH_B =
        new Nethermind.Int256.UInt256(0x1234567890ABCDEFUL, 0xCAFEBABEF00DBA11UL, 0x00000000DEADBEEFUL, 0x0000000000000000UL);
    private static readonly Nethermind.Int256.Int256 NETH_IA = new Nethermind.Int256.Int256(NETH_A);
    private static readonly Nethermind.Int256.Int256 NETH_IB = new Nethermind.Int256.Int256(NETH_B);

    // MissingValues - UInt256(upper UInt128, lower UInt128) using System.UInt128.
    private static readonly MissingValues.UInt256 MV_A = new MissingValues.UInt256(
        new UInt128(0xFEDCBA9876543210UL, 0x1122334455667788UL),
        new UInt128(0xAABBCCDDEEFF0011UL, 0x0123456789ABCDEFUL));
    private static readonly MissingValues.UInt256 MV_B = new MissingValues.UInt256(
        new UInt128(0x0000000000000000UL, 0x00000000DEADBEEFUL),
        new UInt128(0xCAFEBABEF00DBA11UL, 0x1234567890ABCDEFUL));
    private static readonly MissingValues.Int256 MV_IA = (MissingValues.Int256)MV_A;
    private static readonly MissingValues.Int256 MV_IB = (MissingValues.Int256)MV_B;

    // System.Numerics.BigInteger - parsed from the same decimal value as OUR_A/B
    // so the numeric value is identical. BigInteger is arbitrary-precision and
    // heap-allocated; every operation produces a fresh instance. Included as a
    // reference for "what you pay if you reach for the BCL's big-number type
    // instead of a fixed-width 256-bit value".
    private static readonly BigInteger BIG_A = BigInteger.Parse(
        OUR_A.ToString("D", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    private static readonly BigInteger BIG_B = BigInteger.Parse(
        OUR_B.ToString("D", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    // BigInteger mask to emulate fixed 256-bit wrap-around on Add/Sub/Mul so the
    // comparison stays apples-to-apples with the fixed-width types (which all
    // wrap modulo 2^256). Without masking, BigInteger grows unboundedly each
    // iteration and the benchmark degenerates into measuring allocation size,
    // not operation cost.
    private static readonly BigInteger BIG_MASK_256 = (BigInteger.One << 256) - BigInteger.One;

    // Pre-formatted decimal strings for Parse benchmarks.
    private static readonly string DECIMAL_A = OUR_A.ToString("D", CultureInfo.InvariantCulture);
    private static readonly string DECIMAL_IA = OUR_IA.ToString("D", CultureInfo.InvariantCulture);

    // ════════════════════════════════════════════════════════════
    //  Add   (sanity-check baseline; all libraries should be fast)
    // ════════════════════════════════════════════════════════════

    [BenchmarkCategory("Add"), Benchmark(Baseline = true)]
    public UInt256 Ours_Add()
    {
        UInt256 acc = UInt256.Zero, a = OUR_A, b = OUR_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a + b; a++; }
        return acc;
    }

    [BenchmarkCategory("Add"), Benchmark]
    public Nethermind.Int256.UInt256 Nethermind_Add()
    {
        Nethermind.Int256.UInt256 acc = default;
        Nethermind.Int256.UInt256 a = NETH_A, b = NETH_B;
        Nethermind.Int256.UInt256 one = new Nethermind.Int256.UInt256(1UL, 0UL, 0UL, 0UL);
        for (int i = 0; i < ITERATIONS; i++)
        {
            Nethermind.Int256.UInt256.Add(a, b, out var sum);
            Nethermind.Int256.UInt256.Add(acc, sum, out acc);
            Nethermind.Int256.UInt256.Add(a, one, out a);
        }
        return acc;
    }

    [BenchmarkCategory("Add"), Benchmark]
    public MissingValues.UInt256 MissingValues_Add()
    {
        MissingValues.UInt256 acc = MissingValues.UInt256.Zero, a = MV_A, b = MV_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a + b; a++; }
        return acc;
    }

    [BenchmarkCategory("Add"), Benchmark]
    public BigInteger BigInteger_Add()
    {
        BigInteger acc = BigInteger.Zero, a = BIG_A, b = BIG_B, one = BigInteger.One, mask = BIG_MASK_256;
        for (int i = 0; i < ITERATIONS; i++) { acc = (acc + (a + b)) & mask; a = (a + one) & mask; }
        return acc;
    }

    // ════════════════════════════════════════════════════════════
    //  Subtract
    // ════════════════════════════════════════════════════════════

    [BenchmarkCategory("Sub"), Benchmark(Baseline = true)]
    public UInt256 Ours_Sub()
    {
        UInt256 acc = UInt256.Zero, a = OUR_A, b = OUR_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a - b; a++; }
        return acc;
    }

    [BenchmarkCategory("Sub"), Benchmark]
    public Nethermind.Int256.UInt256 Nethermind_Sub()
    {
        Nethermind.Int256.UInt256 acc = default;
        Nethermind.Int256.UInt256 a = NETH_A, b = NETH_B;
        Nethermind.Int256.UInt256 one = new Nethermind.Int256.UInt256(1UL, 0UL, 0UL, 0UL);
        for (int i = 0; i < ITERATIONS; i++)
        {
            Nethermind.Int256.UInt256.Subtract(a, b, out var diff);
            Nethermind.Int256.UInt256.Add(acc, diff, out acc);
            Nethermind.Int256.UInt256.Add(a, one, out a);
        }
        return acc;
    }

    [BenchmarkCategory("Sub"), Benchmark]
    public MissingValues.UInt256 MissingValues_Sub()
    {
        MissingValues.UInt256 acc = MissingValues.UInt256.Zero, a = MV_A, b = MV_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a - b; a++; }
        return acc;
    }

    [BenchmarkCategory("Sub"), Benchmark]
    public BigInteger BigInteger_Sub()
    {
        BigInteger acc = BigInteger.Zero, a = BIG_A, b = BIG_B, one = BigInteger.One, mask = BIG_MASK_256;
        for (int i = 0; i < ITERATIONS; i++) { acc = (acc + ((a - b) & mask)) & mask; a = (a + one) & mask; }
        return acc;
    }

    // ════════════════════════════════════════════════════════════
    //  Multiply
    // ════════════════════════════════════════════════════════════

    [BenchmarkCategory("Mul"), Benchmark(Baseline = true)]
    public UInt256 Ours_Mul()
    {
        UInt256 acc = UInt256.Zero, a = OUR_A, b = OUR_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a * b; a++; }
        return acc;
    }

    [BenchmarkCategory("Mul"), Benchmark]
    public Nethermind.Int256.UInt256 Nethermind_Mul()
    {
        Nethermind.Int256.UInt256 acc = default;
        Nethermind.Int256.UInt256 a = NETH_A, b = NETH_B;
        Nethermind.Int256.UInt256 one = new Nethermind.Int256.UInt256(1UL, 0UL, 0UL, 0UL);
        for (int i = 0; i < ITERATIONS; i++)
        {
            Nethermind.Int256.UInt256.Multiply(a, b, out var prod);
            Nethermind.Int256.UInt256.Add(acc, prod, out acc);
            Nethermind.Int256.UInt256.Add(a, one, out a);
        }
        return acc;
    }

    [BenchmarkCategory("Mul"), Benchmark]
    public MissingValues.UInt256 MissingValues_Mul()
    {
        MissingValues.UInt256 acc = MissingValues.UInt256.Zero, a = MV_A, b = MV_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a * b; a++; }
        return acc;
    }

    [BenchmarkCategory("Mul"), Benchmark]
    public BigInteger BigInteger_Mul()
    {
        BigInteger acc = BigInteger.Zero, a = BIG_A, b = BIG_B, one = BigInteger.One, mask = BIG_MASK_256;
        for (int i = 0; i < ITERATIONS; i++) { acc = (acc + (a * b)) & mask; a = (a + one) & mask; }
        return acc;
    }

    // ════════════════════════════════════════════════════════════
    //  Divide
    // ════════════════════════════════════════════════════════════

    [BenchmarkCategory("Div"), Benchmark(Baseline = true)]
    public UInt256 Ours_Div()
    {
        UInt256 acc = UInt256.Zero, a = OUR_A, b = OUR_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a / b; a++; }
        return acc;
    }

    [BenchmarkCategory("Div"), Benchmark]
    public Nethermind.Int256.UInt256 Nethermind_Div()
    {
        Nethermind.Int256.UInt256 acc = default;
        Nethermind.Int256.UInt256 a = NETH_A, b = NETH_B;
        Nethermind.Int256.UInt256 one = new Nethermind.Int256.UInt256(1UL, 0UL, 0UL, 0UL);
        for (int i = 0; i < ITERATIONS; i++)
        {
            Nethermind.Int256.UInt256.Divide(a, b, out var q);
            Nethermind.Int256.UInt256.Add(acc, q, out acc);
            Nethermind.Int256.UInt256.Add(a, one, out a);
        }
        return acc;
    }

    [BenchmarkCategory("Div"), Benchmark]
    public MissingValues.UInt256 MissingValues_Div()
    {
        MissingValues.UInt256 acc = MissingValues.UInt256.Zero, a = MV_A, b = MV_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a / b; a++; }
        return acc;
    }

    [BenchmarkCategory("Div"), Benchmark]
    public BigInteger BigInteger_Div()
    {
        BigInteger acc = BigInteger.Zero, a = BIG_A, b = BIG_B, one = BigInteger.One, mask = BIG_MASK_256;
        for (int i = 0; i < ITERATIONS; i++) { acc = (acc + (a / b)) & mask; a = (a + one) & mask; }
        return acc;
    }

    // ════════════════════════════════════════════════════════════
    //  Modulo
    // ════════════════════════════════════════════════════════════

    [BenchmarkCategory("Mod"), Benchmark(Baseline = true)]
    public UInt256 Ours_Mod()
    {
        UInt256 acc = UInt256.Zero, a = OUR_A, b = OUR_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a % b; a++; }
        return acc;
    }

    [BenchmarkCategory("Mod"), Benchmark]
    public Nethermind.Int256.UInt256 Nethermind_Mod()
    {
        Nethermind.Int256.UInt256 acc = default;
        Nethermind.Int256.UInt256 a = NETH_A, b = NETH_B;
        Nethermind.Int256.UInt256 one = new Nethermind.Int256.UInt256(1UL, 0UL, 0UL, 0UL);
        for (int i = 0; i < ITERATIONS; i++)
        {
            Nethermind.Int256.UInt256.Mod(a, b, out var r);
            Nethermind.Int256.UInt256.Add(acc, r, out acc);
            Nethermind.Int256.UInt256.Add(a, one, out a);
        }
        return acc;
    }

    [BenchmarkCategory("Mod"), Benchmark]
    public MissingValues.UInt256 MissingValues_Mod()
    {
        MissingValues.UInt256 acc = MissingValues.UInt256.Zero, a = MV_A, b = MV_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a % b; a++; }
        return acc;
    }

    [BenchmarkCategory("Mod"), Benchmark]
    public BigInteger BigInteger_Mod()
    {
        BigInteger acc = BigInteger.Zero, a = BIG_A, b = BIG_B, one = BigInteger.One, mask = BIG_MASK_256;
        for (int i = 0; i < ITERATIONS; i++) { acc = (acc + (a % b)) & mask; a = (a + one) & mask; }
        return acc;
    }

    // ════════════════════════════════════════════════════════════
    //  ToString (decimal)
    // ════════════════════════════════════════════════════════════

    [BenchmarkCategory("ToString"), Benchmark(Baseline = true)]
    public int Ours_ToString()
    {
        int len = 0;
        UInt256 a = OUR_A;
        for (int i = 0; i < ITERATIONS; i++) { len += a.ToString("D", CultureInfo.InvariantCulture).Length; a++; }
        return len;
    }

    [BenchmarkCategory("ToString"), Benchmark]
    public int Nethermind_ToString()
    {
        int len = 0;
        Nethermind.Int256.UInt256 a = NETH_A;
        Nethermind.Int256.UInt256 one = new Nethermind.Int256.UInt256(1UL, 0UL, 0UL, 0UL);
        for (int i = 0; i < ITERATIONS; i++)
        {
            len += a.ToString().Length;
            Nethermind.Int256.UInt256.Add(a, one, out a);
        }
        return len;
    }

    [BenchmarkCategory("ToString"), Benchmark]
    public int MissingValues_ToString()
    {
        int len = 0;
        MissingValues.UInt256 a = MV_A;
        for (int i = 0; i < ITERATIONS; i++) { len += a.ToString("D", CultureInfo.InvariantCulture).Length; a++; }
        return len;
    }

    [BenchmarkCategory("ToString"), Benchmark]
    public int BigInteger_ToString()
    {
        int len = 0;
        BigInteger a = BIG_A, one = BigInteger.One, mask = BIG_MASK_256;
        for (int i = 0; i < ITERATIONS; i++) { len += a.ToString().Length; a = (a + one) & mask; }
        return len;
    }

    // ════════════════════════════════════════════════════════════
    //  Parse (decimal)
    // ════════════════════════════════════════════════════════════

    [BenchmarkCategory("Parse"), Benchmark(Baseline = true)]
    public UInt256 Ours_Parse()
    {
        UInt256 acc = UInt256.Zero;
        string s = DECIMAL_A;
        for (int i = 0; i < ITERATIONS; i++) { acc += UInt256.Parse(s); }
        return acc;
    }

    [BenchmarkCategory("Parse"), Benchmark]
    public Nethermind.Int256.UInt256 Nethermind_Parse()
    {
        Nethermind.Int256.UInt256 acc = default;
        string s = DECIMAL_A;
        for (int i = 0; i < ITERATIONS; i++)
        {
            Nethermind.Int256.UInt256.TryParse(s, out var v);
            Nethermind.Int256.UInt256.Add(acc, v, out acc);
        }
        return acc;
    }

    [BenchmarkCategory("Parse"), Benchmark]
    public MissingValues.UInt256 MissingValues_Parse()
    {
        MissingValues.UInt256 acc = MissingValues.UInt256.Zero;
        string s = DECIMAL_A;
        for (int i = 0; i < ITERATIONS; i++) { acc += MissingValues.UInt256.Parse(s, CultureInfo.InvariantCulture); }
        return acc;
    }

    [BenchmarkCategory("Parse"), Benchmark]
    public BigInteger BigInteger_Parse()
    {
        BigInteger acc = BigInteger.Zero, mask = BIG_MASK_256;
        string s = DECIMAL_A;
        for (int i = 0; i < ITERATIONS; i++) { acc = (acc + BigInteger.Parse(s, CultureInfo.InvariantCulture)) & mask; }
        return acc;
    }
}
