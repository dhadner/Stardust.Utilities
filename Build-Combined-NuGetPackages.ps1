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
    Write-Host "  <version>            Version number (default: read from Directory.Build.props)"
    Write-Host ""
    Write-Host "OPTIONS:" -ForegroundColor Yellow
    Write-Host "  -SkipTests           Skip running unit tests"
    Write-Host "  -Configuration       Build configuration: Debug or Release (default: Release)"
    Write-Host "  -Help, -h, -?        Show this help message"
    Write-Host ""
    Write-Host "EXAMPLES:" -ForegroundColor Yellow
    Write-Host "  .\Build-Combined-NuGetPackages.ps1                                # Use version from Directory.Build.props"
    Write-Host "  .\Build-Combined-NuGetPackages.ps1 -SkipTests                     # Skip tests, auto version"
    Write-Host "  .\Build-Combined-NuGetPackages.ps1 0.9.0                          # Override version"
    Write-Host "  .\Build-Combined-NuGetPackages.ps1 0.9.0 -SkipTests               # Override version, skip tests"
    Write-Host "  .\Build-Combined-NuGetPackages.ps1 1.0.0-beta1 -SkipTests"
    Write-Host ""
    Write-Host "OUTPUT:" -ForegroundColor Yellow
    Write-Host "  - Packages saved to: ./nupkg/"
    Write-Host "  - Packages published to: ~/.nuget/local-packages/"
    Write-Host "  - NuGet cache cleared for stardust.utilities and stardust.generators"
    Write-Host "  - Demo app bin/obj cleared to prevent stale builds"
    Write-Host ""
    exit 0
}

# Version is required when not showing help
if (-not $Version) {
    # Read default version from Directory.Build.props
    $propsFile = "$ScriptDir\Directory.Build.props"
    if (Test-Path $propsFile) {
        [xml]$props = Get-Content $propsFile
        $Version = $props.Project.PropertyGroup.Version
    }
    if (-not $Version) {
        Write-Host "ERROR: Version is required (not found in Directory.Build.props)." -ForegroundColor Red
        Write-Host "Usage: .\Build-Combined-NuGetPackages.ps1 <version> [options]" -ForegroundColor Yellow
        Write-Host "Run with -Help for more information." -ForegroundColor Gray
        exit 1
    }
    Write-Host "Using version $Version from Directory.Build.props" -ForegroundColor Gray
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
Write-Host "[1/7] Cleaning previous build artifacts..." -ForegroundColor Yellow
$foldersToClean = @(
    "$ScriptDir\bin",
    "$ScriptDir\obj",
    "$ScriptDir\Generators\bin",
    "$ScriptDir\Generators\obj",
    "$ScriptDir\Test\bin",
    "$ScriptDir\Test\obj"
)

foreach ($folder in $foldersToClean) {
    if (Test-Path $folder) {
        Remove-Item -Path $folder -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  Cleaned: $folder" -ForegroundColor Gray
    }
}
Write-Host "  Done." -ForegroundColor Green

# Step 2: Lint README.md for image references nuget.org cannot render.
# nuget.org's README renderer does not resolve relative image paths against
# packed files -- it only loads images from an allow-listed set of domains
# (raw.githubusercontent.com, img.shields.io, camo.githubusercontent.com, etc.).
# We require every Markdown image reference to use an absolute http(s):// URL,
# which catches the v0.9.9-class regression before a package is ever uploaded.
Write-Host ""
Write-Host "[2/7] Linting README.md image URLs..." -ForegroundColor Yellow

$readmePath = "$ScriptDir\README.md"
if (-not (Test-Path $readmePath)) {
    Write-Host "ERROR: README.md not found at: $readmePath" -ForegroundColor Red
    exit 1
}

$readmeText = Get-Content $readmePath -Raw
# Markdown image syntax: ![alt](url)  --  leading ! distinguishes images from hyperlinks.
$imageMatches = [regex]::Matches($readmeText, '!\[[^\]]*\]\(([^)\s]+)')
$violations = @()
foreach ($m in $imageMatches) {
    $url = $m.Groups[1].Value
    if ($url -notmatch '^https?://') {
        $violations += $url
    }
}

if ($violations.Count -gt 0) {
    Write-Host "ERROR: README.md contains image references that nuget.org cannot render:" -ForegroundColor Red
    foreach ($v in $violations) {
        Write-Host "  - $v" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "  nuget.org only loads images from absolute http(s):// URLs on its trusted" -ForegroundColor Yellow
    Write-Host "  domain allowlist (raw.githubusercontent.com is the usual choice). Relative" -ForegroundColor Yellow
    Write-Host "  paths render on github.com but appear broken on nuget.org." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  Example fix:" -ForegroundColor Yellow
    Write-Host "    ![alt](Graphics/foo.png)" -ForegroundColor Gray
    Write-Host "  becomes:" -ForegroundColor Yellow
    Write-Host "    ![alt](https://raw.githubusercontent.com/dhadner/Stardust.Utilities/main/Graphics/foo.png)" -ForegroundColor Gray
    exit 1
}
Write-Host "  $($imageMatches.Count) image reference(s) checked, all absolute." -ForegroundColor Green

# Step 3: Build the Generator NuGet package using the dedicated script
Write-Host ""
Write-Host "[3/7] Building Stardust.Generators package..." -ForegroundColor Yellow

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

# Step 7: Publish to local NuGet feed
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

# Clear bin/obj of consuming projects so they pick up the fresh package on next build
$consumerDirs = @(
    "$ScriptDir\Demo\BitFields.DemoApp\bin",
    "$ScriptDir\Demo\BitFields.DemoApp\obj",
    "$ScriptDir\Demo\BitFields.DemoWeb\bin",
    "$ScriptDir\Demo\BitFields.DemoWeb\obj"
)
foreach ($dir in $consumerDirs) {
    if (Test-Path $dir) {
        Remove-Item -Path $dir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  Cleared consumer build: $dir" -ForegroundColor Gray
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
Write-Host "Note: Only publish Stardust.Utilities. The Stardust.Generators package is for local development only." -ForegroundColor Gray
Write-Host ""

# Only show publish command for Stardust.Utilities (never publish Stardust.Generators separately)
$utilitiesPackage = $packageFiles | Where-Object { $_.Name -like "Stardust.Utilities.*" }
if ($utilitiesPackage) {
    Write-Host "To publish to NuGet.org:" -ForegroundColor Yellow
    Write-Host "  dotnet nuget push `"$nupkgFolder\$($utilitiesPackage.Name)`" -k YOUR_API_KEY -s https://api.nuget.org/v3/index.json" -ForegroundColor Gray
    Write-Host ""
}

