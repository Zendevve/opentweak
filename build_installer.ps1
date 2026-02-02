param (
    [switch]$Official
)

$ErrorActionPreference = "Stop"

Write-Host "Building OpenTweak..." -ForegroundColor Cyan

# 1. Publish the .NET application
$projectPath = "OpenTweak\OpenTweak.csproj"
$publishDir = "OpenTweak\bin\Release\net8.0-windows\win-x64\publish"

# Clean previous build
if (Test-Path $publishDir) {
    Remove-Item -Path $publishDir -Recurse -Force
}

# Publish single file
$buildArgs = @(
    "publish", $projectPath,
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "false",
    "-p:PublishSingleFile=true",
    "-o", $publishDir
)

if ($Official) {
    Write-Host "BUILDING OFFICIAL RELEASE" -ForegroundColor Yellow
    $buildArgs += "-p:OfficialBuild=true"
}

dotnet @buildArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit 1
}

Write-Host "Build successful. Checking for Inno Setup..." -ForegroundColor Cyan

# 2. Find Inno Setup Compiler
$isccPath = "ISCC.exe" # Assume in PATH by default

# Check common install locations if not in PATH
if (-not (Get-Command "ISCC.exe" -ErrorAction SilentlyContinue)) {
    $commonPaths = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
    )

    foreach ($path in $commonPaths) {
        if (Test-Path $path) {
            $isccPath = $path
            break
        }
    }
}

if (-not (Get-Command $isccPath -ErrorAction SilentlyContinue) -and -not (Test-Path $isccPath)) {
    Write-Warning "Inno Setup Compiler (ISCC.exe) not found."
    Write-Warning "Please install Inno Setup from: https://jrsoftware.org/isdl.php"
    Write-Warning "After installing, run this script again."
    exit 0
}

# 3. Build Installer
Write-Host "Building Installer with Inno Setup..." -ForegroundColor Cyan
& $isccPath "setup.iss"

if ($LASTEXITCODE -eq 0) {
    Write-Host "Installer created successfully in .\Output\" -ForegroundColor Green
} else {
    Write-Error "Installer build failed."
    exit 1
}
