using System;
using FluentAssertions;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Comprehensive tests for the <c>int offset</c> parameter on <c>WriteTo</c>, <c>TryWriteTo</c>,
/// <c>ReadFrom</c>, and constructors, as well as the parallel <c>byte[]</c> overloads
/// that exist alongside the <c>ReadOnlySpan&lt;byte&gt;</c> / <c>Span&lt;byte&gt;</c> overloads so callers
/// on any supported .NET target (net7/8/9/10) can pass a byte array directly.
/// </summary>
public class EndianOffsetAndByteArrayTests
{
    // Prefix pad (10 bytes), value payload, trailing pad (10 bytes). The payload offset
    // is always 10; reading/writing through offset must not touch the surrounding pads.
    private const int PAD = 10;
    private const byte SENTINEL = 0xA5;

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

    // Builds a PAD + payload + PAD buffer for offset testing.
    private static byte[] Padded(byte[] payload)
    {
        byte[] buf = new byte[PAD + payload.Length + PAD];
        for (int i = 0; i < buf.Length; i++) buf[i] = SENTINEL;
        Array.Copy(payload, 0, buf, PAD, payload.Length);
        return buf;
    }

    private static byte[] Sentineled(int size)
    {
        byte[] buf = new byte[size];
        for (int i = 0; i < size; i++) buf[i] = SENTINEL;
        return buf;
    }

    private static void AssertPadsUntouched(byte[] buf, int payloadLen)
    {
        for (int i = 0; i < PAD; i++) buf[i].Should().Be(SENTINEL, "prefix pad byte {0} should be untouched", i);
        for (int i = PAD + payloadLen; i < buf.Length; i++)
            buf[i].Should().Be(SENTINEL, "suffix pad byte {0} should be untouched", i);
    }

    // =======================================================================
    // ReadOnlySpan<byte> ctor with offset: pad + payload + pad, offset = PAD.
    // =======================================================================

    [Fact]
    public void UInt16Be_SpanOffsetCtor_ReadsAtOffset()
    {
        byte[] buf = Padded(BE16);
        UInt16Be v = new(buf.AsSpan(), offset: PAD, isBigEndian: true);
        ((ushort)v).Should().Be(U16);
    }

    [Fact]
    public void UInt16Le_SpanOffsetCtor_ReadsAtOffset_BothOrders()
    {
        byte[] be = Padded(BE16);
        byte[] le = Padded(LE16);
        ((ushort)new UInt16Le(be.AsSpan(), PAD, isBigEndian: true)).Should().Be(U16);
        ((ushort)new UInt16Le(le.AsSpan(), PAD, isBigEndian: false)).Should().Be(U16);
    }

    [Fact]
    public void Int16Be_SpanOffsetCtor_ReadsAtOffset() =>
        ((short)new Int16Be(Padded(BE16).AsSpan(), PAD)).Should().Be(I16);

    [Fact]
    public void Int16Le_SpanOffsetCtor_ReadsAtOffset() =>
        ((short)new Int16Le(Padded(LE16).AsSpan(), PAD)).Should().Be(I16);

    [Fact]
    public void UInt32Be_SpanOffsetCtor_ReadsAtOffset() =>
        ((uint)new UInt32Be(Padded(BE32).AsSpan(), PAD)).Should().Be(U32);

    [Fact]
    public void UInt32Le_SpanOffsetCtor_ReadsAtOffset() =>
        ((uint)new UInt32Le(Padded(LE32).AsSpan(), PAD)).Should().Be(U32);

    [Fact]
    public void Int32Be_SpanOffsetCtor_ReadsAtOffset() =>
        ((int)new Int32Be(Padded(BE32).AsSpan(), PAD)).Should().Be(I32);

    [Fact]
    public void Int32Le_SpanOffsetCtor_ReadsAtOffset() =>
        ((int)new Int32Le(Padded(LE32).AsSpan(), PAD)).Should().Be(I32);

    [Fact]
    public void UInt64Be_SpanOffsetCtor_ReadsAtOffset() =>
        ((ulong)new UInt64Be(Padded(BE64).AsSpan(), PAD)).Should().Be(U64);

    [Fact]
    public void UInt64Le_SpanOffsetCtor_ReadsAtOffset() =>
        ((ulong)new UInt64Le(Padded(LE64).AsSpan(), PAD)).Should().Be(U64);

    [Fact]
    public void Int64Be_SpanOffsetCtor_ReadsAtOffset() =>
        ((long)new Int64Be(Padded(BE64).AsSpan(), PAD)).Should().Be(I64);

    [Fact]
    public void Int64Le_SpanOffsetCtor_ReadsAtOffset() =>
        ((long)new Int64Le(Padded(LE64).AsSpan(), PAD)).Should().Be(I64);

    [Fact]
    public void UInt128Be_SpanOffsetCtor_ReadsAtOffset()
    {
        UInt128Be v = new(Padded(BE128).AsSpan(), PAD);
        byte[] round = new byte[16];
        v.WriteTo(round);
        round.Should().Equal(BE128);
    }

    [Fact]
    public void UInt128Le_SpanOffsetCtor_ReadsAtOffset()
    {
        UInt128Le v = new(Padded(LE128).AsSpan(), PAD);
        byte[] round = new byte[16];
        v.WriteTo(round);
        round.Should().Equal(LE128);
    }

    [Fact]
    public void Int128Be_SpanOffsetCtor_ReadsAtOffset()
    {
        Int128Be v = new(Padded(BE128).AsSpan(), PAD);
        byte[] round = new byte[16];
        v.WriteTo(round);
        round.Should().Equal(BE128);
    }

    [Fact]
    public void Int128Le_SpanOffsetCtor_ReadsAtOffset()
    {
        Int128Le v = new(Padded(LE128).AsSpan(), PAD);
        byte[] round = new byte[16];
        v.WriteTo(round);
        round.Should().Equal(LE128);
    }

    [Fact]
    public void UInt256Be_SpanOffsetCtor_ReadsAtOffset()
    {
        UInt256Be v = new(Padded(BE256).AsSpan(), PAD);
        byte[] round = new byte[32];
        v.WriteTo(round);
        round.Should().Equal(BE256);
    }

    [Fact]
    public void UInt256Le_SpanOffsetCtor_ReadsAtOffset()
    {
        UInt256Le v = new(Padded(LE256).AsSpan(), PAD);
        byte[] round = new byte[32];
        v.WriteTo(round);
        round.Should().Equal(LE256);
    }

    [Fact]
    public void Int256Be_SpanOffsetCtor_ReadsAtOffset()
    {
        Int256Be v = new(Padded(BE256).AsSpan(), PAD);
        byte[] round = new byte[32];
        v.WriteTo(round);
        round.Should().Equal(BE256);
    }

    [Fact]
    public void Int256Le_SpanOffsetCtor_ReadsAtOffset()
    {
        Int256Le v = new(Padded(LE256).AsSpan(), PAD);
        byte[] round = new byte[32];
        v.WriteTo(round);
        round.Should().Equal(LE256);
    }

    // =======================================================================
    // byte[] constructors (no .AsSpan() required): pass a byte[] directly.
    // =======================================================================

    [Fact]
    public void UInt16Be_ByteArrayCtor_NoOffset() =>
        ((ushort)new UInt16Be(BE16)).Should().Be(U16);

    [Fact]
    public void UInt16Le_ByteArrayCtor_NoOffset() =>
        ((ushort)new UInt16Le(LE16)).Should().Be(U16);

    [Fact]
    public void Int16Be_ByteArrayCtor_NoOffset() =>
        ((short)new Int16Be(BE16)).Should().Be(I16);

    [Fact]
    public void Int16Le_ByteArrayCtor_NoOffset() =>
        ((short)new Int16Le(LE16)).Should().Be(I16);

    [Fact]
    public void UInt32Be_ByteArrayCtor_NoOffset() =>
        ((uint)new UInt32Be(BE32)).Should().Be(U32);

    [Fact]
    public void UInt32Le_ByteArrayCtor_NoOffset() =>
        ((uint)new UInt32Le(LE32)).Should().Be(U32);

    [Fact]
    public void Int32Be_ByteArrayCtor_NoOffset() =>
        ((int)new Int32Be(BE32)).Should().Be(I32);

    [Fact]
    public void Int32Le_ByteArrayCtor_NoOffset() =>
        ((int)new Int32Le(LE32)).Should().Be(I32);

    [Fact]
    public void UInt64Be_ByteArrayCtor_NoOffset() =>
        ((ulong)new UInt64Be(BE64)).Should().Be(U64);

    [Fact]
    public void UInt64Le_ByteArrayCtor_NoOffset() =>
        ((ulong)new UInt64Le(LE64)).Should().Be(U64);

    [Fact]
    public void Int64Be_ByteArrayCtor_NoOffset() =>
        ((long)new Int64Be(BE64)).Should().Be(I64);

    [Fact]
    public void Int64Le_ByteArrayCtor_NoOffset() =>
        ((long)new Int64Le(LE64)).Should().Be(I64);

    [Fact]
    public void UInt16Be_ByteArrayCtor_ReverseOrder() =>
        ((ushort)new UInt16Be(LE16, isBigEndian: false)).Should().Be(U16);

    [Fact]
    public void UInt16Le_ByteArrayCtor_ReverseOrder() =>
        ((ushort)new UInt16Le(BE16, isBigEndian: true)).Should().Be(U16);

    [Fact]
    public void UInt32Be_ByteArrayCtor_ReverseOrder() =>
        ((uint)new UInt32Be(LE32, isBigEndian: false)).Should().Be(U32);

    [Fact]
    public void UInt32Le_ByteArrayCtor_ReverseOrder() =>
        ((uint)new UInt32Le(BE32, isBigEndian: true)).Should().Be(U32);

    [Fact]
    public void UInt64Be_ByteArrayCtor_ReverseOrder() =>
        ((ulong)new UInt64Be(LE64, isBigEndian: false)).Should().Be(U64);

    [Fact]
    public void UInt64Le_ByteArrayCtor_ReverseOrder() =>
        ((ulong)new UInt64Le(BE64, isBigEndian: true)).Should().Be(U64);

    [Fact]
    public void UInt128Be_ByteArrayCtor_ReverseOrder()
    {
        UInt128Be v = new(LE128, isBigEndian: false);
        byte[] round = new byte[16];
        v.WriteTo(round);
        round.Should().Equal(BE128);
    }

    [Fact]
    public void UInt256Le_ByteArrayCtor_ReverseOrder()
    {
        UInt256Le v = new(BE256, isBigEndian: true);
        byte[] round = new byte[32];
        v.WriteTo(round);
        round.Should().Equal(LE256);
    }

    // ---------- byte[] ctor with offset ----------

    [Fact]
    public void UInt32Be_ByteArrayOffsetCtor_ReadsAtOffset() =>
        ((uint)new UInt32Be(Padded(BE32), PAD)).Should().Be(U32);

    [Fact]
    public void UInt32Le_ByteArrayOffsetCtor_ReadsAtOffset() =>
        ((uint)new UInt32Le(Padded(LE32), PAD)).Should().Be(U32);

    [Fact]
    public void UInt64Be_ByteArrayOffsetCtor_ReadsAtOffset_ReverseOrder() =>
        ((ulong)new UInt64Be(Padded(LE64), PAD, isBigEndian: false)).Should().Be(U64);

    [Fact]
    public void UInt64Le_ByteArrayOffsetCtor_ReadsAtOffset_ReverseOrder() =>
        ((ulong)new UInt64Le(Padded(BE64), PAD, isBigEndian: true)).Should().Be(U64);

    [Fact]
    public void UInt128Be_ByteArrayOffsetCtor_ReadsAtOffset()
    {
        UInt128Be v = new(Padded(BE128), PAD);
        byte[] round = new byte[16];
        v.WriteTo(round);
        round.Should().Equal(BE128);
    }

    [Fact]
    public void Int128Le_ByteArrayOffsetCtor_ReadsAtOffset()
    {
        Int128Le v = new(Padded(LE128), PAD);
        byte[] round = new byte[16];
        v.WriteTo(round);
        round.Should().Equal(LE128);
    }

    [Fact]
    public void UInt256Be_ByteArrayOffsetCtor_ReadsAtOffset()
    {
        UInt256Be v = new(Padded(BE256), PAD);
        byte[] round = new byte[32];
        v.WriteTo(round);
        round.Should().Equal(BE256);
    }

    [Fact]
    public void Int256Le_ByteArrayOffsetCtor_ReadsAtOffset()
    {
        Int256Le v = new(Padded(LE256), PAD);
        byte[] round = new byte[32];
        v.WriteTo(round);
        round.Should().Equal(LE256);
    }

    // =======================================================================
    // WriteTo(Span<byte>, int offset, bool isBigEndian): writes at offset into
    // a padded buffer and leaves surrounding bytes untouched.
    // =======================================================================

    [Fact]
    public void UInt16Be_WriteTo_Span_WithOffset_PadsUntouched()
    {
        UInt16Be v = new(U16);
        byte[] buf = Sentineled(PAD + 2 + PAD);
        v.WriteTo(buf.AsSpan(), offset: PAD);
        for (int i = 0; i < 2; i++) buf[PAD + i].Should().Be(BE16[i]);
        AssertPadsUntouched(buf, 2);
    }

    [Fact]
    public void UInt16Le_WriteTo_Span_WithOffset_ReverseOrder_PadsUntouched()
    {
        UInt16Le v = new(U16);
        byte[] buf = Sentineled(PAD + 2 + PAD);
        v.WriteTo(buf.AsSpan(), offset: PAD, isBigEndian: true);
        for (int i = 0; i < 2; i++) buf[PAD + i].Should().Be(BE16[i]);
        AssertPadsUntouched(buf, 2);
    }

    [Fact]
    public void UInt32Be_WriteTo_Span_WithOffset()
    {
        UInt32Be v = new(U32);
        byte[] buf = Sentineled(PAD + 4 + PAD);
        v.WriteTo(buf.AsSpan(), offset: PAD);
        for (int i = 0; i < 4; i++) buf[PAD + i].Should().Be(BE32[i]);
        AssertPadsUntouched(buf, 4);
    }

    [Fact]
    public void UInt32Le_WriteTo_Span_WithOffset_ReverseOrder()
    {
        UInt32Le v = new(U32);
        byte[] buf = Sentineled(PAD + 4 + PAD);
        v.WriteTo(buf.AsSpan(), offset: PAD, isBigEndian: true);
        for (int i = 0; i < 4; i++) buf[PAD + i].Should().Be(BE32[i]);
        AssertPadsUntouched(buf, 4);
    }

    [Fact]
    public void UInt64Be_WriteTo_Span_WithOffset_ReverseOrder()
    {
        UInt64Be v = new(U64);
        byte[] buf = Sentineled(PAD + 8 + PAD);
        v.WriteTo(buf.AsSpan(), offset: PAD, isBigEndian: false);
        for (int i = 0; i < 8; i++) buf[PAD + i].Should().Be(LE64[i]);
        AssertPadsUntouched(buf, 8);
    }

    [Fact]
    public void Int64Le_WriteTo_Span_WithOffset_ReverseOrder()
    {
        Int64Le v = new(I64);
        byte[] buf = Sentineled(PAD + 8 + PAD);
        v.WriteTo(buf.AsSpan(), offset: PAD, isBigEndian: true);
        for (int i = 0; i < 8; i++) buf[PAD + i].Should().Be(BE64[i]);
        AssertPadsUntouched(buf, 8);
    }

    [Fact]
    public void UInt128Be_WriteTo_Span_WithOffset_ReverseOrder()
    {
        UInt128Be v = new(BE128.AsSpan());
        byte[] buf = Sentineled(PAD + 16 + PAD);
        v.WriteTo(buf.AsSpan(), offset: PAD, isBigEndian: false);
        for (int i = 0; i < 16; i++) buf[PAD + i].Should().Be(LE128[i]);
        AssertPadsUntouched(buf, 16);
    }

    [Fact]
    public void Int128Le_WriteTo_Span_WithOffset_ReverseOrder()
    {
        Int128Le v = new(LE128.AsSpan());
        byte[] buf = Sentineled(PAD + 16 + PAD);
        v.WriteTo(buf.AsSpan(), offset: PAD, isBigEndian: true);
        for (int i = 0; i < 16; i++) buf[PAD + i].Should().Be(BE128[i]);
        AssertPadsUntouched(buf, 16);
    }

    [Fact]
    public void UInt256Be_WriteTo_Span_WithOffset_ReverseOrder()
    {
        UInt256Be v = new(BE256.AsSpan());
        byte[] buf = Sentineled(PAD + 32 + PAD);
        v.WriteTo(buf.AsSpan(), offset: PAD, isBigEndian: false);
        for (int i = 0; i < 32; i++) buf[PAD + i].Should().Be(LE256[i]);
        AssertPadsUntouched(buf, 32);
    }

    [Fact]
    public void Int256Le_WriteTo_Span_WithOffset_ReverseOrder()
    {
        Int256Le v = new(LE256.AsSpan());
        byte[] buf = Sentineled(PAD + 32 + PAD);
        v.WriteTo(buf.AsSpan(), offset: PAD, isBigEndian: true);
        for (int i = 0; i < 32; i++) buf[PAD + i].Should().Be(BE256[i]);
        AssertPadsUntouched(buf, 32);
    }

    // =======================================================================
    // WriteTo(byte[], int offset, bool isBigEndian): the new byte[] overload
    // that replaces the deprecated ToBytes(byte[], ...).
    // =======================================================================

    [Fact]
    public void UInt16Be_WriteTo_ByteArray_WithOffset()
    {
        UInt16Be v = new(U16);
        byte[] buf = Sentineled(PAD + 2 + PAD);
        v.WriteTo(buf, offset: PAD);
        for (int i = 0; i < 2; i++) buf[PAD + i].Should().Be(BE16[i]);
        AssertPadsUntouched(buf, 2);
    }

    [Fact]
    public void UInt32Le_WriteTo_ByteArray_WithOffset()
    {
        UInt32Le v = new(U32);
        byte[] buf = Sentineled(PAD + 4 + PAD);
        v.WriteTo(buf, offset: PAD);
        for (int i = 0; i < 4; i++) buf[PAD + i].Should().Be(LE32[i]);
        AssertPadsUntouched(buf, 4);
    }

    [Fact]
    public void Int64Be_WriteTo_ByteArray_WithOffset_ReverseOrder()
    {
        Int64Be v = new(I64);
        byte[] buf = Sentineled(PAD + 8 + PAD);
        v.WriteTo(buf, offset: PAD, isBigEndian: false);
        for (int i = 0; i < 8; i++) buf[PAD + i].Should().Be(LE64[i]);
        AssertPadsUntouched(buf, 8);
    }

    [Fact]
    public void UInt128Le_WriteTo_ByteArray_WithOffset()
    {
        UInt128Le v = new(LE128.AsSpan());
        byte[] buf = Sentineled(PAD + 16 + PAD);
        v.WriteTo(buf, offset: PAD);
        for (int i = 0; i < 16; i++) buf[PAD + i].Should().Be(LE128[i]);
        AssertPadsUntouched(buf, 16);
    }

    [Fact]
    public void UInt256Be_WriteTo_ByteArray_WithOffset()
    {
        UInt256Be v = new(BE256.AsSpan());
        byte[] buf = Sentineled(PAD + 32 + PAD);
        v.WriteTo(buf, offset: PAD);
        for (int i = 0; i < 32; i++) buf[PAD + i].Should().Be(BE256[i]);
        AssertPadsUntouched(buf, 32);
    }

    // =======================================================================
    // TryWriteTo(Span<byte>, int offset, bool isBigEndian): returns false when
    // offset + N exceeds the destination length.
    // =======================================================================

    [Fact]
    public void UInt16Be_TryWriteTo_Span_WithOffset_Success()
    {
        UInt16Be v = new(U16);
        byte[] buf = Sentineled(PAD + 2);
        v.TryWriteTo(buf.AsSpan(), offset: PAD).Should().BeTrue();
        for (int i = 0; i < 2; i++) buf[PAD + i].Should().Be(BE16[i]);
    }

    [Fact]
    public void UInt16Be_TryWriteTo_Span_WithOffset_TooShort_ReturnsFalse()
    {
        UInt16Be v = new(U16);
        byte[] buf = new byte[PAD + 1]; // only 1 byte at offset — short by one
        v.TryWriteTo(buf.AsSpan(), offset: PAD).Should().BeFalse();
    }

    [Fact]
    public void UInt32Le_TryWriteTo_ByteArray_WithOffset_TooShort_ReturnsFalse()
    {
        UInt32Le v = new(U32);
        byte[] buf = new byte[PAD + 3];
        v.TryWriteTo(buf, offset: PAD).Should().BeFalse();
    }

    [Fact]
    public void UInt64Be_TryWriteTo_Span_WithOffset_ExactFit_Success()
    {
        UInt64Be v = new(U64);
        byte[] buf = new byte[PAD + 8];
        v.TryWriteTo(buf.AsSpan(), offset: PAD).Should().BeTrue();
        for (int i = 0; i < 8; i++) buf[PAD + i].Should().Be(BE64[i]);
    }

    [Fact]
    public void UInt128Be_TryWriteTo_Span_WithOffset_TooShort_ReturnsFalse()
    {
        UInt128Be v = new(BE128.AsSpan());
        byte[] buf = new byte[PAD + 15];
        v.TryWriteTo(buf.AsSpan(), offset: PAD).Should().BeFalse();
    }

    [Fact]
    public void UInt256Be_TryWriteTo_ByteArray_WithOffset_TooShort_ReturnsFalse()
    {
        UInt256Be v = new(BE256.AsSpan());
        byte[] buf = new byte[PAD + 31];
        v.TryWriteTo(buf, offset: PAD).Should().BeFalse();
    }

    [Fact]
    public void Int256Le_TryWriteTo_ByteArray_WithOffset_Success()
    {
        Int256Le v = new(LE256.AsSpan());
        byte[] buf = Sentineled(PAD + 32 + PAD);
        v.TryWriteTo(buf, offset: PAD).Should().BeTrue();
        for (int i = 0; i < 32; i++) buf[PAD + i].Should().Be(LE256[i]);
        AssertPadsUntouched(buf, 32);
    }

    // =======================================================================
    // WriteTo(Span<byte>, int offset, ...) throws on too-short destination.
    // =======================================================================

    [Fact]
    public void UInt16Be_WriteTo_Span_WithOffset_TooShort_Throws()
    {
        UInt16Be v = new(U16);
        byte[] buf = new byte[PAD + 1];
        Action act = () => v.WriteTo(buf.AsSpan(), offset: PAD);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UInt32Le_WriteTo_ByteArray_WithOffset_TooShort_Throws()
    {
        UInt32Le v = new(U32);
        byte[] buf = new byte[PAD + 3];
        Action act = () => v.WriteTo(buf, offset: PAD);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UInt128Be_WriteTo_Span_WithOffset_TooShort_Throws()
    {
        UInt128Be v = new(BE128.AsSpan());
        byte[] buf = new byte[PAD + 15];
        Action act = () => v.WriteTo(buf.AsSpan(), offset: PAD);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Int256Le_WriteTo_ByteArray_WithOffset_TooShort_Throws()
    {
        Int256Le v = new(LE256.AsSpan());
        byte[] buf = new byte[PAD + 31];
        Action act = () => v.WriteTo(buf, offset: PAD);
        act.Should().Throw<ArgumentException>();
    }

    // =======================================================================
    // Offset ctor throws on inadequate length at the given offset.
    // =======================================================================

    [Fact]
    public void UInt16Be_SpanOffsetCtor_TooShort_Throws()
    {
        byte[] buf = new byte[PAD + 1];
        Action act = () => { _ = new UInt16Be(buf.AsSpan(), offset: PAD); };
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UInt32Le_ByteArrayOffsetCtor_TooShort_Throws()
    {
        byte[] buf = new byte[PAD + 3];
        Action act = () => { _ = new UInt32Le(buf, PAD); };
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UInt64Be_SpanOffsetCtor_TooShort_Throws()
    {
        byte[] buf = new byte[PAD + 7];
        Action act = () => { _ = new UInt64Be(buf.AsSpan(), PAD); };
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UInt128Le_ByteArrayOffsetCtor_TooShort_Throws()
    {
        byte[] buf = new byte[PAD + 15];
        Action act = () => { _ = new UInt128Le(buf, PAD); };
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UInt256Be_SpanOffsetCtor_TooShort_Throws()
    {
        byte[] buf = new byte[PAD + 31];
        Action act = () => { _ = new UInt256Be(buf.AsSpan(), PAD); };
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Int256Le_ByteArrayOffsetCtor_TooShort_Throws()
    {
        byte[] buf = new byte[PAD + 31];
        Action act = () => { _ = new Int256Le(buf, PAD); };
        act.Should().Throw<ArgumentException>();
    }

    // =======================================================================
    // ReadFrom(ReadOnlySpan<byte>, int offset, bool) and ReadFrom(byte[], int, bool)
    // =======================================================================

    [Fact]
    public void UInt16Be_ReadFrom_Span_WithOffset() =>
        ((ushort)UInt16Be.ReadFrom(Padded(BE16).AsSpan(), offset: PAD)).Should().Be(U16);

    [Fact]
    public void UInt16Le_ReadFrom_ByteArray_WithOffset() =>
        ((ushort)UInt16Le.ReadFrom(Padded(LE16), offset: PAD)).Should().Be(U16);

    [Fact]
    public void Int16Be_ReadFrom_Span_WithOffset_ReverseOrder() =>
        ((short)Int16Be.ReadFrom(Padded(LE16).AsSpan(), PAD, isBigEndian: false)).Should().Be(I16);

    [Fact]
    public void Int16Le_ReadFrom_ByteArray_WithOffset_ReverseOrder() =>
        ((short)Int16Le.ReadFrom(Padded(BE16), PAD, isBigEndian: true)).Should().Be(I16);

    [Fact]
    public void UInt32Be_ReadFrom_Span_WithOffset() =>
        ((uint)UInt32Be.ReadFrom(Padded(BE32).AsSpan(), PAD)).Should().Be(U32);

    [Fact]
    public void UInt32Le_ReadFrom_ByteArray_WithOffset() =>
        ((uint)UInt32Le.ReadFrom(Padded(LE32), PAD)).Should().Be(U32);

    [Fact]
    public void Int32Be_ReadFrom_Span_WithOffset_ReverseOrder() =>
        ((int)Int32Be.ReadFrom(Padded(LE32).AsSpan(), PAD, isBigEndian: false)).Should().Be(I32);

    [Fact]
    public void Int32Le_ReadFrom_ByteArray_WithOffset() =>
        ((int)Int32Le.ReadFrom(Padded(LE32), PAD)).Should().Be(I32);

    [Fact]
    public void UInt64Be_ReadFrom_Span_WithOffset_ReverseOrder() =>
        ((ulong)UInt64Be.ReadFrom(Padded(LE64).AsSpan(), PAD, isBigEndian: false)).Should().Be(U64);

    [Fact]
    public void UInt64Le_ReadFrom_ByteArray_WithOffset_ReverseOrder() =>
        ((ulong)UInt64Le.ReadFrom(Padded(BE64), PAD, isBigEndian: true)).Should().Be(U64);

    [Fact]
    public void Int64Be_ReadFrom_Span_WithOffset() =>
        ((long)Int64Be.ReadFrom(Padded(BE64).AsSpan(), PAD)).Should().Be(I64);

    [Fact]
    public void Int64Le_ReadFrom_ByteArray_WithOffset() =>
        ((long)Int64Le.ReadFrom(Padded(LE64), PAD)).Should().Be(I64);

    [Fact]
    public void UInt128Be_ReadFrom_ByteArray_WithOffset()
    {
        UInt128Be v = UInt128Be.ReadFrom(Padded(BE128), PAD);
        byte[] round = new byte[16];
        v.WriteTo(round);
        round.Should().Equal(BE128);
    }

    [Fact]
    public void UInt128Le_ReadFrom_Span_WithOffset_ReverseOrder()
    {
        UInt128Le v = UInt128Le.ReadFrom(Padded(BE128).AsSpan(), PAD, isBigEndian: true);
        byte[] round = new byte[16];
        v.WriteTo(round);
        round.Should().Equal(LE128);
    }

    [Fact]
    public void Int128Be_ReadFrom_ByteArray_WithOffset()
    {
        Int128Be v = Int128Be.ReadFrom(Padded(BE128), PAD);
        byte[] round = new byte[16];
        v.WriteTo(round);
        round.Should().Equal(BE128);
    }

    [Fact]
    public void Int128Le_ReadFrom_Span_WithOffset()
    {
        Int128Le v = Int128Le.ReadFrom(Padded(LE128).AsSpan(), PAD);
        byte[] round = new byte[16];
        v.WriteTo(round);
        round.Should().Equal(LE128);
    }

    [Fact]
    public void UInt256Be_ReadFrom_ByteArray_WithOffset()
    {
        UInt256Be v = UInt256Be.ReadFrom(Padded(BE256), PAD);
        byte[] round = new byte[32];
        v.WriteTo(round);
        round.Should().Equal(BE256);
    }

    [Fact]
    public void UInt256Le_ReadFrom_Span_WithOffset()
    {
        UInt256Le v = UInt256Le.ReadFrom(Padded(LE256).AsSpan(), PAD);
        byte[] round = new byte[32];
        v.WriteTo(round);
        round.Should().Equal(LE256);
    }

    [Fact]
    public void Int256Be_ReadFrom_ByteArray_WithOffset_ReverseOrder()
    {
        Int256Be v = Int256Be.ReadFrom(Padded(LE256), PAD, isBigEndian: false);
        byte[] round = new byte[32];
        v.WriteTo(round);
        round.Should().Equal(BE256);
    }

    [Fact]
    public void Int256Le_ReadFrom_Span_WithOffset_ReverseOrder()
    {
        Int256Le v = Int256Le.ReadFrom(Padded(BE256).AsSpan(), PAD, isBigEndian: true);
        byte[] round = new byte[32];
        v.WriteTo(round);
        round.Should().Equal(LE256);
    }

    // =======================================================================
    // Round-trip: ctor-at-offset + WriteTo-at-offset preserves bytes
    // regardless of source/destination endianness and offset.
    // =======================================================================

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void UInt32Be_OffsetRoundTrip(bool ctorIsBigEndian, bool exportIsBigEndian)
    {
        byte[] src = Padded(ctorIsBigEndian ? BE32 : LE32);
        UInt32Be v = new(src.AsSpan(), offset: PAD, isBigEndian: ctorIsBigEndian);
        byte[] dst = Sentineled(PAD + 4 + PAD);
        v.WriteTo(dst, offset: PAD, isBigEndian: exportIsBigEndian);
        byte[] expected = exportIsBigEndian ? BE32 : LE32;
        for (int i = 0; i < 4; i++) dst[PAD + i].Should().Be(expected[i]);
        AssertPadsUntouched(dst, 4);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void UInt256Le_OffsetRoundTrip(bool ctorIsBigEndian, bool exportIsBigEndian)
    {
        byte[] src = Padded(ctorIsBigEndian ? BE256 : LE256);
        UInt256Le v = new(src, offset: PAD, isBigEndian: ctorIsBigEndian);
        byte[] dst = Sentineled(PAD + 32 + PAD);
        v.WriteTo(dst, offset: PAD, isBigEndian: exportIsBigEndian);
        byte[] expected = exportIsBigEndian ? BE256 : LE256;
        for (int i = 0; i < 32; i++) dst[PAD + i].Should().Be(expected[i]);
        AssertPadsUntouched(dst, 32);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void Int128Be_OffsetRoundTrip(bool ctorIsBigEndian, bool exportIsBigEndian)
    {
        byte[] src = Padded(ctorIsBigEndian ? BE128 : LE128);
        Int128Be v = new(src.AsSpan(), offset: PAD, isBigEndian: ctorIsBigEndian);
        byte[] dst = Sentineled(PAD + 16 + PAD);
        v.WriteTo(dst.AsSpan(), offset: PAD, isBigEndian: exportIsBigEndian);
        byte[] expected = exportIsBigEndian ? BE128 : LE128;
        for (int i = 0; i < 16; i++) dst[PAD + i].Should().Be(expected[i]);
        AssertPadsUntouched(dst, 16);
    }
}
