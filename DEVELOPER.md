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
- [Building for Big-Endian Platforms](#building-for-big-endian-platforms)
  - [The BigEndian MSBuild Property](#the-bigendian-msbuild-property)
  - [Why Not Auto-Detect from Architecture Name?](#why-not-auto-detect-from-architecture-name)
  - [Runtime Validation](#runtime-validation)
  - [Testing on s390x via QEMU User-Mode](#testing-on-s390x-via-qemu-user-mode)
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
- [Demo Apps](#demo-apps)
  - [DemoWeb (Blazor WebAssembly)](#demoweb-blazor-webassembly)
  - [DemoApp (WPF)](#demoapp-wpf)
- [Troubleshooting](#troubleshooting)
- [Version History](#version-history)
- [API Simplification Trade Study (v0.3.0)](#api-simplification-trade-study-v030)
- [Releasing a Version](#releasing-a-version)

## Quick Reference: Build Workflow

**To build a new version:**

```powershell
# Navigate to the Stardust.Utilities directory
cd Stardust.Utilities

# Build both NuGet packages (version read from Directory.Build.props)
.\Build-Combined-NuGetPackages.ps1

# Or skip tests for faster iteration during development
.\Build-Combined-NuGetPackages.ps1 -SkipTests

# Or override the version explicitly
.\Build-Combined-NuGetPackages.ps1 0.9.9
```

**What happens automatically:**
1. Reads the version from `Directory.Build.props` (unless overridden on the command line)
2. Lints `README.md` for image references with non-absolute URLs (nuget.org only renders images from its trusted-domain allowlist)
3. Lints `README.md` for stale version references (install-snippet `Version="x.y.z"` and bolded `**vX.Y.Z**` banners must all match `$Version`, or the build fails)
4. Builds the generator and library
5. Runs unit tests (unless `-SkipTests`)
6. **Pins relative `.md` links in a packed copy of `README.md`** to tag-pinned absolute URLs (`https://github.com/dhadner/Stardust.Utilities/blob/v{Version}/<file>.md`) -- the on-disk source README is always restored after pack (including on failure), so the working tree is never left in the rewritten state
7. Creates packages in `./nupkg/`
8. Publishes to local NuGet feed (`~/.nuget/local-packages/`)
9. Clears NuGet cache so consuming projects pick up changes

**Then update consuming projects:**
1. Update `PackageReference` version in each .csproj that uses `Stardust.Utilities`
2. Rebuild the consuming solution

> **Note:** The package version is defined in `Directory.Build.props` at the repo root. Demo app .csproj files reference it via `$(Version)` automatically. The build script reads this version by default; you can override it by passing a version argument.

> **Release order matters.** Because step 5 above pins README links to the `v{Version}` git tag, push that tag to GitHub **before** uploading the `.nupkg` to nuget.org -- otherwise the links in the published README 404 until the tag lands. See [Releasing a Version](#releasing-a-version) for the full sequence.

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
│   ├── RecordStructViewGenerator.cs      # [BitFields] record struct view generator
│   └── *.cs                             # Additional generator source files
├── Test/
│   └── Stardust.Utilities.Tests.csproj
│   └── *.cs                             # Test cases and supporting source files
├── build/
│   ├── Stardust.Utilities.props         # Auto-enables IntelliSense for consumers
│   └── Stardust.Utilities.targets       # Auto-excludes generated files from compilation
├── nupkg/                               # Local NuGet packages output
├── *.cs                                 # Stardust.Utilities source files
├── Build-Generator-NuGetPackage.ps1     # Builds generator package (for local development)
├── Build-Combined-NuGetPackages.ps1     # Builds the distributable package
├── CODE_OF_CONDUCT.md                   # Code of conduct
├── CONTRIBUTING.md                      # Contribution guidelines
├── DEVELOPER.md                         # This file
├── ENDIAN.md                            # Endianness documentation (Int32Be, UInt16Be, etc.)
├── EXTENSIONS.md                        # Detailed documentation on the Extensions class methods
├── PRIVACY.md                           # Privacy statement (no telemetry, no data collection)
├── README.md                            # User documentation
├── RESULT.md                            # Result documentation -- used extensively throughout Stardust.Utilities
└── SECURITY.md                          # Security documentation and how to report vulnerabilities
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

# Build using version from Directory.Build.props (runs tests, publishes to local feed)
.\Build-Combined-NuGetPackages.ps1

# Skip tests for faster iteration
.\Build-Combined-NuGetPackages.ps1 -SkipTests

# Override version explicitly
.\Build-Combined-NuGetPackages.ps1 0.9.9

# Use Debug configuration
.\Build-Combined-NuGetPackages.ps1 0.9.9 -Configuration Debug
```

**Parameters:**

| Parameter | Required | Description |
|-----------|----------|-------------|
| `<version>` | No | Version number (e.g., `0.9.9`, `1.0.0-beta1`). Defaults to the value in `Directory.Build.props`. |
| `-SkipTests` | No | Skip running unit tests |
| `-Configuration` | No | `Debug` or `Release` (default: Release) |
| `-Help` | No | Show help message |

**What it does:**
1. Cleans previous build artifacts
2. Lints `README.md` image URLs (all must be absolute `http(s)://`)
3. Lints `README.md` for stale hardcoded version references that don't match `$Version`
4. Builds the generator package
5. Builds the main library
6. Runs unit tests (unless `-SkipTests`)
7. Pins relative `.md` links in a packed copy of `README.md` to `https://github.com/dhadner/Stardust.Utilities/blob/v{Version}/<file>.md`; the on-disk source is restored after pack
8. Creates NuGet packages in `./nupkg/`
9. Copies packages to `~/.nuget/local-packages/` and clears the NuGet cache for `stardust.utilities` and `stardust.generators`

**Output locations:**
- `./nupkg/` - Package files
- `~/.nuget/local-packages/` - Local NuGet feed (packages appear in NuGet Package Manager)

> **Local feed setup for contributors:** The build script copies packages to `~/.nuget/local-packages/`.
> If this is your first time building, you may need to register the folder as a NuGet source:
> ```powershell
> dotnet nuget add source "$env:USERPROFILE\.nuget\local-packages" --name local-packages
> ```
> You can verify it was added with `dotnet nuget list source`. The local feed appears in
> Visual Studio's NuGet Package Manager under **Package source > local-packages**.

#### Build-Generator-NuGetPackage.ps1

Builds only the `Stardust.Generators.x.y.z.nupkg` standalone generator package **for local development**.

> **Note:** This package is not published to NuGet.org. It exists only to support debugging scenarios where
> `Stardust.Utilities` is referenced via `ProjectReference`.

**Use this when:**
- Debugging `Stardust.Utilities` via ProjectReference in Visual Studio
- The embedded analyzer doesn't load with ProjectReference, so this standalone package provides the generator

```powershell
# Specify a version
.\Build-Generator-NuGetPackage.ps1 -Version "0.9.7"
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

> **Enum casting rule:** When generating code that applies bitwise operators (`&`, `|`, `^`)
> to a `value` parameter whose type comes from `field.PropertyType`, always cast `value` to
> the storage type first (e.g., `({info.StorageType})value`). C# does not allow `enum & int`
> directly. The setter and shift != 0 paths already follow this pattern; the shift == 0 path
> in `GenerateWithBitFieldMethod` was fixed in v0.9.5 to match. See the test
> `GeneratedBitFields_WithEnumAtBitZero` for the regression test.

**Step 2:** Rebuild the package:

```powershell
# Rebuild both packages (recommended)
.\Build-Combined-NuGetPackages.ps1 -SkipTests
```

**Step 3:**
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

## Building for Big-Endian Platforms

### The BigEndian MSBuild Property

The endian types (`UInt32Be`, `Int128Le`, `UInt256Be`, etc.) use `#if BIG_ENDIAN` compile-time
guards to select optimized code paths for the host platform. On little-endian hardware (x86/x64/ARM64),
the default build is correct and optimal. When targeting a **big-endian platform**, you must pass
`-p:BigEndian=true` to enable the BE-optimized paths:

```sh
# Build the library for a big-endian target
dotnet build -p:BigEndian=true

# Run unit tests on a big-endian machine
dotnet test Test/Stardust.Utilities.Tests.csproj -p:BigEndian=true -f net8.0 \
  --filter "Category!=Performance"

# Build the NuGet package for a big-endian target
./Build-Combined-NuGetPackages.ps1 -SkipTests
# (set BigEndian=true as an env var, or pass -p:BigEndian=true to individual dotnet commands)
```

This is wired in `Directory.Build.props`:

```xml
<PropertyGroup Condition="'$(BigEndian)' == 'true'">
  <DefineConstants>$(DefineConstants);BIG_ENDIAN</DefineConstants>
</PropertyGroup>
```

### Why Not Auto-Detect from Architecture Name?

The `BigEndian` property is set **explicitly by the developer** rather than inferred from the
`RuntimeIdentifier` or CPU architecture name for three reasons:

1. **Bi-endian architectures.** PowerPC and MIPS can run in either byte order depending on the
   OS configuration (`ppc64le` vs `ppc64be`, `mipsel` vs `mips`). The RID alone does not
   determine endianness; the OS configuration does.

2. **Cross-compilation.** When cross-compiling on an x86 host for an s390x target, the RID is
   `linux-s390x` but the host is little-endian. Inferring `BIG_ENDIAN` from the RID would
   immediately fail the `EndiannessCheck` module initializer, which validates at **runtime**
   against `BitConverter.IsLittleEndian`. The flag must reflect the **target** endianness, and
   only the developer (or CI script) knows that with certainty.

3. **Future-proofing.** Hardcoding architecture names creates a maintenance burden: every new
   BE architecture must be added to the MSBuild condition. An explicit opt-in is more robust.

### Runtime Validation

`EndiannessCheck.cs` contains a `[ModuleInitializer]` that runs before any library code and
validates that the compile-time `BIG_ENDIAN` flag matches `BitConverter.IsLittleEndian`:

```csharp
[ModuleInitializer]
internal static void EnsureCorrectEndianness()
{
#if BIG_ENDIAN
    if (BitConverter.IsLittleEndian)
        throw new PlatformNotSupportedException(
            "This build was compiled with BIG_ENDIAN but is running on a little-endian machine. " +
            "Rebuild without -p:BigEndian=true.");
#else
    if (!BitConverter.IsLittleEndian)
        throw new PlatformNotSupportedException(
            "This build was compiled for little-endian but is running on a big-endian machine. " +
            "Rebuild with -p:BigEndian=true.");
#endif
}
```

This catches mismatched builds immediately at startup. If you see `PlatformNotSupportedException`
on launch, check whether `BigEndian=true` was (or was not) passed to the build.

### Testing on s390x via QEMU User-Mode

The canonical procedure for verifying BE correctness on IBM Z (s390x) without physical hardware
uses WSL2 + QEMU user-mode emulation + a debootstrapped Ubuntu s390x chroot + IBM's community
.NET 8 SDK for s390x.

**One-time setup (from WSL2 Ubuntu):**

```bash
# 1. Install QEMU user-mode and debootstrap
sudo apt-get update
sudo apt-get install -y qemu-user-static binfmt-support debootstrap

# 2. Register QEMU binfmt handlers so s390x ELF binaries run transparently
sudo update-binfmts --enable qemu-s390x

# 3. Create an s390x Ubuntu 22.04 chroot (jammy)
sudo debootstrap --arch=s390x --foreign jammy /opt/s390x-chroot \
    http://ports.ubuntu.com/ubuntu-ports

# 4. Copy the QEMU static binary into the chroot so it can run inside
sudo cp /usr/bin/qemu-s390x-static /opt/s390x-chroot/usr/bin/

# 5. Complete the second-stage debootstrap inside the chroot
sudo chroot /opt/s390x-chroot /debootstrap/debootstrap --second-stage

# 6. Install IBM .NET 8 SDK for s390x inside the chroot.
#    IBM's community .NET distributions for s390x are available from:
#    https://github.com/ibmruntimes/dotnet-s390x (tarballs) or via the
#    IBM package feed. Install the SDK tarball to /usr/local/dotnet-s390x:
sudo chroot /opt/s390x-chroot bash -c "
  apt-get install -y curl libicu-dev libssl-dev
  mkdir -p /usr/local/dotnet
  curl -L <IBM_DOTNET8_S390X_TARBALL_URL> | tar -xz -C /usr/local/dotnet
  ln -sf /usr/local/dotnet/dotnet /usr/local/bin/dotnet
"
```

**Building and running tests:**

```bash
# Mount the source tree into the chroot
sudo mount --bind /path/to/Stardust.Utilities /opt/s390x-chroot/mnt/stardust

# Enter the chroot and run tests with BigEndian=true, net8.0 only
sudo chroot /opt/s390x-chroot bash -c "
  cd /mnt/stardust
  dotnet test Test/Stardust.Utilities.Tests.csproj \
    -p:BigEndian=true -f net8.0 \
    --filter 'Category!=Performance' \
    -c Release
"
```

**What to check:**
- All tests pass (none skipped or failed due to BE-specific assumptions).
- The `EndiannessCheck` module initializer does **not** throw (confirms `BigEndian=true` matched
  the actual s390x runtime).
- `BitConverter.IsLittleEndian` is `false` on s390x (verifiable with a trivial test or by
  inspecting the test runner output).

**Performance note:** QEMU user-mode instruction emulation runs at roughly 1/10–1/100th of
native speed on the x86 host. BenchmarkDotNet numbers measured inside the chroot reflect
emulation overhead, not real s390x hardware performance. Run only unit tests for correctness
verification; defer performance measurement to bare-metal s390x hardware.

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

The `[BitField]` attribute can use Rust-style inclusive bit ranges:

```csharp
[BitFields(StorageType.Byte)]
public partial struct RegisterA
{
    // 3-bit field at bits 0, 1, 2 (like Rust's 0..=2)
    [BitField(0, End = 2)] public partial byte Sound { get; set; }
    
    // Single bit at position 3
    [BitField(3, End = 3)] public partial byte Flag { get; set; }
    
    // 4-bit field at bits 4, 5, 6, 7 (like Rust's 4..=7)
    [BitField(4, End = 7)] public partial byte Mode { get; set; }
}
```

Width is calculated as `(end - start + 1)`.

The `[BitField]` attribute can also use start and width specifiers:

```csharp
[BitFields(typeof(byte))]
public partial struct RegisterA
{
    // 3-bit field at bits 0, 1, 2
    [BitField(0, Width = 3)] public partial byte Sound { get; set; }
    
    // Single bit at position 3. Could also use [BitFlag(3)] here for an automatic boolean.
    [BitField(3, Width = 1)] public partial byte Flag { get; set; }
    
    // 4-bit field at bits 4, 5, 6, 7
    [BitField(4, Width = 4)] public partial byte Mode { get; set; }
}
```


### Nested Struct Support

BitFields structs can be nested inside classes. The containing types must be marked `partial`:

```csharp
public partial class HardwareController
{
    [BitFields(typeof(byte))]
    public partial struct StatusRegister
    {
        [BitFlag(0)] public partial bool Ready { get; set; }
        [BitField(4, End = 7)] public partial byte Mode { get; set; }  // bits 4..=7 (4 bits)
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

## Demo Apps

### DemoWeb (Blazor WebAssembly)

The `Demo/BitFields.DemoWeb` project is a Blazor WebAssembly app that showcases
BitFields, protocol headers, PE viewers, and the RFC diagram generator.

**Build requirements:**

- The `wasm-tools` workload is required because the project customizes the
  native WASM build. Install it once:
  ```powershell
  dotnet workload install wasm-tools
  ```
- After installing the workload, **restart Visual Studio** so its build host
  picks up the new workload.

**WASM compatibility settings (csproj):**

The project disables two WASM features to maximize browser compatibility:

| Property | Default | DemoWeb | Why |
|----------|---------|---------|-----|
| `WasmEnableSIMD` | `true` | `false` | V8 JIT-less mode crashes on SIMD instructions |
| `WasmEnableExceptionHandling` | `true` | `false` | V8 JIT-less mode crashes on native WASM EH |

These settings cause `dotnet.native.wasm` to be relinked without SIMD and with
JavaScript-based exception handling. The result is a slightly slower but
universally compatible binary. This is appropriate for a demo app.

**Edge Enhanced Security Mode (Strict):**

Edge's Enhanced Security Mode (Strict) disables WebAssembly JIT compilation for
large modules. Even with SIMD and native EH disabled, the 14 MB
`dotnet.native.wasm` crashes the renderer with `STATUS_ILLEGAL_INSTRUCTION`.
Small WASM modules (under ~1 MB) compile fine, so no JavaScript probe can detect
the problem ahead of time.

The `index.html` boot script uses a three-tier strategy to handle this:

**Non-Edge browsers** (Chrome, Firefox, Safari): auto-load Blazor immediately.
These browsers are not affected by the JIT-less issue.

**Edge, first visit**: the script detects Edge via User-Agent (`/Edg\//`) and
shows a welcome page with a "Load Interactive Demo" button, a video walkthrough
link, and README screenshots link. This prevents the renderer crash from being
the first thing a visitor sees. If the user clicks "Load Demo":

- **Balanced mode** (default): Blazor loads successfully. `Program.cs` sets
  `localStorage('blazorBoot') = 'success'`. All subsequent visits auto-load.
- **Strict mode**: the renderer crashes with `STATUS_ILLEGAL_INSTRUCTION`.
  The `localStorage` flag stays at `'loading'` (the browser process manages
  localStorage, so it survives renderer crashes). The next visit shows a
  compatibility panel with the Edge settings fix and fallback content links.

**Edge, return visit after crash** (`blazorBoot === 'loading'`): shows the
compatibility panel immediately (no crash). The primary fix instructs the user
to add the site to the exception list at
`edge://settings/privacy/security/secureModeSites`, with video/screenshots as
a fallback. A "Try again" button clears the flag and reloads.

**Any browser, return visit after success** (`blazorBoot === 'success'`):
auto-loads Blazor regardless of User-Agent.

**GitHub Pages deployment:**

The `deploy-demo.yml` workflow installs `wasm-tools` and publishes with the
same WASM settings as local builds. Both environments produce identical binaries.

### DemoApp (WPF)

The `Demo/BitFields.DemoApp` project is a WPF desktop app (Windows only). It
shares model and utility files with DemoWeb via `<Compile Include>` links.
No special build requirements beyond the standard .NET SDK.

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

See [CHANGELOG.md](CHANGELOG.md) for detailed version history and release notes.

## API Simplification Trade Study (v0.3.0)

### Background

Prior to v0.3.0, BitFields supported two patterns:
1. **User-declared Value field**: `[BitFields]` with `public byte Value;`
2. **Generator-created Value field**: `[BitFields(typeof(byte))]` with private Value

Performance testing showed no benefit to the user-declared pattern.  That is, directly
accessing the Value field from user code vs. implicit conversions had negligible effect 
on speed.

```
// Example structs for performance comparison

// User-declared public value determined type

[BitFields]               
public partial struct RegisterWithVisibleValue
{
    public byte Value; 

    [BitField(0, End = 3)] public partial byte Field1 { get; set; }
    [BitField(4, End = 7)] public partial byte Field2 { get; set; }
}

// Attribute determines type, generator creates private Value field

[BitFields(typeof(byte))] 
public partial struct RegisterWithConversion
{
    [BitField(0, End = 3)] public partial byte Field1 { get; set; }
    [BitField(4, End = 7)] public partial byte Field2 { get; set; }
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

## Releasing a Version

### Release Checklist

The tag **must be pushed to GitHub before the `.nupkg` is pushed to nuget.org**, because the
packed README links are rewritten at pack time to absolute URLs of the form
`https://github.com/dhadner/Stardust.Utilities/blob/v{Version}/<file>.md`. If the package
lands on nuget.org before the tag exists on GitHub, every link in the rendered README will
404 until the tag is pushed.

1. **Bump** `Version` in `Directory.Build.props` (single source of truth; demo app
   `PackageReference` versions use `$(Version)` automatically).
2. **CHANGELOG.md** -- add a new section for the version with the current date and a
   summary of changes.
3. **PackageReleaseNotes** in `Stardust.Utilities.csproj` -- add a version-specific paragraph
   at the top (keep prior paragraphs so nuget.org shows release history).
4. **README.md** -- no version string should need changing: the install snippet is
   versionless and the "What's New" section just links to `CHANGELOG.md`. If you added a
   new versioned snippet, update it; the build will fail on step 3 (version lint) if any
   literal `Version="X.Y.Z"` or `**vX.Y.Z**` in `README.md` doesn't match `$Version`. See
   [Avoiding Stale Version References in README](#avoiding-stale-version-references-in-readme)
   for the full policy.
5. **Build and test:** `.\Build-Combined-NuGetPackages.ps1` -- step 3 catches stale
   README versions, and step 7 pins README `.md` links to `v{Version}` at pack time.
6. **Verify** the packed README in NuGet Package Explorer (drag the `.nupkg` onto it; inspect
   the Readme tab -- images should render and every `.md` link in the feature matrix should
   point at `blob/v{Version}/<file>.md`).
7. **Commit and push** all changes to `main`.
8. **Create and push the Git tag**, with the `v` prefix:
   ```powershell
   git tag v0.9.12
   git push origin v0.9.12
   ```
   **Do not skip or postpone this step.** The packed README in the `.nupkg` already
   references this tag; nuget.org will try to resolve the links as soon as the package is
   published.
9. **Publish to nuget.org** with the command shown at the end of the build script output:
   ```powershell
   dotnet nuget push .\nupkg\Stardust.Utilities.0.9.12.nupkg -k YOUR_API_KEY -s https://api.nuget.org/v3/index.json
   ```
10. **Smoke-test nuget.org** -- open the package page, confirm screenshots render, and click
    at least one `.md` link in the "What's In The Box" feature matrix; it must resolve to
    `blob/v{Version}/<file>.md`, not 404.
11. **For re-releases** (fixing README / docs on an already-published version): unlist and
    deprecate the prior version on nuget.org so installs fall through to the new version.

### Tag Naming Convention

All release tags use the `v` prefix (e.g., `v0.9.12`, `v1.0.0`). The initial `0.9.2` tag
predates this convention. New releases must use the `v` prefix for consistency.

### Avoiding Stale Version References in README

Hardcoded version strings in `README.md` drifted twice in recent releases (v0.9.10 and
v0.9.11 both shipped with `Version="0.9.9"` in the install snippet and a `**v0.9.9**`
"What's New" banner). The policy going forward is *"don't hardcode versions that don't
need to be hardcoded, and lint the ones that do"*:

**1. The install snippet does not name a version.**

```xml
<PackageReference Include="Stardust.Utilities" />
```

NuGet resolves this to the latest stable version on restore, which is what most consumers
want. The shields.io badge at the top of the README (`https://img.shields.io/nuget/v/Stardust.Utilities.svg`)
shows what "latest" is today -- updated by nuget.org automatically, no maintenance
required. If a consumer needs a reproducible build pinned to a specific version, they add
`Version="x.y.z"` themselves.

**2. The "What's New" section points at `CHANGELOG.md` instead of summarizing inline.**

The versioned `**vX.Y.Z** -- <prose>` blurb is gone. `CHANGELOG.md` already covers every
release with dated entries; duplicating that in the README just creates a second place
that can drift. On nuget.org, `PackageReleaseNotes` in the csproj plays the same role on
the **Release Notes** tab.

**3. `Build-Combined-NuGetPackages.ps1` step 3 lints for any re-introduction.**

The build script scans `README.md` for two patterns and fails the build if either finds
a literal version that does not match `$Version` from `Directory.Build.props`:

- `Stardust\.Utilities"\s+Version="([0-9.]+(?:-[A-Za-z0-9.-]+)?)"` -- install-snippet version
- `(?m)^\s*\*\*v([0-9.]+(?:-[A-Za-z0-9.-]+)?)\*\*` -- bolded version banner at the start of a line

Placeholder strings like `Version="x.y.z"` in prose are explicitly allowed (the regexes
require all-digit versions with periods), so documentation can still show the syntax
without triggering the lint.

**When a new spot starts carrying a literal version,** add its pattern to the same lint
rather than relying on the author to remember to bump it. The regex additions live in
`Build-Combined-NuGetPackages.ps1` under `Step 3: Lint README.md for stale version
references`.
