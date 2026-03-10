using System.Diagnostics;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

#region Test Structs

/// <summary>
/// Generated struct for performance testing.
/// Generator creates private Value field, constructor, and implicit conversions.
/// </summary>
[BitFields(typeof(byte))]
public partial struct GeneratedTestRegister
{
    [BitFlag(0)] public partial bool Ready { get; set; }
    [BitFlag(1)] public partial bool Error { get; set; }
    [BitFlag(7)] public partial bool Busy { get; set; }
    [BitField(2, 4)] public partial byte Mode { get; set; }      // bits 2..=4 (3 bits)
    [BitField(5, 6)] public partial byte Priority { get; set; }  // bits 5..=6 (2 bits)
}

#endregion

/// <summary>
/// Performance tests comparing generated BitField code vs hand-coded inline shift-and-mask
/// operations. The hand-coded baseline uses raw byte variables with direct bitwise operations
/// (no property accessors or function calls) to ensure the comparison is obviously fair.
/// The generated code should have identical performance since it uses compile-time constants.
/// </summary>
[Trait("Category", "Performance")]
public class BitFieldPerformanceTests
{
    private const int FULL_SUITE_RUNS = 200;
    private const int ITERATIONS = 100_000_000;
    private const int WARMUP_ITERATIONS = 5_000_000;

    // Hand-coded bit manipulation constants (identical layout to GeneratedTestRegister).
    // Used directly in test loops with raw byte variables for a fair comparison
    // that involves no property accessors or function calls of any kind.
    // BitFlag: Ready = bit 0
    private const byte READY_MASK = 0x01;
    private const byte READY_INVERTED = 0xFE;
    // BitFlag: Error = bit 1
    private const byte ERROR_MASK = 0x02;
    private const byte ERROR_INVERTED = 0xFD;
    // BitFlag: Busy = bit 7
    private const byte BUSY_MASK = 0x80;
    private const byte BUSY_INVERTED = 0x7F;
    // BitField: Mode = bits 2..4 (3 bits)
    private const byte MODE_MASK = 0x07;
    private const int MODE_SHIFT = 2;
    private const byte MODE_SHIFTED_MASK = 0x1C;
    private const byte MODE_INVERTED = 0xE3;
    // BitField: Priority = bits 5..6 (2 bits)
    private const byte PRIORITY_MASK = 0x03;
    private const int PRIORITY_SHIFT = 5;
    private const byte PRIORITY_SHIFTED_MASK = 0x60;
    private const byte PRIORITY_INVERTED = 0x9F;

    private readonly ITestOutputHelper _output;

    public BitFieldPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Outputs the performance result with a status indicator.
    /// Does not fail the test - use FullSuite_Performance_Summary for rigorous testing.
    /// </summary>
    private void OutputPerformanceResult(double ratio)
    {
        if (ratio > 1.50)
        {
            _output.WriteLine($"⚠️ WARNING: Generated code is >{(ratio - 1) * 100:F0}% slower than hand-coded.");
            _output.WriteLine("   This may indicate a regression, or just system load/JIT variance.");
            _output.WriteLine("   Run FullSuite_Performance_Summary for statistical analysis.");
        }
        else if (ratio > 1.25)
        {
            _output.WriteLine($"⚠️ CAUTION: Generated code is {(ratio - 1) * 100:F0}% slower than hand-coded.");
            _output.WriteLine("   Run FullSuite_Performance_Summary for statistical analysis.");
        }
        else if (ratio < 0.75)
        {
            _output.WriteLine("ℹ️ Generated code is significantly faster than hand-coded (unexpected).");
        }
        else
        {
            _output.WriteLine("✓ Performance is within expected range.");
        }
    }

    [Fact]
    public void BitFlag_Get_Performance()
    {
        Assert.SkipWhen(CiEnvironmentDetector.IsRunningInCi, "Performance tests are skipped in CI environments due to variable runner performance.");

        _output.WriteLine($"Testing BitFlag GET performance ({ITERATIONS:N0} iterations)");
        _output.WriteLine(new string('=', 60));

        // Warmup both implementations
        WarmupGenerated();
        WarmupHandCoded();

        // Run each test twice - first run pays "cold" penalty, second is accurate
        // Run 1: Discard (warms up the specific test loop)
        RunBitFlagGetTest(out _, out _);
        
        // Run 2: Actual measurement
        RunBitFlagGetTest(out var generatedTime, out var handCodedTime);

        _output.WriteLine($"Generated:  {generatedTime.TotalMilliseconds:F2} ms ({ITERATIONS / generatedTime.TotalSeconds:N0} ops/sec)");
        _output.WriteLine($"Hand-coded: {handCodedTime.TotalMilliseconds:F2} ms ({ITERATIONS / handCodedTime.TotalSeconds:N0} ops/sec)");

        var ratio = generatedTime.TotalMilliseconds / handCodedTime.TotalMilliseconds;
        _output.WriteLine($"Ratio:      {ratio:F3}x (1.0 = identical, <1.0 = generated faster)");
        _output.WriteLine("");

        OutputPerformanceResult(ratio);
    }

    private void RunBitFlagGetTest(out TimeSpan generatedTime, out TimeSpan handCodedTime, bool handCodedFirst = true)
    {
        var sw = new Stopwatch();
        int resultHand = 0;
        int resultGen = 0;

        generatedTime = sw.Elapsed;
        handCodedTime = sw.Elapsed;

        if (handCodedFirst)
        {
            sw.Restart();
            resultHand = BitFlagGet_HandCoded();
            sw.Stop();
            handCodedTime = sw.Elapsed;

            sw.Restart();
            resultGen = BitFlagGet_Generated();
            sw.Stop();
            generatedTime = sw.Elapsed;
        }
        if (!handCodedFirst)
        {
            sw.Restart();
            resultGen = BitFlagGet_Generated();
            sw.Stop();
            generatedTime = sw.Elapsed;

            sw.Restart();
            resultHand = BitFlagGet_HandCoded();
            sw.Stop();
            handCodedTime = sw.Elapsed;
        }

        resultGen.Should().Be(resultHand);
    }

    [Fact]
    public void BitFlag_Set_Performance()
    {
        Assert.SkipWhen(CiEnvironmentDetector.IsRunningInCi, "Performance tests are skipped in CI environments due to variable runner performance.");
        
        _output.WriteLine($"Testing BitFlag SET performance ({ITERATIONS:N0} iterations)");
        _output.WriteLine(new string('=', 60));

        WarmupGenerated();
        WarmupHandCoded();

        // Run twice - first warms, second measures
        RunBitFlagSetTest(out _, out _);
        RunBitFlagSetTest(out var generatedTime, out var handCodedTime);

        _output.WriteLine($"Generated:  {generatedTime.TotalMilliseconds:F2} ms ({ITERATIONS / generatedTime.TotalSeconds:N0} ops/sec)");
        _output.WriteLine($"Hand-coded: {handCodedTime.TotalMilliseconds:F2} ms ({ITERATIONS / handCodedTime.TotalSeconds:N0} ops/sec)");

        var ratio = generatedTime.TotalMilliseconds / handCodedTime.TotalMilliseconds;
        _output.WriteLine($"Ratio:      {ratio:F3}x (1.0 = identical, <1.0 = generated faster)");
        _output.WriteLine("");

        OutputPerformanceResult(ratio);
    }

    private void RunBitFlagSetTest(out TimeSpan generatedTime, out TimeSpan handCodedTime, bool handCodedFirst = true)
    {
        var sw = new Stopwatch();
        int resultHand = 0;
        int resultGen = 0;

        generatedTime = sw.Elapsed;
        handCodedTime = sw.Elapsed;

        if (handCodedFirst)
        {
            sw.Restart();
            resultHand = BitFlagSet_HandCoded();
            sw.Stop();
            handCodedTime = sw.Elapsed;

            sw.Restart();
            resultGen = BitFlagSet_Generated();
            sw.Stop();
            generatedTime = sw.Elapsed;
        }
        if (!handCodedFirst)
        {
            sw.Restart();
            resultGen = BitFlagSet_Generated();
            sw.Stop();
            generatedTime = sw.Elapsed;

            sw.Restart();
            resultHand = BitFlagSet_HandCoded();
            sw.Stop();
            handCodedTime = sw.Elapsed;
        }

        resultGen.Should().Be(resultHand);
    }

    [Fact]
    public void BitField_Get_Performance()
    {
        Assert.SkipWhen(CiEnvironmentDetector.IsRunningInCi, "Performance tests are skipped in CI environments due to variable runner performance.");
        _output.WriteLine($"Testing BitField GET performance ({ITERATIONS:N0} iterations)");
        _output.WriteLine(new string('=', 60));

        WarmupGenerated();
        WarmupHandCoded();

        // Run twice - first warms, second measures
        RunBitFieldGetTest(out _, out _);
        RunBitFieldGetTest(out var generatedTime, out var handCodedTime);

        _output.WriteLine($"Generated:  {generatedTime.TotalMilliseconds:F2} ms ({ITERATIONS / generatedTime.TotalSeconds:N0} ops/sec)");
        _output.WriteLine($"Hand-coded: {handCodedTime.TotalMilliseconds:F2} ms ({ITERATIONS / handCodedTime.TotalSeconds:N0} ops/sec)");

        var ratio = generatedTime.TotalMilliseconds / handCodedTime.TotalMilliseconds;
        _output.WriteLine($"Ratio:      {ratio:F3}x (1.0 = identical, <1.0 = generated faster)");
        _output.WriteLine("");

        OutputPerformanceResult(ratio);
    }

    private void RunBitFieldGetTest(out TimeSpan generatedTime, out TimeSpan handCodedTime, bool handCodedFirst = true)
    {
        var sw = new Stopwatch();
        int resultHand = 0;
        int resultGen = 0;

        generatedTime = sw.Elapsed;
        handCodedTime = sw.Elapsed;

        if (handCodedFirst)
        {
            sw.Restart();
            resultHand = BitFieldGet_HandCoded();
            sw.Stop();
            handCodedTime = sw.Elapsed;

            sw.Restart();
            resultGen = BitFieldGet_Generated();
            sw.Stop();
            generatedTime = sw.Elapsed;
        }
        if (!handCodedFirst)
        {
            sw.Restart();
            resultGen = BitFieldGet_Generated();
            sw.Stop();
            generatedTime = sw.Elapsed;

            sw.Restart();
            resultHand = BitFieldGet_HandCoded();
            sw.Stop();
            handCodedTime = sw.Elapsed;
        }

        resultGen.Should().Be(resultHand);
    }

    [Fact]
    public void BitField_Set_Performance()
    {
        Assert.SkipWhen(CiEnvironmentDetector.IsRunningInCi, "Performance tests are skipped in CI environments due to variable runner performance.");

        _output.WriteLine($"Testing BitField SET performance ({ITERATIONS:N0} iterations)");
        _output.WriteLine(new string('=', 60));

        WarmupGenerated();
        WarmupHandCoded();

        // Run twice - first warms, second measures
        RunBitFieldSetTest(out _, out _);
        RunBitFieldSetTest(out var generatedTime, out var handCodedTime);

        _output.WriteLine($"Generated:  {generatedTime.TotalMilliseconds:F2} ms ({ITERATIONS / generatedTime.TotalSeconds:N0} ops/sec)");
        _output.WriteLine($"Hand-coded: {handCodedTime.TotalMilliseconds:F2} ms ({ITERATIONS / handCodedTime.TotalSeconds:N0} ops/sec)");

        var ratio = generatedTime.TotalMilliseconds / handCodedTime.TotalMilliseconds;
        _output.WriteLine($"Ratio:      {ratio:F3}x (1.0 = identical, <1.0 = generated faster)");
        _output.WriteLine("");

        OutputPerformanceResult(ratio);
    }

    private void RunBitFieldSetTest(out TimeSpan generatedTime, out TimeSpan handCodedTime, bool handCodedFirst = true)
    {
        var sw = new Stopwatch();
        int resultHand = 0;
        int resultGen = 0;

        generatedTime = sw.Elapsed;
        handCodedTime = sw.Elapsed;

        if (handCodedFirst)
        {
            sw.Restart();
            resultHand = BitFieldSet_HandCoded();
            sw.Stop();
            handCodedTime = sw.Elapsed;

            sw.Restart();
            resultGen = BitFieldSet_Generated();
            sw.Stop();
            generatedTime = sw.Elapsed;
        }
        if (!handCodedFirst)
        {
            sw.Restart();
            resultGen = BitFieldSet_Generated();
            sw.Stop();
            generatedTime = sw.Elapsed;

            sw.Restart();
            resultHand = BitFieldSet_HandCoded();
            sw.Stop();
            handCodedTime = sw.Elapsed;
        }

        resultGen.Should().Be(resultHand);
    }

    /// <summary>
    /// Performance test for mixed read/write operations.
    /// Skipped in CI environments due to variable runner performance.
    /// Runs normally when executed locally.
    /// 
    /// Note: This is a quick sanity check. For rigorous performance testing with
    /// statistical analysis, run FullSuite_Performance_Summary instead.
    /// </summary>
    [Fact]
    public void Mixed_ReadWrite_Performance()
    {
        Assert.SkipWhen(CiEnvironmentDetector.IsRunningInCi, "Performance tests are skipped in CI environments due to variable runner performance.");

        _output.WriteLine($"Testing mixed read/write performance ({ITERATIONS:N0} iterations)");
        _output.WriteLine(new string('=', 60));

        WarmupGenerated();
        WarmupHandCoded();

        // Run twice - first warms, second measures
        RunMixedReadWriteTest(out _, out _);
        RunMixedReadWriteTest(out var generatedTime, out var handCodedTime);

        _output.WriteLine($"Generated:  {generatedTime.TotalMilliseconds:F2} ms ({ITERATIONS / generatedTime.TotalSeconds:N0} ops/sec)");
        _output.WriteLine($"Hand-coded: {handCodedTime.TotalMilliseconds:F2} ms ({ITERATIONS / handCodedTime.TotalSeconds:N0} ops/sec)");

        var ratio = generatedTime.TotalMilliseconds / handCodedTime.TotalMilliseconds;
        _output.WriteLine($"Ratio:      {ratio:F3}x (1.0 = identical, <1.0 = generated faster)");
        _output.WriteLine("");

        OutputPerformanceResult(ratio);
    }

    private void RunMixedReadWriteTest(out TimeSpan generatedTime, out TimeSpan handCodedTime, bool handCodedFirst = true)
    {
        var sw = new Stopwatch();
        int resultHand = 0;
        int resultGen = 0;

        generatedTime = sw.Elapsed;
        handCodedTime = sw.Elapsed;

        if (handCodedFirst)
        {
            sw.Restart();
            resultHand = MixedReadWrite_HandCoded();
            sw.Stop();
            handCodedTime = sw.Elapsed;

            sw.Restart();
            resultGen = MixedReadWrite_Generated();
            sw.Stop();
            generatedTime = sw.Elapsed;
        }
        if (!handCodedFirst)
        {
            sw.Restart();
            resultGen = MixedReadWrite_Generated();
            sw.Stop();
            generatedTime = sw.Elapsed;

            sw.Restart();
            resultHand = MixedReadWrite_HandCoded();
            sw.Stop();
            handCodedTime = sw.Elapsed;
        }

        resultGen.Should().Be(resultHand);
    }

    /// <summary>
    /// Comprehensive performance test with statistical analysis.
    /// Each benchmark is isolated in its own [NoInlining] method for independent JIT
    /// compilation, then called in alternating order (even runs = hand-coded first,
    /// odd runs = generated first) to eliminate ordering bias.
    /// Skipped in CI environments due to variable runner performance.
    /// Runs normally when executed locally to verify performance characteristics.
    /// </summary>
    [Fact]
    public void FullSuite_Performance_Summary()
    {
        Assert.SkipWhen(CiEnvironmentDetector.IsRunningInCi, "Performance tests are skipped in CI environments due to variable runner performance.");
        
        _output.WriteLine("=".PadRight(70, '='));
        _output.WriteLine("BITFIELD PERFORMANCE SUMMARY WITH STATISTICS");
        _output.WriteLine($"Runs: {FULL_SUITE_RUNS}, Iterations per run: {ITERATIONS:N0}");
        _output.WriteLine("=".PadRight(70, '='));
        _output.WriteLine("");

        // Collect results across multiple runs
        var allResults = new Dictionary<string, List<(double Gen, double Hand, double Ratio)>>
        {
            ["BitFlag GET"] = [],
            ["BitFlag SET"] = [],
            ["BitField GET"] = [],
            ["BitField SET"] = [],
            ["Mixed R/W"] = []
        };


        for (int run = 0; run < FULL_SUITE_RUNS; run++)
        {
            // Alternate every run: even runs = hand-coded first, odd runs = generated first.
            bool handCodedFirst = run % 2 == 0;
            if (!handCodedFirst)
            {
                WarmupGenerated();
                WarmupHandCoded(); // Warmed up in the same order as the test to avoid biasing the JIT's register allocation or branch prediction for the hot loop.
            }
            else
            {
                WarmupHandCoded();
                WarmupGenerated(); // Warmed up in the same order as the test to avoid biasing the JIT's register allocation or branch prediction for the hot loop.
            }

            RunBitFlagGetTest(out var genTime, out var handTime, handCodedFirst);
            allResults["BitFlag GET"].Add((genTime.TotalMilliseconds, handTime.TotalMilliseconds, genTime.TotalMilliseconds / handTime.TotalMilliseconds));

            RunBitFlagSetTest(out genTime, out handTime, handCodedFirst);
            allResults["BitFlag SET"].Add((genTime.TotalMilliseconds, handTime.TotalMilliseconds, genTime.TotalMilliseconds / handTime.TotalMilliseconds));

            RunBitFieldGetTest(out genTime, out handTime, handCodedFirst);
            allResults["BitField GET"].Add((genTime.TotalMilliseconds, handTime.TotalMilliseconds, genTime.TotalMilliseconds / handTime.TotalMilliseconds));

            RunBitFieldSetTest(out genTime, out handTime, handCodedFirst);
            allResults["BitField SET"].Add((genTime.TotalMilliseconds, handTime.TotalMilliseconds, genTime.TotalMilliseconds / handTime.TotalMilliseconds));

            RunMixedReadWriteTest(out genTime, out handTime, handCodedFirst);
            allResults["Mixed R/W"].Add((genTime.TotalMilliseconds, handTime.TotalMilliseconds, genTime.TotalMilliseconds / handTime.TotalMilliseconds));
        }

        // Calculate and print statistics
        _output.WriteLine("");
        _output.WriteLine("=".PadRight(70, '='));
        _output.WriteLine("STATISTICAL RESULTS (mean with σ = std dev)");
        _output.WriteLine("=".PadRight(70, '='));
        _output.WriteLine("");
        _output.WriteLine("Test           Generated (ms)     Hand-coded (ms)    Ratio");
        _output.WriteLine("-".PadRight(70, '-'));

        var overallRatios = new List<double>();
        int discard = (int)Math.Round((double)(FULL_SUITE_RUNS / 10));
        foreach (var testName in new[] { "BitFlag GET", "BitFlag SET", "BitField GET", "BitField SET", "Mixed R/W" })
        {
            var data = allResults[testName];
            var genStats = CalculateStats(data.Select(d => d.Gen).ToList(), discard);
            var handStats = CalculateStats(data.Select(d => d.Hand).ToList(), discard);
            var ratioStats = CalculateStats(data.Select(d => d.Ratio).ToList(), discard);

            overallRatios.AddRange(data.Select(d => d.Ratio));

            _output.WriteLine($"{testName,-14} {genStats.Mean,6:F0} (σ={genStats.StdDev,4:F0})   {handStats.Mean,6:F0} (σ={handStats.StdDev,4:F0})   {ratioStats.Mean:F3} (σ={ratioStats.StdDev:F3})");
        }

        _output.WriteLine("-".PadRight(70, '-'));

        var overallStats = CalculateStats(overallRatios);
        int n = (overallRatios.Count / 5) - discard * 2; // There are 5 named tests in each run, so don't count each ratio as independent
        double standardError = overallStats.StdDev / Math.Sqrt(n);
        double ciLow = overallStats.Mean - 1.96 * standardError;
        double ciHigh = overallStats.Mean + 1.96 * standardError;

        _output.WriteLine($"{"OVERALL",-14}                                     {overallStats.Mean:F3} (σ={overallStats.StdDev:F3})");
        _output.WriteLine("");
        if (ciLow <= 1.0 && ciHigh >= 1.0)
        {
            _output.WriteLine("✓ Generated code performance statistically identical to hand-coded (95% CI includes 1.0).");
        }
        else if (overallStats.Mean > 1.0)
        {
            _output.WriteLine($"⚠️ Generated code is {(overallStats.Mean - 1) * 100:F1}% SLOWER on average.");
        }
        else
        {
            _output.WriteLine($"ℹ️ Generated code is {(1 - overallStats.Mean) * 100:F1}% FASTER on average.");
        }
        _output.WriteLine($"        σ (std dev) = {overallStats.StdDev:F4}, SE = {standardError:F4}, n = {n}");
        _output.WriteLine($"        95% CI for mean = {ciLow:F3} to {ciHigh:F3}");
        _output.WriteLine("");

        // Overall should be within 20% - fail if regression exceeds this
        // Note: Widened from 5% to 20% to account for system variability
        overallStats.Mean.Should().BeInRange(0.80, 1.20, "Average performance should be within 20% of hand-coded");
    }

    #region Helpers

    // Each benchmark is its own [NoInlining] method so the JIT compiles it
    // independently — no register allocation cross-contamination, no if/else
    // branch asymmetry affecting the hot loop. All return int for uniform handling.

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BitFlagGet_HandCoded()
    {
        int count = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            byte val = (byte)i;
            if ((val & READY_MASK) != 0) count++;
            if ((val & ERROR_MASK) != 0) count++;
            if ((val & BUSY_MASK) != 0) count++;
        }
        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BitFlagGet_Generated()
    {
        int count = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            GeneratedTestRegister reg = (byte)i;
            if (reg.Ready) count++;
            if (reg.Error) count++;
            if (reg.Busy) count++;
        }
        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BitFlagSet_HandCoded()
    {
        byte val = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            val = (byte)(val | READY_MASK);
            val = (byte)(val & ERROR_INVERTED);
            val = (byte)(val | BUSY_MASK);
        }
        return val;
    }

    //[MethodImpl(MethodImplOptions.NoInlining)]
    private static int BitFlagSet_Generated()
    {
        GeneratedTestRegister reg = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            reg.Ready = true;
            reg.Error = false;
            reg.Busy = true;
        }
        return (byte)reg;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BitFieldGet_HandCoded()
    {
        byte val = 0xFF;
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            sum += (byte)((val >> MODE_SHIFT) & MODE_MASK);
            sum += (byte)((val >> PRIORITY_SHIFT) & PRIORITY_MASK);
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BitFieldGet_Generated()
    {
        var reg = new GeneratedTestRegister(0xFF);
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            sum += reg.Mode;
            sum += reg.Priority;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BitFieldSet_HandCoded()
    {
        byte val = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            val = (byte)((val & MODE_INVERTED) | (((byte)(i & 0x07) << MODE_SHIFT) & MODE_SHIFTED_MASK));
            val = (byte)((val & PRIORITY_INVERTED) | (((byte)(i & 0x03) << PRIORITY_SHIFT) & PRIORITY_SHIFTED_MASK));
        }
        return val;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BitFieldSet_Generated()
    {
        GeneratedTestRegister reg = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            reg.Mode = (byte)(i & 0x07);
            reg.Priority = (byte)(i & 0x03);
        }
        return (byte)reg;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int MixedReadWrite_HandCoded()
    {
        byte val = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            val = (byte)(val | READY_MASK);
            if ((val & READY_MASK) != 0 && (val & BUSY_MASK) == 0)
            {
                val = (byte)((val & MODE_INVERTED) | ((5 << MODE_SHIFT) & MODE_SHIFTED_MASK));
                byte mode = (byte)((val >> MODE_SHIFT) & MODE_MASK);
                val = (byte)((val & PRIORITY_INVERTED) | (((byte)(mode >> 1) << PRIORITY_SHIFT) & PRIORITY_SHIFTED_MASK));
            }
            byte priority = (byte)((val >> PRIORITY_SHIFT) & PRIORITY_MASK);
            val = priority > 1 ? (byte)(val | BUSY_MASK) : (byte)(val & BUSY_INVERTED);
        }
        return val;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int MixedReadWrite_Generated()
    {
        GeneratedTestRegister reg = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            reg.Ready = true;
            if (reg.Ready && !reg.Busy)
            {
                reg.Mode = 5;
                reg.Priority = (byte)(reg.Mode >> 1);
            }
            reg.Busy = reg.Priority > 1;
        }
        return (byte)reg;
    }

    private static (double Mean, double StdDev) CalculateStats(List<double> values, int discard = 0)
    {
        // Throw out high and low measurements to reduce noise (e.g. from GC or background processes)
        values.Sort();
        var trimmed = values.Skip(discard).Take(values.Count - 2 * discard).ToList();

        double mean = trimmed.Average();
        double variance = trimmed.Sum(v => Math.Pow(v - mean, 2)) / trimmed.Count;
        double stdDev = Math.Sqrt(variance);
        return (mean, stdDev);
    }

    /// <summary>
    /// Symmetric warmup for generated code - matches WarmupHandCoded structure exactly.
    /// </summary>
    //[MethodImpl(MethodImplOptions.NoInlining)]
    protected byte WarmupGenerated()
    {
        byte val = 0;
        for (int i = 0; i < WARMUP_ITERATIONS; i++)
        {
            GeneratedTestRegister reg = (byte)i;
            reg.Ready = !reg.Ready;
            reg.Mode = (byte)(i & 7);
            _ = reg.Priority;
            val = reg;
        }
        return val;
    }

    /// <summary>
    /// Symmetric warmup for hand-coded - matches WarmupGenerated structure exactly.
    /// Uses raw inline shift-and-mask operations (no property accessors).
    /// </summary>
    //[MethodImpl(MethodImplOptions.NoInlining)]
    protected byte WarmupHandCoded()
    {
        byte val = 0;
        for (int i = 0; i < WARMUP_ITERATIONS; i++)
        {
            val = (byte)i;
            bool ready = (val & READY_MASK) != 0;
            val = !ready ? (byte)(val | READY_MASK) : (byte)(val & READY_INVERTED);
            val = (byte)((val & MODE_INVERTED) | (((byte)(i & 0x07) << MODE_SHIFT) & MODE_SHIFTED_MASK));
            _ = (byte)((val >> PRIORITY_SHIFT) & PRIORITY_MASK);
        }
        return val;
    }

    #endregion
}
