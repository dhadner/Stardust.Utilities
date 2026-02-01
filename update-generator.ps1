# update-generator.ps1
# Updates the Stardust.Generators version across all projects and rebuilds the NuGet package.
#
# Usage: .\update-generator.ps1 -NewVersion "0.2.1"
# Can be run from any directory.

param(
    [Parameter(Mandatory=$true)]
    [string]$NewVersion
)

$ErrorActionPreference = "Stop"

# Find the solution root (directory containing MacView.sln)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionRoot = Split-Path -Parent $scriptDir

# Verify we found the right directory
if (-not (Test-Path (Join-Path $solutionRoot "MacView.sln"))) {
    Write-Host "Error: Could not find MacView.sln in $solutionRoot" -ForegroundColor Red
    exit 1
}

Push-Location $solutionRoot

try {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " Updating Stardust.Generators to $NewVersion" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " (Working from: $solutionRoot)" -ForegroundColor DarkGray
    Write-Host ""

    # Step 1: Update version in Generators project
    $genCsproj = "Stardust.Utilities\Generators\Stardust.Generators.csproj"
    Write-Host "[1/6] Updating $genCsproj" -ForegroundColor Yellow
    (Get-Content $genCsproj) -replace '<Version>.*</Version>', "<Version>$NewVersion</Version>" | Set-Content $genCsproj
    Write-Host "      Done" -ForegroundColor Green

    # Step 2: Update version in main project
    $mainCsproj = "Stardust.Utilities\Stardust.Utilities.csproj"
    Write-Host "[2/6] Updating $mainCsproj" -ForegroundColor Yellow
    (Get-Content $mainCsproj) -replace '<Version>.*</Version>', "<Version>$NewVersion</Version>" | Set-Content $mainCsproj
    Write-Host "      Done" -ForegroundColor Green


    # Step 3: Update package references in ALL consuming projects
    Write-Host "[3/6] Updating PackageReference in consuming projects" -ForegroundColor Yellow
    $consumingProjects = @(
        "Stardust.Utilities\Test\Stardust.Utilities.Tests.csproj",
        "MacSE.Tests\MacSE.Tests.csproj",
        "MacSE\MacSE.csproj",
        "MacDebug\MacDebug.csproj",
        "MacDebug.Tests\MacDebug.Tests.csproj"
    )

    foreach ($proj in $consumingProjects) {
        if (Test-Path $proj) {
            $content = Get-Content $proj -Raw
            if ($content -match 'Stardust\.Generators') {
                (Get-Content $proj) -replace 'Include="Stardust.Generators" Version="[^"]*"', "Include=`"Stardust.Generators`" Version=`"$NewVersion`"" | Set-Content $proj
                Write-Host "      Updated $proj" -ForegroundColor Green
            } else {
                Write-Host "      Skipped $proj (no Stardust.Generators reference)" -ForegroundColor DarkGray
            }
        } else {
            Write-Host "      Skipped $proj (not found)" -ForegroundColor DarkGray
        }
    }

    # Step 4: Pack the new version
    Write-Host "[4/6] Building NuGet package" -ForegroundColor Yellow
    $packResult = dotnet pack $genCsproj -c Release -o "Stardust.Utilities\nupkg" 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "      FAILED!" -ForegroundColor Red
        Write-Host $packResult
        exit 1
    }
    Write-Host "      Created Stardust.Generators.$NewVersion.nupkg" -ForegroundColor Green


    # Step 6: Restore and build using MSBuild (required for native code projects)
    Write-Host "[6/6] Restoring and building with MSBuild..." -ForegroundColor Yellow
    $buildResult = msbuild MacView.sln /p:Configuration=Release "/p:Platform=Any CPU" /m /v:minimal /t:Restore,Build 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "      FAILED!" -ForegroundColor Red
        $buildResult | Select-Object -Last 20 | ForEach-Object { Write-Host $_ }
        exit 1
    }
    Write-Host "      Done" -ForegroundColor Green

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " Success! Version updated to $NewVersion" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor White
    Write-Host "  1. Run 'dotnet build' to verify everything compiles" -ForegroundColor White
    Write-Host "  2. Run tests to verify generator output is correct" -ForegroundColor White
    Write-Host ""
}
finally {
    Pop-Location
}
