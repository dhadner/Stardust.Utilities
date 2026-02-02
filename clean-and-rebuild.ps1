# clean-and-rebuild.ps1
# Aggressively cleans all build artifacts and NuGet cache, then rebuilds.
# Run this when you get NuGet restore loops or locked file errors.
#
# Usage: .\clean-and-rebuild.ps1
# Must be run from the MacView solution directory.

$ErrorActionPreference = "Continue"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Clean and Rebuild" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Kill processes that might lock files
Write-Host "[1/7] Killing processes that might lock files..." -ForegroundColor Yellow
$processesToKill = @("dotnet", "MSBuild", "VBCSCompiler", "testhost", "vstest.console")
foreach ($proc in $processesToKill) {
    $running = Get-Process -Name $proc -ErrorAction SilentlyContinue
    if ($running) {
        Write-Host "      Stopping $proc..." -ForegroundColor DarkGray
        Stop-Process -Name $proc -Force -ErrorAction SilentlyContinue
    }
}
Start-Sleep -Seconds 2
Write-Host "      Done" -ForegroundColor Green

# Step 2: Clean all obj/bin directories
Write-Host "[2/7] Removing obj and bin directories..." -ForegroundColor Yellow
$dirsToRemove = Get-ChildItem -Path . -Directory -Recurse -Include "obj", "bin" -ErrorAction SilentlyContinue
foreach ($dir in $dirsToRemove) {
    try {
        Remove-Item $dir.FullName -Recurse -Force -ErrorAction Stop
        Write-Host "      Removed $($dir.FullName)" -ForegroundColor DarkGray
    } catch {
        Write-Host "      Could not remove $($dir.FullName): $($_.Exception.Message)" -ForegroundColor DarkYellow
    }
}
Write-Host "      Done" -ForegroundColor Green

# Step 3: Remove Generated directories
Write-Host "[3/7] Removing Generated directories..." -ForegroundColor Yellow
$generatedDirs = Get-ChildItem -Path . -Directory -Recurse -Include "Generated" -ErrorAction SilentlyContinue
foreach ($dir in $generatedDirs) {
    try {
        Remove-Item $dir.FullName -Recurse -Force -ErrorAction Stop
        Write-Host "      Removed $($dir.FullName)" -ForegroundColor DarkGray
    } catch {
        Write-Host "      Could not remove $($dir.FullName)" -ForegroundColor DarkYellow
    }
}
Write-Host "      Done" -ForegroundColor Green

# Step 4: Clean old nupkg files
Write-Host "[4/7] Removing old nupkg files..." -ForegroundColor Yellow
$nupkgFiles = Get-ChildItem -Path "Stardust.Utilities\nupkg" -Filter "*.nupkg" -ErrorAction SilentlyContinue
foreach ($file in $nupkgFiles) {
    Remove-Item $file.FullName -Force -ErrorAction SilentlyContinue
    Write-Host "      Removed $($file.Name)" -ForegroundColor DarkGray
}
Write-Host "      Done" -ForegroundColor Green

# Step 5: Clear NuGet caches
Write-Host "[5/7] Clearing NuGet caches..." -ForegroundColor Yellow
dotnet nuget locals http-cache --clear 2>$null
dotnet nuget locals temp --clear 2>$null
dotnet nuget locals plugins-cache --clear 2>$null
# Try global-packages, but it often fails due to locks
$result = dotnet nuget locals global-packages --clear 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "      Note: Could not fully clear global-packages (files may be locked)" -ForegroundColor DarkYellow
    Write-Host "      This is usually OK - locked packages will be overwritten." -ForegroundColor DarkGray
} else {
    Write-Host "      Cleared global packages" -ForegroundColor DarkGray
}
Write-Host "      Done" -ForegroundColor Green

# Step 6: Build the generator first (important!)
Write-Host "[6/7] Building source generator..." -ForegroundColor Yellow
$genResult = dotnet build "Stardust.Utilities\Generators\Stardust.Generators.csproj" -c Release 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "      FAILED to build generator!" -ForegroundColor Red
    Write-Host $genResult -ForegroundColor Red
    exit 1
}
Write-Host "      Done" -ForegroundColor Green

# Step 7: Build the solution using MSBuild (required for native code projects)
Write-Host "[7/7] Building solution with MSBuild..." -ForegroundColor Yellow
$msbuildResult = msbuild MacView.sln /p:Configuration=Release "/p:Platform=Any CPU" /m /v:minimal /t:Restore,Build 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "      Build had errors:" -ForegroundColor Red
    # Show last 20 lines of output
    $msbuildResult | Select-Object -Last 20 | ForEach-Object { Write-Host $_ }
    exit 1
}
Write-Host "      Done" -ForegroundColor Green

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Build completed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
