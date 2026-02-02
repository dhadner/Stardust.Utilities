using System.Diagnostics;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Tests for the BitFields generator comparing generated code vs hand-coded implementations.
/// </summary>
public class BitFieldComparisonTests
{
    private const int ITERATIONS = 100_000_000;
    private const int WARMUP_ITERATIONS = 1_000_000;

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

    #region Comparison with Hand-Coded

    [Fact]
    public void Generated_MatchesHandCoded_BitPatterns()
    {
        // Test that generated code produces identical bit patterns to hand-coded
        GeneratedTestRegister genReg = 0;
        var handReg = new HandCodedTestRegister { Value = 0 };

        // Set same values
        genReg.Ready = true;
        genReg.Error = false;
        genReg.Busy = true;
        genReg.Mode = 5;
        genReg.Priority = 2;

        handReg.Ready = true;
        handReg.Error = false;
        handReg.Busy = true;
        handReg.Mode = 5;
        handReg.Priority = 2;

        // Both should produce same byte value
        byte genValue = genReg;
        byte handValue = handReg.Value;
        genValue.Should().Be(handValue);

        // Read back and verify
        genReg.Ready.Should().Be(handReg.Ready);
        genReg.Error.Should().Be(handReg.Error);
        genReg.Busy.Should().Be(handReg.Busy);
        genReg.Mode.Should().Be(handReg.Mode);
        genReg.Priority.Should().Be(handReg.Priority);
    }

    #endregion

    #region Performance vs Hand-Coded

    [Fact]
    public void Performance_Generated_vs_HandCoded_Get()
    {
        _output.WriteLine("Comparing GET performance: Generated vs Hand-coded");
        _output.WriteLine(new string('=', 70));
        _output.WriteLine($"Iterations: {ITERATIONS:N0}");
        _output.WriteLine("");

        // Warmup
        WarmupGenerated();
        WarmupHandCoded();

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

        // Test Hand-coded
        var handReg = new HandCodedTestRegister { Value = 0xFF };
        sw.Restart();
        int sum2 = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            sum2 += handReg.Mode;
            sum2 += handReg.Priority;
        }
        sw.Stop();
        var handTime = sw.Elapsed;
        _output.WriteLine($"Hand-coded:   {handTime.TotalMilliseconds,8:F2} ms ({ITERATIONS / handTime.TotalSeconds:N0} ops/sec)");

        sum1.Should().Be(sum2, "Both should produce same result");

        var ratio = genTime.TotalMilliseconds / handTime.TotalMilliseconds;
        _output.WriteLine("");
        _output.WriteLine($"Ratio (Gen/Hand): {ratio:F3}x");
    }

    [Fact]
    public void Performance_Generated_vs_HandCoded_Set()
    {
        _output.WriteLine("Comparing SET performance: Generated vs Hand-coded");
        _output.WriteLine(new string('=', 70));
        _output.WriteLine($"Iterations: {ITERATIONS:N0}");
        _output.WriteLine("");

        // Warmup
        WarmupGenerated();
        WarmupHandCoded();

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

        // Test Hand-coded
        var handReg = new HandCodedTestRegister();
        sw.Restart();
        for (int i = 0; i < ITERATIONS; i++)
        {
            handReg.Mode = (byte)(i & 0x07);
            handReg.Priority = (byte)(i & 0x03);
        }
        sw.Stop();
        var handTime = sw.Elapsed;
        _output.WriteLine($"Hand-coded:   {handTime.TotalMilliseconds,8:F2} ms ({ITERATIONS / handTime.TotalSeconds:N0} ops/sec)");

        // Both should end up with same final value
        byte genFinal = genReg;
        byte handFinal = handReg.Value;
        genFinal.Should().Be(handFinal, "Both should produce same final value");

        var ratio = genTime.TotalMilliseconds / handTime.TotalMilliseconds;
        _output.WriteLine("");
        _output.WriteLine($"Ratio (Gen/Hand): {ratio:F3}x");
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
