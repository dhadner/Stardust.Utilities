using System;
using System.Globalization;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Stardust.Utilities.Benchmarks;

/// <summary>
/// Establishes a performance baseline for the 256-bit integer types
/// (<see cref="Int256"/>, <see cref="UInt256"/>, <see cref="UInt256Be"/>,
/// <see cref="UInt256Le"/>) against <see cref="BigInteger"/> and
/// <see cref="UInt128"/> for the operations that currently delegate to
/// <see cref="BigInteger"/> under the hood:
///
///   * operator /   (long-division)
///   * operator %   (long-division remainder)
///   * ToString     (decimal formatting)
///   * Parse        (decimal parsing)
///
/// The package is marketed as providing "native performance"; any routine
/// that internally allocates a byte[] and round-trips through BigInteger
/// is orders of magnitude slower than the equivalent on UInt128 and should
/// be surfaced here. Results from these benchmarks form the baseline used
/// to validate replacement implementations (native 256-bit long-division
/// and base-10 convert/parse).
///
/// Iteration counts are intentionally lower than the bit-level benchmarks
/// because each operation is far more expensive.
/// </summary>
[MemoryDiagnoser(true)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class Int256ArithmeticBenchmarks
{
    private const int ITERATIONS = 10_000;

    // Operands chosen so that:
    //  * Numerator fills all 256 bits (exercises full long-division path).
    //  * Divisor is multi-word and non-trivial (avoids the single-word fast
    //    path that a native implementation may provide).
    private static readonly UInt256 U256_A =
        new UInt256(0xFEDCBA9876543210UL, 0x1122334455667788UL, 0xAABBCCDDEEFF0011UL, 0x0123456789ABCDEFUL);
    private static readonly UInt256 U256_B =
        new UInt256(0x0000000000000000UL, 0x00000000DEADBEEFUL, 0xCAFEBABEF00DBA11UL, 0x1234567890ABCDEFUL);

    private static readonly Int256 I256_A = (Int256)U256_A;
    private static readonly Int256 I256_B = (Int256)U256_B;

    private static readonly UInt256Be U256Be_A = U256_A;
    private static readonly UInt256Be U256Be_B = U256_B;
    private static readonly UInt256Le U256Le_A = U256_A;
    private static readonly UInt256Le U256Le_B = U256_B;

    private static readonly BigInteger BIG_A = new BigInteger(new byte[]
    {
        0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01,
        0x11, 0x00, 0xFF, 0xEE, 0xDD, 0xCC, 0xBB, 0xAA,
        0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11,
        0x10, 0x32, 0x54, 0x76, 0x98, 0xBA, 0xDC, 0xFE,
        0x00 // force unsigned
    }, isUnsigned: true, isBigEndian: false);

    private static readonly BigInteger BIG_B = new BigInteger(new byte[]
    {
        0xEF, 0xCD, 0xAB, 0x90, 0x78, 0x56, 0x34, 0x12,
        0x11, 0xBA, 0x0D, 0xF0, 0xBE, 0xBA, 0xFE, 0xCA,
        0xEF, 0xBE, 0xAD, 0xDE, 0x00, 0x00, 0x00, 0x00,
        0x00
    }, isUnsigned: true, isBigEndian: false);

    private static readonly UInt128 U128_A = new UInt128(0xFEDCBA9876543210UL, 0x1122334455667788UL);
    private static readonly UInt128 U128_B = new UInt128(0x00000000DEADBEEFUL, 0xCAFEBABEF00DBA11UL);

    // Strings for Parse benchmarks. Pre-formatted once so ToString cost is
    // not included in the Parse measurement.
    private static readonly string U256_DECIMAL = U256_A.ToString("D", CultureInfo.InvariantCulture);
    private static readonly string I256_DECIMAL = I256_A.ToString("D", CultureInfo.InvariantCulture);
    private static readonly string BIG_DECIMAL = BIG_A.ToString(CultureInfo.InvariantCulture);
    private static readonly string U128_DECIMAL = U128_A.ToString(CultureInfo.InvariantCulture);

    // ════════════════════════════════════════════════════════════
    //  Division
    // ════════════════════════════════════════════════════════════

    [BenchmarkCategory("Div"), Benchmark(Baseline = true)]
    public UInt128 UInt128_Div()
    {
        UInt128 acc = UInt128.Zero;
        UInt128 a = U128_A, b = U128_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a / b; a++; }
        return acc;
    }

    [BenchmarkCategory("Div"), Benchmark]
    public BigInteger BigInteger_Div()
    {
        BigInteger acc = BigInteger.Zero;
        BigInteger a = BIG_A, b = BIG_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a / b; a++; }
        return acc;
    }

    [BenchmarkCategory("Div"), Benchmark]
    public UInt256 UInt256_Div()
    {
        UInt256 acc = UInt256.Zero;
        UInt256 a = U256_A, b = U256_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a / b; a++; }
        return acc;
    }

    [BenchmarkCategory("Div"), Benchmark]
    public Int256 Int256_Div()
    {
        Int256 acc = Int256.Zero;
        Int256 a = I256_A, b = I256_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a / b; a++; }
        return acc;
    }

    [BenchmarkCategory("Div"), Benchmark]
    public UInt256Be UInt256Be_Div()
    {
        UInt256Be acc = (UInt256Be)UInt256.Zero;
        UInt256Be a = U256Be_A, b = U256Be_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a / b; a++; }
        return acc;
    }

    [BenchmarkCategory("Div"), Benchmark]
    public UInt256Le UInt256Le_Div()
    {
        UInt256Le acc = (UInt256Le)UInt256.Zero;
        UInt256Le a = U256Le_A, b = U256Le_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a / b; a++; }
        return acc;
    }

    // ════════════════════════════════════════════════════════════
    //  Modulo
    // ════════════════════════════════════════════════════════════

    [BenchmarkCategory("Mod"), Benchmark(Baseline = true)]
    public UInt128 UInt128_Mod()
    {
        UInt128 acc = UInt128.Zero;
        UInt128 a = U128_A, b = U128_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a % b; a++; }
        return acc;
    }

    [BenchmarkCategory("Mod"), Benchmark]
    public BigInteger BigInteger_Mod()
    {
        BigInteger acc = BigInteger.Zero;
        BigInteger a = BIG_A, b = BIG_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a % b; a++; }
        return acc;
    }

    [BenchmarkCategory("Mod"), Benchmark]
    public UInt256 UInt256_Mod()
    {
        UInt256 acc = UInt256.Zero;
        UInt256 a = U256_A, b = U256_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a % b; a++; }
        return acc;
    }

    [BenchmarkCategory("Mod"), Benchmark]
    public Int256 Int256_Mod()
    {
        Int256 acc = Int256.Zero;
        Int256 a = I256_A, b = I256_B;
        for (int i = 0; i < ITERATIONS; i++) { acc += a % b; a++; }
        return acc;
    }

    // ════════════════════════════════════════════════════════════
    //  ToString (decimal)
    // ════════════════════════════════════════════════════════════

    [BenchmarkCategory("ToString"), Benchmark(Baseline = true)]
    public int UInt128_ToString()
    {
        int len = 0;
        UInt128 a = U128_A;
        for (int i = 0; i < ITERATIONS; i++) { len += a.ToString(CultureInfo.InvariantCulture).Length; a++; }
        return len;
    }

    [BenchmarkCategory("ToString"), Benchmark]
    public int BigInteger_ToString()
    {
        int len = 0;
        BigInteger a = BIG_A;
        for (int i = 0; i < ITERATIONS; i++) { len += a.ToString(CultureInfo.InvariantCulture).Length; a++; }
        return len;
    }

    [BenchmarkCategory("ToString"), Benchmark]
    public int UInt256_ToString()
    {
        int len = 0;
        UInt256 a = U256_A;
        for (int i = 0; i < ITERATIONS; i++) { len += a.ToString("D", CultureInfo.InvariantCulture).Length; a++; }
        return len;
    }

    [BenchmarkCategory("ToString"), Benchmark]
    public int Int256_ToString()
    {
        int len = 0;
        Int256 a = I256_A;
        for (int i = 0; i < ITERATIONS; i++) { len += a.ToString("D", CultureInfo.InvariantCulture).Length; a++; }
        return len;
    }

    [BenchmarkCategory("ToString"), Benchmark]
    public int UInt256Be_ToString()
    {
        int len = 0;
        UInt256Be a = U256Be_A;
        for (int i = 0; i < ITERATIONS; i++) { len += a.ToString("D", CultureInfo.InvariantCulture).Length; a++; }
        return len;
    }

    [BenchmarkCategory("ToString"), Benchmark]
    public int UInt256Le_ToString()
    {
        int len = 0;
        UInt256Le a = U256Le_A;
        for (int i = 0; i < ITERATIONS; i++) { len += a.ToString("D", CultureInfo.InvariantCulture).Length; a++; }
        return len;
    }

    // ════════════════════════════════════════════════════════════
    //  Parse (decimal)
    // ════════════════════════════════════════════════════════════

    [BenchmarkCategory("Parse"), Benchmark(Baseline = true)]
    public UInt128 UInt128_Parse()
    {
        UInt128 acc = UInt128.Zero;
        string s = U128_DECIMAL;
        for (int i = 0; i < ITERATIONS; i++) { acc += UInt128.Parse(s, CultureInfo.InvariantCulture); }
        return acc;
    }

    [BenchmarkCategory("Parse"), Benchmark]
    public BigInteger BigInteger_Parse()
    {
        BigInteger acc = BigInteger.Zero;
        string s = BIG_DECIMAL;
        for (int i = 0; i < ITERATIONS; i++) { acc += BigInteger.Parse(s, CultureInfo.InvariantCulture); }
        return acc;
    }

    [BenchmarkCategory("Parse"), Benchmark]
    public UInt256 UInt256_Parse()
    {
        UInt256 acc = UInt256.Zero;
        string s = U256_DECIMAL;
        for (int i = 0; i < ITERATIONS; i++) { acc += UInt256.Parse(s); }
        return acc;
    }

    [BenchmarkCategory("Parse"), Benchmark]
    public Int256 Int256_Parse()
    {
        Int256 acc = Int256.Zero;
        string s = I256_DECIMAL;
        for (int i = 0; i < ITERATIONS; i++) { acc += Int256.Parse(s); }
        return acc;
    }

    [BenchmarkCategory("Parse"), Benchmark]
    public UInt256Be UInt256Be_Parse()
    {
        UInt256Be acc = (UInt256Be)UInt256.Zero;
        string s = U256_DECIMAL;
        for (int i = 0; i < ITERATIONS; i++) { acc += UInt256Be.Parse(s); }
        return acc;
    }

    [BenchmarkCategory("Parse"), Benchmark]
    public UInt256Le UInt256Le_Parse()
    {
        UInt256Le acc = (UInt256Le)UInt256.Zero;
        string s = U256_DECIMAL;
        for (int i = 0; i < ITERATIONS; i++) { acc += UInt256Le.Parse(s); }
        return acc;
    }
}
