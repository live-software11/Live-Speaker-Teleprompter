# Live Speaker Teleprompter - Portable Extractor
# Extracts exe + install-language.txt and launches the app

$ErrorActionPreference = "Stop"
$extractDir = $PSScriptRoot
if ([string]::IsNullOrEmpty($extractDir)) { $extractDir = (Get-Location).Path }

$zipBase64 = "##EMBEDDED_ZIP##"
$zipBytes = [System.Convert]::FromBase64String($zipBase64)
$zipTemp = Join-Path $env:TEMP "live-speaker-portable-" + [guid]::NewGuid().ToString('N').Substring(0,8) + ".zip"
try {
    [System.IO.File]::WriteAllBytes($zipTemp, $zipBytes)
    Expand-Archive -Path $zipTemp -DestinationPath $extractDir -Force
} finally {
    Remove-Item $zipTemp -Force -ErrorAction SilentlyContinue
}

$exePath = Join-Path $extractDir "Live Speaker Teleprompter.exe"
Start-Process -FilePath $exePath
