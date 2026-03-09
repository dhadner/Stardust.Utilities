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
    [BitField(2, 4)] public partial byte LowField { get; set; }   // bits 2..=4 (3 bits)
    [BitField(5, 6)] public partial byte HighField { get; set; }  // bits 5..=6 (2 bits)
}

/// <summary>
/// 16-bit signed register (short) for testing signed storage types.
/// </summary>
[BitFields(typeof(short))]
public partial struct SignedReg16
{
    [BitFlag(0)] public partial bool Flag0 { get; set; }
    [BitFlag(15)] public partial bool Sign { get; set; }  // Sign bit
    [BitField(1, 7)] public partial byte LowByte { get; set; }    // bits 1..=7 (7 bits)
    [BitField(8, 14)] public partial byte HighByte { get; set; }  // bits 8..=14 (7 bits)
}

/// <summary>
/// 32-bit signed register (int) for testing signed storage types.
/// </summary>
[BitFields(typeof(int))]
public partial struct SignedReg32
{
    [BitFlag(0)] public partial bool Flag0 { get; set; }
    [BitFlag(31)] public partial bool Sign { get; set; }  // Sign bit
    [BitField(1, 15)] public partial ushort LowWord { get; set; }   // bits 1..=15 (15 bits)
    [BitField(16, 30)] public partial ushort HighWord { get; set; } // bits 16..=30 (15 bits)
}

/// <summary>
/// 64-bit signed register (long) for testing signed storage types.
/// </summary>
[BitFields(typeof(long))]
public partial struct SignedReg64
{
    [BitFlag(0)] public partial bool Flag0 { get; set; }
    [BitFlag(63)] public partial bool Sign { get; set; }  // Sign bit
    [BitField(1, 31)] public partial uint LowDword { get; set; }    // bits 1..=31 (31 bits)
    [BitField(32, 62)] public partial uint HighDword { get; set; }  // bits 32..=62 (31 bits)
}

/// <summary>
/// Generator-created signed 32-bit register for comparison.
/// </summary>
[BitFields(typeof(int))]
public partial struct SignedGenReg32
{
    [BitFlag(0)] public partial bool Flag0 { get; set; }
    [BitFlag(31)] public partial bool Sign { get; set; }
    [BitField(1, 15)] public partial ushort LowWord { get; set; }   // bits 1..=15 (15 bits)
    [BitField(16, 30)] public partial ushort HighWord { get; set; } // bits 16..=30 (15 bits)
}

#endregion

#region Signed Property Type Test Structs (Sign Extension)

/// <summary>
/// Tests signed property types with UNSIGNED storage.
/// The property type determines sign extension, not the storage type.
/// </summary>
[BitFields(typeof(ushort))]
public partial struct SignedPropertyReg16
{
    /// <summary>3-bit signed field at bits 13-15. Values: -4 to +3.</summary>
    [BitField(13, 15)] public partial sbyte Delta { get; set; }       // 3 bits, signed property
    
    /// <summary>3-bit unsigned field at bits 10-12. Values: 0 to 7.</summary>
    [BitField(10, 12)] public partial byte UnsignedField { get; set; } // 3 bits, unsigned property
    
    /// <summary>4-bit signed field at bits 6-9. Values: -8 to +7.</summary>
    [BitField(6, 9)] public partial sbyte SignedNibble { get; set; }  // 4 bits, signed property
    
    /// <summary>6-bit signed field at bits 0-5. Values: -32 to +31.</summary>
    [BitField(0, 5)] public partial sbyte Offset { get; set; }        // 6 bits, signed property
}

/// <summary>
/// Tests signed property types with SIGNED storage (should work the same).
/// </summary>
[BitFields(typeof(short))]
public partial struct SignedPropertyRegSigned16
{
    /// <summary>3-bit signed field at bits 13-15. Values: -4 to +3.</summary>
    [BitField(13, 15)] public partial sbyte Delta { get; set; }
    
    /// <summary>4-bit signed field at bits 9-12. Values: -8 to +7.</summary>
    [BitField(9, 12)] public partial sbyte SignedNibble { get; set; }
}

/// <summary>
/// Tests 32-bit signed property fields with unsigned storage.
/// </summary>
[BitFields(typeof(uint))]
public partial struct SignedPropertyReg32
{
    /// <summary>8-bit signed field at bits 24-31. Values: -128 to +127.</summary>
    [BitField(24, 31)] public partial sbyte HighByte { get; set; }
    
    /// <summary>16-bit signed field at bits 8-23. Values: -32768 to +32767.</summary>
    [BitField(8, 23)] public partial short MiddleWord { get; set; }
    
    /// <summary>8-bit unsigned field at bits 0-7. Values: 0 to 255.</summary>
    [BitField(0, 7)] public partial byte LowByte { get; set; }
}

/// <summary>
/// Tests int property type for larger signed fields.
/// </summary>
[BitFields(typeof(ulong))]
public partial struct SignedPropertyReg64
{
    /// <summary>32-bit signed field at bits 32-63. Full int range.</summary>
    [BitField(32, 63)] public partial int HighInt { get; set; }
    
    /// <summary>32-bit unsigned field at bits 0-31.</summary>
    [BitField(0, 31)] public partial uint LowUInt { get; set; }
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

    #region Signed Property Type Tests (Sign Extension)

    /// <summary>
    /// Tests that a 3-bit signed property returns negative values when MSB is set.
    /// Field bits 13-15, values: -4 to +3 (3-bit two's complement).
    /// </summary>
    [Fact]
    public void SignedProperty_3BitField_ReturnsNegativeWhenMsbSet()
    {
        SignedPropertyReg16 reg = 0;

        // Test all 3-bit values: 0-7 unsigned maps to 0,1,2,3,-4,-3,-2,-1 signed
        // Binary  Unsigned  Signed
        // 000     0         0
        // 001     1         1
        // 010     2         2
        // 011     3         3
        // 100     4        -4
        // 101     5        -3
        // 110     6        -2
        // 111     7        -1

        reg.Delta = 0;
        reg.Delta.Should().Be(0);

        reg.Delta = 1;
        reg.Delta.Should().Be(1);

        reg.Delta = 2;
        reg.Delta.Should().Be(2);

        reg.Delta = 3;
        reg.Delta.Should().Be(3);

        reg.Delta = -4;
        reg.Delta.Should().Be(-4);

        reg.Delta = -3;
        reg.Delta.Should().Be(-3);

        reg.Delta = -2;
        reg.Delta.Should().Be(-2);

        reg.Delta = -1;
        reg.Delta.Should().Be(-1);
    }

    /// <summary>
    /// Tests that setting raw bit patterns correctly sign-extends on read.
    /// </summary>
    [Fact]
    public void SignedProperty_RawBitPattern_SignExtendsCorrectly()
    {
        // Set Delta field (bits 13-15) to binary 100 = -4
        // Bits 13-15 = 0b100 shifted = 0x8000
        SignedPropertyReg16 reg = unchecked((ushort)0x8000);
        reg.Delta.Should().Be(-4, "binary 100 in 3-bit signed is -4");

        // Set Delta field to binary 111 = -1
        // Bits 13-15 = 0b111 shifted = 0xE000
        reg = unchecked((ushort)0xE000);
        reg.Delta.Should().Be(-1, "binary 111 in 3-bit signed is -1");

        // Set Delta field to binary 011 = +3
        // Bits 13-15 = 0b011 shifted = 0x6000
        reg = unchecked((ushort)0x6000);
        reg.Delta.Should().Be(3, "binary 011 in 3-bit signed is +3");
    }

    /// <summary>
    /// Tests 4-bit signed nibble field. Values: -8 to +7.
    /// </summary>
    [Fact]
    public void SignedProperty_4BitNibble_SignExtendsCorrectly()
    {
        SignedPropertyReg16 reg = 0;

        // Positive values
        reg.SignedNibble = 0;
        reg.SignedNibble.Should().Be(0);

        reg.SignedNibble = 7;
        reg.SignedNibble.Should().Be(7);

        // Negative values
        reg.SignedNibble = -8;
        reg.SignedNibble.Should().Be(-8);

        reg.SignedNibble = -1;
        reg.SignedNibble.Should().Be(-1);

        reg.SignedNibble = -4;
        reg.SignedNibble.Should().Be(-4);
    }

    /// <summary>
    /// Tests 6-bit signed offset field. Values: -32 to +31.
    /// </summary>
    [Fact]
    public void SignedProperty_6BitOffset_SignExtendsCorrectly()
    {
        SignedPropertyReg16 reg = 0;

        // Positive boundary
        reg.Offset = 31;
        reg.Offset.Should().Be(31);

        // Negative boundary
        reg.Offset = -32;
        reg.Offset.Should().Be(-32);

        // Middle values
        reg.Offset = 15;
        reg.Offset.Should().Be(15);

        reg.Offset = -16;
        reg.Offset.Should().Be(-16);
    }

    /// <summary>
    /// Tests that unsigned property type does NOT sign extend.
    /// </summary>
    [Fact]
    public void UnsignedProperty_DoesNotSignExtend()
    {
        SignedPropertyReg16 reg = 0;

        // UnsignedField is 3 bits with byte property type
        reg.UnsignedField = 7;
        reg.UnsignedField.Should().Be(7);

        reg.UnsignedField = 4;
        reg.UnsignedField.Should().Be(4, "unsigned 3-bit field with MSB set stays positive");
    }

    /// <summary>
    /// Tests signed property with signed storage type (should work the same as unsigned storage).
    /// </summary>
    [Fact]
    public void SignedProperty_WithSignedStorage_WorksCorrectly()
    {
        SignedPropertyRegSigned16 reg = 0;

        // Delta is 3-bit signed at bits 13-15
        reg.Delta = -4;
        reg.Delta.Should().Be(-4);

        reg.Delta = 3;
        reg.Delta.Should().Be(3);

        // SignedNibble is 4-bit signed at bits 9-12
        reg.SignedNibble = -8;
        reg.SignedNibble.Should().Be(-8);

        reg.SignedNibble = 7;
        reg.SignedNibble.Should().Be(7);
    }

    /// <summary>
    /// Tests 32-bit register with various signed field sizes.
    /// </summary>
    [Fact]
    public void SignedProperty_32BitRegister_SignExtendsCorrectly()
    {
        SignedPropertyReg32 reg = 0;

        // 8-bit signed high byte (bits 24-31)
        reg.HighByte = -128;
        reg.HighByte.Should().Be(-128);

        reg.HighByte = 127;
        reg.HighByte.Should().Be(127);

        reg.HighByte = -1;
        reg.HighByte.Should().Be(-1);

        // 16-bit signed middle word (bits 8-23)
        reg.MiddleWord = short.MinValue;
        reg.MiddleWord.Should().Be(short.MinValue);

        reg.MiddleWord = short.MaxValue;
        reg.MiddleWord.Should().Be(short.MaxValue);

        reg.MiddleWord = -1;
        reg.MiddleWord.Should().Be(-1);

        // 8-bit unsigned low byte (bits 0-7) - should not sign extend
        reg.LowByte = 255;
        reg.LowByte.Should().Be(255);
    }

    /// <summary>
    /// Tests 64-bit register with 32-bit signed int property.
    /// </summary>
    [Fact]
    public void SignedProperty_64BitRegister_32BitSignedField()
    {
        SignedPropertyReg64 reg = 0;

        // HighInt is 32-bit signed at bits 32-63
        reg.HighInt = int.MinValue;
        reg.HighInt.Should().Be(int.MinValue);

        reg.HighInt = int.MaxValue;
        reg.HighInt.Should().Be(int.MaxValue);

        reg.HighInt = -1;
        reg.HighInt.Should().Be(-1);

        reg.HighInt = 0;
        reg.HighInt.Should().Be(0);

        // Verify low uint is independent
        reg.LowUInt = uint.MaxValue;
        reg.LowUInt.Should().Be(uint.MaxValue);
        reg.HighInt.Should().Be(0, "setting LowUInt should not affect HighInt");
    }

    /// <summary>
    /// Tests that mixing signed and unsigned fields in same register works correctly.
    /// </summary>
    [Fact]
    public void SignedProperty_MixedFields_IndependentAndCorrect()
    {
        SignedPropertyReg16 reg = 0;

        // Set all fields
        reg.Delta = -2;          // 3-bit signed
        reg.UnsignedField = 5;   // 3-bit unsigned
        reg.SignedNibble = -5;   // 4-bit signed
        reg.Offset = -20;        // 6-bit signed

        // Verify all fields independently
        reg.Delta.Should().Be(-2);
        reg.UnsignedField.Should().Be(5);
        reg.SignedNibble.Should().Be(-5);
        reg.Offset.Should().Be(-20);

        // Modify one and verify others unchanged
        reg.Delta = 2;
        reg.Delta.Should().Be(2);
        reg.UnsignedField.Should().Be(5);
        reg.SignedNibble.Should().Be(-5);
        reg.Offset.Should().Be(-20);
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
