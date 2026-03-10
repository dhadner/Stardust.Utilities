# BitFields

Source-generated, type-safe, zero-overhead bit manipulation for .NET.

Two attributes, one concept: define bit-level fields with `[BitField]` and `[BitFlag]`, then choose your backing strategy.

| Attribute | Backing | Best for |
|-----------|---------|----------|
| `[BitFields]` | Value type (`byte`, `ushort`, `uint`, `nuint`, ...) | Hardware registers, opcodes, small bit-packed structs |
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
- [Native Integer Types (nint / nuint)](#native-integer-types-nint--nuint)
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

**Pre-Defined Types**
- [Numeric Decomposition Types](#numeric-decomposition-types)

**Real-World Examples**
- [Hardware Registers](#hardware-registers)
- [Network Protocol Headers (RFC)](#network-protocol-headers-rfc)
- [Parsing a Captured Network Packet](#parsing-a-captured-network-packet)
- [Mixed-Endian Capture File](#mixed-endian-capture-file)

**Visualization**
- [RFC Diagram Generator](#rfc-diagram-generator)
  - [Instance API](#instance-api)
  - [Static Type-Based API](#static-type-based-api)
  - [Static Field-Based API](#static-field-based-api)
- [Demo Application](#demo-application)

**Reference**
- [Performance](#performance)
- [Generated Code Listing](#generated-code-listing)

---

## Quick Start -- BitFields (Value Type)

```csharp
[BitFields(StorageType.UInt16)]  // StorageType enum -- preferred
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

The `StorageType` enum is the recommended way to specify the backing type. IntelliSense
autocomplete shows all supported types as you type, so you discover valid choices instantly
without consulting documentation. If you choose an unsupported type via the older `typeof(T)`
form, the error is only discovered after writing the struct body and building, which can mean
significant rework. The `StorageType` enum moves that validation to the moment you write the
attribute -- making development faster, less error prone, and reducing late-stage surprises.

The `typeof(T)` form is still fully supported for backward compatibility:

```csharp
[BitFields(typeof(ushort))]  // typeof(T) -- also valid
public partial struct KeyboardReg { ... }
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
| `[BitFields(StorageType.X)]` | `StorageType` enum, optional `UndefinedBitsMustBe`, optional `BitOrder` | Preferred. Enum provides IntelliSense discovery of all supported types |
| `[BitFields(typeof(T))]` | Storage type, optional `UndefinedBitsMustBe`, optional `BitOrder` | Also supported. Equivalent to the enum form; exists for backward compatibility |
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

`[BitFields]` supports these storage types. The `StorageType` enum is the preferred way to
specify the backing type because IntelliSense shows all valid choices as you type, preventing
mistakes before they happen. The `typeof(T)` form is also supported for backward compatibility.

| `StorageType` Enum | `typeof(T)` | Size | Notes |
|--------------------|-------------|------|-------|
| `StorageType.Byte` / `.SByte` | `typeof(byte)` / `typeof(sbyte)` | 8 bits | |
| `StorageType.UInt16` / `.Int16` | `typeof(ushort)` / `typeof(short)` | 16 bits | |
| `StorageType.UInt32` / `.Int32` | `typeof(uint)` / `typeof(int)` | 32 bits | |
| `StorageType.UInt64` / `.Int64` | `typeof(ulong)` / `typeof(long)` | 64 bits | |
| `StorageType.NUInt` / `.NInt` | `typeof(nuint)` / `typeof(nint)` | 32 or 64 bits | Platform-dependent; see [Native Integer Types](#native-integer-types-nint--nuint) |
| `StorageType.UInt128` / `.Int128` | `typeof(UInt128)` / `typeof(Int128)` | 128 bits | |
| `StorageType.Half` | `typeof(Half)` | 16 bits | IEEE 754 half-precision |
| `StorageType.Single` | `typeof(float)` | 32 bits | IEEE 754 single-precision |
| `StorageType.Double` | `typeof(double)` | 64 bits | IEEE 754 double-precision |
| `StorageType.Decimal` | `typeof(decimal)` | 128 bits | .NET decimal |
| `[BitFields(N)]` | `[BitFields(N)]` | N bits | Arbitrary width, 1 to 16,384 bits |

Using `typeof(T)` with a type not in this table (for example, `typeof(Guid)`) produces
compiler error **SD0003**, which names the unsupported type and lists all valid alternatives.
The `StorageType` enum avoids this entirely because only valid values appear in IntelliSense.

## Native Integer Types (nint / nuint)

`nint` and `nuint` are platform-dependent native integer types: 32 bits wide in a 32-bit process,
64 bits wide in a 64-bit process. They are useful for memory-mapped registers, pointer-sized
bit-packed values, and other platform-width-sensitive structures.

```csharp
// 32-bit safe: all fields fit within bits 0-31 on any platform
[BitFields(StorageType.NUInt)]
public partial struct PointerTagReg
{
    [BitField(0, 7)]  public partial byte Tag { get; set; }       // bits 0..=7
    [BitField(8, 11)] public partial byte Command { get; set; }   // bits 8..=11
    [BitFlag(28)]     public partial bool Enabled { get; set; }   // bit 28
    [BitFlag(31)]     public partial bool Valid { get; set; }     // bit 31
}

// 64-bit only: uses high bits above 31
[BitFields(StorageType.NUInt)]
public partial struct WideNativeReg
{
#pragma warning disable SD0002 // High bits: only valid on 64-bit
    [BitField(0, 7)]   public partial byte Status { get; set; }    // bits 0..=7
    [BitField(8, 23)]  public partial ushort Data { get; set; }    // bits 8..=23
    [BitField(24, 55)] public partial uint Address { get; set; }   // bits 24..=55
    [BitFlag(56)]      public partial bool Valid { get; set; }     // bit 56
    [BitFlag(57)]      public partial bool Ready { get; set; }     // bit 57
#pragma warning restore SD0002
}
```

The generated struct includes a platform-dependent `SIZE_IN_BYTES` property (`nint.Size`) and
uses platform-branched serialization for byte span operations (`BinaryPrimitives.ReadUInt32LittleEndian`
on 32-bit, `BinaryPrimitives.ReadUInt64LittleEndian` on 64-bit).

### Compiler Diagnostics for nint/nuint

Because `nint`/`nuint` can be 32 bits on some platforms, the source generator performs
compile-time validation of all field and flag bit positions. If any field or flag accesses a bit
above bit 31, the generator emits a diagnostic whose severity depends on the project's
`PlatformTarget` setting:

| Diagnostic | Severity | When Emitted | Meaning |
|------------|----------|--------------|---------|
| **SD0001** | **Error** | `PlatformTarget` is `x86` | The build is restricted to 32-bit. `nint`/`nuint` is always 32 bits, so bits 32+ are unreachable. The struct will corrupt data at runtime. |
| **SD0002** | **Warning** | `PlatformTarget` is `AnyCPU` (default) or unset | The binary may run on either 32-bit or 64-bit. Bits 32+ work on 64-bit but are silently unreachable on 32-bit, causing data loss. |
| *(none)* | | `PlatformTarget` is `x64` or `ARM64` | The build is restricted to 64-bit. `nint`/`nuint` is always 64 bits, so all bit positions are valid. |

The diagnostic location points to the specific property declaration that exceeds the 32-bit boundary.
For multi-bit fields, the check uses the highest bit of the field (e.g., `[BitField(24, 55)]` checks
bit 55, not bit 24).

**Resolving SD0001 (Error):**
- Move all fields to bits 0-31.
- Or change the storage type to `ulong`/`long` for a guaranteed 64-bit width.

**Resolving SD0002 (Warning):**
- Set `<PlatformTarget>x64</PlatformTarget>` in your `.csproj` if the binary only runs on 64-bit.
- Or change the storage type to `ulong`/`long` for a fixed 64-bit width on all platforms.
- Or suppress the warning with `#pragma warning disable SD0002` if you have confirmed the binary
  will only run on 64-bit processes.

Non-native storage types (`byte`, `uint`, `ulong`, etc.) are never affected by these diagnostics.
Their bit widths are fixed regardless of platform.

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

Enum fields work at any bit position, including bit 0, and support fluent `With` methods:

```csharp
[BitFields(typeof(byte))]
public partial struct CommandReg
{
    [BitField(0, 2)] public partial OpMode Command { get; set; }  // enum at bit 0
    [BitField(3, 5)] public partial OpMode Status { get; set; }   // enum at bit 3
    [BitField(6, 7)] public partial byte Flags { get; set; }
}

var reg = CommandReg.Zero
    .WithCommand(OpMode.Run)
    .WithStatus(OpMode.Sleep)
    .WithFlags(3);

reg.Command;  // OpMode.Run
reg.Status;   // OpMode.Sleep
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

`MustBe` constraints are enforced at every entry point -- construction, implicit conversion,
operators, `With` methods, `Parse`, and `ReadFrom`. This guarantee holds regardless of how the
raw value is produced:

```csharp
[BitFields(typeof(byte))]
public partial struct SyncedReg
{
    [BitFlag(0)]              public partial bool Active { get; set; }
    [BitField(1, 2, MustBe.Zero)] public partial byte Reserved { get; set; }
    [BitField(3, 6)]         public partial byte Data { get; set; }
    [BitFlag(7, MustBe.One)] public partial bool Sync { get; set; }
}

SyncedReg reg = 0x00;
byte raw = reg;           // 0x80 (Sync forced to 1)

reg = 0xFF;
raw = reg;                // 0xF9 (Reserved bits 1-2 cleared, Sync stays 1)

reg.Reserved = 3;         // setter ignores the value -- bits stay 0
reg.Sync = false;         // setter ignores the value -- bit stays 1

var r = ~reg;             // complement goes through constructor -- constraints re-applied
var s = reg | (SyncedReg)0x06;  // OR result normalized -- Reserved stays 0

// Span round-trip also enforces constraints
byte[] buf = [0xFF];
var fromSpan = new SyncedReg(new ReadOnlySpan<byte>(buf));
byte spanRaw = fromSpan;  // 0xF9 (same normalization)

// Parse enforces too
var parsed = SyncedReg.Parse("0xFF", null);
byte parsedRaw = parsed;  // 0xF9
```

For `MustBe.Zero` flags, the getter always returns `false`. For `MustBe.One` flags, it always
returns `true`. Both can be freely combined with `UndefinedBitsMustBe`:

```csharp
[BitFields(typeof(byte), UndefinedBitsMustBe.Zeroes)]
public partial struct ProtocolByte
{
    [BitField(0, 2)]          public partial byte Flags { get; set; }     // normal field
    [BitFlag(3, MustBe.One)]  public partial bool AlwaysHigh { get; set; } // forced to 1
    // Bits 4-7: undefined, forced to 0 by UndefinedBitsMustBe.Zeroes
}

ProtocolByte p = 0xFF;
byte raw = p;             // 0x0F (bits 4-7 zeroed, bit 3 set, Flags preserved)

p = 0x00;
raw = p;                  // 0x08 (only AlwaysHigh forced on)
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
[BitFields(StorageType.Byte)]
public partial struct StatusFlags
{
    [BitFlag(0)] public partial bool Ready { get; set; }
    [BitFlag(1)] public partial bool Error { get; set; }
    [BitField(4, 7)] public partial byte Priority { get; set; }
}

[BitFields(StorageType.UInt16)]
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
[BitFields(StorageType.Byte)]
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
[BitFields(StorageType.Byte)]
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
[BitFields(StorageType.UInt16)]
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
[BitFields(StorageType.UInt64)]
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

## Numeric Decomposition Types

The library ships four pre-defined BitFields structs that decompose .NET numeric types into
their constituent bit fields. Just `using Stardust.Utilities;` and start using them -- no
struct definitions required.

| Type | Storage | Fields | Use case |
|------|---------|--------|----------|
| `IEEE754Half` | `Half` (16-bit) | Sign, BiasedExponent (5-bit, bias 15), Mantissa (10-bit) | Half-precision analysis |
| `IEEE754Single` | `float` (32-bit) | Sign, BiasedExponent (8-bit, bias 127), Mantissa (23-bit) | Single-precision analysis |
| `IEEE754Double` | `double` (64-bit) | Sign, BiasedExponent (11-bit, bias 1023), Mantissa (52-bit) | Double-precision analysis |
| `DecimalBitFields` | `decimal` (128-bit) | Sign, Scale (0-28), Coefficient (96-bit) | Decimal inspection |

All four types include implicit conversions to/from their storage type, full operator support,
and classification properties.

### IEEE754Double

```
IEEE 754 Double-Precision (64-bit)
                 6                                                 5                                                 4                                                 3                                                 2                                                 1                                                 0
  3    2    1    0    9    8    7    6    5    4    3    2    1    0    9    8    7    6    5    4    3    2    1    0    9    8    7    6    5    4    3    2    1    0    9    8    7    6    5    4    3    2    1    0    9    8    7    6    5    4    3    2    1    0    9    8    7    6    5    4    3    2    1    0
+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+
|Sign|                    BiasedExponent                    |                                                                                                                             Mantissa                                                                                                                              |
+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+

  Mantissa: 52-bit significand (fractional part); implicit leading 1 not stored
  BiasedExponent: 11-bit biased exponent (bias 1023); subtract 1023 for true power of 2
  Sign: Sign bit: 1 = negative, 0 = positive
```

```csharp
using Stardust.Utilities;

// Inspect any double -- implicit conversion from double
IEEE754Double pi = Math.PI;
pi.Sign;              // false
pi.BiasedExponent;    // 1024 (raw stored value, includes +1023 bias)
pi.Exponent;          // 1    (true mathematical power: 2^1, since 2 <= pi < 4)
pi.Mantissa;          // 0x921FB54442D18

// Construct from bit fields
IEEE754Double val = default;
val.Sign = false;
val.BiasedExponent = 1024;
val.Mantissa = 0x921FB54442D18;
double result = val;  // == Math.PI

// Negate by flipping the sign bit
IEEE754Double x = 42.0;
x.Sign = !x.Sign;    // x == -42.0

// Full arithmetic works through generated operators
IEEE754Double a = 1.0, sqrt5 = Math.Sqrt(5.0), two = 2.0;
IEEE754Double phi = (a + sqrt5) / two;  // golden ratio

// Classification properties
pi.IsNormal;              // true
pi.IsNaN;                 // false
pi.IsInfinity;            // false
pi.IsDenormalized;        // false
pi.IsZero;                // false

// Classify special values
IEEE754Double nan = double.NaN;
nan.IsNaN;                // true

IEEE754Double inf = double.PositiveInfinity;
inf.IsInfinity;           // true

IEEE754Double tiny = double.Epsilon;
tiny.IsDenormalized;      // true
```

### IEEE754Single

```
IEEE 754 Single-Precision (32-bit)
       3                                                 2                                                 1                                                 0
  1    0    9    8    7    6    5    4    3    2    1    0    9    8    7    6    5    4    3    2    1    0    9    8    7    6    5    4    3    2    1    0
+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+
|Sign|            BiasedExponent             |                                                     Mantissa                                                     |
+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+

  Mantissa: 23-bit significand (fractional part); implicit leading 1 not stored
  BiasedExponent: 8-bit biased exponent (bias 127); subtract 127 for true power of 2
  Sign: Sign bit: 1 = negative, 0 = positive
```

```csharp
IEEE754Single f = 1.5f;
f.Sign;            // false
f.BiasedExponent;  // 127 (raw stored value)
f.Exponent;        // 0   (true power: 1.5 is in [1, 2), so 2^0)
f.Mantissa;        // 0x400000 (bit 22 set = 0.5)

// Build from parts
var built = IEEE754Single.Zero
    .WithSign(false)
    .WithBiasedExponent(127)
    .WithMantissa(0x400000u);
((float)built);  // 1.5f

// Classification
IEEE754Single eps = float.Epsilon;
eps.IsDenormalized;  // true
eps.IsNormal;        // false
```

### IEEE754Half

```
IEEE 754 Half-Precision (16-bit)
                           1                                                 0
  5    4    3    2    1    0    9    8    7    6    5    4    3    2    1    0
+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+
|Sign|     BiasedExponent     |                    Mantissa                     |
+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+

  Mantissa: 10-bit significand (fractional part); implicit leading 1 not stored
  BiasedExponent: 5-bit biased exponent (bias 15); subtract 15 for true power of 2
  Sign: Sign bit: 1 = negative, 0 = positive
```

```csharp
IEEE754Half h = (Half)1.5;
h.Sign;            // false
h.BiasedExponent;  // 15 (raw stored value)
h.Exponent;        // 0  (true power: 1.5 is in [1, 2), so 2^0)
h.Mantissa;        // 0x200 (bit 9 set = 0.5)
h.IsNormal;        // true

// Constants for reference
IEEE754Half.EXPONENT_BIAS;  // 15
IEEE754Half.MAX_BIASED_EXPONENT;   // 0x1F (31)
```

### DecimalBitFields

```
.NET Decimal (128-bit)
       3                                                 2                                                 1                                                 0
  1    0    9    8    7    6    5    4    3    2    1    0    9    8    7    6    5    4    3    2    1    0    9    8    7    6    5    4    3    2    1    0
+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+
|                                                                          Coefficient                                                                          |
+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+
|                                                                          Coefficient                                                                          |
+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+
|                                                                          Coefficient                                                                          |
+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+
|Sign|               Undefined               |              Scale               |                                   Undefined                                   |
+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+----+

  Coefficient: 96-bit unsigned integer coefficient (value before scaling)
  Scale: Scale factor (0-28); value = Coefficient / 10^Scale
  Sign: Sign bit: 1 = negative, 0 = positive

  U/Undefined = bits not defined in the struct
```

```csharp
DecimalBitFields price = 19.99m;
price.Sign;          // false
price.Scale;         // 2 (divided by 10^2)
price.Coefficient;   // 1999

// Full decimal arithmetic
DecimalBitFields a = 10.5m, b = 3m;
decimal sum  = a + b;   // 13.5m
decimal prod = a * b;   // 31.5m

// Inspect sign
DecimalBitFields neg = -42m;
neg.Sign;       // true
neg.Coefficient; // 42
```

### Constants

Each IEEE 754 type provides bias and max-exponent constants:

| Type | `EXPONENT_BIAS` | `MAX_BIASED_EXPONENT` | `MIN_EXPONENT` | `MAX_EXPONENT` |
|------|-----------------|----------------|----------------|---------------------|
| `IEEE754Half` | 15 | 31 (0x1F) | -14 | 15 |
| `IEEE754Single` | 127 | 255 (0xFF) | -126 | 127 |
| `IEEE754Double` | 1023 | 2047 (0x7FF) | -1022 | 1023 |
| `DecimalBitFields` | -- | `MAX_SCALE` = 28 | -- | -- |

`MIN_EXPONENT` and `MAX_EXPONENT` define the normal range for the `WithExponent(int)` method
and the `Exponent` setter (the true mathematical exponent, after bias removal). Values outside
this range are masked by the underlying `WithBiasedExponent`/`BiasedExponent` setter and may
produce non-normal encodings (zero, denormalized, infinity, or NaN).

### Classification Properties (IEEE 754 types)

All three IEEE 754 types provide the same classification properties:

| Property | Condition | Description |
|----------|-----------|-------------|
| `IsNormal` | 0 < biasedExponent < max | Ordinary floating-point value |
| `IsDenormalized` | biasedExponent = 0, mantissa != 0 | Subnormal (very small) value |
| `IsZero` | biasedExponent = 0, mantissa = 0 | Positive or negative zero |
| `IsInfinity` | biasedExponent = max, mantissa = 0 | Positive or negative infinity |
| `IsNaN` | biasedExponent = max, mantissa != 0 | Not a Number |
| `Exponent` | Normal values only | True mathematical exponent (biased minus bias), or `null`. Setter applies bias automatically; assigning `null` sets `BiasedExponent` to 0, and `BiasedExponent` == 0 returns null for `Exponent` |

### WithExponent (Fluent True-Exponent Setter)

All three IEEE 754 types provide a `WithExponent(int)` fluent method that sets the
`BiasedExponent` from a true mathematical exponent (the bias is added automatically).
Out-of-range values are masked by the underlying `WithBiasedExponent` method, consistent
with all other generated `With...` methods.

The `Exponent` property also provides a setter: assigning an `int` applies the bias and
sets `BiasedExponent`; assigning `null` sets `BiasedExponent` to 0.

```csharp
// Build 2^3 = 8.0 from a true exponent
var d = IEEE754Double.Zero.WithExponent(3).WithMantissa(0);
double value = d;  // 8.0

// Round-trip: read Exponent, rebuild with WithExponent
IEEE754Double pi = Math.PI;
int exp = pi.Exponent!.Value;           // 1
var rebuilt = IEEE754Double.Zero
    .WithExponent(exp)
    .WithMantissa(pi.Mantissa);
double result = rebuilt;                 // == Math.PI

// Set Exponent directly via the setter
IEEE754Double d2 = 1.0;
d2.Exponent = 3;                         // BiasedExponent = 3 + 1023 = 1026
d2.Exponent = null;                      // BiasedExponent = 0

// Out-of-range values are masked (no exception)
var h = IEEE754Half.Zero.WithExponent(16);  // biased value masked to 5-bit field
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
    [BitFlag(2, MustBe.Zero)] public partial bool Reserved { get; set; } // Must be 0 even though defined
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

## RFC Diagram Generator

`BitFieldDiagram` generates RFC 2360-style ASCII bit field diagrams from any `[BitFields]` or
`[BitFieldsView]` struct. It reads the generated `Fields` metadata property and produces a
text diagram with bit-position headers, byte offsets, and auto-sized cells.

There are three ways to use it. The **instance API is preferred** because it supports
subclassing, runtime option changes, and reusable diagram objects. The static methods
still work but cannot be overridden.

| Approach | Best for |
|----------|----------|
| **Instance API** (`new BitFieldDiagram(...)`) | **Preferred.** Reusable, configurable, subclass-friendly diagrams -- UI bindings, multi-struct lists, changing options at runtime |
| **Static type-based API** (`BitFieldDiagram.Render(typeof(...))`) | Quick one-shot rendering from a `Type` |
| **Static field-based API** (`BitFieldDiagram.Render(fields)`) | Low-level control when you already have `ReadOnlySpan<BitFieldInfo>` |

### Instance API

Create a `BitFieldDiagram` object, configure it, add one or more structs, and render.

**Creating a diagram**

```csharp
using Stardust.Utilities;

// Empty diagram -- add structs later
var diagram = new BitFieldDiagram();

// Single struct
var diagram = new BitFieldDiagram(typeof(IPv4HeaderView), description: "IPv4 Header");

// Multiple structs in one diagram
var diagram = new BitFieldDiagram(
    [typeof(M68020DataRegisters), typeof(M68020SR), typeof(M68020CCR)],
    description: "68020 Register Set");
```

**Setting options**

All options are mutable properties, so you can change them at any time before rendering:

```csharp
var diagram = new BitFieldDiagram(typeof(IPv4HeaderView));
diagram.BitsPerRow = 16;
diagram.IncludeDescriptions = true;
diagram.ShowByteOffset = true;
diagram.CommentPrefix = "// ";
diagram.Description = "IPv4 Header";
```

Options can also be set via constructor parameters:

```csharp
var diagram = new BitFieldDiagram(
    typeof(TcpHeaderView),
    description: "TCP Header",
    commentPrefix: "/// ",
    bitsPerRow: 32,
    includeDescriptions: true,
    showByteOffset: true);
```

| Property | Default | Description |
|----------|---------|-------------|
| `BitsPerRow` | 32 | Number of bits per row. Common values: 8, 16, 32, 64. |
| `IncludeDescriptions` | true (constructor) | Appends a legend with field descriptions below the diagram. |
| `ShowByteOffset` | false (constructor) | Shows hex byte offset (e.g., `0x00`) at the left of each content row. |
| `CommentPrefix` | null | When non-null, prepended to every output line (e.g., `"// "`, `"/// "`). |
| `Description` | null | Caption shown above the diagram. When `DescriptionResourceType` is set, this is used as a resource key. |
| `DescriptionResourceType` | null | Optional `Type` with a `ResourceManager` property for localized descriptions. |

**Adding structs**

Use `AddStruct` to add `[BitFields]` or `[BitFieldsView]` types incrementally. It returns a
`Result<string>` so you can check for errors:

```csharp
var diagram = new BitFieldDiagram();
diagram.AddStruct(typeof(IPv4HeaderView));
diagram.AddStruct(typeof(TcpHeaderView));

// Error handling
Result<string> result = diagram.AddStruct(typeof(string)); // not a BitFields type
if (result.IsFailure)
    Console.WriteLine(result.Error); // "Struct 'String' is not a valid [BitFields] or [BitFieldsView] type."
```

Use `AddStructs` to add multiple types in a single call. It accepts any `IEnumerable<Type>`
and stops on the first failure, returning the error:

```csharp
var diagram = new BitFieldDiagram();
diagram.AddStructs([typeof(IPv4HeaderView), typeof(TcpHeaderView)]);

// From a dynamic list
List<Type> headerTypes = GetHeaderTypes();
Result<string> result = diagram.AddStructs(headerTypes);
if (result.IsFailure)
    Console.WriteLine(result.Error);
```

The `Structs` property exposes the current list of types:

```csharp
var diagram = new BitFieldDiagram(typeof(IPv4HeaderView));
Console.WriteLine(diagram.Structs.Count); // 1
```

**Rendering**

```csharp
var diagram = new BitFieldDiagram(typeof(IPv4HeaderView), description: "IPv4 Header");
diagram.BitsPerRow = 32;
diagram.IncludeDescriptions = true;

// Render as a single string
Result<string, string> stringResult = diagram.RenderToString();
if (stringResult.IsSuccess)
    Console.WriteLine(stringResult.Value);

// Render as a list of lines
Result<List<string>, string> linesResult = diagram.Render();
if (linesResult.IsSuccess)
    foreach (string line in linesResult.Value)
        Console.WriteLine(line);
```

Both `Render()` and `RenderToString()` return `Result` types. They return an error when no
structs have been added.

**Real-world example (Blazor UI)**

The instance API is ideal for UI scenarios where options change at runtime:

```csharp
// Create reusable diagram objects at initialization
BitFieldDiagram[] sources =
[
    new(typeof(IPv4HeaderView), "IPv4 Header"),
    new(typeof(TcpHeaderView), "TCP Header"),
    new([typeof(M68020DataRegisters), typeof(M68020SR), typeof(M68020CCR)],
        "68020 Register Set"),
];

// Update options from UI controls and re-render
var diagram = sources[selectedIndex];
diagram.BitsPerRow = bitsPerRow;
diagram.IncludeDescriptions = showDescriptions;
diagram.ShowByteOffset = showByteOffset;
diagram.CommentPrefix = commentPrefix;
string text = diagram.RenderToString().Value;
```

### Static Type-Based API

For quick one-shot diagrams, pass a `Type` directly to the static methods:

```csharp
// Render a single type
List<string> lines = BitFieldDiagram.Render(typeof(IPv4HeaderView), bitsPerRow: 32);
string diagram = BitFieldDiagram.RenderToString(typeof(IPv4HeaderView), bitsPerRow: 32);

// Render multiple types as a unified diagram with consistent cell widths
List<string> lines = BitFieldDiagram.RenderList(
    bitFieldsTypes: [typeof(M68020DataRegisters), typeof(M68020SR)],
    bitsPerRow: 32,
    includeDescriptions: true);

string diagram = BitFieldDiagram.RenderListToString(
    bitFieldsTypes: [typeof(M68020DataRegisters), typeof(M68020SR)],
    bitsPerRow: 32,
    includeDescriptions: true);
```

### Static Field-Based API

When you already have a `ReadOnlySpan<BitFieldInfo>` (from the generated `Fields` property),
use the field-based overloads for direct control:

```csharp
// Render as a list of lines
List<string> lines = BitFieldDiagram.Render(IPv4HeaderView.Fields);

// Render as a single string
string diagram = BitFieldDiagram.RenderToString(IPv4HeaderView.Fields);
```

| Parameter | Default | Description |
|-----------|---------|-------------|
| `bitsPerRow` | 32 | Number of bits per row. Common values: 8, 16, 32, 64. |
| `includeDescriptions` | false | Appends a legend with `Description` text for each field. |
| `showByteOffset` | true | Shows hex byte offset (e.g., `0x00`) at the left of each content row. |
| `minCellWidth` | 0 (auto) | Minimum cell width in characters per bit column. When 0, computed automatically. Used internally by `RenderList` for consistent scale. |
| `commentPrefix` | null | When non-null, prepended to every output line. |

```csharp
// 8 bits per row for small registers
string diagram = BitFieldDiagram.RenderToString(StatusRegister.Fields, bitsPerRow: 8);

// 64-bit wide display
string diagram = BitFieldDiagram.RenderToString(TcpHeaderView.Fields, bitsPerRow: 64);

// Include field descriptions
string diagram = BitFieldDiagram.RenderToString(StatusRegister.Fields, includeDescriptions: true);

// Hide byte offsets for compact output
string diagram = BitFieldDiagram.RenderToString(StatusRegister.Fields, showByteOffset: false);

// Add a comment prefix for embedding in source code
string diagram = BitFieldDiagram.RenderToString(StatusRegister.Fields, commentPrefix: "// ");
```

Use `ComputeMinCellWidth` to pre-compute the shared width if you need it for custom layout logic.

### Features

- **Auto-sized cells** -- Cell width adjusts automatically so all field names fit without truncation.
- **Byte offsets** -- Each content row shows the hex byte offset (e.g., `0x00`, `0x04`) on the left.
- **Bit-position headers** -- Tens and ones digit rows in standard RFC format, with digits centered
  over cell dashes.
- **Undefined bits** -- Gaps between defined fields are labeled `Undefined` (or `U` if the span is
  too narrow). A legend is appended when undefined bits are present.
- **Struct-sized rows** -- The last row ends at the struct's last defined bit rather than padding
  to `bitsPerRow`.
- **Single separators** -- One `+-+-+` line between rows, matching RFC 791 style.

### Example Output

IPv4 header at 32 bits per row:

```
        0                   1                   2                   3
        0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
       +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
0x00   |Version|  Ihl  |   Undefined   |          TotalLength          |
       +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
0x04   |                           Undefined                           |
       +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
0x08   |   Undefined   |   Protocol    |           Undefined           |
       +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
0x0C   |                         SourceAddress                         |
       +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
0x10   |                      DestinationAddress                       |
       +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

  U/Undefined = bits not defined in the struct
  Version (bits 0-3): IP protocol version (always 4 for IPv4)
  Ihl (bits 4-7): Internet Header Length in 32-bit words (min 5 = 20 bytes)
  TotalLength (bits 16-31): Total packet length in bytes, including header and payload
  Protocol (bits 72-79): Upper-layer protocol number (6=TCP, 17=UDP, 1=ICMP)
  SourceAddress (bits 96-127): 32-bit source IPv4 address
  DestinationAddress (bits 128-159): 32-bit destination IPv4 address
```

CPU status register with descriptions:

```csharp
var diagram = new BitFieldDiagram(typeof(StatusRegister), bitsPerRow: 16, includeDescriptions: true);
string output = diagram.RenderToString().Value;
```

### Demo Application

**[Try the interactive web demo](https://dhadner.github.io/Stardust.Utilities/)** -- includes an RFC Diagram tab with struct picker, bits/row selector, description and byte offset toggles, and copy to clipboard.

The source code includes two demo apps:
- `Demo/BitFields.DemoWeb` -- Blazor WebAssembly app (runs in any browser, no install)
- `Demo/BitFields.DemoApp` -- WPF desktop app (Windows)

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

The generator creates (abbreviated -- the full output also includes parsing, formatting,
`IComparable`, `IEquatable`, span serialization, and a JSON converter):

```csharp
[JsonConverter(typeof(StatusRegisterJsonConverter))]
public partial struct StatusRegister : IComparable, IComparable<StatusRegister>, IEquatable<StatusRegister>,
                                      IFormattable, ISpanFormattable, IParsable<StatusRegister>, ISpanParsable<StatusRegister>
{
    private byte Value;

    public const int SizeInBytes = 1;
    public static StatusRegister Zero => default;

    public StatusRegister(byte value) { Value = value; }

    // ── BitFlag properties ──────────────────────────────────────
    public partial bool Ready
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (Value & 0x01) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Value = value ? (byte)(Value | 0x01) : (byte)(Value & 0xFE);
    }

    // ── BitField properties ─────────────────────────────────────
    // Note: value is cast to the storage type before shifting to prevent
    // widening promotion from corrupting adjacent bits.
    public partial byte Mode
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (byte)((Value >> 2) & 0x07);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Value = (byte)((Value & 0xE3) | ((((byte)value) << 2) & 0x1C));
    }

    // ── Static Bit and Mask properties ──────────────────────────
    public static StatusRegister ReadyBit => new((byte)0x01);
    public static StatusRegister ErrorBit => new((byte)0x02);
    public static StatusRegister ModeMask => new((byte)0x1C);

    // ── Metadata ────────────────────────────────────────────────
    public static string? StructDescription => null;
    public static Type? StructDescriptionResourceType => null;
    public static ReadOnlySpan<BitFieldInfo> Fields => new BitFieldInfo[]
    {
        new("Ready", 0, 1, "bool", true, ByteOrder.LittleEndian, BitOrder.BitZeroIsLsb, StructTotalBits: 8, ...),
        new("Error", 1, 1, "bool", true, ByteOrder.LittleEndian, BitOrder.BitZeroIsLsb, StructTotalBits: 8, ...),
        new("Mode",  2, 3, "byte", false, ByteOrder.LittleEndian, BitOrder.BitZeroIsLsb, StructTotalBits: 8, ...),
    };

    // ── Fluent With methods ─────────────────────────────────────
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StatusRegister WithReady(bool value) =>
        new(value ? (byte)(Value | 0x01) : (byte)(Value & 0xFE));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StatusRegister WithMode(byte value) =>
        new((byte)((Value & 0xE3) | (((byte)value << 2) & 0x1C)));

    // ── Operators (all AggressiveInlining) ──────────────────────
    public static StatusRegister operator ~(StatusRegister a) => new((byte)~a.Value);
    public static StatusRegister operator |(StatusRegister a, StatusRegister b) => new((byte)(a.Value | b.Value));
    public static StatusRegister operator &(StatusRegister a, StatusRegister b) => new((byte)(a.Value & b.Value));
    public static StatusRegister operator ^(StatusRegister a, StatusRegister b) => new((byte)(a.Value ^ b.Value));
    public static StatusRegister operator +(StatusRegister a, StatusRegister b) => new(unchecked((byte)(a.Value + b.Value)));
    public static StatusRegister operator -(StatusRegister a, StatusRegister b) => new(unchecked((byte)(a.Value - b.Value)));
    public static StatusRegister operator -(StatusRegister a) => new(unchecked((byte)(0 - a.Value)));
    // ... plus +, -, *, /, % with storage-type operand on either side

    // Small types return int so (bits >> n) & 1 works without casting
    public static int operator <<(StatusRegister a, int b) => a.Value << b;
    public static int operator >>(StatusRegister a, int b) => a.Value >> b;
    public static int operator >>>(StatusRegister a, int b) => a.Value >>> b;

    public static bool operator <(StatusRegister a, StatusRegister b) => a.Value < b.Value;
    public static bool operator >(StatusRegister a, StatusRegister b) => a.Value > b.Value;
    public static bool operator <=(StatusRegister a, StatusRegister b) => a.Value <= b.Value;
    public static bool operator >=(StatusRegister a, StatusRegister b) => a.Value >= b.Value;
    public static bool operator ==(StatusRegister a, StatusRegister b) => a.Value == b.Value;
    public static bool operator !=(StatusRegister a, StatusRegister b) => a.Value != b.Value;

    // ── Conversions ─────────────────────────────────────────────
    public static implicit operator byte(StatusRegister value) => value.Value;
    public static implicit operator StatusRegister(byte value) => new(value);
    public static implicit operator StatusRegister(int value) => new(unchecked((byte)value));

    // ── Span serialization ──────────────────────────────────────
    public StatusRegister(ReadOnlySpan<byte> bytes) { /* validates length, reads LE */ }
    public static StatusRegister ReadFrom(ReadOnlySpan<byte> bytes) => new(bytes);
    public void WriteTo(Span<byte> destination) { /* validates length, writes LE */ }
    public bool TryWriteTo(Span<byte> destination, out int bytesWritten) { /* ... */ }
    public byte[] ToByteArray() { /* ... */ }

    // ── Equality, hashing, formatting ───────────────────────────
    public override bool Equals(object? obj) => obj is StatusRegister other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => $"0x{Value:X}";

    // ── Interface implementations (IComparable, IEquatable, IParsable, etc.) ──
    // ── JSON converter (reads/writes as string) ──
}
```

### BitFieldsView Generated Code

For this view struct:

```csharp
[BitFieldsView(ByteOrder.BigEndian, BitOrder.BitZeroIsMsb)]
public partial record struct ByteFlagsView
{
    [BitFlag(0)] public partial bool MsbFlag { get; set; }
    [BitFlag(7)] public partial bool LsbFlag { get; set; }
    [BitField(1, 4)] public partial byte Middle { get; set; }
}
```

The generator creates:

```csharp
public partial record struct ByteFlagsView
{
    private readonly Memory<byte> _data;
    private readonly byte _bitOffset;

    public const int SizeInBytes = 1;
    public const int BitWidth = 8;

    // ── Constructors ────────────────────────────────────────────
    public ByteFlagsView(Memory<byte> data) { /* validates length */ _data = data; _bitOffset = 0; }
    public ByteFlagsView(byte[] data) : this(data.AsMemory()) { }
    public ByteFlagsView(byte[] data, int offset) : this(data.AsMemory(offset)) { }
    internal ByteFlagsView(Memory<byte> data, int bitOffset) { /* for nested views */ }

    public Memory<byte> Data => _data;

    // ── Property accessors (read/write directly to buffer) ──────
    // Fast path when bitOffset == 0, fallback path for sub-byte nesting.
    public partial byte Middle
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var s = _data.Span;
            if (_bitOffset == 0)
                return (byte)((s[0] >> 3) & 0x0F);
            // Fallback: BinaryPrimitives read with bit offset calculation
            int ep = 1 + _bitOffset;
            int bi = ep >> 3;
            int endInWindow = (ep + 3) - bi * 8;
            int sh = 16 - 1 - endInWindow;
            return (byte)((BinaryPrimitives.ReadUInt16BigEndian(s.Slice(bi)) >> sh) & 0x000F);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            var s = _data.Span;
            if (_bitOffset == 0)
            {
                s[0] = (byte)((s[0] & 0x87) | (((byte)value << 3) & 0x78));
            }
            else
            {
                // Fallback: read-modify-write via BinaryPrimitives
                int ep = 1 + _bitOffset;
                int bi = ep >> 3;
                int endInWindow = (ep + 3) - bi * 8;
                int sh = 16 - 1 - endInWindow;
                var slice = s.Slice(bi);
                ushort raw = BinaryPrimitives.ReadUInt16BigEndian(slice);
                ushort m = (ushort)(0x000F << sh);
                raw = (ushort)((raw & (ushort)~m) | (((ushort)value << sh) & m));
                BinaryPrimitives.WriteUInt16BigEndian(slice, raw);
            }
        }
    }

    public partial bool MsbFlag
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var s = _data.Span;
            if (_bitOffset == 0) return (s[0] & 0x80) != 0;
            int ep = 0 + _bitOffset;
            return (s[ep >> 3] & (1 << (7 - (ep & 7)))) != 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            var s = _data.Span;
            if (_bitOffset == 0)
            {
                s[0] = value ? (byte)(s[0] | 0x80) : (byte)(s[0] & 0x7F);
                return;
            }
            int ep = 0 + _bitOffset;
            int bi = ep >> 3;
            int m = 1 << (7 - (ep & 7));
            s[bi] = value ? (byte)(s[bi] | m) : (byte)(s[bi] & ~m);
        }
    }

    // ── Metadata ────────────────────────────────────────────────
    public static ReadOnlySpan<BitFieldInfo> Fields => new BitFieldInfo[] { /* ... */ };
}
```
