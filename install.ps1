$ErrorActionPreference = 'Stop'
$appRoot = if ((Split-Path $PSScriptRoot -Leaf) -eq '_Projekt') { Split-Path $PSScriptRoot -Parent } else { $PSScriptRoot }
$exe = Join-Path $appRoot 'GLYPHLUME.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw 'GLYPHLUME.exe fehlt. Zuerst build.ps1 ausfuehren.' }
$installProcess = Start-Process -FilePath $exe -ArgumentList '--install' -WindowStyle Hidden -Wait -PassThru
if ($installProcess.ExitCode -ne 0) { throw 'Kontextmenue konnte nicht eingerichtet werden.' }
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Desktop')) 'GLYPHLUME.lnk'
$wsh = New-Object -ComObject WScript.Shell
try {
    $shortcut = $wsh.CreateShortcut($shortcutPath)
    try {
        if ($shortcut.TargetPath -and $shortcut.TargetPath -notin @($exe,(Join-Path $appRoot 'IconStudio.exe'))) { throw 'Desktop shortcut belongs to another installation.' }
        $shortcut.TargetPath = $exe
        $shortcut.WorkingDirectory = $appRoot
        $shortcut.IconLocation = "$exe,0"
        $shortcut.Description = 'Prompt, KI-Vorschau und echter Windows-ICO-Export mit Codex'
        $shortcut.Save()
    } finally { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($shortcut) }
} finally { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($wsh) }
Write-Output "Installiert: $exe"
Write-Output "Desktop: $shortcutPath"
