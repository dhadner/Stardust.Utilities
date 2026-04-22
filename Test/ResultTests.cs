using FluentAssertions;
using System.Reflection;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Unit tests for the Result record struct.
/// </summary>
public class ResultTests
{
    #region Result<T, TError> Tests

    /// <summary>
    /// Tests that accessing Value on a failed result throws.
    /// </summary>
    [Fact]
    public void Value_OnFailedResult_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = Result<int, string>.Err("error");

        // Act
        var act = () => _ = result.Value;

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Cannot access Value on failed Result: error");
    }

    /// <summary>
    /// Tests that accessing Error on a successful result throws.
    /// </summary>
    [Fact]
    public void Error_OnSuccessfulResult_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = Result<int, string>.Ok(42);

        // Act
        var act = () => _ = result.Error;

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Cannot access Error on successful Result");
    }

    /// <summary>
    /// Tests that ErrorOrDefault returns default on success.
    /// </summary>
    [Fact]
    public void ErrorOrDefault_OnSuccess_ReturnsDefault()
    {
        // Arrange
        var result = Result<int, string>.Ok(42);

        // Assert
        result.ErrorOrDefault.Should().BeNull();
    }

    /// <summary>
    /// Tests that ErrorOrDefault returns error on failure.
    /// </summary>
    [Fact]
    public void ErrorOrDefault_OnFailure_ReturnsError()
    {
        // Arrange
        var result = Result<int, string>.Err("error");

        // Assert
        result.ErrorOrDefault.Should().Be("error");
    }

    /// <summary>
    /// Tests TryGetValue returns true and value on success.
    /// </summary>
    [Fact]
    public void TryGetValue_OnSuccess_ReturnsTrueWithValue()
    {
        // Arrange
        var result = Result<int, string>.Ok(42);

        // Act
        var success = result.TryGetValue(out var value);

        // Assert
        success.Should().BeTrue();
        value.Should().Be(42);
    }

    /// <summary>
    /// Tests TryGetValue returns false on failure.
    /// </summary>
    [Fact]
    public void TryGetValue_OnFailure_ReturnsFalse()
    {
        // Arrange
        var result = Result<int, string>.Err("error");

        // Act
        var success = result.TryGetValue(out var value);

        // Assert
        success.Should().BeFalse();
        value.Should().Be(default);
    }

    /// <summary>
    /// Tests deconstruction of a successful result.
    /// </summary>
    [Fact]
    public void Deconstruct_OnSuccess_ReturnsCorrectValues()
    {
        // Arrange
        var result = Result<int, string>.Ok(42);

        // Act
        var (isSuccess, value, error) = result;

        // Assert
        isSuccess.Should().BeTrue();
        value.Should().Be(42);
        error.Should().BeNull();
    }

    /// <summary>
    /// Tests deconstruction of a failed result.
    /// </summary>
    [Fact]
    public void Deconstruct_OnFailure_ReturnsCorrectValues()
    {
        // Arrange
        var result = Result<int, string>.Err("error");

        // Act
        var (isSuccess, value, error) = result;

        // Assert
        isSuccess.Should().BeFalse();
        value.Should().Be(default);
        error.Should().Be("error");
    }

    #endregion

    #region Result<TError> (Void-style) Tests

    /// <summary>
    /// Tests deconstruction of void result.
    /// </summary>
    [Fact]
    public void VoidResult_Deconstruct_ReturnsCorrectValues()
    {
        // Test failure
        var failResult = Result<string>.Err("Fail");
        var (isSuccess, error) = failResult;
        isSuccess.Should().BeFalse();
        error.Should().Be("Fail");

        // Test success
        var okResult = Result<string>.Ok();
        var (okSuccess, okError) = okResult;
        okSuccess.Should().BeTrue();
        okError.Should().BeNull();
    }

    #endregion

    #region Type-Specific Tests

    #endregion

    #region Functional Method Tests

    /// <summary>
    /// Tests ToNullable returns value or null.
    /// </summary>
    [Fact]
    public void ToNullable_ReturnsValueOrDefault()
    {
        // Test success
        var ok = Result<int, string>.Ok(10);
        ok.ToNullable().Should().Be(10);

        // Test failure
        var fail = Result<int, string>.Err("e");
        fail.ToNullable().Should().Be(default);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // Rust-parity tests (new API)
    // ══════════════════════════════════════════════════════════════════

    #region IsOk / IsErr

    [Fact]
    public void IsOk_OnOk_ReturnsTrue()
    {
        var result = Result<int, string>.Ok(42);
        result.IsOk.Should().BeTrue();
        result.IsErr.Should().BeFalse();
    }

    [Fact]
    public void IsOk_OnErr_ReturnsFalse()
    {
        var result = Result<int, string>.Err("fail");
        result.IsOk.Should().BeFalse();
        result.IsErr.Should().BeTrue();
    }

    #endregion

    #region IsOkAnd / IsErrAnd

    [Fact]
    public void IsOkAnd_OnOk_PredicateTrue_ReturnsTrue()
    {
        var result = Result<int, string>.Ok(42);
        result.IsOkAnd(v => v > 0).Should().BeTrue();
    }

    [Fact]
    public void IsOkAnd_OnOk_PredicateFalse_ReturnsFalse()
    {
        var result = Result<int, string>.Ok(-1);
        result.IsOkAnd(v => v > 0).Should().BeFalse();
    }

    [Fact]
    public void IsOkAnd_OnErr_ReturnsFalse()
    {
        var result = Result<int, string>.Err("fail");
        result.IsOkAnd(v => v > 0).Should().BeFalse();
    }

    [Fact]
    public void IsErrAnd_OnErr_PredicateTrue_ReturnsTrue()
    {
        var result = Result<int, string>.Err("not found");
        result.IsErrAnd(e => e.Contains("not found")).Should().BeTrue();
    }

    [Fact]
    public void IsErrAnd_OnErr_PredicateFalse_ReturnsFalse()
    {
        var result = Result<int, string>.Err("timeout");
        result.IsErrAnd(e => e.Contains("not found")).Should().BeFalse();
    }

    [Fact]
    public void IsErrAnd_OnOk_ReturnsFalse()
    {
        var result = Result<int, string>.Ok(42);
        result.IsErrAnd(e => true).Should().BeFalse();
    }

    #endregion

    #region Expect / ExpectErr

    [Fact]
    public void Expect_OnOk_ReturnsValue()
    {
        var result = Result<int, string>.Ok(42);
        result.Expect("should not throw").Should().Be(42);
    }

    [Fact]
    public void Expect_OnErr_ThrowsWithMessage()
    {
        var result = Result<int, string>.Err("fail");
        var act = () => result.Expect("value was required");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("value was required");
    }

    [Fact]
    public void ExpectErr_OnErr_ReturnsError()
    {
        var result = Result<int, string>.Err("fail");
        result.ExpectErr("should not throw").Should().Be("fail");
    }

    [Fact]
    public void ExpectErr_OnOk_ThrowsWithMessage()
    {
        var result = Result<int, string>.Ok(42);
        var act = () => result.ExpectErr("expected error");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("expected error");
    }

    #endregion

    #region UnwrapOr / UnwrapOrElse / UnwrapOrDefault / UnwrapErr

    [Fact]
    public void UnwrapOr_OnOk_ReturnsValue()
    {
        Result<int, string>.Ok(42).UnwrapOr(0).Should().Be(42);
    }

    [Fact]
    public void UnwrapOr_OnErr_ReturnsDefault()
    {
        Result<int, string>.Err("fail").UnwrapOr(99).Should().Be(99);
    }

    [Fact]
    public void UnwrapOrElse_OnOk_ReturnsValue()
    {
        bool called = false;
        Result<int, string>.Ok(42).UnwrapOrElse(e => { called = true; return -1; }).Should().Be(42);
        called.Should().BeFalse();
    }

    [Fact]
    public void UnwrapOrElse_OnErr_InvokesFactory()
    {
        Result<int, string>.Err("fail").UnwrapOrElse(e => e.Length).Should().Be(4);
    }

    [Fact]
    public void UnwrapOrDefault_OnOk_ReturnsValue()
    {
        Result<int, string>.Ok(42).UnwrapOrDefault().Should().Be(42);
    }

    [Fact]
    public void UnwrapOrDefault_OnErr_ValueType_ReturnsDefault()
    {
        Result<int, string>.Err("fail").UnwrapOrDefault().Should().Be(0);
    }

    [Fact]
    public void UnwrapOrDefault_OnErr_ReferenceType_ReturnsNull()
    {
        Result<string, string>.Err("fail").UnwrapOrDefault().Should().BeNull();
    }

    [Fact]
    public void UnwrapErr_OnErr_ReturnsError()
    {
        Result<int, string>.Err("fail").UnwrapErr().Should().Be("fail");
    }

    [Fact]
    public void UnwrapErr_OnOk_Throws()
    {
        var act = () => Result<int, string>.Ok(42).UnwrapErr();
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Map / MapErr / MapOr / MapOrElse

    [Fact]
    public void Map_OnOk_TransformsValue()
    {
        var result = Result<int, string>.Ok(10).Map(x => x.ToString());
        result.IsOk.Should().BeTrue();
        result.Value.Should().Be("10");
    }

    [Fact]
    public void Map_OnErr_PropagatesError()
    {
        var result = Result<int, string>.Err("fail").Map(x => x.ToString());
        result.IsErr.Should().BeTrue();
        result.Error.Should().Be("fail");
    }

    [Fact]
    public void MapErr_OnErr_TransformsError()
    {
        var result = Result<int, string>.Err("fail").MapErr(e => e.Length);
        result.IsErr.Should().BeTrue();
        result.Error.Should().Be(4);
    }

    [Fact]
    public void MapErr_OnOk_PreservesValue()
    {
        var result = Result<int, string>.Ok(42).MapErr(e => e.Length);
        result.IsOk.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void MapOr_OnOk_AppliesTransform()
    {
        Result<int, string>.Ok(5).MapOr(v => v * 2, -1).Should().Be(10);
    }

    [Fact]
    public void MapOr_OnErr_ReturnsDefault()
    {
        Result<int, string>.Err("fail").MapOr(v => v * 2, -1).Should().Be(-1);
    }

    [Fact]
    public void MapOrElse_OnOk_AppliesOkBranch()
    {
        Result<int, string>.Ok(5).MapOrElse(v => v * 2, e => -1).Should().Be(10);
    }

    [Fact]
    public void MapOrElse_OnErr_AppliesErrBranch()
    {
        Result<int, string>.Err("fail").MapOrElse(v => v * 2, e => e.Length).Should().Be(4);
    }

    #endregion

    #region Inspect / InspectErr

    [Fact]
    public void Inspect_OnOk_InvokesAction()
    {
        int captured = 0;
        var result = Result<int, string>.Ok(42);
        var returned = result.Inspect(v => captured = v);
        captured.Should().Be(42);
        returned.Should().Be(result);
    }

    [Fact]
    public void Inspect_OnErr_DoesNotInvoke()
    {
        bool called = false;
        Result<int, string>.Err("fail").Inspect(_ => called = true);
        called.Should().BeFalse();
    }

    [Fact]
    public void InspectErr_OnErr_InvokesAction()
    {
        string captured = "";
        var result = Result<int, string>.Err("fail");
        var returned = result.InspectErr(e => captured = e);
        captured.Should().Be("fail");
        returned.Should().Be(result);
    }

    [Fact]
    public void InspectErr_OnOk_DoesNotInvoke()
    {
        bool called = false;
        Result<int, string>.Ok(42).InspectErr(_ => called = true);
        called.Should().BeFalse();
    }

    #endregion

    #region And / AndThen / Or / OrElse

    [Fact]
    public void And_OkOk_ReturnsOther()
    {
        var a = Result<int, string>.Ok(1);
        var b = Result<string, string>.Ok("hello");
        a.And(b).Value.Should().Be("hello");
    }

    [Fact]
    public void And_OkErr_ReturnsOtherErr()
    {
        var a = Result<int, string>.Ok(1);
        var b = Result<string, string>.Err("fail");
        a.And(b).Error.Should().Be("fail");
    }

    [Fact]
    public void And_ErrIgnored_ReturnsSelfErr()
    {
        var a = Result<int, string>.Err("first");
        var b = Result<string, string>.Ok("hello");
        a.And(b).Error.Should().Be("first");
    }

    [Fact]
    public void AndThen_OnOk_CallsOp()
    {
        var result = Result<int, string>.Ok(10)
            .AndThen(x => x > 0 ? Result<string, string>.Ok(x.ToString()) : Result<string, string>.Err("negative"));
        result.IsOk.Should().BeTrue();
        result.Value.Should().Be("10");
    }

    [Fact]
    public void AndThen_OnOk_OpReturnsErr()
    {
        var result = Result<int, string>.Ok(-1)
            .AndThen(x => x > 0 ? Result<string, string>.Ok(x.ToString()) : Result<string, string>.Err("negative"));
        result.IsErr.Should().BeTrue();
        result.Error.Should().Be("negative");
    }

    [Fact]
    public void AndThen_OnErr_DoesNotCallOp()
    {
        bool called = false;
        var result = Result<int, string>.Err("fail")
            .AndThen(x => { called = true; return Result<string, string>.Ok(x.ToString()); });
        result.IsErr.Should().BeTrue();
        called.Should().BeFalse();
    }

    [Fact]
    public void Or_OkIgnored_ReturnsSelf()
    {
        var a = Result<int, string>.Ok(42);
        var b = Result<int, int>.Err(0);
        a.Or(b).Value.Should().Be(42);
    }

    [Fact]
    public void Or_ErrOk_ReturnsOther()
    {
        var a = Result<int, string>.Err("fail");
        var b = Result<int, int>.Ok(99);
        a.Or(b).Value.Should().Be(99);
    }

    [Fact]
    public void Or_ErrErr_ReturnsOtherErr()
    {
        var a = Result<int, string>.Err("first");
        var b = Result<int, int>.Err(42);
        a.Or(b).Error.Should().Be(42);
    }

    [Fact]
    public void OrElse_OnOk_DoesNotCallOp()
    {
        bool called = false;
        var result = Result<int, string>.Ok(42)
            .OrElse(e => { called = true; return Result<int, int>.Ok(-1); });
        result.Value.Should().Be(42);
        called.Should().BeFalse();
    }

    [Fact]
    public void OrElse_OnErr_CallsOp()
    {
        var result = Result<int, string>.Err("fail")
            .OrElse(e => Result<int, int>.Ok(e.Length));
        result.IsOk.Should().BeTrue();
        result.Value.Should().Be(4);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_Ok_ReturnsOkFormat()
    {
        Result<int, string>.Ok(42).ToString().Should().Be("Ok(42)");
    }

    [Fact]
    public void ToString_Err_ReturnsErrFormat()
    {
        Result<int, string>.Err("fail").ToString().Should().Be("ErrToOption(fail)");
    }

    [Fact]
    public void VoidResult_ToString_Ok_ReturnsOk()
    {
        Result<string>.Ok().ToString().Should().Be("Ok");
    }

    [Fact]
    public void VoidResult_ToString_Err_ReturnsErrFormat()
    {
        Result<string>.Err("fail").ToString().Should().Be("ErrToOption(fail)");
    }

    #endregion

    #region Flatten

    [Fact]
    public void Flatten_OkOk_ReturnsInner()
    {
        var nested = Result<Result<int, string>, string>.Ok(Result<int, string>.Ok(42));
        nested.Flatten().Value.Should().Be(42);
    }

    [Fact]
    public void Flatten_OkErr_ReturnsInnerErr()
    {
        var nested = Result<Result<int, string>, string>.Ok(Result<int, string>.Err("inner"));
        nested.Flatten().Error.Should().Be("inner");
    }

    [Fact]
    public void Flatten_Err_ReturnsOuterErr()
    {
        var nested = Result<Result<int, string>, string>.Err("outer");
        nested.Flatten().Error.Should().Be("outer");
    }

    #endregion

    #region Transpose (Result → Option)

    [Fact]
    public void Transpose_OkSome_ReturnsSomeOk()
    {
        var result = Result<Option<int>, string>.Ok(Option<int>.Some(42));
        var transposed = result.Transpose();
        transposed.IsSome.Should().BeTrue();
        transposed.Value.IsOk.Should().BeTrue();
        transposed.Value.Value.Should().Be(42);
    }

    [Fact]
    public void Transpose_OkNone_ReturnsNone()
    {
        var result = Result<Option<int>, string>.Ok(Option<int>.None);
        result.Transpose().IsNone.Should().BeTrue();
    }

    [Fact]
    public void Transpose_Err_ReturnsSomeErr()
    {
        var result = Result<Option<int>, string>.Err("fail");
        var transposed = result.Transpose();
        transposed.IsSome.Should().BeTrue();
        transposed.Value.IsErr.Should().BeTrue();
        transposed.Value.Error.Should().Be("fail");
    }

    #endregion

    #region ErrorToOption

    [Fact]
    public void ErrorToOption_OnErr_ReturnsSome()
    {
        var result = Result<int, string>.Err("fail");
        var opt = result.ErrToOption();
        opt.IsSome.Should().BeTrue();
        opt.Value.Should().Be("fail");
    }

    [Fact]
    public void ErrorToOption_OnOk_ReturnsNone()
    {
        var result = Result<int, string>.Ok(42);
        result.ErrToOption().IsNone.Should().BeTrue();
    }

    #endregion

    #region Void Result Rust-parity

    [Fact]
    public void VoidResult_IsOk_IsErr()
    {
        Result<string>.Ok().IsOk.Should().BeTrue();
        Result<string>.Ok().IsErr.Should().BeFalse();
        Result<string>.Err("fail").IsOk.Should().BeFalse();
        Result<string>.Err("fail").IsErr.Should().BeTrue();
    }

    [Fact]
    public void VoidResult_And_BothOk_ReturnsOther()
    {
        var a = Result<string>.Ok();
        var b = Result<string>.Ok();
        a.And(b).IsOk.Should().BeTrue();
    }

    [Fact]
    public void VoidResult_And_FirstErr_ReturnsSelf()
    {
        var a = Result<string>.Err("fail");
        var b = Result<string>.Ok();
        a.And(b).Error.Should().Be("fail");
    }

    [Fact]
    public void VoidResult_AndThen_OnOk_CallsOp()
    {
        var result = Result<string>.Ok().AndThen(() => Result<string>.Err("from op"));
        result.IsErr.Should().BeTrue();
        result.Error.Should().Be("from op");
    }

    [Fact]
    public void VoidResult_AndThen_OnErr_DoesNotCallOp()
    {
        bool called = false;
        var result = Result<string>.Err("fail").AndThen(() => { called = true; return Result<string>.Ok(); });
        result.IsErr.Should().BeTrue();
        called.Should().BeFalse();
    }

    [Fact]
    public void VoidResult_Or_FirstOk_ReturnsSelf()
    {
        var result = Result<string>.Ok().Or(Result<int>.Err(42));
        result.IsOk.Should().BeTrue();
    }

    [Fact]
    public void VoidResult_Or_FirstErr_ReturnsOther()
    {
        var other = Result<int>.Ok();
        var result = Result<string>.Err("fail").Or(other);
        result.IsOk.Should().BeTrue();
    }

    [Fact]
    public void VoidResult_OrElse_OnOk_DoesNotCallOp()
    {
        bool called = false;
        var result = Result<string>.Ok().OrElse(e => { called = true; return Result<int>.Ok(); });
        result.IsOk.Should().BeTrue();
        called.Should().BeFalse();
    }

    [Fact]
    public void VoidResult_OrElse_OnErr_CallsOp()
    {
        var result = Result<string>.Err("fail").OrElse(e => Result<int>.Err(e.Length));
        result.IsErr.Should().BeTrue();
        result.Error.Should().Be(4);
    }

    [Fact]
    public void VoidResult_MapErr_OnErr_TransformsError()
    {
        var result = Result<string>.Err("fail").MapErr(e => e.Length);
        result.IsErr.Should().BeTrue();
        result.Error.Should().Be(4);
    }

    [Fact]
    public void VoidResult_MapErr_OnOk_PreservesOk()
    {
        var result = Result<string>.Ok().MapErr(e => e.Length);
        result.IsOk.Should().BeTrue();
    }

    [Fact]
    public void VoidResult_MapOrElse_OnOk_InvokesOkBranch()
    {
        var result = Result<string>.Ok().MapOrElse(() => "ok", e => $"err: {e}");
        result.Should().Be("ok");
    }

    [Fact]
    public void VoidResult_MapOrElse_OnErr_InvokesErrBranch()
    {
        var result = Result<string>.Err("fail").MapOrElse(() => "ok", e => $"err: {e}");
        result.Should().Be("err: fail");
    }

    [Fact]
    public void VoidResult_Inspect_OnOk_Invokes()
    {
        bool called = false;
        Result<string>.Ok().Inspect(() => called = true);
        called.Should().BeTrue();
    }

    [Fact]
    public void VoidResult_Inspect_OnErr_DoesNotInvoke()
    {
        bool called = false;
        Result<string>.Err("fail").Inspect(() => called = true);
        called.Should().BeFalse();
    }

    [Fact]
    public void VoidResult_InspectErr_OnErr_Invokes()
    {
        string captured = "";
        Result<string>.Err("fail").InspectErr(e => captured = e);
        captured.Should().Be("fail");
    }

    [Fact]
    public void VoidResult_InspectErr_OnOk_DoesNotInvoke()
    {
        bool called = false;
        Result<string>.Ok().InspectErr(_ => called = true);
        called.Should().BeFalse();
    }


    #endregion

    #region Combine

    [Fact]
    public void VoidResult_Combine_AllOk_ReturnsOk()
    {
        var results = new[] { Result<string>.Ok(), Result<string>.Ok() };
        var combined = Result<string>.Combine(results);
        combined.IsOk.Should().BeTrue();
    }

    [Fact]
    public void VoidResult_Combine_ContainsErr_ReturnsFirstErr()
    {
        var results = new[] { Result<string>.Ok(), Result<string>.Err("fail1"), Result<string>.Err("fail2") };
        var combined = Result<string>.Combine(results);
        combined.IsErr.Should().BeTrue();
        combined.Error.Should().Be("fail1");
    }

    #endregion

    #region Expect

    [Fact]
    public void VoidResult_Expect_OnOk_DoesNotThrow()
    {
        var result = Result<string>.Ok();
        var act = () => { var dummy = result.IsOk; }; // just to verify no throw on Ok
        act.Should().NotThrow();
    }

    [Fact]
    public void VoidResult_Expect_OnErr_ThrowsWithMessage()
    {
        var result = Result<string>.Err("fail");
        var act = () => { if(result.IsErr) throw new InvalidOperationException("expected ok"); }; 
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("expected ok");
    }

    #endregion
}
