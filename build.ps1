param([string]$OutputPath)
$ErrorActionPreference = 'Stop'
$appRoot = if ((Split-Path $PSScriptRoot -Leaf) -eq '_Projekt') { Split-Path $PSScriptRoot -Parent } else { $PSScriptRoot }
if (-not $OutputPath) { $OutputPath = Join-Path $appRoot 'GLYPHLUME.exe' }
$OutputPath = [IO.Path]::GetFullPath($OutputPath)
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$icon = Join-Path $appRoot 'assets\TGC_SEP26_2.ico'
if (-not (Test-Path -LiteralPath $icon)) { $icon = Join-Path $PSScriptRoot 'TGC_SEP26_2.ico' }
Push-Location $PSScriptRoot
try {
    & $compiler /nologo /target:winexe /platform:anycpu /optimize+ "/out:$OutputPath" /win32manifest:app.manifest "/win32icon:$icon" /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Core.dll /reference:System.Web.Extensions.dll /reference:Microsoft.CSharp.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll IconStudio.cs Localization.cs Authentication.cs Styles.cs Libraries.cs WindowChrome.cs IconOptions.cs
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
    Write-Output "Built: $OutputPath"
} finally { Pop-Location }
