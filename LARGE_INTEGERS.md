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
- [Performance](#performance)
  - [Design Principles](#design-principles)
  - [Benchmark Results](#benchmark-results)
  - [Reproducing the Benchmarks](#reproducing-the-benchmarks)
- [When to Use What](#when-to-use-what)

---

## Overview

| Type | Bits | Layout | Heap alloc | Representation |
|------|------|--------|------------|----------------|
| `UInt256` | 256 (unsigned) | Two `UInt128` halves (`_hi`, `_lo`) | Never | Host-native |
| `Int256` | 256 (signed, two's complement) | Two `UInt128` halves | Never | Host-native |
| `UInt256Le` / `UInt256Be` | 256 (unsigned) | 32 bytes, explicit layout | Never | Little/big-endian wire format |

`UInt256` and `Int256` are host-native in memory and are the type you want for arithmetic,
comparison and general-purpose use. `UInt256Le` / `UInt256Be` are byte-ordered types for
serialization and interop with protocols or files that demand a specific endianness.

Internally, both host-native types store the value as two `UInt128` halves. This matters:
the JIT on .NET 7+ lowers many `UInt128` operations to native 128-bit CPU instructions where
available (or to a tight pair of 64-bit ops otherwise), so `UInt256` arithmetic composes
directly onto whatever the platform supports without paying an object-allocation tax.

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
Use `UInt256Le` (little-endian, x86 native order) or `UInt256Be` (big-endian, network byte
order) at the I/O boundary and convert to `UInt256` for arithmetic:

```csharp
ReadOnlySpan<byte> wire = ...;                         // 32 bytes off the wire (BE)
UInt256Be be  = new(wire);
UInt256    hv = be;                                    // implicit conversion to host-native

UInt256 result = hv * 3 + 1;

// Write back out
Span<byte> outBuf = stackalloc byte[32];
((UInt256Be)result).WriteTo(outBuf);
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

| From | To | Conversion | Notes |
|------|-----|------------|-------|
| `ulong`, `uint`, `ushort`, `byte`, `UInt128` | `UInt256` | **Implicit** | Zero-extended |
| `UInt256` | `UInt128`, `ulong`, `uint`, `ushort`, `byte` | **Explicit** | Truncates to low bits |
| `UInt256` | `Int256` | **Explicit** | Bit-reinterpret (no value change) |
| `UInt256` | `BigInteger` | `.ToBigInteger()` or explicit operator | Always non-negative |
| `BigInteger` | `UInt256` | `UInt256.FromBigInteger(big)` | Throws `OverflowException` if negative or > 256 bits |
| `Int256` | `UInt256` | **Explicit** | Bit-reinterpret (two's-complement pattern preserved) |
| `Int128`, `long`, `int`, `short`, `sbyte` | `Int256` | **Implicit** | Sign-extended |
| `Int256` | `Int128`, `long`, `int`, `short`, `sbyte` | **Explicit** | Truncates, sign-sensitive |

All conversions are pure value-type operations -- no allocations, no runtime type checks.
`BigInteger` conversions are the exception (they must allocate because `BigInteger` itself is
heap-allocated), and they exist exclusively for boundary interop.

## API Surface

Both types implement:

- `IComparable`, `IComparable<T>`, `IEquatable<T>`
- `IFormattable`, `ISpanFormattable`
- `IParsable<T>`, `ISpanParsable<T>`

All standard operators: `+ - * / % & | ^ ~ << >> >>> == != < > <= >= ++ --`

Static factories: `Zero`, `One`, `MaxValue`, `MinValue`, `FromBigInteger`.

Instance helpers: `Upper` / `Lower` (high/low `UInt128` halves), `ToBigInteger()`,
`WriteLittleEndianBytes(Span<byte>)` (internal; used by the endian-aware wrappers).

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
3. **Structure the code so the JIT can keep limbs in registers.** Passing by value rather
   than by `in`, splitting limbs into named `ulong` locals at the top of an operator, and
   using fused helpers like `BigMulAdd` lets the JIT assign dedicated registers to each
   limb. This is measurably faster than letting the JIT decide for itself with a software
   `UInt128` struct -- we verified this with BenchmarkDotNet under every configuration.

### Benchmark Results

Comparative benchmarks live in `BenchmarkSuite1/Int256LibraryComparisonBenchmarks.cs` and
measure `UInt256` alongside two other widely used managed 256-bit libraries:

- `Nethermind.Numerics.Int256` version **1.5.0** (NuGet).
- `MissingValues` version **2.2.1** (NuGet).

All benchmarks exercise identical operations on the same randomized input set. Each entry
reports the BenchmarkDotNet ratio relative to the Stardust baseline
(`Ratio = Mean_library / Mean_Stardust`). `1.00x` is the Stardust baseline; values above
`1.00x` are slower than the baseline, values below are faster.

**Representative results, .NET 10 x64 (Release):**

| Operation | `Stardust.UInt256` | `Nethermind.Numerics.Int256` 1.5.0 | `MissingValues.UInt256` 2.2.1 |
|-----------|:-------------------:|:-------------------:|:------------------------:|
| **Add** | **1.00x** (baseline) | ~1.15x | ~1.00x |
| **Mul** (full 256x256 -> low 256) | **1.00x** (baseline) | ~1.30x | ~1.20x |
| **Div** (256 / 256) | **1.00x** (baseline) | ~1.05x | ~2.1x |
| **Mod** (256 % 256) | **1.00x** (baseline) | ~1.05x | ~2.1x |
| **ToString** (decimal) | **1.00x** (baseline) | ~1.40x | ~1.00x |
| **Parse** (decimal) | **1.00x** (baseline) | ~1.25x | ~1.10x |

Across these six operations, `Stardust.UInt256` is at parity with or faster than both of
the compared libraries on our test hardware and runtime. All three are high-quality
implementations, and the gaps are generally small; the main goal here is to demonstrate
that the type is competitive on the operations most code spends time in, not to claim a
definitive ranking. Libraries evolve -- re-run the suite against the current versions if
the decision matters for your project.

Exact nanosecond values vary with CPU, memory bandwidth, .NET version, and library
version, so the table reports **ratios** rather than absolute times. Ratios are reasonably
stable across hardware when the same BDN config is used; absolute times should always be
re-measured on the target deployment hardware if precise budgets matter.

Notes on methodology:

- **Add, Mul** measure the full 256-bit operation; multiply returns the low 256 bits of
  the product (standard unsigned wrapping semantics).
- **Div, Mod** use randomized divisors that span the full 256-bit range, so the benchmark
  exercises the general-case Knuth Algorithm D path rather than a short-divisor fast path.
- **ToString, Parse** use decimal format. Hex paths are typically faster for every library
  and are less discriminating, so they are not included in the headline table.
- All entries report `Allocated: 0 B` except `ToString`, which must allocate the returned
  `string`.

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
| Reading a fixed 32-byte big-endian field off the wire | `UInt256Be` (then convert to `UInt256` for math) |
| Reading a fixed 32-byte little-endian field from a file | `UInt256Le` |
| Named bit-field access within a 256-bit register | `[BitFields(typeof(UInt256))]` source generator |
| Arbitrary-precision integers that may exceed 256 bits | BCL `BigInteger` |
| Value fits in 128 bits | BCL `UInt128` / `Int128` |
| Value fits in 64 bits | `ulong` / `long` |

Stay on `UInt256` / `Int256` for everything that fits in 256 bits. `BigInteger` is the right
answer only when the width is genuinely unbounded; otherwise you pay for the heap allocation
and the indirection on every arithmetic operation for no gain.

---

*Last updated: 0.9.9 release.*
