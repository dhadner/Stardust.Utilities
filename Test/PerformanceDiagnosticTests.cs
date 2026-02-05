using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;
using Stardust.Utilities;
using FluentAssertions;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Diagnostic test to isolate the performance difference between generated and hand-coded structs.
/// </summary>
public partial class PerformanceDiagnosticTests
{
    private readonly ITestOutputHelper _output;

    public PerformanceDiagnosticTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// The $64,000 question: What is the overhead of property accessors vs raw bit manipulation?
    /// This test compares:
    /// 1. Generated struct (partial properties with AggressiveInlining)
    /// 2. Hand-coded struct (regular properties with AggressiveInlining)
    /// 3. Raw inline bit manipulation (no properties, just masks and shifts)
    /// </summary>
    [Fact]
    public void PropertyVsRawBitManipulation_Overhead()
    {
        Assert.SkipWhen(CiEnvironmentDetector.IsRunningInCi, "Performance tests skipped in CI");

        const int ITERATIONS = 500_000_000;

        _output.WriteLine("=".PadRight(70, '='));
        _output.WriteLine("PROPERTY vs RAW BIT MANIPULATION OVERHEAD");
        _output.WriteLine($"Iterations: {ITERATIONS:N0}");
        _output.WriteLine("=".PadRight(70, '='));
        _output.WriteLine("");

        // Warmup all approaches
        WarmupAll(ITERATIONS / 10);

        var results = new List<(string Name, double Ms)>();
        Stopwatch sw = new();

        // ===== TEST 1: Raw bit manipulation (baseline - the fastest possible) =====
        // This is what you'd write if you didn't use properties at all
        {
            byte value = 0xAA;
            const byte READY_MASK = 0x01;
            const byte ERROR_MASK = 0x02;
            const byte BUSY_MASK = 0x80;

            int count = 0;
            sw.Restart();
            for (int i = 0; i < ITERATIONS; i++)
            {
                if ((value & READY_MASK) != 0) count++;
                if ((value & ERROR_MASK) != 0) count++;
                if ((value & BUSY_MASK) != 0) count++;
            }
            sw.Stop();
            results.Add(("Raw bit ops (baseline)", sw.Elapsed.TotalMilliseconds));
            _output.WriteLine($"Raw bit ops (baseline):     {sw.Elapsed.TotalMilliseconds,8:F2} ms  count={count}");
        }

        // ===== TEST 2: Hand-coded struct with properties =====
        {
            var reg = new HandCodedTestRegister { Value = 0xAA };
            int count = 0;
            sw.Restart();
            for (int i = 0; i < ITERATIONS; i++)
            {
                if (reg.Ready) count++;
                if (reg.Error) count++;
                if (reg.Busy) count++;
            }
            sw.Stop();
            results.Add(("Hand-coded properties", sw.Elapsed.TotalMilliseconds));
            _output.WriteLine($"Hand-coded properties:      {sw.Elapsed.TotalMilliseconds,8:F2} ms  count={count}");
        }

        // ===== TEST 3: Generated struct with partial properties =====
        {
            GeneratedTestRegister reg = 0xAA;
            int count = 0;
            sw.Restart();
            for (int i = 0; i < ITERATIONS; i++)
            {
                if (reg.Ready) count++;
                if (reg.Error) count++;
                if (reg.Busy) count++;
            }
            sw.Stop();
            results.Add(("Generated properties", sw.Elapsed.TotalMilliseconds));
            _output.WriteLine($"Generated properties:       {sw.Elapsed.TotalMilliseconds,8:F2} ms  count={count}");
        }

        // ===== TEST 4: Raw bit manipulation for multi-bit fields (shift + mask) =====
        {
            byte value = 0xFF;
            const int MODE_SHIFT = 2;
            const byte MODE_MASK = 0x07;
            const int PRIORITY_SHIFT = 5;
            const byte PRIORITY_MASK = 0x03;

            int sum = 0;
            sw.Restart();
            for (int i = 0; i < ITERATIONS; i++)
            {
                sum += (value >> MODE_SHIFT) & MODE_MASK;
                sum += (value >> PRIORITY_SHIFT) & PRIORITY_MASK;
            }
            sw.Stop();
            results.Add(("Raw shift+mask (baseline)", sw.Elapsed.TotalMilliseconds));
            _output.WriteLine($"Raw shift+mask (baseline):  {sw.Elapsed.TotalMilliseconds,8:F2} ms  sum={sum}");
        }

        // ===== TEST 5: Hand-coded struct for multi-bit fields =====
        {
            var reg = new HandCodedTestRegister { Value = 0xFF };
            int sum = 0;
            sw.Restart();
            for (int i = 0; i < ITERATIONS; i++)
            {
                sum += reg.Mode;
                sum += reg.Priority;
            }
            sw.Stop();
            results.Add(("Hand-coded field props", sw.Elapsed.TotalMilliseconds));
            _output.WriteLine($"Hand-coded field props:     {sw.Elapsed.TotalMilliseconds,8:F2} ms  sum={sum}");
        }

        // ===== TEST 6: Generated struct for multi-bit fields =====
        {
            var reg = new GeneratedTestRegister(0xFF);
            int sum = 0;
            sw.Restart();
            for (int i = 0; i < ITERATIONS; i++)
            {
                sum += reg.Mode;
                sum += reg.Priority;
            }
            sw.Stop();
            results.Add(("Generated field props", sw.Elapsed.TotalMilliseconds));
            _output.WriteLine($"Generated field props:      {sw.Elapsed.TotalMilliseconds,8:F2} ms  sum={sum}");
        }

        // ===== ANALYSIS =====
        _output.WriteLine("");
        _output.WriteLine("=".PadRight(70, '='));
        _output.WriteLine("ANALYSIS: Overhead of property accessors vs raw bit manipulation");
        _output.WriteLine("=".PadRight(70, '='));
        _output.WriteLine("");

        // Boolean flag operations
        var rawBoolMs = results.First(r => r.Name == "Raw bit ops (baseline)").Ms;
        var handBoolMs = results.First(r => r.Name == "Hand-coded properties").Ms;
        var genBoolMs = results.First(r => r.Name == "Generated properties").Ms;

        _output.WriteLine("BOOLEAN FLAGS (single bit test):");
        _output.WriteLine($"  Raw bit ops:        {rawBoolMs,8:F2} ms  (baseline)");
        _output.WriteLine($"  Hand-coded props:   {handBoolMs,8:F2} ms  ({(handBoolMs / rawBoolMs - 1) * 100:+0.0;-0.0;0}% overhead)");
        _output.WriteLine($"  Generated props:    {genBoolMs,8:F2} ms  ({(genBoolMs / rawBoolMs - 1) * 100:+0.0;-0.0;0}% overhead)");
        _output.WriteLine("");

        // Multi-bit field operations
        var rawFieldMs = results.First(r => r.Name == "Raw shift+mask (baseline)").Ms;
        var handFieldMs = results.First(r => r.Name == "Hand-coded field props").Ms;
        var genFieldMs = results.First(r => r.Name == "Generated field props").Ms;

        _output.WriteLine("MULTI-BIT FIELDS (shift + mask):");
        _output.WriteLine($"  Raw shift+mask:     {rawFieldMs,8:F2} ms  (baseline)");
        _output.WriteLine($"  Hand-coded props:   {handFieldMs,8:F2} ms  ({(handFieldMs / rawFieldMs - 1) * 100:+0.0;-0.0;0}% overhead)");
        _output.WriteLine($"  Generated props:    {genFieldMs,8:F2} ms  ({(genFieldMs / rawFieldMs - 1) * 100:+0.0;-0.0;0}% overhead)");
        _output.WriteLine("");

        // Summary
        _output.WriteLine("=".PadRight(70, '='));
        _output.WriteLine("CONCLUSION:");
        
        var avgRawMs = (rawBoolMs + rawFieldMs) / 2;
        var avgHandMs = (handBoolMs + handFieldMs) / 2;
        var avgGenMs = (genBoolMs + genFieldMs) / 2;

        var handOverhead = (avgHandMs / avgRawMs - 1) * 100;
        var genOverhead = (avgGenMs / avgRawMs - 1) * 100;

        if (Math.Abs(handOverhead) < 10 && Math.Abs(genOverhead) < 10)
        {
            _output.WriteLine("? AggressiveInlining eliminates property accessor overhead!");
            _output.WriteLine("  All approaches are statistically indistinguishable (within measurement noise).");
            _output.WriteLine("  There is ZERO performance penalty for using property accessors.");
        }
        else if (handOverhead > 10 || genOverhead > 10)
        {
            _output.WriteLine($"?? Property accessor overhead detected:");
            _output.WriteLine($"   Hand-coded: {handOverhead:+0.0;-0.0;0}% vs raw");
            _output.WriteLine($"   Generated:  {genOverhead:+0.0;-0.0;0}% vs raw");
        }
        else
        {
            _output.WriteLine("? All approaches are statistically indistinguishable.");
            _output.WriteLine($"   Hand-coded: {handOverhead:+0.0;-0.0;0}% vs raw (within noise)");
            _output.WriteLine($"   Generated:  {genOverhead:+0.0;-0.0;0}% vs raw (within noise)");
        }
        _output.WriteLine("=".PadRight(70, '='));
    }

    /// <summary>
    /// Tests SET operations: property setters vs raw bit manipulation.
    /// </summary>
    [Fact]
    public void PropertySetVsRawBitManipulation_Overhead()
    {
        Assert.SkipWhen(CiEnvironmentDetector.IsRunningInCi, "Performance tests skipped in CI");

        const int ITERATIONS = 500_000_000;

        _output.WriteLine("=".PadRight(70, '='));
        _output.WriteLine("PROPERTY SET vs RAW BIT MANIPULATION OVERHEAD");
        _output.WriteLine($"Iterations: {ITERATIONS:N0}");
        _output.WriteLine("=".PadRight(70, '='));
        _output.WriteLine("");

        WarmupAll(ITERATIONS / 10);

        var results = new List<(string Name, double Ms)>();
        Stopwatch sw = new();

        // ===== TEST 1: Raw bit SET operations (baseline) =====
        {
            byte value = 0;
            const byte READY_MASK = 0x01;
            const byte ERROR_INV = 0xFD;
            const byte BUSY_MASK = 0x80;

            sw.Restart();
            for (int i = 0; i < ITERATIONS; i++)
            {
                value = (byte)(value | READY_MASK);      // Set Ready = true
                value = (byte)(value & ERROR_INV);       // Set Error = false
                value = (byte)(value | BUSY_MASK);       // Set Busy = true
            }
            sw.Stop();
            results.Add(("Raw bit SET (baseline)", sw.Elapsed.TotalMilliseconds));
            _output.WriteLine($"Raw bit SET (baseline):     {sw.Elapsed.TotalMilliseconds,8:F2} ms  value=0x{value:X2}");
        }

        // ===== TEST 2: Hand-coded struct SET =====
        {
            var reg = new HandCodedTestRegister();
            sw.Restart();
            for (int i = 0; i < ITERATIONS; i++)
            {
                reg.Ready = true;
                reg.Error = false;
                reg.Busy = true;
            }
            sw.Stop();
            results.Add(("Hand-coded SET", sw.Elapsed.TotalMilliseconds));
            _output.WriteLine($"Hand-coded SET:             {sw.Elapsed.TotalMilliseconds,8:F2} ms  value=0x{reg.Value:X2}");
        }

        // ===== TEST 3: Generated struct SET =====
        {
            GeneratedTestRegister reg = 0;
            sw.Restart();
            for (int i = 0; i < ITERATIONS; i++)
            {
                reg.Ready = true;
                reg.Error = false;
                reg.Busy = true;
            }
            sw.Stop();
            results.Add(("Generated SET", sw.Elapsed.TotalMilliseconds));
            _output.WriteLine($"Generated SET:              {sw.Elapsed.TotalMilliseconds,8:F2} ms  value=0x{(byte)reg:X2}");
        }

        // ===== ANALYSIS =====
        _output.WriteLine("");
        var rawMs = results[0].Ms;
        var handMs = results[1].Ms;
        var genMs = results[2].Ms;

        _output.WriteLine("OVERHEAD ANALYSIS:");
        _output.WriteLine($"  Raw bit SET:      {rawMs,8:F2} ms  (baseline)");
        _output.WriteLine($"  Hand-coded SET:   {handMs,8:F2} ms  ({(handMs / rawMs - 1) * 100:+0.0;-0.0;0}% overhead)");
        _output.WriteLine($"  Generated SET:    {genMs,8:F2} ms  ({(genMs / rawMs - 1) * 100:+0.0;-0.0;0}% overhead)");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void WarmupAll(int iterations)
    {
        // Warmup raw bit ops
        byte value = 0;
        for (int i = 0; i < iterations; i++)
        {
            value = (byte)(value | 0x01);
            value = (byte)(value & 0xFE);
        }

        // Warmup hand-coded
        var hand = new HandCodedTestRegister();
        for (int i = 0; i < iterations; i++)
        {
            hand.Ready = !hand.Ready;
        }

        // Warmup generated
        GeneratedTestRegister gen = 0;
        for (int i = 0; i < iterations; i++)
        {
            gen.Ready = !gen.Ready;
        }
    }
}
