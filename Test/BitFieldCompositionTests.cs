using FluentAssertions;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

#region Composable BitFields Test Structs

/// <summary>
/// A reusable 8-bit flags structure that can be embedded in larger BitFields.
/// </summary>
[BitFields(typeof(byte))]
public partial struct StatusFlags
{
    [BitFlag(0)] public partial bool Ready { get; set; }
    [BitFlag(1)] public partial bool Error { get; set; }
    [BitFlag(2)] public partial bool Busy { get; set; }
    [BitFlag(3)] public partial bool Complete { get; set; }
    [BitField(4, End = 7)] public partial byte Priority { get; set; }  // 4-bit priority (0-15)
}

/// <summary>
/// A reusable 4-bit command code structure.
/// </summary>
[BitFields(typeof(byte))]
public partial struct CommandCode
{
    [BitField(0, End = 3)] public partial byte Code { get; set; }  // 4-bit command (0-15)
}

/// <summary>
/// A 16-bit protocol header that embeds StatusFlags as a property type.
/// This demonstrates composing BitFields structs.
/// </summary>
[BitFields(typeof(ushort))]
public partial struct ProtocolHeader16
{
    /// <summary>8-bit status flags at bits 0-7.</summary>
    [BitField(0, End = 7)] public partial StatusFlags Status { get; set; }
    
    /// <summary>8-bit payload length at bits 8-15.</summary>
    [BitField(8, End = 15)] public partial byte Length { get; set; }
}

/// <summary>
/// A 32-bit protocol header with multiple embedded BitFields types.
/// </summary>
[BitFields(typeof(uint))]
public partial struct ProtocolHeader32
{
    /// <summary>8-bit status flags at bits 0-7.</summary>
    [BitField(0, End = 7)] public partial StatusFlags Status { get; set; }
    
    /// <summary>4-bit command code at bits 8-11.</summary>
    [BitField(8, End = 11)] public partial CommandCode Command { get; set; }
    
    /// <summary>4-bit version at bits 12-15.</summary>
    [BitField(12, End = 15)] public partial byte Version { get; set; }
    
    /// <summary>16-bit sequence number at bits 16-31.</summary>
    [BitField(16, End = 31)] public partial ushort Sequence { get; set; }
}

/// <summary>
/// A file header structure demonstrating real-world composition.
/// </summary>
[BitFields(typeof(ulong))]
public partial struct FileHeader
{
    /// <summary>Magic number (16 bits).</summary>
    [BitField(0, End = 15)] public partial ushort Magic { get; set; }
    
    /// <summary>Embedded status flags (8 bits).</summary>
    [BitField(16, End = 23)] public partial StatusFlags Flags { get; set; }
    
    /// <summary>Version major (8 bits).</summary>
    [BitField(24, End = 31)] public partial byte VersionMajor { get; set; }
    
    /// <summary>Version minor (8 bits).</summary>
    [BitField(32, End = 39)] public partial byte VersionMinor { get; set; }
    
    /// <summary>Reserved (24 bits).</summary>
    [BitField(40, End = 63)] public partial uint Reserved { get; set; }
}

#endregion

#region Partial-Width Composition Test Structs (Undefined High Bits)

/// <summary>
/// A 9-bit sub-header stored in a ushort (16 bits).
/// Bits 9-15 are UNDEFINED and will be masked to zero.
/// </summary>
[BitFields(typeof(ushort), UndefinedBitsMustBe.Zeroes)]
public partial struct SubHeader9
{
    /// <summary>4-bit type code at bits 0-3.</summary>
    [BitField(0, End = 3)] public partial byte TypeCode { get; set; }
    
    /// <summary>5-bit flags at bits 4-8.</summary>
    [BitField(4, End = 8)] public partial byte Flags { get; set; }
    
    // Bits 9-15 are UNDEFINED - masked to zero
}

/// <summary>
/// A 27-bit protocol header stored in a uint (32 bits).
/// Bits 27-31 are UNDEFINED and will be masked to zero.
/// Contains an embedded 9-bit SubHeader9.
/// </summary>
[BitFields(typeof(uint), UndefinedBitsMustBe.Zeroes)]
public partial struct Header27
{
    /// <summary>9-bit sub-header at bits 0-8.</summary>
    [BitField(0, End = 8)] public partial SubHeader9 SubHeader { get; set; }
    
    /// <summary>10-bit payload size at bits 9-18.</summary>
    [BitField(9, End = 18)] public partial ushort PayloadSize { get; set; }
    
    /// <summary>8-bit sequence at bits 19-26.</summary>
    [BitField(19, End = 26)] public partial byte Sequence { get; set; }
    
    // Bits 27-31 are UNDEFINED - masked to zero
}

/// <summary>
/// A 64-bit main header that embeds the 27-bit Header27.
/// </summary>
[BitFields(typeof(ulong))]
public partial struct MainHeader64
{
    /// <summary>27-bit protocol header at bits 0-26.</summary>
    [BitField(0, End = 26)] public partial Header27 Protocol { get; set; }
    
    /// <summary>5-bit priority at bits 27-31.</summary>
    [BitField(27, End = 31)] public partial byte Priority { get; set; }
    
    /// <summary>32-bit timestamp at bits 32-63.</summary>
    [BitField(32, End = 63)] public partial uint Timestamp { get; set; }
}

/// <summary>
/// Signed version of 27-bit header.
/// Bits 27-31 are UNDEFINED and will be masked to zero.
/// </summary>
[BitFields(typeof(int), UndefinedBitsMustBe.Zeroes)]
public partial struct SignedHeader27
{
    /// <summary>9-bit sub-header at bits 0-8.</summary>
    [BitField(0, End = 8)] public partial SubHeader9 SubHeader { get; set; }
    
    /// <summary>10-bit payload size at bits 9-18.</summary>
    [BitField(9, End = 18)] public partial ushort PayloadSize { get; set; }
    
    /// <summary>8-bit sequence at bits 19-26.</summary>
    [BitField(19, End = 26)] public partial byte Sequence { get; set; }
    
    // Bits 27-31 are UNDEFINED - masked to zero
}

/// <summary>
/// A 9-bit sub-header with Any mode (default) for comparison testing.
/// </summary>
[BitFields(typeof(ushort))]  // Default is UndefinedBitsMustBe.Any
public partial struct SubHeader9Native
{
    /// <summary>4-bit type code at bits 0-3.</summary>
    [BitField(0, End = 3)] public partial byte TypeCode { get; set; }
    
    /// <summary>5-bit flags at bits 4-8.</summary>
    [BitField(4, End = 8)] public partial byte Flags { get; set; }
    
    // Bits 9-15 are UNDEFINED - preserved as raw data
}

/// <summary>
/// A 9-bit sub-header with Ones mode for testing.
/// </summary>
[BitFields(typeof(ushort), UndefinedBitsMustBe.Ones)]
public partial struct SubHeader9Ones
{
    /// <summary>4-bit type code at bits 0-3.</summary>
    [BitField(0, End = 3)] public partial byte TypeCode { get; set; }
    
    /// <summary>5-bit flags at bits 4-8.</summary>
    [BitField(4, End = 8)] public partial byte Flags { get; set; }
    
    // Bits 9-15 are UNDEFINED - set to 1
}

/// <summary>
/// Tests SPARSE undefined bits - bits 0, 3, and 7 are undefined (gaps in the middle).
/// This is a critical edge case: undefined bits are NOT just at the high end.
/// Storage: sbyte (8 bits), Defined: bits 1-2, 4-6, Undefined: bits 0, 3, 7
/// </summary>
[BitFields(typeof(sbyte), UndefinedBitsMustBe.Zeroes)]
public partial struct SparseUndefinedZero
{
    // bit 0: UNDEFINED
    [BitField(1, End = 2)] public partial byte LowField { get; set; }   // bits 1-2 (2 bits)
    // bit 3: UNDEFINED
    [BitField(4, End = 6)] public partial byte HighField { get; set; }  // bits 4-6 (3 bits)
    // bit 7: UNDEFINED
}

/// <summary>
/// Same sparse pattern with Ones mode.
/// </summary>
[BitFields(typeof(sbyte), UndefinedBitsMustBe.Ones)]
public partial struct SparseUndefinedOnes
{
    // bit 0: UNDEFINED
    [BitField(1, End = 2)] public partial byte LowField { get; set; }
    // bit 3: UNDEFINED
    [BitField(4, End = 6)] public partial byte HighField { get; set; }
    // bit 7: UNDEFINED
}

/// <summary>
/// Same sparse pattern with Any mode (default).
/// </summary>
[BitFields(typeof(sbyte))]  // UndefinedBitsMustBe.Any (default)
public partial struct SparseUndefinedNative
{
    // bit 0: UNDEFINED
    [BitField(1, End = 2)] public partial byte LowField { get; set; }
    // bit 3: UNDEFINED
    [BitField(4, End = 6)] public partial byte HighField { get; set; }
    // bit 7: UNDEFINED
}

#endregion

/// <summary>
/// Tests for BitFields struct composition - using one BitFields type as a property type in another.
/// </summary>
public class BitFieldCompositionTests
{
    #region Basic Composition Tests

    /// <summary>
    /// Tests that a BitFields struct can be used as a property type in another BitFields struct.
    /// </summary>
    [Fact]
    public void Composition_StatusFlagsInHeader_GetAndSet()
    {
        ProtocolHeader16 header = 0;
        
        // Get the embedded StatusFlags
        StatusFlags status = header.Status;
        status.Should().Be((StatusFlags)0);
        
        // Create a StatusFlags with values
        StatusFlags newStatus = 0;
        newStatus.Ready = true;
        newStatus.Priority = 5;
        
        // Set it in the header
        header.Status = newStatus;
        
        // Verify the embedded flags are accessible through the header
        header.Status.Ready.Should().BeTrue();
        header.Status.Priority.Should().Be(5);
        
        // Verify other fields are not affected
        header.Length.Should().Be(0);
    }

    /// <summary>
    /// Tests that modifying the embedded struct correctly updates the parent's raw value.
    /// </summary>
    [Fact]
    public void Composition_ModifyEmbedded_UpdatesParentRawValue()
    {
        ProtocolHeader16 header = 0;
        
        // Set status flags
        StatusFlags status = 0;
        status.Ready = true;      // bit 0
        status.Error = true;      // bit 1
        status.Priority = 0xF;    // bits 4-7 = 1111
        
        header.Status = status;
        header.Length = 0x42;
        
        // The raw value should reflect both fields
        // Status: Ready(1) | Error(2) | Priority(0xF << 4) = 0xF3
        // Length: 0x42 << 8 = 0x4200
        // Total: 0x42F3
        ushort raw = header;
        raw.Should().Be(0x42F3);
    }

    /// <summary>
    /// Tests that the embedded struct can be read correctly from a raw value.
    /// </summary>
    [Fact]
    public void Composition_ReadFromRawValue_ExtractsEmbeddedCorrectly()
    {
        // Set up raw value: Status=0xA5, Length=0x7B
        ProtocolHeader16 header = 0x7BA5;
        
        // Extract the status flags
        StatusFlags status = header.Status;
        
        // Verify individual bits (0xA5 = 1010_0101)
        status.Ready.Should().BeTrue();       // bit 0 = 1
        status.Error.Should().BeFalse();      // bit 1 = 0
        status.Busy.Should().BeTrue();        // bit 2 = 1
        status.Complete.Should().BeFalse();   // bit 3 = 0
        status.Priority.Should().Be(0xA);     // bits 4-7 = 1010 = 10
        
        // Verify length
        header.Length.Should().Be(0x7B);
    }

    #endregion

    #region Multiple Embedded Structs Tests

    /// <summary>
    /// Tests a header with multiple embedded BitFields types.
    /// </summary>
    [Fact]
    public void Composition_MultipleEmbedded_IndependentFields()
    {
        ProtocolHeader32 header = 0;
        
        // Set status
        StatusFlags status = 0;
        status.Ready = true;
        status.Priority = 3;
        header.Status = status;
        
        // Set command
        CommandCode cmd = 0;
        cmd.Code = 0xC;
        header.Command = cmd;
        
        // Set other fields
        header.Version = 2;
        header.Sequence = 0x1234;
        
        // Verify all fields
        header.Status.Ready.Should().BeTrue();
        header.Status.Priority.Should().Be(3);
        header.Command.Code.Should().Be(0xC);
        header.Version.Should().Be(2);
        header.Sequence.Should().Be(0x1234);
    }

    /// <summary>
    /// Tests that modifying one embedded struct doesn't affect others.
    /// </summary>
    [Fact]
    public void Composition_ModifyOne_OthersUnchanged()
    {
        ProtocolHeader32 header = 0xFFFFFFFF;  // All bits set
        
        // Clear just the status
        header.Status = 0;
        
        // Verify status is cleared
        header.Status.Ready.Should().BeFalse();
        header.Status.Error.Should().BeFalse();
        header.Status.Priority.Should().Be(0);
        
        // Verify other fields still have their bits set
        header.Command.Code.Should().Be(0xF);
        header.Version.Should().Be(0xF);
        header.Sequence.Should().Be(0xFFFF);
    }

    #endregion

    #region 64-bit Composition Tests

    /// <summary>
    /// Tests composition in a 64-bit struct.
    /// </summary>
    [Fact]
    public void Composition_64Bit_EmbeddedFlagsWork()
    {
        FileHeader header = 0;
        
        // Set magic number
        header.Magic = 0x4D42;  // "BM" for bitmap
        
        // Set flags
        StatusFlags flags = 0;
        flags.Ready = true;
        flags.Complete = true;
        flags.Priority = 7;
        header.Flags = flags;
        
        // Set versions
        header.VersionMajor = 3;
        header.VersionMinor = 14;
        
        // Verify
        header.Magic.Should().Be(0x4D42);
        header.Flags.Ready.Should().BeTrue();
        header.Flags.Complete.Should().BeTrue();
        header.Flags.Priority.Should().Be(7);
        header.VersionMajor.Should().Be(3);
        header.VersionMinor.Should().Be(14);
    }

    /// <summary>
    /// Tests round-trip of embedded struct through raw value.
    /// </summary>
    [Fact]
    public void Composition_RoundTrip_PreservesAllBits()
    {
        // Create a header with known values
        ProtocolHeader32 original = 0;
        
        StatusFlags status = 0;
        status.Ready = true;
        status.Error = true;
        status.Priority = 0xA;
        original.Status = status;
        
        CommandCode cmd = 0;
        cmd.Code = 0x5;
        original.Command = cmd;
        
        original.Version = 0x3;
        original.Sequence = 0xBEEF;
        
        // Convert to raw and back
        uint raw = original;
        ProtocolHeader32 restored = raw;
        
        // Verify all fields preserved
        restored.Status.Ready.Should().Be(original.Status.Ready);
        restored.Status.Error.Should().Be(original.Status.Error);
        restored.Status.Priority.Should().Be(original.Status.Priority);
        restored.Command.Code.Should().Be(original.Command.Code);
        restored.Version.Should().Be(original.Version);
        restored.Sequence.Should().Be(original.Sequence);
    }

    #endregion

    #region Implicit Conversion Tests

    /// <summary>
    /// Tests that embedded BitFields types use implicit conversion correctly.
    /// </summary>
    [Fact]
    public void Composition_ImplicitConversion_Works()
    {
        ProtocolHeader16 header = 0;
        
        // StatusFlags has implicit conversion from byte
        // This should work if the property type is correctly handled
        header.Status = 0xAB;  // Implicit byte -> StatusFlags
        
        StatusFlags status = header.Status;
        byte statusByte = status;  // Implicit StatusFlags -> byte
        statusByte.Should().Be(0xAB);
    }

    /// <summary>
    /// Tests chained property access.
    /// </summary>
    [Fact]
    public void Composition_ChainedAccess_ReadProperties()
    {
        ProtocolHeader16 header = 0x42F3;
        
        // Access nested properties directly
        // This works because header.Status returns a StatusFlags value
        bool isReady = header.Status.Ready;
        byte priority = header.Status.Priority;
        
        isReady.Should().BeTrue();     // bit 0 of 0xF3 = 1
        priority.Should().Be(0xF);     // bits 4-7 of 0xF3 = 1111
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Tests embedded struct at bit 0.
    /// </summary>
    [Fact]
    public void Composition_AtBitZero_WorksCorrectly()
    {
        ProtocolHeader16 header = 0;
        
        StatusFlags status = 0xFF;  // All bits set
        header.Status = status;
        
        // Status should be in bits 0-7
        ushort raw = header;
        raw.Should().Be(0x00FF);
        
        // Read back
        header.Status.Should().Be((StatusFlags)0xFF);
    }

    /// <summary>
    /// Tests embedded struct not at bit 0.
    /// </summary>
    [Fact]
    public void Composition_NotAtBitZero_ShiftsCorrectly()
    {
        ProtocolHeader32 header = 0;
        
        CommandCode cmd = 0;
        cmd.Code = 0xA;
        header.Command = cmd;
        
        // Command is at bits 8-11
        uint raw = header;
        raw.Should().Be(0x00000A00u);
        
        
        // Read back
        header.Command.Code.Should().Be(0xA);
    }

    /// <summary>
    /// Tests that the embedded struct's full width is used.
    /// </summary>
    [Fact]
    public void Composition_FullWidth_AllBitsAccessible()
    {
        ProtocolHeader16 header = 0;
        
        // StatusFlags is 8 bits, embedded in bits 0-7
        // Set all 8 bits through the embedded struct
        StatusFlags status = 0;
        status.Ready = true;      // bit 0
        status.Error = true;      // bit 1  
        status.Busy = true;       // bit 2
        status.Complete = true;   // bit 3
        status.Priority = 0xF;    // bits 4-7
        
        header.Status = status;
        
        // Verify raw value
        ushort raw = header;
        (raw & 0xFF).Should().Be(0xFF);  // All 8 low bits set
    }

    #endregion

    #region Partial-Width Composition Tests (Undefined High Bits)

    /// <summary>
    /// Tests basic functionality of the 9-bit sub-header (stored in ushort).
    /// Only bits 0-8 are defined; bits 9-15 are undefined.
    /// </summary>
    [Fact]
    public void PartialWidth_SubHeader9_BasicFunctionality()
    {
        SubHeader9 sub = 0;
        
        // Set defined fields
        sub.TypeCode = 0xA;   // bits 0-3
        sub.Flags = 0x1F;     // bits 4-8 (max 5-bit value)
        
        // Verify fields
        sub.TypeCode.Should().Be(0xA);
        sub.Flags.Should().Be(0x1F);
        
        // Raw value should only have bits 0-8 set
        // TypeCode = 0xA (bits 0-3) = 0x00A
        // Flags = 0x1F (bits 4-8) = 0x1F << 4 = 0x1F0
        // Total = 0x1FA
        ushort raw = sub;
        raw.Should().Be(0x01FA);
    }

    /// <summary>
    /// Tests that undefined high bits in SubHeader9 are masked off consistently.
    /// Whether standalone or embedded, undefined bits are always zero.
    /// </summary>
    [Fact]
    public void PartialWidth_SubHeader9_UndefinedBitsMaskedOff()
    {
        // Create a SubHeader9 with garbage in the undefined high bits
        // The implicit conversion should mask them off
        SubHeader9 sub = 0xFFFF;  // All 16 bits set, but only 9 are defined
        
        // The defined fields should read correctly
        sub.TypeCode.Should().Be(0xF);    // bits 0-3 = 1111
        sub.Flags.Should().Be(0x1F);      // bits 4-8 = 11111
        
        // The raw value should NOT have the undefined bits set
        // because implicit conversion masks them off
        ushort raw = sub;
        (raw & 0xFE00).Should().Be(0, "undefined bits 9-15 should be masked off by implicit conversion");
        raw.Should().Be(0x01FF, "only defined bits 0-8 should be set");
    }

    /// <summary>
    /// Tests the 27-bit header with embedded 9-bit sub-header.
    /// </summary>
    [Fact]
    public void PartialWidth_Header27_WithEmbeddedSubHeader()
    {
        Header27 header = 0;
        
        // Create sub-header
        SubHeader9 sub = 0;
        sub.TypeCode = 0xB;
        sub.Flags = 0x15;
        
        // Set all fields
        header.SubHeader = sub;
        header.PayloadSize = 0x3FF;  // Max 10-bit value
        header.Sequence = 0xAB;
        
        // Verify
        header.SubHeader.TypeCode.Should().Be(0xB);
        header.SubHeader.Flags.Should().Be(0x15);
        header.PayloadSize.Should().Be(0x3FF);
        header.Sequence.Should().Be(0xAB);
        
        // Calculate expected raw value (bits 0-26 only)
        // SubHeader (bits 0-8): TypeCode=0xB | (Flags=0x15 << 4) = 0x15B
        // PayloadSize (bits 9-18): 0x3FF << 9 = 0x7FE00
        // Sequence (bits 19-26): 0xAB << 19 = 0x55800000
        // Total = 0x0557FFDB (27 bits)
        uint raw = header;
        (raw & 0x07FFFFFF).Should().Be((0xABu << 19) | (0x3FFu << 9) | 0x15Bu);
    }

    /// <summary>
    /// Tests that undefined high bits (27-31) in Header27 are masked off consistently.
    /// </summary>
    [Fact]
    public void PartialWidth_Header27_UndefinedBitsMaskedOff()
    {
        // Try to set all 32 bits - undefined bits should be masked off
        Header27 header = 0xFFFFFFFF;
        
        // Defined fields should read correctly
        header.SubHeader.TypeCode.Should().Be(0xF);
        header.SubHeader.Flags.Should().Be(0x1F);
        header.PayloadSize.Should().Be(0x3FF);
        header.Sequence.Should().Be(0xFF);
        
        // Undefined bits (27-31) should NOT be set - they're masked off
        uint raw = header;
        (raw & 0xF8000000u).Should().Be(0u, "undefined bits 27-31 should be masked off");
        (raw & 0x07FFFFFFu).Should().Be(0x07FFFFFFu, "defined bits 0-26 should all be set");
    }

    /// <summary>
    /// Tests the full 3-level nesting: MainHeader64 -> Header27 -> SubHeader9.
    /// </summary>
    [Fact]
    public void PartialWidth_ThreeLevelNesting_FullRoundTrip()
    {
        MainHeader64 main = 0;
        
        // Build from innermost out
        SubHeader9 sub = 0;
        sub.TypeCode = 0x7;
        sub.Flags = 0x12;
        
        Header27 protocol = 0;
        protocol.SubHeader = sub;
        protocol.PayloadSize = 512;
        protocol.Sequence = 42;
        
        main.Protocol = protocol;
        main.Priority = 0x1F;  // Max 5-bit value
        main.Timestamp = 0xDEADBEEF;
        
        // Verify all levels
        main.Protocol.SubHeader.TypeCode.Should().Be(0x7);
        main.Protocol.SubHeader.Flags.Should().Be(0x12);
        main.Protocol.PayloadSize.Should().Be(512);
        main.Protocol.Sequence.Should().Be(42);
        main.Priority.Should().Be(0x1F);
        main.Timestamp.Should().Be(0xDEADBEEF);
        
        // Round-trip through raw value
        ulong raw = main;
        MainHeader64 restored = raw;
        
        restored.Protocol.SubHeader.TypeCode.Should().Be(0x7);
        restored.Protocol.SubHeader.Flags.Should().Be(0x12);
        restored.Protocol.PayloadSize.Should().Be(512);
        restored.Protocol.Sequence.Should().Be(42);
        restored.Priority.Should().Be(0x1F);
        restored.Timestamp.Should().Be(0xDEADBEEF);
    }

    /// <summary>
    /// Tests that when embedding a partial-width struct, only the defined bits are transferred.
    /// The undefined high bits of the embedded struct's storage type are MASKED OFF.
    /// </summary>
    [Fact]
    public void PartialWidth_EmbeddingMasksUndefinedBits()
    {
        // Create a Header27 with garbage in undefined bits (27-31)
        Header27 protocol = 0xFFFFFFFF;  // All bits set
        
        // Set actual field values
        SubHeader9 sub = 0;
        sub.TypeCode = 0x3;
        sub.Flags = 0x0A;
        protocol.SubHeader = sub;
        protocol.PayloadSize = 100;
        protocol.Sequence = 5;
        
        // Embed in MainHeader64 - only bits 0-26 should be transferred
        MainHeader64 main = 0;
        main.Protocol = protocol;
        main.Priority = 0;
        main.Timestamp = 0;
        
        // The raw value should NOT have bits 27-31 from the protocol's undefined region
        // because MainHeader64.Protocol is defined as [BitField(0, End = 26)]
        ulong raw = main;
        
        // Bits 27-31 in main are the Priority field, which we set to 0
        // So bits 27-31 should be 0, not the garbage from protocol's undefined bits
        ((raw >> 27) & 0x1F).Should().Be(0, "Priority is 0");
        
        // Verify the protocol data is correct (bits 0-26)
        main.Protocol.SubHeader.TypeCode.Should().Be(0x3);
        main.Protocol.SubHeader.Flags.Should().Be(0x0A);
        main.Protocol.PayloadSize.Should().Be(100);
        main.Protocol.Sequence.Should().Be(5);
    }

    /// <summary>
    /// Tests behavior of undefined bits with unsigned storage types.
    /// Undefined bits are always masked off - they cannot hold data.
    /// </summary>
    [Fact]
    public void PartialWidth_UnsignedStorage_UndefinedBitsMaskedOff()
    {
        // Header27 uses uint (unsigned)
        Header27 header = 0;
        
        // Set fields
        header.SubHeader = 0;
        header.PayloadSize = 0;
        header.Sequence = 0;
        
        // Get raw - should be 0
        uint raw = header;
        raw.Should().Be(0);
        
        // Try to set undefined bits directly via implicit conversion
        header = 0x80000000;  // Try to set bit 31 (undefined)
        
        // Fields should be 0
        header.SubHeader.TypeCode.Should().Be(0);
        header.PayloadSize.Should().Be(0);
        header.Sequence.Should().Be(0);
        
        // Undefined bit should be masked off
        raw = header;
        (raw & 0x80000000).Should().Be(0, "undefined bit 31 should be masked off");
    }

    /// <summary>
    /// Tests behavior with signed storage type (int) - undefined bits are still masked off.
    /// The raw value can only represent defined bits, so sign is determined by highest defined bit.
    /// </summary>
    [Fact]
    public void PartialWidth_SignedStorage_UndefinedBitsMaskedOff()
    {
        // SignedHeader27 uses int (signed), but only bits 0-26 are defined
        SignedHeader27 header = 0;
        
        // Set some field values
        SubHeader9 sub = 0;
        sub.TypeCode = 5;
        header.SubHeader = sub;
        header.PayloadSize = 100;
        header.Sequence = 10;
        
        // Get as signed - should be positive (highest defined bit 26 is not the sign bit)
        int signedRaw = header;
        signedRaw.Should().BeGreaterThan(0);
        
        // Try to set bit 31 (undefined) - it should be masked off
        header = unchecked((int)0x80000000);
        
        // All fields should be 0 (because bits 0-26 are all 0 after masking)
        header.SubHeader.TypeCode.Should().Be(0);
        header.PayloadSize.Should().Be(0);
        header.Sequence.Should().Be(0);
        
        // The raw int should NOT be negative - bit 31 was masked off
        signedRaw = header;
        signedRaw.Should().Be(0, "undefined bits including bit 31 should be masked off");
    }

    /// <summary>
    /// Demonstrates the key insight: undefined bits are always masked off,
    /// ensuring consistent behavior whether standalone or embedded.
    /// </summary>
    [Fact]
    public void PartialWidth_Documentation_UndefinedBitsAlwaysMaskedOff()
    {
        // SubHeader9 has 9 defined bits (0-8), stored in ushort (16 bits)
        // Bits 9-15 are undefined and will be masked off (AlwaysZero mode)
        
        // Try to create with all bits set
        SubHeader9 sub = 0xFFFF;
        
        // Only defined bits affect field values
        sub.TypeCode.Should().Be(0xF);   // bits 0-3
        sub.Flags.Should().Be(0x1F);     // bits 4-8
        
        // The raw value only has the 9 defined bits - undefined bits are masked off
        ushort raw = sub;
        raw.Should().Be(0x01FF, "only 9 defined bits, undefined bits masked off");
        
        // When embedded in Header27[0,8], the same 9 bits are copied
        Header27 header = 0;
        header.SubHeader = sub;
        
        // The embedded sub-header has the same values
        header.SubHeader.TypeCode.Should().Be(0xF);
        header.SubHeader.Flags.Should().Be(0x1F);
        
        // Verify consistency: standalone and embedded behave identically
        SubHeader9 extracted = header.SubHeader;
        ((ushort)extracted).Should().Be((ushort)sub, "embedded and standalone should be identical");
    }


    #endregion

    #region UndefinedBits MustBe Mode Tests

    /// <summary>
    /// Tests MustBe.Any mode (default) - undefined bits are preserved.
    /// </summary>
    [Fact]
    public void UndefinedBits_Any_PreservesUndefinedBits()
    {
        // SubHeader9Native uses Any mode (default)
        SubHeader9Native sub = 0xFFFF;  // All 16 bits set
        
        // Defined fields work correctly
        sub.TypeCode.Should().Be(0xF);
        sub.Flags.Should().Be(0x1F);
        
        // Raw value preserves all bits including undefined
        ushort raw = sub;
        raw.Should().Be(0xFFFF, "Any mode preserves undefined bits");
    }

    /// <summary>
    /// Tests MustBe.Zero mode - undefined bits are masked to zero.
    /// </summary>
    [Fact]
    public void UndefinedBits_Zero_MasksUndefinedBits()
    {
        // SubHeader9 uses MustBe.Zero mode
        SubHeader9 sub = 0xFFFF;  // All 16 bits set
        
        // Defined fields work correctly
        sub.TypeCode.Should().Be(0xF);
        sub.Flags.Should().Be(0x1F);
        
        // Raw value only has defined bits - undefined bits are zeroed
        ushort raw = sub;
        raw.Should().Be(0x01FF, "Zero mode masks undefined bits to zero");
        (raw & 0xFE00).Should().Be(0, "bits 9-15 should be zero");
    }


    /// <summary>
    /// Tests MustBe.One mode - undefined bits are set to one.
    /// </summary>
    [Fact]
    public void UndefinedBits_One_SetsUndefinedBits()
    {
        // SubHeader9Ones uses MustBe.Ones mode
        SubHeader9Ones sub = 0x0000;  // All bits clear
        
        // Defined fields are 0
        sub.TypeCode.Should().Be(0);
        sub.Flags.Should().Be(0);
        
        // Raw value has undefined bits set to 1
        ushort raw = sub;
        (raw & 0xFE00).Should().Be(0xFE00, "One mode sets undefined bits to 1");
        raw.Should().Be(0xFE00, "defined bits are 0, undefined bits are 1");
        
        // Set some defined fields
        sub.TypeCode = 0xA;
        sub.Flags = 0x15;
        
        raw = sub;
        (raw & 0xFE00).Should().Be(0xFE00, "undefined bits still 1 after setting fields");
        (raw & 0x01FF).Should().Be(0x015A, "defined bits reflect field values");
    }

    /// <summary>
    /// Tests that all three modes work correctly with the same defined field values.
    /// </summary>
    [Fact]
    public void UndefinedBits_AllModes_DefinedFieldsWorkSame()
    {
        // Set same field values in all three modes
        SubHeader9Native native = 0;
        native.TypeCode = 0x5;
        native.Flags = 0x12;

        SubHeader9 zero = 0;
        zero.TypeCode = 0x5;
        zero.Flags = 0x12;

        SubHeader9Ones ones = 0;
        ones.TypeCode = 0x5;
        ones.Flags = 0x12;

        // All three should have same defined bits
        ushort nativeRaw = native;
        ushort zeroRaw = zero;
        ushort onesRaw = ones;

        // Defined bits (0-8) should be identical
        (nativeRaw & 0x01FF).Should().Be(0x0125);
        (zeroRaw & 0x01FF).Should().Be(0x0125);
        (onesRaw & 0x01FF).Should().Be(0x0125);

        // Undefined bits differ by mode
        (nativeRaw & 0xFE00).Should().Be(0x0000, "Any mode starts with 0s");
        (zeroRaw & 0xFE00).Should().Be(0x0000, "Zero mode always 0");
        (onesRaw & 0xFE00).Should().Be(0xFE00, "One mode always 1");
    }

    #endregion

    #region Sparse Undefined Bits Tests (Edge Cases)

    /// <summary>
    /// Tests that sparse undefined bits (not just high bits) are correctly handled.
    /// Pattern: bits 0, 3, 7 are undefined; bits 1-2, 4-6 are defined.
    /// </summary>
    [Fact]
    public void SparseUndefined_AlwaysZero_SetToMinusOne_UndefinedBitsAreZero()
    {
        // Set all bits (sbyte -1 = 0xFF)
        SparseUndefinedZero reg = unchecked((sbyte)-1);  // 0xFF
        
        // Defined bits should be set
        reg.LowField.Should().Be(0x03, "bits 1-2 should be 11");
        reg.HighField.Should().Be(0x07, "bits 4-6 should be 111");
        
        // Undefined bits (0, 3, 7) should be zero
        sbyte raw = reg;
        (raw & 0x01).Should().Be(0, "bit 0 should be 0 (undefined)");
        (raw & 0x08).Should().Be(0, "bit 3 should be 0 (undefined)");
        (raw & 0x80).Should().Be(0, "bit 7 should be 0 (undefined, and sign bit stays 0)");
        
        // Defined bits should be set: bits 1-2 (0x06), bits 4-6 (0x70)
        // Expected: 0x76 = 0b0111_0110
        raw.Should().Be(0x76, "only defined bits should be set");
    }

    /// <summary>
    /// Tests that OR operation doesn't set undefined bits.
    /// </summary>
    [Fact]
    public void SparseUndefined_AlwaysZero_OrWithUndefinedBit_StaysZero()
    {
        SparseUndefinedZero reg = 0;
        
        // Try to set bit 3 (undefined) via OR
        reg = (SparseUndefinedZero)((sbyte)reg | 0x08);  // OR with bit 3
        
        sbyte raw = reg;
        (raw & 0x08).Should().Be(0, "bit 3 should still be 0 after OR (undefined)");
    }

    /// <summary>
    /// Tests that addition doesn't carry into undefined bit 0.
    /// </summary>
    [Fact]
    public void SparseUndefined_AlwaysZero_AddOne_NoCarryToUndefinedBit0()
    {
        // If bit 0 is undefined (always 0), adding 1 shouldn't set it
        // and shouldn't carry through the defined bits in unexpected ways
        SparseUndefinedZero reg = 0;
        
        // Add 1 - this should NOT set bit 0 since it's undefined
        reg = (SparseUndefinedZero)((sbyte)reg + 1);
        
        sbyte raw = reg;
        (raw & 0x01).Should().Be(0, "bit 0 should still be 0 after +1 (undefined)");
        
        // The addition result gets masked, so the value stays 0
        raw.Should().Be(0, "adding 1 to 0 with undefined bit 0 results in 0");
    }

    /// <summary>
    /// Tests that setting a field doesn't affect undefined bits.
    /// </summary>
    [Fact]
    public void SparseUndefined_AlwaysZero_SetField_UndefinedBitsStayZero()
    {
        SparseUndefinedZero reg = 0;
        
        // Set the defined fields to max values
        reg.LowField = 0x03;   // bits 1-2 = 11
        reg.HighField = 0x07;  // bits 4-6 = 111
        
        sbyte raw = reg;
        
        // Undefined bits should still be 0
        (raw & 0x01).Should().Be(0, "bit 0 should be 0");
        (raw & 0x08).Should().Be(0, "bit 3 should be 0");
        (raw & 0x80).Should().Be(0, "bit 7 should be 0");
        
        // Value should be: bits 1-2 (0x06) | bits 4-6 (0x70) = 0x76
        raw.Should().Be(0x76);
    }

    /// <summary>
    /// Tests AlwaysOne mode with sparse undefined bits.
    /// </summary>
    [Fact]
    public void SparseUndefined_AlwaysOne_SetToZero_UndefinedBitsAreOne()
    {
        // Set all bits to 0
        SparseUndefinedOnes reg = 0;
        
        // Defined bits should be 0
        reg.LowField.Should().Be(0);
        reg.HighField.Should().Be(0);
        
        // Undefined bits (0, 3, 7) should be 1
        sbyte raw = reg;
        (raw & 0x01).Should().Be(0x01, "bit 0 should be 1 (undefined)");
        (raw & 0x08).Should().Be(0x08, "bit 3 should be 1 (undefined)");
        // raw & 0x80 promotes to int and returns 0x80 (128), not -128
        (raw & 0x80).Should().Be(0x80, "bit 7 should be 1 (undefined)");
        
        // Expected: bits 0, 3, 7 set = 0x89 = 0b1000_1001 = -119 as sbyte
        raw.Should().Be(unchecked((sbyte)0x89));
    }

    /// <summary>
    /// Tests Native mode with sparse undefined bits - preserves whatever is there.
    /// </summary>
    [Fact]
    public void SparseUndefined_Native_PreservesAllBits()
    {
        // Set all bits
        SparseUndefinedNative reg = unchecked((sbyte)-1);  // 0xFF
        
        // All bits should be preserved
        sbyte raw = reg;
        raw.Should().Be(-1, "Native mode preserves all bits including undefined");
        
        // Now set to a specific pattern
        reg = unchecked((sbyte)0xAA);  // 0b1010_1010
        raw = reg;
        raw.Should().Be(unchecked((sbyte)0xAA), "Native mode preserves exact bit pattern");
    }

    /// <summary>
    /// Tests that all three modes have same defined field behavior with sparse bits.
    /// </summary>
    [Fact]
    public void SparseUndefined_AllModes_DefinedFieldsWork()
    {
        SparseUndefinedZero zero = 0;
        SparseUndefinedOnes ones = 0;
        SparseUndefinedNative native = 0;
        
        // Set same field values
        zero.LowField = 2;
        zero.HighField = 5;
        
        ones.LowField = 2;
        ones.HighField = 5;
        
        native.LowField = 2;
        native.HighField = 5;
        
        // All should read same field values
        zero.LowField.Should().Be(2);
        zero.HighField.Should().Be(5);
        
        ones.LowField.Should().Be(2);
        ones.HighField.Should().Be(5);
        
        native.LowField.Should().Be(2);
        native.HighField.Should().Be(5);
        
        // But raw values differ in undefined bits
        // Defined bits: LowField=2 at bits 1-2 = 0x04, HighField=5 at bits 4-6 = 0x50
        // Total defined bits pattern = 0x54
        sbyte zeroRaw = zero;
        sbyte onesRaw = ones;
        sbyte nativeRaw = native;
        
        (zeroRaw & 0x76).Should().Be(0x54, "defined bits in AlwaysZero");
        (onesRaw & 0x76).Should().Be(0x54, "defined bits in AlwaysOne");
        (nativeRaw & 0x76).Should().Be(0x54, "defined bits in Native");
        
        // Undefined bits differ (& 0x89 promotes to int, so compare with int values)
        (zeroRaw & 0x89).Should().Be(0x00, "undefined bits are 0 in AlwaysZero");
        (onesRaw & 0x89).Should().Be(0x89, "undefined bits are 1 in AlwaysOne");
        (nativeRaw & 0x89).Should().Be(0x00, "undefined bits start at 0 in Native");
    }

    #endregion
}

#region BitFields(N) Smallest Backing Store Composition Test Structs

/// <summary>
/// A 5-bit status code declared with [BitFields(5)].
/// Backed by byte (smallest type for 5 bits).
/// </summary>
[BitFields(5)]
public partial struct StatusCode5
{
    [BitField(0, End = 2)] public partial byte Category { get; set; }   // 3-bit category (0-7)
    [BitFlag(3)] public partial bool Urgent { get; set; }          // 1-bit flag
    [BitFlag(4)] public partial bool Acknowledged { get; set; }    // 1-bit flag
}

/// <summary>
/// A 12-bit sensor reading declared with [BitFields(12)].
/// Backed by ushort (smallest type for 12 bits).
/// </summary>
[BitFields(12)]
public partial struct SensorReading12
{
    [BitField(0, End = 9)] public partial ushort AdcValue { get; set; }  // 10-bit ADC value (0-1023)
    [BitField(10, End = 11)] public partial byte Channel { get; set; } // 2-bit channel (0-3)
}

/// <summary>
/// A 24-bit RGB color declared with [BitFields(24)].
/// Backed by uint (smallest type for 24 bits).
/// </summary>
[BitFields(24, UndefinedBitsMustBe.Zeroes)]
public partial struct RgbColor24
{
    [BitField(0, End = 7)] public partial byte Red { get; set; }
    [BitField(8, End = 15)] public partial byte Green { get; set; }
    [BitField(16, End = 23)] public partial byte Blue { get; set; }
}

/// <summary>
/// A 48-bit timestamp declared with [BitFields(48)].
/// Backed by ulong (smallest type for 48 bits).
/// </summary>
[BitFields(48, UndefinedBitsMustBe.Zeroes)]
public partial struct Timestamp48
{
    [BitField(0, End = 31)] public partial uint Seconds { get; set; }   // 32-bit seconds
    [BitField(32, End = 47)] public partial ushort Millis { get; set; } // 16-bit milliseconds
}

/// <summary>
/// A 32-bit struct that embeds a [BitFields(5)] and a [BitFields(12)] struct.
/// Tests composing smallest-backing-store structs inside a typeof(T) struct.
/// </summary>
[BitFields(typeof(uint))]
public partial struct PacketWithNarrowFields
{
    [BitField(0, End = 4)] public partial StatusCode5 Status { get; set; }
    [BitField(5, End = 16)] public partial SensorReading12 Sensor { get; set; }
    [BitField(17, End = 31)] public partial ushort SequenceNum { get; set; }
}

/// <summary>
/// A 64-bit struct that embeds a [BitFields(24)] and a [BitFields(5)] struct.
/// Tests composing smallest-backing-store structs inside a typeof(T) struct.
/// </summary>
[BitFields(typeof(ulong))]
public partial struct FrameWithColor
{
    [BitField(0, End = 23)] public partial RgbColor24 Color { get; set; }
    [BitField(24, End = 28)] public partial StatusCode5 Status { get; set; }
    [BitField(32, End = 63)] public partial uint Payload { get; set; }
}

/// <summary>
/// A [BitFields(N)] struct embedding another [BitFields(N)] struct.
/// Tests N-to-N composition with smallest backing stores.
/// </summary>
[BitFields(32, UndefinedBitsMustBe.Zeroes)]
public partial struct Packet32WithStatus5
{
    [BitField(0, End = 4)] public partial StatusCode5 Status { get; set; }
    [BitField(5, End = 15)] public partial ushort DataField { get; set; }
    [BitField(16, End = 31)] public partial ushort Checksum { get; set; }
}

/// <summary>
/// Record struct view that embeds [BitFields(N)] structs.
/// Tests smallest-backing-store structs as property types in views.
/// </summary>
[BitFields]
public partial record struct ViewWithNarrowStructs
{
    [BitField(0, End = 4)] public partial StatusCode5 Status { get; set; }
    [BitField(8, End = 19)] public partial SensorReading12 Sensor { get; set; }
    [BitField(24, End = 47)] public partial RgbColor24 Color { get; set; }
    [BitField(48, End = 55)] public partial byte Tag { get; set; }
}

#endregion

/// <summary>
/// Tests for [BitFields(N)] smallest-backing-store structs used as embedded property types.
/// Covers composing N-bit structs (N&lt;=64) in value-type structs, other N-bit structs,
/// and record struct views.
/// </summary>
public class BitFieldsNarrowCompositionTests
{
    #region [BitFields(N)] Basic Standalone Tests

    [Fact]
    public void StatusCode5_RoundTrip()
    {
        StatusCode5 sc = 0;
        sc.Category = 5;
        sc.Urgent = true;
        sc.Acknowledged = false;

        sc.Category.Should().Be(5);
        sc.Urgent.Should().BeTrue();
        sc.Acknowledged.Should().BeFalse();

        // Byte backing: category=5 (bits 0-2) | urgent=1 (bit 3) = 0x0D
        byte raw = sc;
        raw.Should().Be(0x0D);
    }

    [Fact]
    public void SensorReading12_RoundTrip()
    {
        SensorReading12 sr = 0;
        sr.AdcValue = 1023;
        sr.Channel = 3;

        sr.AdcValue.Should().Be(1023);
        sr.Channel.Should().Be(3);

        // ushort backing: value=1023 (bits 0-9) | channel=3 (bits 10-11) = 0x0FFF
        ushort raw = sr;
        raw.Should().Be(0x0FFF);
    }

    [Fact]
    public void RgbColor24_RoundTrip()
    {
        RgbColor24 c = 0;
        c.Red = 0xAB;
        c.Green = 0xCD;
        c.Blue = 0xEF;

        c.Red.Should().Be(0xAB);
        c.Green.Should().Be(0xCD);
        c.Blue.Should().Be(0xEF);

        // uint backing: 0x00EFCDAB (LE bit order)
        uint raw = c;
        raw.Should().Be(0x00EFCDAB);
    }

    [Fact]
    public void Timestamp48_RoundTrip()
    {
        Timestamp48 ts = 0;
        ts.Seconds = 0xDEADBEEF;
        ts.Millis = 999;

        ts.Seconds.Should().Be(0xDEADBEEF);
        ts.Millis.Should().Be(999);
    }

    [Fact]
    public void RgbColor24_UndefinedBitsMaskedOff()
    {
        // Set all 32 bits — undefined bits (24-31) should be masked to zero
        RgbColor24 c = 0xFFFFFFFF;
        uint raw = c;
        (raw & 0xFF000000u).Should().Be(0, "undefined bits 24-31 should be masked off");
        (raw & 0x00FFFFFFu).Should().Be(0x00FFFFFFu, "defined bits 0-23 should all be set");
    }

    #endregion

    #region [BitFields(N)] Embedded in typeof(T) Struct

    [Fact]
    public void PacketWithNarrowFields_Set_And_Get()
    {
        PacketWithNarrowFields pkt = 0;

        var status = new StatusCode5();
        status.Category = 7;
        status.Urgent = true;
        pkt.Status = status;

        var sensor = new SensorReading12();
        sensor.AdcValue = 512;
        sensor.Channel = 2;
        pkt.Sensor = sensor;

        pkt.SequenceNum = 0x7FFF;

        pkt.Status.Category.Should().Be(7);
        pkt.Status.Urgent.Should().BeTrue();
        pkt.Sensor.AdcValue.Should().Be(512);
        pkt.Sensor.Channel.Should().Be(2);
        pkt.SequenceNum.Should().Be(0x7FFF);
    }

    [Fact]
    public void PacketWithNarrowFields_RawRoundTrip()
    {
        PacketWithNarrowFields pkt = 0;

        var status = new StatusCode5();
        status.Category = 3;
        status.Acknowledged = true;
        pkt.Status = status;

        var sensor = new SensorReading12();
        sensor.AdcValue = 100;
        sensor.Channel = 1;
        pkt.Sensor = sensor;

        pkt.SequenceNum = 42;

        uint raw = pkt;
        PacketWithNarrowFields restored = raw;

        restored.Status.Category.Should().Be(3);
        restored.Status.Acknowledged.Should().BeTrue();
        restored.Sensor.AdcValue.Should().Be(100);
        restored.Sensor.Channel.Should().Be(1);
        restored.SequenceNum.Should().Be(42);
    }

    [Fact]
    public void PacketWithNarrowFields_ModifyOneField_OthersUnchanged()
    {
        PacketWithNarrowFields pkt = 0;

        var status = new StatusCode5();
        status.Category = 5;
        status.Urgent = true;
        pkt.Status = status;

        var sensor = new SensorReading12();
        sensor.AdcValue = 999;
        sensor.Channel = 3;
        pkt.Sensor = sensor;

        pkt.SequenceNum = 1000;

        // Modify only the sensor
        var newSensor = new SensorReading12();
        newSensor.AdcValue = 0;
        newSensor.Channel = 0;
        pkt.Sensor = newSensor;

        // Status and sequence should be untouched
        pkt.Status.Category.Should().Be(5);
        pkt.Status.Urgent.Should().BeTrue();
        pkt.SequenceNum.Should().Be(1000);
        pkt.Sensor.AdcValue.Should().Be(0);
        pkt.Sensor.Channel.Should().Be(0);
    }

    [Fact]
    public void FrameWithColor_EmbeddedRgb24_RoundTrip()
    {
        FrameWithColor frame = 0;

        var color = new RgbColor24();
        color.Red = 0xFF;
        color.Green = 0x80;
        color.Blue = 0x00;
        frame.Color = color;

        var status = new StatusCode5();
        status.Category = 6;
        status.Urgent = true;
        frame.Status = status;

        frame.Payload = 0xCAFEBABE;

        frame.Color.Red.Should().Be(0xFF);
        frame.Color.Green.Should().Be(0x80);
        frame.Color.Blue.Should().Be(0x00);
        frame.Status.Category.Should().Be(6);
        frame.Status.Urgent.Should().BeTrue();
        frame.Payload.Should().Be(0xCAFEBABE);
    }

    #endregion

    #region [BitFields(N)] Embedded in [BitFields(N)] Struct

    [Fact]
    public void Packet32WithStatus5_NToN_RoundTrip()
    {
        Packet32WithStatus5 pkt = 0;

        var status = new StatusCode5();
        status.Category = 4;
        status.Urgent = true;
        status.Acknowledged = true;
        pkt.Status = status;

        pkt.DataField = 0x7FF;
        pkt.Checksum = 0xBEEF;

        pkt.Status.Category.Should().Be(4);
        pkt.Status.Urgent.Should().BeTrue();
        pkt.Status.Acknowledged.Should().BeTrue();
        pkt.DataField.Should().Be(0x7FF);
        pkt.Checksum.Should().Be(0xBEEF);

        // Round-trip through raw
        uint raw = pkt;
        Packet32WithStatus5 restored = raw;

        restored.Status.Category.Should().Be(4);
        restored.Status.Urgent.Should().BeTrue();
        restored.Status.Acknowledged.Should().BeTrue();
        restored.DataField.Should().Be(0x7FF);
        restored.Checksum.Should().Be(0xBEEF);
    }

    [Fact]
    public void Packet32WithStatus5_UndefinedBitsMasked()
    {
        // All bits set — undefined bits should be masked off
        Packet32WithStatus5 pkt = 0xFFFFFFFF;
        uint raw = pkt;

        // All 32 bits are defined (5 + 11 + 16 = 32), so no masking needed
        pkt.Status.Category.Should().Be(7);
        pkt.Status.Urgent.Should().BeTrue();
        pkt.Status.Acknowledged.Should().BeTrue();
        pkt.DataField.Should().Be(0x7FF);
        pkt.Checksum.Should().Be(0xFFFF);
    }

    #endregion

    #region [BitFields(N)] Embedded in Record Struct View

    [Fact]
    public void ViewWithNarrowStructs_StatusCode5_RoundTrip()
    {
        var data = new byte[8];
        var view = new ViewWithNarrowStructs(data);

        var status = new StatusCode5();
        status.Category = 6;
        status.Urgent = true;
        view.Status = status;

        view.Status.Category.Should().Be(6);
        view.Status.Urgent.Should().BeTrue();
        view.Status.Acknowledged.Should().BeFalse();
    }

    [Fact]
    public void ViewWithNarrowStructs_SensorReading12_RoundTrip()
    {
        var data = new byte[8];
        var view = new ViewWithNarrowStructs(data);

        var sensor = new SensorReading12();
        sensor.AdcValue = 768;
        sensor.Channel = 2;
        view.Sensor = sensor;

        view.Sensor.AdcValue.Should().Be(768);
        view.Sensor.Channel.Should().Be(2);
    }

    [Fact]
    public void ViewWithNarrowStructs_RgbColor24_RoundTrip()
    {
        var data = new byte[8];
        var view = new ViewWithNarrowStructs(data);

        var color = new RgbColor24();
        color.Red = 0x12;
        color.Green = 0x34;
        color.Blue = 0x56;
        view.Color = color;

        view.Color.Red.Should().Be(0x12);
        view.Color.Green.Should().Be(0x34);
        view.Color.Blue.Should().Be(0x56);
    }

    [Fact]
    public void ViewWithNarrowStructs_AllFields_Independent()
    {
        var data = new byte[8];
        var view = new ViewWithNarrowStructs(data);

        var status = new StatusCode5();
        status.Category = 3;
        status.Urgent = true;
        view.Status = status;

        var sensor = new SensorReading12();
        sensor.AdcValue = 500;
        sensor.Channel = 1;
        view.Sensor = sensor;

        var color = new RgbColor24();
        color.Red = 0xAA;
        color.Green = 0xBB;
        color.Blue = 0xCC;
        view.Color = color;

        view.Tag = 0x42;

        // All fields should be readable independently
        view.Status.Category.Should().Be(3);
        view.Status.Urgent.Should().BeTrue();
        view.Sensor.AdcValue.Should().Be(500);
        view.Sensor.Channel.Should().Be(1);
        view.Color.Red.Should().Be(0xAA);
        view.Color.Green.Should().Be(0xBB);
        view.Color.Blue.Should().Be(0xCC);
        view.Tag.Should().Be(0x42);

        // Modify one field, others should be unaffected
        view.Tag = 0x99;
        view.Status.Category.Should().Be(3, "status unchanged after modifying tag");
        view.Sensor.AdcValue.Should().Be(500, "sensor unchanged after modifying tag");
        view.Color.Red.Should().Be(0xAA, "color unchanged after modifying tag");
    }

    #endregion
}

#region Multi-Word Composition Test Structs

/// <summary>
/// A 128-bit struct backed by typeof(UInt128).
/// Tests multi-word (2-ulong) composition as an embedded property.
/// </summary>
[BitFields(typeof(UInt128))]
public partial struct GuidBits128
{
    [BitField(0, End = 63)]   public partial ulong Low { get; set; }
    [BitField(64, End = 127)] public partial ulong High { get; set; }
}

/// <summary>
/// A 128-bit struct backed by typeof(decimal).
/// Tests decimal-backed multi-word composition.
/// </summary>
[BitFields(typeof(decimal))]
public partial struct DecimalPayload128
{
    [BitField(0, End = 95)]    public partial ulong Coefficient { get; set; }
    [BitField(112, End = 118)] public partial byte Scale { get; set; }
    [BitFlag(127)]             public partial bool Sign { get; set; }
}

/// <summary>
/// A 256-bit struct using [BitFields(N)] multi-word.
/// Tests arbitrary-width multi-word composition.
/// </summary>
[BitFields(256)]
public partial struct WidePayload256
{
    [BitField(0, End = 63)]    public partial ulong Word0 { get; set; }
    [BitField(64, End = 127)]  public partial ulong Word1 { get; set; }
    [BitField(128, End = 191)] public partial ulong Word2 { get; set; }
    [BitField(192, End = 255)] public partial ulong Word3 { get; set; }
}

/// <summary>
/// A 512-bit multi-word parent that embeds two 128-bit structs and a 256-bit struct.
/// Tests multi-word in multi-word composition.
/// </summary>
[BitFields(512)]
public partial struct TelemetryFrame512
{
    [BitField(0, End = 127)]   public partial GuidBits128 Id { get; set; }
    [BitField(128, End = 383)] public partial WidePayload256 Payload { get; set; }
    [BitField(384, End = 511)] public partial DecimalPayload128 Footer { get; set; }
}

/// <summary>
/// A record struct view that embeds a 128-bit UInt128-backed struct.
/// Tests multi-word in view composition.
/// </summary>
[BitFields]
public partial record struct FrameViewWith128
{
    [BitField(0, End = 31)]    public partial uint Header { get; set; }
    [BitField(32, End = 159)]  public partial GuidBits128 Id { get; set; }
    [BitField(160, End = 191)] public partial uint Checksum { get; set; }
}

/// <summary>
/// A record struct view that embeds a 256-bit multi-word struct.
/// Tests large multi-word in view composition.
/// </summary>
[BitFields]
public partial record struct FrameViewWith256
{
    [BitField(0, End = 15)]    public partial ushort Tag { get; set; }
    [BitField(16, End = 271)]  public partial WidePayload256 Payload { get; set; }
    [BitField(272, End = 287)] public partial ushort Crc { get; set; }
}

/// <summary>
/// A 512-bit multi-word parent with a 128-bit embedded struct at a non-byte-aligned
/// bit position. Tests that multi-word embedding does NOT require byte alignment.
/// </summary>
[BitFields(512)]
public partial struct UnalignedFrame512
{
    [BitField(0, End = 3)]     public partial byte Tag { get; set; }         // 4 bits
    [BitField(4, End = 131)]   public partial GuidBits128 Id { get; set; }   // 128 bits at bit 4 (not byte-aligned)
    [BitField(132, End = 139)] public partial byte Footer { get; set; }      // 8 bits
}

/// <summary>
/// A record struct view with a 128-bit embedded struct at a non-byte-aligned
/// bit position. Tests that view multi-word embedding does NOT require byte alignment.
/// </summary>
[BitFields]
public partial record struct UnalignedViewWith128
{
    [BitField(0, End = 3)]     public partial byte Tag { get; set; }         // 4 bits
    [BitField(4, End = 131)]   public partial GuidBits128 Id { get; set; }   // 128 bits at bit 4
    [BitField(132, End = 139)] public partial byte Footer { get; set; }      // 8 bits
}

#endregion

/// <summary>
/// Tests for multi-word (128-bit, 256-bit, etc.) [BitFields] structs used as
/// embedded property types in multi-word parents and record struct views.
/// </summary>
public class BitFieldsMultiWordCompositionTests
{
    #region Multi-Word Standalone Tests

    [Fact]
    public void GuidBits128_RoundTrip()
    {
        GuidBits128 g = default;
        g.Low = 0xDEADBEEFCAFEBABE;
        g.High = 0x0123456789ABCDEF;
        g.Low.Should().Be(0xDEADBEEFCAFEBABE);
        g.High.Should().Be(0x0123456789ABCDEF);
    }

    [Fact]
    public void WidePayload256_RoundTrip()
    {
        WidePayload256 w = default;
        w.Word0 = 0x1111111111111111;
        w.Word1 = 0x2222222222222222;
        w.Word2 = 0x3333333333333333;
        w.Word3 = 0x4444444444444444;
        w.Word0.Should().Be(0x1111111111111111);
        w.Word1.Should().Be(0x2222222222222222);
        w.Word2.Should().Be(0x3333333333333333);
        w.Word3.Should().Be(0x4444444444444444);
    }

    #endregion

    #region Multi-Word in Multi-Word Tests

    [Fact]
    public void TelemetryFrame512_EmbeddedGuid_RoundTrip()
    {
        TelemetryFrame512 frame = default;

        var id = new GuidBits128 { Low = 0xAABBCCDDEEFF0011, High = 0x2233445566778899 };
        frame.Id = id;

        frame.Id.Low.Should().Be(0xAABBCCDDEEFF0011);
        frame.Id.High.Should().Be(0x2233445566778899);
    }

    [Fact]
    public void TelemetryFrame512_EmbeddedDecimalPayload_RoundTrip()
    {
        TelemetryFrame512 frame = default;

        var footer = new DecimalPayload128 { Coefficient = 12345678, Scale = 2, Sign = true };
        frame.Footer = footer;

        frame.Footer.Coefficient.Should().Be(12345678);
        frame.Footer.Scale.Should().Be(2);
        frame.Footer.Sign.Should().BeTrue();
    }

    [Fact]
    public void TelemetryFrame512_AllEmbedded_Independent()
    {
        TelemetryFrame512 frame = default;

        // Set all embedded fields
        var id = new GuidBits128 { Low = 0x1111111111111111, High = 0x2222222222222222 };
        var footer = new DecimalPayload128 { Coefficient = 99, Scale = 5, Sign = false };
        frame.Id = id;
        frame.Footer = footer;

        // Verify they're independent
        frame.Id.Low.Should().Be(0x1111111111111111);
        frame.Id.High.Should().Be(0x2222222222222222);
        frame.Footer.Coefficient.Should().Be(99);
        frame.Footer.Scale.Should().Be(5);
        frame.Footer.Sign.Should().BeFalse();

        // Modify one, verify others unchanged
        frame.Id = new GuidBits128 { Low = 0, High = 0 };
        frame.Footer.Coefficient.Should().Be(99, "footer unchanged after modifying id");
        frame.Footer.Scale.Should().Be(5, "footer unchanged after modifying id");
    }

    #endregion

    #region Multi-Word in View Tests

    [Fact]
    public void FrameViewWith128_Embedded_RoundTrip()
    {
        byte[] buffer = new byte[FrameViewWith128.SIZE_IN_BYTES];
        var view = new FrameViewWith128(buffer);

        view.Header = 0xDEAD;
        var id = new GuidBits128 { Low = 0xCAFEBABE12345678, High = 0xFEDCBA9876543210 };
        view.Id = id;
        view.Checksum = 0xBEEF;

        view.Header.Should().Be(0xDEAD);
        view.Id.Low.Should().Be(0xCAFEBABE12345678);
        view.Id.High.Should().Be(0xFEDCBA9876543210);
        view.Checksum.Should().Be(0xBEEF);
    }

    [Fact]
    public void FrameViewWith128_ModifyOne_OthersUnchanged()
    {
        byte[] buffer = new byte[FrameViewWith128.SIZE_IN_BYTES];
        var view = new FrameViewWith128(buffer);

        view.Header = 0x1234;
        view.Id = new GuidBits128 { Low = 0xAAAA, High = 0xBBBB };
        view.Checksum = 0x5678;

        // Modify embedded, verify others unchanged
        view.Id = new GuidBits128 { Low = 0, High = 0 };
        view.Header.Should().Be(0x1234, "header unchanged after modifying embedded");
        view.Checksum.Should().Be(0x5678, "checksum unchanged after modifying embedded");
    }

    [Fact]
    public void FrameViewWith256_Embedded_RoundTrip()
    {
        byte[] buffer = new byte[FrameViewWith256.SIZE_IN_BYTES];
        var view = new FrameViewWith256(buffer);

        view.Tag = 0x1234;
        var data = new WidePayload256
        {
            Word0 = 0x1111111111111111,
            Word1 = 0x2222222222222222,
            Word2 = 0x3333333333333333,
            Word3 = 0x4444444444444444
        };
        view.Payload = data;
        view.Crc = 0xABCD;

        view.Tag.Should().Be(0x1234);
        view.Payload.Word0.Should().Be(0x1111111111111111);
        view.Payload.Word1.Should().Be(0x2222222222222222);
        view.Payload.Word2.Should().Be(0x3333333333333333);
        view.Payload.Word3.Should().Be(0x4444444444444444);
        view.Crc.Should().Be(0xABCD);
    }

    [Fact]
    public void FrameViewWith256_ModifyEmbedded_OthersUnchanged()
    {
        byte[] buffer = new byte[FrameViewWith256.SIZE_IN_BYTES];
        var view = new FrameViewWith256(buffer);

        view.Tag = 0x9999;
        view.Payload = new WidePayload256 { Word0 = 1, Word1 = 2, Word2 = 3, Word3 = 4 };
        view.Crc = 0x7777;

        // Modify embedded, verify others unchanged
        view.Payload = new WidePayload256 { Word0 = 0xFF, Word1 = 0xFF, Word2 = 0xFF, Word3 = 0xFF };
        view.Tag.Should().Be(0x9999, "tag unchanged after modifying embedded data");
        view.Crc.Should().Be(0x7777, "crc unchanged after modifying embedded data");
    }

    #endregion

    #region Non-Byte-Aligned Multi-Word Tests

    [Fact]
    public void UnalignedFrame512_EmbeddedAt4Bits_RoundTrip()
    {
        UnalignedFrame512 frame = default;

        frame.Tag = 0x0A;  // 4 bits
        var id = new GuidBits128 { Low = 0xDEADBEEFCAFEBABE, High = 0x0123456789ABCDEF };
        frame.Id = id;
        frame.Footer = 0x55;

        frame.Tag.Should().Be(0x0A);
        frame.Id.Low.Should().Be(0xDEADBEEFCAFEBABE);
        frame.Id.High.Should().Be(0x0123456789ABCDEF);
        frame.Footer.Should().Be(0x55);
    }

    [Fact]
    public void UnalignedFrame512_ModifyEmbedded_OthersUnchanged()
    {
        UnalignedFrame512 frame = default;

        frame.Tag = 0x0F;
        frame.Id = new GuidBits128 { Low = 0xAAAA, High = 0xBBBB };
        frame.Footer = 0xCC;

        // Modify embedded, verify others unchanged
        frame.Id = new GuidBits128 { Low = 0, High = 0 };
        frame.Tag.Should().Be(0x0F, "tag unchanged after clearing embedded");
        frame.Footer.Should().Be(0xCC, "footer unchanged after clearing embedded");
    }

    [Fact]
    public void UnalignedFrame512_AllBitPatterns()
    {
        UnalignedFrame512 frame = default;

        // Fill with all-ones pattern
        var id = new GuidBits128 { Low = ulong.MaxValue, High = ulong.MaxValue };
        frame.Tag = 0x0F;  // 4 bits all set
        frame.Id = id;
        frame.Footer = 0xFF;

        frame.Tag.Should().Be(0x0F);
        frame.Id.Low.Should().Be(ulong.MaxValue);
        frame.Id.High.Should().Be(ulong.MaxValue);
        frame.Footer.Should().Be(0xFF);

        // Now clear embedded, verify neighbors kept their bits
        frame.Id = new GuidBits128 { Low = 0, High = 0 };
        frame.Tag.Should().Be(0x0F, "tag preserved when embedded cleared");
        frame.Id.Low.Should().Be(0UL);
        frame.Id.High.Should().Be(0UL);
        frame.Footer.Should().Be(0xFF, "footer preserved when embedded cleared");
    }

    [Fact]
    public void UnalignedViewWith128_EmbeddedAt4Bits_RoundTrip()
    {
        byte[] buffer = new byte[UnalignedViewWith128.SIZE_IN_BYTES];
        var view = new UnalignedViewWith128(buffer);

        view.Tag = 0x0A;
        var id = new GuidBits128 { Low = 0xDEADBEEFCAFEBABE, High = 0x0123456789ABCDEF };
        view.Id = id;
        view.Footer = 0x55;

        view.Tag.Should().Be(0x0A);
        view.Id.Low.Should().Be(0xDEADBEEFCAFEBABE);
        view.Id.High.Should().Be(0x0123456789ABCDEF);
        view.Footer.Should().Be(0x55);
    }

    [Fact]
    public void UnalignedViewWith128_ModifyEmbedded_OthersUnchanged()
    {
        byte[] buffer = new byte[UnalignedViewWith128.SIZE_IN_BYTES];
        var view = new UnalignedViewWith128(buffer);

        view.Tag = 0x0F;
        view.Id = new GuidBits128 { Low = 0x1111, High = 0x2222 };
        view.Footer = 0xCC;

        // Modify embedded, verify others unchanged
        view.Id = new GuidBits128 { Low = 0, High = 0 };
        view.Tag.Should().Be(0x0F, "tag unchanged after clearing embedded");
        view.Footer.Should().Be(0xCC, "footer unchanged after clearing embedded");
    }

    #endregion
}
