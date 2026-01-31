# Stardust.Utilities Developer Guide

This guide explains how to modify the source generators and update consuming projects.

## Project Structure

```
Stardust.Utilities/
??? Stardust.Utilities.csproj    # Main library (types, attributes)
??? Generators/
?   ??? Stardust.Generators.csproj   # Source generator project
?   ??? BitFieldsGenerator.cs        # [BitFields] generator
?   ??? EnhancedEnumGenerator.cs     # [EnhancedEnum] generator
??? Test/
?   ??? Stardust.Utilities.Tests.csproj
??? nupkg/                       # Local NuGet packages
??? README.md
```

## What Requires What Workflow?

| What You Changed | How to See Changes |
|------------------|-------------------|
| **Stardust.Utilities library** (types, attributes, BitField, etc.) | Just rebuild in Visual Studio |
| **Stardust.Generators** (source generator code) | Run `update-generator.ps1` |

**Why?** Projects like MacSE.Tests use:
- `ProjectReference` for Stardust.Utilities ? changes are immediate
- `PackageReference` for Stardust.Generators ? requires version bump + cache clear

## Updating the Source Generator

### Step 1: Make Your Changes

Edit the generator files in `Generators/`:
- `BitFieldsGenerator.cs` - handles `[BitFields]` structs
- `EnhancedEnumGenerator.cs` - handles `[EnhancedEnum]` structs

### Step 2: Update the Version Number

**Important:** You must increment the version in **both** `.csproj` files:

```xml
<!-- Stardust.Utilities/Generators/Stardust.Generators.csproj -->
<Version>0.2.1</Version>

<!-- Stardust.Utilities/Stardust.Utilities.csproj -->
<Version>0.2.1</Version>
```

Also update consuming projects' `PackageReference`:

```xml
<!-- In each project that uses the generator -->
<PackageReference Include="Stardust.Generators" Version="0.2.1" />
```

### Step 3: Rebuild the NuGet Package

```powershell
# From the solution root directory
dotnet pack "Stardust.Utilities\Generators\Stardust.Generators.csproj" -c Release -o "Stardust.Utilities\nupkg"
```

### Step 4: Clear NuGet Cache (Critical!)

NuGet caches packages aggressively. You **must** clear the cache to pick up the new version:

```powershell
# Clear just this package from the cache
dotnet nuget locals all --clear

# Or more targeted (clears only global-packages cache)
dotnet nuget locals global-packages --clear
```

### Step 5: Restore and Rebuild

```powershell
# Restore packages (will fetch new version from local nupkg folder)
dotnet restore

# Rebuild solution
dotnet build
```

## Quick Update Script

Here's a PowerShell script to automate the update process:

```powershell
# update-generator.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$NewVersion
)

Write-Host "Updating Stardust.Generators to version $NewVersion" -ForegroundColor Cyan

# Update version in Generators project
$genCsproj = "Stardust.Utilities\Generators\Stardust.Generators.csproj"
(Get-Content $genCsproj) -replace '<Version>.*</Version>', "<Version>$NewVersion</Version>" | Set-Content $genCsproj

# Update version in main project
$mainCsproj = "Stardust.Utilities\Stardust.Utilities.csproj"
(Get-Content $mainCsproj) -replace '<Version>.*</Version>', "<Version>$NewVersion</Version>" | Set-Content $mainCsproj

# Update package references in test projects
$testProjects = @(
    "Stardust.Utilities\Test\Stardust.Utilities.Tests.csproj",
    "MacSE.Tests\MacSE.Tests.csproj"
)

foreach ($proj in $testProjects) {
    if (Test-Path $proj) {
        (Get-Content $proj) -replace 'Include="Stardust.Generators" Version="[^"]*"', "Include=`"Stardust.Generators`" Version=`"$NewVersion`"" | Set-Content $proj
        Write-Host "  Updated $proj" -ForegroundColor Green
    }
}

# Pack the new version
Write-Host "Packing NuGet package..." -ForegroundColor Cyan
dotnet pack $genCsproj -c Release -o "Stardust.Utilities\nupkg"

# Clear NuGet cache
Write-Host "Clearing NuGet cache..." -ForegroundColor Cyan
dotnet nuget locals global-packages --clear

# Restore
Write-Host "Restoring packages..." -ForegroundColor Cyan
dotnet restore

Write-Host "Done! Run 'dotnet build' to verify." -ForegroundColor Green
```

**Usage:**
```powershell
.\update-generator.ps1 -NewVersion "0.2.1"
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
   dotnet build "Stardust.Utilities\Generators\Stardust.Generators.csproj"
   ```

### Viewing generated code

To see what the generator produces:

1. **Visual Studio:** Project ? Dependencies ? Analyzers ? Stardust.Generators
2. **File System:** `obj\Debug\net10.0\generated\Stardust.Generators\`

### Debugging the generator

1. Add `System.Diagnostics.Debugger.Launch()` at the start of the generator's `Execute` method
2. Build the consuming project
3. A debugger prompt will appear - attach Visual Studio

## Version History

| Version | Changes |
|---------|---------|
| 0.2.0   | Initial release with BitFields and EnhancedEnum generators |

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
