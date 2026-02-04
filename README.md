# Stardust.Utilities

[![CI/CD](https://github.com/dhadner/Stardust.Utilities/actions/workflows/ci.yml/badge.svg)](https://github.com/dhadner/Stardust.Utilities/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/nuget/v/Stardust.Utilities.svg)](https://www.nuget.org/packages/Stardust.Utilities/)

A collection of utility types for .NET applications, focused on bit manipulation, error handling, and big-endian data types. Includes a source 
generator for zero-heap-allocation `[BitFields]` structs.  Provides native hand-coded speed for bit access with no boilerplate needed.

## Table of Contents

[![Stardust Utilities](https://github.com/dhadner/Stardust.Utilities/blob/main/icon.png)](https://github.com/dhadner/Stardust.Utilities)

- [Installation](#installation)
- [Features](#features)
  - [BitField](#bitfield)
  - [Result Types](#result-types)
  - [Big-Endian Types](#big-endian-types)
  - [Extension Methods](#extension-methods)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [Security](#security)
- [License](#license)

---

## Installation

```xml
<PackageReference Include="Stardust.Utilities" Version="0.9.2" />
```

That's it, the source generator is included automatically.

---

## Features



### BitField

**The easiest way to work with hardware registers and bit-packed data.**

The BitField feature automatically creates property implementations for bit fields and flags within a struct. This eliminates boilerplate code and makes working with hardware registers readable and maintainable.

See [BITFIELD.md](https://github.com/dhadner/Stardust.Utilities/blob/main/BITFIELD.md) for comprehensive documentation and examples.

#### [+] Zero Performance Overhead

The source generator emits **inline bit manipulation with compile-time constants** — the exact same code you would write by hand. There is no abstraction penalty, no runtime reflection, no boxing, and no static field allocations.

**Benchmark Results** (n=20 runs, 100M iterations each, .NET 10):

| Test | Generated | ? | Hand-coded | ? | Ratio | ? | 95% CI |
|------|-----------|---|------------|---|-------|---|--------|
| BitFlag GET | 584 ms | 14 | 568 ms | 15 | 1.029 | 0.035 | 0.960 – 1.098 |
| BitFlag SET | 825 ms | 27 | 821 ms | 22 | 1.006 | 0.031 | 0.945 – 1.067 |
| BitField GET | 402 ms | 36 | 405 ms | 18 | 0.995 | 0.087 | 0.824 – 1.166 |
| BitField SET | 413 ms | 9 | 410 ms | 7 | 1.007 | 0.020 | 0.968 – 1.046 |
| Mixed R/W | 1031 ms | 13 | 1030 ms | 23 | 1.001 | 0.024 | 0.954 – 1.048 |
| **Overall** | | | | | **1.008** | **0.048** | **0.914 – 1.102** |

*? = standard deviation (1 sigma). 95% CI = mean ± 1.96?.*


**Result:** Generated code performs within **0.8%** of hand-coded on average.

#### Quick Start

```csharp
using Stardust.Utilities;

[BitFields(typeof(byte))]  // Specify storage type
public partial struct StatusRegister
{
    [BitFlag(0)] public partial bool Ready { get; set; }         // bit 0
    [BitFlag(1)] public partial bool Error { get; set; }         // bit 1
    [BitFlag(7)] public partial bool Busy { get; set; }          // bit 7
    [BitField(2, 4)] public partial byte Mode { get; set; }      // bits 2..=4 (3 bits)
    [BitField(5, 6)] public partial byte Priority { get; set; }  // bits 5..=6 (2 bits)
}
```

The source generator automatically creates the property implementations during build.  The
property type can be defined in your code.  For example, you can make the property types enums 
for better readability with no runtime overhead:

```csharp
using Stardust.Utilities;

public enum OpMode : byte
{
    Mode0 = 0,
    Mode1 = 1,
    Mode2 = 2,
    Mode3 = 3,
    Mode4 = 4,
    Mode5 = 5,
    Mode6 = 6,
    Mode7 = 7,
}

[BitFields(typeof(byte))]  // Specify storage type
public partial struct StatusRegister
{
    [BitFlag(0)] public partial bool Ready { get; set; }        // bit 0
    [BitFlag(1)] public partial bool Error { get; set; }        // bit 1
    [BitFlag(7)] public partial bool Busy { get; set; }         // bit 7
    [BitField(2, 4)] public partial OpMode Mode { get; set; }   // bits 2..=4 (3 bits)
    [BitField(5, 6)] public partial byte Priority { get; set; } // bits 5..=6 (2 bits)
}
```

#### Usage

```csharp
// Create using constructor or implicit conversion
StatusRegister status = 0;
var status2 = new StatusRegister(0x42);

// Set individual flags
status.Ready = true;
status.Error = false;

// Set multi-bit fields
status.Mode = 5;      // Sets bits 2-4 to value 5
status.Priority = 2;  // Sets bits 5-6 to value 2

// Read values
bool isReady = status.Ready;
byte mode = status.Mode;

// Implicit conversion (generated automatically)
byte b = status;               // Converts to byte
status = 0x42;                 // Converts from byte
```

#### Attributes

| Attribute | Parameters | Description |
|-----------|------------|-------------|
| `[BitFields(typeof(T))]` | `T`: storage type | Marks struct; generator creates private `Value` field |
| `[BitFlag(bit)]` | `bit`: 0-based position | Single-bit boolean flag |
| `[BitField(startBit, endBit)]` | Rust-style inclusive range | Multi-bit field (width = endBit - startBit + 1) |

**BitField Examples:**
- `[BitField(0, 2)]` - 3-bit field at bits 0, 1, 2 (like Rust's `0..=2`)
- `[BitField(4, 7)]` - 4-bit field at bits 4, 5, 6, 7 (like Rust's `4..=7`)
- `[BitField(3, 3)]` - 1-bit field at bit 3 only

#### Supported Storage Types

| Storage Type | Size | Signed Alternative |
|--------------|------|-------------------|
| `byte` | 8 bits | `sbyte` |
| `ushort` | 16 bits | `short` |
| `uint` | 32 bits | `int` |
| `ulong` | 64 bits | `long` |

#### Nested Structs

BitFields structs can be nested inside classes or other structs. Containing types must be marked `partial`:

```csharp
public partial class HardwareController
{
    [BitFields(typeof(ushort))]
    public partial struct StatusRegister
    {
        [BitFlag(0)] public partial bool Ready { get; set; }
        [BitFlag(1)] public partial bool Error { get; set; }
        [BitField(8, 15)] public partial byte ErrorCode { get; set; }  // bits 8..=15 (8 bits)
    }
    
    private StatusRegister _status;
    
    public bool IsReady => _status.Ready;
}
```

#### Generated Operators

BitFields types are full-featured numeric types with complete operator support:

```csharp
StatusRegister a = 0x42;
StatusRegister b = 0x18;

// Arithmetic operators
var sum = a + b;           // Addition
var diff = a - b;          // Subtraction
var prod = a * 2;          // Multiplication (with storage type)
var quot = a / b;          // Division
var rem = a % b;           // Modulus
var neg = -a;              // Unary negation (two's complement)

// Bitwise operators
var or = a | b;            // OR
var and = a & b;           // AND
var xor = a ^ b;           // XOR
var not = ~a;              // Complement
var shl = a << 2;          // Left shift
var shr = a >> 2;          // Right shift
var ushr = a >>> 2;        // Unsigned right shift

// Comparison operators
bool lt = a < b;           // Less than
bool le = a <= b;          // Less than or equal
bool gt = a > b;           // Greater than
bool ge = a >= b;          // Greater than or equal

// Equality operators
bool eq = a == b;          // Equality
bool ne = a != b;          // Inequality

// Mixed-type operations (with storage type)
StatusRegister c = a + (byte)10;
StatusRegister d = (byte)5 | b;
```

#### Parsing Support

BitFields types implement `IParsable<T>` and `ISpanParsable<T>` with support for multiple formats:

```csharp
// Decimal parsing
StatusRegister dec = StatusRegister.Parse("255");

// Hexadecimal parsing (0x or 0X prefix)
StatusRegister hex = StatusRegister.Parse("0xFF");
StatusRegister hex2 = StatusRegister.Parse("0XAB");

// Binary parsing (0b or 0B prefix)
StatusRegister bin = StatusRegister.Parse("0b11111111");
StatusRegister bin2 = StatusRegister.Parse("0B10101010");

// C#-style underscore digit separators (all formats)
StatusRegister d1 = StatusRegister.Parse("1_000");           // Decimal: 1000
StatusRegister h1 = StatusRegister.Parse("0xFF_00");         // Hex: 0xFF00  
StatusRegister b1 = StatusRegister.Parse("0b1111_0000");     // Binary: 0xF0

// TryParse for safe parsing
if (StatusRegister.TryParse("0b1010_1010", out var result))
{
    Console.WriteLine(result);  // 0xAA
}

// With format provider
var parsed = StatusRegister.Parse("42", CultureInfo.InvariantCulture);
```

#### Formatting Support

BitFields types implement `IFormattable` and `ISpanFormattable`:

```csharp
StatusRegister value = 0xAB;

// Standard format strings
string hex = value.ToString("X2", null);    // "AB"
string dec = value.ToString("D", null);     // "171"

// Default ToString returns hex
string str = value.ToString();              // "0xAB"

// Allocation-free formatting
Span<char> buffer = stackalloc char[10];
if (value.TryFormat(buffer, out int written, "X4", null))
{
    // buffer[..written] contains "00AB"
}
```

#### Fluent API

Generated `With{PropertyName}` methods enable immutable-style updates:

```csharp
StatusRegister initial = 0;

// Fluent building
var configured = initial
    .WithReady(true)
    .WithMode(5)
    .WithPriority(2);

// Original unchanged
Console.WriteLine(initial);     // 0x0
Console.WriteLine(configured);  // 0x6D
```

#### Static Bit and Mask Properties

For each flag and field, static properties provide the corresponding bit patterns:

```csharp
// For [BitFlag(0)] Ready - get a value with only that bit set
StatusRegister readyBit = StatusRegister.ReadyBit;   // 0x01

// For [BitField(2, 4)] Mode - get the mask for that field
StatusRegister modeMask = StatusRegister.ModeMask;   // 0x1C (bits 2-4)

// Useful for testing and masking
if ((status & StatusRegister.ReadyBit) != 0)
{
    // Ready bit is set
}

var modeOnly = status & StatusRegister.ModeMask;
```

#### Interface Implementations

Every BitFields type automatically implements:

| Interface | Purpose |
|-----------|---------|
| `IComparable` | Non-generic comparison |
| `IComparable<T>` | Generic comparison |
| `IEquatable<T>` | Value equality |
| `IFormattable` | Format string support |
| `ISpanFormattable` | Allocation-free formatting |
| `IParsable<T>` | String parsing |
| `ISpanParsable<T>` | Span-based parsing |

---

### Result Types

Railway-Oriented Programming (ROP) error handling without exceptions. Inspired by Rust's `Result<T, E>` type.

See [RESULT.md](https://github.com/dhadner/Stardust.Utilities/blob/main/RESULT.md) for comprehensive documentation and examples.

#### Basic Usage

Use `global using static` to enable cleaner Ok() and Err() syntax:

```csharp
// In GlobalUsings.cs
global using static Stardust.Utilities.Result<int,string>;
global using static Stardust.Utilities.Result<string>;
// Add additional global usings as needed for other Result types for clean usage syntax.

// In your source filees
// Function that might fail
Result<int, string> Divide(int a, int b)
{
    if (b == 0)
        return Err("Division by zero");
    return Ok(a / b);
}

// Using the result
var result = Divide(10, 2);

if (result.IsSuccess)
{
    Console.WriteLine($"Result: {result.Value}");
}
else
{
    Console.WriteLine($"Error: {result.Error}");
}

// Pattern matching
var message = result.Match(
    onSuccess: value => $"Answer is {value}",
    onFailure: error => $"Failed: {error}"
);

// Chaining operations
var finalResult = Divide(100, 5)
    .Then(x => x * 2)                    // Transform value
    .Then(x => Divide(x, 2))             // Chain another Result
    .OnSuccess(x => Console.WriteLine(x)) // Side effect on success
    .OnFailure(e => Log(e));              // Side effect on failure
```

#### Void Results

For operations that succeed or fail without returning a value:

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

// Global using makes this even cleaner:
// In your GlobalUsings.cs or at top of file:
global using static Stardust.Utilities.Result<string>;

// Then simply:
return Ok();
return Err("Something went wrong");
```

---

### Big-Endian Types

**Type-safe network byte order integers with full operator support.**

Big-endian types store bytes in network order (most significant byte first), essential for network protocols, binary file formats, and hardware emulation. These aren't just byte-swapping utilities—they're complete numeric types with arithmetic, bitwise, and comparison operators.

See [ENDIAN.md](https://github.com/dhadner/Stardust.Utilities/blob/main/ENDIAN.md) for comprehensive documentation and examples.

#### Available Types

| Type | Size | Native Equivalent |
|------|------|-------------------|
| `UInt16Be` / `Int16Be` | 2 bytes | `ushort` / `short` |
| `UInt32Be` / `Int32Be` | 4 bytes | `uint` / `int` |
| `UInt64Be` / `Int64Be` | 8 bytes | `ulong` / `long` |

#### Quick Example

```csharp
using Stardust.Utilities;

// Create from native values
UInt32Be networkValue = 0x12345678;
// Stored in memory as: [0x12, 0x34, 0x56, 0x78] (big-endian)

// Arithmetic and bitwise operations work naturally
UInt32Be sum = networkValue + 100;
UInt32Be masked = networkValue & 0xFF00FF00;

// Zero-allocation I/O with Span<byte>
Span<byte> buffer = stackalloc byte[4];
networkValue.WriteTo(buffer);

// Read back
var restored = new UInt32Be(buffer);
// Or: var restored = UInt32Be.ReadFrom(buffer);

// Implicit conversion to/from native types
uint native = networkValue;
networkValue = 0xDEADBEEF;

// Modern .NET parsing (IParsable<T>, ISpanParsable<T>)
UInt64Be parsed = UInt64Be.Parse("18446744073709551615", null);
if (UInt32Be.TryParse("DEADBEEF".AsSpan(), NumberStyles.HexNumber, null, out var hex))
{
    Console.WriteLine(hex);  // 0xdeadbeef
}

// Allocation-free formatting (ISpanFormattable)
Span<char> chars = stackalloc char[16];
networkValue.TryFormat(chars, out int written, "X8", null);
```

#### Key Features

- **Full operator support**: `+`, `-`, `*`, `/`, `%`, `&`, `|`, `^`, `~`, `<<`, `>>`, `<`, `>`, `==`, etc.
- **Modern .NET interfaces**: `IParsable<T>`, `ISpanParsable<T>`, `ISpanFormattable`
- **Zero-allocation APIs**: `ReadOnlySpan<byte>` constructors, `WriteTo(Span<byte>)`, `TryWriteTo()`
- **Type converters**: PropertyGrid support for UI editing
- **Guaranteed layout**: `[StructLayout(LayoutKind.Explicit)]` ensures correct byte ordering

---

### Extension Methods

Utility extension methods for bit manipulation.

See [EXTENSIONS.md](https://github.com/dhadner/Stardust.Utilities/blob/main/EXTENSIONS.md) for comprehensive documentation and examples.

```csharp
using Stardust.Utilities;

// Hi/Lo byte extraction
ushort word = 0x1234;
byte hi = word.Hi();  // 0x12
byte lo = word.Lo();  // 0x34

// Hi/Lo modification
word = word.SetHi(0xFF);  // 0xFF34
word = word.SetLo(0x00);  // 0xFF00

// Saturating arithmetic (clamps instead of overflows)
int a = int.MaxValue;
int result = a.SaturatingAdd(1);     // Still int.MaxValue, not overflow
uint r2 = 10u.SaturatingSub(20u);    // 0, not large number

```

---

## Troubleshooting

### "Partial property must have an implementation" errors

**Problem:** Compiler errors like `CS9248: Partial property 'MyStruct.MyProperty' must have an implementation part`.

**Cause:** The source generator isn't running.

**Solution:** 
1. Ensure you have the NuGet package installed:
   ```xml
   <PackageReference Include="Stardust.Utilities" Version="0.2.0" />
   ```
2. Clean and rebuild the solution
3. Restart Visual Studio if needed (sometimes required after first install)

### Generated code not updating

**Problem:** You changed your `[BitFields]` struct but the generated code wasn't updated.

**Solution:**
1. Ensure you're using `partial struct` (not `class` or `record`)
2. Check that attributes are spelled correctly: `[BitFields]`, `[BitField]`, `[BitFlag]`
3. Clean and rebuild the solution

### IntelliSense not working for generated members

**Problem:** Visual Studio doesn't show IntelliSense for generated properties or methods.

**Solution:**
1. Build the project at least once (source generators run during build)
2. If still not working, close and reopen the solution
3. Check **View ? Error List** for any generator errors
4. Ensure your Visual Studio is up to date (VS 2022 17.0+ required for incremental generators)

### Viewing generated code

**In Visual Studio:**
1. Expand your project in **Solution Explorer**
2. Expand **Dependencies** ? **Analyzers** ? **Stardust.Generators**
3. Expand the generator (e.g., **Stardust.Generators.BitFieldsGenerator**)
4. Double-click any `.g.cs` file to view the generated source

**On disk:**
Generated files are located at:
```
obj/Debug/net*/generated/Stardust.Generators/
    Stardust.Generators.BitFieldsGenerator/
        YourTypeName.g.cs
```

**Tip:** To persist generated files to disk (useful for source control or debugging), add to your `.csproj`:
```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```
This creates a `Generated/` folder in your project with all generated `.cs` files.

---

## License

This project is provided under the MIT License.

See [LICENSE](https://github.com/dhadner/Stardust.Utilities/blob/main/LICENSE) for license details.

---

## Contributing

Contributions are welcome! Please read our guidelines before submitting issues or pull requests.

- [Contributing Guidelines](https://github.com/dhadner/Stardust.Utilities/blob/main/CONTRIBUTING.md)
- [Code of Conduct](https://github.com/dhadner/Stardust.Utilities/blob/main/CODE_OF_CONDUCT.md)

---

## Security

To report a security vulnerability, please use GitHub's private vulnerability reporting feature. **Do not report security issues through public GitHub issues.**

See [SECURITY.md](https://github.com/dhadner/Stardust.Utilities/blob/main/SECURITY.md) for details.

