# BitField Source Generator

Type-safe, high-performance bitfield manipulation for hardware register emulation.

## Quick Start

```csharp
[BitFields(typeof(ushort))]
public partial struct MyRegister
{
    [BitField(0, 7)] public partial byte KeyCode { get; set; }
    [BitFlag(7)] public partial bool KeyUp { get; set; }
    [BitField(8, 7)] public partial byte SecondKey { get; set; }
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
- Implicit conversion operators to/from the storage type

## Attributes

| Attribute | Usage | Description |
|-----------|-------|-------------|
| `[BitFields(typeof(T))]` | Struct | Enables generation with storage type T |
| `[BitField(shift, width)]` | Property | Multi-bit field definition |
| `[BitFlag(bit)]` | Property | Single-bit flag definition |

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
    [BitField(0, 3)] public partial byte SoundVolume { get; set; }
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
    [BitField(0, 7)] public partial byte SecondKeyCode { get; set; }
    [BitFlag(7)] public partial bool SecondKeyUp { get; set; }
    [BitField(8, 7)] public partial byte FirstKeyCode { get; set; }
    [BitFlag(15)] public partial bool FirstKeyUp { get; set; }
}
```

### 64-bit Status Register

```csharp
[BitFields(typeof(ulong))]
public partial struct StatusReg64
{
    [BitField(0, 8)] public partial byte Status { get; set; }
    [BitField(8, 16)] public partial ushort DataWord { get; set; }
    [BitField(24, 32)] public partial uint Address { get; set; }
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
    [BitField(1, 15)] public partial ushort LowWord { get; set; }
    [BitField(16, 15)] public partial ushort HighWord { get; set; }
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
    [BitField(2, 3)] public partial byte Mode { get; set; }
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
