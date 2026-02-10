using System;
using FluentAssertions;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Mixed-endian scenario: a network protocol (BE) carrying an x86 binary file
/// payload (LE) that contains blobs with embedded BE network captures.
///
/// Layout:
///   Outer transport (BE, MSB-first):
///     bytes 0-1:  MessageType     (ushort, BE)
///     bytes 2-5:  PayloadLength   (uint, BE)
///     bytes 6-7:  Checksum        (ushort, BE)
///     bytes 8+:   FileBlob        (sub-view, LE)
///
///   FileBlob (LE, LSB-first) -- from an x86 binary file:
///     bytes 0-3:  Magic           (uint, LE -- native x86)
///     bytes 4-7:  Timestamp       (uint, LE -- native x86)
///     bytes 8-11: CapturedSrcIp   (UInt32Be -- a network IP stored in the blob in BE)
///     bytes 12-13: RecordCount    (ushort, LE -- native x86)
///     bytes 14+:  CaptureHeader   (sub-view, BE -- embedded network capture)
///
///   CaptureHeader (BE, MSB-first) -- embedded RFC-style header:
///     bytes 0-1:  Protocol        (ushort, BE)
///     bytes 2-3:  Length          (ushort, BE)
///     bytes 4-7:  SequenceNum     (uint, BE)
/// </summary>
public partial class MixedEndianScenarioTests
{
    // ---- View definitions ----

    /// <summary>
    /// Embedded network capture header in native BE/MSB-first order.
    /// </summary>
    [BitFieldsView(ByteOrder.BigEndian, BitOrder.MsbIsBitZero)]
    public partial record struct CaptureHeaderView
    {
        [BitField(0, 15)]  public partial ushort Protocol { get; set; }
        [BitField(16, 31)] public partial ushort Length { get; set; }
        [BitField(32, 63)] public partial uint SequenceNum { get; set; }
    }

    /// <summary>
    /// x86 binary file blob in native LE/LSB-first order.
    /// Contains an embedded BE field (CapturedSrcIp) and a nested BE sub-view (Capture).
    /// </summary>
    [BitFieldsView(ByteOrder.LittleEndian, BitOrder.LsbIsBitZero)]
    public partial record struct FileBlobView
    {
        [BitField(0, 31)]    public partial uint Magic { get; set; }
        [BitField(32, 63)]   public partial uint Timestamp { get; set; }
        [BitField(64, 95)]   public partial UInt32Be CapturedSrcIp { get; set; }
        [BitField(96, 111)]  public partial ushort RecordCount { get; set; }
        [BitField(112, 175)] public partial CaptureHeaderView Capture { get; set; }
    }

    /// <summary>
    /// Outer transport protocol in network byte order (BE/MSB-first).
    /// Carries the x86 binary file blob as a nested LE sub-view.
    /// </summary>
    [BitFieldsView(ByteOrder.BigEndian, BitOrder.MsbIsBitZero)]
    public partial record struct TransportHeaderView
    {
        [BitField(0, 15)]   public partial ushort MessageType { get; set; }
        [BitField(16, 47)]  public partial uint PayloadLength { get; set; }
        [BitField(48, 63)]  public partial ushort Checksum { get; set; }
        [BitField(64, 239)] public partial FileBlobView Blob { get; set; }
    }

    // ---- Tests ----

    [Fact]
    public void CaptureHeader_BE_RoundTrip()
    {
        var data = new byte[CaptureHeaderView.SizeInBytes];
        var view = new CaptureHeaderView(data);

        view.Protocol = 0x0800;
        view.Length = 100;
        view.SequenceNum = 0x12345678;

        view.Protocol.Should().Be(0x0800);
        view.Length.Should().Be(100);
        view.SequenceNum.Should().Be(0x12345678U);

        // Verify BE wire format
        data[0].Should().Be(0x08);
        data[1].Should().Be(0x00);
        data[4].Should().Be(0x12);
        data[5].Should().Be(0x34);
        data[6].Should().Be(0x56);
        data[7].Should().Be(0x78);
    }

    [Fact]
    public void FileBlob_LE_WithBeField_RoundTrip()
    {
        var data = new byte[FileBlobView.SizeInBytes];
        var view = new FileBlobView(data);

        view.Magic = 0x46494C45;        // "FILE" in LE
        view.Timestamp = 0xDEADBEEF;
        view.CapturedSrcIp = new UInt32Be(0xC0A80101); // 192.168.1.1 in BE
        view.RecordCount = 42;

        view.Magic.Should().Be(0x46494C45U);
        view.Timestamp.Should().Be(0xDEADBEEFU);
        ((uint)view.CapturedSrcIp).Should().Be(0xC0A80101U);
        view.RecordCount.Should().Be(42);

        // Magic is LE: LSB first
        data[0].Should().Be(0x45);
        data[1].Should().Be(0x4C);
        data[2].Should().Be(0x49);
        data[3].Should().Be(0x46);

        // Timestamp is LE: LSB first
        data[4].Should().Be(0xEF);
        data[5].Should().Be(0xBE);
        data[6].Should().Be(0xAD);
        data[7].Should().Be(0xDE);

        // CapturedSrcIp is UInt32Be: MSB first even in LE struct
        data[8].Should().Be(0xC0);
        data[9].Should().Be(0xA8);
        data[10].Should().Be(0x01);
        data[11].Should().Be(0x01);

        // RecordCount is LE
        data[12].Should().Be(42);
        data[13].Should().Be(0x00);
    }

    [Fact]
    public void FileBlob_NestedBeCaptureInLeBlob_RoundTrip()
    {
        var data = new byte[FileBlobView.SizeInBytes];
        var view = new FileBlobView(data);

        // Set LE fields
        view.Magic = 0x424C4F42;        // "BLOB"
        view.Timestamp = 1000000;
        view.RecordCount = 1;

        // Write through to the nested BE capture header
        var capture = view.Capture;
        capture.Protocol = 0x0800;       // IPv4
        capture.Length = 64;
        capture.SequenceNum = 99;

        // Read back through the blob
        view.Magic.Should().Be(0x424C4F42U);
        view.RecordCount.Should().Be(1);

        var readCapture = view.Capture;
        readCapture.Protocol.Should().Be(0x0800);
        readCapture.Length.Should().Be(64);
        readCapture.SequenceNum.Should().Be(99U);

        // Verify the capture header bytes (at offset 14 in the blob)
        // are in BE order despite the blob being LE
        data[14].Should().Be(0x08, "capture Protocol MSB");
        data[15].Should().Be(0x00, "capture Protocol LSB");
        data[16].Should().Be(0x00, "capture Length MSB");
        data[17].Should().Be(64, "capture Length LSB");
        data[18].Should().Be(0x00, "capture SeqNum byte 0");
        data[19].Should().Be(0x00, "capture SeqNum byte 1");
        data[20].Should().Be(0x00, "capture SeqNum byte 2");
        data[21].Should().Be(99, "capture SeqNum byte 3");
    }

    [Fact]
    public void Transport_FullStack_BuildAndParse()
    {
        // Build a complete mixed-endian packet:
        // BE transport header wrapping LE file blob wrapping BE capture header
        var data = new byte[TransportHeaderView.SizeInBytes];
        var transport = new TransportHeaderView(data);

        // Outer transport (BE)
        transport.MessageType = 0x0001;
        transport.PayloadLength = 22;    // blob size
        transport.Checksum = 0xCAFE;

        // Verify transport header is BE on the wire
        data[0].Should().Be(0x00, "MessageType MSB");
        data[1].Should().Be(0x01, "MessageType LSB");

        // Access the nested LE blob
        var blob = transport.Blob;
        blob.Magic = 0x46494C45;         // "FILE" in LE
        blob.Timestamp = 0x5F3E2D1C;    // arbitrary timestamp
        blob.CapturedSrcIp = new UInt32Be(0x0A000001); // 10.0.0.1 in network order
        blob.RecordCount = 3;

        // Access the doubly-nested BE capture header
        var capture = blob.Capture;
        capture.Protocol = 17;           // UDP
        capture.Length = 28;
        capture.SequenceNum = 0xAABBCCDD;

        // ---- Parse it all back ----
        var t2 = new TransportHeaderView(data);
        t2.MessageType.Should().Be(0x0001);
        t2.PayloadLength.Should().Be(22U);
        t2.Checksum.Should().Be(0xCAFE);

        var b2 = t2.Blob;
        b2.Magic.Should().Be(0x46494C45U);
        b2.Timestamp.Should().Be(0x5F3E2D1CU);
        ((uint)b2.CapturedSrcIp).Should().Be(0x0A000001U);
        b2.RecordCount.Should().Be(3);

        var c2 = b2.Capture;
        c2.Protocol.Should().Be(17);
        c2.Length.Should().Be(28);
        c2.SequenceNum.Should().Be(0xAABBCCDDU);
    }

    [Fact]
    public void Transport_FullStack_VerifyWireBytes()
    {
        var data = new byte[TransportHeaderView.SizeInBytes];
        var transport = new TransportHeaderView(data);

        transport.MessageType = 0x0102;
        transport.PayloadLength = 22;
        transport.Checksum = 0xABCD;

        var blob = transport.Blob;
        blob.Magic = 0x01020304;
        blob.Timestamp = 0x05060708;
        blob.CapturedSrcIp = new UInt32Be(0xC0A80101);
        blob.RecordCount = 0x0A0B;

        var capture = blob.Capture;
        capture.Protocol = 0x0C0D;
        capture.Length = 0x0E0F;
        capture.SequenceNum = 0x10111213;

        // Transport header (bytes 0-7): BE
        data[0].Should().Be(0x01, "MessageType[0] BE");
        data[1].Should().Be(0x02, "MessageType[1] BE");
        data[2].Should().Be(0x00, "PayloadLength[0] BE");
        data[3].Should().Be(0x00, "PayloadLength[1] BE");
        data[4].Should().Be(0x00, "PayloadLength[2] BE");
        data[5].Should().Be(0x16, "PayloadLength[3] BE (22 = 0x16)");
        data[6].Should().Be(0xAB, "Checksum[0] BE");
        data[7].Should().Be(0xCD, "Checksum[1] BE");

        // Blob header (bytes 8-21): LE for native fields
        // Magic (LE): 0x01020304 -> [0x04, 0x03, 0x02, 0x01]
        data[8].Should().Be(0x04, "Magic[0] LE");
        data[9].Should().Be(0x03, "Magic[1] LE");
        data[10].Should().Be(0x02, "Magic[2] LE");
        data[11].Should().Be(0x01, "Magic[3] LE");

        // Timestamp (LE): 0x05060708 -> [0x08, 0x07, 0x06, 0x05]
        data[12].Should().Be(0x08, "Timestamp[0] LE");
        data[13].Should().Be(0x07, "Timestamp[1] LE");
        data[14].Should().Be(0x06, "Timestamp[2] LE");
        data[15].Should().Be(0x05, "Timestamp[3] LE");

        // CapturedSrcIp (UInt32Be override in LE struct): 0xC0A80101 -> [0xC0, 0xA8, 0x01, 0x01]
        data[16].Should().Be(0xC0, "CapturedSrcIp[0] BE override");
        data[17].Should().Be(0xA8, "CapturedSrcIp[1] BE override");
        data[18].Should().Be(0x01, "CapturedSrcIp[2] BE override");
        data[19].Should().Be(0x01, "CapturedSrcIp[3] BE override");

        // RecordCount (LE): 0x0A0B -> [0x0B, 0x0A]
        data[20].Should().Be(0x0B, "RecordCount[0] LE");
        data[21].Should().Be(0x0A, "RecordCount[1] LE");

        // Capture header (bytes 22-29): BE (nested sub-view with its own ByteOrder)
        // Protocol (BE): 0x0C0D -> [0x0C, 0x0D]
        data[22].Should().Be(0x0C, "Capture.Protocol[0] BE");
        data[23].Should().Be(0x0D, "Capture.Protocol[1] BE");

        // Length (BE): 0x0E0F -> [0x0E, 0x0F]
        data[24].Should().Be(0x0E, "Capture.Length[0] BE");
        data[25].Should().Be(0x0F, "Capture.Length[1] BE");

        // SequenceNum (BE): 0x10111213 -> [0x10, 0x11, 0x12, 0x13]
        data[26].Should().Be(0x10, "Capture.SequenceNum[0] BE");
        data[27].Should().Be(0x11, "Capture.SequenceNum[1] BE");
        data[28].Should().Be(0x12, "Capture.SequenceNum[2] BE");
        data[29].Should().Be(0x13, "Capture.SequenceNum[3] BE");
    }

    [Fact]
    public void Transport_ZeroCopy_WriteThrough_AllLayers()
    {
        var data = new byte[TransportHeaderView.SizeInBytes];
        var transport = new TransportHeaderView(data);

        // Write at the deepest nesting level
        var capture = transport.Blob.Capture;
        capture.SequenceNum = 0xDEADC0DE;

        // Read from the same buffer at different nesting levels
        transport.Blob.Capture.SequenceNum.Should().Be(0xDEADC0DEU,
            "write-through: deepest write visible at all levels");

        // Write at blob level (must use local var since sub-view returns by value)
        var blob = transport.Blob;
        blob.Magic = 0xCAFEBABE;
        transport.Blob.Magic.Should().Be(0xCAFEBABEU);

        // Verify capture is untouched
        transport.Blob.Capture.SequenceNum.Should().Be(0xDEADC0DEU,
            "blob write does not corrupt nested capture");
    }

    [Fact]
    public void ParsePrebuiltPacket_MixedEndian()
    {
        // Simulate receiving a packet with known wire bytes
        var packet = new byte[30];

        // Transport header (BE)
        packet[0] = 0x00; packet[1] = 0x42;     // MessageType = 0x0042
        packet[2] = 0x00; packet[3] = 0x00;
        packet[4] = 0x00; packet[5] = 0x16;     // PayloadLength = 22
        packet[6] = 0xFF; packet[7] = 0xFE;     // Checksum = 0xFFFE

        // Blob (LE)
        packet[8] = 0x89; packet[9] = 0x50;
        packet[10] = 0x4E; packet[11] = 0x47;   // Magic = 0x474E5089 (PNG signature-ish, LE)
        packet[12] = 0x01; packet[13] = 0x00;
        packet[14] = 0x00; packet[15] = 0x00;   // Timestamp = 1 (LE)
        packet[16] = 0x0A; packet[17] = 0x00;
        packet[18] = 0x00; packet[19] = 0x01;   // CapturedSrcIp = 0x0A000001 (BE)
        packet[20] = 0x01; packet[21] = 0x00;   // RecordCount = 1 (LE)

        // Capture header (BE)
        packet[22] = 0x08; packet[23] = 0x00;   // Protocol = 0x0800 (IPv4)
        packet[24] = 0x00; packet[25] = 0x1C;   // Length = 28
        packet[26] = 0x00; packet[27] = 0x00;
        packet[28] = 0x00; packet[29] = 0x01;   // SequenceNum = 1

        var t = new TransportHeaderView(packet);
        t.MessageType.Should().Be(0x0042);
        t.PayloadLength.Should().Be(22U);
        t.Checksum.Should().Be(0xFFFE);

        var b = t.Blob;
        b.Magic.Should().Be(0x474E5089U, "PNG-like magic in LE");
        b.Timestamp.Should().Be(1U);
        ((uint)b.CapturedSrcIp).Should().Be(0x0A000001U, "IP address preserved in BE");
        b.RecordCount.Should().Be(1);

        var c = b.Capture;
        c.Protocol.Should().Be(0x0800, "IPv4 ethertype");
        c.Length.Should().Be(28);
        c.SequenceNum.Should().Be(1U);
    }
}
