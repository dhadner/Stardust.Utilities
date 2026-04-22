# Result Types

Efficient railway-oriented (ROP) error handling without exceptions. Inspired by Rust's `Result<T, E>` type.

## Overview

The Result types provide a way to handle operations that can fail without using exceptions for expected failure cases. They encode success or failure in the type system, making error handling explicit and composable.

**When to use Result types:**
- Validation failures
- Parse errors
- Business rule violations
- File/network operations that might fail
- Any operation where failure is an expected outcome

**When to use exceptions:**
- Programming errors (null references, index out of bounds)
- Truly exceptional circumstances
- Unrecoverable errors

## Quick Start

```csharp
using Stardust.Utilities;

// Function that might fail
Result<int, string> Divide(int a, int b)
{
    if (b == 0)
        return Result<int, string>.Err("Division by zero");
    return Result<int, string>.Ok(a / b);
}

// Using the result
var result = Divide(10, 2);

if (result.IsOk)
    Console.WriteLine($"Result: {result.Value}");
else
    Console.WriteLine($"Error: {result.Error}");
```

## Global Using for Cleaner Syntax

Use `global using static` to enable cleaner `Ok()` and `Err()` syntax:

```csharp
// In GlobalUsings.cs
global using static Stardust.Utilities.Result<int,string>;
global using static Stardust.Utilities.Result<string>;
// Add additional global usings as needed for other Result types for clean usage syntax.

// Now in your code:
Result<int, string> GetValue()
{
    if (someCondition)
        return Ok(42);      // Instead of Result<int, string>.Ok(42)
    return Err("Failed");   // Instead of Result<int, string>.Err("Failed")
}

Result<string> DoWork()
{
    if (success)
        return Ok();        // Instead of Result<string>.Ok()
    return Err("Failed");   // Instead of Result<string>.Err("Failed")
}
```

## Result Types

### Result<T, TError> - Value Result

For operations that return a value on success or an error on failure.

```csharp
// Create success with value
var success = Result<int, string>.Ok(42);

// Create failure with error
var failure = Result<int, string>.Err("Something went wrong");

// Check status
if (success.IsOk)
    Console.WriteLine(success.Value);  // 42

if (failure.IsErr)
    Console.WriteLine(failure.Error);  // "Something went wrong"
```

### Result<TError> - Void Result

For operations that succeed or fail without returning a value.

```csharp
Result<string> SaveFile(string path, string content)
{
    try
    {
        File.WriteAllText(path, content);
        return Result<string>.Ok();
    }
    catch (Exception ex)
    {
        return Result<string>.Err(ex.Message);
    }
}

var result = SaveFile("test.txt", "Hello");
if (result.IsErr)
    Console.WriteLine($"Save failed: {result.Error}");
```

## Custom Error Types

Results aren't limited to string errors. Use any type for rich error information:

### Enum Errors

```csharp
public enum ValidationError
{
    InvalidEmail,
    PasswordTooShort,
    UsernameTaken
}

Result<User, ValidationError> ValidateUser(string email, string password)
{
    if (!email.Contains('@'))
        return Result<User, ValidationError>.Err(ValidationError.InvalidEmail);

    if (password.Length < 8)
        return Result<User, ValidationError>.Err(ValidationError.PasswordTooShort);

    return Result<User, ValidationError>.Ok(new User(email, password));
}
```

### Error Objects

```csharp
public record ApiError(int Code, string Message, string? Details = null);

Result<User, ApiError> GetUser(int id)
{
    if (id <= 0)
        return Result<User, ApiError>.Err(new ApiError(400, "Invalid ID", $"ID was {id}"));

    // ... fetch user
    return Result<User, ApiError>.Ok(user);
}
```

### Exception Wrappers

```csharp
Result<string, Exception> ReadFile(string path)
{
    try
    {
        return Result<string, Exception>.Ok(File.ReadAllText(path));
    }
    catch (Exception ex)
    {
        return Result<string, Exception>.Err(ex);
    }
}

var result = ReadFile("config.json");
result.InspectErr(ex => Console.WriteLine($"Failed: {ex.GetType().Name}: {ex.Message}"));
```

## Querying State

### IsOk / IsErr

```csharp
var result = Result<int, string>.Ok(42);

result.IsOk;   // true
result.IsErr;  // false
```

### IsOkAnd / IsErrAnd

Test the state and a predicate in one call:

```csharp
var result = Result<int, string>.Ok(42);

result.IsOkAnd(v => v > 0);   // true
result.IsOkAnd(v => v < 0);   // false

var err = Result<int, string>.Err("not found");
err.IsErrAnd(e => e.Contains("not found"));  // true
```

## Accessing Values

### Direct Access

```csharp
var result = Result<int, string>.Ok(42);

// These throw if called on wrong state
int value = result.Value;       // Throws if IsErr
string error = result.Error;    // Throws if IsOk

// Safe access (returns default if wrong state)
string? maybeError = result.ErrorOrDefault;  // null if IsOk
```

### TryGet Pattern

```csharp
if (result.TryGetValue(out var value))
{
    Console.WriteLine($"Got value: {value}");
}

if (result.TryGetError(out var error))
{
    Console.WriteLine($"Got error: {error}");
}
```

### Unwrap / Expect

```csharp
// Unwrap -- aliases for Value / Error properties
int value = result.Unwrap();        // Same as .Value, throws if Err
TError error = result.UnwrapErr();  // Same as .Error, throws if Ok

// Expect -- unwrap with a custom error message
int value = result.Expect("config value must be present");
TError error = result.ExpectErr("expected an error here");
```

### UnwrapOr - Default Values

```csharp
// Return default value if Err
int value = result.UnwrapOr(0);

// Compute default from error (lazy, only called on Err)
int value = result.UnwrapOrElse(error => error.Length);

// Return default(T) if Err
int? value = result.UnwrapOrDefault();  // 0 for int, null for reference types
```

### Deconstruction

```csharp
// Value result
var (isSuccess, value, error) = result;
if (isSuccess)
    Console.WriteLine(value);
else
    Console.WriteLine(error);

// Void result
var (ok, err) = voidResult;
```

### ToNullable

```csharp
int? value = result.ToNullable();  // null if IsErr
```

## Transforms

### Map - Transform Success Value

Transform the Ok value. Err values pass through unchanged:

```csharp
Result<int, string> result = GetNumber();

// Transform int to string (cannot fail)
Result<string, string> stringResult = result.Map(x => x.ToString());

// Transform with expression
var doubled = result.Map(x => x * 2);
```

### MapErr - Transform Errors

Convert between error types at layer boundaries:

```csharp
// Internal error type
Result<User, DatabaseError> user = GetUserFromDb(id);

// Convert to API error for response
Result<User, ApiError> apiResult = user.MapErr(dbErr => new ApiError(
    Code: 500,
    Message: "Database error",
    Details: dbErr.Message
));
```

### MapOr / MapOrElse - Pattern Match

Transform both success and failure cases into a single result:

```csharp
var result = Divide(10, 3);

// MapOrElse -- lazy None branch
string message = result.MapOrElse(
    onOk: value => $"Answer is {value}",
    onErr: error => $"Failed: {error}"
);

// MapOr -- eager default
int doubled = result.MapOr(v => v * 2, defaultValue: -1);

// For void results
var voidResult = SaveFile("test.txt", "data");
string status = voidResult.MapOrElse(
    onOk: () => "Saved successfully",
    onErr: error => $"Save failed: {error}"
);
```

## Side Effects

### Inspect / InspectErr

Execute actions without changing the result. Returns self for chaining:

```csharp
var result = GetUser(id)
    .Inspect(user => Console.WriteLine($"Found user: {user.Name}"))
    .InspectErr(error => Logger.Error($"User lookup failed: {error}"))
    .AndThen(user => SendWelcomeEmail(user))
    .Inspect(_ => Console.WriteLine("Email sent"))
    .InspectErr(error => Logger.Error($"Email failed: {error}"));
```

## Boolean Operators

### And - Require Both

Returns the second result if this is Ok; otherwise returns this Err:

```csharp
var a = Result<int, string>.Ok(1);
var b = Result<string, string>.Ok("hello");

a.And(b);  // Ok("hello")

Result<int, string>.Err("fail").And(b);  // Err("fail")
```

### AndThen - Chain Fallible Operations

Flat-map: if Ok, calls the function with the value; otherwise returns the Err:

```csharp
Result<int, string> Parse(string input) { ... }
Result<int, string> Validate(int value) { ... }
Result<string, string> Format(int value) { ... }

// Chain multiple fallible operations
var result = Parse("42")
    .AndThen(x => Validate(x))
    .AndThen(x => Format(x));

// If any step fails, the error propagates
```

### Or / OrElse - Fallback

Returns this result if Ok; otherwise returns the fallback:

```csharp
var a = Result<int, string>.Err("fail");
var b = Result<int, int>.Ok(99);

a.Or(b);  // Ok(99)

// OrElse -- lazy, receives the error
a.OrElse(e => Result<int, int>.Ok(e.Length));  // Ok(4)
```

### WithValue - Add Value to Void Result

Convert a void result to a value result:

```csharp
Result<string> voidResult = SaveFile("test.txt", "data");

// Add a value on success
Result<int, string> withCount = voidResult.WithValue(42);
```

## Combining Results

### Combine - All Must Succeed

```csharp
var r1 = ValidateEmail(email);
var r2 = ValidatePassword(password);
var r3 = ValidateUsername(username);

// Returns first failure, or Ok() if all succeed
var combined = Result<string>.Combine(r1, r2, r3);

if (combined.IsOk)
    CreateUser(email, password, username);
else
    ShowError(combined.Error);
```

## Flatten

Collapses a nested `Result<Result<T, E>, E>` into `Result<T, E>`:

```csharp
var nested = Result<Result<int, string>, string>.Ok(Result<int, string>.Ok(42));
nested.Flatten();  // Ok(42)

var nestedErr = Result<Result<int, string>, string>.Ok(Result<int, string>.Err("inner"));
nestedErr.Flatten();  // Err("inner")

var outerErr = Result<Result<int, string>, string>.Err("outer");
outerErr.Flatten();  // Err("outer")
```

## Interop with Option

### ToOption

Converts Ok to Some, Err to None (the error is discarded):

```csharp
var ok = Result<int, string>.Ok(42);
Option<int> opt = ok.ToOption();  // Some(42)

var err = Result<int, string>.Err("fail");
Option<int> opt2 = err.ToOption();  // None
```

### ErrToOption

Converts Err to Some, Ok to None (the value is discarded):

```csharp
var err = Result<int, string>.Err("fail");
Option<string> opt = err.ErrToOption();  // Some("fail")

var ok = Result<int, string>.Ok(42);
ok.ErrToOption();  // None
```

### Transpose

Swaps the nesting of `Result` and `Option`:

```csharp
// Result<Option<T>, E> --> Option<Result<T, E>>
var result = Result<Option<int>, string>.Ok(Option<int>.Some(42));
Option<Result<int, string>> transposed = result.Transpose();
// Some(Ok(42))

Result<Option<int>, string>.Ok(Option<int>.None).Transpose();
// None

Result<Option<int>, string>.Err("fail").Transpose();
// Some(Err("fail"))
```

See also [OPTION.md](OPTION.md) for the corresponding `Option.OkOr`, `Option.OkOrElse`, and `Option.Transpose` methods.

## Async Support

Extension methods support async/await patterns:

### Async Map

```csharp
// Chain async transform
var result = await GetUserAsync(id)
    .Map(user => user.Name);  // Sync transform on async result
```

### Async AndThen

```csharp
// Chain sync fallible operation after async result
var result = await GetUserAsync(id)
    .AndThen(user => ValidateUser(user));

// Chain async operations
var result = await GetUserAsync(id)
    .AndThen(user => GetOrdersAsync(user.Id))
    .AndThen(orders => CalculateTotalAsync(orders));
```

### Async MapErr

```csharp
// Transform error type asynchronously
var apiResult = await GetUserFromDbAsync(id)
    .MapErr(dbErr => new ApiError(500, dbErr.Message));
```

## Type Conversions

### From Tuple

```csharp
// Convert (error, value) tuple to Result
// null error = success
(string? error, int value) tuple = (null, 42);
Result<int, string> result = tuple.ToResult();  // Ok(42)

(string? error, int value) failTuple = ("Error!", 0);
Result<int, string> failResult = failTuple.ToResult();  // Err("Error!")
```

### From Nullable Error

```csharp
// null = success, non-null = failure
string? error = GetValidationError();
Result<string> result = error.ToResult();
```

### Void to Value Result

```csharp
Result<string> voidResult = DoSomething();

// Implicit conversion (works for collections and strings)
Result<List<int>, string> listResult = voidResult;  // Empty list on success
Result<string, string> stringResult = voidResult;   // Empty string on success

// Note: Throws for non-collection value types
// Result<int, string> intResult = voidResult;  // Throws!
```

## Collection Defaults

When creating success results with null values, collection types get empty defaults:

```csharp
// Null arrays become empty arrays
var arrayResult = Result<int[], string>.Ok(null);
arrayResult.Value.Length.Should().Be(0);

// Null lists become empty lists
var listResult = Result<List<int>, string>.Ok(null);
listResult.Value.Count.Should().Be(0);

// Null strings become empty strings
var stringResult = Result<string, string>.Ok(null);
stringResult.Value.Should().Be("");

// Works with Dictionary, HashSet, Queue, Stack, etc.
var dictResult = Result<Dictionary<string, int>, string>.Ok(null);
dictResult.Value.Count.Should().Be(0);
```

## Real-World Examples

### Validation Pipeline

```csharp
public record User(string Email, string Password, string Name);

public record ValidationError(string Field, string Message);

Result<User, ValidationError> CreateUser(string email, string password, string name)
{
    return ValidateEmail(email)
        .AndThen(_ => ValidatePassword(password))
        .AndThen(_ => ValidateName(name))
        .AndThen(_ => Result<User, ValidationError>.Ok(new User(email, password, name)));
}

Result<string, ValidationError> ValidateEmail(string email)
{
    if (string.IsNullOrWhiteSpace(email))
        return Result<string, ValidationError>.Err(new("Email", "Required"));
    if (!email.Contains('@'))
        return Result<string, ValidationError>.Err(new("Email", "Invalid format"));
    return Result<string, ValidationError>.Ok(email);
}
```

### API Response Handling

```csharp
public record ApiResponse<T>(T? Data, string? Error, int StatusCode);

Result<T, ApiError> FromApiResponse<T>(ApiResponse<T> response)
{
    if (response.StatusCode >= 400)
        return Result<T, ApiError>.Err(new ApiError(response.StatusCode, response.Error!));

    return Result<T, ApiError>.Ok(response.Data!);
}

// Usage
var response = await httpClient.GetAsync<User>("/api/users/1");
var result = FromApiResponse(response)
    .Inspect(user => cache.Set(user.Id, user))
    .InspectErr(err => logger.Warn($"API error {err.Code}: {err.Message}"));
```

### Database Operations

```csharp
public async Task<Result<User, DbError>> GetUserAsync(int id)
{
    try
    {
        var user = await db.Users.FindAsync(id);
        if (user == null)
            return Result<User, DbError>.Err(DbError.NotFound);
        return Result<User, DbError>.Ok(user);
    }
    catch (DbException ex)
    {
        return Result<User, DbError>.Err(DbError.ConnectionFailed(ex.Message));
    }
}

public async Task<Result<Order, DbError>> CreateOrderAsync(int userId, OrderRequest request)
{
    return await GetUserAsync(userId)
        .AndThen(user => ValidateOrderRequest(request))
        .AndThen(async validated => 
        {
            var order = new Order(userId, validated);
            await db.Orders.AddAsync(order);
            await db.SaveChangesAsync();
            return Result<Order, DbError>.Ok(order);
        });
}
```

### Configuration Loading

```csharp
public Result<AppConfig, ConfigError> LoadConfig(string path)
{
    if (!File.Exists(path))
        return Result<AppConfig, ConfigError>.Err(
            ConfigError.FileNotFound(path));

    try
    {
        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<AppConfig>(json);

        if (config == null)
            return Result<AppConfig, ConfigError>.Err(
                ConfigError.InvalidFormat("Deserialization returned null"));

        return ValidateConfig(config);
    }
    catch (JsonException ex)
    {
        return Result<AppConfig, ConfigError>.Err(
            ConfigError.InvalidFormat(ex.Message));
    }
}

// Usage with fallback
var config = LoadConfig("config.json")
    .UnwrapOrElse(error => 
    {
        logger.Warn($"Config load failed: {error}, using defaults");
        return AppConfig.Default;
    });
```

## API Reference

### Result<T, TError>

| Member | Description |
|--------|-------------|
| `Ok(T? value)` | Creates a successful result |
| `Err(TError error)` | Creates a failed result |
| `IsOk` | True if Ok |
| `IsErr` | True if Err |
| `IsOkAnd(Func<T, bool>)` | True if Ok and predicate passes |
| `IsErrAnd(Func<TError, bool>)` | True if Err and predicate passes |
| `Value` | The success value (throws if Err) |
| `Error` | The error value (throws if Ok) |
| `ErrorOrDefault` | Error or default if Ok |
| `TryGetValue(out T?)` | Try to get value, returns success status |
| `TryGetError(out TError?)` | Try to get error, returns failure status |
| `Unwrap()` | Alias for Value |
| `Expect(string)` | Unwrap with custom error message |
| `UnwrapErr()` | Alias for Error |
| `ExpectErr(string)` | Unwrap error with custom message |
| `UnwrapOr(T)` | Value or default if Err |
| `UnwrapOrElse(Func<TError, T>)` | Value or computed default |
| `UnwrapOrDefault()` | Value or default(T) if Err |
| `Map<TNew>(Func<T, TNew>)` | Transform value (cannot fail) |
| `MapErr<TNewError>(Func<TError, TNewError>)` | Transform error type |
| `MapOr<TResult>(Func<T, TResult>, TResult)` | Transform value or return eager default |
| `MapOrElse<TResult>(Func<T, TResult>, Func<TError, TResult>)` | Pattern match both cases |
| `Inspect(Action<T>)` | Side-effect on Ok, returns self |
| `InspectErr(Action<TError>)` | Side-effect on Err, returns self |
| `And<TNew>(Result<TNew, TError>)` | Return other if Ok |
| `AndThen<TNew>(Func<T, Result<TNew, TError>>)` | Chain fallible operation |
| `Or<TNewError>(Result<T, TNewError>)` | Return this if Ok, otherwise other |
| `OrElse<TNewError>(Func<TError, Result<T, TNewError>>)` | Return this if Ok, otherwise lazy fallback |
| `ToNullable()` | Convert to nullable (null on Err) |
| `Deconstruct(...)` | Enables tuple deconstruction |

### Result<TError>

| Member | Description |
|--------|-------------|
| `Ok()` | Creates a successful void result |
| `Ok<T>(T value)` | Creates a successful value result (for static import) |
| `Err(TError? error)` | Creates a failed result |
| `IsOk` | True if Ok |
| `IsErr` | True if Err |
| `Error` | The error value (throws if Ok) |
| `ErrorOrDefault` | Error or default if Ok |
| `TryGetError(out TError?)` | Try to get error |
| `MapErr<TNewError>(Func<TError, TNewError>)` | Transform error type |
| `MapOrElse<TResult>(Func<TResult>, Func<TError, TResult>)` | Pattern match both cases |
| `Inspect(Action)` | Side-effect on Ok, returns self |
| `InspectErr(Action<TError>)` | Side-effect on Err, returns self |
| `And(Result<TError>)` | Return other if Ok |
| `AndThen(Func<Result<TError>>)` | Chain another void operation |
| `Or<TNewError>(Result<TNewError>)` | Return this if Ok, otherwise other |
| `OrElse<TNewError>(Func<TError, Result<TNewError>>)` | Return this if Ok, otherwise lazy fallback |
| `WithValue<T>(T)` | Convert to value result |
| `Combine(params Result<TError>[])` | Combine multiple results |
| `Deconstruct(...)` | Enables tuple deconstruction |

### ResultExtensions

| Member | Description |
|--------|-------------|
| `ToResult<T, TError>(tuple)` | Convert (error, value) tuple to Result |
| `ToResult<TError>(error)` | Convert nullable error to Result |
| `Flatten<T, TError>(Result<Result<T, TError>, TError>)` | Collapse nested result |
| `Transpose<T, TError>(Result<Option<T>, TError>)` | Swap Result/Option nesting |
| `ErrToOption<T, TError>(Result<T, TError>)` | Err to Some, Ok to None |
| `Map<T, TNew, TError>(Task, Func)` | Async Map |
| `AndThen<T, TNew, TError>(Task, Func)` | Async AndThen (sync transform) |
| `AndThen<T, TNew, TError>(Task, Func<..., Task>)` | Async AndThen (async transform) |
| `AndThen<TError>(Task, Func<Task>)` | Async AndThen for void results |
| `AndThen<TError>(Task, Func)` | Async AndThen for void results (sync) |
| `MapErr<T, TError, TNewError>(Task, Func)` | Async MapErr |

