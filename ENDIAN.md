# Big-Endian Types

Type-safe big-endian (network byte order) integer types for .NET.

## Overview

Big-endian types store the most significant byte first in memory, which is the standard byte order for network protocols, many file formats, and non-x86 hardware. These types provide:

- **Type safety** - Distinct types prevent accidentally mixing endianness
- **Natural syntax** - Full operator support for arithmetic and bitwise operations
- **Modern .NET APIs** - `Span<T>`, `IParsable<T>`, `ISpanFormattable` support
- **Zero-copy I/O** - Read/write directly from spans without allocation
- **PropertyGrid support** - TypeConverters for UI editing

## Available Types

| Type | Size | Native Equivalent | Description |
|------|------|-------------------|-------------|
| `UInt16Be` | 2 bytes | `ushort` | Unsigned 16-bit |
| `Int16Be` | 2 bytes | `short` | Signed 16-bit |
| `UInt32Be` | 4 bytes | `uint` | Unsigned 32-bit |
| `Int32Be` | 4 bytes | `int` | Signed 32-bit |
| `UInt64Be` | 8 bytes | `ulong` | Unsigned 64-bit |
| `Int64Be` | 8 bytes | `long` | Signed 64-bit |

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
