using System.Diagnostics;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

#region Signed Storage Type Test Structs

/// <summary>
/// 8-bit signed register (sbyte) for testing signed storage types.
/// </summary>
[BitFields(typeof(sbyte))]
public partial struct SignedReg8
{
    [BitFlag(0)] public partial bool Flag0 { get; set; }
    [BitFlag(1)] public partial bool Flag1 { get; set; }
    [BitFlag(7)] public partial bool Sign { get; set; }  // Sign bit
    [BitField(2, 3)] public partial byte LowField { get; set; }  // bits 2-4
    [BitField(5, 2)] public partial byte HighField { get; set; } // bits 5-6
}

/// <summary>
/// 16-bit signed register (short) for testing signed storage types.
/// </summary>
[BitFields(typeof(short))]
public partial struct SignedReg16
{
    [BitFlag(0)] public partial bool Flag0 { get; set; }
    [BitFlag(15)] public partial bool Sign { get; set; }  // Sign bit
    [BitField(1, 7)] public partial byte LowByte { get; set; }   // bits 1-7
    [BitField(8, 7)] public partial byte HighByte { get; set; }  // bits 8-14
}

/// <summary>
/// 32-bit signed register (int) for testing signed storage types.
/// </summary>
[BitFields(typeof(int))]
public partial struct SignedReg32
{
    [BitFlag(0)] public partial bool Flag0 { get; set; }
    [BitFlag(31)] public partial bool Sign { get; set; }  // Sign bit
    [BitField(1, 15)] public partial ushort LowWord { get; set; }  // bits 1-15
    [BitField(16, 15)] public partial ushort HighWord { get; set; } // bits 16-30
}

/// <summary>
/// 64-bit signed register (long) for testing signed storage types.
/// </summary>
[BitFields(typeof(long))]
public partial struct SignedReg64
{
    [BitFlag(0)] public partial bool Flag0 { get; set; }
    [BitFlag(63)] public partial bool Sign { get; set; }  // Sign bit
    [BitField(1, 31)] public partial uint LowDword { get; set; }   // bits 1-31
    [BitField(32, 31)] public partial uint HighDword { get; set; } // bits 32-62
}

/// <summary>
/// Generator-created signed 32-bit register for comparison.
/// </summary>
[BitFields(typeof(int))]
public partial struct SignedGenReg32
{
    [BitFlag(0)] public partial bool Flag0 { get; set; }
    [BitFlag(31)] public partial bool Sign { get; set; }
    [BitField(1, 15)] public partial ushort LowWord { get; set; }
    [BitField(16, 15)] public partial ushort HighWord { get; set; }
}

#endregion

/// <summary>
/// Unit tests for signed storage types and Value field access vs implicit conversion performance.
/// </summary>
public class BitFieldSignedAndAccessTests
{
    private const int ITERATIONS = 100_000_000;
    private const int WARMUP_ITERATIONS = 1_000_000;

    private readonly ITestOutputHelper _output;

    public BitFieldSignedAndAccessTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Signed sbyte (8-bit) Tests

    /// <summary>
    /// Tests signed 8-bit register - flags work correctly with negative values.
    /// </summary>
    [Fact]
    public void SignedReg8_Flags_WithNegativeValues()
    {
        SignedReg8 reg = -1; // 0xFF in two's complement

        reg.Flag0.Should().BeTrue();
        reg.Flag1.Should().BeTrue();
        reg.Sign.Should().BeTrue();

        // Clear sign bit
        reg.Sign = false;
        ((sbyte)reg).Should().Be(0x7F); // 127
        reg.Sign.Should().BeFalse();

        // Set sign bit (makes value negative)
        reg.Sign = true;
        ((sbyte)reg).Should().Be(-1); // 0xFF
    }

    /// <summary>
    /// Tests signed 8-bit register - fields work correctly with negative values.
    /// </summary>
    [Fact]
    public void SignedReg8_Fields_WithNegativeValues()
    {
        SignedReg8 reg = -128; // 0x80 - only sign bit set

        reg.Sign.Should().BeTrue();
        reg.LowField.Should().Be(0);
        reg.HighField.Should().Be(0);

        // Set fields while preserving sign
        reg.LowField = 7;
        reg.LowField.Should().Be(7);
        reg.Sign.Should().BeTrue();

        reg.HighField = 3;
        reg.HighField.Should().Be(3);
        reg.Sign.Should().BeTrue();

        // Expected: 0x80 | (7 << 2) | (3 << 5) = 0x80 | 0x1C | 0x60 = 0xFC = -4
        ((sbyte)reg).Should().Be(-4);
    }

    /// <summary>
    /// Tests signed 8-bit register - implicit conversion.
    /// </summary>
    [Fact]
    public void SignedReg8_ImplicitConversion()
    {
        SignedReg8 reg = -128;
        ((sbyte)reg).Should().Be(-128);
        reg.Sign.Should().BeTrue();

        sbyte value = reg;
        value.Should().Be(-128);

        // Test positive value
        reg = 127;
        ((sbyte)reg).Should().Be(127);
        reg.Sign.Should().BeFalse();
    }

    #endregion

    #region Signed short (16-bit) Tests

    /// <summary>
    /// Tests signed 16-bit register - flags work correctly with negative values.
    /// </summary>
    [Fact]
    public void SignedReg16_Flags_WithNegativeValues()
    {
        SignedReg16 reg = -1; // 0xFFFF

        reg.Flag0.Should().BeTrue();
        reg.Sign.Should().BeTrue();

        // Clear sign bit
        reg.Sign = false;
        ((short)reg).Should().Be(0x7FFF); // 32767
        reg.Sign.Should().BeFalse();

        // Set sign bit
        reg.Sign = true;
        ((short)reg).Should().Be(-1); // 0xFFFF
    }

    /// <summary>
    /// Tests signed 16-bit register - fields work correctly with negative values.
    /// </summary>
    [Fact]
    public void SignedReg16_Fields_WithNegativeValues()
    {
        SignedReg16 reg = short.MinValue; // 0x8000

        reg.Sign.Should().BeTrue();
        reg.LowByte.Should().Be(0);
        reg.HighByte.Should().Be(0);

        // Set fields
        reg.LowByte = 0x55;  // bits 1-7
        reg.HighByte = 0x2A; // bits 8-14

        reg.LowByte.Should().Be(0x55);
        reg.HighByte.Should().Be(0x2A);
        reg.Sign.Should().BeTrue();
    }

    /// <summary>
    /// Tests signed 16-bit register - implicit conversion.
    /// </summary>
    [Fact]
    public void SignedReg16_ImplicitConversion()
    {
        SignedReg16 reg = short.MinValue;
        ((short)reg).Should().Be(short.MinValue);
        reg.Sign.Should().BeTrue();

        short value = reg;
        value.Should().Be(short.MinValue);

        reg = short.MaxValue;
        ((short)reg).Should().Be(short.MaxValue);
        reg.Sign.Should().BeFalse();
    }

    #endregion

    #region Signed int (32-bit) Tests

    /// <summary>
    /// Tests signed 32-bit register - flags work correctly with negative values.
    /// </summary>
    [Fact]
    public void SignedReg32_Flags_WithNegativeValues()
    {
        SignedReg32 reg = -1; // 0xFFFFFFFF

        reg.Flag0.Should().BeTrue();
        reg.Sign.Should().BeTrue();

        reg.Sign = false;
        ((int)reg).Should().Be(int.MaxValue); // 0x7FFFFFFF

        reg.Sign = true;
        ((int)reg).Should().Be(-1);
    }

    /// <summary>
    /// Tests signed 32-bit register - fields work correctly with negative values.
    /// </summary>
    [Fact]
    public void SignedReg32_Fields_WithNegativeValues()
    {
        SignedReg32 reg = int.MinValue; // 0x80000000

        reg.Sign.Should().BeTrue();
        reg.LowWord.Should().Be(0);
        reg.HighWord.Should().Be(0);

        reg.LowWord = 0x5555;  // bits 1-15
        reg.HighWord = 0x2AAA; // bits 16-30

        reg.LowWord.Should().Be(0x5555);
        reg.HighWord.Should().Be(0x2AAA);
        reg.Sign.Should().BeTrue();
    }

    /// <summary>
    /// Tests signed 32-bit register - implicit conversion.
    /// </summary>
    [Fact]
    public void SignedReg32_ImplicitConversion()
    {
        SignedReg32 reg = int.MinValue;
        ((int)reg).Should().Be(int.MinValue);
        reg.Sign.Should().BeTrue();

        int value = reg;
        value.Should().Be(int.MinValue);

        reg = int.MaxValue;
        ((int)reg).Should().Be(int.MaxValue);
        reg.Sign.Should().BeFalse();
    }

    #endregion

    #region Signed long (64-bit) Tests

    /// <summary>
    /// Tests signed 64-bit register - flags work correctly with negative values.
    /// </summary>
    [Fact]
    public void SignedReg64_Flags_WithNegativeValues()
    {
        SignedReg64 reg = -1L; // 0xFFFFFFFFFFFFFFFF

        reg.Flag0.Should().BeTrue();
        reg.Sign.Should().BeTrue();

        reg.Sign = false;
        ((long)reg).Should().Be(long.MaxValue); // 0x7FFFFFFFFFFFFFFF

        reg.Sign = true;
        ((long)reg).Should().Be(-1L);
    }

    /// <summary>
    /// Tests signed 64-bit register - fields work correctly with negative values.
    /// </summary>
    [Fact]
    public void SignedReg64_Fields_WithNegativeValues()
    {
        SignedReg64 reg = long.MinValue; // 0x8000000000000000

        reg.Sign.Should().BeTrue();
        reg.LowDword.Should().Be(0);
        reg.HighDword.Should().Be(0);

        reg.LowDword = 0x55555555;  // bits 1-31
        reg.HighDword = 0x2AAAAAAA; // bits 32-62

        reg.LowDword.Should().Be(0x55555555);
        reg.HighDword.Should().Be(0x2AAAAAAA);
        reg.Sign.Should().BeTrue();
    }

    /// <summary>
    /// Tests signed 64-bit register - implicit conversion.
    /// </summary>
    [Fact]
    public void SignedReg64_ImplicitConversion()
    {
        SignedReg64 reg = long.MinValue;
        ((long)reg).Should().Be(long.MinValue);
        reg.Sign.Should().BeTrue();

        long value = reg;
        value.Should().Be(long.MinValue);

        reg = long.MaxValue;
        ((long)reg).Should().Be(long.MaxValue);
        reg.Sign.Should().BeFalse();
    }

    #endregion

    #region Signed vs Unsigned Comparison

    /// <summary>
    /// Verifies that signed and unsigned storage types with same bit layout produce same field values.
    /// </summary>
    [Fact]
    public void SignedReg32_MatchesUnsignedBitPattern()
    {
        // Both use generator-created Value now
        SignedReg32 signedReg = unchecked((int)0xAAAAAAAA);
        SignedGenReg32 genReg = unchecked((int)0xAAAAAAAA);

        // Both should give same field values
        signedReg.Flag0.Should().Be(genReg.Flag0);
        signedReg.Sign.Should().Be(genReg.Sign);
        signedReg.LowWord.Should().Be(genReg.LowWord);
        signedReg.HighWord.Should().Be(genReg.HighWord);
    }

    #endregion

    #region Signed Type Performance Tests

    /// <summary>
    /// Tests that signed storage types don't have significant performance penalties vs unsigned.
    /// </summary>
    [Fact]
    public void Performance_Signed_vs_Unsigned_Fields()
    {
        _output.WriteLine("Comparing SIGNED vs UNSIGNED storage type performance");
        _output.WriteLine(new string('=', 70));
        _output.WriteLine($"Iterations: {ITERATIONS:N0}");
        _output.WriteLine("");

        // Warmup
        WarmupSigned();
        WarmupUnsigned();

        // Test signed int storage
        SignedReg32 signedReg = 0;
        var sw = Stopwatch.StartNew();
        int sum1 = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            signedReg.LowWord = (ushort)(i & 0x7FFF);
            signedReg.HighWord = (ushort)((i >> 15) & 0x7FFF);
            sum1 += signedReg.LowWord + signedReg.HighWord;
        }
        sw.Stop();
        var signedTime = sw.Elapsed;
        _output.WriteLine($"Signed (int):             {signedTime.TotalMilliseconds,8:F2} ms ({ITERATIONS / signedTime.TotalSeconds:N0} ops/sec)");

        // Test unsigned uint storage
        GeneratedControlReg32 unsignedReg = 0;
        sw.Restart();
        int sum2 = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            unsignedReg.Address = (uint)(i & 0xFFFFFF);
            unsignedReg.Command = (byte)((i >> 24) & 0x0F);
            sum2 += (int)unsignedReg.Address + unsignedReg.Command;
        }
        sw.Stop();
        var unsignedTime = sw.Elapsed;
        _output.WriteLine($"Unsigned (uint):          {unsignedTime.TotalMilliseconds,8:F2} ms ({ITERATIONS / unsignedTime.TotalSeconds:N0} ops/sec)");

        var ratio = signedTime.TotalMilliseconds / unsignedTime.TotalMilliseconds;
        _output.WriteLine("");
        _output.WriteLine($"Ratio (Signed/Unsigned): {ratio:F3}x");
        _output.WriteLine(ratio > 1.2 ? "*** Signed is SLOWER ***" :
                          ratio < 0.8 ? "*** Signed is FASTER ***" :
                          "Performance is similar - signed types are viable");
    }

    #endregion

    #region Helpers

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void WarmupSigned()
    {
        SignedReg32 reg = 0;
        for (int i = 0; i < WARMUP_ITERATIONS; i++)
        {
            reg.LowWord = (ushort)i;
            _ = reg.LowWord;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void WarmupUnsigned()
    {
        GeneratedControlReg32 reg = 0;
        for (int i = 0; i < WARMUP_ITERATIONS; i++)
        {
            reg.Address = (uint)i;
            _ = reg.Address;
        }
    }

    #endregion
}
