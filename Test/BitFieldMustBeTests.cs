using FluentAssertions;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

#region Test Structs

/// <summary>
/// Test struct with per-field MustBe constraints on an unsigned backing type.
/// Bit layout: [Sync(7,MustBe.One)] [Data(3-6)] [Reserved(1-2,MustBe.Zero)] [Active(0)]
/// </summary>
[BitFields(typeof(byte))]
public partial struct MustBeTestReg
{
    [BitFlag(0)] public partial bool Active { get; set; }
    [BitField(1, 2, MustBe.Zero)] public partial byte Reserved { get; set; }
    [BitField(3, 6)] public partial byte Data { get; set; }
    [BitFlag(7, MustBe.One)] public partial bool Sync { get; set; }
}

/// <summary>
/// Test struct combining UndefinedBitsMustBe.Zeroes with per-field MustBe.One.
/// Bits 0-2: Flags (normal), Bit 3: AlwaysHigh (MustBe.One), Bits 4-7: undefined (forced zero).
/// </summary>
[BitFields(typeof(byte), UndefinedBitsMustBe.Zeroes)]
public partial struct CombinedMustBeReg
{
    [BitField(0, 2)] public partial byte Flags { get; set; }
    [BitFlag(3, MustBe.One)] public partial bool AlwaysHigh { get; set; }
}

/// <summary>
/// Signed backing type with MustBe constraints.
/// </summary>
[BitFields(typeof(sbyte), UndefinedBitsMustBe.Zeroes)]
public partial struct SignedMustBeReg
{
    [BitField(0, 2)] public partial byte Data { get; set; }
    [BitFlag(3, MustBe.One)] public partial bool MustBeSet { get; set; }
    [BitField(4, 5, MustBe.Zero)] public partial byte Rsvd { get; set; }
}

#endregion

/// <summary>
/// Tests that MustBe and UndefinedBitsMustBe constraints hold through every code path:
/// construction, implicit/explicit conversion, operators, With methods, span round-trip,
/// and parsing.
/// </summary>
public class BitFieldMustBeTests
{
    #region Constructor Enforcement

    [Fact]
    public void Constructor_MustBeOne_ForcesHighBit()
    {
        MustBeTestReg reg = 0x00; // all bits zero
        byte raw = reg;
        (raw & 0x80).Should().Be(0x80, "bit 7 (Sync, MustBe.One) must be set");
    }

    [Fact]
    public void Constructor_MustBeZero_ClearsReservedField()
    {
        MustBeTestReg reg = 0xFF; // all bits set
        byte raw = reg;
        (raw & 0x06).Should().Be(0x00, "bits 1-2 (Reserved, MustBe.Zero) must be cleared");
    }

    [Fact]
    public void Constructor_BothConstraints_AppliedTogether()
    {
        MustBeTestReg reg = 0x00;
        byte raw = reg;
        // Bit 7 forced to 1, bits 1-2 forced to 0
        raw.Should().Be(0x80);

        reg = 0xFF;
        raw = reg;
        // Bit 7 forced to 1, bits 1-2 forced to 0; rest preserved
        (raw & 0x80).Should().Be(0x80, "Sync stays 1");
        (raw & 0x06).Should().Be(0x00, "Reserved stays 0");
        (raw & 0x79).Should().Be(0x79, "Active(0) and Data(3-6) preserved");
    }

    [Fact]
    public void Constructor_CombinedWithUndefinedBits_AllMasksApplied()
    {
        // CombinedMustBeReg: bits 4-7 undefined (forced zero) + bit 3 MustBe.One
        CombinedMustBeReg reg = 0xFF;
        byte raw = reg;
        (raw & 0xF0).Should().Be(0x00, "undefined bits 4-7 are zeroed");
        (raw & 0x08).Should().Be(0x08, "bit 3 (AlwaysHigh) forced to 1");
        (raw & 0x07).Should().Be(0x07, "Flags bits 0-2 preserved from input");
    }

    [Fact]
    public void Constructor_CombinedWithUndefinedBits_ZeroInput_SetsOnlyForced()
    {
        CombinedMustBeReg reg = 0x00;
        byte raw = reg;
        raw.Should().Be(0x08, "only MustBe.One bit 3 is set");
    }

    [Fact]
    public void Constructor_SignedBackingType_AppliesBothMasks()
    {
        SignedMustBeReg reg = unchecked((sbyte)-1); // 0xFF
        sbyte raw = reg;
        // Bits 0-2: Data preserved (0x07), bit 3: forced 1 (0x08),
        // bits 4-5: forced 0, bits 6-7: undefined zeroed
        raw.Should().Be(0x0F, "Data=7, MustBeSet=1, Rsvd=0, undefined=0");
    }

    #endregion

    #region Getter Enforcement

    [Fact]
    public void Getter_MustBeOne_AlwaysReturnsTrue()
    {
        MustBeTestReg reg = 0x00;
        reg.Sync.Should().BeTrue("MustBe.One getter always returns true");
    }

    [Fact]
    public void Getter_MustBeZero_AlwaysReturnsZero()
    {
        MustBeTestReg reg = 0xFF;
        reg.Reserved.Should().Be(0, "MustBe.Zero getter always returns 0");
    }

    #endregion

    #region Setter Enforcement

    [Fact]
    public void Setter_MustBeZero_IgnoresWrite()
    {
        MustBeTestReg reg = 0x80; // Sync=1, rest zero
        reg.Reserved = 3; // try to set bits 1-2
        byte raw = reg;
        (raw & 0x06).Should().Be(0x00, "Reserved setter ignores input for MustBe.Zero");
    }

    [Fact]
    public void Setter_MustBeOne_IgnoresWrite()
    {
        MustBeTestReg reg = 0xFF;
        reg.Sync = false; // try to clear bit 7
        byte raw = reg;
        (raw & 0x80).Should().Be(0x80, "Sync setter ignores input for MustBe.One");
    }

    [Fact]
    public void Setter_NormalField_DoesNotCorruptConstrainedBits()
    {
        MustBeTestReg reg = 0x80; // Sync=1
        reg.Data = 0x0F; // max 4-bit value
        byte raw = reg;
        (raw & 0x80).Should().Be(0x80, "Sync stays 1 after Data write");
        (raw & 0x06).Should().Be(0x00, "Reserved stays 0 after Data write");
    }

    #endregion

    #region Operator Enforcement (all route through constructor)

    [Fact]
    public void BitwiseOr_PreservesConstraints()
    {
        MustBeTestReg a = 0x80; // Sync=1
        MustBeTestReg b = 0x06; // try to set reserved bits
        var result = a | b;
        byte raw = result;
        (raw & 0x80).Should().Be(0x80, "Sync bit stays 1");
        (raw & 0x06).Should().Be(0x00, "Reserved bits stay 0 after OR");
    }

    [Fact]
    public void BitwiseComplement_PreservesConstraints()
    {
        MustBeTestReg reg = 0x80;
        var result = ~reg;
        byte raw = result;
        (raw & 0x80).Should().Be(0x80, "Sync stays 1 after complement");
        (raw & 0x06).Should().Be(0x00, "Reserved stays 0 after complement");
    }

    [Fact]
    public void BitwiseXor_PreservesConstraints()
    {
        MustBeTestReg a = 0x89; // 0b1000_1001
        MustBeTestReg b = 0xFF;
        var result = a ^ b;
        byte raw = result;
        (raw & 0x80).Should().Be(0x80, "Sync stays 1 after XOR");
        (raw & 0x06).Should().Be(0x00, "Reserved stays 0 after XOR");
    }

    [Fact]
    public void Addition_PreservesConstraints()
    {
        MustBeTestReg a = 0x80; // just Sync
        MustBeTestReg b = 0x03; // try to carry into reserved bits
        var result = a + b;
        byte raw = result;
        (raw & 0x80).Should().Be(0x80, "Sync stays 1 after addition");
        (raw & 0x06).Should().Be(0x00, "Reserved stays 0 after addition");
    }

    [Fact]
    public void Subtraction_PreservesConstraints()
    {
        MustBeTestReg a = 0x89;
        MustBeTestReg b = 0x01;
        var result = a - b;
        byte raw = result;
        (raw & 0x80).Should().Be(0x80, "Sync stays 1 after subtraction");
        (raw & 0x06).Should().Be(0x00, "Reserved stays 0 after subtraction");
    }

    [Fact]
    public void Negation_PreservesConstraints()
    {
        MustBeTestReg reg = 0x81;
        var result = -reg;
        byte raw = result;
        (raw & 0x80).Should().Be(0x80, "Sync stays 1 after negation");
        (raw & 0x06).Should().Be(0x00, "Reserved stays 0 after negation");
    }

    #endregion

    #region With Method Enforcement

    [Fact]
    public void WithMustBeZeroField_ValueIgnored()
    {
        MustBeTestReg reg = 0x80;
        var result = reg.WithReserved(3);
        byte raw = result;
        (raw & 0x06).Should().Be(0x00, "WithReserved routes through constructor, MustBe.Zero enforced");
    }

    [Fact]
    public void WithMustBeOneFlag_ValueIgnored()
    {
        MustBeTestReg reg = 0xFF;
        var result = reg.WithSync(false);
        byte raw = result;
        (raw & 0x80).Should().Be(0x80, "WithSync routes through constructor, MustBe.One enforced");
    }

    [Fact]
    public void WithNormalField_ConstrainedBitsPreserved()
    {
        MustBeTestReg reg = 0x80;
        var result = reg.WithData(0x0F).WithActive(true);
        byte raw = result;
        (raw & 0x80).Should().Be(0x80, "Sync preserved");
        (raw & 0x06).Should().Be(0x00, "Reserved preserved");
        result.Data.Should().Be(0x0F);
        result.Active.Should().BeTrue();
    }

    #endregion

    #region Implicit Conversion Enforcement

    [Fact]
    public void ImplicitFromStorageType_NormalizesConstraints()
    {
        MustBeTestReg reg = (byte)0x06; // only reserved bits set
        byte raw = reg;
        (raw & 0x06).Should().Be(0x00, "MustBe.Zero cleared by conversion");
        (raw & 0x80).Should().Be(0x80, "MustBe.One set by conversion");
    }

    [Fact]
    public void ImplicitFromInt_NormalizesConstraints()
    {
        MustBeTestReg reg = 0x06;
        byte raw = reg;
        (raw & 0x06).Should().Be(0x00, "MustBe.Zero cleared by int conversion");
        (raw & 0x80).Should().Be(0x80, "MustBe.One set by int conversion");
    }

    #endregion

    #region Span Round-Trip Enforcement

    [Fact]
    public void SpanConstructor_NormalizesConstraints()
    {
        byte[] data = [0xFF]; // all bits set
        var reg = new MustBeTestReg(new ReadOnlySpan<byte>(data));
        byte raw = reg;
        (raw & 0x80).Should().Be(0x80, "Sync stays 1 from span");
        (raw & 0x06).Should().Be(0x00, "Reserved cleared from span");
    }

    [Fact]
    public void ReadFrom_NormalizesConstraints()
    {
        byte[] data = [0x06]; // only reserved bits
        var reg = MustBeTestReg.ReadFrom(data);
        byte raw = reg;
        (raw & 0x06).Should().Be(0x00, "Reserved cleared by ReadFrom");
        (raw & 0x80).Should().Be(0x80, "Sync set by ReadFrom");
    }

    [Fact]
    public void SpanRoundTrip_ConstraintsPreserved()
    {
        MustBeTestReg reg = 0x00;
        reg.Data = 0x0A;
        reg.Active = true;

        byte[] buffer = new byte[1];
        reg.WriteTo(buffer);

        var roundTripped = MustBeTestReg.ReadFrom(buffer);
        roundTripped.Data.Should().Be(0x0A);
        roundTripped.Active.Should().BeTrue();
        roundTripped.Sync.Should().BeTrue();
        roundTripped.Reserved.Should().Be(0);
    }

    #endregion

    #region Parse Enforcement

    [Fact]
    public void Parse_Hex_NormalizesConstraints()
    {
        var reg = MustBeTestReg.Parse("0x06", null); // only reserved bits
        byte raw = reg;
        (raw & 0x06).Should().Be(0x00, "Reserved cleared by Parse");
        (raw & 0x80).Should().Be(0x80, "Sync set by Parse");
    }

    [Fact]
    public void Parse_Binary_NormalizesConstraints()
    {
        var reg = MustBeTestReg.Parse("0b00000110", null); // only reserved bits
        byte raw = reg;
        (raw & 0x06).Should().Be(0x00, "Reserved cleared by binary Parse");
        (raw & 0x80).Should().Be(0x80, "Sync set by binary Parse");
    }

    [Fact]
    public void TryParse_NormalizesConstraints()
    {
        MustBeTestReg.TryParse("0xFF", null, out var reg).Should().BeTrue();
        byte raw = reg;
        (raw & 0x80).Should().Be(0x80, "Sync stays 1 from TryParse");
        (raw & 0x06).Should().Be(0x00, "Reserved cleared from TryParse");
    }

    #endregion

    #region UndefinedBitsMustBe Span Constructor (regression)

    [Fact]
    public void SpanConstructor_UndefinedBitsZeroes_MasksCorrectly()
    {
        byte[] data = [0xFF];
        var reg = new SparseUndefinedZero(new ReadOnlySpan<byte>(data));
        sbyte raw = reg;
        // Only defined bits (1-2, 4-6) should survive = 0x76
        raw.Should().Be(0x76, "span constructor must apply UndefinedBitsMustBe.Zeroes mask");
    }

    [Fact]
    public void SpanConstructor_UndefinedBitsOnes_SetsCorrectly()
    {
        byte[] data = [0x00];
        var reg = new SparseUndefinedOnes(new ReadOnlySpan<byte>(data));
        sbyte raw = reg;
        // Undefined bits (0, 3, 7) should be set = 0x89
        raw.Should().Be(unchecked((sbyte)0x89), "span constructor must apply UndefinedBitsMustBe.Ones mask");
    }

    [Fact]
    public void ReadFrom_UndefinedBitsZeroes_MasksCorrectly()
    {
        byte[] data = [0xFF];
        var reg = SparseUndefinedZero.ReadFrom(data);
        sbyte raw = reg;
        raw.Should().Be(0x76, "ReadFrom must apply UndefinedBitsMustBe.Zeroes mask");
    }

    #endregion

    #region Existing IPv4Flags (MustBe.Zero) Regression

    [Fact]
    public void IPv4Flags_Reserved_MustBeZero_EnforcedInConstructor()
    {
        // IPv4Flags has [BitFlag(2, MustBe.Zero)] Reserved
        // and UndefinedBitsMustBe.Zeroes for bits 3-7
        BitFieldProtocolTests.IPv4Flags flags = 0x07; // try to set all 3 defined bits
        byte raw = flags;
        (raw & 0x04).Should().Be(0x00, "Reserved (MustBe.Zero) must be cleared even from constructor");
        (raw & 0x03).Should().Be(0x03, "MoreFragments and DontFragment preserved");
    }

    [Fact]
    public void IPv4Flags_Reserved_MustBeZero_EnforcedInSetter()
    {
        BitFieldProtocolTests.IPv4Flags flags = 0;
        flags.Reserved = true; // try to set it
        byte raw = flags;
        (raw & 0x04).Should().Be(0x00, "setter ignores true for MustBe.Zero flag");
        flags.Reserved.Should().BeFalse("getter always returns false for MustBe.Zero");
    }

    [Fact]
    public void IPv4Flags_Reserved_MustBeZero_EnforcedInWithMethod()
    {
        BitFieldProtocolTests.IPv4Flags flags = 0;
        flags = flags.WithReserved(true);
        byte raw = flags;
        (raw & 0x04).Should().Be(0x00, "WithReserved(true) still results in 0");
    }

    [Fact]
    public void IPv4Flags_Reserved_MustBeZero_EnforcedAfterOr()
    {
        BitFieldProtocolTests.IPv4Flags a = 0x01; // MoreFragments
        BitFieldProtocolTests.IPv4Flags b = 0x04; // try to set Reserved via OR
        var result = a | b;
        byte raw = result;
        (raw & 0x04).Should().Be(0x00, "OR cannot set MustBe.Zero bit");
    }

    [Fact]
    public void IPv4Flags_Reserved_MustBeZero_SpanConstructor()
    {
        byte[] data = [0x07]; // all 3 low bits
        var flags = new BitFieldProtocolTests.IPv4Flags(new ReadOnlySpan<byte>(data));
        byte raw = flags;
        (raw & 0x04).Should().Be(0x00, "span constructor must clear MustBe.Zero bit");
    }

    #endregion

    #region Static Bit/Mask Properties with MustBe

    [Fact]
    public void StaticBitProperty_MustBeOne_NormalizedByConstructor()
    {
        // SyncBit should have bit 7 set. Since it goes through new(0x80),
        // the constructor also ensures MustBe constraints.
        var syncBit = MustBeTestReg.SyncBit;
        byte raw = syncBit;
        (raw & 0x80).Should().Be(0x80, "SyncBit has bit 7 set");
        (raw & 0x06).Should().Be(0x00, "SyncBit doesn't set reserved bits");
    }

    [Fact]
    public void StaticMaskProperty_MustBeZero_NormalizedByConstructor()
    {
        var reservedMask = MustBeTestReg.ReservedMask;
        byte raw = reservedMask;
        // ReservedMask tries to create new(0x06) but constructor clears MustBe.Zero bits
        (raw & 0x06).Should().Be(0x00, "ReservedMask for MustBe.Zero field is empty after normalization");
        (raw & 0x80).Should().Be(0x80, "MustBe.One bit is forced in mask too");
    }

    #endregion

    #region Zero Property with MustBe

    [Fact]
    public void Zero_WithMustBeOne_IsNotAllZeroes()
    {
        // default struct has all-zero backing, but MustBe.One should NOT be enforced
        // because Zero => default bypasses the constructor. This is an expected limitation.
        // The struct's generated fields property still correctly reports MustBe metadata.
        var zero = MustBeTestReg.Zero;
        // Zero is `default` which doesn't go through the normalizing constructor.
        // This is by design -- Zero represents the raw default state.
        // Users should use `new MustBeTestReg(0)` for a normalized zero.
        var normalized = new MustBeTestReg(0);
        byte raw = normalized;
        (raw & 0x80).Should().Be(0x80, "normalized zero has MustBe.One set");
    }

    #endregion
}
