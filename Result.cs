using System.Runtime.CompilerServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Represents the result of an operation that can succeed with a value or fail with an error.
    /// Use this for operations that may fail in expected ways (validation, bounds checks, etc.)
    /// rather than being truly unexpected exceptional cases where throwing exceptions is appropriate.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    public readonly record struct Result<T, TError>
    {
        private readonly T? _value;
        private readonly TError? _error;
        private readonly bool _isSuccess;

        /// <summary>
        /// Creates a successful result with the specified value.
        /// </summary>
        /// <param name="value">The success value.</param>
        /// <param name="_">Dummy parameter only needed to resolve call to correct constructor overload.</param>
        private Result(T? value, bool _)
        {
            _value = value;
            _error = default;
            _isSuccess = true;
        }

        /// <summary>
        /// Creates an error result with the specified error.
        /// </summary>
        /// <param name="error"></param>
        private Result(TError error)
        {
            _value = default;
            _error = error;
            _isSuccess = false;
        }

        // ── Querying the variant ───────────────────────────────────

        /// <summary>
        /// True if the result is Ok (success).
        /// </summary>
        public bool IsOk
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _isSuccess;
        }

        /// <summary>
        /// True if the result is Err (failure).
        /// </summary>
        public bool IsErr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !_isSuccess;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the result is Ok and the contained value
        /// satisfies the given <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">The predicate to test the contained value.</param>
        /// <returns><see langword="true"/> if Ok and predicate returns true; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsOkAnd(Func<T, bool> predicate) => _isSuccess && predicate(_value!);

        /// <summary>
        /// Returns <see langword="true"/> if the result is Err and the contained error
        /// satisfies the given <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">The predicate to test the contained error.</param>
        /// <returns><see langword="true"/> if Err and predicate returns true; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsErrAnd(Func<TError, bool> predicate) => !_isSuccess && predicate(_error!);

        /// <summary>
        /// The success value. Throws if IsFailure.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the result is a failure.</exception>
        public T Value => _isSuccess
            ? _value!
            : throw new InvalidOperationException($"Cannot access Value on failed Result: {_error}");

        /// <summary>
        /// The error value. Throws if IsSuccess.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the result is a success.</exception>
        public TError Error => !_isSuccess
            ? _error!
            : throw new InvalidOperationException("Cannot access Error on successful Result");

        /// <summary>
        /// The error value, or default if IsSuccess.
        /// </summary>
        public TError? ErrorOrDefault => _error;

        /// <summary>
        /// Creates a successful result with the specified value.
        /// </summary>
        /// <param name="value">The success value.</param>
        /// <returns>A successful Result containing the value.</returns>
        public static Result<T, TError> Ok(T? value) => new(value, true);

        /// <summary>
        /// Creates a failed result with the specified error.
        /// </summary>
        /// <param name="error">The error value.</param>
        /// <returns>A failed result containing the error.</returns>
        public static Result<T, TError> Err(TError error) => new(error);

        /// <summary>
        /// Implicitly converts a void-style Result (Result&lt;TError&gt;) into a value Result (Result&lt;T, TError&gt;).
        /// On success the conversion returns an appropriate default value (empty array/list for collection types).
        /// On failure the error is preserved.
        /// </summary>
        /// <param name="result">Source Result&lt;TError&gt; to convert.</param>
        /// <returns>Converted Result&lt;T, TError&gt;.</returns>
        public static implicit operator Result<T, TError>(Result<TError> result) =>
            result.IsOk ? throw new InvalidCastException("No value provided") : Err(result.Error);


        /// <summary>
        /// Deconstructs the result for pattern matching.
        /// The Deconstruct method allows usage like:
        ///    var (isSuccess, value, error) = result;
        /// </summary>
        /// <param name="isSuccess">True if the result is successful.</param>
        /// <param name="value">The success value.</param>
        /// <param name="error">The error value.</param>
        public void Deconstruct(out bool isSuccess, out T? value, out TError? error)
        {
            isSuccess = _isSuccess;
            value = _value;
            error = _error;
        }

        /// <summary>
        /// Try to get the value. Returns false if failed.
        /// </summary>
        /// <param name="value">The success value.</param>
        /// <returns>True if the result is successful; otherwise, false.</returns>
        public bool TryGetValue(out T? value)
        {
            value = _value;
            return _isSuccess;
        }

        /// <summary>
        /// Try to get the error. Returns false if succeeded.
        /// </summary>
        /// <param name="error">The error value.</param>
        /// <returns>True if the result is a failure and the error is set; otherwise, false.</returns>
        public bool TryGetError(out TError? error)
        {
            error = _error;
            return !_isSuccess;
        }

        // ── Extracting contained values ───────────────────────────

        /// <summary>
        /// Unwraps the value, throwing if Err. Alias for <see cref="Value"/>.
        /// </summary>
        /// <returns>The success value.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the result is Err.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Unwrap() => Value;

        /// <summary>
        /// Unwraps the value, or throws with a custom message if Err.
        /// </summary>
        /// <param name="message">Error message for the exception.</param>
        /// <returns>The success value.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the result is Err.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Expect(string message) => _isSuccess
            ? _value!
            : throw new InvalidOperationException(message);

        /// <summary>
        /// Unwraps the error, throwing if Ok. Alias for <see cref="Error"/>.
        /// </summary>
        /// <returns>The error value.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the result is Ok.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TError UnwrapErr() => Error;

        /// <summary>
        /// Unwraps the error, or throws with a custom message if Ok.
        /// </summary>
        /// <param name="message">Error message for the exception.</param>
        /// <returns>The error value.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the result is Ok.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TError ExpectErr(string message) => !_isSuccess
            ? _error!
            : throw new InvalidOperationException(message);

        /// <summary>
        /// Returns the success value or <paramref name="defaultValue"/> if Err.
        /// </summary>
        /// <param name="defaultValue">The default value to return on failure.</param>
        /// <returns>The success value or the provided default.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T UnwrapOr(T defaultValue) => _isSuccess ? _value! : defaultValue;

        /// <summary>
        /// Returns the success value or invokes <paramref name="defaultFactory"/> if Err.
        /// </summary>
        /// <param name="defaultFactory">Factory invoked with the error when the result is Err.</param>
        /// <returns>The success value or the factory result.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T UnwrapOrElse(Func<TError, T> defaultFactory) =>
            _isSuccess ? _value! : defaultFactory(_error!);

        /// <summary>
        /// Returns the success value or <c>default(T)</c> if Err.
        /// </summary>
        /// <returns>The success value or the default for <typeparamref name="T"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? UnwrapOrDefault() => _isSuccess ? _value : default;

        // ── Transforming contained values ─────────────────────────

        /// <summary>
        /// Transforms the Ok value with <paramref name="transform"/>.
        /// Err values are passed through unchanged.
        /// </summary>
        /// <param name="transform">The function to apply to the contained value.</param>
        /// <returns>A new Result containing the transformed value, or the original error.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<TNew, TError> Map<TNew>(Func<T, TNew> transform) =>
            _isSuccess ? Result<TNew, TError>.Ok(transform(_value!)) : Result<TNew, TError>.Err(_error!);

        /// <summary>
        /// Transforms the Err value with <paramref name="transform"/>.
        /// Ok values are passed through unchanged.
        /// </summary>
        /// <param name="transform">The transform applied to the error.</param>
        /// <returns>A result with the transformed error.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<T, TNewError> MapErr<TNewError>(Func<TError, TNewError> transform) =>
            _isSuccess ? Result<T, TNewError>.Ok(_value!) : Result<T, TNewError>.Err(transform(_error!));

        /// <summary>
        /// Transforms the Ok value with <paramref name="onOk"/>, or returns
        /// <paramref name="defaultValue"/> if Err. The default is evaluated eagerly;
        /// use <see cref="MapOrElse{TResult}"/> for lazy evaluation.
        /// </summary>
        /// <param name="onOk">Function invoked with the value when Ok.</param>
        /// <param name="defaultValue">Value returned when Err.</param>
        /// <returns>The transformed value or the default.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult MapOr<TResult>(Func<T, TResult> onOk, TResult defaultValue) =>
            _isSuccess ? onOk(_value!) : defaultValue;

        /// <summary>
        /// Transforms the Ok value with <paramref name="onOk"/>, or invokes
        /// <paramref name="onErr"/> with the error if Err.
        /// </summary>
        /// <param name="onOk">Function invoked with the value when Ok.</param>
        /// <param name="onErr">Function invoked with the error when Err.</param>
        /// <returns>The result of whichever branch was taken.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult MapOrElse<TResult>(Func<T, TResult> onOk, Func<TError, TResult> onErr) =>
            _isSuccess ? onOk(_value!) : onErr(_error!);

        // ── Side-effects ─────────────────────────────────────────

        /// <summary>
        /// Executes <paramref name="action"/> if Ok. Returns self for chaining.
        /// </summary>
        /// <param name="action">The action to execute on the contained value.</param>
        /// <returns>This result unchanged.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<T, TError> Inspect(Action<T> action)
        {
            if (_isSuccess) action(_value!);
            return this;
        }

        /// <summary>
        /// Executes <paramref name="action"/> if Err. Returns self for chaining.
        /// </summary>
        /// <param name="action">The action to execute on the contained error.</param>
        /// <returns>This result unchanged.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<T, TError> InspectErr(Action<TError> action)
        {
            if (!_isSuccess) action(_error!);
            return this;
        }

        // ── Boolean operators ─────────────────────────────────────

        /// <summary>
        /// Returns <paramref name="other"/> if this result is Ok; otherwise returns the Err value.
        /// The <paramref name="other"/> argument is evaluated eagerly;
        /// use <see cref="AndThen{TNew}"/> for lazy evaluation.
        /// </summary>
        /// <param name="other">The result to return when this is Ok.</param>
        /// <returns><paramref name="other"/> if Ok; otherwise Err.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<TNew, TError> And<TNew>(Result<TNew, TError> other) =>
            _isSuccess ? other : Result<TNew, TError>.Err(_error!);

        /// <summary>
        /// Returns <paramref name="other"/> (void result) if this result is Ok;
        /// otherwise returns the Err value.
        /// </summary>
        /// <param name="other">The void result to return when this is Ok.</param>
        /// <returns><paramref name="other"/> if Ok; otherwise Err.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<TError> And(Result<TError> other) =>
            _isSuccess ? other : Result<TError>.Err(_error!);

        /// <summary>
        /// Flat-maps: if Ok, calls <paramref name="op"/> with the value; otherwise
        /// returns the Err value.
        /// </summary>
        /// <param name="op">The function to apply to the contained value.</param>
        /// <returns>The resulting Result, or the original Err.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<TNew, TError> AndThen<TNew>(Func<T, Result<TNew, TError>> op) =>
            _isSuccess ? op(_value!) : Result<TNew, TError>.Err(_error!);

        /// <summary>
        /// Returns this result if Ok; otherwise returns <paramref name="other"/>.
        /// The <paramref name="other"/> argument is evaluated eagerly;
        /// use <see cref="OrElse{TNewError}"/> for lazy evaluation.
        /// </summary>
        /// <param name="other">The fallback result.</param>
        /// <returns>This result if Ok; otherwise <paramref name="other"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<T, TNewError> Or<TNewError>(Result<T, TNewError> other) =>
            _isSuccess ? Result<T, TNewError>.Ok(_value!) : other;

        /// <summary>
        /// Returns this result if Ok; otherwise calls <paramref name="op"/> with the error.
        /// </summary>
        /// <param name="op">The function to apply to the error.</param>
        /// <returns>This result if Ok; otherwise the function result.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<T, TNewError> OrElse<TNewError>(Func<TError, Result<T, TNewError>> op) =>
            _isSuccess ? Result<T, TNewError>.Ok(_value!) : op(_error!);

        // ── Interop with Nullable ─────────────────────────────────

        /// <summary>
        /// Converts to a nullable, returning null on failure.
        /// </summary>
        /// <returns>The success value or <see langword="null"/> on failure.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? ToNullable() => _isSuccess ? _value : default;

        // ── ToString ─────────────────────────────────────────────

        /// <summary>
        /// Returns "Ok(value)" or "Err(error)".
        /// </summary>
        public override string ToString() => _isSuccess ? $"Ok({_value})" : $"ErrToOption({_error})";
    }

    /// <summary>
    /// Represents the result of an operation that can succeed (no value) or fail with an error.
    /// Use for void-returning operations that may fail.
    /// </summary>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    public readonly record struct Result<TError>
    {
        private readonly TError? _error;
        private readonly bool _isSuccess;

        private Result(bool isSuccess, TError? error = default)
        {
            _isSuccess = isSuccess;
            _error = error;
        }

        // ── Querying the variant ───────────────────────────────────

        /// <summary>
        /// True if the result is Ok (success).
        /// </summary>
        public bool IsOk
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _isSuccess;
        }

        /// <summary>
        /// True if the result is Err (failure).
        /// </summary>
        public bool IsErr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !_isSuccess;
        }

        /// <summary>
        /// The error value. Throws if IsSuccess.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the result is a success.</exception>
        public TError Error => !_isSuccess
            ? _error!
            : throw new InvalidOperationException("Cannot access Error on successful Result");

        /// <summary>
        /// The error value, or default if IsSuccess.
        /// </summary>
        public TError? ErrorOrDefault => _error;

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        /// <returns>A successful result.</returns>
        public static Result<TError> Ok() => new(true);

        /// <summary>
        /// Creates a successful Result&lt;T, TError&gt; with the specified value.
        /// Allows Ok(value) syntax when 'using static Result&lt;TError&gt;' is present.
        /// </summary>
        /// <param name="value">The success value.</param>
        /// <returns>A successful result containing the value.</returns>
        public static Result<T, TError> Ok<T>(T value) => Result<T, TError>.Ok(value);

        /// <summary>
        /// Creates a failed result with the specified error.
        /// </summary>
        /// <param name="error">The error value.</param>
        /// <returns>A failed result containing the error.</returns>
        public static Result<TError> Err(TError? error = default) => new(false, error);

        /// <summary>
        /// Creates a failed Result&lt;T, TError&gt; with the specified error.
        /// Allows Err(error) syntax when 'using static Result&lt;TError&gt;' is present.
        /// </summary>
        /// <param name="error">The error value.</param>
        /// <returns>A failed value result containing the error.</returns>
        public static Result<T, TError> Err<T>(TError? error = default) =>
            Result<T, TError>.Err(error!);

        /// <summary>
        /// Deconstructs the result for pattern matching.
        /// Allows for usage like:
        ///   var (isSuccess, error) = result;
        /// </summary>
        /// <param name="isSuccess">True if the result is successful.</param>
        /// <param name="error">The error value.</param>
        public void Deconstruct(out bool isSuccess, out TError? error)
        {
            isSuccess = _isSuccess;
            error = _error;
        }

        /// <summary>
        /// Try to get the error.
        /// </summary>
        /// <param name="error">The error value.</param>
        /// <returns>True if the result is a failure and the error is set; otherwise, false.</returns>
        public bool TryGetError(out TError? error)
        {
            error = _error;
            return !_isSuccess;
        }

        // ── Transforms ─────────────────────────────────────────────────

        /// <summary>
        /// Transforms the Err value with <paramref name="transform"/>.
        /// Ok is passed through unchanged.
        /// </summary>
        /// <param name="transform">The transform applied to the error.</param>
        /// <returns>A result with the transformed error.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<TNewError> MapErr<TNewError>(Func<TError, TNewError> transform) =>
            _isSuccess ? Result<TNewError>.Ok() : Result<TNewError>.Err(transform(_error!));

        /// <summary>
        /// Transforms the Ok value with <paramref name="onOk"/>, or invokes
        /// <paramref name="onErr"/> with the error if Err.
        /// </summary>
        /// <param name="onOk">Function invoked when Ok.</param>
        /// <param name="onErr">Function invoked with the error when Err.</param>
        /// <returns>The result of whichever branch was taken.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult MapOrElse<TResult>(Func<TResult> onOk, Func<TError, TResult> onErr) =>
            _isSuccess ? onOk() : onErr(_error!);

        // ── Side-effects ─────────────────────────────────────────────

        /// <summary>
        /// Executes <paramref name="action"/> if Ok. Returns self for chaining.
        /// </summary>
        /// <param name="action">The action to execute on success.</param>
        /// <returns>This result unchanged.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<TError> Inspect(Action action)
        {
            if (_isSuccess) action();
            return this;
        }

        /// <summary>
        /// Executes <paramref name="action"/> if Err. Returns self for chaining.
        /// </summary>
        /// <param name="action">The action to execute on the contained error.</param>
        /// <returns>This result unchanged.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<TError> InspectErr(Action<TError> action)
        {
            if (!_isSuccess) action(_error!);
            return this;
        }

        // ── Boolean operators ─────────────────────────────────────────

        /// <summary>
        /// Returns <paramref name="other"/> if this result is Ok; otherwise returns the Err value.
        /// The <paramref name="other"/> argument is evaluated eagerly;
        /// use <see cref="AndThen"/> for lazy evaluation.
        /// </summary>
        /// <param name="other">The result to return when this is Ok.</param>
        /// <returns><paramref name="other"/> if Ok; otherwise Err.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<TError> And(Result<TError> other) =>
            _isSuccess ? other : this;

        /// <summary>
        /// Flat-maps: if Ok, calls <paramref name="op"/>; otherwise returns the Err value.
        /// </summary>
        /// <param name="op">The function to invoke on success.</param>
        /// <returns>The resulting Result, or the original Err.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<TError> AndThen(Func<Result<TError>> op) =>
            _isSuccess ? op() : this;

        /// <summary>
        /// Returns this result if Ok; otherwise returns <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The fallback result.</param>
        /// <returns>This result if Ok; otherwise <paramref name="other"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<TNewError> Or<TNewError>(Result<TNewError> other) =>
            _isSuccess ? Result<TNewError>.Ok() : other;

        /// <summary>
        /// Returns this result if Ok; otherwise calls <paramref name="op"/> with the error.
        /// </summary>
        /// <param name="op">The function to apply to the error.</param>
        /// <returns>This result if Ok; otherwise the function result.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<TNewError> OrElse<TNewError>(Func<TError, Result<TNewError>> op) =>
            _isSuccess ? Result<TNewError>.Ok() : op(_error!);

        /// <summary>
        /// Converts to Result&lt;T, TError&gt; with the specified value on success.
        /// </summary>
        /// <param name="value">The value to return on success.</param>
        /// <returns>A value result carrying the provided value on success.</returns>
        public Result<T, TError> WithValue<T>(T value) =>
            _isSuccess ? Result<T, TError>.Ok(value) : Result<T, TError>.Err(_error!);

        /// <summary>
        /// Combines multiple Results, returning first failure or success if all succeed.
        /// </summary>
        /// <param name="results">The results to combine.</param>
        /// <returns>The first failure or success if all succeed.</returns>
        public static Result<TError> Combine(params Result<TError>[] results)
        {
            foreach (var result in results)
            {
                if (result.IsErr) return result;
            }
            return Ok();
        }

        // ── ToString ─────────────────────────────────────────────────

        /// <summary>
        /// Returns "Ok" or "Err(error)".
        /// </summary>
        public override string ToString() => _isSuccess ? "Ok" : $"ErrToOption({_error})";
    }

    /// <summary>
    /// Extension methods for async Result operations and type conversions.
    /// <para>
    /// These methods are implemented as extensions rather than instance methods on
    /// <see cref="Result{T, TError}"/> and <see cref="Result{TError}"/> for several reasons:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///       <b>Async chaining on Task&lt;Result&gt;</b>: Methods like <c>Then</c> that operate on
    ///       <c>Task&lt;Result&lt;T, TError&gt;&gt;</c> cannot be instance methods because the task
    ///       wrapper is a different type from the Result struct itself.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Type conversions</b>: Methods like <see cref="ToResult{T, TError}"/> convert from
    ///       external types (tuples, nullable references) into Results. These naturally belong as
    ///       extensions on the source types rather than static factory methods.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Additional type constraints</b>: Some conversions require constraints like
    ///       <c>where TError : class</c> that would be inappropriate for the general-purpose
    ///       Result structs.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Separation of concerns</b>: Keeps the core Result types focused on synchronous,
    ///       allocation-free operations while async support remains opt-in via this extensions class.
    ///     </description>
    ///   </item>
    /// </list>
    /// </summary>
    public static class ResultExtensions
    {
        /// <summary>
        /// Converts a tuple of (error, value) to Result&lt;T, TError&gt;.
        /// </summary>
        /// <param name="tuple">The tuple containing an error and value.</param>
        /// <returns>The converted result.</returns>
        public static Result<T, TError> ToResult<T, TError>(this (TError? error, T? value) tuple)
            where TError : class =>
            tuple.error == null ? Result<T, TError>.Ok(tuple.value!) : Result<T, TError>.Err(tuple.error);

        /// <summary>
        /// Converts a nullable error to Result&lt;TError&gt; (null = success).
        /// </summary>
        /// <param name="error">The error value.</param>
        /// <returns>The converted result.</returns>
        public static Result<TError> ToResult<TError>(this TError? error) where TError : class =>
            error == null ? Result<TError>.Ok() : Result<TError>.Err(error);

        // ── Async Map / AndThen ──────────────────────────────────────

        /// <summary>
        /// Chains an async transform that cannot fail.
        /// </summary>
        /// <param name="resultTask">The task producing the result.</param>
        /// <param name="transform">The transform to apply on success.</param>
        /// <returns>A task containing the transformed result.</returns>
        public static async Task<Result<TNew, TError>> Map<T, TNew, TError>(
            this Task<Result<T, TError>> resultTask,
            Func<T, TNew> transform)
        {
            var result = await resultTask;
            return result.Map(transform);
        }

        /// <summary>
        /// Chains a sync operation that can fail after an async Result.
        /// </summary>
        /// <param name="resultTask">The task producing the result.</param>
        /// <param name="op">The next operation to execute on success.</param>
        /// <returns>A task containing the next result.</returns>
        public static async Task<Result<TNew, TError>> AndThen<T, TNew, TError>(
            this Task<Result<T, TError>> resultTask,
            Func<T, Result<TNew, TError>> op)
        {
            var result = await resultTask;
            return result.AndThen(op);
        }

        /// <summary>
        /// Chains an async operation that can fail.
        /// </summary>
        /// <param name="resultTask">The task producing the result.</param>
        /// <param name="op">The async operation to execute on success.</param>
        /// <returns>A task containing the next result.</returns>
        public static async Task<Result<TNew, TError>> AndThen<T, TNew, TError>(
            this Task<Result<T, TError>> resultTask,
            Func<T, Task<Result<TNew, TError>>> op)
        {
            var result = await resultTask;
            return result.IsOk
                ? await op(result.Value)
                : Result<TNew, TError>.Err(result.Error);
        }

        /// <summary>
        /// Chains async operations for void Results.
        /// </summary>
        /// <param name="resultTask">The task producing the result.</param>
        /// <param name="op">The async operation to execute on success.</param>
        /// <returns>A task containing the next result.</returns>
        public static async Task<Result<TError>> AndThen<TError>(
            this Task<Result<TError>> resultTask,
            Func<Task<Result<TError>>> op)
        {
            var result = await resultTask;
            return result.IsOk ? await op() : result;
        }

        /// <summary>
        /// Chains a sync operation for void Results after an async Result.
        /// </summary>
        /// <param name="resultTask">The task producing the result.</param>
        /// <param name="op">The operation to execute on success.</param>
        /// <returns>A task containing the next result.</returns>
        public static async Task<Result<TError>> AndThen<TError>(
            this Task<Result<TError>> resultTask,
            Func<Result<TError>> op)
        {
            var result = await resultTask;
            return result.AndThen(op);
        }

        /// <summary>
        /// Transforms the error of an async Result if failed.
        /// </summary>
        /// <param name="resultTask">The task producing the result.</param>
        /// <param name="transform">The transform applied to the error.</param>
        /// <returns>A task containing the result with a transformed error.</returns>
        public static async Task<Result<T, TNewError>> MapErr<T, TError, TNewError>(
            this Task<Result<T, TError>> resultTask,
            Func<TError, TNewError> transform)
        {
            var result = await resultTask;
            return result.MapErr(transform);
        }

        // ── Flatten / Transpose / Conversions ────────────────────────

        /// <summary>
        /// Flattens a nested <c>Result&lt;Result&lt;T, TError&gt;, TError&gt;</c> into
        /// <c>Result&lt;T, TError&gt;</c>.
        /// </summary>
        /// <param name="result">The nested result.</param>
        /// <returns>The inner result if Ok; otherwise the outer Err.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T, TError> Flatten<T, TError>(
            this Result<Result<T, TError>, TError> result) =>
            result.IsOk ? result.Value : Result<T, TError>.Err(result.Error);

        /// <summary>
        /// Transposes a <c>Result&lt;Option&lt;T&gt;, TError&gt;</c> into an
        /// <c>Option&lt;Result&lt;T, TError&gt;&gt;</c>.
        /// <para>
        /// <c>Ok(None)</c> becomes <c>None</c>;
        /// <c>Ok(Some(v))</c> becomes <c>Some(Ok(v))</c>;
        /// <c>Err(e)</c> becomes <c>Some(Err(e))</c>.
        /// </para>
        /// </summary>
        /// <param name="result">The result wrapping an option.</param>
        /// <returns>An option wrapping a result.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<Result<T, TError>> Transpose<T, TError>(
            this Result<Option<T>, TError> result)
        {
            if (result.IsErr)
                return Option<Result<T, TError>>.Some(Result<T, TError>.Err(result.Error));

            var inner = result.Value;
            return inner.IsSome
                ? Option<Result<T, TError>>.Some(Result<T, TError>.Ok(inner.Value))
                : Option<Result<T, TError>>.None;
        }

        /// <summary>
        /// Converts the error to an Option: Err becomes Some; Ok becomes None.
        /// Equivalent to Rust's <c>err()</c> method on Result.
        /// </summary>
        /// <param name="result">The result to convert.</param>
        /// <returns>Some containing the error if Err; otherwise None.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<TError> ErrToOption<T, TError>(this Result<T, TError> result) =>
            result.IsErr ? Option<TError>.Some(result.Error) : Option<TError>.None;
    }
}
