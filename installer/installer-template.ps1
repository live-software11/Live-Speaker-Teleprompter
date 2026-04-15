# Live Speaker Teleprompter Installer
# Self-extracting, self-destructing

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = "Stop"
$appName = "Live Speaker Teleprompter"
$publisherName = "Live Speaker"

# Installer UI language: English (i18n-installer.mdc - lingua ufficiale installer)
$installLang = "en"

# Strings based on language
$L = @{
    it = @{
        Title = "Installazione"
        TitleFull = "Installazione $appName"
        Version = "Versione 2.3.3 - Applicazione autonoma (nessun runtime richiesto)"
        SelectFolder = "Seleziona la cartella di installazione:"
        Browse = "Sfoglia..."
        BrowseDesc = "Seleziona la cartella di installazione"
        StartMenu = "Crea collegamento nel menu Start"
        Desktop = "Crea collegamento sul desktop"
        Install = "Installa"
        Cancel = "Annulla"
        Done = "Installazione completata!"
        Path = "Percorso"
        LaunchQ = "Vuoi avviare $appName ora?"
        Error = "Errore"
        Uninstalled = "$appName disinstallato."
    }
    en = @{
        Title = "Installation"
        TitleFull = "Install $appName"
        Version = "Version 2.3.3 - Standalone application (no runtime required)"
        SelectFolder = "Select installation folder:"
        Browse = "Browse..."
        BrowseDesc = "Select installation folder"
        StartMenu = "Create Start menu shortcut"
        Desktop = "Create desktop shortcut"
        Install = "Install"
        Cancel = "Cancel"
        Done = "Installation complete!"
        Path = "Path"
        LaunchQ = "Do you want to launch $appName now?"
        Error = "Error"
        Uninstalled = "$appName uninstalled."
    }
}
$s = $L[$installLang]

# ── UI ──
$form = New-Object System.Windows.Forms.Form
$form.Text = "$appName - $($s.Title)"
# Icona finestra (broadcast-style, coerente con branding)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$iconPath = Join-Path $scriptDir "app-icon.ico"
if (Test-Path $iconPath) {
    try { $form.Icon = [System.Drawing.Icon]::new($iconPath) } catch { }
}
$form.Size = New-Object System.Drawing.Size(520, 340)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox = $false
$form.MinimizeBox = $false
$form.BackColor = [System.Drawing.Color]::FromArgb(15, 23, 42)

$titleLabel = New-Object System.Windows.Forms.Label
$titleLabel.Location = New-Object System.Drawing.Point(20, 20)
$titleLabel.Size = New-Object System.Drawing.Size(460, 30)
$titleLabel.Text = $s.TitleFull
$titleLabel.Font = New-Object System.Drawing.Font("Segoe UI", 16, [System.Drawing.FontStyle]::Bold)
$titleLabel.ForeColor = [System.Drawing.Color]::White
$form.Controls.Add($titleLabel)

$verLabel = New-Object System.Windows.Forms.Label
$verLabel.Location = New-Object System.Drawing.Point(20, 55)
$verLabel.Size = New-Object System.Drawing.Size(460, 20)
$verLabel.Text = $s.Version
$verLabel.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$verLabel.ForeColor = [System.Drawing.Color]::FromArgb(148, 163, 184)
$form.Controls.Add($verLabel)

$infoLabel = New-Object System.Windows.Forms.Label
$infoLabel.Location = New-Object System.Drawing.Point(20, 85)
$infoLabel.Size = New-Object System.Drawing.Size(460, 25)
$infoLabel.Text = $s.SelectFolder
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
$browseButton.Text = $s.Browse
$browseButton.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$browseButton.FlatStyle = "Flat"
$browseButton.BackColor = [System.Drawing.Color]::FromArgb(59, 130, 246)
$browseButton.ForeColor = [System.Drawing.Color]::White
$browseButton.FlatAppearance.BorderSize = 0
$browseButton.Add_Click({
    $fb = New-Object System.Windows.Forms.FolderBrowserDialog
    $fb.Description = $s.BrowseDesc
    $fb.SelectedPath = $pathTextBox.Text
    if ($fb.ShowDialog() -eq "OK") { $pathTextBox.Text = $fb.SelectedPath }
})
$form.Controls.Add($browseButton)

$startMenuCb = New-Object System.Windows.Forms.CheckBox
$startMenuCb.Location = New-Object System.Drawing.Point(20, 155)
$startMenuCb.Size = New-Object System.Drawing.Size(460, 25)
$startMenuCb.Text = $s.StartMenu
$startMenuCb.Checked = $true
$startMenuCb.Font = New-Object System.Drawing.Font("Segoe UI", 10)
$startMenuCb.ForeColor = [System.Drawing.Color]::FromArgb(203, 213, 225)
$startMenuCb.FlatStyle = "Flat"
$form.Controls.Add($startMenuCb)

$desktopCb = New-Object System.Windows.Forms.CheckBox
$desktopCb.Location = New-Object System.Drawing.Point(20, 185)
$desktopCb.Size = New-Object System.Drawing.Size(460, 25)
$desktopCb.Text = $s.Desktop
$desktopCb.Checked = $false
$desktopCb.Font = New-Object System.Drawing.Font("Segoe UI", 10)
$desktopCb.ForeColor = [System.Drawing.Color]::FromArgb(203, 213, 225)
$desktopCb.FlatStyle = "Flat"
$form.Controls.Add($desktopCb)

$installButton = New-Object System.Windows.Forms.Button
$installButton.Location = New-Object System.Drawing.Point(290, 240)
$installButton.Size = New-Object System.Drawing.Size(95, 38)
$installButton.Text = $s.Install
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
$cancelButton.Text = $s.Cancel
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
    # Upgrade: kill running instance and clean old files (license data in
    # %LOCALAPPDATA%\com.livesoftware.live-speaker-teleprompter is NOT touched)
    $existingExe = Join-Path $installPath "$appName.exe"
    if (Test-Path $existingExe) {
        Get-Process | Where-Object {
            $_.Path -and ($_.Path -eq $existingExe -or $_.Name -eq $appName)
        } | Stop-Process -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 500
        Get-ChildItem $installPath -File | Remove-Item -Force -ErrorAction SilentlyContinue
    }

    # Create install directory
    if (-not (Test-Path $installPath)) {
        New-Item -ItemType Directory -Path $installPath -Force | Out-Null
    }

    # Extract embedded payload
    $zipBase64 = "##EMBEDDED_ZIP##"
    $zipBytes = [System.Convert]::FromBase64String($zipBase64)
    $zipTemp = Join-Path $env:TEMP "live-speaker-install-payload.zip"
    [System.IO.File]::WriteAllBytes($zipTemp, $zipBytes)
    Expand-Archive -Path $zipTemp -DestinationPath $installPath -Force
    Remove-Item $zipTemp -Force -ErrorAction SilentlyContinue

    # Find the main exe
    $mainExe = Get-ChildItem $installPath -Filter "*.exe" | Select-Object -First 1
    if (-not $mainExe) { throw "Executable not found after extraction." }

    # Start Menu shortcut (app)
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

    # Create uninstaller script (must remove registry BEFORE deleting folder)
    # Rilascio licenza: prima di rimuovere la cartella, esegue l'exe con --deactivate
    # (chiama /api/deactivate su Live WORKS, poi cancella il file locale in LOCALAPPDATA).
    $uninstallPs1Path = Join-Path $installPath "Uninstall.ps1"
    $uninstallContent = @"
# Live Speaker Teleprompter - Uninstaller
`$regPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\LiveSpeakerTeleprompter"
if (Test-Path `$regPath) { Remove-Item `$regPath -Recurse -Force -ErrorAction SilentlyContinue }
Remove-Item "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\$publisherName" -Recurse -Force -ErrorAction SilentlyContinue
`$desktopLnk = Join-Path ([Environment]::GetFolderPath('Desktop')) "$appName.lnk"
Remove-Item `$desktopLnk -Force -ErrorAction SilentlyContinue
# Rilascio licenza (best-effort: timeout 30s)
`$mainExePath = Join-Path "$installPath" "$appName.exe"
if (Test-Path `$mainExePath) {
    try {
        `$deactProc = Start-Process -FilePath `$mainExePath -ArgumentList "--deactivate" -PassThru -WindowStyle Hidden
        if (`$deactProc) { `$deactProc | Wait-Process -Timeout 30 -ErrorAction SilentlyContinue }
    } catch { }
}
Remove-Item "$installPath" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "$($s.Uninstalled)" -ForegroundColor Green
"@
    $uninstallContent | Out-File -FilePath $uninstallPs1Path -Encoding UTF8

    # Shortcut "Disinstalla" nel menu Start (se creato)
    if ($createStartMenu) {
        $smDir = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\$publisherName"
        $uninstallLabel = if ($installLang -eq "it") { "Disinstalla $appName" } else { "Uninstall $appName" }
        $ws = New-Object -ComObject WScript.Shell
        $sc = $ws.CreateShortcut("$smDir\$uninstallLabel.lnk")
        $sc.TargetPath = "powershell.exe"
        $sc.Arguments = "-ExecutionPolicy Bypass -WindowStyle Normal -File `"$uninstallPs1Path`""
        $sc.WorkingDirectory = $installPath
        $sc.Description = $uninstallLabel
        $sc.Save()
    }

    # Register in Windows Add/Remove Programs (Apps & Features)
    $regPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\LiveSpeakerTeleprompter"
    New-Item -Path $regPath -Force | Out-Null
    Set-ItemProperty -Path $regPath -Name "DisplayName" -Value $appName
    Set-ItemProperty -Path $regPath -Name "DisplayVersion" -Value "2.3.3"
    Set-ItemProperty -Path $regPath -Name "Publisher" -Value $publisherName
    Set-ItemProperty -Path $regPath -Name "InstallLocation" -Value $installPath
    Set-ItemProperty -Path $regPath -Name "UninstallString" -Value "powershell.exe -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$uninstallPs1Path`""
    Set-ItemProperty -Path $regPath -Name "DisplayIcon" -Value "$($mainExe.FullName)"
    Set-ItemProperty -Path $regPath -Name "NoModify" -Value 1 -Type DWord
    Set-ItemProperty -Path $regPath -Name "NoRepair" -Value 1 -Type DWord

    [System.Windows.Forms.MessageBox]::Show(
        "$($s.Done)`n`n$($s.Path): $installPath",
        $appName, "OK", "Information") | Out-Null

    # Launch?
    $launch = [System.Windows.Forms.MessageBox]::Show(
        $s.LaunchQ, $appName, "YesNo", "Question")
    if ($launch -eq "Yes") {
        Start-Process $mainExe.FullName
    }
} catch {
    [System.Windows.Forms.MessageBox]::Show(
        "$($s.Error): $($_.Exception.Message)", "$appName - $($s.Error)", "OK", "Error") | Out-Null
    exit 1
}
