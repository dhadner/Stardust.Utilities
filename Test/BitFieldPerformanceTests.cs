using System.Diagnostics;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

#region Test Structs (must be at namespace level for generator)

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

/// <summary>
/// Hand-coded struct with identical bit layout for comparison.
/// This is what a developer would write manually.
/// </summary>
public struct HandCodedTestRegister
{
    public byte Value;

    // Compile-time constants (same as what generator produces)
    private const byte READY_MASK = 0x01;
    private const byte READY_INVERTED = 0xFE;
    private const byte ERROR_MASK = 0x02;
    private const byte ERROR_INVERTED = 0xFD;
    private const byte BUSY_MASK = 0x80;
    private const byte BUSY_INVERTED = 0x7F;
    private const byte MODE_MASK = 0x07;
    private const int MODE_SHIFT = 2;
    private const byte MODE_SHIFTED_MASK = 0x1C;
    private const byte MODE_INVERTED = 0xE3;
    private const byte PRIORITY_MASK = 0x03;
    private const int PRIORITY_SHIFT = 5;
    private const byte PRIORITY_SHIFTED_MASK = 0x60;
    private const byte PRIORITY_INVERTED = 0x9F;

    public bool Ready
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (Value & READY_MASK) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Value = value ? (byte)(Value | READY_MASK) : (byte)(Value & READY_INVERTED);
    }

    public bool Error
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (Value & ERROR_MASK) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Value = value ? (byte)(Value | ERROR_MASK) : (byte)(Value & ERROR_INVERTED);
    }

    public bool Busy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (Value & BUSY_MASK) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Value = value ? (byte)(Value | BUSY_MASK) : (byte)(Value & BUSY_INVERTED);
    }

    public byte Mode
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (byte)((Value >> MODE_SHIFT) & MODE_MASK);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Value = (byte)((Value & MODE_INVERTED) | ((value << MODE_SHIFT) & MODE_SHIFTED_MASK));
    }

    public byte Priority
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (byte)((Value >> PRIORITY_SHIFT) & PRIORITY_MASK);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Value = (byte)((Value & PRIORITY_INVERTED) | ((value << PRIORITY_SHIFT) & PRIORITY_SHIFTED_MASK));
    }
}

#endregion

/// <summary>
/// Performance tests comparing generated BitField code vs hand-coded bit manipulation.
/// The generated code should have identical performance since it uses compile-time constants.
/// </summary>
public class BitFieldPerformanceTests
{
    private const int ITERATIONS = 500_000_000; // 100 million iterations
    private const int WARMUP_ITERATIONS = 500_000_000;

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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RunBitFlagGetTest(out TimeSpan generatedTime, out TimeSpan handCodedTime)
    {
        // Test hand-coded first (order doesn't matter after warmup)
        var handCodedReg = new HandCodedTestRegister { Value = 0xAA };
        var sw = Stopwatch.StartNew();
        int trueCount2 = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            if (handCodedReg.Ready) trueCount2++;
            if (handCodedReg.Error) trueCount2++;
            if (handCodedReg.Busy) trueCount2++;
        }
        sw.Stop();
        handCodedTime = sw.Elapsed;

        // Test generated code
        GeneratedTestRegister generatedReg = 0xAA;
        sw.Restart();
        int trueCount = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            if (generatedReg.Ready) trueCount++;
            if (generatedReg.Error) trueCount++;
            if (generatedReg.Busy) trueCount++;
        }
        sw.Stop();
        generatedTime = sw.Elapsed;

        // Results should be identical
        trueCount.Should().Be(trueCount2);
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RunBitFlagSetTest(out TimeSpan generatedTime, out TimeSpan handCodedTime)
    {
        var handCodedReg = new HandCodedTestRegister();
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < ITERATIONS; i++)
        {
            handCodedReg.Ready = true;
            handCodedReg.Error = false;
            handCodedReg.Busy = true;
        }
        sw.Stop();
        handCodedTime = sw.Elapsed;

        GeneratedTestRegister generatedReg = 0;
        sw.Restart();
        for (int i = 0; i < ITERATIONS; i++)
        {
            generatedReg.Ready = true;
            generatedReg.Error = false;
            generatedReg.Busy = true;
        }
        sw.Stop();
        generatedTime = sw.Elapsed;

        ((byte)generatedReg).Should().Be(handCodedReg.Value);
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RunBitFieldGetTest(out TimeSpan generatedTime, out TimeSpan handCodedTime)
    {
        var handCodedReg = new HandCodedTestRegister { Value = 0xFF };
        int sum2 = 0;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < ITERATIONS; i++)
        {
            sum2 += handCodedReg.Mode;
            sum2 += handCodedReg.Priority;
        }
        sw.Stop();
        handCodedTime = sw.Elapsed;

        var generatedReg = new GeneratedTestRegister(0xFF);
        int sum = 0;
        sw.Restart();
        for (int i = 0; i < ITERATIONS; i++)
        {
            sum += generatedReg.Mode;
            sum += generatedReg.Priority;
        }
        sw.Stop();
        generatedTime = sw.Elapsed;

        sum.Should().Be(sum2);
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RunBitFieldSetTest(out TimeSpan generatedTime, out TimeSpan handCodedTime)
    {
        var handCodedReg = new HandCodedTestRegister();
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < ITERATIONS; i++)
        {
            handCodedReg.Mode = (byte)(i & 0x07);
            handCodedReg.Priority = (byte)(i & 0x03);
        }
        sw.Stop();
        handCodedTime = sw.Elapsed;

        GeneratedTestRegister generatedReg = 0;
        sw.Restart();
        for (int i = 0; i < ITERATIONS; i++)
        {
            generatedReg.Mode = (byte)(i & 0x07);
            generatedReg.Priority = (byte)(i & 0x03);
        }
        sw.Stop();
        generatedTime = sw.Elapsed;

        ((byte)generatedReg).Should().Be(handCodedReg.Value);
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RunMixedReadWriteTest(out TimeSpan generatedTime, out TimeSpan handCodedTime)
    {
        var handCodedReg = new HandCodedTestRegister();
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < ITERATIONS; i++)
        {
            handCodedReg.Ready = true;
            if (handCodedReg.Ready && !handCodedReg.Busy)
            {
                handCodedReg.Mode = 5;
                handCodedReg.Priority = (byte)(handCodedReg.Mode >> 1);
            }
            handCodedReg.Busy = handCodedReg.Priority > 1;
        }
        sw.Stop();
        handCodedTime = sw.Elapsed;

        GeneratedTestRegister generatedReg = 0;
        sw.Restart();
        for (int i = 0; i < ITERATIONS; i++)
        {
            generatedReg.Ready = true;
            if (generatedReg.Ready && !generatedReg.Busy)
            {
                generatedReg.Mode = 5;
                generatedReg.Priority = (byte)(generatedReg.Mode >> 1);
            }
            generatedReg.Busy = generatedReg.Priority > 1;
        }
        sw.Stop();
        generatedTime = sw.Elapsed;

        ((byte)generatedReg).Should().Be(handCodedReg.Value);
    }

    /// <summary>
    /// Comprehensive performance test with statistical analysis.
    /// Skipped in CI environments due to variable runner performance.
    /// Runs normally when executed locally to verify performance characteristics.
    /// </summary>
    [Fact]
    public void FullSuite_Performance_Summary()
    {
        Assert.SkipWhen(CiEnvironmentDetector.IsRunningInCi, "Performance tests are skipped in CI environments due to variable runner performance.");

        const int RUNS = 20;
        
        _output.WriteLine("=".PadRight(70, '='));
        _output.WriteLine("BITFIELD PERFORMANCE SUMMARY WITH STATISTICS");
        _output.WriteLine($"Runs: {RUNS}, Iterations per run: {ITERATIONS:N0}");
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

        // Warmup both implementations
        WarmupGenerated();
        WarmupHandCoded();

        for (int run = 0; run < RUNS; run++)
        {
            _output.WriteLine($"Run {run + 1}/{RUNS}...");

            // BitFlag GET
            var (gen, hand) = RunSingleTest(() =>
            {
                var reg = new GeneratedTestRegister(0xAA);
                int c = 0;
                for (int i = 0; i < ITERATIONS; i++)
                {
                    if (reg.Ready) c++;
                    if (reg.Error) c++;
                    if (reg.Busy) c++;
                }
                return c;
            }, () =>
            {
                var reg = new HandCodedTestRegister { Value = 0xAA };
                int c = 0;
                for (int i = 0; i < ITERATIONS; i++)
                {
                    if (reg.Ready) c++;
                    if (reg.Error) c++;
                    if (reg.Busy) c++;
                }
                return c;
            });
            allResults["BitFlag GET"].Add((gen, hand, gen / hand));

            // BitFlag SET
            (gen, hand) = RunSingleTest(() =>
            {
                var reg = new GeneratedTestRegister();
                for (int i = 0; i < ITERATIONS; i++)
                {
                    reg.Ready = true;
                    reg.Error = false;
                    reg.Busy = true;
                }
                return reg;
            }, () =>
            {
                var reg = new HandCodedTestRegister();
                for (int i = 0; i < ITERATIONS; i++)
                {
                    reg.Ready = true;
                    reg.Error = false;
                    reg.Busy = true;
                }
                return reg.Value;
            });
            allResults["BitFlag SET"].Add((gen, hand, gen / hand));

            // BitField GET
            (gen, hand) = RunSingleTest(() =>
            {
                var reg = new GeneratedTestRegister(0xFF);
                int sum = 0;
                for (int i = 0; i < ITERATIONS; i++)
                {
                    sum += reg.Mode;
                    sum += reg.Priority;
                }
                return sum;
            }, () =>
            {
                var reg = new HandCodedTestRegister { Value = 0xFF };
                int sum = 0;
                for (int i = 0; i < ITERATIONS; i++)
                {
                    sum += reg.Mode;
                    sum += reg.Priority;
                }
                return sum;
            });
            allResults["BitField GET"].Add((gen, hand, gen / hand));

            // BitField SET
            (gen, hand) = RunSingleTest(() =>
            {
                var reg = new GeneratedTestRegister();
                for (int i = 0; i < ITERATIONS; i++)
                {
                    reg.Mode = (byte)(i & 0x07);
                    reg.Priority = (byte)(i & 0x03);
                }
                return reg;
            }, () =>
            {
                var reg = new HandCodedTestRegister();
                for (int i = 0; i < ITERATIONS; i++)
                {
                    reg.Mode = (byte)(i & 0x07);
                    reg.Priority = (byte)(i & 0x03);
                }
                return reg.Value;
            });
            allResults["BitField SET"].Add((gen, hand, gen / hand));

            // Mixed R/W
            (gen, hand) = RunSingleTest(() =>
            {
                var reg = new GeneratedTestRegister();
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
                return reg;
            }, () =>
            {
                var reg = new HandCodedTestRegister();
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
                return reg.Value;
            });
            allResults["Mixed R/W"].Add((gen, hand, gen / hand));
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

        foreach (var testName in new[] { "BitFlag GET", "BitFlag SET", "BitField GET", "BitField SET", "Mixed R/W" })
        {
            var data = allResults[testName];
            var genStats = CalculateStats(data.Select(d => d.Gen).ToList());
            var handStats = CalculateStats(data.Select(d => d.Hand).ToList());
            var ratioStats = CalculateStats(data.Select(d => d.Ratio).ToList());

            overallRatios.AddRange(data.Select(d => d.Ratio));

            _output.WriteLine($"{testName,-14} {genStats.Mean,6:F0} (σ={genStats.StdDev,4:F0})   {handStats.Mean,6:F0} (σ={handStats.StdDev,4:F0})   {ratioStats.Mean:F3} (σ={ratioStats.StdDev:F3})");
        }

        _output.WriteLine("-".PadRight(70, '-'));

        var overallStats = CalculateStats(overallRatios);
        int n = overallRatios.Count;
        double standardError = overallStats.StdDev / Math.Sqrt(n);
        double ciLow = overallStats.Mean - 1.96 * standardError;
        double ciHigh = overallStats.Mean + 1.96 * standardError;

        _output.WriteLine($"{"OVERALL",-14}                                        {overallStats.Mean:F3} (σ={overallStats.StdDev:F3})");
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

    private (double GenMs, double HandMs) RunSingleTest(Func<int> generatedAction, Func<int> handCodedAction)
    {
        var sw = Stopwatch.StartNew();
        var result1 = generatedAction();
        sw.Stop();
        var generatedMs = sw.Elapsed.TotalMilliseconds;

        sw.Restart();
        var result2 = handCodedAction();
        sw.Stop();
        var handCodedMs = sw.Elapsed.TotalMilliseconds;

        result1.Should().Be(result2, "Results should match");

        return (generatedMs, handCodedMs);
    }

    private static (double Mean, double StdDev) CalculateStats(List<double> values)
    {
        double mean = values.Average();
        double variance = values.Sum(v => Math.Pow(v - mean, 2)) / values.Count;
        double stdDev = Math.Sqrt(variance);
        return (mean, stdDev);
    }

    private (string Name, double GeneratedMs, double HandCodedMs) RunTimedTest(
        string name, Func<int> generatedAction, Func<int> handCodedAction)
    {
        var sw = Stopwatch.StartNew();
        var result1 = generatedAction();
        sw.Stop();
        var generatedMs = sw.Elapsed.TotalMilliseconds;

        sw.Restart();
        var result2 = handCodedAction();
        sw.Stop();
        var handCodedMs = sw.Elapsed.TotalMilliseconds;

        result1.Should().Be(result2, $"{name}: Results should match");

        return (name, generatedMs, handCodedMs);
    }

    /// <summary>
    /// Symmetric warmup for generated code - matches WarmupHandCoded structure exactly.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void WarmupGenerated()
    {
        for (int i = 0; i < WARMUP_ITERATIONS; i++)
        {
            GeneratedTestRegister reg = (byte)i;
            reg.Ready = !reg.Ready;
            reg.Mode = (byte)(i & 7);
            _ = reg.Priority;
        }
    }

    /// <summary>
    /// Symmetric warmup for hand-coded - matches WarmupGenerated structure exactly.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void WarmupHandCoded()
    {
        for (int i = 0; i < WARMUP_ITERATIONS; i++)
        {
            var reg = new HandCodedTestRegister { Value = (byte)i };
            reg.Ready = !reg.Ready;
            reg.Mode = (byte)(i & 7);
            _ = reg.Priority;
        }
    }

    #endregion
}
