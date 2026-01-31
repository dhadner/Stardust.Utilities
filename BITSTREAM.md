# BitStream

A stream class for reading and writing individual bits. Extends `System.IO.Stream` for compatibility with standard stream operations.

## Overview

`BitStream` allows you to work at the bit level, which is useful for:
- Parsing binary protocols with non-byte-aligned fields
- Implementing compression algorithms
- Working with bit-packed data structures
- Serial communication protocols

## Quick Start

```csharp
using Stardust.Utilities.Bits;

// Create a stream
var stream = new BitStream();

// Write individual bits
stream.Write(true);   // 1
stream.Write(false);  // 0
stream.Write(true);   // 1

// Write a byte (8 bits)
stream.WriteByte(0xAB);

// Seek back to start
stream.Seek(0, SeekOrigin.Begin);

// Read bits back
bool bit1 = stream.Read();  // true
bool bit2 = stream.Read();  // false
bool bit3 = stream.Read();  // true

// Skip to byte boundary and read byte
stream.Seek(3, SeekOrigin.Begin);  // Position after first 3 bits
int value = stream.ReadByte();     // Returns 0xAB (if aligned)
```

## Creating a BitStream

```csharp
// Default capacity (2048 bits / 256 bytes)
var stream = new BitStream();

// Specific capacity in bits
var stream = new BitStream(1024);  // 1024 bits capacity
```

## Writing Bits

### Single Bits

```csharp
// Write returns 1 if successful, 0 if at capacity
int written = stream.Write(true);   // Write a 1 bit
written = stream.Write(false);      // Write a 0 bit
```

### Bytes

```csharp
// Write a single byte (8 bits)
stream.WriteByte(0xFF);

// Write from byte array
byte[] data = { 0x01, 0x02, 0x03 };
stream.Write(data, offset: 0, count: 3);  // Writes 24 bits
```

## Reading Bits

### Single Bits

```csharp
// Read one bit, throws if past end
bool bit = stream.Read();

// Safe read - returns -1 if at end
int result = stream.Read(out bool bit);
if (result == 1)
{
    // bit contains the value
}
```

### Bytes

```csharp
// Read a byte (8 bits), returns -1 if not enough bits
int value = stream.ReadByte();
if (value >= 0)
{
    byte b = (byte)value;
}

// Read into buffer
byte[] buffer = new byte[10];
int bytesRead = stream.Read(buffer, offset: 0, count: 10);
```

## Positioning

### Properties

```csharp
long pos = stream.Position;   // Current position in bits
long len = stream.Length;     // Used length in bits
long cap = stream.Capacity;   // Total capacity in bits
```

### Seeking

```csharp
// Seek from beginning
stream.Seek(0, SeekOrigin.Begin);

// Seek relative to current position
stream.Seek(8, SeekOrigin.Current);   // Forward 8 bits
stream.Seek(-4, SeekOrigin.Current);  // Back 4 bits

// Seek from end
stream.Seek(-1, SeekOrigin.End);      // Last bit
```

### Setting Length

```csharp
// Extend or truncate the stream
stream.SetLength(100);  // 100 bits

// Truncate from beginning (removes first N bits)
stream.Truncate(8, SeekOrigin.Begin);  // Remove first 8 bits

// Truncate from end (removes last N bits)
stream.Truncate(8, SeekOrigin.End);    // Remove last 8 bits
```

## Capacity Management

The stream automatically grows when needed, doubling capacity each time:

```csharp
// Manual capacity control
stream.Capacity = 4096;  // Set capacity to 4096 bits

// Access underlying buffer
byte[] buffer = stream.GetBuffer();
```

## Example: Parsing a Bit-Packed Protocol

```csharp
// Protocol: 3-bit type, 5-bit length, N bytes of data
public (int type, byte[] data) ParsePacket(BitStream stream)
{
    // Read 3-bit type
    int type = 0;
    for (int i = 0; i < 3; i++)
    {
        if (stream.Read())
            type |= (1 << i);
    }
    
    // Read 5-bit length
    int length = 0;
    for (int i = 0; i < 5; i++)
    {
        if (stream.Read())
            length |= (1 << i);
    }
    
    // Read data bytes
    byte[] data = new byte[length];
    stream.Read(data, 0, length);
    
    return (type, data);
}
```

## Example: Building a Bit-Packed Message

```csharp
public byte[] BuildMessage(int type, byte[] data)
{
    var stream = new BitStream();
    
    // Write 3-bit type
    for (int i = 0; i < 3; i++)
    {
        stream.Write((type & (1 << i)) != 0);
    }
    
    // Write 5-bit length
    int length = data.Length;
    for (int i = 0; i < 5; i++)
    {
        stream.Write((length & (1 << i)) != 0);
    }
    
    // Write data bytes
    stream.Write(data, 0, data.Length);
    
    // Get result (may have trailing bits in last byte)
    return stream.GetBuffer()[..(int)((stream.Length + 7) / 8)];
}
```

## Stream Compatibility

`BitStream` extends `System.IO.Stream`, so it works with APIs expecting streams:

```csharp
// Note: Byte-level operations may not align with bit boundaries
public override bool CanRead => true;
public override bool CanSeek => true;
public override bool CanWrite => true;
```

However, be aware that:
- `Position` and `Length` are in **bits**, not bytes
- `ReadByte()` requires at least 8 bits remaining
- Mixing bit and byte operations requires careful position management

## API Reference

| Member | Description |
|--------|-------------|
| `Write(bool)` | Write a single bit |
| `Write(byte[], int, int)` | Write bytes (offset and count in bytes) |
| `WriteByte(byte)` | Write 8 bits |
| `Read()` | Read a single bit (throws if past end) |
| `Read(out bool)` | Safe read (returns -1 if at end) |
| `Read(byte[], int, int)` | Read bytes into buffer |
| `ReadByte()` | Read 8 bits as int (-1 if not enough) |
| `Seek(long, SeekOrigin)` | Move position (in bits) |
| `SetLength(long)` | Set length (in bits) |
| `Truncate(int, SeekOrigin)` | Remove bits from beginning or end |
| `GetBuffer()` | Access underlying byte array |
| `Position` | Current position in bits |
| `Length` | Used length in bits |
| `Capacity` | Total capacity in bits |
