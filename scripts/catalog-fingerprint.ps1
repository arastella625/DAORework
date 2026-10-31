#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Prints the fingerprints a known-items catalog entry needs (catalog/README.md) from mod downloads.
.DESCRIPTION
  Reads every .zip and .dazip under -Path without installing or extracting anything. For a .dazip (or a .dazip
  inside a Nexus .zip) it prints the AddInItem UID and Name from Manifest.xml and the add-in folders it contains;
  for a plain zip it prints the file paths, so an override mod can be recognized by a distinctive file. The
  SHA-256 of each archive is printed so the source can be checked again later. A .7z or .rar download has to be
  extracted first; a folder under -Path with no archives in it is listed file by file.
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File .\scripts\catalog-fingerprint.ps1 -Path "$env:USERPROFILE\Downloads\dao-mods"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$Path,
    [int]$MaxFiles = 40
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Show-Manifest([System.IO.Compression.ZipArchive]$zip, [string]$indent) {
    $manifest = $zip.Entries | Where-Object { $_.FullName -match '(^|/)manifest\.xml$' } | Select-Object -First 1
    if (-not $manifest) { return $false }
    $reader = [System.IO.StreamReader]::new($manifest.Open())
    try { [xml]$xml = $reader.ReadToEnd() } finally { $reader.Dispose() }
    foreach ($item in $xml.SelectNodes('//AddInItem')) {
        Write-Host "${indent}AddInItem UID=""$($item.UID)"" Name=""$($item.Name)"" ExtendedModuleUID=""$($item.ExtendedModuleUID)""" -ForegroundColor Green
    }
    $folders = $zip.Entries | ForEach-Object { $_.FullName -replace '\\', '/' } |
        Where-Object { $_ -match '(?i)^contents/(addins|packages)/[^/]+/' } |
        ForEach-Object { ($_ -split '/')[0..2] -join '/' } | Sort-Object -Unique
    foreach ($folder in $folders) { Write-Host "${indent}folder $folder" }
    return $true
}

function Show-Files([System.IO.Compression.ZipArchive]$zip, [string]$indent) {
    $files = $zip.Entries | Where-Object { $_.Name } | ForEach-Object { $_.FullName -replace '\\', '/' }
    Write-Host "${indent}$($files.Count) files" -ForegroundColor Yellow
    $files | Select-Object -First $MaxFiles | ForEach-Object { Write-Host "${indent}  $_" }
    if ($files.Count -gt $MaxFiles) { Write-Host "${indent}  ... ($($files.Count - $MaxFiles) more)" }
}

function Show-Archive([System.IO.Stream]$stream, [string]$indent) {
    $zip = [System.IO.Compression.ZipArchive]::new($stream, [System.IO.Compression.ZipArchiveMode]::Read)
    try {
        $nested = $zip.Entries | Where-Object { $_.Name -match '(?i)\.dazip$' }
        foreach ($entry in $nested) {
            Write-Host "${indent}contains $($entry.FullName)" -ForegroundColor Cyan
            $buffer = [System.IO.MemoryStream]::new()
            $inner = $entry.Open()
            try { $inner.CopyTo($buffer) } finally { $inner.Dispose() }
            $buffer.Position = 0
            Show-Archive $buffer "$indent    "
        }
        if (-not $nested -and -not (Show-Manifest $zip $indent)) { Show-Files $zip $indent }
    }
    finally { $zip.Dispose() }
}

$archives = @(Get-ChildItem -Path (Join-Path $Path '*') -Recurse -File -Include *.zip, *.dazip)
$folders = @(Get-ChildItem -LiteralPath $Path -Directory |
    Where-Object { -not (Get-ChildItem -Path (Join-Path $_.FullName '*') -Recurse -File -Include *.zip, *.dazip) })
if (-not $archives -and -not $folders) { throw "No .zip, .dazip or extracted mod folder under $Path." }
foreach ($archive in $archives) {
    $hash = (Get-FileHash -LiteralPath $archive.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Host ''
    Write-Host "== $($archive.Name) ($($archive.Length) bytes, sha256 $hash)" -ForegroundColor Cyan
    $stream = [System.IO.File]::OpenRead($archive.FullName)
    try { Show-Archive $stream '  ' }
    catch { Write-Host "  could not read: $($_.Exception.Message)" -ForegroundColor Red }
    finally { $stream.Dispose() }
}

foreach ($folder in $folders) {
    Write-Host ''
    Write-Host "== folder $($folder.Name)" -ForegroundColor Cyan
    $files = Get-ChildItem -LiteralPath $folder.FullName -Recurse -File |
        ForEach-Object { $_.FullName.Substring($folder.FullName.Length + 1) -replace '\\', '/' }
    Write-Host "  $($files.Count) files" -ForegroundColor Yellow
    $files | Select-Object -First $MaxFiles | ForEach-Object { Write-Host "    $_" }
    if ($files.Count -gt $MaxFiles) { Write-Host "    ... ($($files.Count - $MaxFiles) more)" }
}
