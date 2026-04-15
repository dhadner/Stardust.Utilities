using System.Diagnostics;
using System.Runtime.CompilerServices;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Performance tests measuring the overhead of MustBe / UndefinedBitsMustBe constraints.
/// Compares operations on unconstrained BitFields (GeneratedTestRegister) vs constrained
/// BitFields (MustBeTestReg) to quantify the cost of constraint enforcement.
///
/// Two categories of overhead:
/// - Atomic operations (construction, bitwise): a single AND+OR normalization per operation.
///   Overhead is negligible on modern hardware.
/// - Shift operators (&lt;&lt;, &gt;&gt;, &gt;&gt;&gt;): iterative single-step shifts with normalization
///   after each step. Overhead scales linearly with the shift count.
///
/// Property getters and setters are NOT benchmarked here because MustBe constraints do not
/// affect them at all — accessors use pure shift-and-mask on the backing field, identical
/// to unconstrained types. See BitFieldPerformanceTests for property accessor benchmarks.
/// </summary>
[Trait("Category", "Performance")]
public class BitFieldMustBePerformanceTests
{
    private const int ITERATIONS = 50_000_000;
    private const int WARMUP_ITERATIONS = 5_000_000;
    private const int FULL_SUITE_RUNS = 50;

    private readonly ITestOutputHelper _output;

    public BitFieldMustBePerformanceTests(ITestOutputHelper output) => _output = output;

    /// <summary>
    /// Comprehensive test measuring overhead of MustBe constraints across all affected
    /// operation types. Outputs a formatted comparison table suitable for documentation.
    ///
    /// Unconstrained type: GeneratedTestRegister (byte, no MustBe constraints).
    /// Constrained type:   MustBeTestReg (byte, MustBe.One on bit 7, MustBe.Zero on bits 1-2).
    /// </summary>
    [Fact]
    public void MustBe_Constraint_Overhead_Summary()
    {
        Assert.SkipWhen(CiEnvironmentDetector.IsRunningInCi,
            "Performance tests are skipped in CI environments due to variable runner performance.");

        _output.WriteLine("=".PadRight(80, '='));
        _output.WriteLine("MUSTBE CONSTRAINT PERFORMANCE OVERHEAD");
        _output.WriteLine($"Runs: {FULL_SUITE_RUNS}, Iterations per run: {ITERATIONS:N0}");
        _output.WriteLine("Unconstrained: GeneratedTestRegister (byte, no MustBe)");
        _output.WriteLine("Constrained:   MustBeTestReg (byte, MustBe.One bit 7, MustBe.Zero bits 1-2)");
        _output.WriteLine("=".PadRight(80, '='));
        _output.WriteLine("");

        // Warmup all code paths to trigger JIT compilation
        Warmup_Unconstrained();
        Warmup_Constrained();

        var sw = new Stopwatch();

        // Ordered list of benchmarks to run
        var benchmarkNames = new[] { "Construction", "Bitwise OR", "Shift << 1", "Shift << 4", "Shift << 7", "Shift >> 4" };

        var results = new Dictionary<string, List<(double U, double C)>>();
        foreach (var name in benchmarkNames)
            results[name] = [];

        // Mapping of names to (unconstrained, constrained) benchmark method pairs
        var benchmarks = new Dictionary<string, (Func<int> U, Func<int> C)>
        {
            ["Construction"] = (Unconstrained_Construction, Constrained_Construction),
            ["Bitwise OR"]   = (Unconstrained_BitwiseOr, Constrained_BitwiseOr),
            ["Shift << 1"]   = (Unconstrained_ShiftLeft1, Constrained_ShiftLeft1),
            ["Shift << 4"]   = (Unconstrained_ShiftLeft4, Constrained_ShiftLeft4),
            ["Shift << 7"]   = (Unconstrained_ShiftLeft7, Constrained_ShiftLeft7),
            ["Shift >> 4"]   = (Unconstrained_ShiftRight4, Constrained_ShiftRight4),
        };

        // Run warmup pass of each benchmark method to pre-JIT the specific hot loops
        foreach (var (_, (u, c)) in benchmarks)
        {
            u();
            c();
        }

        for (int run = 0; run < FULL_SUITE_RUNS; run++)
        {
            // Alternate ordering every run to eliminate ordering bias
            bool constrainedFirst = run % 2 == 0;

            foreach (var name in benchmarkNames)
            {
                var (u, c) = benchmarks[name];
                double uTime, cTime;
                if (constrainedFirst)
                {
                    sw.Restart(); c(); sw.Stop(); cTime = sw.Elapsed.TotalMilliseconds;
                    sw.Restart(); u(); sw.Stop(); uTime = sw.Elapsed.TotalMilliseconds;
                }
                else
                {
                    sw.Restart(); u(); sw.Stop(); uTime = sw.Elapsed.TotalMilliseconds;
                    sw.Restart(); c(); sw.Stop(); cTime = sw.Elapsed.TotalMilliseconds;
                }
                results[name].Add((uTime, cTime));
            }
        }

        // Calculate statistics and print table
        int discard = FULL_SUITE_RUNS / 10;

        _output.WriteLine($"{"Operation",-16} {"Unconstrained",-20} {"Constrained",-20} {"Overhead"}");
        _output.WriteLine("-".PadRight(78, '-'));

        foreach (var name in benchmarkNames)
        {
            var data = results[name];
            var uStats = CalculateStats(data.Select(d => d.U).ToList(), discard);
            var cStats = CalculateStats(data.Select(d => d.C).ToList(), discard);
            var ratios = data.Select(d => d.C / d.U).ToList();
            var rStats = CalculateStats(ratios, discard);

            string overhead = rStats.Mean < 1.10
                ? "~0% (noise)"
                : $"+{(rStats.Mean - 1) * 100:F0}% ({rStats.Mean:F1}x)";

            _output.WriteLine(
                $"{name,-16} {uStats.Mean,5:F0} ms (σ={uStats.StdDev,3:F0})      " +
                $"{cStats.Mean,5:F0} ms (σ={cStats.StdDev,3:F0})      {overhead}");
        }

        _output.WriteLine("-".PadRight(78, '-'));
        _output.WriteLine("");
        _output.WriteLine("Key observations:");
        _output.WriteLine("- Construction and bitwise: negligible overhead (single AND+OR normalization).");
        _output.WriteLine("- Shift operators: overhead proportional to shift count (N iterative steps).");
        _output.WriteLine("- Shift << 7 on byte storage is the worst case (7 normalization steps).");
        _output.WriteLine("  Larger types (uint, ulong) shift fewer positions relative to their width,");
        _output.WriteLine("  so the relative overhead is lower for typical use cases.");
        _output.WriteLine("- Property getters/setters are unaffected by MustBe constraints (not shown).");
    }

    #region Benchmark Methods

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Unconstrained_Construction()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
            sum += (byte)new GeneratedTestRegister((byte)i);
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Constrained_Construction()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
            sum += (byte)new MustBeTestReg((byte)i);
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Unconstrained_BitwiseOr()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            GeneratedTestRegister a = (byte)i;
            GeneratedTestRegister b = (byte)(i >> 4);
            sum += (byte)(a | b);
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Constrained_BitwiseOr()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            MustBeTestReg a = (byte)i;
            MustBeTestReg b = (byte)(i >> 4);
            sum += (byte)(a | b);
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Unconstrained_ShiftLeft1()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
            sum += (byte)(new GeneratedTestRegister((byte)i) << 1);
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Constrained_ShiftLeft1()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
            sum += (byte)(new MustBeTestReg((byte)i) << 1);
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Unconstrained_ShiftLeft4()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
            sum += (byte)(new GeneratedTestRegister((byte)i) << 4);
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Constrained_ShiftLeft4()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
            sum += (byte)(new MustBeTestReg((byte)i) << 4);
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Unconstrained_ShiftLeft7()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
            sum += (byte)(new GeneratedTestRegister((byte)i) << 7);
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Constrained_ShiftLeft7()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
            sum += (byte)(new MustBeTestReg((byte)i) << 7);
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Unconstrained_ShiftRight4()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
            sum += (byte)(new GeneratedTestRegister((byte)i) >> 4);
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Constrained_ShiftRight4()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
            sum += (byte)(new MustBeTestReg((byte)i) >> 4);
        return sum;
    }

    private void Warmup_Unconstrained()
    {
        for (int i = 0; i < WARMUP_ITERATIONS; i++)
        {
            GeneratedTestRegister reg = (byte)i;
            _ = (byte)(reg << 1);
            _ = (byte)(reg >> 1);
            _ = (byte)(reg | reg);
        }
    }

    private void Warmup_Constrained()
    {
        for (int i = 0; i < WARMUP_ITERATIONS; i++)
        {
            MustBeTestReg reg = (byte)i;
            _ = (byte)(reg << 1);
            _ = (byte)(reg >> 1);
            _ = (byte)(reg | reg);
        }
    }

    #endregion

    #region Helpers

    private static (double Mean, double StdDev) CalculateStats(List<double> values, int discard = 0)
    {
        values.Sort();
        var trimmed = values.Skip(discard).Take(values.Count - 2 * discard).ToList();
        double mean = trimmed.Average();
        double variance = trimmed.Sum(v => Math.Pow(v - mean, 2)) / trimmed.Count;
        return (mean, Math.Sqrt(variance));
    }

    #endregion
}
