# CLAUDE.md

Guidelines for AI assistants working with the Stardust.Utilities codebase.

## Project Overview

Stardust.Utilities is a .NET library providing zero-overhead bit manipulation via Roslyn source generators, endian-aware data types, and utility extensions. Users annotate `partial struct` types with `[BitFields]`, `[BitField]`, `[BitFlag]`, and `[BitFieldsView]` attributes; the source generator emits optimized property accessors, operators, parsing, and formatting at compile time.

## Repository Structure

```
Stardust.Utilities/
├── *.cs                              # Main library (attributes, endian types, extensions)
├── Stardust.Utilities.csproj         # Library project (net7.0;net8.0;net9.0;net10.0)
├── Stardust.Utilities.slnx           # Solution file (XML format, .NET 9+)
├── Generators/                       # Roslyn incremental source generator
│   ├── Stardust.Generators.csproj    # Targets netstandard2.0
│   ├── BitFieldsGenerator*.cs        # [BitFields] generator (partial files)
│   ├── BitFieldsViewGenerator.cs     # [BitFieldsView] generator
│   └── BitFieldsMultiWordGenerator*.cs # Arbitrary-width (up to 16,384-bit) generator
├── Test/                             # xUnit v3 test project
│   └── Stardust.Utilities.Tests.csproj  # Targets net8.0;net9.0;net10.0
├── Demo/
│   ├── BitFields.DemoApp/            # WPF demo (net10.0-windows)
│   └── BitFields.DemoWeb/            # Blazor WASM demo (net10.0)
├── build/                            # MSBuild .props/.targets for NuGet consumers
├── nupkg/                            # Built packages output (gitignored)
├── Build-Combined-NuGetPackages.ps1  # Primary build script
└── Build-Generator-NuGetPackage.ps1  # Generator-only build script
```

## Build Commands

```bash
# Restore and build everything (library + generator + tests)
dotnet build

# Build in Release mode (required for CI parity and JIT inlining validation)
dotnet build -c Release
```

**After modifying generator code** (`Generators/` directory), you must rebuild the NuGet package:

```powershell
.\Build-Combined-NuGetPackages.ps1 0.9.4 -SkipTests
```

Library-only or test-only changes do NOT require rebuilding the NuGet package — a regular `dotnet build` suffices.

## Test Commands

```bash
# Run all tests except performance (same as CI)
dotnet test Test/Stardust.Utilities.Tests.csproj -c Release --filter "Category!=Performance"

# Run tests on a specific framework
dotnet test Test/Stardust.Utilities.Tests.csproj -c Release --framework net10.0 --filter "Category!=Performance"

# Run all tests including performance (local only)
dotnet test Test/Stardust.Utilities.Tests.csproj -c Release

# Run only performance tests
dotnet test Test/Stardust.Utilities.Tests.csproj -c Release --filter "Category=Performance"

# Run fuzz tests
dotnet test -c Release --filter "FullyQualifiedName~ParsingFuzzTests" --framework net10.0
```

**Important testing rules:**
- Always restore/build individual `.csproj` files, never the `.slnx` solution, to avoid `SolutionPath` being set (which restricts multi-targeting to net10.0 only)
- Performance tests are excluded from CI via `--filter "Category!=Performance"` and also have an in-code `Assert.SkipWhen` guard
- xUnit v3 does not support .NET 7, so there is no `--framework net7.0` test step despite the library targeting it

## CI Pipeline

GitHub Actions (`.github/workflows/ci.yml`) runs on `windows-latest`:
1. Restores each `.csproj` individually (Generator, Library, Tests)
2. Builds in dependency order: Generator → Library → Tests (all Release, `--no-restore`)
3. Runs tests separately for net8.0, net9.0, net10.0 with `--filter "Category!=Performance"`

## Code Conventions

- **Namespaces**: `Stardust.Utilities` (library), `Stardust.Generators` (generator)
- **Namespace style**: File-scoped (`namespace Stardust.Utilities;`)
- **Language features**: `ImplicitUsings`, `Nullable` enabled; `LangVersion: latest` (library), `preview` (tests)
- **Naming**: `PascalCase` public members, `_camelCase` private fields, `camelCase` locals/parameters
- **Attributes**: Suffix with `Attribute` (e.g., `BitFieldsAttribute`)
- **Endian types**: `{Type}{Endian}` pattern (e.g., `UInt16Be`, `Int32Le`)
- **Test classes**: `{Feature}Tests` (e.g., `BitFieldTests`, `BigEndianTests`)
- **Test naming**: `MethodName_Scenario_ExpectedBehavior`
- **Test framework**: xUnit v3 with FluentAssertions (`.Should().Be(...)` style)
- **Performance**: Use `[MethodImpl(MethodImplOptions.AggressiveInlining)]` on hot paths
- **Documentation**: All public types/members require XML doc comments (`///`)
- **Generator files**: Split into partial classes by concern (e.g., `BitFieldsGenerator.Properties.cs`, `BitFieldsGenerator.Operators.cs`)

## Architecture Notes

### Source Generator

The generator is a Roslyn `IIncrementalGenerator` targeting `netstandard2.0`. It is:
- Referenced as a `ProjectReference` with `OutputItemType="Analyzer"` during development
- Embedded inside the `Stardust.Utilities` NuGet package under `analyzers/dotnet/cs/`
- The separate `Stardust.Generators` NuGet package is for local dev only, never distributed

### NuGet Packaging

- Version is specified at build time via the build script, NOT in `.csproj` files
- The current pre-release version is **0.9.4**
- Only `Stardust.Utilities` is published to NuGet.org; `Stardust.Generators` is local-only
- Package includes `build/*.props` and `build/*.targets` that auto-configure consumers (enable `EmitCompilerGeneratedFiles`, exclude generated files from double-compilation)

### Multi-Targeting

- Library targets: `net7.0`, `net8.0`, `net9.0`, `net10.0`
- Tests target: `net8.0`, `net9.0`, `net10.0` (when `SolutionPath` is unset)
- Generator targets: `netstandard2.0`

### Submodule Support

This repo is designed to work both standalone and as a Git submodule inside a parent solution. When used as a submodule, always commit/push inside the submodule first, then update the pointer in the parent repo.

## Common Pitfalls

1. **Generator changes not taking effect**: Clear the NuGet cache with `dotnet nuget locals global-packages --clear`, then restore and rebuild
2. **Duplicate compilation errors (CS0102)**: The `build/*.targets` file should handle this; if not, add `<Compile Remove="$(CompilerGeneratedFilesOutputPath)\**\*.cs" />`
3. **Bare `dotnet restore`**: Never run `dotnet restore` without specifying a `.csproj`; it discovers the `.slnx` and sets `SolutionPath`, breaking multi-target test builds
4. **Don't bump the version number** in build scripts unless explicitly asked — version `0.9.4` is the current pre-release
