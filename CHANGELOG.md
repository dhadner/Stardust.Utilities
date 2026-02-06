# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.9.4] - 2026-02-06
### Added
- Added support for signed properties in a BitFields struct.
- Although already supported, added testing and documentation for BitFields struct composition (using one BitFields type as a property type in another).
- Added `UndefinedBitsMustBe` enum with `Any`, `Zeroes`, and `Ones` values for controlling undefined bit behavior in `[BitFields]`.
- Added `MustBe` enum with `Any`, `Zero`, `One`, and `Ones` values for per-field/flag bit control.
- Added `ValueOverride` parameter to `[BitField]` and `[BitFlag]` attributes for per-field/flag bit override.
- Added support for sparse undefined bits (gaps between defined fields).
- Added fuzz testing for parsers. No errors found.
- Performance testing now runs on local dev machine. Still disabled during CI builds.
- Builds are now deterministic.







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