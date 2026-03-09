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
    [BitField(4, 7)] public partial byte Priority { get; set; }  // 4-bit priority (0-15)
}

/// <summary>
/// A reusable 4-bit command code structure.
/// </summary>
[BitFields(typeof(byte))]
public partial struct CommandCode
{
    [BitField(0, 3)] public partial byte Code { get; set; }  // 4-bit command (0-15)
}

/// <summary>
/// A 16-bit protocol header that embeds StatusFlags as a property type.
/// This demonstrates composing BitFields structs.
/// </summary>
[BitFields(typeof(ushort))]
public partial struct ProtocolHeader16
{
    /// <summary>8-bit status flags at bits 0-7.</summary>
    [BitField(0, 7)] public partial StatusFlags Status { get; set; }
    
    /// <summary>8-bit payload length at bits 8-15.</summary>
    [BitField(8, 15)] public partial byte Length { get; set; }
}

/// <summary>
/// A 32-bit protocol header with multiple embedded BitFields types.
/// </summary>
[BitFields(typeof(uint))]
public partial struct ProtocolHeader32
{
    /// <summary>8-bit status flags at bits 0-7.</summary>
    [BitField(0, 7)] public partial StatusFlags Status { get; set; }
    
    /// <summary>4-bit command code at bits 8-11.</summary>
    [BitField(8, 11)] public partial CommandCode Command { get; set; }
    
    /// <summary>4-bit version at bits 12-15.</summary>
    [BitField(12, 15)] public partial byte Version { get; set; }
    
    /// <summary>16-bit sequence number at bits 16-31.</summary>
    [BitField(16, 31)] public partial ushort Sequence { get; set; }
}

/// <summary>
/// A file header structure demonstrating real-world composition.
/// </summary>
[BitFields(typeof(ulong))]
public partial struct FileHeader
{
    /// <summary>Magic number (16 bits).</summary>
    [BitField(0, 15)] public partial ushort Magic { get; set; }
    
    /// <summary>Embedded status flags (8 bits).</summary>
    [BitField(16, 23)] public partial StatusFlags Flags { get; set; }
    
    /// <summary>Version major (8 bits).</summary>
    [BitField(24, 31)] public partial byte VersionMajor { get; set; }
    
    /// <summary>Version minor (8 bits).</summary>
    [BitField(32, 39)] public partial byte VersionMinor { get; set; }
    
    /// <summary>Reserved (24 bits).</summary>
    [BitField(40, 63)] public partial uint Reserved { get; set; }
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
    [BitField(0, 3)] public partial byte TypeCode { get; set; }
    
    /// <summary>5-bit flags at bits 4-8.</summary>
    [BitField(4, 8)] public partial byte Flags { get; set; }
    
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
    [BitField(0, 8)] public partial SubHeader9 SubHeader { get; set; }
    
    /// <summary>10-bit payload size at bits 9-18.</summary>
    [BitField(9, 18)] public partial ushort PayloadSize { get; set; }
    
    /// <summary>8-bit sequence at bits 19-26.</summary>
    [BitField(19, 26)] public partial byte Sequence { get; set; }
    
    // Bits 27-31 are UNDEFINED - masked to zero
}

/// <summary>
/// A 64-bit main header that embeds the 27-bit Header27.
/// </summary>
[BitFields(typeof(ulong))]
public partial struct MainHeader64
{
    /// <summary>27-bit protocol header at bits 0-26.</summary>
    [BitField(0, 26)] public partial Header27 Protocol { get; set; }
    
    /// <summary>5-bit priority at bits 27-31.</summary>
    [BitField(27, 31)] public partial byte Priority { get; set; }
    
    /// <summary>32-bit timestamp at bits 32-63.</summary>
    [BitField(32, 63)] public partial uint Timestamp { get; set; }
}

/// <summary>
/// Signed version of 27-bit header.
/// Bits 27-31 are UNDEFINED and will be masked to zero.
/// </summary>
[BitFields(typeof(int), UndefinedBitsMustBe.Zeroes)]
public partial struct SignedHeader27
{
    /// <summary>9-bit sub-header at bits 0-8.</summary>
    [BitField(0, 8)] public partial SubHeader9 SubHeader { get; set; }
    
    /// <summary>10-bit payload size at bits 9-18.</summary>
    [BitField(9, 18)] public partial ushort PayloadSize { get; set; }
    
    /// <summary>8-bit sequence at bits 19-26.</summary>
    [BitField(19, 26)] public partial byte Sequence { get; set; }
    
    // Bits 27-31 are UNDEFINED - masked to zero
}

/// <summary>
/// A 9-bit sub-header with Any mode (default) for comparison testing.
/// </summary>
[BitFields(typeof(ushort))]  // Default is UndefinedBitsMustBe.Any
public partial struct SubHeader9Native
{
    /// <summary>4-bit type code at bits 0-3.</summary>
    [BitField(0, 3)] public partial byte TypeCode { get; set; }
    
    /// <summary>5-bit flags at bits 4-8.</summary>
    [BitField(4, 8)] public partial byte Flags { get; set; }
    
    // Bits 9-15 are UNDEFINED - preserved as raw data
}

/// <summary>
/// A 9-bit sub-header with Ones mode for testing.
/// </summary>
[BitFields(typeof(ushort), UndefinedBitsMustBe.Ones)]
public partial struct SubHeader9Ones
{
    /// <summary>4-bit type code at bits 0-3.</summary>
    [BitField(0, 3)] public partial byte TypeCode { get; set; }
    
    /// <summary>5-bit flags at bits 4-8.</summary>
    [BitField(4, 8)] public partial byte Flags { get; set; }
    
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
    [BitField(1, 2)] public partial byte LowField { get; set; }   // bits 1-2 (2 bits)
    // bit 3: UNDEFINED
    [BitField(4, 6)] public partial byte HighField { get; set; }  // bits 4-6 (3 bits)
    // bit 7: UNDEFINED
}

/// <summary>
/// Same sparse pattern with Ones mode.
/// </summary>
[BitFields(typeof(sbyte), UndefinedBitsMustBe.Ones)]
public partial struct SparseUndefinedOnes
{
    // bit 0: UNDEFINED
    [BitField(1, 2)] public partial byte LowField { get; set; }
    // bit 3: UNDEFINED
    [BitField(4, 6)] public partial byte HighField { get; set; }
    // bit 7: UNDEFINED
}

/// <summary>
/// Same sparse pattern with Any mode (default).
/// </summary>
[BitFields(typeof(sbyte))]  // UndefinedBitsMustBe.Any (default)
public partial struct SparseUndefinedNative
{
    // bit 0: UNDEFINED
    [BitField(1, 2)] public partial byte LowField { get; set; }
    // bit 3: UNDEFINED
    [BitField(4, 6)] public partial byte HighField { get; set; }
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
        // because MainHeader64.Protocol is defined as [BitField(0, 26)]
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



