# BitField Library

Type-safe, high-performance bitfield manipulation for hardware register emulation.

## Quick Start

### Option 1: Source Generator (Recommended)

The simplest approach with the best performance:

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

The generator creates:
- Non-generic `BitField16`/`BitFlag16` definitions for each field
- Inlined property implementations
- Implicit conversion operators

### Option 2: Manual Definition with Generic Types

For flexibility when defining registers manually:

```csharp
public record struct KeyboardReg0(ushort Value)
{
    public static readonly BitFlag<ushort> KeyUp = new(15);
    public static readonly BitField<ushort, byte> KeyCode = new(8, 7);

    public static implicit operator ushort(KeyboardReg0 r) => r.Value;
    public static implicit operator KeyboardReg0(ushort v) => new(v);
}

// Usage
KeyboardReg0 reg = 0xFFFF;
bool isUp = KeyboardReg0.KeyUp[reg.Value];
byte code = KeyboardReg0.KeyCode[reg.Value];
reg = reg with { Value = KeyboardReg0.KeyCode.Set(reg.Value, 0x1A) };
```

### Option 3: Non-Generic Types for Hot Paths

For manually optimized hot paths:

```csharp
public struct ViaRegB
{
    public static readonly BitFlag8 HeadSelect = new(5);
    public static readonly BitField8 SoundVolume = new(0, 3);

    public byte Value;
}

// Usage - direct method calls
bool headSel = ViaRegB.HeadSelect.IsSet(regB.Value);
byte volume = ViaRegB.SoundVolume.GetByte(regB.Value);
```

## API Reference

### Attributes (for Source Generator)

| Attribute | Usage | Description |
|-----------|-------|-------------|
| `[BitFields]` | Struct | Enables source generation |
| `[BitField(shift, width)]` | Property | Multi-bit field definition |
| `[BitFlag(bit)]` | Property | Single-bit flag definition |

### Generic Types

| Type | Description |
|------|-------------|
| `BitFlag<TStorage>` | Single-bit flag with bool access |
| `BitField<TStorage, TField>` | Multi-bit field with typed extraction |

**BitFlag<TStorage>:**
```csharp
bool this[TStorage value]     // Get flag value
TStorage Set(value, bool)     // Set or clear flag
TStorage Toggle(value)        // Toggle flag
```

**BitField<TStorage, TField>:**
```csharp
TField this[TStorage value]   // Extract field
TStorage Set(value, TField)   // Set field value
```

### Non-Generic Types (Hot Path Performance)

| Storage | Field Type | Flag Type |
|---------|------------|-----------|
| `ulong` | `BitField64` | `BitFlag64` |
| `uint` | `BitField32` | `BitFlag32` |
| `ushort` | `BitField16` | `BitFlag16` |
| `byte` | `BitField8` | `BitFlag8` |

**BitFieldXX:**
```csharp
byte GetByte(value)           // Extract as byte
ushort GetUShort(value)       // Extract as ushort (16/32/64 only)
uint GetUInt(value)           // Extract as uint (32/64 only)
ulong GetULong(value)         // Extract as ulong (64 only)
TStorage Set(value, field)    // Set field value
```

**BitFlagXX:**
```csharp
bool IsSet(value)             // Check if flag is set
TStorage Set(value, bool)     // Set or clear flag
TStorage Toggle(value)        // Toggle flag
```

## Performance

The library is designed to meet a **?20% overhead** target vs hand-coded bit manipulation.

| Approach | Overhead | Notes |
|----------|----------|-------|
| Source Generator | ~0-15% | Uses non-generic types |
| Non-Generic Manual | ~0-15% | Direct method calls |
| Generic Types | ~50-100% | Generic interface dispatch |

Use the source generator or non-generic types for hot paths.

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
