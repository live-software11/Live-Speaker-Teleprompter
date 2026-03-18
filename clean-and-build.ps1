# Live Speaker Teleprompter - Clean & Build
# Pulisce tutto l'obsoleto e crea build aggiornate per setup e portable.
# Lancia dalla root del progetto: .\clean-and-build.ps1

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host ""
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  Live Speaker Teleprompter - Clean and Build" -ForegroundColor Cyan
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

# -- 2. Pulizia output release e rimozione cartella portable obsoleta ----
Write-Host "[2/5] Pulizia output release..." -ForegroundColor Yellow
$releaseDir = Join-Path $root "release"
if (-not (Test-Path $releaseDir)) {
    New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
}
$releaseFiles = @(
    "Live_Speaker_Teleprompter_Portable.exe",
    "Live_Speaker_Teleprompter_Portable_ITA.exe",
    "Live_Speaker_Teleprompter_Portable_ENG.exe",
    "Live_Speaker_Teleprompter_Portable.zip",
    "Live_Speaker_Teleprompter_Setup.exe",
    "Live-Speaker-Teleprompter-*",
    "*.msi"
)
foreach ($pattern in $releaseFiles) {
    Get-ChildItem $releaseDir -Filter $pattern -ErrorAction SilentlyContinue | ForEach-Object {
        Remove-Item $_.FullName -Force
        Write-Host "  Rimosso: $($_.Name)" -ForegroundColor DarkGray
    }
}
# Rimuovi cartella portable obsoleta (migrazione a release)
$obsoletePortable = Join-Path $root "portable"
if (Test-Path $obsoletePortable) {
    $prefsPath = Join-Path $obsoletePortable "preferences.json"
    if (Test-Path $prefsPath) {
        Copy-Item $prefsPath (Join-Path $releaseDir "preferences.json") -Force -ErrorAction SilentlyContinue
    }
    Remove-Item $obsoletePortable -Recurse -Force
    Write-Host "  Rimossa cartella obsoleta: portable\" -ForegroundColor DarkGray
}
Write-Host "  Pulizia release completata." -ForegroundColor Green

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
Write-Host "  Clean and Build completato!" -ForegroundColor Green
Write-Host "  Output in: $releaseDir (2 file: Portable.exe + Setup.exe)" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Green
Write-Host ""
