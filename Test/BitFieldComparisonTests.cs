using System.Diagnostics;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Stardust.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Tests comparing the two BitFields approaches:
/// 1. User-declared Value field: [BitFields] with user's public byte Value;
/// 2. Generator-created Value field: [BitFields(typeof(byte))] with private Value
/// 
/// Both functional correctness and performance are tested.
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

    #region Functional Tests - User-Declared Value Field

    [Fact]
    public void UserValue_Flags_GetAndSet()
    {
        var reg = new PerfTestUserValueRegister { Value = 0 };

        reg.Ready.Should().BeFalse();
        reg.Error.Should().BeFalse();
        reg.Busy.Should().BeFalse();

        reg.Ready = true;
        reg.Ready.Should().BeTrue();
        reg.Value.Should().Be(0x01);

        reg.Error = true;
        reg.Error.Should().BeTrue();
        reg.Value.Should().Be(0x03);

        reg.Busy = true;
        reg.Busy.Should().BeTrue();
        reg.Value.Should().Be(0x83);

        reg.Ready = false;
        reg.Ready.Should().BeFalse();
        reg.Value.Should().Be(0x82);
    }

    [Fact]
    public void UserValue_Fields_GetAndSet()
    {
        var reg = new PerfTestUserValueRegister { Value = 0 };

        reg.Mode.Should().Be(0);
        reg.Priority.Should().Be(0);

        reg.Mode = 5;
        reg.Mode.Should().Be(5);
        reg.Value.Should().Be(0x14); // 5 << 2 = 0x14

        reg.Priority = 3;
        reg.Priority.Should().Be(3);
        reg.Value.Should().Be(0x74); // 0x14 | (3 << 5) = 0x74
    }

    [Fact]
    public void UserValue_ImplicitConversion()
    {
        PerfTestUserValueRegister reg = 0xAA;
        reg.Value.Should().Be(0xAA);

        byte value = reg;
        value.Should().Be(0xAA);
    }

    #endregion

    #region Functional Tests - Generator-Created Value Field

    [Fact]
    public void GenValue_Flags_GetAndSet()
    {
        PerfTestGenValueRegister reg = 0;

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
    public void GenValue_Fields_GetAndSet()
    {
        PerfTestGenValueRegister reg = 0;

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
    public void GenValue_ImplicitConversion()
    {
        PerfTestGenValueRegister reg = 0xAA;
        ((byte)reg).Should().Be(0xAA);

        byte value = reg;
        value.Should().Be(0xAA);
    }

    #endregion

    #region Cross-Comparison Tests

    [Fact]
    public void BothApproaches_ProduceSameResults()
    {
        // Test that both approaches produce identical bit patterns
        var userReg = new PerfTestUserValueRegister { Value = 0 };
        PerfTestGenValueRegister genReg = 0;

        // Set same values
        userReg.Ready = true;
        userReg.Error = false;
        userReg.Busy = true;
        userReg.Mode = 5;
        userReg.Priority = 2;

        genReg.Ready = true;
        genReg.Error = false;
        genReg.Busy = true;
        genReg.Mode = 5;
        genReg.Priority = 2;

        // Both should produce same byte value
        byte userValue = userReg;
        byte genValue = genReg;
        userValue.Should().Be(genValue);

        // Read back and verify
        userReg.Ready.Should().Be(genReg.Ready);
        userReg.Error.Should().Be(genReg.Error);
        userReg.Busy.Should().Be(genReg.Busy);
        userReg.Mode.Should().Be(genReg.Mode);
        userReg.Priority.Should().Be(genReg.Priority);
    }

    #endregion

    #region Performance Comparison Tests

    [Fact]
    public void Performance_UserValue_vs_GenValue_Get()
    {
        _output.WriteLine("Comparing GET performance: User-declared Value vs Generator-created Value");
        _output.WriteLine(new string('=', 70));
        _output.WriteLine($"Iterations: {ITERATIONS:N0}");
        _output.WriteLine("");

        // Warmup
        WarmupUserValue();
        WarmupGenValue();

        // Test User-declared Value field
        var userReg = new PerfTestUserValueRegister { Value = 0xFF };
        var sw = Stopwatch.StartNew();
        int sum1 = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            sum1 += userReg.Mode;
            sum1 += userReg.Priority;
        }
        sw.Stop();
        var userTime = sw.Elapsed;
        _output.WriteLine($"User-declared Value:      {userTime.TotalMilliseconds,8:F2} ms ({ITERATIONS / userTime.TotalSeconds:N0} ops/sec)");

        // Test Generator-created Value field
        PerfTestGenValueRegister genReg = 0xFF;
        sw.Restart();
        int sum2 = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            sum2 += genReg.Mode;
            sum2 += genReg.Priority;
        }
        sw.Stop();
        var genTime = sw.Elapsed;
        _output.WriteLine($"Generator-created Value:  {genTime.TotalMilliseconds,8:F2} ms ({ITERATIONS / genTime.TotalSeconds:N0} ops/sec)");

        sum1.Should().Be(sum2, "Both should produce same result");

        var ratio = genTime.TotalMilliseconds / userTime.TotalMilliseconds;
        _output.WriteLine("");
        _output.WriteLine($"Ratio (Gen/User): {ratio:F3}x");
        _output.WriteLine(ratio > 1.1 ? "*** Generator-created is SLOWER ***" : 
                          ratio < 0.9 ? "*** Generator-created is FASTER ***" : 
                          "Performance is similar");
    }

    [Fact]
    public void Performance_UserValue_vs_GenValue_Set()
    {
        _output.WriteLine("Comparing SET performance: User-declared Value vs Generator-created Value");
        _output.WriteLine(new string('=', 70));
        _output.WriteLine($"Iterations: {ITERATIONS:N0}");
        _output.WriteLine("");

        // Warmup
        WarmupUserValue();
        WarmupGenValue();

        // Test User-declared Value field
        var userReg = new PerfTestUserValueRegister();
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < ITERATIONS; i++)
        {
            userReg.Mode = (byte)(i & 0x07);
            userReg.Priority = (byte)(i & 0x03);
        }
        sw.Stop();
        var userTime = sw.Elapsed;
        _output.WriteLine($"User-declared Value:      {userTime.TotalMilliseconds,8:F2} ms ({ITERATIONS / userTime.TotalSeconds:N0} ops/sec)");

        // Test Generator-created Value field
        PerfTestGenValueRegister genReg = 0;
        sw.Restart();
        for (int i = 0; i < ITERATIONS; i++)
        {
            genReg.Mode = (byte)(i & 0x07);
            genReg.Priority = (byte)(i & 0x03);
        }
        sw.Stop();
        var genTime = sw.Elapsed;
        _output.WriteLine($"Generator-created Value:  {genTime.TotalMilliseconds,8:F2} ms ({ITERATIONS / genTime.TotalSeconds:N0} ops/sec)");

        // Both should end up with same final value
        byte userFinal = userReg;
        byte genFinal = genReg;
        userFinal.Should().Be(genFinal, "Both should produce same final value");

        var ratio = genTime.TotalMilliseconds / userTime.TotalMilliseconds;
        _output.WriteLine("");
        _output.WriteLine($"Ratio (Gen/User): {ratio:F3}x");
        _output.WriteLine(ratio > 1.1 ? "*** Generator-created is SLOWER ***" : 
                          ratio < 0.9 ? "*** Generator-created is FASTER ***" : 
                          "Performance is similar");
    }

    [Fact]
    public void Performance_UserValue_vs_GenValue_Mixed()
    {
        _output.WriteLine("Comparing MIXED performance: User-declared Value vs Generator-created Value");
        _output.WriteLine(new string('=', 70));
        _output.WriteLine($"Iterations: {ITERATIONS:N0}");
        _output.WriteLine("");

        // Warmup
        WarmupUserValue();
        WarmupGenValue();

        // Test User-declared Value field
        var userReg = new PerfTestUserValueRegister();
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < ITERATIONS; i++)
        {
            userReg.Ready = true;
            if (userReg.Ready && !userReg.Busy)
            {
                userReg.Mode = 5;
                userReg.Priority = (byte)(userReg.Mode >> 1);
            }
            userReg.Busy = userReg.Priority > 1;
        }
        sw.Stop();
        var userTime = sw.Elapsed;
        _output.WriteLine($"User-declared Value:      {userTime.TotalMilliseconds,8:F2} ms ({ITERATIONS / userTime.TotalSeconds:N0} ops/sec)");

        // Test Generator-created Value field
        PerfTestGenValueRegister genReg = 0;
        sw.Restart();
        for (int i = 0; i < ITERATIONS; i++)
        {
            genReg.Ready = true;
            if (genReg.Ready && !genReg.Busy)
            {
                genReg.Mode = 5;
                genReg.Priority = (byte)(genReg.Mode >> 1);
            }
            genReg.Busy = genReg.Priority > 1;
        }
        sw.Stop();
        var genTime = sw.Elapsed;
        _output.WriteLine($"Generator-created Value:  {genTime.TotalMilliseconds,8:F2} ms ({ITERATIONS / genTime.TotalSeconds:N0} ops/sec)");

        // Both should end up with same final value
        byte userFinal = userReg;
        byte genFinal = genReg;
        userFinal.Should().Be(genFinal, "Both should produce same final value");

        var ratio = genTime.TotalMilliseconds / userTime.TotalMilliseconds;
        _output.WriteLine("");
        _output.WriteLine($"Ratio (Gen/User): {ratio:F3}x");
        _output.WriteLine(ratio > 1.1 ? "*** Generator-created is SLOWER ***" : 
                          ratio < 0.9 ? "*** Generator-created is FASTER ***" : 
                          "Performance is similar");
    }

    #endregion

    #region Helpers

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void WarmupUserValue()
    {
        var reg = new PerfTestUserValueRegister { Value = 0x55 };
        for (int i = 0; i < WARMUP_ITERATIONS; i++)
        {
            reg.Ready = !reg.Ready;
            reg.Mode = (byte)(i & 7);
            _ = reg.Priority;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void WarmupGenValue()
    {
        PerfTestGenValueRegister reg = 0x55;
        for (int i = 0; i < WARMUP_ITERATIONS; i++)
        {
            reg.Ready = !reg.Ready;
            reg.Mode = (byte)(i & 7);
            _ = reg.Priority;
        }
    }

    #endregion
}
