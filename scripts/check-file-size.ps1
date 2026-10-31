#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Fails when any C# file exceeds the hard line limit (ENGINEERING_STANDARDS §1).
.DESCRIPTION
  Hard limit 300 lines, soft limit 200 (warning only). Files listed in
  scripts/file-size-exceptions.txt are reported but do not fail the check; each
  entry names the roadmap task that removes it.
#>
[CmdletBinding()]
param(
    [int]$HardLimit = 300,
    [int]$SoftLimit = 200,
    [string[]]$Roots = @('src', 'tests'),
    [string]$ExceptionsFile = 'scripts/file-size-exceptions.txt'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$exceptions = @{}
if (Test-Path $ExceptionsFile) {
    Get-Content $ExceptionsFile | ForEach-Object {
        $line = $_.Trim()
        if ($line -and -not $line.StartsWith('#')) {
            $parts = $line -split '\s+', 2
            $exceptions[$parts[0].Replace('\', '/')] = if ($parts.Count -gt 1) { $parts[1] } else { '' }
        }
    }
}

$failures = 0
$files = Get-ChildItem -Path $Roots -Recurse -Filter '*.cs' -File |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }

foreach ($file in $files) {
    $lines = @(Get-Content $file.FullName).Count
    $relative = [System.IO.Path]::GetRelativePath($repoRoot, $file.FullName).Replace('\', '/')

    if ($lines -gt $HardLimit) {
        if ($exceptions.ContainsKey($relative)) {
            Write-Host "EXEMPT  $relative ($lines lines): $($exceptions[$relative])"
        }
        else {
            Write-Host "FAIL    $relative has $lines lines (hard limit $HardLimit)"
            $failures++
        }
    }
    elseif ($lines -gt $SoftLimit) {
        Write-Host "WARN    $relative has $lines lines (soft limit $SoftLimit)"
    }
}

Write-Host "Checked $($files.Count) files."
if ($failures -gt 0) {
    Write-Host "File-size check failed: $failures file(s) over $HardLimit lines."
    exit 1
}
Write-Host 'File-size check passed.'
