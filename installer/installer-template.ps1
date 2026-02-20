# R-Speaker Teleprompter Installer
# Self-extracting, self-destructing

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = "Stop"
$appName = "R-Speaker Teleprompter"
$publisherName = "R-Speaker"

# ── UI ──
$form = New-Object System.Windows.Forms.Form
$form.Text = "$appName - Installazione"
$form.Size = New-Object System.Drawing.Size(520, 340)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox = $false
$form.MinimizeBox = $false
$form.BackColor = [System.Drawing.Color]::FromArgb(15, 23, 42)

$titleLabel = New-Object System.Windows.Forms.Label
$titleLabel.Location = New-Object System.Drawing.Point(20, 20)
$titleLabel.Size = New-Object System.Drawing.Size(460, 30)
$titleLabel.Text = "Installazione $appName"
$titleLabel.Font = New-Object System.Drawing.Font("Segoe UI", 16, [System.Drawing.FontStyle]::Bold)
$titleLabel.ForeColor = [System.Drawing.Color]::White
$form.Controls.Add($titleLabel)

$verLabel = New-Object System.Windows.Forms.Label
$verLabel.Location = New-Object System.Drawing.Point(20, 55)
$verLabel.Size = New-Object System.Drawing.Size(460, 20)
$verLabel.Text = "Versione 2.1.0 - Applicazione autonoma (nessun runtime richiesto)"
$verLabel.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$verLabel.ForeColor = [System.Drawing.Color]::FromArgb(148, 163, 184)
$form.Controls.Add($verLabel)

$infoLabel = New-Object System.Windows.Forms.Label
$infoLabel.Location = New-Object System.Drawing.Point(20, 85)
$infoLabel.Size = New-Object System.Drawing.Size(460, 25)
$infoLabel.Text = "Seleziona la cartella di installazione:"
$infoLabel.Font = New-Object System.Drawing.Font("Segoe UI", 10)
$infoLabel.ForeColor = [System.Drawing.Color]::FromArgb(203, 213, 225)
$form.Controls.Add($infoLabel)

$pathTextBox = New-Object System.Windows.Forms.TextBox
$pathTextBox.Location = New-Object System.Drawing.Point(20, 115)
$pathTextBox.Size = New-Object System.Drawing.Size(380, 25)
$pathTextBox.Text = "$env:LOCALAPPDATA\$publisherName\$appName"
$pathTextBox.Font = New-Object System.Drawing.Font("Segoe UI", 10)
$pathTextBox.BackColor = [System.Drawing.Color]::FromArgb(30, 41, 59)
$pathTextBox.ForeColor = [System.Drawing.Color]::White
$pathTextBox.BorderStyle = "FixedSingle"
$form.Controls.Add($pathTextBox)

$browseButton = New-Object System.Windows.Forms.Button
$browseButton.Location = New-Object System.Drawing.Point(410, 113)
$browseButton.Size = New-Object System.Drawing.Size(75, 28)
$browseButton.Text = "Sfoglia..."
$browseButton.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$browseButton.FlatStyle = "Flat"
$browseButton.BackColor = [System.Drawing.Color]::FromArgb(59, 130, 246)
$browseButton.ForeColor = [System.Drawing.Color]::White
$browseButton.FlatAppearance.BorderSize = 0
$browseButton.Add_Click({
    $fb = New-Object System.Windows.Forms.FolderBrowserDialog
    $fb.Description = "Seleziona la cartella di installazione"
    $fb.SelectedPath = $pathTextBox.Text
    if ($fb.ShowDialog() -eq "OK") { $pathTextBox.Text = $fb.SelectedPath }
})
$form.Controls.Add($browseButton)

$startMenuCb = New-Object System.Windows.Forms.CheckBox
$startMenuCb.Location = New-Object System.Drawing.Point(20, 155)
$startMenuCb.Size = New-Object System.Drawing.Size(460, 25)
$startMenuCb.Text = "Crea collegamento nel menu Start"
$startMenuCb.Checked = $true
$startMenuCb.Font = New-Object System.Drawing.Font("Segoe UI", 10)
$startMenuCb.ForeColor = [System.Drawing.Color]::FromArgb(203, 213, 225)
$startMenuCb.FlatStyle = "Flat"
$form.Controls.Add($startMenuCb)

$desktopCb = New-Object System.Windows.Forms.CheckBox
$desktopCb.Location = New-Object System.Drawing.Point(20, 185)
$desktopCb.Size = New-Object System.Drawing.Size(460, 25)
$desktopCb.Text = "Crea collegamento sul desktop"
$desktopCb.Checked = $false
$desktopCb.Font = New-Object System.Drawing.Font("Segoe UI", 10)
$desktopCb.ForeColor = [System.Drawing.Color]::FromArgb(203, 213, 225)
$desktopCb.FlatStyle = "Flat"
$form.Controls.Add($desktopCb)

$installButton = New-Object System.Windows.Forms.Button
$installButton.Location = New-Object System.Drawing.Point(290, 240)
$installButton.Size = New-Object System.Drawing.Size(95, 38)
$installButton.Text = "Installa"
$installButton.Font = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$installButton.FlatStyle = "Flat"
$installButton.BackColor = [System.Drawing.Color]::FromArgb(34, 197, 94)
$installButton.ForeColor = [System.Drawing.Color]::White
$installButton.FlatAppearance.BorderSize = 0
$installButton.DialogResult = "OK"
$form.Controls.Add($installButton)
$form.AcceptButton = $installButton

$cancelButton = New-Object System.Windows.Forms.Button
$cancelButton.Location = New-Object System.Drawing.Point(395, 240)
$cancelButton.Size = New-Object System.Drawing.Size(90, 38)
$cancelButton.Text = "Annulla"
$cancelButton.Font = New-Object System.Drawing.Font("Segoe UI", 10)
$cancelButton.FlatStyle = "Flat"
$cancelButton.BackColor = [System.Drawing.Color]::FromArgb(71, 85, 105)
$cancelButton.ForeColor = [System.Drawing.Color]::White
$cancelButton.FlatAppearance.BorderSize = 0
$cancelButton.DialogResult = "Cancel"
$form.Controls.Add($cancelButton)
$form.CancelButton = $cancelButton

$result = $form.ShowDialog()
if ($result -ne "OK") { exit 0 }

$installPath = $pathTextBox.Text
$createStartMenu = $startMenuCb.Checked
$createDesktop = $desktopCb.Checked

try {
    # Create install directory
    if (-not (Test-Path $installPath)) {
        New-Item -ItemType Directory -Path $installPath -Force | Out-Null
    }

    # Extract embedded payload
    $zipBase64 = "##EMBEDDED_ZIP##"
    $zipBytes = [System.Convert]::FromBase64String($zipBase64)
    $zipTemp = Join-Path $env:TEMP "r-speaker-install-payload.zip"
    [System.IO.File]::WriteAllBytes($zipTemp, $zipBytes)
    Expand-Archive -Path $zipTemp -DestinationPath $installPath -Force
    Remove-Item $zipTemp -Force -ErrorAction SilentlyContinue

    # Find the main exe
    $mainExe = Get-ChildItem $installPath -Filter "*.exe" | Select-Object -First 1
    if (-not $mainExe) { throw "Executable not found after extraction." }

    # Start Menu shortcut
    if ($createStartMenu) {
        $smDir = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\$publisherName"
        if (-not (Test-Path $smDir)) { New-Item -ItemType Directory -Path $smDir -Force | Out-Null }
        $ws = New-Object -ComObject WScript.Shell
        $sc = $ws.CreateShortcut("$smDir\$appName.lnk")
        $sc.TargetPath = $mainExe.FullName
        $sc.WorkingDirectory = $installPath
        $sc.Description = "$appName - Professional Teleprompter"
        $sc.Save()
    }

    # Desktop shortcut
    if ($createDesktop) {
        $ws = New-Object -ComObject WScript.Shell
        $desktopPath = [Environment]::GetFolderPath('Desktop')
        $sc = $ws.CreateShortcut("$desktopPath\$appName.lnk")
        $sc.TargetPath = $mainExe.FullName
        $sc.WorkingDirectory = $installPath
        $sc.Description = "$appName - Professional Teleprompter"
        $sc.Save()
    }

    # Create uninstaller script
    $uninstallContent = @"
Remove-Item "$installPath" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\$publisherName" -Recurse -Force -ErrorAction SilentlyContinue
`$desktopLnk = Join-Path ([Environment]::GetFolderPath('Desktop')) "$appName.lnk"
Remove-Item `$desktopLnk -Force -ErrorAction SilentlyContinue
Write-Host "$appName disinstallato." -ForegroundColor Green
"@
    $uninstallContent | Out-File -FilePath (Join-Path $installPath "Uninstall.ps1") -Encoding UTF8

    [System.Windows.Forms.MessageBox]::Show(
        "Installazione completata!`n`nPercorso: $installPath",
        $appName, "OK", "Information") | Out-Null

    # Launch?
    $launch = [System.Windows.Forms.MessageBox]::Show(
        "Vuoi avviare $appName ora?", $appName, "YesNo", "Question")
    if ($launch -eq "Yes") {
        Start-Process $mainExe.FullName
    }
} catch {
    [System.Windows.Forms.MessageBox]::Show(
        "Errore: $($_.Exception.Message)", "$appName - Errore", "OK", "Error") | Out-Null
    exit 1
}
