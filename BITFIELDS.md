# BitFields

Source-generated, type-safe, zero-overhead bit manipulation for .NET.

Two attributes, one concept: define bit-level fields with `[BitField]` and `[BitFlag]`, then choose your backing strategy.

| Attribute | Backing | Best for |
|-----------|---------|----------|
| `[BitFields]` | Value type (`byte`, `ushort`, `uint`, ...) | Hardware registers, opcodes, small bit-packed structs |
| `[BitFieldsView]` | `Memory<byte>` (zero-copy buffer view) | Network packets, file headers, DMA buffers |

Both share the same `[BitField(start, end)]` and `[BitFlag(bit)]` attributes. Learn one, use both.

## Table of Contents

**Getting Started**
- [Quick Start -- BitFields (Value Type)](#quick-start----bitfields-value-type)
- [Quick Start -- BitFieldsView (Buffer View)](#quick-start----bitfieldsview-buffer-view)
- [Choosing Between BitFields and BitFieldsView](#choosing-between-bitfields-and-bitfieldsview)

**Shared Concepts**
- [Attributes](#attributes)
- [Byte Order and Bit Order](#byte-order-and-bit-order)
- [Signed Property Types (Sign Extension)](#signed-property-types-sign-extension)

**BitFields (Value Type)**
- [Supported Storage Types](#supported-storage-types)
- [Operators](#operators)
- [Parsing and Formatting](#parsing-and-formatting)
- [Static Bit and Mask Properties](#static-bit-and-mask-properties)
- [Fluent With Methods](#fluent-with-methods)
- [Undefined and Reserved Bits](#undefined-and-reserved-bits)
- [Interface Implementations](#interface-implementations)

**BitFieldsView (Buffer View)**
- [Constructors](#constructors)
- [Zero-Copy Semantics](#zero-copy-semantics)
- [Generated Members](#generated-members)
- [Record Struct Equality](#record-struct-equality)

**Composition**
- [BitFields Inside BitFields](#bitfields-inside-bitfields)
- [BitFields Inside BitFieldsView](#bitfields-inside-bitfieldsview)
- [Sub-View Nesting](#sub-view-nesting)
- [Mixed-Endian Nesting](#mixed-endian-nesting)

**Real-World Examples**
- [Hardware Registers](#hardware-registers)
- [IEEE 754 Floating-Point Decomposition](#ieee-754-floating-point-decomposition)
- [.NET Decimal Decomposition](#net-decimal-decomposition)
- [Network Protocol Headers (RFC)](#network-protocol-headers-rfc)
- [Parsing a Captured Network Packet](#parsing-a-captured-network-packet)
- [Mixed-Endian Capture File](#mixed-endian-capture-file)

**Reference**
- [Performance](#performance)
- [Generated Code Listing](#generated-code-listing)

---

## Quick Start -- BitFields (Value Type)

```csharp
[BitFields(typeof(ushort))]
public partial struct KeyboardReg
{
    [BitField(0, 6)]  public partial byte KeyCode { get; set; }   // bits 0..=6 (7 bits)
    [BitFlag(7)]      public partial bool KeyUp { get; set; }
    [BitField(8, 14)] public partial byte SecondKey { get; set; } // bits 8..=14 (7 bits)
    [BitFlag(15)]     public partial bool SecondKeyUp { get; set; }
}

KeyboardReg reg = 0xFFFF;       // implicit conversion from ushort
reg.KeyUp = false;
ushort raw = reg;               // implicit conversion to ushort
```

`[BitFields]` generates a value type with inline bit manipulation, full operator support,
parsing, formatting, and implicit conversions. Zero heap allocations, zero abstraction penalty.

## Quick Start -- BitFieldsView (Buffer View)

```csharp
[BitFieldsView(ByteOrder.BigEndian, BitOrder.BitZeroIsMsb)]
public partial record struct IPv4HeaderView
{
    [BitField(0, 3)]     public partial byte Version { get; set; }
    [BitField(4, 7)]     public partial byte Ihl { get; set; }
    [BitField(16, 31)]   public partial ushort TotalLength { get; set; }
    [BitField(96, 127)]  public partial uint SourceAddress { get; set; }
    [BitField(128, 159)] public partial uint DestinationAddress { get; set; }
}

byte[] packet = ReceiveFromNetwork();
var header = new IPv4HeaderView(packet);
byte version = header.Version;     // reads directly from packet buffer
header.TotalLength = 52;           // writes directly to packet buffer
```

`[BitFieldsView]` generates a `record struct` that wraps `Memory<byte>`. All reads and writes go
directly through the buffer -- no copies, no allocations. The struct-level `ByteOrder` controls
how multi-byte fields are serialized; plain `ushort` and `uint` properties are all you need.

## Choosing Between BitFields and BitFieldsView

| | `[BitFields]` | `[BitFieldsView]` |
|---|---|---|
| Backing | Private value field | `Memory<byte>` (external buffer) |
| Copy cost | Copies all data on assignment | Copies only the 24-byte view header |
| Max size | ~16 KB | Unlimited |
| Operators | Full arithmetic, bitwise, comparison | None (it is a view, not a value) |
| Conversions | Implicit to/from storage type | Constructor from `byte[]` / `Memory<byte>` |
| Use case | Registers, opcodes, flags | Network packets, file formats, DMA buffers |

**Use `[BitFields]`** when the data is a small, self-contained value -- hardware registers,
instruction opcodes, status flags, or anything that fits in a primitive and benefits from
operators and implicit conversions.

**Use `[BitFieldsView]`** when the data lives in an external buffer and you want zero-copy
access -- network packets, memory-mapped file headers, DMA buffers.

**Use both together** when a protocol has small reusable flag groups embedded in larger
buffer-backed headers. See [Composition](#bitfields-inside-bitfieldsview).

---

## Attributes

Both `[BitFields]` and `[BitFieldsView]` use the same field attributes:

| Attribute | Parameters | Description |
|-----------|------------|-------------|
| `[BitFields(typeof(T))]` | Storage type, optional `UndefinedBitsMustBe`, optional `BitOrder` | Marks a `partial struct` for value-type generation |
| `[BitFieldsView]` | Optional `ByteOrder`, optional `BitOrder` | Marks a `partial record struct` for buffer-view generation |
| `[BitField(startBit, endBit)]` | Rust-style inclusive range, optional `MustBe` | Multi-bit field (width = endBit - startBit + 1) |
| `[BitFlag(bit)]` | 0-based bit position, optional `MustBe` | Single-bit boolean flag |

**BitField range examples:**
- `[BitField(0, 2)]` -- 3-bit field at bits 0, 1, 2 (like Rust's `0..=2`)
- `[BitField(4, 7)]` -- 4-bit field at bits 4, 5, 6, 7
- `[BitField(3, 3)]` -- 1-bit field at bit 3 only

## Byte Order and Bit Order

Both attributes support configurable byte order and bit numbering:

| Setting | Default | Meaning | Typical use |
|---------|---------|---------|-------------|
| `ByteOrder.LittleEndian` | Yes | LSB first in memory | x86 registers, USB, PCI, PE files |
| `ByteOrder.BigEndian` | No | MSB first in memory | Network protocols, Java class files |
| `ByteOrder.NetworkEndian` | No | Synonym for `BigEndian` | Readability in protocol code |
| `BitOrder.BitZeroIsLsb` | Yes | Bit 0 = least significant | Hardware datasheets, x86 convention |
| `BitOrder.BitZeroIsMsb` | No | Bit 0 = most significant | RFCs, IETF specifications |

For `[BitFields]`, only `BitOrder` applies (the value is stored in a native integer).
For `[BitFieldsView]`, both `ByteOrder` and `BitOrder` apply.

**LSB-first bit layout** (default):
```
Byte 0:  bit7  bit6  bit5  bit4  bit3  bit2  bit1  bit0
         0x80  0x40  0x20  0x10  0x08  0x04  0x02  0x01
```

**MSB-first bit layout** (RFC convention):
```
Byte 0:  bit0  bit1  bit2  bit3  bit4  bit5  bit6  bit7
         0x80  0x40  0x20  0x10  0x08  0x04  0x02  0x01
```

With MSB-first, bit positions in `[BitField]` attributes match RFC diagrams directly:

```csharp
// RFC 791 IPv4 header, first 32 bits:
//  0                   1                   2                   3
//  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
// |Version|  IHL  |Type of Service|          Total Length         |
// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

[BitFieldsView(ByteOrder.BigEndian, BitOrder.BitZeroIsMsb)]
public partial record struct IPv4Word0
{
    [BitField(0, 3)]   public partial byte Version { get; set; }      // matches RFC diagram
    [BitField(4, 7)]   public partial byte Ihl { get; set; }
    [BitField(8, 15)]  public partial byte TypeOfService { get; set; }
    [BitField(16, 31)] public partial ushort TotalLength { get; set; }
}
```

## Signed Property Types (Sign Extension)

When a property type is signed (`sbyte`, `short`, `int`, `long`), the generator automatically
sign-extends the extracted field value. The **property type** determines sign extension, not the
storage type:

```csharp
[BitFields(typeof(ushort))]
public partial struct MotionRegister
{
    [BitField(13, 15)] public partial sbyte DeltaX { get; set; }  // 3-bit signed: -4 to +3
    [BitField(10, 12)] public partial byte Speed { get; set; }    // 3-bit unsigned: 0 to 7
}

MotionRegister reg = 0;
reg.DeltaX = -3;
Console.WriteLine(reg.DeltaX);  // -3 (correctly sign-extended)
reg.Speed = 5;
Console.WriteLine(reg.Speed);   // 5 (unsigned, stays positive)
```

| Field Width | Signed Range | Unsigned Range |
|-------------|--------------|----------------|
| 3 bits | -4 to +3 | 0 to 7 |
| 4 bits | -8 to +7 | 0 to 15 |
| 8 bits | -128 to +127 | 0 to 255 |

---

## Supported Storage Types

`[BitFields]` supports these storage types:

| Storage Type | Size | Notes |
|--------------|------|-------|
| `byte` / `sbyte` | 8 bits | |
| `ushort` / `short` | 16 bits | |
| `uint` / `int` | 32 bits | |
| `ulong` / `long` | 64 bits | |
| `UInt128` / `Int128` | 128 bits | |
| `Half` | 16 bits | IEEE 754 half-precision |
| `float` | 32 bits | IEEE 754 single-precision |
| `double` | 64 bits | IEEE 754 double-precision |
| `decimal` | 128 bits | .NET decimal |
| `[BitFields(N)]` | N bits | Arbitrary width, 1 to 16,384 bits |

## Enum Property Types

Property types can be enums for self-documenting code with zero runtime overhead:

```csharp
public enum OpMode : byte { Idle = 0, Run = 1, Sleep = 2, Reset = 3 }

[BitFields(typeof(byte))]
public partial struct ControlRegister
{
    [BitField(0, 1)] public partial OpMode Mode { get; set; }
    [BitFlag(2)]     public partial bool Enable { get; set; }
}

ControlRegister reg = 0;
reg.Mode = OpMode.Run;
OpMode mode = reg.Mode;  // OpMode.Run
```

## Operators

`[BitFields]` types are full-featured numeric types:

```csharp
StatusRegister a = 0x42;
StatusRegister b = 0x18;

// Arithmetic
var sum = a + b;       var diff = a - b;      var neg = -a;

// Bitwise
var or = a | b;        var and = a & b;       var not = ~a;

// Shift (returns int for small types, enabling: (bits >> n) & 1)
int bit = (a >> 2) & 1;

// Comparison and equality
bool lt = a < b;       bool eq = a == b;
```

For small types (`byte`, `ushort`), shift operators return `int` for intuitive bit extraction.
For larger types (`int`, `uint`, `long`, `ulong`), shift operators return the BitFields type.

### Shift-and-Mask Pattern

The `int` return type for small-type shifts enables the classic bit-extraction idiom:

```csharp
[BitFields(typeof(byte))]
public partial struct MyReg { /* ... */ }

MyReg bits = 0b0000_1110;

int lsb  = (bits >> 1) & 1;   // Gets bit 1: result = 1
int bit2 = (bits >> 2) & 1;   // Gets bit 2: result = 1
int bit0 = bits & 1;          // Gets bit 0: result = 0

// Assign the result back (implicit int -> MyReg truncates to storage type)
MyReg extracted = (bits >> 1) & 0x07;
```

## Parsing and Formatting

```csharp
// Parse from multiple formats
StatusRegister dec = StatusRegister.Parse("255");
StatusRegister hex = StatusRegister.Parse("0xFF");
StatusRegister bin = StatusRegister.Parse("0b1111_0000");

// Format
StatusRegister value = 0xAB;
value.ToString();              // "0xAB"
value.ToString("X2", null);    // "AB"

// Allocation-free
Span<char> buf = stackalloc char[10];
value.TryFormat(buf, out int written, "X4", null);
```

For `Half`, `float`, `double`, and `decimal` storage types, parsing and formatting
use the native numeric formatters instead of integer hex/binary parsing.

## Static Bit and Mask Properties

```csharp
// For [BitFlag(0)] Ready -- a value with only that bit set
StatusRegister readyBit = StatusRegister.ReadyBit;   // 0x01

// For [BitField(2, 4)] Mode -- the mask covering that field
StatusRegister modeMask = StatusRegister.ModeMask;    // 0x1C

// Use for testing and masking
if ((status & StatusRegister.ReadyBit) != 0) { /* ready */ }
var flags = StatusRegister.ReadyBit | StatusRegister.ErrorBit;
```

## Fluent With Methods

Solve the struct-as-property problem with immutable-style updates:

```csharp
// Direct setters work on local variables
StatusRegister reg = 0;
reg.Ready = true;

// But not on properties (C# returns a copy)
// obj.Status.Ready = true;  // compiles but doesn't work

// Use With methods instead
obj.Status = obj.Status.WithReady(true).WithMode(5);
```

## Undefined and Reserved Bits

Control how bits not covered by any field are handled:

```csharp
[BitFields(typeof(ushort), UndefinedBitsMustBe.Zeroes)]
public partial struct CleanHeader
{
    [BitField(0, 3)] public partial byte TypeCode { get; set; }
    [BitField(4, 8)] public partial byte Flags { get; set; }
    // Bits 9-15: always forced to zero
}

CleanHeader h = 0xFFFF;
ushort raw = h;           // 0x01FF (undefined bits masked off)
```

| Mode | Behavior | Use case |
|------|----------|----------|
| `.Any` (default) | Preserved as-is | Hardware registers |
| `.Zeroes` | Masked to zero | Protocol headers, serialization |
| `.Ones` | Set to one | Reserved-high protocols |

Individual fields can also override with `MustBe`:

```csharp
[BitFlag(7, MustBe.One)] public partial bool Sync { get; set; }       // always 1
[BitField(1, 3, MustBe.Zero)] public partial byte Reserved { get; set; } // always 0
```

Sparse undefined bits (gaps between fields) are handled correctly:

```csharp
[BitFields(typeof(sbyte), UndefinedBitsMustBe.Zeroes)]
public partial struct SparseReg
{
    // bit 0: UNDEFINED
    [BitField(1, 2)] public partial byte LowField { get; set; }
    // bit 3: UNDEFINED
    [BitField(4, 6)] public partial byte HighField { get; set; }
    // bit 7: UNDEFINED
}

SparseReg reg = unchecked((sbyte)-1);  // try to set all bits
sbyte raw = reg;                        // 0x76 (only defined bits survive)
```

## Nested Structs

BitFields structs can be nested inside classes or other structs. Containing types must be `partial`:

```csharp
public partial class HardwareController
{
    [BitFields(typeof(ushort))]
    public partial struct StatusRegister
    {
        [BitFlag(0)] public partial bool Ready { get; set; }
        [BitField(8, 15)] public partial byte ErrorCode { get; set; }
    }

    private StatusRegister _status;
    public bool IsReady => _status.Ready;
}
```

## Interface Implementations

Every `[BitFields]` type implements:

| Interface | Purpose |
|-----------|---------|
| `IComparable`, `IComparable<T>` | Sorting and comparison |
| `IEquatable<T>` | Value equality |
| `IFormattable`, `ISpanFormattable` | Format string support |
| `IParsable<T>`, `ISpanParsable<T>` | String and span parsing |

---

## Constructors

`[BitFieldsView]` generates three constructors:

```csharp
var view = new IPv4HeaderView(buffer.AsMemory());  // from Memory<byte>
var view = new IPv4HeaderView(packetBytes);        // from byte[]
var view = new IPv4HeaderView(frameBytes, 14);     // from byte[] with offset
```

All constructors validate that the buffer contains at least `SizeInBytes` bytes.

## Zero-Copy Semantics

The view holds a reference to the original buffer. All property access goes through that reference:

```csharp
byte[] buffer = new byte[20];
var view = new IPv4HeaderView(buffer);

view.Version = 4;
Console.WriteLine(buffer[0]);    // 0x40 -- buffer modified directly

buffer[0] = 0x60;
Console.WriteLine(view.Version); // 6 -- view reads live data
```

Two views over the same buffer see each other's writes:

```csharp
var v1 = new IPv4HeaderView(buffer);
var v2 = new IPv4HeaderView(buffer);
v1.Version = 4;
Console.WriteLine(v2.Version);   // 4
```

## Generated Members

For each `[BitFieldsView]` struct, the generator produces:

| Member | Description |
|--------|-------------|
| `_data` | Private `Memory<byte>` field |
| Constructors | `Memory<byte>`, `byte[]`, `byte[] + offset` |
| `Data` property | Exposes the underlying `Memory<byte>` |
| `SizeInBytes` constant | Minimum buffer size required |
| Property accessors | Inline `BinaryPrimitives` reads/writes with `AggressiveInlining` |

## Record Struct Equality

Two views are equal if they reference the same segment of the same array:

```csharp
var v1 = new IPv4HeaderView(buffer);
var v2 = new IPv4HeaderView(buffer);
Console.WriteLine(v1 == v2);  // True

var v3 = new IPv4HeaderView(new byte[20]);
Console.WriteLine(v1 == v3);  // False -- different arrays
```

---

## BitFields Inside BitFields

BitFields types can be used as property types within other BitFields, enabling reusable sub-structures:

```csharp
[BitFields(typeof(byte))]
public partial struct StatusFlags
{
    [BitFlag(0)] public partial bool Ready { get; set; }
    [BitFlag(1)] public partial bool Error { get; set; }
    [BitField(4, 7)] public partial byte Priority { get; set; }
}

[BitFields(typeof(ushort))]
public partial struct ProtocolHeader
{
    [BitField(0, 7)]  public partial StatusFlags Status { get; set; }  // embedded!
    [BitField(8, 15)] public partial byte Length { get; set; }
}

ProtocolHeader header = 0;
header.Status = new StatusFlags { Ready = true, Priority = 5 };
bool ready = header.Status.Ready;  // true
```

## BitFields Inside BitFieldsView

`[BitFields]` types work as property types inside `[BitFieldsView]`. The implicit conversions
handle packing and unpacking automatically:

```csharp
[BitFields(typeof(byte))]
public partial struct StatusFlags
{
    [BitFlag(0)] public partial bool Active { get; set; }
    [BitFlag(1)] public partial bool Valid { get; set; }
    [BitField(4, 7)] public partial byte Code { get; set; }
}

[BitFieldsView]
public partial record struct PacketView
{
    [BitField(0, 7)]  public partial StatusFlags Flags { get; set; }
    [BitField(8, 15)] public partial byte Payload { get; set; }
}

byte[] buffer = new byte[2];
var view = new PacketView(buffer);
view.Flags = new StatusFlags { Active = true, Code = 5 };
Console.WriteLine(view.Flags.Active); // True
Console.WriteLine(view.Flags.Code);   // 5
```

## Sub-View Nesting

`[BitFieldsView]` types can be nested inside other `[BitFieldsView]` types. The inner view
operates on the **same underlying buffer** at the specified offset -- zero-copy all the way down.

### Byte-Aligned Nesting

When the start bit is a multiple of 8, the inner view is sliced at a byte boundary:

```csharp
[BitFieldsView]
public partial record struct InnerView
{
    [BitField(0, 7)] public partial byte Value { get; set; }
}

[BitFieldsView]
public partial record struct OuterView
{
    [BitField(0, 7)]   public partial byte Header { get; set; }
    [BitField(16, 23)] public partial InnerView Inner { get; set; }  // byte 2
}

byte[] buffer = new byte[4];
var outer = new OuterView(buffer);
var inner = outer.Inner;      // view over same buffer at byte 2
inner.Value = 0x42;           // writes directly to buffer[2]
```

### Non-Byte-Aligned Nesting

When the start bit is not byte-aligned, the inner view receives a bit offset:

```csharp
[BitFieldsView]
public partial record struct OuterView
{
    [BitField(0, 3)]  public partial byte LowNibble { get; set; }
    [BitField(4, 11)] public partial InnerView Inner { get; set; }  // byte 0, bit 4
}

var inner = outer.Inner;   // view at byte 0 with 4-bit offset
inner.Value = 0xFF;        // writes bits 4-11 of the buffer
```

The general case `[BitField(20, 27)]` places the inner view at byte 2, bit 4.

### Write-Through

Writes through nested views go directly to the underlying buffer:

```csharp
var outer = new OuterView(buffer);
outer.Header = 0x99;
var inner = outer.Inner;
inner.Value = 0xAB;

outer.Header;       // still 0x99 -- different bytes
outer.Inner.Value;  // 0xAB -- reads the same memory inner wrote to
```

## Mixed-Endian Nesting

Each nested `[BitFieldsView]` independently controls its own byte order.
For individual fields that differ from the struct default, use endian-aware types
(`UInt32Be`, `UInt16Le`, etc.) as a per-field override.

```csharp
// Embedded network capture header -- big-endian
[BitFieldsView(ByteOrder.BigEndian, BitOrder.BitZeroIsMsb)]
public partial record struct CaptureHeaderView
{
    [BitField(0, 15)]  public partial ushort Protocol { get; set; }
    [BitField(16, 31)] public partial ushort Length { get; set; }
    [BitField(32, 63)] public partial uint SequenceNum { get; set; }
}

// x86 binary file blob -- little-endian, with one BE field and a nested BE sub-view
[BitFieldsView]
public partial record struct FileBlobView
{
    [BitField(0, 31)]    public partial uint Magic { get; set; }              // LE
    [BitField(32, 63)]   public partial uint Timestamp { get; set; }          // LE
    [BitField(64, 95)]   public partial UInt32Be CapturedSrcIp { get; set; }  // per-field BE override
    [BitField(96, 111)]  public partial ushort RecordCount { get; set; }      // LE
    [BitField(112, 175)] public partial CaptureHeaderView Capture { get; set; } // nested BE sub-view
}

// Outer transport -- big-endian, wrapping the LE blob
[BitFieldsView(ByteOrder.BigEndian, BitOrder.BitZeroIsMsb)]
public partial record struct TransportView
{
    [BitField(0, 15)]   public partial ushort MessageType { get; set; }
    [BitField(16, 47)]  public partial uint PayloadLength { get; set; }
    [BitField(48, 63)]  public partial ushort Checksum { get; set; }
    [BitField(64, 239)] public partial FileBlobView Blob { get; set; }  // nested LE
}
```

Each layer reads/writes with its own byte order. The `UInt32Be` property in the LE struct
stores the IP address in network order without needing a separate sub-view. All writes go
to the same underlying buffer.

---

## Hardware Registers

### 8-bit VIA Register

```csharp
[BitFields(typeof(byte))]
public partial struct ViaRegB
{
    [BitField(0, 2)] public partial byte SoundVolume { get; set; }
    [BitFlag(3)]     public partial bool SoundBuffer { get; set; }
    [BitFlag(4)]     public partial bool OverlayRom { get; set; }
    [BitFlag(5)]     public partial bool HeadSelect { get; set; }
    [BitFlag(6)]     public partial bool VideoPage { get; set; }
    [BitFlag(7)]     public partial bool SccAccess { get; set; }
}
```

### 16-bit Keyboard Register

```csharp
[BitFields(typeof(ushort))]
public partial struct KeyboardReg0
{
    [BitField(0, 6)]  public partial byte SecondKeyCode { get; set; }
    [BitFlag(7)]      public partial bool SecondKeyUp { get; set; }
    [BitField(8, 14)] public partial byte FirstKeyCode { get; set; }
    [BitFlag(15)]     public partial bool FirstKeyUp { get; set; }
}
```

### 64-bit Status Register

```csharp
[BitFields(typeof(ulong))]
public partial struct StatusReg64
{
    [BitField(0, 7)]   public partial byte Status { get; set; }
    [BitField(8, 23)]  public partial ushort DataWord { get; set; }
    [BitField(24, 55)] public partial uint Address { get; set; }
    [BitFlag(56)]      public partial bool Enable { get; set; }
    [BitFlag(57)]      public partial bool Ready { get; set; }
    [BitFlag(58)]      public partial bool Error { get; set; }
}
```

## IEEE 754 Floating-Point Decomposition

`[BitFields(typeof(double))]` decomposes IEEE 754 values into their constituent bit fields.
Because the struct is `partial`, you can add computed properties alongside the generated accessors.

```
Bit:  63 | 62 ??? 52 | 51 ???????????????????? 0
      S  | Exponent   | Mantissa (fractional)
      1  |   11 bits  |       52 bits
```

```csharp
[BitFields(typeof(double))]
public partial struct IEEE754Double
{
    [BitField(0, 51)]  public partial ulong  Mantissa { get; set; }
    [BitField(52, 62)] public partial ushort Exponent { get; set; }
    [BitFlag(63)]      public partial bool   Sign     { get; set; }

    public bool IsNaN => Exponent == 0x7FF && Mantissa != 0;
    public bool IsInfinity => Exponent == 0x7FF && Mantissa == 0;
    public bool IsDenormalized => Exponent == 0 && Mantissa != 0;
    public bool IsNormal => Exponent > 0 && Exponent < 0x7FF;
    public int? UnbiasedExponent => IsNormal ? Exponent - 1023 : null;
}

// Inspect any double
IEEE754Double pi = Math.PI;
pi.Sign;              // false
pi.Exponent;          // 1024
pi.UnbiasedExponent;  // 1 (2^1 range, since 2 <= pi < 4)
pi.Mantissa;          // 0x921FB54442D18

// Construct from bit fields
IEEE754Double val = default;
val.Sign = false;
val.Exponent = 1024;
val.Mantissa = 0x921FB54442D18;
double result = val;  // == Math.PI

// Negate by flipping the sign bit
IEEE754Double x = 42.0;
x.Sign = !x.Sign;    // x == -42.0

// Full arithmetic works through generated operators
IEEE754Double a = 1.0, sqrt5 = Math.Sqrt(5.0), two = 2.0;
IEEE754Double phi = (a + sqrt5) / two;  // golden ratio
```

The same pattern works for `Half` (16-bit) and `float` (32-bit):

```csharp
[BitFields(typeof(Half))]
public partial struct IEEE754Half
{
    [BitField(0, 9)]   public partial ushort Mantissa { get; set; }
    [BitField(10, 14)] public partial byte   Exponent { get; set; }
    [BitFlag(15)]      public partial bool   Sign     { get; set; }
}
```

## .NET Decimal Decomposition

`[BitFields(typeof(decimal))]` decomposes .NET's 128-bit decimal into its constituent fields:

```
Bits:  127 | 126-119 | 118-112 | 111-96   | 95 ?????????????????? 0
       Sign| Reserved| Scale   | Reserved | 96-bit unsigned coefficient
```

```csharp
[BitFields(typeof(decimal))]
public partial struct DecimalParts
{
    [BitField(0, 95)]    public partial UInt128 Coefficient { get; set; }
    [BitField(112, 118)] public partial byte    Scale       { get; set; }
    [BitFlag(127)]       public partial bool    Sign        { get; set; }
}

DecimalParts price = 19.99m;
price.Sign;          // false
price.Scale;         // 2 (divided by 10^2)
price.Coefficient;   // 1999

// Full decimal arithmetic
DecimalParts a = 10.5m, b = 3m;
decimal sum  = a + b;   // 13.5m
decimal prod = a * b;   // 31.5m
```

## Network Protocol Headers (RFC)

### Value-Type Approach (BitFields Composition)

For protocols parsed word-by-word, embed reusable flag structs inside larger word structs:

```csharp
// 3-bit IPv4 flags
[BitFields(typeof(byte), UndefinedBitsMustBe.Zeroes)]
public partial struct IPv4Flags
{
    [BitFlag(0)] public partial bool MoreFragments { get; set; }
    [BitFlag(1)] public partial bool DontFragment { get; set; }
    [BitFlag(2)] public partial bool Reserved { get; set; }
}

// 9-bit TCP control flags
[BitFields(typeof(ushort), UndefinedBitsMustBe.Zeroes)]
public partial struct TcpFlags
{
    [BitFlag(0)] public partial bool FIN { get; set; }
    [BitFlag(1)] public partial bool SYN { get; set; }
    [BitFlag(2)] public partial bool RST { get; set; }
    [BitFlag(3)] public partial bool PSH { get; set; }
    [BitFlag(4)] public partial bool ACK { get; set; }
    [BitFlag(5)] public partial bool URG { get; set; }
}

// Embed flags into 32-bit header words
[BitFields(typeof(uint))]
public partial struct IPv4FragmentWord
{
    [BitField(16, 31)] public partial ushort Identification { get; set; }
    [BitField(13, 15)] public partial IPv4Flags Flags { get; set; }       // composed!
    [BitField(0, 12)]  public partial ushort FragmentOffset { get; set; }
}

[BitFields(typeof(uint))]
public partial struct TcpControlWord
{
    [BitField(28, 31)] public partial byte DataOffset { get; set; }
    [BitField(16, 24)] public partial TcpFlags Flags { get; set; }        // composed!
    [BitField(0, 15)]  public partial ushort WindowSize { get; set; }
}

// Fluent construction
var tcp = TcpControlWord.Zero
    .WithDataOffset(5)
    .WithFlags(TcpFlags.Zero.WithSYN(true).WithACK(true))
    .WithWindowSize(65535);

tcp.Flags.SYN;  // true
tcp.Flags.ACK;  // true

// TCP three-way handshake using static Bit properties
var syn    = TcpFlags.SYNBit;
var synAck = TcpFlags.SYNBit | TcpFlags.ACKBit;
var ack    = TcpFlags.ACKBit;
```

### Buffer-View Approach (BitFieldsView)

For zero-copy parsing of complete headers over byte buffers, use `[BitFieldsView]` with
`ByteOrder.NetworkEndian` and `BitOrder.BitZeroIsMsb` so that bit positions match RFC diagrams directly.
Plain `ushort` and `uint` properties are all you need -- the struct-level `ByteOrder` handles
serialization.

Reusable protocol header files are available in the test project at `Test/Protocols/`.

### IPv4 Header (RFC 791)

See `Test/Protocols/IPv4HeaderView.cs` for a copy-pasteable implementation.

```
 0                   1                   2                   3
 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|Version|  IHL  |Type of Service|          Total Length         |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|         Identification        |Flags|      Fragment Offset    |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|  Time to Live |    Protocol   |         Header Checksum       |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                       Source Address                          |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                    Destination Address                        |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
```

```csharp
[BitFieldsView(ByteOrder.NetworkEndian, BitOrder.BitZeroIsMsb)]
public partial record struct IPv4HeaderView
{
    [BitField(0, 3)]     public partial byte Version { get; set; }
    [BitField(4, 7)]     public partial byte Ihl { get; set; }
    [BitField(8, 13)]    public partial byte Dscp { get; set; }
    [BitField(14, 15)]   public partial byte Ecn { get; set; }
    [BitField(16, 31)]   public partial ushort TotalLength { get; set; }
    [BitField(32, 47)]   public partial ushort Identification { get; set; }
    [BitFlag(48)]        public partial bool ReservedFlag { get; set; }
    [BitFlag(49)]        public partial bool DontFragment { get; set; }
    [BitFlag(50)]        public partial bool MoreFragments { get; set; }
    [BitField(51, 63)]   public partial ushort FragmentOffset { get; set; }
    [BitField(64, 71)]   public partial byte TimeToLive { get; set; }
    [BitField(72, 79)]   public partial byte Protocol { get; set; }
    [BitField(80, 95)]   public partial ushort HeaderChecksum { get; set; }
    [BitField(96, 127)]  public partial uint SourceAddress { get; set; }
    [BitField(128, 159)] public partial uint DestinationAddress { get; set; }

    public int HeaderLengthBytes => Ihl * 4;
}
```

### TCP Header (RFC 793)

See `Test/Protocols/TcpHeaderView.cs` for a copy-pasteable implementation.

```
 0                   1                   2                   3
 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|          Source Port          |       Destination Port        |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                        Sequence Number                        |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                    Acknowledgment Number                      |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|  Data |       |N|C|E|U|A|P|R|S|F|                             |
| Offset| Rsvd  |S|W|C|R|C|S|S|Y|I|         Window Size         |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|           Checksum            |         Urgent Pointer        |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
```

```csharp
[BitFieldsView(ByteOrder.NetworkEndian, BitOrder.BitZeroIsMsb)]
public partial record struct TcpHeaderView
{
    [BitField(0, 15)]    public partial ushort SourcePort { get; set; }
    [BitField(16, 31)]   public partial ushort DestinationPort { get; set; }
    [BitField(32, 63)]   public partial uint SequenceNumber { get; set; }
    [BitField(64, 95)]   public partial uint AcknowledgmentNumber { get; set; }
    [BitField(96, 99)]   public partial byte DataOffset { get; set; }
    [BitField(100, 102)] public partial byte Reserved { get; set; }
    [BitFlag(103)]       public partial bool NS { get; set; }
    [BitFlag(104)]       public partial bool CWR { get; set; }
    [BitFlag(105)]       public partial bool ECE { get; set; }
    [BitFlag(106)]       public partial bool URG { get; set; }
    [BitFlag(107)]       public partial bool ACK { get; set; }
    [BitFlag(108)]       public partial bool PSH { get; set; }
    [BitFlag(109)]       public partial bool RST { get; set; }
    [BitFlag(110)]       public partial bool SYN { get; set; }
    [BitFlag(111)]       public partial bool FIN { get; set; }
    [BitField(112, 127)] public partial ushort WindowSize { get; set; }
    [BitField(128, 143)] public partial ushort Checksum { get; set; }
    [BitField(144, 159)] public partial ushort UrgentPointer { get; set; }

    public int HeaderLengthBytes => DataOffset * 4;
}
```

### UDP Header (RFC 768)

See `Test/Protocols/UdpHeaderView.cs` for a copy-pasteable implementation.

```csharp
[BitFieldsView(ByteOrder.NetworkEndian, BitOrder.BitZeroIsMsb)]
public partial record struct UdpHeaderView
{
    [BitField(0, 15)]  public partial ushort SourcePort { get; set; }
    [BitField(16, 31)] public partial ushort DestinationPort { get; set; }
    [BitField(32, 47)] public partial ushort Length { get; set; }
    [BitField(48, 63)] public partial ushort Checksum { get; set; }
}
```

### IPv6 Header (RFC 2460)

See `Test/Protocols/IPv6HeaderView.cs` for a copy-pasteable implementation.

```csharp
[BitFieldsView(ByteOrder.NetworkEndian, BitOrder.BitZeroIsMsb)]
public partial record struct IPv6HeaderView
{
    [BitField(0, 3)]     public partial byte Version { get; set; }
    [BitField(4, 11)]    public partial byte TrafficClass { get; set; }
    [BitField(12, 31)]   public partial uint FlowLabel { get; set; }
    [BitField(32, 47)]   public partial ushort PayloadLength { get; set; }
    [BitField(48, 55)]   public partial byte NextHeader { get; set; }
    [BitField(56, 63)]   public partial byte HopLimit { get; set; }
}
```

## Parsing a Captured Network Packet

Parse a raw Ethernet/IPv4/TCP packet using the protocol views above:

```csharp
byte[] frame = File.ReadAllBytes("captured_frame.bin");

// Skip 14-byte Ethernet header to reach the IPv4 header
var ip = new IPv4HeaderView(frame, 14);

Console.WriteLine(ip.Version);           // 4
Console.WriteLine(ip.Ihl);              // 5 (20-byte header)
Console.WriteLine(ip.TotalLength);      // total IP packet length
Console.WriteLine(ip.TimeToLive);       // e.g. 64
Console.WriteLine(ip.Protocol);         // 6 = TCP

// Parse the TCP header immediately after the IPv4 header
int tcpOffset = 14 + ip.HeaderLengthBytes;
var tcp = new TcpHeaderView(frame, tcpOffset);

Console.WriteLine(tcp.SourcePort);      // e.g. 443
Console.WriteLine(tcp.DestinationPort); // e.g. 54321
Console.WriteLine(tcp.SYN);            // true for SYN packets
Console.WriteLine(tcp.ACK);            // true for ACK packets

// The application payload starts after both headers
int payloadOffset = tcpOffset + tcp.HeaderLengthBytes;
var payload = frame.AsSpan(payloadOffset);
```

All reads go directly to the original `frame` buffer. No copies, no allocations.

## Mixed-Endian Capture File

A pcap-style capture file on Windows (LE) containing network packets (BE):

```csharp
// Pcap global header -- little-endian (x86 native)
[BitFieldsView]
public partial record struct PcapGlobalHeader
{
    [BitField(0, 31)]   public partial uint MagicNumber { get; set; }    // 0xA1B2C3D4
    [BitField(32, 47)]  public partial ushort VersionMajor { get; set; }
    [BitField(48, 63)]  public partial ushort VersionMinor { get; set; }
    [BitField(128, 159)] public partial uint SnapLen { get; set; }
    [BitField(160, 191)] public partial uint LinkType { get; set; }      // 1 = Ethernet
}

// Pcap per-packet header -- little-endian
[BitFieldsView]
public partial record struct PcapPacketHeader
{
    [BitField(0, 31)]   public partial uint TimestampSec { get; set; }
    [BitField(32, 63)]  public partial uint TimestampUsec { get; set; }
    [BitField(64, 95)]  public partial uint IncludedLen { get; set; }
    [BitField(96, 127)] public partial uint OriginalLen { get; set; }
}

// Parse a pcap file
byte[] pcap = File.ReadAllBytes("capture.pcap");

var global = new PcapGlobalHeader(pcap);
Console.WriteLine(global.MagicNumber == 0xA1B2C3D4); // True (LE native)
Console.WriteLine(global.VersionMajor);               // 2
Console.WriteLine(global.VersionMinor);               // 4

// First packet header starts at byte 24 (after global header)
var pktHdr = new PcapPacketHeader(pcap, 24);
int packetLen = (int)pktHdr.IncludedLen;

// The packet data (Ethernet frame) starts at byte 40
// Parse it with big-endian views -- they handle their own byte order
var ip = new IPv4HeaderView(pcap, 40 + 14);  // skip Ethernet header
Console.WriteLine(ip.Version);               // 4 -- BE field in LE file
Console.WriteLine(ip.SourceAddress);         // parsed as big-endian

int tcpOff = 40 + 14 + ip.HeaderLengthBytes;
var tcp = new TcpHeaderView(pcap, tcpOff);
Console.WriteLine(tcp.SourcePort);           // BE, correct
Console.WriteLine(tcp.SYN);
```

The pcap headers (LE) and network headers (BE) each declare their own byte order.
No manual byte-swapping. No endian-aware types needed for the common case.
`UInt32Be` / `UInt16Le` per-field overrides are only needed for the uncommon case
where a single struct mixes endianness at the individual field level.

---

## Performance

Benchmarks show generated code performs within **1%** of hand-coded bit manipulation.

**Property Accessors vs Raw Bit Manipulation** (500M iterations, .NET 10):

| Operation | Raw Bit Ops | Generated Properties | Difference |
|-----------|-------------|---------------------|------------|
| Boolean GET | 271 ms | 263 ms | ~0% (noise) |
| Boolean SET | 506 ms | 494 ms | ~0% (noise) |
| Field GET (shift+mask) | 124 ms | 123 ms | ~0% (noise) |

All masks and shifts are compile-time constants. `[MethodImpl(MethodImplOptions.AggressiveInlining)]`
eliminates all property call overhead. No heap allocations, no reflection, no boxing.

## Generated Code Listing

### BitFields Generated Code

For this struct:

```csharp
[BitFields(typeof(byte))]
public partial struct StatusRegister
{
    [BitFlag(0)] public partial bool Ready { get; set; }
    [BitFlag(1)] public partial bool Error { get; set; }
    [BitField(2, 4)] public partial byte Mode { get; set; }
}
```

The generator creates:

```csharp
public partial struct StatusRegister
{
    private byte Value;
    public StatusRegister(byte value) { Value = value; }

    // BitFlag properties
    public partial bool Ready
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (Value & 0x01) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Value = value ? (byte)(Value | 0x01) : (byte)(Value & 0xFE);
    }

    // BitField properties
    public partial byte Mode
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (byte)((Value >> 2) & 0x07);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Value = (byte)((Value & 0xE3) | ((value << 2) & 0x1C));
    }

    // Static properties
    public static StatusRegister ReadyBit => new(0x01);
    public static StatusRegister ModeMask => new(0x1C);

    // Fluent methods
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StatusRegister WithReady(bool value) =>
        new(value ? (byte)(Value | 0x01) : (byte)(Value & 0xFE));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StatusRegister WithMode(byte value) =>
        new((byte)((Value & 0xE3) | ((value << 2) & 0x1C)));

    // Operators (arithmetic, bitwise, shift, comparison, equality)
    public static StatusRegister operator |(StatusRegister a, StatusRegister b) =>
        new((byte)(a.Value | b.Value));
    public static int operator <<(StatusRegister a, int b) => a.Value << b;
    // ... plus +, -, *, /, %, &, ^, ~, >>, >>>, <, >, <=, >=, ==, !=

    // Implicit conversions
    public static implicit operator byte(StatusRegister value) => value.Value;
    public static implicit operator StatusRegister(byte value) => new(value);

    // Interfaces: IComparable, IComparable<T>, IEquatable<T>,
    // IFormattable, ISpanFormattable, IParsable<T>, ISpanParsable<T>
}
```

### BitFieldsView Generated Code

For a `[BitFieldsView]` struct, the generator creates:

- Private `Memory<byte>` field
- Constructors: `Memory<byte>`, `byte[]`, `byte[] + offset`
- `Data` property exposing the underlying memory
- `SizeInBytes` constant
- Property accessors using `BinaryPrimitives` with `AggressiveInlining`
