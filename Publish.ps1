<#
.SYNOPSIS
    Publishes the MicrosoftDynamics.Api package to NuGet.org

.DESCRIPTION
    This script performs the following steps:
    1. Checks for clean git working directory (porcelain)
    2. Determines the Nerdbank GitVersioning version
    3. Validates nuget-key.txt exists, has content, and is gitignored
    4. Runs unit tests (unless -SkipTests is specified)
    5. Publishes to NuGet.org

.PARAMETER SkipTests
    Skip running unit tests before publishing

.EXAMPLE
    .\Publish.ps1
    
.EXAMPLE
    .\Publish.ps1 -SkipTests
#>

param(
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string]$Message)
    Write-Host "`n=== $Message ===" -ForegroundColor Cyan
}

function Exit-WithError {
    param([string]$Message)
    Write-Host "ERROR: $Message" -ForegroundColor Red
    exit 1
}

function Get-NbgvPath {
    # Try to find nbgv in PATH first
    $nbgvInPath = Get-Command nbgv -ErrorAction SilentlyContinue
    if ($nbgvInPath) {
        return $nbgvInPath.Source
    }
    
    # Try the default global tools location
    $defaultPath = Join-Path $env:USERPROFILE ".dotnet\tools\nbgv.exe"
    if (Test-Path $defaultPath) {
        return $defaultPath
    }
    
    return $null
}

# Step 1: Check for clean git working directory
Write-Step "Checking git working directory status"

$gitStatus = git status --porcelain 2>&1
if ($LASTEXITCODE -ne 0) {
    Exit-WithError "Failed to get git status: $gitStatus"
}

if ($gitStatus) {
    Exit-WithError "Git working directory is not clean. Please commit or stash your changes.`n$gitStatus"
}

Write-Host "Git working directory is clean" -ForegroundColor Green

# Step 2: Determine Nerdbank GitVersioning version
Write-Step "Determining version from Nerdbank GitVersioning"

$nbgvPath = Get-NbgvPath
if (-not $nbgvPath) {
    Exit-WithError "nbgv tool not found. Install it with: dotnet tool install -g nbgv"
}

$versionOutput = & $nbgvPath get-version --format json 2>&1
if ($LASTEXITCODE -ne 0) {
    Exit-WithError "Failed to get version from Nerdbank GitVersioning: $versionOutput"
}

$versionInfo = $versionOutput | ConvertFrom-Json
$version = $versionInfo.NuGetPackageVersion

if (-not $version) {
    Exit-WithError "Could not determine NuGet package version from Nerdbank GitVersioning"
}

Write-Host "Version: $version" -ForegroundColor Green

# Step 3: Check nuget-key.txt exists, has content, and is gitignored
Write-Step "Validating nuget-key.txt"

$nugetKeyPath = Join-Path $PSScriptRoot "nuget-key.txt"

if (-not (Test-Path $nugetKeyPath)) {
    Exit-WithError "nuget-key.txt not found at: $nugetKeyPath"
}

$nugetKey = (Get-Content $nugetKeyPath -Raw).Trim()

if ([string]::IsNullOrWhiteSpace($nugetKey)) {
    Exit-WithError "nuget-key.txt is empty"
}

# Check if nuget-key.txt is gitignored
$gitIgnoreCheck = git check-ignore -q "nuget-key.txt" 2>&1
if ($LASTEXITCODE -ne 0) {
    Exit-WithError "nuget-key.txt is not gitignored. Add 'nuget-key.txt' to .gitignore to prevent accidental commits"
}

Write-Host "nuget-key.txt is valid and gitignored" -ForegroundColor Green

# Step 4: Run unit tests (unless -SkipTests is specified)
if ($SkipTests) {
    Write-Step "Skipping unit tests (-SkipTests specified)"
} else {
    Write-Step "Running unit tests"
    
    dotnet test --configuration Release
    if ($LASTEXITCODE -ne 0) {
        Exit-WithError "Unit tests failed"
    }
    
    Write-Host "All tests passed" -ForegroundColor Green
}

# Step 5: Build and publish to NuGet.org
Write-Step "Building package"

$projectPath = Join-Path $PSScriptRoot "MicrosoftDynamics.Api" "MicrosoftDynamics.Api.csproj"
$outputPath = Join-Path $PSScriptRoot "artifacts"

# Clean artifacts folder
if (Test-Path $outputPath) {
    Remove-Item $outputPath -Recurse -Force
}

dotnet pack $projectPath --configuration Release --output $outputPath
if ($LASTEXITCODE -ne 0) {
    Exit-WithError "Failed to build package"
}

Write-Host "Package built successfully" -ForegroundColor Green

# Find the package file
$packageFile = Get-ChildItem -Path $outputPath -Filter "*.nupkg" | Where-Object { $_.Name -notlike "*.symbols.nupkg" } | Select-Object -First 1

if (-not $packageFile) {
    Exit-WithError "No .nupkg file found in $outputPath"
}

Write-Step "Publishing to NuGet.org"

Write-Host "Publishing: $($packageFile.Name)"

dotnet nuget push $packageFile.FullName --api-key $nugetKey --source https://api.nuget.org/v3/index.json --skip-duplicate
if ($LASTEXITCODE -ne 0) {
    Exit-WithError "Failed to publish package to NuGet.org"
}

Write-Host "`nSuccessfully published $($packageFile.Name) to NuGet.org" -ForegroundColor Green

exit 0
