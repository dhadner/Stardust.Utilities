# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.9.4] - 2026-02-09
### Added
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
- **`PeParser`** shared utility -- demonstrates `Result<T, TError>.Then()` chaining for multi-step PE header validation pipeline.
- **BitFieldDiagram test suite** -- 29 tests covering `Render`, `RenderToString`, `RenderList`, `RenderListToString`, `ComputeMinCellWidth`, bit order handling, undefined bits, descriptions, and separator structure.

### Backwards Compatibility
All APIs are backwards compatible with 0.9.3. New parameters on `[BitFields]`, `[BitField]`, and `[BitFlag]` attribute constructors use optional defaults that preserve existing behavior. `[BitFieldsView]`, `BitOrder`, `ByteOrder`, and the little-endian endian-aware types are entirely new additions. The `StorageType` property on `BitFieldsAttribute` changed from `Type` to `Type?` to support the new bit-count constructor overload.

## [0.9.3] - 2026-02-05
### Added
- Added support for .NET 7 and .NET 8 in addition to .NET 10.
- No feature changes.

## [0.9.2] - 2026-02-04 (First NuGet Release)
### Added
- Added several NuGet project properties, icon, links in preparation for release.
- Added CHANGELOG.md, SECURITY.md, CODE_OF_CONDUCT.md.
- Added GitHub templates for issues and pull requests.

### Removed
- Removed unused BitStream feature - not useful enough yet.
- Removed a few unnecessary Extensions features that can be accomplished easily in .NET already.

## [Unreleased]

## [0.9.1] - 2026-02-01
### Added
- Migrated from app-specific in-house library to NuGet package for better reuse.

## [0.9.0] - 2026-01-28
### Added
- Migrated from mature in-house library to NuGet package for better reuse.
- Added support for C#-style `_` digit separators in `Parse` and `TryParse` methods for `[BitField]` types.
- Added support for binary format parsing (e.g., `0b1101`) for `[BitField]` types.

### Changed
- `[BitFields]` types now implement `ISpanFormattable` for allocation-free string formatting.
- `[BitFields]` types now implement `ISpanParsable<T>` for allocation-free string parsing.

## [0.0.1] - 2023-04-07
### Added
- Initial internal release to private GitHub repo.