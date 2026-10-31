#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Prints the CHANGELOG.md section for one version, for the GitHub release body.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$Version,
    [string]$ChangelogPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'CHANGELOG.md')
)

$ErrorActionPreference = 'Stop'
$lines = Get-Content $ChangelogPath
$start = [array]::FindIndex($lines, [Predicate[string]] { param($l) $l -match "^## \[$([regex]::Escape($Version))\]" })
if ($start -lt 0) { throw "CHANGELOG.md has no '## [$Version]' section." }
$end = [array]::FindIndex($lines, $start + 1, [Predicate[string]] { param($l) $l -match '^## \[' -or $l -match '^\[[^\]]+\]: ' })
if ($end -lt 0) { $end = $lines.Count }
($lines[($start + 1)..($end - 1)] -join "`n").Trim()
