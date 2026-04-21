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
| `UInt128`, `Int128` | `ulong` | Lower 64 bits |
| `UInt16Be`, `Int16Be` | `byte` | Lower 8 bits |
| `UInt32Be`, `Int32Be` | `UInt16Be` | Lower 16 bits |
| `UInt64Be`, `Int64Be` | `UInt32Be` | Lower 32 bits |
| `UInt128Be`, `Int128Be` | `UInt64Be` | Lower 64 bits |
| `UInt16Le`, `Int16Le` | `byte` | Lower 8 bits |
| `UInt32Le`, `Int32Le` | `UInt16Le` | Lower 16 bits |
| `UInt64Le`, `Int64Le` | `UInt32Le` | Lower 32 bits |
| `UInt128Le`, `Int128Le` | `UInt64Le` | Lower 64 bits |

**Example:**
```csharp
ushort value = 0x1234;
byte lo = value.Lo();  // 0x34

uint value32 = 0x12345678;
ushort lo16 = value32.Lo();  // 0x5678

UInt128 value128 = ((UInt128)0x0123456789ABCDEFUL << 64) | 0xFEDCBA9876543210UL;
ulong lo64 = value128.Lo();  // 0xFEDCBA9876543210
```

### Hi (Get High Bits)

Gets the most-significant portion of a value.

| Source Type | Returns | Description |
|-------------|---------|-------------|
| `ushort`, `short` | `byte` | Upper 8 bits |
| `uint`, `int` | `ushort` | Upper 16 bits |
| `ulong`, `long` | `uint` | Upper 32 bits |
| `UInt128`, `Int128` | `ulong` | Upper 64 bits |
| `UInt16Be`, `Int16Be` | `byte` | Upper 8 bits |
| `UInt32Be`, `Int32Be` | `UInt16Be` | Upper 16 bits |
| `UInt64Be`, `Int64Be` | `UInt32Be` | Upper 32 bits |
| `UInt128Be`, `Int128Be` | `UInt64Be` | Upper 64 bits |
| `UInt16Le`, `Int16Le` | `byte` | Upper 8 bits |
| `UInt32Le`, `Int32Le` | `UInt16Le` | Upper 16 bits |
| `UInt64Le`, `Int64Le` | `UInt32Le` | Upper 32 bits |
| `UInt128Le`, `Int128Le` | `UInt64Le` | Upper 64 bits |

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
public static UInt128 SetLo(this UInt128 value, ulong lo)
public static Int128 SetLo(this Int128 value, ulong lo)
// Plus overloads for big-endian and little-endian types
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
public static UInt128 SetHi(this UInt128 value, ulong hi)
public static Int128 SetHi(this Int128 value, ulong hi)
// Plus overloads for big-endian and little-endian types
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
// Native integer types
public static byte    SaturatingAdd(this byte    a, byte    b)
public static sbyte   SaturatingAdd(this sbyte   a, sbyte   b)
public static short   SaturatingAdd(this short   a, short   b)
public static ushort  SaturatingAdd(this ushort  a, ushort  b)
public static int     SaturatingAdd(this int     a, int     b)
public static uint    SaturatingAdd(this uint    a, uint    b)
public static long    SaturatingAdd(this long    a, long    b)
public static ulong   SaturatingAdd(this ulong   a, ulong   b)
public static Int128  SaturatingAdd(this Int128  a, Int128  b)
public static UInt128 SaturatingAdd(this UInt128 a, UInt128 b)
public static Int256  SaturatingAdd(this Int256  a, Int256  b)
public static UInt256 SaturatingAdd(this UInt256 a, UInt256 b)

// Big-endian wrapper types
public static UInt16Be  SaturatingAdd(this UInt16Be  a, UInt16Be  b)
public static Int16Be   SaturatingAdd(this Int16Be   a, Int16Be   b)
public static UInt32Be  SaturatingAdd(this UInt32Be  a, UInt32Be  b)
public static Int32Be   SaturatingAdd(this Int32Be   a, Int32Be   b)
public static UInt64Be  SaturatingAdd(this UInt64Be  a, UInt64Be  b)
public static Int64Be   SaturatingAdd(this Int64Be   a, Int64Be   b)
public static UInt128Be SaturatingAdd(this UInt128Be a, UInt128Be b)
public static Int128Be  SaturatingAdd(this Int128Be  a, Int128Be  b)
public static UInt256Be SaturatingAdd(this UInt256Be a, UInt256Be b)
public static Int256Be  SaturatingAdd(this Int256Be  a, Int256Be  b)

// Little-endian wrapper types
public static UInt16Le  SaturatingAdd(this UInt16Le  a, UInt16Le  b)
public static Int16Le   SaturatingAdd(this Int16Le   a, Int16Le   b)
public static UInt32Le  SaturatingAdd(this UInt32Le  a, UInt32Le  b)
public static Int32Le   SaturatingAdd(this Int32Le   a, Int32Le   b)
public static UInt64Le  SaturatingAdd(this UInt64Le  a, UInt64Le  b)
public static Int64Le   SaturatingAdd(this Int64Le   a, Int64Le   b)
public static UInt128Le SaturatingAdd(this UInt128Le a, UInt128Le b)
public static Int128Le  SaturatingAdd(this Int128Le  a, Int128Le  b)
public static UInt256Le SaturatingAdd(this UInt256Le a, UInt256Le b)
public static Int256Le  SaturatingAdd(this Int256Le  a, Int256Le  b)
```

**Example:**
```csharp
int     result  = int.MaxValue.SaturatingAdd(1);                       // int.MaxValue (not overflow)
uint    result2 = uint.MaxValue.SaturatingAdd(100u);                   // uint.MaxValue
UInt128 result3 = UInt128.MaxValue.SaturatingAdd((UInt128)1);          // UInt128.MaxValue
Int128  result4 = Int128.MinValue.SaturatingAdd((Int128)(-1));         // Int128.MinValue
UInt256 result5 = UInt256.MaxValue.SaturatingAdd(new UInt256(1UL));    // UInt256.MaxValue
Int256  result6 = Int256.MinValue.SaturatingAdd(new Int256(-1L));      // Int256.MinValue
```

### SaturatingSub

Subtracts two values, returning `MinValue` on underflow or `MaxValue` on overflow (for signed types).

```csharp
// Native integer types
public static byte    SaturatingSub(this byte    a, byte    b)
public static sbyte   SaturatingSub(this sbyte   a, sbyte   b)
public static short   SaturatingSub(this short   a, short   b)
public static ushort  SaturatingSub(this ushort  a, ushort  b)
public static int     SaturatingSub(this int     a, int     b)
public static uint    SaturatingSub(this uint    a, uint    b)
public static long    SaturatingSub(this long    a, long    b)
public static ulong   SaturatingSub(this ulong   a, ulong   b)
public static Int128  SaturatingSub(this Int128  a, Int128  b)
public static UInt128 SaturatingSub(this UInt128 a, UInt128 b)
public static Int256  SaturatingSub(this Int256  a, Int256  b)
public static UInt256 SaturatingSub(this UInt256 a, UInt256 b)

// Big-endian wrapper types
public static UInt16Be  SaturatingSub(this UInt16Be  a, UInt16Be  b)
public static Int16Be   SaturatingSub(this Int16Be   a, Int16Be   b)
public static UInt32Be  SaturatingSub(this UInt32Be  a, UInt32Be  b)
public static Int32Be   SaturatingSub(this Int32Be   a, Int32Be   b)
public static UInt64Be  SaturatingSub(this UInt64Be  a, UInt64Be  b)
public static Int64Be   SaturatingSub(this Int64Be   a, Int64Be   b)
public static UInt128Be SaturatingSub(this UInt128Be a, UInt128Be b)
public static Int128Be  SaturatingSub(this Int128Be  a, Int128Be  b)
public static UInt256Be SaturatingSub(this UInt256Be a, UInt256Be b)
public static Int256Be  SaturatingSub(this Int256Be  a, Int256Be  b)

// Little-endian wrapper types
public static UInt16Le  SaturatingSub(this UInt16Le  a, UInt16Le  b)
public static Int16Le   SaturatingSub(this Int16Le   a, Int16Le   b)
public static UInt32Le  SaturatingSub(this UInt32Le  a, UInt32Le  b)
public static Int32Le   SaturatingSub(this Int32Le   a, Int32Le   b)
public static UInt64Le  SaturatingSub(this UInt64Le  a, UInt64Le  b)
public static Int64Le   SaturatingSub(this Int64Le   a, Int64Le   b)
public static UInt128Le SaturatingSub(this UInt128Le a, UInt128Le b)
public static Int128Le  SaturatingSub(this Int128Le  a, Int128Le  b)
public static UInt256Le SaturatingSub(this UInt256Le a, UInt256Le b)
public static Int256Le  SaturatingSub(this Int256Le  a, Int256Le  b)
```

**Example:**
```csharp
int     result  = int.MinValue.SaturatingSub(1);                       // int.MinValue (not underflow)
uint    result2 = 5u.SaturatingSub(10u);                               // 0 (not wrap to MaxValue)
UInt128 result3 = ((UInt128)5).SaturatingSub((UInt128)10);             // 0
Int128  result4 = Int128.MaxValue.SaturatingSub((Int128)(-1));         // Int128.MaxValue
UInt256 result5 = new UInt256(5UL).SaturatingSub(new UInt256(10UL));   // UInt256.MinValue (0)
Int256  result6 = Int256.MaxValue.SaturatingSub(new Int256(-1L));      // Int256.MaxValue
```

---

## Performance Notes

All methods use `[MethodImpl(MethodImplOptions.AggressiveInlining)]` to hint to the JIT compiler that these should be inlined at call sites for maximum performance. This is particularly important for hot paths in emulators and low-level code.

### Saturating Arithmetic Overhead

The saturating methods add a bounds check (branch) after the arithmetic operation. For 8-bit and 16-bit types, the check is performed by widening to `int` and comparing against `MinValue`/`MaxValue` -- effectively free on modern CPUs. For 32-bit and 64-bit signed types, the overflow detection uses a branchless XOR idiom. Unsigned types use a simple comparison.

The table below shows measured overhead of saturating vs. normal (wrapping) arithmetic on 10M iterations (BenchmarkDotNet, .NET 10, Release, x64). The "Overhead" column shows the ratio of saturating to normal time.

| Type | Operation | Normal | Saturating | Overhead |
|------|-----------|--------|------------|----------|
| `byte` | Add | 6.4 ms | 7.0 ms | 1.09x |
| `byte` | Sub | 6.5 ms | 7.0 ms | 1.08x |
| `short` | Add | 6.5 ms | 7.1 ms | 1.09x |
| `ushort` | Add | 6.4 ms | 7.7 ms | 1.20x |
| `int` | Add | 5.4 ms | 9.7 ms | 1.80x |
| `uint` | Sub | 5.4 ms | 6.6 ms | 1.22x |
| `long` | Add | 5.1 ms | 8.0 ms | 1.57x |
| `ulong` | Sub | 5.4 ms | 6.3 ms | 1.17x |
| `Int128` | Add | 9.7 ms | 22.4 ms | 2.31x |
| `UInt128` | Sub | 9.9 ms | 11.8 ms | 1.19x |
| `Int256` | Add | ~19 ms | ~45 ms | ~2.4x |
| `UInt256` | Sub | ~20 ms | ~24 ms | ~1.2x |
| `UInt32Be` | Add | 48.6 ms | 51.7 ms | 1.06x |
| `UInt32Le` | Add | 51.1 ms | 53.4 ms | 1.04x |

**Key takeaways:**

- **8-bit and 16-bit:** Negligible overhead (< 1.2x). The widening-to-int pattern is essentially free.
- **Unsigned 32/64/128-bit:** Low overhead (1.17x--1.22x). The single comparison (`b > a` or `result < a`) is very cheap.
- **Signed 32/64-bit:** Moderate overhead (1.57x--1.80x). The XOR-based overflow detection adds a few instructions but no branches in the common (non-overflow) case.
- **Signed 128/256-bit:** Higher overhead (~2.3--2.4x). Multi-word comparisons are multi-instruction sequences on x64. Still low absolute cost.
- **Unsigned 256-bit:** Low overhead (~1.2x). The sign-bit comparison used by `Int256` is replaced by a simple less-than check.
- **Endian wrapper types (Be/Le):** The saturating overhead is negligible (1.04x--1.06x) because the endian conversion dominates the total cost.
- **Zero heap allocation:** All methods allocate zero managed memory.

---

## See Also

- [BITFIELDS.md](BITFIELDS.md) - Source generator for bit field structs and buffer views
- [ENDIAN.md](ENDIAN.md) - Big-endian and little-endian primitive types
- [OPTION.md](OPTION.md) - Optional values without null
- [RESULT.md](RESULT.md) - Railway-oriented error handling
- [README.md](README.md) - Package overview
