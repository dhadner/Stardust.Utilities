# Option Types

Explicit optional values without null. Inspired by Rust's `Option<T>` type.

## Overview

`Option<T>` represents an optional value: every `Option<T>` is either `Some(value)` or `None`. Use this to make the absence of a value explicit in the type system instead of relying on nullable references or sentinel values.

Unlike `T?`, `Option<T>` works identically for both value types and reference types, and forces callers to handle the `None` case.

**When to use Option:**
- Dictionary lookups that may not find a key
- Parsing operations that may not succeed
- Configuration values that may not be set
- First-or-default searches where "not found" is expected
- Any function where "no value" is a normal outcome, not an error

**When to use Result instead:**
- When you need to know *why* something failed
- Validation with error messages
- I/O operations with specific error types

**When to use nullable (`T?`) instead:**
- Interop with existing APIs that use null
- Tight inner loops using delegate-based methods (`Map`, `AndThen`, `Filter`) where the nanosecond-scale overhead matters -- see [Performance](#performance) for details
- Simple cases where the calling code is entirely local

## Quick Start

```csharp
using Stardust.Utilities;

// Create Some and None
Option<int> some = Option<int>.Some(42);
Option<int> none = Option<int>.None;

// Implicit conversion from value
Option<int> fromValue = 42;

// Untyped None sentinel converts to any Option<T>
Option<int> fromNone = Option.None;

// Type-inferred factory
var inferred = Option.Some(42);

// Safe extraction
if (some.TryGetValue(out var value))
    Console.WriteLine($"Got: {value}");

// Transform chains
var result = Option.Some(3)
    .Map(x => x + 1)
    .Map(x => x * 10)
    .Map(x => x.ToString());
// result == Some("40")
```

## Global Using for Cleaner Syntax

Add a single `global using static` to your `GlobalUsings.cs` to enable unqualified `Some()` and `None` syntax across all files. Unlike `Result`, which needs one import per type combination, `Option` needs only **one import for all `Option<T>` types** because `Some<T>` uses generic type inference and `None` is an untyped sentinel that implicitly converts to any `Option<T>`:

```csharp
// In GlobalUsings.cs
global using static Stardust.Utilities.Option;
// That's it -- one import enables Some() and None for ALL Option<T> types.
```

Now you can write `Some` and `None` unqualified:

```csharp
// Return positions
Option<int> ParsePositive(string s)
{
    if (int.TryParse(s, out var n) && n > 0)
        return Some(n);   // calls Option.Some<int>(n) via type inference
    return None;           // Option.None (NoneOption) -> implicit to Option<int>
}

// Ternaries
Option<int> MaybeDouble(int n) => n > 0 ? Some(n * 2) : None;

// Method arguments
Process(Some(42));
Process(None);

// Chains
var result = Some(3).Map(x => x + 1).Map(x => x * 10);

// Generic return positions -- None works for any T
Option<T> GetNone<T>() => None;
```

**How it works:** `Some(42)` resolves to `Option.Some<int>(42)` via the static import, which returns `Option<int>` directly. `None` resolves to `Option.None`, which returns a `NoneOption` sentinel; the existing `implicit operator Option<T>(NoneOption _)` on `Option<T>` converts it to the target type at the call site. No additional operators are needed.

## Design

`Option<T>` is a `readonly record struct` -- zero heap allocation, copy semantics, structural equality, and the same size as `T` plus one `bool`. All hot-path methods are marked `[MethodImpl(AggressiveInlining)]` so the JIT can eliminate the `_isSome` branch when the state is statically known.

Key design decisions:
- **`default` is `None`** -- an uninitialized `Option<T>` is always `None`, never in an invalid state.
- **`Some(null)` is valid** -- wrapping `null` as `Some` explicitly distinguishes "I have a value that happens to be null" from "I have no value".
- **Value types and reference types behave identically** -- no special-casing, no different semantics.

## Performance

`Option<T>` is designed so that **construction, state checks, and value extraction are zero-cost** -- the JIT inlines them to the same machine code as hand-written `T?` / null-check patterns. Methods that accept delegates (`Func<>` / `Action<>`) carry measurable overhead compared to hand-inlined ternary expressions, because the delegate calling convention is inherently more expensive than a raw branch even when the JIT inlines the lambda body.

> **TL;DR for Architects:** `Some()`, `IsSome`, `UnwrapOr`, `Or`, `Zip`, and the other non-delegate methods are free -- use them without hesitation in hot paths. `Map`, `AndThen`, `Filter`, `MapOrElse`, and other delegate-accepting methods are ~3-4x slower than hand-inlined nullable ternaries. This is the inherent cost of delegate dispatch, not an `Option<T>` deficiency; any API taking `Func<>` pays the same price. In practice, the overhead is single-digit nanoseconds per call and is irrelevant outside tight inner loops.

### Zero-Cost Tier (no delegates)

These methods compile down to the same inline ternary / branch instructions as the equivalent `T?` code. No delegates, no allocations, no overhead.

**Option&lt;T&gt; vs T?** (100M iterations, .NET 10):

| Operation | Option&lt;T&gt; | T? Baseline | Ratio |
|-----------|------------|-------------|-------|
| Create Some | 35 ms | 34 ms | ≈1.0 (noise) |
| IsSome check | 46 ms | 49 ms | ≈1.0 (noise) |
| UnwrapOr (??) | 122 ms | 127 ms | ≈1.0 (noise) |
| Or | 139 ms | 148 ms | ≈1.0 (noise) |
| Zip | 164 ms | 157 ms | ≈1.0 (noise) |

*All differences are within measurement noise. The zero-cost methods are statistically indistinguishable from raw nullable code.*

### Delegate Tier (Func&lt;&gt; / Action&lt;&gt; parameters)

These methods accept delegates. The baseline comparison is hand-inlined nullable code with no delegate overhead (e.g., a raw ternary `val.HasValue ? val.Value * 2 : null`). The overhead shown is the inherent cost of delegate dispatch, not an `Option<T>`-specific penalty -- any API accepting `Func<>` pays the same price.

**Option&lt;T&gt; (delegate) vs T? (hand-inlined)** (100M iterations, .NET 10):

| Operation | Option&lt;T&gt; | T? Baseline | Ratio |
|-----------|------------|-------------|-------|
| Map | 197 ms | 66 ms | ≈3.0x |
| AndThen | 196 ms | 65 ms | ≈3.0x |
| Filter | 217 ms | 56 ms | ≈3.8x |
| MapOrElse | 238 ms | 52 ms | ≈4.6x |

The overhead is ~130-190 ms over 100M iterations, which is **1.3-1.9 nanoseconds per call**. This is negligible for all but the tightest inner loops.

### Key Findings

- ✅ **Zero-cost construction and extraction** -- `Some()`, `IsSome`, `Value`, `UnwrapOr`, `Or`, `Zip`, `Xor`, `And`, `TryGetValue`, and `Deconstruct` are statistically identical to `T?` code
- ✅ **No heap allocations** -- `readonly record struct` with copy semantics; same size as `T` plus one `bool`
- ✅ **Aggressive inlining** -- all hot-path methods are marked `[MethodImpl(AggressiveInlining)]`
- ⚠️ **Delegate methods have inherent overhead** -- `Map`, `AndThen`, `Filter`, `MapOrElse`, `MapOr`, `Inspect`, `OrElse`, `UnwrapOrElse`, and `ZipWith` are ~3-4x slower than a hand-inlined ternary, which is the expected cost of delegate dispatch in .NET

### Recommendations

| Scenario | Recommendation |
|----------|----------------|
| Hot inner loops (millions of iterations) | Use `IsSome` + `Value` or `UnwrapOr` for zero-cost extraction. Avoid delegate methods in the loop body. |
| General application code | Use any method freely. The delegate overhead is single-digit nanoseconds and unmeasurable at application scale. |
| Transform chains (`Map` / `AndThen` / `Filter`) | The readability benefit far outweighs the nanosecond-scale cost in all but extreme hot paths. Use them. |
| Performance-critical code that also needs delegates | Consider hoisting the lambda to a `static` field to avoid repeated allocation, or use `TryGetValue` with manual branching. |

## Creating Options

### Explicit Factories

```csharp
// Typed factories
var some = Option<int>.Some(42);
var none = Option<int>.None;

// Type-inferred factory (via static companion class)
var inferred = Option.Some(42);    // Option<int>
var inferredStr = Option.Some("hello");  // Option<string>
```

### Implicit Conversions

```csharp
// Value to Some
Option<int> opt = 42;
Option<string> str = "hello";

// Untyped None sentinel to any Option<T>
Option<int> n1 = Option.None;
Option<string> n2 = Option.None;
```

### From Nullable

```csharp
// Reference types
string? s = GetNullableString();
Option<string> opt1 = Option.FromNullable(s);   // Some if non-null, None if null
Option<string> opt2 = s.ToOption();              // Extension method equivalent

// Value types
int? n = GetNullableInt();
Option<int> opt3 = Option.FromNullable(n);       // Some if HasValue, None if null
Option<int> opt4 = n.ToOption();                 // Extension method equivalent
```

## Accessing Values

### Direct Access

```csharp
var opt = Option<int>.Some(42);

// These throw InvalidOperationException if None
int value = opt.Value;
int unwrapped = opt.Unwrap();     // Alias for Value
```

### Safe Access

```csharp
// TryGetValue pattern
if (opt.TryGetValue(out var value))
    Console.WriteLine(value);

// Check state
if (opt.IsSome) { /* has value */ }
if (opt.IsNone) { /* empty */ }
```

### Expect

```csharp
// Unwrap with a custom error message
int value = opt.Expect("Config value must be present");
// Throws InvalidOperationException("Config value must be present") if None
```

### Default Values

```csharp
// Provide a fallback
int value = opt.UnwrapOr(0);

// Lazy fallback (factory only called if None)
int value = opt.UnwrapOrElse(() => ComputeDefault());

// Type default (0 for int, null for reference types)
int? value = opt.UnwrapOrDefault();
```

### Unchecked Access

```csharp
// No branch -- returns default(T) if None
// Only use when you've already verified IsSome
int? value = opt.UnwrapUnchecked();
```

### Deconstruction

```csharp
var (isSome, value) = opt;
if (isSome)
    Console.WriteLine(value);
```

## Transforms

### Map

Transforms the contained value. Returns `None` if the option is `None`.

```csharp
var opt = Option<int>.Some(5);
Option<string> mapped = opt.Map(x => x.ToString());  // Some("5")

Option<int>.None.Map(x => x * 2);  // None (transform never called)
```

### AndThen (Flat-Map / Bind)

Chains operations that themselves return `Option<T>`.

```csharp
Option<string> GetUser(int id) => /* ... */;
Option<string> GetEmail(string user) => /* ... */;

// Chain lookups -- short-circuits on first None
var email = GetUser(1).AndThen(GetEmail);
```

### Filter

Keeps the value only if a predicate is true.

```csharp
var opt = Option<int>.Some(42);
opt.Filter(x => x > 0);   // Some(42)
opt.Filter(x => x < 0);   // None
```

### MapOrElse / MapOr

Pattern-match-style transform that handles both Some and None.

```csharp
// Lazy None branch
string message = opt.MapOrElse(
    onSome: v => $"Got {v}",
    onNone: () => "Nothing");

// Eager None branch
int doubled = opt.MapOr(v => v * 2, defaultValue: -1);
```

## Side Effects

### Inspect

Executes an action if `Some`, returns the option unchanged for chaining.

```csharp
var result = Option.Some(42)
    .Inspect(v => Console.WriteLine($"Value: {v}"))
    .Map(v => v * 2);
```

## Combinators

### And

Returns the second option if this is `Some`; otherwise `None`.

```csharp
var a = Option<int>.Some(1);
var b = Option<string>.Some("x");

a.And(b);  // Some("x")
Option<int>.None.And(b);  // None
```

### Or / OrElse

Returns this option if `Some`; otherwise the fallback.

```csharp
var a = Option<int>.None;
var b = Option<int>.Some(2);

a.Or(b);  // Some(2)

// Lazy fallback
a.OrElse(() => Option<int>.Some(99));  // Some(99)
```

### Xor

Returns `Some` if exactly one of the two options is `Some`.

```csharp
Option<int>.Some(1).Xor(Option<int>.None);    // Some(1)
Option<int>.Some(1).Xor(Option<int>.Some(2)); // None
Option<int>.None.Xor(Option<int>.None);       // None
```

### Zip / ZipWith

Combines two options into a tuple or a combined value. Returns `None` if either is `None`.

```csharp
var a = Option<int>.Some(1);
var b = Option<string>.Some("x");

a.Zip(b);  // Some((1, "x"))

a.ZipWith(Option<int>.Some(4), (x, y) => x * y);  // Some(4)
```

### Flatten

Collapses a nested `Option<Option<T>>` into `Option<T>`.

```csharp
var nested = Option<Option<int>>.Some(Option<int>.Some(42));
nested.Flatten();  // Some(42)

var nestedNone = Option<Option<int>>.Some(Option<int>.None);
nestedNone.Flatten();  // None
```

## Interop with Nullable

```csharp
// Option to nullable
int? nullable = opt.ToNullable();  // Returns default(T) if None

// Nullable to Option
string? s = "hello";
Option<string> fromRef = s.ToOption();

int? n = 42;
Option<int> fromVal = n.ToOption();
```

## Interop with Result

Convert between `Option<T>` and `Result<T, TError>` to bridge the two error-handling styles.

### Option to Result

```csharp
var opt = Option<int>.Some(42);

// Eager error
Result<int, string> result = opt.OkOr("value was missing");

// Lazy error
Result<int, string> result = opt.OkOrElse(() => "computed error");
```

### Result to Option

```csharp
var result = Result<int, string>.Ok(42);
Option<int> opt = result.ToOption();  // Some(42)

var err = Result<int, string>.Err("fail");
Option<int> opt2 = err.ToOption();  // None (error discarded)
```

### Transpose

Swaps the nesting of `Option` and `Result`:

```csharp
// Option<Result<T, E>> --> Result<Option<T>, E>
var opt = Option<Result<int, string>>.Some(Result<int, string>.Ok(42));
Result<Option<int>, string> transposed = opt.Transpose();
// Ok(Some(42))

Option<Result<int, string>>.None.Transpose();
// Ok(None)

Option<Result<int, string>>.Some(Result<int, string>.Err("fail")).Transpose();
// Err("fail")
```

## Async Support

Extension methods support async/await patterns.

```csharp
// Async Map
var result = await Task.FromResult(Option<int>.Some(5))
    .Map(x => x * 2);
// Some(10)

// Async AndThen (sync transform)
var result = await Task.FromResult(Option<int>.Some(5))
    .AndThen(x => Option<string>.Some(x.ToString()));
// Some("5")

// Async AndThen (async transform)
var result = await Task.FromResult(Option<int>.Some(5))
    .AndThen(async x =>
    {
        await Task.Yield();
        return Option<string>.Some(x.ToString());
    });
// Some("5")
```

## Equality

`Option<T>` is a `readonly record struct`, so equality is structural:

```csharp
Option<int>.Some(42) == Option<int>.Some(42);  // true
Option<int>.Some(1) == Option<int>.Some(2);    // false
Option<int>.None == Option<int>.None;          // true
Option<int>.Some(42) == Option<int>.None;      // false
```

## ToString

```csharp
Option<int>.Some(42).ToString();   // "Some(42)"
Option<int>.None.ToString();       // "None"
```

## Real-World Examples

### Dictionary Lookup

```csharp
Option<int> Lookup(Dictionary<string, int> dict, string key) =>
    dict.TryGetValue(key, out var val) ? Option<int>.Some(val) : Option<int>.None;

var dict = new Dictionary<string, int> { ["a"] = 1 };
Lookup(dict, "a").Value;          // 1
Lookup(dict, "z").IsNone;         // true
```

### Safe Parsing

```csharp
static Option<int> ParsePositive(string s)
{
    if (int.TryParse(s, out var n) && n > 0)
        return n;            // implicit conversion
    return Option.None;      // untyped sentinel
}

ParsePositive("42").Value;     // 42
ParsePositive("-1").IsNone;    // true
ParsePositive("abc").IsNone;   // true
```

### Chained Lookups

```csharp
var users = new Dictionary<int, string> { [1] = "alice" };
var emails = new Dictionary<string, string> { ["alice"] = "alice@example.com" };

Option<string> GetUser(int id) =>
    users.TryGetValue(id, out var name) ? name : Option.None;

Option<string> GetEmail(string user) =>
    emails.TryGetValue(user, out var email) ? email : Option.None;

// Happy path -- chains through
GetUser(1).AndThen(GetEmail).Value;  // "alice@example.com"

// Missing user -- short-circuits
GetUser(999).AndThen(GetEmail).IsNone;  // true
```

### Configuration with Defaults

```csharp
Option<string> GetEnv(string key) =>
    Environment.GetEnvironmentVariable(key).ToOption();

string connectionString = GetEnv("DB_CONNECTION")
    .UnwrapOr("Server=localhost;Database=dev");

int port = GetEnv("PORT")
    .AndThen(s => int.TryParse(s, out var n) ? Option<int>.Some(n) : Option<int>.None)
    .UnwrapOr(8080);
```

## API Reference

### Option&lt;T&gt;

| Member | Description |
|--------|-------------|
| `Some(T value)` | Creates an Option containing the value |
| `None` | Returns an empty Option |
| `IsSome` | True if the option contains a value |
| `IsNone` | True if the option is empty |
| `Value` | The contained value (throws if None) |
| `Unwrap()` | Alias for Value |
| `Expect(string message)` | Unwrap with a custom error message |
| `UnwrapOr(T defaultValue)` | Value or fallback |
| `UnwrapOrElse(Func<T> factory)` | Value or lazy fallback |
| `UnwrapOrDefault()` | Value or default(T) |
| `UnwrapUnchecked()` | Value without branch (returns default if None) |
| `TryGetValue(out T value)` | Try-pattern extraction |
| `Deconstruct(out bool, out T)` | Tuple deconstruction |
| `Map<TNew>(Func<T, TNew>)` | Transform contained value |
| `AndThen<TNew>(Func<T, Option<TNew>>)` | Flat-map / bind |
| `Filter(Func<T, bool>)` | Keep value if predicate passes |
| `Inspect(Action<T>)` | Side-effect on Some, returns self |
| `MapOrElse<TResult>(Func<T, TResult>, Func<TResult>)` | Pattern match with lazy None |
| `MapOr<TResult>(Func<T, TResult>, TResult)` | Pattern match with eager None |
| `ToNullable()` | Convert to T? |
| `OkOr<TError>(TError)` | Convert to Result (eager error) |
| `OkOrElse<TError>(Func<TError>)` | Convert to Result (lazy error) |
| `And<TOther>(Option<TOther>)` | Return other if this is Some |
| `Or(Option<T>)` | Return this if Some, otherwise other |
| `OrElse(Func<Option<T>>)` | Return this if Some, otherwise lazy fallback |
| `Xor(Option<T>)` | Some if exactly one is Some |
| `Zip<TOther>(Option<TOther>)` | Combine two options into a tuple |
| `ZipWith<TOther, TResult>(Option<TOther>, Func)` | Combine two options with a function |
| `ToString()` | "Some(value)" or "None" |

### Option (Static Companion)

| Member | Description |
|--------|-------------|
| `None` | Untyped None sentinel (converts to any `Option<T>`) |
| `Some<T>(T value)` | Type-inferred Some factory |
| `FromNullable<T>(T? value)` | Reference type nullable to Option |
| `FromNullable<T>(T? value)` | Value type nullable to Option |

### OptionExtensions

| Member | Description |
|--------|-------------|
| `Flatten<T>(Option<Option<T>>)` | Collapse nested option |
| `Transpose<T, TError>(Option<Result<T, TError>>)` | Swap Option/Result nesting |
| `ToOption<T>(T?)` | Reference type nullable to Option |
| `ToOption<T>(T?)` | Value type nullable to Option |
| `ToOption<T, TError>(Result<T, TError>)` | Result to Option (discards error) |
| `Map<T, TNew>(Task<Option<T>>, Func)` | Async Map |
| `AndThen<T, TNew>(Task<Option<T>>, Func)` | Async AndThen (sync transform) |
| `AndThen<T, TNew>(Task<Option<T>>, Func<T, Task<Option<TNew>>>)` | Async AndThen (async transform) |
