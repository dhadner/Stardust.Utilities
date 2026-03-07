using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Comprehensive tests for nint and nuint BitFields structs.
/// High-bit tests (bits 32+) are guarded by nint.Size == 8 since they are
/// only valid on 64-bit platforms.
/// </summary>
public class BitFieldNativeIntTests
{
    #region 32-bit Safe nint Tests

    /// <summary>
    /// Tests that a 32-bit-safe nint struct works on all platforms.
    /// </summary>
    [Fact]
    public void NintSafe32_Flags_GetAndSet()
    {
        NintSafe32Reg reg = 0;
        reg.Enabled.Should().BeFalse();
        reg.Active.Should().BeFalse();

        reg.Enabled = true;
        reg.Enabled.Should().BeTrue();
        reg.Active.Should().BeFalse();

        reg.Active = true;
        reg.Active.Should().BeTrue();
    }

    [Fact]
    public void NintSafe32_Fields_GetAndSet()
    {
        NintSafe32Reg reg = 0;

        reg.Status = 0xAB;
        reg.Status.Should().Be(0xAB);

        reg.Command = 0x0F;
        reg.Command.Should().Be(0x0F);
    }

    [Fact]
    public void NintSafe32_FieldIsolation()
    {
        // Set all defined bits
        NintSafe32Reg reg = 0;
        reg.Status = 0xFF;
        reg.Command = 0x0F;
        reg.Enabled = true;
        reg.Active = true;

        // Clear Status, other fields should remain
        reg.Status = 0;
        reg.Status.Should().Be(0);
        reg.Command.Should().Be(0x0F);
        reg.Enabled.Should().BeTrue();
        reg.Active.Should().BeTrue();
    }

    [Fact]
    public void NintSafe32_CombinedValue()
    {
        NintSafe32Reg reg = 0;
        reg.Status = 0xAB;          // bits 0-7
        reg.Command = 0x0F;         // bits 8-11
        reg.Enabled = true;         // bit 28
        reg.Active = true;          // bit 29

        nint expected = (nint)(0xAB | (0x0F << 8) | (1 << 28) | (1 << 29));
        ((nint)reg).Should().Be(expected);
    }

    [Fact]
    public void NintSafe32_ImplicitConversion()
    {
        NintSafe32Reg reg = (nint)0x42;
        nint val = reg;
        val.Should().Be((nint)0x42);
    }

    [Fact]
    public void NintSafe32_WithMethods()
    {
        NintSafe32Reg reg = 0;
        var result = reg.WithStatus(0xCD).WithCommand(0x0A).WithEnabled(true);
        result.Status.Should().Be(0xCD);
        result.Command.Should().Be(0x0A);
        result.Enabled.Should().BeTrue();

        // Original unchanged
        reg.Status.Should().Be(0);
    }

    [Fact]
    public void NintSafe32_BitwiseOps()
    {
        NintSafe32Reg a = (nint)0x0F;
        NintSafe32Reg b = (nint)0xF0;

        var orResult = a | b;
        ((nint)orResult).Should().Be((nint)0xFF);

        var andResult = a & b;
        ((nint)andResult).Should().Be((nint)0);

        var xorResult = a ^ b;
        ((nint)xorResult).Should().Be((nint)0xFF);
    }

    [Fact]
    public void NintSafe32_Equality()
    {
        NintSafe32Reg a = (nint)0x42;
        NintSafe32Reg b = (nint)0x42;
        NintSafe32Reg c = (nint)0x24;

        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
        a.Equals(b).Should().BeTrue();
        a.Equals(c).Should().BeFalse();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void NintSafe32_Comparison()
    {
        NintSafe32Reg low = (nint)0x10;
        NintSafe32Reg high = (nint)0x20;

        (low < high).Should().BeTrue();
        (high > low).Should().BeTrue();
#pragma warning disable CS1718 // Comparison made to same variable
        (low <= low).Should().BeTrue();
        (high >= high).Should().BeTrue();
#pragma warning restore CS1718
    }

    [Fact]
    public void NintSafe32_ToString()
    {
        NintSafe32Reg reg = (nint)0xAB;
        reg.ToString().Should().Be("0xAB");
    }

    [Fact]
    public void NintSafe32_Parse()
    {
        var result = NintSafe32Reg.Parse("0xAB");
        result.Status.Should().Be(0xAB);
    }

    [Fact]
    public void NintSafe32_TryParse()
    {
        NintSafe32Reg.TryParse("0xFF", out var result).Should().BeTrue();
        result.Status.Should().Be(0xFF);

        NintSafe32Reg.TryParse("invalid", out _).Should().BeFalse();
    }

    [Fact]
    public void NintSafe32_JsonRoundTrip()
    {
        NintSafe32Reg original = (nint)0xAB;
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<NintSafe32Reg>(json);
        ((nint)restored).Should().Be((nint)0xAB);
    }

    [Fact]
    public void NintSafe32_ByteSpan_RoundTrip()
    {
        NintSafe32Reg original = (nint)0x12345678;
        original.Status.Should().Be(0x78);
        original.Command.Should().Be(0x06); // bits 8-11 of 0x12345678 = 0x6

        var bytes = original.ToByteArray();
        bytes.Length.Should().Be(NintSafe32Reg.SIZE_IN_BYTES);

        var restored = NintSafe32Reg.ReadFrom(bytes);
        ((nint)restored).Should().Be((nint)original);
    }

    [Fact]
    public void NintSafe32_StaticBitProperties()
    {
        // EnabledBit should have bit 28 set
        NintSafe32Reg enabledBit = NintSafe32Reg.EnabledBit;
        enabledBit.Enabled.Should().BeTrue();
        enabledBit.Status.Should().Be(0);
        enabledBit.Command.Should().Be(0);

        // ActiveBit should have bit 29 set
        NintSafe32Reg activeBit = NintSafe32Reg.ActiveBit;
        activeBit.Active.Should().BeTrue();
        activeBit.Enabled.Should().BeFalse();
    }

    [Fact]
    public void NintSafe32_StaticMaskProperties()
    {
        // StatusMask should cover bits 0-7
        NintSafe32Reg statusMask = NintSafe32Reg.StatusMask;
        ((nint)statusMask).Should().Be((nint)0xFF);

        // CommandMask should cover bits 8-11
        NintSafe32Reg commandMask = NintSafe32Reg.CommandMask;
        ((nint)commandMask).Should().Be((nint)0xF00);
    }

    [Fact]
    public void NintSafe32_FieldMetadata()
    {
        var fields = NintSafe32Reg.Fields;
        fields.Length.Should().Be(4);
        fields[0].Name.Should().Be("Status");
        fields[0].StartBit.Should().Be(0);
        fields[0].BitLength.Should().Be(8);
    }

    [Fact]
    public void NintSafe32_Arithmetic()
    {
        NintSafe32Reg a = (nint)10;
        NintSafe32Reg b = (nint)3;

        ((nint)(a + b)).Should().Be((nint)13);
        ((nint)(a - b)).Should().Be((nint)7);
    }

    [Fact]
    public void NintSafe32_Shift()
    {
        NintSafe32Reg a = (nint)1;
        ((nint)(a << 4)).Should().Be((nint)16);
        ((nint)(a << 4 >> 4)).Should().Be((nint)1);
    }

    #endregion

    #region 32-bit Safe nuint Tests

    [Fact]
    public void NuintSafe32_Flags_GetAndSet()
    {
        NuintSafe32Reg reg = 0;
        reg.Enabled.Should().BeFalse();
        reg.Active.Should().BeFalse();

        reg.Enabled = true;
        reg.Enabled.Should().BeTrue();

        reg.Active = true;
        reg.Active.Should().BeTrue();
    }

    [Fact]
    public void NuintSafe32_Fields_GetAndSet()
    {
        NuintSafe32Reg reg = 0;

        reg.Status = 0xAB;
        reg.Status.Should().Be(0xAB);

        reg.Command = 0x0F;
        reg.Command.Should().Be(0x0F);
    }

    [Fact]
    public void NuintSafe32_FieldIsolation()
    {
        NuintSafe32Reg reg = 0;
        reg.Status = 0xFF;
        reg.Command = 0x0F;
        reg.Enabled = true;
        reg.Active = true;

        reg.Status = 0;
        reg.Status.Should().Be(0);
        reg.Command.Should().Be(0x0F);
        reg.Enabled.Should().BeTrue();
        reg.Active.Should().BeTrue();
    }

    [Fact]
    public void NuintSafe32_CombinedValue()
    {
        NuintSafe32Reg reg = 0;
        reg.Status = 0xAB;
        reg.Command = 0x0F;
        reg.Enabled = true;
        reg.Active = true;

        nuint expected = (nuint)(0xABu | (0x0Fu << 8) | (1u << 28) | (1u << 29));
        ((nuint)reg).Should().Be(expected);
    }

    [Fact]
    public void NuintSafe32_ImplicitConversion()
    {
        NuintSafe32Reg reg = (nuint)0x42;
        nuint val = reg;
        val.Should().Be((nuint)0x42);
    }

    [Fact]
    public void NuintSafe32_WithMethods()
    {
        NuintSafe32Reg reg = 0;
        var result = reg.WithStatus(0xCD).WithCommand(0x0A).WithEnabled(true);
        result.Status.Should().Be(0xCD);
        result.Command.Should().Be(0x0A);
        result.Enabled.Should().BeTrue();
        reg.Status.Should().Be(0);
    }

    [Fact]
    public void NuintSafe32_BitwiseOps()
    {
        NuintSafe32Reg a = (nuint)0x0F;
        NuintSafe32Reg b = (nuint)0xF0;

        ((nuint)(a | b)).Should().Be((nuint)0xFF);
        ((nuint)(a & b)).Should().Be((nuint)0);
        ((nuint)(a ^ b)).Should().Be((nuint)0xFF);
    }

    [Fact]
    public void NuintSafe32_Equality()
    {
        NuintSafe32Reg a = (nuint)0x42;
        NuintSafe32Reg b = (nuint)0x42;
        NuintSafe32Reg c = (nuint)0x24;

        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
        a.Equals(b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void NuintSafe32_ToString()
    {
        NuintSafe32Reg reg = (nuint)0xAB;
        reg.ToString().Should().Be("0xAB");
    }

    [Fact]
    public void NuintSafe32_Parse()
    {
        var result = NuintSafe32Reg.Parse("0xAB");
        result.Status.Should().Be(0xAB);
    }

    [Fact]
    public void NuintSafe32_JsonRoundTrip()
    {
        NuintSafe32Reg original = (nuint)0xAB;
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<NuintSafe32Reg>(json);
        ((nuint)restored).Should().Be((nuint)0xAB);
    }

    [Fact]
    public void NuintSafe32_ByteSpan_RoundTrip()
    {
        NuintSafe32Reg original = (nuint)0x12345678;
        var bytes = original.ToByteArray();
        bytes.Length.Should().Be(NuintSafe32Reg.SIZE_IN_BYTES);
        var restored = NuintSafe32Reg.ReadFrom(bytes);
        ((nuint)restored).Should().Be((nuint)original);
    }

    [Fact]
    public void NuintSafe32_StaticBitProperties()
    {
        NuintSafe32Reg enabledBit = NuintSafe32Reg.EnabledBit;
        enabledBit.Enabled.Should().BeTrue();
        enabledBit.Status.Should().Be(0);
    }

    [Fact]
    public void NuintSafe32_StaticMaskProperties()
    {
        ((nuint)NuintSafe32Reg.StatusMask).Should().Be((nuint)0xFF);
        ((nuint)NuintSafe32Reg.CommandMask).Should().Be((nuint)0xF00);
    }

    [Fact]
    public void NuintSafe32_WriteTo_LittleEndian()
    {
        NuintSafe32Reg value = (nuint)0x01020304;
        Span<byte> buf = stackalloc byte[NuintSafe32Reg.SIZE_IN_BYTES];
        value.WriteTo(buf);
        buf[0].Should().Be(0x04);
        buf[1].Should().Be(0x03);
        buf[2].Should().Be(0x02);
        buf[3].Should().Be(0x01);
    }

    [Fact]
    public void NuintSafe32_TryWriteTo_FailsWithTooSmallSpan()
    {
        NuintSafe32Reg value = (nuint)42;
        Span<byte> buf = stackalloc byte[NuintSafe32Reg.SIZE_IN_BYTES - 1];
        value.TryWriteTo(buf, out int written).Should().BeFalse();
        written.Should().Be(0);
    }

    [Fact]
    public void NuintSafe32_SpanConstructor_ThrowsOnEmpty()
    {
        var act = () => new NuintSafe32Reg(ReadOnlySpan<byte>.Empty);
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region 64-bit Wide nint Tests (require 64-bit platform)

    [Fact]
    public void WideNint_SizeIsPlatformDependent()
    {
        GeneratedWideRegNint.SIZE_IN_BYTES.Should().Be(nint.Size);
    }

    [Fact]
    public void WideNint_LowBits_GetAndSet()
    {
        // Low bits (0-23) should work on all platforms
        var reg = new GeneratedWideRegNint();

        reg.Status = 0xAB;
        reg.Status.Should().Be(0xAB);

        reg.Data = 0xCDEF;
        reg.Data.Should().Be(0xCDEF);
    }

    [Fact]
    public void WideNint_LowBits_FieldIsolation()
    {
        var reg = new GeneratedWideRegNint();
        reg.Status = 0xFF;
        reg.Data = 0xFFFF;

        // Change Status, Data should be untouched
        reg.Status = 0x00;
        reg.Status.Should().Be(0x00);
        reg.Data.Should().Be(0xFFFF);
    }

    [Fact]
    public void WideNint_HighBits_FullTest()
    {
        Assert.SkipWhen(nint.Size != 8, "Requires 64-bit platform");

        var reg = new GeneratedWideRegNint();

        // Set all fields
        reg.Status = 0xAB;
        reg.Data = 0xCDEF;
        reg.Address = 0x12345678;
        reg.Valid = true;
        reg.Ready = true;

        // Verify
        reg.Status.Should().Be(0xAB);
        reg.Data.Should().Be(0xCDEF);
        reg.Address.Should().Be(0x12345678);
        reg.Valid.Should().BeTrue();
        reg.Ready.Should().BeTrue();
    }

    [Fact]
    public void WideNint_HighBits_FlagOperations()
    {
        Assert.SkipWhen(nint.Size != 8, "Requires 64-bit platform");

        var reg = new GeneratedWideRegNint();

        reg.Valid = true;
        reg.Valid.Should().BeTrue();
        reg.Ready.Should().BeFalse();

        reg.Ready = true;
        reg.Ready.Should().BeTrue();

        // Clear Valid, Ready should remain
        reg.Valid = false;
        reg.Valid.Should().BeFalse();
        reg.Ready.Should().BeTrue();
    }

    [Fact]
    public void WideNint_HighBits_AddressField()
    {
        Assert.SkipWhen(nint.Size != 8, "Requires 64-bit platform");

        var reg = new GeneratedWideRegNint();

        // Address is bits 24-55 (32 bits, straddles the 32-bit boundary)
        reg.Address = 0xFFFFFFFF;
        reg.Address.Should().Be(0xFFFFFFFF);

        reg.Address = 0x00000000;
        reg.Address.Should().Be(0x00000000);

        reg.Address = 0xDEADBEEF;
        reg.Address.Should().Be(0xDEADBEEF);
    }

    [Fact]
    public void WideNint_WithMethods()
    {
        Assert.SkipWhen(nint.Size != 8, "Requires 64-bit platform");

        var reg = new GeneratedWideRegNint();
        var result = reg.WithStatus(0xAB).WithData(0xCDEF).WithAddress(0x12345678).WithValid(true).WithReady(true);

        result.Status.Should().Be(0xAB);
        result.Data.Should().Be(0xCDEF);
        result.Address.Should().Be(0x12345678);
        result.Valid.Should().BeTrue();
        result.Ready.Should().BeTrue();

        // Original unchanged
        reg.Status.Should().Be(0);
    }

    [Fact]
    public void WideNint_StaticBitProperties()
    {
        Assert.SkipWhen(nint.Size != 8, "Requires 64-bit platform");

        var validBit = GeneratedWideRegNint.ValidBit;
        validBit.Valid.Should().BeTrue();
        validBit.Ready.Should().BeFalse();
        validBit.Status.Should().Be(0);

        var readyBit = GeneratedWideRegNint.ReadyBit;
        readyBit.Ready.Should().BeTrue();
        readyBit.Valid.Should().BeFalse();
    }

    [Fact]
    public void WideNint_StaticMaskProperties()
    {
        Assert.SkipWhen(nint.Size != 8, "Requires 64-bit platform");

        var statusMask = GeneratedWideRegNint.StatusMask;
        statusMask.Status.Should().Be(0xFF);
        statusMask.Data.Should().Be(0);

        var addressMask = GeneratedWideRegNint.AddressMask;
        addressMask.Address.Should().Be(0xFFFFFFFF);
    }

    [Fact]
    public void WideNint_BitwiseOps()
    {
        Assert.SkipWhen(nint.Size != 8, "Requires 64-bit platform");

        var reg = new GeneratedWideRegNint();
        reg.Status = 0xFF;
        reg.Valid = true;

        // Use mask to clear Status
        var cleared = reg & ~GeneratedWideRegNint.StatusMask;
        cleared.Status.Should().Be(0);
        cleared.Valid.Should().BeTrue();

        // Use OR to set flags
        GeneratedWideRegNint combined = GeneratedWideRegNint.ValidBit | GeneratedWideRegNint.ReadyBit;
        combined.Valid.Should().BeTrue();
        combined.Ready.Should().BeTrue();
        combined.Status.Should().Be(0);
    }

    [Fact]
    public void WideNint_Equality()
    {
        var a = new GeneratedWideRegNint((nint)0x42);
        var b = new GeneratedWideRegNint((nint)0x42);
        var c = new GeneratedWideRegNint((nint)0x24);

        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
        a.Equals(b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void WideNint_Comparison()
    {
        var low = new GeneratedWideRegNint((nint)0x10);
        var high = new GeneratedWideRegNint((nint)0x20);

        (low < high).Should().BeTrue();
        (high > low).Should().BeTrue();
#pragma warning disable CS1718 // Comparison made to same variable
        (low <= low).Should().BeTrue();
#pragma warning restore CS1718
    }

    [Fact]
    public void WideNint_ToString()
    {
        var reg = new GeneratedWideRegNint((nint)0xAB);
        reg.ToString().Should().Be("0xAB");
    }

    [Fact]
    public void WideNint_Parse()
    {
        var result = GeneratedWideRegNint.Parse("0xAB");
        result.Status.Should().Be(0xAB);
    }

    [Fact]
    public void WideNint_TryParse()
    {
        GeneratedWideRegNint.TryParse("0xFF", out var result).Should().BeTrue();
        result.Status.Should().Be(0xFF);

        GeneratedWideRegNint.TryParse("invalid", out _).Should().BeFalse();
    }

    [Fact]
    public void WideNint_JsonRoundTrip()
    {
        var original = new GeneratedWideRegNint((nint)0xAB);
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<GeneratedWideRegNint>(json);
        ((nint)restored).Should().Be((nint)0xAB);
    }

    [Fact]
    public void WideNint_ByteSpan_RoundTrip()
    {
        var original = new GeneratedWideRegNint((nint)0x12345678);
        var bytes = original.ToByteArray();
        bytes.Length.Should().Be(GeneratedWideRegNint.SIZE_IN_BYTES);

        var restored = GeneratedWideRegNint.ReadFrom(bytes);
        ((nint)restored).Should().Be((nint)original);
    }

    [Fact]
    public void WideNint_SpanConstructor_ThrowsOnEmpty()
    {
        var act = () => new GeneratedWideRegNint(ReadOnlySpan<byte>.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WideNint_FullCombinedValue()
    {
        Assert.SkipWhen(nint.Size != 8, "Requires 64-bit platform");

        var reg = new GeneratedWideRegNint();
        reg.Status = 0xAB;
        reg.Data = 0xCDEF;
        reg.Address = 0x12345678;
        reg.Valid = true;
        reg.Ready = true;

        // Verify combined value matches manual construction
        long expected = 0xAB
            | ((long)0xCDEF << 8)
            | ((long)0x12345678 << 24)
            | (1L << 56)
            | (1L << 57);
        ((nint)reg).Should().Be((nint)expected);
    }

    [Fact]
    public void WideNint_FieldMetadata()
    {
        var fields = GeneratedWideRegNint.Fields;
        fields.Length.Should().Be(5);
        fields[0].Name.Should().Be("Status");
        fields[0].StartBit.Should().Be(0);
        fields[0].BitLength.Should().Be(8);
        fields[4].Name.Should().Be("Ready");
        fields[4].StartBit.Should().Be(57);
        fields[4].BitLength.Should().Be(1);
    }

    #endregion

    #region 64-bit Wide nuint Tests (require 64-bit platform)

    [Fact]
    public void WideNuint_SizeIsPlatformDependent()
    {
        GeneratedWideRegNuint.SIZE_IN_BYTES.Should().Be(nint.Size);
    }

    [Fact]
    public void WideNuint_LowBits_GetAndSet()
    {
        var reg = new GeneratedWideRegNuint();

        reg.Status = 0xAB;
        reg.Status.Should().Be(0xAB);

        reg.Data = 0xCDEF;
        reg.Data.Should().Be(0xCDEF);
    }

    [Fact]
    public void WideNuint_LowBits_FieldIsolation()
    {
        var reg = new GeneratedWideRegNuint();
        reg.Status = 0xFF;
        reg.Data = 0xFFFF;

        reg.Status = 0x00;
        reg.Status.Should().Be(0x00);
        reg.Data.Should().Be(0xFFFF);
    }

    [Fact]
    public void WideNuint_HighBits_FullTest()
    {
        Assert.SkipWhen(nint.Size != 8, "Requires 64-bit platform");

        var reg = new GeneratedWideRegNuint();

        reg.Status = 0xAB;
        reg.Data = 0xCDEF;
        reg.Address = 0x12345678;
        reg.Valid = true;
        reg.Ready = true;

        reg.Status.Should().Be(0xAB);
        reg.Data.Should().Be(0xCDEF);
        reg.Address.Should().Be(0x12345678);
        reg.Valid.Should().BeTrue();
        reg.Ready.Should().BeTrue();
    }

    [Fact]
    public void WideNuint_HighBits_FlagOperations()
    {
        Assert.SkipWhen(nint.Size != 8, "Requires 64-bit platform");

        var reg = new GeneratedWideRegNuint();

        reg.Valid = true;
        reg.Valid.Should().BeTrue();
        reg.Ready.Should().BeFalse();

        reg.Ready = true;
        reg.Ready.Should().BeTrue();

        reg.Valid = false;
        reg.Valid.Should().BeFalse();
        reg.Ready.Should().BeTrue();
    }

    [Fact]
    public void WideNuint_HighBits_AddressField()
    {
        Assert.SkipWhen(nint.Size != 8, "Requires 64-bit platform");

        var reg = new GeneratedWideRegNuint();

        reg.Address = 0xFFFFFFFF;
        reg.Address.Should().Be(0xFFFFFFFF);

        reg.Address = 0x00000000;
        reg.Address.Should().Be(0x00000000);

        reg.Address = 0xDEADBEEF;
        reg.Address.Should().Be(0xDEADBEEF);
    }

    [Fact]
    public void WideNuint_WithMethods()
    {
        Assert.SkipWhen(nint.Size != 8, "Requires 64-bit platform");

        var reg = new GeneratedWideRegNuint();
        var result = reg.WithStatus(0xAB).WithData(0xCDEF).WithAddress(0x12345678).WithValid(true).WithReady(true);

        result.Status.Should().Be(0xAB);
        result.Data.Should().Be(0xCDEF);
        result.Address.Should().Be(0x12345678);
        result.Valid.Should().BeTrue();
        result.Ready.Should().BeTrue();

        reg.Status.Should().Be(0);
    }

    [Fact]
    public void WideNuint_StaticBitProperties()
    {
        Assert.SkipWhen(nint.Size != 8, "Requires 64-bit platform");

        var validBit = GeneratedWideRegNuint.ValidBit;
        validBit.Valid.Should().BeTrue();
        validBit.Ready.Should().BeFalse();
        validBit.Status.Should().Be(0);

        var readyBit = GeneratedWideRegNuint.ReadyBit;
        readyBit.Ready.Should().BeTrue();
        readyBit.Valid.Should().BeFalse();
    }

    [Fact]
    public void WideNuint_StaticMaskProperties()
    {
        Assert.SkipWhen(nint.Size != 8, "Requires 64-bit platform");

        var statusMask = GeneratedWideRegNuint.StatusMask;
        statusMask.Status.Should().Be(0xFF);
        statusMask.Data.Should().Be(0);

        var addressMask = GeneratedWideRegNuint.AddressMask;
        addressMask.Address.Should().Be(0xFFFFFFFF);
    }

    [Fact]
    public void WideNuint_BitwiseOps()
    {
        Assert.SkipWhen(nint.Size != 8, "Requires 64-bit platform");

        var reg = new GeneratedWideRegNuint();
        reg.Status = 0xFF;
        reg.Valid = true;

        var cleared = reg & ~GeneratedWideRegNuint.StatusMask;
        cleared.Status.Should().Be(0);
        cleared.Valid.Should().BeTrue();

        GeneratedWideRegNuint combined = GeneratedWideRegNuint.ValidBit | GeneratedWideRegNuint.ReadyBit;
        combined.Valid.Should().BeTrue();
        combined.Ready.Should().BeTrue();
        combined.Status.Should().Be(0);
    }

    [Fact]
    public void WideNuint_Equality()
    {
        var a = new GeneratedWideRegNuint((nuint)0x42);
        var b = new GeneratedWideRegNuint((nuint)0x42);
        var c = new GeneratedWideRegNuint((nuint)0x24);

        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
        a.Equals(b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void WideNuint_Comparison()
    {
        var low = new GeneratedWideRegNuint((nuint)0x10);
        var high = new GeneratedWideRegNuint((nuint)0x20);

        (low < high).Should().BeTrue();
        (high > low).Should().BeTrue();
#pragma warning disable CS1718 // Comparison made to same variable
        (low <= low).Should().BeTrue();
#pragma warning restore CS1718
    }

    [Fact]
    public void WideNuint_ToString()
    {
        var reg = new GeneratedWideRegNuint((nuint)0xAB);
        reg.ToString().Should().Be("0xAB");
    }

    [Fact]
    public void WideNuint_Parse()
    {
        var result = GeneratedWideRegNuint.Parse("0xAB");
        result.Status.Should().Be(0xAB);
    }

    [Fact]
    public void WideNuint_TryParse()
    {
        GeneratedWideRegNuint.TryParse("0xFF", out var result).Should().BeTrue();
        result.Status.Should().Be(0xFF);

        GeneratedWideRegNuint.TryParse("invalid", out _).Should().BeFalse();
    }

    [Fact]
    public void WideNuint_JsonRoundTrip()
    {
        var original = new GeneratedWideRegNuint((nuint)0xAB);
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<GeneratedWideRegNuint>(json);
        ((nuint)restored).Should().Be((nuint)0xAB);
    }

    [Fact]
    public void WideNuint_ByteSpan_RoundTrip()
    {
        var original = new GeneratedWideRegNuint((nuint)0x12345678);
        var bytes = original.ToByteArray();
        bytes.Length.Should().Be(GeneratedWideRegNuint.SIZE_IN_BYTES);

        var restored = GeneratedWideRegNuint.ReadFrom(bytes);
        ((nuint)restored).Should().Be((nuint)original);
    }

    [Fact]
    public void WideNuint_SpanConstructor_ThrowsOnEmpty()
    {
        var act = () => new GeneratedWideRegNuint(ReadOnlySpan<byte>.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WideNuint_FullCombinedValue()
    {
        Assert.SkipWhen(nint.Size != 8, "Requires 64-bit platform");

        var reg = new GeneratedWideRegNuint();
        reg.Status = 0xAB;
        reg.Data = 0xCDEF;
        reg.Address = 0x12345678;
        reg.Valid = true;
        reg.Ready = true;

        ulong expected = 0xAB
            | ((ulong)0xCDEF << 8)
            | ((ulong)0x12345678 << 24)
            | (1UL << 56)
            | (1UL << 57);
        ((nuint)reg).Should().Be((nuint)expected);
    }

    [Fact]
    public void WideNuint_FieldMetadata()
    {
        var fields = GeneratedWideRegNuint.Fields;
        fields.Length.Should().Be(5);
        fields[0].Name.Should().Be("Status");
        fields[0].StartBit.Should().Be(0);
        fields[0].BitLength.Should().Be(8);
        fields[4].Name.Should().Be("Ready");
        fields[4].StartBit.Should().Be(57);
        fields[4].BitLength.Should().Be(1);
    }

    #endregion

    #region Cross-type Consistency Tests

    /// <summary>
    /// Verifies that nint and nuint produce identical results for the same bit patterns
    /// (on 64-bit platforms where both types are 64 bits).
    /// </summary>
    [Fact]
    public void NintAndNuint_ProduceSameResults()
    {
        Assert.SkipWhen(nint.Size != 8, "Requires 64-bit platform");

        var nintReg = new GeneratedWideRegNint();
        var nuintReg = new GeneratedWideRegNuint();

        nintReg.Status = 0xAB;
        nuintReg.Status = 0xAB;
        nintReg.Status.Should().Be(nuintReg.Status);

        nintReg.Data = 0xCDEF;
        nuintReg.Data = 0xCDEF;
        nintReg.Data.Should().Be(nuintReg.Data);

        nintReg.Address = 0x12345678;
        nuintReg.Address = 0x12345678;
        nintReg.Address.Should().Be(nuintReg.Address);

        nintReg.Valid = true;
        nuintReg.Valid = true;
        nintReg.Valid.Should().Be(nuintReg.Valid);
    }

    /// <summary>
    /// Verifies nint/nuint produce same results as ulong for the same field layout.
    /// </summary>
    [Fact]
    public void NativeInt_MatchesUlong_SameLayout()
    {
        Assert.SkipWhen(nint.Size != 8, "Requires 64-bit platform");

        var ulongReg = new GeneratedWideReg64();
        var nuintReg = new GeneratedWideRegNuint();

        ulongReg.Status = 0xAB;
        nuintReg.Status = 0xAB;
        ulongReg.Status.Should().Be(nuintReg.Status);

        ulongReg.Data = 0xCDEF;
        nuintReg.Data = 0xCDEF;
        ulongReg.Data.Should().Be(nuintReg.Data);

        ulongReg.Address = 0x12345678;
        nuintReg.Address = 0x12345678;
        ulongReg.Address.Should().Be(nuintReg.Address);

        ulongReg.Valid = true;
        nuintReg.Valid = true;
        ulongReg.Valid.Should().Be(nuintReg.Valid);

        ulongReg.Ready = true;
        nuintReg.Ready = true;
        ulongReg.Ready.Should().Be(nuintReg.Ready);

        // Raw values should match (when cast to ulong)
        ((ulong)ulongReg).Should().Be((ulong)(nuint)nuintReg);
    }

    /// <summary>
    /// Verifies that the 32-bit safe nint and nuint structs produce identical values.
    /// </summary>
    [Fact]
    public void Safe32_NintAndNuint_ProduceSameResults()
    {
        NintSafe32Reg nintReg = 0;
        NuintSafe32Reg nuintReg = 0;

        nintReg.Status = 0xAB;
        nuintReg.Status = 0xAB;
        nintReg.Status.Should().Be(nuintReg.Status);

        nintReg.Command = 0x0F;
        nuintReg.Command = 0x0F;
        nintReg.Command.Should().Be(nuintReg.Command);

        nintReg.Enabled = true;
        nuintReg.Enabled = true;
        nintReg.Enabled.Should().Be(nuintReg.Enabled);
    }

    #endregion

    #region Boundary Value Tests

    [Fact]
    public void NuintSafe32_MaxFieldValues()
    {
        NuintSafe32Reg reg = 0;

        // Status is 8 bits (max 0xFF)
        reg.Status = 0xFF;
        reg.Status.Should().Be(0xFF);

        // Command is 4 bits (max 0x0F)
        reg.Command = 0x0F;
        reg.Command.Should().Be(0x0F);

        // Values exceeding field width should be truncated
        reg.Command = 0xFF; // Should become 0x0F
        reg.Command.Should().Be(0x0F);
    }

    [Fact]
    public void WideNuint_MaxFieldValues()
    {
        Assert.SkipWhen(nint.Size != 8, "Requires 64-bit platform");

        var reg = new GeneratedWideRegNuint();

        // Status is 8 bits
        reg.Status = 0xFF;
        reg.Status.Should().Be(0xFF);

        // Data is 16 bits
        reg.Data = 0xFFFF;
        reg.Data.Should().Be(0xFFFF);

        // Address is 32 bits
        reg.Address = 0xFFFFFFFF;
        reg.Address.Should().Be(0xFFFFFFFF);

        // All should coexist
        reg.Valid = true;
        reg.Ready = true;

        reg.Status.Should().Be(0xFF);
        reg.Data.Should().Be(0xFFFF);
        reg.Address.Should().Be(0xFFFFFFFF);
        reg.Valid.Should().BeTrue();
        reg.Ready.Should().BeTrue();
    }

    [Fact]
    public void NintSafe32_Zero_IsDefault()
    {
        NintSafe32Reg.Zero.Should().Be(default(NintSafe32Reg));
        ((nint)NintSafe32Reg.Zero).Should().Be((nint)0);
    }

    [Fact]
    public void NuintSafe32_Zero_IsDefault()
    {
        NuintSafe32Reg.Zero.Should().Be(default(NuintSafe32Reg));
        ((nuint)NuintSafe32Reg.Zero).Should().Be((nuint)0);
    }

    #endregion
}

#region 32-bit Safe nint/nuint Test Structs

/// <summary>
/// 32-bit-safe nint register. All fields fit within bits 0-31, so this
/// is safe on both 32-bit and 64-bit platforms.
/// </summary>
[BitFields(typeof(nint))]
public partial struct NintSafe32Reg
{
    [BitField(0, 7)] public partial byte Status { get; set; }     // bits 0..=7 (8 bits)
    [BitField(8, 11)] public partial byte Command { get; set; }   // bits 8..=11 (4 bits)
    [BitFlag(28)] public partial bool Enabled { get; set; }
    [BitFlag(29)] public partial bool Active { get; set; }
}

/// <summary>
/// 32-bit-safe nuint register. All fields fit within bits 0-31, so this
/// is safe on both 32-bit and 64-bit platforms.
/// </summary>
[BitFields(typeof(nuint))]
public partial struct NuintSafe32Reg
{
    [BitField(0, 7)] public partial byte Status { get; set; }     // bits 0..=7 (8 bits)
    [BitField(8, 11)] public partial byte Command { get; set; }   // bits 8..=11 (4 bits)
    [BitFlag(28)] public partial bool Enabled { get; set; }
    [BitFlag(29)] public partial bool Active { get; set; }
}

#endregion
