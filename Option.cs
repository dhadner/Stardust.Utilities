using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Represents an optional value: every <see cref="Option{T}"/> is either <c>Some(value)</c>
    /// or <c>None</c>. Inspired by Rust's <c>Option&lt;T&gt;</c> type.
    /// <para>
    /// Use this to make the absence of a value explicit in the type system instead of relying
    /// on nullable references or sentinel values. Unlike <c>T?</c>, this works identically for
    /// both value types and reference types, and forces callers to handle the <c>None</c> case.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type of the contained value.</typeparam>
    /// <remarks>
    /// <para>This is a <c>readonly record struct</c> -- zero heap allocation, copy semantics,
    /// structural equality, and the same size as <c>T</c> plus one <c>bool</c>.</para>
    /// <para>All hot-path methods are marked <c>[MethodImpl(AggressiveInlining)]</c> so the
    /// JIT can eliminate the <c>_isSome</c> branch when the state is statically known.</para>
    /// </remarks>
    public readonly record struct Option<T>
    {
        private readonly T? _value;
        private readonly bool _isSome;

        /// <summary>
        /// Creates an Option containing a value.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        private Option(T value)
        {
            _value = value;
            _isSome = true;
        }

        /// <summary>
        /// True if this Option contains a value.
        /// </summary>
        public bool IsSome
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _isSome;
        }

        /// <summary>
        /// True if this Option is empty.
        /// </summary>
        public bool IsNone
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !_isSome;
        }

        /// <summary>
        /// The contained value. Throws if <see cref="IsNone"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the option is None.</exception>
        public T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _isSome
                ? _value!
                : throw new InvalidOperationException("Cannot access Value on None Option");
        }

        // ── Factories ────────────────────────────────────────────────

        /// <summary>
        /// Creates an Option containing the specified value.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        /// <returns>An Option containing <paramref name="value"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> Some(T value) => new(value);

        /// <summary>
        /// Returns an empty Option.
        /// </summary>
        /// <returns>An empty Option.</returns>
        public static Option<T> None => default;

        // ── Conversions ──────────────────────────────────────────────

        /// <summary>
        /// Implicitly wraps a value in <c>Some</c>.
        /// Allows <c>Option&lt;int&gt; x = 42;</c>.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Option<T>(T value) => new(value);

        /// <summary>
        /// Implicitly converts <see cref="NoneOption"/> to a typed <c>None</c>.
        /// Allows <c>Option&lt;int&gt; x = Option.None;</c>.
        /// </summary>
        /// <param name="_">The sentinel value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Option<T>(NoneOption _) => default;

        // ── Query / Extract ──────────────────────────────────────────

        /// <summary>
        /// Attempts to retrieve the value. Returns <see langword="false"/> if <see cref="IsNone"/>.
        /// </summary>
        /// <param name="value">The contained value when the option is Some; otherwise, <c>default</c>.</param>
        /// <returns><see langword="true"/> if the option contains a value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue([MaybeNullWhen(false)] out T value)
        {
            value = _value;
            return _isSome;
        }

        /// <summary>
        /// Unwraps the value, throwing if None. Alias for <see cref="Value"/>.
        /// </summary>
        /// <returns>The contained value.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the option is None.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Unwrap() => Value;

        /// <summary>
        /// Unwraps the value, or throws with a custom message if None.
        /// </summary>
        /// <param name="message">Error message for the exception.</param>
        /// <returns>The contained value.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the option is None.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Expect(string message) => _isSome
            ? _value!
            : throw new InvalidOperationException(message);

        /// <summary>
        /// Returns the contained value or <paramref name="defaultValue"/> if None.
        /// </summary>
        /// <param name="defaultValue">The fallback value.</param>
        /// <returns>The contained value or the fallback.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T UnwrapOr(T defaultValue) => _isSome ? _value! : defaultValue;

        /// <summary>
        /// Returns the contained value or invokes <paramref name="defaultFactory"/> if None.
        /// </summary>
        /// <param name="defaultFactory">Factory invoked lazily when the option is None.</param>
        /// <returns>The contained value or the factory result.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T UnwrapOrElse(Func<T> defaultFactory) => _isSome ? _value! : defaultFactory();

        /// <summary>
        /// Returns the contained value or <c>default(T)</c> if None.
        /// For value types this returns the type's zero/false value; for reference types it
        /// returns <see langword="null"/>.
        /// </summary>
        /// <returns>The contained value or the default for <typeparamref name="T"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? UnwrapOrDefault() => _isSome ? _value : default;

        /// <summary>
        /// Returns the contained value without checking whether the option is Some.
        /// <para>
        /// <strong>Safety:</strong> Calling this on a None option does not throw, but the
        /// returned value is <c>default(T)</c> (zero / <see langword="null"/>), which may
        /// cause downstream errors. Only use this when the caller has already verified
        /// <see cref="IsSome"/> and needs to avoid the redundant branch.
        /// </para>
        /// </summary>
        /// <returns>The contained value, or <c>default(T)</c> if None.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? UnwrapUnchecked() => _value;

        // ── Deconstruct ──────────────────────────────────────────────

        /// <summary>
        /// Deconstructs for pattern matching.
        /// <code>var (isSome, value) = option;</code>
        /// </summary>
        /// <param name="isSome">True if the option contains a value.</param>
        /// <param name="value">The contained value, or <c>default</c> if None.</param>
        public void Deconstruct(out bool isSome, [MaybeNullWhen(false)] out T value)
        {
            isSome = _isSome;
#pragma warning disable CS8601 // Possible null reference assignment.
            value = _value;
#pragma warning restore CS8601 // Possible null reference assignment.
        }

        // ── Transforms ───────────────────────────────────────────────

        /// <summary>
        /// Transforms the contained value with <paramref name="transform"/>.
        /// Returns None if this option is None.
        /// </summary>
        /// <param name="transform">The function to apply to the contained value.</param>
        /// <returns>A new Option containing the transformed value, or None.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<TNew> Map<TNew>(Func<T, TNew> transform) =>
            _isSome ? Option<TNew>.Some(transform(_value!)) : Option<TNew>.None;

        /// <summary>
        /// Flat-maps (bind): transforms the contained value into another Option.
        /// Returns None if this option is None.
        /// </summary>
        /// <param name="transform">The function to apply to the contained value.</param>
        /// <returns>The resulting Option, or None.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<TNew> AndThen<TNew>(Func<T, Option<TNew>> transform) =>
            _isSome ? transform(_value!) : Option<TNew>.None;

        /// <summary>
        /// Returns this option if it is Some and <paramref name="predicate"/> returns true;
        /// otherwise returns None.
        /// </summary>
        /// <param name="predicate">The predicate to test the contained value.</param>
        /// <returns>This option if it passes the predicate; otherwise None.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<T> Filter(Func<T, bool> predicate) =>
            _isSome && predicate(_value!) ? this : default;

        // ── Side-effects ─────────────────────────────────────────────

        /// <summary>
        /// Executes <paramref name="action"/> if Some. Returns self for chaining.
        /// </summary>
        /// <param name="action">The action to execute on the contained value.</param>
        /// <returns>This option unchanged.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<T> Inspect(Action<T> action)
        {
            if (_isSome) action(_value!);
            return this;
        }

        // ── Pattern match ────────────────────────────────────────────

        /// <summary>
        /// Transforms the contained value with <paramref name="onSome"/>, or invokes
        /// <paramref name="onNone"/> if None.
        /// </summary>
        /// <param name="onSome">Function invoked with the value when Some.</param>
        /// <param name="onNone">Function invoked when None.</param>
        /// <returns>The result of whichever branch was taken.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult MapOrElse<TResult>(Func<T, TResult> onSome, Func<TResult> onNone) =>
            _isSome ? onSome(_value!) : onNone();

        /// <summary>
        /// Transforms the contained value with <paramref name="onSome"/>, or returns
        /// <paramref name="defaultValue"/> if None. The default value is evaluated eagerly;
        /// use <see cref="MapOrElse{TResult}(Func{T, TResult}, Func{TResult})"/> for lazy
        /// evaluation of the fallback.
        /// </summary>
        /// <param name="onSome">Function invoked with the value when Some.</param>
        /// <param name="defaultValue">Value returned when None.</param>
        /// <returns>The transformed value or the default.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult MapOr<TResult>(Func<T, TResult> onSome, TResult defaultValue) =>
            _isSome ? onSome(_value!) : defaultValue;

        // ── Interop with Nullable ────────────────────────────────────

        /// <summary>
        /// Converts to <c>T?</c>. Returns <c>default</c> if None.
        /// </summary>
        /// <returns>The contained value or <c>default(T)</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? ToNullable() => _isSome ? _value : default;

        // ── Interop with Result ──────────────────────────────────────

        /// <summary>
        /// Converts to <see cref="Result{T, TError}"/>. Some becomes Ok; None becomes Err
        /// with the specified <paramref name="error"/>.
        /// </summary>
        /// <param name="error">The error to use when the option is None.</param>
        /// <returns>A result containing the value or the error.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<T, TError> OkOr<TError>(TError error) =>
            _isSome ? Result<T, TError>.Ok(_value!) : Result<T, TError>.Err(error);

        /// <summary>
        /// Converts to <see cref="Result{T, TError}"/>. Some becomes Ok; None invokes
        /// <paramref name="errorFactory"/> to produce the error.
        /// </summary>
        /// <param name="errorFactory">Factory invoked lazily when the option is None.</param>
        /// <returns>A result containing the value or the error.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<T, TError> OkOrElse<TError>(Func<TError> errorFactory) =>
            _isSome ? Result<T, TError>.Ok(_value!) : Result<T, TError>.Err(errorFactory());

        // ── Combinators ──────────────────────────────────────────────

        /// <summary>
        /// Returns <paramref name="other"/> if this option is Some; otherwise returns None.
        /// Equivalent to Rust's <c>and</c>.
        /// </summary>
        /// <param name="other">The option to return when this is Some.</param>
        /// <returns><paramref name="other"/> if Some; otherwise None.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<TOther> And<TOther>(Option<TOther> other) =>
            _isSome ? other : Option<TOther>.None;

        /// <summary>
        /// Returns this option if Some; otherwise returns <paramref name="other"/>.
        /// Equivalent to Rust's <c>or</c>.
        /// </summary>
        /// <param name="other">The fallback option.</param>
        /// <returns>This option if Some; otherwise <paramref name="other"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<T> Or(Option<T> other) => _isSome ? this : other;

        /// <summary>
        /// Returns this option if Some; otherwise invokes <paramref name="factory"/>.
        /// Equivalent to Rust's <c>or_else</c>.
        /// </summary>
        /// <param name="factory">Factory invoked lazily when this option is None.</param>
        /// <returns>This option if Some; otherwise the factory result.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<T> OrElse(Func<Option<T>> factory) => _isSome ? this : factory();

        /// <summary>
        /// Returns Some if exactly one of this and <paramref name="other"/> is Some.
        /// Equivalent to Rust's <c>xor</c>.
        /// </summary>
        /// <param name="other">The other option.</param>
        /// <returns>Some if exactly one option has a value; otherwise None.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<T> Xor(Option<T> other) => (_isSome, other._isSome) switch
        {
            (true, false) => this,
            (false, true) => other,
            _ => default
        };

        /// <summary>
        /// Zips two options together. Returns Some containing a tuple if both are Some;
        /// otherwise returns None.
        /// </summary>
        /// <param name="other">The other option.</param>
        /// <returns>A tuple of both values if both are Some; otherwise None.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<(T First, TOther Second)> Zip<TOther>(Option<TOther> other) =>
            _isSome && other._isSome
                ? Option<(T, TOther)>.Some((_value!, other._value!))
                : Option<(T, TOther)>.None;

        /// <summary>
        /// Zips two options together using a combining function. Returns Some containing
        /// the result of <paramref name="combine"/> if both options are Some;
        /// otherwise returns None.
        /// </summary>
        /// <param name="other">The other option.</param>
        /// <param name="combine">The function to combine both values.</param>
        /// <returns>An Option containing the combined result, or None.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<TResult> ZipWith<TOther, TResult>(
            Option<TOther> other, Func<T, TOther, TResult> combine) =>
            _isSome && other._isSome
                ? Option<TResult>.Some(combine(_value!, other._value!))
                : Option<TResult>.None;

        // ── Flatten ──────────────────────────────────────────────────

        // Flatten is provided via OptionExtensions.Flatten<T>.

        // ── ToString ─────────────────────────────────────────────────

        /// <summary>
        /// Returns "Some(value)" or "None".
        /// </summary>
        public override string ToString() => _isSome ? $"Some({_value})" : "None";
    }

    /// <summary>
    /// A sentinel type used solely to enable <c>Option.None</c> to convert to any
    /// <c>Option&lt;T&gt;</c> via implicit conversion.
    /// </summary>
    public readonly record struct NoneOption;

    /// <summary>
    /// Non-generic companion providing the untyped <c>None</c> sentinel and factory helpers.
    /// <para>
    /// Add <c>global using static Stardust.Utilities.Option;</c> to your <c>GlobalUsings.cs</c>
    /// to enable unqualified <c>Some(value)</c> and <c>None</c> syntax in all files:
    /// <code>
    /// Option&lt;int&gt; Parse(string s)
    /// {
    ///     if (int.TryParse(s, out var n)) return Some(n);
    ///     return None;
    /// }
    /// </code>
    /// Without the global using, qualify with <c>Option.Some(42)</c> and <c>Option.None</c>.
    /// </para>
    /// </summary>
    public static class Option
    {
        /// <summary>
        /// An untyped <c>None</c> that implicitly converts to any <c>Option&lt;T&gt;</c>.
        /// </summary>
        public static NoneOption None => default;

        /// <summary>
        /// Creates <c>Some(value)</c> with type inference.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        /// <returns>An Option containing the value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> Some<T>(T value) => Option<T>.Some(value);

        /// <summary>
        /// Creates an Option from a nullable value: non-null becomes Some, null becomes None.
        /// </summary>
        /// <param name="value">The nullable value.</param>
        /// <returns>Some if non-null; otherwise None.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> FromNullable<T>(T? value) where T : class =>
            value is not null ? Option<T>.Some(value) : Option<T>.None;

        /// <summary>
        /// Creates an Option from a nullable value type: HasValue becomes Some, null becomes None.
        /// </summary>
        /// <param name="value">The nullable value type.</param>
        /// <returns>Some if HasValue; otherwise None.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> FromNullable<T>(T? value) where T : struct =>
            value.HasValue ? Option<T>.Some(value.Value) : Option<T>.None;
    }

    /// <summary>
    /// Extension methods for <see cref="Option{T}"/> providing flatten, async chaining,
    /// and conversions from external types.
    /// </summary>
    public static class OptionExtensions
    {
        /// <summary>
        /// Flattens a nested <c>Option&lt;Option&lt;T&gt;&gt;</c> into <c>Option&lt;T&gt;</c>.
        /// </summary>
        /// <param name="option">The nested option.</param>
        /// <returns>The inner option if both layers are Some; otherwise None.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> Flatten<T>(this Option<Option<T>> option) =>
            option.IsSome ? option.Value : Option<T>.None;

        /// <summary>
        /// Transposes an <c>Option&lt;Result&lt;T, TError&gt;&gt;</c> into a
        /// <c>Result&lt;Option&lt;T&gt;, TError&gt;</c>.
        /// <para>
        /// <c>None</c> becomes <c>Ok(None)</c>; <c>Some(Ok(v))</c> becomes <c>Ok(Some(v))</c>;
        /// <c>Some(Err(e))</c> becomes <c>Err(e)</c>.
        /// </para>
        /// </summary>
        /// <param name="option">The option wrapping a result.</param>
        /// <returns>A result wrapping an option.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<Option<T>, TError> Transpose<T, TError>(
            this Option<Result<T, TError>> option)
        {
            if (option.IsNone)
                return Result<Option<T>, TError>.Ok(Option<T>.None);

            var inner = option.Value;
            return inner.IsOk
                ? Result<Option<T>, TError>.Ok(Option<T>.Some(inner.Value))
                : Result<Option<T>, TError>.Err(inner.Error);
        }

        /// <summary>
        /// Converts a nullable reference to an Option: non-null becomes Some, null becomes None.
        /// </summary>
        /// <param name="value">The nullable reference.</param>
        /// <returns>Some if non-null; otherwise None.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> ToOption<T>(this T? value) where T : class =>
            value is not null ? Option<T>.Some(value) : Option<T>.None;

        /// <summary>
        /// Converts a nullable value type to an Option: HasValue becomes Some, null becomes None.
        /// </summary>
        /// <param name="value">The nullable value type.</param>
        /// <returns>Some if HasValue; otherwise None.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> ToOption<T>(this T? value) where T : struct =>
            value.HasValue ? Option<T>.Some(value.Value) : Option<T>.None;

        /// <summary>
        /// Converts a <see cref="Result{T, TError}"/> to an Option:
        /// success becomes Some, failure becomes None (the error is discarded).
        /// </summary>
        /// <param name="result">The result to convert.</param>
        /// <returns>Some containing the value if success; otherwise None.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> ToOption<T, TError>(this Result<T, TError> result) =>
            result.IsOk ? Option<T>.Some(result.Value) : Option<T>.None;

        /// <summary>
        /// Chains an async transform that cannot fail.
        /// </summary>
        /// <param name="optionTask">The task producing the option.</param>
        /// <param name="transform">The transform to apply on Some.</param>
        /// <returns>A task containing the transformed option.</returns>
        public static async Task<Option<TNew>> Map<T, TNew>(
            this Task<Option<T>> optionTask,
            Func<T, TNew> transform)
        {
            var option = await optionTask;
            return option.Map(transform);
        }

        /// <summary>
        /// Chains an async flat-map operation.
        /// </summary>
        /// <param name="optionTask">The task producing the option.</param>
        /// <param name="transform">The transform to apply on Some.</param>
        /// <returns>A task containing the resulting option.</returns>
        public static async Task<Option<TNew>> AndThen<T, TNew>(
            this Task<Option<T>> optionTask,
            Func<T, Option<TNew>> transform)
        {
            var option = await optionTask;
            return option.AndThen(transform);
        }

        /// <summary>
        /// Chains an async operation that returns an option.
        /// </summary>
        /// <param name="optionTask">The task producing the option.</param>
        /// <param name="transform">The async transform to apply on Some.</param>
        /// <returns>A task containing the resulting option.</returns>
        public static async Task<Option<TNew>> AndThen<T, TNew>(
            this Task<Option<T>> optionTask,
            Func<T, Task<Option<TNew>>> transform)
        {
            var option = await optionTask;
            return option.IsSome
                ? await transform(option.Value)
                : Option<TNew>.None;
        }
    }
}
