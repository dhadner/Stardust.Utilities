# BitField Source Generator

Type-safe, high-performance bitfield manipulation for hardware register emulation.

## Table of Contents

**Getting Started**
- [Quick Start](#quick-start)
- [Attributes](#attributes)
- [Supported Storage Types](#supported-storage-types)

**Usage**
- [Parsing](#parsing)
- [Formatting](#formatting)
- [Shift-and-Mask Pattern](#shift-and-mask-pattern)

**Features**
- [Signed Property Types (Sign Extension)](#signed-property-types-sign-extension)
- [BitFields Composition](#bitfields-composition)
- [Undefined Bits Control](#partial-width-embedding-and-undefined-bits)

**Examples**
- [Hardware Registers (8/16/32/64-bit)](#examples)
- [IEEE 754 Floating-Point Decomposition](#example-ieee-754-floating-point-decomposition)
- [.NET Decimal Decomposition](#example-net-decimal-decomposition)
- [Network Protocol Headers](#example-network-protocol-headers)

**API Reference**
- [Static Bit and Mask Properties](#static-bit-properties)
- [Fluent With{Name} Methods](#fluent-withname-methods-v080)
- [Operators](#operators-and-static-properties)
- [Interface Implementations](#interface-implementations)

**Appendix**
- [Generated Code Listing](#generated-code)
- [Performance](#performance)

---

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

| Storage | Bits | Notes |
|---------|------|-------|
| `byte` | 8 | Signed alternative: `sbyte` |
| `ushort` | 16 | Signed alternative: `short` |
| `uint` | 32 | Signed alternative: `int` |
| `ulong` | 64 | Signed alternative: `long` |
| `float` | 32 | IEEE 754 single-precision; backed by `uint`, user-facing type is `float` |
| `double` | 64 | IEEE 754 double-precision; backed by `ulong`, user-facing type is `double` |
| `decimal` | 128 | .NET decimal (96-bit coefficient + scale + sign); full decimal arithmetic |
| `UInt128` | 128 | 128-bit unsigned with conversion operators |
| `Int128` | 128 | 128-bit signed with conversion operators |
| `[BitFields(N)]` | N | Arbitrary width from 1 to 16,384 bits |

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

**Supported Formats (integer storage types):**
- Decimal: `"255"`, `"1234"`, `"1_000_000"`
- Hexadecimal with prefix: `"0xFF"`, `"0XFF"`, `"0x1234_ABCD"`
- Binary with prefix: `"0b1010"`, `"0B1111_0000"`

### Floating-Point and Decimal Storage Types

For `float`, `double`, and `decimal` storage types, parsing uses the native parser
instead of integer hex/binary parsing. Values are parsed as their respective numeric
types:

```csharp
[BitFields(typeof(double))]
public partial struct IEEE754Double { /* ... */ }

// Parse as a floating-point number (delegates to double.Parse)
IEEE754Double pi = IEEE754Double.Parse("3.14159265358979", CultureInfo.InvariantCulture);

// Scientific notation is supported (via double.Parse)
IEEE754Double avogadro = IEEE754Double.Parse("6.022E23", CultureInfo.InvariantCulture);

// TryParse works the same way
IEEE754Double.TryParse("42.5", out var val);   // true
IEEE754Double.TryParse("0xFF", out _);          // false (hex not supported for float/double/decimal)

// float storage type works identically
[BitFields(typeof(float))]
public partial struct FloatReg { /* ... */ }

FloatReg f = FloatReg.Parse("3.14", CultureInfo.InvariantCulture);

// decimal storage type uses decimal.Parse
[BitFields(typeof(decimal))]
public partial struct DecimalReg { /* ... */ }

DecimalReg d = DecimalReg.Parse("3.14", CultureInfo.InvariantCulture);
```

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

For `float`, `double`, and `decimal` storage types, formatting uses the native
formatter. The default `ToString()` returns the numeric representation (not the raw bits):

```csharp
IEEE754Double pi = Math.PI;
pi.ToString();                                   // "3.141592653589793"
pi.ToString("F2", CultureInfo.InvariantCulture); // "3.14"
pi.ToString("E", CultureInfo.InvariantCulture);  // "3.141593E+000"

DecimalReg price = 19.99m;
price.ToString();                                // "19.99"
price.ToString("C", CultureInfo.InvariantCulture); // "¤19.99"
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

### Example: IEEE 754 Floating-Point Decomposition

This example demonstrates how `[BitFields(typeof(double))]` can decompose IEEE 754 floating-point values into their constituent bit fields. Because the struct is `partial`, you can add your own computed properties alongside the generated bit-field accessors.

```csharp
[BitFields(typeof(double))]
public partial struct IEEE754Double
{
    // ── Generated bit-field properties ──────────────────────────────
    [BitField(0, 51)]  public partial ulong  Mantissa { get; set; } // 52-bit fractional
    [BitField(52, 62)] public partial ushort Exponent { get; set; } // 11-bit biased exponent
    [BitFlag(63)]      public partial bool   Sign     { get; set; } // sign bit

    // ── User-defined computed properties (not generated) ───────────

    /// <summary>True if the value is NaN (exponent = all 1s, mantissa ≠ 0).</summary>
    public bool IsNaN => Exponent == 0x7FF && Mantissa != 0;

    /// <summary>True if the value is ±Infinity (exponent = all 1s, mantissa = 0).</summary>
    public bool IsInfinity => Exponent == 0x7FF && Mantissa == 0;

    /// <summary>True if the value is a denormalized number (exponent = 0, mantissa ≠ 0).</summary>
    public bool IsDenormalized => Exponent == 0 && Mantissa != 0;

    /// <summary>True if the value is a normalized number (0 &lt; exponent &lt; 0x7FF).</summary>
    public bool IsNormal => Exponent > 0 && Exponent < 0x7FF;

    /// <summary>True if the value is ±0 (exponent = 0, mantissa = 0).</summary>
    public bool IsZero => Exponent == 0 && Mantissa == 0;

    /// <summary>The unbiased exponent (biased − 1023), or null for non-normal values (zero, denormalized, infinity, NaN).</summary>
    public int? UnbiasedExponent => IsNormal ? Exponent - 1023 : null;
}
```

**IEEE 754 double-precision layout:**

```
Bit:  63 | 62 ─── 52 | 51 ──────────────────── 0
      S  | Exponent   | Mantissa (fractional)
      1  |   11 bits  |       52 bits
```

Value = (-1)^S × 2^(Exponent - 1023) × (1 + Mantissa / 2^52)

**Construct values from their bit-level components:**

```csharp
// Build π from its IEEE 754 fields
IEEE754Double pi = default;
pi.Sign = false;
pi.Exponent = 1024;                // bias 1023 + 1, since 2 ≤ π < 4
pi.Mantissa = 0x921FB54442D18;     // the fractional bits of π
double result = pi;                // result == Math.PI ✓

// Construct special values
IEEE754Double inf = default;
inf.Exponent = 0x7FF;
inf.IsInfinity;                     // true — exponent all 1s, mantissa 0
inf.IsNaN;                          // false

IEEE754Double nan = default;
nan.Exponent = 0x7FF;
nan.Mantissa = 1;                   // any non-zero mantissa
nan.IsNaN;                          // true

// Smallest positive value: denormalized
IEEE754Double tiny = double.Epsilon;
tiny.IsDenormalized;                // true — exponent 0, mantissa ≠ 0
tiny.IsNormal;                      // false
tiny.UnbiasedExponent;              // null (not a normal number)

// Normal numbers
IEEE754Double pi2 = Math.PI;
pi2.IsNormal;                       // true
pi2.UnbiasedExponent;               // 1 (2^1 range, since 2 ≤ π < 4)

// Negate by flipping just the sign bit
IEEE754Double val = 42.0;
val.Sign = !val.Sign;              // val == -42.0 ✓
```

**Decompose and inspect any double:**

```csharp
IEEE754Double e = Math.E;           // 2.71828...
e.Sign;                             // false (positive)
e.Exponent;                         // 1024 (2^1 range, since 2 ≤ e < 4)
e.UnbiasedExponent;                 // 1

// Powers of 2 always have zero mantissa
IEEE754Double eight = 8.0;
eight.UnbiasedExponent;             // 3 (i.e. 2^3)
eight.Mantissa;                     // 0 (exact power of 2)

// Classify any value
IEEE754Double zero = 0.0;
zero.IsZero;                        // true
zero.IsNormal;                      // false
zero.UnbiasedExponent;              // null
```

**Full floating-point arithmetic works through the generated operators:**

```csharp
// Compute the golden ratio: φ = (1 + √5) / 2
IEEE754Double one = 1.0;
IEEE754Double sqrt5 = Math.Sqrt(5.0);
IEEE754Double two = 2.0;
IEEE754Double phi = (one + sqrt5) / two;    // ≈ 1.618033988749895

// Solve the quadratic x² - 5x + 6 = 0  →  x = 3, 2
IEEE754Double a = 1.0, b = -5.0, c = 6.0;
IEEE754Double disc = b * b - 4.0 * a * c;  // discriminant = 1.0
IEEE754Double x1 = (-b + Math.Sqrt(disc)) / (two * a);  // x1 = 3.0
IEEE754Double x2 = (-b - Math.Sqrt(disc)) / (two * a);  // x2 = 2.0

// Fluent construction
var onePointFive = IEEE754Double.Zero
    .WithExponent(1023)
    .WithMantissa(0x8_0000_0000_0000);       // 1.5 ✓
```

### Example: .NET Decimal Decomposition

`[BitFields(typeof(decimal))]` decomposes .NET's 128-bit decimal representation into its constituent fields. The bit layout follows `decimal.GetBits()` canonical order:

```
Bits:  127 | 126-119 | 118-112 | 111-96   | 95 ────────────────── 0
       Sign| Reserved| Scale   | Reserved | 96-bit unsigned coefficient
       1   | 8 bits  | 7 bits  | 16 bits  |       96 bits
```

Value = (-1)^Sign × Coefficient / 10^Scale

```csharp
[BitFields(typeof(decimal))]
public partial struct DecimalParts
{
    [BitField(0, 95)]   public partial UInt128 Coefficient { get; set; } // 96-bit integer
    [BitField(112, 118)] public partial byte   Scale       { get; set; } // 0-28
    [BitFlag(127)]       public partial bool   Sign        { get; set; } // sign bit
}
```

**Inspect decimal values:**

```csharp
DecimalParts price = 19.99m;
price.Sign;                   // false (positive)
price.Scale;                  // 2 (divided by 10^2)
price.Coefficient;            // 1999

DecimalParts big = decimal.MaxValue;
big.Scale;                    // 0
big.Coefficient;              // 79228162514264337593543950335

// Negate by flipping the sign bit
DecimalParts val = 42m;
val.Sign = !val.Sign;         // val == -42m ✓
```

**Full decimal arithmetic works through the generated operators:**

```csharp
DecimalParts a = 10.5m;
DecimalParts b = 3m;
decimal sum  = a + b;         // 13.5m
decimal prod = a * b;         // 31.5m
decimal quot = a / b;         // 3.5m
```

### Example: Network Protocol Headers

This example models real Ethernet/IPv4/TCP packet headers using BitFields composition.
Small flag structs (3-bit IPv4 flags, 9-bit TCP flags) are embedded into the 32-bit
header words that contain them.

```csharp
// 3-bit IPv4 flags — embedded into a 3-bit field of the fragment word
[BitFields(typeof(byte), UndefinedBitsMustBe.Zeroes)]
public partial struct IPv4Flags
{
    [BitFlag(0)] public partial bool MoreFragments { get; set; } // MF
    [BitFlag(1)] public partial bool DontFragment { get; set; }  // DF
    [BitFlag(2)] public partial bool Reserved { get; set; }      // must be 0
}

// 9-bit TCP control flags — embedded into a 9-bit field of the control word
[BitFields(typeof(ushort), UndefinedBitsMustBe.Zeroes)]
public partial struct TcpFlags
{
    [BitFlag(0)] public partial bool FIN { get; set; } // finish
    [BitFlag(1)] public partial bool SYN { get; set; } // synchronize
    [BitFlag(2)] public partial bool RST { get; set; } // reset
    [BitFlag(3)] public partial bool PSH { get; set; } // push
    [BitFlag(4)] public partial bool ACK { get; set; } // acknowledge
    [BitFlag(5)] public partial bool URG { get; set; } // urgent
    [BitFlag(6)] public partial bool ECE { get; set; } // ECN-Echo
    [BitFlag(7)] public partial bool CWR { get; set; } // congestion window reduced
    [BitFlag(8)] public partial bool NS { get; set; }  // ECN-nonce
}

// IPv4 header word 0: Version(4) | IHL(4) | DSCP(6) | ECN(2) | TotalLength(16)
[BitFields(typeof(uint))]
public partial struct IPv4VersionWord
{
    [BitField(28, 31)] public partial byte Version { get; set; }
    [BitField(24, 27)] public partial byte IHL { get; set; }
    [BitField(18, 23)] public partial byte DSCP { get; set; }
    [BitField(16, 17)] public partial byte ECN { get; set; }
    [BitField(0, 15)]  public partial ushort TotalLength { get; set; }
}

// IPv4 fragment word — embeds IPv4Flags in a 3-bit field
[BitFields(typeof(uint))]
public partial struct IPv4FragmentWord
{
    [BitField(16, 31)] public partial ushort Identification { get; set; }
    [BitField(13, 15)] public partial IPv4Flags Flags { get; set; }       // ← composed!
    [BitField(0, 12)]  public partial ushort FragmentOffset { get; set; }
}

// TCP control word — embeds TcpFlags in a 9-bit field
[BitFields(typeof(uint))]
public partial struct TcpControlWord
{
    [BitField(28, 31)] public partial byte DataOffset { get; set; }
    [BitField(25, 27)] public partial byte Reserved { get; set; }
    [BitField(16, 24)] public partial TcpFlags Flags { get; set; }        // ← composed!
    [BitField(0, 15)]  public partial ushort WindowSize { get; set; }
}
```

**Assemble an Ethernet/IPv4/TCP packet carrying "Hello world!":**

```csharp
byte[] payload = Encoding.ASCII.GetBytes("Hello world!");  // 12 bytes

// TCP: push data, acknowledge previous segment
var tcp = TcpControlWord.Zero
    .WithDataOffset(5)                                      // 20-byte header
    .WithFlags(TcpFlags.Zero.WithPSH(true).WithACK(true))
    .WithWindowSize(65535);

// IPv4: version 4, 20-byte header, total = IP + TCP + payload
var ip = IPv4VersionWord.Zero
    .WithVersion(4)
    .WithIHL(5)
    .WithTotalLength((ushort)(20 + 20 + payload.Length));    // 52 bytes

// Don't fragment this packet
var frag = IPv4FragmentWord.Zero
    .WithIdentification(0x1A2B)
    .WithFlags(IPv4Flags.Zero.WithDontFragment(true));

// Read through composition
tcp.Flags.PSH;                       // true
tcp.Flags.ACK;                       // true
frag.Flags.DontFragment;             // true
ip.TotalLength;                      // 52

// Total frame: 14 (Ethernet) + 52 (IP total) = 66 bytes

// TCP three-way handshake using static Bit properties
var syn    = TcpFlags.SYNBit;                       // SYN
var synAck = TcpFlags.SYNBit | TcpFlags.ACKBit;     // SYN+ACK
var ack    = TcpFlags.ACKBit;                        // ACK
```

**Bit packing is verifiable:**

```csharp
// Version=4 (0100) at bits 28-31, IHL=5 (0101) at bits 24-27
var word = IPv4VersionWord.Zero.WithVersion(4).WithIHL(5);
((uint)word) == 0x4500_0000;          // true ✓
```

**Why this works:** `IPv4Flags` (3 bits in a `byte`) and `TcpFlags` (9 bits in a `ushort`) are
full BitFields types with their own operators and `With{Name}` methods. When embedded in a
wider struct's field, the generated implicit conversions handle the packing and unpacking
automatically. `UndefinedBitsMustBe.Zeroes` ensures clean serialization — only the defined
flag bits survive.

### BitFields Composition

BitFields structs can be used as property types within other BitFields structs. This enables reusable sub-structures for protocols, file headers, and complex hardware registers.

```csharp
// Reusable 8-bit status flags structure
[BitFields(typeof(byte))]
public partial struct StatusFlags
{
    [BitFlag(0)] public partial bool Ready { get; set; }
    [BitFlag(1)] public partial bool Error { get; set; }
    [BitFlag(2)] public partial bool Busy { get; set; }
    [BitFlag(3)] public partial bool Complete { get; set; }
    [BitField(4, 7)] public partial byte Priority { get; set; }
}

// 16-bit protocol header that embeds StatusFlags
[BitFields(typeof(ushort))]
public partial struct ProtocolHeader
{
    [BitField(0, 7)] public partial StatusFlags Status { get; set; }   // Embedded BitFields!
    [BitField(8, 15)] public partial byte Length { get; set; }
}
```

**Usage:**

```csharp
ProtocolHeader header = 0;

// Create and set embedded flags
StatusFlags status = 0;
status.Ready = true;
status.Priority = 5;
header.Status = status;
header.Length = 64;

// Read embedded struct properties (chained access)
bool isReady = header.Status.Ready;      // true
byte priority = header.Status.Priority;  // 5

// Round-trip through raw value
ushort raw = header;    // 0x4031 (Length=0x40, Status=0x31)
header = raw;           // Restore from raw value
```

**How It Works:**

Composition leverages implicit conversions. The generated getter casts to the property type, and the setter casts from it:

```csharp
public partial StatusFlags Status
{
    get => (StatusFlags)(Value & 0x00FF);           // Implicit ushort -> StatusFlags
    set => Value = (ushort)((Value & 0xFF00) | ((ushort)value & 0x00FF));  // Implicit StatusFlags -> ushort
}
```

**Use Cases:**
- **Network protocols**: Reusable header fields, flags, and type codes
- **File formats**: Magic numbers, version info, and metadata structures  
- **Hardware registers**: Common flag patterns shared across multiple registers
- **Layered structures**: Building complex formats from simpler building blocks

### Partial-Width Embedding and Undefined Bits

When a BitFields struct doesn't define fields covering all bits of its storage type, those undefined bits have specific behaviors controlled by the `UndefinedBitsMustBe` parameter:

```csharp
// 9-bit sub-header - undefined bits always zero
[BitFields(typeof(ushort), UndefinedBitsMustBe.Zeroes)]
public partial struct SubHeader9
{
    [BitField(0, 3)] public partial byte TypeCode { get; set; }  // bits 0-3
    [BitField(4, 8)] public partial byte Flags { get; set; }     // bits 4-8
    // Bits 9-15: UNDEFINED - always zero
}

// 27-bit header - undefined bits always zero
[BitFields(typeof(uint), UndefinedBitsMustBe.Zeroes)]
public partial struct Header27
{
    [BitField(0, 8)] public partial SubHeader9 SubHeader { get; set; }  // 9 bits
    [BitField(9, 18)] public partial ushort PayloadSize { get; set; }   // 10 bits
    [BitField(19, 26)] public partial byte Sequence { get; set; }       // 8 bits
    // Bits 27-31: UNDEFINED - always zero
}

// Hardware register - undefined bits preserved as-is (default)
[BitFields(typeof(uint))]  // UndefinedBitsMustBe.Any is the default
public partial struct HardwareReg
{
    [BitField(0, 7)] public partial byte Control { get; set; }
    // Bits 8-31: May have hardware meaning, preserved as-is
}
```

**Mode Behaviors:**

| Mode | Undefined Bits | Use Case |
|------|----------------|----------|
| `UndefinedBitsMustBe.Any` (default) | Preserved as raw data | Hardware registers where undefined bits may be meaningful |
| `UndefinedBitsMustBe.Zeroes` | Always masked to zero | Protocol headers, clean serialization |
| `UndefinedBitsMustBe.Ones` | Always set to one | Protocols requiring 1s in reserved regions |

**Sparse Undefined Bits:**

Undefined bits don't have to be contiguous. The generator correctly handles sparse patterns:

```csharp
// Bits 0, 3, and 7 are undefined (gaps between defined bits)
[BitFields(typeof(sbyte), UndefinedBitsMustBe.Zeroes)]
public partial struct SparseReg
{
    // bit 0: UNDEFINED
    [BitField(1, 2)] public partial byte LowField { get; set; }   // bits 1-2
    // bit 3: UNDEFINED  
    [BitField(4, 6)] public partial byte HighField { get; set; }  // bits 4-6
    // bit 7: UNDEFINED
}

SparseReg reg = unchecked((sbyte)-1);  // Try to set all bits (0xFF)
sbyte raw = reg;                        // raw == 0x76 (only defined bits set)

// With UndefinedBitsMustBe.Zeroes:
// - OR with 0x08 doesn't set bit 3 (undefined)
// - Adding 1 doesn't set bit 0 (undefined)
// - Arithmetic results are always masked through the constructor
```

**Example:**

```csharp
// UndefinedBitsMustBe.Zeroes - undefined bits are zeroed
SubHeader9 sub = 0xFFFF;  // Try to set all 16 bits
ushort raw = sub;         // raw == 0x01FF (only 9 defined bits)

// UndefinedBitsMustBe.Any (default) - undefined bits preserved  
SubHeader9Native nat = 0xFFFF;
ushort rawNat = nat;      // rawNat == 0xFFFF (all bits preserved)

// UndefinedBitsMustBe.Ones - undefined bits set to 1
SubHeader9Ones ones = 0x0000;
ushort rawOnes = ones;    // rawOnes == 0xFE00 (undefined bits are 1)
```

**Consistent Embedding:**

With `UndefinedBitsMustBe.Zeroes`, embedded structs behave identically whether standalone or nested:

```csharp
SubHeader9 sub = 0xFFFF;           // Bits 9-15 masked off → 0x01FF
Header27 header = 0;
header.SubHeader = sub;            // Same 9 bits embedded
SubHeader9 extracted = header.SubHeader;
((ushort)extracted) == ((ushort)sub)  // true - identical behavior
```

**Implementation Note:**

The `UndefinedBitsMustBe` mode is applied in the **constructor**, ensuring all operations (implicit conversions, operators, parsing) consistently handle undefined bits.

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
