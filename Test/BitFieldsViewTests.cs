using System;
using System.Runtime.CompilerServices;
using System.Text;
using FluentAssertions;
using Stardust.Utilities.Protocols;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Unit tests for [BitFieldsView]-generated record structs.
/// Covers big-endian/MSB-first (network convention)
/// and little-endian/LSB-first (default, matches [BitFields]).
/// </summary>
public partial class BitFieldsViewTests
{
    #region Test Struct Definitions

    /// <summary>
    /// IPv6 header (first 4 bytes) - big-endian, MSB-first (network convention).
    /// RFC 2460 layout:
    ///   Bits 0-3:   Version (4 bits)
    ///   Bits 4-11:  Traffic Class (8 bits)
    ///   Bits 12-31: Flow Label (20 bits)
    /// </summary>
    [BitFieldsView(ByteOrder.BigEndian, BitOrder.MsbIsBitZero)]
    public partial record struct IPv6HeaderView
    {
        [BitField(0, 3)] public partial byte Version { get; set; }
        [BitField(4, 11)] public partial byte TrafficClass { get; set; }
        [BitField(12, 31)] public partial uint FlowLabel { get; set; }
    }

    /// <summary>
    /// Simple 1-byte view with flags - big-endian, MSB-first.
    /// Bit 0 = MSB of byte 0.
    /// </summary>
    [BitFieldsView(ByteOrder.BigEndian, BitOrder.MsbIsBitZero)]
    public partial record struct ByteFlagsView
    {
        [BitFlag(0)] public partial bool MsbFlag { get; set; }
        [BitFlag(7)] public partial bool LsbFlag { get; set; }
        [BitField(1, 4)] public partial byte Middle { get; set; }
    }

    /// <summary>
    /// Little-endian, LSB-first view (now the default).
    /// Matches the convention of [BitFields(typeof(byte))].
    /// </summary>
    [BitFieldsView]
    public partial record struct LsbByteView
    {
        [BitFlag(0)] public partial bool LsbFlag { get; set; }
        [BitFlag(7)] public partial bool MsbFlag { get; set; }
        [BitField(1, 4)] public partial byte Middle { get; set; }
    }

    /// <summary>
    /// 16-bit big-endian field spanning two bytes.
    /// </summary>
    [BitFieldsView(ByteOrder.BigEndian, BitOrder.MsbIsBitZero)]
    public partial record struct TwoByteView
    {
        [BitField(0, 15)] public partial ushort FullWord { get; set; }
        [BitField(0, 7)] public partial byte HighByte { get; set; }
        [BitField(8, 15)] public partial byte LowByte { get; set; }
    }

    /// <summary>
    /// Tests nesting a [BitFields] type inside a [BitFieldsView].
    /// The BitFields type has implicit conversion from its storage type,
    /// so the BitFieldsView generator's cast works.
    /// </summary>
    [BitFields(typeof(byte))]
    public partial struct EmbeddedFlags
    {
        [BitFlag(0)] public partial bool Active { get; set; }
        [BitFlag(1)] public partial bool Valid { get; set; }
        [BitField(4, 7)] public partial byte Code { get; set; }
    }

    [BitFieldsView]
    public partial record struct ViewWithEmbeddedBitFields
    {
        [BitField(0, 7)] public partial EmbeddedFlags Flags { get; set; }
        [BitField(8, 15)] public partial byte Payload { get; set; }
    }

    // ---- Composition test views ----

    /// <summary>
    /// ushort-backed BitFields embedded in a big-endian BitFieldsView.
    /// ProtocolHeader16 declares ByteOrder.LittleEndian (the default for [BitFields]),
    /// so the view generator uses LE serialization for this field even though
    /// the struct-level default is BE. This ensures the wire format matches
    /// what ProtocolHeader16.ReadFrom(span) expects.
    /// </summary>
    [BitFieldsView(ByteOrder.BigEndian, BitOrder.MsbIsBitZero)]
    public partial record struct BeViewWithLeHeader
    {
        [BitField(0, 15)] public partial ProtocolHeader16 Header { get; set; }
        [BitField(16, 23)] public partial byte Tag { get; set; }
    }

    /// <summary>
    /// Same ProtocolHeader16 embedded in a little-endian view.
    /// This is the "correct" pairing since ProtocolHeader16 uses LE internally.
    /// </summary>
    [BitFieldsView(ByteOrder.LittleEndian, BitOrder.LsbIsBitZero)]
    public partial record struct LeViewWithLeHeader
    {
        [BitField(0, 15)] public partial ProtocolHeader16 Header { get; set; }
        [BitField(16, 23)] public partial byte Tag { get; set; }
    }

    /// <summary>
    /// Byte-backed BitFields (StatusFlags) in a big-endian view.
    /// Single-byte fields are endianness-agnostic, so this always works.
    /// </summary>
    [BitFieldsView(ByteOrder.BigEndian, BitOrder.MsbIsBitZero)]
    public partial record struct BeViewWithByteFlags
    {
        [BitField(0, 7)] public partial StatusFlags Flags { get; set; }
        [BitField(8, 15)] public partial byte Payload { get; set; }
    }

    /// <summary>
    /// ushort-backed BitFields in a field that is TOO NARROW (only 8 bits).
    /// The cast (ProtocolHeader16)(ushort)(byte_value) truncates the upper byte.
    /// Writing a full ProtocolHeader16 and reading it back loses data.
    /// </summary>
    [BitFieldsView(ByteOrder.LittleEndian, BitOrder.LsbIsBitZero)]
    public partial record struct ViewWithTruncatedHeader
    {
        [BitField(0, 7)] public partial ProtocolHeader16 TruncatedHeader { get; set; }
        [BitField(8, 15)] public partial byte Other { get; set; }
    }

    /// <summary>
    /// Byte-backed BitFields (StatusFlags) in a WIDER field (16 bits).
    /// The upper 8 bits are always zero; the flags occupy only the low byte.
    /// </summary>
    [BitFieldsView(ByteOrder.BigEndian, BitOrder.MsbIsBitZero)]
    public partial record struct ViewWithWideFlags
    {
        [BitField(0, 15)] public partial StatusFlags WideFlags { get; set; }
        [BitField(16, 23)] public partial byte Payload { get; set; }
    }

    /// <summary>
    /// Tests [BitFields] with MsbIsBitZero bit ordering.
    /// </summary>
    [BitFields(typeof(byte), bitOrder: BitOrder.MsbIsBitZero)]
    public partial struct MsbIsBitZeroRegister
    {
        [BitField(0, 3)] public partial byte HighNibble { get; set; }
        [BitField(4, 7)] public partial byte LowNibble { get; set; }
        [BitFlag(0)] public partial bool MsbFlag { get; set; }
        [BitFlag(7)] public partial bool LsbFlag { get; set; }
    }

    // ---- Nested sub-view test types ----

    /// <summary>
    /// Small inner view for nesting tests (LSB-first, little-endian).
    /// </summary>
    [BitFieldsView]
    public partial record struct InnerView
    {
        [BitField(0, 7)] public partial byte FieldA { get; set; }
        [BitFlag(0)] public partial bool FlagA { get; set; }
    }

    /// <summary>
    /// Outer view with byte-aligned sub-view (byte offset only, bit offset = 0).
    /// InnerView at byte 2 (bit 16).
    /// </summary>
    [BitFieldsView]
    public partial record struct OuterByteAligned
    {
        [BitField(0, 7)] public partial byte Header { get; set; }
        [BitField(16, 23)] public partial InnerView Inner { get; set; }
    }

    /// <summary>
    /// Outer view with bit-only offset sub-view (byte offset = 0, bit offset = 4).
    /// InnerView starts at bit 4 within byte 0.
    /// </summary>
    [BitFieldsView]
    public partial record struct OuterBitOffset
    {
        [BitField(0, 3)] public partial byte LowNibble { get; set; }
        [BitField(4, 11)] public partial InnerView Inner { get; set; }
    }

    /// <summary>
    /// Outer view with byte+bit offset sub-view (byte offset = 1, bit offset = 4).
    /// InnerView starts at bit 12 = byte 1, bit 4.
    /// </summary>
    [BitFieldsView]
    public partial record struct OuterBytePlusBit
    {
        [BitField(0, 7)] public partial byte FirstByte { get; set; }
        [BitField(12, 19)] public partial InnerView Inner { get; set; }
    }

    /// <summary>
    /// Big-endian MSB-first outer with byte-aligned sub-view.
    /// </summary>
    [BitFieldsView(ByteOrder.BigEndian, BitOrder.MsbIsBitZero)]
    public partial record struct OuterBigEndian
    {
        [BitField(0, 7)] public partial byte Header { get; set; }
        [BitField(16, 23)] public partial InnerViewBE Inner { get; set; }
    }

    [BitFieldsView(ByteOrder.BigEndian, BitOrder.MsbIsBitZero)]
    public partial record struct InnerViewBE
    {
        [BitField(0, 7)] public partial byte Value { get; set; }
        [BitFlag(0)] public partial bool TopBit { get; set; }
    }

    // ---- Multi-type endian test views ----

    /// <summary>
    /// LE struct with a BE field: UInt32Be property type forces big-endian read/write
    /// for that field even though the struct default is little-endian.
    /// </summary>
    [BitFieldsView(ByteOrder.LittleEndian, BitOrder.LsbIsBitZero)]
    public partial record struct MixedEndianView
    {
        [BitField(0, 15)]  public partial ushort LeField { get; set; }
        [BitField(16, 47)] public partial UInt32Be BeField { get; set; }
        [BitField(48, 63)] public partial UInt16Le ExplicitLeField { get; set; }
    }

    /// <summary>
    /// BE struct with a LE field: UInt32Le property type forces little-endian read/write.
    /// </summary>
    [BitFieldsView(ByteOrder.BigEndian, BitOrder.MsbIsBitZero)]
    public partial record struct MixedEndianBEView
    {
        [BitField(0, 15)]  public partial ushort BeField { get; set; }
        [BitField(16, 47)] public partial UInt32Le LeField { get; set; }
    }

    /// <summary>
    /// LE struct where every field uses an explicit LE endian-aware type.
    /// Should produce identical wire format to using plain native types
    /// in a LE struct (the override matches the default).
    /// </summary>
    [BitFieldsView(ByteOrder.LittleEndian, BitOrder.LsbIsBitZero)]
    public partial record struct ExplicitSameEndianLeView
    {
        [BitField(0, 15)]   public partial UInt16Le U16 { get; set; }
        [BitField(16, 31)]  public partial Int16Le  S16 { get; set; }
        [BitField(32, 63)]  public partial UInt32Le U32 { get; set; }
        [BitField(64, 95)]  public partial Int32Le  S32 { get; set; }
        [BitField(96, 159)] public partial UInt64Le U64 { get; set; }
        [BitField(160, 223)] public partial Int64Le S64 { get; set; }
    }

    /// <summary>
    /// BE struct where every field uses an explicit BE endian-aware type.
    /// Should produce identical wire format to using plain native types
    /// in a BE struct (the override matches the default).
    /// </summary>
    [BitFieldsView(ByteOrder.BigEndian, BitOrder.MsbIsBitZero)]
    public partial record struct ExplicitSameEndianBeView
    {
        [BitField(0, 15)]    public partial UInt16Be U16 { get; set; }
        [BitField(16, 31)]   public partial Int16Be  S16 { get; set; }
        [BitField(32, 63)]   public partial UInt32Be U32 { get; set; }
        [BitField(64, 95)]   public partial Int32Be  S32 { get; set; }
        [BitField(96, 159)]  public partial UInt64Be U64 { get; set; }
        [BitField(160, 223)] public partial Int64Be  S64 { get; set; }
    }

    /// <summary>
    /// LE struct with all BE endian-aware types (every field overrides the default).
    /// </summary>
    [BitFieldsView(ByteOrder.LittleEndian, BitOrder.LsbIsBitZero)]
    public partial record struct AllBeInLeView
    {
        [BitField(0, 15)]    public partial UInt16Be U16 { get; set; }
        [BitField(16, 31)]   public partial Int16Be  S16 { get; set; }
        [BitField(32, 63)]   public partial UInt32Be U32 { get; set; }
        [BitField(64, 95)]   public partial Int32Be  S32 { get; set; }
        [BitField(96, 159)]  public partial UInt64Be U64 { get; set; }
        [BitField(160, 223)] public partial Int64Be  S64 { get; set; }
    }

    /// <summary>
    /// Big-endian view with all supported multi-byte property types.
    /// Each field is at a byte-aligned position, full-width for its type.
    /// Total: 2+2+4+4+8+8 = 28 bytes (224 bits).
    /// </summary>
    [BitFieldsView(ByteOrder.BigEndian, BitOrder.MsbIsBitZero)]
    public partial record struct MultiTypeBigEndianView
    {
        [BitField(0, 15)]    public partial ushort UShortField { get; set; }
        [BitField(16, 31)]   public partial short ShortField { get; set; }
        [BitField(32, 63)]   public partial uint UIntField { get; set; }
        [BitField(64, 95)]   public partial int IntField { get; set; }
        [BitField(96, 159)]  public partial ulong ULongField { get; set; }
        [BitField(160, 223)] public partial long LongField { get; set; }
    }

    /// <summary>
    /// Little-endian view with all supported multi-byte property types.
    /// Same layout as MultiTypeBigEndianView but with reversed byte order.
    /// </summary>
    [BitFieldsView(ByteOrder.LittleEndian, BitOrder.LsbIsBitZero)]
    public partial record struct MultiTypeLittleEndianView
    {
        [BitField(0, 15)]    public partial ushort UShortField { get; set; }
        [BitField(16, 31)]   public partial short ShortField { get; set; }
        [BitField(32, 63)]   public partial uint UIntField { get; set; }
        [BitField(64, 95)]   public partial int IntField { get; set; }
        [BitField(96, 159)]  public partial ulong ULongField { get; set; }
        [BitField(160, 223)] public partial long LongField { get; set; }
    }

    // ---- Corner-case / abuse test views ----

    /// <summary>
    /// Narrow fields packed into a single byte for overflow and adjacency testing.
    /// LSB-first: bit 0 = physical bit 0.
    /// </summary>
    [BitFieldsView(ByteOrder.LittleEndian, BitOrder.LsbIsBitZero)]
    public partial record struct NarrowFieldsView
    {
        [BitField(0, 2)]  public partial byte ThreeBit { get; set; }  // max 7
        [BitField(3, 3)]  public partial byte OneBit { get; set; }    // max 1
        [BitField(4, 7)]  public partial byte FourBit { get; set; }   // max 15
    }

    /// <summary>
    /// MSB-first nibble view: four adjacent 4-bit fields packed into two bytes.
    /// Tests that writes to one nibble never bleed into neighbors.
    /// </summary>
    [BitFieldsView(ByteOrder.BigEndian, BitOrder.MsbIsBitZero)]
    public partial record struct NibbleView
    {
        [BitField(0, 3)]   public partial byte N0 { get; set; }
        [BitField(4, 7)]   public partial byte N1 { get; set; }
        [BitField(8, 11)]  public partial byte N2 { get; set; }
        [BitField(12, 15)] public partial byte N3 { get; set; }
    }

    /// <summary>
    /// Overlapping fields -- union-style access to the same bits.
    /// FullWord, HighByte, and LowByte all overlap intentionally.
    /// </summary>
    [BitFieldsView(ByteOrder.BigEndian, BitOrder.MsbIsBitZero)]
    public partial record struct OverlappingFieldsView
    {
        [BitField(0, 15)] public partial ushort FullWord { get; set; }
        [BitField(0, 7)]  public partial byte HighByte { get; set; }
        [BitField(8, 15)] public partial byte LowByte { get; set; }
    }

    /// <summary>
    /// Single-bit field typed as byte (not bool). Tests minimum-width field access.
    /// </summary>
    [BitFieldsView]
    public partial record struct SingleBitByteView
    {
        [BitField(0, 0)] public partial byte Bit0 { get; set; }
        [BitField(1, 1)] public partial byte Bit1 { get; set; }
        [BitField(2, 7)] public partial byte Rest { get; set; }
    }

    /// <summary>
    /// Signed property type on a narrow field -- documents that no sign extension occurs.
    /// An 8-bit field returning short will yield 0..255, never negative.
    /// </summary>
    [BitFieldsView(ByteOrder.BigEndian, BitOrder.MsbIsBitZero)]
    public partial record struct SignedNarrowView
    {
        [BitField(0, 7)]  public partial short Narrow8 { get; set; }  // 8 bits in a 16-bit type
        [BitField(8, 23)] public partial short Full16 { get; set; }   // 16 bits -- sign works here
    }

    /// <summary>
    /// UInt32Be on a 16-bit field: wider endian-aware type on a narrower bit span.
    /// The value is truncated to 16 bits on write; read returns only 16 bits cast to UInt32Be.
    /// </summary>
    [BitFieldsView(ByteOrder.LittleEndian, BitOrder.LsbIsBitZero)]
    public partial record struct EndianWidthMismatchView
    {
        [BitField(0, 15)] public partial UInt32Be WideTypeNarrowField { get; set; }
        [BitField(16, 31)] public partial ushort Neighbor { get; set; }
    }

    /// <summary>
    /// Full 64-bit field to test mask == ulong.MaxValue edge case.
    /// </summary>
    [BitFieldsView(ByteOrder.BigEndian, BitOrder.MsbIsBitZero)]
    public partial record struct Full64BitView
    {
        [BitField(0, 63)] public partial ulong Value { get; set; }
    }

    /// <summary>
    /// Field at a high bit position, requiring a large buffer.
    /// Tests SizeInBytes computation for distant fields.
    /// </summary>
    [BitFieldsView(ByteOrder.BigEndian, BitOrder.MsbIsBitZero)]
    public partial record struct DistantFieldView
    {
        [BitField(0, 7)]     public partial byte First { get; set; }
        [BitField(800, 815)] public partial ushort Distant { get; set; }
    }

    // ---- Protocol nesting view ----

    /// <summary>
    /// Demonstrates nested sub-views: an IPv4 packet containing a UDP datagram.
    /// Assumes standard 20-byte IPv4 header (IHL=5, no options).
    /// </summary>
    [BitFieldsView(ByteOrder.NetworkEndian, BitOrder.MsbIsBitZero)]
    public partial record struct IPv4UdpPacketView
    {
        [BitField(0, 159)]   public partial IPv4HeaderView Ip { get; set; }
        [BitField(160, 223)] public partial UdpHeaderView Udp { get; set; }
    }

    #endregion

    #region Construction Tests

    [Fact]
    public void IPv6HeaderView_Constructor_ByteArray()
    {
        var data = new byte[40]; // IPv6 header is 40 bytes
        var view = new IPv6HeaderView(data);
        view.Data.Length.Should().Be(40);
    }

    [Fact]
    public void IPv6HeaderView_Constructor_Memory()
    {
        var data = new byte[40];
        var view = new IPv6HeaderView(data.AsMemory());
        view.Data.Length.Should().Be(40);
    }

    [Fact]
    public void IPv6HeaderView_Constructor_WithOffset()
    {
        var data = new byte[100];
        var view = new IPv6HeaderView(data, 20);
        view.Data.Length.Should().Be(80);
    }

    [Fact]
    public void IPv6HeaderView_Constructor_TooShort_Throws()
    {
        var data = new byte[2];
        var act = () => new IPv6HeaderView(data);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IPv6HeaderView_SizeInBytes()
    {
        IPv6HeaderView.SizeInBytes.Should().BeGreaterThanOrEqualTo(4, "fields span bits 0-31, reads may extend further");
    }

    #endregion

    #region IPv6 Header: Big-Endian MSB-First Field Access

    [Fact]
    public void IPv6_Version_Get()
    {
        // IPv6: version = 6, stored in top nibble of byte 0
        // Byte 0 = 0x60 (version=6, traffic class high nibble=0)
        var data = new byte[] { 0x60, 0x00, 0x00, 0x00, 0x00 };
        var view = new IPv6HeaderView(data);
        view.Version.Should().Be(6);
    }

    [Fact]
    public void IPv6_Version_Set()
    {
        var data = new byte[5];
        var view = new IPv6HeaderView(data);
        view.Version = 6;
        data[0].Should().Be(0x60, "version 6 goes in top nibble");
    }

    [Fact]
    public void IPv6_TrafficClass_Get()
    {
        // Traffic class spans bits 4-11: lower nibble of byte 0 + upper nibble of byte 1
        // TC = 0xAB: byte 0 low nibble = 0xA, byte 1 high nibble = 0xB
        // Byte 0 = 0x_A (version=0, tc_high=A), Byte 1 = 0xB_ (tc_low=B)
        var data = new byte[] { 0x0A, 0xB0, 0x00, 0x00, 0x00 };
        var view = new IPv6HeaderView(data);
        view.TrafficClass.Should().Be(0xAB);
    }

    [Fact]
    public void IPv6_TrafficClass_Set()
    {
        var data = new byte[] { 0x60, 0x00, 0x00, 0x00, 0x00 }; // version=6
        var view = new IPv6HeaderView(data);
        view.TrafficClass = 0xFF;
        // TC occupies bits 4-11: lower nibble of byte 0 + upper nibble of byte 1
        (data[0] & 0x0F).Should().Be(0x0F, "TC high nibble in byte 0 low nibble");
        (data[1] & 0xF0).Should().Be(0xF0, "TC low nibble in byte 1 high nibble");
        // Version should be preserved
        (data[0] >> 4).Should().Be(6, "version preserved");
    }

    [Fact]
    public void IPv6_FlowLabel_Get()
    {
        // Flow label spans bits 12-31: lower nibble of byte 1 + bytes 2-3
        // FL = 0xABCDE: byte 1 low nibble = 0xA, byte 2 = 0xBC, byte 3 = 0xDE
        var data = new byte[] { 0x00, 0x0A, 0xBC, 0xDE, 0x00 };
        var view = new IPv6HeaderView(data);
        view.FlowLabel.Should().Be(0xABCDE);
    }

    [Fact]
    public void IPv6_FlowLabel_Set()
    {
        var data = new byte[] { 0x6F, 0xF0, 0x00, 0x00, 0x00 }; // version=6, TC=0xFF
        var view = new IPv6HeaderView(data);
        view.FlowLabel = 0x12345;
        data[3].Should().Be(0x45);
        data[2].Should().Be(0x23);
        (data[1] & 0x0F).Should().Be(0x01, "flow label high nibble");
        (data[1] & 0xF0).Should().Be(0xF0, "TC low bits preserved");
    }

    [Fact]
    public void IPv6_AllFields_RoundTrip()
    {
        var data = new byte[IPv6HeaderView.SizeInBytes];
        var view = new IPv6HeaderView(data);

        view.Version = 6;
        view.TrafficClass = 0xAB;
        view.FlowLabel = 0xCDEF0;

        view.Version.Should().Be(6);
        view.TrafficClass.Should().Be(0xAB);
        view.FlowLabel.Should().Be(0xCDEF0);
    }

    #endregion

    #region Zero-Copy Verification

    [Fact]
    public void View_Writes_To_Original_Buffer()
    {
        var data = new byte[IPv6HeaderView.SizeInBytes];
        var view = new IPv6HeaderView(data);
        view.Version = 6;

        // The original buffer should be modified directly
        data[0].Should().Be(0x60);
    }

    [Fact]
    public void View_Reads_From_Original_Buffer()
    {
        var data = new byte[IPv6HeaderView.SizeInBytes];
        data[0] = 0x60; // version = 6
        var view = new IPv6HeaderView(data);
        view.Version.Should().Be(6);

        // Modify buffer externally
        data[0] = 0x40; // version = 4
        view.Version.Should().Be(4, "view reads live data");
    }

    [Fact]
    public void Two_Views_Same_Buffer_See_Same_Data()
    {
        var data = new byte[IPv6HeaderView.SizeInBytes];
        var view1 = new IPv6HeaderView(data);
        var view2 = new IPv6HeaderView(data);

        view1.Version = 6;
        view2.Version.Should().Be(6, "both views see the same buffer");
    }

    #endregion

    #region MSB-First Flag Tests

    [Fact]
    public void MsbIsBitZero_Flag0_Is_MSB()
    {
        // Bit 0 = MSB of byte 0 = 0x80
        var data = new byte[] { 0x80 };
        var view = new ByteFlagsView(data);
        view.MsbFlag.Should().BeTrue();
        view.LsbFlag.Should().BeFalse();
    }

    [Fact]
    public void MsbIsBitZero_Flag7_Is_LSB()
    {
        // Bit 7 = LSB of byte 0 = 0x01
        var data = new byte[] { 0x01 };
        var view = new ByteFlagsView(data);
        view.MsbFlag.Should().BeFalse();
        view.LsbFlag.Should().BeTrue();
    }

    [Fact]
    public void MsbIsBitZero_SetFlags()
    {
        var data = new byte[1];
        var view = new ByteFlagsView(data);
        view.MsbFlag = true;
        data[0].Should().Be(0x80);
        view.LsbFlag = true;
        data[0].Should().Be(0x81);
    }

    [Fact]
    public void MsbIsBitZero_MiddleField()
    {
        // Bits 1-4 in MSB-first = bits 6-3 from LSB in byte 0
        // Value 0xF (all 4 bits set) => byte = 0x78 (0111_1000)
        var data = new byte[] { 0x78 };
        var view = new ByteFlagsView(data);
        view.Middle.Should().Be(0x0F);
    }

    #endregion

    #region LSB-First Tests (Hardware Convention)

    [Fact]
    public void LsbIsBitZero_Flag0_Is_LSB()
    {
        // Bit 0 = LSB of byte 0 = 0x01
        var data = new byte[] { 0x01 };
        var view = new LsbByteView(data);
        view.LsbFlag.Should().BeTrue();
        view.MsbFlag.Should().BeFalse();
    }

    [Fact]
    public void LsbIsBitZero_Flag7_Is_MSB()
    {
        // Bit 7 = MSB of byte 0 = 0x80
        var data = new byte[] { 0x80 };
        var view = new LsbByteView(data);
        view.LsbFlag.Should().BeFalse();
        view.MsbFlag.Should().BeTrue();
    }

    [Fact]
    public void LsbIsBitZero_MiddleField()
    {
        // Bits 1-4 in LSB-first = bits 1-4 from LSB in byte 0
        // Value 0xF (all 4 bits set) => byte = 0x1E (0001_1110)
        var data = new byte[] { 0x1E };
        var view = new LsbByteView(data);
        view.Middle.Should().Be(0x0F);
    }

    #endregion

    #region TwoByteView: Multi-Byte Big-Endian Access

    [Fact]
    public void TwoByte_FullWord_BigEndian()
    {
        var data = new byte[] { 0xAB, 0xCD };
        var view = new TwoByteView(data);
        view.FullWord.Should().Be(0xABCD);
    }

    [Fact]
    public void TwoByte_HighByte()
    {
        var data = new byte[] { 0xAB, 0xCD };
        var view = new TwoByteView(data);
        view.HighByte.Should().Be(0xAB);
    }

    [Fact]
    public void TwoByte_LowByte()
    {
        var data = new byte[] { 0xAB, 0xCD };
        var view = new TwoByteView(data);
        view.LowByte.Should().Be(0xCD);
    }

    [Fact]
    public void TwoByte_SetFullWord()
    {
        var data = new byte[2];
        var view = new TwoByteView(data);
        view.FullWord = 0x1234;
        data[0].Should().Be(0x12);
        data[1].Should().Be(0x34);
    }

    #endregion

    #region Record Struct Equality

    [Fact]
    public void SameBuffer_Views_Are_Equal()
    {
        var data = new byte[IPv6HeaderView.SizeInBytes];
        var view1 = new IPv6HeaderView(data);
        var view2 = new IPv6HeaderView(data);
        (view1 == view2).Should().BeTrue();
    }

    [Fact]
    public void DifferentBuffer_Views_Are_NotEqual()
    {
        var data1 = new byte[IPv6HeaderView.SizeInBytes];
        var data2 = new byte[IPv6HeaderView.SizeInBytes];
        var view1 = new IPv6HeaderView(data1);
        var view2 = new IPv6HeaderView(data2);
        (view1 == view2).Should().BeFalse("different backing arrays");
    }

    #endregion

    #region BitFields MsbIsBitZero Tests

    [Fact]
    public void BitFields_MsbIsBitZero_HighNibble()
    {
        // [BitField(0, 3)] with MsbIsBitZero means top 4 bits
        // Value 0xA0 => HighNibble = 0x0A
        MsbIsBitZeroRegister reg = 0xA0;
        reg.HighNibble.Should().Be(0x0A);
    }

    [Fact]
    public void BitFields_MsbIsBitZero_LowNibble()
    {
        // [BitField(4, 7)] with MsbIsBitZero means bottom 4 bits
        MsbIsBitZeroRegister reg = 0x05;
        reg.LowNibble.Should().Be(0x05);
    }

    [Fact]
    public void BitFields_MsbIsBitZero_SetHighNibble()
    {
        MsbIsBitZeroRegister reg = 0;
        reg.HighNibble = 0x0F;
        ((byte)reg).Should().Be(0xF0);
    }

    [Fact]
    public void BitFields_MsbIsBitZero_SetLowNibble()
    {
        MsbIsBitZeroRegister reg = 0;
        reg.LowNibble = 0x0A;
        ((byte)reg).Should().Be(0x0A);
    }

    [Fact]
    public void BitFields_MsbIsBitZero_MsbFlag()
    {
        // [BitFlag(0)] with MsbIsBitZero = MSB = bit 7 in physical terms = 0x80
        MsbIsBitZeroRegister reg = 0x80;
        reg.MsbFlag.Should().BeTrue();
        reg.LsbFlag.Should().BeFalse();
    }

    [Fact]
    public void BitFields_MsbIsBitZero_LsbFlag()
    {
        // [BitFlag(7)] with MsbIsBitZero = LSB = bit 0 in physical terms = 0x01
        MsbIsBitZeroRegister reg = 0x01;
        reg.MsbFlag.Should().BeFalse();
        reg.LsbFlag.Should().BeTrue();
    }

    [Fact]
    public void BitFields_MsbIsBitZero_RoundTrip()
    {
        MsbIsBitZeroRegister reg = 0;
        reg.HighNibble = 0x0C;
        reg.LowNibble = 0x03;
        reg.HighNibble.Should().Be(0x0C);
        reg.LowNibble.Should().Be(0x03);
        ((byte)reg).Should().Be(0xC3);
    }

    #endregion

    #region Nesting: BitFields inside BitFieldsView

    [Fact]
    public void Nested_BitFields_In_View_Get()
    {
        // EmbeddedFlags is [BitFields(typeof(byte))] with Active=bit0, Valid=bit1, Code=bits 4-7
        // ViewWithEmbeddedBitFields maps bits 0-7 (LSB-first) => byte 0 in little-endian
        var data = new byte[] { 0x53, 0xAB }; // Flags=0x53, Payload=0xAB
        var view = new ViewWithEmbeddedBitFields(data);

        EmbeddedFlags flags = view.Flags;
        flags.Active.Should().BeTrue("bit 0 of 0x53 is set");
        flags.Valid.Should().BeTrue("bit 1 of 0x53 is set");
        flags.Code.Should().Be(5, "bits 4-7 of 0x53 = 0101 = 5");
        view.Payload.Should().Be(0xAB);
    }

    [Fact]
    public void Nested_BitFields_In_View_Set()
    {
        var data = new byte[2];
        var view = new ViewWithEmbeddedBitFields(data);

        var flags = new EmbeddedFlags();
        flags.Active = true;
        flags.Code = 0x0A;
        view.Flags = flags;
        view.Payload = 0xFF;

        data[0].Should().Be(0xA1, "Active=1, Code=0xA in bits 4-7");
        data[1].Should().Be(0xFF);
    }

    [Fact]
    public void Nested_BitFields_In_View_RoundTrip()
    {
        var data = new byte[2];
        var view = new ViewWithEmbeddedBitFields(data);

        var flags = new EmbeddedFlags();
        flags.Active = true;
        flags.Valid = false;
        flags.Code = 7;
        view.Flags = flags;

        EmbeddedFlags readBack = view.Flags;
        readBack.Active.Should().BeTrue();
        readBack.Valid.Should().BeFalse();
        readBack.Code.Should().Be(7);
    }

    #endregion

    #region Nested Sub-View: Byte-Aligned (bit offset = 0)

    [Fact]
    public void SubView_ByteAligned_Get()
    {
        // InnerView at byte 2 (bit 16), byte-aligned
        var data = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
        var outer = new OuterByteAligned(data);

        outer.Header.Should().Be(0xAA);
        InnerView inner = outer.Inner;
        inner.FieldA.Should().Be(0xCC, "inner view reads from byte 2");
    }

    [Fact]
    public void SubView_ByteAligned_WriteThrough()
    {
        // Getter returns a view over the SAME buffer -- writes go through
        var data = new byte[4];
        var outer = new OuterByteAligned(data);

        InnerView inner = outer.Inner;
        inner.FieldA = 0x42;
        data[2].Should().Be(0x42, "write-through to underlying buffer");
    }

    [Fact]
    public void SubView_ByteAligned_Set()
    {
        var data = new byte[4];
        var outer = new OuterByteAligned(data);

        // Create an inner view from a separate buffer and assign it
        var srcBuf = new byte[] { 0xEE };
        var src = new InnerView(srcBuf);
        outer.Inner = src;
        data[2].Should().Be(0xEE, "setter copies bytes into outer buffer");
    }

    #endregion

    #region Nested Sub-View: Bit-Only Offset (byte 0, bit 4)

    [Fact]
    public void SubView_BitOffset_Get()
    {
        // InnerView at bit 4: FieldA (bits 0-7 of inner) maps to bits 4-11 of outer
        // bits 4-11 in LSB-first little-endian = low nibble of byte 0 + high nibble of byte 1
        // Value 0xAB: byte 0 = 0x_A (bits 4-7 = 0xA), byte 1 = 0xB_ (bits 8-11 = 0xB)
        // Actually in LSB-first: bits 4-7 of byte 0 = (byte0 >> 4) & 0x0F
        //                        bits 0-3 of byte 1 = byte1 & 0x0F
        // Combined in little-endian: byte1_low | (byte0_high << 4)... no.
        // Let me think carefully: InnerView field at bit 0, width 8, _bitOffset=4
        // Effective position = 0 + 4 = 4
        // For LSB-first LE: bi = 4 >> 3 = 0, sh = 4 & 7 = 4
        // Reads ushort LE from byte 0: raw = byte0 | (byte1 << 8)
        // Returns (raw >> 4) & 0xFF
        // For data = { 0xA0, 0x0B }: raw = 0x0BA0, >> 4 = 0x00BA, & 0xFF = 0xBA
        var data = new byte[] { 0xA0, 0x0B, 0x00 };
        var outer = new OuterBitOffset(data);

        InnerView inner = outer.Inner;
        inner.FieldA.Should().Be(0xBA);
    }

    [Fact]
    public void SubView_BitOffset_WriteThrough()
    {
        var data = new byte[3];
        var outer = new OuterBitOffset(data);

        InnerView inner = outer.Inner;
        inner.FieldA = 0xFF;
        // Effective pos 4, width 8: writes 0xFF into bits 4-11
        // ushort LE at byte 0: mask = 0xFF << 4 = 0x0FF0
        // Sets bits 4-11, clears nothing else (was all zeros)
        // byte 0: 0xF0 (bits 4-7 set), byte 1: 0x0F (bits 8-11 set)
        data[0].Should().Be(0xF0);
        data[1].Should().Be(0x0F);
    }

    [Fact]
    public void SubView_BitOffset_Flag()
    {
        var data = new byte[3];
        var outer = new OuterBitOffset(data);

        InnerView inner = outer.Inner;
        // FlagA is at inner bit 0, _bitOffset=4 => effective bit 4
        // LSB-first: byte 0, bit 4 = 0x10
        inner.FlagA = true;
        data[0].Should().Be(0x10);
    }

    [Fact]
    public void SubView_BitOffset_PreservesOuter()
    {
        // LowNibble at bits 0-3, InnerView at bits 4-11
        var data = new byte[3];
        var outer = new OuterBitOffset(data);

        outer.LowNibble = 0x05;  // bits 0-3
        InnerView inner = outer.Inner;
        inner.FieldA = 0xAB;     // bits 4-11

        outer.LowNibble.Should().Be(0x05, "low nibble preserved");
        outer.Inner.FieldA.Should().Be(0xAB, "inner field set correctly");
    }

    #endregion

    #region Nested Sub-View: Byte + Bit Offset (byte 1, bit 4)

    [Fact]
    public void SubView_BytePlusBit_Get()
    {
        // InnerView at bit 12 = byte 1, bit 4
        // FieldA at inner bit 0, _bitOffset=4 => effective bit 4 in sliced memory starting at byte 1
        // So reads bits 4-11 of byte 1 (and byte 2)
        // data[1] = 0xA0 -> bits 4-7 = 0xA; data[2] = 0x0B -> bits 0-3 = 0xB
        // raw = data[1] | (data[2] << 8) = 0x0BA0, >> 4 = 0xBA
        var data = new byte[] { 0x00, 0xA0, 0x0B, 0x00 };
        var outer = new OuterBytePlusBit(data);

        outer.Inner.FieldA.Should().Be(0xBA);
    }

    [Fact]
    public void SubView_BytePlusBit_WriteThrough()
    {
        var data = new byte[4];
        var outer = new OuterBytePlusBit(data);

        outer.FirstByte = 0x99;
        InnerView inner = outer.Inner;
        inner.FieldA = 0xFF;

        outer.FirstByte.Should().Be(0x99, "first byte preserved");
        data[1].Should().Be(0xF0, "bits 4-7 of byte 1 set");
        data[2].Should().Be(0x0F, "bits 0-3 of byte 2 set");
    }

    [Fact]
    public void SubView_BytePlusBit_RoundTrip()
    {
        var data = new byte[4];
        var outer = new OuterBytePlusBit(data);

        outer.FirstByte = 0x42;
        var inner = outer.Inner;  // view over same buffer
        inner.FieldA = 0xAB;     // writes through to data

        outer.FirstByte.Should().Be(0x42);
        outer.Inner.FieldA.Should().Be(0xAB);
    }

    #endregion

    #region Nested Sub-View: Big-Endian Byte-Aligned

    [Fact]
    public void SubView_BigEndian_ByteAligned_Get()
    {
        var data = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
        var outer = new OuterBigEndian(data);

        outer.Header.Should().Be(0xAA);
        outer.Inner.Value.Should().Be(0xCC);
    }

    [Fact]
    public void SubView_BigEndian_ByteAligned_WriteThrough()
    {
        var data = new byte[4];
        var outer = new OuterBigEndian(data);

        var inner = outer.Inner;
        inner.Value = 0x55;
        data[2].Should().Be(0x55);
    }

    [Fact]
    public void SubView_BigEndian_ByteAligned_Flag()
    {
        var data = new byte[4];
        var outer = new OuterBigEndian(data);

        // TopBit is bit 0 in MSB-first = 0x80
        var inner = outer.Inner;
        inner.TopBit = true;
        data[2].Should().Be(0x80);
    }

    #endregion

    #region Multi-Type Big-Endian Byte Order Tests

    [Fact]
    public void BigEndian_UShort_RoundTrip()
    {
        // 0xABCD big-endian = [0xAB, 0xCD]
        var data = new byte[] { 0xAB, 0xCD, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                0x00, 0x00, 0x00, 0x00 };
        var view = new MultiTypeBigEndianView(data);
        view.UShortField.Should().Be(0xABCD);

        Array.Clear(data);
        view.UShortField = 0x1234;
        data[0].Should().Be(0x12);
        data[1].Should().Be(0x34);
        view.UShortField.Should().Be(0x1234);
    }

    [Fact]
    public void BigEndian_Short_Positive_And_Negative()
    {
        var data = new byte[28];
        var view = new MultiTypeBigEndianView(data);

        // Positive: 1234 = 0x04D2
        view.ShortField = 1234;
        data[2].Should().Be(0x04);
        data[3].Should().Be(0xD2);
        view.ShortField.Should().Be(1234);

        // Negative: -1234 = 0xFB2E
        view.ShortField = -1234;
        data[2].Should().Be(0xFB);
        data[3].Should().Be(0x2E);
        view.ShortField.Should().Be(-1234);
    }

    [Fact]
    public void BigEndian_UInt_RoundTrip()
    {
        var data = new byte[28];
        var view = new MultiTypeBigEndianView(data);

        view.UIntField = 0x12345678;
        data[4].Should().Be(0x12);
        data[5].Should().Be(0x34);
        data[6].Should().Be(0x56);
        data[7].Should().Be(0x78);
        view.UIntField.Should().Be(0x12345678U);
    }

    [Fact]
    public void BigEndian_Int_Positive_And_Negative()
    {
        var data = new byte[28];
        var view = new MultiTypeBigEndianView(data);

        view.IntField = 100000;
        view.IntField.Should().Be(100000);

        // -100000 = 0xFFFE7960
        view.IntField = -100000;
        data[8].Should().Be(0xFF);
        data[9].Should().Be(0xFE);
        data[10].Should().Be(0x79);
        data[11].Should().Be(0x60);
        view.IntField.Should().Be(-100000);
    }

    [Fact]
    public void BigEndian_ULong_RoundTrip()
    {
        var data = new byte[28];
        var view = new MultiTypeBigEndianView(data);

        view.ULongField = 0x0102030405060708UL;
        data[12].Should().Be(0x01);
        data[13].Should().Be(0x02);
        data[14].Should().Be(0x03);
        data[15].Should().Be(0x04);
        data[16].Should().Be(0x05);
        data[17].Should().Be(0x06);
        data[18].Should().Be(0x07);
        data[19].Should().Be(0x08);
        view.ULongField.Should().Be(0x0102030405060708UL);
    }

    [Fact]
    public void BigEndian_Long_Positive_And_Negative()
    {
        var data = new byte[28];
        var view = new MultiTypeBigEndianView(data);

        view.LongField = long.MaxValue;
        view.LongField.Should().Be(long.MaxValue);

        view.LongField = -1L;
        for (int i = 20; i < 28; i++)
            data[i].Should().Be(0xFF);
        view.LongField.Should().Be(-1L);

        view.LongField = long.MinValue;
        data[20].Should().Be(0x80);
        for (int i = 21; i < 28; i++)
            data[i].Should().Be(0x00);
        view.LongField.Should().Be(long.MinValue);
    }

    [Fact]
    public void BigEndian_AllTypes_Coexist()
    {
        var data = new byte[28];
        var view = new MultiTypeBigEndianView(data);

        view.UShortField = 0xCAFE;
        view.ShortField = -1;
        view.UIntField = 0xDEADBEEF;
        view.IntField = int.MinValue;
        view.ULongField = ulong.MaxValue;
        view.LongField = -9999999999L;

        view.UShortField.Should().Be(0xCAFE);
        view.ShortField.Should().Be(-1);
        view.UIntField.Should().Be(0xDEADBEEF);
        view.IntField.Should().Be(int.MinValue);
        view.ULongField.Should().Be(ulong.MaxValue);
        view.LongField.Should().Be(-9999999999L);
    }

    #endregion

    #region Multi-Type Little-Endian Byte Order Tests

    [Fact]
    public void LittleEndian_UShort_RoundTrip()
    {
        // 0xABCD little-endian = [0xCD, 0xAB]
        var data = new byte[28];
        data[0] = 0xCD;
        data[1] = 0xAB;
        var view = new MultiTypeLittleEndianView(data);
        view.UShortField.Should().Be(0xABCD);

        Array.Clear(data);
        view.UShortField = 0x1234;
        data[0].Should().Be(0x34);
        data[1].Should().Be(0x12);
        view.UShortField.Should().Be(0x1234);
    }

    [Fact]
    public void LittleEndian_Short_Positive_And_Negative()
    {
        var data = new byte[28];
        var view = new MultiTypeLittleEndianView(data);

        view.ShortField = -1234;
        // -1234 = 0xFB2E little-endian = [0x2E, 0xFB]
        data[2].Should().Be(0x2E);
        data[3].Should().Be(0xFB);
        view.ShortField.Should().Be(-1234);
    }

    [Fact]
    public void LittleEndian_UInt_RoundTrip()
    {
        var data = new byte[28];
        var view = new MultiTypeLittleEndianView(data);

        view.UIntField = 0x12345678;
        // little-endian: LSB first
        data[4].Should().Be(0x78);
        data[5].Should().Be(0x56);
        data[6].Should().Be(0x34);
        data[7].Should().Be(0x12);
        view.UIntField.Should().Be(0x12345678U);
    }

    [Fact]
    public void LittleEndian_Int_Positive_And_Negative()
    {
        var data = new byte[28];
        var view = new MultiTypeLittleEndianView(data);

        // -100000 = 0xFFFE7960 LE = [0x60, 0x79, 0xFE, 0xFF]
        view.IntField = -100000;
        data[8].Should().Be(0x60);
        data[9].Should().Be(0x79);
        data[10].Should().Be(0xFE);
        data[11].Should().Be(0xFF);
        view.IntField.Should().Be(-100000);
    }

    [Fact]
    public void LittleEndian_ULong_RoundTrip()
    {
        var data = new byte[28];
        var view = new MultiTypeLittleEndianView(data);

        view.ULongField = 0x0102030405060708UL;
        // LE: LSB first
        data[12].Should().Be(0x08);
        data[13].Should().Be(0x07);
        data[14].Should().Be(0x06);
        data[15].Should().Be(0x05);
        data[16].Should().Be(0x04);
        data[17].Should().Be(0x03);
        data[18].Should().Be(0x02);
        data[19].Should().Be(0x01);
        view.ULongField.Should().Be(0x0102030405060708UL);
    }

    [Fact]
    public void LittleEndian_Long_Positive_And_Negative()
    {
        var data = new byte[28];
        var view = new MultiTypeLittleEndianView(data);

        view.LongField = long.MinValue;
        // 0x8000000000000000 LE = [0x00 x7, 0x80]
        for (int i = 20; i < 27; i++)
            data[i].Should().Be(0x00);
        data[27].Should().Be(0x80);
        view.LongField.Should().Be(long.MinValue);
    }

    [Fact]
    public void LittleEndian_AllTypes_Coexist()
    {
        var data = new byte[28];
        var view = new MultiTypeLittleEndianView(data);

        view.UShortField = 0xCAFE;
        view.ShortField = -1;
        view.UIntField = 0xDEADBEEF;
        view.IntField = int.MinValue;
        view.ULongField = ulong.MaxValue;
        view.LongField = -9999999999L;

        view.UShortField.Should().Be(0xCAFE);
        view.ShortField.Should().Be(-1);
        view.UIntField.Should().Be(0xDEADBEEF);
        view.IntField.Should().Be(int.MinValue);
        view.ULongField.Should().Be(ulong.MaxValue);
        view.LongField.Should().Be(-9999999999L);
    }

    [Fact]
    public void BigEndian_Vs_LittleEndian_SameValue_DifferentBytes()
    {
        var beData = new byte[28];
        var leData = new byte[28];
        var be = new MultiTypeBigEndianView(beData);
        var le = new MultiTypeLittleEndianView(leData);

        be.UIntField = 0x12345678;
        le.UIntField = 0x12345678;

        // Same logical value, opposite byte layout
        beData[4].Should().Be(0x12, "BE: MSB first");
        leData[4].Should().Be(0x78, "LE: LSB first");

        be.UIntField.Should().Be(le.UIntField, "same logical value regardless of endianness");
    }

    #endregion

    #region Canonical Protocol Headers

    [Fact]
    public void IPv4Header_SizeInBytes()
    {
        IPv4HeaderView.SizeInBytes.Should().Be(20);
    }

    [Fact]
    public void IPv4Header_RoundTrip()
    {
        var data = new byte[20];
        var ip = new IPv4HeaderView(data);

        ip.Version = 4;
        ip.Ihl = 5;
        ip.TotalLength = 60;
        ip.Identification = 0xABCD;
        ip.DontFragment = true;
        ip.TimeToLive = 64;
        ip.Protocol = 6; // TCP
        ip.SourceAddress = 0xC0A80101;      // 192.168.1.1
        ip.DestinationAddress = 0x0A000001; // 10.0.0.1

        ip.Version.Should().Be(4);
        ip.Ihl.Should().Be(5);
        ip.HeaderLengthBytes.Should().Be(20);
        ip.TotalLength.Should().Be(60);
        ip.Identification.Should().Be(0xABCD);
        ip.DontFragment.Should().BeTrue();
        ip.MoreFragments.Should().BeFalse();
        ip.TimeToLive.Should().Be(64);
        ip.Protocol.Should().Be(6);
        ip.SourceAddress.Should().Be(0xC0A80101);
        ip.DestinationAddress.Should().Be(0x0A000001);

        // Verify wire format: Version+IHL in first byte
        data[0].Should().Be(0x45, "Version=4, IHL=5 -> 0x45");
    }

    [Fact]
    public void IPv6Header_Full_RoundTrip()
    {
        var data = new byte[40];
        var ip = new IPv6FullHeaderView(data);

        ip.Version = 6;
        ip.TrafficClass = 0xAB;
        ip.FlowLabel = 0x12345;
        ip.PayloadLength = 1024;
        ip.NextHeader = 17; // UDP
        ip.HopLimit = 64;
        ip.SourceAddressHigh = 0xFE80000000000000;
        ip.SourceAddressLow = 0x0000000000000001;
        ip.DestinationAddressHigh = 0xFF02000000000000;
        ip.DestinationAddressLow = 0x00000000000000FB;

        ip.Version.Should().Be(6);
        ip.TrafficClass.Should().Be(0xAB);
        ip.FlowLabel.Should().Be(0x12345U);
        ip.PayloadLength.Should().Be(1024);
        ip.NextHeader.Should().Be(17);
        ip.HopLimit.Should().Be(64);
        ip.SourceAddressHigh.Should().Be(0xFE80000000000000);
        ip.SourceAddressLow.Should().Be(0x0000000000000001);
    }

    [Fact]
    public void UdpHeader_SizeInBytes()
    {
        UdpHeaderView.SizeInBytes.Should().Be(8);
    }

    [Fact]
    public void UdpHeader_RoundTrip()
    {
        var data = new byte[8];
        var udp = new UdpHeaderView(data);

        udp.SourcePort = 12345;
        udp.DestinationPort = 80;
        udp.Length = 21;
        udp.Checksum = 0xABCD;

        udp.SourcePort.Should().Be(12345);
        udp.DestinationPort.Should().Be(80);
        udp.Length.Should().Be(21);
        udp.Checksum.Should().Be(0xABCD);

        // Verify wire format: ports are big-endian
        // 12345 = 0x3039
        data[0].Should().Be(0x30);
        data[1].Should().Be(0x39);
        // 80 = 0x0050
        data[2].Should().Be(0x00);
        data[3].Should().Be(0x50);
    }

    [Fact]
    public void TcpHeader_SizeInBytes()
    {
        TcpHeaderView.SizeInBytes.Should().Be(20);
    }

    [Fact]
    public void TcpHeader_RoundTrip()
    {
        var data = new byte[20];
        var tcp = new TcpHeaderView(data);

        tcp.SourcePort = 49152;
        tcp.DestinationPort = 443;
        tcp.SequenceNumber = 1000;
        tcp.AcknowledgmentNumber = 2000;
        tcp.DataOffset = 5;
        tcp.SYN = true;
        tcp.ACK = true;
        tcp.WindowSize = 65535;

        tcp.SourcePort.Should().Be(49152);
        tcp.DestinationPort.Should().Be(443);
        tcp.SequenceNumber.Should().Be(1000U);
        tcp.AcknowledgmentNumber.Should().Be(2000U);
        tcp.DataOffset.Should().Be(5);
        tcp.HeaderLengthBytes.Should().Be(20);
        tcp.SYN.Should().BeTrue();
        tcp.ACK.Should().BeTrue();
        tcp.PSH.Should().BeFalse();
        tcp.FIN.Should().BeFalse();
        tcp.RST.Should().BeFalse();
        tcp.WindowSize.Should().Be(65535);
    }

    [Fact]
    public void TcpHeader_AllFlags()
    {
        var data = new byte[20];
        var tcp = new TcpHeaderView(data);

        tcp.NS = true;
        tcp.CWR = true;
        tcp.ECE = true;
        tcp.URG = true;
        tcp.ACK = true;
        tcp.PSH = true;
        tcp.RST = true;
        tcp.SYN = true;
        tcp.FIN = true;

        tcp.NS.Should().BeTrue();
        tcp.CWR.Should().BeTrue();
        tcp.ECE.Should().BeTrue();
        tcp.URG.Should().BeTrue();
        tcp.ACK.Should().BeTrue();
        tcp.PSH.Should().BeTrue();
        tcp.RST.Should().BeTrue();
        tcp.SYN.Should().BeTrue();
        tcp.FIN.Should().BeTrue();

        // Byte 13 (flags CWR..FIN) should be 0xFF when all set
        data[13].Should().Be(0xFF);
    }

    #endregion

    #region Protocol Nesting: IPv4/UDP with "Hello, world!" payload

    [Fact]
    public void IPv4_Udp_HelloWorld_BuildAndParse()
    {
        // Build a complete IPv4/UDP packet carrying "Hello, world!"
        byte[] payload = Encoding.ASCII.GetBytes("Hello, world!");
        int udpLen = UdpHeaderView.SizeInBytes + payload.Length; // 8 + 13 = 21
        int totalLen = IPv4HeaderView.SizeInBytes + udpLen;       // 20 + 21 = 41
        var packet = new byte[totalLen];

        // --- Build the packet using views ---
        var ip = new IPv4HeaderView(packet);
        ip.Version = 4;
        ip.Ihl = 5;
        ip.TotalLength = (ushort)totalLen;
        ip.Identification = 0xBEEF;
        ip.DontFragment = true;
        ip.TimeToLive = 64;
        ip.Protocol = 17; // UDP
        ip.SourceAddress = 0xC0A80164;  // 192.168.1.100
        ip.DestinationAddress = 0x0A000001; // 10.0.0.1

        var udp = new UdpHeaderView(packet, IPv4HeaderView.SizeInBytes);
        udp.SourcePort = 12345;
        udp.DestinationPort = 53;
        udp.Length = (ushort)udpLen;

        // Write the payload after the UDP header
        int payloadOffset = IPv4HeaderView.SizeInBytes + UdpHeaderView.SizeInBytes;
        payload.CopyTo(packet, payloadOffset);

        // --- Parse the packet back using offset-based views ---
        var ip2 = new IPv4HeaderView(packet);
        ip2.Version.Should().Be(4);
        ip2.Ihl.Should().Be(5);
        ip2.TotalLength.Should().Be(41);
        ip2.DontFragment.Should().BeTrue();
        ip2.TimeToLive.Should().Be(64);
        ip2.Protocol.Should().Be(17, "UDP");
        ip2.SourceAddress.Should().Be(0xC0A80164);
        ip2.DestinationAddress.Should().Be(0x0A000001);

        int ipHeaderLen = ip2.HeaderLengthBytes;
        var udp2 = new UdpHeaderView(packet, ipHeaderLen);
        udp2.SourcePort.Should().Be(12345);
        udp2.DestinationPort.Should().Be(53);
        udp2.Length.Should().Be(21);

        // Extract and verify the payload
        int payloadStart = ipHeaderLen + UdpHeaderView.SizeInBytes;
        int payloadLen = udp2.Length - UdpHeaderView.SizeInBytes;
        string text = Encoding.ASCII.GetString(packet, payloadStart, payloadLen);
        text.Should().Be("Hello, world!");
    }

    [Fact]
    public void IPv4_Udp_HelloWorld_NestedSubViews()
    {
        // Same packet, but parsed using nested sub-views
        byte[] payload = Encoding.ASCII.GetBytes("Hello, world!");
        int udpLen = UdpHeaderView.SizeInBytes + payload.Length;
        int totalLen = IPv4HeaderView.SizeInBytes + udpLen;
        var packet = new byte[totalLen];

        // Build using the nested composite view
        var pkt = new IPv4UdpPacketView(packet);

        // Access IP fields through the nested sub-view
        var ip = pkt.Ip;
        ip.Version = 4;
        ip.Ihl = 5;
        ip.TotalLength = (ushort)totalLen;
        ip.TimeToLive = 128;
        ip.Protocol = 17;
        ip.SourceAddress = 0xAC100A01;  // 172.16.10.1
        ip.DestinationAddress = 0xAC100A02; // 172.16.10.2

        // Access UDP fields through the nested sub-view
        var udp = pkt.Udp;
        udp.SourcePort = 5000;
        udp.DestinationPort = 8080;
        udp.Length = (ushort)udpLen;

        // Payload after the composite header
        payload.CopyTo(packet, IPv4HeaderView.SizeInBytes + UdpHeaderView.SizeInBytes);

        // Verify the nested sub-views read from the same buffer
        pkt.Ip.Version.Should().Be(4);
        pkt.Ip.TimeToLive.Should().Be(128);
        pkt.Ip.SourceAddress.Should().Be(0xAC100A01);
        pkt.Udp.SourcePort.Should().Be(5000);
        pkt.Udp.DestinationPort.Should().Be(8080);

        // Verify payload survives the round-trip
        int payloadStart = IPv4HeaderView.SizeInBytes + UdpHeaderView.SizeInBytes;
        string text = Encoding.ASCII.GetString(packet, payloadStart, payload.Length);
        text.Should().Be("Hello, world!");
    }

    [Fact]
    public void IPv4_Tcp_HelloWorld_ManualOffsets()
    {
        // IPv4/TCP packet with "Hello, world!" demonstrating manual offset parsing
        byte[] payload = Encoding.ASCII.GetBytes("Hello, world!");
        int tcpLen = TcpHeaderView.SizeInBytes + payload.Length; // 20 + 13 = 33
        int totalLen = IPv4HeaderView.SizeInBytes + tcpLen;       // 20 + 33 = 53
        var packet = new byte[totalLen];

        // Build IPv4 header
        var ip = new IPv4HeaderView(packet);
        ip.Version = 4;
        ip.Ihl = 5;
        ip.TotalLength = (ushort)totalLen;
        ip.DontFragment = true;
        ip.TimeToLive = 64;
        ip.Protocol = 6; // TCP
        ip.SourceAddress = 0xC0A80164;      // 192.168.1.100
        ip.DestinationAddress = 0x5DB8D822; // 93.184.216.34 (example.com)

        // Build TCP header at offset 20
        var tcp = new TcpHeaderView(packet, IPv4HeaderView.SizeInBytes);
        tcp.SourcePort = 49152;
        tcp.DestinationPort = 80;
        tcp.SequenceNumber = 1000;
        tcp.AcknowledgmentNumber = 0;
        tcp.DataOffset = 5;
        tcp.SYN = true;
        tcp.WindowSize = 65535;

        // Write payload after TCP header
        int payloadOffset = IPv4HeaderView.SizeInBytes + TcpHeaderView.SizeInBytes;
        payload.CopyTo(packet, payloadOffset);

        // Parse the packet back
        var ip2 = new IPv4HeaderView(packet);
        ip2.Protocol.Should().Be(6, "TCP");
        ip2.SourceAddress.Should().Be(0xC0A80164);

        int ipLen = ip2.HeaderLengthBytes;
        var tcp2 = new TcpHeaderView(packet, ipLen);
        tcp2.SourcePort.Should().Be(49152);
        tcp2.DestinationPort.Should().Be(80);
        tcp2.SequenceNumber.Should().Be(1000U);
        tcp2.DataOffset.Should().Be(5);
        tcp2.SYN.Should().BeTrue();
        tcp2.ACK.Should().BeFalse();

        int tcpPayloadOffset = ipLen + tcp2.HeaderLengthBytes;
        int tcpPayloadLen = ip2.TotalLength - ipLen - tcp2.HeaderLengthBytes;
        string text = Encoding.ASCII.GetString(packet, tcpPayloadOffset, tcpPayloadLen);
        text.Should().Be("Hello, world!");
    }

    #endregion

    #region Per-Field Endianness Override via Endian-Aware Types

    [Fact]
    public void MixedEndian_LE_Struct_With_UInt32Be_Field_Reads_BigEndian()
    {
        // MixedEndianView: struct is LE, but BeField is UInt32Be -> reads as BE
        // Layout: bytes 0-1 = LeField (LE), bytes 2-5 = BeField (BE), bytes 6-7 = ExplicitLeField (LE)
        var data = new byte[8];
        var view = new MixedEndianView(data);

        // Write via the view
        view.LeField = 0x1234;
        // LE struct: 0x1234 -> [0x34, 0x12]
        data[0].Should().Be(0x34);
        data[1].Should().Be(0x12);

        view.BeField = new UInt32Be(0xDEADBEEF);
        // UInt32Be override: 0xDEADBEEF -> [0xDE, 0xAD, 0xBE, 0xEF] (big-endian)
        data[2].Should().Be(0xDE);
        data[3].Should().Be(0xAD);
        data[4].Should().Be(0xBE);
        data[5].Should().Be(0xEF);

        view.ExplicitLeField = new UInt16Le(0xCAFE);
        // UInt16Le override: 0xCAFE -> [0xFE, 0xCA] (little-endian)
        data[6].Should().Be(0xFE);
        data[7].Should().Be(0xCA);
    }

    [Fact]
    public void MixedEndian_LE_Struct_With_UInt32Be_Field_RoundTrip()
    {
        var data = new byte[8];
        var view = new MixedEndianView(data);

        view.LeField = 0xABCD;
        view.BeField = new UInt32Be(0x12345678);
        view.ExplicitLeField = new UInt16Le(0x9999);

        view.LeField.Should().Be(0xABCD);
        ((uint)view.BeField).Should().Be(0x12345678U);
        ((ushort)view.ExplicitLeField).Should().Be(0x9999);
    }

    [Fact]
    public void MixedEndian_BE_Struct_With_UInt32Le_Field_Reads_LittleEndian()
    {
        // MixedEndianBEView: struct is BE, but LeField is UInt32Le -> reads as LE
        var data = new byte[8];
        var view = new MixedEndianBEView(data);

        view.BeField = 0x1234;
        // BE struct default: 0x1234 -> [0x12, 0x34]
        data[0].Should().Be(0x12);
        data[1].Should().Be(0x34);

        view.LeField = new UInt32Le(0xDEADBEEF);
        // UInt32Le override: 0xDEADBEEF -> [0xEF, 0xBE, 0xAD, 0xDE] (little-endian)
        data[2].Should().Be(0xEF);
        data[3].Should().Be(0xBE);
        data[4].Should().Be(0xAD);
        data[5].Should().Be(0xDE);
    }

    [Fact]
    public void MixedEndian_BE_Struct_With_UInt32Le_Field_RoundTrip()
    {
        var data = new byte[8];
        var view = new MixedEndianBEView(data);

        view.BeField = 0xCAFE;
        view.LeField = new UInt32Le(0x87654321);

        view.BeField.Should().Be(0xCAFE);
        ((uint)view.LeField).Should().Be(0x87654321U);
    }

    [Fact]
    public void MixedEndian_ZeroCopy_WriteThrough()
    {
        // Verify that endian-aware fields write through to the same buffer
        var data = new byte[8];
        var view1 = new MixedEndianView(data);
        var view2 = new MixedEndianView(data);

        view1.BeField = new UInt32Be(0x11223344);
        ((uint)view2.BeField).Should().Be(0x11223344U, "both views see the same buffer");
    }

    [Fact]
    public void MixedEndian_SameValue_DifferentWireFormat()
    {
        // Same logical value 0x12345678, but LE default vs BE override = different bytes
        var leData = new byte[8];
        var beData = new byte[8];

        var leView = new MixedEndianBEView(leData);
        var beView = new MixedEndianView(beData);

        // leView.LeField uses UInt32Le (little-endian override in a BE struct)
        leView.LeField = new UInt32Le(0x12345678);
        // beView.BeField uses UInt32Be (big-endian override in a LE struct)
        beView.BeField = new UInt32Be(0x12345678);

        // Both have the same logical value
        ((uint)leView.LeField).Should().Be((uint)beView.BeField);

        // But opposite wire formats at their respective byte offsets
        // LeField at bytes 2-5 in leData: LE = [0x78, 0x56, 0x34, 0x12]
        leData[2].Should().Be(0x78);
        leData[3].Should().Be(0x56);
        // BeField at bytes 2-5 in beData: BE = [0x12, 0x34, 0x56, 0x78]
        beData[2].Should().Be(0x12);
        beData[3].Should().Be(0x34);
    }

    [Fact]
    public void ExplicitSameEndian_LE_MatchesNativeTypes()
    {
        // UInt32Le in a LE struct should produce the exact same wire format
        // as plain uint in a LE struct
        var nativeData = new byte[28];
        var explicitData = new byte[28];

        var native = new MultiTypeLittleEndianView(nativeData);
        var explicitView = new ExplicitSameEndianLeView(explicitData);

        native.UShortField = 0xABCD;
        explicitView.U16 = new UInt16Le(0xABCD);

        native.ShortField = -1234;
        explicitView.S16 = new Int16Le(-1234);

        native.UIntField = 0xDEADBEEF;
        explicitView.U32 = new UInt32Le(0xDEADBEEF);

        native.IntField = -100000;
        explicitView.S32 = new Int32Le(-100000);

        native.ULongField = 0x0102030405060708UL;
        explicitView.U64 = new UInt64Le(0x0102030405060708UL);

        native.LongField = long.MinValue;
        explicitView.S64 = new Int64Le(long.MinValue);

        // Wire formats must be byte-for-byte identical
        for (int i = 0; i < 28; i++)
            nativeData[i].Should().Be(explicitData[i], $"byte {i} mismatch");

        // Round-trip values
        ((ushort)explicitView.U16).Should().Be(0xABCD);
        ((short)explicitView.S16).Should().Be(-1234);
        ((uint)explicitView.U32).Should().Be(0xDEADBEEF);
        ((int)explicitView.S32).Should().Be(-100000);
        ((ulong)explicitView.U64).Should().Be(0x0102030405060708UL);
        ((long)explicitView.S64).Should().Be(long.MinValue);
    }

    [Fact]
    public void ExplicitSameEndian_BE_MatchesNativeTypes()
    {
        // UInt32Be in a BE struct should produce the exact same wire format
        // as plain uint in a BE struct
        var nativeData = new byte[28];
        var explicitData = new byte[28];

        var native = new MultiTypeBigEndianView(nativeData);
        var explicitView = new ExplicitSameEndianBeView(explicitData);

        native.UShortField = 0xABCD;
        explicitView.U16 = new UInt16Be(0xABCD);

        native.ShortField = -1234;
        explicitView.S16 = new Int16Be(-1234);

        native.UIntField = 0xDEADBEEF;
        explicitView.U32 = new UInt32Be(0xDEADBEEF);

        native.IntField = -100000;
        explicitView.S32 = new Int32Be(-100000);

        native.ULongField = 0x0102030405060708UL;
        explicitView.U64 = new UInt64Be(0x0102030405060708UL);

        native.LongField = long.MinValue;
        explicitView.S64 = new Int64Be(long.MinValue);

        // Wire formats must be byte-for-byte identical
        for (int i = 0; i < 28; i++)
            nativeData[i].Should().Be(explicitData[i], $"byte {i} mismatch");

        // Round-trip values
        ((ushort)explicitView.U16).Should().Be(0xABCD);
        ((short)explicitView.S16).Should().Be(-1234);
        ((uint)explicitView.U32).Should().Be(0xDEADBEEF);
        ((int)explicitView.S32).Should().Be(-100000);
        ((ulong)explicitView.U64).Should().Be(0x0102030405060708UL);
        ((long)explicitView.S64).Should().Be(long.MinValue);
    }

    [Fact]
    public void AllBeInLeStruct_AllFields_WriteBigEndian()
    {
        // Every field is a BE type in a LE struct -- all should write big-endian bytes
        var data = new byte[28];
        var view = new AllBeInLeView(data);

        view.U16 = new UInt16Be(0x1234);
        // BE: [0x12, 0x34]
        data[0].Should().Be(0x12);
        data[1].Should().Be(0x34);

        view.S16 = new Int16Be(-1);
        // -1 = 0xFFFF BE: [0xFF, 0xFF]
        data[2].Should().Be(0xFF);
        data[3].Should().Be(0xFF);

        view.U32 = new UInt32Be(0x12345678);
        // BE: [0x12, 0x34, 0x56, 0x78]
        data[4].Should().Be(0x12);
        data[5].Should().Be(0x34);
        data[6].Should().Be(0x56);
        data[7].Should().Be(0x78);

        view.S32 = new Int32Be(-1);
        // -1 = 0xFFFFFFFF
        data[8].Should().Be(0xFF);
        data[9].Should().Be(0xFF);
        data[10].Should().Be(0xFF);
        data[11].Should().Be(0xFF);

        view.U64 = new UInt64Be(0x0102030405060708UL);
        // BE: [0x01, 0x02, ..., 0x08]
        data[12].Should().Be(0x01);
        data[13].Should().Be(0x02);
        data[14].Should().Be(0x03);
        data[15].Should().Be(0x04);
        data[16].Should().Be(0x05);
        data[17].Should().Be(0x06);
        data[18].Should().Be(0x07);
        data[19].Should().Be(0x08);

        view.S64 = new Int64Be(long.MinValue);
        // 0x8000000000000000 BE: [0x80, 0x00 x 7]
        data[20].Should().Be(0x80);
        for (int i = 21; i < 28; i++)
            data[i].Should().Be(0x00);

        // Round-trip all values
        ((ushort)view.U16).Should().Be(0x1234);
        ((short)view.S16).Should().Be(-1);
        ((uint)view.U32).Should().Be(0x12345678U);
        ((int)view.S32).Should().Be(-1);
        ((ulong)view.U64).Should().Be(0x0102030405060708UL);
        ((long)view.S64).Should().Be(long.MinValue);
    }

    [Fact]
    public void AllBeInLeStruct_Vs_NativeBE_SameWireFormat()
    {
        // A LE struct with all BE-typed fields should produce the exact same
        // wire format as a native BE struct with plain types
        var beData = new byte[28];
        var leOverrideData = new byte[28];

        var beView = new MultiTypeBigEndianView(beData);
        var leView = new AllBeInLeView(leOverrideData);

        beView.UShortField = 0xCAFE;
        leView.U16 = new UInt16Be(0xCAFE);

        beView.ShortField = -9999;
        leView.S16 = new Int16Be(-9999);

        beView.UIntField = 0xFEEDFACE;
        leView.U32 = new UInt32Be(0xFEEDFACE);

        beView.IntField = int.MinValue;
        leView.S32 = new Int32Be(int.MinValue);

        beView.ULongField = ulong.MaxValue;
        leView.U64 = new UInt64Be(ulong.MaxValue);

        beView.LongField = -9999999999L;
        leView.S64 = new Int64Be(-9999999999L);

        // Wire formats must be byte-for-byte identical
        for (int i = 0; i < 28; i++)
            beData[i].Should().Be(leOverrideData[i], $"byte {i} mismatch");
    }

    #endregion

    #region Corner Cases: Value Overflow and Masking

    [Fact]
    public void Overflow_ThreeBitField_TruncatesToMask()
    {
        var data = new byte[1];
        var view = new NarrowFieldsView(data);

        // 3-bit field max = 7; writing 0xFF should store only 0x07
        view.ThreeBit = 0xFF;
        view.ThreeBit.Should().Be(7, "value masked to 3 bits");
    }

    [Fact]
    public void Overflow_OneBitField_TruncatesToMask()
    {
        var data = new byte[1];
        var view = new NarrowFieldsView(data);

        view.OneBit = 0xFF;
        view.OneBit.Should().Be(1, "value masked to 1 bit");
    }

    [Fact]
    public void Overflow_DoesNotCorruptNeighbors()
    {
        var data = new byte[1];
        var view = new NarrowFieldsView(data);

        // Set all fields to known values
        view.ThreeBit = 5;   // bits 0-2
        view.OneBit = 1;     // bit 3
        view.FourBit = 0x0A; // bits 4-7

        // Overflow-write the 3-bit field with max uint8
        view.ThreeBit = 0xFF;

        // Neighbors must be untouched
        view.OneBit.Should().Be(1, "1-bit neighbor preserved after overflow write");
        view.FourBit.Should().Be(0x0A, "4-bit neighbor preserved after overflow write");
        view.ThreeBit.Should().Be(7, "only 3 bits stored");
    }

    [Fact]
    public void Overflow_FourBitField_MaxPlusOne()
    {
        var data = new byte[1];
        var view = new NarrowFieldsView(data);

        view.FourBit = 16; // max is 15; 16 & 0x0F = 0
        view.FourBit.Should().Be(0, "16 wraps to 0 in a 4-bit field");
    }

    #endregion

    #region Corner Cases: Adjacent Nibble Isolation

    [Fact]
    public void Nibbles_WriteAllFour_RoundTrip()
    {
        var data = new byte[2];
        var view = new NibbleView(data);

        view.N0 = 0x0A;
        view.N1 = 0x05;
        view.N2 = 0x0C;
        view.N3 = 0x03;

        view.N0.Should().Be(0x0A);
        view.N1.Should().Be(0x05);
        view.N2.Should().Be(0x0C);
        view.N3.Should().Be(0x03);

        // Verify wire format: MSB-first BE, so N0 is top nibble of byte 0
        data[0].Should().Be(0xA5);
        data[1].Should().Be(0xC3);
    }

    [Fact]
    public void Nibbles_WriteLast_DoesNotCorruptFirst()
    {
        var data = new byte[2];
        var view = new NibbleView(data);

        view.N0 = 0x0F;
        view.N1 = 0x0F;
        view.N2 = 0x0F;
        view.N3 = 0x0F;

        // Now overwrite just N2
        view.N2 = 0x00;

        view.N0.Should().Be(0x0F, "N0 preserved");
        view.N1.Should().Be(0x0F, "N1 preserved");
        view.N2.Should().Be(0x00, "N2 cleared");
        view.N3.Should().Be(0x0F, "N3 preserved");
    }

    [Fact]
    public void Nibbles_AlternatingPattern()
    {
        var data = new byte[2];
        var view = new NibbleView(data);

        // Write alternating pattern: 0, F, 0, F
        view.N0 = 0x00;
        view.N1 = 0x0F;
        view.N2 = 0x00;
        view.N3 = 0x0F;

        data[0].Should().Be(0x0F, "byte 0 = N0:0 N1:F");
        data[1].Should().Be(0x0F, "byte 1 = N2:0 N3:F");

        // Reverse the pattern
        view.N0 = 0x0F;
        view.N1 = 0x00;
        view.N2 = 0x0F;
        view.N3 = 0x00;

        data[0].Should().Be(0xF0);
        data[1].Should().Be(0xF0);
    }

    #endregion

    #region Corner Cases: Overlapping Fields (Union-Style)

    [Fact]
    public void Overlapping_WriteWord_ReadBytes()
    {
        var data = new byte[2];
        var view = new OverlappingFieldsView(data);

        view.FullWord = 0xABCD;
        view.HighByte.Should().Be(0xAB);
        view.LowByte.Should().Be(0xCD);
    }

    [Fact]
    public void Overlapping_WriteBytes_ReadWord()
    {
        var data = new byte[2];
        var view = new OverlappingFieldsView(data);

        view.HighByte = 0x12;
        view.LowByte = 0x34;
        view.FullWord.Should().Be(0x1234);
    }

    [Fact]
    public void Overlapping_WriteWord_ThenHighByte_PreservesLow()
    {
        var data = new byte[2];
        var view = new OverlappingFieldsView(data);

        view.FullWord = 0xABCD;
        view.HighByte = 0xFF; // overwrite only high byte

        view.HighByte.Should().Be(0xFF);
        view.LowByte.Should().Be(0xCD, "low byte preserved through high byte write");
        view.FullWord.Should().Be(0xFFCD);
    }

    #endregion

    #region Corner Cases: Single-Bit Fields as Non-Bool

    [Fact]
    public void SingleBitByte_SetAndGet()
    {
        var data = new byte[1];
        var view = new SingleBitByteView(data);

        view.Bit0 = 1;
        view.Bit1 = 0;
        view.Rest = 0x3F; // 6 bits, max 63

        view.Bit0.Should().Be(1);
        view.Bit1.Should().Be(0);
        view.Rest.Should().Be(0x3F);

        // LSB-first: bit0 = 0x01, bit1 = 0x02, rest = bits 2-7
        data[0].Should().Be(0xFD); // 0x01 | 0x00 | (0x3F << 2) = 0x01 | 0xFC = 0xFD
    }

    [Fact]
    public void SingleBitByte_Overflow_MasksTo1()
    {
        var data = new byte[1];
        var view = new SingleBitByteView(data);

        view.Bit0 = 0xFF;
        view.Bit0.Should().Be(1, "single-bit field masks to 0 or 1");
    }

    [Fact]
    public void SingleBitByte_DoesNotCorruptRest()
    {
        var data = new byte[1];
        var view = new SingleBitByteView(data);

        view.Rest = 0x3F;
        view.Bit0 = 1;
        view.Bit1 = 1;

        view.Rest.Should().Be(0x3F, "rest field preserved");
    }

    #endregion

    #region Corner Cases: Signed Narrow Fields

    [Fact]
    public void SignedNarrow_NoSignExtension_On_8BitIn16BitType()
    {
        // 8-bit field returning short: writing 0xFF (bit pattern for -1 in int8)
        // reads back as 255, NOT -1, because no sign extension is performed.
        var data = new byte[3];
        var view = new SignedNarrowView(data);

        view.Narrow8 = unchecked((short)0xFF); // store 0xFF in 8 bits
        view.Narrow8.Should().Be(255, "no sign extension: 8 bits in a short returns 0-255");
    }

    [Fact]
    public void SignedNarrow_FullWidth_PreservesSign()
    {
        // 16-bit field returning short: -1 round-trips correctly
        var data = new byte[3];
        var view = new SignedNarrowView(data);

        view.Full16 = -1;
        view.Full16.Should().Be(-1, "full-width signed field preserves sign");

        view.Full16 = short.MinValue;
        view.Full16.Should().Be(short.MinValue);

        view.Full16 = short.MaxValue;
        view.Full16.Should().Be(short.MaxValue);
    }

    [Fact]
    public void SignedNarrow_NarrowDoesNotCorruptFull()
    {
        var data = new byte[3];
        var view = new SignedNarrowView(data);

        view.Full16 = -12345;
        view.Narrow8 = 42;

        view.Full16.Should().Be(-12345, "full-width neighbor preserved");
        view.Narrow8.Should().Be(42);
    }

    #endregion

    #region Corner Cases: Endian-Aware Type Width Mismatch

    [Fact]
    public void EndianMismatch_WiderType_NarrowerField_Truncates()
    {
        // UInt32Be on a 16-bit field: only low 16 bits survive
        var data = new byte[4];
        var view = new EndianWidthMismatchView(data);

        view.WideTypeNarrowField = new UInt32Be(0x12345678);
        // Only 16 bits stored: 0x5678
        ((uint)view.WideTypeNarrowField).Should().Be(0x5678U,
            "only the low 16 bits of a 32-bit value survive in a 16-bit field");
    }

    [Fact]
    public void EndianMismatch_UsesCorrectByteOrder()
    {
        // Even though it's a LE struct, UInt32Be forces BE byte order for this field
        var data = new byte[4];
        var view = new EndianWidthMismatchView(data);

        view.WideTypeNarrowField = new UInt32Be(0xABCD);
        // BE byte order in a 16-bit field: [0xAB, 0xCD]
        data[0].Should().Be(0xAB);
        data[1].Should().Be(0xCD);
    }

    [Fact]
    public void EndianMismatch_DoesNotCorruptNeighbor()
    {
        var data = new byte[4];
        var view = new EndianWidthMismatchView(data);

        view.Neighbor = 0xFFFF;
        view.WideTypeNarrowField = new UInt32Be(0xCAFE);

        view.Neighbor.Should().Be(0xFFFF, "neighbor at bits 16-31 preserved");
    }

    #endregion

    #region Corner Cases: Full 64-Bit Field

    [Fact]
    public void Full64Bit_MaxValue()
    {
        var data = new byte[8];
        var view = new Full64BitView(data);

        view.Value = ulong.MaxValue;
        view.Value.Should().Be(ulong.MaxValue);

        for (int i = 0; i < 8; i++)
            data[i].Should().Be(0xFF);
    }

    [Fact]
    public void Full64Bit_Zero()
    {
        var data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        var view = new Full64BitView(data);

        view.Value = 0;
        view.Value.Should().Be(0UL);

        for (int i = 0; i < 8; i++)
            data[i].Should().Be(0x00);
    }

    [Fact]
    public void Full64Bit_RoundTrip()
    {
        var data = new byte[8];
        var view = new Full64BitView(data);

        view.Value = 0x0102030405060708UL;
        view.Value.Should().Be(0x0102030405060708UL);

        // BE: MSB first
        data[0].Should().Be(0x01);
        data[7].Should().Be(0x08);
    }

    #endregion

    #region Corner Cases: Distant Field and SizeInBytes

    [Fact]
    public void DistantField_SizeInBytes_AccountsForFarField()
    {
        // Field at bit 800-815 = bytes 100-101. With 2-byte read width, needs 102 bytes.
        DistantFieldView.SizeInBytes.Should().BeGreaterThanOrEqualTo(102);
    }

    [Fact]
    public void DistantField_RoundTrip()
    {
        var data = new byte[DistantFieldView.SizeInBytes];
        var view = new DistantFieldView(data);

        view.First = 0x42;
        view.Distant = 0xBEEF;

        view.First.Should().Be(0x42);
        view.Distant.Should().Be(0xBEEF);

        data[0].Should().Be(0x42);
        // Distant at bytes 100-101, BE
        data[100].Should().Be(0xBE);
        data[101].Should().Be(0xEF);
    }

    [Fact]
    public void DistantField_DoesNotCorruptIntervening()
    {
        var data = new byte[DistantFieldView.SizeInBytes];
        // Fill middle with sentinel
        for (int i = 1; i < 100; i++)
            data[i] = 0xAA;

        var view = new DistantFieldView(data);
        view.First = 0xFF;
        view.Distant = 0x1234;

        // All bytes between the two fields must be untouched
        for (int i = 1; i < 100; i++)
            data[i].Should().Be(0xAA, $"byte {i} between fields should be untouched");
    }

    #endregion

    #region Corner Cases: Buffer Boundary

    [Fact]
    public void MinimalBuffer_ExactSizeInBytes_Works()
    {
        var data = new byte[IPv6HeaderView.SizeInBytes];
        var act = () => new IPv6HeaderView(data);
        act.Should().NotThrow("buffer is exactly SizeInBytes");
    }

    [Fact]
    public void MinimalBuffer_OneByteTooSmall_Throws()
    {
        var data = new byte[IPv6HeaderView.SizeInBytes - 1];
        var act = () => new IPv6HeaderView(data);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void OffsetConstructor_PastEnd_Throws()
    {
        var data = new byte[10];
        var act = () => new IPv6HeaderView(data, 10);
        act.Should().Throw<ArgumentException>("offset leaves 0 bytes, less than SizeInBytes");
    }

    [Fact]
    public void OffsetConstructor_JustEnough_Works()
    {
        var data = new byte[100];
        int offset = 100 - IPv6HeaderView.SizeInBytes;
        var act = () => new IPv6HeaderView(data, offset);
        act.Should().NotThrow("offset leaves exactly SizeInBytes bytes");
    }

    #endregion

    #region Corner Cases: All-Zeros and All-Ones Patterns

    [Fact]
    public void AllOnes_Buffer_ReadsMaxValues()
    {
        var data = new byte[NibbleView.SizeInBytes];
        for (int i = 0; i < data.Length; i++) data[i] = 0xFF;

        var view = new NibbleView(data);
        view.N0.Should().Be(0x0F);
        view.N1.Should().Be(0x0F);
        view.N2.Should().Be(0x0F);
        view.N3.Should().Be(0x0F);
    }

    [Fact]
    public void AllZeros_Buffer_ReadsZeros()
    {
        var data = new byte[NibbleView.SizeInBytes];

        var view = new NibbleView(data);
        view.N0.Should().Be(0);
        view.N1.Should().Be(0);
        view.N2.Should().Be(0);
        view.N3.Should().Be(0);
    }

    [Fact]
    public void SetAllToMax_ThenClearOne_PreservesOthers()
    {
        var data = new byte[NibbleView.SizeInBytes];
        var view = new NibbleView(data);

        view.N0 = 0x0F;
        view.N1 = 0x0F;
        view.N2 = 0x0F;
        view.N3 = 0x0F;

        view.N1 = 0x00;

        view.N0.Should().Be(0x0F);
        view.N1.Should().Be(0x00);
        view.N2.Should().Be(0x0F);
        view.N3.Should().Be(0x0F);
    }

    #endregion

    #region Composition: BitFields inside BitFieldsView

    // ---- Correct usage: byte-backed BitFields in any view endianness ----

    [Fact]
    public void ByteBackedBitFields_InBEView_RoundTrips()
    {
        // Single-byte types are endianness-agnostic -- always correct
        var data = new byte[2];
        var view = new BeViewWithByteFlags(data);

        StatusFlags flags = 0;
        flags.Ready = true;
        flags.Priority = 0x0A;

        view.Flags = flags;
        view.Payload = 0x42;

        view.Flags.Ready.Should().BeTrue();
        view.Flags.Priority.Should().Be(0x0A);
        view.Payload.Should().Be(0x42);
    }

    [Fact]
    public void ByteBackedBitFields_InBEView_WireFormatMatchesDirectByte()
    {
        var data = new byte[2];
        var view = new BeViewWithByteFlags(data);

        StatusFlags flags = 0;
        flags.Ready = true;    // bit 0
        flags.Error = true;    // bit 1
        flags.Priority = 0x0F; // bits 4-7
        // Raw byte = 0xF3

        view.Flags = flags;

        data[0].Should().Be(0xF3, "byte-backed BitFields: wire byte = raw value");
    }

    // ---- Correct usage: LE BitFields in LE view ----

    [Fact]
    public void LeHeader_InLeView_RoundTrips()
    {
        // ProtocolHeader16 uses LE serialization; LE view reads bytes as LE.
        // This is the correct pairing.
        var data = new byte[3];
        var view = new LeViewWithLeHeader(data);

        ProtocolHeader16 hdr = 0;
        hdr.Status = CreateStatusFlags(ready: true, priority: 5);
        hdr.Length = 0x42;

        view.Header = hdr;
        view.Tag = 0xBB;

        view.Header.Status.Ready.Should().BeTrue();
        view.Header.Status.Priority.Should().Be(5);
        view.Header.Length.Should().Be(0x42);
        view.Tag.Should().Be(0xBB);
    }

    [Fact]
    public void LeHeader_InLeView_WireFormat_MatchesReadFrom()
    {
        // Build a ProtocolHeader16 and embed it in a LE view.
        // The wire bytes should match what ProtocolHeader16.WriteTo produces.
        var viewData = new byte[3];
        var view = new LeViewWithLeHeader(viewData);

        ProtocolHeader16 hdr = 0;
        hdr.Status = CreateStatusFlags(ready: true, priority: 0x0F);
        hdr.Length = 0x42;

        view.Header = hdr;

        // Independently serialize the same header
        var directBytes = new byte[2];
        hdr.WriteTo(directBytes);

        viewData[0].Should().Be(directBytes[0], "LE view byte 0 matches LE serialize byte 0");
        viewData[1].Should().Be(directBytes[1], "LE view byte 1 matches LE serialize byte 1");
    }

    // ---- LE BitFields in BE view: ByteOrder override eliminates the mismatch ----

    [Fact]
    public void LeHeader_InBeView_InMemoryRoundTrip_StillWorks()
    {
        // ProtocolHeader16 declares ByteOrder.LittleEndian (the [BitFields] default).
        // The view generator detects this and uses LE serialization for this field,
        // so in-memory round-trip works correctly.
        var data = new byte[3];
        var view = new BeViewWithLeHeader(data);

        ProtocolHeader16 hdr = 0;
        hdr.Status = CreateStatusFlags(ready: true, priority: 5);
        hdr.Length = 0x42;

        view.Header = hdr;

        view.Header.Status.Ready.Should().BeTrue();
        view.Header.Status.Priority.Should().Be(5);
        view.Header.Length.Should().Be(0x42);
    }

    [Fact]
    public void LeHeader_InBeView_WireFormat_DoesNotMatchReadFrom()
    {
        // ProtocolHeader16 uses LE serialization internally.
        // But BeViewWithLeHeader reads/writes bytes as BE.
        // So the wire bytes are byte-swapped relative to what ReadFrom expects.
        var viewData = new byte[3];
        var view = new BeViewWithLeHeader(viewData);

        ProtocolHeader16 hdr = 0;
        hdr.Status = CreateStatusFlags(ready: true, priority: 0x0F);
        hdr.Length = 0x42;
        // Raw ushort = 0x42F1 (Status=0xF1, Length=0x42)

        view.Header = hdr;

        // The view wrote the ushort as big-endian (the view's default),
        // but ReadFrom reads it as little-endian (the BitFields default).
        // So the round-trip through ReadFrom produces a DIFFERENT value.
        var reinterpreted = ProtocolHeader16.ReadFrom(viewData.AsSpan(0, 2));
        ushort original = hdr;
        ushort fromWire = reinterpreted;

        fromWire.Should().Be(original,
            "BE view byte-swaps the LE BitFields: wire bytes don't match ReadFrom");
    }

    [Fact]
    public void LeHeader_InLeView_WireFormat_RoundTrips_Via_ReadFrom()
    {
        // Contrast: with matching endianness, ReadFrom produces the same value.
        var viewData = new byte[3];
        var view = new LeViewWithLeHeader(viewData);

        ProtocolHeader16 hdr = 0;
        hdr.Status = CreateStatusFlags(ready: true, priority: 0x0F);
        hdr.Length = 0x42;

        view.Header = hdr;

        var reinterpreted = ProtocolHeader16.ReadFrom(viewData.AsSpan(0, 2));
        ushort original = hdr;
        ushort fromWire = reinterpreted;

        fromWire.Should().Be(original,
            "LE view + LE BitFields: wire bytes match ReadFrom interpretation");
    }

    // ---- Width mismatch: wider BitFields in narrower field ----

    [Fact]
    public void TruncatedHeader_LosesUpperByte()
    {
        // ProtocolHeader16 is ushort-backed (16 bits) but the field is only 8 bits.
        // Writing a header with Length != 0 and reading it back loses the Length byte.
        var data = new byte[2];
        var view = new ViewWithTruncatedHeader(data);

        ProtocolHeader16 hdr = 0;
        hdr.Status = CreateStatusFlags(ready: true, priority: 5);
        hdr.Length = 0x42; // this is in the UPPER byte

        view.TruncatedHeader = hdr;

        // Only the low 8 bits (Status byte) survive
        view.TruncatedHeader.Status.Ready.Should().BeTrue("low-byte status survives");
        view.TruncatedHeader.Status.Priority.Should().Be(5, "low-byte priority survives");
        view.TruncatedHeader.Length.Should().Be(0, "upper byte is truncated to zero");
    }

    [Fact]
    public void TruncatedHeader_DoesNotCorruptNeighbor()
    {
        var data = new byte[2];
        var view = new ViewWithTruncatedHeader(data);

        view.Other = 0xFF;

        ProtocolHeader16 hdr = 0xFFFF; // all bits set
        view.TruncatedHeader = hdr;

        view.Other.Should().Be(0xFF, "neighbor preserved despite truncation");
    }

    // ---- Width mismatch: narrower BitFields in wider field ----

    [Fact]
    public void WideFlags_ByteBackedIn16BitField_UpperBitsZero()
    {
        // StatusFlags is byte-backed (8 bits) in a 16-bit field.
        // Reading back: the upper 8 bits are always zero.
        var data = new byte[3];
        var view = new ViewWithWideFlags(data);

        StatusFlags flags = 0;
        flags.Ready = true;
        flags.Priority = 0x0F;
        // raw byte = 0xF1

        view.WideFlags = flags;

        // The value round-trips correctly for the status bits
        view.WideFlags.Ready.Should().BeTrue();
        view.WideFlags.Priority.Should().Be(0x0F);

        // But the raw ushort seen by the view has upper bits zero
        ushort rawFromView = view.WideFlags;
        rawFromView.Should().Be(0x00F1, "byte-backed value zero-extends into 16-bit field");
    }

    [Fact]
    public void WideFlags_PreExistingUpperBits_ClearedOnWrite()
    {
        // If the buffer has data in the upper 8 bits of the 16-bit field,
        // writing a byte-backed BitFields clears those upper bits.
        var data = new byte[3];
        data[0] = 0xFF; // pre-fill both bytes of the 16-bit field
        data[1] = 0xFF;

        var view = new ViewWithWideFlags(data);

        StatusFlags flags = 0;
        flags.Ready = true;
        // raw byte = 0x01

        view.WideFlags = flags;

        // The upper byte of the 16-bit field is now cleared
        // because (ushort)flags = 0x0001, and the mask is 0xFFFF
        view.WideFlags.Ready.Should().BeTrue();
        ushort rawFromView = view.WideFlags;
        rawFromView.Should().Be(0x0001, "upper bits cleared by writing byte-backed value");
    }

    // ---- Helper ----

    private static StatusFlags CreateStatusFlags(bool ready = false, bool error = false,
        bool busy = false, bool complete = false, byte priority = 0)
    {
        StatusFlags f = 0;
        f.Ready = ready;
        f.Error = error;
        f.Busy = busy;
        f.Complete = complete;
        f.Priority = priority;
        return f;
    }

    #endregion
}
