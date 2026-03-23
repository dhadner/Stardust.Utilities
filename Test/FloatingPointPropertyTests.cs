using System;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Tests for floating-point types (Half, float, double) used as *property types*
/// inside [BitFields] structs, record struct views, and arbitrary-width multi-word
/// structs.  These tests verify that the generator correctly emits BitConverter
/// wrapping so the raw bit pattern is reinterpreted as the floating-point value.
/// </summary>
public partial class FloatingPointPropertyTests
{
    #region Test Struct Definitions — Value Types

    /// <summary>
    /// 128-bit struct (UInt128) with mixed floating-point property types and padding.
    /// Layout (LSB-first):
    ///   bits   0-63  : double  (64 bits)
    ///   bits  64-95  : float   (32 bits)
    ///   bits  96-111 : Half    (16 bits)
    ///   bits 112-125 : padding (14 bits, unused)
    ///   bit  126     : pad flag (1 bit)
    ///   bit  127     : sign flag (1 bit)
    /// </summary>
    [BitFields(typeof(UInt128))]
    public partial struct MixedFloatRegister128
    {
        [BitField(0, End = 63)] public partial double DoubleVal { get; set; }
        [BitField(64, End = 95)] public partial float FloatVal { get; set; }
        [BitField(96, End = 111)] public partial Half HalfVal { get; set; }
        [BitFlag(126)] public partial bool PadFlag { get; set; }
        [BitFlag(127)] public partial bool SignFlag { get; set; }
    }

    /// <summary>
    /// 64-bit struct (ulong) with a float and a Half side by side.
    /// Layout:
    ///   bits  0-31 : float  (32 bits)
    ///   bits 32-47 : Half   (16 bits)
    ///   bits 48-63 : ushort tag
    /// </summary>
    [BitFields(typeof(ulong))]
    public partial struct FloatHalfPair64
    {
        [BitField(0, End = 31)] public partial float FloatVal { get; set; }
        [BitField(32, End = 47)] public partial Half HalfVal { get; set; }
        [BitField(48, End = 63)] public partial ushort Tag { get; set; }
    }

    /// <summary>
    /// Double-backed struct where the 64 bits are viewed as a double property.
    /// This is the identity case — the entire storage is one double property.
    /// </summary>
    [BitFields(typeof(ulong))]
    public partial struct FullDouble64
    {
        [BitField(0, End = 63)] public partial double DoubleVal { get; set; }
    }

    /// <summary>
    /// Float-backed struct for a 32-bit storage with a float property.
    /// </summary>
    [BitFields(typeof(uint))]
    public partial struct FullFloat32
    {
        [BitField(0, End = 31)] public partial float FloatVal { get; set; }
    }

    /// <summary>
    /// Half-backed struct for a 16-bit storage with a Half property.
    /// </summary>
    [BitFields(typeof(ushort))]
    public partial struct FullHalf16
    {
        [BitField(0, End = 15)] public partial Half HalfVal { get; set; }
    }

    #endregion

    #region Test Struct Definitions — Multi-Word (Arbitrary Width)

    /// <summary>
    /// 128-bit arbitrary-width struct with a Half, float, and double packed together.
    /// Layout:
    ///   bits   0-15  : Half   (16 bits)
    ///   bits  16-47  : float  (32 bits)
    ///   bits  48-63  : pad    (16 bits)
    ///   bits  64-127 : double (64 bits)
    /// </summary>
    [BitFields(128)]
    public partial struct MixedFloatMultiWord128
    {
        [BitField(0, End = 15)] public partial Half HalfVal { get; set; }
        [BitField(16, End = 47)] public partial float FloatVal { get; set; }
        [BitField(48, End = 63)] public partial ushort Pad { get; set; }
        [BitField(64, End = 127)] public partial double DoubleVal { get; set; }
    }

    #endregion

    #region Test Struct Definitions — Record Struct Views

    /// <summary>
    /// Little-endian view with a Half, float, and double at byte-aligned positions.
    /// Layout:
    ///   bits   0-15  : Half   (bytes 0-1)
    ///   bits  16-47  : float  (bytes 2-5)
    ///   bits  48-63  : pad    (bytes 6-7)
    ///   bits  64-127 : double (bytes 8-15)
    /// </summary>
    [BitFields]
    public partial record struct MixedFloatView
    {
        [BitField(0, End = 15)] public partial Half HalfVal { get; set; }
        [BitField(16, End = 47)] public partial float FloatVal { get; set; }
        [BitField(48, End = 63)] public partial ushort Pad { get; set; }
        [BitField(64, End = 127)] public partial double DoubleVal { get; set; }
    }

    #endregion

    #region Value Type: Full-Width Round-Trip Tests

    [Fact]
    public void FullDouble64_RoundTrips()
    {
        FullDouble64 reg = default;
        reg.DoubleVal = Math.PI;
        reg.DoubleVal.Should().Be(Math.PI);
    }

    [Fact]
    public void FullFloat32_RoundTrips()
    {
        FullFloat32 reg = default;
        reg.FloatVal = 3.14f;
        reg.FloatVal.Should().Be(3.14f);
    }

    [Fact]
    public void FullHalf16_RoundTrips()
    {
        FullHalf16 reg = default;
        reg.HalfVal = (Half)1.5;
        reg.HalfVal.Should().Be((Half)1.5);
    }

    [Fact]
    public void FullDouble64_NegativeInfinity()
    {
        FullDouble64 reg = default;
        reg.DoubleVal = double.NegativeInfinity;
        reg.DoubleVal.Should().Be(double.NegativeInfinity);
    }

    [Fact]
    public void FullFloat32_NaN()
    {
        FullFloat32 reg = default;
        reg.FloatVal = float.NaN;
        float.IsNaN(reg.FloatVal).Should().BeTrue();
    }

    [Fact]
    public void FullHalf16_Zero()
    {
        FullHalf16 reg = default;
        reg.HalfVal = (Half)0.0;
        reg.HalfVal.Should().Be((Half)0.0);
    }

    [Fact]
    public void FullHalf16_NegativeZero()
    {
        FullHalf16 reg = default;
        reg.HalfVal = (Half)(-0.0);
        // Negative zero has the sign bit set
        Half.IsNegative(reg.HalfVal).Should().BeTrue();
    }

    #endregion

    #region Value Type: Mixed Struct (UInt128) Tests

    [Fact]
    public void MixedFloatRegister128_Double_RoundTrips()
    {
        MixedFloatRegister128 reg = default;
        reg.DoubleVal = Math.E;
        reg.DoubleVal.Should().Be(Math.E);
    }

    [Fact]
    public void MixedFloatRegister128_Float_RoundTrips()
    {
        MixedFloatRegister128 reg = default;
        reg.FloatVal = 2.5f;
        reg.FloatVal.Should().Be(2.5f);
    }

    [Fact]
    public void MixedFloatRegister128_Half_RoundTrips()
    {
        MixedFloatRegister128 reg = default;
        reg.HalfVal = (Half)3.0;
        reg.HalfVal.Should().Be((Half)3.0);
    }

    [Fact]
    public void MixedFloatRegister128_AllFieldsIndependent()
    {
        MixedFloatRegister128 reg = default;

        // Set all fields
        reg.DoubleVal = Math.PI;
        reg.FloatVal = 1.5f;
        reg.HalfVal = (Half)2.0;
        reg.PadFlag = true;
        reg.SignFlag = false;

        // Verify each is independently stored
        reg.DoubleVal.Should().Be(Math.PI);
        reg.FloatVal.Should().Be(1.5f);
        reg.HalfVal.Should().Be((Half)2.0);
        reg.PadFlag.Should().BeTrue();
        reg.SignFlag.Should().BeFalse();
    }

    [Fact]
    public void MixedFloatRegister128_DoubleDoesNotCorruptFloat()
    {
        MixedFloatRegister128 reg = default;
        reg.FloatVal = 42.0f;
        reg.DoubleVal = double.MaxValue;
        reg.FloatVal.Should().Be(42.0f, "setting double should not corrupt float");
    }

    [Fact]
    public void MixedFloatRegister128_FloatDoesNotCorruptHalf()
    {
        MixedFloatRegister128 reg = default;
        reg.HalfVal = (Half)1.0;
        reg.FloatVal = float.MinValue;
        reg.HalfVal.Should().Be((Half)1.0, "setting float should not corrupt Half");
    }

    [Fact]
    public void MixedFloatRegister128_BitPattern_Double()
    {
        // Verify the raw bit pattern: Math.PI stored in bits 0-63
        ulong piBits = BitConverter.DoubleToUInt64Bits(Math.PI);

        MixedFloatRegister128 reg = default;
        reg.DoubleVal = Math.PI;

        // Extract the low 64 bits from the UInt128 storage via ToByteArray
        byte[] bytes = reg.ToByteArray();
        ulong lowBits = BitConverter.ToUInt64(bytes, 0);
        lowBits.Should().Be(piBits);
    }

    [Fact]
    public void MixedFloatRegister128_BitPattern_Float()
    {
        uint expectedBits = BitConverter.SingleToUInt32Bits(2.5f);

        MixedFloatRegister128 reg = default;
        reg.FloatVal = 2.5f;

        byte[] bytes = reg.ToByteArray();
        uint floatBits = BitConverter.ToUInt32(bytes, 8);
        floatBits.Should().Be(expectedBits);
    }

    [Fact]
    public void MixedFloatRegister128_BitPattern_Half()
    {
        ushort expectedBits = BitConverter.HalfToUInt16Bits((Half)3.0);

        MixedFloatRegister128 reg = default;
        reg.HalfVal = (Half)3.0;

        byte[] bytes = reg.ToByteArray();
        ushort halfBits = BitConverter.ToUInt16(bytes, 12);
        halfBits.Should().Be(expectedBits);
    }

    #endregion

    #region Value Type: 64-bit Pair Tests

    [Fact]
    public void FloatHalfPair64_IndependentFields()
    {
        FloatHalfPair64 reg = default;
        reg.FloatVal = 1.25f;
        reg.HalfVal = (Half)0.5;
        reg.Tag = 0xBEEF;

        reg.FloatVal.Should().Be(1.25f);
        reg.HalfVal.Should().Be((Half)0.5);
        reg.Tag.Should().Be(0xBEEF);
    }

    [Fact]
    public void FloatHalfPair64_SpecialValues()
    {
        FloatHalfPair64 reg = default;
        reg.FloatVal = float.PositiveInfinity;
        reg.HalfVal = Half.NaN;

        float.IsPositiveInfinity(reg.FloatVal).Should().BeTrue();
        Half.IsNaN(reg.HalfVal).Should().BeTrue();
    }

    #endregion

    #region Multi-Word: Round-Trip Tests

    [Fact]
    public void MixedFloatMultiWord128_Half_RoundTrips()
    {
        MixedFloatMultiWord128 mw = default;
        mw.HalfVal = (Half)2.5;
        mw.HalfVal.Should().Be((Half)2.5);
    }

    [Fact]
    public void MixedFloatMultiWord128_Float_RoundTrips()
    {
        MixedFloatMultiWord128 mw = default;
        mw.FloatVal = 6.28f;
        mw.FloatVal.Should().Be(6.28f);
    }

    [Fact]
    public void MixedFloatMultiWord128_Double_RoundTrips()
    {
        MixedFloatMultiWord128 mw = default;
        mw.DoubleVal = Math.Tau;
        mw.DoubleVal.Should().Be(Math.Tau);
    }

    [Fact]
    public void MixedFloatMultiWord128_AllFieldsIndependent()
    {
        MixedFloatMultiWord128 mw = default;
        mw.HalfVal = (Half)1.0;
        mw.FloatVal = 2.0f;
        mw.Pad = 0x1234;
        mw.DoubleVal = 3.0;

        mw.HalfVal.Should().Be((Half)1.0);
        mw.FloatVal.Should().Be(2.0f);
        mw.Pad.Should().Be(0x1234);
        mw.DoubleVal.Should().Be(3.0);
    }

    [Fact]
    public void MixedFloatMultiWord128_DoubleDoesNotCorruptLowerFields()
    {
        MixedFloatMultiWord128 mw = default;
        mw.HalfVal = (Half)1.5;
        mw.FloatVal = 42.0f;
        mw.DoubleVal = double.Epsilon;

        mw.HalfVal.Should().Be((Half)1.5, "setting double should not corrupt Half");
        mw.FloatVal.Should().Be(42.0f, "setting double should not corrupt float");
    }

    #endregion

    #region Record Struct View: Round-Trip Tests

    [Fact]
    public void MixedFloatView_Half_RoundTrips()
    {
        byte[] buffer = new byte[MixedFloatView.SIZE_IN_BYTES];
        var view = new MixedFloatView(buffer);

        view.HalfVal = (Half)2.5;
        view.HalfVal.Should().Be((Half)2.5);
    }

    [Fact]
    public void MixedFloatView_Float_RoundTrips()
    {
        byte[] buffer = new byte[MixedFloatView.SIZE_IN_BYTES];
        var view = new MixedFloatView(buffer);

        view.FloatVal = 3.14f;
        view.FloatVal.Should().Be(3.14f);
    }

    [Fact]
    public void MixedFloatView_Double_RoundTrips()
    {
        byte[] buffer = new byte[MixedFloatView.SIZE_IN_BYTES];
        var view = new MixedFloatView(buffer);

        view.DoubleVal = Math.PI;
        view.DoubleVal.Should().Be(Math.PI);
    }

    [Fact]
    public void MixedFloatView_AllFieldsIndependent()
    {
        byte[] buffer = new byte[MixedFloatView.SIZE_IN_BYTES];
        var view = new MixedFloatView(buffer);

        view.HalfVal = (Half)1.0;
        view.FloatVal = 2.0f;
        view.Pad = 0xABCD;
        view.DoubleVal = 3.0;

        view.HalfVal.Should().Be((Half)1.0);
        view.FloatVal.Should().Be(2.0f);
        view.Pad.Should().Be(0xABCD);
        view.DoubleVal.Should().Be(3.0);
    }

    [Fact]
    public void MixedFloatView_WritesToUnderlyingBuffer()
    {
        byte[] buffer = new byte[MixedFloatView.SIZE_IN_BYTES];
        var view = new MixedFloatView(buffer);

        view.HalfVal = (Half)1.5;

        // Verify the raw bytes in the buffer match the Half bit pattern
        ushort expected = BitConverter.HalfToUInt16Bits((Half)1.5);
        ushort actual = BitConverter.ToUInt16(buffer, 0);
        actual.Should().Be(expected);
    }

    [Fact]
    public void MixedFloatView_ReadsFromPrefilledBuffer()
    {
        byte[] buffer = new byte[MixedFloatView.SIZE_IN_BYTES];

        // Write a float bit pattern directly into the buffer at bytes 2-5 (bits 16-47)
        float expected = 42.0f;
        BitConverter.TryWriteBytes(buffer.AsSpan(2), expected);

        var view = new MixedFloatView(buffer);
        view.FloatVal.Should().Be(expected);
    }

    [Fact]
    public void MixedFloatView_DoubleWriteReadsBack()
    {
        byte[] buffer = new byte[MixedFloatView.SIZE_IN_BYTES];

        // Write a double directly at byte offset 8 (bits 64-127)
        double expected = Math.E;
        BitConverter.TryWriteBytes(buffer.AsSpan(8), expected);

        var view = new MixedFloatView(buffer);
        view.DoubleVal.Should().Be(expected);
    }

    [Fact]
    public void MixedFloatView_SpecialValues()
    {
        byte[] buffer = new byte[MixedFloatView.SIZE_IN_BYTES];
        var view = new MixedFloatView(buffer);

        view.HalfVal = Half.PositiveInfinity;
        view.FloatVal = float.NegativeInfinity;
        view.DoubleVal = double.NaN;

        view.HalfVal.Should().Be(Half.PositiveInfinity);
        float.IsNegativeInfinity(view.FloatVal).Should().BeTrue();
        double.IsNaN(view.DoubleVal).Should().BeTrue();
    }

    [Fact]
    public void MixedFloatView_ZeroBuffer_ReturnsZeroes()
    {
        byte[] buffer = new byte[MixedFloatView.SIZE_IN_BYTES];
        var view = new MixedFloatView(buffer);

        view.HalfVal.Should().Be((Half)0.0);
        view.FloatVal.Should().Be(0.0f);
        view.DoubleVal.Should().Be(0.0);
    }

    #endregion

    #region Fluent API Tests

    [Fact]
    public void FullDouble64_WithDoubleVal()
    {
        var reg = default(FullDouble64).WithDoubleVal(Math.PI);
        reg.DoubleVal.Should().Be(Math.PI);
    }

    [Fact]
    public void FullFloat32_WithFloatVal()
    {
        var reg = default(FullFloat32).WithFloatVal(2.5f);
        reg.FloatVal.Should().Be(2.5f);
    }

    [Fact]
    public void FullHalf16_WithHalfVal()
    {
        var reg = default(FullHalf16).WithHalfVal((Half)1.5);
        reg.HalfVal.Should().Be((Half)1.5);
    }

    [Fact]
    public void MixedFloatRegister128_FluentBuild()
    {
        var reg = default(MixedFloatRegister128)
            .WithDoubleVal(Math.PI)
            .WithFloatVal(2.5f)
            .WithHalfVal((Half)1.0)
            .WithSignFlag(true);

        reg.DoubleVal.Should().Be(Math.PI);
        reg.FloatVal.Should().Be(2.5f);
        reg.HalfVal.Should().Be((Half)1.0);
        reg.SignFlag.Should().BeTrue();
    }

    #endregion
}
