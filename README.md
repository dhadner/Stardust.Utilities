# Stardust.Utilities

[![CI/CD](https://github.com/dhadner/Stardust.Utilities/actions/workflows/ci.yml/badge.svg)](https://github.com/dhadner/Stardust.Utilities/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Stardust.Utilities.svg)](https://www.nuget.org/packages/Stardust.Utilities/)
[![.NET 7](https://img.shields.io/badge/.NET-7.0-512BD4)](https://dotnet.microsoft.com/)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

[![Stardust Utilities](https://raw.githubusercontent.com/dhadner/Stardust.Utilities/main/icon.png)](https://github.com/dhadner/Stardust.Utilities)

**Zero-cost bit manipulation, Rust-style error handling, endian-safe integers, and fixed-width 256-bit math for modern .NET.** One NuGet package. One source generator. No runtime dependencies.

Stardust.Utilities is a *library*, not a framework. You pick the pieces you need and use them exactly where you need them -- parsing a packet, decoding a register, shaping an API result. Nothing runs on its own, nothing calls home, and nothing ships with your binary except the code you touch.

---

## Table of Contents

- [Why Stardust.Utilities?](#why-stardustutilities)
- [See It In One Snippet](#see-it-in-one-snippet)
- [Try It Live](#try-it-live)
- [Installation](#installation)
- [What's In The Box](#whats-in-the-box)
- [Features](#features)
  - [BitFields](#bitfields)
  - [Result Types](#result-types)
  - [Option Types](#option-types)
  - [Endian Types](#endian-types)
  - [Large Integers (256-bit)](#large-integers-256-bit)
  - [Extension Methods](#extension-methods)
  - [RFC Diagram Generator](#rfc-diagram-generator)
  - [Pre-Defined Numeric Types](#pre-defined-numeric-types)
- [Compatibility](#compatibility)
- [Common Gotchas](#common-gotchas)
- [What's New](#whats-new)
- [License](#license)
- [Contributing](#contributing)
- [Security](#security)
- [Privacy](#privacy)

---

## Why Stardust.Utilities?

- **Zero abstraction penalty.** Source-generated `[BitFields]` types compile to the same IL as hand-coded shift/mask -- benchmark is statistically identical to raw bit manipulation across 160 independent runs ([BITFIELDS.md -- Performance](BITFIELDS.md#performance)).
- **Zero heap allocations.** Every value-type operation -- arithmetic, parsing, formatting, JSON round-trips, span I/O -- is stack-only. Value types never box.
- **Competitive 256-bit math.** `UInt256` / `Int256` match or beat the leading managed 256-bit libraries on the operations that matter (add, sub, mul, div, parse, format). See the comparison table in the [Large Integers](#large-integers-256-bit) section.
- **No runtime dependency.** The source generator runs at build time and ships only as an analyzer. Your compiled app picks up nothing extra -- no second DLL, no reflection, no interface-based dispatch.
- **Full modern .NET surface, generated for you.** `IParsable<T>`, `ISpanParsable<T>`, `ISpanFormattable`, `System.Text.Json`, full operator overloading, implicit conversions, span-based serialization -- all produced by the generator, not hand-written per struct.
- **Rust-inspired ergonomics, no religion.** `Result<T, TError>` and `Option<T>` live alongside normal exceptions. Use them where they fit; don't let them take over the codebase.
- **Runs everywhere .NET 7+ runs.** Multi-targets `net7.0` / `net8.0` / `net9.0` / `net10.0`. Tested on x64, ARM64, and big-endian architectures (via QEMU in CI).

If you have ever hand-rolled a bit-packed protocol header, juggled `BinaryPrimitives.ReverseEndianness`, or written the same `if (result.IsOk) ... else ...` boilerplate for the twentieth time -- this package removes that code.

---

## See It In One Snippet

Parse a live IPv4 header straight out of the packet buffer, zero copies:

```csharp
using Stardust.Utilities;

// Define the RFC 791 layout once. No hand-written shift/mask anywhere.
[BitFields(ByteOrder.BigEndian, BitOrder.BitZeroIsMsb)]
public partial record struct IPv4Header
{
    [BitField(0,   3)]  public partial byte   Version      { get; set; }
    [BitField(4,   7)]  public partial byte   IHL          { get; set; }
    [BitField(8,  15)]  public partial byte   TypeOfService{ get; set; }
    [BitField(16, 31)]  public partial ushort TotalLength  { get; set; }
    [BitField(64, 71)]  public partial byte   TTL          { get; set; }
    [BitField(72, 79)]  public partial byte   Protocol     { get; set; }
    [BitField(96, 127)] public partial uint   SourceIP     { get; set; }
}

// Read directly from the packet. No allocations, no copies.
byte[] packet = ReceiveFromNetwork();
var hdr = new IPv4Header(packet);

if (hdr.Version == 4 && hdr.Protocol == 6)
    Console.WriteLine($"TCP packet, length {hdr.TotalLength}, TTL {hdr.TTL}");
```

The generator emits inline bit-shift/mask property implementations with compile-time constants. There is no runtime reflection, no boxing, and no allocation. See [BITFIELDS.md](BITFIELDS.md) for the full model.

---

## Try It Live

**[Launch the interactive web demo](https://dhadner.github.io/Stardust.Utilities/)** -- explore `[BitFields]`, PE headers, network packets, CPU registers, and RFC diagrams directly in your browser. No install required.

Prefer video? [Watch a walkthrough of the demo app.](https://github.com/dhadner/Stardust.Utilities/blob/main/Graphics/DemoWebVideo.mp4)

| RFC bit-field diagram | PE header viewer | Composable floating-point lab |
|---|---|---|
| ![RFC Diagram](https://raw.githubusercontent.com/dhadner/Stardust.Utilities/main/Graphics/TCPHeaderDiagram.png) | ![PE Viewer](https://raw.githubusercontent.com/dhadner/Stardust.Utilities/main/Graphics/PEHeaderViewDemo.png) | ![FP Lab](https://raw.githubusercontent.com/dhadner/Stardust.Utilities/main/Graphics/FP-Lab-Demo.png) |

---

## Installation

```xml
<PackageReference Include="Stardust.Utilities" />
```

That's it. NuGet restores the latest stable version; the badge above shows what that is today. To pin a specific version add `Version="x.y.z"` -- useful for reproducible builds, but most projects don't need it.

---

## What's In The Box

| Feature | What it does | Deep dive |
|---|---|---|
| **BitFields value types** | Generate zero-cost bit-packed structs with operators, parsing, JSON, and span I/O | [BITFIELDS.md](BITFIELDS.md) |
| **BitFields zero-copy views** | Read/write bits directly on a `Memory<byte>` buffer -- no copies | [BITFIELDS.md](BITFIELDS.md#quick-start-zero-copy-view-record-struct) |
| **Result&lt;T, TError&gt; / Result&lt;TError&gt;** | Explicit railway-oriented error handling without exceptions | [RESULT.md](RESULT.md) |
| **Option&lt;T&gt;** | Explicit optional values without `null`, zero-cost on the hot path | [OPTION.md](OPTION.md) |
| **Endian types (16 through 256 bits)** | Type-safe big- and little-endian integers that compile to a single `BSWAP` | [ENDIAN.md](ENDIAN.md) |
| **UInt256 / Int256** | Fixed-width 256-bit arithmetic with native CPU instructions (BMI2, DivRem) | [LARGE_INTEGERS.md](LARGE_INTEGERS.md) |
| **Hi / Lo / Saturating extensions** | Upper/lower byte access and clamped arithmetic on every integer type | [EXTENSIONS.md](EXTENSIONS.md) |
| **RFC diagram generator** | Render ASCII bit-field diagrams from any `[BitFields]` struct at runtime | [BITFIELDS.md -- RFC Diagram Generator](BITFIELDS.md#rfc-diagram-generator) |
| **IEEE 754 / Decimal decomposers** | Pre-built `[BitFields]` types for `Half`, `float`, `double`, `decimal` | [BITFIELDS.md -- Numeric Decomposition Types](BITFIELDS.md#numeric-decomposition-types) |

---

## Features

### BitFields

**One attribute. Two struct kinds. Hardware-speed bit access with no boilerplate.**

Annotate a `partial struct` for a self-contained value type, or a `partial record struct` for a zero-copy view over an external `Memory<byte>` buffer. Both use the same `[BitField]` / `[BitFlag]` property attributes and produce identical JSON and span I/O:

```csharp
using Stardust.Utilities;

// Value type: backed by a storage primitive (byte, uint, ulong, UInt128, UInt256, ...)
[BitFields(StorageType.Byte)]
public partial struct StatusRegister
{
    [BitFlag(0)]               public partial bool Ready    { get; set; }
    [BitFlag(1)]               public partial bool Error    { get; set; }
    [BitFlag(7)]               public partial bool Busy     { get; set; }
    [BitField(2, End = 4)]     public partial byte Mode     { get; set; }  // 3 bits
    [BitField(5, End = 6)]     public partial byte Priority { get; set; }  // 2 bits
}

StatusRegister reg = 0x42;
reg.Ready = true;
reg.Mode  = 5;                          // bits 2-4
byte raw  = reg;                        // implicit conversion to storage type
var next  = reg.WithPriority(2);        // fluent, immutable-style update
string j  = JsonSerializer.Serialize(reg);  // "0x4D"
```

**Proven zero-overhead** (500M iterations, .NET 10 -- full methodology in [BITFIELDS.md](BITFIELDS.md#performance)):

| Operation | Raw bit ops | Generated property | Difference |
|---|---|---|---|
| Boolean GET | 271 ms | 263 ms | ~0% (noise) |
| Boolean SET | 506 ms | 494 ms | ~0% (noise) |
| Field GET (shift+mask) | 124 ms | 123 ms | ~0% (noise) |

**Choosing between the two shapes:**

| | `partial struct` (value type) | `partial record struct` (view) |
|---|---|---|
| Backing | Private value field | External `Memory<byte>` |
| Copy cost | Copies all data | Copies a 24-byte view header |
| Max size | ~16 KB (or up to 16,384 bits via `[BitFields(N)]`) | Unlimited |
| Operators | Full arithmetic, bitwise, comparison | None (it is a view, not a value) |
| Typical use | Registers, opcodes, flags, network fields | Network packets, file headers, DMA buffers |

**Key benefits:**

- Identical-to-hand-coded performance, confirmed by statistical benchmarking.
- Full modern .NET surface generated per type: `IParsable<T>`, `ISpanParsable<T>`, `ISpanFormattable`, `IComparable<T>`, `IEquatable<T>`, `System.Text.Json` converter, span constructors, `ReadFrom`/`WriteTo`/`TryWriteTo`, `ToByteArray`.
- Storage types from `byte` up to `UInt256`, plus auto-sizing, plus arbitrary-width `[BitFields(N)]` from 1 to 16,384 bits.
- `MustBe` / `UndefinedBitsMustBe` enforce reserved-bit invariants at every entry point with under 1 ns construction overhead and zero getter overhead.
- Composition: value types nest in views, views nest in views, `UInt256` nests in anything -- each nested type carries its own byte/bit order.

See [BITFIELDS.md](BITFIELDS.md) for the complete attribute reference, storage types, composition rules, JSON/span I/O, compiler diagnostics, and real-world protocol examples.

---

### Result Types

**Railway-oriented error handling without exceptions.** Inspired by Rust's `Result<T, E>`.

```csharp
// Enable clean Ok() / Err() syntax via global using
global using static Stardust.Utilities.Result<int, string>;

Result<int, string> Divide(int a, int b) =>
    b == 0 ? Err("Division by zero") : Ok(a / b);

var message = Divide(10, 2)
    .Map(x => x * 2)                         // transform the value
    .AndThen(x => Divide(x, 4))              // chain another Result
    .Inspect(x => Console.WriteLine(x))      // side effect on success
    .MapOrElse(
        onOk:  v => $"Answer: {v}",
        onErr: e => $"Failed: {e}");
```

**Key benefits:**

- Explicit success/failure in the type signature -- impossible to forget error handling.
- Full monadic surface: `Map`, `MapErr`, `AndThen`, `OrElse`, `Inspect`, `InspectErr`, `Flatten`, `Transpose`, `MapOr`, `MapOrElse`.
- Interop with `Option<T>` via `ToOption` / `OkOr` / `Transpose`.
- Void form `Result<TError>` for save/send/delete operations that succeed or fail but return nothing.
- Works with `async`/`await` end-to-end.

See [RESULT.md](RESULT.md) for the full API, custom error types, real-world examples, and async patterns.

---

### Option Types

**Explicit optional values without `null`.** Inspired by Rust's `Option<T>`.

```csharp
global using static Stardust.Utilities.Option;

Option<int> ParsePositive(string s) =>
    int.TryParse(s, out var n) && n > 0 ? Some(n) : None;

int port = ParsePositive(input).UnwrapOr(8080);

var email = GetUserById(id)
    .AndThen(user => user.Email)
    .Filter(e => e.Contains('@'));
```

**Zero-cost on the hot path** (100M iterations, .NET 10):

| Operation | `Option<T>` | `T?` | Difference |
|---|---|---|---|
| Create Some | 35 ms | 34 ms | ~0% (noise) |
| `IsSome` check | 46 ms | 49 ms | ~0% (noise) |
| `UnwrapOr` (`??`) | 122 ms | 127 ms | ~0% (noise) |

**Key benefits:**

- State checks, value extraction, and defaults are statistically identical to hand-written `T?` code.
- Delegate-based transforms (`Map`, `AndThen`, `Filter`) add 1-2 ns -- fine outside tight inner loops.
- Bidirectional interop with `Result<T, TError>`.
- `None` as a static singleton means no allocation on the `None` branch.

See [OPTION.md](OPTION.md) for the full API, performance analysis, interop patterns, and async support.

---

### Endian Types

**Type-safe endian-aware integers at native speed.** 16 types covering 16 through 256 bits, big-endian and little-endian, signed and unsigned.

```csharp
using Stardust.Utilities;

// Big-endian wire values with native arithmetic
UInt32Be seq = 0x12345678;                // stored as 12 34 56 78
UInt32Be next = seq + 1;                  // arithmetic works natively
Span<byte> buf = stackalloc byte[4];
next.WriteTo(buf);                         // zero-allocation serialization

// Read back from a packet
var incoming = new UInt32Be(receivedBytes);
uint native  = incoming;                   // implicit conversion
```

**No performance penalty** (BenchmarkDotNet, .NET 10):

| Width | `Le` vs. native | `Be` vs. hand-coded `BSWAP` |
|---|---|---|
| 16-bit | **1.0x** | **1.0x** (single register rotate) |
| 32-bit | **1.0x** | **1.0x** (single `BSWAP`) |
| 64-bit | **1.0x** | **1.0x** (single `BSWAP`) |
| 128-bit | **1.0x** | **1.0x** (two `BSWAPs` + swap halves) |
| 256-bit | **1.0x** | 7-19x faster than `BigInteger`-backed alternatives |

**Key benefits:**

- `[StructLayout(LayoutKind.Explicit)]` guarantees byte ordering in memory regardless of host.
- Full operator set: arithmetic, bitwise, shift, comparison, equality.
- Modern .NET interfaces generated: `IParsable<T>`, `ISpanParsable<T>`, `ISpanFormattable`.
- Zero-allocation `ReadOnlySpan<byte>` constructors plus `WriteTo` / `TryWriteTo`.
- PropertyGrid `TypeConverter` for every type.
- Per-field endian override inside `[BitFields]` views -- mix big- and little-endian fields in the same buffer.

See [ENDIAN.md](ENDIAN.md) for the full type matrix, benchmark tables, migration guide from manual `ReverseEndianness`, and real-world examples.

---

### Large Integers (256-bit)

**Fixed-width 256-bit signed and unsigned integers that are competitive with the leading managed alternatives on every operation that matters.**

`UInt256` and `Int256` fill the gap between the BCL's `UInt128` / `Int128` and the heap-allocated, arbitrary-precision `BigInteger`. They are the right tool for cryptographic digests, Ethereum / EVM-style values, large accumulators, GUID-scale identifiers, and any fixed-32-byte quantity that needs real arithmetic -- not just byte manipulation.

```csharp
using Stardust.Utilities;

UInt256 a = 42;
UInt256 b = UInt256.Parse("115792089237316195423570985008687907853269984665640564039457584007913129639935");
UInt256 hash = (a * 31 + b) ^ UInt256.MaxValue;

// Zero-allocation wire I/O
ReadOnlySpan<byte> wire = ReceivePacket();   // 32 bytes, network order
UInt256Be be = new(wire);
UInt256 value = be;                           // implicit conversion to host-native
```

**Competitive with the leading managed 256-bit libraries** (.NET 10 x64, lower = faster; `1.00x` is the Stardust baseline):

| Operation | `Stardust.Utilities.UInt256` | `Nethermind.Numerics.Int256` 1.5.0 | `MissingValues.UInt256` 2.2.1 | `BigInteger` (BCL) |
|---|:---:|:---:|:---:|:---:|
| Add | **1.00x** | 1.49x | 1.00x | 38.5x |
| Sub | **1.00x** | 1.70x | 1.00x | 46.3x |
| Mul | **1.00x** | 1.73x | 1.01x | 25.3x |
| Div | **1.00x** | 1.14x | 2.08x | 7.36x |
| Mod | **1.00x** | 1.17x | 2.16x | 7.22x |
| ToString | **1.00x** | 1.58x | 1.08x | 1.98x |
| Parse | **1.00x** | 4.32x | 2.27x | 4.60x |

`BigInteger` allocates 2-3 MB per 10,000-iteration run and triggers GC; the 7-46x ratios reflect that cost, not raw throughput. All three fixed-width libraries are high-quality implementations. Library versions move -- re-run `BenchmarkSuite1/Int256LibraryComparisonBenchmarks.cs` against current packages if the decision matters.

**Key benefits:**

- Zero heap allocations on the hot path (`readonly struct` backed by four `ulong` values).
- Native CPU instructions: multiply via BMI2 `MULX`, divide via `X86Base.X64.DivRem`, with hand-rolled carry-chain fallbacks on ARM64 and non-x64 platforms.
- Full operator and interface surface -- drop-in replacement for `UInt128` / `BigInteger` where 32-byte fixed width is the right shape.
- Wire-format safety via `UInt256Be` / `UInt256Le` with zero conversion cost on LE hardware.
- First-class `[BitFields]` citizen: `[BitFields(typeof(UInt256))]` gets a 4-ulong backing store with full operator and JSON support.
- Fuzz-tested on every build: `Int256NativeArithmeticTests` cross-validates every operation against `BigInteger` on randomized inputs.

See [LARGE_INTEGERS.md](LARGE_INTEGERS.md) for the full API, benchmark methodology, and guidance on choosing between `UInt256`, `UInt128`, and `BigInteger`.

---

### Extension Methods

**Upper/lower byte access and clamped arithmetic on every integer type, native or endian.**

```csharp
using Stardust.Utilities;

ushort word = 0x1234;
byte hi = word.Hi();               // 0x12
byte lo = word.Lo();               // 0x34
word = word.SetHi(0xFF);           // 0xFF34

// Saturating arithmetic: clamps instead of overflowing
int     r1 = int.MaxValue.SaturatingAdd(1);                     // int.MaxValue
uint    r2 = 10u.SaturatingSub(20u);                            // 0
UInt256 r3 = UInt256.MaxValue.SaturatingAdd(new UInt256(1UL));  // UInt256.MaxValue
Int256  r4 = Int256.MinValue.SaturatingSub(new Int256(1L));     // Int256.MinValue
```

**Key benefits:**

- Works uniformly on native (`ushort` ... `Int256`), big-endian (`UInt16Be` ... `Int256Be`), and little-endian types.
- `Hi()` / `Lo()` return the upper/lower half as the appropriate half-width type.
- `SetHi()` / `SetLo()` produce a new value -- no mutation, no allocation.
- `SaturatingAdd` / `SaturatingSub` clamp to the type's representable range.

See [EXTENSIONS.md](EXTENSIONS.md) for the full method list and performance notes.

---

### RFC Diagram Generator

**Render RFC 2360-style ASCII bit-field diagrams from any `[BitFields]` struct at runtime.**

```csharp
var diagram = new BitFieldDiagram(typeof(IPv4HeaderView));
string output = diagram.RenderToString().Value;

// Render multiple structs in a single diagram with consistent scale
var combined = new BitFieldDiagram(
    [typeof(M68020DataRegisters), typeof(M68020SR)],
    description: "68020 Registers");
Console.WriteLine(combined.RenderToString().Value);
```

Cells auto-size to fit field names, byte offsets label each row, and undefined bits are clearly marked. See [BITFIELDS.md -- RFC Diagram Generator](BITFIELDS.md#rfc-diagram-generator) for configuration options and examples.

---

### Pre-Defined Numeric Types

Four pre-built `[BitFields]` types that decompose .NET numeric types into their constituent fields. Just `using Stardust.Utilities;` and start using them -- no struct definitions required.

| Type | Storage | Use case |
|---|---|---|
| `IEEE754Half` | `Half` (16-bit) | Half-precision analysis |
| `IEEE754Single` | `float` (32-bit) | Single-precision analysis |
| `IEEE754Double` | `double` (64-bit) | Double-precision analysis |
| `DecimalBitFields` | `decimal` (128-bit) | Decimal inspection |

```csharp
IEEE754Double pi = Math.PI;
pi.Sign;            // false
pi.BiasedExponent;  // 1024 (raw stored value, includes +1023 bias)
pi.Exponent;        // 1    (true mathematical power: 2^1, since 2 <= pi < 4)
pi.Mantissa;        // 0x921FB54442D18
pi.IsNormal;        // true

var rebuilt = IEEE754Double.Zero
    .WithExponent(pi.Exponent!.Value)
    .WithMantissa(pi.Mantissa);
double result = rebuilt;   // == Math.PI
```

All four types include implicit conversions to and from their storage type, full operator support, classification properties (`IsNormal`, `IsNaN`, `IsInfinity`, `IsDenormalized`, `IsZero`), both the raw `BiasedExponent` field and a computed `Exponent` property that removes the bias, and a `WithExponent(int)` fluent method that sets the exponent from its true mathematical value.

See [Numeric Decomposition Types](BITFIELDS.md#numeric-decomposition-types) in BITFIELDS.md for full details, bit-layout diagrams, constants, and classification reference.

---

## Compatibility

| Dimension | Supported |
|---|---|
| Target frameworks | `net7.0`, `net8.0`, `net9.0`, `net10.0` |
| Process architectures | x86, x64, ARM64 (nint/nuint diagnostics steer you around 32-bit pitfalls) |
| Host endianness | Little-endian (x86/x64/ARM) native; big-endian fully supported and CI-tested via QEMU |
| Runtime deps | None (source generator ships as an analyzer; no extra DLL in your output) |
| Trimming / AOT | Value types and generated code are trim-safe; no reflection on the hot path |
| Compiler requirement | C# 13+ (VS 2022 17.0+ for incremental generator support) |

---

## Common Gotchas

Three most-frequent issues and their fixes. For the full diagnostic reference (every `SD00xx` error and warning, browser/WASM notes, how to view generated code) see [TROUBLESHOOTING.md](TROUBLESHOOTING.md).

- **`CS9248: partial property must have an implementation part`** -- add the `partial` keyword to every `[BitField]` / `[BitFlag]` property. If you also see `SD0004`, the generator is pointing directly at the missing keyword.
- **Generated code didn't update** -- make sure the type is `partial struct` or `partial record struct` (not `class`), then clean and rebuild.
- **IntelliSense missing for generated members** -- build the project at least once, then close and reopen the solution if needed.

---

## What's New

Every release gets a dated entry in [CHANGELOG.md](CHANGELOG.md) covering added features, behavior changes, deprecations, and backwards compatibility notes. On nuget.org the same information appears on the package's **Release Notes** tab.

---

## License

MIT. See [LICENSE](https://github.com/dhadner/Stardust.Utilities/blob/main/LICENSE).

---

## Contributing

Contributions are welcome. Please read the guidelines before submitting issues or pull requests.

- [CONTRIBUTING.md](https://github.com/dhadner/Stardust.Utilities/blob/main/CONTRIBUTING.md)
- [CODE_OF_CONDUCT.md](https://github.com/dhadner/Stardust.Utilities/blob/main/CODE_OF_CONDUCT.md)

---

## Security

To report a security vulnerability, please use GitHub's private vulnerability reporting feature. **Do not report security issues through public GitHub issues.** See [SECURITY.md](SECURITY.md) for details.

---

## Privacy

Stardust.Utilities does not collect, transmit, or store any personal data, telemetry, or usage information. See [PRIVACY.md](PRIVACY.md) for details.
