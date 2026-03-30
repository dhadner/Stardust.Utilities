using FluentAssertions;
using Xunit;

namespace Stardust.Utilities.Tests;

#region Test Structs

/// <summary>
/// Unsigned byte-backed struct with a 3-bit saturating field (bits 0-2) and a 4-bit
/// non-saturating field (bits 3-6) for comparison.
/// </summary>
[BitFields(typeof(byte))]
public partial struct SatUnsignedByte
{
    [BitField(0, Width = 3, Saturating = true)] public partial byte Low { get; set; }
    [BitField(3, Width = 4)] public partial byte High { get; set; }
}

/// <summary>
/// Signed int-backed struct with a 5-bit signed saturating field.
/// </summary>
[BitFields(typeof(int))]
public partial struct SatSignedInt
{
    [BitField(0, Width = 5, Saturating = true)] public partial int Signed { get; set; }
    [BitField(5, Width = 4)] public partial int Other { get; set; }
}

/// <summary>
/// Unsigned ushort-backed struct with a 10-bit saturating field.
/// </summary>
[BitFields(typeof(ushort))]
public partial struct SatUnsignedUshort
{
    [BitField(0, Width = 10, Saturating = true)] public partial ushort Clamped { get; set; }
}

/// <summary>
/// Unsigned uint-backed struct with saturating field at non-zero shift.
/// </summary>
[BitFields(typeof(uint))]
public partial struct SatShiftedField
{
    [BitField(0, Width = 4)] public partial byte Prefix { get; set; }
    [BitField(4, Width = 5, Saturating = true)] public partial byte Clamped { get; set; }
}

/// <summary>
/// Signed sbyte-backed struct with a 3-bit signed saturating field.
/// Valid range: [-4, 3].
/// </summary>
[BitFields(typeof(sbyte))]
public partial struct SatSignedSbyte
{
    [BitField(0, Width = 3, Saturating = true)] public partial sbyte Val { get; set; }
}

/// <summary>
/// Full-width field with Saturating=true — saturation is a no-op because
/// the field width matches the property type width.
/// </summary>
[BitFields(typeof(byte))]
public partial struct SatFullWidth
{
    [BitField(0, Width = 8, Saturating = true)] public partial byte Full { get; set; }
}

/// <summary>
/// Multiple saturating fields in one struct for independence testing.
/// </summary>
[BitFields(typeof(ushort))]
public partial struct SatMultiField
{
    [BitField(0, Width = 4, Saturating = true)] public partial byte FieldA { get; set; }
    [BitField(4, Width = 4, Saturating = true)] public partial byte FieldB { get; set; }
    [BitField(8, Width = 4)] public partial byte FieldC { get; set; }
}

/// <summary>
/// Signed short-backed struct with saturating field for With method testing.
/// </summary>
[BitFields(typeof(short))]
public partial struct SatWithMethod
{
    [BitField(0, Width = 6, Saturating = true)] public partial short Clamped { get; set; }
}

/// <summary>
/// Multi-word struct with saturating field.
/// </summary>
[BitFields(133)]
public partial struct SatMultiWord
{
    [BitField(0, Width = 10, Saturating = true)] public partial ushort Low { get; set; }
    [BitField(10, Width = 50)] public partial ulong High { get; set; }
}

/// <summary>
/// View (record struct) with saturating field.
/// </summary>
[BitFields]
public partial record struct SatView
{
    [BitField(0, Width = 5, Saturating = true)] public partial byte Clamped { get; set; }
    [BitField(5, Width = 3)] public partial byte Other { get; set; }
}

#endregion

/// <summary>
/// Tests for the <c>Saturating</c> parameter on <see cref="BitFieldAttribute"/>.
/// Verifies that when <c>Saturating = true</c>, setters and <c>With{Name}</c> methods
/// clamp incoming values to the valid range instead of truncating (wrapping).
/// </summary>
public class BitFieldSaturatingTests
{
    #region Unsigned Setter — Clamping

    [Fact]
    public void UnsignedSetter_ValueWithinRange_PassesThrough()
    {
        var reg = new SatUnsignedByte();
        reg.Low = 5;
        reg.Low.Should().Be(5);
    }

    [Fact]
    public void UnsignedSetter_MaxValue_PassesThrough()
    {
        var reg = new SatUnsignedByte();
        reg.Low = 7;  // 2^3 - 1
        reg.Low.Should().Be(7);
    }

    [Fact]
    public void UnsignedSetter_Overflow_ClampsToMax()
    {
        var reg = new SatUnsignedByte();
        reg.Low = 10;  // exceeds 7
        reg.Low.Should().Be(7, "saturating clamps to max value for 3-bit field");
    }

    [Fact]
    public void UnsignedSetter_LargeOverflow_ClampsToMax()
    {
        var reg = new SatUnsignedByte();
        reg.Low = 255;  // max byte, far exceeds 7
        reg.Low.Should().Be(7);
    }

    [Fact]
    public void UnsignedSetter_Zero_PassesThrough()
    {
        var reg = new SatUnsignedByte();
        reg.Low = 7;
        reg.Low = 0;
        reg.Low.Should().Be(0);
    }

    #endregion

    #region Signed Setter — Clamping

    [Fact]
    public void SignedSetter_ValueWithinRange_PassesThrough()
    {
        var reg = new SatSignedInt();
        reg.Signed = 10;  // 5-bit signed: range [-16, 15]
        reg.Signed.Should().Be(10);
    }

    [Fact]
    public void SignedSetter_MaxBoundary_PassesThrough()
    {
        var reg = new SatSignedInt();
        reg.Signed = 15;  // max for 5-bit signed
        reg.Signed.Should().Be(15);
    }

    [Fact]
    public void SignedSetter_MinBoundary_PassesThrough()
    {
        var reg = new SatSignedInt();
        reg.Signed = -16;  // min for 5-bit signed
        reg.Signed.Should().Be(-16);
    }

    [Fact]
    public void SignedSetter_PositiveOverflow_ClampsToMax()
    {
        var reg = new SatSignedInt();
        reg.Signed = 20;  // exceeds 15
        reg.Signed.Should().Be(15, "saturating clamps to max for 5-bit signed field");
    }

    [Fact]
    public void SignedSetter_NegativeOverflow_ClampsToMin()
    {
        var reg = new SatSignedInt();
        reg.Signed = -20;  // below -16
        reg.Signed.Should().Be(-16, "saturating clamps to min for 5-bit signed field");
    }

    [Fact]
    public void SignedSetter_LargeOverflow_ClampsCorrectly()
    {
        var reg = new SatSignedInt();
        reg.Signed = 1000;
        reg.Signed.Should().Be(15);

        reg.Signed = -1000;
        reg.Signed.Should().Be(-16);
    }

    #endregion

    #region SByte Signed — Small Type

    [Fact]
    public void SignedSbyte_ClampsToRange()
    {
        // 3-bit signed: range [-4, 3]
        var reg = new SatSignedSbyte();
        reg.Val = 3;
        reg.Val.Should().Be(3);

        reg.Val = -4;
        reg.Val.Should().Be(-4);

        reg.Val = 10;
        reg.Val.Should().Be(3, "clamped to max");

        reg.Val = -10;
        reg.Val.Should().Be(-4, "clamped to min");
    }

    #endregion

    #region Non-saturating — Wrapping Behavior Unchanged

    [Fact]
    public void NonSaturating_Overflow_Wraps()
    {
        var reg = new SatUnsignedByte();
        reg.High = 20;  // 4-bit field, max 15, non-saturating
        // Without saturation, 20 & 0xF = 4 (wrapping)
        reg.High.Should().Be(4, "non-saturating fields should wrap/truncate");
    }

    #endregion

    #region Field Independence

    [Fact]
    public void MultiField_SaturationDoesNotAffectOtherFields()
    {
        var reg = new SatMultiField();
        reg.FieldA = 20;  // clamped to 15
        reg.FieldB = 20;  // clamped to 15
        reg.FieldC = 5;   // non-saturating, passes through

        reg.FieldA.Should().Be(15);
        reg.FieldB.Should().Be(15);
        reg.FieldC.Should().Be(5);

        // Verify they don't interfere
        reg.FieldA = 3;
        reg.FieldA.Should().Be(3);
        reg.FieldB.Should().Be(15, "changing FieldA should not affect FieldB");
        reg.FieldC.Should().Be(5, "changing FieldA should not affect FieldC");
    }

    #endregion

    #region Shifted Field

    [Fact]
    public void ShiftedField_SaturationWorksWithNonZeroShift()
    {
        var reg = new SatShiftedField();
        // 5-bit field at bits 4-8, max = 31
        reg.Clamped = 31;
        reg.Clamped.Should().Be(31);

        reg.Clamped = 40;  // exceeds 31
        reg.Clamped.Should().Be(31, "clamped at non-zero bit offset");

        // Verify prefix field unaffected
        reg.Prefix = 0xA;
        reg.Prefix.Should().Be(0xA);
        reg.Clamped.Should().Be(31);
    }

    #endregion

    #region Full-Width — No-op

    [Fact]
    public void FullWidth_SaturationIsNoop()
    {
        // 8-bit field in byte: saturating has no effect
        var reg = new SatFullWidth();
        reg.Full = 255;
        reg.Full.Should().Be(255);

        reg.Full = 0;
        reg.Full.Should().Be(0);
    }

    #endregion

    #region With Method

    [Fact]
    public void WithMethod_ClampsOverflow()
    {
        var reg = new SatWithMethod();
        // 6-bit signed: range [-32, 31]
        var result = reg.WithClamped(50);
        result.Clamped.Should().Be(31, "WithClamped should clamp overflow");
    }

    [Fact]
    public void WithMethod_ClampsUnderflow()
    {
        var reg = new SatWithMethod();
        var result = reg.WithClamped(-50);
        result.Clamped.Should().Be(-32, "WithClamped should clamp underflow");
    }

    [Fact]
    public void WithMethod_InRange_PassesThrough()
    {
        var reg = new SatWithMethod();
        var result = reg.WithClamped(10);
        result.Clamped.Should().Be(10);
    }

    #endregion

    #region Ushort 10-bit

    [Fact]
    public void Ushort10Bit_ClampsAbove1023()
    {
        var reg = new SatUnsignedUshort();
        reg.Clamped = 1023;
        reg.Clamped.Should().Be(1023);

        reg.Clamped = 2000;
        reg.Clamped.Should().Be(1023, "10-bit max = 1023");
    }

    #endregion

    #region Multi-Word

    [Fact]
    public void MultiWord_SaturationClampsField()
    {
        var reg = new SatMultiWord();
        // 10-bit unsigned field, max = 1023
        reg.Low = 1023;
        reg.Low.Should().Be(1023);

        reg.Low = 2000;
        reg.Low.Should().Be(1023, "multi-word saturating field clamped to 1023");
    }

    [Fact]
    public void MultiWord_NonSaturatingFieldUnchanged()
    {
        var reg = new SatMultiWord();
        reg.High = 100;
        reg.High.Should().Be(100);
    }

    #endregion

    #region View

    [Fact]
    public void View_SaturationClampsField()
    {
        var buffer = new byte[2];
        var view = new SatView(buffer);

        // 5-bit unsigned field, max = 31
        view.Clamped = 31;
        view.Clamped.Should().Be(31);

        view.Clamped = 40;
        view.Clamped.Should().Be(31, "view saturating field clamped to 31");
    }

    [Fact]
    public void View_NonSaturatingFieldUnchanged()
    {
        var buffer = new byte[2];
        var view = new SatView(buffer);

        view.Other = 5;
        view.Other.Should().Be(5);
    }

    [Fact]
    public void View_SaturationFieldIndependence()
    {
        var buffer = new byte[2];
        var view = new SatView(buffer);

        view.Clamped = 50;  // clamped to 31
        view.Other = 7;

        view.Clamped.Should().Be(31);
        view.Other.Should().Be(7, "setting saturating field should not affect adjacent field");
    }

    #endregion

    #region Saturating=false (explicit)

    /// <summary>
    /// Verifies that explicitly setting Saturating=false produces wrapping behaviour,
    /// identical to omitting the parameter entirely.
    /// </summary>
    [Fact]
    public void ExplicitFalse_Wraps()
    {
        var reg = new SatUnsignedByte();
        reg.High = 20;  // Saturating is not set (defaults to false)
        reg.High.Should().Be(4, "Saturating=false should wrap: 20 & 0xF = 4");
    }

    #endregion
}
