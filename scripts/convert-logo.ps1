# Converte Logo Teleprompter.png in app-icon.ico
# Cerca il PNG in: icons/, root progetto, cartella parent
# Esegui dalla root: .\scripts\convert-logo.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$parentRoot = Split-Path -Parent $root
$candidates = @(
    (Join-Path $root "icons\Logo Teleprompter.png"),
    (Join-Path $root "Logo Teleprompter.png"),
    (Join-Path $parentRoot "Logo Teleprompter.png")
)
$pngPath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
$icoPath = Join-Path $root "icons\app-icon.ico"

if (-not $pngPath -or -not (Test-Path $pngPath)) {
    Write-Host "ERRORE: PNG non trovato." -ForegroundColor Red
    Write-Host "Cerca in: $($candidates -join ', ')" -ForegroundColor Yellow
    Write-Host "Inserisci 'Logo Teleprompter.png' in icons/, root del progetto o cartella parent." -ForegroundColor Yellow
    exit 1
}

$iconsDir = Split-Path -Parent $icoPath
if (-not (Test-Path $iconsDir)) {
    New-Item -ItemType Directory -Path $iconsDir -Force | Out-Null
}

$converterDir = Join-Path $PSScriptRoot "PngToIco"
Push-Location $converterDir
try {
    dotnet run -- $pngPath $icoPath
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Icona creata: $icoPath" -ForegroundColor Green
    } else {
        exit $LASTEXITCODE
    }
} finally {
    Pop-Location
}
