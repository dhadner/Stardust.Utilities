using System;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Tests for floating-point property types (Half, float, double) at non-byte-aligned
/// bit positions.  The byte-aligned tests live in <see cref="FloatingPointPropertyTests"/>;
/// this file exercises every interesting alignment: odd starts (1, 3, 5, 7), even
/// non-byte-aligned starts (2, 4, 6, 10, 14...), and cross-word-boundary splits.
/// Covers value types, multi-word structs, and record struct views (LE and BE).
/// Decimal property type tests (byte-aligned and non-word-aligned) are in
/// <see cref="DecimalPropertyTests"/>.
/// </summary>
public partial class FloatingPointPropertyNonAlignedTests
{
    // ═══════════════════════════════════════════════════════════════
    //  Struct Definitions — Value Types
    // ═══════════════════════════════════════════════════════════════

    #region Test Struct Definitions — Value Types (Non-Aligned)

    /// <summary>
    /// User's suggested layout: 1-bit MSB pad, float, 2-bit pad, Half, pad rest.
    /// Layout (LSB-first):
    ///   bits  0-12 : LowPad   (13 bits)
    ///   bits 13-28 : HalfVal  (16 bits at bit 13 — odd)
    ///   bits 29-30 : MidPad   (2 bits)
    ///   bits 31-62 : FloatVal (32 bits at bit 31 — odd)
    ///   bit  63    : TopFlag  (1 bit)
    /// </summary>
    [BitFields(typeof(ulong))]
    public partial struct OddStart64
    {
        [BitField(0, End = 12)]  public partial ushort LowPad { get; set; }
        [BitField(13, End = 28)] public partial Half HalfVal { get; set; }
        [BitField(29, End = 30)] public partial byte MidPad { get; set; }
        [BitField(31, End = 62)] public partial float FloatVal { get; set; }
        [BitFlag(63)]            public partial bool TopFlag { get; set; }
    }

    /// <summary>
    /// Even-but-not-byte-aligned positions: starts at bits 2, 20, 54.
    /// Layout (ulong, 64 bits):
    ///   bits  0-1  : LowPad   (2 bits)
    ///   bits  2-17 : HalfVal  (16 bits at bit 2 — even, not byte-aligned)
    ///   bits 18-19 : MidPad   (2 bits)
    ///   bits 20-51 : FloatVal (32 bits at bit 20 — even, not byte-aligned)
    ///   bits 52-63 : HighPad  (12 bits)
    /// </summary>
    [BitFields(typeof(ulong))]
    public partial struct EvenNonByte64
    {
        [BitField(0, End = 1)]   public partial byte LowPad { get; set; }
        [BitField(2, End = 17)]  public partial Half HalfVal { get; set; }
        [BitField(18, End = 19)] public partial byte MidPad { get; set; }
        [BitField(20, End = 51)] public partial float FloatVal { get; set; }
        [BitField(52, End = 63)] public partial ushort HighPad { get; set; }
    }

    /// <summary>
    /// All three float types at odd bit positions in a UInt128.
    /// The double at bits 57-120 also crosses the internal word boundary.
    /// Layout:
    ///   bits   0-2   : LowPad   (3 bits)
    ///   bits   3-18  : HalfVal  (16 bits at bit 3 — odd)
    ///   bit    19    : MidFlag
    ///   bits  20-51  : FloatVal (32 bits at bit 20 — even, non-byte)
    ///   bits  52-56  : MidPad   (5 bits)
    ///   bits  57-120 : DoubleVal(64 bits at bit 57 — odd, crosses word boundary)
    ///   bits 121-126 : HighPad  (6 bits)
    ///   bit   127    : TopFlag
    /// </summary>
    [BitFields(typeof(UInt128))]
    public partial struct OddTriple128
    {
        [BitField(0, End = 2)]     public partial byte LowPad { get; set; }
        [BitField(3, End = 18)]    public partial Half HalfVal { get; set; }
        [BitFlag(19)]              public partial bool MidFlag { get; set; }
        [BitField(20, End = 51)]   public partial float FloatVal { get; set; }
        [BitField(52, End = 56)]   public partial byte MidPad { get; set; }
        [BitField(57, End = 120)]  public partial double DoubleVal { get; set; }
        [BitField(121, End = 126)] public partial byte HighPad { get; set; }
        [BitFlag(127)]             public partial bool TopFlag { get; set; }
    }

    /// <summary>
    /// Even non-byte-aligned positions in a UInt128: 4, 22, 58.
    /// Layout:
    ///   bits   0-3   : LowPad   (4 bits)
    ///   bits   4-19  : HalfVal  (16 bits at bit 4 — nibble boundary)
    ///   bits  20-21  : MidPad1  (2 bits)
    ///   bits  22-53  : FloatVal (32 bits at bit 22 — even, not byte)
    ///   bits  54-57  : MidPad2  (4 bits)
    ///   bits  58-121 : DoubleVal(64 bits at bit 58 — even, not byte, crosses word)
    ///   bits 122-127 : HighPad  (6 bits)
    /// </summary>
    [BitFields(typeof(UInt128))]
    public partial struct EvenNonByteTriple128
    {
        [BitField(0, End = 3)]     public partial byte LowPad { get; set; }
        [BitField(4, End = 19)]    public partial Half HalfVal { get; set; }
        [BitField(20, End = 21)]   public partial byte MidPad1 { get; set; }
        [BitField(22, End = 53)]   public partial float FloatVal { get; set; }
        [BitField(54, End = 57)]   public partial byte MidPad2 { get; set; }
        [BitField(58, End = 121)]  public partial double DoubleVal { get; set; }
        [BitField(122, End = 127)] public partial byte HighPad { get; set; }
    }

    /// <summary>
    /// Float at bit 6 (even, not byte-aligned), Half at bit 38 (even, not byte-aligned).
    /// </summary>
    [BitFields(typeof(ulong))]
    public partial struct SixBitOffset64
    {
        [BitField(0, End = 5)]   public partial byte LowPad { get; set; }
        [BitField(6, End = 37)]  public partial float FloatVal { get; set; }
        [BitField(38, End = 53)] public partial Half HalfVal { get; set; }
        [BitField(54, End = 63)] public partial ushort HighPad { get; set; }
    }

    /// <summary>
    /// Half at the very last nibble boundary: bit 48 is byte-aligned,
    /// but bit 44 is even-not-byte.  Float at bit 10 (even, not byte).
    /// </summary>
    [BitFields(typeof(ulong))]
    public partial struct TenBitOffset64
    {
        [BitField(0, End = 9)]   public partial ushort LowPad { get; set; }
        [BitField(10, End = 41)] public partial float FloatVal { get; set; }
        [BitField(42, End = 43)] public partial byte MidPad { get; set; }
        [BitField(44, End = 59)] public partial Half HalfVal { get; set; }
        [BitField(60, End = 63)] public partial byte HighPad { get; set; }
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    //  Struct Definitions — Multi-Word (Cross-Word Boundary)
    // ═══════════════════════════════════════════════════════════════

    #region Test Struct Definitions — Multi-Word Cross-Word Boundary

    /// <summary>
    /// Double crossing the 64-bit word boundary at bit 3 (61 bits in word 0, 3 in word 1).
    /// </summary>
    [BitFields(128)]
    public partial struct CrossWordDouble128
    {
        [BitField(0, End = 2)]     public partial byte LowPad { get; set; }
        [BitField(3, End = 66)]    public partial double DoubleVal { get; set; }
        [BitFlag(67)]              public partial bool MidFlag { get; set; }
        [BitField(68, End = 99)]   public partial float FloatVal { get; set; }
        [BitField(100, End = 115)] public partial Half HalfVal { get; set; }
        [BitField(116, End = 127)] public partial ushort HighPad { get; set; }
    }

    /// <summary>
    /// Float crossing the 64-bit word boundary at bit 50 (14 bits in word 0, 18 in word 1).
    /// </summary>
    [BitFields(128)]
    public partial struct CrossWordFloat128
    {
        [BitField(0, End = 49)]   public partial ulong LowField { get; set; }
        [BitField(50, End = 81)]  public partial float FloatVal { get; set; }
        [BitField(82, End = 97)]  public partial Half HalfVal { get; set; }
        [BitField(98, End = 126)] public partial uint HighPad { get; set; }
        [BitFlag(127)]            public partial bool EndFlag { get; set; }
    }

    /// <summary>
    /// Half crossing the 64-bit word boundary at bit 55 (9 bits in word 0, 7 in word 1).
    /// </summary>
    [BitFields(128)]
    public partial struct CrossWordHalf128
    {
        [BitField(0, End = 54)]    public partial ulong LowField { get; set; }
        [BitField(55, End = 70)]   public partial Half HalfVal { get; set; }
        [BitField(71, End = 102)]  public partial float FloatVal { get; set; }
        [BitField(103, End = 127)] public partial uint HighPad { get; set; }
    }

    /// <summary>
    /// Float crossing the word boundary by exactly 1 bit (31 bits in word 0, 1 in word 1).
    /// Most extreme cross-word split possible for a 32-bit field.
    /// </summary>
    [BitFields(128)]
    public partial struct EdgeCrossFloat128
    {
        [BitField(0, End = 32)]    public partial ulong LowField { get; set; }
        [BitField(33, End = 64)]   public partial float FloatVal { get; set; }
        [BitField(65, End = 80)]   public partial Half HalfVal { get; set; }
        [BitField(81, End = 127)]  public partial ulong HighPad { get; set; }
    }

    /// <summary>
    /// Double at even-not-byte position crossing word boundary.
    /// Double at bit 6: 58 bits in word 0, 6 bits in word 1.
    /// </summary>
    [BitFields(128)]
    public partial struct CrossWordDoubleEven128
    {
        [BitField(0, End = 5)]     public partial byte LowPad { get; set; }
        [BitField(6, End = 69)]    public partial double DoubleVal { get; set; }
        [BitField(70, End = 85)]   public partial Half HalfVal { get; set; }
        [BitField(86, End = 117)]  public partial float FloatVal { get; set; }
        [BitField(118, End = 127)] public partial ushort HighPad { get; set; }
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    //  Struct Definitions — Record Struct Views
    // ═══════════════════════════════════════════════════════════════

    #region Test Struct Definitions — Views (Non-Aligned)

    /// <summary>
    /// LE view with all three float types at odd bit positions.
    /// 128 bits = 16 bytes.
    /// </summary>
    [BitFields]
    public partial record struct OddAlignedView
    {
        [BitField(0, End = 2)]     public partial byte LowPad { get; set; }
        [BitField(3, End = 18)]    public partial Half HalfVal { get; set; }
        [BitField(19, End = 20)]   public partial byte MidPad1 { get; set; }
        [BitField(21, End = 52)]   public partial float FloatVal { get; set; }
        [BitField(53, End = 57)]   public partial byte MidPad2 { get; set; }
        [BitField(58, End = 121)]  public partial double DoubleVal { get; set; }
        [BitField(122, End = 127)] public partial byte TopPad { get; set; }
    }

    /// <summary>
    /// LE view with all three float types at even-but-not-byte-aligned positions.
    /// 128 bits = 16 bytes.
    /// </summary>
    [BitFields]
    public partial record struct EvenNonByteView
    {
        [BitField(0, End = 3)]     public partial byte LowPad { get; set; }
        [BitField(4, End = 19)]    public partial Half HalfVal { get; set; }
        [BitField(20, End = 21)]   public partial byte MidPad1 { get; set; }
        [BitField(22, End = 53)]   public partial float FloatVal { get; set; }
        [BitField(54, End = 57)]   public partial byte MidPad2 { get; set; }
        [BitField(58, End = 121)]  public partial double DoubleVal { get; set; }
        [BitField(122, End = 127)] public partial byte TopPad { get; set; }
    }

    /// <summary>
    /// LE view with everything shifted by exactly 1 bit (minimal odd alignment).
    /// 64 bits = 8 bytes.
    /// </summary>
    [BitFields]
    public partial record struct SingleBitOffsetView
    {
        [BitFlag(0)]              public partial bool StartFlag { get; set; }
        [BitField(1, End = 16)]   public partial Half HalfVal { get; set; }
        [BitField(17, End = 48)]  public partial float FloatVal { get; set; }
        [BitField(49, End = 63)]  public partial ushort TopPad { get; set; }
    }

    /// <summary>
    /// BE MSB-first view with float and Half at odd positions.
    /// 64 bits = 8 bytes.
    /// </summary>
    [BitFields(ByteOrder.BigEndian, BitOrder.BitZeroIsMsb)]
    public partial record struct BigEndianOddView
    {
        [BitFlag(0)]              public partial bool StartFlag { get; set; }
        [BitField(1, End = 16)]   public partial Half HalfVal { get; set; }
        [BitField(17, End = 18)]  public partial byte MidPad { get; set; }
        [BitField(19, End = 50)]  public partial float FloatVal { get; set; }
        [BitField(51, End = 63)]  public partial ushort LowPad { get; set; }
    }

    /// <summary>
    /// LE view with float at bit 6 and Half at bit 38 — even, not byte-aligned.
    /// 64 bits = 8 bytes.
    /// </summary>
    [BitFields]
    public partial record struct SixBitOffsetView
    {
        [BitField(0, End = 5)]   public partial byte LowPad { get; set; }
        [BitField(6, End = 37)]  public partial float FloatVal { get; set; }
        [BitField(38, End = 53)] public partial Half HalfVal { get; set; }
        [BitField(54, End = 63)] public partial ushort HighPad { get; set; }
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    //  Value Type Tests — Odd Starts (1, 3, 13, 31, 57)
    // ═══════════════════════════════════════════════════════════════

    #region Value Type: OddStart64

    [Fact]
    public void OddStart64_Half_RoundTrips()
    {
        OddStart64 reg = default;
        reg.HalfVal = (Half)1.5;
        reg.HalfVal.Should().Be((Half)1.5);
    }

    [Fact]
    public void OddStart64_Float_RoundTrips()
    {
        OddStart64 reg = default;
        reg.FloatVal = 3.14f;
        reg.FloatVal.Should().Be(3.14f);
    }

    [Fact]
    public void OddStart64_AllFieldsIndependent()
    {
        OddStart64 reg = default;
        reg.LowPad = 0x1FFF;
        reg.HalfVal = (Half)2.0;
        reg.MidPad = 3;
        reg.FloatVal = 42.0f;
        reg.TopFlag = true;

        reg.LowPad.Should().Be(0x1FFF);
        reg.HalfVal.Should().Be((Half)2.0);
        reg.MidPad.Should().Be(3);
        reg.FloatVal.Should().Be(42.0f);
        reg.TopFlag.Should().BeTrue();
    }

    [Fact]
    public void OddStart64_FloatDoesNotCorruptHalf()
    {
        OddStart64 reg = default;
        reg.HalfVal = (Half)1.0;
        reg.FloatVal = float.MaxValue;
        reg.HalfVal.Should().Be((Half)1.0, "setting float should not corrupt Half");
    }

    [Fact]
    public void OddStart64_SpecialValues()
    {
        OddStart64 reg = default;
        reg.FloatVal = float.PositiveInfinity;
        reg.HalfVal = Half.NegativeInfinity;

        float.IsPositiveInfinity(reg.FloatVal).Should().BeTrue();
        Half.IsNegativeInfinity(reg.HalfVal).Should().BeTrue();
    }

    [Fact]
    public void OddStart64_NaN()
    {
        OddStart64 reg = default;
        reg.FloatVal = float.NaN;
        reg.HalfVal = Half.NaN;

        float.IsNaN(reg.FloatVal).Should().BeTrue();
        Half.IsNaN(reg.HalfVal).Should().BeTrue();
    }

    [Fact]
    public void OddStart64_NegativeZero()
    {
        OddStart64 reg = default;
        reg.FloatVal = -0.0f;
        reg.HalfVal = (Half)(-0.0);

        float.IsNegative(reg.FloatVal).Should().BeTrue();
        Half.IsNegative(reg.HalfVal).Should().BeTrue();
    }

    [Fact]
    public void OddStart64_BitPattern_Float()
    {
        uint expectedBits = BitConverter.SingleToUInt32Bits(2.5f);
        OddStart64 reg = default;
        reg.FloatVal = 2.5f;

        // FloatVal occupies bits 31-62 of the ulong
        ulong raw = reg;
        uint extractedBits = (uint)((raw >> 31) & 0xFFFFFFFF);
        extractedBits.Should().Be(expectedBits);
    }

    [Fact]
    public void OddStart64_BitPattern_Half()
    {
        ushort expectedBits = BitConverter.HalfToUInt16Bits((Half)3.0);
        OddStart64 reg = default;
        reg.HalfVal = (Half)3.0;

        // HalfVal occupies bits 13-28 of the ulong
        ulong raw = reg;
        ushort extractedBits = (ushort)((raw >> 13) & 0xFFFF);
        extractedBits.Should().Be(expectedBits);
    }

    [Fact]
    public void OddStart64_Fluent()
    {
        var reg = default(OddStart64)
            .WithHalfVal((Half)1.5)
            .WithFloatVal(42.0f)
            .WithTopFlag(true);

        reg.HalfVal.Should().Be((Half)1.5);
        reg.FloatVal.Should().Be(42.0f);
        reg.TopFlag.Should().BeTrue();
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    //  Value Type Tests — Even Non-Byte (2, 4, 6, 10, 20, 22, 38, 44, 58)
    // ═══════════════════════════════════════════════════════════════

    #region Value Type: EvenNonByte64

    [Fact]
    public void EvenNonByte64_Half_RoundTrips()
    {
        EvenNonByte64 reg = default;
        reg.HalfVal = (Half)2.5;
        reg.HalfVal.Should().Be((Half)2.5);
    }

    [Fact]
    public void EvenNonByte64_Float_RoundTrips()
    {
        EvenNonByte64 reg = default;
        reg.FloatVal = 6.28f;
        reg.FloatVal.Should().Be(6.28f);
    }

    [Fact]
    public void EvenNonByte64_AllFieldsIndependent()
    {
        EvenNonByte64 reg = default;
        reg.LowPad = 3;
        reg.HalfVal = (Half)1.0;
        reg.MidPad = 3;
        reg.FloatVal = 2.0f;
        reg.HighPad = 0xFFF;

        reg.LowPad.Should().Be(3);
        reg.HalfVal.Should().Be((Half)1.0);
        reg.MidPad.Should().Be(3);
        reg.FloatVal.Should().Be(2.0f);
        reg.HighPad.Should().Be(0xFFF);
    }

    [Fact]
    public void EvenNonByte64_BitPattern_Float()
    {
        uint expectedBits = BitConverter.SingleToUInt32Bits(1.25f);
        EvenNonByte64 reg = default;
        reg.FloatVal = 1.25f;

        ulong raw = reg;
        uint extractedBits = (uint)((raw >> 20) & 0xFFFFFFFF);
        extractedBits.Should().Be(expectedBits);
    }

    [Fact]
    public void EvenNonByte64_BitPattern_Half()
    {
        ushort expectedBits = BitConverter.HalfToUInt16Bits((Half)0.5);
        EvenNonByte64 reg = default;
        reg.HalfVal = (Half)0.5;

        ulong raw = reg;
        ushort extractedBits = (ushort)((raw >> 2) & 0xFFFF);
        extractedBits.Should().Be(expectedBits);
    }

    #endregion

    #region Value Type: SixBitOffset64

    [Fact]
    public void SixBitOffset64_Float_RoundTrips()
    {
        SixBitOffset64 reg = default;
        reg.FloatVal = 3.14f;
        reg.FloatVal.Should().Be(3.14f);
    }

    [Fact]
    public void SixBitOffset64_Half_RoundTrips()
    {
        SixBitOffset64 reg = default;
        reg.HalfVal = (Half)2.5;
        reg.HalfVal.Should().Be((Half)2.5);
    }

    [Fact]
    public void SixBitOffset64_AllFieldsIndependent()
    {
        SixBitOffset64 reg = default;
        reg.LowPad = 0x3F;
        reg.FloatVal = 42.0f;
        reg.HalfVal = (Half)1.0;
        reg.HighPad = 0x3FF;

        reg.LowPad.Should().Be(0x3F);
        reg.FloatVal.Should().Be(42.0f);
        reg.HalfVal.Should().Be((Half)1.0);
        reg.HighPad.Should().Be(0x3FF);
    }

    #endregion

    #region Value Type: TenBitOffset64

    [Fact]
    public void TenBitOffset64_Float_RoundTrips()
    {
        TenBitOffset64 reg = default;
        reg.FloatVal = 1.5f;
        reg.FloatVal.Should().Be(1.5f);
    }

    [Fact]
    public void TenBitOffset64_Half_RoundTrips()
    {
        TenBitOffset64 reg = default;
        reg.HalfVal = (Half)0.25;
        reg.HalfVal.Should().Be((Half)0.25);
    }

    [Fact]
    public void TenBitOffset64_AllFieldsIndependent()
    {
        TenBitOffset64 reg = default;
        reg.LowPad = 0x3FF;
        reg.FloatVal = 100.0f;
        reg.MidPad = 3;
        reg.HalfVal = (Half)3.0;
        reg.HighPad = 0xF;

        reg.LowPad.Should().Be(0x3FF);
        reg.FloatVal.Should().Be(100.0f);
        reg.MidPad.Should().Be(3);
        reg.HalfVal.Should().Be((Half)3.0);
        reg.HighPad.Should().Be(0xF);
    }

    #endregion

    #region Value Type: OddTriple128

    [Fact]
    public void OddTriple128_Half_RoundTrips()
    {
        OddTriple128 reg = default;
        reg.HalfVal = (Half)2.5;
        reg.HalfVal.Should().Be((Half)2.5);
    }

    [Fact]
    public void OddTriple128_Float_RoundTrips()
    {
        OddTriple128 reg = default;
        reg.FloatVal = 6.28f;
        reg.FloatVal.Should().Be(6.28f);
    }

    [Fact]
    public void OddTriple128_Double_RoundTrips()
    {
        OddTriple128 reg = default;
        reg.DoubleVal = Math.Tau;
        reg.DoubleVal.Should().Be(Math.Tau);
    }

    [Fact]
    public void OddTriple128_AllFieldsIndependent()
    {
        OddTriple128 reg = default;
        reg.LowPad = 7;
        reg.HalfVal = (Half)1.0;
        reg.MidFlag = true;
        reg.FloatVal = 2.0f;
        reg.MidPad = 0x1F;
        reg.DoubleVal = 3.0;
        reg.HighPad = 0x3F;
        reg.TopFlag = true;

        reg.LowPad.Should().Be(7);
        reg.HalfVal.Should().Be((Half)1.0);
        reg.MidFlag.Should().BeTrue();
        reg.FloatVal.Should().Be(2.0f);
        reg.MidPad.Should().Be(0x1F);
        reg.DoubleVal.Should().Be(3.0);
        reg.HighPad.Should().Be(0x3F);
        reg.TopFlag.Should().BeTrue();
    }

    [Fact]
    public void OddTriple128_DoubleDoesNotCorruptOthers()
    {
        OddTriple128 reg = default;
        reg.HalfVal = (Half)1.5;
        reg.FloatVal = 42.0f;
        reg.DoubleVal = double.MaxValue;

        reg.HalfVal.Should().Be((Half)1.5, "double should not corrupt Half");
        reg.FloatVal.Should().Be(42.0f, "double should not corrupt float");
    }

    [Fact]
    public void OddTriple128_SpecialValues()
    {
        OddTriple128 reg = default;
        reg.HalfVal = Half.PositiveInfinity;
        reg.FloatVal = float.NegativeInfinity;
        reg.DoubleVal = double.NaN;

        reg.HalfVal.Should().Be(Half.PositiveInfinity);
        float.IsNegativeInfinity(reg.FloatVal).Should().BeTrue();
        double.IsNaN(reg.DoubleVal).Should().BeTrue();
    }

    #endregion

    #region Value Type: EvenNonByteTriple128

    [Fact]
    public void EvenNonByteTriple128_Half_RoundTrips()
    {
        EvenNonByteTriple128 reg = default;
        reg.HalfVal = (Half)1.5;
        reg.HalfVal.Should().Be((Half)1.5);
    }

    [Fact]
    public void EvenNonByteTriple128_Float_RoundTrips()
    {
        EvenNonByteTriple128 reg = default;
        reg.FloatVal = 3.14f;
        reg.FloatVal.Should().Be(3.14f);
    }

    [Fact]
    public void EvenNonByteTriple128_Double_RoundTrips()
    {
        EvenNonByteTriple128 reg = default;
        reg.DoubleVal = Math.E;
        reg.DoubleVal.Should().Be(Math.E);
    }

    [Fact]
    public void EvenNonByteTriple128_AllFieldsIndependent()
    {
        EvenNonByteTriple128 reg = default;
        reg.LowPad = 0xF;
        reg.HalfVal = (Half)1.0;
        reg.MidPad1 = 3;
        reg.FloatVal = 2.0f;
        reg.MidPad2 = 0xF;
        reg.DoubleVal = 3.0;
        reg.HighPad = 0x3F;

        reg.LowPad.Should().Be(0xF);
        reg.HalfVal.Should().Be((Half)1.0);
        reg.MidPad1.Should().Be(3);
        reg.FloatVal.Should().Be(2.0f);
        reg.MidPad2.Should().Be(0xF);
        reg.DoubleVal.Should().Be(3.0);
        reg.HighPad.Should().Be(0x3F);
    }

    [Fact]
    public void EvenNonByteTriple128_SpecialValues()
    {
        EvenNonByteTriple128 reg = default;
        reg.HalfVal = Half.NaN;
        reg.FloatVal = float.Epsilon;
        reg.DoubleVal = double.NegativeInfinity;

        Half.IsNaN(reg.HalfVal).Should().BeTrue();
        reg.FloatVal.Should().Be(float.Epsilon);
        double.IsNegativeInfinity(reg.DoubleVal).Should().BeTrue();
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    //  Multi-Word Cross-Word Boundary Tests
    // ═══════════════════════════════════════════════════════════════

    #region Multi-Word: CrossWordDouble128

    [Fact]
    public void CrossWordDouble_RoundTrips()
    {
        CrossWordDouble128 mw = default;
        mw.DoubleVal = Math.PI;
        mw.DoubleVal.Should().Be(Math.PI);
    }

    [Fact]
    public void CrossWordDouble_Float_RoundTrips()
    {
        CrossWordDouble128 mw = default;
        mw.FloatVal = 3.14f;
        mw.FloatVal.Should().Be(3.14f);
    }

    [Fact]
    public void CrossWordDouble_Half_RoundTrips()
    {
        CrossWordDouble128 mw = default;
        mw.HalfVal = (Half)1.5;
        mw.HalfVal.Should().Be((Half)1.5);
    }

    [Fact]
    public void CrossWordDouble_AllFieldsIndependent()
    {
        CrossWordDouble128 mw = default;
        mw.LowPad = 5;
        mw.DoubleVal = Math.E;
        mw.MidFlag = true;
        mw.FloatVal = 42.0f;
        mw.HalfVal = (Half)2.0;
        mw.HighPad = 0xFFF;

        mw.LowPad.Should().Be(5);
        mw.DoubleVal.Should().Be(Math.E);
        mw.MidFlag.Should().BeTrue();
        mw.FloatVal.Should().Be(42.0f);
        mw.HalfVal.Should().Be((Half)2.0);
        mw.HighPad.Should().Be(0xFFF);
    }

    [Fact]
    public void CrossWordDouble_DoesNotCorruptAdjacentFields()
    {
        CrossWordDouble128 mw = default;
        mw.LowPad = 7;
        mw.MidFlag = true;
        mw.DoubleVal = double.MaxValue;

        mw.LowPad.Should().Be(7, "double should not corrupt LowPad");
        mw.MidFlag.Should().BeTrue("double should not corrupt MidFlag");
    }

    [Fact]
    public void CrossWordDouble_SpecialValues()
    {
        CrossWordDouble128 mw = default;
        mw.DoubleVal = double.NegativeInfinity;
        mw.FloatVal = float.NaN;
        mw.HalfVal = Half.Epsilon;

        double.IsNegativeInfinity(mw.DoubleVal).Should().BeTrue();
        float.IsNaN(mw.FloatVal).Should().BeTrue();
        mw.HalfVal.Should().Be(Half.Epsilon);
    }

    [Fact]
    public void CrossWordDouble_MaxValue()
    {
        CrossWordDouble128 mw = default;
        mw.DoubleVal = double.MaxValue;
        mw.DoubleVal.Should().Be(double.MaxValue);
    }

    #endregion

    #region Multi-Word: CrossWordFloat128

    [Fact]
    public void CrossWordFloat_RoundTrips()
    {
        CrossWordFloat128 mw = default;
        mw.FloatVal = 3.14f;
        mw.FloatVal.Should().Be(3.14f);
    }

    [Fact]
    public void CrossWordFloat_Half_RoundTrips()
    {
        CrossWordFloat128 mw = default;
        mw.HalfVal = (Half)2.5;
        mw.HalfVal.Should().Be((Half)2.5);
    }

    [Fact]
    public void CrossWordFloat_AllFieldsIndependent()
    {
        CrossWordFloat128 mw = default;
        mw.LowField = 0x3FFFFFFFFFFFF;
        mw.FloatVal = 1.25f;
        mw.HalfVal = (Half)0.5;
        mw.HighPad = 0x1FFFFFFF;
        mw.EndFlag = true;

        mw.LowField.Should().Be(0x3FFFFFFFFFFFF);
        mw.FloatVal.Should().Be(1.25f);
        mw.HalfVal.Should().Be((Half)0.5);
        mw.HighPad.Should().Be(0x1FFFFFFF);
        mw.EndFlag.Should().BeTrue();
    }

    [Fact]
    public void CrossWordFloat_SpecialValues()
    {
        CrossWordFloat128 mw = default;
        mw.FloatVal = float.NaN;
        mw.HalfVal = Half.NegativeInfinity;

        float.IsNaN(mw.FloatVal).Should().BeTrue();
        Half.IsNegativeInfinity(mw.HalfVal).Should().BeTrue();
    }

    #endregion

    #region Multi-Word: CrossWordHalf128

    [Fact]
    public void CrossWordHalf_RoundTrips()
    {
        CrossWordHalf128 mw = default;
        mw.HalfVal = (Half)1.5;
        mw.HalfVal.Should().Be((Half)1.5);
    }

    [Fact]
    public void CrossWordHalf_Float_RoundTrips()
    {
        CrossWordHalf128 mw = default;
        mw.FloatVal = 6.28f;
        mw.FloatVal.Should().Be(6.28f);
    }

    [Fact]
    public void CrossWordHalf_AllFieldsIndependent()
    {
        CrossWordHalf128 mw = default;
        mw.LowField = 0x7FFFFFFFFFFFFF;
        mw.HalfVal = (Half)2.0;
        mw.FloatVal = 42.0f;
        mw.HighPad = 0x1FFFFFF;

        mw.LowField.Should().Be(0x7FFFFFFFFFFFFF);
        mw.HalfVal.Should().Be((Half)2.0);
        mw.FloatVal.Should().Be(42.0f);
        mw.HighPad.Should().Be(0x1FFFFFF);
    }

    [Fact]
    public void CrossWordHalf_SpecialValues()
    {
        CrossWordHalf128 mw = default;
        mw.HalfVal = Half.NaN;
        Half.IsNaN(mw.HalfVal).Should().BeTrue();
    }

    #endregion

    #region Multi-Word: EdgeCrossFloat128

    [Fact]
    public void EdgeCrossFloat_RoundTrips()
    {
        EdgeCrossFloat128 mw = default;
        mw.FloatVal = 3.14f;
        mw.FloatVal.Should().Be(3.14f);
    }

    [Fact]
    public void EdgeCrossFloat_Half_RoundTrips()
    {
        EdgeCrossFloat128 mw = default;
        mw.HalfVal = (Half)2.5;
        mw.HalfVal.Should().Be((Half)2.5);
    }

    [Fact]
    public void EdgeCrossFloat_MaxValue()
    {
        EdgeCrossFloat128 mw = default;
        mw.FloatVal = float.MaxValue;
        mw.FloatVal.Should().Be(float.MaxValue);
    }

    [Fact]
    public void EdgeCrossFloat_DoesNotCorruptLowField()
    {
        EdgeCrossFloat128 mw = default;
        mw.LowField = 0x1FFFFFFFF;
        mw.FloatVal = float.MaxValue;
        mw.LowField.Should().Be(0x1FFFFFFFF, "float should not corrupt LowField");
    }

    [Fact]
    public void EdgeCrossFloat_NaN()
    {
        EdgeCrossFloat128 mw = default;
        mw.FloatVal = float.NaN;
        float.IsNaN(mw.FloatVal).Should().BeTrue();
    }

    #endregion

    #region Multi-Word: CrossWordDoubleEven128

    [Fact]
    public void CrossWordDoubleEven_RoundTrips()
    {
        CrossWordDoubleEven128 mw = default;
        mw.DoubleVal = Math.PI;
        mw.DoubleVal.Should().Be(Math.PI);
    }

    [Fact]
    public void CrossWordDoubleEven_AllFieldsIndependent()
    {
        CrossWordDoubleEven128 mw = default;
        mw.LowPad = 0x3F;
        mw.DoubleVal = Math.E;
        mw.HalfVal = (Half)1.0;
        mw.FloatVal = 42.0f;
        mw.HighPad = 0x3FF;

        mw.LowPad.Should().Be(0x3F);
        mw.DoubleVal.Should().Be(Math.E);
        mw.HalfVal.Should().Be((Half)1.0);
        mw.FloatVal.Should().Be(42.0f);
        mw.HighPad.Should().Be(0x3FF);
    }

    [Fact]
    public void CrossWordDoubleEven_SpecialValues()
    {
        CrossWordDoubleEven128 mw = default;
        mw.DoubleVal = double.NaN;
        mw.HalfVal = Half.NegativeInfinity;
        mw.FloatVal = float.Epsilon;

        double.IsNaN(mw.DoubleVal).Should().BeTrue();
        Half.IsNegativeInfinity(mw.HalfVal).Should().BeTrue();
        mw.FloatVal.Should().Be(float.Epsilon);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    //  Record Struct View Tests — Odd, Even-Non-Byte, BE
    // ═══════════════════════════════════════════════════════════════

    #region View: OddAlignedView

    [Fact]
    public void OddAlignedView_Half_RoundTrips()
    {
        byte[] buffer = new byte[OddAlignedView.SIZE_IN_BYTES];
        var view = new OddAlignedView(buffer);
        view.HalfVal = (Half)2.5;
        view.HalfVal.Should().Be((Half)2.5);
    }

    [Fact]
    public void OddAlignedView_Float_RoundTrips()
    {
        byte[] buffer = new byte[OddAlignedView.SIZE_IN_BYTES];
        var view = new OddAlignedView(buffer);
        view.FloatVal = 3.14f;
        view.FloatVal.Should().Be(3.14f);
    }

    [Fact]
    public void OddAlignedView_Double_RoundTrips()
    {
        byte[] buffer = new byte[OddAlignedView.SIZE_IN_BYTES];
        var view = new OddAlignedView(buffer);
        view.DoubleVal = Math.PI;
        view.DoubleVal.Should().Be(Math.PI);
    }

    [Fact]
    public void OddAlignedView_AllFieldsIndependent()
    {
        byte[] buffer = new byte[OddAlignedView.SIZE_IN_BYTES];
        var view = new OddAlignedView(buffer);

        view.LowPad = 5;
        view.HalfVal = (Half)1.0;
        view.MidPad1 = 3;
        view.FloatVal = 2.0f;
        view.MidPad2 = 0x1F;
        view.DoubleVal = 3.0;
        view.TopPad = 0x3F;

        view.LowPad.Should().Be(5);
        view.HalfVal.Should().Be((Half)1.0);
        view.MidPad1.Should().Be(3);
        view.FloatVal.Should().Be(2.0f);
        view.MidPad2.Should().Be(0x1F);
        view.DoubleVal.Should().Be(3.0);
        view.TopPad.Should().Be(0x3F);
    }

    [Fact]
    public void OddAlignedView_SpecialValues()
    {
        byte[] buffer = new byte[OddAlignedView.SIZE_IN_BYTES];
        var view = new OddAlignedView(buffer);

        view.HalfVal = Half.PositiveInfinity;
        view.FloatVal = float.NegativeInfinity;
        view.DoubleVal = double.NaN;

        view.HalfVal.Should().Be(Half.PositiveInfinity);
        float.IsNegativeInfinity(view.FloatVal).Should().BeTrue();
        double.IsNaN(view.DoubleVal).Should().BeTrue();
    }

    [Fact]
    public void OddAlignedView_ZeroBuffer_ReturnsZeroes()
    {
        byte[] buffer = new byte[OddAlignedView.SIZE_IN_BYTES];
        var view = new OddAlignedView(buffer);

        view.HalfVal.Should().Be((Half)0.0);
        view.FloatVal.Should().Be(0.0f);
        view.DoubleVal.Should().Be(0.0);
    }

    #endregion

    #region View: EvenNonByteView

    [Fact]
    public void EvenNonByteView_Half_RoundTrips()
    {
        byte[] buffer = new byte[EvenNonByteView.SIZE_IN_BYTES];
        var view = new EvenNonByteView(buffer);
        view.HalfVal = (Half)1.5;
        view.HalfVal.Should().Be((Half)1.5);
    }

    [Fact]
    public void EvenNonByteView_Float_RoundTrips()
    {
        byte[] buffer = new byte[EvenNonByteView.SIZE_IN_BYTES];
        var view = new EvenNonByteView(buffer);
        view.FloatVal = 6.28f;
        view.FloatVal.Should().Be(6.28f);
    }

    [Fact]
    public void EvenNonByteView_Double_RoundTrips()
    {
        byte[] buffer = new byte[EvenNonByteView.SIZE_IN_BYTES];
        var view = new EvenNonByteView(buffer);
        view.DoubleVal = Math.E;
        view.DoubleVal.Should().Be(Math.E);
    }

    [Fact]
    public void EvenNonByteView_AllFieldsIndependent()
    {
        byte[] buffer = new byte[EvenNonByteView.SIZE_IN_BYTES];
        var view = new EvenNonByteView(buffer);

        view.LowPad = 0xF;
        view.HalfVal = (Half)1.0;
        view.MidPad1 = 3;
        view.FloatVal = 2.0f;
        view.MidPad2 = 0xF;
        view.DoubleVal = 3.0;
        view.TopPad = 0x3F;

        view.LowPad.Should().Be(0xF);
        view.HalfVal.Should().Be((Half)1.0);
        view.MidPad1.Should().Be(3);
        view.FloatVal.Should().Be(2.0f);
        view.MidPad2.Should().Be(0xF);
        view.DoubleVal.Should().Be(3.0);
        view.TopPad.Should().Be(0x3F);
    }

    [Fact]
    public void EvenNonByteView_SpecialValues()
    {
        byte[] buffer = new byte[EvenNonByteView.SIZE_IN_BYTES];
        var view = new EvenNonByteView(buffer);

        view.HalfVal = Half.NaN;
        view.FloatVal = float.Epsilon;
        view.DoubleVal = double.NegativeInfinity;

        Half.IsNaN(view.HalfVal).Should().BeTrue();
        view.FloatVal.Should().Be(float.Epsilon);
        double.IsNegativeInfinity(view.DoubleVal).Should().BeTrue();
    }

    #endregion

    #region View: SingleBitOffsetView

    [Fact]
    public void SingleBitOffset_Half_RoundTrips()
    {
        byte[] buffer = new byte[SingleBitOffsetView.SIZE_IN_BYTES];
        var view = new SingleBitOffsetView(buffer);
        view.HalfVal = (Half)1.5;
        view.HalfVal.Should().Be((Half)1.5);
    }

    [Fact]
    public void SingleBitOffset_Float_RoundTrips()
    {
        byte[] buffer = new byte[SingleBitOffsetView.SIZE_IN_BYTES];
        var view = new SingleBitOffsetView(buffer);
        view.FloatVal = 42.0f;
        view.FloatVal.Should().Be(42.0f);
    }

    [Fact]
    public void SingleBitOffset_AllFieldsIndependent()
    {
        byte[] buffer = new byte[SingleBitOffsetView.SIZE_IN_BYTES];
        var view = new SingleBitOffsetView(buffer);

        view.StartFlag = true;
        view.HalfVal = (Half)2.0;
        view.FloatVal = 3.14f;
        view.TopPad = 0x7FFF;

        view.StartFlag.Should().BeTrue();
        view.HalfVal.Should().Be((Half)2.0);
        view.FloatVal.Should().Be(3.14f);
        view.TopPad.Should().Be(0x7FFF);
    }

    #endregion

    #region View: SixBitOffsetView

    [Fact]
    public void SixBitOffsetView_Float_RoundTrips()
    {
        byte[] buffer = new byte[SixBitOffsetView.SIZE_IN_BYTES];
        var view = new SixBitOffsetView(buffer);
        view.FloatVal = 3.14f;
        view.FloatVal.Should().Be(3.14f);
    }

    [Fact]
    public void SixBitOffsetView_Half_RoundTrips()
    {
        byte[] buffer = new byte[SixBitOffsetView.SIZE_IN_BYTES];
        var view = new SixBitOffsetView(buffer);
        view.HalfVal = (Half)2.5;
        view.HalfVal.Should().Be((Half)2.5);
    }

    [Fact]
    public void SixBitOffsetView_AllFieldsIndependent()
    {
        byte[] buffer = new byte[SixBitOffsetView.SIZE_IN_BYTES];
        var view = new SixBitOffsetView(buffer);

        view.LowPad = 0x3F;
        view.FloatVal = 42.0f;
        view.HalfVal = (Half)1.0;
        view.HighPad = 0x3FF;

        view.LowPad.Should().Be(0x3F);
        view.FloatVal.Should().Be(42.0f);
        view.HalfVal.Should().Be((Half)1.0);
        view.HighPad.Should().Be(0x3FF);
    }

    #endregion

    #region View: BigEndianOddView

    [Fact]
    public void BigEndianOdd_Half_RoundTrips()
    {
        byte[] buffer = new byte[BigEndianOddView.SIZE_IN_BYTES];
        var view = new BigEndianOddView(buffer);
        view.HalfVal = (Half)1.5;
        view.HalfVal.Should().Be((Half)1.5);
    }

    [Fact]
    public void BigEndianOdd_Float_RoundTrips()
    {
        byte[] buffer = new byte[BigEndianOddView.SIZE_IN_BYTES];
        var view = new BigEndianOddView(buffer);
        view.FloatVal = 3.14f;
        view.FloatVal.Should().Be(3.14f);
    }

    [Fact]
    public void BigEndianOdd_AllFieldsIndependent()
    {
        byte[] buffer = new byte[BigEndianOddView.SIZE_IN_BYTES];
        var view = new BigEndianOddView(buffer);

        view.StartFlag = true;
        view.HalfVal = (Half)2.0;
        view.MidPad = 3;
        view.FloatVal = 42.0f;
        view.LowPad = 0x1FFF;

        view.StartFlag.Should().BeTrue();
        view.HalfVal.Should().Be((Half)2.0);
        view.MidPad.Should().Be(3);
        view.FloatVal.Should().Be(42.0f);
        view.LowPad.Should().Be(0x1FFF);
    }

    [Fact]
    public void BigEndianOdd_SpecialValues()
    {
        byte[] buffer = new byte[BigEndianOddView.SIZE_IN_BYTES];
        var view = new BigEndianOddView(buffer);

        view.HalfVal = Half.NaN;
        view.FloatVal = float.PositiveInfinity;

        Half.IsNaN(view.HalfVal).Should().BeTrue();
        float.IsPositiveInfinity(view.FloatVal).Should().BeTrue();
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    //  Parameterized Value Sweep Tests
    // ═══════════════════════════════════════════════════════════════

    #region Theory: Float Value Sweep at Odd Positions

    [Theory]
    [InlineData(0.0f)]
    [InlineData(1.0f)]
    [InlineData(-1.0f)]
    [InlineData(0.5f)]
    [InlineData(3.14f)]
    [InlineData(42.0f)]
    [InlineData(-42.0f)]
    [InlineData(float.Epsilon)]
    [InlineData(float.MaxValue)]
    [InlineData(float.MinValue)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(float.NaN)]
    [InlineData(1.17549435E-38f)]   // Smallest normal
    public void OddStart64_FloatSweep(float value)
    {
        OddStart64 reg = default;
        reg.FloatVal = value;

        if (float.IsNaN(value))
            float.IsNaN(reg.FloatVal).Should().BeTrue();
        else
            reg.FloatVal.Should().Be(value);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(1.0f)]
    [InlineData(-1.0f)]
    [InlineData(3.14f)]
    [InlineData(float.MaxValue)]
    [InlineData(float.MinValue)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(float.NaN)]
    public void CrossWordFloat_FloatSweep(float value)
    {
        CrossWordFloat128 mw = default;
        mw.FloatVal = value;

        if (float.IsNaN(value))
            float.IsNaN(mw.FloatVal).Should().BeTrue();
        else
            mw.FloatVal.Should().Be(value);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(1.0f)]
    [InlineData(-1.0f)]
    [InlineData(3.14f)]
    [InlineData(float.MaxValue)]
    [InlineData(float.Epsilon)]
    [InlineData(float.NaN)]
    public void EvenNonByte64_FloatSweep(float value)
    {
        EvenNonByte64 reg = default;
        reg.FloatVal = value;

        if (float.IsNaN(value))
            float.IsNaN(reg.FloatVal).Should().BeTrue();
        else
            reg.FloatVal.Should().Be(value);
    }

    #endregion

    #region Theory: Half Value Sweep at Odd Positions

    [Theory]
    [InlineData(0.0f)]
    [InlineData(1.0f)]
    [InlineData(-1.0f)]
    [InlineData(0.5f)]
    [InlineData(1.5f)]
    [InlineData(65504.0f)]     // Half.MaxValue
    [InlineData(-65504.0f)]    // Half.MinValue
    public void OddStart64_HalfSweep(float rawValue)
    {
        Half value = (Half)rawValue;
        OddStart64 reg = default;
        reg.HalfVal = value;
        reg.HalfVal.Should().Be(value);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(1.0f)]
    [InlineData(-1.0f)]
    [InlineData(1.5f)]
    [InlineData(65504.0f)]
    public void CrossWordHalf_HalfSweep(float rawValue)
    {
        Half value = (Half)rawValue;
        CrossWordHalf128 mw = default;
        mw.HalfVal = value;
        mw.HalfVal.Should().Be(value);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(1.0f)]
    [InlineData(-1.0f)]
    [InlineData(0.5f)]
    [InlineData(65504.0f)]
    public void EvenNonByte64_HalfSweep(float rawValue)
    {
        Half value = (Half)rawValue;
        EvenNonByte64 reg = default;
        reg.HalfVal = value;
        reg.HalfVal.Should().Be(value);
    }

    #endregion

    #region Theory: Double Value Sweep at Odd Positions

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    [InlineData(0.5)]
    [InlineData(3.141592653589793)]
    [InlineData(2.718281828459045)]
    [InlineData(double.Epsilon)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.NaN)]
    [InlineData(2.2250738585072014E-308)]   // Smallest normal
    public void OddTriple128_DoubleSweep(double value)
    {
        OddTriple128 reg = default;
        reg.DoubleVal = value;

        if (double.IsNaN(value))
            double.IsNaN(reg.DoubleVal).Should().BeTrue();
        else
            reg.DoubleVal.Should().Be(value);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    [InlineData(3.141592653589793)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.NaN)]
    public void CrossWordDouble_DoubleSweep(double value)
    {
        CrossWordDouble128 mw = default;
        mw.DoubleVal = value;

        if (double.IsNaN(value))
            double.IsNaN(mw.DoubleVal).Should().BeTrue();
        else
            mw.DoubleVal.Should().Be(value);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    [InlineData(double.MaxValue)]
    [InlineData(double.Epsilon)]
    [InlineData(double.NaN)]
    public void EvenNonByteTriple128_DoubleSweep(double value)
    {
        EvenNonByteTriple128 reg = default;
        reg.DoubleVal = value;

        if (double.IsNaN(value))
            double.IsNaN(reg.DoubleVal).Should().BeTrue();
        else
            reg.DoubleVal.Should().Be(value);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    [InlineData(3.141592653589793)]
    [InlineData(double.MaxValue)]
    [InlineData(double.NaN)]
    public void CrossWordDoubleEven_DoubleSweep(double value)
    {
        CrossWordDoubleEven128 mw = default;
        mw.DoubleVal = value;

        if (double.IsNaN(value))
            double.IsNaN(mw.DoubleVal).Should().BeTrue();
        else
            mw.DoubleVal.Should().Be(value);
    }

    #endregion

    #region Theory: View Float Sweep at Odd Positions

    [Theory]
    [InlineData(0.0f)]
    [InlineData(1.0f)]
    [InlineData(-1.0f)]
    [InlineData(3.14f)]
    [InlineData(float.MaxValue)]
    [InlineData(float.Epsilon)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void OddAlignedView_FloatSweep(float value)
    {
        byte[] buffer = new byte[OddAlignedView.SIZE_IN_BYTES];
        var view = new OddAlignedView(buffer);
        view.FloatVal = value;

        if (float.IsNaN(value))
            float.IsNaN(view.FloatVal).Should().BeTrue();
        else
            view.FloatVal.Should().Be(value);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    [InlineData(3.141592653589793)]
    [InlineData(double.MaxValue)]
    [InlineData(double.NaN)]
    public void OddAlignedView_DoubleSweep(double value)
    {
        byte[] buffer = new byte[OddAlignedView.SIZE_IN_BYTES];
        var view = new OddAlignedView(buffer);
        view.DoubleVal = value;

        if (double.IsNaN(value))
            double.IsNaN(view.DoubleVal).Should().BeTrue();
        else
            view.DoubleVal.Should().Be(value);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(1.0f)]
    [InlineData(-1.0f)]
    [InlineData(3.14f)]
    [InlineData(float.MaxValue)]
    [InlineData(float.NaN)]
    public void BigEndianOdd_FloatSweep(float value)
    {
        byte[] buffer = new byte[BigEndianOddView.SIZE_IN_BYTES];
        var view = new BigEndianOddView(buffer);
        view.FloatVal = value;

        if (float.IsNaN(value))
            float.IsNaN(view.FloatVal).Should().BeTrue();
        else
            view.FloatVal.Should().Be(value);
    }

    #endregion
}
