# BitField Source Generator

Type-safe, high-performance bitfield manipulation for hardware register emulation.

## Quick Start

Two usage patterns are supported:

### Option 1: User-Declared Value Field

You declare the `Value` field explicitly. This gives you control over the field's visibility.

```csharp
[BitFields]
public partial struct MyRegister
{
    public ushort Value;  // You declare this

    [BitField(0, 7)] public partial byte KeyCode { get; set; }
    [BitFlag(7)] public partial bool KeyUp { get; set; }
    [BitField(8, 7)] public partial byte SecondKey { get; set; }
    [BitFlag(15)] public partial bool SecondKeyUp { get; set; }
}

// Usage
var reg = new MyRegister { Value = 0xFFFF };
reg.KeyUp = false;
ushort raw = reg;                 // Implicit conversion
```

### Option 2: Generator-Created Value Field

Specify the storage type in the attribute, and the generator creates a private `Value` field and constructor.

```csharp
[BitFields(typeof(ushort))]
public partial struct MyRegister
{
    // No Value field needed - generator creates it as private

    [BitField(0, 7)] public partial byte KeyCode { get; set; }
    [BitFlag(7)] public partial bool KeyUp { get; set; }
    [BitField(8, 7)] public partial byte SecondKey { get; set; }
    [BitFlag(15)] public partial bool SecondKeyUp { get; set; }
}

// Usage - use implicit conversion or constructor
MyRegister reg = 0xFFFF;          // Implicit conversion
var reg2 = new MyRegister(0x1234); // Constructor
reg.KeyUp = false;
ushort raw = reg;                 // Implicit conversion
```

Both approaches generate inline bit manipulation with **compile-time constants** - identical performance to hand-coded bit manipulation.

## Attributes

| Attribute | Usage | Description |
|-----------|-------|-------------|
| `[BitFields]` | Struct | Enables generation; user must declare `Value` field |
| `[BitFields(typeof(T))]` | Struct | Enables generation; generator creates private `Value` field of type T |
| `[BitField(shift, width)]` | Property | Multi-bit field definition |
| `[BitFlag(bit)]` | Property | Single-bit flag definition |

## Supported Storage Types

| Storage | Bits | Attribute Syntax |
|---------|------|------------------|
| `byte` | 8 | `[BitFields(typeof(byte))]` |
| `ushort` | 16 | `[BitFields(typeof(ushort))]` |
| `uint` | 32 | `[BitFields(typeof(uint))]` |
| `ulong` | 64 | `[BitFields(typeof(ulong))]` |

## Performance

Benchmarks show the generated code performs within **1%** of hand-coded bit manipulation (see README.md for full statistical analysis).

## Examples

### ADB Keyboard Register (User-Declared Value)

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

### VIA Register (Generator-Created Value)

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

## Generated Code

### With User-Declared Value Field

For `[BitFields]` with user-declared `Value`, the generator creates property implementations and implicit conversions:

```csharp
// User writes:
[BitFields]
public partial struct StatusRegister
{
    public byte Value;
    [BitFlag(0)] public partial bool Ready { get; set; }
    [BitField(2, 3)] public partial byte Mode { get; set; }
}

// Generator creates:
public partial struct StatusRegister
{
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
    public static implicit operator StatusRegister(byte value) => new() { Value = value };
}
```

### With Generator-Created Value Field

For `[BitFields(typeof(T))]`, the generator also creates the private `Value` field and constructor:

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

All masks are compile-time hex constants, allowing the JIT to generate optimal machine code.
