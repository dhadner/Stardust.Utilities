# Extension Methods

This document describes the extension methods provided by `Stardust.Utilities` for byte ordering and arithmetic operations.

All extension methods are marked with `[MethodImpl(MethodImplOptions.AggressiveInlining)]` for maximum performance.

## Table of Contents

- [Byte Ordering (Hi/Lo)](#byte-ordering-hilo)
  - [Lo (Get Low Bits)](#lo-get-low-bits)
  - [Hi (Get High Bits)](#hi-get-high-bits)
  - [SetLo](#setlo)
  - [SetHi](#sethi)
- [Saturating Arithmetic](#saturating-arithmetic)
  - [SaturatingAdd](#saturatingadd)
  - [SaturatingSub](#saturatingsub)

---

## Byte Ordering (Hi/Lo)

These methods extract or set the high and low portions of multi-byte values. They work with both native types and the big-endian types (`UInt16Be`, `Int32Be`, etc.).

### Lo (Get Low Bits)

Gets the least-significant portion of a value.

| Source Type | Returns | Description |
|-------------|---------|-------------|
| `ushort`, `short` | `byte` | Lower 8 bits |
| `uint`, `int` | `ushort` | Lower 16 bits |
| `ulong`, `long` | `uint` | Lower 32 bits |
| `UInt16Be`, `Int16Be` | `byte` | Lower 8 bits |
| `UInt32Be`, `Int32Be` | `UInt16Be` | Lower 16 bits |
| `UInt64Be`, `Int64Be` | `UInt32Be` | Lower 32 bits |

**Example:**
```csharp
ushort value = 0x1234;
byte lo = value.Lo();  // 0x34

uint value32 = 0x12345678;
ushort lo16 = value32.Lo();  // 0x5678
```

### Hi (Get High Bits)

Gets the most-significant portion of a value.

| Source Type | Returns | Description |
|-------------|---------|-------------|
| `ushort`, `short` | `byte` | Upper 8 bits |
| `uint`, `int` | `ushort` | Upper 16 bits |
| `ulong`, `long` | `uint` | Upper 32 bits |
| `UInt16Be`, `Int16Be` | `byte` | Upper 8 bits |
| `UInt32Be`, `Int32Be` | `UInt16Be` | Upper 16 bits |
| `UInt64Be`, `Int64Be` | `UInt32Be` | Upper 32 bits |

**Example:**
```csharp
ushort value = 0x1234;
byte hi = value.Hi();  // 0x12

uint value32 = 0x12345678;
ushort hi16 = value32.Hi();  // 0x1234
```

### SetLo

Returns a new value with the low portion replaced.

```csharp
public static ushort SetLo(this ushort value, byte lo)
public static short SetLo(this short value, byte lo)
public static uint SetLo(this uint value, ushort lo)
public static int SetLo(this int value, ushort lo)
public static ulong SetLo(this ulong value, uint lo)
public static long SetLo(this long value, uint lo)
// Plus overloads for big-endian types
```

**Example:**
```csharp
ushort value = 0x1234;
value = value.SetLo(0xFF);  // 0x12FF
```

### SetHi

Returns a new value with the high portion replaced.

```csharp
public static ushort SetHi(this ushort value, byte hi)
public static short SetHi(this short value, byte hi)
public static uint SetHi(this uint value, ushort hi)
public static int SetHi(this int value, ushort hi)
public static ulong SetHi(this ulong value, uint hi)
public static long SetHi(this long value, uint hi)
// Plus overloads for big-endian types
```

**Example:**
```csharp
ushort value = 0x1234;
value = value.SetHi(0xFF);  // 0xFF34
```

---

## Saturating Arithmetic

These methods perform arithmetic that clamps results to the type's min/max values instead of overflowing or wrapping.

### SaturatingAdd

Adds two values, returning `MaxValue` on overflow or `MinValue` on underflow (for signed types).

```csharp
public static int SaturatingAdd(this int a, int b)
public static long SaturatingAdd(this long a, long b)
public static uint SaturatingAdd(this uint a, uint b)
public static ulong SaturatingAdd(this ulong a, ulong b)
```

**Example:**
```csharp
int result = int.MaxValue.SaturatingAdd(1);  // int.MaxValue (not overflow)
uint result2 = uint.MaxValue.SaturatingAdd(100);  // uint.MaxValue
```

### SaturatingSub

Subtracts two values, returning `MinValue` on underflow or `MaxValue` on overflow (for signed types).

```csharp
public static int SaturatingSub(this int a, int b)
public static long SaturatingSub(this long a, long b)
public static uint SaturatingSub(this uint a, uint b)
public static ulong SaturatingSub(this ulong a, ulong b)
```

**Example:**
```csharp
int result = int.MinValue.SaturatingSub(1);  // int.MinValue (not underflow)
uint result2 = 5u.SaturatingSub(10);  // 0 (not wrap to MaxValue)
```

---

## Performance Notes

All methods use `[MethodImpl(MethodImplOptions.AggressiveInlining)]` to hint to the JIT compiler that these should be inlined at call sites for maximum performance. This is particularly important for hot paths in emulators and low-level code.

---

## See Also

- [BITFIELD.md](BITFIELD.md) - Source generator for bit field structs
- [ENDIAN.md](ENDIAN.md) - Big-endian primitive types
- [README.md](README.md) - Package overview
