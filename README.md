# Stardust.Utilities

A collection of utility types for .NET applications, focused on bit manipulation, error handling, and type-safe discriminated unions. Includes source generators for zero-boilerplate `[BitFields]` and `[EnhancedEnum]` structs.

## Table of Contents

- [Installation](#installation)
- [Features](#features)
  - [BitField](#bitfield)
  - [EnhancedEnum](#enhancedenum)
  - [Result Types](#result-types)
  - [Big-Endian Types](#big-endian-types)
  - [BitStream](#bitstream)
  - [Extension Methods](#extension-methods)
- [Troubleshooting](#troubleshooting)

---

## Installation

```xml
<PackageReference Include="Stardust.Utilities" Version="0.2.0" />
```

**That's it!** The source generator is included automatically.

---

## Features



### BitField

**The easiest way to work with hardware registers and bit-packed data.**

The BitField feature automatically creates property implementations for bit fields and flags within a struct. This eliminates boilerplate code and makes working with hardware registers readable and maintainable.

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

Two usage patterns are supported:

**Option 1: User-Declared Value Field**
```csharp
using Stardust.Utilities;

[BitFields]
public partial struct StatusRegister
{
    public byte Value;  // You declare this

    [BitFlag(0)] public partial bool Ready { get; set; }
    [BitFlag(1)] public partial bool Error { get; set; }
    [BitFlag(7)] public partial bool Busy { get; set; }
    [BitField(2, 3)] public partial byte Mode { get; set; }
    [BitField(5, 2)] public partial byte Priority { get; set; }
}
```

**Option 2: Generator-Created Value Field**
```csharp
using Stardust.Utilities;

[BitFields(typeof(byte))]  // Specify storage type
public partial struct StatusRegister
{
    // No Value field needed - generator creates it as private

    [BitFlag(0)] public partial bool Ready { get; set; }
    [BitFlag(1)] public partial bool Error { get; set; }
    [BitFlag(7)] public partial bool Busy { get; set; }
    [BitField(2, 3)] public partial byte Mode { get; set; }
    [BitField(5, 2)] public partial byte Priority { get; set; }
}
```

The source generator automatically creates the property implementations during build.

#### Usage

```csharp
// Create with user-declared Value field
var status = new StatusRegister { Value = 0 };

// Or with generator-created Value field (use constructor or implicit conversion)
StatusRegister status2 = 0;
var status3 = new StatusRegister(0x42);

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
| `[BitFields]` | none | Marks struct; user must declare `Value` field |
| `[BitFields(typeof(T))]` | `T`: storage type | Marks struct; generator creates private `Value` field |
| `[BitFlag(bit)]` | `bit`: 0-based position | Single-bit boolean flag |
| `[BitField(shift, width)]` | `shift`: start bit, `width`: bit count | Multi-bit field |

#### Supported Storage Types

| Storage Type | Size | Max BitField Width |
|--------------|------|-------------------|
| `byte` | 8 bits | 8 |
| `ushort` | 16 bits | 16 |
| `uint` | 32 bits | 32 |
| `ulong` | 64 bits | 64 |

---

### Result Types

Railway-oriented error handling without exceptions. Inspired by Rust's `Result<T, E>` type.

#### Basic Usage

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

Network byte order (big-endian) integer types with automatic byte swapping.

```csharp
using Stardust.Utilities;

// Create from native values
UInt16Be value16 = 0x1234;  // Stored as 0x12, 0x34 in memory
UInt32Be value32 = 0x12345678;
Int16Be signed16 = -1;
Int32Be signed32 = -1000;

// Implicit conversion to/from native types
ushort native = value16;    // Automatic conversion
value16 = (ushort)0xABCD;   // Automatic conversion

// Arithmetic works normally
UInt16Be sum = value16 + 1;

// Serialization to bytes
byte[] buffer = new byte[4];
value32.ToBytes(buffer, offset: 0);  // Big-endian order

// Parse from bytes
var fromBytes = new UInt32Be(buffer, offset: 0);

// Extension methods
byte hi = value16.Hi();  // High byte
byte lo = value16.Lo();  // Low byte
```

#### TypeConverters

Each type has a corresponding `TypeConverter` for use with `PropertyGrid` and other UI frameworks:
- `UInt16BeTypeConverter`
- `UInt32BeTypeConverter`
- `Int16BeTypeConverter`
- `Int32BeTypeConverter`

---

### BitStream

Read and write individual bits to a stream.

```csharp
using Stardust.Utilities.Bits;

var stream = new BitStream();

// Write individual bits
stream.Write(true);
stream.Write(false);
stream.Write(true);

// Write bytes
stream.WriteByte(0xAB);

// Read back
stream.Position = 0;
bool bit1 = stream.Read();  // true
bool bit2 = stream.Read();  // false
int byte1 = stream.ReadByte();  // 0xAB (if aligned)

// Seek within the stream
stream.Seek(0, SeekOrigin.Begin);
stream.Seek(-1, SeekOrigin.Current);

// Truncate
stream.Truncate(8, SeekOrigin.Begin);  // Remove first 8 bits
```


---


### EnhancedEnum

Discriminated unions (sum types) for C#. Similar to Rust enums with associated data.

**Why use this?** In C#, a regular `enum` can only hold a single integer value. But what if each enum case needs to carry *different* data? For example, a debugger command might be "Step" (no data), "SetBreakpoint" (needs an address), or "Evaluate" (needs an expression string). `EnhancedEnum` lets each variant carry its own strongly-typed payload.

Stardust.Utilities provides **three approaches** to discriminated unions:

| Approach | Best For | Pattern Matching | Allocation |
|----------|----------|------------------|------------|
| **Source Generator** | Most use cases | Match method | **Zero allocation** (struct) |
| **EnhancedEnum&lt;T&gt;** | Manual control | Via Deconstruct | Boxes value types |
| **EnhancedEnumFlex&lt;T&gt;** | Hot paths | Manual if/TryGet | Zero allocation |

---

#### EnhancedEnum (Source Generator) **[Recommended]**

The source generator creates a **zero-allocation struct** with inline payload storage. Both value types and reference types are supported.

```csharp
using Stardust.Utilities;

[EnhancedEnum]
public partial struct DebugCommand
{
    [EnumKind]
    public enum Kind
    {
        [EnumValue(typeof((uint address, int hitCount)))]
        SetBreakpoint,
        
        [EnumValue(typeof(string))]  // Reference types work too
        Evaluate,
        
        [EnumValue]  // No payload
        Step,
        
        [EnumValue]
        Continue,
    }
}
```

**What gets generated:**

```csharp
public readonly partial struct DebugCommand : IEquatable<DebugCommand>
{
    // Private fields for payloads (stored inline, no boxing)
    private readonly Kind _tag;
    private readonly (uint address, int hitCount) _setBreakpointPayload;
    private readonly string? _evaluatePayload;
    
    // Static factory methods
    public static DebugCommand SetBreakpoint((uint, int) value);
    public static DebugCommand Evaluate(string value);
    public static DebugCommand Step();
    public static DebugCommand Continue();
    
    // Is properties
    public bool IsSetBreakpoint => _tag == Kind.SetBreakpoint;
    
    // TryGet methods
    public bool TryGetSetBreakpoint(out (uint, int) value);
    
    // Match methods (exhaustive)
    public TResult Match<TResult>(
        Func<(uint, int), TResult> SetBreakpoint,
        Func<string, TResult> Evaluate,
        Func<TResult> Step,
        Func<TResult> @Continue);
}
```

**Usage:**

```csharp
// Create variants using static factory methods
var cmd1 = DebugCommand.SetBreakpoint((0x00401000u, 3));
var cmd2 = DebugCommand.Evaluate("PC + 4");
var cmd3 = DebugCommand.Step();

// Pattern matching with Match (exhaustive, compiler-enforced)
string result = cmd.Match(
    SetBreakpoint: bp => $"BP at 0x{bp.address:X8}, hits={bp.hitCount}",
    Evaluate: expr => $"Eval: {expr}",
    Step: () => "Step",
    @Continue: () => "Continue"
);

// TryGet for conditional access
if (cmd.TryGetSetBreakpoint(out var bp))
{
    Console.WriteLine($"Address: 0x{bp.address:X8}");
}

// Is properties for quick checks
if (cmd.IsStep)
{
    // Handle step
}

// Equality works correctly
var a = DebugCommand.Step();
var b = DebugCommand.Step();
bool same = a == b;  // true
```

**Benefits:**
- [x] **Zero allocation** - struct, no heap
- [x] **No boxing** - value payloads stored inline
- [x] **Reference types supported** - stored as references
- [x] **Type-safe** - compiler-enforced exhaustive matching
- [x] **AggressiveInlining** - optimal performance

---

#### EnhancedEnum&lt;T&gt; (Manual Approach)

For cases where you need more control or want to avoid the code generator:

```csharp
using Stardust.Utilities;

public enum DebugCommandKind 
{ 
    Step, StepOver, SetBreakpoint, Evaluate
}

public sealed record DebugCommand : EnhancedEnum<DebugCommandKind>
{
    static DebugCommand()
    {
        RegisterKindType(DebugCommandKind.Step, null);
        RegisterKindType(DebugCommandKind.StepOver, null);
        RegisterKindType(DebugCommandKind.SetBreakpoint, typeof((uint address, int hitCount)));
        RegisterKindType(DebugCommandKind.Evaluate, typeof(string));
    }

    public DebugCommand(DebugCommandKind kind, object? value = null) : base(kind, value) { }
    
    // Optional: convenience factory methods
    public static DebugCommand Step() => new(DebugCommandKind.Step);
    public static DebugCommand SetBreakpoint(uint address, int hitCount = 1) 
        => new(DebugCommandKind.SetBreakpoint, (address, hitCount));
}
```

**Pattern matching via Deconstruct:**

```csharp
string result = cmd switch
{
    (DebugCommandKind.SetBreakpoint, (uint addr, int hits)) => $"BP: {addr:X8}",
    (DebugCommandKind.Evaluate, string expr) => $"Eval: {expr}",
    (DebugCommandKind.Step, _) => "Step",
    _ => "Unknown"
};

// Type-safe extraction
if (cmd.TryValueAs(out (uint address, int hitCount) bp))
{
    Console.WriteLine($"Address: 0x{bp.address:X8}");
}
```

---

#### EnhancedEnumFlex&lt;T&gt; (Legacy Zero-Allocation)

`EnhancedEnumFlex<T>` predates the struct-based source generator. For new code, prefer the source generator which offers the same zero-allocation benefits with better ergonomics.

---

### Extension Methods

Utility extension methods for bit manipulation.

```csharp
using Stardust.Utilities;

// Bit checking
byte value = 0b10101010;
bool isSet = value.IsSet(0b00001000);    // true
bool isClear = value.IsClear(0b00000001); // true

// Hi/Lo byte extraction
ushort word = 0x1234;
byte hi = word.Hi();  // 0x12
byte lo = word.Lo();  // 0x34

// Hi/Lo modification
word = word.SetHi(0xFF);  // 0xFF34
word = word.SetLo(0x00);  // 0xFF00

// Boolean to byte
bool flag = true;
byte b = flag.ToByte();  // 1


// Saturating arithmetic (clamps instead of overflow)
int a = int.MaxValue;
int result = a.SaturatingAdd(1);  // Still int.MaxValue, not overflow
int r2 = 10.SaturatingSub(20);    // 0, not negative (for uint)

// Enum flag checking
[Flags] enum Options { None = 0, A = 1, B = 2 }
Options opts = Options.A | Options.B;
bool hasA = opts.IsSet(Options.A);   // true
bool noC = opts.IsClear(Options.A);  // false
```

---

## Troubleshooting

### "Partial property must have an implementation" errors

**Problem:** Compiler errors like `CS9248: Partial property 'MyStruct.MyProperty' must have an implementation part`.

**Cause:** The source generator isn't running.

**Solution:** Make sure you have the NuGet package installed:
```xml
<PackageReference Include="Stardust.Utilities" Version="0.2.0" />
```
Then clean and rebuild the solution.

### Generated code not updating

**Problem:** You changed your `[EnhancedEnum]` or `[BitFields]` struct but the generated code wasn't updated.

**Solution:**
1. Ensure you're using `partial struct` (not `class` or `record`)
2. Check that attributes are spelled correctly: `[EnhancedEnum]`, `[EnumKind]`, `[EnumValue]`, `[BitFields]`, `[BitField]`, `[BitFlag]`
3. For `[EnhancedEnum]`, ensure the `Kind` enum is nested inside the struct and has `[EnumKind]` attribute
4. Clean and rebuild the solution

### Viewing generated code

To inspect the generated code:
1. In Visual Studio, expand your project in Solution Explorer
2. Expand **Dependencies** ? **Analyzers** ? **Stardust.Generators**
3. You'll see the generated `.g.cs` files for each type

Alternatively, look in your project's `obj/Debug/net*/Stardust.Generators/` folder.

---

## License

This project is provided under the MIT License.

## Contributing

Contributions are welcome! Please open an issue or pull request on GitHub.
