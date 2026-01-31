# BitField Source Generator

Type-safe, high-performance bitfield manipulation for hardware register emulation.

## Quick Start

```csharp
[BitFields]
public partial struct MyRegister
{
    public ushort Value;

    [BitField(0, 7)] public partial byte KeyCode { get; set; }
    [BitFlag(7)] public partial bool KeyUp { get; set; }
    [BitField(8, 7)] public partial byte SecondKey { get; set; }
    [BitFlag(15)] public partial bool SecondKeyUp { get; set; }
}

// Usage
MyRegister reg = 0xFFFF;          // Implicit conversion
reg.KeyUp = false;                // Property setter
reg.KeyCode = 0x1A;               // Property setter
byte code = reg.KeyCode;          // Property getter
ushort raw = reg;                 // Implicit conversion
```

The generator creates inline bit manipulation with **compile-time constants** — identical performance to hand-coded bit manipulation.

## Attributes

| Attribute | Usage | Description |
|-----------|-------|-------------|
| `[BitFields]` | Struct | Enables source generation |
| `[BitField(shift, width)]` | Property | Multi-bit field definition |
| `[BitFlag(bit)]` | Property | Single-bit flag definition |

## Supported Storage Types

| Storage | Bits |
|---------|------|
| `byte` | 8 |
| `ushort` | 16 |
| `uint` | 32 |
| `ulong` | 64 |

## Performance

Benchmarks show the generated code performs within **1%** of hand-coded bit manipulation (see README.md for full statistical analysis).

## Examples

### ADB Keyboard Register

```csharp
[BitFields]
public partial struct KeyboardReg0
{
    public ushort Value;

    [BitField(0, 7)] public partial byte SecondKeyCode { get; set; }
    [BitFlag(7)] public partial bool SecondKeyUp { get; set; }
    [BitField(8, 7)] public partial byte FirstKeyCode { get; set; }
    [BitFlag(15)] public partial bool FirstKeyUp { get; set; }
}
```

### VIA Register

```csharp
[BitFields]
public partial struct ViaRegB
{
    public byte Value;

    [BitField(0, 3)] public partial byte SoundVolume { get; set; }
    [BitFlag(3)] public partial bool SoundBuffer { get; set; }
    [BitFlag(4)] public partial bool OverlayRom { get; set; }
    [BitFlag(5)] public partial bool HeadSelect { get; set; }
    [BitFlag(6)] public partial bool VideoPage { get; set; }
    [BitFlag(7)] public partial bool SccAccess { get; set; }
}
```

### 64-bit Status Register

```csharp
[BitFields]
public partial struct StatusReg64
{
    public ulong Value;

    [BitField(0, 8)] public partial byte Status { get; set; }
    [BitField(8, 16)] public partial ushort DataWord { get; set; }
    [BitField(24, 32)] public partial uint Address { get; set; }
    [BitFlag(56)] public partial bool Enable { get; set; }
    [BitFlag(57)] public partial bool Ready { get; set; }
    [BitFlag(58)] public partial bool Error { get; set; }
}
```

## Generated Code

For each `[BitField]` or `[BitFlag]` property, the generator emits inline bit manipulation:

```csharp
// Generated for [BitFlag(0)]
public partial bool Ready
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => (Value & 0x01) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => Value = value ? (byte)(Value | 0x01) : (byte)(Value & 0xFE);
}

// Generated for [BitField(2, 3)]
public partial byte Mode
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => (byte)((Value >> 2) & 0x07);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => Value = (byte)((Value & 0xE3) | (((byte)value << 2) & 0x1C));
}
```

All masks are compile-time hex constants, allowing the JIT to generate optimal machine code.
