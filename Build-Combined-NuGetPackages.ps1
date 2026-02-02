# Build-Combined-NuGetPackages.ps1
# Builds BOTH NuGet packages:
#   - Stardust.Generators.x.y.z.nupkg (standalone generator, for local development only)
#   - Stardust.Utilities.x.y.z.nupkg (combined: generator + utility types)
#
# Usage:
#   .\Build-Combined-NuGetPackages.ps1 -Help                              # Show help
#   .\Build-Combined-NuGetPackages.ps1 0.9.0                              # Build version 0.9.0
#   .\Build-Combined-NuGetPackages.ps1 0.9.0 -SkipTests                   # Skip tests

param(
    [Parameter(Position=0)]
    [string]$Version,
    
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [switch]$SkipTests,
    
    [Alias('h', '?')]
    [switch]$Help
)

$ErrorActionPreference = 'Stop'
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Show help if requested
if ($Help) {
    Write-Host ""
    Write-Host "Build-Combined-NuGetPackages.ps1" -ForegroundColor Cyan
    Write-Host "================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Builds the Stardust.Utilities NuGet package (with embedded source generator)."
    Write-Host "Packages are automatically published to the local NuGet feed."
    Write-Host ""
    Write-Host "USAGE:" -ForegroundColor Yellow
    Write-Host "  .\Build-Combined-NuGetPackages.ps1 <version> [options]"
    Write-Host ""
    Write-Host "ARGUMENTS:" -ForegroundColor Yellow
    Write-Host "  <version>            Version number (required, e.g., '0.9.0')"
    Write-Host ""
    Write-Host "OPTIONS:" -ForegroundColor Yellow
    Write-Host "  -SkipTests           Skip running unit tests"
    Write-Host "  -Configuration       Build configuration: Debug or Release (default: Release)"
    Write-Host "  -Help, -h, -?        Show this help message"
    Write-Host ""
    Write-Host "EXAMPLES:" -ForegroundColor Yellow
    Write-Host "  .\Build-Combined-NuGetPackages.ps1 0.9.0"
    Write-Host "  .\Build-Combined-NuGetPackages.ps1 0.9.0 -SkipTests"
    Write-Host "  .\Build-Combined-NuGetPackages.ps1 1.0.0-beta1 -SkipTests"
    Write-Host ""
    Write-Host "OUTPUT:" -ForegroundColor Yellow
    Write-Host "  - Packages saved to: ./nupkg/"
    Write-Host "  - Packages published to: ~/.nuget/local-packages/"
    Write-Host "  - NuGet cache cleared for stardust.utilities and stardust.generators"
    Write-Host ""
    exit 0
}

# Version is required when not showing help
if (-not $Version) {
    Write-Host "ERROR: Version is required." -ForegroundColor Red
    Write-Host "Usage: .\Build-Combined-NuGetPackages.ps1 <version> [options]" -ForegroundColor Yellow
    Write-Host "Example: .\Build-Combined-NuGetPackages.ps1 0.9.0" -ForegroundColor Gray
    Write-Host "Run with -Help for more information." -ForegroundColor Gray
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Stardust Combined NuGet Package Builder" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verify we're in the right directory
$mainProject = "$ScriptDir\Stardust.Utilities.csproj"
$generatorProject = "$ScriptDir\Generators\Stardust.Generators.csproj"

if (-not (Test-Path $mainProject)) {
    Write-Host "ERROR: Main project not found at: $mainProject" -ForegroundColor Red
    exit 1
}

# Step 1: Clean previous build artifacts
Write-Host "[1/6] Cleaning previous build artifacts..." -ForegroundColor Yellow
$foldersToClean = @(
    "$ScriptDir\bin",
    "$ScriptDir\obj",
    "$ScriptDir\Generators\bin",
    "$ScriptDir\Generators\obj",
    "$ScriptDir\Test\bin",
    "$ScriptDir\Test\obj",
    "$ScriptDir\Test\Generated"
)

foreach ($folder in $foldersToClean) {
    if (Test-Path $folder) {
        Remove-Item -Path $folder -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  Cleaned: $folder" -ForegroundColor Gray
    }
}
Write-Host "  Done." -ForegroundColor Green

# Step 2: Build the Generator NuGet package using the dedicated script
Write-Host ""
Write-Host "[2/6] Building Stardust.Generators package..." -ForegroundColor Yellow

$genBuildArgs = @{
    Configuration = $Configuration
    Version = $Version
}

& "$ScriptDir\Build-Generator-NuGetPackage.ps1" @genBuildArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Generator package build failed!" -ForegroundColor Red
    exit 1
}

# Verify generator DLL exists (needed for Stardust.Utilities package)
$generatorDll = "$ScriptDir\Generators\bin\$Configuration\netstandard2.0\Stardust.Generators.dll"
if (-not (Test-Path $generatorDll)) {
    Write-Host "ERROR: Generator DLL not found at: $generatorDll" -ForegroundColor Red
    exit 1
}
Write-Host "  Generator DLL: $generatorDll" -ForegroundColor Gray

# Step 3: Build the main Stardust.Utilities project
Write-Host ""
Write-Host "[3/6] Building Stardust.Utilities..." -ForegroundColor Yellow

dotnet build $mainProject -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Main project build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  Done." -ForegroundColor Green

# Step 4: Run tests (unless skipped)
if (-not $SkipTests) {
    Write-Host ""
    Write-Host "[4/6] Running tests..." -ForegroundColor Yellow
    $testProject = "$ScriptDir\Test\Stardust.Utilities.Tests.csproj"
    
    if (Test-Path $testProject) {
        dotnet test $testProject -c $Configuration --nologo --verbosity minimal
        if ($LASTEXITCODE -ne 0) {
            Write-Host "ERROR: Tests failed!" -ForegroundColor Red
            exit 1
        }
        Write-Host "  All tests passed." -ForegroundColor Green
    } else {
        Write-Host "  Test project not found, skipping." -ForegroundColor Gray
    }
} else {
    Write-Host ""
    Write-Host "[4/6] Skipping tests (-SkipTests specified)" -ForegroundColor Yellow
}

# Step 5: Pack the Stardust.Utilities NuGet package
Write-Host ""
Write-Host "[5/6] Creating Stardust.Utilities package..." -ForegroundColor Yellow

$nupkgFolder = "$ScriptDir\nupkg"
if (-not (Test-Path $nupkgFolder)) {
    New-Item -Path $nupkgFolder -ItemType Directory -Force | Out-Null
}

$packArgs = @(
    "pack",
    $mainProject,
    "-c", $Configuration,
    "--nologo",
    "--no-build",
    "-o", $nupkgFolder,
    "-p:Version=$Version"
)

dotnet @packArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Stardust.Utilities pack failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  Done." -ForegroundColor Green

# Find all created packages for this version
$packageFiles = Get-ChildItem -Path $nupkgFolder -Filter "*$Version.nupkg"

if ($packageFiles.Count -eq 0) {
    Write-Host "ERROR: No NuGet packages found!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "  Packages created:" -ForegroundColor Green
foreach ($pkg in $packageFiles) {
    Write-Host "    - $($pkg.Name)" -ForegroundColor White
}

# Step 6: Publish to local NuGet feed
Write-Host ""
Write-Host "[6/6] Publishing to local NuGet feed..." -ForegroundColor Yellow

# Default local feed location
$localFeed = "$env:USERPROFILE\.nuget\local-packages"
if (-not (Test-Path $localFeed)) {
    New-Item -Path $localFeed -ItemType Directory -Force | Out-Null
}

foreach ($pkg in $packageFiles) {
    Copy-Item -Path $pkg.FullName -Destination $localFeed -Force
    Write-Host "  Published: $($pkg.Name)" -ForegroundColor Green
}

# Clear NuGet cache for these packages to force reload
$cacheDirs = @(
    "$env:USERPROFILE\.nuget\packages\stardust.utilities",
    "$env:USERPROFILE\.nuget\packages\stardust.generators"
)
foreach ($cacheDir in $cacheDirs) {
    if (Test-Path $cacheDir) {
        Remove-Item -Path $cacheDir -Recurse -Force
        Write-Host "  Cleared cache: $cacheDir" -ForegroundColor Gray
    }
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Packages available at:" -ForegroundColor White
Write-Host "  Local:  $nupkgFolder" -ForegroundColor Gray
Write-Host "  Feed:   $localFeed" -ForegroundColor Gray
Write-Host ""
foreach ($pkg in $packageFiles) {
    Write-Host "  - $($pkg.Name)" -ForegroundColor White
}
Write-Host ""
Write-Host "To publish to NuGet.org:" -ForegroundColor Gray
foreach ($pkg in $packageFiles) {
    Write-Host "  dotnet nuget push `"$nupkgFolder\$($pkg.Name)`" -k YOUR_API_KEY -s https://api.nuget.org/v3/index.json" -ForegroundColor Gray
}
Write-Host ""
