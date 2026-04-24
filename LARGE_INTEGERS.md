# Large Integers (`UInt256` / `Int256`)

Stardust.Utilities ships native-layout **256-bit unsigned** (`UInt256`) and **256-bit signed**
(`Int256`) integer value types. They provide full arithmetic, bitwise, comparison, parsing and
formatting support, and interop cleanly with `BigInteger`, `UInt128`, and the `[BitFields]`
source generator.

These types exist because:

- `BigInteger` is always heap-allocated and arbitrary-precision -- the wrong tool when you
  know the value fits in exactly 256 bits.
- `UInt128` / `Int128` are first-class in the BCL but cap out at 128 bits.
- Cryptography, Ethereum/EVM-style values, checksums, large accumulators, and GUID-like
  identifiers often need a *fixed-width 256-bit* type.

## Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [Endian-Aware Wire Types](#endian-aware-wire-types)
- [Integration with `[BitFields]`](#integration-with-bitfields)
- [Interop with `BigInteger`, `UInt128`, `Int256`](#interop-with-biginteger-uint128-int256)
- [API Surface](#api-surface)
  - [Static Math Helpers](#static-math-helpers)
  - [Bit-Level Operations](#bit-level-operations)
  - [`Is*` Predicates](#is-predicates)
  - [Endian Read / Write](#endian-read--write)
  - [Checked Operators](#checked-operators)
  - [Parsing and Formatting](#parsing-and-formatting)
  - [Generic Math Interfaces](#generic-math-interfaces)
- [Performance](#performance)
  - [Design Principles](#design-principles)
  - [Benchmark Results](#benchmark-results)
  - [Reproducing the Benchmarks](#reproducing-the-benchmarks)
- [When to Use What](#when-to-use-what)

---

## Overview

| Type | Bits | Layout | Heap alloc | Representation |
|------|------|--------|------------|----------------|
| `UInt256` | 256 (unsigned) | Four `ulong` limbs (`_p0`.._p3`) | Never | Host-native |
| `Int256` | 256 (signed, two's complement) | Four `ulong` limbs (`_p0`.._p3`); top bit of `_p3` is the sign | Never | Host-native |
| `UInt256Le` / `UInt256Be` | 256 (unsigned) | 32 bytes, explicit layout | Never | Little/big-endian wire format |
| `Int256Le` / `Int256Be` | 256 (signed) | 32 bytes, explicit layout | Never | Little/big-endian wire format |

`UInt256` and `Int256` are host-native in memory and are the type you want for arithmetic,
comparison and general-purpose use. `UInt256Le` / `UInt256Be` are byte-ordered types for
serialization and interop with protocols or files that demand a specific endianness.

Internally, `UInt256` stores the value as four native `ulong` limbs. This allows Add and
Sub operators to read all eight limbs with no extraction overhead, which the JIT can assign
to dedicated registers. The public `Lower` and `Upper` properties return the corresponding
`UInt128` halves (computed on access) for callers that need them.

Both types implement the full generic-math surface
(`INumber<T>` / `IBinaryInteger<T>` / `IBinaryNumber<T>`, plus `IUtf8SpanFormattable` /
`IUtf8SpanParsable<T>` on .NET 8+), so anywhere you would use `UInt128.PopCount`,
`Int128.DivRem`, `Int128.CreateChecked`, or `UInt128.TryReadBigEndian`, the same member
exists on `UInt256` / `Int256` with identical semantics. See
[API Surface](#api-surface) for the full list.

## Quick Start

```csharp
using Stardust.Utilities;

// Construction
UInt256 a = 42;                                        // implicit from ulong
UInt256 b = UInt256.Parse("115792089237316195423570985008687907853269984665640564039457584007913129639935");
UInt256 c = new(hi: UInt128.Zero, lo: (UInt128)123);  // two-UInt128 ctor
UInt256 d = new(u3: 0, u2: 0, u1: 0, u0: 7);          // four-ulong ctor (MSB first)

// Arithmetic -- fully checked, wraps modulo 2^256 on overflow (like all unsigned .NET types)
UInt256 sum  = a + b;
UInt256 prod = a * b;
UInt256 quot = b / a;
UInt256 mod  = b % a;

// Bitwise
UInt256 mask = UInt256.MaxValue >> 32;
UInt256 flip = ~a;

// Parsing from hex (with optional "0x" prefix)
UInt256 hex  = UInt256.Parse("0xDEADBEEFCAFEBABE", NumberStyles.HexNumber);

// Formatting
string dec   = a.ToString();              // "42"
string hex32 = a.ToString("X", null);     // "2A" (trimmed) or "0...2A" with precision specifier
string padded = a.ToString("D10", null);  // "0000000042"

// Compare / equality / hashing all work as expected
bool lt = a < b;
int  h  = a.GetHashCode();

// Interop with BigInteger and UInt128 for perimeter I/O
BigInteger big   = a.ToBigInteger();
UInt256    back  = UInt256.FromBigInteger(big);
UInt128    narrow = (UInt128)a;           // narrowing cast truncates to low 128 bits
```

`Int256` exposes the same surface, with sign-preserving semantics on conversion, comparison,
and formatting:

```csharp
Int256 n = -1;
Int256 m = Int256.MinValue;
Int256 p = (Int256)new UInt256(UInt128.Zero, UInt128.MaxValue);   // bit-reinterpret
string s = n.ToString();              // "-1"
```

## Endian-Aware Wire Types

When bytes hit a network socket, a file, or a memory-mapped register, endianness matters.
Use `UInt256Le`/`Int256Le` (little-endian, x86 native order) or `UInt256Be`/`Int256Be`
(big-endian, network byte order) at the I/O boundary and convert to the host-native type
for arithmetic:

```csharp
ReadOnlySpan<byte> wire = ...;                         // 32 bytes off the wire (BE, unsigned)
UInt256Be be  = new(wire);
UInt256    hv = be;                                    // implicit conversion to host-native

UInt256 result = hv * 3 + 1;

// Write back out
Span<byte> outBuf = stackalloc byte[32];
((UInt256Be)result).WriteTo(outBuf);

// Signed variant — identical pattern
ReadOnlySpan<byte> signedWire = ...;                   // 32 bytes off the wire (BE, signed)
Int256Be sbe = new(signedWire);
Int256   sv  = sbe;                                    // implicit conversion preserves sign
Int256   res = sv * -1;
((Int256Be)res).WriteTo(outBuf);
```

See [ENDIAN.md](ENDIAN.md) for the full endian-type story and the complete 16/32/64/128/256-bit
matrix.

## Integration with `[BitFields]`

Both `UInt256` and `Int256` are first-class storage types for the `[BitFields]` source
generator. Use them when you need a 256-bit register-like value whose fields you address by
name:

```csharp
[BitFields(typeof(UInt256))]
public partial struct Keccak256Register
{
    [BitField(0,   End = 63)]  public partial ulong Lane0 { get; set; }
    [BitField(64,  End = 127)] public partial ulong Lane1 { get; set; }
    [BitField(128, End = 191)] public partial ulong Lane2 { get; set; }
    [BitField(192, End = 255)] public partial ulong Lane3 { get; set; }
}

// Implicit conversions round-trip through the generator
UInt256            digest = ComputeHash(data);
Keccak256Register  reg    = digest;
ulong              lane2  = reg.Lane2;
UInt256            again  = reg;
```

The generator emits a four-`ulong` backing store and implicit conversion operators
`UInt256 <-> YourStruct` (and `Int256 <-> YourStruct` for signed). Full arithmetic, bitwise,
parse/format, JSON, span I/O and `With...` methods are generated -- identical to the
128-bit `UInt128` / `Int128` path.

Also supported: the enum-based form for discoverability via IntelliSense.

```csharp
[BitFields(StorageType.UInt256)]    // or StorageType.Int256
public partial struct MyStruct { /* ... */ }
```

See [BITFIELDS.md](BITFIELDS.md) for the full generator reference.

## Interop with `BigInteger`, `UInt128`, `Int256`

**Widening to `UInt256`:**

| From | Conversion | Notes |
|------|------------|-------|
| `byte`, `ushort`, `uint`, `ulong`, `UInt128`, `char` | **Implicit** | Zero-extended |
| `sbyte`, `short`, `int`, `long`, `Int128` | **Explicit** | Unchecked: bit-reinterpret (two's-complement). Checked: throws on negative |
| `float`, `double`, `decimal` | **Explicit** | Truncates toward zero. Unchecked out-of-range wraps; checked throws |
| `BigInteger` | `UInt256.FromBigInteger(big)` | Throws `OverflowException` if negative or > 256 bits |
| `Int256` | **Explicit** | Bit-reinterpret (two's-complement pattern preserved) |

**Narrowing from `UInt256`:**

| To | Conversion | Notes |
|----|------------|-------|
| `byte`, `ushort`, `uint`, `ulong`, `UInt128` | **Explicit** (unchecked + checked) | Unchecked truncates low bits; checked throws on overflow |
| `sbyte`, `short`, `int`, `long`, `Int128` | **Explicit** (unchecked + checked) | Unchecked truncates; checked throws on overflow |
| `float`, `double` | **Explicit** | May lose precision |
| `decimal` | **Explicit** | Throws `OverflowException` if value > `decimal.MaxValue` |
| `BigInteger` | `.ToBigInteger()` | Always non-negative |
| `Int256` | **Explicit** | Bit-reinterpret (no value change) |

**Widening to `Int256`:**

| From | Conversion | Notes |
|------|------------|-------|
| `sbyte`, `short`, `int`, `long`, `Int128`, `byte`, `ushort`, `uint`, `char` | **Implicit** | Sign- or zero-extended as appropriate |
| `ulong`, `UInt128` | **Explicit** | Cross-sign; matches BCL `Int128` policy |
| `float`, `double`, `decimal` | **Explicit** | Truncates toward zero. Unchecked out-of-range wraps to `MinValue`; checked throws |
| `BigInteger` | `Int256.FromBigInteger(big)` | Throws `OverflowException` if outside `Int256` range |

**Narrowing from `Int256`:**

| To | Conversion | Notes |
|----|------------|-------|
| `sbyte`, `short`, `int`, `long`, `Int128` | **Explicit** (unchecked + checked) | Unchecked truncates; checked throws on overflow |
| `byte`, `ushort`, `uint`, `ulong`, `UInt128`, `char` | **Explicit** (unchecked + checked) | Unchecked truncates; checked throws on negative or overflow |
| `float`, `double` | **Explicit** | Sign-preserving; may lose precision |
| `decimal` | **Explicit** | Throws if magnitude > `decimal.MaxValue` |
| `BigInteger` | `.ToBigInteger()` | Signed (two's-complement) |

All value-type conversions are pure register operations -- no allocations, no runtime type checks.
`BigInteger` conversions are the exception (they must allocate because `BigInteger` itself is
heap-allocated), and they exist exclusively for boundary interop.

The checked `explicit operator checked T(...)` variants are accessed inside a `checked { }` block
or with the `checked(...)` expression and throw `OverflowException` on out-of-range inputs --
matching the behaviour of the BCL `UInt128` / `Int128` checked conversions.

## API Surface

`UInt256` and `Int256` mirror the full BCL `UInt128` / `Int128` shape. Anywhere you would
reach for `UInt128.PopCount`, `Int128.DivRem`, `Int128.CreateChecked`, or
`UInt128.TryReadBigEndian`, the same member exists on `UInt256` / `Int256` with identical
semantics.

The static factory properties are unchanged from earlier releases: `Zero`, `One`,
`MaxValue`, `MinValue`, `AdditiveIdentity`, `MultiplicativeIdentity`, `Radix` (= 2), and --
on .NET 8+ -- `AllBitsSet`. `Int256` additionally exposes `NegativeOne`. The `FromBigInteger`
factory and `ToBigInteger()` instance method remain the allocating perimeter for
`BigInteger` interop.

### Static Math Helpers

| Member | `UInt256` | `Int256` | Notes |
|---|:---:|:---:|---|
| `Abs(value)` | ✓ (no-op) | ✓ | `Int256.Abs` throws on `MinValue` |
| `Clamp(value, min, max)` | ✓ | ✓ | Throws `ArgumentException` if `min > max` |
| `CopySign(value, sign)` | ✓ (no-op) | ✓ | |
| `DivRem(left, right)` | ✓ | ✓ | Returns `(Quotient, Remainder)` tuple |
| `Max(x, y)` / `Min(x, y)` | ✓ | ✓ | Signed-aware on `Int256` |
| `MaxMagnitude` / `MinMagnitude` | ✓ | ✓ | `Int256` compares by `|value|` |
| `MaxMagnitudeNumber` / `MinMagnitudeNumber` | ✓ | ✓ | Aliases required by `INumberBase<T>` |
| `Sign(value)` | ✓ (0 or 1) | ✓ (-1 / 0 / 1) | |

### Bit-Level Operations

| Member | Returns | Notes |
|---|---|---|
| `LeadingZeroCount(value)` | `T` (0..256) | |
| `TrailingZeroCount(value)` | `T` (0..256) | |
| `PopCount(value)` | `T` | Count of set bits |
| `Log2(value)` | `T` | `Int256` throws on negative; zero returns 0 (matches BCL) |
| `RotateLeft(value, int)` / `RotateRight(value, int)` | `T` | Rotate amount interpreted `mod 256` |
| `GetByteCount()` (instance) | `int` | Always 32 |
| `GetShortestBitLength()` (instance) | `int` | BCL semantics: `256 - LeadingZeroCount(value)` for non-negative, `257 - LeadingZeroCount(~value)` for negative `Int256` |

All bit-level methods compile to fully-unrolled limb scans -- no heap, no branches in the
hot path beyond the zero-limb shortcut.

### `Is*` Predicates

Every `Is*` predicate required by `INumberBase<T>` is present. The integer-relevant ones
(all returning `bool`):

| Predicate | `UInt256` | `Int256` |
|---|---|---|
| `IsZero` | value == 0 | value == 0 |
| `IsEvenInteger` / `IsOddInteger` | LSB-based | LSB-based |
| `IsPow2` | `true` iff `PopCount == 1` | `true` iff value > 0 and `PopCount == 1` |
| `IsNegative` | always `false` | true iff top bit set |
| `IsPositive` | always `true` | true iff top bit clear (matches BCL: 0 is positive) |

The BCL-required trivial predicates -- `IsCanonical` (true), `IsFinite` (true), `IsInteger`
(true), `IsInfinity` / `IsNaN` / `IsPositiveInfinity` / `IsNegativeInfinity` / `IsSubnormal`
/ `IsImaginaryNumber` / `IsComplexNumber` (all false), `IsNormal` (value != 0), `IsRealNumber`
(true) -- are all present with the constant values dictated by `INumberBase<T>`.

### Endian Read / Write

The `UInt256` / `Int256` types themselves -- not just the `UInt256Be` / `UInt256Le`
wrappers -- can read and write big- or little-endian byte sequences directly.

```csharp
// Static: parse from bytes. isUnsigned controls sign-extension and overflow.
UInt256 parsed = UInt256.ReadBigEndian(buf, isUnsigned: true);
if (Int256.TryReadLittleEndian(buf, isUnsigned: false, out Int256 v)) { /* ... */ }

// Instance: write to bytes.
Span<byte> out32 = stackalloc byte[32];
int n = value.WriteBigEndian(out32);                 // always 32 on success
bool ok = value.TryWriteLittleEndian(out32, out n);
```

| Direction | Members |
|---|---|
| Static read | `ReadBigEndian`, `ReadLittleEndian`, `TryReadBigEndian`, `TryReadLittleEndian` |
| Instance write | `WriteBigEndian`, `WriteLittleEndian`, `TryWriteBigEndian`, `TryWriteLittleEndian` |

For signed `Int256`, `isUnsigned: false` sign-extends a shorter source and rejects
unsigned sources whose top bit is set. For unsigned `UInt256`, `isUnsigned: false` forces
the caller to acknowledge the source is signed and rejects negative inputs.

### Checked Operators

Both types expose C# 11 `checked` operator variants that throw `OverflowException` on
out-of-range results:

```csharp
UInt256 safe = checked(x + y);       // throws if sum > MaxValue
Int256  safe = checked(x * y);       // throws on signed overflow
Int256  safe = checked(-x);          // throws on MinValue
Int256  safe = checked(x / y);       // Int256: throws on MinValue / -1
```

The full set on both types: `checked +`, `checked -` (binary and unary), `checked *`,
`checked /`, `checked ++`, `checked --`. `Int256` division additionally traps the
`MinValue / -1` overflow case.

### Parsing and Formatting

Every `Parse` / `TryParse` overload defined by `INumberBase<T>` is implemented:

| Overload | `UInt256` | `Int256` |
|---|:---:|:---:|
| `Parse(string, NumberStyles, IFormatProvider?)` | ✓ | ✓ |
| `Parse(ReadOnlySpan<char>, NumberStyles, IFormatProvider?)` | ✓ | ✓ |
| `TryParse(string?, NumberStyles, IFormatProvider?, out T)` | ✓ | ✓ |
| `TryParse(ReadOnlySpan<char>, NumberStyles, IFormatProvider?, out T)` | ✓ | ✓ |

On .NET 8+, `IUtf8SpanFormattable` and `IUtf8SpanParsable<T>` are also implemented:

```csharp
// Format directly to UTF-8 without the intermediate string.
Span<byte> utf8 = stackalloc byte[80];
value.TryFormat(utf8, out int n, "G", CultureInfo.InvariantCulture);

// Parse from UTF-8 bytes (e.g., HTTP header, protobuf string).
UInt256 v = UInt256.Parse(utf8Bytes, CultureInfo.InvariantCulture);
```

### Generic Math Interfaces

Both types implement the full generic-math surface. Declarations:

```
UInt256 : INumber<UInt256>, IBinaryInteger<UInt256>, IBinaryNumber<UInt256>,
          IMinMaxValue<UInt256>, IUnsignedNumber<UInt256>,
          IUtf8SpanFormattable, IUtf8SpanParsable<UInt256>        // (.NET 8+)

Int256  : INumber<Int256>, IBinaryInteger<Int256>, IBinaryNumber<Int256>,
          IMinMaxValue<Int256>, ISignedNumber<Int256>,
          IUtf8SpanFormattable, IUtf8SpanParsable<Int256>         // (.NET 8+)
```

This means generic-math code that works over `UInt128` / `Int128` works unchanged:

```csharp
static T Sum<T>(ReadOnlySpan<T> values) where T : INumber<T>
{
    T total = T.Zero;
    foreach (T v in values) total += v;
    return total;
}

UInt256 total = Sum<UInt256>(...);     // no changes to Sum<T>
```

The `CreateChecked` / `CreateSaturating` / `CreateTruncating` factories are exposed as
concrete public static methods (matching how the BCL exposes them) so callers can write
`UInt256.CreateChecked(someInt128)` without going through the interface:

```csharp
UInt256 a = UInt256.CreateChecked(someInt);          // throws on negative
UInt256 b = UInt256.CreateSaturating(-1);            // clamps to Zero
UInt256 c = UInt256.CreateTruncating(-1L);           // wraps to ulong.MaxValue (64-bit truncation)
Int256  d = Int256.CreateSaturating(1e78);           // saturates to Int256.MaxValue
```

Every BCL numeric type is supported as the source/target: `byte`, `sbyte`, `short`,
`ushort`, `int`, `uint`, `long`, `ulong`, `nint`, `nuint`, `Int128`, `UInt128`, `char`,
`Half`, `float`, `double`, `decimal`. The `TryConvertFromChecked` / `TryConvertFromSaturating`
/ `TryConvertFromTruncating` / `TryConvertToChecked` / `TryConvertToSaturating` /
`TryConvertToTruncating` methods are implemented on the interface for each.

Other instance helpers: `Upper` / `Lower` (high/low `UInt128` halves), `ToBigInteger()`.

## Performance

### Design Principles

Three rules drove the implementation and are the reason performance is competitive with the
best .NET 256-bit libraries:

1. **Never allocate on the hot path.** Arithmetic, formatting, parsing, comparison, and
   bitwise operations all operate entirely in registers / stack locals. The only allocations
   are (a) the `string` produced by `ToString`, and (b) temporary buffers during
   `BigInteger` interop. Everything else is zero-alloc.
2. **Route through native instructions wherever possible.** The multiply / divide / modulo
   kernels use `Bmi2.X64.MultiplyNoFlags` (MULX) and `X86Base.X64.DivRem` intrinsics on
   x64 where available, and fall back to pure 64-bit arithmetic with explicit carry chains
   everywhere else. No software-emulated `UInt128` path ever runs in a hot loop.
3. **Structure the code so the JIT can keep limbs in registers.** `UInt256` stores the
   value as four native `ulong` fields (`_p0`.._p3`). Arithmetic operators read those
   fields directly with no extraction overhead, and the JIT assigns a dedicated register
   to each limb. Using fused helpers like `BigMulAdd` for multiply avoids flag-dependency
   stalls. All of this is measurably faster than working through `UInt128` intermediates
   -- we verified this with BenchmarkDotNet under every configuration.

### Benchmark Results

Comparative benchmarks live in `BenchmarkSuite1/Int256LibraryComparisonBenchmarks.cs` and
measure `UInt256` alongside two other widely used managed 256-bit libraries:

- `Nethermind.Numerics.Int256` version **1.5.0** (NuGet).
- `MissingValues` version **2.2.1** (NuGet).

All benchmarks exercise identical operations on the same randomized input set. Each entry
reports the BenchmarkDotNet ratio relative to the Stardust baseline
(`Ratio = Mean_library / Mean_Stardust`). `1.00x` is the Stardust baseline; values above
`1.00x` are slower than the baseline, values below are faster.

`BigInteger` is included as the BCL reference: it is arbitrary-precision and heap-allocated,
so every operation returns a fresh object. The ratios illustrate the heap-allocation and
variable-width tax relative to a fixed-32-byte value type.

**`UInt256` — measured results, .NET 10 x64 (Release), 11th Gen Intel Core i7-11370H 3.30 GHz, median of three full runs:**

| Operation | `Stardust.Utilities.UInt256` | `Nethermind.Numerics.Int256` 1.5.0 | `MissingValues.UInt256` 2.2.1 | `System.Numerics.BigInteger` (BCL) |
|-----------|:-------------------:|:------------------------------------:|:------------------------------:|:-----------------------------------:|
| **Add** | **1.0x** (baseline) | 1.8x | 1.0x | 44x |
| **Sub** | **1.0x** (baseline) | 1.6x | 1.0x | 46x |
| **Mul** (full 256×256 → low 256) | **1.0x** (baseline) | 1.7x | 1.2x | 24x |
| **Div** (256 / 256) | **1.0x** (baseline) | 1.1x | 2.0x | 7.4x |
| **Mod** (256 % 256) | **1.0x** (baseline) | 1.2x | 2.2x | 7.0x |
| **ToString** (decimal) | **1.0x** (baseline) | 1.5x | 1.0x | 2.0x |
| **Parse** (decimal) | **1.0x** (baseline) | 4.1x | 2.2x | 4.4x |

**`Int256` — same machine, same methodology:**

| Operation | `Stardust.Utilities.Int256` | `Nethermind.Numerics.Int256` 1.5.0 | `MissingValues.Int256` 2.2.1 | `System.Numerics.BigInteger` (BCL) |
|-----------|:-------------------:|:------------------------------------:|:------------------------------:|:-----------------------------------:|
| **Add** | **1.0x** (baseline) | 1.7x | 1.0x | 80x |
| **Sub** | **1.0x** (baseline) | 1.7x | 1.0x | 71x |
| **Mul** (full 256×256 → low 256) | **1.0x** (baseline) | 2.4x | 1.3x | 45x |
| **Div** (256 / 256) | **1.0x** (baseline) | 1.1x | 2.3x | 9.3x |
| **Mod** (256 % 256) | **1.0x** (baseline) | 1.2x | 2.3x | 9.3x |
| **ToString** (decimal) | **1.0x** (baseline) | 1.6x | 1.0x | 2.3x |
| **Parse** (decimal) | **1.0x** (baseline) | 3.6x | 1.9x | 4.7x |

Ratios are rounded to 2 significant digits — the third digit lives in the run-to-run
noise band (see "How to read the ratios" below) and would be misleading precision.

### How to read the ratios

The numbers above are the **median of three complete benchmark runs** on the same
machine. Single-run ratios for the simple-arithmetic operations (Add, Sub, Mul,
ToString) fluctuate by ±5–10% between runs — large enough that any single-run claim of
a "win" or "loss" in that range would be cherry-picking. The ratios for Div, Mod, and
Parse are stable to within ~2% across runs because those operations spend longer per
call and are less sensitive to scheduling/cache noise.

The benchmark suite reports `RatioSD` (the standard deviation of the ratio
distribution within a single run) alongside each ratio. RatioSD is in the 0.01–0.04
range, but observed run-to-run variance is larger than RatioSD because hardware
thermals, OS scheduling, JIT codegen sequence, and cache state all shift between
invocations. Hardware, .NET runtime version, and competitor library version drift each
add another few percent on top when reproducing elsewhere.

For the purposes of these notes (using a deliberately conservative threshold):

- **Tied** (within ~10% across multiple runs): not distinguishable from noise on this
  hardware.
- **Small** (10–25%): real but modest.
- **Substantial** (> 25%): clear difference.

Applied to the **`UInt256`** table:

- vs **MissingValues**: Stardust is **substantially faster** on Div (2.0×), Mod
  (2.2×), and Parse (2.2×); **small win** on Mul (1.2×); **tied** on Add, Sub, and
  ToString (all ~1.0×).
- vs **Nethermind**: Stardust is **substantially faster** on Add (1.8×), Sub (1.6×),
  Mul (1.7×), ToString (1.5×), and Parse (4.1×); **small win** on Mod (1.2×) and Div
  (1.1×).

Applied to the **`Int256`** table:

- vs **MissingValues**: Stardust is **substantially faster** on Mul (1.3×), Div (2.3×),
  Mod (2.3×), and Parse (1.9×); **tied** on Add, Sub, and ToString.
- vs **Nethermind**: Stardust is **substantially faster** on Add (1.7×), Sub (1.7×),
  Mul (2.4×), ToString (1.6×), and Parse (3.6×); **small win** on Mod (1.2×) and Div
  (1.1×).
- vs **`BigInteger`**: arithmetic operations allocate 2–3 MB per 10 000-iteration run and
  trigger GC — the 7–46× ratios reflect allocation + GC cost, not raw instruction
  throughput. ToString and Parse are less penalized because all libraries must allocate a
  `string` there anyway.

Across these seven operations, Stardust is at parity with or faster than both
fixed-width competitors on every operation. The goal of this table is to demonstrate
that Stardust is at the achievable performance ceiling for managed 256-bit arithmetic,
not to claim a definitive ranking. Libraries evolve — re-run the suite against current
versions on your target hardware (and take the median of multiple runs) if the decision
matters.

Exact nanosecond values vary with CPU, memory bandwidth, .NET version, and library
version, so the table reports **ratios** rather than absolute times. Ratios are reasonably
stable across hardware when the same BDN config is used; absolute times should always be
re-measured on the target deployment hardware if precise budgets matter.

Notes on methodology:

- **Add, Sub, Mul** measure the full 256-bit operation; multiply returns the low 256 bits of
  the product (standard unsigned wrapping semantics). Sub uses modulo-2^256 wrap (unsigned
  borrow), consistent with all fixed-width types.
- **Div, Mod** use randomized divisors that span the full 256-bit range, so the benchmark
  exercises the general-case Knuth Algorithm D path rather than a short-divisor fast path.
- **ToString, Parse** use decimal format. Hex paths are typically faster for every library
  and are less discriminating, so they are not included in the headline table.
- For `BigInteger`, arithmetic results are masked with `(BigInteger.One << 256) - 1` to
  emulate fixed-width 256-bit wrap-around; without masking the accumulator would grow
  unboundedly and the benchmark would degenerate into measuring allocation size.
- All fixed-width entries report `Allocated: 0 B` except `ToString`, which must allocate
  the returned `string`. Every `BigInteger` arithmetic benchmark allocates one or more
  objects per iteration.

### Reproducing the Benchmarks

From the repository root:

```powershell
dotnet run -c Release --project BenchmarkSuite1 -- --filter '*Int256LibraryComparison*'
```

BenchmarkDotNet will print a table with absolute means, standard errors, ratios relative to
the Stardust baseline, and allocation counts. All entries should report `Allocated: 0 B`
except for `ToString` (which unavoidably allocates the result string).

The full benchmark file (`BenchmarkSuite1/Int256LibraryComparisonBenchmarks.cs`) is the
authoritative source for how each operation is measured. Read it first if a number in the
table above surprises you -- it documents the exact per-category setup.

## When to Use What

| Scenario | Use |
|----------|------|
| Arithmetic / comparison / hashing / dictionary keys | `UInt256` or `Int256` |
| Reading a fixed 32-byte big-endian unsigned field off the wire | `UInt256Be` (then convert to `UInt256` for math) |
| Reading a fixed 32-byte big-endian signed field off the wire | `Int256Be` (then convert to `Int256` for math) |
| Reading a fixed 32-byte little-endian unsigned field from a file | `UInt256Le` |
| Reading a fixed 32-byte little-endian signed field from a file | `Int256Le` |
| Named bit-field access within a 256-bit register | `[BitFields(typeof(UInt256))]` source generator |
| Arbitrary-precision integers that may exceed 256 bits | BCL `BigInteger` |
| Value fits in 128 bits | BCL `UInt128` / `Int128` |
| Value fits in 64 bits | `ulong` / `long` |

Stay on `UInt256` / `Int256` for everything that fits in 256 bits. `BigInteger` is the right
answer only when the width is genuinely unbounded; otherwise you pay for the heap allocation
and the indirection on every arithmetic operation for no gain.

---

*Last updated: 0.9.13 release.*
