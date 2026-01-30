using FluentAssertions;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Test enum for EnhancedEnum tests.
/// </summary>
public sealed record TestEnhancedEnum : EnhancedEnum<TestEnhancedEnum.TestKind>
{
    /// <summary>
    /// Enum representing the variant kinds.
    /// </summary>
    public enum TestKind
    {
        Int,        // int payload
        Float,      // float payload
        Tuple,      // (uint, int) payload
        NoValue,    // no payload
        Byte,       // byte payload
        UInt16,     // ushort payload
        UInt32,     // uint payload
        UInt32UInt32, // (uint, uint) payload
        UInt32Int32,  // (uint, int) payload
        String,     // string payload
        Reference,  // object reference payload
        Boxed,      // DateTime (boxed) payload
    }

    static TestEnhancedEnum()
    {
        // Register expected kinds and their associated CLR types
        RegisterKindType(TestKind.Int, typeof(int));
        RegisterKindType(TestKind.Float, typeof(float));
        RegisterKindType(TestKind.Tuple, typeof((uint address, int count)));
        RegisterKindType(TestKind.NoValue, null);
        RegisterKindType(TestKind.Byte, typeof(byte));
        RegisterKindType(TestKind.UInt16, typeof(ushort));
        RegisterKindType(TestKind.UInt32, typeof(uint));
        RegisterKindType(TestKind.UInt32UInt32, typeof((uint a, uint b)));
        RegisterKindType(TestKind.UInt32Int32, typeof((uint a, int b)));
        RegisterKindType(TestKind.String, typeof(string));
        RegisterKindType(TestKind.Reference, typeof(object));
        RegisterKindType(TestKind.Boxed, typeof(DateTime));

        // Register EnhancedEnumFlex payload kinds
        EnhancedEnumFlex<TestKind>.RegisterKindPayloadKind(TestKind.NoValue, EnhancedEnumFlex<TestKind>.PayloadKind.None);
        EnhancedEnumFlex<TestKind>.RegisterKindPayloadKind(TestKind.Byte, EnhancedEnumFlex<TestKind>.PayloadKind.Byte);
        EnhancedEnumFlex<TestKind>.RegisterKindPayloadKind(TestKind.UInt16, EnhancedEnumFlex<TestKind>.PayloadKind.UInt16);
        EnhancedEnumFlex<TestKind>.RegisterKindPayloadKind(TestKind.UInt32, EnhancedEnumFlex<TestKind>.PayloadKind.UInt32);
        EnhancedEnumFlex<TestKind>.RegisterKindPayloadKind(TestKind.UInt32UInt32, EnhancedEnumFlex<TestKind>.PayloadKind.UInt32_UInt32);
        EnhancedEnumFlex<TestKind>.RegisterKindPayloadKind(TestKind.UInt32Int32, EnhancedEnumFlex<TestKind>.PayloadKind.UInt32_Int32);
        EnhancedEnumFlex<TestKind>.RegisterKindPayloadKind(TestKind.String, EnhancedEnumFlex<TestKind>.PayloadKind.String);
        EnhancedEnumFlex<TestKind>.RegisterKindPayloadKind(TestKind.Reference, EnhancedEnumFlex<TestKind>.PayloadKind.Reference);
        EnhancedEnumFlex<TestKind>.RegisterKindPayloadKind(TestKind.Boxed, EnhancedEnumFlex<TestKind>.PayloadKind.BoxedValue);
    }

    public TestEnhancedEnum(TestKind kind, object? value = null) : base(kind, value) { }
}

/// <summary>
/// Unit tests for the EnhancedEnum and EnhancedEnumFlex types.
/// </summary>
public class EnhancedEnumTests
{
    private static void EnsureRegistered()
    {
        // Force TestEnhancedEnum static constructor to run
        var _ = new TestEnhancedEnum(TestEnhancedEnum.TestKind.Int, 0);
    }

    private sealed class RefPayload
    {
        public int Id { get; }
        public RefPayload(int id) => Id = id;
    }

    private static EnhancedEnumFlex<TestEnhancedEnum.TestKind> MakeFlexReference(
        TestEnhancedEnum.TestKind kind, RefPayload payload)
    {
        return EnhancedEnumFlex<TestEnhancedEnum.TestKind>.CreateReference(kind, payload);
    }

    #region EnhancedEnum<TEnum> Tests

    /// <summary>
    /// Tests that TryValueAs succeeds for int values.
    /// </summary>
    [Fact]
    public void TryValueAs_Int_Succeeds()
    {
        var e = new TestEnhancedEnum(TestEnhancedEnum.TestKind.Int, 42);

        e.TryValueAs(out int v).Should().BeTrue();
        v.Should().Be(42);

        // ValueAs should also return the value
        e.ValueAs<int>().Should().Be(42);
    }

    /// <summary>
    /// Tests that TryValueAs succeeds for float values.
    /// </summary>
    [Fact]
    public void TryValueAs_Float_Succeeds()
    {
        var e = new TestEnhancedEnum(TestEnhancedEnum.TestKind.Float, 3.14f);

        e.TryValueAs(out float f).Should().BeTrue();
        f.Should().Be(3.14f);

        e.ValueAs<float>().Should().Be(3.14f);
    }

    /// <summary>
    /// Tests that TryValueAs succeeds for tuple values.
    /// </summary>
    [Fact]
    public void TryValueAs_Tuple_Succeeds()
    {
        var tuple = ((uint)0x00000204, 1);
        var e = new TestEnhancedEnum(TestEnhancedEnum.TestKind.Tuple, tuple);

        e.TryValueAs(out (uint address, int count) result).Should().BeTrue();
        result.address.Should().Be(tuple.Item1);
        result.count.Should().Be(tuple.Item2);

        var v = e.ValueAs<(uint, int)>();
        v.Item1.Should().Be(tuple.Item1);
        v.Item2.Should().Be(tuple.Item2);
    }

    /// <summary>
    /// Tests that TryValueAs handles NoValue correctly.
    /// </summary>
    [Fact]
    public void TryValueAs_NoValue_NullableAcceptsNull_ReferenceDoesNot()
    {
        var e = new TestEnhancedEnum(TestEnhancedEnum.TestKind.NoValue);

        // Reference type request should fail for null stored value
        e.TryValueAs(out object? o).Should().BeFalse();
        o.Should().BeNull();

        // Nullable value type should succeed and produce default
        e.TryValueAs(out int? ni).Should().BeTrue();
        ni.Should().BeNull();
    }

    /// <summary>
    /// Tests that ValueAs throws for invalid cast.
    /// </summary>
    [Fact]
    public void ValueAs_InvalidCast_Throws()
    {
        var e = new TestEnhancedEnum(TestEnhancedEnum.TestKind.Int, 42);

        var act = () => e.ValueAs<float>();
        act.Should().Throw<InvalidCastException>();
    }

    /// <summary>
    /// Tests that RegisterKindType prevents incompatible assignments.
    /// </summary>
    [Fact]
    public void RegisterKindType_PreventsIncompatibleAssignments()
    {
        // First registration registers int implicitly
        var e1 = new TestEnhancedEnum(TestEnhancedEnum.TestKind.Int, 42);
        e1.TryValueAs(out int v).Should().BeTrue();
        v.Should().Be(42);

        // Attempt with incompatible type must throw
        var act = () => new TestEnhancedEnum(TestEnhancedEnum.TestKind.Int, "bad");
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that Deconstruct works correctly.
    /// </summary>
    [Fact]
    public void Deconstruct_Works()
    {
        var tuple = ((uint)0x40000000, 3);
        var e = new TestEnhancedEnum(TestEnhancedEnum.TestKind.Tuple, tuple);

        var (kind, value) = e;
        kind.Should().Be(TestEnhancedEnum.TestKind.Tuple);
        value.Should().NotBeNull();
        value.Should().BeOfType<ValueTuple<uint, int>>();

        var bp = ((uint, int))value!;
        bp.Item1.Should().Be(tuple.Item1);
        bp.Item2.Should().Be(tuple.Item2);
    }

    /// <summary>
    /// Tests that Is method works correctly.
    /// </summary>
    [Fact]
    public void Is_ChecksKindCorrectly()
    {
        var e = new TestEnhancedEnum(TestEnhancedEnum.TestKind.Int, 42);

        e.Is(TestEnhancedEnum.TestKind.Int).Should().BeTrue();
        e.Is(TestEnhancedEnum.TestKind.Float).Should().BeFalse();
    }

    /// <summary>
    /// Tests pattern-match style Is method with value extraction.
    /// </summary>
    [Fact]
    public void Is_WithValueExtraction_Works()
    {
        var e = new TestEnhancedEnum(TestEnhancedEnum.TestKind.Int, 42);

        e.Is(TestEnhancedEnum.TestKind.Int, out int value).Should().BeTrue();
        value.Should().Be(42);

        e.Is(TestEnhancedEnum.TestKind.Float, out float f).Should().BeFalse();
    }

    /// <summary>
    /// Tests implicit conversion to TEnum.
    /// </summary>
    [Fact]
    public void ImplicitConversion_ToTEnum_Works()
    {
        var e = new TestEnhancedEnum(TestEnhancedEnum.TestKind.Int, 42);
        TestEnhancedEnum.TestKind kind = e;
        kind.Should().Be(TestEnhancedEnum.TestKind.Int);
    }

    /// <summary>
    /// Tests positional pattern matching in switch expressions.
    /// </summary>
    [Fact]
    public void PositionalPattern_Matches_SwitchExpression()
    {
        var eInt = new TestEnhancedEnum(TestEnhancedEnum.TestKind.Int, 42);
        var eFloat = new TestEnhancedEnum(TestEnhancedEnum.TestKind.Float, 3.14f);
        var eNoValue = new TestEnhancedEnum(TestEnhancedEnum.TestKind.NoValue);

        string Describe(TestEnhancedEnum e) => e switch
        {
            (TestEnhancedEnum.TestKind.Int, int i) => $"int:{i}",
            (TestEnhancedEnum.TestKind.Float, float f) => $"float:{f}",
            (TestEnhancedEnum.TestKind.NoValue, _) => "no-value",
            _ => "unknown"
        };

        Describe(eInt).Should().Be("int:42");
        Describe(eFloat).Should().Be("float:3.14");
        Describe(eNoValue).Should().Be("no-value");
    }

    #endregion

    #region EnhancedEnumFlex<TEnum> Tests

    /// <summary>
    /// Tests Flex Deconstruct for all payload types.
    /// </summary>
    [Fact]
    public void Flex_Deconstruct_Works()
    {
        EnsureRegistered();

        // Test NoValue payload
        var noValue = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.Create(TestEnhancedEnum.TestKind.NoValue);
        var (kind1, value1) = noValue;
        kind1.Should().Be(TestEnhancedEnum.TestKind.NoValue);
        value1.Should().BeNull();

        // Test Byte payload
        var byteEnum = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.Create(TestEnhancedEnum.TestKind.Byte, (byte)0x42);
        var (kind2, value2) = byteEnum;
        kind2.Should().Be(TestEnhancedEnum.TestKind.Byte);
        value2.Should().BeOfType<byte>();
        ((byte)value2!).Should().Be(0x42);

        // Test UInt16 payload
        var u16Enum = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.Create(TestEnhancedEnum.TestKind.UInt16, (ushort)0xCAFE);
        var (kind3, value3) = u16Enum;
        kind3.Should().Be(TestEnhancedEnum.TestKind.UInt16);
        value3.Should().BeOfType<ushort>();
        ((ushort)value3!).Should().Be(0xCAFE);

        // Test UInt32 payload
        var u32Enum = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.Create(TestEnhancedEnum.TestKind.UInt32, 0xDEADBEEFu);
        var (kind4, value4) = u32Enum;
        kind4.Should().Be(TestEnhancedEnum.TestKind.UInt32);
        value4.Should().BeOfType<uint>();
        ((uint)value4!).Should().Be(0xDEADBEEFu);

        // Test UInt32UInt32 payload
        var u32u32Enum = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.Create(
            TestEnhancedEnum.TestKind.UInt32UInt32, (0x11111111u, 0x22222222u));
        var (kind5, value5) = u32u32Enum;
        kind5.Should().Be(TestEnhancedEnum.TestKind.UInt32UInt32);
        value5.Should().BeOfType<(uint, uint)>();
        var pair = ((uint a, uint b))value5!;
        pair.a.Should().Be(0x11111111u);
        pair.b.Should().Be(0x22222222u);

        // Test UInt32Int32 payload
        var u32i32Enum = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.Create(
            TestEnhancedEnum.TestKind.UInt32Int32, (0x89ABCDEFu, -42));
        var (kind6, value6) = u32i32Enum;
        kind6.Should().Be(TestEnhancedEnum.TestKind.UInt32Int32);
        value6.Should().BeOfType<(uint, int)>();
        var mixed = ((uint a, int b))value6!;
        mixed.a.Should().Be(0x89ABCDEFu);
        mixed.b.Should().Be(-42);

        // Test String payload
        var strEnum = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.Create(
            TestEnhancedEnum.TestKind.String, "test-deconstruct");
        var (kind7, value7) = strEnum;
        kind7.Should().Be(TestEnhancedEnum.TestKind.String);
        value7.Should().BeOfType<string>();
        ((string)value7!).Should().Be("test-deconstruct");

        // Test Reference payload
        var payload = new RefPayload(999);
        var refEnum = MakeFlexReference(TestEnhancedEnum.TestKind.Reference, payload);
        var (kind8, value8) = refEnum;
        kind8.Should().Be(TestEnhancedEnum.TestKind.Reference);
        value8.Should().BeOfType<RefPayload>();
        ((RefPayload)value8!).Id.Should().Be(999);

        // Test Boxed value payload
        var when = new DateTime(2025, 06, 15, 10, 30, 0, DateTimeKind.Utc);
        var boxedEnum = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.CreateValue(
            TestEnhancedEnum.TestKind.Boxed, when);
        var (kind9, value9) = boxedEnum;
        kind9.Should().Be(TestEnhancedEnum.TestKind.Boxed);
        value9.Should().BeOfType<DateTime>();
        ((DateTime)value9!).Should().Be(when);
    }

    /// <summary>
    /// Tests that tag-only values work correctly.
    /// </summary>
    [Fact]
    public void EnhancedEnumFlex_TagOnly_IsAndMismatchBehave()
    {
        EnsureRegistered();
        var e = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.Create(TestEnhancedEnum.TestKind.NoValue);

        e.Is(TestEnhancedEnum.TestKind.NoValue).Should().BeTrue();
        e.Is(TestEnhancedEnum.TestKind.Int).Should().BeFalse();

        e.TryGetByte(TestEnhancedEnum.TestKind.Byte, out _).Should().BeFalse();
        e.TryGetUInt16(TestEnhancedEnum.TestKind.UInt16, out _).Should().BeFalse();
        e.TryGetUInt32(TestEnhancedEnum.TestKind.UInt32, out _).Should().BeFalse();
        e.TryGetUInt32UInt32(TestEnhancedEnum.TestKind.UInt32UInt32, out _).Should().BeFalse();
        e.TryGetUInt32Int32(TestEnhancedEnum.TestKind.UInt32Int32, out _).Should().BeFalse();
        e.TryGetString(TestEnhancedEnum.TestKind.String, out _).Should().BeFalse();
        e.TryGetReference<RefPayload>(TestEnhancedEnum.TestKind.Reference, out _).Should().BeFalse();
    }

    /// <summary>
    /// Tests inline payload round-trips.
    /// </summary>
    [Fact]
    public void EnhancedEnumFlex_InlinePayloads_RoundTrip()
    {
        EnsureRegistered();

        // Byte
        var b = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.Create(TestEnhancedEnum.TestKind.Byte, (byte)0x7F);
        b.TryGetByte(TestEnhancedEnum.TestKind.Byte, out var bv).Should().BeTrue();
        bv.Should().Be(0x7F);
        b.TryGetUInt16(TestEnhancedEnum.TestKind.UInt16, out _).Should().BeFalse();

        // UInt16
        var u16 = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.Create(TestEnhancedEnum.TestKind.UInt16, (ushort)0xBEEF);
        u16.TryGetUInt16(TestEnhancedEnum.TestKind.UInt16, out var u16v).Should().BeTrue();
        u16v.Should().Be(0xBEEF);

        // UInt32
        var u32 = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.Create(TestEnhancedEnum.TestKind.UInt32, 0xDEADBEEFu);
        u32.TryGetUInt32(TestEnhancedEnum.TestKind.UInt32, out var u32v).Should().BeTrue();
        u32v.Should().Be(0xDEADBEEFu);

        // UInt32UInt32
        var u32u32 = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.Create(
            TestEnhancedEnum.TestKind.UInt32UInt32, (0x11111111u, 0x22222222u));
        u32u32.TryGetUInt32UInt32(TestEnhancedEnum.TestKind.UInt32UInt32, out var pair).Should().BeTrue();
        pair.a.Should().Be(0x11111111u);
        pair.b.Should().Be(0x22222222u);

        // UInt32Int32
        var u32i32 = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.Create(
            TestEnhancedEnum.TestKind.UInt32Int32, (0x89ABCDEFu, -5));
        u32i32.TryGetUInt32Int32(TestEnhancedEnum.TestKind.UInt32Int32, out var mixed).Should().BeTrue();
        mixed.a.Should().Be(0x89ABCDEFu);
        mixed.b.Should().Be(-5);

        // Wrong kind/type combinations should fail
        u32i32.TryGetUInt32UInt32(TestEnhancedEnum.TestKind.UInt32Int32, out _).Should().BeFalse();
        u32i32.TryGetUInt32Int32(TestEnhancedEnum.TestKind.UInt32UInt32, out _).Should().BeFalse();

        // Generic TryGet supports inline payloads too
        u32i32.TryGet<(uint, int)>(TestEnhancedEnum.TestKind.UInt32Int32, out var mixed2).Should().BeTrue();
        mixed2.Should().Be(mixed);
    }

    /// <summary>
    /// Tests string payload round-trip.
    /// </summary>
    [Fact]
    public void EnhancedEnumFlex_StringPayload_RoundTrip()
    {
        EnsureRegistered();
        var e = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.Create(TestEnhancedEnum.TestKind.String, "hello");

        e.TryGetString(TestEnhancedEnum.TestKind.String, out var s).Should().BeTrue();
        s.Should().Be("hello");
        e.TryGetString(TestEnhancedEnum.TestKind.NoValue, out _).Should().BeFalse();
    }

    /// <summary>
    /// Tests reference payload round-trip.
    /// </summary>
    [Fact]
    public void EnhancedEnumFlex_ReferencePayload_RoundTrip()
    {
        EnsureRegistered();
        var payload = new RefPayload(123);
        var e = MakeFlexReference(TestEnhancedEnum.TestKind.Reference, payload);

        e.TryGetReference<RefPayload>(TestEnhancedEnum.TestKind.Reference, out var p).Should().BeTrue();
        p!.Id.Should().Be(123);

        e.TryGetReference<RefPayload>(TestEnhancedEnum.TestKind.String, out _).Should().BeFalse();
        e.TryGetReference<string>(TestEnhancedEnum.TestKind.Reference, out _).Should().BeFalse();
    }

    /// <summary>
    /// Tests boxed value payload via generic TryGet.
    /// </summary>
    [Fact]
    public void EnhancedEnumFlex_BoxedFallback_RoundTrip_ViaGenericTryGet()
    {
        EnsureRegistered();
        var when = new DateTime(2025, 01, 01, 12, 00, 00, DateTimeKind.Utc);
        var e = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.CreateValue(TestEnhancedEnum.TestKind.Boxed, when);

        e.TryGet<DateTime>(TestEnhancedEnum.TestKind.Boxed, out var dt).Should().BeTrue();
        dt.Should().Be(when);

        e.TryGet<DateTime>(TestEnhancedEnum.TestKind.NoValue, out _).Should().BeFalse();
        e.TryGet<int>(TestEnhancedEnum.TestKind.Boxed, out _).Should().BeFalse();
    }

    /// <summary>
    /// Tests Get method for direct value retrieval.
    /// </summary>
    [Fact]
    public void EnhancedEnumFlex_Get_ReturnsValue()
    {
        EnsureRegistered();
        var e = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.Create(TestEnhancedEnum.TestKind.UInt32, 0x12345678u);

        e.Get<uint>().Should().Be(0x12345678u);
    }

    /// <summary>
    /// Tests GetByte method.
    /// </summary>
    [Fact]
    public void EnhancedEnumFlex_GetByte_ReturnsValue()
    {
        EnsureRegistered();
        var e = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.Create(TestEnhancedEnum.TestKind.Byte, (byte)0xAB);

        e.GetByte().Should().Be(0xAB);
    }

    /// <summary>
    /// Tests GetByte throws when wrong payload kind.
    /// </summary>
    [Fact]
    public void EnhancedEnumFlex_GetByte_WrongKind_Throws()
    {
        EnsureRegistered();
        var e = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.Create(TestEnhancedEnum.TestKind.UInt32, 0x12345678u);

        var act = () => e.GetByte();
        act.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// Tests GetUInt16 method.
    /// </summary>
    [Fact]
    public void EnhancedEnumFlex_GetUInt16_ReturnsValue()
    {
        EnsureRegistered();
        var e = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.Create(TestEnhancedEnum.TestKind.UInt16, (ushort)0xABCD);

        e.GetUInt16().Should().Be(0xABCD);
    }

    /// <summary>
    /// Tests GetUInt32 method.
    /// </summary>
    [Fact]
    public void EnhancedEnumFlex_GetUInt32_ReturnsValue()
    {
        EnsureRegistered();
        var e = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.Create(TestEnhancedEnum.TestKind.UInt32, 0xDEADBEEFu);

        e.GetUInt32().Should().Be(0xDEADBEEFu);
    }

    /// <summary>
    /// Tests GetUInt32UInt32 method.
    /// </summary>
    [Fact]
    public void EnhancedEnumFlex_GetUInt32UInt32_ReturnsValue()
    {
        EnsureRegistered();
        var e = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.Create(
            TestEnhancedEnum.TestKind.UInt32UInt32, (0xAAAAAAAAu, 0xBBBBBBBBu));

        var result = e.GetUInt32UInt32();
        result.a.Should().Be(0xAAAAAAAAu);
        result.b.Should().Be(0xBBBBBBBBu);
    }

    /// <summary>
    /// Tests GetUInt32Int32 method.
    /// </summary>
    [Fact]
    public void EnhancedEnumFlex_GetUInt32Int32_ReturnsValue()
    {
        EnsureRegistered();
        var e = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.Create(
            TestEnhancedEnum.TestKind.UInt32Int32, (0xAAAAAAAAu, -100));

        var result = e.GetUInt32Int32();
        result.a.Should().Be(0xAAAAAAAAu);
        result.b.Should().Be(-100);
    }

    /// <summary>
    /// Tests GetString method.
    /// </summary>
    [Fact]
    public void EnhancedEnumFlex_GetString_ReturnsValue()
    {
        EnsureRegistered();
        var e = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.Create(TestEnhancedEnum.TestKind.String, "test");

        e.GetString().Should().Be("test");
    }

    /// <summary>
    /// Tests GetReference method.
    /// </summary>
    [Fact]
    public void EnhancedEnumFlex_GetReference_ReturnsValue()
    {
        EnsureRegistered();
        var payload = new RefPayload(456);
        var e = MakeFlexReference(TestEnhancedEnum.TestKind.Reference, payload);

        e.GetReference<RefPayload>().Id.Should().Be(456);
    }

    /// <summary>
    /// Tests CreateValue routes hot-path types correctly.
    /// </summary>
    [Fact]
    public void EnhancedEnumFlex_CreateValue_RoutesHotPathTypes()
    {
        EnsureRegistered();

        // byte should use inline storage
        var b = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.CreateValue(TestEnhancedEnum.TestKind.Byte, (byte)0x12);
        b.TryGetByte(TestEnhancedEnum.TestKind.Byte, out var bv).Should().BeTrue();
        bv.Should().Be(0x12);

        // ushort should use inline storage
        var u16 = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.CreateValue(TestEnhancedEnum.TestKind.UInt16, (ushort)0x1234);
        u16.TryGetUInt16(TestEnhancedEnum.TestKind.UInt16, out var u16v).Should().BeTrue();
        u16v.Should().Be(0x1234);

        // uint should use inline storage
        var u32 = EnhancedEnumFlex<TestEnhancedEnum.TestKind>.CreateValue(TestEnhancedEnum.TestKind.UInt32, 0x12345678u);
        u32.TryGetUInt32(TestEnhancedEnum.TestKind.UInt32, out var u32v).Should().BeTrue();
        u32v.Should().Be(0x12345678u);
    }

    #endregion
}
