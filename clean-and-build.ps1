# Live Speaker Teleprompter - Clean & Build
# Pulisce tutto l'obsoleto e crea build aggiornate per setup e portable.
# Lancia dalla root del progetto: .\clean-and-build.ps1

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host ""
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  Live Speaker Teleprompter - Clean & Build" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

# -- 1. Pulizia bin/ obj/ publish/ ---------------------------------------
Write-Host "[1/5] Pulizia cartelle build obsolete..." -ForegroundColor Yellow
$dirsToClean = @(
    (Join-Path $root "src\TeleprompterApp\bin"),
    (Join-Path $root "src\TeleprompterApp\obj"),
    (Join-Path $root "src\TeleprompterApp\publish")
)
foreach ($d in $dirsToClean) {
    if (Test-Path $d) {
        Remove-Item $d -Recurse -Force
        Write-Host "  Rimosso: $d" -ForegroundColor DarkGray
    }
}
Write-Host "  Pulizia bin/obj completata." -ForegroundColor Green

# -- 2. Pulizia output portable ------------------------------------------
Write-Host "[2/5] Pulizia output portable obsoleti..." -ForegroundColor Yellow
$portableDir = Join-Path $root "portable"
if (-not (Test-Path $portableDir)) {
    New-Item -ItemType Directory -Path $portableDir -Force | Out-Null
}
$portableFiles = @(
    "Live-Speaker-Teleprompter-Portable.exe",
    "Live-Speaker-Teleprompter-Portable.zip",
    "Live-Speaker-Teleprompter-Installer.exe",
    "*.msi"
)
foreach ($pattern in $portableFiles) {
    Get-ChildItem $portableDir -Filter $pattern -ErrorAction SilentlyContinue | ForEach-Object {
        Remove-Item $_.FullName -Force
        Write-Host "  Rimosso: $($_.Name)" -ForegroundColor DarkGray
    }
}
Write-Host "  Pulizia portable completata." -ForegroundColor Green

# -- 3. Genera icona da logo (se PNG presente) ---------------------------
Write-Host "[3/5] Generazione icona da logo..." -ForegroundColor Yellow
$convertScript = Join-Path $root "scripts\convert-logo.ps1"
if (Test-Path $convertScript) {
    & $convertScript
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Icona aggiornata." -ForegroundColor Green
    } else {
        Write-Host "  Logo PNG non trovato, uso icona esistente se presente." -ForegroundColor DarkGray
    }
} else {
    Write-Host "  Script convert-logo.ps1 non trovato, salto." -ForegroundColor DarkGray
}

# -- 4. dotnet restore ---------------------------------------------------
Write-Host "[4/5] dotnet restore..." -ForegroundColor Yellow
Push-Location $root
dotnet restore --nologo -v minimal
if ($LASTEXITCODE -ne 0) {
    Pop-Location
    Write-Error "dotnet restore fallito."
    exit 1
}
Pop-Location
Write-Host "  Restore completato." -ForegroundColor Green

# -- 5. Build completo (publish + portable + installer) ------------------
Write-Host "[5/5] Build completo (publish, portable, installer)..." -ForegroundColor Yellow
& (Join-Path $root "installer\build-installer.ps1")

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build fallito."
    exit 1
}

Write-Host ""
Write-Host "========================================================" -ForegroundColor Green
Write-Host "  Clean & Build completato!" -ForegroundColor Green
Write-Host "  Output in: $portableDir" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Green
Write-Host ""
