using System;
using FluentAssertions;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Tests for the isBigEndian parameter added to the static ReadFrom factory on every
/// Int/UInt *Be and *Le type. ReadFrom forwards to the (ReadOnlySpan&lt;byte&gt;, bool)
/// constructor, so these tests confirm the forwarding preserves the parameter and
/// defaults match the type's own storage endianness.
/// </summary>
public class EndianReadFromWithOrderTests
{
    private static readonly byte[] BE16 = [0x12, 0x34];
    private static readonly byte[] LE16 = [0x34, 0x12];
    private const ushort U16 = 0x1234;
    private const short I16 = 0x1234;

    private static readonly byte[] BE32 = [0x12, 0x34, 0x56, 0x78];
    private static readonly byte[] LE32 = [0x78, 0x56, 0x34, 0x12];
    private const uint U32 = 0x12345678u;
    private const int I32 = 0x12345678;

    private static readonly byte[] BE64 = [0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF];
    private static readonly byte[] LE64 = [0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01];
    private const ulong U64 = 0x0123456789ABCDEFul;
    private const long I64 = 0x0123456789ABCDEFL;

    private static readonly byte[] BE128 =
    [
        0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
        0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF
    ];
    private static readonly byte[] LE128 =
    [
        0xFF, 0xEE, 0xDD, 0xCC, 0xBB, 0xAA, 0x99, 0x88,
        0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11, 0x00
    ];

    private static readonly byte[] BE256 =
    [
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
        0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
        0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
        0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F
    ];
    private static readonly byte[] LE256 =
    [
        0x1F, 0x1E, 0x1D, 0x1C, 0x1B, 0x1A, 0x19, 0x18,
        0x17, 0x16, 0x15, 0x14, 0x13, 0x12, 0x11, 0x10,
        0x0F, 0x0E, 0x0D, 0x0C, 0x0B, 0x0A, 0x09, 0x08,
        0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01, 0x00
    ];

    // ---------- Be types: default is isBigEndian: true ----------

    [Fact]
    public void UInt16Be_ReadFrom_DefaultIsBigEndian()
    {
        UInt16Be v = UInt16Be.ReadFrom(BE16.AsSpan());
        ((ushort)v).Should().Be(U16);
    }

    [Fact]
    public void UInt16Be_ReadFrom_ExplicitBigEndian()
    {
        UInt16Be v = UInt16Be.ReadFrom(BE16.AsSpan(), isBigEndian: true);
        ((ushort)v).Should().Be(U16);
    }

    [Fact]
    public void UInt16Be_ReadFrom_LittleEndianSource_ReversesBytes()
    {
        UInt16Be v = UInt16Be.ReadFrom(LE16.AsSpan(), isBigEndian: false);
        ((ushort)v).Should().Be(U16);
    }

    [Fact]
    public void Int16Be_ReadFrom_DefaultAndReverse()
    {
        Int16Be def = Int16Be.ReadFrom(BE16.AsSpan());
        Int16Be rev = Int16Be.ReadFrom(LE16.AsSpan(), isBigEndian: false);
        ((short)def).Should().Be(I16);
        ((short)rev).Should().Be(I16);
    }

    [Fact]
    public void UInt32Be_ReadFrom_DefaultAndReverse()
    {
        ((uint)UInt32Be.ReadFrom(BE32.AsSpan())).Should().Be(U32);
        ((uint)UInt32Be.ReadFrom(LE32.AsSpan(), isBigEndian: false)).Should().Be(U32);
    }

    [Fact]
    public void Int32Be_ReadFrom_DefaultAndReverse()
    {
        ((int)Int32Be.ReadFrom(BE32.AsSpan())).Should().Be(I32);
        ((int)Int32Be.ReadFrom(LE32.AsSpan(), isBigEndian: false)).Should().Be(I32);
    }

    [Fact]
    public void UInt64Be_ReadFrom_DefaultAndReverse()
    {
        ((ulong)UInt64Be.ReadFrom(BE64.AsSpan())).Should().Be(U64);
        ((ulong)UInt64Be.ReadFrom(LE64.AsSpan(), isBigEndian: false)).Should().Be(U64);
    }

    [Fact]
    public void Int64Be_ReadFrom_DefaultAndReverse()
    {
        ((long)Int64Be.ReadFrom(BE64.AsSpan())).Should().Be(I64);
        ((long)Int64Be.ReadFrom(LE64.AsSpan(), isBigEndian: false)).Should().Be(I64);
    }

    [Fact]
    public void UInt128Be_ReadFrom_DefaultAndReverse_ProduceEqualValues()
    {
        UInt128Be def = UInt128Be.ReadFrom(BE128.AsSpan());
        UInt128Be rev = UInt128Be.ReadFrom(LE128.AsSpan(), isBigEndian: false);
        def.Should().Be(rev);
    }

    [Fact]
    public void Int128Be_ReadFrom_DefaultAndReverse_ProduceEqualValues()
    {
        Int128Be def = Int128Be.ReadFrom(BE128.AsSpan());
        Int128Be rev = Int128Be.ReadFrom(LE128.AsSpan(), isBigEndian: false);
        def.Should().Be(rev);
    }

    [Fact]
    public void UInt256Be_ReadFrom_DefaultAndReverse_ProduceEqualValues()
    {
        UInt256Be def = UInt256Be.ReadFrom(BE256.AsSpan());
        UInt256Be rev = UInt256Be.ReadFrom(LE256.AsSpan(), isBigEndian: false);
        def.Should().Be(rev);
    }

    [Fact]
    public void Int256Be_ReadFrom_DefaultAndReverse_ProduceEqualValues()
    {
        Int256Be def = Int256Be.ReadFrom(BE256.AsSpan());
        Int256Be rev = Int256Be.ReadFrom(LE256.AsSpan(), isBigEndian: false);
        def.Should().Be(rev);
    }

    // ---------- Le types: default is isBigEndian: false ----------

    [Fact]
    public void UInt16Le_ReadFrom_DefaultIsLittleEndian()
    {
        UInt16Le v = UInt16Le.ReadFrom(LE16.AsSpan());
        ((ushort)v).Should().Be(U16);
    }

    [Fact]
    public void UInt16Le_ReadFrom_ExplicitLittleEndian()
    {
        UInt16Le v = UInt16Le.ReadFrom(LE16.AsSpan(), isBigEndian: false);
        ((ushort)v).Should().Be(U16);
    }

    [Fact]
    public void UInt16Le_ReadFrom_BigEndianSource_ReversesBytes()
    {
        UInt16Le v = UInt16Le.ReadFrom(BE16.AsSpan(), isBigEndian: true);
        ((ushort)v).Should().Be(U16);
    }

    [Fact]
    public void Int16Le_ReadFrom_DefaultAndReverse()
    {
        ((short)Int16Le.ReadFrom(LE16.AsSpan())).Should().Be(I16);
        ((short)Int16Le.ReadFrom(BE16.AsSpan(), isBigEndian: true)).Should().Be(I16);
    }

    [Fact]
    public void UInt32Le_ReadFrom_DefaultAndReverse()
    {
        ((uint)UInt32Le.ReadFrom(LE32.AsSpan())).Should().Be(U32);
        ((uint)UInt32Le.ReadFrom(BE32.AsSpan(), isBigEndian: true)).Should().Be(U32);
    }

    [Fact]
    public void Int32Le_ReadFrom_DefaultAndReverse()
    {
        ((int)Int32Le.ReadFrom(LE32.AsSpan())).Should().Be(I32);
        ((int)Int32Le.ReadFrom(BE32.AsSpan(), isBigEndian: true)).Should().Be(I32);
    }

    [Fact]
    public void UInt64Le_ReadFrom_DefaultAndReverse()
    {
        ((ulong)UInt64Le.ReadFrom(LE64.AsSpan())).Should().Be(U64);
        ((ulong)UInt64Le.ReadFrom(BE64.AsSpan(), isBigEndian: true)).Should().Be(U64);
    }

    [Fact]
    public void Int64Le_ReadFrom_DefaultAndReverse()
    {
        ((long)Int64Le.ReadFrom(LE64.AsSpan())).Should().Be(I64);
        ((long)Int64Le.ReadFrom(BE64.AsSpan(), isBigEndian: true)).Should().Be(I64);
    }

    [Fact]
    public void UInt128Le_ReadFrom_DefaultAndReverse_ProduceEqualValues()
    {
        UInt128Le def = UInt128Le.ReadFrom(LE128.AsSpan());
        UInt128Le rev = UInt128Le.ReadFrom(BE128.AsSpan(), isBigEndian: true);
        def.Should().Be(rev);
    }

    [Fact]
    public void Int128Le_ReadFrom_DefaultAndReverse_ProduceEqualValues()
    {
        Int128Le def = Int128Le.ReadFrom(LE128.AsSpan());
        Int128Le rev = Int128Le.ReadFrom(BE128.AsSpan(), isBigEndian: true);
        def.Should().Be(rev);
    }

    [Fact]
    public void UInt256Le_ReadFrom_DefaultAndReverse_ProduceEqualValues()
    {
        UInt256Le def = UInt256Le.ReadFrom(LE256.AsSpan());
        UInt256Le rev = UInt256Le.ReadFrom(BE256.AsSpan(), isBigEndian: true);
        def.Should().Be(rev);
    }

    [Fact]
    public void Int256Le_ReadFrom_DefaultAndReverse_ProduceEqualValues()
    {
        Int256Le def = Int256Le.ReadFrom(LE256.AsSpan());
        Int256Le rev = Int256Le.ReadFrom(BE256.AsSpan(), isBigEndian: true);
        def.Should().Be(rev);
    }

    // ---------- ReadFrom must agree with the matching (ReadOnlySpan<byte>, bool) ctor. ----------

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UInt32Be_ReadFrom_MatchesCtor(bool isBigEndian)
    {
        byte[] src = isBigEndian ? BE32 : LE32;
        UInt32Be viaCtor = new(src.AsSpan(), isBigEndian: isBigEndian);
        UInt32Be viaFactory = UInt32Be.ReadFrom(src.AsSpan(), isBigEndian: isBigEndian);
        viaCtor.Should().Be(viaFactory);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UInt64Le_ReadFrom_MatchesCtor(bool isBigEndian)
    {
        byte[] src = isBigEndian ? BE64 : LE64;
        UInt64Le viaCtor = new(src.AsSpan(), isBigEndian: isBigEndian);
        UInt64Le viaFactory = UInt64Le.ReadFrom(src.AsSpan(), isBigEndian: isBigEndian);
        viaCtor.Should().Be(viaFactory);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UInt256Be_ReadFrom_MatchesCtor(bool isBigEndian)
    {
        byte[] src = isBigEndian ? BE256 : LE256;
        UInt256Be viaCtor = new(src.AsSpan(), isBigEndian: isBigEndian);
        UInt256Be viaFactory = UInt256Be.ReadFrom(src.AsSpan(), isBigEndian: isBigEndian);
        viaCtor.Should().Be(viaFactory);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UInt256Le_ReadFrom_MatchesCtor(bool isBigEndian)
    {
        byte[] src = isBigEndian ? BE256 : LE256;
        UInt256Le viaCtor = new(src.AsSpan(), isBigEndian: isBigEndian);
        UInt256Le viaFactory = UInt256Le.ReadFrom(src.AsSpan(), isBigEndian: isBigEndian);
        viaCtor.Should().Be(viaFactory);
    }

    // ---------- Too-short span must still throw via the ReadFrom path (forwarded from ctor). ----------

    [Fact]
    public void UInt32Be_ReadFrom_TooShort_Throws()
    {
        byte[] tooShort = new byte[3];
        Action act = () => { _ = UInt32Be.ReadFrom(tooShort.AsSpan(), isBigEndian: true); };
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UInt64Le_ReadFrom_TooShort_Throws()
    {
        byte[] tooShort = new byte[7];
        Action act = () => { _ = UInt64Le.ReadFrom(tooShort.AsSpan(), isBigEndian: true); };
        act.Should().Throw<ArgumentException>();
    }
}
