# Result Types

Railway-oriented error handling without exceptions. Inspired by Rust's `Result<T, E>` type.

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

if (result.IsSuccess)
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
if (success.IsSuccess)
    Console.WriteLine(success.Value);  // 42

if (failure.IsFailure)
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
if (result.IsFailure)
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
result.OnFailure(ex => Console.WriteLine($"Failed: {ex.GetType().Name}: {ex.Message}"));
```

## Accessing Values

### Direct Access

```csharp
var result = Result<int, string>.Ok(42);

// These throw if called on wrong state
int value = result.Value;       // Throws if IsFailure
string error = result.Error;    // Throws if IsSuccess

// Safe access (returns default if wrong state)
string? maybeError = result.ErrorOrDefault;  // null if IsSuccess
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

### ValueOr - Default Values

```csharp
// Return default value if failed
int value = result.ValueOr(0);

// Compute default from error
int value = result.ValueOr(error => error.Length);  // Use error to compute default
```

### Unwrap

```csharp
// Aliases for Value/Error properties
int value = result.Unwrap();        // Same as .Value
string error = result.UnwrapError();  // Same as .Error
```

## Pattern Matching

### Match

Transform both success and failure cases into a single result:

```csharp
var result = Divide(10, 3);

string message = result.Match(
    onSuccess: value => $"Answer is {value}",
    onFailure: error => $"Failed: {error}"
);

// For void results
var voidResult = SaveFile("test.txt", "data");
string status = voidResult.Match(
    onSuccess: () => "Saved successfully",
    onFailure: error => $"Save failed: {error}"
);
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
int? value = result.ToNullable();  // null if IsFailure
```

## Chaining Operations

### Then - Transform Success Value

Chain operations that cannot fail:

```csharp
Result<int, string> result = GetNumber();

// Transform int to string (cannot fail)
Result<string, string> stringResult = result.Then(x => x.ToString());

// Transform with expression
var doubled = result.Then(x => x * 2);
```

### Then - Chain Fallible Operations

Chain operations that might fail:

```csharp
Result<int, string> Parse(string input) { ... }
Result<int, string> Validate(int value) { ... }
Result<string, string> Format(int value) { ... }

// Chain multiple fallible operations
var result = Parse("42")
    .Then(x => Validate(x))
    .Then(x => Format(x));

// If any step fails, the error propagates
```

### MapError - Transform Errors

Convert between error types at layer boundaries:

```csharp
// Internal error type
Result<User, DatabaseError> user = GetUserFromDb(id);

// Convert to API error for response
Result<User, ApiError> apiResult = user.MapError(dbErr => new ApiError(
    Code: 500,
    Message: "Database error",
    Details: dbErr.Message
));
```

### OnSuccess / OnFailure - Side Effects

Execute actions without changing the result:

```csharp
var result = GetUser(id)
    .OnSuccess(user => Console.WriteLine($"Found user: {user.Name}"))
    .OnFailure(error => Logger.Error($"User lookup failed: {error}"))
    .Then(user => SendWelcomeEmail(user))
    .OnSuccess(_ => Console.WriteLine("Email sent"))
    .OnFailure(error => Logger.Error($"Email failed: {error}"));
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

if (combined.IsSuccess)
    CreateUser(email, password, username);
else
    ShowError(combined.Error);
```

## Async Support

Extension methods support async/await patterns:

### Async Transform

```csharp
// Chain async transform
var result = await GetUserAsync(id)
    .Then(user => user.Name);  // Sync transform on async result
```

### Async Chain

```csharp
// Chain async operations
var result = await GetUserAsync(id)
    .Then(user => GetOrdersAsync(user.Id))
    .Then(orders => CalculateTotalAsync(orders));
```

### Async Error Transform

```csharp
// Transform error type asynchronously
var apiResult = await GetUserFromDbAsync(id)
    .ThenError(dbErr => new ApiError(500, dbErr.Message));
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
        .Then(_ => ValidatePassword(password))
        .Then(_ => ValidateName(name))
        .Then(_ => new User(email, password, name));
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
    .OnSuccess(user => cache.Set(user.Id, user))
    .OnFailure(err => logger.Warn($"API error {err.Code}: {err.Message}"));
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
        .Then(user => ValidateOrderRequest(request))
        .Then(async validated => 
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
    .ValueOr(error => 
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
| `IsSuccess` | True if operation succeeded |
| `IsFailure` | True if operation failed |
| `Value` | The success value (throws if failed) |
| `Error` | The error value (throws if succeeded) |
| `ErrorOrDefault` | Error or default if succeeded |
| `TryGetValue(out T?)` | Try to get value, returns success status |
| `TryGetError(out TError?)` | Try to get error, returns failure status |
| `ValueOr(T)` | Value or default if failed |
| `ValueOr(Func<TError, T>)` | Value or computed default |
| `Then<TNew>(Func<T, TNew>)` | Transform value (cannot fail) |
| `Then<TNew>(Func<T, Result<TNew, TError>>)` | Chain fallible operation |
| `MapError<TNewError>(Func<TError, TNewError>)` | Transform error type |
| `OnSuccess(Action<T>)` | Execute action on success |
| `OnFailure(Action<TError>)` | Execute action on failure |
| `Match<TResult>(...)` | Pattern match both cases |
| `ToNullable()` | Convert to nullable (null on failure) |
| `Unwrap()` | Alias for Value |
| `UnwrapError()` | Alias for Error |
| `Deconstruct(...)` | Enables tuple deconstruction |

### Result<TError>

| Member | Description |
|--------|-------------|
| `Ok()` | Creates a successful void result |
| `Ok<T>(T value)` | Creates a successful value result (for static import) |
| `Err(TError? error)` | Creates a failed result |
| `IsSuccess` | True if operation succeeded |
| `IsFailure` | True if operation failed |
| `Error` | The error value (throws if succeeded) |
| `ErrorOrDefault` | Error or default if succeeded |
| `TryGetError(out TError?)` | Try to get error |
| `Then(Func<Result<TError>>)` | Chain another void operation |
| `MapError<TNewError>(...)` | Transform error type |
| `OnSuccess(Action)` | Execute action on success |
| `OnFailure(Action<TError>)` | Execute action on failure |
| `Match<TResult>(...)` | Pattern match both cases |
| `WithValue<T>(T)` | Convert to value result |
| `Combine(params Result<TError>[])` | Combine multiple results |
| `Deconstruct(...)` | Enables tuple deconstruction |

### ResultExtensions

| Member | Description |
|--------|-------------|
| `ToResult<T, TError>(tuple)` | Convert (error, value) tuple to Result |
| `ToResult<TError>(error)` | Convert nullable error to Result |
| `Then<...>(Task, Func)` | Async transform |
| `Then<...>(Task, Func<..., Task>)` | Async chain |
| `ThenError<...>(Task, Func)` | Async error transform |
