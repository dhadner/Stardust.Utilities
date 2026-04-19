# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project will adhere to [Semantic Versioning](https://semver.org/spec/v2.0.0.html) once version 1.0 is released.

## [0.9.9] - Release date TBD

### 🟢 Added
- **`UInt256` and `Int256` value types** -- fixed-width 256-bit unsigned and signed integer types backed by two `UInt128` halves. Zero heap allocations on the hot path, routed through BMI2 `MULX` (`Bmi2.X64.MultiplyNoFlags`) for multiply and the `X86Base.X64.DivRem` hardware intrinsic for divide / modulo / decimal chunk extraction on x64 .NET 8+. Full operator set (`+ - * / % & | ^ ~ << >> >>> == != < > <= >= ++ --`), parsing (decimal and hex), formatting (`D`, `G`, `R`, `X`, `x`, with precision specifiers), `BigInteger` interop, and implicit / explicit conversions to the full width spectrum (`byte` through `UInt128` / `Int128`). Implements `IComparable`, `IComparable<T>`, `IEquatable<T>`, `IFormattable`, `ISpanFormattable`, `IParsable<T>`, `ISpanParsable<T>`. Benchmarked alongside `Nethermind.Numerics.Int256` 1.5.0 and `MissingValues` 2.2.1 with `BenchmarkSuite1/Int256LibraryComparisonBenchmarks.cs` -- competitive with both libraries across Add, Mul, Div, Mod, ToString, and Parse on our test hardware; see [LARGE_INTEGERS.md](LARGE_INTEGERS.md) for the full table, methodology, and instructions for reproducing on your own hardware. See also [ENDIAN.md](ENDIAN.md) for the endian-aware wrappers.
- **`UInt256Be` / `UInt256Le` endian-aware 256-bit types** -- 32-byte explicit-layout structs for wire-format interop. Implicit conversion to/from the host-native `UInt256`. Zero-allocation `WriteTo(Span<byte>)` / `ReadFrom(ReadOnlySpan<byte>)` / `TryWriteTo` / `ToByteArray` API identical to the 16/32/64/128-bit endian types.
- **`UInt256` and `Int256` as `[BitFields]` storage types** -- both `[BitFields(typeof(UInt256))]` / `[BitFields(typeof(Int256))]` (and the corresponding `StorageType.UInt256` / `StorageType.Int256` enum values) are now first-class. The generator emits a 4-ulong backing store (`__w0`..`__w3`), implicit conversions between the generated struct and `UInt256` / `Int256`, full arithmetic and bitwise operators via the existing multi-word code path, JSON serialization, span I/O, `With...` fluent methods, and field metadata. Also wired into `RecordStructViewGenerator` for use as an embedded property type in zero-copy views. 13 new tests added to `BitFieldSpecializationTests` covering round-trip construction (`U256Register_FromUInt256_RoundTrips`, `_Zero`, `_MaxValue`, signed positive / negative / min / max), field-level read/write (`U256Register_FieldAccess_ViaConversion`, `U256Register_SetFields_ConvertBack`), struct size (`_SizeIs32Bytes`), arithmetic (`U256Register_Addition`, `U256Register_Addition_CarriesAcrossAllWordBoundaries` -- verifies the full 256-bit carry chain), and bitwise operations.
- **Auto-sized `[BitFields]` on value-type structs**
- **Cross-type widening/narrowing conversions for all Little-Endian types** -- Le types now have the same cross-type endian-to-endian implicit widening and explicit narrowing conversions that Be types have. Widening (implicit): `UInt16Le` to `UInt32Le`/`UInt64Le`/`UInt128Le`, `UInt32Le` to `UInt64Le`/`UInt128Le`, `UInt64Le` to `UInt128Le`, and equivalent signed conversions (`Int16Le` to `Int32Le`/`Int64Le`/`Int128Le`, etc.). Narrowing (explicit): the reverse direction. Also includes `Int16Le` to `UInt32Le` (matching `Int16Be` to `UInt32Be`) and `Int32Le(Int16Le)` explicit sign-extending conversion (matching `Int32Be(Int16Be)`).
- **`SaturatingAdd` / `SaturatingSub` extension methods for all native integer types** -- complete coverage from 8-bit through 128-bit: `byte`/`sbyte`, `short`/`ushort`, `int`/`uint`, `long`/`ulong`, `Int128`/`UInt128`. Previously only `int`/`uint`/`long`/`ulong` were supported. Unsigned types clamp to `0`/`MaxValue`; signed types clamp to `MinValue`/`MaxValue`.
- **`SaturatingAdd` / `SaturatingSub` extension methods for all big-endian and little-endian types** -- every endian type from `UInt16Be`/`Int16Be` through `UInt128Be`/`Int128Be`, and `UInt16Le`/`Int16Le` through `UInt128Le`/`Int128Le`, now supports `SaturatingAdd` and `SaturatingSub`. Each delegates to the corresponding native-type saturating method for zero-overhead correctness. Combined with the native-type methods, this completes saturating arithmetic support across every integer type in the library.
- **Hi/Lo/SetHi/SetLo extension methods now cover all sizes** -- native types (`ushort`/`short` through `UInt128`/`Int128`), big-endian types (`UInt16Be`/`Int16Be` through `UInt128Be`/`Int128Be`), and little-endian types (`UInt16Le`/`Int16Le` through `UInt128Le`/`Int128Le`) all have `Hi`/`Lo`/`SetHi`/`SetLo`. 8-bit types are excluded (they are the atomic unit with no halves to split).
- **Performance benchmarks for saturating arithmetic and endian types** -- BenchmarkDotNet suites document the cost of `SaturatingAdd`/`SaturatingSub` vs. normal arithmetic (see EXTENSIONS.md) and the cost of Be/Le types vs. native primitives (see ENDIAN.md). Results tables with key takeaways are included in the documentation to help users make informed choices.

### 🟣 Changed
- **Endian type conversions optimized to native speed** -- All Le types (16/32/64/128-bit) now operate at native CPU speed on little-endian hardware (x86/x64/ARM64), compiling to the same machine code as bare primitive operations. All Be types now compile to a single `BSWAP` instruction per conversion -- the same code a developer would hand-write, with no additional overhead from the type abstraction. Previously, conversions went through multi-level field decomposition costing 4--21x; now both Le and Be types run at the speed of optimal hand-coded byte manipulation. 16-bit types use an overlapping `[FieldOffset(0)] _value` field aliased with the byte fields, giving the JIT a single primitive to keep in CPU registers while preserving direct byte access. 32-bit and wider types use `Unsafe.As` for zero-cost memory reinterpretation. Hi/Lo extensions for 16-bit Be types refactored to use the fast native conversion path, avoiding load-after-store stalls from the field aliasing. All 549 endian and extension tests pass.
- **Missing operators added to signed Le types (Int16Le, Int32Le, Int64Le)** -- `*`, `/`, `%`, `&`, `|`, `^`, `~`, `>>`, `<<` were present on all unsigned Le types and all signed Be types but missing from the three signed Le types. All signed Le types now have full operator parity with their Be counterparts. Includes sign-extending right shift (`>>`) with test coverage. 33 new tests added.
- **Missing operators added to UInt32Le, UInt32Be, UInt64Le, Int32Be** -- `%`, `^` added to UInt32Le and UInt64Le; `++`, `--`, `%`, `^`, `~` added to UInt32Be and Int32Be. All 16 endian types now have the complete operator set.
- **Unsigned right shift (`>>>`) added to all 16 endian types** -- All signed types (Int16Le/Be through Int128Le/Be) implement `>>>` as a logical right shift that zero-fills from the left, matching the C# 11 semantics. All unsigned types (UInt16Le/Be through UInt128Le/Be) also define `>>>` for API parity with native `uint`/`ulong`/`ushort`/`UInt128`, where it is identical to `>>`. The `>>>=` compound assignment is automatically synthesized by the compiler. 11 new tests added.
- **`[MethodImpl(MethodImplOptions.AggressiveInlining)]` added to all operators across all 16 endian types** -- every operator and conversion operator on every Be and Le type is now explicitly marked for aggressive inlining. While the JIT already inlines these one-liner methods, the attribute makes the performance contract visible and ensures inlining even under tight inlining budgets in deeply nested call chains.
- **`BIT_WIDTH` constant added to all generated types**
- **`StructDescription` and `StructDescriptionResourceType` added to record struct views** -- these static properties were previously only generated on value-type structs. They are now emitted on record struct views as well, providing programmatic access to the `[BitFields(Description = ...)]` value for all struct kinds. `BitFieldDiagram` already read this data from `BitFieldInfo` metadata so diagram rendering was not affected; the new properties enable direct access without going through `Fields`.
- **All generated private members now use `__` prefix** -- every private field and constant emitted by the source generator has been prefixed with double underscores to eliminate naming collisions with user-declared members. Single-word value-type structs: `Value` renamed to `__value`. Multi-word structs: `_w0`/`_w1`/etc. renamed to `__w0`/`__w1`/etc., `WORD_COUNT`/`TOTAL_BITS`/`LAST_WORD_MASK` renamed to `__WORD_COUNT`/`__TOTAL_BITS`/`__LAST_WORD_MASK`. Record struct views: `_data`/`_bitOffset` renamed to `__data`/`__bitOffset`. Per-field mask/shift/bit constants (e.g., `MODE_MASK`, `MODE_START_BIT`, `MODE_SHIFTED_MASK`, `MODE_INVERTED_MASK`, `MODE_SAT_MIN`, `MODE_SAT_MAX`) are now prefixed as `__MODE_MASK`, `__MODE_START_BIT`, etc. Fixed-name constants `NORMALIZATION_AND_MASK`/`NORMALIZATION_OR_MASK` are now `__NORMALIZATION_AND_MASK`/`__NORMALIZATION_OR_MASK`. The double-underscore prefix follows the C# convention for compiler/generator-reserved identifiers and frees users to name their properties anything they want without collision.
- **`UndefinedBits` property on `[BitFields]` is now settable** -- the `UndefinedBits` property can now be set as a named argument (e.g., `[BitFields(UndefinedBits = UndefinedBitsMustBe.Zeroes)]`) in addition to the existing constructor parameter. This is especially useful with auto-sized structs where there is no constructor parameter for `UndefinedBitsMustBe`.

### 🟤 Backwards Compatibility
- **`UInt256` / `Int256` and `StorageType.UInt256` / `.Int256` are entirely additive.** New enum values (`Int256 = 16`, `UInt256 = 17`) extend `StorageType` without reordering or renumbering the existing members (`Int128 = 14`, `UInt128 = 15` are unchanged). Existing `[BitFields]` consumers are unaffected. Code that reflects over `StorageType` values and switches exhaustively on them will want to add arms for the two new members.
- **`BIT_WIDTH` is additive**
- **`StructDescription` / `StructDescriptionResourceType` on views is additive** -- these were already generated on value types. Adding them to views is a new public member; code that already declares these names on a `partial record struct` will get a compile error.
- **All generated private members renamed with `__` prefix** -- code that accessed generated private fields or constants via reflection, `Unsafe` operations, or other non-public access patterns will break. These members were always `private` and not part of the public API, so normal usage through generated properties, operators, and conversions is unaffected. If you have code that references generated members directly (e.g., via `typeof(MyStruct).GetField("Value", BindingFlags.NonPublic | BindingFlags.Instance)`), update the name to use the `__` prefix (e.g., `"__value"`). The full rename mapping: `Value` to `__value`, `_w0`/`_w1` to `__w0`/`__w1`, `_data` to `__data`, `_bitOffset` to `__bitOffset`, `{FIELD}_MASK` to `__{FIELD}_MASK` (and similarly for all other per-field constants), `NORMALIZATION_AND_MASK` to `__NORMALIZATION_AND_MASK`, `NORMALIZATION_OR_MASK` to `__NORMALIZATION_OR_MASK`, `WORD_COUNT` to `__WORD_COUNT`, `TOTAL_BITS` to `__TOTAL_BITS`, `LAST_WORD_MASK` to `__LAST_WORD_MASK`.
- Auto-sized `[BitFields]` is entirely additive. Existing code that specifies a storage type or bit count is unaffected.
- Making `UndefinedBits` settable is a source-compatible change. Existing code that passes `UndefinedBitsMustBe` via constructor parameters continues to work identically.

## [0.9.8] - 2026-04-10

### 🟣 Changed
- **`[BitField]` accepts bit positions in either order** -- when the end bit is less than the start bit (e.g., `[BitField(7, 0)]`, `[BitField(Start = 7, End = 3)]`, or `[BitField(7, End = 3)]`), the values are silently swapped so either order produces the same correctly-ranged field. This applies to all forms: the positional two-parameter constructor, fully-named syntax, and mixed positional+named syntax. Previously, the two-parameter positional constructor threw `ArgumentException` when `end < start`; reversed named syntax produced incorrect negative widths at code-generation time. This change makes `[BitField]` more tolerant of copy-paste or transposed values from hardware datasheets.
- **`[BitField(End = N, Width = W)]` derives `Start` automatically** -- when `End` and `Width` are both specified as named arguments but `Start` is omitted, the generator computes `Start = End - Width + 1`. This is the natural counterpart to the existing `[BitField(start, Width = W)]` form (which derives `End = start + W - 1`). For example, `[BitField(End = 7, Width = 4)]` is equivalent to `[BitField(4, End = 7)]`. Previously this combination produced an SD0019 error. The formula is bit-order-agnostic and works correctly with both `BitOrder.BitZeroIsLsb` and `BitOrder.BitZeroIsMsb`.
- **Result API renamed to Rust-parity conventions** -- the `Result<T, TError>` and `Result<TError>` types have been updated with Rust-aligned method names for consistency with the new `Option<T>` type and the broader Rust FP convention. All previous method names are preserved as `[Obsolete]` shims that delegate to the new methods, so existing code compiles with warnings but no breaking changes. The renames are: `IsSuccess` to `IsOk`, `IsFailure` to `IsErr`, `Then(transform)` to `Map(transform)`, `Then(fallible)` / `Then(result)` to `AndThen(fallible)` / `And(result)`, `MapError` to `MapErr`, `Match` to `MapOrElse`, `OnSuccess` to `Inspect`, `OnFailure` to `InspectErr`, `ValueOr` to `UnwrapOr` / `UnwrapOrElse`, `UnwrapError` to `UnwrapErr`. Async extension methods follow the same pattern: `Then` to `Map` / `AndThen`, `ThenError` to `MapErr`.
- **Updated README.md DemoWeb app graphics** to show the Composable Floating Point lab page that was added in version 0.9.7.
- **Updated README.md and BITFIELDS.md documentation** to reflect the above changes and provide more examples of valid `[BitField]` syntax.
- **Updated RESULT.md documentation** to use the new Rust-parity method names throughout all examples, API reference tables, and code samples. Added sections for the new methods and a deprecation reference table listing all old-to-new name mappings.

### 🟢 Added
- **`Option<T>` type** -- a new `readonly record struct` representing an optional value: every `Option<T>` is either `Some(value)` or `None`. Inspired by Rust's `Option<T>`. Zero heap allocation, structural equality, and works identically for both value types and reference types. Includes: `Some(T)` / `None` factories, implicit conversions from `T` and `NoneOption`, `IsSome` / `IsNone` properties, `Value` / `Unwrap()` / `Expect(message)` / `UnwrapOr(default)` / `UnwrapOrElse(factory)` / `UnwrapOrDefault()` / `UnwrapUnchecked()`, `TryGetValue(out T)`, `Deconstruct(out bool, out T)`, `Map` / `AndThen` / `Filter` / `Inspect` / `MapOrElse` / `MapOr`, `And` / `Or` / `OrElse` / `Xor` / `Zip` / `ZipWith`, `ToNullable()`, `OkOr` / `OkOrElse` (convert to Result), and `ToString()` returning `"Some(value)"` or `"None"`.
- **`Option` static companion class** -- provides the untyped `Option.None` sentinel (implicitly converts to any `Option<T>` via `NoneOption`), `Option.Some<T>(value)` for type-inferred construction, and `Option.FromNullable<T>(T?)` overloads for both reference and value types.
- **`OptionExtensions` class** -- extension methods: `Flatten<T>(Option<Option<T>>)` collapses nested options, `Transpose<T, TError>(Option<Result<T, TError>>)` swaps Option/Result nesting, `ToOption<T>(T?)` converts nullable references and nullable value types to Option, `ToOption<T, TError>(Result<T, TError>)` converts Result to Option (discards error), and async `Map` / `AndThen` overloads on `Task<Option<T>>`.
- **`OPTION.md` documentation** -- comprehensive documentation covering all API methods, design rationale, usage patterns, interop with Result and nullable, async support, and real-world examples.
- **Option types added to README.md** -- new section in Table of Contents and Features with basic usage, transform chains, and Result interop examples, plus link to OPTION.md.
- **Result-Option interop on Result side** -- `Flatten<T, TError>(Result<Result<T, TError>, TError>)` collapses nested results, `Transpose<T, TError>(Result<Option<T>, TError>)` swaps Result/Option nesting, `ErrToOption<T, TError>(Result<T, TError>)` converts Err to Some (Ok to None).
- **New Result query methods** -- `IsOkAnd(Func<T, bool>)` and `IsErrAnd(Func<TError, bool>)` test state and predicate in one call. `Expect(string)` and `ExpectErr(string)` unwrap with custom error messages. `UnwrapOrDefault()` returns `default(T)` on Err.
- **New Result transform methods** -- `MapOr<TResult>(Func<T, TResult>, TResult)` transforms Ok value or returns eager default. `Or<TNewError>(Result<T, TNewError>)` and `OrElse<TNewError>(Func<TError, Result<T, TNewError>>)` provide fallback results.
- **`Saturating` parameter on `[BitField]`** -- when `Saturating = true`, the generated property setter and `With{Name}` method clamp the incoming value to the field's valid range instead of silently truncating (wrapping). For unsigned property types the value is clamped to `[0, 2^Width - 1]`; for signed property types it is clamped to `[-(2^(Width-1)), 2^(Width-1) - 1]`. Defaults to `false` (existing truncation behaviour). Supported property types: `byte`, `sbyte`, `ushort`, `short`, `uint`, `int`, `ulong`, `long`, `nint`, `nuint`. Silently ignored for floating-point types, embedded `[BitFields]` struct types, enum types, fields whose `ValueOverride` forces a fixed value, and full-width fields where the field width equals the property type width. Works across all generator modes: single-word value types, multi-word value types, and record struct views. Generated code uses named `SCREAMING_SNAKE_CASE` constants for the saturation bounds (`{FIELD}_SAT_MIN`, `{FIELD}_SAT_MAX`) for easy verification. Includes 26 tests covering unsigned clamping, signed clamping (positive/negative overflow, boundary values), sbyte small type, non-saturating wrapping comparison, field independence, shifted fields, full-width no-op, `With{Name}` method clamping, 10-bit ushort, multi-word structs, and record struct views.

### 🟠 Deprecated
- **Result method renames** -- the following `Result<T, TError>` and `Result<TError>` members are marked `[Obsolete]` and will be removed in a future release: `IsSuccess` (use `IsOk`), `IsFailure` (use `IsErr`), `UnwrapError()` (use `UnwrapErr()`), `ValueOr(T)` (use `UnwrapOr(T)`), `ValueOr(Func<TError, T>)` (use `UnwrapOrElse(Func<TError, T>)`), `Then<TNew>(Func<T, TNew>)` (use `Map<TNew>`), `Then<TNew>(Func<T, Result<TNew, TError>>)` (use `AndThen<TNew>`), `Then(Result)` (use `And(Result)`), `MapError<TNewError>` (use `MapErr<TNewError>`), `Match<TResult>` (use `MapOrElse<TResult>`), `OnSuccess(Action)` (use `Inspect(Action)`), `OnFailure(Action)` (use `InspectErr(Action)`). Async extension renames: `Then` overloads to `Map` / `AndThen`, `ThenError` to `MapErr`. All deprecated members delegate to the new methods with zero overhead.

### 🔴 Removed
- **`[BitFieldsView]` attribute removed** -- `BitFieldsViewAttribute` was deprecated in 0.9.7 and has now been removed. Use `[BitFields]` on a `partial record struct` instead. The generator detects the `record` keyword and produces identical view code. Migration is a find-and-replace: `[BitFieldsView]` to `[BitFields]`, `[BitFieldsView(` to `[BitFields(`.
- **`IsBitFieldsViewType()` extension method removed** -- use `IsBitFieldsType()` or `IsBitsType()` instead.
- **`BitFieldsViewTests` renamed to `RecordStructViewTests`** -- all test struct definitions converted from `[BitFieldsView]` to `[BitFields]`. 4 unified-attribute comparison tests removed (they compared legacy vs unified which is no longer applicable).
- Internal generator classes renamed: `BitFieldsViewGenerator` to `RecordStructViewGenerator`, `BitFieldsViewInfo` to `RecordStructViewInfo`.

### 🟤 Backwards Compatibility
- The `BitFieldAttribute(int start, int end)` constructor no longer throws `ArgumentException` when `end < start`. Code that relied on catching that exception must be updated -- the constructor now silently swaps the values instead.
- The `Saturating` parameter on `[BitField]` is entirely additive. Existing code that does not use `Saturating` is unaffected; the default is `false` which preserves the existing truncation (masking) behaviour.
- **`[BitFieldsView]` removed** -- code using `[BitFieldsView]` must be changed to `[BitFields]` on a `partial record struct`. The generated code is identical. `IsBitFieldsViewType()` callers should switch to `IsBitFieldsType()` or `IsBitsType()`.
- **Result API renames are non-breaking** -- all renamed Result methods are preserved as `[Obsolete]` shims that delegate to the new methods. Existing code compiles with deprecation warnings but requires no immediate changes. The deprecated members will be removed in a future release.
- **`Option<T>` is entirely additive** -- no existing APIs are affected by the new Option type.

## [0.9.7] - 2026-03-28
### 🟣 Changed
- **Unified `[BitFields]` attribute for both value types and views** -- `[BitFields]` now works on `partial record struct` in addition to `partial struct`. The generator detects the `record` keyword and produces zero-copy `Memory<byte>`-backed view code (the same codegen previously produced by `[BitFieldsView]`). New parameterless and `(ByteOrder, BitOrder)` constructors on `BitFieldsAttribute` support the view use case. Existing value-type constructors (`StorageType`, `typeof(T)`, `int bitCount`) are unchanged.
- **Generator package description** updated to reflect unified `[BitFields]` attribute.
- **Generator package tags** updated to include `bitfieldsview`.
- **Documentation consolidated** -- README.md and BITFIELDS.md rewritten to present a single `[BitFields]` attribute with `struct` vs `record struct` as the only differentiator. The `[BitFieldsView]` attribute is mentioned only in a deprecation notice.
- **CHANGELOG.md format changed** to use standard ordering of sections and standard colorized emojis for easier visual scanning.

### 🟢 Added
- **Right-sized backing for `[BitFields(N)]` (N \<= 64)** -- `[BitFields(N)]` where N is 1--64 now selects the smallest unsigned primitive that can hold N bits: `byte` for N \<= 8, `ushort` for N \<= 16, `uint` for N \<= 32, `ulong` for N \<= 64. Previously these were always backed by `ulong`. The smaller backing type reduces struct size and enables efficient composition -- for example, a `[BitFields(5)]` struct is only 1 byte and can be embedded in larger structs or views at its exact bit width.
- **Full composability of ALL `[BitFields]` types** -- any `[BitFields]` value-type struct can now be used as a property type inside other `[BitFields]` value-type structs, multi-word structs, and record struct views. This includes single-word types (`byte` through `ulong`, `nint`/`nuint`, `Half`/`float`/`double`), right-sized `[BitFields(N)]` (N <= 64), and multi-word types (`UInt128`, `Int128`, `decimal`, `[BitFields(N)]` N > 64). Single-word and right-sized types embed via efficient cast chains through implicit conversion operators. Multi-word types embed via generated span-based `ReadFrom`/`WriteTo` calls at any bit position (byte-alignment is not required; non-aligned positions use byte-level bit-shifting). Width validation (SD0021) enforces exact bit-width match for `[BitFields(N)]` types. New diagnostic: SD0023 (cannot embed multi-word type in a single-word parent). Includes 29 composition tests covering standalone round-trips, N-in-typeof(T), N-in-N, N-in-view, 128-bit-in-512-bit, 256-bit-in-512-bit, 128-bit-in-view, 256-bit-in-view, non-byte-aligned multi-word embedding, and field independence verification across all embedding contexts.
- **JSON serialization for all `[BitFields]` types** -- every generated `[BitFields]` type (both value types and record struct views) now includes a `System.Text.Json` converter applied via `[JsonConverter]`. The converter serializes the underlying storage as a `"0x..."` hex string and round-trips through `Parse`. For record struct views, the converter serializes the `Memory<byte>` bytes in the same hex format and deserializes by parsing the hex string back into a `byte[]`. Works in DTOs (Data Transfer Objects), REST APIs, and configuration files without setup. Comprehensive test coverage for all 16 supported storage types (`byte`, `sbyte`, `ushort`, `short`, `uint`, `int`, `ulong`, `long`, `nint`, `nuint`, `Half`, `float`, `double`, `decimal`, `UInt128`, `Int128`), all multi-word size classes (65/128/200/256/512/16384-bit including cross-word fields), `StorageType` enum-constructor variants, big-endian and little-endian views, embedded BitFields composition, hex/binary/decimal format deserialization, and null-to-default behavior. Added dedicated JSON serialization sections in README.md and BITFIELDS.md.
- **Span serialization documentation** -- added dedicated `Span Serialization` sections in README.md and BITFIELDS.md documenting the generated `ReadFrom`, `WriteTo`, `TryWriteTo`, and `ToByteArray` methods.
- **Unified attribute tests** -- 4 tests verifying that `[BitFields]` on a `record struct` produces identical behavior to the deprecated `[BitFieldsView]`, covering big-endian, little-endian, set/get, and JSON round-trip.
- **Floating-point and decimal property types (`Half`, `float`, `double`, `decimal`)** -- `Half`, `float`, `double`, and `decimal` can now be used as property types inside `[BitFields]` structs and record struct views. All four types are treated as opaque bit patterns: the raw bits are reinterpreted without inspecting sign, scale, or mantissa. `Half` uses `BitConverter.UInt16BitsToHalf`/`HalfToUInt16Bits`, `float` uses `SingleToUInt32Bits`/`UInt32BitsToSingle`, `double` uses `DoubleToUInt64Bits`/`UInt64BitsToDouble`, and `decimal` (128 bits) uses `Unsafe.As<UInt128, decimal>`/`Unsafe.As<decimal, UInt128>` for zero-cost reinterpretation on .NET 7+. `decimal` additionally uses `UInt128` arithmetic for multi-word storage and `BinaryPrimitives.ReadUInt128LittleEndian`/`WriteUInt128LittleEndian` for views. Includes 106 tests covering multi-word (word-aligned, non-aligned, and 3-word span), views (byte-aligned and non-byte-aligned), and a parameterized sweep of 16 decimal values including extremes.
- **Floating-point property width validation (SD0020)** -- the source generator now emits a compile error when ANY floating-point property type (`Half`, `float`, `double`, or `decimal`) is used with a `[BitField]` width that does not match the type's exact bit size. Required widths: `Half` = 16, `float` = 32, `double` = 64, `decimal` = 128. Because these types are stored as opaque bit patterns, any width mismatch would silently corrupt the value at runtime -- this diagnostic catches the mistake at compile time. The error message names the property, its type, the required width, and the declared width. Applies to value types, multi-word structs, and record struct views. Includes 11 diagnostic tests.

### 🟠 Deprecated
- **`[BitFieldsView]` deprecated** -- `BitFieldsViewAttribute` is marked `[Obsolete]`. The generator still recognizes it and produces identical code, but users should migrate to `[BitFields]` on a `partial record struct`. `BitFieldsViewAttribute` will be removed in a future release.

### 🔴 Removed
- **`DiagramSection` type** and the `DiagramSection[]`-based overloads of `RenderList`/`RenderListToString` on `BitFieldDiagram`. These were deprecated in 0.9.5 and replaced by the `Description` parameter on `[BitFields]` attributes combined with the Type-based `RenderList`/`RenderListToString` overloads, which remain unchanged.
- **`BitFieldDiagram.GetFieldInfo(Type)`** static method (was an obsolete wrapper around `GetFields`).
- 12 tests for the removed `DiagramSection` API and `GetFieldInfo` method.
- Dead `DiagramSection`-related code in the WPF demo app (`MainWindow.xaml.cs`).

### 🔵 Fixed
- **64-bit field mask overflow** -- the code generator used `(1UL << 64) - 1` as the mask for 64-bit fields, which overflows to 0 in C# (the shift count is taken mod 64). This caused all 64-bit fields in `ulong`-backed structs to always read as 0 and all writes to be silently discarded. Fixed by special-casing `width == 64` to emit `ulong.MaxValue` directly. ([#16](https://github.com/dhadner/Stardust.Utilities/issues/16))
- **View generator `ReadUInt64` reads too few bytes for non-byte-aligned 64-bit fields** -- when a 64-bit field (e.g., `double`) starts at a non-byte-aligned offset in a record struct view, the raw bits span 9 bytes. The generator emitted `ReadUInt64LittleEndian` (8 bytes), silently losing the topmost bits. Fixed by widening the read to `ReadUInt128LittleEndian` when `byteSpan > 8`, with the wider arithmetic used only when necessary to avoid impacting existing field performance. ([#17](https://github.com/dhadner/Stardust.Utilities/issues/17))
- **Minor bit definition issue in the pre-defined `DecimalBitFields` type** -- the `Scale` field was defined as bits 16 to 22 but should have been 16 to 23.  No impact because `Scale` cannot be > 28, which fits in the previous 7-bit definition.  Also, undefined bits must be 0 per spec and this is now enforced.

### 🟤 Backwards Compatibility
- The `DiagramSection` type and its `RenderList`/`RenderListToString` overloads are removed. Code using them must migrate to the Type-based `RenderList(Type[])` / `RenderListToString(Type[])` overloads (available since 0.9.5) or the instance API (`new BitFieldDiagram(typeof(T), description)`). The struct-level `Description` parameter on `[BitFields]` replaces `DiagramSection` labels.
- The `BitFieldDiagram.GetFieldInfo(Type)` static method is removed. Use `BitFieldDiagram.GetFields(Type)` instead (same behavior, returns `Result<BitFieldInfo[], string>`).
- `[BitFieldsView]` is deprecated but still fully functional. Existing code using `[BitFieldsView]` will compile with a warning; no code changes are required until the attribute is removed in a future release. Migration is a find-and-replace: `[BitFieldsView]` to `[BitFields]`, `[BitFieldsView(` to `[BitFields(`.
- All other APIs are backwards compatible with 0.9.6.

## [0.9.6] - 2026-03-21
### 🟣 Changed
- **Two-parameter `[BitField(startBit, endBit)]` constructor** -- parameter names changed to 'start' and 'end' for brevity, so if you have been using `startBit` or `endBit` as named parameters in your constructor calls (unusual for constructors), those will need to change. Because the positional `end` parameter is easily confused with a bit count and can result in incorrect behavior, the source generator now emits info/message SD0015 when this form is used. Migrate to `[BitField(start, End = N)]` or `[BitField(start, Width = N)]` or disable the message if desired. This can be done by adding/updating the .editorconfig file in your project or solution and including `dotnet_diagnostic.SD0015.severity = none` in the `[*.cs]` section..
- **`MustBe`/`UndefinedBitsMustBe` enforcement across all operations** -- `MustBe.Zero` and `MustBe.One` constraints on `[BitField]`/`[BitFlag]` are now enforced in setters, `With...` methods, constructors, parsing, and implicit conversions. Previously these constraints were only masked on read.

### 🟢 Added
- **`nint`/`nuint` storage type support** for `[BitFields]`. Generates compiler error (SD0001) when fields exceed bit 31 on x86 builds, and compiler warning (SD0002) on AnyCPU builds where 32-bit execution would silently lose data.
- **`StorageType` enum** -- new constructor overload `[BitFields(StorageType.Byte)]` (etc.) for IDE code-time and compile-time validation of storage type. Generates compiler error (SD0003) for unsupported types. The existing `[BitFields(typeof(T))]` and `[BitFields(int)]` constructors remain supported.
- **Non-partial property diagnostic (SD0004)** -- the source generator now emits a clear error pointing at the user's source file when a `[BitField]` or `[BitFlag]` property is missing the `partial` keyword. The error message includes the property name, attribute, and corrected declaration. This replaces the confusing `CS9248` and `CS0102` compiler errors that previously appeared from the generated `.g.cs` file.
- **Pre-defined numeric decomposition types**: `IEEE754Half`, `IEEE754Single`, `IEEE754Double`, and `DecimalBitFields` for inspecting IEEE 754 and .NET decimal bit layouts. Includes implicit conversions to/from their storage type, classification properties (`IsNormal`, `IsNaN`, `IsInfinity`, `IsDenormalized`, `IsZero`), true `Exponent` property (with getter and setter), `WithExponent(int)` fluent setter, and full arithmetic operator support.
- **Named SCREAMING_SNAKE_CASE mask constants** in generated code (e.g., `MANTISSA_MASK`, `MANTISSA_SHIFTED_MASK`, `MANTISSA_INVERTED_MASK`, `MANTISSA_START_BIT`) replacing inline hex literals for improved readability and reviewability.
- **`AddStructs` API** on `BitFieldDiagram` for incrementally adding struct types to an existing diagram instance after construction.
- XML documentation coverage for all public APIs, now included in the NuGet package.
- **Named property syntax for `[BitField]`** -- new `End` and `Width` named properties plus parameterless and single-parameter constructors. Supports three self-documenting styles: `[BitField(0, Width = 8)]`, `[BitField(0, End = 7)]`, and `[BitField(Start = 0, Width = 8)]`. Both `End` and `Width` can be specified together if they are consistent (redundancy warning SD0016; error SD0017 if inconsistent). Missing range produces error SD0018; missing Start produces error SD0019.

### 🔵 Fixed
- Fixed constant naming (`MAX_EXPONENT`, `MAX_TRUE_EXPONENT`) to be consistent with existing constant naming convention and property names (`MAX_BIASED_EXPONENT`, `MAX_EXPONENT`) on pre-defined numeric decomposition types.
- Fixed nuisance build warnings due to missing release tracking files for compiler diagnostics.

### 🟤 Backwards Compatibility
- All existing `[BitFields]` and `[BitFieldsView]` APIs are backwards compatible with 0.9.5. The `StorageType` enum constructor, `nint`/`nuint` support, numeric decomposition types, named mask constants, SD0004 diagnostic, and `AddStructs` API are entirely new additions.
- `MustBe`/`UndefinedBitsMustBe` enforcement is now applied consistently in setters, `With...` methods, constructors, and parsing. Code that previously wrote invalid values to `MustBe.Zero`/`MustBe.One` fields will now have those writes silently corrected. This is a behavioral change but aligns with the documented contract.

## [0.9.5] - 2026-03-05
### 🟣 Changed
- Diagrams now handle overlapping bit fields by rendering them in the order they are declared, with later fields potentially overwriting earlier ones in the diagram. This allows for intentional overlapping fields while still rendering all declared fields.

### 🟢 Added
- Added `Description` to `[BitFields]` and `[BitFieldsView]` structs, used for diagram section descriptions and demo app tooltips. The 
purpose of this is to simplify the API and deprecate `DiagramSection` that adds unnecessary complexity to the API. 

### 🟠 Deprecated
- Deprecated `DiagramSection` feature, replaced with the `Description` field for `[BitFields]` and `[BitFieldsView]` structs. Will be removed in a future version.
- Deprecated the `BitFieldDiagram.GetFieldInfo(Type)` static method, a wrapper around `GetFields`.

### 🔵 Fixed
- Fixed compile error in generated `With{Name}` methods when the property type is a byte-backed enum and the field starts at bit 0 (shift == 0). The generated code now casts the value to the storage type before applying the mask, matching the pattern already used by the setter and the shift != 0 branch.

### 🟤 Backwards Compatibility
- All APIs are backwards compatible with 0.9.4. The `Description` parameter on `[BitFields]` and `[BitFieldsView]` is optional 
and does not affect existing functionality. 
- The `DiagramSection` type and related `RenderList`/`RenderListToString` methods are still present but marked as deprecated, 
with no breaking changes.

## [0.9.4] - 2026-02-09
### 🟢 Added
- **`BitFieldDiagram` RFC diagram generator** -- generates RFC 2360-style ASCII bit field diagrams from `[BitFields]` and `[BitFieldsView]` struct metadata via `BitFieldDiagram.Render()` and `RenderToString()`. Features auto-sized cells to fit field names, byte offset labels, bit-position headers (tens/ones digits), undefined bit marking (`Undefined` or `U` with legend), configurable bits-per-row (8, 16, 32, 64), optional field description legend, and struct-sized last rows.
- **`RenderList` / `RenderListToString`** -- render multiple struct sections as a unified diagram with consistent cell widths via `DiagramSection`. The widest field name across all sections determines the scale. `ComputeMinCellWidth` exposed for custom layout logic.
- **`showByteOffset` parameter** -- controls hex byte offset labels on diagram content rows (default: true).
- **`Description` parameter** on `[BitField]` and `[BitFlag]` attributes for field-level documentation, used by diagram descriptions legend and demo app tooltips.
- **`[BitFieldsView]` source generator** -- zero-copy `record struct` views over `Memory<byte>` buffers with per-field bit manipulation, supporting both big-endian/MSB-first (network protocols) and little-endian/LSB-first (hardware registers) conventions. Includes nested sub-view composition and per-field endianness override via `[BitFields]` ByteOrder detection.
- **`BitOrder` enum** (`BitZeroIsMsb`, `BitZeroIsLsb`) for controlling bit numbering in `[BitFields]` and `[BitFieldsView]`.
- **`ByteOrder` enum** (`BigEndian`/`NetworkEndian`, `LittleEndian`) for controlling byte order in `[BitFields]` serialization and `[BitFieldsView]` multi-byte field access.
- **`bitOrder` and `byteOrder` optional parameters** on `[BitFields]` attribute constructors. Defaults (`BitZeroIsLsb`, `LittleEndian`) preserve backwards compatibility.
- **Little-endian endian-aware types**: `UInt16Le`, `UInt32Le`, `UInt64Le`, `Int16Le`, `Int32Le`, `Int64Le` with `TypeConverter` support, complementing the existing big-endian types.
- **`[BitFieldsView]` per-field endianness override**: using endian-aware property types (e.g., `UInt32Be` in a LE view) or embedding a `[BitFields]` struct whose declared `ByteOrder` differs from the view's default.
- **Canonical protocol header examples**: `IPv4HeaderView`, `IPv6FullHeaderView`, `UdpHeaderView`, `TcpHeaderView` in the test suite, demonstrating real-world network packet parsing with nested sub-views.
- Added support for signed properties in a `[BitFields]` struct.
- Added `UndefinedBitsMustBe` enum with `Any`, `Zeroes`, and `Ones` values for controlling undefined bit behavior in `[BitFields]`.
- Added `MustBe` enum with `Any`, `Zero`, `One`, and `Ones` values for per-field/flag bit control.
- Added `ValueOverride` parameter (`MustBe` enum type) to `[BitField]` and `[BitFlag]` attributes for per-field/flag bit override.
- Added support for sparse undefined bits (gaps between defined fields).
- Added `[BitFields]` struct composition (using one `[BitFields]` type as a property type in another), with testing and documentation.
- Added `Half` (16-bit float) and `decimal` storage type support for `[BitFields]`.
- Added fuzz testing for parsers. No errors found.
- Performance testing now runs on local dev machine. Still disabled during CI builds.
- Builds are now deterministic.
- **Blazor WebAssembly demo app** (`Demo/BitFields.DemoWeb`) -- interactive browser-based playground for BitFields, PE headers, network packets, CPU registers, and RFC diagrams. Deployable to GitHub Pages.
- **`PeParser`** shared utility -- demonstrates `Result<T, TError>.AndThen()` chaining for multi-step PE header validation pipeline.
- **BitFieldDiagram test suite** -- 29 tests covering `Render`, `RenderToString`, `RenderList`, `RenderListToString`, `ComputeMinCellWidth`, bit order handling, undefined bits, descriptions, and separator structure.

### 🟤 Backwards Compatibility
- All APIs are backwards compatible with 0.9.3. New parameters on `[BitFields]`, `[BitField]`, and `[BitFlag]` attribute constructors use optional defaults that preserve existing behavior. `[BitFieldsView]`, `BitOrder`, `ByteOrder`, and the little-endian endian-aware types are entirely new additions. The `StorageType` property on `BitFieldsAttribute` changed from `Type` to `Type?` to support the new bit-count constructor overload.

## [0.9.3] - 2026-02-05
### 🟢 Added
- Added support for .NET 7 and .NET 8 in addition to .NET 10.
- No feature changes.

## [0.9.2] - 2026-02-04 (First NuGet Release)
### 🟢 Added
- Added several NuGet project properties, icon, links in preparation for release.
- Added CHANGELOG.md, SECURITY.md, CODE_OF_CONDUCT.md.
- Added GitHub templates for issues and pull requests.

### 🔴 Removed
- Removed unused BitStream feature - not useful enough yet.
- Removed a few unnecessary Extensions features that can be accomplished easily in .NET already.

## [0.9.1] - 2026-02-01
### 🟢 Added
- Migrated from app-specific in-house library to NuGet package for better reuse.

## [0.9.0] - 2026-01-28
### 🟣 Changed
- `[BitFields]` types now implement `ISpanFormattable` for allocation-free string formatting.
- `[BitFields]` types now implement `ISpanParsable<T>` for allocation-free string parsing.

### 🟢 Added
- Migrated from mature in-house library to NuGet package for better reuse.
- Added support for C#-style `_` digit separators in `Parse` and `TryParse` methods for `[BitField]` types.
- Added support for binary format parsing (e.g., `0b1101`) for `[BitField]` types.

## [0.0.1] - 2023-04-07
### 🟢 Added
- Initial internal release to private GitHub repo.
