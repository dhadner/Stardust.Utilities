# Endian Types

Type-safe big-endian and little-endian integer types for .NET. Designed for network protocols, file formats, and hardware emulation where explicit control over byte order is required. These types provide a natural syntax for arithmetic and bitwise operations while ensuring correct endianness handling across platforms.

Since modern .NET implementations (Core, 5, 6, 7, 8, 9, 10) primarily target little-endian machines, the library enforces little-endian usage at runtime.  The only known exception to this is the IBM Z architecture (linux-s390x) used by large banks and government agencies, which is big-endian. If you need to run on big-endian hardware, you can compile the library with `#define BIG_ENDIAN` to enable big-endian support and disable the runtime check.


## Overview

Endian types store bytes in a guaranteed byte order regardless of the host platform. Big-endian types store the most significant byte first (network byte order), while little-endian types store the least significant byte first (x86 native order). These types provide:

- **Type safety** - Distinct types prevent accidentally mixing endianness
- **Natural syntax** - Full operator support for arithmetic and bitwise operations
- **Modern .NET APIs** - `Span<T>`, `IParsable<T>`, `ISpanFormattable` support
- **Zero-copy I/O** - Read/write directly from spans without allocation
- **PropertyGrid support** - TypeConverters for UI editing

## Available Types

### Big-Endian (Network Byte Order)

| Type | Size | Native Equivalent | Description |
|------|------|-------------------|-------------|
| `UInt16Be` | 2 bytes | `ushort` | Unsigned 16-bit |
| `Int16Be` | 2 bytes | `short` | Signed 16-bit |
| `UInt32Be` | 4 bytes | `uint` | Unsigned 32-bit |
| `Int32Be` | 4 bytes | `int` | Signed 32-bit |
| `UInt64Be` | 8 bytes | `ulong` | Unsigned 64-bit |
| `Int64Be` | 8 bytes | `long` | Signed 64-bit |
| `UInt128Be` | 16 bytes | `UInt128` | Unsigned 128-bit |
| `Int128Be` | 16 bytes | `Int128` | Signed 128-bit |
| `UInt256Be` | 32 bytes | `UInt256` | Unsigned 256-bit -- see [LARGE_INTEGERS.md](LARGE_INTEGERS.md) |
| `Int256Be` | 32 bytes | `Int256` | Signed 256-bit -- see [LARGE_INTEGERS.md](LARGE_INTEGERS.md) |

### Little-Endian

| Type | Size | Native Equivalent | Description |
|------|------|-------------------|-------------|
| `UInt16Le` | 2 bytes | `ushort` | Unsigned 16-bit |
| `Int16Le` | 2 bytes | `short` | Signed 16-bit |
| `UInt32Le` | 4 bytes | `uint` | Unsigned 32-bit |
| `Int32Le` | 4 bytes | `int` | Signed 32-bit |
| `UInt64Le` | 8 bytes | `ulong` | Unsigned 64-bit |
| `Int64Le` | 8 bytes | `long` | Signed 64-bit |
| `UInt128Le` | 16 bytes | `UInt128` | Unsigned 128-bit |
| `Int128Le` | 16 bytes | `Int128` | Signed 128-bit |
| `UInt256Le` | 32 bytes | `UInt256` | Unsigned 256-bit -- see [LARGE_INTEGERS.md](LARGE_INTEGERS.md) |
| `Int256Le` | 32 bytes | `Int256` | Signed 256-bit -- see [LARGE_INTEGERS.md](LARGE_INTEGERS.md) |

## Quick Start

```csharp
using Stardust.Utilities;

// Create from native values - bytes are automatically reordered
UInt32Be networkValue = 0x12345678;

// Stored in memory as: 0x12, 0x34, 0x56, 0x78 (big-endian order)
// On little-endian x86, native uint would be: 0x78, 0x56, 0x34, 0x12

// Convert back to native
uint nativeValue = networkValue;  // Implicit conversion

// Arithmetic works naturally
UInt32Be sum = networkValue + 100;
UInt32Be product = networkValue * 2;

// Serialize to network/file
Span<byte> buffer = stackalloc byte[4];
networkValue.WriteTo(buffer);
// buffer now contains: [0x12, 0x34, 0x56, 0x78]

// Deserialize from network/file
var restored = new UInt32Be(buffer);
```

## Construction

### From Native Values

```csharp
// Implicit conversion from native types
UInt16Be val16 = 0x1234;
UInt32Be val32 = 0x12345678U;
UInt64Be val64 = 0x123456789ABCDEF0UL;

Int16Be signed16 = -1000;
Int32Be signed32 = -1000000;
Int64Be signed64 = -1234567890123456789L;

// Explicit constructor
var explicit32 = new UInt32Be(0xDEADBEEF);
```

### From Byte Arrays

Every endian type has four `byte[]` constructors, matching the `ReadOnlySpan<byte>` ctors one-to-one. The `isBigEndian` parameter lets you feed a buffer of the opposite byte order and it will be flipped as it is stored.

```csharp
byte[] networkData = new byte[] { 0x12, 0x34, 0x56, 0x78 };

// Default -- reads in the type's own storage order (Be = big-endian, Le = little-endian)
var a = new UInt32Be(networkData);

// Override the source order without specifying an offset
var b = new UInt32Be(networkData, isBigEndian: false);  // reverses on the way in

// Offset into the buffer -- payload starts at networkData[offset]
var c = new UInt32Be(networkData, offset: 0);

// Offset + source order
byte[] packet = new byte[PAD + 4];
// ... fill packet ...
var d = new UInt32Be(packet, offset: PAD, isBigEndian: true);

// Static factory form (parallels ReadFrom on Span)
var e = UInt32Be.ReadFrom(networkData, offset: 0);
```

Prior releases also supported `IList<byte>` constructors. Those are marked `[Obsolete]` in 0.9.13 -- use `byte[]` or `ReadOnlySpan<byte>` instead.

### From Spans (Zero-Allocation)

```csharp
// From ReadOnlySpan<byte> - zero allocation
ReadOnlySpan<byte> packet = stackalloc byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
var header = new UInt32Be(packet);

// Override the source byte order
var reversed = new UInt32Be(packet, isBigEndian: false);

// Read from an offset inside a larger span
var offsetValue = new UInt32Be(packet, offset: 0, isBigEndian: true);

// Static factory method (same offset / isBigEndian overloads)
var value = UInt32Be.ReadFrom(packet);
var atOffset = UInt32Be.ReadFrom(packet, offset: 0, isBigEndian: false);

// Slice for multiple values
var first = new UInt16Be(packet[..2]);
var second = new UInt16Be(packet[2..4]);
```

## Serialization

### To Byte Arrays

`WriteTo` and `TryWriteTo` work on both `Span<byte>` and `byte[]` with the same `(destination, int offset = 0, bool isBigEndian = X)` shape, so a caller with a `byte[]` no longer needs `.AsSpan()` to reach the offset / byte-order overloads.

```csharp
UInt64Be value = 0x0102030405060708UL;

// byte[] overload -- default order
byte[] buffer = new byte[8];
value.WriteTo(buffer);
// buffer: [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]

// Offset into a larger buffer
byte[] frame = new byte[16];
value.WriteTo(frame, offset: 8);

// Flip byte order at the destination
value.WriteTo(buffer, isBigEndian: false);
// buffer: [0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01]

// Static optimized write for a native value (uses BinaryPrimitives internally)
UInt64Be.WriteTo(0x0102030405060708UL, buffer);
```

> **Deprecated in 0.9.13.** The instance `ToBytes(byte[], int offset = 0, bool isBigEndian = X)` method is marked `[Obsolete]` on every endian type. Replace `v.ToBytes(buf, offset, isBigEndian)` with `v.WriteTo(buf, offset, isBigEndian)` -- the signatures and semantics are identical.

### To Spans (Zero-Allocation)

```csharp
UInt32Be value = 0x12345678;

// WriteTo - throws if span too small
Span<byte> buffer = stackalloc byte[4];
value.WriteTo(buffer);

// WriteTo at an offset inside a larger span
Span<byte> frame = stackalloc byte[16];
value.WriteTo(frame, offset: 4);

// Flip byte order at the destination
value.WriteTo(buffer, isBigEndian: false);

// TryWriteTo - returns false if the span is too short from the given offset
Span<byte> smallBuffer = stackalloc byte[2];
bool ok = value.TryWriteTo(smallBuffer);                // false
bool ok2 = value.TryWriteTo(frame, offset: 13);         // false (needs 4 bytes, only 3 left)
bool ok3 = value.TryWriteTo(frame, offset: 12);         // true

// Static optimized write (uses BinaryPrimitives internally)
UInt32Be.WriteTo(0x12345678U, buffer);
```

## Operators

All types support the full range of operators:

### Arithmetic

```csharp
UInt32Be a = 100;
UInt32Be b = 50;

UInt32Be sum = a + b;        // 150
UInt32Be diff = a - b;       // 50
UInt32Be product = a * b;    // 5000
UInt32Be quotient = a / b;   // 2
UInt32Be remainder = a % b;  // 0

UInt32Be negated = -a;       // Two's complement negation
UInt32Be incremented = ++a;  // 101
UInt32Be decremented = --b;  // 49
```

### Bitwise

```csharp
UInt32Be x = 0xFF00FF00;
UInt32Be y = 0x00FF00FF;

UInt32Be andResult = x & y;   // 0x00000000
UInt32Be orResult = x | y;    // 0xFFFFFFFF
UInt32Be xorResult = x ^ y;   // 0xFFFFFFFF
UInt32Be notResult = ~x;      // 0x00FF00FF

// Shift operators
UInt32Be shifted = x >> 8;    // 0x00FF00FF
UInt32Be leftShift = y << 8;  // 0xFF00FF00
```

### Comparison

```csharp
UInt32Be a = 100;
UInt32Be b = 200;

bool less = a < b;       // true
bool greater = a > b;    // false
bool lessEq = a <= b;    // true
bool greaterEq = a >= b; // false
bool equal = a == b;     // false
bool notEqual = a != b;  // true
```

### Signed Comparisons

```csharp
Int32Be positive = 100;
Int32Be negative = -100;

// Signed comparison respects sign
bool result = negative < positive;  // true (correct signed comparison)
```

## Type Conversions

### Implicit Conversions (Safe, No Data Loss)

```csharp
// Native to big-endian
UInt16Be val16 = (ushort)0x1234;
UInt32Be val32 = 0x12345678U;

// Widening conversions (Big-Endian)
UInt64Be wide = val32;           // UInt32Be -> UInt64Be
UInt64Be fromSmall = val16;      // UInt16Be -> UInt64Be

// Signed widening (sign-extends correctly)
Int16Be small = -100;
Int64Be large = small;           // Still -100, sign-extended

// Widening conversions (Little-Endian) -- symmetrical with Big-Endian
UInt16Le le16 = 0x1234;
UInt32Le le32 = le16;            // UInt16Le -> UInt32Le
UInt64Le le64 = le32;            // UInt32Le -> UInt64Le
UInt64Le fromSmallLe = le16;     // UInt16Le -> UInt64Le
UInt128Le le128 = le64;          // UInt64Le -> UInt128Le

Int16Le sle16 = -100;
Int64Le sle64 = sle16;           // Int16Le -> Int64Le (sign-extends)
Int128Le sle128 = sle64;         // Int64Le -> Int128Le

// Big-endian to native
ushort native16 = val16;
uint native32 = val32;
ulong native64 = (UInt64Be)val32;
```

### Explicit Conversions (May Truncate)

```csharp
UInt64Be big = 0x123456789ABCDEF0UL;

// Narrowing Big-Endian - takes low bytes
UInt32Be truncated32 = (UInt32Be)big;  // 0x9ABCDEF0
UInt16Be truncated16 = (UInt16Be)big;  // 0xDEF0
byte truncatedByte = (byte)big;         // 0xF0

// Narrowing Little-Endian -- symmetrical with Big-Endian
UInt64Le bigLe = 0x123456789ABCDEF0UL;
UInt32Le trunc32Le = (UInt32Le)bigLe;  // 0x9ABCDEF0
UInt16Le trunc16Le = (UInt16Le)bigLe;  // 0xDEF0

// 128-bit narrowing
UInt128Le big128 = new((UInt128)0xDEADBEEF);
UInt64Le from128 = (UInt64Le)big128;
UInt32Le from128_32 = (UInt32Le)big128;
```

## Hi/Lo Extension Methods

Extract or replace the high/low halves of values:

### Hi() - Get Upper Half

```csharp
// 16-bit types -> byte
UInt16Be val16 = 0x1234;
byte hi16 = val16.Hi();  // 0x12

// 32-bit types -> UInt16Be / ushort
UInt32Be val32 = 0x12345678;
UInt16Be hi32 = val32.Hi();  // 0x1234

// 64-bit types -> UInt32Be / uint
UInt64Be val64 = 0x123456789ABCDEF0UL;
UInt32Be hi64 = val64.Hi();  // 0x12345678

// 128-bit types -> UInt64Be / ulong
UInt128Be val128 = new(((UInt128)0x0123456789ABCDEFUL << 64) | 0xFEDCBA9876543210UL);
UInt64Be hi128 = val128.Hi();  // 0x0123456789ABCDEF

// Native types work too
ulong native = 0x123456789ABCDEF0UL;
uint nativeHi = native.Hi();  // 0x12345678

UInt128 native128 = ((UInt128)0x0123456789ABCDEFUL << 64) | 0xFEDCBA9876543210UL;
ulong nativeHi128 = native128.Hi();  // 0x0123456789ABCDEF

// Little-endian types follow the same pattern
UInt16Le le16 = 0xABCD;
byte hiLe = le16.Hi();  // 0xAB

UInt64Le le64 = 0x123456789ABCDEF0UL;
UInt32Le hiLe64 = le64.Hi();  // 0x12345678
```

### Lo() - Get Lower Half

```csharp
// 16-bit types -> byte
UInt16Be val16 = 0x1234;
byte lo16 = val16.Lo();  // 0x34

// 32-bit types -> UInt16Be / ushort
UInt32Be val32 = 0x12345678;
UInt16Be lo32 = val32.Lo();  // 0x5678

// 64-bit types -> UInt32Be / uint
UInt64Be val64 = 0x123456789ABCDEF0UL;
UInt32Be lo64 = val64.Lo();  // 0x9ABCDEF0

// 128-bit types -> UInt64Be / ulong
UInt128Be val128 = new(((UInt128)0x0123456789ABCDEFUL << 64) | 0xFEDCBA9876543210UL);
UInt64Be lo128 = val128.Lo();  // 0xFEDCBA9876543210

// Native types work too
ulong native = 0x123456789ABCDEF0UL;
uint nativeLo = native.Lo();  // 0x9ABCDEF0

UInt128 native128 = ((UInt128)0x0123456789ABCDEFUL << 64) | 0xFEDCBA9876543210UL;
ulong nativeLo128 = native128.Lo();  // 0xFEDCBA9876543210
```

### SetHi() / SetLo() - Replace Half

```csharp
// Set upper half
UInt64Be value = 0x123456789ABCDEF0UL;
UInt64Be newHi = value.SetHi((UInt32Be)0xDEADBEEFU);  // 0xDEADBEEF9ABCDEF0

// Set lower half
UInt64Be newLo = value.SetLo((UInt32Be)0xCAFEBABEU);  // 0x12345678CAFEBABE

// 128-bit types
UInt128Be big128 = new((UInt128)0);
UInt128Be with128Hi = big128.SetHi((UInt64Be)0x0000000000000001UL);

// Works with native types too
ulong nativeVal = 0x123456789ABCDEF0UL;
ulong withNewHi = nativeVal.SetHi(0xDEADBEEFU);  // 0xDEADBEEF9ABCDEF0
ulong withNewLo = nativeVal.SetLo(0xCAFEBABEU);  // 0x12345678CAFEBABE

UInt128 native128val = (UInt128)0;
UInt128 withNative128Hi = native128val.SetHi(0x0000000000000001UL);  // 1 << 64

// Little-endian SetHi/SetLo
UInt64Le leval = 0x123456789ABCDEF0UL;
UInt64Le leNewHi = leval.SetHi((UInt32Le)0xDEADBEEFU);  // 0xDEADBEEF9ABCDEF0
```

## Parsing and Formatting

### Parsing

```csharp
// Basic parsing
UInt32Be decimal = UInt32Be.Parse("12345678");
UInt32Be hex = UInt32Be.Parse("DEADBEEF", NumberStyles.HexNumber);
UInt32Be hexWithPrefix = UInt32Be.Parse("0xDEADBEEF".Replace("0x", ""), NumberStyles.HexNumber);

// With format provider (IParsable<T>)
UInt64Be value = UInt64Be.Parse("18446744073709551615", CultureInfo.InvariantCulture);

// Span-based parsing (ISpanParsable<T>)
ReadOnlySpan<char> text = "12345678";
UInt32Be fromSpan = UInt32Be.Parse(text, CultureInfo.InvariantCulture);

// TryParse
if (UInt32Be.TryParse("invalid", null, out var result))
{
    // Won't reach here
}

// TryParse with span
if (UInt64Be.TryParse("9876543210".AsSpan(), null, out var spanResult))
{
    Console.WriteLine(spanResult);
}
```

### Formatting

```csharp
UInt32Be value = 0xDEADBEEF;

// Default ToString (hex with 0x prefix)
string defaultStr = value.ToString();  // "0xdeadbeef"

// IFormattable - custom formats
string hexUpper = value.ToString("X8", null);   // "DEADBEEF"
string decimalStr = value.ToString("D", null);  // "3735928559"
string grouped = value.ToString("N0", CultureInfo.InvariantCulture);  // "3,735,928,559"

// ISpanFormattable - allocation-free formatting
Span<char> buffer = stackalloc char[16];
if (value.TryFormat(buffer, out int written, "X8", null))
{
    string result = new string(buffer[..written]);  // "DEADBEEF"
}
```

## Interface Support

All endian types implement:

| Interface | Description |
|-----------|-------------|
| `IComparable` | Non-generic comparison |
| `IComparable<T>` | Generic comparison |
| `IEquatable<T>` | Equality comparison |
| `IFormattable` | String formatting with format/provider |
| `ISpanFormattable` | Allocation-free span formatting |
| `IParsable<T>` | Static parsing with provider |
| `ISpanParsable<T>` | Static span-based parsing |

## Real-World Examples

### Network Protocol Header

```csharp
public ref struct PacketHeader
{
    public UInt16Be Version;
    public UInt16Be Length;
    public UInt32Be SequenceNumber;
    public UInt64Be Timestamp;
    
    public static PacketHeader Read(ReadOnlySpan<byte> data)
    {
        return new PacketHeader
        {
            Version = new UInt16Be(data[0..2]),
            Length = new UInt16Be(data[2..4]),
            SequenceNumber = new UInt32Be(data[4..8]),
            Timestamp = new UInt64Be(data[8..16])
        };
    }
    
    public void Write(Span<byte> destination)
    {
        Version.WriteTo(destination[0..2]);
        Length.WriteTo(destination[2..4]);
        SequenceNumber.WriteTo(destination[4..8]);
        Timestamp.WriteTo(destination[8..16]);
    }
}

// Usage
Span<byte> packet = stackalloc byte[16];
var header = new PacketHeader
{
    Version = 1,
    Length = 100,
    SequenceNumber = 12345,
    Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
};
header.Write(packet);
```

### Binary File Format

```csharp
public class BinaryFileReader
{
    private readonly Stream _stream;
    private readonly byte[] _buffer = new byte[8];
    
    public UInt32Be ReadUInt32Be()
    {
        _stream.ReadExactly(_buffer, 0, 4);
        return new UInt32Be(_buffer, 0);
    }
    
    public UInt64Be ReadUInt64Be()
    {
        _stream.ReadExactly(_buffer, 0, 8);
        return new UInt64Be(_buffer, 0);
    }
}

// Reading a file header
using var reader = new BinaryFileReader(fileStream);
UInt32Be magic = reader.ReadUInt32Be();
if (magic != 0x89504E47)  // PNG magic number
{
    throw new InvalidDataException("Not a PNG file");
}
```

### Hardware Register (with BitFields)

Big-endian types can be used as storage types in `[BitFields]` structs, and as per-field
endian overrides in record struct views. See [BITFIELDS.md](BITFIELDS.md) for full
documentation on composition and mixed-endian nesting.

```csharp
// Combining Big-Endian with BitFields for hardware emulation
[BitFields(typeof(UInt32Be))]
public partial struct StatusRegister
{
    [BitField(0, End = 8)] public partial byte ErrorCode { get; set; }
    [BitField(8, End = 16)] public partial ushort DataLength { get; set; }
    [BitFlag(24)] public partial bool Ready { get; set; }
    [BitFlag(25)] public partial bool Error { get; set; }
    [BitFlag(31)] public partial bool Valid { get; set; }
}

// Usage
Span<byte> mmioRegion = GetMemoryMappedRegion();
StatusRegister status = new StatusRegister(UInt32Be.ReadFrom(mmioRegion));
if (status.Ready && !status.Error)
{
    ushort length = status.DataLength;
    // Process data...
}
```

Big-endian types can also override individual field endianness within a record struct view.
This is useful when a single struct mixes byte orders at the field level:

```csharp
// x86 file blob (little-endian default) with one big-endian IP address field
[BitFields]
public partial record struct FileBlobView
{
    [BitField(0, End = 31)]  public partial uint Timestamp { get; set; }          // LE (struct default)
    [BitField(32, End = 63)] public partial UInt32Be NetworkIp { get; set; }      // BE (per-field override)
}
```

For uniform-endian structs, per-field overrides are unnecessary -- just set
`ByteOrder.BigEndian` on the `[BitFields]` attribute and use plain `uint`, `ushort`, etc.

### Zero-Allocation Network I/O

```csharp
public class NetworkHandler
{
    public void ProcessPacket(ReadOnlySpan<byte> packet)
    {
        // Parse header without any allocation
        var version = UInt16Be.ReadFrom(packet);
        var length = UInt16Be.ReadFrom(packet[2..]);
        var checksum = UInt32Be.ReadFrom(packet[4..]);
        
        // Validate
        if (version != 1 || length > packet.Length)
        {
            return;
        }
        
        // Process payload
        ReadOnlySpan<byte> payload = packet[8..(int)(ushort)length];
        ProcessPayload(payload);
    }
    
    public int BuildResponse(Span<byte> buffer, ushort status, uint sequenceId)
    {
        // Write response header without allocation
        ((UInt16Be)1).WriteTo(buffer);              // Version
        ((UInt16Be)12).WriteTo(buffer[2..]);        // Length  
        ((UInt32Be)status).WriteTo(buffer[4..]);    // Status
        ((UInt32Be)sequenceId).WriteTo(buffer[8..]); // Sequence
        return 12;
    }
}
```

## TypeConverters

Both big-endian and little-endian types have identical feature parity: the same operators,
interfaces, span APIs, and `TypeConverter` support. Each type has a `TypeConverter` for
WinForms PropertyGrid and similar UI scenarios:

| Type | TypeConverter |
|------|---------------|
| `UInt16Be` | `UInt16BeTypeConverter` |
| `Int16Be` | `Int16BeTypeConverter` |
| `UInt32Be` | `UInt32BeTypeConverter` |
| `Int32Be` | `Int32BeTypeConverter` |
| `UInt64Be` | `UInt64BeTypeConverter` |
| `Int64Be` | `Int64BeTypeConverter` |
| `UInt16Le` | `UInt16LeTypeConverter` |
| `Int16Le` | `Int16LeTypeConverter` |
| `UInt32Le` | `UInt32LeTypeConverter` |
| `Int32Le` | `Int32LeTypeConverter` |
| `UInt64Le` | `UInt64LeTypeConverter` |
| `Int64Le` | `Int64LeTypeConverter` |
| `UInt128Be` | `UInt128BeTypeConverter` |
| `Int128Be` | `Int128BeTypeConverter` |
| `UInt128Le` | `UInt128LeTypeConverter` |
| `Int128Le` | `Int128LeTypeConverter` |
| `UInt256Be` | `UInt256BeTypeConverter` |
| `Int256Be` | `Int256BeTypeConverter` |
| `UInt256Le` | `UInt256LeTypeConverter` |
| `Int256Le` | `Int256LeTypeConverter` |

```csharp
// TypeConverters support hex input with 0x prefix
var converter = new UInt32BeTypeConverter();
object? result = converter.ConvertFrom(null, null, "0xDEADBEEF");
// result is UInt32Be with value 0xDEADBEEF
```

The host-native `UInt256` and `Int256` types have matching converters
(`UInt256TypeConverter`, `Int256TypeConverter`) that share the same input grammar
but emit **decimal** strings for output (matching `System.ComponentModel.Int32Converter`
and the other BCL numeric converters); the endian wrappers emit `0x` + 64-hex because
their `ToString()` does the same. See
[LARGE_INTEGERS.md — TypeConverters](LARGE_INTEGERS.md#typeconverters).

## Performance Notes

- All types use `[StructLayout(LayoutKind.Explicit)]` for guaranteed memory layout
- Hot-path methods are marked with `[MethodImpl(MethodImplOptions.AggressiveInlining)]`
- Static `WriteTo()` methods use `BinaryPrimitives` for optimized byte swapping
- Span-based APIs enable zero-allocation I/O patterns
- Implicit conversions compile to simple register operations

## Migration from Manual Byte Swapping

Before (manual approach):
```csharp
// Writing
uint value = 0x12345678;
buffer[0] = (byte)(value >> 24);
buffer[1] = (byte)(value >> 16);
buffer[2] = (byte)(value >> 8);
buffer[3] = (byte)value;

// Reading
uint result = (uint)buffer[0] << 24 
            | (uint)buffer[1] << 16 
            | (uint)buffer[2] << 8 
            | buffer[3];
```

After (with UInt32Be):
```csharp
// Writing
UInt32Be value = 0x12345678;
value.WriteTo(buffer);

// Reading
UInt32Be result = new UInt32Be(buffer);
```

Both produce identical machine code, but the big-endian type version:
- Is more readable
- Is type-safe (can't accidentally use wrong endianness)
- Catches size mismatches at compile time
- Provides consistent API across 16/32/64-bit sizes

## Design Decisions

### When to Use Endian Types

**Big-endian types (`*Be`)** are for data that must be stored most-significant-byte first:
- Network protocols (TCP/IP uses "network byte order" = big-endian)
- Many file formats (PNG, JPEG, Java class files, etc.)
- Big-endian hardware emulation (68000, PowerPC, SPARC, MIPS32/64 (BE), ARM (BE-8 mode))

**Little-endian types (`*Le`)** are for data that must be stored least-significant-byte first:
- Per-field endian overrides in a `[BitFields]` struct whose default is big-endian
- Cross-platform binary formats that mandate little-endian storage
- Big-endian host machines that need to read/write x86-native data

On mainstream platforms (x86, x64, ARM), native types like `uint` are already little-endian
in memory. In most code you can use plain native types and only reach for `*Le` when you need
a type-safe marker or a per-field override inside a BE `[BitFields]` struct.

### Historical Context
1. **Mono Runtime (Cross-Platform .NET Implementation)**<br><br>
  Mono was designed to be portable and has been compiled for several big-endian architectures.

    |Platform / Device	|CPU Architecture	|Endianness	|Notes |
    |:------------------|:------------------|:--------------|:------------------|
    |PowerPC (PPC) (older Macs, Linux PPC)	| PowerPC 32/64-bit	| Big-endian	| Early Mono supported Mac OS X PPC and Linux PPC. |
    |PlayStation 3	| Cell Broadband Engine (PPE core = PowerPC)	| Big-endian	| Unity games on PS3 used Mono in big-endian mode. |
    |Nintendo Wii	| PowerPC 750CL	| Big-endian	| Mono was ported unofficially for homebrew. |
    |Nintendo GameCube	| PowerPC Gekko	| Big-endian	| Experimental Mono builds existed. |
    |SPARC (Solaris, Linux)	| SPARC V8/V9	| Big-endian	| Mono had experimental SPARC support. |
    |MIPS (BE mode)	| MIPS32/64	| Big-endian	| Used in some embedded devices and routers. |

2. **.NET Compact Framework (CF)**<br><br>
  The .NET Compact Framework (for Windows CE / Windows Mobile) ran on multiple CPU types, including big-endian ones.

    |Platform / Device	|CPU Architecture	|Endianness	|Notes |
    |:------------------|:------------------|:--------------|:------------------|
    |MIPS (BE)	| MIPS32	| Big-endian	| Some Windows CE devices used big-endian MIPS.
    | SH-4	| Hitachi SuperH-4	| Big-endian	| Used in some industrial controllers and set-top boxes. |
    | ARM (BE-8 mode)	| ARMv5/v6	| Mixed-endian (big-endian data)	| Rare; some specialized CE devices used this mode. |

3. **Unity Engine (Mono-based)**<br><br>
  Unity’s scripting backend was Mono for many years, so any Unity build targeting a big-endian console inherited Mono’s endianness.

    |Platform / Device	|CPU Architecture	|Endianness	|Notes |
    |:------------------|:------------------|:--------------|:------------------|
    | PS3	| PowerPC	| Big-endian	| Widely used in AAA games. |
    | Wii	| PowerPC	| Big-endian	| Unity 4.x supported Wii. |        
4. **Experimental / Research Ports**<br><br>
  Mono on IBM System z (s390x) → Big-endian mainframe architecture.<br>
  Mono on HP-UX PA-RISC → Big-endian RISC CPU.<br>
  Custom embedded boards → Some ARM and MIPS boards in big-endian mode.

5. **Modern Status (as of 2026)**<br><br>
  Microsoft .NET (Core / 5 / 6 / 7 / 8) → Only little-endian officially.<br>
  Mono → Still can be compiled for big-endian, but most active builds are little-endian.<br>
  Unity → Dropped support for big-endian consoles after PS3/Wii era.

### Memory Layout

The types use `[StructLayout(LayoutKind.Explicit)]` with explicit `[FieldOffset]` attributes to guarantee byte ordering:

```csharp
[StructLayout(LayoutKind.Explicit, Size = 4)]
public struct UInt32Be
{
    [FieldOffset(0)] internal UInt16Be hi;  // Most significant bytes
    [FieldOffset(2)] internal UInt16Be lo;  // Least significant bytes
}
```

This ensures:
- Predictable memory layout regardless of compiler/runtime optimizations
- Direct byte access via `hi` and `lo` fields
- Correct behavior when casting to/from `Span<byte>`

### Endianness-Agnostic Implementation

The byte extraction uses bit shifting rather than pointer casting, making the implementation correct on both little-endian and big-endian machines:

```csharp
// This works correctly regardless of machine endianness
public UInt16Be(ushort num)
{
    hi = (byte)(num >> 8);   // Always extracts the high byte
    lo = (byte)(num & 0xff); // Always extracts the low byte
}
```

The JIT compiler optimizes this to efficient native instructions on all platforms.

---

## Performance

Endian types guarantee byte layout while operating at or near native speed on little-endian hardware (x86/x64/ARM64). The implementation uses two complementary techniques:

- **16-bit types:** an overlapping `[FieldOffset(0)] ushort _value` aliased with the `byte hi`/`byte lo` fields. The JIT keeps the value in a single CPU register while preserving direct byte access.
- **32–128-bit types:** `Unsafe.As` reinterpretation. On LE hardware the struct memory IS the native value (Le: zero-cost identity, Be: single `BSWAP` instruction).
- **256-bit Le types:** `Unsafe.As` reinterpretation, identical to 128-bit Le. On LE hardware the struct bytes ARE the native `UInt256`/`Int256` value -- zero-cost identity conversion.
- **256-bit Be types:** On BE hardware, direct `Unsafe.As<UInt256Be, ulong>` field writes/reads (no BSWAPs, no span). On LE hardware, `BinaryPrimitives.WriteUInt64BigEndian/ReadUInt64BigEndian × 4` with a single `BSWAP` per 64-bit word. `WriteTo(Span<byte>)` and span constructors are a single 32-byte `MemoryCopy`. Equality and bitwise ops (`&`, `|`, `^`, `~`) use raw 4-ulong comparison/mutation with no BSWAPs.

**Bitwise ops are BSWAP-free.** Because `BSWAP(a) OP BSWAP(b) = BSWAP(a OP b)` holds for AND, OR, XOR, and NOT, all Be types implement these operators by working directly on the stored (already-swapped) byte pattern -- no conversion to native and back. The `&`, `|`, `^`, and `~` operators therefore compile to the same machine instructions as their native equivalents: a single `AND`/`OR`/`XOR`/`NOT` for 32/64-bit, two for 128-bit, four for 256-bit.

The table below shows measured cost per operation (BenchmarkDotNet, .NET 10, Release, x64, 10 million iterations; total elapsed ÷ iteration count). **The benchmarks measure a full read-operate-write cycle**: each iteration also converts the accumulator and advances the counter (requiring boundary conversions), so the overhead shown for Be types reflects the mandatory `BSWAP` at the I/O boundary, not hidden overhead inside the operator itself.

### Cost by Width and Operation

| Width | Operation | Native | Big-Endian | Be/Native | Little-Endian | Le/Native |
|-------|-----------|--------|------------|-----------|---------------|-----------|
| 16-bit | Add     | 0.65 ns | 1.90 ns | 2.9x | **0.64 ns** | **1.0x** |
| 16-bit | Sub     | 0.76 ns | 1.91 ns | 2.5x | **0.65 ns** | **0.9x** |
| 16-bit | AND     | 0.65 ns | 1.82 ns | 2.8x | **0.65 ns** | **1.0x** |
| 16-bit | OR      | 0.77 ns | 1.81 ns | 2.4x | **0.76 ns** | **1.0x** |
| 16-bit | XOR     | 0.77 ns | 1.80 ns | 2.3x | **0.76 ns** | **1.0x** |
| 16-bit | Compare | 0.80 ns | 1.75 ns | 2.2x | **0.79 ns** | **1.0x** |
| 16-bit | Hi/Lo   | 0.89 ns | 2.00 ns | 2.2x | **0.62 ns** | **0.7x** |
| 32-bit | Add     | 0.39 ns | 1.07 ns | 2.7x | **0.39 ns** | **1.0x** |
| 32-bit | AND     | 0.43 ns | 1.01 ns | 2.4x | **0.44 ns** | **1.0x** |
| 32-bit | OR      | 0.43 ns | 1.02 ns | 2.4x | **0.43 ns** | **1.0x** |
| 32-bit | XOR     | 0.43 ns | 1.02 ns | 2.4x | **0.42 ns** | **1.0x** |
| 64-bit | Add     | 0.51 ns | 1.72 ns | 3.4x | **0.46 ns** | **0.9x** |
| 64-bit | AND     | 0.46 ns | 1.53 ns | 3.3x | **0.46 ns** | **1.0x** |
| 64-bit | OR      | 0.45 ns | 1.51 ns | 3.4x | **0.44 ns** | **1.0x** |
| 64-bit | XOR     | 0.44 ns | 1.50 ns | 3.4x | **0.44 ns** | **1.0x** |
| 128-bit | Add    | 0.86 ns | 2.62 ns | 3.0x | **0.82 ns** | **1.0x** |
| 128-bit | AND    | 0.72 ns | 2.32 ns | 3.2x | **0.71 ns** | **1.0x** |
| 128-bit | OR     | 0.72 ns | 2.28 ns | 3.2x | **0.73 ns** | **1.0x** |
| 128-bit | XOR    | 0.71 ns | 2.33 ns | 3.3x | **0.71 ns** | **1.0x** |
| 256-bit | Add    | 1.26 ns | 23.60 ns | 18.7x | **1.26 ns** | **1.0x** |
| 256-bit | AND    | 1.27 ns |  8.74 ns |  6.9x | **1.29 ns** | **1.0x** |
| 256-bit | OR     | 1.27 ns | 10.75 ns |  8.5x | **1.31 ns** | **1.0x** |
| 256-bit | XOR    | 1.27 ns |  8.77 ns |  6.9x | **1.27 ns** | **1.0x** |
| 256-bit | RoundTrip¹ | — | **13.72 ns** | — | **8.74 ns** | — |

¹ RoundTrip = `new XxxBe(span)` + `→ UInt256` + `WriteTo(span)`: the typical I/O boundary pattern. Uses 1M iterations (the extra work per iteration is ~10× heavier than a single arithmetic op). Be cost is dominated by the 32-byte memory copy plus 4 BSWAPs; Le cost is lower because the Le→UInt256 conversion is a zero-copy `Unsafe.As` with no BSWAPs.

**Why Be bitwise ops still show overhead in the benchmark.** Even though `&`, `|`, `^` are BSWAP-free inside the operator, each benchmark iteration must also (a) convert the result to native to accumulate it, and (b) convert to native to add 1 to the counter, then convert back. Those boundary conversions are measured too. The 256-bit numbers make this visible: Be-AND at 8.74 ns/op vs Be-Add at 23.60 ns/op -- AND is 2.7× cheaper than Add, because Add performs BSWAPs inside the operator while AND does not. **256-bit Le types are now at full native parity (1.0x)**: the `Unsafe.As` zero-copy optimization eliminates all boundary conversion overhead, so the loop counter and accumulator updates are as cheap as for `UInt256` itself.

Run the benchmark suite on your own hardware:

```powershell
dotnet run -c Release --project BenchmarkSuite1 -- --filter '*EndianOverhead*'
```

### Summary by Width

| Width | Le vs. Native | Be vs. Hand-Coded BSWAP | Notes |
|-------|---------------|-------------------------|-------|
| 16-bit | **1.0x** | **1.0x** (optimal) | Le: identity; Be: single register rotate |
| 32-bit | **1.0x** | **1.0x** (optimal) | Le: identity reinterpret; Be: single `BSWAP` |
| 64-bit | **1.0x** | **1.0x** (optimal) | Le: identity reinterpret; Be: single `BSWAP` |
| 128-bit | **1.0x** | **1.0x** (optimal) | Le: cascades from 64-bit; Be: two BSWAPs + swap halves |
| 256-bit | **1.0x** | see note | Le: `Unsafe.As` zero-copy (same as 128-bit); Be: 4×BSWAP at boundary; I/O: 32-byte span copy |

### Key Takeaways

- **Le types are zero-cost on LE hardware.** `*Le` types are identity operations on x86/x64/ARM64 -- the struct bytes ARE the native value. All operations compile to the same machine code as plain `uint`, `ulong`, etc.
- **Be types add only the inherent BSWAP cost.** `*Be` types compile to the same `BSWAP` instruction a developer would hand-write. The type abstraction adds no overhead beyond the byte-swap itself.
- **Bitwise AND/OR/XOR/NOT are BSWAP-free on all Be types.** These operators work directly on the stored byte pattern (using `Unsafe.As` reinterpretation or direct field access) without any conversion to/from the native byte order. The operation itself compiles to bare `AND`/`OR`/`XOR`/`NOT` instructions. The overhead shown in the table comes from the benchmark's mandatory boundary conversions, not from the operators.
- **16-bit types use overlapping field aliasing** -- a `_value` field at offset 0 overlaps the `hi`/`lo` byte fields, giving the JIT a single primitive to keep in registers while preserving byte access.
- **32–256-bit Le types use `Unsafe.As`** for zero-cost reinterpretation on LE hardware -- the struct bytes ARE the native value. Be types compile to single or cascaded `BSWAP` instructions (one per 64-bit word).
- **256-bit Be types** use direct `Unsafe.As<UInt256Be, ulong>` field access on BE hardware (no BSWAPs, no span), or `BinaryPrimitives × 4` with BSWAPs on LE hardware. `WriteTo` and span constructors are single 32-byte memory copies. Equality compares 4 raw `ulong` slots with no BSWAPs. Bitwise ops mutate those same 4 raw slots directly.
- **Zero heap allocation** across all types and operations.
- **For Be hot paths** that do many operations between conversions, the convert-at-boundary pattern can amortize the BSWAP cost:

```csharp
// Optional optimization for Be types in hot loops:
UInt32Be header = ReadFromNetwork();
uint native = (uint)header;            // BSWAP once
native = DoComplexMath(native);        // Compute in native (no BSWAPs)
UInt32Be result = (UInt32Be)native;    // BSWAP once
```

This pattern applies equally to 256-bit types -- convert once at the boundary, do all arithmetic in `UInt256` / `Int256`, convert back:

```csharp
// 256-bit boundary pattern:
ReadOnlySpan<byte> wire = GetBytesFromNetwork();  // 32 bytes, big-endian
UInt256Be be = new(wire);                          // single 32-byte copy
UInt256 value = be;                                // 4 BSWAPs
UInt256 result = value * 3 + 1;                    // compute in native
((UInt256Be)result).WriteTo(outBuf);               // 4 BSWAPs + 32-byte copy
```

For bitwise-only loops on Be types, no conversion is needed at all -- operate directly:

```csharp
// Bitwise ops on Be types: zero BSWAP overhead
UInt256Be mask = new UInt256Be(GetMaskFromNetwork());
UInt256Be data = new UInt256Be(GetDataFromNetwork());
UInt256Be masked = data & mask;   // 4 AND instructions, no BSWAPs
UInt256Be combined = masked | extra;   // 4 OR instructions, no BSWAPs
masked.WriteTo(outBuf);           // single 32-byte copy
```

For Le types, no such optimization is needed -- they are already at native speed:

```csharp
// Le types: compute directly -- zero overhead on LE hardware
UInt32Le a = ReadFromBuffer();
UInt32Le b = ReadFromBuffer();
UInt32Le sum = a + b;  // Same machine code as uint + uint
```

---

## See Also

- [BITFIELDS.md](BITFIELDS.md) - Source generator for bit field structs and buffer views
- [EXTENSIONS.md](EXTENSIONS.md) - Hi/Lo byte extraction and saturating arithmetic
- [OPTION.md](OPTION.md) - Optional values without null
- [RESULT.md](RESULT.md) - Railway-oriented error handling
- [README.md](README.md) - Package overview
