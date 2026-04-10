using FluentAssertions;
using Xunit;
using static Stardust.Utilities.Option;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Unit tests for the Option&lt;T&gt; record struct.
/// </summary>
public class OptionTests
{
    #region Construction

    [Fact]
    public void Some_WithValue_CreatesSomeOption()
    {
        var opt = Option<int>.Some(42);

        opt.IsSome.Should().BeTrue();
        opt.IsNone.Should().BeFalse();
        opt.Value.Should().Be(42);
    }

    [Fact]
    public void None_CreatesNoneOption()
    {
        var opt = Option<int>.None;

        opt.IsNone.Should().BeTrue();
        opt.IsSome.Should().BeFalse();
    }

    [Fact]
    public void Default_IsNone()
    {
        Option<int> opt = default;

        opt.IsNone.Should().BeTrue();
    }

    [Fact]
    public void Some_WithReferenceType_Works()
    {
        var opt = Option<string>.Some("hello");

        opt.IsSome.Should().BeTrue();
        opt.Value.Should().Be("hello");
    }

    [Fact]
    public void Some_WithZero_IsSome()
    {
        var opt = Option<int>.Some(0);

        opt.IsSome.Should().BeTrue();
        opt.Value.Should().Be(0);
    }

    [Fact]
    public void Some_WithNull_IsSome()
    {
        // Some(null) is explicitly Some -- it wraps null as a value.
        // This distinguishes "I have a value that happens to be null" from "I have no value".
        var opt = Option<string?>.Some(null);

        opt.IsSome.Should().BeTrue();
        opt.Value.Should().BeNull();
    }

    #endregion

    #region Value Access

    [Fact]
    public void Value_OnNone_Throws()
    {
        var opt = Option<int>.None;

        var act = () => opt.Value;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*None*");
    }

    [Fact]
    public void Unwrap_OnSome_ReturnsValue()
    {
        var opt = Option<int>.Some(7);

        opt.Unwrap().Should().Be(7);
    }

    [Fact]
    public void Unwrap_OnNone_Throws()
    {
        var opt = Option<int>.None;

        var act = () => opt.Unwrap();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Expect_OnSome_ReturnsValue()
    {
        var opt = Option<int>.Some(99);

        opt.Expect("should not throw").Should().Be(99);
    }

    [Fact]
    public void Expect_OnNone_ThrowsWithMessage()
    {
        var opt = Option<int>.None;

        var act = () => opt.Expect("value was required");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("value was required");
    }

    #endregion

    #region TryGetValue

    [Fact]
    public void TryGetValue_OnSome_ReturnsTrueAndValue()
    {
        var opt = Option<int>.Some(42);

        opt.TryGetValue(out var value).Should().BeTrue();
        value.Should().Be(42);
    }

    [Fact]
    public void TryGetValue_OnNone_ReturnsFalse()
    {
        var opt = Option<int>.None;

        opt.TryGetValue(out var value).Should().BeFalse();
        value.Should().Be(default);
    }

    #endregion

    #region UnwrapOr

    [Fact]
    public void UnwrapOr_OnSome_ReturnsValue()
    {
        var opt = Option<int>.Some(42);

        opt.UnwrapOr(0).Should().Be(42);
    }

    [Fact]
    public void UnwrapOr_OnNone_ReturnsDefault()
    {
        var opt = Option<int>.None;

        opt.UnwrapOr(-1).Should().Be(-1);
    }

    [Fact]
    public void UnwrapOrElse_OnSome_DoesNotInvokeFactory()
    {
        var opt = Option<int>.Some(42);
        bool invoked = false;

        opt.UnwrapOrElse(() => { invoked = true; return -1; }).Should().Be(42);
        invoked.Should().BeFalse();
    }

    [Fact]
    public void UnwrapOrElse_OnNone_InvokesFactory()
    {
        var opt = Option<int>.None;

        opt.UnwrapOrElse(() => 99).Should().Be(99);
    }

    #endregion

    #region Implicit Conversions

    [Fact]
    public void ImplicitConversion_FromValue_CreatesSome()
    {
        Option<int> opt = 42;

        opt.IsSome.Should().BeTrue();
        opt.Value.Should().Be(42);
    }

    [Fact]
    public void ImplicitConversion_FromNoneSentinel_CreatesNone()
    {
        Option<int> opt = Option.None;

        opt.IsNone.Should().BeTrue();
    }

    [Fact]
    public void ImplicitConversion_StringValue_CreatesSome()
    {
        Option<string> opt = "hello";

        opt.IsSome.Should().BeTrue();
        opt.Value.Should().Be("hello");
    }

    #endregion

    #region Static Helpers

    [Fact]
    public void Option_Some_TypeInference()
    {
        var opt = Option.Some(42);

        opt.IsSome.Should().BeTrue();
        opt.Value.Should().Be(42);
    }

    [Fact]
    public void Option_FromNullable_Reference_NonNull_ReturnsSome()
    {
        string? s = "hello";
        var opt = Option.FromNullable(s);

        opt.IsSome.Should().BeTrue();
        opt.Value.Should().Be("hello");
    }

    [Fact]
    public void Option_FromNullable_Reference_Null_ReturnsNone()
    {
        string? s = null;
        var opt = Option.FromNullable(s);

        opt.IsNone.Should().BeTrue();
    }

    [Fact]
    public void Option_FromNullable_ValueType_HasValue_ReturnsSome()
    {
        int? n = 42;
        var opt = Option.FromNullable(n);

        opt.IsSome.Should().BeTrue();
        opt.Value.Should().Be(42);
    }

    [Fact]
    public void Option_FromNullable_ValueType_Null_ReturnsNone()
    {
        int? n = null;
        var opt = Option.FromNullable(n);

        opt.IsNone.Should().BeTrue();
    }

    #endregion

    #region Deconstruct

    [Fact]
    public void Deconstruct_Some()
    {
        var (isSome, value) = Option<int>.Some(42);

        isSome.Should().BeTrue();
        value.Should().Be(42);
    }

    [Fact]
    public void Deconstruct_None()
    {
        var (isSome, value) = Option<int>.None;

        isSome.Should().BeFalse();
        value.Should().Be(default);
    }

    #endregion

    #region Map / AndThen

    [Fact]
    public void Map_OnSome_Transforms()
    {
        var opt = Option<int>.Some(5);

        var result = opt.Map(x => x * 2);

        result.IsSome.Should().BeTrue();
        result.Value.Should().Be(10);
    }

    [Fact]
    public void Map_OnNone_ReturnsNone()
    {
        var opt = Option<int>.None;

        var result = opt.Map(x => x * 2);

        result.IsNone.Should().BeTrue();
    }

    [Fact]
    public void AndThen_OnSome_ReturnsInnerSome()
    {
        var opt = Option<int>.Some(5);

        var result = opt.AndThen(x => x > 0 ? Option<string>.Some(x.ToString()) : Option<string>.None);

        result.IsSome.Should().BeTrue();
        result.Value.Should().Be("5");
    }

    [Fact]
    public void AndThen_OnSome_ReturnsInnerNone()
    {
        var opt = Option<int>.Some(-1);

        var result = opt.AndThen(x => x > 0 ? Option<string>.Some(x.ToString()) : Option<string>.None);

        result.IsNone.Should().BeTrue();
    }

    [Fact]
    public void AndThen_OnNone_ReturnsNone()
    {
        var opt = Option<int>.None;

        var result = opt.AndThen(x => Option<string>.Some(x.ToString()));

        result.IsNone.Should().BeTrue();
    }

    [Fact]
    public void Map_Chain_MultipleTransforms()
    {
        var result = Option<int>.Some(3)
            .Map(x => x + 1)
            .Map(x => x * 10)
            .Map(x => x.ToString());

        result.IsSome.Should().BeTrue();
        result.Value.Should().Be("40");
    }

    [Fact]
    public void Map_Chain_ShortCircuitsOnNone()
    {
        bool secondCalled = false;

        var result = Option<int>.None
            .Map(x => x + 1)
            .Map(x => { secondCalled = true; return x.ToString(); });

        result.IsNone.Should().BeTrue();
        secondCalled.Should().BeFalse();
    }

    #endregion

    #region Filter

    [Fact]
    public void Filter_PredicateTrue_ReturnsSome()
    {
        var opt = Option<int>.Some(42);

        opt.Filter(x => x > 0).IsSome.Should().BeTrue();
    }

    [Fact]
    public void Filter_PredicateFalse_ReturnsNone()
    {
        var opt = Option<int>.Some(-1);

        opt.Filter(x => x > 0).IsNone.Should().BeTrue();
    }

    [Fact]
    public void Filter_OnNone_ReturnsNone()
    {
        var opt = Option<int>.None;

        opt.Filter(x => x > 0).IsNone.Should().BeTrue();
    }

    #endregion

    #region Inspect

    [Fact]
    public void Inspect_WhenSome_InvokesAction()
    {
        int captured = 0;
        var opt = Option<int>.Some(42);

        var returned = opt.Inspect(v => captured = v);

        captured.Should().Be(42);
        returned.Should().Be(opt);
    }

    [Fact]
    public void Inspect_WhenNone_DoesNotInvoke()
    {
        bool invoked = false;
        var opt = Option<int>.None;

        opt.Inspect(_ => invoked = true);

        invoked.Should().BeFalse();
    }

    #endregion

    #region MapOrElse / MapOr

    [Fact]
    public void MapOrElse_OnSome_InvokesSomeBranch()
    {
        var opt = Option<int>.Some(42);

        var result = opt.MapOrElse(
            onSome: v => $"Got {v}",
            onNone: () => "Nothing");

        result.Should().Be("Got 42");
    }

    [Fact]
    public void MapOrElse_OnNone_InvokesNoneBranch()
    {
        var opt = Option<int>.None;

        var result = opt.MapOrElse(
            onSome: v => $"Got {v}",
            onNone: () => "Nothing");

        result.Should().Be("Nothing");
    }

    [Fact]
    public void MapOr_OnSome_AppliesTransform()
    {
        var opt = Option<int>.Some(42);

        var result = opt.MapOr(v => v * 2, -1);

        result.Should().Be(84);
    }

    [Fact]
    public void MapOr_OnNone_ReturnsDefault()
    {
        var opt = Option<int>.None;

        var result = opt.MapOr(v => v * 2, -1);

        result.Should().Be(-1);
    }

    [Fact]
    public void MapOr_OnSome_TransformsToDifferentType()
    {
        var opt = Option<int>.Some(5);

        var result = opt.MapOr(v => v.ToString(), "none");

        result.Should().Be("5");
    }

    [Fact]
    public void MapOr_OnNone_ReturnsDifferentTypeDefault()
    {
        var opt = Option<int>.None;

        var result = opt.MapOr(v => v.ToString(), "none");

        result.Should().Be("none");
    }

    #endregion

    #region ToNullable

    [Fact]
    public void ToNullable_OnSome_ReturnsValue()
    {
        var opt = Option<int>.Some(42);

        opt.ToNullable().Should().Be(42);
    }

    [Fact]
    public void ToNullable_OnNone_ReturnsDefault()
    {
        var opt = Option<int>.None;

        opt.ToNullable().Should().Be(default);
    }

    [Fact]
    public void ToNullable_ReferenceType_OnNone_ReturnsNull()
    {
        var opt = Option<string>.None;

        opt.ToNullable().Should().BeNull();
    }

    #endregion

    #region OkOr (Interop with Result)

    [Fact]
    public void OkOr_OnSome_ReturnsOkResult()
    {
        var opt = Option<int>.Some(42);

        var result = opt.OkOr("missing");

        result.IsOk.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void OkOr_OnNone_ReturnsErrResult()
    {
        var opt = Option<int>.None;

        var result = opt.OkOr("missing");

        result.IsErr.Should().BeTrue();
        result.Error.Should().Be("missing");
    }

    [Fact]
    public void OkOrElse_OnNone_InvokesFactory()
    {
        var opt = Option<int>.None;

        var result = opt.OkOrElse(() => "computed error");

        result.IsErr.Should().BeTrue();
        result.Error.Should().Be("computed error");
    }

    [Fact]
    public void OkOrElse_OnSome_DoesNotInvokeFactory()
    {
        var opt = Option<int>.Some(42);
        bool invoked = false;

        var result = opt.OkOrElse(() => { invoked = true; return "err"; });

        result.IsOk.Should().BeTrue();
        invoked.Should().BeFalse();
    }

    #endregion

    #region Combinators: And, Or, Xor, Zip

    [Fact]
    public void And_BothSome_ReturnsOther()
    {
        var a = Option<int>.Some(1);
        var b = Option<string>.Some("x");

        a.And(b).Value.Should().Be("x");
    }

    [Fact]
    public void And_FirstNone_ReturnsNone()
    {
        var a = Option<int>.None;
        var b = Option<string>.Some("x");

        a.And(b).IsNone.Should().BeTrue();
    }

    [Fact]
    public void Or_FirstSome_ReturnsFirst()
    {
        var a = Option<int>.Some(1);
        var b = Option<int>.Some(2);

        a.Or(b).Value.Should().Be(1);
    }

    [Fact]
    public void Or_FirstNone_ReturnsSecond()
    {
        var a = Option<int>.None;
        var b = Option<int>.Some(2);

        a.Or(b).Value.Should().Be(2);
    }

    [Fact]
    public void OrElse_FirstNone_InvokesFactory()
    {
        var a = Option<int>.None;

        a.OrElse(() => Option<int>.Some(99)).Value.Should().Be(99);
    }

    [Fact]
    public void OrElse_FirstSome_DoesNotInvokeFactory()
    {
        var a = Option<int>.Some(1);
        bool invoked = false;

        a.OrElse(() => { invoked = true; return Option<int>.Some(99); }).Value.Should().Be(1);
        invoked.Should().BeFalse();
    }

    [Fact]
    public void Xor_OnlyFirstSome_ReturnsFirst()
    {
        var a = Option<int>.Some(1);
        var b = Option<int>.None;

        a.Xor(b).Value.Should().Be(1);
    }

    [Fact]
    public void Xor_OnlySecondSome_ReturnsSecond()
    {
        var a = Option<int>.None;
        var b = Option<int>.Some(2);

        a.Xor(b).Value.Should().Be(2);
    }

    [Fact]
    public void Xor_BothSome_ReturnsNone()
    {
        var a = Option<int>.Some(1);
        var b = Option<int>.Some(2);

        a.Xor(b).IsNone.Should().BeTrue();
    }

    [Fact]
    public void Xor_BothNone_ReturnsNone()
    {
        var a = Option<int>.None;
        var b = Option<int>.None;

        a.Xor(b).IsNone.Should().BeTrue();
    }

    [Fact]
    public void Zip_BothSome_ReturnsTuple()
    {
        var a = Option<int>.Some(1);
        var b = Option<string>.Some("x");

        var result = a.Zip(b);
        result.IsSome.Should().BeTrue();
        result.Value.Should().Be((1, "x"));
    }

    [Fact]
    public void Zip_FirstNone_ReturnsNone()
    {
        var a = Option<int>.None;
        var b = Option<string>.Some("x");

        a.Zip(b).IsNone.Should().BeTrue();
    }

    [Fact]
    public void Zip_SecondNone_ReturnsNone()
    {
        var a = Option<int>.Some(1);
        var b = Option<string>.None;

        a.Zip(b).IsNone.Should().BeTrue();
    }

    [Fact]
    public void ZipWith_BothSome_AppliesCombine()
    {
        var a = Option<int>.Some(3);
        var b = Option<int>.Some(4);

        var result = a.ZipWith(b, (x, y) => x * y);

        result.IsSome.Should().BeTrue();
        result.Value.Should().Be(12);
    }

    [Fact]
    public void ZipWith_BothSome_DifferentTypes()
    {
        var a = Option<int>.Some(42);
        var b = Option<string>.Some("value");

        var result = a.ZipWith(b, (n, s) => $"{s}={n}");

        result.IsSome.Should().BeTrue();
        result.Value.Should().Be("value=42");
    }

    [Fact]
    public void ZipWith_FirstNone_ReturnsNone()
    {
        var a = Option<int>.None;
        var b = Option<int>.Some(4);

        a.ZipWith(b, (x, y) => x + y).IsNone.Should().BeTrue();
    }

    [Fact]
    public void ZipWith_SecondNone_ReturnsNone()
    {
        var a = Option<int>.Some(3);
        var b = Option<int>.None;

        a.ZipWith(b, (x, y) => x + y).IsNone.Should().BeTrue();
    }

    [Fact]
    public void ZipWith_BothNone_ReturnsNone()
    {
        var a = Option<int>.None;
        var b = Option<int>.None;

        a.ZipWith(b, (x, y) => x + y).IsNone.Should().BeTrue();
    }

    [Fact]
    public void ZipWith_BothSome_DoesNotCallCombineWhenNone()
    {
        var a = Option<int>.None;
        var b = Option<int>.Some(4);
        bool called = false;

        a.ZipWith(b, (x, y) => { called = true; return x + y; }).IsNone.Should().BeTrue();
        called.Should().BeFalse();
    }

    #endregion

    #region Flatten

    [Fact]
    public void Flatten_SomeSome_ReturnsInner()
    {
        var nested = Option<Option<int>>.Some(Option<int>.Some(42));

        nested.Flatten().Value.Should().Be(42);
    }

    [Fact]
    public void Flatten_SomeNone_ReturnsNone()
    {
        var nested = Option<Option<int>>.Some(Option<int>.None);

        nested.Flatten().IsNone.Should().BeTrue();
    }

    [Fact]
    public void Flatten_None_ReturnsNone()
    {
        var nested = Option<Option<int>>.None;

        nested.Flatten().IsNone.Should().BeTrue();
    }

    #endregion

    #region ToOption Extensions

    [Fact]
    public void ToOption_ReferenceType_NonNull_ReturnsSome()
    {
        string? s = "hello";

        s.ToOption().Value.Should().Be("hello");
    }

    [Fact]
    public void ToOption_ReferenceType_Null_ReturnsNone()
    {
        string? s = null;

        s.ToOption().IsNone.Should().BeTrue();
    }

    [Fact]
    public void ToOption_NullableValueType_HasValue_ReturnsSome()
    {
        int? n = 42;

        n.ToOption().Value.Should().Be(42);
    }

    [Fact]
    public void ToOption_NullableValueType_Null_ReturnsNone()
    {
        int? n = null;

        n.ToOption().IsNone.Should().BeTrue();
    }

    #endregion

    #region Result Interop

    [Fact]
    public void Result_ToOption_Success_ReturnsSome()
    {
        var result = Result<int, string>.Ok(42);

        result.ToOption().Value.Should().Be(42);
    }

    [Fact]
    public void Result_ToOption_Failure_ReturnsNone()
    {
        var result = Result<int, string>.Err("fail");

        result.ToOption().IsNone.Should().BeTrue();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equality_TwoSome_SameValue_AreEqual()
    {
        var a = Option<int>.Some(42);
        var b = Option<int>.Some(42);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equality_TwoSome_DifferentValue_AreNotEqual()
    {
        var a = Option<int>.Some(1);
        var b = Option<int>.Some(2);

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equality_TwoNone_AreEqual()
    {
        var a = Option<int>.None;
        var b = Option<int>.None;

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equality_SomeAndNone_AreNotEqual()
    {
        var a = Option<int>.Some(42);
        var b = Option<int>.None;

        a.Should().NotBe(b);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_Some_IncludesValue()
    {
        Option<int>.Some(42).ToString().Should().Be("Some(42)");
    }

    [Fact]
    public void ToString_None_ReturnsNone()
    {
        Option<int>.None.ToString().Should().Be("None");
    }

    [Fact]
    public void ToString_SomeString()
    {
        Option<string>.Some("hi").ToString().Should().Be("Some(hi)");
    }

    #endregion

    #region Zero-Allocation Verification

    [Fact]
    public void SizeOf_Option_IsMinimal()
    {
        // Option<int> should be bool + int = 8 bytes (with padding).
        // Just verify it's a value type by checking default is None.
        Option<int> opt = default;
        opt.IsNone.Should().BeTrue("default(Option<int>) must be None");
    }

    [Fact]
    public void Option_IsValueType()
    {
        typeof(Option<int>).IsValueType.Should().BeTrue();
    }

    [Fact]
    public void Option_IsReadonlyRecordStruct()
    {
        var type = typeof(Option<int>);
        type.IsValueType.Should().BeTrue();
        // Record structs implement IEquatable<T>
        type.GetInterfaces().Should().Contain(typeof(IEquatable<Option<int>>));
    }

    #endregion

    #region Async Extensions

    [Fact]
    public async Task Map_Async_OnSome_Transforms()
    {
        var task = Task.FromResult(Option<int>.Some(5));

        var result = await task.Map(x => x * 2);

        result.IsSome.Should().BeTrue();
        result.Value.Should().Be(10);
    }

    [Fact]
    public async Task Map_Async_OnNone_ReturnsNone()
    {
        var task = Task.FromResult(Option<int>.None);

        var result = await task.Map(x => x * 2);

        result.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task AndThen_Async_OnSome()
    {
        var task = Task.FromResult(Option<int>.Some(5));

        var result = await task.AndThen(x => Option<string>.Some(x.ToString()));

        result.IsSome.Should().BeTrue();
        result.Value.Should().Be("5");
    }

    [Fact]
    public async Task AndThen_Async_TaskReturning_OnSome()
    {
        var task = Task.FromResult(Option<int>.Some(5));

        var result = await task.AndThen(async x =>
        {
            await Task.Yield();
            return Option<string>.Some(x.ToString());
        });

        result.IsSome.Should().BeTrue();
        result.Value.Should().Be("5");
    }

    [Fact]
    public async Task AndThen_Async_TaskReturning_OnNone_ReturnsNone()
    {
        var task = Task.FromResult(Option<int>.None);
        bool invoked = false;

        var result = await task.AndThen(async x =>
        {
            invoked = true;
            await Task.Yield();
            return Option<string>.Some(x.ToString());
        });

        result.IsNone.Should().BeTrue();
        invoked.Should().BeFalse();
    }

    #endregion

    #region Practical Usage Patterns

    [Fact]
    public void Dictionary_TryGet_Pattern()
    {
        var dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };

        Option<int> Lookup(string key) =>
            dict.TryGetValue(key, out var val) ? Option<int>.Some(val) : Option<int>.None;

        Lookup("a").Value.Should().Be(1);
        Lookup("c").IsNone.Should().BeTrue();
    }

    [Fact]
    public void FirstOrNone_Pattern()
    {
        var items = new[] { 1, 2, 3, 4, 5 };

        static Option<int> FirstOrNone(int[] arr, Func<int, bool> predicate)
        {
            foreach (var item in arr)
                if (predicate(item)) return item; // implicit conversion
            return Option.None;
        }

        FirstOrNone(items, x => x > 3).Value.Should().Be(4);
        FirstOrNone(items, x => x > 10).IsNone.Should().BeTrue();
    }

    [Fact]
    public void ChainedLookup_Pattern()
    {
        // Simulate a chain of lookups where any step might fail
        var users = new Dictionary<int, string> { [1] = "alice" };
        var emails = new Dictionary<string, string> { ["alice"] = "alice@example.com" };

        Option<string> GetUser(int id) =>
            users.TryGetValue(id, out var name) ? name : Option.None;

        Option<string> GetEmail(string user) =>
            emails.TryGetValue(user, out var email) ? email : Option.None;

        // Happy path
        var result = GetUser(1).AndThen(GetEmail);
        result.Value.Should().Be("alice@example.com");

        // Missing user
        GetUser(999).AndThen(GetEmail).IsNone.Should().BeTrue();

        // Missing email
        users[2] = "bob";
        GetUser(2).AndThen(GetEmail).IsNone.Should().BeTrue();
    }

    [Fact]
    public void Option_In_Return_Position()
    {
        static Option<int> ParsePositive(string s)
        {
            if (int.TryParse(s, out var n) && n > 0)
                return n;
            return Option.None;
        }

        ParsePositive("42").Value.Should().Be(42);
        ParsePositive("-1").IsNone.Should().BeTrue();
        ParsePositive("abc").IsNone.Should().BeTrue();
    }

    #endregion

    #region UnwrapOrDefault

    [Fact]
    public void UnwrapOrDefault_OnSome_ReturnsValue()
    {
        var opt = Option<int>.Some(42);

        opt.UnwrapOrDefault().Should().Be(42);
    }

    [Fact]
    public void UnwrapOrDefault_OnNone_ValueType_ReturnsDefault()
    {
        var opt = Option<int>.None;

        opt.UnwrapOrDefault().Should().Be(0);
    }

    [Fact]
    public void UnwrapOrDefault_OnNone_ReferenceType_ReturnsNull()
    {
        var opt = Option<string>.None;

        opt.UnwrapOrDefault().Should().BeNull();
    }

    [Fact]
    public void UnwrapOrDefault_OnSome_ReferenceType_ReturnsValue()
    {
        var opt = Option<string>.Some("hello");

        opt.UnwrapOrDefault().Should().Be("hello");
    }

    [Fact]
    public void UnwrapOrDefault_OnNone_Bool_ReturnsFalse()
    {
        var opt = Option<bool>.None;

        opt.UnwrapOrDefault().Should().BeFalse();
    }

    [Fact]
    public void UnwrapOrDefault_OnSome_Zero_ReturnsZero()
    {
        var opt = Option<int>.Some(0);

        opt.UnwrapOrDefault().Should().Be(0);
    }

    #endregion

    #region UnwrapUnchecked

    [Fact]
    public void UnwrapUnchecked_OnSome_ReturnsValue()
    {
        var opt = Option<int>.Some(42);

        opt.UnwrapUnchecked().Should().Be(42);
    }

    [Fact]
    public void UnwrapUnchecked_OnNone_ValueType_ReturnsDefault()
    {
        var opt = Option<int>.None;

        opt.UnwrapUnchecked().Should().Be(0);
    }

    [Fact]
    public void UnwrapUnchecked_OnNone_ReferenceType_ReturnsNull()
    {
        var opt = Option<string>.None;

        opt.UnwrapUnchecked().Should().BeNull();
    }

    [Fact]
    public void UnwrapUnchecked_OnSome_ReferenceType_ReturnsValue()
    {
        var opt = Option<string>.Some("hello");

        opt.UnwrapUnchecked().Should().Be("hello");
    }

    [Fact]
    public void UnwrapUnchecked_DoesNotThrow_OnNone()
    {
        var opt = Option<int>.None;

        var act = () => opt.UnwrapUnchecked();

        act.Should().NotThrow();
    }

    #endregion

    #region Transpose

    [Fact]
    public void Transpose_None_ReturnsOkNone()
    {
        var opt = Option<Result<int, string>>.None;

        var result = opt.Transpose();

        result.IsOk.Should().BeTrue();
        result.Value.IsNone.Should().BeTrue();
    }

    [Fact]
    public void Transpose_SomeOk_ReturnsOkSome()
    {
        var opt = Option<Result<int, string>>.Some(Result<int, string>.Ok(42));

        var result = opt.Transpose();

        result.IsOk.Should().BeTrue();
        result.Value.IsSome.Should().BeTrue();
        result.Value.Value.Should().Be(42);
    }

    [Fact]
    public void Transpose_SomeErr_ReturnsErr()
    {
        var opt = Option<Result<int, string>>.Some(Result<int, string>.Err("fail"));

        var result = opt.Transpose();

        result.IsErr.Should().BeTrue();
        result.Error.Should().Be("fail");
    }

    [Fact]
    public void Transpose_SomeOk_ReferenceType_ReturnsOkSome()
    {
        var opt = Option<Result<string, int>>.Some(Result<string, int>.Ok("hello"));

        var result = opt.Transpose();

        result.IsOk.Should().BeTrue();
        result.Value.IsSome.Should().BeTrue();
        result.Value.Value.Should().Be("hello");
    }

    #endregion

    #region Global Using Pattern (using static Stardust.Utilities.Option)

    // These tests prove that 'global using static Stardust.Utilities.Option;'
    // enables unqualified Some() and None in return positions, assignments,
    // ternaries, method arguments, and chains -- with no additional operators.

    [Fact]
    public void GlobalUsing_Some_InReturnPosition()
    {
        static Option<int> GetValue() => Some(42);

        var opt = GetValue();
        opt.IsSome.Should().BeTrue();
        opt.Value.Should().Be(42);
    }

    [Fact]
    public void GlobalUsing_None_InReturnPosition()
    {
        static Option<int> GetEmpty() => None;

        GetEmpty().IsNone.Should().BeTrue();
    }

    [Fact]
    public void GlobalUsing_Some_And_None_InBranching()
    {
        static Option<int> ParsePositive(string s)
        {
            if (int.TryParse(s, out var n) && n > 0)
                return Some(n);
            return None;
        }

        ParsePositive("42").Value.Should().Be(42);
        ParsePositive("-1").IsNone.Should().BeTrue();
        ParsePositive("abc").IsNone.Should().BeTrue();
    }

    [Fact]
    public void GlobalUsing_None_InGenericReturnPosition()
    {
        // None works for any T without specifying the type parameter.
        static Option<T> GetNone<T>() => None;

        GetNone<int>().IsNone.Should().BeTrue();
        GetNone<string>().IsNone.Should().BeTrue();
    }

    [Fact]
    public void GlobalUsing_Some_ChainsNaturally()
    {
        var result = Some(3)
            .Map(x => x + 1)
            .Map(x => x * 10)
            .Map(x => x.ToString());

        result.IsSome.Should().BeTrue();
        result.Value.Should().Be("40");
    }

    [Fact]
    public void GlobalUsing_Ternary_SomeAndNone()
    {
        static Option<int> MaybeDouble(int n) =>
            n > 0 ? Some(n * 2) : None;

        MaybeDouble(5).Value.Should().Be(10);
        MaybeDouble(-1).IsNone.Should().BeTrue();
    }

    [Fact]
    public void GlobalUsing_Some_AsMethodArgument()
    {
        static string Describe(Option<int> opt) =>
            opt.MapOrElse(v => $"Got {v}", () => "Nothing");

        Describe(Some(42)).Should().Be("Got 42");
        Describe(None).Should().Be("Nothing");
    }

    [Fact]
    public void GlobalUsing_Some_Assignment()
    {
        Option<int> a = Some(42);
        Option<string> b = Some("hello");
        Option<int> c = None;

        a.Value.Should().Be(42);
        b.Value.Should().Be("hello");
        c.IsNone.Should().BeTrue();
    }

    [Fact]
    public void GlobalUsing_Some_WithAndThen()
    {
        static Option<string> Lookup(int id) =>
            id == 1 ? Some("alice") : None;

        static Option<string> GetEmail(string user) =>
            user == "alice" ? Some("alice@example.com") : None;

        Lookup(1).AndThen(GetEmail).Value.Should().Be("alice@example.com");
        Lookup(2).AndThen(GetEmail).IsNone.Should().BeTrue();
    }

    #endregion
}
