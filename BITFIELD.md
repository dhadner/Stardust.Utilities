# BitField Source Generator

Type-safe, high-performance bitfield manipulation for hardware register emulation.

## Quick Start

```csharp
[BitFields(typeof(ushort))]
public partial struct MyRegister
{
    [BitField(0, 6)] public partial byte KeyCode { get; set; }       // bits 0..=6 (7 bits)
    [BitFlag(7)] public partial bool KeyUp { get; set; }
    [BitField(8, 14)] public partial byte SecondKey { get; set; }    // bits 8..=14 (7 bits)
    [BitFlag(15)] public partial bool SecondKeyUp { get; set; }
}

// Usage - use implicit conversion or constructor
MyRegister reg = 0xFFFF;          // Implicit conversion from ushort
var reg2 = new MyRegister(0x1234); // Constructor
reg.KeyUp = false;
ushort raw = reg;                 // Implicit conversion to ushort
```

The generator creates:
- A private `Value` field of the specified storage type
- A constructor taking the storage type
- Property implementations with inline bit manipulation
- Static `{Name}Bit` properties for each `[BitFlag]`
- Static `{Name}Mask` properties for each `[BitField]`
- Fluent `Set{Name}` methods for each property
- Bitwise operators: `|`, `&`, `^`, `~`
- Mixed-type operators (struct with storage type)
- Equality operators: `==`, `!=`
- Implicit conversion operators to/from the storage type

## Attributes

| Attribute | Usage | Description |
|-----------|-------|-------------|
| `[BitFields(typeof(T))]` | Struct | Enables generation with storage type T |
| `[BitField(startBit, endBit)]` | Property | Multi-bit field (Rust-style inclusive range) |
| `[BitFlag(bit)]` | Property | Single-bit flag definition |

**BitField Examples:**
- `[BitField(0, 2)]` - 3-bit field at bits 0, 1, 2 (like Rust's `0..=2`)
- `[BitField(4, 7)]` - 4-bit field at bits 4, 5, 6, 7 (like Rust's `4..=7`)
- `[BitField(3, 3)]` - 1-bit field at bit 3 only

## Supported Storage Types

| Storage | Bits | Signed Alternative |
|---------|------|-------------------|
| `byte` | 8 | `sbyte` |
| `ushort` | 16 | `short` |
| `uint` | 32 | `int` |
| `ulong` | 64 | `long` |

## Performance

Benchmarks show the generated code performs within **1%** of hand-coded bit manipulation. All bit manipulation uses **compile-time constants** for zero runtime overhead.

## Examples

### VIA Register (8-bit)

```csharp
[BitFields(typeof(byte))]
public partial struct ViaRegB
{
    [BitField(0, 2)] public partial byte SoundVolume { get; set; }  // bits 0..=2 (3 bits)
    [BitFlag(3)] public partial bool SoundBuffer { get; set; }
    [BitFlag(4)] public partial bool OverlayRom { get; set; }
    [BitFlag(5)] public partial bool HeadSelect { get; set; }
    [BitFlag(6)] public partial bool VideoPage { get; set; }
    [BitFlag(7)] public partial bool SccAccess { get; set; }
}
```

### ADB Keyboard Register (16-bit)

```csharp
[BitFields(typeof(ushort))]
public partial struct KeyboardReg0
{
    [BitField(0, 6)] public partial byte SecondKeyCode { get; set; }  // bits 0..=6 (7 bits)
    [BitFlag(7)] public partial bool SecondKeyUp { get; set; }
    [BitField(8, 14)] public partial byte FirstKeyCode { get; set; }  // bits 8..=14 (7 bits)
    [BitFlag(15)] public partial bool FirstKeyUp { get; set; }
}
```

### 64-bit Status Register

```csharp
[BitFields(typeof(ulong))]
public partial struct StatusReg64
{
    [BitField(0, 7)] public partial byte Status { get; set; }       // bits 0..=7 (8 bits)
    [BitField(8, 23)] public partial ushort DataWord { get; set; }  // bits 8..=23 (16 bits)
    [BitField(24, 55)] public partial uint Address { get; set; }    // bits 24..=55 (32 bits)
    [BitFlag(56)] public partial bool Enable { get; set; }
    [BitFlag(57)] public partial bool Ready { get; set; }
    [BitFlag(58)] public partial bool Error { get; set; }
}
```

### Signed Storage Type (32-bit)

For hardware registers that use signed values:

```csharp
[BitFields(typeof(int))]
public partial struct SignedReg32
{
    [BitFlag(0)] public partial bool Flag0 { get; set; }
    [BitFlag(31)] public partial bool Sign { get; set; }  // Sign bit
    [BitField(1, 15)] public partial ushort LowWord { get; set; }   // bits 1..=15 (15 bits)
    [BitField(16, 30)] public partial ushort HighWord { get; set; } // bits 16..=30 (15 bits)
}
```

## Generated Code

For `[BitFields(typeof(byte))]`, the generator creates:

```csharp
// User writes:
[BitFields(typeof(byte))]
public partial struct StatusRegister
{
    [BitFlag(0)] public partial bool Ready { get; set; }
    [BitField(2, 4)] public partial byte Mode { get; set; }  // bits 2..=4 (3 bits)
}

// Generator creates:
public partial struct StatusRegister
{
    private byte Value;

    /// <summary>Creates a new StatusRegister with the specified raw value.</summary>
    public StatusRegister(byte value) { Value = value; }

    public partial bool Ready
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (Value & 0x01) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Value = value ? (byte)(Value | 0x01) : (byte)(Value & 0xFE);
    }

    public partial byte Mode
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (byte)((Value >> 2) & 0x07);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Value = (byte)((Value & 0xE3) | (((byte)value << 2) & 0x1C));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator byte(StatusRegister value) => value.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StatusRegister(byte value) => new(value);
}
```

## Migration from Previous API

If you were using the user-declared Value field pattern:

```csharp
// Old pattern (no longer supported):
[BitFields]
public partial struct MyReg
{
    public byte Value;  // User declares this
    [BitFlag(0)] public partial bool Flag { get; set; }
}

// New pattern:
[BitFields(typeof(byte))]
public partial struct MyReg
{
    [BitFlag(0)] public partial bool Flag { get; set; }
}

// Old usage:
var reg = new MyReg { Value = 0xFF };
byte raw = reg.Value;

// New usage:
MyReg reg = 0xFF;        // Implicit conversion
byte raw = reg;          // Implicit conversion
var reg2 = new MyReg(0xFF);  // Constructor
```

## Operators and Static Properties (v0.7.0+)

### Static Bit Properties

For each `[BitFlag]` property, a static `{Name}Bit` property is generated that returns a struct with only that bit set:

```csharp
// Instead of:
IFRFields bit = 0;
bit.CA1_Vbl = true;
ClearInterrupt(bit);

// Use the static Bit property:
ClearInterrupt(IFRFields.CA1_VblBit);

// Combine multiple flags:
var flags = IFRFields.CA1_VblBit | IFRFields.CA2_RtcBit;
```

### Static Mask Properties

For each `[BitField]` property, a static `{Name}Mask` property is generated that returns the mask for that field:

```csharp
// Clear a multi-bit field:
var reg = ~RegAFields.SoundMask & someValue;

// Check if any bits in a field are set:
if ((value & RegAFields.SoundMask) != 0) { ... }
```

### Bitwise Operators

Full support for bitwise operations without casting:


```csharp
IFRFields a = 0x01;
IFRFields b = 0x02;

// Binary operators (return struct type)
var or = a | b;           // Bitwise OR
var and = a & b;          // Bitwise AND
var xor = a ^ b;          // Bitwise XOR
var inv = ~a;             // Bitwise complement

// Mixed-type operators
var mixed = a | (byte)0x80;
var mixed2 = (byte)0x40 | b;

// Complex expressions
var result = IFR & ~IFRFields.EnableBit;  // Clear a flag
```

### Equality Operators

```csharp
IFRFields a = 0x42;
IFRFields b = 0x42;

if (a == b) { ... }     // Equality
if (a != b) { ... }     // Inequality
a.Equals(b);            // Object equality
a.GetHashCode();        // Hash code
a.ToString();           // Returns "0x42"
```

## Fluent Set{Name} Methods (v0.8.0+)

### The Struct-as-Property Problem

When a BitFields struct is exposed as a property, direct setter calls don't work:

```csharp
public class MyClass
{
    public IFRFields IFR { get; set; }
}

// ? This modifies a COPY, not the original!
obj.IFR.Ready = true;  // Compiles but doesn't work
```

This is a fundamental C# behavior: property getters return a copy of the struct.

### Solution: Set{Name} Methods

For each property, a `Set{Name}` method is generated that returns a new struct:

```csharp
// ? This works correctly
obj.IFR = obj.IFR.SetReady(true);

// ? Chain multiple changes
obj.IFR = obj.IFR.SetReady(true).SetError(false).SetMode(5);
```

### Examples

```csharp
// BitFlags
IFRFields flags = 0;
flags = flags.SetReady(true);           // Set flag
flags = flags.SetReady(false);          // Clear flag

// BitFields (multi-bit)
RegAFields reg = 0;
reg = reg.SetSound(5);                  // Set 3-bit field to 5
reg = reg.SetPriority(2);               // Set 2-bit field to 2

// Chaining
var result = reg
    .SetSound(7)
    .SetPage2(true)
    .SetDriveSel(false);

// With properties
container.Status = container.Status.SetReady(true).SetMode(3);
```

### Alternative: Bitwise Operators

For simple flag operations, bitwise operators also work with properties:

```csharp
// Set a flag
obj.IFR = obj.IFR | IFRFields.ReadyBit;

// Clear a flag  
obj.IFR = obj.IFR & ~IFRFields.ReadyBit;

// Toggle a flag
obj.IFR = obj.IFR ^ IFRFields.ReadyBit;
```

### Summary: Choosing the Right Approach

| Scenario | Recommended Approach |
|----------|---------------------|
| Struct is a local variable | Direct setter: `reg.Ready = true` |
| Struct is a property | Set method: `obj.IFR = obj.IFR.SetReady(true)` |
| Setting multiple values | Chain: `reg.SetA(1).SetB(2).SetC(true)` |
| Simple flag set/clear | Bitwise: `obj.IFR = obj.IFR \| IFRFields.ReadyBit` |
