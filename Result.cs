using System.Diagnostics.CodeAnalysis;

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
        /// If value is null/default and T is a collection type, an empty collection is used instead.
        /// </summary>
        /// <param name="value">The success value, or null/default for collection types to get an empty collection.</param>
        /// <param name="_">Dummy parameter only needed to resolve call to correct constructor overload.</param>
        private Result(T? value, bool _)
        {
            _value = IsNull(value) ? GetDefaultValue() : value;
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

        /// <summary>
        /// Determines if a value is null.  Returns true if null, false if
        /// not null.  Specifically designed to return false if the value
        /// is default for value types.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if the value is null.</returns>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Needed to suppress nuisance message")]
        private static bool IsNull(T? value)
        {
            // Do not check for default for value types.
#pragma warning disable S2955 // Generic type parameter should be constrained to a class or use 'EqualityComparer<T>.Default' or 'object.Equals'
            if (value == null) return true;
#pragma warning restore S2955
            return false;
        }

        /// <summary>
        /// True if the operation succeeded.
        /// </summary>
        public bool IsSuccess => _isSuccess;

        /// <summary>
        /// True if the operation failed.
        /// </summary>
        public bool IsFailure => !_isSuccess;

        /// <summary>
        /// The success value. Throws if IsFailure.
        /// </summary>
        public T Value => _isSuccess
            ? _value!
            : throw new InvalidOperationException($"Cannot access Value on failed Result: {_error}");

        /// <summary>
        /// The error value. Throws if IsSuccess.
        /// </summary>
        public TError Error => !_isSuccess
            ? _error!
            : throw new InvalidOperationException("Cannot access Error on successful Result");

        /// <summary>
        /// The error value, or default if IsSuccess.
        /// </summary>
        public TError? ErrorOrDefault => _error;

        /// <summary>
        /// Creates a successful result with the specified value.
        /// If value is null/default and T is a collection type, an empty collection is used instead.
        /// </summary>
        /// <param name="value">The success value, or default for collection types to get an empty collection.</param>
        /// <returns>A successful Result containing the value or an appropriate default.</returns>
        public static Result<T, TError> Ok(T? value = default) => new(value, true);

        /// <summary>
        /// Creates a failed result with the specified error.
        /// To create an error result, use Result&lt;TError&gt;.Err(error) and 
        /// cast the Result&lt;TError&gt; to Result&lt;T, TError&gt;
        /// </summary>
        /// <param name="error">The error value.</param>
        /// <returns>A failed result containing the error.</returns>
        public static Result<T, TError> Err(TError error) => new(error);

        /// <summary>
        /// Throw exception if the type is not string or collection type.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private static T? GetDefaultValueOrThrow()
        {
            T? value = GetDefaultValue();
            if (EqualityComparer<T>.Default.Equals(value, default))
            {
                // For all other types, throw an error since this may otherwise cause a subtle application malfunction.
                throw new InvalidOperationException("Cannot cast Ok() to Result<T, TError>");
            }
            return value;
        }

        /// <summary>
        /// Gets the default value for type T.
        /// Returns an empty collection for array types and common collection types
        /// (List, Dictionary, HashSet, Queue, Stack, etc.), or default(T) for other types.
        /// </summary>
        /// <returns>An appropriate default value for type T.</returns>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Needed to suppress nuisance message")]
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Type T (the array type) must appear in the application in any case.")]
        [SuppressMessage("Trimming", "IL2090:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The generic parameter of the source method or type does not have matching annotations.", Justification = "<Pending>")]
        private static T? GetDefaultValue()
        {
            var type = typeof(T);

            // Handle array types - create empty array
            if (type.IsArray)
            {
                var elementType = type.GetElementType()!;
                return (T)(object)Array.CreateInstance(elementType, 0);
            }

            // Handle string type - return empty string instead of null
            if (type == typeof(string))
            {
                return (T)(object)string.Empty;
            }

            // Handle generic collection types that have parameterless constructors
            // This covers List<>, Dictionary<,>, HashSet<>, Queue<>, Stack<>, 
            // LinkedList<>, SortedSet<>, SortedList<,>, SortedDictionary<,>, etc.
            if (type.IsGenericType &&
                typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
            {
                var constructor = type.GetConstructor(Type.EmptyTypes);
                if (constructor != null)
                {
                    return (T)constructor.Invoke(null);
                }
            }

            // Handle non-generic collection types (ArrayList, Hashtable, etc.)
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
            {
                var constructor = type.GetConstructor(Type.EmptyTypes);
                if (constructor != null)
                {
                    return (T)constructor.Invoke(null);
                }
            }

            return default;
        }

        /// <summary>
        /// Implicitly converts a void-style Result (Result&lt;TError&gt;) into a value Result (Result&lt;T, TError&gt;).
        /// On success the conversion returns an appropriate default value (empty array/list for collection types).
        /// On failure the error is preserved.
        /// </summary>
        /// <param name="result">Source Result&lt;TError&gt; to convert.</param>
        /// <returns>Converted Result&lt;T, TError&gt;.</returns>
        public static implicit operator Result<T, TError>(Result<TError> result) => 
            result.IsSuccess ? new Result<T, TError>(GetDefaultValueOrThrow(), true) : Err(result.Error);
        

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

        /// <summary>
        /// Returns the value if successful, or the specified default if failed.
        /// </summary>
        /// <param name="defaultValue">The default value to return on failure.</param>
        /// <returns>The success value or the provided default.</returns>
        public T ValueOr(T defaultValue) => _isSuccess ? _value! : defaultValue;

        /// <summary>
        /// Returns the value if successful, or invokes the factory to get a default.
        /// </summary>
        /// <param name="defaultFactory">Factory invoked when the result is a failure.</param>
        /// <returns>The success value or the factory result.</returns>
        public T ValueOr(Func<TError, T> defaultFactory) =>
            _isSuccess ? _value! : defaultFactory(_error!);

        /// <summary>
        /// Chains a transform that cannot fail.
        /// If successful, applies the transform to the value; otherwise propagates the error.
        /// </summary>
        /// <param name="transform">The transform to apply on success.</param>
        /// <returns>The transformed result or the original error.</returns>
        /// <example>
        /// result.Then(x => x.ToString())  // Transform int to string
        /// </example>
        public Result<TNew, TError> Then<TNew>(Func<T, TNew> transform) =>
            _isSuccess ? Result<TNew, TError>.Ok(transform(_value!)) : Result<TNew, TError>.Err(_error!);

        /// <summary>
        /// Chains an operation that can fail.
        /// If successful, executes the next operation; otherwise propagates the error.
        /// </summary>
        /// <param name="nextStep">The operation to execute on success.</param>
        /// <returns>The next result or the original error.</returns>
        /// <example>
        /// result.Then(x => Validate(x))  // Validate returns Result
        /// </example>
        public Result<TNew, TError> Then<TNew>(Func<T, Result<TNew, TError>> nextStep) =>
            _isSuccess ? nextStep(_value!) : Result<TNew, TError>.Err(_error!);

        /// <summary>
        /// Chains an operation that can fail.
        /// If successful, returns the next result; otherwise propagates the error.
        /// Warning: The nextResult argument is evaluated eagerly!
        /// </summary>
        /// <param name="nextResult">The next result to return on success.</param>
        /// <returns>The next result or the original error.</returns>
        public Result<TNew, TError> Then<TNew>(Result<TNew, TError> nextResult) =>
            _isSuccess ? nextResult : Result<TNew, TError>.Err(_error!);

        /// <summary>
        /// Chains an operation that can fail.
        /// If successful, returns the next result; otherwise propagates the error.
        /// Warning: The nextResult argument is evaluated eagerly!
        /// </summary>
        /// <param name="nextResult">The next result to return on success.</param>
        /// <returns>The next result or the original error.</returns>
        public Result<TError> Then(Result<TError> nextResult) =>
            _isSuccess ? nextResult : Result<TError>.Err(_error!);

        /// <summary>
        /// Transforms the error if failed, preserving success. This is useful when
        ///  - Converting between error types at layer boundaries
        ///  - Adding context to errors as they bubble up
        ///  - Translating technical errors to user-friendly messages
        /// </summary>
        /// <param name="transform">The transform applied to the error.</param>
        /// <returns>A result with the transformed error.</returns>
        public Result<T, TNewError> MapError<TNewError>(Func<TError, TNewError> transform) =>
            _isSuccess ? Result<T, TNewError>.Ok(_value!) : Result<T, TNewError>.Err(transform(_error!));

        /// <summary>
        /// Executes an action if successful, returns self for chaining.
        /// </summary>
        /// <param name="action">The action to execute on success.</param>
        /// <returns>The current result.</returns>
        public Result<T, TError> OnSuccess(Action<T> action)
        {
            if (_isSuccess) action(_value!);
            return this;
        }

        /// <summary>
        /// Executes an action if failed, returns self for chaining.
        /// </summary>
        /// <param name="action">The action to execute on failure.</param>
        /// <returns>The current result.</returns>
        public Result<T, TError> OnFailure(Action<TError> action)
        {
            if (!_isSuccess) action(_error!);
            return this;
        }

        /// <summary>
        /// Pattern matches on success or failure.
        /// </summary>
        /// <param name="onSuccess">The function to invoke on success.</param>
        /// <param name="onFailure">The function to invoke on failure.</param>
        /// <returns>The result of the invoked function.</returns>
        public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<TError, TResult> onFailure) =>
            _isSuccess ? onSuccess(_value!) : onFailure(_error!);

        /// <summary>
        /// Converts to a nullable, returning null on failure.
        /// </summary>
        /// <returns>The success value or <see langword="null"/> on failure.</returns>
        public T? ToNullable() => _isSuccess ? _value : default;

        /// <summary>
        /// Unwraps the value, throwing if failed. Alias for Value property.
        /// </summary>
        /// <returns>The success value.</returns>
        public T Unwrap() => Value;

        /// <summary>
        /// Unwraps the error, throwing if successful. Alias for Error property.
        /// </summary>
        /// <returns>The error value.</returns>
        public TError UnwrapError() => Error;
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

        /// <summary>
        /// True if the operation succeeded.
        /// </summary>
        public bool IsSuccess => _isSuccess;

        /// <summary>
        /// True if the operation failed.
        /// </summary>
        public bool IsFailure => !_isSuccess;

        /// <summary>
        /// The error value. Throws if IsSuccess.
        /// </summary>
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

        /// <summary>
        /// Chains an operation that can fail.
        /// If successful, executes the next operation; otherwise propagates the error.
        /// </summary>
        /// <param name="nextStep">The operation to execute on success.</param>
        /// <returns>The next result or the original error.</returns>
        public Result<TError> Then(Func<Result<TError>> nextStep) =>
            _isSuccess ? nextStep() : this;

        /// <summary>
        /// Chains an operation that can fail.
        /// If successful, returns the next result; otherwise propagates the error.
        /// Warning: The nextResult argument is evaluated eagerly!
        /// </summary>
        /// <param name="nextResult">The next result to return on success.</param>
        /// <returns>The next result or the original error.</returns>
        public Result<TError> Then(Result<TError> nextResult) =>
            _isSuccess ? nextResult : this;

        /// <summary>
        /// Transforms the error if failed, preserving success. This is useful when
        ///  - Converting between error types at layer boundaries
        ///  - Adding context to errors as they bubble up
        ///  - Translating technical errors to user-friendly messages
        /// </summary>
        /// <param name="transform">The transform applied to the error.</param>
        /// <returns>A result with the transformed error.</returns>
        public Result<TNewError> MapError<TNewError>(Func<TError, TNewError> transform) =>
            _isSuccess ? Result<TNewError>.Ok() : Result<TNewError>.Err(transform(_error!));

        /// <summary>
        /// Executes an action if successful, returns self for chaining.
        /// </summary>
        /// <param name="action">The action to execute on success.</param>
        /// <returns>The current result.</returns>
        public Result<TError> OnSuccess(Action action)
        {
            if (_isSuccess) action();
            return this;
        }

        /// <summary>
        /// Executes an action if failed, returns self for chaining.
        /// </summary>
        /// <param name="action">The action to execute on failure.</param>
        /// <returns>The current result.</returns>
        public Result<TError> OnFailure(Action<TError> action)
        {
            if (!_isSuccess) action(_error!);
            return this;
        }

        /// <summary>
        /// Pattern matches on success or failure.
        /// </summary>
        /// <param name="onSuccess">The function to invoke on success.</param>
        /// <param name="onFailure">The function to invoke on failure.</param>
        /// <returns>The result of the invoked function.</returns>
        public TResult Match<TResult>(Func<TResult> onSuccess, Func<TError, TResult> onFailure) =>
            _isSuccess ? onSuccess() : onFailure(_error!);

        /// <summary>
        /// Converts to Result&lt;T, TError&gt; with the specified value on success.
        /// </summary>
        /// <param name="value">The value to return on success.</param>
        /// <returns>A value result carrying the provided value on success.</returns>
        public Result<T, TError> WithValue<T>(T value) =>
            _isSuccess ? Result<T, TError>.Ok(value) : Err(_error!);

        /// <summary>
        /// Combines multiple Results, returning first failure or success if all succeed.
        /// </summary>
        /// <param name="results">The results to combine.</param>
        /// <returns>The first failure or success if all succeed.</returns>
        public static Result<TError> Combine(params Result<TError>[] results)
        {
            foreach (var result in results)
            {
                if (result.IsFailure) return result;
            }
            return Ok();
        }
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

        /// <summary>
        /// Chains an async transform that cannot fail.
        /// </summary>
        /// <param name="resultTask">The task producing the result.</param>
        /// <param name="transform">The transform to apply on success.</param>
        /// <returns>A task containing the transformed result.</returns>
        /// <example>
        /// await GetDataAsync().Then(x => x.ToString())
        /// </example>
        public static async Task<Result<TNew, TError>> Then<T, TNew, TError>(
            this Task<Result<T, TError>> resultTask,
            Func<T, TNew> transform)
        {
            var result = await resultTask;
            return result.Then(transform);
        }

        /// <summary>
        /// Chains a sync operation that can fail after an async Result.
        /// </summary>
        /// <param name="resultTask">The task producing the result.</param>
        /// <param name="nextStep">The next operation to execute on success.</param>
        /// <returns>A task containing the next result.</returns>
        /// <example>
        /// await GetDataAsync().Then(x => Validate(x))
        /// </example>
        public static async Task<Result<TNew, TError>> Then<T, TNew, TError>(
            this Task<Result<T, TError>> resultTask,
            Func<T, Result<TNew, TError>> nextStep)
        {
            var result = await resultTask;
            return result.Then(nextStep);
        }

        /// <summary>
        /// Chains an async operation that can fail.
        /// </summary>
        /// <param name="resultTask">The task producing the result.</param>
        /// <param name="nextStep">The async operation to execute on success.</param>
        /// <returns>A task containing the next result.</returns>
        /// <example>
        /// await GetDataAsync()
        ///     .Then(data => ProcessAsync(data))
        ///     .Then(processed => SaveAsync(processed));
        /// </example>
        public static async Task<Result<TNew, TError>> Then<T, TNew, TError>(
            this Task<Result<T, TError>> resultTask,
            Func<T, Task<Result<TNew, TError>>> nextStep)
        {
            var result = await resultTask;
            return result.IsSuccess
                ? await nextStep(result.Value)
                : Result<TNew, TError>.Err(result.Error);
        }

        /// <summary>
        /// Chains async operations for void Results.
        /// </summary>
        /// <param name="resultTask">The task producing the result.</param>
        /// <param name="nextStep">The async operation to execute on success.</param>
        /// <returns>A task containing the next result.</returns>
        public static async Task<Result<TError>> Then<TError>(
            this Task<Result<TError>> resultTask,
            Func<Task<Result<TError>>> nextStep)
        {
            var result = await resultTask;
            return result.IsSuccess ? await nextStep() : result;
        }

        /// <summary>
        /// Chains a sync operation for void Results after an async Result.
        /// </summary>
        /// <param name="resultTask">The task producing the result.</param>
        /// <param name="nextStep">The operation to execute on success.</param>
        /// <returns>A task containing the next result.</returns>
        public static async Task<Result<TError>> Then<TError>(
            this Task<Result<TError>> resultTask,
            Func<Result<TError>> nextStep)
        {
            var result = await resultTask;
            return result.Then(nextStep);
        }

        /// <summary>
        /// Transforms the error of an async Result if failed.
        /// </summary>
        /// <param name="resultTask">The task producing the result.</param>
        /// <param name="transform">The transform applied to the error.</param>
        /// <returns>A task containing the result with a transformed error.</returns>
        public static async Task<Result<T, TNewError>> ThenError<T, TError, TNewError>(
            this Task<Result<T, TError>> resultTask,
            Func<TError, TNewError> transform)
        {
            var result = await resultTask;
            return result.MapError(transform);
        }
    }
}
