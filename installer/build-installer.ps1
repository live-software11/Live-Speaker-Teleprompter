# Live Speaker Teleprompter - Unified Build Script
# Produces:
#   1. Portable EXE (release/Live_Speaker_Teleprompter_Portable.exe)
#      - Self-contained, nessun runtime richiesto.
#      - NESSUN gate licenze (LICENSE_ENABLED = false): versione per demo/uso interno.
#   2. Self-extracting installer EXE (release/Live_Speaker_Teleprompter_Setup.exe)
#      - Self-contained, nessun runtime richiesto.
#      - Include il gate licenze Live WORKS (LICENSE_ENABLED = true): versione vendibile.
#      - Uninstaller chiama `--deactivate` per rilasciare la licenza su /api/deactivate.
#
# Due publish passes distinti per differenziare gli eseguibili.
#
# Usage:
#   .\build-installer.ps1                  # full build (portable + setup)
#
# T-04 (opzionale, se userai APP_CHALLENGE_ENFORCED=true in produzione):
#   $env:LIVEWORKS_APP_CHALLENGE_SECRET = "<stesso valore di APP_CHALLENGE_SECRET_SPEAKER_TELEPROMPTER (Secret Manager)>"
#   .\build-installer.ps1
# La publish SETUP passa -p:LiveWorksAppChallengeSecret=...; la publish Portable no.

param(
    [string]$ProjectDir  = (Resolve-Path "$PSScriptRoot\..\src\TeleprompterApp"),
    [string]$OutputDir   = (Join-Path (Split-Path $PSScriptRoot -Parent) "release"),
    [string]$InstallerName = "Live_Speaker_Teleprompter_Setup.exe"
)

$ErrorActionPreference = "Stop"

# -- Paths ---------------------------------------------------------------
$publishDir   = Join-Path $ProjectDir "bin\Release\net8.0-windows\win-x64\publish"
$releaseDir   = Join-Path $ProjectDir "bin\Release"
$objDir       = Join-Path $ProjectDir "obj"
$exeTarget    = Join-Path $OutputDir $InstallerName
$portableExeTarget = Join-Path $OutputDir "Live_Speaker_Teleprompter_Portable.exe"
$templatePath = Join-Path $PSScriptRoot "installer-template.ps1"

function Invoke-DotnetPublish {
    param([bool]$LicenseEnabled)
    $label = if ($LicenseEnabled) { 'SETUP (licensed)' } else { 'PORTABLE (no-license)' }
    Write-Host "  dotnet publish -> $label" -ForegroundColor DarkGray
    if (Test-Path $releaseDir) { Remove-Item $releaseDir -Recurse -Force }
    if (Test-Path $objDir) { Remove-Item $objDir -Recurse -Force }
    Push-Location $ProjectDir
    try {
        $licArg = if ($LicenseEnabled) { 'true' } else { 'false' }
        $chArgs = @()
        if ($LicenseEnabled -and -not [string]::IsNullOrWhiteSpace($env:LIVEWORKS_APP_CHALLENGE_SECRET)) {
            $chArgs = @(
                "-p:LiveWorksAppChallengeSecret=$($env:LIVEWORKS_APP_CHALLENGE_SECRET)"
            )
        }
        $publishArgs = @("publish", "-c", "Release", "-p:LicenseEnabled=$licArg", "--nologo", "-v", "minimal")
        if ($chArgs.Count -gt 0) { $publishArgs = $publishArgs + $chArgs }
        & dotnet $publishArgs
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet publish ($label) failed with exit code $LASTEXITCODE"
        }
    } finally {
        Pop-Location
    }
    if (-not (Test-Path $publishDir)) { throw "Publish directory not found: $publishDir" }
}

function Get-FriendlyExe {
    $originalExe = Join-Path $publishDir "TeleprompterApp.exe"
    $friendlyExe = Join-Path $publishDir "Live Speaker Teleprompter.exe"
    if (Test-Path $originalExe) {
        Move-Item $originalExe $friendlyExe -Force
    }
    $originalPdb = Join-Path $publishDir "TeleprompterApp.pdb"
    $friendlyPdb = Join-Path $publishDir "Live Speaker Teleprompter.pdb"
    if (Test-Path $originalPdb) {
        Move-Item $originalPdb $friendlyPdb -Force
    }
    return $friendlyExe
}

Write-Host ""
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  Live Speaker Teleprompter - Build Pipeline v2.1" -ForegroundColor Cyan
Write-Host "  Dual build: Portable (no-license) + Setup (licensed)" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

# -- Step 0: Prepare output directory -----------------------------------
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}
Remove-Item $exeTarget         -Force -ErrorAction SilentlyContinue
Remove-Item $portableExeTarget -Force -ErrorAction SilentlyContinue

# -- Step 1: Publish PORTABLE (no-license) ------------------------------
Write-Host "[1/4] Publish PORTABLE (LICENSE_ENABLED=false)..." -ForegroundColor Yellow
Invoke-DotnetPublish -LicenseEnabled $false
$friendlyExe = Get-FriendlyExe
$portableInfo = Get-Item $friendlyExe
Write-Host "  Portable publish OK ($([math]::Round($portableInfo.Length/1MB, 1)) MB)" -ForegroundColor Green

Copy-Item $friendlyExe $portableExeTarget -Force
$portableInfo = Get-Item $portableExeTarget
Write-Host "  Portable EXE -> Live_Speaker_Teleprompter_Portable.exe ($([math]::Round($portableInfo.Length/1MB, 1)) MB)" -ForegroundColor Green

# -- Step 2: Publish SETUP (licensed) -----------------------------------
Write-Host ""
Write-Host "[2/4] Publish SETUP (LICENSE_ENABLED=true)..." -ForegroundColor Yellow
Invoke-DotnetPublish -LicenseEnabled $true
$friendlyExe = Get-FriendlyExe
$setupInfo = Get-Item $friendlyExe
Write-Host "  Setup publish OK ($([math]::Round($setupInfo.Length/1MB, 1)) MB)" -ForegroundColor Green

# -- Step 3: Validate different binaries --------------------------------
Write-Host ""
Write-Host "[3/4] Validating dual-binary output..." -ForegroundColor Yellow
$portableHash = (Get-FileHash $portableExeTarget -Algorithm SHA256).Hash
$setupSourceHash = (Get-FileHash $friendlyExe -Algorithm SHA256).Hash
if ($portableHash -eq $setupSourceHash) {
    Write-Warning "  Portable e Setup hanno lo stesso hash SHA-256: la separazione LicenseEnabled potrebbe non essere attiva."
} else {
    Write-Host "  OK: binari distinti (Portable != Setup)." -ForegroundColor Green
}

# -- Step 4: Wrap SETUP in self-extracting installer --------------------
Write-Host ""
Write-Host "[4/4] Creating self-extracting installer (IExpress)..." -ForegroundColor Yellow

if (-not (Test-Path $templatePath)) {
    Write-Warning "Installer template not found at $templatePath -- skipping installer."
} else {
    $tempDir = Join-Path $env:TEMP ("live-speaker-build-" + [guid]::NewGuid().ToString('N').Substring(0,8))
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

    try {
        # Installer ZIP: stesso exe ma con nome "Live Speaker Teleprompter.exe" per installazione pulita
        $installerZipPath = Join-Path $tempDir "installer-payload.zip"
        Compress-Archive -Path $friendlyExe -DestinationPath $installerZipPath -CompressionLevel Optimal -Force

        # Read the installer template and embed the ZIP as base64 payload
        $zipBytes  = [System.IO.File]::ReadAllBytes($installerZipPath)
        $zipBase64 = [System.Convert]::ToBase64String($zipBytes)

        # Write the final installer script with injected payload
        $installerScriptPath = Join-Path $tempDir "installer.ps1"
        $finalContent = (Get-Content $templatePath -Raw).Replace('##EMBEDDED_ZIP##', $zipBase64)
        [System.IO.File]::WriteAllText($installerScriptPath, $finalContent, [System.Text.Encoding]::UTF8)
        Remove-Variable finalContent

        # Copy app icon for form title bar (broadcast-style installer)
        $iconSrc = Join-Path (Split-Path $PSScriptRoot -Parent) "icons\app-icon.ico"
        if (Test-Path $iconSrc) {
            Copy-Item $iconSrc (Join-Path $tempDir "app-icon.ico") -Force
        }

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
        $sedContent += "FriendlyName=Live Speaker Teleprompter Installer`r`n"
        $sedContent += "AppLaunched=cmd /c powershell.exe -NoProfile -ExecutionPolicy Bypass -File " + '"installer.ps1"' + "`r`n"
        $sedContent += "PostInstallCmd=<None>`r`n"
        $sedContent += "AdminQuietInstCmd=`r`n"
        $sedContent += "UserQuietInstCmd=`r`n"
        $sedContent += 'FILE0="installer.ps1"' + "`r`n"
        if (Test-Path (Join-Path $tempDir "app-icon.ico")) {
            $sedContent += 'FILE1="app-icon.ico"' + "`r`n"
        }
        $sedContent += "`r`n"
        $sedContent += "[SourceFiles]`r`n"
        $sedContent += "SourceFiles0=$tempDir\`r`n"
        $sedContent += "`r`n"
        $sedContent += "[SourceFiles0]`r`n"
        $sedContent += "%FILE0%=`r`n"
        if (Test-Path (Join-Path $tempDir "app-icon.ico")) {
            $sedContent += "%FILE1%=`r`n"
        }

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

# -- Step 5: Copy README ITA/ENG to release --------------------------------
$docsDir = Join-Path (Split-Path $PSScriptRoot -Parent) "docs"
$readmeIta = Join-Path $docsDir "README_ITA_Live_Speaker_Teleprompter.md"
$readmeEng = Join-Path $docsDir "README_ENG_Live_Speaker_Teleprompter.md"
if (Test-Path $readmeIta) { Copy-Item $readmeIta $OutputDir -Force }
if (Test-Path $readmeEng) { Copy-Item $readmeEng $OutputDir -Force }

# -- Summary -------------------------------------------------------------
Write-Host ""
Write-Host "========================================================" -ForegroundColor Green
Write-Host "  Build complete!" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Outputs in: $OutputDir" -ForegroundColor Cyan
if (Test-Path $portableExeTarget) {
    Write-Host "  [OK] Portable:  Live_Speaker_Teleprompter_Portable.exe" -ForegroundColor White
}
if (Test-Path $exeTarget) {
    Write-Host "  [OK] Setup:     $InstallerName" -ForegroundColor White
}
if (Test-Path (Join-Path $OutputDir "README_ITA_Live_Speaker_Teleprompter.md")) {
    Write-Host "  [OK] README ITA" -ForegroundColor White
}
if (Test-Path (Join-Path $OutputDir "README_ENG_Live_Speaker_Teleprompter.md")) {
    Write-Host "  [OK] README ENG" -ForegroundColor White
}
Write-Host ""
