# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

**Build the NuGet packages** (required after any generator or library code change):
```powershell
# Standard build (reads version from Directory.Build.props)
.\Build-Combined-NuGetPackages.ps1

# Skip tests for faster iteration
.\Build-Combined-NuGetPackages.ps1 -SkipTests

# Override version explicitly
.\Build-Combined-NuGetPackages.ps1 0.9.9
```

The script builds both packages, runs tests, outputs to `./nupkg/`, and publishes to `~/.nuget/local-packages/`.

**Demo app changes only** — no need to rebuild the NuGet package; just use `dotnet build` in the demo project.

**Run tests:**
```bash
dotnet test Test/Stardust.Utilities.Tests.csproj
```

Do not run the `Performance` test category unless specifically checking for performance regressions — it is slow and times out.

**Run a specific test:**
```bash
dotnet test Test/Stardust.Utilities.Tests.csproj --filter "FullyQualifiedName~TestClassName"
```

## Architecture

**Solution file:** `Stardust.Utilities.slnx` (modern XML solution format)

**Projects:**
- `Stardust.Utilities.csproj` — Main library, multi-targets net7.0/8.0/9.0/10.0
- `Generators/Stardust.Generators.csproj` — Roslyn source generator (targets netstandard2.0); packed into the NuGet package under `analyzers/dotnet/cs/`
- `Test/Stardust.Utilities.Tests.csproj` — xUnit 3 + FluentAssertions test suite (39 test files)
- `BenchmarkSuite1/` — BenchmarkDotNet performance suite
- `Demo/BitFields.DemoApp/` — WPF demo
- `Demo/BitFields.DemoWeb/` — Blazor WASM demo

**Version:** Centralized in `Directory.Build.props`. Demo `.csproj` files pick it up via `$(Version)` automatically. Do not change the version unless asked.

### Core Components

**BitFields (source-generated bit manipulation):**
- Annotate a `partial struct` with `[BitFields(StorageType.UInt32)]` and `[BitField]`/`[BitFlag]` properties.
- The generator emits inline bit-shift/mask property implementations with zero overhead.
- Two modes: *value type* (`partial struct`) stores a private `__value` field; *zero-copy view* (`partial record struct`) stores a `Memory<byte> __data` field.
- Generator source lives in `Generators/` (split across `BitFieldsGenerator.cs`, `RecordStructViewGenerator.cs`, `BitFieldsMultiWordGenerator.cs`, and related partial files).
- Compiler diagnostics are `SD0001`–`SD0023`, defined in `BitFieldsDiagnostics.cs`.

**Endian types (16 types):**
- `UInt16Be`/`Le`, `UInt32Be`/`Le`, `UInt64Be`/`Le`, `UInt128Be`/`Le`, `UInt256Be`/`Le`
- `Int16Be`/`Le`, `Int32Be`/`Le`, `Int64Be`/`Le`, `Int128Be`/`Le`, `Int256Be`/`Le`
- Each file also has a corresponding `*TypeConverter.cs` for PropertyGrid support.
- Zero-cost on native platform; a single `BSWAP` instruction on the non-native side.

**Large integers:**
- `UInt256.cs` / `Int256.cs` — 256-bit fixed-width (4×ulong); hardware-accelerated (BMI2 MULX, X86Base.X64.DivRem).
- `UInt256Math.cs`, `UInt256Vector.cs` — helpers for multiply/divide and SIMD paths.

**Result / Option (Rust-style error handling):**
- `Result.cs` — `Result<T, TError>` and void `Result<TError>`; factories `Ok()`/`Err()` available via global using.
- `Option.cs` — `Option<T>`; factories `Some(T)` / `None`.
- Full monadic transform methods: `Map`, `AndThen`, `MapErr`, `Flatten`, `Transpose`, `OkOr`, etc.

**Extensions (`Extensions.cs`):**
- `Hi()`, `Lo()`, `SetHi()`, `SetLo()` — byte extraction/modification on all integer types.
- `SaturatingAdd()`, `SaturatingSub()` — clamped arithmetic on all integer types.

**BitFieldDiagram (`BitFieldDiagram.cs`):**
- Generates RFC 2360-style ASCII bit-field diagrams from `[BitFields]` metadata.

## Coding Conventions

- All constants (`const` fields and `const` locals) must use `SCREAMING_SNAKE_CASE`.
- Features involving user or external input require fuzz tests in addition to unit tests.
- Do not add test cases that will be invalidated by a change — instead modify them. Never reduce test coverage.
- When changing features, update test coverage in the same commit (add, modify, or remove tests accordingly).
- Unhandled or poorly-handled edge cases are not acceptable; the library must always behave correctly.

## GitHub Issues

- Use the `bug` label for bugs and the `enhancement` label for new features/improvements.
- Use the project issue template when creating new issues.

## Output Formatting

- Use plain text only in responses — do not use markdown hyperlinks (known rendering bug).
