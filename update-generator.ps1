# update-generator.ps1
# Updates the Stardust.Utilities and Stardust.Generators version and rebuilds the NuGet packages.
# This script is standalone and does not depend on any parent solution.
#
# Usage: .\update-generator.ps1 -NewVersion "0.5.3"
# Can be run from the Stardust.Utilities directory or any subdirectory.

param(
    [Parameter(Mandatory=$true)]
    [string]$NewVersion,
    
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"

# Find the Stardust.Utilities root (directory containing Stardust.Utilities.csproj)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Verify we found the right directory
if (-not (Test-Path (Join-Path $scriptDir "Stardust.Utilities.csproj"))) {
    Write-Host "Error: Could not find Stardust.Utilities.csproj in $scriptDir" -ForegroundColor Red
    exit 1
}

Push-Location $scriptDir

try {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " Updating Stardust.Utilities to $NewVersion" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " (Working from: $scriptDir)" -ForegroundColor DarkGray
    Write-Host ""

    # Step 1: Update version in Generators project
    $genCsproj = "Generators\Stardust.Generators.csproj"
    Write-Host "[1/5] Updating $genCsproj" -ForegroundColor Yellow
    if (Test-Path $genCsproj) {
        (Get-Content $genCsproj) -replace '<Version>.*</Version>', "<Version>$NewVersion</Version>" | Set-Content $genCsproj
        Write-Host "      Done" -ForegroundColor Green
    } else {
        Write-Host "      ERROR: File not found!" -ForegroundColor Red
        exit 1
    }

    # Step 2: Update version in main project
    $mainCsproj = "Stardust.Utilities.csproj"
    Write-Host "[2/5] Updating $mainCsproj" -ForegroundColor Yellow
    if (Test-Path $mainCsproj) {
        (Get-Content $mainCsproj) -replace '<Version>.*</Version>', "<Version>$NewVersion</Version>" | Set-Content $mainCsproj
        Write-Host "      Done" -ForegroundColor Green
    } else {
        Write-Host "      ERROR: File not found!" -ForegroundColor Red
        exit 1
    }

    # Step 3: Build the Generator NuGet package
    Write-Host "[3/5] Building Stardust.Generators NuGet package" -ForegroundColor Yellow
    $packResult = dotnet pack $genCsproj -c Release -o "nupkg" 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "      FAILED!" -ForegroundColor Red
        Write-Host $packResult
        exit 1
    }
    Write-Host "      Created Stardust.Generators.$NewVersion.nupkg" -ForegroundColor Green

    # Step 4: Build the main Stardust.Utilities NuGet package (uses Build-NuGetPackage.ps1)
    Write-Host "[4/5] Building Stardust.Utilities NuGet package" -ForegroundColor Yellow
    $buildArgs = @("-Configuration", "Release")
    if ($SkipTests) {
        $buildArgs += "-SkipTests"
    }
    & "$scriptDir\Build-NuGetPackage.ps1" @buildArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Host "      FAILED!" -ForegroundColor Red
        exit 1
    }
    Write-Host "      Done" -ForegroundColor Green

    # Step 5: Summary
    Write-Host "[5/5] Verifying packages" -ForegroundColor Yellow
    $packages = Get-ChildItem -Path "nupkg" -Filter "*$NewVersion.nupkg"
    foreach ($pkg in $packages) {
        Write-Host "      $($pkg.Name)" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " Success! Version updated to $NewVersion" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Packages created in: $scriptDir\nupkg" -ForegroundColor White
    Write-Host ""
    Write-Host "To publish to NuGet.org:" -ForegroundColor Gray
    Write-Host "  dotnet nuget push `"nupkg\Stardust.Utilities.$NewVersion.nupkg`" -k YOUR_API_KEY -s https://api.nuget.org/v3/index.json" -ForegroundColor Gray
    Write-Host "  dotnet nuget push `"nupkg\Stardust.Generators.$NewVersion.nupkg`" -k YOUR_API_KEY -s https://api.nuget.org/v3/index.json" -ForegroundColor Gray
    Write-Host ""
}
finally {
    Pop-Location
}
