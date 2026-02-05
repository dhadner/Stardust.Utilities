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
- Fluent `With{Name}` methods for each property
- Arithmetic operators: `+`, `-`, `*`, `/`, `%`, unary `-`
- Bitwise operators: `|`, `&`, `^`, `~`
- Shift operators: `<<`, `>>`, `>>>` (return `int` for small types)
- Comparison operators: `<`, `<=`, `>`, `>=`
- Equality operators: `==`, `!=`
- Mixed-type operators (for `int`, `uint`, `long`, `ulong` storage types)
- Implicit conversion operators to/from the storage type
- Parsing support via `IParsable<T>` and `ISpanParsable<T>` interfaces
- Formatting support via `IFormattable` and `ISpanFormattable` interfaces
- Comparison interfaces: `IComparable`, `IComparable<T>`, `IEquatable<T>`

## Shift-and-Mask Pattern

For small storage types (`byte`, `sbyte`, `short`, `ushort`), shift operators return `int` to enable intuitive bit manipulation with integer literals:

```csharp
[BitFields(typeof(byte))]
public partial struct MyReg { /* ... */ }

MyReg bits = 0b0000_1110;

// This works intuitively because (bits >> 1) returns int
int lsb = (bits >> 1) & 1;  // Gets bit 1: result = 1
int bit2 = (bits >> 2) & 1; // Gets bit 2: result = 1
int bit0 = bits & 1;        // Gets bit 0: result = 0 (uses implicit conversion)

// You can also assign the result back to a BitFields type
// (implicit conversion from int truncates to storage type)
MyReg extracted = (bits >> 1) & 0x07;  // Implicit int -> MyReg
```

For larger storage types (`int`, `uint`, `long`, `ulong`), shift operators return the BitFields type.

### Implicit Conversions

Small BitFields types support implicit conversion from `int`:

```csharp
MyReg reg = 42;                    // Implicit int -> MyReg (truncates)
MyReg mask = (bits >> 4) & 0x0F;   // Result of int expression assigned to MyReg
```

This is intentional because BitFields represent hardware registers where truncation is expected.

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

## Parsing

BitFields structs implement `IParsable<T>` and `ISpanParsable<T>` interfaces, allowing parsing from strings in multiple formats:

```csharp
// Decimal parsing
MyRegister dec = MyRegister.Parse("255");

// Hexadecimal parsing (0x or 0X prefix)
MyRegister hex = MyRegister.Parse("0xFF");
MyRegister hex2 = MyRegister.Parse("0XAB");

// Binary parsing (0b or 0B prefix)
MyRegister bin = MyRegister.Parse("0b11111111");
MyRegister bin2 = MyRegister.Parse("0B10101010");

// C#-style underscore digit separators (all formats)
MyRegister d1 = MyRegister.Parse("1_000");           // Decimal: 1000
MyRegister h1 = MyRegister.Parse("0xFF_00");         // Hex: 0xFF00  
MyRegister b1 = MyRegister.Parse("0b1111_0000");     // Binary: 0xF0

// TryParse pattern for safe parsing
if (MyRegister.TryParse("0b1010_1010", out var result))
{
    Console.WriteLine($"Parsed: {result}");  // 0xAA
}

// With IFormatProvider for culture-specific parsing
var reg3 = MyRegister.Parse("42", CultureInfo.InvariantCulture);

// ReadOnlySpan<char> overloads for performance
var span = "0xFF".AsSpan();
var reg4 = MyRegister.Parse(span, null);
```

**Supported Formats:**
- Decimal: `"255"`, `"1234"`, `"1_000_000"`
- Hexadecimal with prefix: `"0xFF"`, `"0XFF"`, `"0x1234_ABCD"`
- Binary with prefix: `"0b1010"`, `"0B1111_0000"`

## Formatting

BitFields structs implement `IFormattable` and `ISpanFormattable` interfaces:

```csharp
MyRegister value = 0xAB;

// Standard format strings
string hex = value.ToString("X2", null);    // "AB"
string dec = value.ToString("D", null);     // "171"

// Default ToString returns hex
string str = value.ToString();              // "0xAB"

// Allocation-free formatting with Span<char>
Span<char> buffer = stackalloc char[10];
if (value.TryFormat(buffer, out int written, "X4", null))
{
    // buffer[..written] contains "00AB"
}
```

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

### Signed Property Types (Sign Extension)

When a property type is signed (`sbyte`, `short`, `int`, `long`), the generator automatically sign-extends the extracted field value. This is essential for hardware registers where fields represent signed quantities like deltas, offsets, or two's complement values.

**The property type determines sign extension, not the storage type.**

```csharp
[BitFields(typeof(ushort))]
public partial struct MotionRegister
{
    // 3-bit signed delta at bits 13-15. Values: -4 to +3 (two's complement)
    [BitField(13, 15)] public partial sbyte DeltaX { get; set; }
    
    // 3-bit unsigned field at bits 10-12. Values: 0 to 7
    [BitField(10, 12)] public partial byte UnsignedField { get; set; }
    
    // 4-bit signed nibble at bits 6-9. Values: -8 to +7
    [BitField(6, 9)] public partial sbyte DeltaY { get; set; }
    
    // 6-bit signed offset at bits 0-5. Values: -32 to +31
    [BitField(0, 5)] public partial sbyte Offset { get; set; }
}
```

**Usage:**

```csharp
MotionRegister reg = 0;

// Setting negative values works correctly
reg.DeltaX = -3;
Console.WriteLine(reg.DeltaX);  // Output: -3

// The 3-bit field stores the two's complement representation
// Binary: 101 (unsigned 5) = -3 (signed 3-bit)
reg.DeltaX = -1;
Console.WriteLine(reg.DeltaX);  // Output: -1

// Positive values in range also work
reg.DeltaX = 3;
Console.WriteLine(reg.DeltaX);  // Output: 3

// Unsigned fields do NOT sign extend
reg.UnsignedField = 5;
Console.WriteLine(reg.UnsignedField);  // Output: 5 (stays positive)
```

**Generated Code for Signed Properties:**

For a 3-bit signed field at bits 13-15, the generator produces:

```csharp
public partial sbyte DeltaX
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => (sbyte)(((int)(Value & 0xE000) << 16) >> 29);
    //              ↑ mask in place   ↑ shift MSB to bit 31   ↑ arithmetic right shift
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => Value = (ushort)((Value & 0x1FFF) | ((((ushort)value) << 13) & 0xE000));
}
```

The getter uses an optimized mask-then-shift pattern:
1. **Mask in place**: `Value & 0xE000` extracts bits 13-15 without shifting first
2. **Left shift**: `<< 16` positions the field's MSB at bit 31 (the int sign bit)
3. **Arithmetic right shift**: `>> 29` propagates the sign bit down to bits 0-2

This produces correct two's complement sign extension with only 3 operations (mask, shift, shift) plus the final cast.

**Sign Extension by Field Width:**

| Field Width | Signed Range | Example |
|-------------|--------------|---------|
| 3 bits | -4 to +3 | `sbyte DeltaX` |
| 4 bits | -8 to +7 | `sbyte DeltaY` |
| 6 bits | -32 to +31 | `sbyte Offset` |
| 8 bits | -128 to +127 | `sbyte SignedByte` (no sign extension needed) |
| 16 bits | -32768 to +32767 | `short SignedWord` (no sign extension needed) |

**Zero Overhead for Unsigned Properties:**

Unsigned property types (`byte`, `ushort`, `uint`, `ulong`) use the standard mask-and-shift pattern with no additional sign extension overhead.

## Generated Code

For the following user-defined struct:

```csharp
[BitFields(typeof(byte))]
public partial struct StatusRegister
{
    [BitFlag(0)] public partial bool Ready { get; set; }
    [BitFlag(1)] public partial bool Error { get; set; }
    [BitField(2, 4)] public partial byte Mode { get; set; }  // bits 2..=4 (3 bits)
}
```

The generator creates the following complete implementation:

```csharp
public partial struct StatusRegister
{
    // ═══════════════════════════════════════════════════════════════════
    // Storage
    // ═══════════════════════════════════════════════════════════════════
    
    private byte Value;

    /// <summary>Creates a new StatusRegister with the specified raw value.</summary>
    public StatusRegister(byte value) { Value = value; }

    // ═══════════════════════════════════════════════════════════════════
    // BitFlag Properties (single-bit)
    // ═══════════════════════════════════════════════════════════════════

    public partial bool Ready
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (Value & 0x01) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Value = value ? (byte)(Value | 0x01) : (byte)(Value & 0xFE);
    }

    public partial bool Error
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (Value & 0x02) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Value = value ? (byte)(Value | 0x02) : (byte)(Value & 0xFD);
    }

    // ═══════════════════════════════════════════════════════════════════
    // BitField Properties (multi-bit)
    // ═══════════════════════════════════════════════════════════════════

    public partial byte Mode
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (byte)((Value >> 2) & 0x07);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Value = (byte)((Value & 0xE3) | ((value << 2) & 0x1C));
    }

    // ═══════════════════════════════════════════════════════════════════
    // Static Bit Properties (for BitFlags)
    // Returns a struct with only the specified bit set
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Returns a StatusRegister with only the Ready bit set.</summary>
    public static StatusRegister ReadyBit => new(0x01);

    /// <summary>Returns a StatusRegister with only the Error bit set.</summary>
    public static StatusRegister ErrorBit => new(0x02);

    // ═══════════════════════════════════════════════════════════════════
    // Static Mask Properties (for BitFields)
    // Returns a struct with the mask for the specified field
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Returns a StatusRegister with the mask for the Mode field (bits 2-4).</summary>
    public static StatusRegister ModeMask => new(0x1C);

    // ═══════════════════════════════════════════════════════════════════
    // Fluent With{Name} Methods
    // Returns a new struct with the specified value changed
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Returns a new StatusRegister with the Ready flag set to the specified value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StatusRegister WithReady(bool value) => 
        new(value ? (byte)(Value | 0x01) : (byte)(Value & 0xFE));

    /// <summary>Returns a new StatusRegister with the Error flag set to the specified value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StatusRegister WithError(bool value) => 
        new(value ? (byte)(Value | 0x02) : (byte)(Value & 0xFD));

    /// <summary>Returns a new StatusRegister with the Mode field set to the specified value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StatusRegister WithMode(byte value) => 
        new((byte)((Value & 0xE3) | ((value << 2) & 0x1C)));

    // ═══════════════════════════════════════════════════════════════════
    // Bitwise Operators
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Bitwise complement operator.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StatusRegister operator ~(StatusRegister a) => new((byte)~a.Value);

    /// <summary>Bitwise OR operator.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StatusRegister operator |(StatusRegister a, StatusRegister b) => 
        new((byte)(a.Value | b.Value));

    /// <summary>Bitwise AND operator.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StatusRegister operator &(StatusRegister a, StatusRegister b) => 
        new((byte)(a.Value & b.Value));

    /// <summary>Bitwise XOR operator.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StatusRegister operator ^(StatusRegister a, StatusRegister b) => 
        new((byte)(a.Value ^ b.Value));

    // ═══════════════════════════════════════════════════════════════════
    // Mixed-Type Bitwise Operators
    // Note: For small types (byte, sbyte, short, ushort), these are NOT
    // generated because shift returns int, enabling native int operators.
    // For larger types (int, uint, long, ulong), these ARE generated.
    // ═══════════════════════════════════════════════════════════════════

    // For small types, use the shift-then-mask pattern:
    //   int bit = (bits >> n) & 1;    // Works because shift returns int
    //
    // For int/uint/long/ulong storage types, the following operators are generated:

    // ═══════════════════════════════════════════════════════════════════
    // Arithmetic Operators
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Addition operator.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StatusRegister operator +(StatusRegister a, StatusRegister b) => 
        new(unchecked((byte)(a.Value + b.Value)));

    /// <summary>Subtraction operator.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StatusRegister operator -(StatusRegister a, StatusRegister b) => 
        new(unchecked((byte)(a.Value - b.Value)));

    /// <summary>Unary negation operator (two's complement).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StatusRegister operator -(StatusRegister a) => 
        new(unchecked((byte)(0 - a.Value)));

    // ... plus *, /, % and mixed-type overloads

    // ═══════════════════════════════════════════════════════════════════
    // Shift Operators
    // For small types (byte, sbyte, short, ushort), returns int to allow
    // intuitive use like: (bits >> n) & 1
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Left shift operator. Returns int for intuitive bitwise operations.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int operator <<(StatusRegister a, int b) => a.Value << b;

    /// <summary>Right shift operator. Returns int for intuitive bitwise operations.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int operator >>(StatusRegister a, int b) => a.Value >> b;

    /// <summary>Unsigned right shift operator. Returns int for intuitive bitwise operations.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int operator >>>(StatusRegister a, int b) => a.Value >>> b;

    // ═══════════════════════════════════════════════════════════════════
    // Comparison Operators
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Less than operator.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(StatusRegister a, StatusRegister b) => a.Value < b.Value;

    /// <summary>Greater than operator.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(StatusRegister a, StatusRegister b) => a.Value > b.Value;

    /// <summary>Less than or equal operator.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(StatusRegister a, StatusRegister b) => a.Value <= b.Value;

    /// <summary>Greater than or equal operator.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(StatusRegister a, StatusRegister b) => a.Value >= b.Value;

    // ═══════════════════════════════════════════════════════════════════
    // Equality Operators
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Equality operator.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(StatusRegister a, StatusRegister b) => a.Value == b.Value;

    /// <summary>Inequality operator.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(StatusRegister a, StatusRegister b) => a.Value != b.Value;

    /// <summary>Determines whether this instance equals another object.</summary>
    public override bool Equals(object? obj) => obj is StatusRegister other && Value == other.Value;

    /// <summary>Returns the hash code for this instance.</summary>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>Returns the hexadecimal string representation of the value.</summary>
    public override string ToString() => $"0x{Value:X2}";

    // ═══════════════════════════════════════════════════════════════════
    // Implicit Conversion Operators
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Implicit conversion to storage type.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator byte(StatusRegister value) => value.Value;

    /// <summary>Implicit conversion from storage type.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StatusRegister(byte value) => new(value);

    // ═══════════════════════════════════════════════════════════════════
    // Interface Implementations (abbreviated)
    // ═══════════════════════════════════════════════════════════════════

    // IComparable<T>
    public int CompareTo(StatusRegister other) => Value.CompareTo(other.Value);

    // IEquatable<T>  
    public bool Equals(StatusRegister other) => Value == other.Value;

    // IFormattable
    public string ToString(string? format, IFormatProvider? formatProvider) => 
        Value.ToString(format, formatProvider);

    // ISpanFormattable
    public bool TryFormat(Span<char> destination, out int charsWritten, 
        ReadOnlySpan<char> format, IFormatProvider? provider) => 
        Value.TryFormat(destination, out charsWritten, format, provider);

    // IParsable<T> and ISpanParsable<T> - Parse/TryParse methods
    // (supports decimal, 0x hex, 0b binary, with underscores)
}
```


## Operators and Static Properties

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

### Arithmetic Operators

Full support for arithmetic operations:

```csharp
MyRegister a = 100;
MyRegister b = 50;

// Binary arithmetic operators
var sum = a + b;           // Addition: 150
var diff = a - b;          // Subtraction: 50
var prod = a * 2;          // Multiplication (with storage type)
var quot = a / b;          // Division: 2
var rem = a % 3;           // Modulus

// Unary operators
var pos = +a;              // Unary plus (returns same value)
var neg = -a;              // Unary negation (two's complement)

// Mixed-type operations (struct with storage type)
var mixed = a + (byte)10;
var mixed2 = (byte)5 + b;
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

### Shift Operators

```csharp
MyRegister a = 0x0F;

var shl = a << 4;         // Left shift: 0xF0
var shr = a >> 2;         // Right shift: 0x03
var ushr = a >>> 1;       // Unsigned right shift (C# 11+)
```

### Comparison Operators

```csharp
MyRegister a = 100;
MyRegister b = 200;

bool lt = a < b;          // Less than: true
bool le = a <= b;         // Less than or equal: true
bool gt = a > b;          // Greater than: false
bool ge = a >= b;         // Greater than or equal: false

// Useful for bounds checking
if (reg < MyRegister.ModeMask) { ... }
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

## Interface Implementations

Every BitFields type automatically implements the following interfaces:

| Interface | Purpose |
|-----------|---------|
| `IComparable` | Non-generic comparison for sorting |
| `IComparable<T>` | Generic comparison |
| `IEquatable<T>` | Value equality |
| `IFormattable` | Format string support (`ToString("X2", null)`) |
| `ISpanFormattable` | Allocation-free formatting |
| `IParsable<T>` | String parsing |
| `ISpanParsable<T>` | Span-based parsing |

```csharp
// IComparable - sorting
var registers = new[] { reg3, reg1, reg2 };
Array.Sort(registers);  // Sorts by underlying value

// IEquatable<T> - efficient equality
bool equal = reg1.Equals(reg2);

// IComparable<T> - comparison
int cmp = reg1.CompareTo(reg2);  // -1, 0, or 1
```

## Fluent With{Name} Methods (v0.8.0+)

### The Struct-as-Property Problem

When a BitFields struct is exposed as a property, direct setter calls don't work:

```csharp
public class MyClass
{
    public IFRFields IFR { get; set; }
}

// This modifies a COPY, not the original!
obj.IFR.Ready = true;  // Compiles but doesn't work
```

This is a fundamental C# behavior: property getters return a copy of the struct.

### Solution: With{Name} Methods

For each property, a `With{Name}` method is generated that returns a new struct and does NOT modify the value of the current struct:

```csharp
// This works correctly
obj.IFR = obj.IFR.WithReady(true);

// Chain multiple changes
obj.IFR = obj.IFR.WithReady(true).WithError(false).WithMode(5);
```

### Examples

```csharp
// BitFlags
IFRFields flags = 0;
flags = flags.WithReady(true);           // Set flag
flags = flags.WithReady(false);          // Clear flag

// BitFields (multi-bit)
RegAFields reg = 0;
reg = reg.WithSound(5);                  // Set 3-bit field to 5
reg = reg.WithPriority(2);               // Set 2-bit field to 2

// Chaining
var result = reg
    .WithSound(7)
    .WithPage2(true)
    .WithDriveSel(false);

// With properties
container.Status = container.Status.WithReady(true).WithMode(3);
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
| Struct is a property | With method: `obj.IFR = obj.IFR.WithReady(true)` |
| Setting multiple values | Chain: `reg = reg.WithStatus(1).WithMode(2).WithReady(true)` |
|                         | Bitwise chaining: `reg = (reg | IFRFields.ReadyBit) & ~IFRFields.ErrorBit` |
| Simple flag set/clear | Bitwise: `obj.IFR = obj.IFR | IFRFields.ReadyBit` |
