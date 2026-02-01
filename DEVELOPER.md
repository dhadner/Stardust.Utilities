# Stardust.Utilities Developer Guide

This guide explains how to modify the source generators and update consuming projects.

## Project Structure

```
Stardust.Utilities/
??? Stardust.Utilities.csproj       # Main library (types, attributes)
??? Generators/
?   ??? Stardust.Generators.csproj  # Source generator project
?   ??? BitFieldsGenerator.cs       # [BitFields] generator
??? Test/
?   ??? Stardust.Utilities.Tests.csproj
??? build/
?   ??? Stardust.Utilities.props    # Auto-enables IntelliSense for consumers
?   ??? Stardust.Utilities.targets  # Auto-excludes generated files from compilation
??? nupkg/                          # Local NuGet packages
??? Build-NuGetPackage.ps1          # Builds the NuGet package
??? update-generator.ps1            # Updates version and rebuilds packages
??? README.md                       # User documentation
??? DEVELOPER.md                    # This file
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
| **Stardust.Generators** (source generator code) | Run `update-generator.ps1 -NewVersion "x.y.z"` |
| **build/*.props or build/*.targets** | Rebuild the NuGet package |

## Build Scripts

### Build-NuGetPackage.ps1

Builds the complete `Stardust.Utilities` NuGet package including the embedded generator.

```powershell
# Basic usage (runs tests)
.\Build-NuGetPackage.ps1 -Configuration Release

# Skip tests for faster builds
.\Build-NuGetPackage.ps1 -Configuration Release -SkipTests

# Specify a version
.\Build-NuGetPackage.ps1 -Configuration Release -Version "0.6.0"

# Publish to local NuGet feed (~/.nuget/local-packages)
.\Build-NuGetPackage.ps1 -Configuration Release -PublishLocal
```

### update-generator.ps1

Updates the version in both projects and rebuilds all NuGet packages.

```powershell
# Update to new version
.\update-generator.ps1 -NewVersion "0.6.0"

# Skip tests for faster builds
.\update-generator.ps1 -NewVersion "0.6.0" -SkipTests
```

This script:
1. Updates `<Version>` in `Generators/Stardust.Generators.csproj`
2. Updates `<Version>` in `Stardust.Utilities.csproj`
3. Builds `Stardust.Generators.*.nupkg`
4. Builds `Stardust.Utilities.*.nupkg`
5. Copies both packages to `nupkg/` folder

## Updating the Source Generator

### Step 1: Make Your Changes

Edit the generator in `Generators/BitFieldsGenerator.cs`.

### Step 2: Run the Update Script

```powershell
.\update-generator.ps1 -NewVersion "0.6.0" -SkipTests
```

### Step 3: Test Locally

If testing with a consuming project (e.g., MacSE):
1. Update the `PackageReference` version in the consuming project
2. Clear NuGet cache: `dotnet nuget locals global-packages --clear`
3. Restore and rebuild

## Features

### Nested Struct Support (v0.5.0+)

BitFields structs can be nested inside classes. The containing types must be marked `partial`:

```csharp
public partial class HardwareController
{
    [BitFields(typeof(byte))]
    public partial struct StatusRegister
    {
        [BitFlag(0)] public partial bool Ready { get; set; }
        [BitField(4, 4)] public partial byte Mode { get; set; }
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
