using System.Globalization;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Tests for the pre-defined numeric BitFields types: IEEE754Half, IEEE754Single,
/// IEEE754Double, and DecimalBitFields.
/// </summary>
public class NumericBitFieldsTests
{
    #region IEEE754Half: Round-Trip and Construction

    [Fact]
    public void Half_RoundTrips()
    {
        IEEE754Half h = (Half)3.14;
        Half result = h;
        result.Should().Be((Half)3.14);
    }

    [Fact]
    public void Half_Zero_RoundTrips()
    {
        IEEE754Half h = Half.Zero;
        Half result = h;
        result.Should().Be(Half.Zero);
    }

    [Fact]
    public void Half_Negative_RoundTrips()
    {
        IEEE754Half h = (Half)(-1.5);
        Half result = h;
        result.Should().Be((Half)(-1.5));
        h.Sign.Should().BeTrue();
    }

    [Fact]
    public void Half_RawBitsConstructor()
    {
        // 1.0 in Half = 0x3C00 (sign=0, exp=15, mantissa=0)
        var h = new IEEE754Half(0x3C00);
        Half result = h;
        result.Should().Be((Half)1.0);
    }

    #endregion

    #region IEEE754Half: Bit Decomposition

    [Fact]
    public void Half_DecomposePositiveOne()
    {
        IEEE754Half h = (Half)1.0;
        h.Sign.Should().BeFalse();
        h.BiasedExponent.Should().Be(15, "biased exponent of 1.0 in Half is 15");
        h.Mantissa.Should().Be(0, "mantissa of 1.0 is 0 (implied 1.0)");
    }

    [Fact]
    public void Half_DecomposeNegativeTwo()
    {
        IEEE754Half h = (Half)(-2.0);
        h.Sign.Should().BeTrue();
        h.BiasedExponent.Should().Be(16, "biased exponent of 2.0 in Half is 16");
        h.Mantissa.Should().Be(0);
    }

    [Fact]
    public void Half_DecomposeOnePointFive()
    {
        IEEE754Half h = (Half)1.5;
        h.Sign.Should().BeFalse();
        h.BiasedExponent.Should().Be(15);
        h.Mantissa.Should().Be(0x200, "bit 9 set = 0.5 contribution");
    }

    #endregion

    #region IEEE754Half: Classification Properties

    [Fact]
    public void Half_IsNormal_ForOrdinaryValues()
    {
        IEEE754Half h = (Half)1.0;
        h.IsNormal.Should().BeTrue();
        h.IsNaN.Should().BeFalse();
        h.IsInfinity.Should().BeFalse();
        h.IsDenormalized.Should().BeFalse();
        h.IsZero.Should().BeFalse();
    }

    [Fact]
    public void Half_IsZero()
    {
        IEEE754Half h = (Half)0.0;
        h.IsZero.Should().BeTrue();
        h.IsNormal.Should().BeFalse();
    }

    [Fact]
    public void Half_IsInfinity()
    {
        IEEE754Half h = Half.PositiveInfinity;
        h.IsInfinity.Should().BeTrue();
        h.IsNaN.Should().BeFalse();
        h.IsNormal.Should().BeFalse();
    }

    [Fact]
    public void Half_IsNaN()
    {
        IEEE754Half h = Half.NaN;
        h.IsNaN.Should().BeTrue();
        h.IsInfinity.Should().BeFalse();
        h.IsNormal.Should().BeFalse();
    }

    [Fact]
    public void Half_Exponent_ForPowerOfTwo()
    {
        IEEE754Half h = (Half)1.0;
        h.Exponent.Should().Be(0, "1.0 = 2^0");

        IEEE754Half four = (Half)4.0;
        four.Exponent.Should().Be(2, "4.0 = 2^2");
    }

    [Fact]
    public void Half_Exponent_NullForNonNormal()
    {
        IEEE754Half zero = (Half)0.0;
        zero.Exponent.Should().BeNull();

        IEEE754Half inf = Half.PositiveInfinity;
        inf.Exponent.Should().BeNull();

        IEEE754Half nan = Half.NaN;
        nan.Exponent.Should().BeNull();
    }

    [Fact]
    public void Half_ClassificationCoversAllCategories()
    {
        (IEEE754Half val, string category)[] cases =
        [
            ((Half)1.0, "normal"),
            ((Half)0.0, "zero"),
            (Half.PositiveInfinity, "infinity"),
            (Half.NaN, "nan"),
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

    #region IEEE754Half: Constants

    [Fact]
    public void Half_ExponentBias_Is15()
    {
        IEEE754Half.EXPONENT_BIAS.Should().Be(15);
    }

    [Fact]
    public void Half_MaxExponent_Is31()
    {
        IEEE754Half.MAX_EXPONENT.Should().Be(0x1F);
    }

    #endregion

    #region IEEE754Half: Set Individual Fields

    [Fact]
    public void Half_SetSign_Negates()
    {
        IEEE754Half h = (Half)1.0;
        h.Sign = true;
        ((Half)h).Should().Be((Half)(-1.0));
    }

    [Fact]
    public void Half_BuildFromParts()
    {
        var h = IEEE754Half.Zero
            .WithSign(false)
            .WithBiasedExponent(15)
            .WithMantissa(0x200);
        ((Half)h).Should().Be((Half)1.5);
    }

    #endregion

    #region IEEE754Half: Arithmetic

    [Fact]
    public void Half_Addition()
    {
        IEEE754Half a = (Half)1.5;
        IEEE754Half b = (Half)2.5;
        Half result = a + b;
        result.Should().Be((Half)4.0);
    }

    [Fact]
    public void Half_Negation()
    {
        IEEE754Half h = (Half)3.0;
        Half result = -h;
        result.Should().Be((Half)(-3.0));
    }

    #endregion

    #region IEEE754Half: Struct Size

    [Fact]
    public void Half_SizeIs2Bytes()
    {
        Unsafe.SizeOf<IEEE754Half>().Should().Be(2);
    }

    #endregion

    #region IEEE754Single: Round-Trip and Construction

    [Fact]
    public void Single_RoundTrips()
    {
        IEEE754Single f = 3.14f;
        float result = f;
        result.Should().Be(3.14f);
    }

    [Fact]
    public void Single_Zero_RoundTrips()
    {
        IEEE754Single f = 0.0f;
        float result = f;
        result.Should().Be(0.0f);
    }

    [Fact]
    public void Single_Negative_RoundTrips()
    {
        IEEE754Single f = -1.5f;
        float result = f;
        result.Should().Be(-1.5f);
        f.Sign.Should().BeTrue();
    }

    [Fact]
    public void Single_RawBitsConstructor()
    {
        // 1.0f = 0x3F800000 in IEEE 754
        var f = new IEEE754Single(0x3F800000u);
        float result = f;
        result.Should().Be(1.0f);
    }

    #endregion

    #region IEEE754Single: Bit Decomposition

    [Fact]
    public void Single_DecomposePositiveOne()
    {
        IEEE754Single f = 1.0f;
        f.Sign.Should().BeFalse();
        f.BiasedExponent.Should().Be(127, "biased exponent of 1.0f is 127");
        f.Mantissa.Should().Be(0u, "mantissa of 1.0f is 0 (implied 1.0)");
    }

    [Fact]
    public void Single_DecomposeNegativeTwo()
    {
        IEEE754Single f = -2.0f;
        f.Sign.Should().BeTrue();
        f.BiasedExponent.Should().Be(128, "biased exponent of 2.0f is 128");
        f.Mantissa.Should().Be(0u);
    }

    [Fact]
    public void Single_DecomposeOnePointFive()
    {
        // 1.5f: sign=0, biasedExponent=127, mantissa=0x400000 (bit 22 set = 0.5)
        IEEE754Single f = 1.5f;
        f.Sign.Should().BeFalse();
        f.BiasedExponent.Should().Be(127);
        f.Mantissa.Should().Be(0x400000u);
    }

    #endregion

    #region IEEE754Single: Classification Properties

    [Fact]
    public void Single_IsNormal_ForOrdinaryValues()
    {
        IEEE754Single f = 3.14f;
        f.IsNormal.Should().BeTrue();
        f.IsNaN.Should().BeFalse();
        f.IsInfinity.Should().BeFalse();
        f.IsDenormalized.Should().BeFalse();
        f.IsZero.Should().BeFalse();
    }

    [Fact]
    public void Single_IsZero()
    {
        IEEE754Single f = 0.0f;
        f.IsZero.Should().BeTrue();
        f.IsNormal.Should().BeFalse();
    }

    [Fact]
    public void Single_NegativeZero()
    {
        IEEE754Single f = -0.0f;
        f.IsZero.Should().BeTrue();
        f.Sign.Should().BeTrue("negative zero has sign bit set");
    }

    [Fact]
    public void Single_IsInfinity()
    {
        IEEE754Single f = float.PositiveInfinity;
        f.IsInfinity.Should().BeTrue();
        f.IsNaN.Should().BeFalse();
    }

    [Fact]
    public void Single_NegativeInfinity()
    {
        IEEE754Single f = float.NegativeInfinity;
        f.IsInfinity.Should().BeTrue();
        f.Sign.Should().BeTrue();
    }

    [Fact]
    public void Single_IsNaN()
    {
        IEEE754Single f = float.NaN;
        f.IsNaN.Should().BeTrue();
        f.IsInfinity.Should().BeFalse();
    }

    [Fact]
    public void Single_IsDenormalized()
    {
        IEEE754Single f = float.Epsilon;
        f.IsDenormalized.Should().BeTrue();
        f.IsNormal.Should().BeFalse();
        f.BiasedExponent.Should().Be(0);
    }

    [Fact]
    public void Single_Exponent_ForPowersOfTwo()
    {
        IEEE754Single one = 1.0f;
        one.Exponent.Should().Be(0, "1.0 = 2^0");

        IEEE754Single eight = 8.0f;
        eight.Exponent.Should().Be(3, "8.0 = 2^3");

        IEEE754Single quarter = 0.25f;
        quarter.Exponent.Should().Be(-2, "0.25 = 2^(-2)");
    }

    [Fact]
    public void Single_Exponent_NullForNonNormal()
    {
        IEEE754Single zero = 0.0f;
        zero.Exponent.Should().BeNull();

        IEEE754Single inf = float.PositiveInfinity;
        inf.Exponent.Should().BeNull();

        IEEE754Single nan = float.NaN;
        nan.Exponent.Should().BeNull();
    }

    [Fact]
    public void Single_ClassificationCoversAllCategories()
    {
        (IEEE754Single val, string category)[] cases =
        [
            (3.14f, "normal"),
            (float.Epsilon, "denormalized"),
            (0.0f, "zero"),
            (float.PositiveInfinity, "infinity"),
            (float.NaN, "nan"),
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

    #region IEEE754Single: Constants

    [Fact]
    public void Single_ExponentBias_Is127()
    {
        IEEE754Single.EXPONENT_BIAS.Should().Be(127);
    }

    [Fact]
    public void Single_MaxExponent_Is255()
    {
        IEEE754Single.MAX_EXPONENT.Should().Be(0xFF);
    }

    #endregion

    #region IEEE754Single: Set Individual Fields

    [Fact]
    public void Single_SetSign_Negates()
    {
        IEEE754Single f = 1.0f;
        f.Sign = true;
        ((float)f).Should().Be(-1.0f);
    }

    [Fact]
    public void Single_BuildFromParts()
    {
        var f = new IEEE754Single(0u);
        f.Sign = false;
        f.BiasedExponent = 127;
        f.Mantissa = 0;
        ((float)f).Should().Be(1.0f);
    }

    [Fact]
    public void Single_FluentBuild_OnePointFive()
    {
        // 1.5f: sign=0, biasedExponent=127, mantissa=0x400000
        var f = IEEE754Single.Zero
            .WithSign(false)
            .WithBiasedExponent(127)
            .WithMantissa(0x400000u);
        ((float)f).Should().Be(1.5f);
    }

    #endregion

    #region IEEE754Single: Arithmetic

    [Fact]
    public void Single_Addition()
    {
        IEEE754Single a = 1.5f;
        IEEE754Single b = 2.5f;
        float result = a + b;
        result.Should().Be(4.0f);
    }

    [Fact]
    public void Single_Subtraction()
    {
        IEEE754Single a = 5.0f;
        IEEE754Single b = 3.0f;
        float result = a - b;
        result.Should().Be(2.0f);
    }

    [Fact]
    public void Single_Multiplication()
    {
        IEEE754Single a = 3.0f;
        IEEE754Single b = 4.0f;
        float result = a * b;
        result.Should().Be(12.0f);
    }

    [Fact]
    public void Single_Division()
    {
        IEEE754Single a = 10.0f;
        IEEE754Single b = 4.0f;
        float result = a / b;
        result.Should().Be(2.5f);
    }

    [Fact]
    public void Single_Negation()
    {
        IEEE754Single f = 3.14f;
        float result = -f;
        result.Should().Be(-3.14f);
    }

    [Fact]
    public void Single_NegateByFlippingSign()
    {
        IEEE754Single f = 42.0f;
        f.Sign = !f.Sign;
        ((float)f).Should().Be(-42.0f);

        f.Sign = !f.Sign;
        ((float)f).Should().Be(42.0f);
    }

    #endregion

    #region IEEE754Single: Struct Size

    [Fact]
    public void Single_SizeIs4Bytes()
    {
        Unsafe.SizeOf<IEEE754Single>().Should().Be(4);
    }

    #endregion

    #region IEEE754Double: Round-Trip and Construction

    [Fact]
    public void Double_RoundTrips()
    {
        IEEE754Double d = 3.14159265358979;
        double result = d;
        result.Should().Be(3.14159265358979);
    }

    [Fact]
    public void Double_Zero_RoundTrips()
    {
        IEEE754Double d = 0.0;
        double result = d;
        result.Should().Be(0.0);
    }

    [Fact]
    public void Double_Negative_RoundTrips()
    {
        IEEE754Double d = -1.5;
        double result = d;
        result.Should().Be(-1.5);
        d.Sign.Should().BeTrue();
    }

    [Fact]
    public void Double_RawBitsConstructor()
    {
        // 1.0 = 0x3FF0000000000000 in IEEE 754
        var d = new IEEE754Double(0x3FF0000000000000UL);
        double result = d;
        result.Should().Be(1.0);
    }

    #endregion

    #region IEEE754Double: Bit Decomposition

    [Fact]
    public void Double_DecomposePositiveOne()
    {
        IEEE754Double d = 1.0;
        d.Sign.Should().BeFalse();
        d.BiasedExponent.Should().Be(1023, "biased exponent of 1.0 is 1023");
        d.Mantissa.Should().Be(0UL, "mantissa of 1.0 is 0 (implied 1.0)");
    }

    [Fact]
    public void Double_DecomposeNegativeTwo()
    {
        IEEE754Double d = -2.0;
        d.Sign.Should().BeTrue();
        d.BiasedExponent.Should().Be(1024);
        d.Mantissa.Should().Be(0UL);
    }

    [Fact]
    public void Double_DecomposePi()
    {
        IEEE754Double pi = Math.PI;
        pi.Sign.Should().BeFalse();
        pi.BiasedExponent.Should().Be(1024, "2 <= pi < 4, so biased exponent is 1024");
        pi.Mantissa.Should().Be(0x921FB54442D18UL);
    }

    #endregion

    #region IEEE754Double: Classification Properties

    [Fact]
    public void Double_IsNormal_ForOrdinaryValues()
    {
        IEEE754Double d = Math.PI;
        d.IsNormal.Should().BeTrue();
        d.IsNaN.Should().BeFalse();
        d.IsInfinity.Should().BeFalse();
        d.IsDenormalized.Should().BeFalse();
        d.IsZero.Should().BeFalse();
    }

    [Fact]
    public void Double_IsZero()
    {
        IEEE754Double d = 0.0;
        d.IsZero.Should().BeTrue();
    }

    [Fact]
    public void Double_NegativeZero()
    {
        IEEE754Double d = -0.0;
        d.IsZero.Should().BeTrue();
        d.Sign.Should().BeTrue("negative zero has sign bit set");
    }

    [Fact]
    public void Double_IsInfinity()
    {
        IEEE754Double posInf = double.PositiveInfinity;
        posInf.IsInfinity.Should().BeTrue();
        posInf.Sign.Should().BeFalse();

        IEEE754Double negInf = double.NegativeInfinity;
        negInf.IsInfinity.Should().BeTrue();
        negInf.Sign.Should().BeTrue();
    }

    [Fact]
    public void Double_IsNaN()
    {
        IEEE754Double d = double.NaN;
        d.IsNaN.Should().BeTrue();
        d.IsInfinity.Should().BeFalse();
    }

    [Fact]
    public void Double_IsDenormalized()
    {
        IEEE754Double d = double.Epsilon;
        d.IsDenormalized.Should().BeTrue();
        d.IsNormal.Should().BeFalse();
        d.BiasedExponent.Should().Be(0);
    }

    [Fact]
    public void Double_Exponent_ForPowersOfTwo()
    {
        IEEE754Double one = 1.0;
        one.Exponent.Should().Be(0, "1.0 = 2^0");

        IEEE754Double eight = 8.0;
        eight.Exponent.Should().Be(3, "8.0 = 2^3");

        IEEE754Double quarter = 0.25;
        quarter.Exponent.Should().Be(-2, "0.25 = 2^(-2)");
    }

    [Fact]
    public void Double_Exponent_NullForNonNormal()
    {
        IEEE754Double zero = 0.0;
        zero.Exponent.Should().BeNull();

        IEEE754Double denorm = double.Epsilon;
        denorm.Exponent.Should().BeNull();

        IEEE754Double inf = double.PositiveInfinity;
        inf.Exponent.Should().BeNull();

        IEEE754Double nan = double.NaN;
        nan.Exponent.Should().BeNull();
    }

    [Fact]
    public void Double_ClassificationCoversAllCategories()
    {
        (IEEE754Double val, string category)[] cases =
        [
            (Math.PI, "normal"),
            (double.Epsilon, "denormalized"),
            (0.0, "zero"),
            (double.PositiveInfinity, "infinity"),
            (double.NaN, "nan"),
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

    [Fact]
    public void Double_BiasedExponentReveals_PowerOfTwo()
    {
        for (int p = -10; p <= 10; p++)
        {
            IEEE754Double d = Math.Pow(2.0, p);
            d.Mantissa.Should().Be(0UL, $"2^{p} has no fractional mantissa");
            d.BiasedExponent.Should().Be((ushort)(1023 + p), $"biased exponent for 2^{p} is {1023 + p}");
        }
    }

    #endregion

    #region IEEE754Double: Constants

    [Fact]
    public void Double_ExponentBias_Is1023()
    {
        IEEE754Double.EXPONENT_BIAS.Should().Be(1023);
    }

    [Fact]
    public void Double_MaxExponent_Is2047()
    {
        IEEE754Double.MAX_EXPONENT.Should().Be(0x7FF);
    }

    #endregion

    #region IEEE754Double: Set Individual Fields and Build

    [Fact]
    public void Double_SetSign_Negates()
    {
        IEEE754Double d = 1.0;
        d.Sign = true;
        ((double)d).Should().Be(-1.0);
    }

    [Fact]
    public void Double_NegateByFlippingSign()
    {
        IEEE754Double pi = Math.PI;
        pi.Sign = !pi.Sign;
        ((double)pi).Should().Be(-Math.PI);

        pi.Sign = !pi.Sign;
        ((double)pi).Should().Be(Math.PI);
    }

    [Fact]
    public void Double_BuildPi_FromParts()
    {
        IEEE754Double pi = default;
        pi.Sign = false;
        pi.BiasedExponent = 1024;
        pi.Mantissa = 0x921FB54442D18UL;
        ((double)pi).Should().Be(Math.PI);
    }

    [Fact]
    public void Double_FluentBuild_OnePointFive()
    {
        var d = IEEE754Double.Zero
            .WithSign(false)
            .WithBiasedExponent(1023)
            .WithMantissa(0x8_0000_0000_0000UL);
        ((double)d).Should().Be(1.5);
    }

    [Fact]
    public void Double_ConstructInfinity()
    {
        IEEE754Double inf = default;
        inf.BiasedExponent = 0x7FF;
        inf.Mantissa = 0;
        double.IsPositiveInfinity(inf).Should().BeTrue();
        inf.IsInfinity.Should().BeTrue();
    }

    [Fact]
    public void Double_ConstructNaN()
    {
        IEEE754Double nan = default;
        nan.BiasedExponent = 0x7FF;
        nan.Mantissa = 1;
        double.IsNaN(nan).Should().BeTrue();
        nan.IsNaN.Should().BeTrue();
    }

    [Fact]
    public void Double_ConstructSmallestDenormalized()
    {
        IEEE754Double tiny = default;
        tiny.BiasedExponent = 0;
        tiny.Mantissa = 1;
        double val = tiny;
        val.Should().Be(double.Epsilon);
    }

    #endregion

    #region IEEE754Double: Arithmetic

    [Fact]
    public void Double_Addition()
    {
        IEEE754Double a = 1.5;
        IEEE754Double b = 2.5;
        double result = a + b;
        result.Should().Be(4.0);
    }

    [Fact]
    public void Double_Subtraction()
    {
        IEEE754Double a = 5.0;
        IEEE754Double b = 3.0;
        double result = a - b;
        result.Should().Be(2.0);
    }

    [Fact]
    public void Double_Multiplication()
    {
        IEEE754Double a = 3.0;
        IEEE754Double b = 4.0;
        double result = a * b;
        result.Should().Be(12.0);
    }

    [Fact]
    public void Double_Division()
    {
        IEEE754Double a = 10.0;
        IEEE754Double b = 4.0;
        double result = a / b;
        result.Should().Be(2.5);
    }

    [Fact]
    public void Double_GoldenRatio()
    {
        IEEE754Double one = 1.0;
        IEEE754Double sqrt5 = Math.Sqrt(5.0);
        IEEE754Double two = 2.0;
        IEEE754Double phi = (one + sqrt5) / two;
        ((double)phi).Should().BeApproximately(1.6180339887498949, 1e-15);
    }

    #endregion

    #region IEEE754Double: Parsing and Formatting

    [Fact]
    public void Double_Parse()
    {
        var d = IEEE754Double.Parse("3.14", CultureInfo.InvariantCulture);
        ((double)d).Should().BeApproximately(3.14, 0.001);
    }

    [Fact]
    public void Double_TryParse_Valid()
    {
        IEEE754Double.TryParse("42.5", out var result).Should().BeTrue();
        ((double)result).Should().Be(42.5);
    }

    [Fact]
    public void Double_TryParse_Invalid()
    {
        IEEE754Double.TryParse("not_a_number", out _).Should().BeFalse();
    }

    [Fact]
    public void Double_ToString_Format()
    {
        IEEE754Double d = 3.14159;
        var str = d.ToString("F2", CultureInfo.InvariantCulture);
        str.Should().Be("3.14");
    }

    #endregion

    #region IEEE754Double: Struct Size

    [Fact]
    public void Double_SizeIs8Bytes()
    {
        Unsafe.SizeOf<IEEE754Double>().Should().Be(8);
    }

    #endregion

    #region DecimalBitFields: Round-Trip and Construction

    [Fact]
    public void Decimal_RoundTrips()
    {
        DecimalBitFields d = 3.14m;
        decimal result = d;
        result.Should().Be(3.14m);
    }

    [Fact]
    public void Decimal_Zero_RoundTrips()
    {
        DecimalBitFields d = 0m;
        decimal result = d;
        result.Should().Be(0m);
    }

    [Fact]
    public void Decimal_Negative_RoundTrips()
    {
        DecimalBitFields d = -1.5m;
        decimal result = d;
        result.Should().Be(-1.5m);
        d.Sign.Should().BeTrue();
    }

    [Fact]
    public void Decimal_MaxValue_RoundTrips()
    {
        DecimalBitFields d = decimal.MaxValue;
        decimal result = d;
        result.Should().Be(decimal.MaxValue);
    }

    [Fact]
    public void Decimal_MinValue_RoundTrips()
    {
        DecimalBitFields d = decimal.MinValue;
        decimal result = d;
        result.Should().Be(decimal.MinValue);
    }

    #endregion

    #region DecimalBitFields: Field Decomposition

    [Fact]
    public void Decimal_Sign_Positive()
    {
        DecimalBitFields d = 42m;
        d.Sign.Should().BeFalse();
    }

    [Fact]
    public void Decimal_Sign_FlipNegates()
    {
        DecimalBitFields d = 100m;
        d.Sign = true;
        ((decimal)d).Should().Be(-100m);
    }

    [Fact]
    public void Decimal_Scale_ReflectsDecimalPlaces()
    {
        DecimalBitFields d = 1.23m;
        d.Scale.Should().Be(2);
    }

    [Fact]
    public void Decimal_Coefficient_WholeNumber()
    {
        DecimalBitFields d = 42m;
        d.Coefficient.Should().Be(42);
        d.Scale.Should().Be(0);
    }

    [Fact]
    public void Decimal_Decompose_19_99()
    {
        DecimalBitFields d = 19.99m;
        d.Sign.Should().BeFalse();
        d.Scale.Should().Be(2);
        d.Coefficient.Should().Be(1999);
    }

    #endregion

    #region DecimalBitFields: Constants

    [Fact]
    public void Decimal_MaxScale_Is28()
    {
        DecimalBitFields.MAX_SCALE.Should().Be(28);
    }

    #endregion

    #region DecimalBitFields: Arithmetic

    [Fact]
    public void Decimal_Addition()
    {
        DecimalBitFields a = 1.5m;
        DecimalBitFields b = 2.5m;
        decimal result = a + b;
        result.Should().Be(4.0m);
    }

    [Fact]
    public void Decimal_Subtraction()
    {
        DecimalBitFields a = 10m;
        DecimalBitFields b = 3.5m;
        decimal result = a - b;
        result.Should().Be(6.5m);
    }

    [Fact]
    public void Decimal_Multiplication()
    {
        DecimalBitFields a = 3m;
        DecimalBitFields b = 7m;
        decimal result = a * b;
        result.Should().Be(21m);
    }

    [Fact]
    public void Decimal_Division()
    {
        DecimalBitFields a = 10m;
        DecimalBitFields b = 4m;
        decimal result = a / b;
        result.Should().Be(2.5m);
    }

    [Fact]
    public void Decimal_Negation()
    {
        DecimalBitFields d = 42m;
        decimal result = -d;
        result.Should().Be(-42m);
    }

    #endregion

    #region DecimalBitFields: Comparison

    [Fact]
    public void Decimal_LessThan()
    {
        DecimalBitFields a = 1m;
        DecimalBitFields b = 2m;
        (a < b).Should().BeTrue();
        (b < a).Should().BeFalse();
    }

    [Fact]
    public void Decimal_Equality()
    {
        DecimalBitFields a = 3.14m;
        DecimalBitFields b = 3.14m;
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    #endregion

    #region DecimalBitFields: Parsing and Formatting

    [Fact]
    public void Decimal_Parse()
    {
        var d = DecimalBitFields.Parse("3.14", CultureInfo.InvariantCulture);
        ((decimal)d).Should().Be(3.14m);
    }

    [Fact]
    public void Decimal_TryParse_Valid()
    {
        DecimalBitFields.TryParse("42.5", out var result).Should().BeTrue();
        ((decimal)result).Should().Be(42.5m);
    }

    [Fact]
    public void Decimal_TryParse_Invalid()
    {
        DecimalBitFields.TryParse("not_a_number", out _).Should().BeFalse();
    }

    [Fact]
    public void Decimal_ToString_Format()
    {
        DecimalBitFields d = 3.14159m;
        var str = d.ToString("F2", CultureInfo.InvariantCulture);
        str.Should().Be("3.14");
    }

    #endregion

    #region DecimalBitFields: Struct Size

    [Fact]
    public void Decimal_SizeIs16Bytes()
    {
        Unsafe.SizeOf<DecimalBitFields>().Should().Be(16);
    }

    #endregion
}
