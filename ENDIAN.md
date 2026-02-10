# Endian Types

Type-safe big-endian and little-endian integer types for .NET.

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

### Little-Endian

| Type | Size | Native Equivalent | Description |
|------|------|-------------------|-------------|
| `UInt16Le` | 2 bytes | `ushort` | Unsigned 16-bit |
| `Int16Le` | 2 bytes | `short` | Signed 16-bit |
| `UInt32Le` | 4 bytes | `uint` | Unsigned 32-bit |
| `Int32Le` | 4 bytes | `int` | Signed 32-bit |
| `UInt64Le` | 8 bytes | `ulong` | Unsigned 64-bit |
| `Int64Le` | 8 bytes | `long` | Signed 64-bit |

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

```csharp
byte[] networkData = new byte[] { 0x12, 0x34, 0x56, 0x78 };

// From array with offset
var value = new UInt32Be(networkData, offset: 0);

// From IList<byte> (works with List<byte>, arrays, etc.)
IList<byte> bytes = networkData;
var fromList = new UInt32Be(bytes, offset: 0);
```

### From Spans (Zero-Allocation)

```csharp
// From ReadOnlySpan<byte> - zero allocation
ReadOnlySpan<byte> packet = stackalloc byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
var header = new UInt32Be(packet);

// Static factory method
var value = UInt32Be.ReadFrom(packet);

// Slice for multiple values
var first = new UInt16Be(packet[..2]);
var second = new UInt16Be(packet[2..4]);
```

## Serialization

### To Byte Arrays

```csharp
UInt64Be value = 0x0102030405060708UL;

// Instance method
byte[] buffer = new byte[8];
value.ToBytes(buffer, offset: 0);
// buffer: [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]

// Static method
UInt64Be.ToBytes(0x0102030405060708UL, buffer, offset: 0);
```

### To Spans (Zero-Allocation)

```csharp
UInt32Be value = 0x12345678;

// WriteTo - throws if span too small
Span<byte> buffer = stackalloc byte[4];
value.WriteTo(buffer);

// TryWriteTo - returns false if span too small
Span<byte> smallBuffer = stackalloc byte[2];
bool success = value.TryWriteTo(smallBuffer);  // false

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

// Widening conversions
UInt64Be wide = val32;           // UInt32Be -> UInt64Be
UInt64Be fromSmall = val16;      // UInt16Be -> UInt64Be

// Signed widening (sign-extends correctly)
Int16Be small = -100;
Int64Be large = small;           // Still -100, sign-extended

// Big-endian to native
ushort native16 = val16;
uint native32 = val32;
ulong native64 = (UInt64Be)val32;
```

### Explicit Conversions (May Truncate)

```csharp
UInt64Be big = 0x123456789ABCDEF0UL;

// Narrowing - takes low bytes
UInt32Be truncated32 = (UInt32Be)big;  // 0x9ABCDEF0
UInt16Be truncated16 = (UInt16Be)big;  // 0xDEF0
byte truncatedByte = (byte)big;         // 0xF0
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

// Native types work too
ulong native = 0x123456789ABCDEF0UL;
uint nativeHi = native.Hi();  // 0x12345678
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

// Native types work too
ulong native = 0x123456789ABCDEF0UL;
uint nativeLo = native.Lo();  // 0x9ABCDEF0
```

### SetHi() / SetLo() - Replace Half

```csharp
// Set upper half
UInt64Be value = 0x123456789ABCDEF0UL;
UInt64Be newHi = value.SetHi((UInt32Be)0xDEADBEEFU);  // 0xDEADBEEF9ABCDEF0

// Set lower half
UInt64Be newLo = value.SetLo((UInt32Be)0xCAFEBABEU);  // 0x12345678CAFEBABE

// Works with native types too
ulong nativeVal = 0x123456789ABCDEF0UL;
ulong withNewHi = nativeVal.SetHi(0xDEADBEEFU);  // 0xDEADBEEF9ABCDEF0
ulong withNewLo = nativeVal.SetLo(0xCAFEBABEU);  // 0x12345678CAFEBABE
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

All big-endian types implement:

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
endian overrides in `[BitFieldsView]` structs. See [BITFIELDS.md](BITFIELDS.md) for full
documentation on composition and mixed-endian nesting.

```csharp
// Combining Big-Endian with BitFields for hardware emulation
[BitFields(typeof(UInt32Be))]
public partial struct StatusRegister
{
    [BitField(0, 8)] public partial byte ErrorCode { get; set; }
    [BitField(8, 16)] public partial ushort DataLength { get; set; }
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

Big-endian types can also override individual field endianness within a `[BitFieldsView]`
struct. This is useful when a single struct mixes byte orders at the field level:

```csharp
// x86 file blob (little-endian default) with one big-endian IP address field
[BitFieldsView]
public partial record struct FileBlobView
{
    [BitField(0, 31)]  public partial uint Timestamp { get; set; }          // LE (struct default)
    [BitField(32, 63)] public partial UInt32Be NetworkIp { get; set; }      // BE (per-field override)
}
```

For uniform-endian structs, per-field overrides are unnecessary -- just set
`ByteOrder.BigEndian` on the `[BitFieldsView]` attribute and use plain `uint`, `ushort`, etc.

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

Each type has a `TypeConverter` for WinForms PropertyGrid and similar UI scenarios:

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

```csharp
// TypeConverters support hex input with 0x prefix
var converter = new UInt32BeTypeConverter();
object? result = converter.ConvertFrom(null, null, "0xDEADBEEF");
// result is UInt32Be with value 0xDEADBEEF
```

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
- Per-field endian overrides in a `[BitFields]` or `[BitFieldsView]` struct whose default is big-endian
- Cross-platform binary formats that mandate little-endian storage
- Big-endian host machines that need to read/write x86-native data

On mainstream platforms (x86, x64, ARM), native types like `uint` are already little-endian
in memory. In most code you can use plain native types and only reach for `*Le` when you need
a type-safe marker or a per-field override inside a BE `[BitFields]` or `[BitFieldsView]`.

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
