using System.Diagnostics.Contracts;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Stardust.Utilities.Tests;


public partial class ExtensionsTests
{
    #region Protocol Header Struct Definitions

    /// <summary>IPv4 header flags (3 bits): Reserved, Don't Fragment, More Fragments.</summary>
    [BitFields(typeof(byte), UndefinedBitsMustBe.Zeroes)]
    public partial struct IPv4Flags
    {
        [BitFlag(0)] public partial bool MoreFragments { get; set; } // MF
        [BitFlag(1)] public partial bool DontFragment { get; set; }  // DF
        [BitFlag(2, MustBe.Zero)] public partial bool Reserved { get; set; } // must be 0 even though defined
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
        [BitField(0, 15)] public partial ushort TotalLength { get; set; } // total packet length in bytes
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
        [BitField(0, 12)] public partial ushort FragmentOffset { get; set; } // offset in 8-byte units
    }

    /// <summary>
    /// IPv4 header word 2 (32 bits): TTL(8) | Protocol(8) | HeaderChecksum(16).
    /// </summary>
    [BitFields(typeof(uint))]
    public partial struct IPv4TtlWord
    {
        [BitField(24, 31)] public partial byte TTL { get; set; }              // time to live (hop limit)
        [BitField(16, 23)] public partial byte Protocol { get; set; }         // 6 = TCP, 17 = UDP
        [BitField(0, 15)] public partial ushort HeaderChecksum { get; set; } // one's complement checksum
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
        [BitField(0, 15)] public partial ushort WindowSize { get; set; } // receive window size
    }

    /// <summary>TCP control flags (9 bits): FIN, SYN, RST, PSH, ACK, URG, ECE, CWR, NS.</summary>
    [BitFieldsView]
    public partial record struct TcpFlagsView
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
    /// TCP header control word (32 bits): DataOffset(4) | Reserved(3) | Flags(9) | WindowSize(16).
    /// Demonstrates composition: TcpFlags embedded in a 9-bit field.
    /// </summary>
    [BitFieldsView]
    public partial record struct TcpControlWordView
    {
        [BitField(28, 31)] public partial byte DataOffset { get; set; }   // header length in 32-bit words
        [BitField(25, 27)] public partial byte Reserved { get; set; }     // must be 0
        [BitField(16, 24)] public partial TcpFlags Flags { get; set; }    // ? composed!
        [BitField(0, 15)] public partial ushort WindowSize { get; set; } // receive window size
    }

    #endregion

    [Theory]
    [InlineData(typeof(int), null, false, false, false, null, null)]
    [InlineData(typeof(TcpControlWord), null, true, false, true, null, null)]
    [InlineData(typeof(TcpControlWord), "DataOffset", true, false, true, true, false)]
    [InlineData(typeof(TcpControlWordView), null, false, true, true, null, null)]
    [InlineData(typeof(TcpControlWordView), "DataOffset", false, true, true, true, false)]
    [InlineData(typeof(TcpControlWordView), null, false, true, true, false, false)]
    public void Extension_MetaData_Works_For_Types(Type bitType, string? propName, bool hasBitFields, bool hasBitFieldsView, bool isBitType, bool? fieldIsBitField, bool? fieldIsBitFlag)
    {
        Assert.Equal(hasBitFields, bitType.IsBitFieldsType());
        Assert.Equal(hasBitFieldsView, bitType.IsBitFieldsViewType());
        Assert.Equal(isBitType, bitType.IsBitsType());
        Assert.Equal(hasBitFields, bitType.GetAttribute<BitFieldsAttribute>() != null);
        Assert.Equal(hasBitFieldsView, bitType.GetAttribute<BitFieldsViewAttribute>() != null);
        if (fieldIsBitField != null && fieldIsBitFlag != null) 
        {
            if (propName == null) propName = "";
            Assert.Equal(fieldIsBitField, bitType.IsBitField(propName));
            Assert.Equal(fieldIsBitFlag, bitType.IsBitFlag(propName));
        }
    }

    [Theory]
    [InlineData(typeof(TcpControlWord), "Flags", 16, 24, false)]
    [InlineData(typeof(TcpControlWord), "WindowSize", 0, 15, false)]
    [InlineData(typeof(TcpControlWord), "DataOffset", 28, 31, false)]
    [InlineData(typeof(TcpControlWordView), "Flags", 16, 24, false)]
    [InlineData(typeof(TcpControlWordView), "WindowSize", 0, 15, false)]
    [InlineData(typeof(TcpControlWordView), "DataOffset", 28, 31, false)]
    [InlineData(typeof(TcpFlagsView), "FIN", 0, 0, true)]
    [InlineData(typeof(TcpFlags), "PSH", 3, 3, true)]
    [InlineData(typeof(TcpFlagsView), "NS", 8, 8, true)]
    public void Extension_MetaData_Works_For_Bits(Type bitType, string propName, int startBit, int endBit, bool isFlag)
    {
        Assert.Equal(isFlag, bitType.IsBitFlag(propName));
        var result = bitType.GetStartAndEndBits(propName);
        Assert.True(result.IsSuccess, $"Expected to find bit field or flag '{propName}' in type '{bitType.Name}'");
        Assert.Equal(startBit, result.Value.startBit);
        Assert.Equal(endBit, result.Value.endBit);
    }

    [Fact]
    public void Extensions_MetaData_Works()
    {
        Assert.True(typeof(TcpControlWord).IsBitFieldsType());
        Assert.True(typeof(IPv4VersionWord).IsBitFieldsType());
        Assert.NotNull(typeof(TcpControlWord).GetAttribute<BitFieldsAttribute>());
        Assert.True(typeof(TcpControlWord).IsBitsType());

        Assert.NotNull(typeof(TcpControlWordView).GetAttribute<BitFieldsViewAttribute>());
        Assert.True(typeof(TcpControlWordView).IsBitsType());

        Assert.True(typeof(TcpControlWordView).IsBitFieldsViewType());

        Assert.True(typeof(TcpControlWord).IsBitField("DataOffset"));
        Assert.True(typeof(TcpControlWordView).IsBitField("DataOffset"));
    }

    // ── GetBitFieldInfoFromAttributes ───────────────────────────

    [Fact]
    public void GetBitFieldInfoFromAttributes_NonBitsType_ReturnsEmpty()
    {
        var fields = typeof(int).GetBitFieldInfoFromAttributes();
        Assert.Empty(fields);
    }

    [Fact]
    public void GetBitFieldInfoFromAttributes_NullType_ReturnsEmpty()
    {
        Type? t = null;
        var fields = t!.GetBitFieldInfoFromAttributes();
        Assert.Empty(fields);
    }

    [Fact]
    public void GetBitFieldInfoFromAttributes_BitFieldsStruct_ReturnsCorrectCount()
    {
        var fields = typeof(TcpControlWord).GetBitFieldInfoFromAttributes();
        Assert.Equal(4, fields.Length);
    }

    [Fact]
    public void GetBitFieldInfoFromAttributes_BitFieldsViewStruct_ReturnsCorrectCount()
    {
        var fields = typeof(TcpControlWordView).GetBitFieldInfoFromAttributes();
        Assert.Equal(4, fields.Length);
    }

    [Fact]
    public void GetBitFieldInfoFromAttributes_SortedByStartBit()
    {
        var fields = typeof(TcpControlWord).GetBitFieldInfoFromAttributes();
        for (int i = 1; i < fields.Length; i++)
            Assert.True(fields[i].StartBit >= fields[i - 1].StartBit);
    }

    [Fact]
    public void GetBitFieldInfoFromAttributes_BitFieldsStruct_CorrectStartEndBits()
    {
        var fields = typeof(TcpControlWord).GetBitFieldInfoFromAttributes();
        var windowSize = fields.First(f => f.Name == "WindowSize");
        Assert.Equal(0, windowSize.StartBit);
        Assert.Equal(15, windowSize.EndBit);
        Assert.False(windowSize.IsFlag);
    }

    [Fact]
    public void GetBitFieldInfoFromAttributes_FlagStruct_CorrectFlags()
    {
        var fields = typeof(TcpFlags).GetBitFieldInfoFromAttributes();
        Assert.Equal(9, fields.Length);
        var fin = fields.First(f => f.Name == "FIN");
        Assert.Equal(0, fin.StartBit);
        Assert.Equal(0, fin.EndBit);
        Assert.True(fin.IsFlag);
        Assert.Equal("bool", fin.PropertyType);
    }

    [Fact]
    public void GetBitFieldInfoFromAttributes_StructTotalBits_SetFromStorageType()
    {
        var fields = typeof(TcpControlWord).GetBitFieldInfoFromAttributes();
        Assert.All(fields, f => Assert.Equal(32, f.StructTotalBits));
    }

    [Fact]
    public void GetBitFieldInfoFromAttributes_ViewStruct_InfersTotalBits()
    {
        var fields = typeof(TcpControlWordView).GetBitFieldInfoFromAttributes();
        Assert.All(fields, f => Assert.True(f.StructTotalBits > 0));
    }

    [Fact]
    public void GetBitFieldInfoFromAttributes_PreservesDescription()
    {
        var fields = typeof(IPv4VersionWord).GetBitFieldInfoFromAttributes();
        // IPv4VersionWord fields don't have descriptions in the test struct,
        // but TcpFlags flags don't either. Let's verify no crash and null is preserved.
        Assert.All(fields, f => Assert.Null(f.Description));
    }

    [Fact]
    public void GetBitFieldInfoFromAttributes_PreservesByteOrder()
    {
        var fields = typeof(TcpControlWord).GetBitFieldInfoFromAttributes();
        Assert.All(fields, f => Assert.Equal(ByteOrder.LittleEndian, f.ByteOrder));
    }

    [Fact]
    public void GetBitFieldInfoFromAttributes_PreservesBitOrder()
    {
        var fields = typeof(TcpControlWord).GetBitFieldInfoFromAttributes();
        Assert.All(fields, f => Assert.Equal(BitOrder.BitZeroIsLsb, f.BitOrder));
    }

    [Fact]
    public void GetBitFieldInfoFromAttributes_UndefinedBitsMustBe_ReadFromAttribute()
    {
        var fields = typeof(TcpFlags).GetBitFieldInfoFromAttributes();
        Assert.All(fields, f => Assert.Equal(UndefinedBitsMustBe.Zeroes, f.StructUndefinedMustBe));
    }

    [Fact]
    public void GetBitFieldInfoFromAttributes_UndefinedBitsMustBe_DefaultIsAny()
    {
        var fields = typeof(IPv4VersionWord).GetBitFieldInfoFromAttributes();
        Assert.All(fields, f => Assert.Equal(UndefinedBitsMustBe.Any, f.StructUndefinedMustBe));
    }

    [Fact]
    public void GetBitFieldInfoFromAttributes_MustBe_ReadFromFlagAttribute()
    {
        var fields = typeof(IPv4Flags).GetBitFieldInfoFromAttributes();
        var reserved = fields.First(f => f.Name == "Reserved");
        Assert.Equal(MustBe.Zero, reserved.FieldMustBe);
    }

    [Fact]
    public void GetBitFieldInfoFromAttributes_MatchesGeneratedFields()
    {
        var fromAttrs = typeof(TcpFlags).GetBitFieldInfoFromAttributes();
        var fromGenerated = TcpFlags.Fields.ToArray();
        Assert.Equal(fromGenerated.Length, fromAttrs.Length);
        for (int i = 0; i < fromGenerated.Length; i++)
        {
            Assert.Equal(fromGenerated[i].Name, fromAttrs[i].Name);
            Assert.Equal(fromGenerated[i].StartBit, fromAttrs[i].StartBit);
            Assert.Equal(fromGenerated[i].EndBit, fromAttrs[i].EndBit);
            Assert.Equal(fromGenerated[i].IsFlag, fromAttrs[i].IsFlag);
            Assert.Equal(fromGenerated[i].BitOrder, fromAttrs[i].BitOrder);
            Assert.Equal(fromGenerated[i].ByteOrder, fromAttrs[i].ByteOrder);
        }
    }

    // ── GetFieldInfo ────────────────────────────────────────────

    [Fact]
    public void GetFieldInfo_NullType_ReturnsError()
    {
        Type? t = null;
        var result = t!.GetFieldInfo();
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void GetFieldInfo_NonBitsType_ReturnsError()
    {
        var result = typeof(int).GetFieldInfo();
        Assert.True(result.IsFailure);
        Assert.Contains("Int32", result.Error);
    }

    [Fact]
    public void GetFieldInfo_BitFieldsStruct_ReturnsSuccess()
    {
        var result = typeof(TcpControlWord).GetFieldInfo();
        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.Length);
    }

    [Fact]
    public void GetFieldInfo_BitFieldsViewStruct_ReturnsSuccess()
    {
        var result = typeof(TcpControlWordView).GetFieldInfo();
        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.Length);
    }

    [Fact]
    public void GetFieldInfo_FlagStruct_ReturnsAllFlags()
    {
        var result = typeof(TcpFlags).GetFieldInfo();
        Assert.True(result.IsSuccess);
        Assert.Equal(9, result.Value.Length);
        Assert.Contains(result.Value, f => f.Name == "FIN" && f.IsFlag);
    }

    [Fact]
    public void GetFieldInfo_MatchesGeneratedFields()
    {
        var result = typeof(TcpFlags).GetFieldInfo();
        Assert.True(result.IsSuccess);
        var fromGenerated = TcpFlags.Fields.ToArray();
        Assert.Equal(fromGenerated.Length, result.Value.Length);
        for (int i = 0; i < fromGenerated.Length; i++)
        {
            Assert.Equal(fromGenerated[i].Name, result.Value[i].Name);
            Assert.Equal(fromGenerated[i].StartBit, result.Value[i].StartBit);
            Assert.Equal(fromGenerated[i].EndBit, result.Value[i].EndBit);
        }
    }
}
