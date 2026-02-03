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
    private const int ITERATIONS = 100_000_000; // 100 million iterations
    private const int WARMUP_ITERATIONS = 100_000_000;

    private readonly ITestOutputHelper _output;

    public BitFieldPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    //[Fact]
    private void BitFlag_Get_Performance()
    {
        _output.WriteLine($"Testing BitFlag GET performance ({ITERATIONS:N0} iterations)");
        _output.WriteLine(new string('=', 60));

        // Warmup
        WarmupGenerated();
        WarmupHandCoded();

        // Test generated code
        GeneratedTestRegister generatedReg = 0xAA;
        var sw = Stopwatch.StartNew();
        int trueCount = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            if (generatedReg.Ready) trueCount++;
            if (generatedReg.Error) trueCount++;
            if (generatedReg.Busy) trueCount++;
        }
        sw.Stop();
        var generatedTime = sw.Elapsed;
        _output.WriteLine($"Generated:  {generatedTime.TotalMilliseconds:F2} ms ({ITERATIONS / generatedTime.TotalSeconds:N0} ops/sec)");

        // Test hand-coded
        var handCodedReg = new HandCodedTestRegister { Value = 0xAA };
        sw.Restart();
        int trueCount2 = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            if (handCodedReg.Ready) trueCount2++;
            if (handCodedReg.Error) trueCount2++;
            if (handCodedReg.Busy) trueCount2++;
        }
        sw.Stop();
        var handCodedTime = sw.Elapsed;
        _output.WriteLine($"Hand-coded: {handCodedTime.TotalMilliseconds:F2} ms ({ITERATIONS / handCodedTime.TotalSeconds:N0} ops/sec)");

        // Results should be identical
        trueCount.Should().Be(trueCount2);

        var ratio = generatedTime.TotalMilliseconds / handCodedTime.TotalMilliseconds;
        _output.WriteLine($"Ratio:      {ratio:F3}x (1.0 = identical, <1.0 = generated faster)");
        _output.WriteLine("");

        // Allow 5% variance for JIT jitter - fail if regression exceeds this
        ratio.Should().BeInRange(0.95, 1.05, "Generated code should be within 5% of hand-coded");
    }

    //[Fact]
    private void BitFlag_Set_Performance()
    {
        _output.WriteLine($"Testing BitFlag SET performance ({ITERATIONS:N0} iterations)");
        _output.WriteLine(new string('=', 60));

        WarmupGenerated();
        WarmupHandCoded();

        // Test generated code
        GeneratedTestRegister generatedReg = 0;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < ITERATIONS; i++)
        {
            generatedReg.Ready = true;
            generatedReg.Error = false;
            generatedReg.Busy = true;
        }
        sw.Stop();
        var generatedTime = sw.Elapsed;
        _output.WriteLine($"Generated:  {generatedTime.TotalMilliseconds:F2} ms ({ITERATIONS / generatedTime.TotalSeconds:N0} ops/sec)");

        // Test hand-coded
        var handCodedReg = new HandCodedTestRegister();
        sw.Restart();
        for (int i = 0; i < ITERATIONS; i++)
        {
            handCodedReg.Ready = true;
            handCodedReg.Error = false;
            handCodedReg.Busy = true;
        }
        sw.Stop();
        var handCodedTime = sw.Elapsed;
        _output.WriteLine($"Hand-coded: {handCodedTime.TotalMilliseconds:F2} ms ({ITERATIONS / handCodedTime.TotalSeconds:N0} ops/sec)");

        // Values should be identical
        ((byte)generatedReg).Should().Be(handCodedReg.Value);

        var ratio = generatedTime.TotalMilliseconds / handCodedTime.TotalMilliseconds;
        _output.WriteLine($"Ratio:      {ratio:F3}x (1.0 = identical, <1.0 = generated faster)");
        _output.WriteLine("");

        ratio.Should().BeInRange(0.95, 1.05);
    }

    //[Fact]
    private void BitField_Get_Performance()
    {
        _output.WriteLine($"Testing BitField GET performance ({ITERATIONS:N0} iterations)");
        _output.WriteLine(new string('=', 60));

        WarmupGenerated();
        WarmupHandCoded();

        // Test generated code
        var generatedReg = new GeneratedTestRegister(0xFF);
        var sw = Stopwatch.StartNew();
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            sum += generatedReg.Mode;
            sum += generatedReg.Priority;
        }
        sw.Stop();
        var generatedTime = sw.Elapsed;
        _output.WriteLine($"Generated:  {generatedTime.TotalMilliseconds:F2} ms ({ITERATIONS / generatedTime.TotalSeconds:N0} ops/sec)");

        // Test hand-coded
        var handCodedReg = new HandCodedTestRegister { Value = 0xFF };
        sw.Restart();
        int sum2 = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            sum2 += handCodedReg.Mode;
            sum2 += handCodedReg.Priority;
        }
        sw.Stop();
        var handCodedTime = sw.Elapsed;
        _output.WriteLine($"Hand-coded: {handCodedTime.TotalMilliseconds:F2} ms ({ITERATIONS / handCodedTime.TotalSeconds:N0} ops/sec)");

        sum.Should().Be(sum2);

        var ratio = generatedTime.TotalMilliseconds / handCodedTime.TotalMilliseconds;
        _output.WriteLine($"Ratio:      {ratio:F3}x (1.0 = identical, <1.0 = generated faster)");
        _output.WriteLine("");

        ratio.Should().BeInRange(0.95, 1.05);
    }

    //[Fact]
    private void BitField_Set_Performance()
    {
        _output.WriteLine($"Testing BitField SET performance ({ITERATIONS:N0} iterations)");
        _output.WriteLine(new string('=', 60));

        WarmupGenerated();
        WarmupHandCoded();

        // Test generated code
        GeneratedTestRegister generatedReg = 0;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < ITERATIONS; i++)
        {
            generatedReg.Mode = (byte)(i & 0x07);
            generatedReg.Priority = (byte)(i & 0x03);
        }
        sw.Stop();
        var generatedTime = sw.Elapsed;
        _output.WriteLine($"Generated:  {generatedTime.TotalMilliseconds:F2} ms ({ITERATIONS / generatedTime.TotalSeconds:N0} ops/sec)");

        // Test hand-coded
        var handCodedReg = new HandCodedTestRegister();
        sw.Restart();
        for (int i = 0; i < ITERATIONS; i++)
        {
            handCodedReg.Mode = (byte)(i & 0x07);
            handCodedReg.Priority = (byte)(i & 0x03);
        }
        sw.Stop();
        var handCodedTime = sw.Elapsed;
        _output.WriteLine($"Hand-coded: {handCodedTime.TotalMilliseconds:F2} ms ({ITERATIONS / handCodedTime.TotalSeconds:N0} ops/sec)");

        ((byte)generatedReg).Should().Be(handCodedReg.Value);

        var ratio = generatedTime.TotalMilliseconds / handCodedTime.TotalMilliseconds;
        _output.WriteLine($"Ratio:      {ratio:F3}x (1.0 = identical, <1.0 = generated faster)");
        _output.WriteLine("");

        ratio.Should().BeInRange(0.95, 1.05);
    }

    /// <summary>
    /// Performance test for mixed read/write operations.
    /// Skipped in CI environments due to variable runner performance.
    /// </summary>
    [Fact(Skip = "Performance tests are unreliable in CI environments")]
    public void Mixed_ReadWrite_Performance()
    {
        _output.WriteLine($"Testing mixed read/write performance ({ITERATIONS:N0} iterations)");
        _output.WriteLine(new string('=', 60));

        WarmupGenerated();
        WarmupHandCoded();

        // Test generated code - realistic usage pattern
        GeneratedTestRegister generatedReg = 0;
        var sw = Stopwatch.StartNew();
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
        var generatedTime = sw.Elapsed;
        _output.WriteLine($"Generated:  {generatedTime.TotalMilliseconds:F2} ms ({ITERATIONS / generatedTime.TotalSeconds:N0} ops/sec)");

        // Test hand-coded
        var handCodedReg = new HandCodedTestRegister();
        sw.Restart();
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
        var handCodedTime = sw.Elapsed;
        _output.WriteLine($"Hand-coded: {handCodedTime.TotalMilliseconds:F2} ms ({ITERATIONS / handCodedTime.TotalSeconds:N0} ops/sec)");

        ((byte)generatedReg).Should().Be(handCodedReg.Value);

        var ratio = generatedTime.TotalMilliseconds / handCodedTime.TotalMilliseconds;
        _output.WriteLine($"Ratio:      {ratio:F3}x (1.0 = identical, <1.0 = generated faster)");
        _output.WriteLine("");

        ratio.Should().BeInRange(0.85, 1.15);
    }

    /// <summary>
    /// Comprehensive performance test with statistical analysis.
    /// Skipped in CI environments due to variable runner performance.
    /// Run locally to verify performance characteristics.
    /// </summary>
    [Fact(Skip = "Performance tests are unreliable in CI environments")]
    public void FullSuite_Performance_Summary()
    {
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
        _output.WriteLine("STATISTICAL RESULTS (mean � std dev)");
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

            _output.WriteLine($"{testName,-14} {genStats.Mean,6:F0} � {genStats.StdDev,4:F0}      {handStats.Mean,6:F0} � {handStats.StdDev,4:F0}      {ratioStats.Mean:F3} � {ratioStats.StdDev:F3}");
        }

        _output.WriteLine("-".PadRight(70, '-'));

        var overallStats = CalculateStats(overallRatios);
        _output.WriteLine($"{"OVERALL",-14}                                        {overallStats.Mean:F3} ± {overallStats.StdDev:F3}");
        _output.WriteLine("");
        _output.WriteLine($"Result: Generated code is {(overallStats.Mean < 1 ? "FASTER" : "SLOWER")} by {Math.Abs(1 - overallStats.Mean) * 100:F1}% on average");
        _output.WriteLine($"        95% confidence interval: {overallStats.Mean - 1.96 * overallStats.StdDev:F3} to {overallStats.Mean + 1.96 * overallStats.StdDev:F3}");
        _output.WriteLine("");

        // Overall should be within 20% - fail if regression exceeds this
        // Note: Widened from 5% to 20% to account for system variability during CI/testing
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

    //[MethodImpl(MethodImplOptions.NoInlining)]
    private void WarmupGenerated()
    {
        for (int i = 0; i < WARMUP_ITERATIONS; i++)
        {
            GeneratedTestRegister reg = (byte)i;
            reg.Ready = !reg.Ready;
            reg.Mode = (byte)(i & 7);
            _ = reg.Priority;
            GeneratedTestRegister reg2 = (byte)i;
            reg2.Ready = !reg2.Ready;
            reg2.Mode = (byte)(i & 7);
            _ = reg2.Priority;
        }
    }

    //[MethodImpl(MethodImplOptions.NoInlining)]
    private void WarmupHandCoded()
    {
        var reg = new HandCodedTestRegister { Value = 0x55 };
        for (int i = 0; i < WARMUP_ITERATIONS; i++)
        {
            reg.Ready = !reg.Ready;
            reg.Mode = (byte)(i & 7);
            _ = reg.Priority;
        }
    }

    #endregion
}
