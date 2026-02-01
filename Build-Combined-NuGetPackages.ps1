# Build-Combined-NuGetPackages.ps1
# Builds BOTH NuGet packages:
#   - Stardust.Generators.x.y.z.nupkg (standalone generator)
#   - Stardust.Utilities.x.y.z.nupkg (combined: generator + utility types)
#
# Usage:
#   .\Build-Combined-NuGetPackages.ps1                                    # Basic build
#   .\Build-Combined-NuGetPackages.ps1 -SkipTests                         # Skip tests
#   .\Build-Combined-NuGetPackages.ps1 -Version "0.6.0"                   # Specify version
#   .\Build-Combined-NuGetPackages.ps1 -Version "0.6.0" -UpdateVersion    # Update .csproj files
#   .\Build-Combined-NuGetPackages.ps1 -PublishLocal                      # Publish to local feed

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [string]$Version = $null,
    
    [switch]$UpdateVersion,
    
    [switch]$SkipTests,
    
    [switch]$PublishLocal
)

$ErrorActionPreference = 'Stop'
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

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
Write-Host "[1/7] Cleaning previous build artifacts..." -ForegroundColor Yellow
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

# Step 2: Optionally update version in both .csproj files
if ($UpdateVersion -and $Version) {
    Write-Host ""
    Write-Host "[2/7] Updating version to $Version..." -ForegroundColor Yellow
    
    # Update Generators project
    (Get-Content $generatorProject) -replace '<Version>.*</Version>', "<Version>$Version</Version>" | Set-Content $generatorProject
    Write-Host "  Updated: $generatorProject" -ForegroundColor Gray
    
    # Update main project
    (Get-Content $mainProject) -replace '<Version>.*</Version>', "<Version>$Version</Version>" | Set-Content $mainProject
    Write-Host "  Updated: $mainProject" -ForegroundColor Gray
    
    Write-Host "  Done." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "[2/7] Using existing versions from .csproj files" -ForegroundColor Yellow
}

# Step 3: Build the Generator NuGet package using the dedicated script
Write-Host ""
Write-Host "[3/7] Building Stardust.Generators package..." -ForegroundColor Yellow

$genBuildArgs = @{
    Configuration = $Configuration
}
if ($Version) {
    $genBuildArgs.Version = $Version
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

# Step 4: Build the main Stardust.Utilities project
Write-Host ""
Write-Host "[4/7] Building Stardust.Utilities..." -ForegroundColor Yellow

dotnet build $mainProject -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Main project build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  Done." -ForegroundColor Green

# Step 5: Run tests (unless skipped)
if (-not $SkipTests) {
    Write-Host ""
    Write-Host "[5/7] Running tests..." -ForegroundColor Yellow
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
    Write-Host "[5/7] Skipping tests (-SkipTests specified)" -ForegroundColor Yellow
}

# Step 6: Pack the Stardust.Utilities NuGet package
Write-Host ""
Write-Host "[6/7] Creating Stardust.Utilities package..." -ForegroundColor Yellow

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
    "-o", $nupkgFolder
)

if ($Version) {
    $packArgs += "-p:Version=$Version"
}

dotnet @packArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Stardust.Utilities pack failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  Done." -ForegroundColor Green

# Find all created packages for this version
if ($Version) {
    $packageFiles = Get-ChildItem -Path $nupkgFolder -Filter "*$Version.nupkg"
} else {
    $packageFiles = Get-ChildItem -Path $nupkgFolder -Filter "*.nupkg" | Sort-Object LastWriteTime -Descending | Select-Object -First 2
}

if ($packageFiles.Count -eq 0) {
    Write-Host "ERROR: No NuGet packages found!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "  Packages created:" -ForegroundColor Green
foreach ($pkg in $packageFiles) {
    Write-Host "    - $($pkg.Name)" -ForegroundColor White
}

# Step 7: Publish to local NuGet feed (optional)
if ($PublishLocal) {
    Write-Host ""
    Write-Host "[7/7] Publishing to local NuGet feed..." -ForegroundColor Yellow
    
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
} else {
    Write-Host ""
    Write-Host "[7/7] Skipping local publish (use -PublishLocal to enable)" -ForegroundColor Yellow
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Packages created in: $nupkgFolder" -ForegroundColor White
foreach ($pkg in $packageFiles) {
    Write-Host "  - $($pkg.Name)" -ForegroundColor White
}
Write-Host ""
Write-Host "To publish to NuGet.org:" -ForegroundColor Gray
foreach ($pkg in $packageFiles) {
    Write-Host "  dotnet nuget push `"$nupkgFolder\$($pkg.Name)`" -k YOUR_API_KEY -s https://api.nuget.org/v3/index.json" -ForegroundColor Gray
}
Write-Host ""
