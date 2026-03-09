using System.Diagnostics;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Tests for the BitFields generator comparing generated code vs raw inline bit manipulation.
/// </summary>
public class BitFieldComparisonTests
{
    private const int ITERATIONS = 100_000_000;
    private const int WARMUP_ITERATIONS = 1_000_000;

    // Hand-coded bit manipulation constants (identical layout to GeneratedTestRegister).
    // BitFlag: Ready = bit 0
    private const byte READY_MASK = 0x01;
    private const byte READY_INVERTED = 0xFE;
    // BitFlag: Error = bit 1
    private const byte ERROR_MASK = 0x02;
    // BitFlag: Busy = bit 7
    private const byte BUSY_MASK = 0x80;
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

    public BitFieldComparisonTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Functional Tests

    [Fact]
    public void Generated_Flags_GetAndSet()
    {
        GeneratedTestRegister reg = 0;

        reg.Ready.Should().BeFalse();
        reg.Error.Should().BeFalse();
        reg.Busy.Should().BeFalse();

        reg.Ready = true;
        reg.Ready.Should().BeTrue();
        ((byte)reg).Should().Be(0x01);

        reg.Error = true;
        reg.Error.Should().BeTrue();
        ((byte)reg).Should().Be(0x03);

        reg.Busy = true;
        reg.Busy.Should().BeTrue();
        ((byte)reg).Should().Be(0x83);

        reg.Ready = false;
        reg.Ready.Should().BeFalse();
        ((byte)reg).Should().Be(0x82);
    }

    [Fact]
    public void Generated_Fields_GetAndSet()
    {
        GeneratedTestRegister reg = 0;

        reg.Mode.Should().Be(0);
        reg.Priority.Should().Be(0);

        reg.Mode = 5;
        reg.Mode.Should().Be(5);
        ((byte)reg).Should().Be(0x14);

        reg.Priority = 3;
        reg.Priority.Should().Be(3);
        ((byte)reg).Should().Be(0x74);
    }

    [Fact]
    public void Generated_ImplicitConversion()
    {
        GeneratedTestRegister reg = 0xAA;
        ((byte)reg).Should().Be(0xAA);

        byte value = reg;
        value.Should().Be(0xAA);
    }

    [Fact]
    public void Generated_Constructor()
    {
        var reg = new GeneratedTestRegister(0xFF);
        
        reg.Ready.Should().BeTrue();
        reg.Error.Should().BeTrue();
        reg.Busy.Should().BeTrue();
        reg.Mode.Should().Be(7);
        reg.Priority.Should().Be(3);
    }

    #endregion

    #region Comparison with Raw Bit Manipulation

    [Fact]
    public void Generated_MatchesRawBitManipulation_BitPatterns()
    {
        // Test that generated code produces identical bit patterns to raw bit manipulation
        GeneratedTestRegister genReg = 0;
        byte handVal = 0;

        // Set same values using generated properties and raw bit ops
        genReg.Ready = true;
        genReg.Error = false;
        genReg.Busy = true;
        genReg.Mode = 5;
        genReg.Priority = 2;

        handVal = (byte)(handVal | READY_MASK);
        handVal = (byte)(handVal & 0xFD);
        handVal = (byte)(handVal | BUSY_MASK);
        handVal = (byte)((handVal & MODE_INVERTED) | ((5 << MODE_SHIFT) & MODE_SHIFTED_MASK));
        handVal = (byte)((handVal & PRIORITY_INVERTED) | ((2 << PRIORITY_SHIFT) & PRIORITY_SHIFTED_MASK));

        // Both should produce same byte value
        byte genValue = genReg;
        genValue.Should().Be(handVal);

        // Read back and verify individual fields match raw extraction
        genReg.Ready.Should().Be((handVal & READY_MASK) != 0);
        genReg.Error.Should().Be((handVal & ERROR_MASK) != 0);
        genReg.Busy.Should().Be((handVal & BUSY_MASK) != 0);
        genReg.Mode.Should().Be((byte)((handVal >> MODE_SHIFT) & MODE_MASK));
        genReg.Priority.Should().Be((byte)((handVal >> PRIORITY_SHIFT) & PRIORITY_MASK));
    }

    #endregion

    #region Performance vs Raw Bit Manipulation

    [Fact]
    public void Performance_Generated_vs_RawBitManipulation_Get()
    {
        _output.WriteLine("Comparing GET performance: Generated vs Raw bit manipulation");
        _output.WriteLine(new string('=', 70));
        _output.WriteLine($"Iterations: {ITERATIONS:N0}");
        _output.WriteLine("");

        // Warmup
        WarmupGenerated();
        WarmupRawBitOps();

        // Test Generated
        GeneratedTestRegister genReg = 0xFF;
        var sw = Stopwatch.StartNew();
        int sum1 = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            sum1 += genReg.Mode;
            sum1 += genReg.Priority;
        }
        sw.Stop();
        var genTime = sw.Elapsed;
        _output.WriteLine($"Generated:    {genTime.TotalMilliseconds,8:F2} ms ({ITERATIONS / genTime.TotalSeconds:N0} ops/sec)");

        // Test raw bit manipulation
        byte handVal = 0xFF;
        sw.Restart();
        int sum2 = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            sum2 += (byte)((handVal >> MODE_SHIFT) & MODE_MASK);
            sum2 += (byte)((handVal >> PRIORITY_SHIFT) & PRIORITY_MASK);
        }
        sw.Stop();
        var handTime = sw.Elapsed;
        _output.WriteLine($"Raw bit ops:  {handTime.TotalMilliseconds,8:F2} ms ({ITERATIONS / handTime.TotalSeconds:N0} ops/sec)");

        sum1.Should().Be(sum2, "Both should produce same result");

        var ratio = genTime.TotalMilliseconds / handTime.TotalMilliseconds;
        _output.WriteLine("");
        _output.WriteLine($"Ratio (Gen/Raw): {ratio:F3}x");
    }

    [Fact]
    public void Performance_Generated_vs_RawBitManipulation_Set()
    {
        _output.WriteLine("Comparing SET performance: Generated vs Raw bit manipulation");
        _output.WriteLine(new string('=', 70));
        _output.WriteLine($"Iterations: {ITERATIONS:N0}");
        _output.WriteLine("");

        // Warmup
        WarmupGenerated();
        WarmupRawBitOps();

        // Test Generated
        GeneratedTestRegister genReg = 0;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < ITERATIONS; i++)
        {
            genReg.Mode = (byte)(i & 0x07);
            genReg.Priority = (byte)(i & 0x03);
        }
        sw.Stop();
        var genTime = sw.Elapsed;
        _output.WriteLine($"Generated:    {genTime.TotalMilliseconds,8:F2} ms ({ITERATIONS / genTime.TotalSeconds:N0} ops/sec)");

        // Test raw bit manipulation
        byte handVal = 0;
        sw.Restart();
        for (int i = 0; i < ITERATIONS; i++)
        {
            handVal = (byte)((handVal & MODE_INVERTED) | (((byte)(i & 0x07) << MODE_SHIFT) & MODE_SHIFTED_MASK));
            handVal = (byte)((handVal & PRIORITY_INVERTED) | (((byte)(i & 0x03) << PRIORITY_SHIFT) & PRIORITY_SHIFTED_MASK));
        }
        sw.Stop();
        var handTime = sw.Elapsed;
        _output.WriteLine($"Raw bit ops:  {handTime.TotalMilliseconds,8:F2} ms ({ITERATIONS / handTime.TotalSeconds:N0} ops/sec)");

        // Both should end up with same final value
        byte genFinal = genReg;
        genFinal.Should().Be(handVal, "Both should produce same final value");

        var ratio = genTime.TotalMilliseconds / handTime.TotalMilliseconds;
        _output.WriteLine("");
        _output.WriteLine($"Ratio (Gen/Raw): {ratio:F3}x");
    }

    #endregion

    #region Helpers

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void WarmupGenerated()
    {
        GeneratedTestRegister reg = 0x55;
        for (int i = 0; i < WARMUP_ITERATIONS; i++)
        {
            reg.Ready = !reg.Ready;
            reg.Mode = (byte)(i & 7);
            _ = reg.Priority;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void WarmupRawBitOps()
    {
        byte val = 0x55;
        for (int i = 0; i < WARMUP_ITERATIONS; i++)
        {
            bool ready = (val & READY_MASK) != 0;
            val = !ready ? (byte)(val | READY_MASK) : (byte)(val & READY_INVERTED);
            val = (byte)((val & MODE_INVERTED) | (((byte)(i & 0x07) << MODE_SHIFT) & MODE_SHIFTED_MASK));
            _ = (byte)((val >> PRIORITY_SHIFT) & PRIORITY_MASK);
        }
    }

    #endregion
}
