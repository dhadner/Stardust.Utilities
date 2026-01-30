# Stardust.Utilities

A collection of utility types for .NET applications, focused on bit manipulation, error handling, and type-safe discriminated unions.

## Table of Contents

- [Installation](#installation)
- [Features](#features)
  - [BitField (Source Generator)](#bitfield-source-generator)
  - [Result Types](#result-types)
  - [Big-Endian Types](#big-endian-types)
  - [BitStream](#bitstream)
  - [EnhancedEnum](#enhancedenum)
    - [Source Generator (Recommended)](#enhancedenum-source-generator--recommended)
    - [Manual Approach](#enhancedenumt-manual-approach)
    - [Zero-Allocation (EnhancedEnumFlex)](#enhancedenumflext-zero-allocation-hot-path)
  - [Extension Methods](#extension-methods)

---

## Installation

Add a reference to `Stardust.Utilities` in your project. The library targets .NET 10.

```xml
<ProjectReference Include="..\Stardust.Utilities\Stardust.Utilities.csproj" />
```

---

## Features

### BitField (Source Generator)

**The easiest way to work with hardware registers and bit-packed data.**

The BitField feature uses a C# source generator to automatically create property implementations for bit fields and flags within a struct. This eliminates boilerplate code and makes working with hardware registers readable and maintainable.

#### ? Zero Performance Overhead


The source generator produces **exactly the same code** you would write by hand using the non-generic `BitFieldDef32`, `BitFlagDef64`, etc. classes. There is no abstraction penalty, no runtime reflection, and no boxing. The generated code uses the optimized, non-generic bit manipulation types with `[MethodImpl(MethodImplOptions.AggressiveInlining)]` for maximum performance.

**Use the source generator with confidence in hot paths** — you get clean, readable property syntax with the same performance as manual bit manipulation.

#### ?? Important: IntelliSense Errors Before First Build

**This is normal and expected!** When you first create a struct with `[BitFields]`, Visual Studio will show red squiggly errors:

- *"'YourStruct' does not contain a definition for 'YourProperty'"*
- *"A partial property may not have multiple defining declarations"*

**These errors disappear after your first build.** The source generator creates the implementation code during compilation. Until then, Visual Studio doesn't know about the generated code.

**Solution:** Just build your project once (`Ctrl+Shift+B`). The errors will vanish, and IntelliSense will work normally.

#### ?? Enabling Full IntelliSense for Generated Code

To make generated code fully discoverable by IntelliSense (similar to Windows Forms designer code), add these properties to your project file:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)\GeneratedFiles</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

This writes generated source files to `obj\GeneratedFiles\`, allowing Visual Studio to index them for IntelliSense, Go To Definition, and Find All References.

#### Quick Start

```csharp
using Stardust.Utilities;

// 1. Mark the struct with [BitFields]
// 2. Declare a 'Value' field or property (byte, ushort, uint, or ulong)
// 3. Declare partial properties with [BitField] or [BitFlag] attributes

[BitFields]
public partial struct StatusRegister
{
    public byte Value;  // The underlying storage (8 bits)

    // Individual bit flags
    [BitFlag(0)] public partial bool Ready { get; set; }
    [BitFlag(1)] public partial bool Error { get; set; }
    [BitFlag(7)] public partial bool Busy { get; set; }

    // Multi-bit fields
    [BitField(2, 3)] public partial byte Mode { get; set; }  // Bits 2-4 (3 bits wide)
    [BitField(5, 2)] public partial byte Priority { get; set; }  // Bits 5-6 (2 bits wide)
}
```

#### Usage

```csharp
// Create and manipulate
var status = new StatusRegister();

// Set individual flags
status.Ready = true;
status.Error = false;

// Set multi-bit fields
status.Mode = 5;      // Sets bits 2-4 to value 5
status.Priority = 2;  // Sets bits 5-6 to value 2

// Read values
bool isReady = status.Ready;
byte mode = status.Mode;

// Direct value access
byte rawValue = status.Value;  // Get the underlying byte
status.Value = 0xFF;           // Set all bits

// Implicit conversion (generated automatically)
byte b = status;               // Converts to byte
status = 0x42;                 // Converts from byte
```

#### Attributes

| Attribute | Parameters | Description |
|-----------|------------|-------------|
| `[BitFields]` | none | Marks a struct for code generation |
| `[BitFlag(bit)]` | `bit`: 0-based position | Single-bit boolean flag |
| `[BitField(shift, width)]` | `shift`: start bit, `width`: bit count | Multi-bit field |

#### Supported Storage Types

| Storage Type | Size | Max BitField Width |
|--------------|------|-------------------|
| `byte` | 8 bits | 8 |
| `ushort` | 16 bits | 16 |
| `uint` | 32 bits | 32 |
| `ulong` | 64 bits | 64 |

#### Manual BitField Usage (No Source Generator)

If you prefer not to use the source generator, you can use the BitFieldDef types directly:

```csharp
using Stardust.Utilities;

// Define fields once (typically as static readonly)
private static readonly BitFieldDef32 ModeField = new(shift: 2, width: 3);
private static readonly BitFlagDef32 ReadyFlag = new(shift: 0);

// Use them
uint value = 0;
value = ModeField.Set(value, 5);     // Set mode to 5
value = ReadyFlag.Set(value, true);  // Set ready flag

byte mode = ModeField.GetByte(value);  // Read mode
bool ready = ReadyFlag.IsSet(value);   // Read ready flag
```

Available non-generic types: `BitFieldDef8`, `BitFieldDef16`, `BitFieldDef32`, `BitFieldDef64` and their flag counterparts `BitFlagDef8`, `BitFlagDef16`, `BitFlagDef32`, `BitFlagDef64`.

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
| **Source Generator** | Most use cases | ? Native C# patterns | Record per instance |
| **EnhancedEnum&lt;T&gt;** | Manual control | Via Deconstruct | Boxes value types |
| **EnhancedEnumFlex&lt;T&gt;** | Hot paths | Manual if/TryGet | Zero allocation |

---

#### EnhancedEnum (Source Generator) ? Recommended

The source generator creates true C# record types for each variant, giving you **native pattern matching** with minimal boilerplate.

```csharp
using Stardust.Utilities;

[EnhancedEnum]
public partial record DebugCommand
{
    [EnumKind]
    private enum Kind
    {
        [EnumValue(typeof((uint address, int hitCount)))]
        SetBreakpoint,
        
        [EnumValue(typeof(string))]
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
// Nested record types for each variant (with public constructors):
public sealed record SetBreakpoint((uint address, int hitCount) Value) : DebugCommand;
public sealed record Evaluate(string Value) : DebugCommand;
public sealed record Step() : DebugCommand;
public sealed record Continue() : DebugCommand;

// Is properties:
public bool IsSetBreakpoint => this is SetBreakpoint;
public bool IsEvaluate => this is Evaluate;
// etc.
```

**Usage with native C# pattern matching:**

```csharp
// Create instances using 'new'
var cmd1 = new DebugCommand.SetBreakpoint((0x00401000u, 3));
var cmd2 = new DebugCommand.Evaluate("PC + 4");
var cmd3 = new DebugCommand.Step();

// Pattern matching in switch expressions - true C# syntax!
string result = cmd switch
{
    DebugCommand.SetBreakpoint(var bp) => $"BP at 0x{bp.address:X8}, hits={bp.hitCount}",
    DebugCommand.Evaluate(var expr) => $"Eval: {expr}",
    DebugCommand.Step => "Step",
    DebugCommand.Continue => "Continue",
    _ => "Unknown"
};

// Type checking
if (cmd is DebugCommand.SetBreakpoint { Value: var breakpoint })
{
    Console.WriteLine($"Address: 0x{breakpoint.address:X8}");
}

// Is properties
if (cmd.IsSetBreakpoint)
{
    // Handle breakpoint
}
```

#### ?? IntelliSense Errors Before First Build

Same as BitFields: Visual Studio shows errors until you build once. This is normal — the source generator creates the code during compilation.

---

#### EnhancedEnum&lt;T&gt; (Manual Approach)

For cases where you need more control or want to avoid the source generator:

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

#### EnhancedEnumFlex&lt;T&gt; (Zero-Allocation Hot Path)

For performance-critical code where boxing must be avoided:

```csharp
using Stardust.Utilities;

public enum InterruptKind { None, VBlank, DMA, IRQ, Exception }

// Register payload kinds ONCE at startup
static class InterruptSystem
{
    static InterruptSystem()
    {
        var PK = EnhancedEnumFlex<InterruptKind>.PayloadKind;
        EnhancedEnumFlex<InterruptKind>.RegisterKindPayloadKind(InterruptKind.None, PK.None);
        EnhancedEnumFlex<InterruptKind>.RegisterKindPayloadKind(InterruptKind.VBlank, PK.None);
        EnhancedEnumFlex<InterruptKind>.RegisterKindPayloadKind(InterruptKind.DMA, PK.Byte);
        EnhancedEnumFlex<InterruptKind>.RegisterKindPayloadKind(InterruptKind.IRQ, PK.Byte);
        EnhancedEnumFlex<InterruptKind>.RegisterKindPayloadKind(InterruptKind.Exception, PK.UInt32_UInt32);
    }
}

// Create without boxing
var interrupt = EnhancedEnumFlex<InterruptKind>.Create(InterruptKind.DMA, (byte)3);

// Extract without boxing
if (interrupt.TryGetByte(InterruptKind.DMA, out byte channel))
{
    ProcessDMA(channel);
}
```

**Supported inline payload types (no boxing):**

| PayloadKind | C# Type |
|-------------|---------|
| `None` | (no data) |
| `Byte` | `byte` |
| `UInt16` | `ushort` |
| `UInt32` | `uint` |
| `UInt32_UInt32` | `(uint, uint)` |
| `UInt32_Int32` | `(uint, int)` |
| `String` | `string` |
| `Reference` | any class |
| `BoxedValue` | any struct (boxed fallback) |

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

## License

This project is provided under the MIT License.

## Contributing

Contributions are welcome! Please open an issue or pull request on GitHub.
