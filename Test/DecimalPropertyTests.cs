using System;
using FluentAssertions;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Tests for <c>decimal</c> used as a *property type* inside [BitFields] structs
/// (multi-word) and record struct views.  The generator treats decimal as an opaque
/// 128-bit blob: raw bits are reinterpreted via Unsafe.As without inspecting sign,
/// scale, or mantissa.
/// </summary>
public partial class DecimalPropertyTests
{
    // ═══════════════════════════════════════════════════════════════
    //  Struct Definitions — Multi-Word (UInt128 storage)
    // ═══════════════════════════════════════════════════════════════

    #region Test Struct Definitions — Multi-Word

    /// <summary>
    /// 128-bit struct backed by UInt128 with a single decimal property
    /// spanning the full width (bits 0-127, word-aligned).
    /// </summary>
    [BitFields(typeof(UInt128))]
    public partial struct FullDecimal128
    {
        [BitField(0, End = 127)] public partial decimal DecimalVal { get; set; }
    }

    /// <summary>
    /// 256-bit multi-word struct with a decimal and integer fields.
    /// The decimal occupies bits 0-127 (word-aligned, words 0-1).
    /// </summary>
    [BitFields(256)]
    public partial struct DecimalWithPad256
    {
        [BitField(0, End = 127)]   public partial decimal DecimalVal { get; set; }
        [BitField(128, End = 191)] public partial ulong HighField { get; set; }
        [BitField(192, End = 255)] public partial ulong TopField { get; set; }
    }

    /// <summary>
    /// 256-bit struct with the decimal NOT at bit 0, but still word-aligned (bit 64).
    /// The decimal spans words 1 and 2.
    /// </summary>
    [BitFields(256)]
    public partial struct DecimalAtWord1
    {
        [BitField(0, End = 63)]    public partial ulong LowField { get; set; }
        [BitField(64, End = 191)]  public partial decimal DecimalVal { get; set; }
        [BitField(192, End = 255)] public partial ulong TopField { get; set; }
    }

    /// <summary>
    /// 256-bit struct with the decimal at a non-word-aligned position (bit 32).
    /// Spans words 0, 1, and 2 with a 32-bit shift.
    /// </summary>
    [BitFields(256)]
    public partial struct DecimalOffset32
    {
        [BitField(0, End = 31)]    public partial uint LowPad { get; set; }
        [BitField(32, End = 159)]  public partial decimal DecimalVal { get; set; }
        [BitField(160, End = 191)] public partial uint MidPad { get; set; }
        [BitField(192, End = 255)] public partial ulong HighPad { get; set; }
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    //  Struct Definitions — Record Struct Views
    // ═══════════════════════════════════════════════════════════════

    #region Test Struct Definitions — Views

    /// <summary>
    /// LE view with a decimal at byte-aligned position (bits 0-127 = bytes 0-15).
    /// </summary>
    [BitFields]
    public partial record struct DecimalView
    {
        [BitField(0, End = 127)]   public partial decimal DecimalVal { get; set; }
        [BitField(128, End = 191)] public partial ulong Tag { get; set; }
    }

    /// <summary>
    /// LE view with a decimal at byte-aligned position bit 8 (byte 1).
    /// The decimal occupies bits 8-135, straddling byte boundaries but starting
    /// on a byte boundary.
    /// </summary>
    [BitFields]
    public partial record struct DecimalViewOffset8
    {
        [BitField(0, End = 7)]     public partial byte LowPad { get; set; }
        [BitField(8, End = 135)]   public partial decimal DecimalVal { get; set; }
        [BitField(136, End = 143)] public partial byte HighPad { get; set; }
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    //  Multi-Word Tests
    // ═══════════════════════════════════════════════════════════════

    #region Multi-Word: FullDecimal128

    [Fact]
    public void FullDecimal128_RoundTrips()
    {
        FullDecimal128 reg = default;
        reg.DecimalVal = 3.14m;
        reg.DecimalVal.Should().Be(3.14m);
    }

    [Fact]
    public void FullDecimal128_Zero()
    {
        FullDecimal128 reg = default;
        reg.DecimalVal = 0m;
        reg.DecimalVal.Should().Be(0m);
    }

    [Fact]
    public void FullDecimal128_One()
    {
        FullDecimal128 reg = default;
        reg.DecimalVal = 1m;
        reg.DecimalVal.Should().Be(1m);
    }

    [Fact]
    public void FullDecimal128_NegativeOne()
    {
        FullDecimal128 reg = default;
        reg.DecimalVal = -1m;
        reg.DecimalVal.Should().Be(-1m);
    }

    [Fact]
    public void FullDecimal128_MaxValue()
    {
        FullDecimal128 reg = default;
        reg.DecimalVal = decimal.MaxValue;
        reg.DecimalVal.Should().Be(decimal.MaxValue);
    }

    [Fact]
    public void FullDecimal128_MinValue()
    {
        FullDecimal128 reg = default;
        reg.DecimalVal = decimal.MinValue;
        reg.DecimalVal.Should().Be(decimal.MinValue);
    }

    [Fact]
    public void FullDecimal128_SmallFraction()
    {
        FullDecimal128 reg = default;
        reg.DecimalVal = 0.0000000000000000000000000001m; // near smallest positive
        reg.DecimalVal.Should().Be(0.0000000000000000000000000001m);
    }

    [Fact]
    public void FullDecimal128_WithDecimalVal()
    {
        var reg = default(FullDecimal128).WithDecimalVal(42.5m);
        reg.DecimalVal.Should().Be(42.5m);
    }

    #endregion

    #region Multi-Word: DecimalWithPad256

    [Fact]
    public void DecimalWithPad256_RoundTrips()
    {
        DecimalWithPad256 mw = default;
        mw.DecimalVal = 123.456m;
        mw.DecimalVal.Should().Be(123.456m);
    }

    [Fact]
    public void DecimalWithPad256_AllFieldsIndependent()
    {
        DecimalWithPad256 mw = default;
        mw.DecimalVal = 99.99m;
        mw.HighField = 0xDEADBEEFCAFEBABE;
        mw.TopField = 0x1234567890ABCDEF;

        mw.DecimalVal.Should().Be(99.99m);
        mw.HighField.Should().Be(0xDEADBEEFCAFEBABE);
        mw.TopField.Should().Be(0x1234567890ABCDEF);
    }

    [Fact]
    public void DecimalWithPad256_IntegerFieldDoesNotCorruptDecimal()
    {
        DecimalWithPad256 mw = default;
        mw.DecimalVal = decimal.MaxValue;
        mw.HighField = ulong.MaxValue;
        mw.DecimalVal.Should().Be(decimal.MaxValue, "setting HighField should not corrupt DecimalVal");
    }

    #endregion

    #region Multi-Word: DecimalAtWord1

    [Fact]
    public void DecimalAtWord1_RoundTrips()
    {
        DecimalAtWord1 mw = default;
        mw.DecimalVal = 7.77m;
        mw.DecimalVal.Should().Be(7.77m);
    }

    [Fact]
    public void DecimalAtWord1_AllFieldsIndependent()
    {
        DecimalAtWord1 mw = default;
        mw.LowField = 0xAAAAAAAAAAAAAAAA;
        mw.DecimalVal = -12345.6789m;
        mw.TopField = 0x5555555555555555;

        mw.LowField.Should().Be(0xAAAAAAAAAAAAAAAA);
        mw.DecimalVal.Should().Be(-12345.6789m);
        mw.TopField.Should().Be(0x5555555555555555);
    }

    [Fact]
    public void DecimalAtWord1_MaxValue()
    {
        DecimalAtWord1 mw = default;
        mw.DecimalVal = decimal.MaxValue;
        mw.DecimalVal.Should().Be(decimal.MaxValue);
    }

    #endregion

    #region Multi-Word: DecimalOffset32

    [Fact]
    public void DecimalOffset32_RoundTrips()
    {
        DecimalOffset32 mw = default;
        mw.DecimalVal = 3.14159m;
        mw.DecimalVal.Should().Be(3.14159m);
    }

    [Fact]
    public void DecimalOffset32_AllFieldsIndependent()
    {
        DecimalOffset32 mw = default;
        mw.LowPad = 0xFFFFFFFF;
        mw.DecimalVal = -0.001m;
        mw.MidPad = 0xBBBBBBBB;
        mw.HighPad = 0xCCCCCCCCCCCCCCCC;

        mw.LowPad.Should().Be(0xFFFFFFFF);
        mw.DecimalVal.Should().Be(-0.001m);
        mw.MidPad.Should().Be(0xBBBBBBBB);
        mw.HighPad.Should().Be(0xCCCCCCCCCCCCCCCC);
    }

    [Fact]
    public void DecimalOffset32_MaxValue()
    {
        DecimalOffset32 mw = default;
        mw.DecimalVal = decimal.MaxValue;
        mw.DecimalVal.Should().Be(decimal.MaxValue);
    }

    [Fact]
    public void DecimalOffset32_MinValue()
    {
        DecimalOffset32 mw = default;
        mw.DecimalVal = decimal.MinValue;
        mw.DecimalVal.Should().Be(decimal.MinValue);
    }

    [Fact]
    public void DecimalOffset32_DoesNotCorruptPadFields()
    {
        DecimalOffset32 mw = default;
        mw.LowPad = 0xDEAD;
        mw.MidPad = 0xCAFE;
        mw.HighPad = 0xBEEF;
        mw.DecimalVal = decimal.MaxValue;

        mw.LowPad.Should().Be(0xDEAD, "setting decimal should not corrupt LowPad");
        mw.MidPad.Should().Be(0xCAFE, "setting decimal should not corrupt MidPad");
        mw.HighPad.Should().Be(0xBEEF, "setting decimal should not corrupt HighPad");
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    //  Record Struct View Tests
    // ═══════════════════════════════════════════════════════════════

    #region View: DecimalView

    [Fact]
    public void DecimalView_RoundTrips()
    {
        byte[] buffer = new byte[DecimalView.SIZE_IN_BYTES];
        var view = new DecimalView(buffer);
        view.DecimalVal = 3.14m;
        view.DecimalVal.Should().Be(3.14m);
    }

    [Fact]
    public void DecimalView_AllFieldsIndependent()
    {
        byte[] buffer = new byte[DecimalView.SIZE_IN_BYTES];
        var view = new DecimalView(buffer);

        view.DecimalVal = 99.99m;
        view.Tag = 0xCAFEBABE;

        view.DecimalVal.Should().Be(99.99m);
        view.Tag.Should().Be(0xCAFEBABE);
    }

    [Fact]
    public void DecimalView_MaxValue()
    {
        byte[] buffer = new byte[DecimalView.SIZE_IN_BYTES];
        var view = new DecimalView(buffer);
        view.DecimalVal = decimal.MaxValue;
        view.DecimalVal.Should().Be(decimal.MaxValue);
    }

    [Fact]
    public void DecimalView_WritesToBuffer()
    {
        byte[] buffer = new byte[DecimalView.SIZE_IN_BYTES];
        var view = new DecimalView(buffer);
        view.DecimalVal = 1.0m;

        // The buffer should have changed from all zeroes
        bool anyNonZero = false;
        for (int i = 0; i < 16; i++)
            if (buffer[i] != 0) { anyNonZero = true; break; }
        anyNonZero.Should().BeTrue("setting decimal should write to the buffer");
    }

    #endregion

    #region View: DecimalViewOffset8

    [Fact]
    public void DecimalViewOffset8_RoundTrips()
    {
        byte[] buffer = new byte[DecimalViewOffset8.SIZE_IN_BYTES];
        var view = new DecimalViewOffset8(buffer);
        view.DecimalVal = 42.5m;
        view.DecimalVal.Should().Be(42.5m);
    }

    [Fact]
    public void DecimalViewOffset8_AllFieldsIndependent()
    {
        byte[] buffer = new byte[DecimalViewOffset8.SIZE_IN_BYTES];
        var view = new DecimalViewOffset8(buffer);

        view.LowPad = 0xFF;
        view.DecimalVal = -999.999m;
        view.HighPad = 0xAB;

        view.LowPad.Should().Be(0xFF);
        view.DecimalVal.Should().Be(-999.999m);
        view.HighPad.Should().Be(0xAB);
    }

    [Fact]
    public void DecimalViewOffset8_MaxValue()
    {
        byte[] buffer = new byte[DecimalViewOffset8.SIZE_IN_BYTES];
        var view = new DecimalViewOffset8(buffer);
        view.DecimalVal = decimal.MaxValue;
        view.DecimalVal.Should().Be(decimal.MaxValue);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    //  Parameterized Sweep Tests
    // ═══════════════════════════════════════════════════════════════

    #region Theory: Decimal Value Sweep

    public static TheoryData<decimal> DecimalTestValues => new()
    {
        0m,
        1m,
        -1m,
        0.5m,
        -0.5m,
        3.14m,
        -3.14m,
        42.0m,
        123456789.123456789m,
        -123456789.123456789m,
        decimal.MaxValue,
        decimal.MinValue,
        0.0000000000000000000000000001m,
        -0.0000000000000000000000000001m,
        7.9228162514264337593543950335m, // MaxValue mantissa (scale 28) — same 96-bit mantissa as decimal.MaxValue, different scale byte
        1.0000000000000000000000000000m, // scale 28
    };

    [Theory]
    [MemberData(nameof(DecimalTestValues))]
    public void FullDecimal128_Sweep(decimal value)
    {
        FullDecimal128 reg = default;
        reg.DecimalVal = value;
        reg.DecimalVal.Should().Be(value);
    }

    [Theory]
    [MemberData(nameof(DecimalTestValues))]
    public void DecimalWithPad256_Sweep(decimal value)
    {
        DecimalWithPad256 mw = default;
        mw.DecimalVal = value;
        mw.DecimalVal.Should().Be(value);
    }

    [Theory]
    [MemberData(nameof(DecimalTestValues))]
    public void DecimalOffset32_Sweep(decimal value)
    {
        DecimalOffset32 mw = default;
        mw.DecimalVal = value;
        mw.DecimalVal.Should().Be(value);
    }

    [Theory]
    [MemberData(nameof(DecimalTestValues))]
    public void DecimalView_Sweep(decimal value)
    {
        byte[] buffer = new byte[DecimalView.SIZE_IN_BYTES];
        var view = new DecimalView(buffer);
        view.DecimalVal = value;
        view.DecimalVal.Should().Be(value);
    }

    [Theory]
    [MemberData(nameof(DecimalTestValues))]
    public void DecimalViewOffset8_Sweep(decimal value)
    {
        byte[] buffer = new byte[DecimalViewOffset8.SIZE_IN_BYTES];
        var view = new DecimalViewOffset8(buffer);
        view.DecimalVal = value;
        view.DecimalVal.Should().Be(value);
    }

    #endregion
}
