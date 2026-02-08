# Stardust.Utilities

[![CI/CD](https://github.com/dhadner/Stardust.Utilities/actions/workflows/ci.yml/badge.svg)](https://github.com/dhadner/Stardust.Utilities/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Stardust.Utilities.svg)](https://www.nuget.org/packages/Stardust.Utilities/)
[![.NET 7](https://img.shields.io/badge/.NET-7.0-512BD4)](https://dotnet.microsoft.com/)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

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
<PackageReference Include="Stardust.Utilities" Version="0.9.4" />
```

That's it, the source generator is included automatically.

---

## Features



### BitField

**The easiest way to work with hardware registers and bit-packed data.**

The BitField feature automatically creates property implementations for bit fields and flags within a struct. 
This eliminates boilerplate code and makes working with hardware registers, S/W floating point, nested
protocol headers, instruction opcodes, file headers, and other bit-packed structures highly performant, 
readable and maintainable.

See [BITFIELD.md](https://github.com/dhadner/Stardust.Utilities/blob/main/BITFIELD.md) for comprehensive 
documentation and examples.

#### :white_check_mark: Zero Performance Overhead

The source generator emits **inline bit manipulation (shift and mask) with compile-time constants** — the exact same code you 
would write by hand. There is no abstraction penalty, no runtime reflection, no boxing, and no heap allocations.

> **TL;DR for Architects:** You can confidently use BitFields in performance-critical code. The JIT compiler 
completely eliminates property accessor overhead through inlining, resulting in **identical performance to raw 
bit manipulation**.

**Property Accessors vs Raw Bit Manipulation** (500M iterations, .NET 10):

| Operation | Raw Bit Ops | Generated Properties | Difference |
|-----------|-------------|---------------------|------------|
| Boolean GET | 271 ms | 263 ms | ≈0% (noise) |
| Boolean SET | 506 ms | 494 ms | ≈0% (noise) |
| Field GET (shift+mask) | 124 ms | 123 ms | ≈0% (noise) |

*All differences are within measurement noise. The generated code is statistically indistinguishable from raw inline bit manipulation.*


**Generated vs Hand-Coded Properties** (n=20 runs, 500M iterations each, .NET 10):

| Test | Generated | Hand-coded | Ratio |
|------|-----------|------------|-------|
| BitFlag GET | 248 ms | 311 ms | ~1.0 |
| BitFlag SET | 578 ms | 509 ms | ~1.0 |
| BitField GET | 372 ms | 300 ms | ~1.0 |
| BitField SET | 591 ms | 592 ms | ~1.0 |
| Mixed R/W | 911 ms | 953 ms | ~1.0 |
| **Overall** | | | **~1.0** (&sigma;=0.07) |

*Individual run variations are due to system load and CPU scheduling, not code differences.*

**Key Findings:**
- ✅ **Zero abstraction penalty** — `[MethodImpl(MethodImplOptions.AggressiveInlining)]` eliminates all property call overhead
- ✅ **Identical to raw bit ops** — Generated properties are statistically indistinguishable from `(value >> SHIFT) & MASK` inline code
- ✅ **Compile-time constants** — All masks and shifts are computed at compile time
- ✅ **No heap allocations** — Value types with no boxing

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
| `[BitFields(typeof(T))]` | `T`: storage type, optional `UndefinedBitsMustBe` | Marks struct; generator creates private `Value` field |
| `[BitFlag(bit)]` | `bit`: 0-based position, optional `MustBe` | Single-bit boolean flag |
| `[BitField(startBit, endBit)]` | Rust-style inclusive range, optional `MustBe` | Multi-bit field (width = endBit - startBit + 1) |

**BitField Examples:**
- `[BitField(0, 2)]` - 3-bit field at bits 0, 1, 2 (like Rust's `0..=2`)
- `[BitField(4, 7)]` - 4-bit field at bits 4, 5, 6, 7 (like Rust's `4..=7`)
- `[BitField(3, 3)]` - 1-bit field at bit 3 only

#### Supported Storage Types

| Storage Type | Size | Notes |
|--------------|------|-------|
| `byte` | 8 bits | Signed alternative: `sbyte` |
| `ushort` | 16 bits | Signed alternative: `short` |
| `uint` | 32 bits | Signed alternative: `int` |
| `ulong` | 64 bits | Signed alternative: `long` |
| `UInt128` | 128 bits | Signed alternative: `Int128` |
| `Half` | 16 bits | IEEE 754 half-precision |
| `float` | 32 bits | IEEE 754 single-precision |
| `double` | 64 bits | IEEE 754 double-precision |
| `decimal` | 128 bits | .NET decimal (96-bit coefficient + scale + sign) |
| `[BitFields(N)]` | N bits | Arbitrary width, 1 to 16,384 bits |

#### Signed Property Types (Sign Extension)

When a property is declared with a signed type (`sbyte`, `short`, `int`, `long`), the generator automatically sign-extends the field value. This is essential for hardware registers with signed quantities like deltas or offsets:

```csharp
[BitFields(typeof(ushort))]
public partial struct MotionRegister
{
    // 3-bit signed delta. Values: -4 to +3
    [BitField(13, 15)] public partial sbyte DeltaX { get; set; }
    
    // 3-bit unsigned field. Values: 0 to 7 (no sign extension)
    [BitField(10, 12)] public partial byte Speed { get; set; }
}

// Usage
MotionRegister reg = 0;
reg.DeltaX = -3;
Console.WriteLine(reg.DeltaX);  // Output: -3 (correctly sign-extended)


reg.Speed = 5;
Console.WriteLine(reg.Speed);   // Output: 5 (unsigned, stays positive)
```

The sign extension is optimized to a single mask-and-shift operation with zero overhead for unsigned property types.

#### BitFields Composition

BitFields structs can be used as property types within other BitFields structs, enabling reusable sub-structures:

```csharp
// Reusable flags structure
[BitFields(typeof(byte))]
public partial struct StatusFlags
{
    [BitFlag(0)] public partial bool Ready { get; set; }
    [BitFlag(1)] public partial bool Error { get; set; }
    [BitField(4, 7)] public partial byte Priority { get; set; }
}

// Header that embeds StatusFlags
[BitFields(typeof(ushort))]
public partial struct ProtocolHeader
{
    [BitField(0, 7)] public partial StatusFlags Status { get; set; }  // Embedded!
    [BitField(8, 15)] public partial byte Length { get; set; }
}

// Usage - chained property access works
ProtocolHeader header = 0;
header.Status = new StatusFlags { Ready = true, Priority = 5 };
bool ready = header.Status.Ready;  // true
```

#### Undefined and Reserved Bits

When a struct doesn't define fields covering all bits of its storage type, those **undefined bits** can be controlled with `UndefinedBitsMustBe`:

```csharp
// Protocol header: undefined bits are always zero (clean serialization)
[BitFields(typeof(ushort), UndefinedBitsMustBe.Zeroes)]
public partial struct SubHeader9
{
    [BitField(0, 3)] public partial byte TypeCode { get; set; }  // bits 0-3
    [BitField(4, 8)] public partial byte Flags { get; set; }     // bits 4-8
    // Bits 9-15: UNDEFINED — always forced to zero
}

SubHeader9 sub = 0xFFFF;  // Try to set all 16 bits
ushort raw = sub;         // raw == 0x01FF (undefined bits masked off)
```

| `UndefinedBitsMustBe` Value | Behavior | Use Case |
|-----------------------------|----------|----------|
| `.Any` (default) | Preserved as raw data | Hardware registers |
| `.Zeroes` | Always masked to zero | Protocol headers, serialization |
| `.Ones` | Always set to one | Reserved-high protocols |

This works with **sparse** undefined bits too (gaps between fields, not just high bits), and is enforced in the constructor so all operations — conversions, arithmetic, bitwise — produce consistent results.

Individual fields and flags can also override their bits with `MustBe`:

```csharp
[BitFields(typeof(byte))]
public partial struct PacketFlags
{
    [BitFlag(0)] public partial bool Valid { get; set; }      // Normal flag
    [BitFlag(7, MustBe.One)] public partial bool Sync { get; set; }  // Always 1
    [BitField(1, 3, MustBe.Zero)] public partial byte Reserved { get; set; }  // Always 0
}
```

See [BITFIELD.md](https://github.com/dhadner/Stardust.Utilities/blob/main/BITFIELD.md) for full details on undefined bits, sparse patterns, and composition with partial-width structs.

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

The easiest way to view generated code is to use **Go to Definition**:

1. In your code, place the cursor on any generated type (e.g., `StatusRegister`) or property (e.g., `.Ready`, `.Mode`)
2. Press **F12** (or right-click → **Go to Definition**)
3. Visual Studio opens the generated `.g.cs` file with full IntelliSense support

This works because VS maintains the generated code in memory during compilation. From the opened file you can:
- View all generated operators, properties, and methods
- Use **Find All References** (Shift+F12) to see all usages
- Navigate to other generated members

**Note:** The file opens from a temporary location - this is normal and expected.

### Saving generated code to disk

If you need to persist generated files to disk (for source control, code review, or CI inspection), add this to your `.csproj`:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>

<!-- Exclude persisted files from compilation (the generator already compiles them) -->
<ItemGroup>
  <Compile Remove="$(CompilerGeneratedFilesOutputPath)/**/*.cs" />
</ItemGroup>
```

This creates a `Generated/` folder with all `.g.cs` files.

**Important limitations:**
- These files are **reference copies only** - not for interactive development
- The files will **not appear in Solution Explorer** (they're excluded from the project)
- IntelliSense and "Find All References" **do not work** from these disk files
- For interactive development, use **F12 (Go to Definition)** instead

To view the files, open them directly from File Explorer or use **File → Open → File** in Visual Studio.

If you want to be able to open these files directly from the Visual Studio Solution Explorer,
you can add them with their Build Action set to "None" by adding this to your `.csproj`:

```
<ItemGroup>
  <!-- 
       Exclude persisted generated files from compilation (they're already compiled by the generator)
       but make them visible in Solution Explorer as non-compiled files.

       NOTE: Visual Studio does not support full Intellisense for these files - only references within
       the file itself.  Use the Shift+F12 "Find all references" method above to find usage across all your projects.
  -->
  <None Include="$(CompilerGeneratedFilesOutputPath)/**/*.cs" />
</ItemGroup>
```

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

