# Build-Generator-NuGetPackage.ps1
# Builds ONLY the Stardust.Generators NuGet package.
#
# Use this script when:
# - Debugging Stardust.Utilities via ProjectReference in Visual Studio
# - But still need the generator as a NuGet package for code generation
#
# Usage:
#   .\Build-Generator-NuGetPackage.ps1                          # Uses version from .csproj
#   .\Build-Generator-NuGetPackage.ps1 -Version "0.6.0"         # Specify version
#   .\Build-Generator-NuGetPackage.ps1 -Configuration Debug     # Debug build

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [string]$Version = $null,
    
    [switch]$UpdateVersion
)

$ErrorActionPreference = 'Stop'
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Stardust.Generators NuGet Package Builder" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verify we're in the right directory
$generatorProject = "$ScriptDir\Generators\Stardust.Generators.csproj"
if (-not (Test-Path $generatorProject)) {
    Write-Host "ERROR: Generator project not found at: $generatorProject" -ForegroundColor Red
    exit 1
}

# Step 1: Optionally update version in .csproj
if ($UpdateVersion -and $Version) {
    Write-Host "[1/3] Updating version to $Version..." -ForegroundColor Yellow
    (Get-Content $generatorProject) -replace '<Version>.*</Version>', "<Version>$Version</Version>" | Set-Content $generatorProject
    Write-Host "  Done." -ForegroundColor Green
} else {
    Write-Host "[1/3] Using existing version from .csproj" -ForegroundColor Yellow
}

# Step 2: Build the Generator project
Write-Host ""
Write-Host "[2/3] Building Stardust.Generators..." -ForegroundColor Yellow

dotnet build $generatorProject -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Generator build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  Done." -ForegroundColor Green

# Verify generator DLL exists
$generatorDll = "$ScriptDir\Generators\bin\$Configuration\netstandard2.0\Stardust.Generators.dll"
if (-not (Test-Path $generatorDll)) {
    Write-Host "ERROR: Generator DLL not found at: $generatorDll" -ForegroundColor Red
    exit 1
}

# Step 3: Pack the NuGet package
Write-Host ""
Write-Host "[3/3] Creating NuGet package..." -ForegroundColor Yellow

$packArgs = @(
    "pack",
    $generatorProject,
    "-c", $Configuration,
    "--nologo",
    "--no-build",
    "-o", "$ScriptDir\nupkg"
)

if ($Version) {
    $packArgs += "-p:Version=$Version"
}

dotnet @packArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: NuGet pack failed!" -ForegroundColor Red
    exit 1
}

# Find the created package
$packageFiles = Get-ChildItem -Path "$ScriptDir\nupkg" -Filter "Stardust.Generators.*.nupkg" | Sort-Object LastWriteTime -Descending
if ($packageFiles.Count -eq 0) {
    Write-Host "ERROR: No NuGet package found!" -ForegroundColor Red
    exit 1
}
$packageFile = $packageFiles[0]
Write-Host "  Package created: $($packageFile.Name)" -ForegroundColor Green

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Package: $($packageFile.FullName)" -ForegroundColor White
Write-Host ""
Write-Host "To use with a ProjectReference to Stardust.Utilities:" -ForegroundColor Gray
Write-Host "  1. Add ProjectReference to Stardust.Utilities.csproj" -ForegroundColor Gray
Write-Host "  2. Add PackageReference to Stardust.Generators (this package)" -ForegroundColor Gray
Write-Host ""
