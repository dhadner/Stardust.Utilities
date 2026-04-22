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
Write-Host "[1/9] Cleaning previous build artifacts..." -ForegroundColor Yellow
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
Write-Host "[2/9] Linting README.md image URLs..." -ForegroundColor Yellow

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

# Step 3: Lint README.md for stale version references.
# We deliberately removed the hardcoded version from the install snippet
# (it is now <PackageReference Include="Stardust.Utilities" /> with no
# Version= attribute, and the shields.io NuGet badge at the top of the
# README shows the current version from nuget.org). This lint is a
# defense-in-depth check: it fails the build if anything in README.md
# still carries a literal version that does NOT match $Version in
# Directory.Build.props. This catches both reintroduced install-snippet
# versions and "What's New" banners like **vX.Y.Z** that drift behind
# the bump.
Write-Host ""
Write-Host "[3/9] Linting README.md for stale version references..." -ForegroundColor Yellow

$staleVersions = @()

# Pattern 1: PackageReference install snippet -- Stardust.Utilities" Version="X.Y.Z"
$installMatches = [regex]::Matches($readmeText, 'Stardust\.Utilities"\s+Version="([0-9]+\.[0-9]+\.[0-9]+(?:-[A-Za-z0-9.-]+)?)"')
foreach ($m in $installMatches) {
    $v = $m.Groups[1].Value
    if ($v -ne $Version) {
        $staleVersions += "  install snippet: Version=`"$v`"  (expected `"$Version`")"
    }
}

# Pattern 2: "What's New" bolded version banner -- **vX.Y.Z** at the start of a line
$bannerMatches = [regex]::Matches($readmeText, '(?m)^\s*\*\*v([0-9]+\.[0-9]+\.[0-9]+(?:-[A-Za-z0-9.-]+)?)\*\*')
foreach ($m in $bannerMatches) {
    $v = $m.Groups[1].Value
    if ($v -ne $Version) {
        $staleVersions += "  **v$v** banner  (expected **v$Version**)"
    }
}

if ($staleVersions.Count -gt 0) {
    Write-Host "ERROR: README.md contains stale version references that do not match Directory.Build.props ($Version):" -ForegroundColor Red
    foreach ($s in $staleVersions) {
        Write-Host $s -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "  Fix: update README.md to match the version being built, or -- preferred --" -ForegroundColor Yellow
    Write-Host "  remove the literal version entirely and rely on the shields.io NuGet badge" -ForegroundColor Yellow
    Write-Host "  and CHANGELOG.md. See DEVELOPER.md -> 'Avoiding Stale Version References'." -ForegroundColor Yellow
    exit 1
}
Write-Host "  $($installMatches.Count) install-snippet and $($bannerMatches.Count) What's-New version(s) checked, all match $Version." -ForegroundColor Green

# Step 4: Build the Generator NuGet package using the dedicated script
Write-Host ""
Write-Host "[4/9] Building Stardust.Generators package..." -ForegroundColor Yellow

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

# Step 5: Build the main Stardust.Utilities project
Write-Host ""
Write-Host "[5/9] Building Stardust.Utilities..." -ForegroundColor Yellow

dotnet build $mainProject -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Main project build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  Done." -ForegroundColor Green

# Step 6: Run tests (unless skipped)
if (-not $SkipTests) {
    Write-Host ""
    Write-Host "[6/9] Running tests..." -ForegroundColor Yellow
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
    Write-Host "[6/9] Skipping tests (-SkipTests specified)" -ForegroundColor Yellow
}

# Step 7: Pin relative .md links in README.md so they resolve on nuget.org.
# nuget.org's README renderer does not resolve relative paths in packed
# READMEs -- the rendered link just bounces back to the same README. To keep
# docs version-pinned (the whole reason we embed them in the package), the
# source README keeps relative links (for GitHub) and the build script
# rewrites them to absolute URLs pinned to the release tag (e.g.
#   https://github.com/dhadner/Stardust.Utilities/blob/v0.9.10/LARGE_INTEGERS.md )
# just for the packed copy. The original README.md is always restored by the
# finally block below, even on pack failure or Ctrl+C.
Write-Host ""
Write-Host "[7/9] Pinning README.md doc links for pack..." -ForegroundColor Yellow

$readmeBackup = "$readmePath.packbackup"
if (Test-Path $readmeBackup) {
    # Prior run crashed after rewriting README but before restoring. The
    # backup is the original; restore it before we overwrite it again.
    Write-Host "  Stale README.md.packbackup from prior run -- restoring original first." -ForegroundColor Yellow
    Copy-Item -Path $readmeBackup -Destination $readmePath -Force
    Remove-Item -Path $readmeBackup -Force
}

Copy-Item -Path $readmePath -Destination $readmeBackup -Force

try {
    $readmeText = Get-Content $readmePath -Raw
    $tag = "v$Version"
    $baseUrl = "https://github.com/dhadner/Stardust.Utilities/blob/$tag/"

    # Match a Markdown link target that:
    #   - is not already an absolute URL  (negative lookahead: https?://, mailto:)
    #   - is not a pure in-doc anchor     (negative lookahead: #)
    #   - ends in .md, with an optional #fragment
    # Backreferences $1 = filename, $2 = #fragment (possibly empty).
    $linkPattern = '\]\((?!https?://|#|mailto:)([^)\s]+\.md)(#[^)]*)?\)'
    $linkMatches = [regex]::Matches($readmeText, $linkPattern)
    $linkCount = $linkMatches.Count

    # In PowerShell double-quoted strings, $tag interpolates (good) and
    # `$1 / `$2 pass through as literal regex backreferences (also good).
    $replacement = "](https://github.com/dhadner/Stardust.Utilities/blob/$tag/`$1`$2)"
    $newText = [regex]::Replace($readmeText, $linkPattern, $replacement)

    Set-Content -Path $readmePath -Value $newText -NoNewline -Encoding UTF8
    Write-Host "  Rewrote $linkCount relative .md link(s) -> $baseUrl<file>.md" -ForegroundColor Green

    # Step 8: Pack the Stardust.Utilities NuGet package
    Write-Host ""
    Write-Host "[8/9] Creating Stardust.Utilities package..." -ForegroundColor Yellow

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
}
finally {
    # Restore the original README.md no matter what -- exit, throw, Ctrl+C.
    if (Test-Path $readmeBackup) {
        Copy-Item -Path $readmeBackup -Destination $readmePath -Force
        Remove-Item -Path $readmeBackup -Force
    }
}

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

# Step 9: Publish to local NuGet feed
Write-Host ""
Write-Host "[9/9] Publishing to local NuGet feed..." -ForegroundColor Yellow

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
    Write-Host "  1. git tag v$Version && git push origin v$Version" -ForegroundColor Gray
    Write-Host "     (packed README links point at blob/v$Version/... -- push the tag FIRST" -ForegroundColor Gray
    Write-Host "      so those links resolve the moment the package is live on nuget.org)" -ForegroundColor Gray
    Write-Host "  2. dotnet nuget push `"$nupkgFolder\$($utilitiesPackage.Name)`" -k YOUR_API_KEY -s https://api.nuget.org/v3/index.json" -ForegroundColor Gray
    Write-Host ""
}

