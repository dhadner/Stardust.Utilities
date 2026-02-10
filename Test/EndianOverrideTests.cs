using System;
using FluentAssertions;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Exhaustive tests for endian-type overrides (*Be/*Le property types)
/// across all combinations of ByteOrder and BitOrder in both
/// [BitFieldsView] and [BitFields] structs.
///
/// Coverage matrix:
///
///   BitFieldsView (4 ByteOrder × BitOrder combos):
///     1. (BE, MsbIsBitZero) with *Le overrides
///     2. (BE, LsbIsBitZero) with *Le overrides
///     3. (LE, LsbIsBitZero) with *Be overrides
///     4. (LE, MsbIsBitZero) with *Be overrides
///
///   BitFields (2 BitOrder values, endian property types):
///     5. LsbIsBitZero (default) with *Be and *Le property types
///     6. MsbIsBitZero with *Be and *Le property types
/// </summary>
public partial class EndianOverrideTests
{
    // ================================================================
    // BitFieldsView struct definitions — all four ByteOrder × BitOrder
    // combos, each with opposite-endian overrides + same-endian explicit
    // ================================================================

    // --- Combo 1: BigEndian / MsbIsBitZero, with *Le overrides ---

    [BitFieldsView(ByteOrder.BigEndian, BitOrder.MsbIsBitZero)]
    public partial record struct View_BE_Msb
    {
        [BitField(0, 15)]   public partial ushort    NativeU16 { get; set; }  // BE (struct default)
        [BitField(16, 47)]  public partial UInt32Le  LeU32     { get; set; }  // LE override
        [BitField(48, 63)]  public partial Int16Le   LeS16     { get; set; }  // LE override (signed)
        [BitField(64, 127)] public partial UInt64Le  LeU64     { get; set; }  // LE override (64-bit)
    }

    // --- Combo 2: BigEndian / LsbIsBitZero, with *Le overrides ---

    [BitFieldsView(ByteOrder.BigEndian, BitOrder.LsbIsBitZero)]
    public partial record struct View_BE_Lsb
    {
        [BitField(0, 15)]   public partial ushort    NativeU16 { get; set; }  // BE (struct default)
        [BitField(16, 47)]  public partial UInt32Le  LeU32     { get; set; }  // LE override
        [BitField(48, 63)]  public partial Int16Le   LeS16     { get; set; }  // LE override (signed)
        [BitField(64, 127)] public partial UInt64Le  LeU64     { get; set; }  // LE override (64-bit)
    }

    // --- Combo 3: LittleEndian / LsbIsBitZero, with *Be overrides ---

    [BitFieldsView(ByteOrder.LittleEndian, BitOrder.LsbIsBitZero)]
    public partial record struct View_LE_Lsb
    {
        [BitField(0, 15)]   public partial ushort    NativeU16 { get; set; }  // LE (struct default)
        [BitField(16, 47)]  public partial UInt32Be  BeU32     { get; set; }  // BE override
        [BitField(48, 63)]  public partial Int16Be   BeS16     { get; set; }  // BE override (signed)
        [BitField(64, 127)] public partial UInt64Be  BeU64     { get; set; }  // BE override (64-bit)
    }

    // --- Combo 4: LittleEndian / MsbIsBitZero, with *Be overrides ---

    [BitFieldsView(ByteOrder.LittleEndian, BitOrder.MsbIsBitZero)]
    public partial record struct View_LE_Msb
    {
        [BitField(0, 15)]   public partial ushort    NativeU16 { get; set; }  // LE (struct default)
        [BitField(16, 47)]  public partial UInt32Be  BeU32     { get; set; }  // BE override
        [BitField(48, 63)]  public partial Int16Be   BeS16     { get; set; }  // BE override (signed)
        [BitField(64, 127)] public partial UInt64Be  BeU64     { get; set; }  // BE override (64-bit)
    }

    // ================================================================
    // BitFields struct definitions — endian property types in value
    // type structs (LsbIsBitZero and MsbIsBitZero)
    // ================================================================

    // --- BitFields with default LsbIsBitZero, endian property types ---

    [BitFields(typeof(ulong))]
    public partial struct Reg_Lsb
    {
        [BitField(0, 15)]  public partial UInt16Be BeU16 { get; set; }
        [BitField(16, 31)] public partial UInt16Le LeU16 { get; set; }
        [BitField(32, 63)] public partial UInt32Be BeU32 { get; set; }
    }

    // --- BitFields with MsbIsBitZero, endian property types ---

    [BitFields(typeof(ulong), bitOrder: BitOrder.MsbIsBitZero)]
    public partial struct Reg_Msb
    {
        [BitField(0, 15)]  public partial UInt16Be BeU16 { get; set; }
        [BitField(16, 31)] public partial UInt16Le LeU16 { get; set; }
        [BitField(32, 63)] public partial UInt32Be BeU32 { get; set; }
    }

    // ================================================================
    // BitFieldsView Tests — Combo 1: BigEndian / MsbIsBitZero + *Le
    // ================================================================

    #region Combo 1: BitFieldsView (BE, MsbIsBitZero) with *Le overrides

    [Fact]
    public void View_BE_Msb_NativeField_WritesBigEndian()
    {
        var data = new byte[View_BE_Msb.SizeInBytes];
        var view = new View_BE_Msb(data);

        view.NativeU16 = 0x1234;

        // BE/MsbIsBitZero: MSB first -> [0x12, 0x34]
        data[0].Should().Be(0x12, "NativeU16 byte 0 (BE)");
        data[1].Should().Be(0x34, "NativeU16 byte 1 (BE)");
    }

    [Fact]
    public void View_BE_Msb_LeU32_OverridesTo_LittleEndian()
    {
        var data = new byte[View_BE_Msb.SizeInBytes];
        var view = new View_BE_Msb(data);

        view.LeU32 = new UInt32Le(0xDEADBEEF);

        // UInt32Le override: LSB first -> [0xEF, 0xBE, 0xAD, 0xDE]
        data[2].Should().Be(0xEF, "LeU32[0] LE override");
        data[3].Should().Be(0xBE, "LeU32[1] LE override");
        data[4].Should().Be(0xAD, "LeU32[2] LE override");
        data[5].Should().Be(0xDE, "LeU32[3] LE override");
    }

    [Fact]
    public void View_BE_Msb_LeS16_SignedOverride_LittleEndian()
    {
        var data = new byte[View_BE_Msb.SizeInBytes];
        var view = new View_BE_Msb(data);

        view.LeS16 = new Int16Le(-2);

        // -2 as LE: [0xFE, 0xFF]
        data[6].Should().Be(0xFE, "LeS16[0] LE override");
        data[7].Should().Be(0xFF, "LeS16[1] LE override");
    }

    [Fact]
    public void View_BE_Msb_LeU64_OverridesTo_LittleEndian()
    {
        var data = new byte[View_BE_Msb.SizeInBytes];
        var view = new View_BE_Msb(data);

        view.LeU64 = new UInt64Le(0x0102030405060708UL);

        // UInt64Le override: LSB first
        data[8].Should().Be(0x08, "LeU64[0] LE override");
        data[9].Should().Be(0x07, "LeU64[1]");
        data[10].Should().Be(0x06, "LeU64[2]");
        data[11].Should().Be(0x05, "LeU64[3]");
        data[12].Should().Be(0x04, "LeU64[4]");
        data[13].Should().Be(0x03, "LeU64[5]");
        data[14].Should().Be(0x02, "LeU64[6]");
        data[15].Should().Be(0x01, "LeU64[7] LE override");
    }

    [Fact]
    public void View_BE_Msb_RoundTrip()
    {
        var data = new byte[View_BE_Msb.SizeInBytes];
        var view = new View_BE_Msb(data);

        view.NativeU16 = 0xCAFE;
        view.LeU32 = new UInt32Le(0x12345678);
        view.LeS16 = new Int16Le(-999);
        view.LeU64 = new UInt64Le(0xAABBCCDDEEFF0011UL);

        view.NativeU16.Should().Be(0xCAFE);
        ((uint)view.LeU32).Should().Be(0x12345678U);
        ((short)view.LeS16).Should().Be(-999);
        ((ulong)view.LeU64).Should().Be(0xAABBCCDDEEFF0011UL);
    }

    #endregion

    // ================================================================
    // BitFieldsView Tests — Combo 2: BigEndian / LsbIsBitZero + *Le
    // ================================================================

    #region Combo 2: BitFieldsView (BE, LsbIsBitZero) with *Le overrides

    [Fact]
    public void View_BE_Lsb_NativeField_WritesBigEndian()
    {
        var data = new byte[View_BE_Lsb.SizeInBytes];
        var view = new View_BE_Lsb(data);

        view.NativeU16 = 0x1234;

        // BE byte order: MSB first -> [0x12, 0x34]
        data[0].Should().Be(0x12, "NativeU16 byte 0 (BE)");
        data[1].Should().Be(0x34, "NativeU16 byte 1 (BE)");
    }

    [Fact]
    public void View_BE_Lsb_LeU32_OverridesTo_LittleEndian()
    {
        var data = new byte[View_BE_Lsb.SizeInBytes];
        var view = new View_BE_Lsb(data);

        view.LeU32 = new UInt32Le(0xDEADBEEF);

        // UInt32Le override: LSB first regardless of struct default
        data[2].Should().Be(0xEF, "LeU32[0] LE override");
        data[3].Should().Be(0xBE, "LeU32[1] LE override");
        data[4].Should().Be(0xAD, "LeU32[2] LE override");
        data[5].Should().Be(0xDE, "LeU32[3] LE override");
    }

    [Fact]
    public void View_BE_Lsb_LeS16_SignedOverride_LittleEndian()
    {
        var data = new byte[View_BE_Lsb.SizeInBytes];
        var view = new View_BE_Lsb(data);

        view.LeS16 = new Int16Le(-2);

        data[6].Should().Be(0xFE, "LeS16[0] LE override");
        data[7].Should().Be(0xFF, "LeS16[1] LE override");
    }

    [Fact]
    public void View_BE_Lsb_LeU64_OverridesTo_LittleEndian()
    {
        var data = new byte[View_BE_Lsb.SizeInBytes];
        var view = new View_BE_Lsb(data);

        view.LeU64 = new UInt64Le(0x0102030405060708UL);

        data[8].Should().Be(0x08, "LeU64[0] LE override");
        data[9].Should().Be(0x07, "LeU64[1]");
        data[10].Should().Be(0x06, "LeU64[2]");
        data[11].Should().Be(0x05, "LeU64[3]");
        data[12].Should().Be(0x04, "LeU64[4]");
        data[13].Should().Be(0x03, "LeU64[5]");
        data[14].Should().Be(0x02, "LeU64[6]");
        data[15].Should().Be(0x01, "LeU64[7] LE override");
    }

    [Fact]
    public void View_BE_Lsb_RoundTrip()
    {
        var data = new byte[View_BE_Lsb.SizeInBytes];
        var view = new View_BE_Lsb(data);

        view.NativeU16 = 0xCAFE;
        view.LeU32 = new UInt32Le(0x12345678);
        view.LeS16 = new Int16Le(-999);
        view.LeU64 = new UInt64Le(0xAABBCCDDEEFF0011UL);

        view.NativeU16.Should().Be(0xCAFE);
        ((uint)view.LeU32).Should().Be(0x12345678U);
        ((short)view.LeS16).Should().Be(-999);
        ((ulong)view.LeU64).Should().Be(0xAABBCCDDEEFF0011UL);
    }

    #endregion

    // ================================================================
    // BitFieldsView Tests — Combo 3: LittleEndian / LsbIsBitZero + *Be
    // ================================================================

    #region Combo 3: BitFieldsView (LE, LsbIsBitZero) with *Be overrides

    [Fact]
    public void View_LE_Lsb_NativeField_WritesLittleEndian()
    {
        var data = new byte[View_LE_Lsb.SizeInBytes];
        var view = new View_LE_Lsb(data);

        view.NativeU16 = 0x1234;

        // LE byte order: LSB first -> [0x34, 0x12]
        data[0].Should().Be(0x34, "NativeU16 byte 0 (LE)");
        data[1].Should().Be(0x12, "NativeU16 byte 1 (LE)");
    }

    [Fact]
    public void View_LE_Lsb_BeU32_OverridesToBigEndian()
    {
        var data = new byte[View_LE_Lsb.SizeInBytes];
        var view = new View_LE_Lsb(data);

        view.BeU32 = new UInt32Be(0xDEADBEEF);

        // UInt32Be override: MSB first -> [0xDE, 0xAD, 0xBE, 0xEF]
        data[2].Should().Be(0xDE, "BeU32[0] BE override");
        data[3].Should().Be(0xAD, "BeU32[1] BE override");
        data[4].Should().Be(0xBE, "BeU32[2] BE override");
        data[5].Should().Be(0xEF, "BeU32[3] BE override");
    }

    [Fact]
    public void View_LE_Lsb_BeS16_SignedOverride_BigEndian()
    {
        var data = new byte[View_LE_Lsb.SizeInBytes];
        var view = new View_LE_Lsb(data);

        view.BeS16 = new Int16Be(-2);

        // -2 as BE: [0xFF, 0xFE]
        data[6].Should().Be(0xFF, "BeS16[0] BE override");
        data[7].Should().Be(0xFE, "BeS16[1] BE override");
    }

    [Fact]
    public void View_LE_Lsb_BeU64_OverridesToBigEndian()
    {
        var data = new byte[View_LE_Lsb.SizeInBytes];
        var view = new View_LE_Lsb(data);

        view.BeU64 = new UInt64Be(0x0102030405060708UL);

        // UInt64Be override: MSB first
        data[8].Should().Be(0x01, "BeU64[0] BE override");
        data[9].Should().Be(0x02, "BeU64[1]");
        data[10].Should().Be(0x03, "BeU64[2]");
        data[11].Should().Be(0x04, "BeU64[3]");
        data[12].Should().Be(0x05, "BeU64[4]");
        data[13].Should().Be(0x06, "BeU64[5]");
        data[14].Should().Be(0x07, "BeU64[6]");
        data[15].Should().Be(0x08, "BeU64[7] BE override");
    }

    [Fact]
    public void View_LE_Lsb_RoundTrip()
    {
        var data = new byte[View_LE_Lsb.SizeInBytes];
        var view = new View_LE_Lsb(data);

        view.NativeU16 = 0xCAFE;
        view.BeU32 = new UInt32Be(0x12345678);
        view.BeS16 = new Int16Be(-999);
        view.BeU64 = new UInt64Be(0xAABBCCDDEEFF0011UL);

        view.NativeU16.Should().Be(0xCAFE);
        ((uint)view.BeU32).Should().Be(0x12345678U);
        ((short)view.BeS16).Should().Be(-999);
        ((ulong)view.BeU64).Should().Be(0xAABBCCDDEEFF0011UL);
    }

    #endregion

    // ================================================================
    // BitFieldsView Tests — Combo 4: LittleEndian / MsbIsBitZero + *Be
    // ================================================================

    #region Combo 4: BitFieldsView (LE, MsbIsBitZero) with *Be overrides

    [Fact]
    public void View_LE_Msb_NativeField_WritesLittleEndian()
    {
        var data = new byte[View_LE_Msb.SizeInBytes];
        var view = new View_LE_Msb(data);

        view.NativeU16 = 0x1234;

        // LE byte order: LSB first -> [0x34, 0x12]
        data[0].Should().Be(0x34, "NativeU16 byte 0 (LE)");
        data[1].Should().Be(0x12, "NativeU16 byte 1 (LE)");
    }

    [Fact]
    public void View_LE_Msb_BeU32_OverridesToBigEndian()
    {
        var data = new byte[View_LE_Msb.SizeInBytes];
        var view = new View_LE_Msb(data);

        view.BeU32 = new UInt32Be(0xDEADBEEF);

        // UInt32Be override: MSB first regardless of struct default
        data[2].Should().Be(0xDE, "BeU32[0] BE override");
        data[3].Should().Be(0xAD, "BeU32[1] BE override");
        data[4].Should().Be(0xBE, "BeU32[2] BE override");
        data[5].Should().Be(0xEF, "BeU32[3] BE override");
    }

    [Fact]
    public void View_LE_Msb_BeS16_SignedOverride_BigEndian()
    {
        var data = new byte[View_LE_Msb.SizeInBytes];
        var view = new View_LE_Msb(data);

        view.BeS16 = new Int16Be(-2);

        data[6].Should().Be(0xFF, "BeS16[0] BE override");
        data[7].Should().Be(0xFE, "BeS16[1] BE override");
    }

    [Fact]
    public void View_LE_Msb_BeU64_OverridesToBigEndian()
    {
        var data = new byte[View_LE_Msb.SizeInBytes];
        var view = new View_LE_Msb(data);

        view.BeU64 = new UInt64Be(0x0102030405060708UL);

        data[8].Should().Be(0x01, "BeU64[0] BE override");
        data[9].Should().Be(0x02, "BeU64[1]");
        data[10].Should().Be(0x03, "BeU64[2]");
        data[11].Should().Be(0x04, "BeU64[3]");
        data[12].Should().Be(0x05, "BeU64[4]");
        data[13].Should().Be(0x06, "BeU64[5]");
        data[14].Should().Be(0x07, "BeU64[6]");
        data[15].Should().Be(0x08, "BeU64[7] BE override");
    }

    [Fact]
    public void View_LE_Msb_RoundTrip()
    {
        var data = new byte[View_LE_Msb.SizeInBytes];
        var view = new View_LE_Msb(data);

        view.NativeU16 = 0xCAFE;
        view.BeU32 = new UInt32Be(0x12345678);
        view.BeS16 = new Int16Be(-999);
        view.BeU64 = new UInt64Be(0xAABBCCDDEEFF0011UL);

        view.NativeU16.Should().Be(0xCAFE);
        ((uint)view.BeU32).Should().Be(0x12345678U);
        ((short)view.BeS16).Should().Be(-999);
        ((ulong)view.BeU64).Should().Be(0xAABBCCDDEEFF0011UL);
    }

    #endregion

    // ================================================================
    // Cross-combo verification: same values, same override type must
    // produce the same wire bytes regardless of struct-level bit order
    // ================================================================

    #region Cross-combo: override wire bytes are bit-order-independent

    [Fact]
    public void LeOverride_SameBytes_Regardless_Of_BitOrder()
    {
        // Combo 1 (BE/MsbIsBitZero) and Combo 2 (BE/LsbIsBitZero) should produce
        // identical wire bytes for the *Le override fields
        var data1 = new byte[View_BE_Msb.SizeInBytes];
        var data2 = new byte[View_BE_Lsb.SizeInBytes];

        var v1 = new View_BE_Msb(data1);
        var v2 = new View_BE_Lsb(data2);

        v1.LeU32 = new UInt32Le(0xDEADBEEF);
        v2.LeU32 = new UInt32Le(0xDEADBEEF);

        // Override bytes at offset 2-5 must match
        for (int i = 2; i < 6; i++)
            data1[i].Should().Be(data2[i], $"LeU32 byte {i} should match across bit orders");

        v1.LeS16 = new Int16Le(-12345);
        v2.LeS16 = new Int16Le(-12345);

        for (int i = 6; i < 8; i++)
            data1[i].Should().Be(data2[i], $"LeS16 byte {i} should match across bit orders");

        v1.LeU64 = new UInt64Le(0x0102030405060708UL);
        v2.LeU64 = new UInt64Le(0x0102030405060708UL);

        for (int i = 8; i < 16; i++)
            data1[i].Should().Be(data2[i], $"LeU64 byte {i} should match across bit orders");
    }

    [Fact]
    public void BeOverride_SameBytes_Regardless_Of_BitOrder()
    {
        // Combo 3 (LE/LsbIsBitZero) and Combo 4 (LE/MsbIsBitZero) should produce
        // identical wire bytes for the *Be override fields
        var data3 = new byte[View_LE_Lsb.SizeInBytes];
        var data4 = new byte[View_LE_Msb.SizeInBytes];

        var v3 = new View_LE_Lsb(data3);
        var v4 = new View_LE_Msb(data4);

        v3.BeU32 = new UInt32Be(0xDEADBEEF);
        v4.BeU32 = new UInt32Be(0xDEADBEEF);

        for (int i = 2; i < 6; i++)
            data3[i].Should().Be(data4[i], $"BeU32 byte {i} should match across bit orders");

        v3.BeS16 = new Int16Be(-12345);
        v4.BeS16 = new Int16Be(-12345);

        for (int i = 6; i < 8; i++)
            data3[i].Should().Be(data4[i], $"BeS16 byte {i} should match across bit orders");

        v3.BeU64 = new UInt64Be(0x0102030405060708UL);
        v4.BeU64 = new UInt64Be(0x0102030405060708UL);

        for (int i = 8; i < 16; i++)
            data3[i].Should().Be(data4[i], $"BeU64 byte {i} should match across bit orders");
    }

    [Fact]
    public void BeOverride_And_LeOverride_ProduceReversedBytes()
    {
        // A BE override and a LE override of the same value should
        // produce byte-reversed wire format for the override field
        var beData = new byte[View_LE_Lsb.SizeInBytes];
        var leData = new byte[View_BE_Msb.SizeInBytes];

        var beView = new View_LE_Lsb(beData);
        var leView = new View_BE_Msb(leData);

        beView.BeU32 = new UInt32Be(0x01020304);
        leView.LeU32 = new UInt32Le(0x01020304);

        // BE at offset 2: [0x01, 0x02, 0x03, 0x04]
        // LE at offset 2: [0x04, 0x03, 0x02, 0x01]
        beData[2].Should().Be(0x01);
        beData[5].Should().Be(0x04);
        leData[2].Should().Be(0x04);
        leData[5].Should().Be(0x01);

        // They should be byte-reversed
        for (int i = 0; i < 4; i++)
            beData[2 + i].Should().Be(leData[5 - i], $"byte {i} should be reversed");
    }

    #endregion

    // ================================================================
    // BitFields value-type tests — endian property types in [BitFields]
    // ================================================================

    #region BitFields (LsbIsBitZero) with endian property types

    [Fact]
    public void Reg_Lsb_BeU16_RoundTrip()
    {
        Reg_Lsb reg = 0;
        reg.BeU16 = new UInt16Be(0x1234);

        ((ushort)reg.BeU16).Should().Be(0x1234);
    }

    [Fact]
    public void Reg_Lsb_LeU16_RoundTrip()
    {
        Reg_Lsb reg = 0;
        reg.LeU16 = new UInt16Le(0xABCD);

        ((ushort)reg.LeU16).Should().Be(0xABCD);
    }

    [Fact]
    public void Reg_Lsb_BeU32_RoundTrip()
    {
        Reg_Lsb reg = 0;
        reg.BeU32 = new UInt32Be(0xDEADBEEF);

        ((uint)reg.BeU32).Should().Be(0xDEADBEEF);
    }

    [Fact]
    public void Reg_Lsb_AllFields_Independent()
    {
        Reg_Lsb reg = 0;
        reg.BeU16 = new UInt16Be(0x1111);
        reg.LeU16 = new UInt16Le(0x2222);
        reg.BeU32 = new UInt32Be(0x33333333);

        ((ushort)reg.BeU16).Should().Be(0x1111, "BeU16 should be independent");
        ((ushort)reg.LeU16).Should().Be(0x2222, "LeU16 should be independent");
        ((uint)reg.BeU32).Should().Be(0x33333333U, "BeU32 should be independent");
    }

    [Fact]
    public void Reg_Lsb_MaxValues()
    {
        Reg_Lsb reg = 0;
        reg.BeU16 = new UInt16Be(ushort.MaxValue);
        reg.LeU16 = new UInt16Le(ushort.MaxValue);
        reg.BeU32 = new UInt32Be(uint.MaxValue);

        ((ushort)reg.BeU16).Should().Be(ushort.MaxValue);
        ((ushort)reg.LeU16).Should().Be(ushort.MaxValue);
        ((uint)reg.BeU32).Should().Be(uint.MaxValue);
    }

    [Fact]
    public void Reg_Lsb_Overwrite_DoesNotCorruptNeighbor()
    {
        Reg_Lsb reg = 0;
        reg.BeU16 = new UInt16Be(0xFFFF);
        reg.LeU16 = new UInt16Le(0xFFFF);
        reg.BeU32 = new UInt32Be(0xFFFFFFFF);

        // Overwrite middle field only
        reg.LeU16 = new UInt16Le(0x0000);

        ((ushort)reg.BeU16).Should().Be(0xFFFF, "BeU16 should not be corrupted");
        ((ushort)reg.LeU16).Should().Be(0x0000, "LeU16 should be zeroed");
        ((uint)reg.BeU32).Should().Be(0xFFFFFFFF, "BeU32 should not be corrupted");
    }

    #endregion

    #region BitFields (MsbIsBitZero) with endian property types

    [Fact]
    public void Reg_Msb_BeU16_RoundTrip()
    {
        Reg_Msb reg = 0;
        reg.BeU16 = new UInt16Be(0x1234);

        ((ushort)reg.BeU16).Should().Be(0x1234);
    }

    [Fact]
    public void Reg_Msb_LeU16_RoundTrip()
    {
        Reg_Msb reg = 0;
        reg.LeU16 = new UInt16Le(0xABCD);

        ((ushort)reg.LeU16).Should().Be(0xABCD);
    }

    [Fact]
    public void Reg_Msb_BeU32_RoundTrip()
    {
        Reg_Msb reg = 0;
        reg.BeU32 = new UInt32Be(0xDEADBEEF);

        ((uint)reg.BeU32).Should().Be(0xDEADBEEF);
    }

    [Fact]
    public void Reg_Msb_AllFields_Independent()
    {
        Reg_Msb reg = 0;
        reg.BeU16 = new UInt16Be(0x1111);
        reg.LeU16 = new UInt16Le(0x2222);
        reg.BeU32 = new UInt32Be(0x33333333);

        ((ushort)reg.BeU16).Should().Be(0x1111, "BeU16 should be independent");
        ((ushort)reg.LeU16).Should().Be(0x2222, "LeU16 should be independent");
        ((uint)reg.BeU32).Should().Be(0x33333333U, "BeU32 should be independent");
    }

    [Fact]
    public void Reg_Msb_MaxValues()
    {
        Reg_Msb reg = 0;
        reg.BeU16 = new UInt16Be(ushort.MaxValue);
        reg.LeU16 = new UInt16Le(ushort.MaxValue);
        reg.BeU32 = new UInt32Be(uint.MaxValue);

        ((ushort)reg.BeU16).Should().Be(ushort.MaxValue);
        ((ushort)reg.LeU16).Should().Be(ushort.MaxValue);
        ((uint)reg.BeU32).Should().Be(uint.MaxValue);
    }

    [Fact]
    public void Reg_Msb_Overwrite_DoesNotCorruptNeighbor()
    {
        Reg_Msb reg = 0;
        reg.BeU16 = new UInt16Be(0xFFFF);
        reg.LeU16 = new UInt16Le(0xFFFF);
        reg.BeU32 = new UInt32Be(0xFFFFFFFF);

        reg.LeU16 = new UInt16Le(0x0000);

        ((ushort)reg.BeU16).Should().Be(0xFFFF, "BeU16 should not be corrupted");
        ((ushort)reg.LeU16).Should().Be(0x0000, "LeU16 should be zeroed");
        ((uint)reg.BeU32).Should().Be(0xFFFFFFFF, "BeU32 should not be corrupted");
    }

    #endregion

    #region BitFields: LsbIsBitZero vs MsbIsBitZero produce same logical values

    [Fact]
    public void Reg_Lsb_And_Msb_SameLogicalValues()
    {
        // Both bit orderings should produce the same logical values
        // for endian-typed properties, just stored at different bit positions
        Reg_Lsb lsb = 0;
        Reg_Msb msb = 0;

        lsb.BeU16 = new UInt16Be(0xCAFE);
        msb.BeU16 = new UInt16Be(0xCAFE);

        lsb.LeU16 = new UInt16Le(0xBEEF);
        msb.LeU16 = new UInt16Le(0xBEEF);

        lsb.BeU32 = new UInt32Be(0x12345678);
        msb.BeU32 = new UInt32Be(0x12345678);

        // Logical values must match
        ((ushort)lsb.BeU16).Should().Be((ushort)msb.BeU16);
        ((ushort)lsb.LeU16).Should().Be((ushort)msb.LeU16);
        ((uint)lsb.BeU32).Should().Be((uint)msb.BeU32);
    }

    #endregion

    // ================================================================
    // BitFieldsView: native field vs same-endian explicit type
    // must produce identical wire bytes for all 4 combos
    // ================================================================

    #region Same-endian explicit types match native types

    /// <summary>
    /// BE struct with explicit UInt16Be field: should produce same wire bytes
    /// as a plain ushort in the same BE struct.
    /// </summary>
    [BitFieldsView(ByteOrder.BigEndian, BitOrder.MsbIsBitZero)]
    public partial record struct SameEndian_BE_Msb
    {
        [BitField(0, 15)]  public partial ushort    NativeU16 { get; set; }
        [BitField(16, 31)] public partial UInt16Be  ExplicitU16 { get; set; }
    }

    [BitFieldsView(ByteOrder.BigEndian, BitOrder.LsbIsBitZero)]
    public partial record struct SameEndian_BE_Lsb
    {
        [BitField(0, 15)]  public partial ushort    NativeU16 { get; set; }
        [BitField(16, 31)] public partial UInt16Be  ExplicitU16 { get; set; }
    }

    [BitFieldsView(ByteOrder.LittleEndian, BitOrder.LsbIsBitZero)]
    public partial record struct SameEndian_LE_Lsb
    {
        [BitField(0, 15)]  public partial ushort    NativeU16 { get; set; }
        [BitField(16, 31)] public partial UInt16Le  ExplicitU16 { get; set; }
    }

    [BitFieldsView(ByteOrder.LittleEndian, BitOrder.MsbIsBitZero)]
    public partial record struct SameEndian_LE_Msb
    {
        [BitField(0, 15)]  public partial ushort    NativeU16 { get; set; }
        [BitField(16, 31)] public partial UInt16Le  ExplicitU16 { get; set; }
    }

    [Fact]
    public void SameEndian_BE_Msb_ExplicitMatchesNative()
    {
        var data = new byte[SameEndian_BE_Msb.SizeInBytes];
        var view = new SameEndian_BE_Msb(data);

        view.NativeU16 = 0xABCD;
        view.ExplicitU16 = new UInt16Be(0xABCD);

        // Both should produce [0xAB, 0xCD]
        data[0].Should().Be(data[2], "native and explicit BE should match byte 0");
        data[1].Should().Be(data[3], "native and explicit BE should match byte 1");
    }

    [Fact]
    public void SameEndian_BE_Lsb_ExplicitMatchesNative()
    {
        var data = new byte[SameEndian_BE_Lsb.SizeInBytes];
        var view = new SameEndian_BE_Lsb(data);

        view.NativeU16 = 0xABCD;
        view.ExplicitU16 = new UInt16Be(0xABCD);

        data[0].Should().Be(data[2], "native and explicit BE should match byte 0");
        data[1].Should().Be(data[3], "native and explicit BE should match byte 1");
    }

    [Fact]
    public void SameEndian_LE_Lsb_ExplicitMatchesNative()
    {
        var data = new byte[SameEndian_LE_Lsb.SizeInBytes];
        var view = new SameEndian_LE_Lsb(data);

        view.NativeU16 = 0xABCD;
        view.ExplicitU16 = new UInt16Le(0xABCD);

        data[0].Should().Be(data[2], "native and explicit LE should match byte 0");
        data[1].Should().Be(data[3], "native and explicit LE should match byte 1");
    }

    [Fact]
    public void SameEndian_LE_Msb_ExplicitMatchesNative()
    {
        var data = new byte[SameEndian_LE_Msb.SizeInBytes];
        var view = new SameEndian_LE_Msb(data);

        view.NativeU16 = 0xABCD;
        view.ExplicitU16 = new UInt16Le(0xABCD);

        data[0].Should().Be(data[2], "native and explicit LE should match byte 0");
        data[1].Should().Be(data[3], "native and explicit LE should match byte 1");
    }

    #endregion

    // ================================================================
    // Parse pre-built buffers: verify reads from known byte patterns
    // ================================================================

    #region Parse pre-built buffers

    [Fact]
    public void View_BE_Msb_ParseKnownBytes()
    {
        // Hand-craft bytes: native BE + LE overrides
        var data = new byte[View_BE_Msb.SizeInBytes];
        data[0] = 0xCA; data[1] = 0xFE;                                       // NativeU16 = 0xCAFE (BE)
        data[2] = 0x78; data[3] = 0x56; data[4] = 0x34; data[5] = 0x12;       // LeU32 = 0x12345678 (LE)
        data[6] = 0xFE; data[7] = 0xFF;                                       // LeS16 = -2 (LE)
        data[8] = 0x08; data[9] = 0x07; data[10] = 0x06; data[11] = 0x05;
        data[12] = 0x04; data[13] = 0x03; data[14] = 0x02; data[15] = 0x01;   // LeU64 = 0x0102030405060708 (LE)

        var view = new View_BE_Msb(data);

        view.NativeU16.Should().Be(0xCAFE);
        ((uint)view.LeU32).Should().Be(0x12345678U);
        ((short)view.LeS16).Should().Be(-2);
        ((ulong)view.LeU64).Should().Be(0x0102030405060708UL);
    }

    [Fact]
    public void View_LE_Lsb_ParseKnownBytes()
    {
        // Hand-craft bytes: native LE + BE overrides
        var data = new byte[View_LE_Lsb.SizeInBytes];
        data[0] = 0xFE; data[1] = 0xCA;                                       // NativeU16 = 0xCAFE (LE)
        data[2] = 0xDE; data[3] = 0xAD; data[4] = 0xBE; data[5] = 0xEF;      // BeU32 = 0xDEADBEEF (BE)
        data[6] = 0xFF; data[7] = 0xFE;                                       // BeS16 = -2 (BE)
        data[8] = 0x01; data[9] = 0x02; data[10] = 0x03; data[11] = 0x04;
        data[12] = 0x05; data[13] = 0x06; data[14] = 0x07; data[15] = 0x08;   // BeU64 = 0x0102030405060708 (BE)

        var view = new View_LE_Lsb(data);

        view.NativeU16.Should().Be(0xCAFE);
        ((uint)view.BeU32).Should().Be(0xDEADBEEFU);
        ((short)view.BeS16).Should().Be(-2);
        ((ulong)view.BeU64).Should().Be(0x0102030405060708UL);
    }

    [Fact]
    public void View_LE_Msb_ParseKnownBytes()
    {
        var data = new byte[View_LE_Msb.SizeInBytes];
        data[0] = 0xFE; data[1] = 0xCA;                                       // NativeU16 = 0xCAFE (LE)
        data[2] = 0xDE; data[3] = 0xAD; data[4] = 0xBE; data[5] = 0xEF;      // BeU32 = 0xDEADBEEF (BE)
        data[6] = 0xFF; data[7] = 0xFE;                                       // BeS16 = -2 (BE)
        data[8] = 0x01; data[9] = 0x02; data[10] = 0x03; data[11] = 0x04;
        data[12] = 0x05; data[13] = 0x06; data[14] = 0x07; data[15] = 0x08;   // BeU64 = 0x0102030405060708 (BE)

        var view = new View_LE_Msb(data);

        view.NativeU16.Should().Be(0xCAFE);
        ((uint)view.BeU32).Should().Be(0xDEADBEEFU);
        ((short)view.BeS16).Should().Be(-2);
        ((ulong)view.BeU64).Should().Be(0x0102030405060708UL);
    }

    [Fact]
    public void View_BE_Lsb_ParseKnownBytes()
    {
        var data = new byte[View_BE_Lsb.SizeInBytes];
        data[0] = 0xCA; data[1] = 0xFE;                                       // NativeU16 = 0xCAFE (BE)
        data[2] = 0x78; data[3] = 0x56; data[4] = 0x34; data[5] = 0x12;       // LeU32 = 0x12345678 (LE)
        data[6] = 0xFE; data[7] = 0xFF;                                       // LeS16 = -2 (LE)
        data[8] = 0x08; data[9] = 0x07; data[10] = 0x06; data[11] = 0x05;
        data[12] = 0x04; data[13] = 0x03; data[14] = 0x02; data[15] = 0x01;   // LeU64 = 0x0102030405060708 (LE)

        var view = new View_BE_Lsb(data);

        view.NativeU16.Should().Be(0xCAFE);
        ((uint)view.LeU32).Should().Be(0x12345678U);
        ((short)view.LeS16).Should().Be(-2);
        ((ulong)view.LeU64).Should().Be(0x0102030405060708UL);
    }

    #endregion
}
