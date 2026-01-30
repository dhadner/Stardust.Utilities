using FluentAssertions;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Unit tests for the Result record struct.
/// </summary>
public class ResultTests
{
    #region Result<T, TError> Tests

    /// <summary>
    /// Tests that Ok() creates a successful result with default value.
    /// </summary>
    [Fact]
    public void Ok_WithDefaultValue_CreatesSuccessfulResult()
    {
        // Act
        var result = Result<int, string>.Ok();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(default);
    }

    /// <summary>
    /// Tests that Ok() with a value creates a successful result.
    /// </summary>
    [Fact]
    public void Ok_WithValue_CreatesSuccessfulResult()
    {
        // Act
        var result = Result<int, string>.Ok(42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(42);
    }

    /// <summary>
    /// Tests that Ok() with string returns empty string for null.
    /// </summary>
    [Fact]
    public void Ok_WithNullString_ReturnsEmptyString()
    {
        // Act
        var result = Result<string, string>.Ok(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(string.Empty);
    }

    /// <summary>
    /// Tests that Ok() with array returns empty array for null.
    /// </summary>
    [Fact]
    public void Ok_WithNullArray_ReturnsEmptyArray()
    {
        // Act
        var result = Result<int[], string>.Ok(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Ok() with List returns empty list for null.
    /// </summary>
    [Fact]
    public void Ok_WithNullList_ReturnsEmptyList()
    {
        // Act
        var result = Result<List<int>, string>.Ok(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Err() creates a failed result.
    /// </summary>
    [Fact]
    public void Err_CreatesFailedResult()
    {
        // Act
        var result = Result<int, string>.Err("error message");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("error message");
    }

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
    /// Tests that Ok() creates a successful void result.
    /// </summary>
    [Fact]
    public void VoidOk_CreatesSuccessfulResult()
    {
        // Act
        var result = Result<string>.Ok();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
    }

    /// <summary>
    /// Tests that Err() creates a failed void result.
    /// </summary>
    [Fact]
    public void VoidErr_CreatesFailedResult()
    {
        // Act
        var result = Result<string>.Err("error message");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("error message");
    }

    /// <summary>
    /// Tests implicit conversion from void result to value result on success with collection type.
    /// </summary>
    [Fact]
    public void ImplicitConversion_VoidToValueResult_OnSuccessWithCollection_ReturnsEmptyCollection()
    {
        // Arrange
        Result<string> voidResult = Result<string>.Ok();

        // Act
        Result<List<int>, string> valueResult = voidResult;

        // Assert
        valueResult.IsSuccess.Should().BeTrue();
        valueResult.Value.Should().NotBeNull();
        valueResult.Value.Should().BeEmpty();
    }

    /// <summary>
    /// Tests implicit conversion from void result to value result on failure.
    /// </summary>
    [Fact]
    public void ImplicitConversion_VoidToValueResult_OnFailure_PreservesError()
    {
        // Arrange
        Result<string> voidResult = Result<string>.Err("error");

        // Act
        Result<int, string> valueResult = voidResult;

        // Assert
        valueResult.IsFailure.Should().BeTrue();
        valueResult.Error.Should().Be("error");
    }

    /// <summary>
    /// Tests implicit conversion from void result to value result on success throws for non-collection types.
    /// </summary>
    [Fact]
    public void ImplicitConversion_VoidToValueResult_OnSuccessWithNonCollection_Throws()
    {
        // Arrange
        Result<string> voidResult = Result<string>.Ok();

        // Act & Assert
        var act = () => { Result<int, string> valueResult = voidResult; };
        act.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// Tests implicit conversion from void result to string result returns empty string.
    /// </summary>
    [Fact]
    public void ImplicitConversion_VoidToStringResult_OnSuccess_ReturnsEmptyString()
    {
        // Arrange
        Result<string> voidResult = Result<string>.Ok();

        // Act
        Result<string, string> valueResult = voidResult;

        // Assert
        valueResult.IsSuccess.Should().BeTrue();
        valueResult.Value.Should().Be(string.Empty);
    }

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

    /// <summary>
    /// Tests Combine method returns success when all results succeed.
    /// </summary>
    [Fact]
    public void Combine_AllSuccess_ReturnsSuccess()
    {
        // Arrange
        var r1 = Result<string>.Ok();
        var r2 = Result<string>.Ok();

        // Act
        var combined = Result<string>.Combine(r1, r2);

        // Assert
        combined.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// Tests Combine method returns first failure.
    /// </summary>
    [Fact]
    public void Combine_OneFailure_ReturnsFirstFailure()
    {
        // Arrange
        var r1 = Result<string>.Ok();
        var rFail = Result<string>.Err("One failed");
        var r2 = Result<string>.Ok();

        // Act
        var combinedFail = Result<string>.Combine(r1, rFail, r2);

        // Assert
        combinedFail.IsFailure.Should().BeTrue();
        combinedFail.Error.Should().Be("One failed");
    }

    #endregion

    #region Type-Specific Tests

    /// <summary>
    /// Tests Result with byte type.
    /// </summary>
    [Fact]
    public void Result_Byte_OkAndErr()
    {
        // Test Ok
        byte value = 42;
        Result<byte, string> okResult = Result<byte, string>.Ok(value);
        okResult.IsSuccess.Should().BeTrue();
        okResult.Value.Should().Be(value);

        // Test Err
        Result<byte, string> errResult = Result<byte, string>.Err("error");
        errResult.IsFailure.Should().BeTrue();
        errResult.Error.Should().Be("error");
    }

    /// <summary>
    /// Tests Result with ushort type.
    /// </summary>
    [Fact]
    public void Result_UShort_OkAndErr()
    {
        // Test Ok
        ushort value = 12345;
        Result<ushort, string> okResult = Result<ushort, string>.Ok(value);
        okResult.IsSuccess.Should().BeTrue();
        okResult.Value.Should().Be(value);

        // Test Err
        Result<ushort, string> errResult = Result<ushort, string>.Err("error");
        errResult.IsFailure.Should().BeTrue();
        errResult.Error.Should().Be("error");
    }

    /// <summary>
    /// Tests Result with uint type.
    /// </summary>
    [Fact]
    public void Result_UInt_OkAndErr()
    {
        // Test Ok
        uint value = 123456789;
        Result<uint, string> okResult = Result<uint, string>.Ok(value);
        okResult.IsSuccess.Should().BeTrue();
        okResult.Value.Should().Be(value);

        // Test Err
        Result<uint, string> errResult = Result<uint, string>.Err("error");
        errResult.IsFailure.Should().BeTrue();
        errResult.Error.Should().Be("error");
    }

    /// <summary>
    /// Tests Result with byte array type.
    /// </summary>
    [Fact]
    public void Result_ByteArray_OkAndErr()
    {
        // Test Ok with value
        byte[] value = [1, 2, 3];
        Result<byte[], string> okResult = Result<byte[], string>.Ok(value);
        okResult.IsSuccess.Should().BeTrue();
        okResult.Value.Should().Equal(value);

        // Test Ok with null returns empty array
        Result<byte[], string> nullResult = Result<byte[], string>.Ok(null);
        nullResult.IsSuccess.Should().BeTrue();
        nullResult.Value.Should().NotBeNull();
        nullResult.Value.Should().BeEmpty();

        // Test Err
        Result<byte[], string> errResult = Result<byte[], string>.Err("error");
        errResult.IsFailure.Should().BeTrue();
        errResult.Error.Should().Be("error");
    }

    #endregion

    #region Functional Method Tests

    /// <summary>
    /// Tests ValueOr returns default value on failure.
    /// </summary>
    [Fact]
    public void ValueOr_OnFailure_ReturnsDefault()
    {
        // Arrange
        Result<int, string> result = Result<int, string>.Err("Fail");

        // Assert
        result.ValueOr(99).Should().Be(99);
    }

    /// <summary>
    /// Tests ValueOr returns value on success.
    /// </summary>
    [Fact]
    public void ValueOr_OnSuccess_ReturnsValue()
    {
        // Arrange
        Result<int, string> result = Result<int, string>.Ok(10);

        // Assert
        result.ValueOr(99).Should().Be(10);
    }

    /// <summary>
    /// Tests MapError transforms the error.
    /// </summary>
    [Fact]
    public void MapError_OnFailure_TransformsError()
    {
        // Arrange
        Result<int, string> result = Result<int, string>.Err("Fail");

        // Act
        var transformed = result.MapError(e => e + "!");

        // Assert
        transformed.IsFailure.Should().BeTrue();
        transformed.Error.Should().Be("Fail!");
    }

    /// <summary>
    /// Tests Then transforms the value.
    /// </summary>
    [Fact]
    public void Then_OnSuccess_TransformsValue()
    {
        // Arrange
        Result<int, string> result = Result<int, string>.Ok(10);

        // Act
        var transformed = result.Then(x => x.ToString());

        // Assert
        transformed.IsSuccess.Should().BeTrue();
        transformed.Value.Should().Be("10");
    }

    /// <summary>
    /// Tests Then chains result-returning functions.
    /// </summary>
    [Fact]
    public void Then_ChainsResultReturningFunc()
    {
        // Test success chain
        Result<int, string> result = Result<int, string>.Ok(10);
        var next = result.Then(x => Result<string, string>.Ok("Success"));
        next.IsSuccess.Should().BeTrue();
        next.Value.Should().Be("Success");

        // Test failure chain
        var failNext = result.Then(x => Result<string, string>.Err("Next failed"));
        failNext.IsFailure.Should().BeTrue();
        failNext.Error.Should().Be("Next failed");
    }

    /// <summary>
    /// Tests OnSuccess executes action on success.
    /// </summary>
    [Fact]
    public void OnSuccess_OnSuccess_ExecutesAction()
    {
        // Arrange
        bool called = false;

        // Act
        Result<int, string>.Ok(1).OnSuccess(x => called = true);

        // Assert
        called.Should().BeTrue();
    }

    /// <summary>
    /// Tests OnSuccess does not execute action on failure.
    /// </summary>
    [Fact]
    public void OnSuccess_OnFailure_DoesNotExecuteAction()
    {
        // Arrange
        bool called = false;

        // Act
        Result<int, string>.Err("e").OnSuccess(x => called = true);

        // Assert
        called.Should().BeFalse();
    }

    /// <summary>
    /// Tests OnFailure executes action on failure.
    /// </summary>
    [Fact]
    public void OnFailure_OnFailure_ExecutesAction()
    {
        // Arrange
        bool called = false;

        // Act
        Result<int, string>.Err("e").OnFailure(e => called = true);

        // Assert
        called.Should().BeTrue();
    }

    /// <summary>
    /// Tests OnFailure does not execute action on success.
    /// </summary>
    [Fact]
    public void OnFailure_OnSuccess_DoesNotExecuteAction()
    {
        // Arrange
        bool called = false;

        // Act
        Result<int, string>.Ok(1).OnFailure(e => called = true);

        // Assert
        called.Should().BeFalse();
    }

    /// <summary>
    /// Tests Match returns result based on state.
    /// </summary>
    [Fact]
    public void Match_ReturnsResultBasedOnState()
    {
        // Test success
        var resultOk = Result<int, string>.Ok(5);
        var valOk = resultOk.Match(
            onSuccess: x => x * 2,
            onFailure: e => -1
        );
        valOk.Should().Be(10);

        // Test failure
        var resultFail = Result<int, string>.Err("error");
        var valFail = resultFail.Match(
            onSuccess: x => x * 2,
            onFailure: e => -1
        );
        valFail.Should().Be(-1);
    }

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

    /// <summary>
    /// Tests Unwrap methods.
    /// </summary>
    [Fact]
    public void Unwrap_Methods()
    {
        // Test Unwrap on success
        var ok = Result<int, string>.Ok(10);
        ok.Unwrap().Should().Be(10);
        var actUnwrapError = () => ok.UnwrapError();
        actUnwrapError.Should().Throw<InvalidOperationException>();

        // Test UnwrapError on failure
        var fail = Result<int, string>.Err("error");
        fail.UnwrapError().Should().Be("error");
        var actUnwrap = () => fail.Unwrap();
        actUnwrap.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// Tests WithValue converts void result to value result.
    /// </summary>
    [Fact]
    public void WithValue_ConvertsVoidResult()
    {
        // Test success
        Result<string> voidOk = Result<string>.Ok();
        Result<int, string> valOk = voidOk.WithValue(123);
        valOk.IsSuccess.Should().BeTrue();
        valOk.Value.Should().Be(123);

        // Test failure
        Result<string> voidFail = Result<string>.Err("Fail");
        Result<int, string> valFail = voidFail.WithValue(123);
        valFail.IsFailure.Should().BeTrue();
        valFail.Error.Should().Be("Fail");
    }

    #endregion
}
