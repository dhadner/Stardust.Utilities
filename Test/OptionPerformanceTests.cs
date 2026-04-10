using System.Diagnostics;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Performance tests comparing Option&lt;T&gt; vs nullable T? / raw null checks.
/// Uses the same statistical methodology as BitFieldPerformanceTests:
/// alternating execution order, trimmed means, and 95% confidence intervals.
/// <para>
/// Tests are split into two tiers:
/// <list type="bullet">
///   <item>
///     <term>Zero-cost</term>
///     <description>
///       Operations that compile to the same inline ternary / branch as hand-coded
///       nullable code. No delegates are involved. These should be statistically
///       identical to <c>T?</c>.
///     </description>
///   </item>
///   <item>
///     <term>Delegate-based</term>
///     <description>
///       Operations that accept <c>Func&lt;&gt;</c> or <c>Action&lt;&gt;</c> parameters.
///       Even when the JIT inlines the delegate, the calling convention is different
///       from a raw ternary. These are expected to carry measurable overhead compared
///       to the hand-written <c>T?</c> equivalent.
///     </description>
///   </item>
/// </list>
/// </para>
/// </summary>
[Trait("Category", "Performance")]
public class OptionPerformanceTests
{
    private const int FULL_SUITE_RUNS = 200;
    private const int ITERATIONS = 100_000_000;
    private const int WARMUP_ITERATIONS = 5_000_000;

    private readonly ITestOutputHelper _output;

    public OptionPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ══════════════════════════════════════════════════════════════
    // Individual quick-sanity tests
    // ══════════════════════════════════════════════════════════════

    #region Individual zero-cost tests

    [Fact]
    public void Option_Create_Some_Performance()
    {
        Assert.SkipWhen(CiEnvironmentDetector.IsRunningInCi, "Performance tests are skipped in CI environments due to variable runner performance.");
        RunSingleBenchmark("Create Some", RunCreateSomeTest);
    }

    [Fact]
    public void Option_IsSome_Check_Performance()
    {
        Assert.SkipWhen(CiEnvironmentDetector.IsRunningInCi, "Performance tests are skipped in CI environments due to variable runner performance.");
        RunSingleBenchmark("IsSome check", RunIsSomeCheckTest);
    }

    [Fact]
    public void Option_UnwrapOr_Performance()
    {
        Assert.SkipWhen(CiEnvironmentDetector.IsRunningInCi, "Performance tests are skipped in CI environments due to variable runner performance.");
        RunSingleBenchmark("UnwrapOr (??)", RunUnwrapOrTest);
    }

    [Fact]
    public void Option_Or_Performance()
    {
        Assert.SkipWhen(CiEnvironmentDetector.IsRunningInCi, "Performance tests are skipped in CI environments due to variable runner performance.");
        RunSingleBenchmark("Or", RunOrTest);
    }

    [Fact]
    public void Option_Zip_Performance()
    {
        Assert.SkipWhen(CiEnvironmentDetector.IsRunningInCi, "Performance tests are skipped in CI environments due to variable runner performance.");
        RunSingleBenchmark("Zip", RunZipTest);
    }

    #endregion

    #region Individual delegate-based tests

    [Fact]
    public void Option_Map_Performance()
    {
        Assert.SkipWhen(CiEnvironmentDetector.IsRunningInCi, "Performance tests are skipped in CI environments due to variable runner performance.");
        RunSingleBenchmark("Map (delegate)", RunMapTest);
    }

    [Fact]
    public void Option_MapOrElse_Performance()
    {
        Assert.SkipWhen(CiEnvironmentDetector.IsRunningInCi, "Performance tests are skipped in CI environments due to variable runner performance.");
        RunSingleBenchmark("MapOrElse (delegate)", RunMapOrElseTest);
    }

    [Fact]
    public void Option_Filter_Performance()
    {
        Assert.SkipWhen(CiEnvironmentDetector.IsRunningInCi, "Performance tests are skipped in CI environments due to variable runner performance.");
        RunSingleBenchmark("Filter (delegate)", RunFilterTest);
    }

    [Fact]
    public void Option_AndThen_Performance()
    {
        Assert.SkipWhen(CiEnvironmentDetector.IsRunningInCi, "Performance tests are skipped in CI environments due to variable runner performance.");
        RunSingleBenchmark("AndThen (delegate)", RunAndThenTest);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // Full statistical suite
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Comprehensive performance test with statistical analysis.
    /// Each benchmark is isolated in its own [NoInlining] method for independent JIT
    /// compilation, then called in alternating order (even runs = nullable first,
    /// odd runs = Option first) to eliminate ordering bias.
    /// <para>
    /// Results are reported in two groups so architects can clearly see which
    /// Option methods are free and which carry delegate overhead.
    /// </para>
    /// </summary>
    [Fact]
    public void FullSuite_Performance_Summary()
    {
        Assert.SkipWhen(CiEnvironmentDetector.IsRunningInCi, "Performance tests are skipped in CI environments due to variable runner performance.");

        _output.WriteLine("=".PadRight(70, '='));
        _output.WriteLine("OPTION<T> PERFORMANCE SUMMARY WITH STATISTICS");
        _output.WriteLine($"Runs: {FULL_SUITE_RUNS}, Iterations per run: {ITERATIONS:N0}");
        _output.WriteLine("=".PadRight(70, '='));
        _output.WriteLine("");

        // Zero-cost tier: no delegates
        string[] zeroCostNames = ["Create Some", "IsSome check", "UnwrapOr (??)", "Or", "Zip"];
        // Delegate-based tier: Func<>/Action<> parameters
        string[] delegateNames = ["Map", "MapOrElse", "Filter", "AndThen"];

        var allResults = new Dictionary<string, List<(double Opt, double Baseline, double Ratio)>>();
        foreach (var name in zeroCostNames.Concat(delegateNames))
            allResults[name] = [];

        for (int run = 0; run < FULL_SUITE_RUNS; run++)
        {
            bool baselineFirst = run % 2 == 0;
            if (baselineFirst)
            {
                WarmupBaseline();
                WarmupOption();
            }
            else
            {
                WarmupOption();
                WarmupBaseline();
            }

            Collect("Create Some", RunCreateSomeTest, baselineFirst);
            Collect("IsSome check", RunIsSomeCheckTest, baselineFirst);
            Collect("UnwrapOr (??)", RunUnwrapOrTest, baselineFirst);
            Collect("Or", RunOrTest, baselineFirst);
            Collect("Zip", RunZipTest, baselineFirst);
            Collect("Map", RunMapTest, baselineFirst);
            Collect("MapOrElse", RunMapOrElseTest, baselineFirst);
            Collect("Filter", RunFilterTest, baselineFirst);
            Collect("AndThen", RunAndThenTest, baselineFirst);
        }

        int discard = (int)Math.Round((double)(FULL_SUITE_RUNS / 10));

        // ── Zero-cost tier ───────────────────────────────────────
        _output.WriteLine("══════════════════════════════════════════════════════════════════");
        _output.WriteLine("ZERO-COST TIER (no delegates -- expected identical to T?)");
        _output.WriteLine("══════════════════════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine($"{"Test",-14} {"Option<T> (ms)",18} {"T? (ms)",18} {"Ratio",18}");
        _output.WriteLine("-".PadRight(70, '-'));

        var zeroCostRatios = new List<double>();
        foreach (var testName in zeroCostNames)
        {
            PrintRow(testName, allResults[testName], discard);
            zeroCostRatios.AddRange(allResults[testName].Select(d => d.Ratio));
        }

        _output.WriteLine("-".PadRight(70, '-'));
        PrintOverall("ZERO-COST", zeroCostRatios, zeroCostNames.Length, discard);

        // ── Delegate-based tier ──────────────────────────────────
        _output.WriteLine("");
        _output.WriteLine("══════════════════════════════════════════════════════════════════");
        _output.WriteLine("DELEGATE TIER (Func<>/Action<> -- overhead expected vs inline T?)");
        _output.WriteLine("══════════════════════════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine($"{"Test",-14} {"Option<T> (ms)",18} {"T? (ms)",18} {"Ratio",18}");
        _output.WriteLine("-".PadRight(70, '-'));

        var delegateRatios = new List<double>();
        foreach (var testName in delegateNames)
        {
            PrintRow(testName, allResults[testName], discard);
            delegateRatios.AddRange(allResults[testName].Select(d => d.Ratio));
        }

        _output.WriteLine("-".PadRight(70, '-'));
        PrintOverall("DELEGATE", delegateRatios, delegateNames.Length, discard);

        return;

        void Collect(string name, BenchmarkRunner runner, bool bFirst)
        {
            runner(out var optTime, out var baseTime, bFirst);
            double optMs = optTime.TotalMilliseconds;
            double baseMs = baseTime.TotalMilliseconds;
            allResults[name].Add((optMs, baseMs, optMs / baseMs));
        }
    }

    #region Reporting Helpers

    private void PrintRow(string testName, List<(double Opt, double Baseline, double Ratio)> data, int discard)
    {
        var optStats = CalculateStats(data.Select(d => d.Opt).ToList(), discard);
        var baseStats = CalculateStats(data.Select(d => d.Baseline).ToList(), discard);
        var ratioStats = CalculateStats(data.Select(d => d.Ratio).ToList(), discard);

        _output.WriteLine(
            $"{testName,-14} {optStats.Mean,6:F0} (\u03c3={optStats.StdDev,4:F0})   " +
            $"{baseStats.Mean,6:F0} (\u03c3={baseStats.StdDev,4:F0})   " +
            $"{ratioStats.Mean:F3} (\u03c3={ratioStats.StdDev:F3})");
    }

    private void PrintOverall(string label, List<double> ratios, int testCount, int discard)
    {
        var overallStats = CalculateStats(ratios);
        int n = (ratios.Count / testCount) - discard * 2;
        double standardError = overallStats.StdDev / Math.Sqrt(n);
        double ciLow = overallStats.Mean - 1.96 * standardError;
        double ciHigh = overallStats.Mean + 1.96 * standardError;

        _output.WriteLine($"{label,-14}                                     {overallStats.Mean:F3} (\u03c3={overallStats.StdDev:F3})");
        _output.WriteLine("");
        if (ciLow <= 1.0 && ciHigh >= 1.0)
        {
            _output.WriteLine($"\u2713 {label} Option<T> performance statistically identical to T? (95% CI includes 1.0).");
        }
        else if (overallStats.Mean > 1.0)
        {
            _output.WriteLine($"\u26a0\ufe0f {label} Option<T> is {(overallStats.Mean - 1) * 100:F1}% SLOWER on average.");
        }
        else
        {
            _output.WriteLine($"\u2139\ufe0f {label} Option<T> is {(1 - overallStats.Mean) * 100:F1}% FASTER on average.");
        }
        _output.WriteLine($"        \u03c3 (std dev) = {overallStats.StdDev:F4}, SE = {standardError:F4}, n = {n}");
        _output.WriteLine($"        95% CI for mean = {ciLow:F3} to {ciHigh:F3}");
    }

    #endregion

    #region Test Runners

    private delegate void BenchmarkRunner(out TimeSpan optionTime, out TimeSpan baselineTime, bool baselineFirst);

    private void RunSingleBenchmark(string name, BenchmarkRunner runner)
    {
        _output.WriteLine($"Testing {name} ({ITERATIONS:N0} iterations)");
        _output.WriteLine(new string('=', 60));

        WarmupOption();
        WarmupBaseline();

        runner(out _, out _, true);
        runner(out var optionTime, out var baselineTime, false);

        _output.WriteLine($"Option<int>:  {optionTime.TotalMilliseconds:F2} ms ({ITERATIONS / optionTime.TotalSeconds:N0} ops/sec)");
        _output.WriteLine($"T? baseline:  {baselineTime.TotalMilliseconds:F2} ms ({ITERATIONS / baselineTime.TotalSeconds:N0} ops/sec)");

        var ratio = optionTime.TotalMilliseconds / baselineTime.TotalMilliseconds;
        _output.WriteLine($"Ratio:        {ratio:F3}x (1.0 = identical, <1.0 = Option faster)");
    }

    private static void RunCreateSomeTest(out TimeSpan optionTime, out TimeSpan baselineTime, bool baselineFirst)
    {
        var sw = new Stopwatch();
        int resultOpt = 0, resultBase = 0;
        optionTime = baselineTime = sw.Elapsed;

        if (baselineFirst)
        {
            sw.Restart(); resultBase = CreateSome_Nullable(); sw.Stop(); baselineTime = sw.Elapsed;
            sw.Restart(); resultOpt = CreateSome_Option(); sw.Stop(); optionTime = sw.Elapsed;
        }
        else
        {
            sw.Restart(); resultOpt = CreateSome_Option(); sw.Stop(); optionTime = sw.Elapsed;
            sw.Restart(); resultBase = CreateSome_Nullable(); sw.Stop(); baselineTime = sw.Elapsed;
        }
        resultOpt.Should().Be(resultBase);
    }

    private static void RunIsSomeCheckTest(out TimeSpan optionTime, out TimeSpan baselineTime, bool baselineFirst)
    {
        var sw = new Stopwatch();
        int resultOpt = 0, resultBase = 0;
        optionTime = baselineTime = sw.Elapsed;

        if (baselineFirst)
        {
            sw.Restart(); resultBase = IsSomeCheck_Nullable(); sw.Stop(); baselineTime = sw.Elapsed;
            sw.Restart(); resultOpt = IsSomeCheck_Option(); sw.Stop(); optionTime = sw.Elapsed;
        }
        else
        {
            sw.Restart(); resultOpt = IsSomeCheck_Option(); sw.Stop(); optionTime = sw.Elapsed;
            sw.Restart(); resultBase = IsSomeCheck_Nullable(); sw.Stop(); baselineTime = sw.Elapsed;
        }
        resultOpt.Should().Be(resultBase);
    }

    private static void RunUnwrapOrTest(out TimeSpan optionTime, out TimeSpan baselineTime, bool baselineFirst)
    {
        var sw = new Stopwatch();
        int resultOpt = 0, resultBase = 0;
        optionTime = baselineTime = sw.Elapsed;

        if (baselineFirst)
        {
            sw.Restart(); resultBase = UnwrapOr_Nullable(); sw.Stop(); baselineTime = sw.Elapsed;
            sw.Restart(); resultOpt = UnwrapOr_Option(); sw.Stop(); optionTime = sw.Elapsed;
        }
        else
        {
            sw.Restart(); resultOpt = UnwrapOr_Option(); sw.Stop(); optionTime = sw.Elapsed;
            sw.Restart(); resultBase = UnwrapOr_Nullable(); sw.Stop(); baselineTime = sw.Elapsed;
        }
        resultOpt.Should().Be(resultBase);
    }

    private static void RunOrTest(out TimeSpan optionTime, out TimeSpan baselineTime, bool baselineFirst)
    {
        var sw = new Stopwatch();
        int resultOpt = 0, resultBase = 0;
        optionTime = baselineTime = sw.Elapsed;

        if (baselineFirst)
        {
            sw.Restart(); resultBase = Or_Nullable(); sw.Stop(); baselineTime = sw.Elapsed;
            sw.Restart(); resultOpt = Or_Option(); sw.Stop(); optionTime = sw.Elapsed;
        }
        else
        {
            sw.Restart(); resultOpt = Or_Option(); sw.Stop(); optionTime = sw.Elapsed;
            sw.Restart(); resultBase = Or_Nullable(); sw.Stop(); baselineTime = sw.Elapsed;
        }
        resultOpt.Should().Be(resultBase);
    }

    private static void RunZipTest(out TimeSpan optionTime, out TimeSpan baselineTime, bool baselineFirst)
    {
        var sw = new Stopwatch();
        int resultOpt = 0, resultBase = 0;
        optionTime = baselineTime = sw.Elapsed;

        if (baselineFirst)
        {
            sw.Restart(); resultBase = Zip_Nullable(); sw.Stop(); baselineTime = sw.Elapsed;
            sw.Restart(); resultOpt = Zip_Option(); sw.Stop(); optionTime = sw.Elapsed;
        }
        else
        {
            sw.Restart(); resultOpt = Zip_Option(); sw.Stop(); optionTime = sw.Elapsed;
            sw.Restart(); resultBase = Zip_Nullable(); sw.Stop(); baselineTime = sw.Elapsed;
        }
        resultOpt.Should().Be(resultBase);
    }

    private static void RunMapTest(out TimeSpan optionTime, out TimeSpan baselineTime, bool baselineFirst)
    {
        var sw = new Stopwatch();
        int resultOpt = 0, resultBase = 0;
        optionTime = baselineTime = sw.Elapsed;

        if (baselineFirst)
        {
            sw.Restart(); resultBase = Map_Nullable(); sw.Stop(); baselineTime = sw.Elapsed;
            sw.Restart(); resultOpt = Map_Option(); sw.Stop(); optionTime = sw.Elapsed;
        }
        else
        {
            sw.Restart(); resultOpt = Map_Option(); sw.Stop(); optionTime = sw.Elapsed;
            sw.Restart(); resultBase = Map_Nullable(); sw.Stop(); baselineTime = sw.Elapsed;
        }
        resultOpt.Should().Be(resultBase);
    }

    private static void RunMapOrElseTest(out TimeSpan optionTime, out TimeSpan baselineTime, bool baselineFirst)
    {
        var sw = new Stopwatch();
        int resultOpt = 0, resultBase = 0;
        optionTime = baselineTime = sw.Elapsed;

        if (baselineFirst)
        {
            sw.Restart(); resultBase = MapOrElse_Nullable(); sw.Stop(); baselineTime = sw.Elapsed;
            sw.Restart(); resultOpt = MapOrElse_Option(); sw.Stop(); optionTime = sw.Elapsed;
        }
        else
        {
            sw.Restart(); resultOpt = MapOrElse_Option(); sw.Stop(); optionTime = sw.Elapsed;
            sw.Restart(); resultBase = MapOrElse_Nullable(); sw.Stop(); baselineTime = sw.Elapsed;
        }
        resultOpt.Should().Be(resultBase);
    }

    private static void RunFilterTest(out TimeSpan optionTime, out TimeSpan baselineTime, bool baselineFirst)
    {
        var sw = new Stopwatch();
        int resultOpt = 0, resultBase = 0;
        optionTime = baselineTime = sw.Elapsed;

        if (baselineFirst)
        {
            sw.Restart(); resultBase = Filter_Nullable(); sw.Stop(); baselineTime = sw.Elapsed;
            sw.Restart(); resultOpt = Filter_Option(); sw.Stop(); optionTime = sw.Elapsed;
        }
        else
        {
            sw.Restart(); resultOpt = Filter_Option(); sw.Stop(); optionTime = sw.Elapsed;
            sw.Restart(); resultBase = Filter_Nullable(); sw.Stop(); baselineTime = sw.Elapsed;
        }
        resultOpt.Should().Be(resultBase);
    }

    private static void RunAndThenTest(out TimeSpan optionTime, out TimeSpan baselineTime, bool baselineFirst)
    {
        var sw = new Stopwatch();
        int resultOpt = 0, resultBase = 0;
        optionTime = baselineTime = sw.Elapsed;

        if (baselineFirst)
        {
            sw.Restart(); resultBase = AndThen_Nullable(); sw.Stop(); baselineTime = sw.Elapsed;
            sw.Restart(); resultOpt = AndThen_Option(); sw.Stop(); optionTime = sw.Elapsed;
        }
        else
        {
            sw.Restart(); resultOpt = AndThen_Option(); sw.Stop(); optionTime = sw.Elapsed;
            sw.Restart(); resultBase = AndThen_Nullable(); sw.Stop(); baselineTime = sw.Elapsed;
        }
        resultOpt.Should().Be(resultBase);
    }

    #endregion

    #region Benchmark Kernels -- Zero-Cost Tier

    // ── Create Some / assign nullable ────────────────────────────

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CreateSome_Option()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            var opt = Option<int>.Some(i);
            sum += opt.Value;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CreateSome_Nullable()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            int? val = i;
            sum += val.Value;
        }
        return sum;
    }

    // ── IsSome / HasValue check ──────────────────────────────────

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int IsSomeCheck_Option()
    {
        int count = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            Option<int> opt = (i % 2 == 0) ? Option<int>.Some(i) : Option<int>.None;
            if (opt.IsSome) count++;
        }
        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int IsSomeCheck_Nullable()
    {
        int count = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            int? val = (i % 2 == 0) ? i : null;
            if (val.HasValue) count++;
        }
        return count;
    }

    // ── UnwrapOr / null-coalescing ?? ─────────────────────────────

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int UnwrapOr_Option()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            Option<int> opt = (i % 3 == 0) ? Option<int>.Some(i) : Option<int>.None;
            sum += opt.UnwrapOr(-1);
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int UnwrapOr_Nullable()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            int? val = (i % 3 == 0) ? i : null;
            sum += val ?? -1;
        }
        return sum;
    }

    // ── Or / nullable fallback ───────────────────────────────────

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Or_Option()
    {
        int sum = 0;
        var fallback = Option<int>.Some(-1);
        for (int i = 0; i < ITERATIONS; i++)
        {
            Option<int> opt = (i % 3 == 0) ? Option<int>.Some(i) : Option<int>.None;
            sum += opt.Or(fallback).Value;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Or_Nullable()
    {
        int sum = 0;
        int? fallback = -1;
        for (int i = 0; i < ITERATIONS; i++)
        {
            int? val = (i % 3 == 0) ? i : null;
            sum += (val ?? fallback).Value;
        }
        return sum;
    }

    // ── Zip / nullable pair ──────────────────────────────────────

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Zip_Option()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            Option<int> a = (i % 2 == 0) ? Option<int>.Some(i) : Option<int>.None;
            Option<int> b = (i % 3 == 0) ? Option<int>.Some(i + 1) : Option<int>.None;
            var zipped = a.Zip(b);
            sum += zipped.IsSome ? zipped.Value.First : 0;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Zip_Nullable()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            int? a = (i % 2 == 0) ? i : null;
            int? b = (i % 3 == 0) ? i + 1 : null;
            sum += (a.HasValue && b.HasValue) ? a.Value : 0;
        }
        return sum;
    }

    #endregion

    #region Benchmark Kernels -- Delegate Tier

    // ── Map (Func<T, TNew>) / ternary map ────────────────────────

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Map_Option()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            Option<int> opt = (i % 2 == 0) ? Option<int>.Some(i) : Option<int>.None;
            var mapped = opt.Map(v => v * 2);
            sum += mapped.UnwrapOr(0);
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Map_Nullable()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            int? val = (i % 2 == 0) ? i : null;
            int? mapped = val.HasValue ? val.Value * 2 : null;
            sum += mapped ?? 0;
        }
        return sum;
    }

    // ── MapOrElse (Func<T,R>, Func<R>) / ternary branch ─────────

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int MapOrElse_Option()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            Option<int> opt = (i % 2 == 0) ? Option<int>.Some(i) : Option<int>.None;
            sum += opt.MapOrElse(v => v, () => -1);
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int MapOrElse_Nullable()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            int? val = (i % 2 == 0) ? i : null;
            sum += val.HasValue ? val.Value : -1;
        }
        return sum;
    }

    // ── Filter (Func<T, bool>) / nullable with predicate ─────────

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Filter_Option()
    {
        int count = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            Option<int> opt = (i % 2 == 0) ? Option<int>.Some(i) : Option<int>.None;
            if (opt.Filter(v => v % 4 == 0).IsSome) count++;
        }
        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Filter_Nullable()
    {
        int count = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            int? val = (i % 2 == 0) ? i : null;
            if (val.HasValue && val.Value % 4 == 0) count++;
        }
        return count;
    }

    // ── AndThen (Func<T, Option<TNew>>) / nullable chain ─────────

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int AndThen_Option()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            Option<int> opt = (i % 2 == 0) ? Option<int>.Some(i) : Option<int>.None;
            var result = opt.AndThen(v => v > 0 ? Option<int>.Some(v * 2) : Option<int>.None);
            sum += result.UnwrapOr(0);
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int AndThen_Nullable()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            int? val = (i % 2 == 0) ? i : null;
            int? result = val.HasValue && val.Value > 0 ? val.Value * 2 : null;
            sum += result ?? 0;
        }
        return sum;
    }

    #endregion

    #region Warmup & Stats

    private static int WarmupOption()
    {
        int sum = 0;
        for (int i = 0; i < WARMUP_ITERATIONS; i++)
        {
            Option<int> opt = (i % 2 == 0) ? Option<int>.Some(i) : Option<int>.None;
            sum += opt.UnwrapOr(0);
        }
        return sum;
    }

    private static int WarmupBaseline()
    {
        int sum = 0;
        for (int i = 0; i < WARMUP_ITERATIONS; i++)
        {
            int? val = (i % 2 == 0) ? i : null;
            sum += val ?? 0;
        }
        return sum;
    }

    private static (double Mean, double StdDev) CalculateStats(List<double> values, int discard = 0)
    {
        values.Sort();
        var trimmed = values.Skip(discard).Take(values.Count - 2 * discard).ToList();

        double mean = trimmed.Average();
        double variance = trimmed.Sum(v => Math.Pow(v - mean, 2)) / trimmed.Count;
        double stdDev = Math.Sqrt(variance);
        return (mean, stdDev);
    }

    #endregion
}
