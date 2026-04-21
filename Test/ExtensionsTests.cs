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
        [BitField(28, End = 31)] public partial byte Version { get; set; }       // always 4 for IPv4
        [BitField(24, End = 27)] public partial byte IHL { get; set; }           // header length in 32-bit words
        [BitField(18, End = 23)] public partial byte DSCP { get; set; }          // differentiated services
        [BitField(16, End = 17)] public partial byte ECN { get; set; }           // congestion notification
        [BitField(0, End = 15)] public partial ushort TotalLength { get; set; } // total packet length in bytes
    }

    /// <summary>
    /// IPv4 header word 1 (32 bits): Identification(16) | Flags(3) | FragmentOffset(13).
    /// Demonstrates composition: IPv4Flags embedded in a 3-bit field.
    /// </summary>
    [BitFields(typeof(uint))]
    public partial struct IPv4FragmentWord
    {
        [BitField(16, End = 31)] public partial ushort Identification { get; set; } // packet ID for reassembly
        [BitField(13, End = 15)] public partial IPv4Flags Flags { get; set; }       // ? composed!
        [BitField(0, End = 12)] public partial ushort FragmentOffset { get; set; } // offset in 8-byte units
    }

    /// <summary>
    /// IPv4 header word 2 (32 bits): TTL(8) | Protocol(8) | HeaderChecksum(16).
    /// </summary>
    [BitFields(typeof(uint))]
    public partial struct IPv4TtlWord
    {
        [BitField(24, End = 31)] public partial byte TTL { get; set; }              // time to live (hop limit)
        [BitField(16, End = 23)] public partial byte Protocol { get; set; }         // 6 = TCP, 17 = UDP
        [BitField(0, End = 15)] public partial ushort HeaderChecksum { get; set; } // one's complement checksum
    }

    /// <summary>
    /// TCP header control word (32 bits): DataOffset(4) | Reserved(3) | Flags(9) | WindowSize(16).
    /// Demonstrates composition: TcpFlags embedded in a 9-bit field.
    /// </summary>
    [BitFields(typeof(uint))]
    public partial struct TcpControlWord
    {
        [BitField(28, End = 31)] public partial byte DataOffset { get; set; }   // header length in 32-bit words
        [BitField(25, End = 27)] public partial byte Reserved { get; set; }     // must be 0
        [BitField(16, End = 24)] public partial TcpFlags Flags { get; set; }    // ? composed!
        [BitField(0, End = 15)] public partial ushort WindowSize { get; set; } // receive window size
    }

    /// <summary>TCP control flags (9 bits): FIN, SYN, RST, PSH, ACK, URG, ECE, CWR, NS.</summary>
    [BitFields]
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
    [BitFields]
    public partial record struct TcpControlWordView
    {
        [BitField(28, End = 31)] public partial byte DataOffset { get; set; }   // header length in 32-bit words
        [BitField(25, End = 27)] public partial byte Reserved { get; set; }     // must be 0
        [BitField(16, End = 24)] public partial TcpFlags Flags { get; set; }    // ? composed!
        [BitField(0, End = 15)] public partial ushort WindowSize { get; set; } // receive window size
    }

    #endregion

    [Theory]
    [InlineData(typeof(int), null, false, false, null, null)]
    [InlineData(typeof(TcpControlWord), null, true, true, null, null)]
    [InlineData(typeof(TcpControlWord), "DataOffset", true, true, true, false)]
    [InlineData(typeof(TcpControlWordView), null, true, true, null, null)]
    [InlineData(typeof(TcpControlWordView), "DataOffset", true, true, true, false)]
    public void Extension_MetaData_Works_For_Types(Type bitType, string? propName, bool hasBitFields, bool isBitType, bool? fieldIsBitField, bool? fieldIsBitFlag)
    {
        Assert.Equal(hasBitFields, bitType.IsBitFieldsType());
        Assert.Equal(isBitType, bitType.IsBitsType());
        Assert.Equal(hasBitFields, bitType.GetAttribute<BitFieldsAttribute>() != null);
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
    public void Extension_MetaData_Works_For_Bits(Type bitType, string propName, int start, int end, bool isFlag)
    {
        Assert.Equal(isFlag, bitType.IsBitFlag(propName));
        var result = bitType.GetStartAndEndBits(propName);
        Assert.True(result.IsOk, $"Expected to find bit field or flag '{propName}' in type '{bitType.Name}'");
        Assert.Equal(start, result.Value.start);
        Assert.Equal(end, result.Value.end);
    }

    [Fact]
    public void Extensions_MetaData_Works()
    {
        Assert.True(typeof(TcpControlWord).IsBitFieldsType());
        Assert.True(typeof(IPv4VersionWord).IsBitFieldsType());
        Assert.NotNull(typeof(TcpControlWord).GetAttribute<BitFieldsAttribute>());
        Assert.True(typeof(TcpControlWord).IsBitsType());

        Assert.NotNull(typeof(TcpControlWordView).GetAttribute<BitFieldsAttribute>());
        Assert.True(typeof(TcpControlWordView).IsBitsType());

        Assert.True(typeof(TcpControlWordView).IsBitFieldsType());

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
    public void GetBitFieldInfoFromAttributes_RecordStructView_ReturnsCorrectCount()
    {
        var fields = typeof(TcpControlWordView).GetBitFieldInfoFromAttributes();
        Assert.Equal(4, fields.Length);
    }

    [Fact]
    public void GetBitFieldInfoFromAttributes_SortedByStartBit()
    {
        var fields = typeof(TcpControlWord).GetBitFieldInfoFromAttributes();
        for (int i = 1; i < fields.Length; i++)
            Assert.True(fields[i].Start >= fields[i - 1].Start);
    }

    [Fact]
    public void GetBitFieldInfoFromAttributes_BitFieldsStruct_CorrectStartEndBits()
    {
        var fields = typeof(TcpControlWord).GetBitFieldInfoFromAttributes();
        var windowSize = fields.First(f => f.Name == "WindowSize");
        Assert.Equal(0, windowSize.Start);
        Assert.Equal(15, windowSize.End);
        Assert.False(windowSize.IsFlag);
    }

    [Fact]
    public void GetBitFieldInfoFromAttributes_FlagStruct_CorrectFlags()
    {
        var fields = typeof(TcpFlags).GetBitFieldInfoFromAttributes();
        Assert.Equal(9, fields.Length);
        var fin = fields.First(f => f.Name == "FIN");
        Assert.Equal(0, fin.Start);
        Assert.Equal(0, fin.End);
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
            Assert.Equal(fromGenerated[i].Start, fromAttrs[i].Start);
            Assert.Equal(fromGenerated[i].End, fromAttrs[i].End);
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
        Assert.True(result.IsErr);
    }

    [Fact]
    public void GetFieldInfo_NonBitsType_ReturnsError()
    {
        var result = typeof(int).GetFieldInfo();
        Assert.True(result.IsErr);
        Assert.Contains("Int32", result.Error);
    }

    [Fact]
    public void GetFieldInfo_BitFieldsStruct_ReturnsSuccess()
    {
        var result = typeof(TcpControlWord).GetFieldInfo();
        Assert.True(result.IsOk);
        Assert.Equal(4, result.Value.Length);
    }

    [Fact]
    public void GetFieldInfo_RecordStructView_ReturnsSuccess()
    {
        var result = typeof(TcpControlWordView).GetFieldInfo();
        Assert.True(result.IsOk);
        Assert.Equal(4, result.Value.Length);
    }

    [Fact]
    public void GetFieldInfo_FlagStruct_ReturnsAllFlags()
    {
        var result = typeof(TcpFlags).GetFieldInfo();
        Assert.True(result.IsOk);
        Assert.Equal(9, result.Value.Length);
        Assert.Contains(result.Value, f => f.Name == "FIN" && f.IsFlag);
    }

    [Fact]
    public void GetFieldInfo_MatchesGeneratedFields()
    {
        var result = typeof(TcpFlags).GetFieldInfo();
        Assert.True(result.IsOk);
        var fromGenerated = TcpFlags.Fields.ToArray();
        Assert.Equal(fromGenerated.Length, result.Value.Length);
        for (int i = 0; i < fromGenerated.Length; i++)
        {
            Assert.Equal(fromGenerated[i].Name, result.Value[i].Name);
            Assert.Equal(fromGenerated[i].Start, result.Value[i].Start);
            Assert.Equal(fromGenerated[i].End, result.Value[i].End);
        }
    }

    // ── Hi / Lo / SetHi / SetLo ─────────────────────────────────

    #region Native Type Hi / Lo / SetHi / SetLo

    [Fact]
    public void Ulong_Hi_ReturnsUpperUint()
    {
        ulong value = 0x123456789ABCDEF0UL;
        uint hi = value.Hi();
        hi.Should().Be(0x12345678U);
    }

    [Fact]
    public void Ulong_Lo_ReturnsLowerUint()
    {
        ulong value = 0x123456789ABCDEF0UL;
        uint lo = value.Lo();
        lo.Should().Be(0x9ABCDEF0U);
    }

    [Fact]
    public void Long_Hi_ReturnsUpperUint()
    {
        long value = 0x123456789ABCDEF0L;
        uint hi = value.Hi();
        hi.Should().Be(0x12345678U);
    }

    [Fact]
    public void Long_Lo_ReturnsLowerUint()
    {
        long value = 0x123456789ABCDEF0L;
        uint lo = value.Lo();
        lo.Should().Be(0x9ABCDEF0U);
    }

    [Fact]
    public void Ulong_SetHi_SetsUpperUint()
    {
        ulong value = 0x123456789ABCDEF0UL;
        ulong result = value.SetHi(0xDEADBEEFU);
        result.Should().Be(0xDEADBEEF9ABCDEF0UL);
    }

    [Fact]
    public void Ulong_SetLo_SetsLowerUint()
    {
        ulong value = 0x123456789ABCDEF0UL;
        ulong result = value.SetLo(0xCAFEBABEU);
        result.Should().Be(0x12345678CAFEBABEU);
    }

    [Fact]
    public void Long_SetHi_SetsUpperUint()
    {
        long value = 0x123456789ABCDEF0L;
        long result = value.SetHi(0x00000001U);
        result.Should().Be(0x000000019ABCDEF0L);
    }

    [Fact]
    public void Long_SetLo_SetsLowerUint()
    {
        long value = 0x123456789ABCDEF0L;
        long result = value.SetLo(0x00000001U);
        result.Should().Be(0x1234567800000001L);
    }

    [Fact]
    public void UInt128_Hi_ReturnsUpperUlong()
    {
        UInt128 value = ((UInt128)0x0123456789ABCDEFUL << 64) | 0xFEDCBA9876543210UL;
        value.Hi().Should().Be(0x0123456789ABCDEFUL);
    }

    [Fact]
    public void UInt128_Lo_ReturnsLowerUlong()
    {
        UInt128 value = ((UInt128)0x0123456789ABCDEFUL << 64) | 0xFEDCBA9876543210UL;
        value.Lo().Should().Be(0xFEDCBA9876543210UL);
    }

    [Fact]
    public void Int128_Hi_ReturnsUpperUlong()
    {
        Int128 value = (Int128)(((UInt128)0x0123456789ABCDEFUL << 64) | 0xFEDCBA9876543210UL);
        value.Hi().Should().Be(0x0123456789ABCDEFUL);
    }

    [Fact]
    public void Int128_Lo_ReturnsLowerUlong()
    {
        Int128 value = (Int128)(((UInt128)0x0123456789ABCDEFUL << 64) | 0xFEDCBA9876543210UL);
        value.Lo().Should().Be(0xFEDCBA9876543210UL);
    }

    [Fact]
    public void UInt128_SetHi_ReplacesUpperHalf()
    {
        UInt128 value = ((UInt128)0x1111111111111111UL << 64) | 0x2222222222222222UL;
        UInt128 result = value.SetHi(0xAAAAAAAAAAAAAAAAUL);
        result.Should().Be(((UInt128)0xAAAAAAAAAAAAAAAAUL << 64) | 0x2222222222222222UL);
    }

    [Fact]
    public void UInt128_SetLo_ReplacesLowerHalf()
    {
        UInt128 value = ((UInt128)0x1111111111111111UL << 64) | 0x2222222222222222UL;
        UInt128 result = value.SetLo(0xBBBBBBBBBBBBBBBBUL);
        result.Should().Be(((UInt128)0x1111111111111111UL << 64) | 0xBBBBBBBBBBBBBBBBUL);
    }

    [Fact]
    public void Int128_SetHi_ReplacesUpperHalf()
    {
        Int128 value = (Int128)0;
        Int128 result = value.SetHi(0x0000000000000001UL);
        result.Should().Be((Int128)((UInt128)1UL << 64));
    }

    [Fact]
    public void Int128_SetLo_ReplacesLowerHalf()
    {
        Int128 value = (Int128)0;
        Int128 result = value.SetLo(0x00000000DEADBEEFU);
        result.Should().Be((Int128)0x00000000DEADBEEFU);
    }

    #endregion

    #region Native Type Hi / Lo / SetHi / SetLo (256-bit)

    [Fact]
    public void UInt256_Hi_ReturnsUpperUInt128()
    {
        UInt256 value = new(0x0102030405060708UL, 0x090A0B0C0D0E0F10UL,
                            0x1112131415161718UL, 0x191A1B1C1D1E1F20UL);
        UInt128 hi = value.Hi();
        UInt128 expected = ((UInt128)0x0102030405060708UL << 64) | 0x090A0B0C0D0E0F10UL;
        hi.Should().Be(expected);
    }

    [Fact]
    public void UInt256_Lo_ReturnsLowerUInt128()
    {
        UInt256 value = new(0x0102030405060708UL, 0x090A0B0C0D0E0F10UL,
                            0x1112131415161718UL, 0x191A1B1C1D1E1F20UL);
        UInt128 lo = value.Lo();
        UInt128 expected = ((UInt128)0x1112131415161718UL << 64) | 0x191A1B1C1D1E1F20UL;
        lo.Should().Be(expected);
    }

    [Fact]
    public void UInt256_SetHi_ReplacesUpperHalf()
    {
        UInt256 value = new(0x1111111111111111UL, 0x2222222222222222UL,
                            0x3333333333333333UL, 0x4444444444444444UL);
        UInt128 newHi = ((UInt128)0xAAAAAAAAAAAAAAAAUL << 64) | 0xBBBBBBBBBBBBBBBBUL;
        UInt256 result = value.SetHi(newHi);
        result.Hi().Should().Be(newHi);
        result.Lo().Should().Be(value.Lo());
    }

    [Fact]
    public void UInt256_SetLo_ReplacesLowerHalf()
    {
        UInt256 value = new(0x1111111111111111UL, 0x2222222222222222UL,
                            0x3333333333333333UL, 0x4444444444444444UL);
        UInt128 newLo = ((UInt128)0xCCCCCCCCCCCCCCCCUL << 64) | 0xDDDDDDDDDDDDDDDDUL;
        UInt256 result = value.SetLo(newLo);
        result.Hi().Should().Be(value.Hi());
        result.Lo().Should().Be(newLo);
    }

    [Fact]
    public void UInt256_Hi_Zero_ReturnsZero()
    {
        UInt256.MinValue.Hi().Should().Be(UInt128.Zero);
    }

    [Fact]
    public void UInt256_Lo_Zero_ReturnsZero()
    {
        UInt256.MinValue.Lo().Should().Be(UInt128.Zero);
    }

    [Fact]
    public void UInt256_Hi_MaxValue_ReturnsMaxUInt128()
    {
        UInt256.MaxValue.Hi().Should().Be(UInt128.MaxValue);
    }

    [Fact]
    public void UInt256_Lo_MaxValue_ReturnsMaxUInt128()
    {
        UInt256.MaxValue.Lo().Should().Be(UInt128.MaxValue);
    }

    [Fact]
    public void Int256_Hi_ReturnsUpperUInt128()
    {
        // Use a positive value so the upper half has known bits
        Int256 value = new((UInt128)0x0102030405060708UL, (UInt128)0x0000000000000001UL);
        value.Hi().Should().Be((UInt128)0x0102030405060708UL);
    }

    [Fact]
    public void Int256_Lo_ReturnsLowerUInt128()
    {
        Int256 value = new((UInt128)0x0102030405060708UL, (UInt128)0xFEDCBA9876543210UL);
        value.Lo().Should().Be((UInt128)0xFEDCBA9876543210UL);
    }

    [Fact]
    public void Int256_SetHi_ReplacesUpperHalf()
    {
        Int256 value = new((UInt128)0x1111111111111111UL, (UInt128)0x2222222222222222UL);
        UInt128 newHi = (UInt128)0xAAAAAAAAAAAAAAAAUL;
        Int256 result = value.SetHi(newHi);
        result.Hi().Should().Be(newHi);
        result.Lo().Should().Be(value.Lo());
    }

    [Fact]
    public void Int256_SetLo_ReplacesLowerHalf()
    {
        Int256 value = new((UInt128)0x1111111111111111UL, (UInt128)0x2222222222222222UL);
        UInt128 newLo = (UInt128)0xCCCCCCCCCCCCCCCCUL;
        Int256 result = value.SetLo(newLo);
        result.Hi().Should().Be(value.Hi());
        result.Lo().Should().Be(newLo);
    }

    [Fact]
    public void Int256_Hi_Negative_ReturnsSignBitHalf()
    {
        Int256.MinValue.Hi().Should().Be((UInt128)1 << 127);
    }

    [Fact]
    public void Int256_Lo_Negative_ReturnsZero()
    {
        Int256.MinValue.Lo().Should().Be(UInt128.Zero);
    }

    #endregion

    #region Big-Endian Hi / Lo / SetHi / SetLo

    [Fact]
    public void UInt64Be_Hi_ReturnsUpperUInt32Be()
    {
        UInt64Be value = 0x123456789ABCDEF0UL;
        UInt32Be hi = value.Hi();
        ((uint)hi).Should().Be(0x12345678U);
    }

    [Fact]
    public void UInt64Be_Lo_ReturnsLowerUInt32Be()
    {
        UInt64Be value = 0x123456789ABCDEF0UL;
        UInt32Be lo = value.Lo();
        ((uint)lo).Should().Be(0x9ABCDEF0U);
    }

    [Fact]
    public void Int64Be_Hi_ReturnsUpperUInt32Be()
    {
        Int64Be value = 0x123456789ABCDEF0L;
        UInt32Be hi = value.Hi();
        ((uint)hi).Should().Be(0x12345678U);
    }

    [Fact]
    public void Int64Be_Lo_ReturnsLowerUInt32Be()
    {
        Int64Be value = 0x123456789ABCDEF0L;
        UInt32Be lo = value.Lo();
        ((uint)lo).Should().Be(0x9ABCDEF0U);
    }

    [Fact]
    public void UInt64Be_SetHi_SetsUpperUInt32Be()
    {
        UInt64Be value = 0x123456789ABCDEF0UL;
        UInt64Be result = value.SetHi((UInt32Be)0xDEADBEEFU);
        ((ulong)result).Should().Be(0xDEADBEEF9ABCDEF0UL);
    }

    [Fact]
    public void UInt64Be_SetLo_SetsLowerUInt32Be()
    {
        UInt64Be value = 0x123456789ABCDEF0UL;
        UInt64Be result = value.SetLo((UInt32Be)0xCAFEBABEU);
        ((ulong)result).Should().Be(0x12345678CAFEBABEU);
    }

    [Fact]
    public void Int64Be_SetHi_SetsUpperUInt32Be()
    {
        Int64Be value = 0x123456789ABCDEF0L;
        Int64Be result = value.SetHi((UInt32Be)0x00000001U);
        ((long)result).Should().Be(0x000000019ABCDEF0L);
    }

    [Fact]
    public void Int64Be_SetLo_SetsLowerUInt32Be()
    {
        Int64Be value = 0x123456789ABCDEF0L;
        Int64Be result = value.SetLo((UInt32Be)0x00000001U);
        ((long)result).Should().Be(0x1234567800000001L);
    }

    [Fact]
    public void UInt128Be_Hi_ReturnsUpperUInt64Be()
    {
        UInt128 native = ((UInt128)0x0123456789ABCDEFUL << 64) | 0xFEDCBA9876543210UL;
        UInt128Be value = new(native);
        ((ulong)value.Hi()).Should().Be(0x0123456789ABCDEFUL);
    }

    [Fact]
    public void UInt128Be_Lo_ReturnsLowerUInt64Be()
    {
        UInt128 native = ((UInt128)0x0123456789ABCDEFUL << 64) | 0xFEDCBA9876543210UL;
        UInt128Be value = new(native);
        ((ulong)value.Lo()).Should().Be(0xFEDCBA9876543210UL);
    }

    [Fact]
    public void Int128Be_Hi_ReturnsUpperUInt64Be()
    {
        Int128 native = (Int128)(((UInt128)0x0123456789ABCDEFUL << 64) | 0xFEDCBA9876543210UL);
        Int128Be value = new(native);
        ((ulong)value.Hi()).Should().Be(0x0123456789ABCDEFUL);
    }

    [Fact]
    public void Int128Be_Lo_ReturnsLowerUInt64Be()
    {
        Int128 native = (Int128)(((UInt128)0x0123456789ABCDEFUL << 64) | 0xFEDCBA9876543210UL);
        Int128Be value = new(native);
        ((ulong)value.Lo()).Should().Be(0xFEDCBA9876543210UL);
    }

    [Fact]
    public void UInt128Be_SetHi_ReplacesUpperHalf()
    {
        UInt128 native = ((UInt128)0x1111111111111111UL << 64) | 0x2222222222222222UL;
        UInt128Be value = new(native);
        UInt128Be result = value.SetHi((UInt64Be)0xAAAAAAAAAAAAAAAAUL);
        ((UInt128)result).Should().Be(((UInt128)0xAAAAAAAAAAAAAAAAUL << 64) | 0x2222222222222222UL);
    }

    [Fact]
    public void UInt128Be_SetLo_ReplacesLowerHalf()
    {
        UInt128 native = ((UInt128)0x1111111111111111UL << 64) | 0x2222222222222222UL;
        UInt128Be value = new(native);
        UInt128Be result = value.SetLo((UInt64Be)0xBBBBBBBBBBBBBBBBUL);
        ((UInt128)result).Should().Be(((UInt128)0x1111111111111111UL << 64) | 0xBBBBBBBBBBBBBBBBUL);
    }

    [Fact]
    public void Int128Be_SetHi_ReplacesUpperHalf()
    {
        Int128Be value = new((Int128)0);
        Int128Be result = value.SetHi((UInt64Be)0x0000000000000001UL);
        ((Int128)result).Should().Be((Int128)((UInt128)1UL << 64));
    }

    [Fact]
    public void Int128Be_SetLo_ReplacesLowerHalf()
    {
        Int128Be value = new((Int128)0);
        Int128Be result = value.SetLo((UInt64Be)0x00000000DEADBEEFU);
        ((Int128)result).Should().Be((Int128)0x00000000DEADBEEFU);
    }

    // ── UInt32Be SetHi (was missing) ────────────────────────────

    [Fact]
    public void UInt32Be_SetHi_ReplacesHighHalf()
    {
        UInt32Be value = 0x12345678U;
        UInt32Be result = value.SetHi((UInt16Be)0xDEADU);
        ((uint)result).Should().Be(0xDEAD5678U);
    }

    [Fact]
    public void UInt32Be_SetHi_Zero_ClearsHighHalf()
    {
        UInt32Be value = 0xFFFFFFFFU;
        UInt32Be result = value.SetHi((UInt16Be)0x0000U);
        ((uint)result).Should().Be(0x0000FFFFU);
    }

    [Fact]
    public void UInt32Be_SetLo_UInt16Be_ReplacesLowHalf()
    {
        UInt32Be value = 0x12345678U;
        UInt32Be result = value.SetLo((UInt16Be)0xBEEFU);
        ((uint)result).Should().Be(0x1234BEEFU);
    }

    [Fact]
    public void Int32Be_SetLo_UInt16Be_ReplacesLowHalf()
    {
        Int32Be value = 0x12345678;
        Int32Be result = value.SetLo((UInt16Be)0x0001U);
        ((int)result).Should().Be(0x12340001);
    }

    // ── UInt256Be / Int256Be Hi / Lo / SetHi / SetLo ────────────

    [Fact]
    public void UInt256Be_Hi_ReturnsUpperUInt128Be()
    {
        UInt256Be value = new(UInt256.MaxValue);
        ((UInt128)value.Hi()).Should().Be(UInt128.MaxValue);
    }

    [Fact]
    public void UInt256Be_Lo_ReturnsLowerUInt128Be()
    {
        // Only upper half set
        UInt256 native = new(UInt128.MaxValue, UInt128.Zero);
        UInt256Be value = new(native);
        ((UInt128)value.Lo()).Should().Be(UInt128.Zero);
    }

    [Fact]
    public void UInt256Be_Hi_DistinctHalves()
    {
        UInt128 hiVal = ((UInt128)0xAAAAAAAAAAAAAAAAUL << 64) | 0xBBBBBBBBBBBBBBBBUL;
        UInt128 loVal = ((UInt128)0xCCCCCCCCCCCCCCCCUL << 64) | 0xDDDDDDDDDDDDDDDDUL;
        UInt256Be value = new(new UInt256(hiVal, loVal));
        ((UInt128)value.Hi()).Should().Be(hiVal);
    }

    [Fact]
    public void UInt256Be_Lo_DistinctHalves()
    {
        UInt128 hiVal = ((UInt128)0xAAAAAAAAAAAAAAAAUL << 64) | 0xBBBBBBBBBBBBBBBBUL;
        UInt128 loVal = ((UInt128)0xCCCCCCCCCCCCCCCCUL << 64) | 0xDDDDDDDDDDDDDDDDUL;
        UInt256Be value = new(new UInt256(hiVal, loVal));
        ((UInt128)value.Lo()).Should().Be(loVal);
    }

    [Fact]
    public void UInt256Be_SetHi_ReplacesUpperHalf()
    {
        UInt128 hiVal = ((UInt128)0x1111111111111111UL << 64) | 0x2222222222222222UL;
        UInt128 loVal = ((UInt128)0x3333333333333333UL << 64) | 0x4444444444444444UL;
        UInt256Be value = new(new UInt256(hiVal, loVal));
        UInt128Be newHi = new(((UInt128)0xAAAAAAAAAAAAAAAAUL << 64) | 0xBBBBBBBBBBBBBBBBUL);
        UInt256Be result = value.SetHi(newHi);
        ((UInt128)result.Hi()).Should().Be((UInt128)newHi);
        ((UInt128)result.Lo()).Should().Be(loVal);
    }

    [Fact]
    public void UInt256Be_SetLo_ReplacesLowerHalf()
    {
        UInt128 hiVal = ((UInt128)0x1111111111111111UL << 64) | 0x2222222222222222UL;
        UInt128 loVal = ((UInt128)0x3333333333333333UL << 64) | 0x4444444444444444UL;
        UInt256Be value = new(new UInt256(hiVal, loVal));
        UInt128Be newLo = new(((UInt128)0xCCCCCCCCCCCCCCCCUL << 64) | 0xDDDDDDDDDDDDDDDDUL);
        UInt256Be result = value.SetLo(newLo);
        ((UInt128)result.Hi()).Should().Be(hiVal);
        ((UInt128)result.Lo()).Should().Be((UInt128)newLo);
    }

    [Fact]
    public void Int256Be_Hi_ReturnsUpperUInt128Be()
    {
        Int256Be value = new(Int256.MaxValue);
        // MaxValue upper half = 0x7FFF...FFFF
        UInt128 expected = Int256.MaxValue.Hi();
        ((UInt128)value.Hi()).Should().Be(expected);
    }

    [Fact]
    public void Int256Be_Lo_ReturnsLowerUInt128Be()
    {
        Int256Be value = new(Int256.MinValue);
        // MinValue lower half = 0
        ((UInt128)value.Lo()).Should().Be(UInt128.Zero);
    }

    [Fact]
    public void Int256Be_SetHi_ReplacesUpperHalf()
    {
        Int256Be value = new(new Int256(0L));
        UInt128Be newHi = new((UInt128)0xAAAAAAAAAAAAAAAAUL);
        Int256Be result = value.SetHi(newHi);
        ((UInt128)result.Hi()).Should().Be((UInt128)newHi);
        ((UInt128)result.Lo()).Should().Be(UInt128.Zero);
    }

    [Fact]
    public void Int256Be_SetLo_ReplacesLowerHalf()
    {
        Int256Be value = new(new Int256(0L));
        UInt128Be newLo = new((UInt128)0xBBBBBBBBBBBBBBBBUL);
        Int256Be result = value.SetLo(newLo);
        ((UInt128)result.Hi()).Should().Be(UInt128.Zero);
        ((UInt128)result.Lo()).Should().Be((UInt128)newLo);
    }

    #endregion

    #region Little-Endian Hi / Lo / SetHi / SetLo

    [Fact]
    public void UInt16Le_Hi_ReturnsHighByte()
    {
        UInt16Le value = 0xABCD;
        value.Hi().Should().Be(0xAB);
    }

    [Fact]
    public void UInt16Le_Lo_ReturnsLowByte()
    {
        UInt16Le value = 0xABCD;
        value.Lo().Should().Be(0xCD);
    }

    [Fact]
    public void Int16Le_Hi_ReturnsHighByte()
    {
        Int16Le value = 0x1234;
        value.Hi().Should().Be(0x12);
    }

    [Fact]
    public void Int16Le_Lo_ReturnsLowByte()
    {
        Int16Le value = 0x1234;
        value.Lo().Should().Be(0x34);
    }

    [Fact]
    public void UInt32Le_Hi_ReturnsHighUInt16Le()
    {
        UInt32Le value = 0x12345678U;
        ((ushort)value.Hi()).Should().Be(0x1234);
    }

    [Fact]
    public void UInt32Le_Lo_ReturnsLowUInt16Le()
    {
        UInt32Le value = 0x12345678U;
        ((ushort)value.Lo()).Should().Be(0x5678);
    }

    [Fact]
    public void Int32Le_Hi_ReturnsHighUInt16Le()
    {
        Int32Le value = 0x12345678;
        ((ushort)value.Hi()).Should().Be(0x1234);
    }

    [Fact]
    public void Int32Le_Lo_ReturnsLowUInt16Le()
    {
        Int32Le value = 0x12345678;
        ((ushort)value.Lo()).Should().Be(0x5678);
    }

    [Fact]
    public void UInt64Le_Hi_ReturnsHighUInt32Le()
    {
        UInt64Le value = 0x123456789ABCDEF0UL;
        ((uint)value.Hi()).Should().Be(0x12345678U);
    }

    [Fact]
    public void UInt64Le_Lo_ReturnsLowUInt32Le()
    {
        UInt64Le value = 0x123456789ABCDEF0UL;
        ((uint)value.Lo()).Should().Be(0x9ABCDEF0U);
    }

    [Fact]
    public void Int64Le_Hi_ReturnsHighUInt32Le()
    {
        Int64Le value = 0x123456789ABCDEF0L;
        ((uint)value.Hi()).Should().Be(0x12345678U);
    }

    [Fact]
    public void Int64Le_Lo_ReturnsLowUInt32Le()
    {
        Int64Le value = 0x123456789ABCDEF0L;
        ((uint)value.Lo()).Should().Be(0x9ABCDEF0U);
    }

    [Fact]
    public void UInt128Le_Hi_ReturnsHighUInt64Le()
    {
        UInt128 native = ((UInt128)0x0123456789ABCDEFUL << 64) | 0xFEDCBA9876543210UL;
        UInt128Le value = new(native);
        ((ulong)value.Hi()).Should().Be(0x0123456789ABCDEFUL);
    }

    [Fact]
    public void UInt128Le_Lo_ReturnsLowUInt64Le()
    {
        UInt128 native = ((UInt128)0x0123456789ABCDEFUL << 64) | 0xFEDCBA9876543210UL;
        UInt128Le value = new(native);
        ((ulong)value.Lo()).Should().Be(0xFEDCBA9876543210UL);
    }

    [Fact]
    public void Int128Le_Hi_ReturnsHighUInt64Le()
    {
        Int128 native = (Int128)(((UInt128)0x0123456789ABCDEFUL << 64) | 0xFEDCBA9876543210UL);
        Int128Le value = new(native);
        ((ulong)value.Hi()).Should().Be(0x0123456789ABCDEFUL);
    }

    [Fact]
    public void Int128Le_Lo_ReturnsLowUInt64Le()
    {
        Int128 native = (Int128)(((UInt128)0x0123456789ABCDEFUL << 64) | 0xFEDCBA9876543210UL);
        Int128Le value = new(native);
        ((ulong)value.Lo()).Should().Be(0xFEDCBA9876543210UL);
    }

    [Fact]
    public void UInt16Le_SetHi_ReplacesHighByte()
    {
        UInt16Le value = 0xABCD;
        UInt16Le result = value.SetHi(0xFF);
        ((ushort)result).Should().Be(0xFFCD);
    }

    [Fact]
    public void UInt16Le_SetLo_ReplacesLowByte()
    {
        UInt16Le value = 0xABCD;
        UInt16Le result = value.SetLo(0xFF);
        ((ushort)result).Should().Be(0xABFF);
    }

    [Fact]
    public void Int16Le_SetHi_ReplacesHighByte()
    {
        Int16Le value = 0x1234;
        Int16Le result = value.SetHi(0xFF);
        ((short)result).Should().Be(unchecked((short)0xFF34));
    }

    [Fact]
    public void Int16Le_SetLo_ReplacesLowByte()
    {
        Int16Le value = 0x1234;
        Int16Le result = value.SetLo(0xFF);
        ((short)result).Should().Be(0x12FF);
    }

    [Fact]
    public void UInt32Le_SetHi_ReplacesHighHalf()
    {
        UInt32Le value = 0x12345678U;
        UInt32Le result = value.SetHi((UInt16Le)0xAAAA);
        ((uint)result).Should().Be(0xAAAA5678U);
    }

    [Fact]
    public void UInt32Le_SetLo_ReplacesLowHalf()
    {
        UInt32Le value = 0x12345678U;
        UInt32Le result = value.SetLo((UInt16Le)0xBBBB);
        ((uint)result).Should().Be(0x1234BBBBU);
    }

    [Fact]
    public void Int32Le_SetHi_ReplacesHighHalf()
    {
        Int32Le value = 0x12345678;
        Int32Le result = value.SetHi((UInt16Le)0x0001);
        ((int)result).Should().Be(0x00015678);
    }

    [Fact]
    public void Int32Le_SetLo_ReplacesLowHalf()
    {
        Int32Le value = 0x12345678;
        Int32Le result = value.SetLo((UInt16Le)0x0001);
        ((int)result).Should().Be(0x12340001);
    }

    [Fact]
    public void UInt64Le_SetHi_ReplacesHighHalf()
    {
        UInt64Le value = 0x123456789ABCDEF0UL;
        UInt64Le result = value.SetHi((UInt32Le)0xDEADBEEFU);
        ((ulong)result).Should().Be(0xDEADBEEF9ABCDEF0UL);
    }

    [Fact]
    public void UInt64Le_SetLo_ReplacesLowHalf()
    {
        UInt64Le value = 0x123456789ABCDEF0UL;
        UInt64Le result = value.SetLo((UInt32Le)0xCAFEBABEU);
        ((ulong)result).Should().Be(0x12345678CAFEBABEU);
    }

    [Fact]
    public void Int64Le_SetHi_ReplacesHighHalf()
    {
        Int64Le value = 0x123456789ABCDEF0L;
        Int64Le result = value.SetHi((UInt32Le)0x00000001U);
        ((long)result).Should().Be(0x000000019ABCDEF0L);
    }

    [Fact]
    public void Int64Le_SetLo_ReplacesLowHalf()
    {
        Int64Le value = 0x123456789ABCDEF0L;
        Int64Le result = value.SetLo((UInt32Le)0x00000001U);
        ((long)result).Should().Be(0x1234567800000001L);
    }

    [Fact]
    public void UInt128Le_SetHi_ReplacesHighHalf()
    {
        UInt128 native = ((UInt128)0x1111111111111111UL << 64) | 0x2222222222222222UL;
        UInt128Le value = new(native);
        UInt128Le result = value.SetHi((UInt64Le)0xAAAAAAAAAAAAAAAAUL);
        ((UInt128)result).Should().Be(((UInt128)0xAAAAAAAAAAAAAAAAUL << 64) | 0x2222222222222222UL);
    }

    [Fact]
    public void UInt128Le_SetLo_ReplacesLowHalf()
    {
        UInt128 native = ((UInt128)0x1111111111111111UL << 64) | 0x2222222222222222UL;
        UInt128Le value = new(native);
        UInt128Le result = value.SetLo((UInt64Le)0xBBBBBBBBBBBBBBBBUL);
        ((UInt128)result).Should().Be(((UInt128)0x1111111111111111UL << 64) | 0xBBBBBBBBBBBBBBBBUL);
    }

    [Fact]
    public void Int128Le_SetHi_ReplacesHighHalf()
    {
        Int128Le value = new((Int128)0);
        Int128Le result = value.SetHi((UInt64Le)0x0000000000000001UL);
        ((Int128)result).Should().Be((Int128)((UInt128)1UL << 64));
    }

    [Fact]
    public void Int128Le_SetLo_ReplacesLowHalf()
    {
        Int128Le value = new((Int128)0);
        Int128Le result = value.SetLo((UInt64Le)0x00000000DEADBEEFU);
        ((Int128)result).Should().Be((Int128)0x00000000DEADBEEFU);
    }

    // ── UInt256Le / Int256Le Hi / Lo / SetHi / SetLo ─────────────

    [Fact]
    public void UInt256Le_Hi_ReturnsUpperUInt128Le()
    {
        UInt256Le value = new(UInt256.MaxValue);
        ((UInt128)value.Hi()).Should().Be(UInt128.MaxValue);
    }

    [Fact]
    public void UInt256Le_Lo_ReturnsLowerUInt128Le()
    {
        // Only upper half set, lower is zero
        UInt256 native = new(UInt128.MaxValue, UInt128.Zero);
        UInt256Le value = new(native);
        ((UInt128)value.Lo()).Should().Be(UInt128.Zero);
    }

    [Fact]
    public void UInt256Le_Hi_DistinctHalves()
    {
        UInt128 hiVal = ((UInt128)0xAAAAAAAAAAAAAAAAUL << 64) | 0xBBBBBBBBBBBBBBBBUL;
        UInt128 loVal = ((UInt128)0xCCCCCCCCCCCCCCCCUL << 64) | 0xDDDDDDDDDDDDDDDDUL;
        UInt256Le value = new(new UInt256(hiVal, loVal));
        ((UInt128)value.Hi()).Should().Be(hiVal);
    }

    [Fact]
    public void UInt256Le_Lo_DistinctHalves()
    {
        UInt128 hiVal = ((UInt128)0xAAAAAAAAAAAAAAAAUL << 64) | 0xBBBBBBBBBBBBBBBBUL;
        UInt128 loVal = ((UInt128)0xCCCCCCCCCCCCCCCCUL << 64) | 0xDDDDDDDDDDDDDDDDUL;
        UInt256Le value = new(new UInt256(hiVal, loVal));
        ((UInt128)value.Lo()).Should().Be(loVal);
    }

    [Fact]
    public void UInt256Le_SetHi_ReplacesUpperHalf()
    {
        UInt128 hiVal = ((UInt128)0x1111111111111111UL << 64) | 0x2222222222222222UL;
        UInt128 loVal = ((UInt128)0x3333333333333333UL << 64) | 0x4444444444444444UL;
        UInt256Le value = new(new UInt256(hiVal, loVal));
        UInt128Le newHi = new(((UInt128)0xAAAAAAAAAAAAAAAAUL << 64) | 0xBBBBBBBBBBBBBBBBUL);
        UInt256Le result = value.SetHi(newHi);
        ((UInt128)result.Hi()).Should().Be((UInt128)newHi);
        ((UInt128)result.Lo()).Should().Be(loVal);
    }

    [Fact]
    public void UInt256Le_SetLo_ReplacesLowerHalf()
    {
        UInt128 hiVal = ((UInt128)0x1111111111111111UL << 64) | 0x2222222222222222UL;
        UInt128 loVal = ((UInt128)0x3333333333333333UL << 64) | 0x4444444444444444UL;
        UInt256Le value = new(new UInt256(hiVal, loVal));
        UInt128Le newLo = new(((UInt128)0xCCCCCCCCCCCCCCCCUL << 64) | 0xDDDDDDDDDDDDDDDDUL);
        UInt256Le result = value.SetLo(newLo);
        ((UInt128)result.Hi()).Should().Be(hiVal);
        ((UInt128)result.Lo()).Should().Be((UInt128)newLo);
    }

    [Fact]
    public void Int256Le_Hi_ReturnsUpperUInt128Le()
    {
        Int256Le value = new(Int256.MaxValue);
        UInt128 expected = Int256.MaxValue.Hi();
        ((UInt128)value.Hi()).Should().Be(expected);
    }

    [Fact]
    public void Int256Le_Lo_ReturnsLowerUInt128Le()
    {
        Int256Le value = new(Int256.MinValue);
        ((UInt128)value.Lo()).Should().Be(UInt128.Zero);
    }

    [Fact]
    public void Int256Le_SetHi_ReplacesUpperHalf()
    {
        Int256Le value = new(new Int256(0L));
        UInt128Le newHi = new((UInt128)0xAAAAAAAAAAAAAAAAUL);
        Int256Le result = value.SetHi(newHi);
        ((UInt128)result.Hi()).Should().Be((UInt128)newHi);
        ((UInt128)result.Lo()).Should().Be(UInt128.Zero);
    }

    [Fact]
    public void Int256Le_SetLo_ReplacesLowerHalf()
    {
        Int256Le value = new(new Int256(0L));
        UInt128Le newLo = new((UInt128)0xBBBBBBBBBBBBBBBBUL);
        Int256Le result = value.SetLo(newLo);
        ((UInt128)result.Hi()).Should().Be(UInt128.Zero);
        ((UInt128)result.Lo()).Should().Be((UInt128)newLo);
    }

    #endregion

    #region Saturating Arithmetic (128-bit)

    // ── UInt128 SaturatingAdd ───────────────────────────────────

    [Fact]
    public void UInt128_SaturatingAdd_Normal()
    {
        UInt128 a = 100;
        UInt128 b = 200;
        a.SaturatingAdd(b).Should().Be((UInt128)300);
    }

    [Fact]
    public void UInt128_SaturatingAdd_Overflow_ClampsToMax()
    {
        UInt128 a = UInt128.MaxValue;
        UInt128 b = 1;
        a.SaturatingAdd(b).Should().Be(UInt128.MaxValue);
    }

    [Fact]
    public void UInt128_SaturatingAdd_BothMax_ClampsToMax()
    {
        UInt128 a = UInt128.MaxValue;
        UInt128 b = UInt128.MaxValue;
        a.SaturatingAdd(b).Should().Be(UInt128.MaxValue);
    }

    [Fact]
    public void UInt128_SaturatingAdd_Zero()
    {
        UInt128 a = 42;
        a.SaturatingAdd((UInt128)0).Should().Be((UInt128)42);
    }

    // ── UInt128 SaturatingSub ───────────────────────────────────

    [Fact]
    public void UInt128_SaturatingSub_Normal()
    {
        UInt128 a = 300;
        UInt128 b = 100;
        a.SaturatingSub(b).Should().Be((UInt128)200);
    }

    [Fact]
    public void UInt128_SaturatingSub_Underflow_ClampsToZero()
    {
        UInt128 a = 10;
        UInt128 b = 20;
        a.SaturatingSub(b).Should().Be(UInt128.MinValue);
    }

    [Fact]
    public void UInt128_SaturatingSub_Zero()
    {
        UInt128 a = 42;
        a.SaturatingSub((UInt128)0).Should().Be((UInt128)42);
    }

    [Fact]
    public void UInt128_SaturatingSub_Equal_ReturnsZero()
    {
        UInt128 a = UInt128.MaxValue;
        a.SaturatingSub(UInt128.MaxValue).Should().Be(UInt128.MinValue);
    }

    // ── Int128 SaturatingAdd ────────────────────────────────────

    [Fact]
    public void Int128_SaturatingAdd_Normal()
    {
        Int128 a = 100;
        Int128 b = 200;
        a.SaturatingAdd(b).Should().Be((Int128)300);
    }

    [Fact]
    public void Int128_SaturatingAdd_PositiveOverflow_ClampsToMax()
    {
        Int128 a = Int128.MaxValue;
        Int128 b = (Int128)1;
        a.SaturatingAdd(b).Should().Be(Int128.MaxValue);
    }

    [Fact]
    public void Int128_SaturatingAdd_NegativeOverflow_ClampsToMin()
    {
        Int128 a = Int128.MinValue;
        Int128 b = (Int128)(-1);
        a.SaturatingAdd(b).Should().Be(Int128.MinValue);
    }

    [Fact]
    public void Int128_SaturatingAdd_MixedSigns_NoOverflow()
    {
        Int128 a = Int128.MaxValue;
        Int128 b = (Int128)(-1);
        a.SaturatingAdd(b).Should().Be(Int128.MaxValue - (Int128)1);
    }

    [Fact]
    public void Int128_SaturatingAdd_Zero()
    {
        Int128 a = (Int128)(-42);
        a.SaturatingAdd((Int128)0).Should().Be((Int128)(-42));
    }

    // ── Int128 SaturatingSub ────────────────────────────────────

    [Fact]
    public void Int128_SaturatingSub_Normal()
    {
        Int128 a = 300;
        Int128 b = 100;
        a.SaturatingSub(b).Should().Be((Int128)200);
    }

    [Fact]
    public void Int128_SaturatingSub_PositiveOverflow_ClampsToMax()
    {
        // MaxValue - (-1) would overflow positive
        Int128 a = Int128.MaxValue;
        Int128 b = (Int128)(-1);
        a.SaturatingSub(b).Should().Be(Int128.MaxValue);
    }

    [Fact]
    public void Int128_SaturatingSub_NegativeOverflow_ClampsToMin()
    {
        // MinValue - 1 would overflow negative
        Int128 a = Int128.MinValue;
        Int128 b = (Int128)1;
        a.SaturatingSub(b).Should().Be(Int128.MinValue);
    }

    [Fact]
    public void Int128_SaturatingSub_SameSigns_NoOverflow()
    {
        Int128 a = Int128.MinValue;
        Int128 b = (Int128)(-1);
        a.SaturatingSub(b).Should().Be(Int128.MinValue + (Int128)1);
    }

    [Fact]
    public void Int128_SaturatingSub_Zero()
    {
        Int128 a = (Int128)(-42);
        a.SaturatingSub((Int128)0).Should().Be((Int128)(-42));
    }

    [Fact]
    public void Int128_SaturatingSub_BothMax_ReturnsZero()
    {
        Int128 a = Int128.MaxValue;
        a.SaturatingSub(Int128.MaxValue).Should().Be((Int128)0);
    }

    #endregion

    #region Saturating Arithmetic (Native 8-bit)

    [Fact]
    public void Byte_SaturatingAdd_Normal() => ((byte)100).SaturatingAdd((byte)50).Should().Be((byte)150);

    [Fact]
    public void Byte_SaturatingAdd_Overflow() => byte.MaxValue.SaturatingAdd((byte)1).Should().Be(byte.MaxValue);

    [Fact]
    public void Byte_SaturatingSub_Normal() => ((byte)200).SaturatingSub((byte)100).Should().Be((byte)100);

    [Fact]
    public void Byte_SaturatingSub_Underflow() => ((byte)10).SaturatingSub((byte)20).Should().Be(byte.MinValue);

    [Fact]
    public void Sbyte_SaturatingAdd_Normal() => ((sbyte)50).SaturatingAdd((sbyte)20).Should().Be((sbyte)70);

    [Fact]
    public void Sbyte_SaturatingAdd_PositiveOverflow() => sbyte.MaxValue.SaturatingAdd((sbyte)1).Should().Be(sbyte.MaxValue);

    [Fact]
    public void Sbyte_SaturatingAdd_NegativeOverflow() => sbyte.MinValue.SaturatingAdd((sbyte)(-1)).Should().Be(sbyte.MinValue);

    [Fact]
    public void Sbyte_SaturatingSub_Normal() => ((sbyte)50).SaturatingSub((sbyte)20).Should().Be((sbyte)30);

    [Fact]
    public void Sbyte_SaturatingSub_PositiveOverflow() => sbyte.MaxValue.SaturatingSub((sbyte)(-1)).Should().Be(sbyte.MaxValue);

    [Fact]
    public void Sbyte_SaturatingSub_NegativeOverflow() => sbyte.MinValue.SaturatingSub((sbyte)1).Should().Be(sbyte.MinValue);

    #endregion

    #region Saturating Arithmetic (Native 16-bit)

    [Fact]
    public void Ushort_SaturatingAdd_Normal() => ((ushort)100).SaturatingAdd((ushort)200).Should().Be((ushort)300);

    [Fact]
    public void Ushort_SaturatingAdd_Overflow() => ushort.MaxValue.SaturatingAdd((ushort)1).Should().Be(ushort.MaxValue);

    [Fact]
    public void Ushort_SaturatingSub_Normal() => ((ushort)300).SaturatingSub((ushort)100).Should().Be((ushort)200);

    [Fact]
    public void Ushort_SaturatingSub_Underflow() => ((ushort)10).SaturatingSub((ushort)20).Should().Be(ushort.MinValue);

    [Fact]
    public void Short_SaturatingAdd_Normal() => ((short)100).SaturatingAdd((short)200).Should().Be((short)300);

    [Fact]
    public void Short_SaturatingAdd_PositiveOverflow() => short.MaxValue.SaturatingAdd((short)1).Should().Be(short.MaxValue);

    [Fact]
    public void Short_SaturatingAdd_NegativeOverflow() => short.MinValue.SaturatingAdd((short)(-1)).Should().Be(short.MinValue);

    [Fact]
    public void Short_SaturatingSub_Normal() => ((short)300).SaturatingSub((short)100).Should().Be((short)200);

    [Fact]
    public void Short_SaturatingSub_PositiveOverflow() => short.MaxValue.SaturatingSub((short)(-1)).Should().Be(short.MaxValue);

    [Fact]
    public void Short_SaturatingSub_NegativeOverflow() => short.MinValue.SaturatingSub((short)1).Should().Be(short.MinValue);

    #endregion

    #region Saturating Arithmetic (Big-Endian)

    // ── UInt16Be ────────────────────────────────────────────────

    [Fact]
    public void UInt16Be_SaturatingAdd_Normal()
    {
        UInt16Be a = 100;
        UInt16Be b = 200;
        ((ushort)a.SaturatingAdd(b)).Should().Be(300);
    }

    [Fact]
    public void UInt16Be_SaturatingAdd_Overflow()
    {
        UInt16Be a = ushort.MaxValue;
        UInt16Be b = 1;
        ((ushort)a.SaturatingAdd(b)).Should().Be(ushort.MaxValue);
    }

    [Fact]
    public void UInt16Be_SaturatingSub_Underflow()
    {
        UInt16Be a = 10;
        UInt16Be b = 20;
        ((ushort)a.SaturatingSub(b)).Should().Be(0);
    }

    // ── Int16Be ─────────────────────────────────────────────────

    [Fact]
    public void Int16Be_SaturatingAdd_PositiveOverflow()
    {
        Int16Be a = short.MaxValue;
        Int16Be b = 1;
        ((short)a.SaturatingAdd(b)).Should().Be(short.MaxValue);
    }

    [Fact]
    public void Int16Be_SaturatingSub_NegativeOverflow()
    {
        Int16Be a = short.MinValue;
        Int16Be b = 1;
        ((short)a.SaturatingSub(b)).Should().Be(short.MinValue);
    }

    // ── UInt32Be ────────────────────────────────────────────────

    [Fact]
    public void UInt32Be_SaturatingAdd_Overflow()
    {
        UInt32Be a = uint.MaxValue;
        UInt32Be b = 1;
        ((uint)a.SaturatingAdd(b)).Should().Be(uint.MaxValue);
    }

    [Fact]
    public void UInt32Be_SaturatingSub_Underflow()
    {
        UInt32Be a = 10;
        UInt32Be b = 20;
        ((uint)a.SaturatingSub(b)).Should().Be(0U);
    }

    // ── Int32Be ─────────────────────────────────────────────────

    [Fact]
    public void Int32Be_SaturatingAdd_PositiveOverflow()
    {
        Int32Be a = int.MaxValue;
        Int32Be b = 1;
        ((int)a.SaturatingAdd(b)).Should().Be(int.MaxValue);
    }

    [Fact]
    public void Int32Be_SaturatingSub_NegativeOverflow()
    {
        Int32Be a = int.MinValue;
        Int32Be b = 1;
        ((int)a.SaturatingSub(b)).Should().Be(int.MinValue);
    }

    // ── UInt64Be ────────────────────────────────────────────────

    [Fact]
    public void UInt64Be_SaturatingAdd_Overflow()
    {
        UInt64Be a = ulong.MaxValue;
        UInt64Be b = 1;
        ((ulong)a.SaturatingAdd(b)).Should().Be(ulong.MaxValue);
    }

    [Fact]
    public void UInt64Be_SaturatingSub_Underflow()
    {
        UInt64Be a = 10;
        UInt64Be b = 20;
        ((ulong)a.SaturatingSub(b)).Should().Be(0UL);
    }

    // ── Int64Be ─────────────────────────────────────────────────

    [Fact]
    public void Int64Be_SaturatingAdd_PositiveOverflow()
    {
        Int64Be a = long.MaxValue;
        Int64Be b = 1;
        ((long)a.SaturatingAdd(b)).Should().Be(long.MaxValue);
    }

    [Fact]
    public void Int64Be_SaturatingSub_NegativeOverflow()
    {
        Int64Be a = long.MinValue;
        Int64Be b = 1;
        ((long)a.SaturatingSub(b)).Should().Be(long.MinValue);
    }

    // ── UInt128Be ───────────────────────────────────────────────

    [Fact]
    public void UInt128Be_SaturatingAdd_Overflow()
    {
        UInt128Be a = new(UInt128.MaxValue);
        UInt128Be b = new((UInt128)1);
        ((UInt128)a.SaturatingAdd(b)).Should().Be(UInt128.MaxValue);
    }

    [Fact]
    public void UInt128Be_SaturatingSub_Underflow()
    {
        UInt128Be a = new((UInt128)10);
        UInt128Be b = new((UInt128)20);
        ((UInt128)a.SaturatingSub(b)).Should().Be(UInt128.MinValue);
    }

    // ── Int128Be ────────────────────────────────────────────────

    [Fact]
    public void Int128Be_SaturatingAdd_PositiveOverflow()
    {
        Int128Be a = new(Int128.MaxValue);
        Int128Be b = new((Int128)1);
        ((Int128)a.SaturatingAdd(b)).Should().Be(Int128.MaxValue);
    }

    [Fact]
    public void Int128Be_SaturatingSub_NegativeOverflow()
    {
        Int128Be a = new(Int128.MinValue);
        Int128Be b = new((Int128)1);
        ((Int128)a.SaturatingSub(b)).Should().Be(Int128.MinValue);
    }

    #endregion

    #region Saturating Arithmetic (Native 32-bit)

    [Fact]
    public void Uint_SaturatingAdd_Normal() => ((uint)100).SaturatingAdd(200U).Should().Be(300U);

    [Fact]
    public void Uint_SaturatingAdd_Overflow() => uint.MaxValue.SaturatingAdd(1U).Should().Be(uint.MaxValue);

    [Fact]
    public void Uint_SaturatingAdd_BothMax() => uint.MaxValue.SaturatingAdd(uint.MaxValue).Should().Be(uint.MaxValue);

    [Fact]
    public void Uint_SaturatingAdd_Zero() => ((uint)42).SaturatingAdd(0U).Should().Be(42U);

    [Fact]
    public void Uint_SaturatingSub_Normal() => ((uint)300).SaturatingSub(100U).Should().Be(200U);

    [Fact]
    public void Uint_SaturatingSub_Underflow() => ((uint)10).SaturatingSub(20U).Should().Be(uint.MinValue);

    [Fact]
    public void Uint_SaturatingSub_Equal() => uint.MaxValue.SaturatingSub(uint.MaxValue).Should().Be(0U);

    [Fact]
    public void Uint_SaturatingSub_Zero() => ((uint)42).SaturatingSub(0U).Should().Be(42U);

    [Fact]
    public void Int_SaturatingAdd_Normal() => ((int)100).SaturatingAdd(200).Should().Be(300);

    [Fact]
    public void Int_SaturatingAdd_PositiveOverflow() => int.MaxValue.SaturatingAdd(1).Should().Be(int.MaxValue);

    [Fact]
    public void Int_SaturatingAdd_NegativeOverflow() => int.MinValue.SaturatingAdd(-1).Should().Be(int.MinValue);

    [Fact]
    public void Int_SaturatingAdd_MixedSigns_NoOverflow() => int.MaxValue.SaturatingAdd(-1).Should().Be(int.MaxValue - 1);

    [Fact]
    public void Int_SaturatingAdd_Zero() => ((int)-42).SaturatingAdd(0).Should().Be(-42);

    [Fact]
    public void Int_SaturatingSub_Normal() => ((int)300).SaturatingSub(100).Should().Be(200);

    [Fact]
    public void Int_SaturatingSub_PositiveOverflow() => int.MaxValue.SaturatingSub(-1).Should().Be(int.MaxValue);

    [Fact]
    public void Int_SaturatingSub_NegativeOverflow() => int.MinValue.SaturatingSub(1).Should().Be(int.MinValue);

    [Fact]
    public void Int_SaturatingSub_SameSigns_NoOverflow() => int.MinValue.SaturatingSub(-1).Should().Be(int.MinValue + 1);

    [Fact]
    public void Int_SaturatingSub_Zero() => ((int)-42).SaturatingSub(0).Should().Be(-42);

    [Fact]
    public void Int_SaturatingSub_BothMax() => int.MaxValue.SaturatingSub(int.MaxValue).Should().Be(0);

    #endregion

    #region Saturating Arithmetic (Native 64-bit)

    [Fact]
    public void Ulong_SaturatingAdd_Normal() => ((ulong)100).SaturatingAdd(200UL).Should().Be(300UL);

    [Fact]
    public void Ulong_SaturatingAdd_Overflow() => ulong.MaxValue.SaturatingAdd(1UL).Should().Be(ulong.MaxValue);

    [Fact]
    public void Ulong_SaturatingAdd_BothMax() => ulong.MaxValue.SaturatingAdd(ulong.MaxValue).Should().Be(ulong.MaxValue);

    [Fact]
    public void Ulong_SaturatingAdd_Zero() => ((ulong)42).SaturatingAdd(0UL).Should().Be(42UL);

    [Fact]
    public void Ulong_SaturatingSub_Normal() => ((ulong)300).SaturatingSub(100UL).Should().Be(200UL);

    [Fact]
    public void Ulong_SaturatingSub_Underflow() => ((ulong)10).SaturatingSub(20UL).Should().Be(ulong.MinValue);

    [Fact]
    public void Ulong_SaturatingSub_Equal() => ulong.MaxValue.SaturatingSub(ulong.MaxValue).Should().Be(0UL);

    [Fact]
    public void Ulong_SaturatingSub_Zero() => ((ulong)42).SaturatingSub(0UL).Should().Be(42UL);

    [Fact]
    public void Long_SaturatingAdd_Normal() => ((long)100).SaturatingAdd(200L).Should().Be(300L);

    [Fact]
    public void Long_SaturatingAdd_PositiveOverflow() => long.MaxValue.SaturatingAdd(1L).Should().Be(long.MaxValue);

    [Fact]
    public void Long_SaturatingAdd_NegativeOverflow() => long.MinValue.SaturatingAdd(-1L).Should().Be(long.MinValue);

    [Fact]
    public void Long_SaturatingAdd_MixedSigns_NoOverflow() => long.MaxValue.SaturatingAdd(-1L).Should().Be(long.MaxValue - 1L);

    [Fact]
    public void Long_SaturatingAdd_Zero() => ((long)-42).SaturatingAdd(0L).Should().Be(-42L);

    [Fact]
    public void Long_SaturatingSub_Normal() => ((long)300).SaturatingSub(100L).Should().Be(200L);

    [Fact]
    public void Long_SaturatingSub_PositiveOverflow() => long.MaxValue.SaturatingSub(-1L).Should().Be(long.MaxValue);

    [Fact]
    public void Long_SaturatingSub_NegativeOverflow() => long.MinValue.SaturatingSub(1L).Should().Be(long.MinValue);

    [Fact]
    public void Long_SaturatingSub_SameSigns_NoOverflow() => long.MinValue.SaturatingSub(-1L).Should().Be(long.MinValue + 1L);

    [Fact]
    public void Long_SaturatingSub_Zero() => ((long)-42).SaturatingSub(0L).Should().Be(-42L);

    [Fact]
    public void Long_SaturatingSub_BothMax() => long.MaxValue.SaturatingSub(long.MaxValue).Should().Be(0L);

    #endregion

    #region Saturating Arithmetic (Native 256-bit)

    // ── UInt256 SaturatingAdd ───────────────────────────────────

    [Fact]
    public void UInt256_SaturatingAdd_Normal()
    {
        UInt256 a = new(100UL);
        UInt256 b = new(200UL);
        a.SaturatingAdd(b).Should().Be(new UInt256(300UL));
    }

    [Fact]
    public void UInt256_SaturatingAdd_Overflow_ClampsToMax()
    {
        UInt256 a = UInt256.MaxValue;
        UInt256 b = new(1UL);
        a.SaturatingAdd(b).Should().Be(UInt256.MaxValue);
    }

    [Fact]
    public void UInt256_SaturatingAdd_BothMax_ClampsToMax()
    {
        UInt256.MaxValue.SaturatingAdd(UInt256.MaxValue).Should().Be(UInt256.MaxValue);
    }

    [Fact]
    public void UInt256_SaturatingAdd_Zero()
    {
        UInt256 a = new(42UL);
        a.SaturatingAdd(UInt256.MinValue).Should().Be(new UInt256(42UL));
    }

    [Fact]
    public void UInt256_SaturatingAdd_OneBeforeMax()
    {
        // MaxValue - 1 + 1 = MaxValue, no saturation needed
        UInt256 a = new(ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue - 1UL);
        UInt256 b = new(1UL);
        a.SaturatingAdd(b).Should().Be(UInt256.MaxValue);
    }

    // ── UInt256 SaturatingSub ───────────────────────────────────

    [Fact]
    public void UInt256_SaturatingSub_Normal()
    {
        UInt256 a = new(300UL);
        UInt256 b = new(100UL);
        a.SaturatingSub(b).Should().Be(new UInt256(200UL));
    }

    [Fact]
    public void UInt256_SaturatingSub_Underflow_ClampsToZero()
    {
        UInt256 a = new(10UL);
        UInt256 b = new(20UL);
        a.SaturatingSub(b).Should().Be(UInt256.MinValue);
    }

    [Fact]
    public void UInt256_SaturatingSub_Zero()
    {
        UInt256 a = new(42UL);
        a.SaturatingSub(UInt256.MinValue).Should().Be(new UInt256(42UL));
    }

    [Fact]
    public void UInt256_SaturatingSub_Equal_ReturnsZero()
    {
        UInt256.MaxValue.SaturatingSub(UInt256.MaxValue).Should().Be(UInt256.MinValue);
    }

    [Fact]
    public void UInt256_SaturatingSub_MaxMinusOne()
    {
        UInt256 expected = new(ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue - 1UL);
        UInt256.MaxValue.SaturatingSub(new UInt256(1UL)).Should().Be(expected);
    }

    // ── Int256 SaturatingAdd ────────────────────────────────────

    [Fact]
    public void Int256_SaturatingAdd_Normal()
    {
        Int256 a = new(100L);
        Int256 b = new(200L);
        a.SaturatingAdd(b).Should().Be(new Int256(300L));
    }

    [Fact]
    public void Int256_SaturatingAdd_PositiveOverflow_ClampsToMax()
    {
        Int256.MaxValue.SaturatingAdd(new Int256(1L)).Should().Be(Int256.MaxValue);
    }

    [Fact]
    public void Int256_SaturatingAdd_NegativeOverflow_ClampsToMin()
    {
        Int256.MinValue.SaturatingAdd(new Int256(-1L)).Should().Be(Int256.MinValue);
    }

    [Fact]
    public void Int256_SaturatingAdd_MixedSigns_NoOverflow()
    {
        // MaxValue + (-1) = MaxValue - 1, no saturation
        Int256 result = Int256.MaxValue.SaturatingAdd(new Int256(-1L));
        result.Should().NotBe(Int256.MaxValue);
        (result > new Int256(0L)).Should().BeTrue();
    }

    [Fact]
    public void Int256_SaturatingAdd_Zero()
    {
        Int256 a = new(-42L);
        a.SaturatingAdd(new Int256(0L)).Should().Be(new Int256(-42L));
    }

    [Fact]
    public void Int256_SaturatingAdd_BothPositiveMax()
    {
        Int256.MaxValue.SaturatingAdd(Int256.MaxValue).Should().Be(Int256.MaxValue);
    }

    [Fact]
    public void Int256_SaturatingAdd_BothNegativeMin()
    {
        Int256.MinValue.SaturatingAdd(Int256.MinValue).Should().Be(Int256.MinValue);
    }

    // ── Int256 SaturatingSub ────────────────────────────────────

    [Fact]
    public void Int256_SaturatingSub_Normal()
    {
        Int256 a = new(300L);
        Int256 b = new(100L);
        a.SaturatingSub(b).Should().Be(new Int256(200L));
    }

    [Fact]
    public void Int256_SaturatingSub_PositiveOverflow_ClampsToMax()
    {
        // MaxValue - (-1) would overflow positive
        Int256.MaxValue.SaturatingSub(new Int256(-1L)).Should().Be(Int256.MaxValue);
    }

    [Fact]
    public void Int256_SaturatingSub_NegativeOverflow_ClampsToMin()
    {
        // MinValue - 1 would overflow negative
        Int256.MinValue.SaturatingSub(new Int256(1L)).Should().Be(Int256.MinValue);
    }

    [Fact]
    public void Int256_SaturatingSub_SameSigns_NoOverflow()
    {
        // MinValue - (-1) = MinValue + 1, no saturation
        Int256 result = Int256.MinValue.SaturatingSub(new Int256(-1L));
        result.Should().NotBe(Int256.MinValue);
        (result < new Int256(0L)).Should().BeTrue();
    }

    [Fact]
    public void Int256_SaturatingSub_Zero()
    {
        Int256 a = new(-42L);
        a.SaturatingSub(new Int256(0L)).Should().Be(new Int256(-42L));
    }

    [Fact]
    public void Int256_SaturatingSub_Equal_ReturnsZero()
    {
        Int256.MaxValue.SaturatingSub(Int256.MaxValue).Should().Be(new Int256(0L));
    }

    [Fact]
    public void Int256_SaturatingSub_MinMinusMin_ReturnsZero()
    {
        Int256.MinValue.SaturatingSub(Int256.MinValue).Should().Be(new Int256(0L));
    }

    #endregion

    #region Saturating Arithmetic (Little-Endian)

    // ── UInt16Le ────────────────────────────────────────────────

    [Fact]
    public void UInt16Le_SaturatingAdd_Normal()
    {
        UInt16Le a = 100;
        UInt16Le b = 200;
        ((ushort)a.SaturatingAdd(b)).Should().Be(300);
    }

    [Fact]
    public void UInt16Le_SaturatingAdd_Overflow()
    {
        UInt16Le a = ushort.MaxValue;
        UInt16Le b = 1;
        ((ushort)a.SaturatingAdd(b)).Should().Be(ushort.MaxValue);
    }

    [Fact]
    public void UInt16Le_SaturatingSub_Underflow()
    {
        UInt16Le a = 10;
        UInt16Le b = 20;
        ((ushort)a.SaturatingSub(b)).Should().Be(0);
    }

    // ── Int16Le ─────────────────────────────────────────────────

    [Fact]
    public void Int16Le_SaturatingAdd_PositiveOverflow()
    {
        Int16Le a = short.MaxValue;
        Int16Le b = 1;
        ((short)a.SaturatingAdd(b)).Should().Be(short.MaxValue);
    }

    [Fact]
    public void Int16Le_SaturatingSub_NegativeOverflow()
    {
        Int16Le a = short.MinValue;
        Int16Le b = 1;
        ((short)a.SaturatingSub(b)).Should().Be(short.MinValue);
    }

    // ── UInt32Le ────────────────────────────────────────────────

    [Fact]
    public void UInt32Le_SaturatingAdd_Overflow()
    {
        UInt32Le a = uint.MaxValue;
        UInt32Le b = 1;
        ((uint)a.SaturatingAdd(b)).Should().Be(uint.MaxValue);
    }

    [Fact]
    public void UInt32Le_SaturatingSub_Underflow()
    {
        UInt32Le a = 10;
        UInt32Le b = 20;
        ((uint)a.SaturatingSub(b)).Should().Be(0U);
    }

    // ── Int32Le ─────────────────────────────────────────────────

    [Fact]
    public void Int32Le_SaturatingAdd_PositiveOverflow()
    {
        Int32Le a = int.MaxValue;
        Int32Le b = 1;
        ((int)a.SaturatingAdd(b)).Should().Be(int.MaxValue);
    }

    [Fact]
    public void Int32Le_SaturatingSub_NegativeOverflow()
    {
        Int32Le a = int.MinValue;
        Int32Le b = 1;
        ((int)a.SaturatingSub(b)).Should().Be(int.MinValue);
    }

    // ── UInt64Le ────────────────────────────────────────────────

    [Fact]
    public void UInt64Le_SaturatingAdd_Overflow()
    {
        UInt64Le a = ulong.MaxValue;
        UInt64Le b = 1;
        ((ulong)a.SaturatingAdd(b)).Should().Be(ulong.MaxValue);
    }

    [Fact]
    public void UInt64Le_SaturatingSub_Underflow()
    {
        UInt64Le a = 10;
        UInt64Le b = 20;
        ((ulong)a.SaturatingSub(b)).Should().Be(0UL);
    }

    // ── Int64Le ─────────────────────────────────────────────────

    [Fact]
    public void Int64Le_SaturatingAdd_PositiveOverflow()
    {
        Int64Le a = long.MaxValue;
        Int64Le b = 1;
        ((long)a.SaturatingAdd(b)).Should().Be(long.MaxValue);
    }

    [Fact]
    public void Int64Le_SaturatingSub_NegativeOverflow()
    {
        Int64Le a = long.MinValue;
        Int64Le b = 1;
        ((long)a.SaturatingSub(b)).Should().Be(long.MinValue);
    }

    // ── UInt128Le ───────────────────────────────────────────────

    [Fact]
    public void UInt128Le_SaturatingAdd_Overflow()
    {
        UInt128Le a = new(UInt128.MaxValue);
        UInt128Le b = new((UInt128)1);
        ((UInt128)a.SaturatingAdd(b)).Should().Be(UInt128.MaxValue);
    }

    [Fact]
    public void UInt128Le_SaturatingSub_Underflow()
    {
        UInt128Le a = new((UInt128)10);
        UInt128Le b = new((UInt128)20);
        ((UInt128)a.SaturatingSub(b)).Should().Be(UInt128.MinValue);
    }

    // ── Int128Le ────────────────────────────────────────────────

    [Fact]
    public void Int128Le_SaturatingAdd_PositiveOverflow()
    {
        Int128Le a = new(Int128.MaxValue);
        Int128Le b = new((Int128)1);
        ((Int128)a.SaturatingAdd(b)).Should().Be(Int128.MaxValue);
    }

    [Fact]
    public void Int128Le_SaturatingSub_NegativeOverflow()
    {
        Int128Le a = new(Int128.MinValue);
        Int128Le b = new((Int128)1);
        ((Int128)a.SaturatingSub(b)).Should().Be(Int128.MinValue);
    }

    #endregion

    #region Saturating Arithmetic (Big-Endian 256-bit)

    // ── UInt256Be ───────────────────────────────────────────────

    [Fact]
    public void UInt256Be_SaturatingAdd_Normal()
    {
        UInt256Be a = new(new UInt256(100UL));
        UInt256Be b = new(new UInt256(200UL));
        ((UInt256)a.SaturatingAdd(b)).Should().Be(new UInt256(300UL));
    }

    [Fact]
    public void UInt256Be_SaturatingAdd_Overflow()
    {
        UInt256Be a = new(UInt256.MaxValue);
        UInt256Be b = new(new UInt256(1UL));
        ((UInt256)a.SaturatingAdd(b)).Should().Be(UInt256.MaxValue);
    }

    [Fact]
    public void UInt256Be_SaturatingAdd_BothMax()
    {
        UInt256Be a = new(UInt256.MaxValue);
        UInt256Be b = new(UInt256.MaxValue);
        ((UInt256)a.SaturatingAdd(b)).Should().Be(UInt256.MaxValue);
    }

    [Fact]
    public void UInt256Be_SaturatingAdd_Zero()
    {
        UInt256Be a = new(new UInt256(42UL));
        UInt256Be b = new(UInt256.MinValue);
        ((UInt256)a.SaturatingAdd(b)).Should().Be(new UInt256(42UL));
    }

    [Fact]
    public void UInt256Be_SaturatingSub_Normal()
    {
        UInt256Be a = new(new UInt256(300UL));
        UInt256Be b = new(new UInt256(100UL));
        ((UInt256)a.SaturatingSub(b)).Should().Be(new UInt256(200UL));
    }

    [Fact]
    public void UInt256Be_SaturatingSub_Underflow()
    {
        UInt256Be a = new(new UInt256(10UL));
        UInt256Be b = new(new UInt256(20UL));
        ((UInt256)a.SaturatingSub(b)).Should().Be(UInt256.MinValue);
    }

    [Fact]
    public void UInt256Be_SaturatingSub_Equal_ReturnsZero()
    {
        UInt256Be a = new(UInt256.MaxValue);
        UInt256Be b = new(UInt256.MaxValue);
        ((UInt256)a.SaturatingSub(b)).Should().Be(UInt256.MinValue);
    }

    [Fact]
    public void UInt256Be_SaturatingSub_Zero()
    {
        UInt256Be a = new(new UInt256(42UL));
        UInt256Be b = new(UInt256.MinValue);
        ((UInt256)a.SaturatingSub(b)).Should().Be(new UInt256(42UL));
    }

    // ── Int256Be ────────────────────────────────────────────────

    [Fact]
    public void Int256Be_SaturatingAdd_Normal()
    {
        Int256Be a = new(new Int256(100L));
        Int256Be b = new(new Int256(200L));
        ((Int256)a.SaturatingAdd(b)).Should().Be(new Int256(300L));
    }

    [Fact]
    public void Int256Be_SaturatingAdd_PositiveOverflow()
    {
        Int256Be a = new(Int256.MaxValue);
        Int256Be b = new(new Int256(1L));
        ((Int256)a.SaturatingAdd(b)).Should().Be(Int256.MaxValue);
    }

    [Fact]
    public void Int256Be_SaturatingAdd_NegativeOverflow()
    {
        Int256Be a = new(Int256.MinValue);
        Int256Be b = new(new Int256(-1L));
        ((Int256)a.SaturatingAdd(b)).Should().Be(Int256.MinValue);
    }

    [Fact]
    public void Int256Be_SaturatingAdd_Zero()
    {
        Int256Be a = new(new Int256(-42L));
        Int256Be b = new(new Int256(0L));
        ((Int256)a.SaturatingAdd(b)).Should().Be(new Int256(-42L));
    }

    [Fact]
    public void Int256Be_SaturatingSub_Normal()
    {
        Int256Be a = new(new Int256(300L));
        Int256Be b = new(new Int256(100L));
        ((Int256)a.SaturatingSub(b)).Should().Be(new Int256(200L));
    }

    [Fact]
    public void Int256Be_SaturatingSub_PositiveOverflow()
    {
        // MaxValue - (-1) would overflow positive
        Int256Be a = new(Int256.MaxValue);
        Int256Be b = new(new Int256(-1L));
        ((Int256)a.SaturatingSub(b)).Should().Be(Int256.MaxValue);
    }

    [Fact]
    public void Int256Be_SaturatingSub_NegativeOverflow()
    {
        // MinValue - 1 would overflow negative
        Int256Be a = new(Int256.MinValue);
        Int256Be b = new(new Int256(1L));
        ((Int256)a.SaturatingSub(b)).Should().Be(Int256.MinValue);
    }

    [Fact]
    public void Int256Be_SaturatingSub_Equal_ReturnsZero()
    {
        Int256Be a = new(Int256.MaxValue);
        Int256Be b = new(Int256.MaxValue);
        ((Int256)a.SaturatingSub(b)).Should().Be(new Int256(0L));
    }

    [Fact]
    public void Int256Be_SaturatingSub_Zero()
    {
        Int256Be a = new(new Int256(-42L));
        Int256Be b = new(new Int256(0L));
        ((Int256)a.SaturatingSub(b)).Should().Be(new Int256(-42L));
    }

    #endregion

    #region Saturating Arithmetic (Little-Endian 256-bit)

    // ── UInt256Le ───────────────────────────────────────────────

    [Fact]
    public void UInt256Le_SaturatingAdd_Normal()
    {
        UInt256Le a = new(new UInt256(100UL));
        UInt256Le b = new(new UInt256(200UL));
        ((UInt256)a.SaturatingAdd(b)).Should().Be(new UInt256(300UL));
    }

    [Fact]
    public void UInt256Le_SaturatingAdd_Overflow()
    {
        UInt256Le a = new(UInt256.MaxValue);
        UInt256Le b = new(new UInt256(1UL));
        ((UInt256)a.SaturatingAdd(b)).Should().Be(UInt256.MaxValue);
    }

    [Fact]
    public void UInt256Le_SaturatingAdd_BothMax()
    {
        UInt256Le a = new(UInt256.MaxValue);
        UInt256Le b = new(UInt256.MaxValue);
        ((UInt256)a.SaturatingAdd(b)).Should().Be(UInt256.MaxValue);
    }

    [Fact]
    public void UInt256Le_SaturatingAdd_Zero()
    {
        UInt256Le a = new(new UInt256(42UL));
        UInt256Le b = new(UInt256.MinValue);
        ((UInt256)a.SaturatingAdd(b)).Should().Be(new UInt256(42UL));
    }

    [Fact]
    public void UInt256Le_SaturatingSub_Normal()
    {
        UInt256Le a = new(new UInt256(300UL));
        UInt256Le b = new(new UInt256(100UL));
        ((UInt256)a.SaturatingSub(b)).Should().Be(new UInt256(200UL));
    }

    [Fact]
    public void UInt256Le_SaturatingSub_Underflow()
    {
        UInt256Le a = new(new UInt256(10UL));
        UInt256Le b = new(new UInt256(20UL));
        ((UInt256)a.SaturatingSub(b)).Should().Be(UInt256.MinValue);
    }

    [Fact]
    public void UInt256Le_SaturatingSub_Equal_ReturnsZero()
    {
        UInt256Le a = new(UInt256.MaxValue);
        UInt256Le b = new(UInt256.MaxValue);
        ((UInt256)a.SaturatingSub(b)).Should().Be(UInt256.MinValue);
    }

    [Fact]
    public void UInt256Le_SaturatingSub_Zero()
    {
        UInt256Le a = new(new UInt256(42UL));
        UInt256Le b = new(UInt256.MinValue);
        ((UInt256)a.SaturatingSub(b)).Should().Be(new UInt256(42UL));
    }

    // ── Int256Le ────────────────────────────────────────────────

    [Fact]
    public void Int256Le_SaturatingAdd_Normal()
    {
        Int256Le a = new(new Int256(100L));
        Int256Le b = new(new Int256(200L));
        ((Int256)a.SaturatingAdd(b)).Should().Be(new Int256(300L));
    }

    [Fact]
    public void Int256Le_SaturatingAdd_PositiveOverflow()
    {
        Int256Le a = new(Int256.MaxValue);
        Int256Le b = new(new Int256(1L));
        ((Int256)a.SaturatingAdd(b)).Should().Be(Int256.MaxValue);
    }

    [Fact]
    public void Int256Le_SaturatingAdd_NegativeOverflow()
    {
        Int256Le a = new(Int256.MinValue);
        Int256Le b = new(new Int256(-1L));
        ((Int256)a.SaturatingAdd(b)).Should().Be(Int256.MinValue);
    }

    [Fact]
    public void Int256Le_SaturatingAdd_Zero()
    {
        Int256Le a = new(new Int256(-42L));
        Int256Le b = new(new Int256(0L));
        ((Int256)a.SaturatingAdd(b)).Should().Be(new Int256(-42L));
    }

    [Fact]
    public void Int256Le_SaturatingSub_Normal()
    {
        Int256Le a = new(new Int256(300L));
        Int256Le b = new(new Int256(100L));
        ((Int256)a.SaturatingSub(b)).Should().Be(new Int256(200L));
    }

    [Fact]
    public void Int256Le_SaturatingSub_PositiveOverflow()
    {
        // MaxValue - (-1) would overflow positive
        Int256Le a = new(Int256.MaxValue);
        Int256Le b = new(new Int256(-1L));
        ((Int256)a.SaturatingSub(b)).Should().Be(Int256.MaxValue);
    }

    [Fact]
    public void Int256Le_SaturatingSub_NegativeOverflow()
    {
        // MinValue - 1 would overflow negative
        Int256Le a = new(Int256.MinValue);
        Int256Le b = new(new Int256(1L));
        ((Int256)a.SaturatingSub(b)).Should().Be(Int256.MinValue);
    }

    [Fact]
    public void Int256Le_SaturatingSub_Equal_ReturnsZero()
    {
        Int256Le a = new(Int256.MaxValue);
        Int256Le b = new(Int256.MaxValue);
        ((Int256)a.SaturatingSub(b)).Should().Be(new Int256(0L));
    }

    [Fact]
    public void Int256Le_SaturatingSub_Zero()
    {
        Int256Le a = new(new Int256(-42L));
        Int256Le b = new(new Int256(0L));
        ((Int256)a.SaturatingSub(b)).Should().Be(new Int256(-42L));
    }

    #endregion
}
