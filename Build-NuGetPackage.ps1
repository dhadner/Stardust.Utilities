# Build-NuGetPackage.ps1
# Builds the Stardust.Utilities NuGet package including the generator analyzer

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [string]$Version = $null,
    
    [switch]$SkipTests,
    
    [switch]$PublishLocal
)

$ErrorActionPreference = 'Stop'
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Stardust.Utilities NuGet Package Builder" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

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

# Step 2: Build the Generator project first (must be built before main project)
Write-Host ""
Write-Host "[2/6] Building Stardust.Generators..." -ForegroundColor Yellow
$generatorProject = "$ScriptDir\Generators\Stardust.Generators.csproj"

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
Write-Host "  Generator DLL: $generatorDll" -ForegroundColor Gray

# Step 3: Build the main Stardust.Utilities project
Write-Host ""
Write-Host "[3/6] Building Stardust.Utilities..." -ForegroundColor Yellow
$mainProject = "$ScriptDir\Stardust.Utilities.csproj"

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
    
    dotnet test $testProject -c $Configuration --nologo --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Tests failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "  All tests passed." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "[4/6] Skipping tests (--SkipTests specified)" -ForegroundColor Yellow
}

# Step 5: Pack the NuGet package
Write-Host ""
Write-Host "[5/6] Creating NuGet package..." -ForegroundColor Yellow

$packArgs = @(
    "pack",
    $mainProject,
    "-c", $Configuration,
    "--nologo",
    "-o", "$ScriptDir\bin\packages"
)

if ($Version) {
    $packArgs += "-p:Version=$Version"
    Write-Host "  Using version: $Version" -ForegroundColor Gray
}

dotnet @packArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: NuGet pack failed!" -ForegroundColor Red
    exit 1
}

# Find the created package
$packageFiles = Get-ChildItem -Path "$ScriptDir\bin\packages" -Filter "*.nupkg" | Sort-Object LastWriteTime -Descending
if ($packageFiles.Count -eq 0) {
    Write-Host "ERROR: No NuGet package found!" -ForegroundColor Red
    exit 1
}
$packageFile = $packageFiles[0]
Write-Host "  Package created: $($packageFile.Name)" -ForegroundColor Green

# Copy to nupkg folder (standard location for committed packages)
$nupkgFolder = "$ScriptDir\nupkg"
if (-not (Test-Path $nupkgFolder)) {
    New-Item -Path $nupkgFolder -ItemType Directory -Force | Out-Null
}
Copy-Item -Path $packageFile.FullName -Destination $nupkgFolder -Force
Write-Host "  Copied to: $nupkgFolder\$($packageFile.Name)" -ForegroundColor Green

# Step 6: Publish to local NuGet feed (optional)
if ($PublishLocal) {
    Write-Host ""
    Write-Host "[6/6] Publishing to local NuGet feed..." -ForegroundColor Yellow
    
    # Default local feed location
    $localFeed = "$env:USERPROFILE\.nuget\local-packages"
    if (-not (Test-Path $localFeed)) {
        New-Item -Path $localFeed -ItemType Directory -Force | Out-Null
    }
    
    Copy-Item -Path $packageFile.FullName -Destination $localFeed -Force
    Write-Host "  Published to: $localFeed" -ForegroundColor Green
    
    # Clear NuGet cache for this package to force reload
    $cacheDir = "$env:USERPROFILE\.nuget\packages\stardust.utilities"
    if (Test-Path $cacheDir) {
        Remove-Item -Path $cacheDir -Recurse -Force
        Write-Host "  Cleared NuGet cache for Stardust.Utilities" -ForegroundColor Gray
    }
} else {
    Write-Host ""
    Write-Host "[6/6] Skipping local publish (use -PublishLocal to enable)" -ForegroundColor Yellow
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Package: $($packageFile.FullName)" -ForegroundColor White
Write-Host ""
Write-Host "To publish to NuGet.org:" -ForegroundColor Gray
Write-Host "  dotnet nuget push `"$($packageFile.FullName)`" -k YOUR_API_KEY -s https://api.nuget.org/v3/index.json" -ForegroundColor Gray
Write-Host ""
