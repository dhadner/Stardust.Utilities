# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project will adhere to [Semantic Versioning](https://semver.org/spec/v2.0.0.html) once version 1.0 is released.

## [0.9.7] - 2026-03-28
### 🟣 Changed
- **Generator package description** updated
- **Generator package tags** updated to include `bitfieldsview`.
- **Documentation consolidated** -- README.md and BITFIELDS.md rewritten to present a single `[BitFields]` attribute with `struct` vs `record struct` as the only differentiator. The `[BitFieldsView]` attribute is mentioned only in a deprecation notice.

### 🟢 Added
- **Unified `[BitFields]` attribute**
- **JSON serialization for all `[BitFields]` types** -- every generated `[BitFields]` type (both value types and record struct views) now includes a `System.Text.Json` converter applied via `[JsonConverter]`. The converter serializes the underlying storage as a `"0x..."` hex string and round-trips through `Parse`. For record struct views, the converter serializes the `Memory<byte>` bytes in the same hex format and deserializes by parsing the hex string back into a `byte[]`. Works in DTOs (Data Transfer Objects), REST APIs, and configuration files without setup. Comprehensive test coverage for all 16 supported storage types (`byte`, `sbyte`, `ushort`, `short`, `uint`, `int`, `ulong`, `long`, `nint`, `nuint`, `Half`, `float`, `double`, `decimal`, `UInt128`, `Int128`), all multi-word size classes (65/128/200/256/512/16384-bit including cross-word fields), `StorageType` enum-constructor variants, big-endian and little-endian views, embedded BitFields composition, hex/binary/decimal format deserialization, and null-to-default behavior. Added dedicated JSON serialization sections in README.md and BITFIELDS.md.
- **Span serialization documentation** -- added dedicated `Span Serialization` sections in README.md and BITFIELDS.md documenting the generated `ReadFrom`, `WriteTo`, `TryWriteTo`, and `ToByteArray` methods.
- **Unified attribute tests** -- 4 tests verifying that `[BitFields]` on a `record struct` produces identical behavior to the deprecated `[BitFieldsView]`, covering big-endian, little-endian, set/get, and JSON round-trip.
- **Floating-point and decimal property types (`Half`, `float`, `double`, `decimal`)** -- `Half`, `float`, `double`, and `decimal` can now be used as property types inside `[BitFields]` structs and record struct views. All four types are treated as opaque bit patterns: the raw bits are reinterpreted without inspecting sign, scale, or mantissa. `Half` uses `BitConverter.UInt16BitsToHalf`/`HalfToUInt16Bits`, `float` uses `SingleToUInt32Bits`/`UInt32BitsToSingle`, `double` uses `DoubleToUInt64Bits`/`UInt64BitsToDouble`, and `decimal` (128 bits) uses `Unsafe.As<UInt128, decimal>`/`Unsafe.As<decimal, UInt128>` for zero-cost reinterpretation on .NET 7+. `decimal` additionally uses `UInt128` arithmetic for multi-word storage and `BinaryPrimitives.ReadUInt128LittleEndian`/`WriteUInt128LittleEndian` for views. Includes 106 tests covering multi-word (word-aligned, non-aligned, and 3-word span), views (byte-aligned and non-byte-aligned), and a parameterized sweep of 16 decimal values including extremes.
- **Floating-point property width validation (SD0020)** -- the source generator now emits a compile error when ANY floating-point property type (`Half`, `float`, `double`, or `decimal`) is used with a `[BitField]` width that does not match the type's exact bit size. Required widths: `Half` = 16, `float` = 32, `double` = 64, `decimal` = 128. Because these types are stored as opaque bit patterns, any width mismatch would silently corrupt the value at runtime -- this diagnostic catches the mistake at compile time. The error message names the property, its type, the required width, and the declared width. Applies to value types, multi-word structs, and record struct views. Includes 11 diagnostic tests.

### 🟠 Deprecated
- **`[BitFieldsView]` deprecated**

### 🔴 Removed
- **`DiagramSection` type**
- **`BitFieldDiagram.GetFieldInfo(Type)`** static method (was an obsolete wrapper around `GetFields`).
- 12 tests for the removed `DiagramSection` API and `GetFieldInfo` method.
- Dead `DiagramSection`-related code in the WPF demo app (`MainWindow.xaml.cs`).

### 🔵 Fixed
- **64-bit field mask overflow**
- **View generator `ReadUInt64` reads too few bytes for non-byte-aligned 64-bit fields** -- when a 64-bit field (e.g., `double`) starts at a non-byte-aligned offset in a record struct view, the raw bits span 9 bytes. The generator emitted `ReadUInt64LittleEndian` (8 bytes), silently losing the topmost bits. Fixed by widening the read to `ReadUInt128LittleEndian` when `byteSpan > 8`, with the wider arithmetic used only when necessary to avoid impacting existing field performance. ([#17](https://github.com/dhadner/Stardust.Utilities/issues/17))
- **Minor bit definition issue in the pre-defined `DecimalBitFields` type** -- the `Scale` field was defined as bits 16 to 22 but should have been 16 to 23.  No impact because `Scale` cannot be > 28, which fits in the previous 7-bit definition.  Also, undefined bits must be 0 per spec and this is now enforced.

### 🟤 Backwards Compatibility
- The `DiagramSection` type and its
- The `BitFieldDiagram.GetFieldInfo(Type)` static method is removed. Use `BitFieldDiagram.GetFields(Type)` instead (same behavior, returns `Result<BitFieldInfo[], string>`).
- `[BitFieldsView]` is deprecated but still fully functional. Existing code using `[BitFieldsView]` will compile with a warning; no code changes are required until the attribute is removed in a future release. Migration is a find-and-replace: `[BitFieldsView]` to `[BitFields]`, `[BitFieldsView(` to `[BitFields(`.
- All other APIs are backwards compatible with 0.9.6.

## [0.9.6] - 2026-03-21
### 🟣 Changed
- **Two-parameter `[BitField(startBit, endBit)]`**
- **`MustBe`/`UndefinedBitsMustBe` enforcement across all operations** -- `MustBe.Zero` and `MustBe.One` constraints on `[BitField]`/`[BitFlag]` are now enforced in setters, `With...` methods, constructors, parsing, and implicit conversions. Previously these constraints were only masked on read.

### 🟢 Added
- **`nint`/`nuint` storage type support**
- **`StorageType` enum** -- new constructor overload `[BitFields(StorageType.Byte)]` (etc.) for IDE code-time and compile-time validation of storage type. Generates compiler error (SD0003) for unsupported types. The existing `[BitFields(typeof(T))]` and `[BitFields(int)]` constructors remain supported.
- **Non-partial property diagnostic (SD0004)** -- the source generator now emits a clear error pointing at the user's source file when a `[BitField]` or `[BitFlag]` property is missing the `partial` keyword. The error message includes the property name, attribute, and corrected declaration. This replaces the confusing `CS9248` and `CS0102` compiler errors that previously appeared from the generated `.g.cs` file.
- **Pre-defined numeric decomposition types**: `IEEE754Half`, `IEEE754Single`, `IEEE754Double`, and `DecimalBitFields` for inspecting IEEE 754 and .NET decimal bit layouts. Includes implicit conversions to/from their storage type, classification properties (`IsNormal`, `IsNaN`, `IsInfinity`, `IsDenormalized`, `IsZero`), true `Exponent` property (with getter and setter), `WithExponent(int)` fluent setter, and full arithmetic operator support.
- **Named SCREAMING_SNAKE_CASE mask constants** in generated code (e.g., `MANTISSA_MASK`, `MANTISSA_SHIFTED_MASK`, `MANTISSA_INVERTED_MASK`, `MANTISSA_START_BIT`) replacing inline hex literals for improved readability and reviewability.
- **`AddStructs` API** on `BitFieldDiagram` for incrementally adding struct types to an existing diagram instance after construction.
- XML documentation coverage for all public APIs, now included in the NuGet package.
- **Named property syntax for `[BitField]`** -- new `End` and `Width` named properties plus parameterless and single-parameter constructors. Supports three self-documenting styles: `[BitField(0, Width = 8)]`, `[BitField(0, End = 7)]`, and `[BitField(Start = 0, Width = 8)]`. Both `End` and `Width` can be specified together if they are consistent (redundancy warning SD0016; error SD0017 if inconsistent). Missing range produces error SD0018; missing Start produces error SD0019.

### 🔵 Fixed
- Fixed constant naming
- Fixed nuisance build warnings due to missing release tracking files for compiler diagnostics.

### 🟤 Backwards Compatibility
- All existing `[BitFields]` and `[BitFieldsView]` APIs are backwards compatible with 0.9.5.
- `MustBe`/`UndefinedBitsMustBe` enforcement is now applied consistently in setters, `With...` methods, constructors, and parsing. Code that previously wrote invalid values to `MustBe.Zero`/`MustBe.One` fields will now have those writes silently corrected. This is a behavioral change but aligns with the documented contract.

## [0.9.5] - 2026-03-05
### 🟣 Changed
- Diagrams now handle overlapping

### 🟢 Added
- Added `Description` to `[BitFields]`
purpose of this is to simplify the API and deprecate `DiagramSection` that adds unnecessary complexity to the API. 

### 🟠 Deprecated
- Deprecated `DiagramSection` feature

### 🔵 Fixed
- Fixed compile error in generated `With{Name}`

### 🟤 Backwards Compatibility
- All APIs are backwards compatible with 0.9.4.
and does not affect existing functionality. 
- The `DiagramSection` type and related `RenderList`/`RenderListToString` methods are still present but marked as deprecated, 
with no breaking changes.

## [0.9.4] - 2026-02-09
### 🟢 Added
- **`BitFieldDiagram` RFC diagram generator**
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
- **`PeParser`** shared utility -- demonstrates `Result<T, TError>.Then()` chaining for multi-step PE header validation pipeline.
- **BitFieldDiagram test suite** -- 29 tests covering `Render`, `RenderToString`, `RenderList`, `RenderListToString`, `ComputeMinCellWidth`, bit order handling, undefined bits, descriptions, and separator structure.

### 🟤 Backwards Compatibility
All APIs are backwards compatible with 0.9.3.

## [0.9.3] - 2026-02-05
### 🟢 Added
- Added support for .NET 7 and .NET 8
- No feature changes.

## [0.9.2] - 2026-02-04 (First NuGet Release)
### 🟢 Added
- Added several NuGet project properties
- Added CHANGELOG.md, SECURITY.md, CODE_OF_CONDUCT.md.
- Added GitHub templates for issues and pull requests.

### 🔴 Removed
- Removed unused BitStream feature
- Removed a few unnecessary Extensions features that can be accomplished easily in .NET already.

## [Unreleased]

## [0.9.1] - 2026-02-01
### 🟢 Added
- Migrated from app-specific

## [0.9.0] - 2026-01-28
### 🟣 Changed
- `[BitFields]` types now implement `ISpanFormattable`
- `[BitFields]` types now implement `ISpanParsable<T>` for allocation-free string parsing.

### 🟢 Added
- Migrated from mature in-house library
- Added support for C#-style `_` digit separators in `Parse` and `TryParse` methods for `[BitField]` types.
- Added support for binary format parsing (e.g., `0b1101`) for `[BitField]` types.

## [0.0.1] - 2023-04-07
### 🟢 Added
- Initial internal release