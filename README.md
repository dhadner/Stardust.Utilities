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

#### Basic EnhancedEnum

```csharp
using Stardust.Utilities;

// A debugger command where each kind has different associated data
public enum DebugCommandKind 
{ 
    Step,           // No data needed
    StepOver,       // No data needed  
    SetBreakpoint,  // Needs: (address, hitCount)
    ClearBreakpoint,// Needs: address
    Evaluate,       // Needs: expression string
    ReadMemory      // Needs: (address, length)
}

public sealed record DebugCommand : EnhancedEnum<DebugCommandKind>
{
    static DebugCommand()
    {
        // Register the payload type for each kind
        RegisterKindType(DebugCommandKind.Step, null);  // No payload
        RegisterKindType(DebugCommandKind.StepOver, null);
        RegisterKindType(DebugCommandKind.SetBreakpoint, typeof((uint address, int hitCount)));
        RegisterKindType(DebugCommandKind.ClearBreakpoint, typeof(uint));
        RegisterKindType(DebugCommandKind.Evaluate, typeof(string));
        RegisterKindType(DebugCommandKind.ReadMemory, typeof((uint address, int length)));
    }

    public DebugCommand(DebugCommandKind kind, object? value = null) : base(kind, value) { }
    
    // Convenience factory methods for type safety
    public static DebugCommand Step() => new(DebugCommandKind.Step);
    public static DebugCommand SetBreakpoint(uint address, int hitCount = 1) 
        => new(DebugCommandKind.SetBreakpoint, (address, hitCount));
    public static DebugCommand Evaluate(string expr) 
        => new(DebugCommandKind.Evaluate, expr);
    public static DebugCommand ReadMemory(uint address, int length) 
        => new(DebugCommandKind.ReadMemory, (address, length));
}
```

#### Pattern Matching with Different Types

The real power shows in switch expressions where each case extracts its specific payload type:

```csharp
string ExecuteCommand(DebugCommand cmd)
{
    return cmd switch
    {
        // Cases with no payload
        (DebugCommandKind.Step, _) => 
            StepOne(),
            
        (DebugCommandKind.StepOver, _) => 
            StepOverCall(),
        
        // Tuple payload: extract address and hitCount
        (DebugCommandKind.SetBreakpoint, (uint addr, int hits)) => 
            $"Breakpoint set at 0x{addr:X8}, will trigger after {hits} hit(s)",
        
        // Simple uint payload
        (DebugCommandKind.ClearBreakpoint, uint addr) => 
            $"Breakpoint cleared at 0x{addr:X8}",
        
        // String payload
        (DebugCommandKind.Evaluate, string expr) => 
            $"Result: {EvaluateExpression(expr)}",
        
        // Another tuple with different types
        (DebugCommandKind.ReadMemory, (uint addr, int len)) => 
            $"Read {len} bytes from 0x{addr:X8}: {FormatMemory(addr, len)}",
        
        _ => "Unknown command"
    };
}

// Usage
var cmd1 = DebugCommand.SetBreakpoint(0x00401000, hitCount: 3);
var cmd2 = DebugCommand.Evaluate("registers.PC + 4");
var cmd3 = DebugCommand.ReadMemory(0x00000100, length: 16);

Console.WriteLine(ExecuteCommand(cmd1));  // "Breakpoint set at 0x00401000..."
Console.WriteLine(ExecuteCommand(cmd2));  // "Result: ..."
Console.WriteLine(ExecuteCommand(cmd3));  // "Read 16 bytes from 0x00000100..."
```

#### Type-Safe Extraction

```csharp
var cmd = DebugCommand.SetBreakpoint(0x00401000, 5);

// Safe extraction with TryValueAs
if (cmd.TryValueAs(out (uint address, int hitCount) bp))
{
    Console.WriteLine($"Address: 0x{bp.address:X8}, Hits: {bp.hitCount}");
}

// Or use Is() for combined kind check + extraction
if (cmd.Is(DebugCommandKind.SetBreakpoint, out (uint addr, int hits) breakpoint))
{
    // Only enters if kind matches AND extraction succeeds
}
```

---

#### EnhancedEnumFlex (Zero-Allocation Hot Path)

`EnhancedEnum<T>` boxes value types when storing them. For most code this is fine, but in hot paths (like an emulator's instruction loop running millions of times per second), boxing creates GC pressure.

`EnhancedEnumFlex<T>` solves this by storing common value types inline without allocation.

**When to use EnhancedEnumFlex:**
- Message passing in tight loops
- Event systems with high throughput
- Emulator/interpreter dispatch loops
- Any scenario where you're creating thousands of instances per frame

```csharp
using Stardust.Utilities;

// Example: CPU interrupt system - called millions of times per second
public enum InterruptKind
{
    None,       // No interrupt pending
    VBlank,     // Vertical blank (no data)
    Timer,      // Timer expired (no data)
    DMA,        // DMA complete: channel number (byte)
    IRQ,        // Hardware IRQ: vector number (byte)  
    Exception,  // CPU exception: (vector, address) tuple
}

// Register payload kinds ONCE at startup (typically in a static constructor)
static class InterruptSystem
{
    static InterruptSystem()
    {
        var PK = EnhancedEnumFlex<InterruptKind>.PayloadKind;
        
        EnhancedEnumFlex<InterruptKind>.RegisterKindPayloadKind(InterruptKind.None, PK.None);
        EnhancedEnumFlex<InterruptKind>.RegisterKindPayloadKind(InterruptKind.VBlank, PK.None);
        EnhancedEnumFlex<InterruptKind>.RegisterKindPayloadKind(InterruptKind.Timer, PK.None);
        EnhancedEnumFlex<InterruptKind>.RegisterKindPayloadKind(InterruptKind.DMA, PK.Byte);
        EnhancedEnumFlex<InterruptKind>.RegisterKindPayloadKind(InterruptKind.IRQ, PK.Byte);
        EnhancedEnumFlex<InterruptKind>.RegisterKindPayloadKind(InterruptKind.Exception, PK.UInt32_UInt32);
    }
    
    public static void Initialize() { } // Call to trigger static constructor
}

// Creating interrupts - NO HEAP ALLOCATION
public static class Interrupt
{
    public static EnhancedEnumFlex<InterruptKind> None() 
        => EnhancedEnumFlex<InterruptKind>.Create(InterruptKind.None);
    
    public static EnhancedEnumFlex<InterruptKind> VBlank() 
        => EnhancedEnumFlex<InterruptKind>.Create(InterruptKind.VBlank);
    
    public static EnhancedEnumFlex<InterruptKind> DMA(byte channel) 
        => EnhancedEnumFlex<InterruptKind>.Create(InterruptKind.DMA, channel);
    
    public static EnhancedEnumFlex<InterruptKind> IRQ(byte vector) 
        => EnhancedEnumFlex<InterruptKind>.Create(InterruptKind.IRQ, vector);
    
    public static EnhancedEnumFlex<InterruptKind> Exception(uint vector, uint address) 
        => EnhancedEnumFlex<InterruptKind>.Create(InterruptKind.Exception, (vector, address));
}

// Processing interrupts - also zero allocation
void HandleInterrupt(EnhancedEnumFlex<InterruptKind> interrupt)
{
    if (interrupt.Is(InterruptKind.None))
        return;
    
    if (interrupt.Is(InterruptKind.VBlank))
    {
        RefreshScreen();
        return;
    }
    
    if (interrupt.TryGetByte(InterruptKind.DMA, out byte channel))
    {
        CompleteDMATransfer(channel);
        return;
    }
    
    if (interrupt.TryGetByte(InterruptKind.IRQ, out byte vector))
    {
        ProcessIRQ(vector);
        return;
    }
    
    if (interrupt.TryGetUInt32UInt32(InterruptKind.Exception, out var ex))
    {
        HandleException(ex.a, ex.b);  // vector, address
        return;
    }
}

// In your hot loop - no allocations!
while (running)
{
    ExecuteInstruction();
    
    var interrupt = CheckForInterrupts();  // Returns EnhancedEnumFlex
    HandleInterrupt(interrupt);            // Zero allocation dispatch
}
```

**Supported inline payload types (no boxing):**
| PayloadKind | C# Type | Size |
|-------------|---------|------|
| `None` | (no data) | 0 |
| `Byte` | `byte` | 1 |
| `UInt16` | `ushort` | 2 |
| `UInt32` | `uint` | 4 |
| `UInt32_UInt32` | `(uint, uint)` | 8 |
| `UInt32_Int32` | `(uint, int)` | 8 |
| `String` | `string` | ref |
| `Reference` | any class | ref |
| `BoxedValue` | any struct | ref (boxed) |

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
