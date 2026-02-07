using System.Text;
using FluentAssertions;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Demonstrates BitFields composition by modeling network protocol headers.
/// Assembles a complete Ethernet ? IPv4 ? TCP packet carrying "Hello world!".
/// Bit positions follow big-endian (network byte order) convention: the first
/// field on the wire maps to the most significant bits of each 32-bit word.
/// </summary>
public partial class BitFieldProtocolTests
{
    #region Protocol Header Struct Definitions

    /// <summary>IPv4 header flags (3 bits): Reserved, Don't Fragment, More Fragments.</summary>
    [BitFields(typeof(byte), UndefinedBitsMustBe.Zeroes)]
    public partial struct IPv4Flags
    {
        [BitFlag(0)] public partial bool MoreFragments { get; set; } // MF
        [BitFlag(1)] public partial bool DontFragment { get; set; }  // DF
        [BitFlag(2)] public partial bool Reserved { get; set; }      // must be 0
    }

    /// <summary>TCP control flags (9 bits): FIN, SYN, RST, PSH, ACK, URG, ECE, CWR, NS.</summary>
    [BitFields(typeof(ushort), UndefinedBitsMustBe.Zeroes)]
    public partial struct TcpFlags
    {
        [BitFlag(0)] public partial bool FIN { get; set; } // connection finish
        [BitFlag(1)] public partial bool SYN { get; set; } // synchronize sequence numbers
        [BitFlag(2)] public partial bool RST { get; set; } // reset connection
        [BitFlag(3)] public partial bool PSH { get; set; } // push data to application
        [BitFlag(4)] public partial bool ACK { get; set; } // acknowledgment field valid
        [BitFlag(5)] public partial bool URG { get; set; } // urgent pointer valid
        [BitFlag(6)] public partial bool ECE { get; set; } // ECN-Echo
        [BitFlag(7)] public partial bool CWR { get; set; } // congestion window reduced
        [BitFlag(8)] public partial bool NS { get; set; }  // ECN-nonce concealment
    }

    /// <summary>
    /// IPv4 header word 0 (32 bits): Version(4) | IHL(4) | DSCP(6) | ECN(2) | TotalLength(16).
    /// </summary>
    [BitFields(typeof(uint))]
    public partial struct IPv4VersionWord
    {
        [BitField(28, 31)] public partial byte Version { get; set; }       // always 4 for IPv4
        [BitField(24, 27)] public partial byte IHL { get; set; }           // header length in 32-bit words
        [BitField(18, 23)] public partial byte DSCP { get; set; }          // differentiated services
        [BitField(16, 17)] public partial byte ECN { get; set; }           // congestion notification
        [BitField(0, 15)]  public partial ushort TotalLength { get; set; } // total packet length in bytes
    }

    /// <summary>
    /// IPv4 header word 1 (32 bits): Identification(16) | Flags(3) | FragmentOffset(13).
    /// Demonstrates composition: IPv4Flags embedded in a 3-bit field.
    /// </summary>
    [BitFields(typeof(uint))]
    public partial struct IPv4FragmentWord
    {
        [BitField(16, 31)] public partial ushort Identification { get; set; } // packet ID for reassembly
        [BitField(13, 15)] public partial IPv4Flags Flags { get; set; }       // ? composed!
        [BitField(0, 12)]  public partial ushort FragmentOffset { get; set; } // offset in 8-byte units
    }

    /// <summary>
    /// IPv4 header word 2 (32 bits): TTL(8) | Protocol(8) | HeaderChecksum(16).
    /// </summary>
    [BitFields(typeof(uint))]
    public partial struct IPv4TtlWord
    {
        [BitField(24, 31)] public partial byte TTL { get; set; }              // time to live (hop limit)
        [BitField(16, 23)] public partial byte Protocol { get; set; }         // 6 = TCP, 17 = UDP
        [BitField(0, 15)]  public partial ushort HeaderChecksum { get; set; } // one's complement checksum
    }

    /// <summary>
    /// TCP header control word (32 bits): DataOffset(4) | Reserved(3) | Flags(9) | WindowSize(16).
    /// Demonstrates composition: TcpFlags embedded in a 9-bit field.
    /// </summary>
    [BitFields(typeof(uint))]
    public partial struct TcpControlWord
    {
        [BitField(28, 31)] public partial byte DataOffset { get; set; }   // header length in 32-bit words
        [BitField(25, 27)] public partial byte Reserved { get; set; }     // must be 0
        [BitField(16, 24)] public partial TcpFlags Flags { get; set; }    // ? composed!
        [BitField(0, 15)]  public partial ushort WindowSize { get; set; } // receive window size
    }

    #endregion

    #region IPv4Flags Tests

    [Fact]
    public void IPv4Flags_DontFragment()
    {
        var flags = IPv4Flags.Zero.WithDontFragment(true);
        flags.DontFragment.Should().BeTrue();
        flags.MoreFragments.Should().BeFalse();
        flags.Reserved.Should().BeFalse();
        ((byte)flags).Should().Be(0x02, "DF is bit 1");
    }

    [Fact]
    public void IPv4Flags_UndefinedBitsZeroed()
    {
        // IPv4Flags uses byte storage but only defines 3 bits (0-2).
        // UndefinedBitsMustBe.Zeroes ensures bits 3-7 are always zero.
        var flags = new IPv4Flags(0xFF);
        ((byte)flags).Should().Be(0x07, "only 3 defined bits should survive");
    }

    [Fact]
    public void IPv4Flags_BitwiseCombine()
    {
        var flags = IPv4Flags.DontFragmentBit | IPv4Flags.MoreFragmentsBit;
        flags.DontFragment.Should().BeTrue();
        flags.MoreFragments.Should().BeTrue();
    }

    #endregion

    #region TcpFlags Tests

    [Fact]
    public void TcpFlags_SynAck()
    {
        var flags = TcpFlags.Zero.WithSYN(true).WithACK(true);
        flags.SYN.Should().BeTrue();
        flags.ACK.Should().BeTrue();
        flags.FIN.Should().BeFalse();
        flags.PSH.Should().BeFalse();
        ((ushort)flags).Should().Be(0x12, "SYN=bit1 + ACK=bit4 ? 0x02|0x10");
    }

    [Fact]
    public void TcpFlags_UndefinedBitsZeroed()
    {
        // TcpFlags uses ushort storage but only defines 9 bits (0-8).
        var flags = new TcpFlags(0xFFFF);
        ((ushort)flags).Should().Be(0x01FF, "only 9 defined bits should survive");
    }

    [Fact]
    public void TcpFlags_ThreeWayHandshake()
    {
        // Step 1: Client ? Server: SYN
        var syn = TcpFlags.SYNBit;
        syn.SYN.Should().BeTrue();
        syn.ACK.Should().BeFalse();

        // Step 2: Server ? Client: SYN+ACK
        var synAck = TcpFlags.SYNBit | TcpFlags.ACKBit;
        synAck.SYN.Should().BeTrue();
        synAck.ACK.Should().BeTrue();

        // Step 3: Client ? Server: ACK
        var ack = TcpFlags.ACKBit;
        ack.ACK.Should().BeTrue();
        ack.SYN.Should().BeFalse();
    }

    [Fact]
    public void TcpFlags_DataPush()
    {
        // PSH+ACK: push data to the application layer immediately
        var flags = TcpFlags.PSHBit | TcpFlags.ACKBit;
        flags.PSH.Should().BeTrue();
        flags.ACK.Should().BeTrue();
        flags.SYN.Should().BeFalse();
        flags.FIN.Should().BeFalse();
    }

    #endregion

    #region IPv4 Header Word Tests

    [Fact]
    public void IPv4VersionWord_RawBitLayout()
    {
        // Version=4 (0100) at bits 28-31, IHL=5 (0101) at bits 24-27
        // Binary: 0100_0101_0000_0000_0000_0000_0000_0000
        var word = IPv4VersionWord.Zero
            .WithVersion(4)
            .WithIHL(5);

        ((uint)word).Should().Be(0x4500_0000);
    }

    [Fact]
    public void IPv4VersionWord_RoundTrip()
    {
        var original = IPv4VersionWord.Zero
            .WithVersion(4)
            .WithIHL(5)
            .WithDSCP(46)              // Expedited Forwarding
            .WithECN(0)
            .WithTotalLength(1500);    // standard MTU

        uint raw = original;
        var restored = new IPv4VersionWord(raw);

        restored.Version.Should().Be(4);
        restored.IHL.Should().Be(5);
        restored.DSCP.Should().Be(46);
        restored.ECN.Should().Be(0);
        restored.TotalLength.Should().Be(1500);
    }

    [Fact]
    public void IPv4TtlWord_ProtocolConstants()
    {
        var tcp = IPv4TtlWord.Zero.WithTTL(64).WithProtocol(6);
        tcp.Protocol.Should().Be(6, "6 = TCP");

        var udp = IPv4TtlWord.Zero.WithTTL(128).WithProtocol(17);
        udp.Protocol.Should().Be(17, "17 = UDP");
    }

    #endregion

    #region Composition Tests

    [Fact]
    public void IPv4FragmentWord_EmbeddedFlags()
    {
        var word = IPv4FragmentWord.Zero
            .WithIdentification(0xABCD)
            .WithFlags(IPv4Flags.Zero.WithDontFragment(true))
            .WithFragmentOffset(0);

        // Read through composition: word ? Flags ? DontFragment
        word.Identification.Should().Be(0xABCD);
        word.Flags.DontFragment.Should().BeTrue();
        word.Flags.MoreFragments.Should().BeFalse();
        word.FragmentOffset.Should().Be(0);
    }

    [Fact]
    public void IPv4FragmentWord_ModifyEmbeddedFlags()
    {
        var word = IPv4FragmentWord.Zero.WithIdentification(0x1234);

        // Set flags via composition
        var flags = word.Flags;
        flags.DontFragment.Should().BeFalse("initially clear");

        word = word.WithFlags(IPv4Flags.Zero.WithDontFragment(true));
        word.Flags.DontFragment.Should().BeTrue("now set via WithFlags");
        word.Identification.Should().Be(0x1234, "other fields unchanged");
    }

    [Fact]
    public void TcpControlWord_EmbeddedFlags()
    {
        var word = TcpControlWord.Zero
            .WithDataOffset(5)
            .WithFlags(TcpFlags.Zero.WithPSH(true).WithACK(true))
            .WithWindowSize(65535);

        word.DataOffset.Should().Be(5);
        word.Reserved.Should().Be(0);
        word.Flags.PSH.Should().BeTrue();
        word.Flags.ACK.Should().BeTrue();
        word.Flags.SYN.Should().BeFalse();
        word.WindowSize.Should().Be(65535);
    }

    [Fact]
    public void TcpControlWord_FlagsRoundTrip()
    {
        // Build flags, embed in control word, extract, verify
        var originalFlags = TcpFlags.Zero
            .WithSYN(true)
            .WithACK(true)
            .WithECE(true);

        var word = TcpControlWord.Zero
            .WithDataOffset(5)
            .WithFlags(originalFlags);

        var extracted = word.Flags;
        extracted.SYN.Should().BeTrue();
        extracted.ACK.Should().BeTrue();
        extracted.ECE.Should().BeTrue();
        extracted.FIN.Should().BeFalse();
        extracted.PSH.Should().BeFalse();
    }

    #endregion

    #region Full Packet Assembly: Ethernet ? IPv4 ? TCP ? "Hello world!"

    [Fact]
    public void HelloWorldPacket_AssembleAndVerify()
    {
        // ???????????????????????????????????????????????????????????????
        // Assemble: Ethernet ? IPv4 ? TCP ? "Hello world!"
        //
        // This test constructs the header fields for a complete network
        // packet and verifies every field through BitFields composition.
        // ???????????????????????????????????????????????????????????????

        byte[] payload = Encoding.ASCII.GetBytes("Hello world!");
        payload.Length.Should().Be(12);

        // ?? Layer 4: TCP ?????????????????????????????????????????????
        ushort tcpSrcPort = 49152;  // ephemeral source port
        ushort tcpDstPort = 80;     // HTTP destination port

        var tcpControl = TcpControlWord.Zero
            .WithDataOffset(5)      // 20-byte TCP header (minimum)
            .WithFlags(TcpFlags.Zero.WithPSH(true).WithACK(true))
            .WithWindowSize(65535);

        int tcpHeaderBytes = tcpControl.DataOffset * 4;
        tcpHeaderBytes.Should().Be(20);

        // ?? Layer 3: IPv4 ????????????????????????????????????????????
        int ipPayloadBytes = tcpHeaderBytes + payload.Length; // 32

        var ipVersion = IPv4VersionWord.Zero
            .WithVersion(4)
            .WithIHL(5)             // 20-byte IP header (minimum)
            .WithTotalLength((ushort)(5 * 4 + ipPayloadBytes));

        var ipFragment = IPv4FragmentWord.Zero
            .WithIdentification(0x1A2B)
            .WithFlags(IPv4Flags.Zero.WithDontFragment(true));

        var ipTtl = IPv4TtlWord.Zero
            .WithTTL(64)
            .WithProtocol(6);       // TCP

        int ipHeaderBytes = ipVersion.IHL * 4;
        ipHeaderBytes.Should().Be(20);

        // ?? Layer 2: Ethernet ????????????????????????????????????????
        const int ethernetHeaderBytes = 14; // 6 dst + 6 src + 2 EtherType
        ushort etherType = 0x0800;          // IPv4

        // ???????????????????????????????????????????????????????????????
        // Verify the complete packet
        // ???????????????????????????????????????????????????????????????

        // -- Frame size --
        int totalFrameBytes = ethernetHeaderBytes + ipVersion.TotalLength;
        totalFrameBytes.Should().Be(66, "14 (Eth) + 20 (IP) + 20 (TCP) + 12 (payload)");

        // -- Ethernet --
        etherType.Should().Be(0x0800);

        // -- IPv4 version word --
        ipVersion.Version.Should().Be(4);
        ipVersion.IHL.Should().Be(5);
        ipVersion.DSCP.Should().Be(0, "best effort");
        ipVersion.ECN.Should().Be(0);
        ipVersion.TotalLength.Should().Be(52, "20 (IP) + 20 (TCP) + 12 (payload)");

        // -- IPv4 fragment word (with embedded IPv4Flags) --
        ipFragment.Identification.Should().Be(0x1A2B);
        ipFragment.Flags.DontFragment.Should().BeTrue("do not fragment this packet");
        ipFragment.Flags.MoreFragments.Should().BeFalse("single unfragmented packet");
        ipFragment.Flags.Reserved.Should().BeFalse("RFC 791: must be zero");
        ipFragment.FragmentOffset.Should().Be(0);

        // -- IPv4 TTL/protocol --
        ipTtl.TTL.Should().Be(64);
        ipTtl.Protocol.Should().Be(6, "TCP");

        // -- TCP (with embedded TcpFlags) --
        tcpSrcPort.Should().Be(49152);
        tcpDstPort.Should().Be(80);
        tcpControl.DataOffset.Should().Be(5);
        tcpControl.Reserved.Should().Be(0);
        tcpControl.Flags.PSH.Should().BeTrue("push data to application");
        tcpControl.Flags.ACK.Should().BeTrue("acknowledgment valid");
        tcpControl.Flags.SYN.Should().BeFalse("not a connection setup");
        tcpControl.Flags.FIN.Should().BeFalse("not closing");
        tcpControl.Flags.RST.Should().BeFalse("not resetting");
        tcpControl.WindowSize.Should().Be(65535);

        // -- Application payload --
        Encoding.ASCII.GetString(payload).Should().Be("Hello world!");
    }

    #endregion
}
