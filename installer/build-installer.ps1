# R-Speaker Teleprompter - Unified Build Script
# Produces:
#   1. Self-contained single-file publish (dotnet publish)
#   2. Portable ZIP  (portable/R-Speaker-Teleprompter-Portable.zip)
#   3. Self-extracting installer EXE (portable/R-Speaker-Teleprompter-Installer.exe)
#
# Both ZIP and Installer contain the EXACT same files, so the portable
# version is 100% identical to the installed version.
#
# Usage:
#   .\build-installer.ps1                  # full build
#   .\build-installer.ps1 -SkipPublish     # reuse previous publish output

param(
    [string]$ProjectDir  = (Resolve-Path "$PSScriptRoot\..\src\TeleprompterApp"),
    [string]$OutputDir   = (Resolve-Path "$PSScriptRoot\..\portable"),
    [string]$ZipName     = "R-Speaker-Teleprompter-Portable.zip",
    [string]$InstallerName = "R-Speaker-Teleprompter-Installer.exe",
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"

# -- Paths ---------------------------------------------------------------
$publishDir   = Join-Path $ProjectDir "bin\Release\net8.0-windows\win-x64\publish"
$zipTarget    = Join-Path $OutputDir $ZipName
$exeTarget    = Join-Path $OutputDir $InstallerName
$templatePath = Join-Path $PSScriptRoot "installer-template.ps1"

Write-Host ""
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  R-Speaker Teleprompter - Build Pipeline" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

# -- Step 1: Publish -----------------------------------------------------
if (-not $SkipPublish) {
    Write-Host "[1/4] Publishing self-contained single-file executable..." -ForegroundColor Yellow

    # Clean previous publish output
    $releaseDir = Join-Path $ProjectDir "bin\Release"
    if (Test-Path $releaseDir) {
        Remove-Item $releaseDir -Recurse -Force
    }

    Push-Location $ProjectDir
    dotnet publish -c Release --nologo -v q
    Pop-Location

    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
        exit 1
    }
} else {
    Write-Host "[1/4] Skipping publish (reusing existing output)..." -ForegroundColor DarkGray
}

# Validate publish output
if (-not (Test-Path $publishDir)) {
    Write-Error "Publish directory not found: $publishDir"
    exit 1
}

$exeFile = Get-ChildItem $publishDir -Filter "*.exe" | Select-Object -First 1
if (-not $exeFile) {
    Write-Error "No executable found in publish directory."
    exit 1
}

# Rename exe to friendly name (AssemblyName must stay TeleprompterApp for WPF XAML compatibility)
$originalExe = Join-Path $publishDir "TeleprompterApp.exe"
$friendlyExe = Join-Path $publishDir "R-Speaker Teleprompter.exe"
if (Test-Path $originalExe) {
    Move-Item $originalExe $friendlyExe -Force
    Write-Host "  Renamed: TeleprompterApp.exe -> R-Speaker Teleprompter.exe" -ForegroundColor DarkGray
}
$originalPdb = Join-Path $publishDir "TeleprompterApp.pdb"
$friendlyPdb = Join-Path $publishDir "R-Speaker Teleprompter.pdb"
if (Test-Path $originalPdb) {
    Move-Item $originalPdb $friendlyPdb -Force
}

$publishFiles = Get-ChildItem $publishDir -Recurse -File
$totalSize = ($publishFiles | Measure-Object -Property Length -Sum).Sum
Write-Host "  Published: $($publishFiles.Count) files, $([math]::Round($totalSize/1MB, 1)) MB" -ForegroundColor Green

# -- Step 2: Ensure output directory -------------------------------------
Write-Host "[2/4] Preparing output directory..." -ForegroundColor Yellow
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# Remove old outputs
Remove-Item $zipTarget  -Force -ErrorAction SilentlyContinue
Remove-Item $exeTarget  -Force -ErrorAction SilentlyContinue

# -- Step 3: Create portable ZIP -----------------------------------------
Write-Host "[3/4] Creating portable ZIP..." -ForegroundColor Yellow
Compress-Archive -Path "$publishDir\*" -DestinationPath $zipTarget -CompressionLevel Optimal

$zipInfo = Get-Item $zipTarget
Write-Host "  ZIP: $ZipName ($([math]::Round($zipInfo.Length/1MB, 1)) MB)" -ForegroundColor Green

# -- Step 4: Create self-extracting installer -----------------------------
Write-Host "[4/4] Creating self-extracting installer..." -ForegroundColor Yellow

if (-not (Test-Path $templatePath)) {
    Write-Warning "Installer template not found at $templatePath -- skipping installer."
} else {
    $tempDir = Join-Path $env:TEMP ("r-speaker-build-" + [guid]::NewGuid().ToString('N').Substring(0,8))
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

    try {
        # Read the installer template and embed the portable ZIP as base64 payload
        $zipBytes  = [System.IO.File]::ReadAllBytes($zipTarget)
        $zipBase64 = [System.Convert]::ToBase64String($zipBytes)

        # Write the final installer script with injected payload
        $installerScriptPath = Join-Path $tempDir "installer.ps1"
        $finalContent = (Get-Content $templatePath -Raw).Replace('##EMBEDDED_ZIP##', $zipBase64)
        [System.IO.File]::WriteAllText($installerScriptPath, $finalContent, [System.Text.Encoding]::UTF8)
        Remove-Variable finalContent

        # Build IExpress SED directive file
        $sedPath = Join-Path $tempDir "installer.sed"
        $sedContent  = "[Version]`r`n"
        $sedContent += "Class=IEXPRESS`r`n"
        $sedContent += "SEDVersion=3`r`n"
        $sedContent += "[Options]`r`n"
        $sedContent += "PackagePurpose=InstallApp`r`n"
        $sedContent += "ShowInstallProgramWindow=0`r`n"
        $sedContent += "HideExtractAnimation=1`r`n"
        $sedContent += "UseLongFileName=1`r`n"
        $sedContent += "InsideCompressed=0`r`n"
        $sedContent += "CAB_FixedSize=0`r`n"
        $sedContent += "CAB_ResvCodeSigning=0`r`n"
        $sedContent += "RebootMode=N`r`n"
        $sedContent += "InstallPrompt=%InstallPrompt%`r`n"
        $sedContent += "DisplayLicense=%DisplayLicense%`r`n"
        $sedContent += "FinishMessage=%FinishMessage%`r`n"
        $sedContent += "TargetName=%TargetName%`r`n"
        $sedContent += "FriendlyName=%FriendlyName%`r`n"
        $sedContent += "AppLaunched=%AppLaunched%`r`n"
        $sedContent += "PostInstallCmd=%PostInstallCmd%`r`n"
        $sedContent += "AdminQuietInstCmd=%AdminQuietInstCmd%`r`n"
        $sedContent += "UserQuietInstCmd=%UserQuietInstCmd%`r`n"
        $sedContent += "SourceFiles=SourceFiles`r`n"
        $sedContent += "`r`n"
        $sedContent += "[Strings]`r`n"
        $sedContent += "InstallPrompt=`r`n"
        $sedContent += "DisplayLicense=`r`n"
        $sedContent += "FinishMessage=`r`n"
        $sedContent += "TargetName=$exeTarget`r`n"
        $sedContent += "FriendlyName=R-Speaker Teleprompter Installer`r`n"
        $sedContent += "AppLaunched=cmd /c powershell.exe -NoProfile -ExecutionPolicy Bypass -File " + '"installer.ps1"' + "`r`n"
        $sedContent += "PostInstallCmd=<None>`r`n"
        $sedContent += "AdminQuietInstCmd=`r`n"
        $sedContent += "UserQuietInstCmd=`r`n"
        $sedContent += 'FILE0="installer.ps1"' + "`r`n"
        $sedContent += "`r`n"
        $sedContent += "[SourceFiles]`r`n"
        $sedContent += "SourceFiles0=$tempDir\`r`n"
        $sedContent += "`r`n"
        $sedContent += "[SourceFiles0]`r`n"
        $sedContent += "%FILE0%=`r`n"

        [System.IO.File]::WriteAllText($sedPath, $sedContent, [System.Text.Encoding]::ASCII)

        # Run IExpress
        $iexpressExe = Join-Path $env:SystemRoot "System32\iexpress.exe"
        if (-not (Test-Path $iexpressExe)) {
            Write-Warning "IExpress not found -- skipping installer EXE."
        } else {
            Write-Host "  Running IExpress..." -ForegroundColor DarkGray
            & $iexpressExe /N $sedPath | Out-Null

            if (Test-Path $exeTarget) {
                $exeInfo = Get-Item $exeTarget
                Write-Host "  EXE: $InstallerName ($([math]::Round($exeInfo.Length/1MB, 1)) MB)" -ForegroundColor Green
            } else {
                Write-Warning "IExpress did not produce installer. ZIP is still available."
            }
        }
    } finally {
        Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# -- Summary -------------------------------------------------------------
Write-Host ""
Write-Host "========================================================" -ForegroundColor Green
Write-Host "  Build complete!" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Outputs in: $OutputDir" -ForegroundColor Cyan
if (Test-Path $zipTarget) {
    Write-Host "  [OK] Portable ZIP:  $ZipName" -ForegroundColor White
}
if (Test-Path $exeTarget) {
    Write-Host "  [OK] Installer EXE: $InstallerName" -ForegroundColor White
}
Write-Host ""
Write-Host "  Both versions contain the exact same self-contained application." -ForegroundColor DarkGray
Write-Host ""
