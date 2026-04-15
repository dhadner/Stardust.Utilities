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
    [BitField(1, MustBe.Zero, End = 2)] public partial byte Reserved { get; set; }
    [BitField(3, End = 6)] public partial byte Data { get; set; }
    [BitFlag(7, MustBe.One)] public partial bool Sync { get; set; }
}

/// <summary>
/// Test struct combining UndefinedBitsMustBe.Zeroes with per-field MustBe.One.
/// Bits 0-2: Flags (normal), Bit 3: AlwaysHigh (MustBe.One), Bits 4-7: undefined (forced zero).
/// </summary>
[BitFields(typeof(byte), UndefinedBitsMustBe.Zeroes)]
public partial struct CombinedMustBeReg
{
    [BitField(0, End = 2)] public partial byte Flags { get; set; }
    [BitFlag(3, MustBe.One)] public partial bool AlwaysHigh { get; set; }
}

/// <summary>
/// Signed backing type with MustBe constraints.
/// </summary>
[BitFields(typeof(sbyte), UndefinedBitsMustBe.Zeroes)]
public partial struct SignedMustBeReg
{
    [BitField(0, End = 2)] public partial byte Data { get; set; }
    [BitFlag(3, MustBe.One)] public partial bool MustBeSet { get; set; }
    [BitField(4, MustBe.Zero, End = 5)] public partial byte Rsvd { get; set; }
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

    #region default(T) Normalization

    [Fact]
    public void Default_WithMustBeOne_ProducesNormalizedOutput()
    {
        // default(T) bypasses the constructor, leaving __value as zero.
        // However, the generated __normalizedValue property applies normalization
        // on every outbound read, so observable output is always correct.
        var zero = default(MustBeTestReg);
        var normalized = new MustBeTestReg(0);

        // Both should produce identical observable output
        byte zeroRaw = zero;
        byte normalizedRaw = normalized;
        zeroRaw.Should().Be(normalizedRaw, "default(T) and new T(0) should produce the same output");
        (zeroRaw & 0x80).Should().Be(0x80, "MustBe.One bit is set in default(T) output");
    }

    [Fact]
    public void Default_WithMustBeOne_EqualityMatchesConstructed()
    {
        var zero = default(MustBeTestReg);
        var constructed = new MustBeTestReg(0);

        zero.Should().Be(constructed, "default(T) should be observably equal to new T(0)");
        zero.GetHashCode().Should().Be(constructed.GetHashCode(), "hash codes should match");
    }

    [Fact]
    public void Default_WithMustBeOne_ToStringMatchesConstructed()
    {
        var zero = default(MustBeTestReg);
        var constructed = new MustBeTestReg(0);

        zero.ToString().Should().Be(constructed.ToString(), "ToString output should match");
    }

    [Fact]
    public void Default_WithMustBeOne_WriteToMatchesConstructed()
    {
        var zero = default(MustBeTestReg);
        var constructed = new MustBeTestReg(0);

        var zeroBytes = zero.ToByteArray();
        var constructedBytes = constructed.ToByteArray();

        zeroBytes.Should().BeEquivalentTo(constructedBytes, "serialized bytes should match");
    }

    #endregion

    #region Iterative Shift Normalization

    [Fact]
    public void LeftShift_Identity_ShiftByN_EqualsNSingleShifts()
    {
        // MustBeTestReg: bit 7 MustBe.One, bits 1-2 MustBe.Zero
        // AND_MASK = 0xF9, OR_MASK = 0x80
        // x << 2 must equal (x << 1) << 1
        MustBeTestReg x = 0x81; // bit 0 and bit 7 (Sync) set
        int shiftBy2 = x << 2;
        int shiftBy1Then1 = (MustBeTestReg)(byte)(x << 1) << 1;
        shiftBy2.Should().Be(shiftBy1Then1, "(x << 2) must equal ((x << 1) << 1)");
    }

    [Fact]
    public void LeftShift_BitAbsorbedByMustBeZero()
    {
        // Bit 0 set, shifting left by 1 moves it to bit 1 which is MustBe.Zero — bit is absorbed.
        MustBeTestReg x = 0x81; // Active=1, Sync=1
        int shifted = x << 1;
        // Bit 0 shifted to bit 1 (MustBe.Zero) → absorbed → only OR_MASK (0x80) survives
        ((byte)shifted & 0x06).Should().Be(0x00, "bit shifted into MustBe.Zero position is absorbed");
        ((byte)shifted & 0x80).Should().Be(0x80, "MustBe.One bit (Sync) is preserved");
    }

    [Fact]
    public void LeftShift_BitCannotTeleportPastMustBeZero()
    {
        // Without iterative normalization, 1 << 2 would skip past the MustBe.Zero bits 1-2
        // and land on bit 2 (0x04). With iterative normalization, the bit is absorbed at bit 1.
        MustBeTestReg x = 0x81; // bit 0 and bit 7
        int shifted = x << 2;
        // Bit 0 → bit 1 (MustBe.Zero, absorbed) → nothing left to shift
        ((byte)shifted & 0x04).Should().Be(0x00, "bit must not teleport past MustBe.Zero constraint");
    }

    [Fact]
    public void RightShift_NormalizesAfterEachStep()
    {
        // Bit 3 set (Data field LSB), shifting right through MustBe.Zero bits 1-2
        MustBeTestReg x = 0x88; // bit 3 and bit 7 (Sync)
        int shifted = x >> 2;
        // Bit 3 → bit 2 → bit 1 (MustBe.Zero, absorbed)
        ((byte)shifted & 0x06).Should().Be(0x00, "bit shifted into MustBe.Zero position is absorbed");
        ((byte)shifted & 0x80).Should().Be(0x80, "MustBe.One bit (Sync) preserved during right shift");
    }

    [Fact]
    public void RightShift_Identity_ShiftByN_EqualsNSingleShifts()
    {
        MustBeTestReg x = 0xF9; // all bits except MustBe.Zero (bits 1-2 cleared by constructor)
        int shiftBy2 = x >> 2;
        int shiftBy1Then1 = (MustBeTestReg)(byte)(x >> 1) >> 1;
        shiftBy2.Should().Be(shiftBy1Then1, "(x >> 2) must equal ((x >> 1) >> 1)");
    }

    [Fact]
    public void UnsignedRightShift_Identity_ShiftByN_EqualsNSingleShifts()
    {
        MustBeTestReg x = 0xF9;
        int shiftBy2 = x >>> 2;
        int shiftBy1Then1 = (MustBeTestReg)(byte)(x >>> 1) >>> 1;
        shiftBy2.Should().Be(shiftBy1Then1, "(x >>> 2) must equal ((x >>> 1) >>> 1)");
    }

    [Fact]
    public void LeftShift_ByZero_ReturnsNormalizedValue()
    {
        MustBeTestReg x = 0x89; // Active=1, bit 3, Sync=1
        int shifted = x << 0;
        ((byte)shifted).Should().Be(0x89, "shift by 0 returns the normalized value unchanged");
    }

    [Fact]
    public void LeftShift_Default_ProducesNormalizedResult()
    {
        // default(T) should produce correct shift results via __normalizedValue
        var x = default(MustBeTestReg);
        int shifted = x << 1;
        // default(T).__normalizedValue = 0x80 (only MustBe.One set)
        // 0x80 << 1 = 0x00, normalize → 0x80
        ((byte)shifted & 0x80).Should().Be(0x80, "MustBe.One bit preserved in default(T) shift");
    }

    [Fact]
    public void LeftShift_SignedType_Identity()
    {
        // Verify iterative shift works correctly with signed backing type
        SignedMustBeReg x = unchecked((sbyte)0x09); // Data=1, MustBeSet=1
        // Shift left by 3: each step normalizes through constructor
        int shiftBy3 = x << 3;
        int step1 = (SignedMustBeReg)unchecked((sbyte)(byte)(x << 1)) << 2;
        shiftBy3.Should().Be(step1, "signed type iterative shift identity must hold");
    }

    [Fact]
    public void UnsignedRightShift_SignedType_NoSignExtension()
    {
        // SignedMustBeReg has sbyte storage, MustBeSet at bit 3 (OR_MASK includes 0x08)
        // Verify >>> doesn't produce sign-extension artifacts
        SignedMustBeReg x = unchecked((sbyte)0x0F); // bits 0-3 set (Data=7, MustBeSet=1)
        int shifted = x >>> 1;
        // After constructor normalization: value = (sbyte)(((byte)0x0F & AND_MASK) | OR_MASK) = 0x0F
        // >>> 1: (byte)0x0F >>> 1 = 0x07, normalize: (0x07 & AND) | OR = 0x08 | 0x07... 
        // The exact value depends on the masks, but sign bits should not leak
        ((byte)shifted & 0xC0).Should().Be(0x00, "unsigned right shift must not produce sign-extension artifacts");
    }

    #endregion
}
