# Stardust.Utilities Developer Guide

This guide explains how to modify the source generators and update consuming projects.

## Project Structure

```
Stardust.Utilities/
??? Stardust.Utilities.csproj            # Main library (types, attributes)
??? Generators/
?   ??? Stardust.Generators.csproj       # Source generator project
?   ??? BitFieldsGenerator.cs            # [BitFields] generator
??? Test/
?   ??? Stardust.Utilities.Tests.csproj
??? build/
?   ??? Stardust.Utilities.props         # Auto-enables IntelliSense for consumers
?   ??? Stardust.Utilities.targets       # Auto-excludes generated files from compilation
??? nupkg/                               # Local NuGet packages output
??? Build-Generator-NuGetPackage.ps1     # Builds generator package only
??? Build-Combined-NuGetPackages.ps1     # Builds both packages
??? README.md                            # User documentation
??? DEVELOPER.md                         # This file
```

## NuGet Package Contents

The `Stardust.Utilities` NuGet package includes:

| Path | Description |
|------|-------------|
| `lib/net10.0/Stardust.Utilities.dll` | Main library with attributes and types |
| `analyzers/dotnet/cs/Stardust.Generators.dll` | Source generator (runs at compile time) |
| `build/Stardust.Utilities.props` | Auto-enables `EmitCompilerGeneratedFiles` for IntelliSense |
| `build/Stardust.Utilities.targets` | Auto-excludes generated files from duplicate compilation |
| `README.md` | Package documentation |

## What Requires What Workflow?

| What You Changed | How to See Changes |
|------------------|-------------------|
| **Stardust.Utilities library** (types, attributes) | Just rebuild in Visual Studio |
| **Stardust.Generators** (source generator code) | Run `Build-Generator-NuGetPackage.ps1` or `Build-Combined-NuGetPackages.ps1` |
| **build/*.props or build/*.targets** | Run `Build-Combined-NuGetPackages.ps1` |

## Build Scripts

### Build-Generator-NuGetPackage.ps1

Builds **only** the `Stardust.Generators.x.y.z.nupkg` standalone generator package.

**Use this when:**
- Debugging `Stardust.Utilities` via ProjectReference in Visual Studio
- But still need the generator as a NuGet package for code generation

```powershell
# Basic usage (uses version from .csproj)
.\Build-Generator-NuGetPackage.ps1

# Specify a version
.\Build-Generator-NuGetPackage.ps1 -Version "0.6.0"

# Update version in .csproj and build
.\Build-Generator-NuGetPackage.ps1 -Version "0.6.0" -UpdateVersion
```

### Build-Combined-NuGetPackages.ps1

Builds **both** NuGet packages:
- `Stardust.Generators.x.y.z.nupkg` - Standalone generator (for projects using ProjectReference to Stardust.Utilities)
- `Stardust.Utilities.x.y.z.nupkg` - Combined package (includes generator as embedded analyzer + utility types)

This script calls `Build-Generator-NuGetPackage.ps1` internally for the generator package.

```powershell
# Basic usage (runs tests, builds both packages)
.\Build-Combined-NuGetPackages.ps1

# Skip tests for faster builds
.\Build-Combined-NuGetPackages.ps1 -SkipTests

# Specify a version
.\Build-Combined-NuGetPackages.ps1 -Version "0.6.0"

# Update version in BOTH .csproj files and build
.\Build-Combined-NuGetPackages.ps1 -Version "0.6.0" -UpdateVersion

# Publish to local NuGet feed (~/.nuget/local-packages)
.\Build-Combined-NuGetPackages.ps1 -PublishLocal
```

**Output:** Both packages are copied to the `nupkg/` folder.

## Package Reference Scenarios

| Scenario | What to Reference |
|----------|-------------------|
| **Normal usage** (just need BitFields) | `Stardust.Utilities` NuGet package only |
| **Debugging Stardust.Utilities** | ProjectReference to `Stardust.Utilities.csproj` + PackageReference to `Stardust.Generators` |

## Updating the Source Generator

### Step 1: Make Your Changes

Edit the generator in `Generators/BitFieldsGenerator.cs`.

### Step 2: Rebuild the Package

```powershell
# Just rebuild the generator package (for debugging with ProjectReference)
.\Build-Generator-NuGetPackage.ps1 -Version "0.6.1"

# Or rebuild both packages for a full release
.\Build-Combined-NuGetPackages.ps1 -Version "0.6.1" -UpdateVersion -SkipTests
```

### Step 3: Test Locally

If testing with a consuming project (e.g., MacSE):
1. Update the `PackageReference` version in the consuming project
2. Clear NuGet cache: `dotnet nuget locals global-packages --clear`
3. Restore and rebuild

## Features

### Rust-Style Bit Ranges (v0.6.0+)

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

### Nested Struct Support (v0.5.0+)

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

### Automatic IntelliSense (v0.5.2+)

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

| Version | Changes |
|---------|---------|
| 0.6.0   | **Breaking:** Changed `[BitField(shift, width)]` to Rust-style `[BitField(startBit, endBit)]` |
| 0.5.2   | Auto-enable IntelliSense via .props/.targets files |
| 0.5.0   | Nested struct support, improved indentation in generated code |
| 0.3.0   | **Breaking:** Simplified BitFields API, added signed storage types |
| 0.2.0   | Initial release with BitFields generator |

## API Simplification Trade Study (v0.3.0)

### Background

Prior to v0.3.0, BitFields supported two patterns:
1. **User-declared Value field**: `[BitFields]` with `public byte Value;`
2. **Generator-created Value field**: `[BitFields(typeof(byte))]` with private Value

Performance testing showed no benefit to the user-declared pattern.

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

## nuget.config

The solution uses a `nuget.config` file to reference the local package folder:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="local" value="Stardust.Utilities/nupkg" />
  </packageSources>
</configuration>
```

This allows consuming projects to find packages in `Stardust.Utilities/nupkg/` without publishing to nuget.org.
