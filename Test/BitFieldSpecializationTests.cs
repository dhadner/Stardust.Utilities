using System.Globalization;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Unit tests for NativeFloat (float/double) and NativeWide (UInt128/Int128) BitFields specializations.
/// </summary>
public partial class BitFieldSpecializationTests
{
    #region Test Struct Definitions

    /// <summary>IEEE 754 single-precision float register (32-bit, stored as uint).</summary>
    [BitFields(typeof(float))]
    public partial struct FloatRegister
    {
        [BitField(0, 22)] public partial uint Mantissa { get; set; }    // bits 0-22 (23-bit mantissa)
        [BitField(23, 30)] public partial byte Exponent { get; set; }   // bits 23-30 (8-bit exponent)
        [BitFlag(31)] public partial bool Sign { get; set; }             // bit 31 (sign bit)
    }

    /// <summary>IEEE 754 double-precision float register (64-bit, stored as ulong).</summary>
    [BitFields(typeof(double))]
    public partial struct DoubleRegister
    {
        [BitField(0, 51)] public partial ulong Mantissa { get; set; }   // bits 0-51 (52-bit mantissa)
        [BitField(52, 62)] public partial ushort Exponent { get; set; } // bits 52-62 (11-bit exponent)
        [BitFlag(63)] public partial bool Sign { get; set; }             // bit 63 (sign bit)

        // ?? User-defined computed properties (not generated) ???????????

        /// <summary>True if the value is NaN (exponent = all 1s, mantissa ? 0).</summary>
        public bool IsNaN => Exponent == 0x7FF && Mantissa != 0;

        /// <summary>True if the value is ±Infinity (exponent = all 1s, mantissa = 0).</summary>
        public bool IsInfinity => Exponent == 0x7FF && Mantissa == 0;

        /// <summary>True if the value is a denormalized number (exponent = 0, mantissa ? 0).</summary>
        public bool IsDenormalized => Exponent == 0 && Mantissa != 0;

        /// <summary>True if the value is a normalized number (0 &lt; exponent &lt; 0x7FF).</summary>
        public bool IsNormal => Exponent > 0 && Exponent < 0x7FF;

        /// <summary>True if the value is ±0 (exponent = 0, mantissa = 0).</summary>
        public bool IsZero => Exponent == 0 && Mantissa == 0;

        /// <summary>The unbiased exponent (biased ? 1023), or null for non-normal values (zero, denormalized, infinity, NaN).</summary>
        public int? UnbiasedExponent => IsNormal ? Exponent - 1023 : null;
    }

    /// <summary>128-bit struct backed by UInt128 (via multi-word with conversion operators).</summary>
    [BitFields(typeof(UInt128))]
    public partial struct U128Register
    {
        [BitField(0, 63)] public partial ulong Low { get; set; }    // bits 0-63
        [BitField(64, 127)] public partial ulong High { get; set; } // bits 64-127
    }

    /// <summary>128-bit struct backed by Int128 (via multi-word with conversion operators).</summary>
    [BitFields(typeof(Int128))]
    public partial struct I128Register
    {
        [BitField(0, 63)] public partial ulong Low { get; set; }    // bits 0-63
        [BitField(64, 127)] public partial ulong High { get; set; } // bits 64-127
    }

    /// <summary>
    /// Decimal register decomposing .NET's decimal layout via GetBits() canonical order:
    /// bits 0-31: lo, bits 32-63: mid, bits 64-95: hi (96-bit coefficient),
    /// bits 112-118: scale, bit 127: sign.
    /// </summary>
    [BitFields(typeof(decimal))]
    public partial struct DecimalRegister
    {
        [BitField(0, 95)] public partial UInt128 Coefficient { get; set; }   // 96-bit unsigned integer
        [BitField(112, 118)] public partial byte Scale { get; set; }         // 0-28 (power of 10 divisor)
        [BitFlag(127)] public partial bool Sign { get; set; }                // sign bit
    }

    /// <summary>
    /// IEEE 754 half-precision float register (16-bit, stored as ushort).
    /// Format: 1 sign bit, 5 exponent bits (bias 15), 10 mantissa bits.
    /// </summary>
    [BitFields(typeof(Half))]
    public partial struct HalfRegister
    {
        [BitField(0, 9)] public partial ushort Mantissa { get; set; }   // bits 0-9 (10-bit mantissa)
        [BitField(10, 14)] public partial byte Exponent { get; set; }   // bits 10-14 (5-bit exponent)
        [BitFlag(15)] public partial bool Sign { get; set; }             // bit 15 (sign bit)
    }

    #endregion

    #region NativeFloat: Construction Tests

    [Fact]
    public void FloatRegister_FromFloat_RoundTrips()
    {
        FloatRegister reg = 3.14f;
        float result = reg;
        result.Should().Be(3.14f);
    }

    [Fact]
    public void FloatRegister_FromFloat_Zero()
    {
        FloatRegister reg = 0.0f;
        float result = reg;
        result.Should().Be(0.0f);
    }

    [Fact]
    public void FloatRegister_FromFloat_Negative()
    {
        FloatRegister reg = -1.5f;
        float result = reg;
        result.Should().Be(-1.5f);
    }

    [Fact]
    public void DoubleRegister_FromDouble_RoundTrips()
    {
        DoubleRegister reg = 3.14159265358979;
        double result = reg;
        result.Should().Be(3.14159265358979);
    }

    [Fact]
    public void FloatRegister_RawBitsConstructor()
    {
        // 1.0f = 0x3F800000 in IEEE 754
        var reg = new FloatRegister(0x3F800000u);
        float result = reg;
        result.Should().Be(1.0f);
    }

    [Fact]
    public void DoubleRegister_RawBitsConstructor()
    {
        // 1.0 = 0x3FF0000000000000 in IEEE 754
        var reg = new DoubleRegister(0x3FF0000000000000UL);
        double result = reg;
        result.Should().Be(1.0);
    }

    #endregion

    #region NativeFloat: IEEE 754 Bit Decomposition

    [Fact]
    public void FloatRegister_DecomposePositiveOne()
    {
        FloatRegister reg = 1.0f;
        reg.Sign.Should().BeFalse("1.0f is positive");
        reg.Exponent.Should().Be(127, "biased exponent of 1.0f is 127");
        reg.Mantissa.Should().Be(0, "mantissa of 1.0f is 0 (implied 1.0)");
    }

    [Fact]
    public void FloatRegister_DecomposeNegativeTwo()
    {
        FloatRegister reg = -2.0f;
        reg.Sign.Should().BeTrue("-2.0f is negative");
        reg.Exponent.Should().Be(128, "biased exponent of 2.0 is 128");
        reg.Mantissa.Should().Be(0, "mantissa of 2.0 is 0 (implied 1.0)");
    }

    [Fact]
    public void FloatRegister_DecomposeZero()
    {
        FloatRegister reg = 0.0f;
        reg.Sign.Should().BeFalse();
        reg.Exponent.Should().Be(0);
        reg.Mantissa.Should().Be(0);
    }

    [Fact]
    public void DoubleRegister_DecomposePositiveOne()
    {
        DoubleRegister reg = 1.0;
        reg.Sign.Should().BeFalse("1.0 is positive");
        reg.Exponent.Should().Be(1023, "biased exponent of 1.0 is 1023");
        reg.Mantissa.Should().Be(0, "mantissa of 1.0 is 0 (implied 1.0)");
    }

    #endregion

    #region NativeFloat: Set Individual Fields

    [Fact]
    public void FloatRegister_SetSign()
    {
        FloatRegister reg = 1.0f;
        reg.Sign = true; // Flip to negative
        float result = reg;
        result.Should().Be(-1.0f);
    }

    [Fact]
    public void FloatRegister_SetExponent()
    {
        // Build +1.0f from parts: sign=false, exponent=127, mantissa=0
        var reg = new FloatRegister(0u); // all zero bits
        reg.Sign = false;
        reg.Exponent = 127;
        reg.Mantissa = 0;
        float result = reg;
        result.Should().Be(1.0f);
    }

    [Fact]
    public void FloatRegister_MantissaBits()
    {
        // 1.5f = sign:0, exponent:127 (bias), mantissa:0x400000 (bit 22 set = 0.5)
        FloatRegister reg = 1.5f;
        reg.Sign.Should().BeFalse();
        reg.Exponent.Should().Be(127);
        reg.Mantissa.Should().Be(0x400000u);
    }

    #endregion

    #region NativeFloat: Arithmetic Operators

    [Fact]
    public void FloatRegister_Addition()
    {
        FloatRegister a = 1.5f;
        FloatRegister b = 2.5f;
        FloatRegister result = a + b;
        ((float)result).Should().Be(4.0f);
    }

    [Fact]
    public void FloatRegister_Subtraction()
    {
        FloatRegister a = 5.0f;
        FloatRegister b = 3.0f;
        FloatRegister result = a - b;
        ((float)result).Should().Be(2.0f);
    }

    [Fact]
    public void FloatRegister_Multiplication()
    {
        FloatRegister a = 3.0f;
        FloatRegister b = 4.0f;
        FloatRegister result = a * b;
        ((float)result).Should().Be(12.0f);
    }

    [Fact]
    public void FloatRegister_Division()
    {
        FloatRegister a = 10.0f;
        FloatRegister b = 4.0f;
        FloatRegister result = a / b;
        ((float)result).Should().Be(2.5f);
    }

    [Fact]
    public void FloatRegister_UnaryNegation()
    {
        FloatRegister a = 3.14f;
        FloatRegister result = -a;
        ((float)result).Should().Be(-3.14f);
    }

    [Fact]
    public void FloatRegister_MixedArithmetic_WithFloat()
    {
        FloatRegister a = 10.0f;
        FloatRegister result = a + 5.0f;
        ((float)result).Should().Be(15.0f);
    }

    [Fact]
    public void DoubleRegister_Addition()
    {
        DoubleRegister a = 1.5;
        DoubleRegister b = 2.5;
        DoubleRegister result = a + b;
        ((double)result).Should().Be(4.0);
    }

    #endregion

    #region NativeFloat: Comparison Operators

    [Fact]
    public void FloatRegister_LessThan()
    {
        FloatRegister a = 1.0f;
        FloatRegister b = 2.0f;
        (a < b).Should().BeTrue();
        (b < a).Should().BeFalse();
    }

    [Fact]
    public void FloatRegister_GreaterThan()
    {
        FloatRegister a = 3.0f;
        FloatRegister b = 1.0f;
        (a > b).Should().BeTrue();
    }

    [Fact]
    public void FloatRegister_Equality()
    {
        FloatRegister a = 42.0f;
        FloatRegister b = 42.0f;
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact]
    public void FloatRegister_Inequality()
    {
        FloatRegister a = 1.0f;
        FloatRegister b = 2.0f;
        (a != b).Should().BeTrue();
    }

    #endregion

    #region NativeFloat: Bitwise Operators

    [Fact]
    public void FloatRegister_BitwiseOps_OnRawBits()
    {
        // Bitwise ops work on the raw uint bits
        FloatRegister a = new FloatRegister(0xFF00FF00u);
        FloatRegister b = new FloatRegister(0x00FF00FFu);
        var result = a | b;
        ((uint)result).Should().Be(0xFFFFFFFFu);
    }

    [Fact]
    public void FloatRegister_Complement()
    {
        var a = new FloatRegister(0u);
        var result = ~a;
        ((uint)result).Should().Be(0xFFFFFFFFu);
    }

    #endregion

    #region NativeFloat: Parsing and Formatting

    [Fact]
    public void FloatRegister_Parse()
    {
        var reg = FloatRegister.Parse("3.14", CultureInfo.InvariantCulture);
        ((float)reg).Should().BeApproximately(3.14f, 0.001f);
    }

    [Fact]
    public void FloatRegister_TryParse_Valid()
    {
        FloatRegister.TryParse("42.5", out var result).Should().BeTrue();
        ((float)result).Should().Be(42.5f);
    }

    [Fact]
    public void FloatRegister_TryParse_Invalid()
    {
        FloatRegister.TryParse("not_a_number", out _).Should().BeFalse();
    }

    [Fact]
    public void FloatRegister_TryParse_Null()
    {
        FloatRegister.TryParse(null, out _).Should().BeFalse();
    }

    [Fact]
    public void FloatRegister_ToString_Default()
    {
        FloatRegister reg = 3.14f;
        reg.ToString().Should().Be(3.14f.ToString());
    }

    [Fact]
    public void FloatRegister_ToString_Format()
    {
        FloatRegister reg = 3.14159f;
        var str = reg.ToString("F2", CultureInfo.InvariantCulture);
        str.Should().Be("3.14");
    }

    [Fact]
    public void DoubleRegister_Parse()
    {
        var reg = DoubleRegister.Parse("2.71828", CultureInfo.InvariantCulture);
        ((double)reg).Should().BeApproximately(2.71828, 0.00001);
    }

    #endregion

    #region NativeFloat: Fluent API and Static Properties

    [Fact]
    public void FloatRegister_WithSign()
    {
        FloatRegister reg = 1.0f;
        var negative = reg.WithSign(true);
        ((float)negative).Should().Be(-1.0f);
        ((float)reg).Should().Be(1.0f, "original should be unchanged");
    }

    [Fact]
    public void FloatRegister_SignBit()
    {
        var signBit = FloatRegister.SignBit;
        signBit.Sign.Should().BeTrue();
        signBit.Exponent.Should().Be(0);
        signBit.Mantissa.Should().Be(0);
    }

    [Fact]
    public void FloatRegister_ExponentMask()
    {
        var mask = FloatRegister.ExponentMask;
        mask.Exponent.Should().Be(0xFF); // all 8 bits set
        mask.Sign.Should().BeFalse();
        mask.Mantissa.Should().Be(0);
    }

    #endregion

    #region NativeFloat: Struct Size

    [Fact]
    public void FloatRegister_SizeIs4Bytes()
    {
        Unsafe.SizeOf<FloatRegister>().Should().Be(4, "float storage = uint = 4 bytes");
    }

    [Fact]
    public void DoubleRegister_SizeIs8Bytes()
    {
        Unsafe.SizeOf<DoubleRegister>().Should().Be(8, "double storage = ulong = 8 bytes");
    }

    #endregion

    #region NativeWide (UInt128): Construction and Conversion

    [Fact]
    public void U128Register_FromUInt128_RoundTrips()
    {
        UInt128 original = ((UInt128)0xDEADBEEF << 64) | 0xCAFEBABE;
        U128Register reg = original;
        UInt128 result = reg;
        result.Should().Be(original);
    }

    [Fact]
    public void U128Register_FromUInt128_MaxValue()
    {
        U128Register reg = UInt128.MaxValue;
        UInt128 result = reg;
        result.Should().Be(UInt128.MaxValue);
    }

    [Fact]
    public void U128Register_FromUInt128_Zero()
    {
        U128Register reg = (UInt128)0;
        UInt128 result = reg;
        result.Should().Be((UInt128)0);
    }

    [Fact]
    public void U128Register_FieldAccess_ViaConversion()
    {
        UInt128 value = ((UInt128)0xAAAAAAAAAAAAAAAA << 64) | 0xBBBBBBBBBBBBBBBB;
        U128Register reg = value;
        reg.Low.Should().Be(0xBBBBBBBBBBBBBBBB);
        reg.High.Should().Be(0xAAAAAAAAAAAAAAAA);
    }

    [Fact]
    public void U128Register_SetFields_ConvertBack()
    {
        U128Register reg = default;
        reg.Low = 0x1234567890ABCDEF;
        reg.High = 0xFEDCBA0987654321;
        UInt128 result = reg;
        UInt128 expected = ((UInt128)0xFEDCBA0987654321 << 64) | 0x1234567890ABCDEF;
        result.Should().Be(expected);
    }

    #endregion

    #region NativeWide (Int128): Construction and Conversion

    [Fact]
    public void I128Register_FromInt128_Positive()
    {
        Int128 original = (Int128)42;
        I128Register reg = original;
        Int128 result = reg;
        result.Should().Be(original);
    }

    [Fact]
    public void I128Register_FromInt128_Negative()
    {
        Int128 original = (Int128)(-1);
        I128Register reg = original;
        Int128 result = reg;
        result.Should().Be(original);
    }

    [Fact]
    public void I128Register_FromInt128_MaxValue()
    {
        I128Register reg = Int128.MaxValue;
        Int128 result = reg;
        result.Should().Be(Int128.MaxValue);
    }

    [Fact]
    public void I128Register_FromInt128_MinValue()
    {
        I128Register reg = Int128.MinValue;
        Int128 result = reg;
        result.Should().Be(Int128.MinValue);
    }

    #endregion

    #region NativeWide: Struct Size

    [Fact]
    public void U128Register_SizeIs16Bytes()
    {
        Unsafe.SizeOf<U128Register>().Should().Be(16, "UInt128 ? 2 ulongs = 16 bytes");
    }

    [Fact]
    public void I128Register_SizeIs16Bytes()
    {
        Unsafe.SizeOf<I128Register>().Should().Be(16, "Int128 ? 2 ulongs = 16 bytes");
    }

    #endregion

    #region NativeWide: Arithmetic via Multi-Word

    [Fact]
    public void U128Register_Addition()
    {
        U128Register a = (UInt128)100;
        U128Register b = (UInt128)200;
        U128Register result = a + b;
        ((UInt128)result).Should().Be((UInt128)300);
    }

    [Fact]
    public void U128Register_Bitwise()
    {
        U128Register a = (UInt128)0xFF00;
        U128Register b = (UInt128)0x00FF;
        UInt128 result = a | b;
        result.Should().Be((UInt128)0xFFFF);
    }

    #endregion

    #region IEEE 754 Double-Precision Deep Dive

    // IEEE 754 double-precision format:
    //   Bit 63:      Sign (0 = positive, 1 = negative)
    //   Bits 52-62:  Exponent (11-bit, biased by 1023)
    //   Bits 0-51:   Mantissa (52-bit fractional, implicit leading 1 for normal numbers)
    //
    // Value = (-1)^sign × 2^(exponent - 1023) × (1 + mantissa/2^52)

    [Fact]
    public void DoubleRegister_BuildPi_FromBitFields()
    {
        // ? ? 3.14159265358979323846
        // IEEE 754: sign=0, exponent=1024 (biased, 2^1), mantissa=0x921FB54442D18
        // Verification: (-1)^0 × 2^(1024-1023) × (1 + 0x921FB54442D18/2^52) = ?
        DoubleRegister pi = default;
        pi.Sign = false;
        pi.Exponent = 1024;
        pi.Mantissa = 0x921FB54442D18;
        ((double)pi).Should().Be(System.Math.PI);
    }

    [Fact]
    public void DoubleRegister_BuildEuler_FromBitFields()
    {
        // e ? 2.71828182845904523536
        // IEEE 754: sign=0, exponent=1024, mantissa=0x5BF0A8B145769
        DoubleRegister e = System.Math.E;
        e.Sign.Should().BeFalse();
        e.Exponent.Should().Be(1024, "2 ? e < 4, so exponent is 1024 (2^1)");

        // Rebuild from decomposed parts
        DoubleRegister rebuilt = default;
        rebuilt.Sign = e.Sign;
        rebuilt.Exponent = e.Exponent;
        rebuilt.Mantissa = e.Mantissa;
        ((double)rebuilt).Should().Be(System.Math.E);
    }

    [Fact]
    public void DoubleRegister_ConstructInfinity()
    {
        // +Infinity: sign=0, exponent=all 1s (2047), mantissa=0
        DoubleRegister inf = default;
        inf.Sign = false;
        inf.Exponent = 0x7FF; // all 11 bits set
        inf.Mantissa = 0;
        double.IsPositiveInfinity(inf).Should().BeTrue();
        inf.IsInfinity.Should().BeTrue();
        inf.IsNaN.Should().BeFalse();

        // -Infinity: just flip the sign
        inf.Sign = true;
        double.IsNegativeInfinity(inf).Should().BeTrue();
        inf.IsInfinity.Should().BeTrue("sign doesn't affect infinity classification");
    }

    [Fact]
    public void DoubleRegister_ConstructNaN()
    {
        // NaN: exponent=all 1s (2047), mantissa?0
        DoubleRegister nan = default;
        nan.Exponent = 0x7FF;
        nan.Mantissa = 1; // any non-zero mantissa
        double.IsNaN(nan).Should().BeTrue();
        nan.IsNaN.Should().BeTrue();
        nan.IsInfinity.Should().BeFalse("NaN is not infinity");
    }

    [Fact]
    public void DoubleRegister_ConstructSmallestDenormalized()
    {
        // Smallest denormalized double: sign=0, exponent=0, mantissa=1
        // Value = 2^(-1074) ? 5 × 10^(-324)
        DoubleRegister tiny = default;
        tiny.Sign = false;
        tiny.Exponent = 0;
        tiny.IsDenormalized.Should().BeFalse("mantissa not yet set");
        tiny.IsZero.Should().BeTrue("exponent=0, mantissa=0 is zero");
        tiny.Mantissa = 1;
        double val = tiny;
        val.Should().Be(double.Epsilon, "should be the smallest positive double");
    }

    [Fact]
    public void DoubleRegister_NegateByFlippingSignBit()
    {
        // Negation in IEEE 754 is just toggling bit 63
        DoubleRegister pi = System.Math.PI;
        pi.Sign = !pi.Sign;
        ((double)pi).Should().Be(-System.Math.PI);

        // Flip back
        pi.Sign = !pi.Sign;
        ((double)pi).Should().Be(System.Math.PI);
    }

    [Fact]
    public void DoubleRegister_Arithmetic_GoldenRatio()
    {
        // ? = (1 + ?5) / 2 ? 1.6180339887...
        DoubleRegister one = 1.0;
        DoubleRegister sqrt5 = System.Math.Sqrt(5.0);
        DoubleRegister two = 2.0;
        DoubleRegister phi = (one + sqrt5) / two;
        ((double)phi).Should().BeApproximately(1.6180339887498949, 1e-15);
    }

    [Fact]
    public void DoubleRegister_Arithmetic_AreaOfCircle()
    {
        // A = ? × r²  for r = 3.5
        DoubleRegister pi = System.Math.PI;
        DoubleRegister r = 3.5;
        DoubleRegister area = pi * r * r;
        ((double)area).Should().BeApproximately(System.Math.PI * 3.5 * 3.5, 1e-12);
    }

    [Fact]
    public void DoubleRegister_Arithmetic_QuadraticFormula()
    {
        // Solve x² - 5x + 6 = 0 ? x = (5 ± ?(25-24)) / 2 = 3, 2
        DoubleRegister a = 1.0;
        DoubleRegister b = -5.0;
        DoubleRegister c = 6.0;
        DoubleRegister four = 4.0;
        DoubleRegister two = 2.0;

        DoubleRegister discriminant = b * b - four * a * c;
        ((double)discriminant).Should().Be(1.0);

        DoubleRegister sqrtDisc = System.Math.Sqrt(discriminant);
        DoubleRegister x1 = (-b + sqrtDisc) / (two * a);
        DoubleRegister x2 = (-b - sqrtDisc) / (two * a);

        ((double)x1).Should().Be(3.0);
        ((double)x2).Should().Be(2.0);
    }

    [Fact]
    public void DoubleRegister_ExponentReveals_PowerOfTwo()
    {
        // For powers of 2, mantissa is always zero
        for (int p = -10; p <= 10; p++)
        {
            DoubleRegister reg = System.Math.Pow(2.0, p);
            reg.Mantissa.Should().Be(0, $"2^{p} has no fractional mantissa");
            reg.Exponent.Should().Be((ushort)(1023 + p), $"exponent for 2^{p} is {1023 + p}");
        }
    }

    [Fact]
    public void DoubleRegister_FluentBuild_OnePointFive()
    {
        // 1.5 = 1 + 0.5 ? exponent=1023 (2^0), mantissa MSB set (0.5 contribution)
        // mantissa bit 51 = 0.5 ? mantissa value = 2^51 = 0x8000000000000
        var reg = DoubleRegister.Zero
            .WithSign(false)
            .WithExponent(1023)
            .WithMantissa(0x8_0000_0000_0000);
        ((double)reg).Should().Be(1.5);
    }

    #endregion

    #region IEEE 754 Double-Precision: Computed Classification Properties

    [Fact]
    public void DoubleRegister_IsNormal_ForOrdinaryValues()
    {
        DoubleRegister pi = System.Math.PI;
        pi.IsNormal.Should().BeTrue();
        pi.IsNaN.Should().BeFalse();
        pi.IsInfinity.Should().BeFalse();
        pi.IsDenormalized.Should().BeFalse();
        pi.IsZero.Should().BeFalse();
    }

    [Fact]
    public void DoubleRegister_IsNaN_ForConstructedNaN()
    {
        DoubleRegister nan = default;
        nan.Exponent = 0x7FF;
        nan.Mantissa = 1;
        nan.IsNaN.Should().BeTrue();
        nan.IsInfinity.Should().BeFalse("NaN has non-zero mantissa");
        nan.IsNormal.Should().BeFalse();
    }

    [Fact]
    public void DoubleRegister_IsInfinity_ForConstructedInfinity()
    {
        DoubleRegister posInf = default;
        posInf.Exponent = 0x7FF;
        posInf.Mantissa = 0;
        posInf.IsInfinity.Should().BeTrue();
        posInf.IsNaN.Should().BeFalse("Infinity has zero mantissa");
        posInf.Sign.Should().BeFalse("positive infinity");

        posInf.Sign = true;
        posInf.IsInfinity.Should().BeTrue("still infinity when negative");
    }

    [Fact]
    public void DoubleRegister_IsDenormalized_ForEpsilon()
    {
        DoubleRegister tiny = double.Epsilon;
        tiny.IsDenormalized.Should().BeTrue();
        tiny.IsNormal.Should().BeFalse();
        tiny.IsZero.Should().BeFalse();
        tiny.Exponent.Should().Be(0, "denormalized numbers have zero exponent");
    }

    [Fact]
    public void DoubleRegister_IsZero_ForPositiveAndNegativeZero()
    {
        DoubleRegister posZero = 0.0;
        posZero.IsZero.Should().BeTrue();
        posZero.Sign.Should().BeFalse();

        DoubleRegister negZero = -0.0;
        negZero.IsZero.Should().BeTrue();
        negZero.Sign.Should().BeTrue("negative zero has sign bit set");
    }

    [Fact]
    public void DoubleRegister_UnbiasedExponent_ForPowersOfTwo()
    {
        DoubleRegister one = 1.0;
        one.UnbiasedExponent.Should().Be(0, "1.0 = 2^0");

        DoubleRegister eight = 8.0;
        eight.UnbiasedExponent.Should().Be(3, "8.0 = 2^3");

        DoubleRegister quarter = 0.25;
        quarter.UnbiasedExponent.Should().Be(-2, "0.25 = 2^(-2)");
    }

    [Fact]
    public void DoubleRegister_UnbiasedExponent_NullForNonNormal()
    {
        DoubleRegister zero = 0.0;
        zero.UnbiasedExponent.Should().BeNull("zero is not a normal number");

        DoubleRegister denorm = double.Epsilon;
        denorm.UnbiasedExponent.Should().BeNull("denormalized is not a normal number");

        DoubleRegister inf = double.PositiveInfinity;
        inf.UnbiasedExponent.Should().BeNull("infinity is not a normal number");

        DoubleRegister nan = double.NaN;
        nan.UnbiasedExponent.Should().BeNull("NaN is not a normal number");
    }

    [Fact]
    public void DoubleRegister_ClassificationCoversAllCategories()
    {
        // Exactly one classification should be true for each value category
        (DoubleRegister val, string category)[] cases =
        [
            (System.Math.PI, "normal"),
            (double.Epsilon, "denormalized"),
            (0.0,            "zero"),
            (double.PositiveInfinity, "infinity"),
            (double.NaN,     "nan"),
        ];

        foreach (var (val, category) in cases)
        {
            int trueCount = (val.IsNormal ? 1 : 0)
                          + (val.IsDenormalized ? 1 : 0)
                          + (val.IsZero ? 1 : 0)
                          + (val.IsInfinity ? 1 : 0)
                          + (val.IsNaN ? 1 : 0);
            trueCount.Should().Be(1, $"{category} should match exactly one classification");
        }
    }

    #endregion

    #region Decimal: Construction and Round-Trip Tests

    [Fact]
    public void DecimalRegister_FromDecimal_RoundTrips()
    {
        DecimalRegister reg = 3.14m;
        decimal result = reg;
        result.Should().Be(3.14m);
    }

    [Fact]
    public void DecimalRegister_FromDecimal_Zero()
    {
        DecimalRegister reg = 0m;
        decimal result = reg;
        result.Should().Be(0m);
    }

    [Fact]
    public void DecimalRegister_FromDecimal_Negative()
    {
        DecimalRegister reg = -1.5m;
        decimal result = reg;
        result.Should().Be(-1.5m);
        reg.Sign.Should().BeTrue();
    }

    [Fact]
    public void DecimalRegister_Sign_Positive()
    {
        DecimalRegister reg = 42m;
        reg.Sign.Should().BeFalse();
    }

    [Fact]
    public void DecimalRegister_Sign_FlipNegates()
    {
        DecimalRegister reg = 100m;
        reg.Sign = true;
        ((decimal)reg).Should().Be(-100m);
    }

    [Fact]
    public void DecimalRegister_Scale_ReflectsDecimalPlaces()
    {
        // 1.23 has scale 2 (divided by 10^2)
        DecimalRegister reg = 1.23m;
        reg.Scale.Should().Be(2);
    }

    [Fact]
    public void DecimalRegister_Coefficient_WholeNumber()
    {
        // 42 has coefficient 42 and scale 0
        DecimalRegister reg = 42m;
        reg.Coefficient.Should().Be(42);
        reg.Scale.Should().Be(0);
    }

    [Fact]
    public void DecimalRegister_MaxValue_RoundTrips()
    {
        DecimalRegister reg = decimal.MaxValue;
        decimal result = reg;
        result.Should().Be(decimal.MaxValue);
    }

    [Fact]
    public void DecimalRegister_MinValue_RoundTrips()
    {
        DecimalRegister reg = decimal.MinValue;
        decimal result = reg;
        result.Should().Be(decimal.MinValue);
    }

    #endregion

    #region Decimal: Arithmetic Tests

    [Fact]
    public void DecimalRegister_Addition()
    {
        DecimalRegister a = 1.5m;
        DecimalRegister b = 2.5m;
        decimal result = a + b;
        result.Should().Be(4.0m);
    }

    [Fact]
    public void DecimalRegister_Subtraction()
    {
        DecimalRegister a = 10m;
        DecimalRegister b = 3.5m;
        decimal result = a - b;
        result.Should().Be(6.5m);
    }

    [Fact]
    public void DecimalRegister_Multiplication()
    {
        DecimalRegister a = 3m;
        DecimalRegister b = 7m;
        decimal result = a * b;
        result.Should().Be(21m);
    }

    [Fact]
    public void DecimalRegister_Division()
    {
        DecimalRegister a = 10m;
        DecimalRegister b = 4m;
        decimal result = a / b;
        result.Should().Be(2.5m);
    }

    [Fact]
    public void DecimalRegister_Negation()
    {
        DecimalRegister reg = 42m;
        decimal result = -reg;
        result.Should().Be(-42m);
    }

    [Fact]
    public void DecimalRegister_MixedArithmetic_WithDecimal()
    {
        DecimalRegister reg = 10m;
        decimal result = reg + 5m;
        result.Should().Be(15m);
    }

    #endregion

    #region Decimal: Comparison Tests

    [Fact]
    public void DecimalRegister_LessThan()
    {
        DecimalRegister a = 1m;
        DecimalRegister b = 2m;
        (a < b).Should().BeTrue();
        (b < a).Should().BeFalse();
    }

    [Fact]
    public void DecimalRegister_CompareTo()
    {
        DecimalRegister a = 5m;
        DecimalRegister b = 10m;
        a.CompareTo(b).Should().BeNegative();
        b.CompareTo(a).Should().BePositive();
        a.CompareTo(a).Should().Be(0);
    }

    [Fact]
    public void DecimalRegister_Equality()
    {
        DecimalRegister a = 3.14m;
        DecimalRegister b = 3.14m;
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    #endregion

    #region Decimal: Parsing and Formatting Tests

    [Fact]
    public void DecimalRegister_Parse()
    {
        var reg = DecimalRegister.Parse("3.14", CultureInfo.InvariantCulture);
        ((decimal)reg).Should().Be(3.14m);
    }

    [Fact]
    public void DecimalRegister_TryParse_Valid()
    {
        DecimalRegister.TryParse("42.5", out var result).Should().BeTrue();
        ((decimal)result).Should().Be(42.5m);
    }

    [Fact]
    public void DecimalRegister_TryParse_Invalid()
    {
        DecimalRegister.TryParse("not_a_number", out _).Should().BeFalse();
    }

    [Fact]
    public void DecimalRegister_TryParse_Null()
    {
        DecimalRegister.TryParse(null, out _).Should().BeFalse();
    }

    [Fact]
    public void DecimalRegister_ToString_Default()
    {
        DecimalRegister reg = 3.14m;
        reg.ToString().Should().Be(3.14m.ToString());
    }

    [Fact]
    public void DecimalRegister_ToString_Format()
    {
        DecimalRegister reg = 3.14159m;
        var str = reg.ToString("F2", CultureInfo.InvariantCulture);
        str.Should().Be("3.14");
    }

    #endregion

    #region Half: Construction and Round-Trip Tests

    [Fact]
    public void HalfRegister_FromHalf_RoundTrips()
    {
        HalfRegister reg = (Half)3.14;
        Half result = reg;
        result.Should().Be((Half)3.14);
    }

    [Fact]
    public void HalfRegister_FromHalf_Zero()
    {
        HalfRegister reg = Half.Zero;
        Half result = reg;
        result.Should().Be(Half.Zero);
    }

    [Fact]
    public void HalfRegister_FromHalf_Negative()
    {
        HalfRegister reg = (Half)(-1.5);
        Half result = reg;
        result.Should().Be((Half)(-1.5));
        reg.Sign.Should().BeTrue();
    }

    [Fact]
    public void HalfRegister_RawBitsConstructor()
    {
        // 1.0 in Half = 0x3C00 (sign=0, exp=15, mantissa=0)
        var reg = new HalfRegister(0x3C00);
        Half result = reg;
        result.Should().Be((Half)1.0);
    }

    #endregion

    #region Half: IEEE 754 Bit Decomposition

    [Fact]
    public void HalfRegister_DecomposePositiveOne()
    {
        HalfRegister reg = (Half)1.0;
        reg.Sign.Should().BeFalse("1.0 is positive");
        reg.Exponent.Should().Be(15, "biased exponent of 1.0 in Half is 15");
        reg.Mantissa.Should().Be(0, "mantissa of 1.0 is 0 (implied 1.0)");
    }

    [Fact]
    public void HalfRegister_DecomposeNegativeTwo()
    {
        HalfRegister reg = (Half)(-2.0);
        reg.Sign.Should().BeTrue("-2.0 is negative");
        reg.Exponent.Should().Be(16, "biased exponent of 2.0 in Half is 16");
        reg.Mantissa.Should().Be(0, "mantissa of 2.0 is 0 (implied 1.0)");
    }

    [Fact]
    public void HalfRegister_DecomposeOnePointFive()
    {
        // 1.5 in Half: sign=0, exponent=15 (bias), mantissa=0x200 (bit 9 set = 0.5)
        HalfRegister reg = (Half)1.5;
        reg.Sign.Should().BeFalse();
        reg.Exponent.Should().Be(15);
        reg.Mantissa.Should().Be(0x200);
    }

    #endregion

    #region Half: Set Individual Fields

    [Fact]
    public void HalfRegister_SetSign()
    {
        HalfRegister reg = (Half)1.0;
        reg.Sign = true;
        Half result = reg;
        result.Should().Be((Half)(-1.0));
    }

    [Fact]
    public void HalfRegister_SetExponent()
    {
        var reg = new HalfRegister((ushort)0); // all zero bits
        reg.Sign = false;
        reg.Exponent = 15; // bias for 2^0
        reg.Mantissa = 0;
        Half result = reg;
        result.Should().Be((Half)1.0);
    }

    #endregion

    #region Half: Arithmetic Operators

    [Fact]
    public void HalfRegister_Addition()
    {
        HalfRegister a = (Half)1.5;
        HalfRegister b = (Half)2.5;
        Half result = a + b;
        result.Should().Be((Half)4.0);
    }

    [Fact]
    public void HalfRegister_Subtraction()
    {
        HalfRegister a = (Half)5.0;
        HalfRegister b = (Half)3.0;
        Half result = a - b;
        result.Should().Be((Half)2.0);
    }

    [Fact]
    public void HalfRegister_Multiplication()
    {
        HalfRegister a = (Half)3.0;
        HalfRegister b = (Half)4.0;
        Half result = a * b;
        result.Should().Be((Half)12.0);
    }

    [Fact]
    public void HalfRegister_Division()
    {
        HalfRegister a = (Half)10.0;
        HalfRegister b = (Half)4.0;
        Half result = a / b;
        result.Should().Be((Half)2.5);
    }

    [Fact]
    public void HalfRegister_UnaryNegation()
    {
        HalfRegister reg = (Half)3.0;
        Half result = -reg;
        result.Should().Be((Half)(-3.0));
    }

    [Fact]
    public void HalfRegister_MixedArithmetic_WithHalf()
    {
        HalfRegister a = (Half)10.0;
        Half result = a + (Half)5.0;
        result.Should().Be((Half)15.0);
    }

    #endregion

    #region Half: Comparison Operators

    [Fact]
    public void HalfRegister_LessThan()
    {
        HalfRegister a = (Half)1.0;
        HalfRegister b = (Half)2.0;
        (a < b).Should().BeTrue();
        (b < a).Should().BeFalse();
    }

    [Fact]
    public void HalfRegister_Equality()
    {
        HalfRegister a = (Half)42.0;
        HalfRegister b = (Half)42.0;
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    #endregion

    #region Half: Parsing and Formatting

    [Fact]
    public void HalfRegister_Parse()
    {
        var reg = HalfRegister.Parse("3.14", CultureInfo.InvariantCulture);
        ((Half)reg).Should().Be((Half)3.14);
    }

    [Fact]
    public void HalfRegister_TryParse_Valid()
    {
        HalfRegister.TryParse("42.5", out var result).Should().BeTrue();
        ((Half)result).Should().Be((Half)42.5);
    }

    [Fact]
    public void HalfRegister_TryParse_Invalid()
    {
        HalfRegister.TryParse("not_a_number", out _).Should().BeFalse();
    }

    [Fact]
    public void HalfRegister_TryParse_Null()
    {
        HalfRegister.TryParse(null, out _).Should().BeFalse();
    }

    [Fact]
    public void HalfRegister_ToString_Default()
    {
        HalfRegister reg = (Half)3.14;
        reg.ToString().Should().Be(((Half)3.14).ToString());
    }

    [Fact]
    public void HalfRegister_ToString_Format()
    {
        HalfRegister reg = (Half)3.14;
        var str = reg.ToString("F1", CultureInfo.InvariantCulture);
        str.Should().Be("3.1");
    }

    #endregion

    #region Half: Struct Size and Fluent API

    [Fact]
    public void HalfRegister_SizeIs2Bytes()
    {
        Unsafe.SizeOf<HalfRegister>().Should().Be(2, "Half storage = ushort = 2 bytes");
    }

    [Fact]
    public void HalfRegister_WithSign()
    {
        HalfRegister reg = (Half)1.0;
        var negative = reg.WithSign(true);
        ((Half)negative).Should().Be((Half)(-1.0));
        ((Half)reg).Should().Be((Half)1.0, "original should be unchanged");
    }

    [Fact]
    public void HalfRegister_FluentBuild_OnePointFive()
    {
        // 1.5 = sign:0, exponent:15 (bias), mantissa:0x200
        var reg = HalfRegister.Zero
            .WithSign(false)
            .WithExponent(15)
            .WithMantissa(0x200);
        ((Half)reg).Should().Be((Half)1.5);
    }

    #endregion
}
