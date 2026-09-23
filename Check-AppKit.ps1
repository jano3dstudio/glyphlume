# Read-only preparation check; no build, update or profile access.
[CmdletBinding()]
param([switch]$Json)
$ErrorActionPreference = 'Stop'
$plan = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'app-kit.plan.json') -Raw | ConvertFrom-Json
$kit = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot $plan.kitSource))
& (Join-Path $kit 'scripts/check-adoption.ps1') -App $plan.app -Json:$Json
if (-not $?) { exit 1 }
