# Stardust.Utilities Developer Guide

This guide explains how to modify the source generators and update consuming projects.

## Table of Contents

- [Quick Reference: Build Workflow](#quick-reference-build-workflow)
- [Getting Started](#getting-started)
  - [Opening the Project](#opening-the-project)
  - [Project Structure](#project-structure)
- [Submodule Workflow](#submodule-workflow)
  - [How the submodule is structured](#how-the-submodule-is-structured)
  - [Committing changes](#committing-changes)
  - [Detached HEAD pitfall](#detached-head-pitfall)
  - [CI is independent](#ci-is-independent)
  - [Pulling upstream changes](#pulling-upstream-changes)
- [Building and Packaging](#building-and-packaging)
  - [What Requires What Workflow?](#what-requires-what-workflow)
  - [Build Scripts](#build-scripts)
  - [Package Reference Scenarios](#package-reference-scenarios)
  - [Updating the Source Generator](#updating-the-source-generator)
  - [NuGet Package Architecture](#nuget-package-architecture)
- [Testing](#testing)
  - [Unit Tests](#unit-tests)
  - [Performance Tests](#performance-tests)
  - [Fuzz Tests](#fuzz-tests)
- [CI and Multi-Targeting](#ci-and-multi-targeting)
  - [Multi-Targeting and SolutionPath](#multi-targeting-and-solutionpath)
  - [CI Workflow Design](#ci-workflow-design)
- [Features](#features)
  - [Rust-Style Bit Ranges](#rust-style-bit-ranges)
  - [Nested Struct Support](#nested-struct-support)
  - [Automatic IntelliSense](#automatic-intellisense)
  - [Full Operator Support](#full-operator-support)
- [Troubleshooting](#troubleshooting)
- [Version History](#version-history)
- [API Simplification Trade Study (v0.3.0)](#api-simplification-trade-study-v030)

## Quick Reference: Build Workflow

**To build a new version (e.g., 0.9.0):**

```powershell
# Navigate to the Stardust.Utilities directory
cd Stardust.Utilities

# Build both NuGet packages (automatically publishes to local feed)
.\Build-Combined-NuGetPackages.ps1 0.9.0

# Or skip tests for faster iteration during development
.\Build-Combined-NuGetPackages.ps1 0.9.0 -SkipTests
```

**What happens automatically:**
1. Builds the generator and library
2. Runs unit tests (unless `-SkipTests`)
3. Creates packages in `./nupkg/`
4. Publishes to local NuGet feed (`~/.nuget/local-packages/`)
5. Clears NuGet cache so consuming projects pick up changes

**Then update consuming projects:**
1. Update `PackageReference` version in each .csproj that uses `Stardust.Utilities`
2. Rebuild the consuming solution

> **Note:** Version is specified at build time and is NOT stored in .csproj files. This keeps source control clean and avoids accidental version mismatches.

## Getting Started

### Opening the Project

Open `Stardust.Utilities.slnx` in Visual Studio or VS Code:

```powershell
# Open in Visual Studio
start Stardust.Utilities.slnx

# Or open in VS Code
code .
```

This solution uses the new XML-based solution format (`.slnx`) which is cleaner and easier to merge than the legacy `.sln` format.

### Project Structure

```
Stardust.Utilities/
├── Stardust.Utilities.slnx              # Solution file (XML format, .NET 9+)
├── Stardust.Utilities.csproj            # Main library (types, attributes)
├── Generators/
│   ├── Stardust.Generators.csproj       # Source generator project
│   ├── BitFieldsGenerator.cs            # [BitFields] generator
│   └── BitFieldsViewGenerator.cs         # [BitFieldsView] generator
├── Test/
│   └── Stardust.Utilities.Tests.csproj
├── build/
│   ├── Stardust.Utilities.props         # Auto-enables IntelliSense for consumers
│   └── Stardust.Utilities.targets       # Auto-excludes generated files from compilation
├── nupkg/                               # Local NuGet packages output
├── Build-Generator-NuGetPackage.ps1     # Builds generator package (for local development)
├── Build-Combined-NuGetPackages.ps1     # Builds the distributable package
├── README.md                            # User documentation
└── DEVELOPER.md                         # This file
```

## Submodule Workflow

Stardust.Utilities is designed to work both as a **standalone repository** and as a
**Git submodule** inside a parent solution (e.g., MySolution). This dual-use pattern requires
some awareness when committing and when modifying CI or build configuration.

### How the submodule is structured

```
MySolution/                       ← parent repo (branch: adb)
├── MySolution.sln                ← parent solution (references Stardust.Utilities projects)
├── Stardust.Utilities/           ← Git submodule (branch: main or dev)
│   ├── Stardust.Utilities.slnx   ← standalone solution
│   ├── .github/workflows/ci.yml  ← CI runs against THIS repo independently
│   └── ...
└── ...
```

The parent solution (`MySolution.sln`) includes `Stardust.Utilities.csproj`,
`Stardust.Generators.csproj`, and `Stardust.Utilities.Tests.csproj` directly.
MSBuild sets `SolutionPath` when building through a solution, which the test project
uses to control multi-targeting (see [Multi-Targeting and SolutionPath](#multi-targeting-and-solutionpath) below).

### Committing changes

When Stardust.Utilities is checked out as a submodule, it has its **own independent Git
history**. Changes must be committed and pushed from within the submodule directory:

```powershell
# 1. Navigate into the submodule
cd Stardust.Utilities

# 2. Verify you are on the correct branch (submodules can detach HEAD)
git branch --show-current
# If empty (detached HEAD), re-attach:
git checkout main   # or dev

# 3. Stage, commit, and push as normal
git add -A
git commit -m "Your commit message"
git push origin main

# 4. Return to parent repo and update the submodule reference
cd ..
git add Stardust.Utilities
git commit -m "Update Stardust.Utilities submodule pointer"
git push
```

> **Important:** Always commit and push inside the submodule **first**, then update the
> submodule pointer in the parent repo. If you push the parent first, it will reference a
> commit that doesn't exist on the remote and other clones will fail.

### Detached HEAD pitfall

Git submodules check out a specific **commit**, not a branch. After cloning the parent
repo or switching branches, the submodule may be in a "detached HEAD" state:

```powershell
cd Stardust.Utilities
git status
# "HEAD detached at abc1234"

# Fix: checkout the branch you want to work on
git checkout dev
```

### CI is independent

The `.github/workflows/ci.yml` inside `Stardust.Utilities/` triggers on pushes to the
**Stardust.Utilities** GitHub repository. It does **not** run when the parent repo is
pushed. This means:

- Stardust.Utilities CI validates the library in isolation (all TFMs, all test categories
  except Performance)
- The parent repo's CI (if any) is responsible for integration testing

### Pulling upstream changes

```powershell
# From the parent repo root
git submodule update --remote Stardust.Utilities

# Or from within the submodule
cd Stardust.Utilities
git pull origin main
```

## Building and Packaging

### What Requires What Workflow?

| What You Changed | How to See Changes |
|------------------|-------------------|
| **Stardust.Utilities library** (types, attributes) | Just rebuild in Visual Studio |
| **Stardust.Generators** (source generator code) | Run `Build-Generator-NuGetPackage.ps1` or `Build-Combined-NuGetPackages.ps1` |
| **build/*.props or build/*.targets** | Run `Build-Combined-NuGetPackages.ps1` |

### Build Scripts

#### Build-Combined-NuGetPackages.ps1

**This is the primary build script.** It builds both NuGet packages and automatically publishes to the local feed:
- `Stardust.Utilities.x.y.z.nupkg` - **The distributable package** (includes generator as embedded analyzer + utility types)
- `Stardust.Generators.x.y.z.nupkg` - Local development package only (not for distribution)

```powershell
# Show help
.\Build-Combined-NuGetPackages.ps1 -Help

# Build version 0.9.0 (runs tests, publishes to local feed)
.\Build-Combined-NuGetPackages.ps1 0.9.0

# Skip tests for faster iteration
.\Build-Combined-NuGetPackages.ps1 0.9.0 -SkipTests

# Use Debug configuration
.\Build-Combined-NuGetPackages.ps1 0.9.0 -Configuration Debug
```

**Parameters:**

| Parameter | Required | Description |
|-----------|----------|-------------|
| `<version>` | Yes | Version number (e.g., `0.9.0`, `1.0.0-beta1`) |
| `-SkipTests` | No | Skip running unit tests |
| `-Configuration` | No | `Debug` or `Release` (default: Release) |
| `-Help` | No | Show help message |

**What it does:**
1. Cleans previous build artifacts
2. Builds the generator package
3. Builds the main library
4. Runs unit tests (unless `-SkipTests`)
5. Creates NuGet packages in `./nupkg/`
6. Copies packages to `~/.nuget/local-packages/`
7. Clears NuGet cache for `stardust.utilities` and `stardust.generators`

**Output locations:**
- `./nupkg/` - Package files
- `~/.nuget/local-packages/` - Local NuGet feed (packages appear in NuGet Package Manager)

#### Build-Generator-NuGetPackage.ps1

Builds only the `Stardust.Generators.x.y.z.nupkg` standalone generator package **for local development**.

> **Note:** This package is not published to NuGet.org. It exists only to support debugging scenarios where
> `Stardust.Utilities` is referenced via `ProjectReference`.

**Use this when:**
- Debugging `Stardust.Utilities` via ProjectReference in Visual Studio
- The embedded analyzer doesn't load with ProjectReference, so this standalone package provides the generator

```powershell
# Specify a version
.\Build-Generator-NuGetPackage.ps1 -Version "0.9.0"
```

### Package Reference Scenarios

| Scenario | What to Reference |
|----------|-------------------|
| **Normal usage** (consuming the library) | `Stardust.Utilities` NuGet package only |
| **Debugging Stardust.Utilities locally** | ProjectReference to `Stardust.Utilities.csproj` + PackageReference to `Stardust.Generators` (local only) |

> **Important:** Only `Stardust.Utilities` is published to NuGet.org. The `Stardust.Generators` package is for local
> development only and should never be distributed separately.

### Updating the Source Generator

**Step 1:** Edit the generator in `Generators/BitFieldsGenerator.cs`.

**Step 2:** Rebuild the package:

```powershell
# Rebuild both packages (recommended)
.\Build-Combined-NuGetPackages.ps1 0.9.0 -SkipTests
```

**Step 3:** Test locally with a consuming project:
1. Update the `PackageReference` version in the consuming project
2. Clear NuGet cache: `dotnet nuget locals global-packages --clear`
3. Restore and rebuild

### NuGet Package Architecture

#### Why Two Packages Exist

The **`Stardust.Generators`** package is **not intended for public distribution**. It exists solely
to support local development scenarios. The source generator is embedded directly within the `Stardust.Utilities`
package for distribution.

The separate `Stardust.Generators` package is needed because:
- When debugging `Stardust.Utilities` via `ProjectReference`, the embedded analyzer doesn't load
- The standalone generator package includes MSBuild `.props`/`.targets` files that automatically configure consumer projects
- This enables IntelliSense for generated code without manual project configuration

**For end users:** Only reference `Stardust.Utilities` — the generator is included automatically.

#### Stardust.Utilities Package Contents

The `Stardust.Utilities` NuGet package includes:

| Path | Description |
|------|-------------|
| `lib/net10.0/Stardust.Utilities.dll` | Main library with attributes and types |
| `analyzers/dotnet/cs/Stardust.Generators.dll` | Source generator (embedded, runs at compile time) |
| `build/Stardust.Utilities.props` | Auto-enables `EmitCompilerGeneratedFiles` for IntelliSense |
| `build/Stardust.Utilities.targets` | Auto-excludes generated files from duplicate compilation |
| `README.md` | Package documentation |

## Testing

### Unit Tests

Run all unit tests with:

```powershell
cd Stardust.Utilities
dotnet test -c Release
```

This runs tests on all target frameworks (.NET 8, 9, and 10).

To run tests on a specific framework:

```powershell
dotnet test -c Release --framework net10.0
```

### Performance Tests

Performance tests compare the generated BitField code against hand-coded bit manipulation to verify
there is no performance overhead from using the source generator.

**Performance tests are excluded from CI** using two independent layers:
1. **CI workflow filter:** `--filter "Category!=Performance"` prevents them from running at all
2. **In-code guard:** `Assert.SkipWhen(CiEnvironmentDetector.IsRunningInCi, ...)` skips them if
   the filter is ever removed or tests are invoked without the filter

This dual-layer approach ensures performance tests never cause CI failures, even if the workflow
is modified or tests are run through a different pipeline.

**Locally in Visual Studio:** Performance tests are visible in Test Explorer (tagged with
`[Trait("Category", "Performance")]`). They run when explicitly selected and produce timing
results with pass/fail status indicators. They also run when "Run All Tests" is invoked, since
the CI guard does not apply locally.

There are six performance tests:
- **`BitFlag_Get_Performance`**: Tests single-bit read performance
- **`BitFlag_Set_Performance`**: Tests single-bit write performance
- **`BitField_Get_Performance`**: Tests multi-bit field read performance
- **`BitField_Set_Performance`**: Tests multi-bit field write performance
- **`Mixed_ReadWrite_Performance`**: Quick sanity check (single run, ~500ms), outputs results but doesn't fail on variance
- **`FullSuite_Performance_Summary`**: Rigorous statistical analysis (20 runs, ~2 minutes), computes mean, σ, and 95% CI

#### Running Performance Tests

**From Visual Studio:** Select individual performance tests in Test Explorer and click Run,
or run all tests (performance tests will execute locally).

**From the command line:**

```powershell
cd Stardust.Utilities

# Run all tests including performance tests
dotnet test Test/Stardust.Utilities.Tests.csproj -c Release

# Run only performance tests
dotnet test Test/Stardust.Utilities.Tests.csproj -c Release --filter "Category=Performance"

# Run the comprehensive statistical suite
dotnet test Test/Stardust.Utilities.Tests.csproj -c Release --filter "FullyQualifiedName~FullSuite_Performance_Summary" --framework net10.0

# Run all tests EXCEPT performance tests (same as CI)
dotnet test Test/Stardust.Utilities.Tests.csproj -c Release --filter "Category!=Performance"
```

#### Understanding Performance Test Output

The `Mixed_ReadWrite_Performance` test outputs timing results with a status indicator:
- ✓ Performance is within expected range (ratio 0.75-1.25)
- ⚠️ WARNING if generated code is >25% slower (may indicate regression or system load)

The `FullSuite_Performance_Summary` test runs 20 iterations of each test and produces statistical output:

```
BITFIELD PERFORMANCE SUMMARY WITH STATISTICS
Runs: 20, Iterations per run: 100,000,000

============================================================

| Test          | Generated | &sigma;  | Hand-coded | &sigma;  | Ratio | &sigma;     | 95% CI        |
|---------------|-----------|----| -----------|----|-------|-------|---------------|
| BitFlag GET   | 584 ms    | 14 | 568 ms     | 15 | 1.029 | 0.035 | 0.960 – 1.098 |
| BitFlag SET   | 825 ms    | 27 | 821 ms     | 22 | 1.006 | 0.031 | 0.945 – 1.067 |
| BitField GET  | 402 ms    | 36 | 405 ms     | 18 | 0.995 | 0.087 | 0.824 – 1.166 |
| BitField SET  | 413 ms    | 9  | 410 ms     | 7  | 1.007 | 0.020 | 0.968 – 1.046 |
| Mixed R/W     | 1031 ms   | 13 | 1030 ms    | 23 | 1.001 | 0.024 | 0.954 – 1.048 |
| **Overall**   |           |    |            |    | 1.008 | 0.048 | 0.914 – 1.102 |
```

- **&sigma;**: Sample standard deviation of the measurements
- **Ratio**: Generated time / Hand-coded time (1.0 = identical performance)
- **95% CI**: 95% Confidence Interval for the mean = mean ± 1.96 × SE, where SE = &sigma;/√n

**Expected results**: Ratio should be between 0.9 and 1.1 (within 10% of hand-coded performance).

### Fuzz Tests

Fuzz tests verify that parsing methods handle malformed, malicious, and edge-case inputs gracefully:

```powershell
# Run only fuzz tests
dotnet test -c Release --filter "FullyQualifiedName~ParsingFuzzTests" --framework net10.0
```

Fuzz tests cover:
- Null/empty/whitespace inputs
- Overflow and boundary values
- Injection attacks (SQL, XSS, command, path traversal)
- Unicode homoglyphs and invisible characters
- Control characters and embedded nulls
- Random garbage data (1000+ random inputs)

## CI and Multi-Targeting

### Multi-Targeting and SolutionPath

The test project (`Test/Stardust.Utilities.Tests.csproj`) uses an MSBuild condition to
control which target frameworks are built:

```xml
<!-- When SolutionPath is unset (CI, standalone dotnet build): multi-target -->
<TargetFrameworks Condition="'$(SolutionPath)' == '' or '$(SolutionPath)' == '*Undefined*'">
  net8.0;net9.0;net10.0
</TargetFrameworks>

<!-- When SolutionPath is set (opened in a solution like MySolution.sln): single-target -->
<TargetFramework Condition="'$(SolutionPath)' != '' and '$(SolutionPath)' != '*Undefined*'">
  net10.0
</TargetFramework>
```

**Why this exists:** When the test project is included in a parent solution (e.g.,
`MySolution.sln`), solution-level NuGet restore writes a single `project.assets.json`. If
the test project multi-targets, the restore for `net8.0`/`net9.0` can overwrite the
assets file needed by other projects in the solution. Limiting to `net10.0` avoids this.

**Impact on different workflows:**

| Workflow | `SolutionPath` | Test TFMs | Notes |
|----------|---------------|-----------|-------|
| Visual Studio (any `.sln` or `.slnx`) | Set | `net10.0` only | Developer sees only .NET 10 tests in Test Explorer |
| `dotnet test Test/*.csproj` (CLI) | Unset | `net8.0`, `net9.0`, `net10.0` | Full multi-target coverage |
| GitHub Actions CI | Unset | `net8.0`, `net9.0`, `net10.0` | Projects restored explicitly, not via solution |
| `Build-Combined-NuGetPackages.ps1` | Unset | `net8.0`, `net9.0`, `net10.0` | Script invokes `dotnet test` on `.csproj` directly |

> **Key rule for CI/build scripts:** Always restore and build individual `.csproj` files,
> never the `.slnx` solution. This keeps `SolutionPath` unset and enables multi-targeting.

### CI Workflow Design

The GitHub Actions workflow (`.github/workflows/ci.yml`) is designed to work correctly
regardless of whether Stardust.Utilities is checked out standalone or as a submodule.

#### Key design decisions

**1. Explicit project restores (not solution restore)**

```yaml
# ✅ Correct: restore each project individually
- run: dotnet restore Generators/Stardust.Generators.csproj
- run: dotnet restore Stardust.Utilities.csproj
- run: dotnet restore Test/Stardust.Utilities.Tests.csproj

# ❌ Wrong: bare "dotnet restore" finds the .slnx and sets SolutionPath,
#    causing the test project to restore for net10.0 only
- run: dotnet restore
```

When `dotnet restore` runs without arguments, it discovers `Stardust.Utilities.slnx` and
restores through the solution. This sets `SolutionPath`, which triggers the single-target
`net10.0` condition in the test project. Restoring each `.csproj` individually leaves
`SolutionPath` unset, so the test project correctly restores for all three TFMs.

**2. Performance test exclusion via `--filter`**

```yaml
- run: dotnet test ... --filter "Category!=Performance"
```

Performance tests have `[Trait("Category", "Performance")]` and are excluded from CI runs
because GitHub Actions runners are shared VMs with unpredictable CPU scheduling. The tests
also contain `Assert.SkipWhen(CiEnvironmentDetector.IsRunningInCi, ...)` as a fallback.

**3. .NET 7 SDK installed but not tested**

The library targets `net7.0` for consumers who haven't upgraded, but xUnit v3 does not
support .NET 7. The .NET 7 SDK is installed so the library **builds** for `net7.0`, but
there is no `dotnet test --framework net7.0` step.

**4. Separate build steps**

The workflow builds Generator → Library → Tests in dependency order with `--no-restore`,
then runs tests per-framework. This matches the project dependency graph and ensures each
step uses the same restored packages.

#### Making changes to the CI workflow

If you modify `ci.yml`, verify these invariants:

- [ ] `dotnet restore` is called on individual `.csproj` files, never bare or on `.slnx`
- [ ] All SDK versions in `setup-dotnet` match the library's `TargetFrameworks` (currently 7, 8, 9, 10)
- [ ] Test steps include `--filter "Category!=Performance"`
- [ ] No `dotnet test --framework net7.0` step (xUnit v3 doesn't support it)
- [ ] Build steps use `-c Release` (required for JIT inlining in performance-sensitive code)

## Features

### Rust-Style Bit Ranges

The `[BitField]` attribute uses Rust-style inclusive bit ranges:

```csharp
[BitFields(typeof(byte))]
public partial struct RegisterA
{
    // 3-bit field at bits 0, 1, 2 (like Rust's 0..=2)
    [BitField(0, 2)] public partial byte Sound { get; set; }
    
    // Single bit at position 3
    [BitField(3, 3)] public partial byte Flag { get; set; }
    
    // 4-bit field at bits 4, 5, 6, 7 (like Rust's 4..=7)
    [BitField(4, 7)] public partial byte Mode { get; set; }
}
```

Width is calculated as `(endBit - startBit + 1)`.

### Nested Struct Support

BitFields structs can be nested inside classes. The containing types must be marked `partial`:

```csharp
public partial class HardwareController
{
    [BitFields(typeof(byte))]
    public partial struct StatusRegister
    {
        [BitFlag(0)] public partial bool Ready { get; set; }
        [BitField(4, 7)] public partial byte Mode { get; set; }  // bits 4..=7 (4 bits)
    }
}
```

### Automatic IntelliSense

The NuGet package automatically enables IntelliSense for generated code:

- **`.props` file**: Sets `EmitCompilerGeneratedFiles=true` by default
- **`.targets` file**: Excludes generated files from compilation (prevents duplicates)
- **Generated files location**: `obj/Generated/` (doesn't clutter project folder)

Users don't need to add any configuration - it just works!

To **disable** automatic file emission, add to your project:
```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>false</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

### Full Operator Support

BitFields structs support a complete set of operators for parity with the underlying storage type and big-endian types:

| Category | Operators | Notes |
|----------|-----------|-------|
| **Arithmetic** | `+`, `-` (unary/binary), `*`, `/`, `%` | Unchecked wraparound matches native behavior |
| **Bitwise** | `&`, `\|`, `^`, `~` | Mixed-type operators with storage type |
| **Shift** | `<<`, `>>`, `>>>` | Shift amount is always `int` |
| **Comparison** | `<`, `>`, `<=`, `>=`, `==`, `!=` | Direct value comparison |
| **Conversions** | Implicit to/from storage type | Zero-cost conversions |

**Interface implementations:**
- `IComparable`, `IComparable<T>` — sorting and ordering support
- `IEquatable<T>` — efficient equality comparison
- `IFormattable`, `ISpanFormattable` — format string support (e.g., `"X2"`, `"D"`)
- `IParsable<T>`, `ISpanParsable<T>` — parsing with hex (`0x`) and binary (`0b`) support, can include underscores

**Example usage:**
```csharp
GeneratedStatusReg8 a = 0x0F;
GeneratedStatusReg8 b = 0x10;

// Arithmetic
var sum = a + b;              // 0x1F
var shifted = a << 4;         // 0xF0

// Comparison and sorting
bool isLess = a < b;          // true
var sorted = new[] { b, a }.OrderBy(x => x).ToArray();

// Formatting
string hex = a.ToString("X2", null);  // "0F"

// Parsing
var parsed = GeneratedStatusReg8.Parse("0xFF");
```

**Parity with native types:**
- Overflow/underflow uses unchecked semantics (wraparound)
- Division by zero throws `DivideByZeroException`
- Shift operators take `int` for shift amount

> **Note:** Unary `-` for unsigned types is an extension (native `uint`/`ulong` don't support it).
> It produces two's complement negation: `-1` on a `byte` yields `255`.

## Troubleshooting

### Generator changes not taking effect

**Symptom:** You modified the generator, but the output code hasn't changed.

**Cause:** NuGet cached the old package version.

**Solution:**
```powershell
dotnet nuget locals global-packages --clear
dotnet restore
dotnet build
```

### "Partial property must have an implementation" errors

**Symptom:** Build fails with CS9248 errors after updating the generator.

**Cause:** The generator isn't running or has a bug.

**Solutions:**
1. Check that the `PackageReference` version matches the packed version
2. Clear NuGet cache and restore
3. Check generator code for compilation errors:
   ```powershell
   dotnet build "Generators\Stardust.Generators.csproj"
   ```

### Duplicate compilation errors

**Symptom:** CS0102 errors about duplicate definitions.

**Cause:** Both the generator AND the emitted files are being compiled.

**Solution:** The package's `.targets` file should handle this automatically. If not:
```xml
<ItemGroup>
  <Compile Remove="$(CompilerGeneratedFilesOutputPath)\**\*.cs" />
</ItemGroup>
```

### Viewing generated code

To see what the generator produces:

1. **Visual Studio:** Project ? Dependencies ? Analyzers ? Stardust.Generators
2. **File System:** `obj\Generated\Stardust.Generators\Stardust.Generators.BitFieldsGenerator\`

### Debugging the generator

1. Add `System.Diagnostics.Debugger.Launch()` at the start of the generator's `Execute` method
2. Build the consuming project
3. A debugger prompt will appear - attach Visual Studio

## Version History

See [CHANGELOG.md](https://github.com/dhadner/Stardust.Utilities/blob/main/CHANGELOG.md) for detailed version history and release notes.

## API Simplification Trade Study (v0.3.0)

### Background

Prior to v0.3.0, BitFields supported two patterns:
1. **User-declared Value field**: `[BitFields]` with `public byte Value;`
2. **Generator-created Value field**: `[BitFields(typeof(byte))]` with private Value

Performance testing showed no benefit to the user-declared pattern.  That is, directly
accessing the Value field from user code vs. implicit conversions had negligible affect 
on speed.

```
// Example structs for performance comparison

// User-declared public value determined type

[BitFields]               
public partial struct RegisterWithVisibleValue
{
    public byte Value; 

    [BitField(0, 3)] public byte Field1 { get; set; }
    [BitField(4, 7)] public byte Field2 { get; set; }
}

// Attribute determines type, generator creates private Value field

[BitFields(typeof(byte))] 
public partial struct RegisterWithConversion
{
    [BitField(0, 3)] public byte Field1 { get; set; }
    [BitField(4, 7)] public byte Field2 { get; set; }
}

// Compare use of both patterns, same performance either way

private void AccessEntireRegister()
{
    RegisterWithVisibleValue regVisibleValue = new();
    RegisterWithConversion reg = new();

    // Use public Value field version
    byte value = regVisibleValue.Value;   // Direct access to public value field
    regVisibleValue.Value = 0xFF;         // Direct assignment to public value field

    // Use implicit conversion version
    byte valConvert = reg;                // Implicit conversion
    reg = 0xFF;                           // Implicit assignment
}
```

### Decision

The user-declared Value field pattern was removed in v0.3.0. The simplified API:
- Requires `[BitFields(typeof(T))]` with explicit storage type
- Generates private Value field automatically
- Uses implicit conversions and constructors for all raw value access
- **No performance penalty** compared to the previous approach

### Signed Storage Type Support

v0.3.0 also added support for signed storage types (`sbyte`, `short`, `int`, `long`).
Performance testing showed signed types are approximately 22% slower than unsigned
equivalents due to the additional casts required to avoid sign extension issues in
bitwise operations. For most use cases, this overhead is acceptable.
