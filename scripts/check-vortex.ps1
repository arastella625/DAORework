#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Reports how Vortex has set up Dragon Age: Origins on this PC, for ADR-0028 (taking over from Vortex). Read-only.
.EXAMPLE
  .\scripts\check-vortex.ps1
  .\scripts\check-vortex.ps1 -Documents "D:\Documents\BioWare\Dragon Age" -Staging "E:\Vortex Mods\dragonage"
.NOTES
  It only reads. It never changes a file, and it never opens a mod's own files: it lists Vortex's record files
  (vortex.deployment*), the fields they hold, which mods they name and how many files each placed, whether a placed
  file is a link, the names of the staging folder's mod folders, and how many add-ins AddIns.xml lists. Your user
  name and profile folder are replaced by %USERPROFILE% in the report. Paste the report (vortex-report.txt) into
  the conversation or the task's issue.
#>
[CmdletBinding()]
param(
    [string]$Documents = (Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'BioWare\Dragon Age'),
    [string]$Staging = (Join-Path $env:APPDATA 'Vortex\dragonage\mods'),
    [string]$Report = (Join-Path (Get-Location) 'vortex-report.txt')
)

$ErrorActionPreference = 'Stop'
$lines = [System.Collections.Generic.List[string]]::new()
$stagingPaths = [System.Collections.Generic.List[string]]::new()

function Hide([string]$text) {
    if (-not $text) { return $text }
    if ($env:USERPROFILE) { $text = $text.Replace($env:USERPROFILE, '%USERPROFILE%') }
    if ($env:USERNAME -and $env:USERNAME.Length -ge 3) { $text = $text.Replace($env:USERNAME, '<user>') }
    return $text
}
function Say([string]$text = '') { $lines.Add((Hide $text)); Write-Host (Hide $text) }

function Show-Manifest([System.IO.FileInfo]$file) {
    Say "Record file: $($file.FullName) ($($file.Length) bytes, changed $($file.LastWriteTime.ToString('yyyy-MM-dd HH:mm')))"
    if ($file.Extension -ne '.json') { Say '  Not JSON; only its name and size are reported.'; return }
    try { $manifest = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json }
    catch { Say "  Could not be read as JSON: $($_.Exception.Message)"; return }

    if ($manifest.stagingPath) { $script:stagingPaths.Add([string]$manifest.stagingPath) }
    foreach ($property in $manifest.PSObject.Properties) {
        if ($property.Value -is [System.Array]) { Say "  $($property.Name): a list of $($property.Value.Count)" }
        elseif ($property.Value -is [System.Management.Automation.PSCustomObject]) { Say "  $($property.Name): an object" }
        else { Say "  $($property.Name): $($property.Value)" }
    }
    $files = @($manifest.files)
    if ($files.Count -eq 0) { return }
    Say "  Fields of a file entry: $(($files[0].PSObject.Properties.Name) -join ', ')"
    Say '  Mods named (source), with how many files each placed:'
    $files | Group-Object -Property source | Sort-Object Name | ForEach-Object { Say "    $($_.Name): $($_.Count)" }
    $merged = @($files | Where-Object { $_.merged })
    if ($merged.Count -gt 0) { Say "  Entries with 'merged': $($merged.Count), for example $($merged[0].relPath)" }
    Say '  The first five placed files, and whether each is still a link:'
    foreach ($entry in ($files | Select-Object -First 5)) {
        $placed = Join-Path $file.DirectoryName $entry.relPath
        if (-not (Test-Path -LiteralPath $placed)) { Say "    $($entry.relPath): missing"; continue }
        $item = Get-Item -LiteralPath $placed -Force
        $kind = if ($item.LinkType) { $item.LinkType } else { 'a plain file' }
        Say "    $($entry.relPath): $kind"
    }
}

Say "check-vortex.ps1, $(Get-Date -Format 'yyyy-MM-dd HH:mm'), PowerShell $($PSVersionTable.PSVersion)"
Say "Documents folder: $Documents (exists: $(Test-Path -LiteralPath $Documents))"
Say ''

Say '== Vortex record files in the documents folder (three folders deep) =='
$found = @()
if (Test-Path -LiteralPath $Documents) {
    $found = @(Get-ChildItem -LiteralPath $Documents -Recurse -Depth 3 -Force -File -Filter 'vortex.deployment*' -ErrorAction SilentlyContinue)
}
if ($found.Count -eq 0) { Say 'None found.' }
foreach ($file in $found) { Show-Manifest $file; Say '' }

Say '== Files Vortex set aside (*.vortex_backup) =='
$backups = @()
if (Test-Path -LiteralPath $Documents) {
    $backups = @(Get-ChildItem -LiteralPath $Documents -Recurse -Depth 4 -Force -File -Filter '*vortex_backup*' -ErrorAction SilentlyContinue)
}
if ($backups.Count -eq 0) { Say 'None found.' }
foreach ($backup in $backups) { Say $backup.FullName }
Say ''

Say '== AddIns.xml =='
$addIns = Join-Path $Documents 'Settings\AddIns.xml'
if (Test-Path -LiteralPath $addIns) {
    $item = Get-Item -LiteralPath $addIns -Force
    $kind = if ($item.LinkType) { $item.LinkType } else { 'a plain file' }
    try { $count = ([xml](Get-Content -LiteralPath $addIns -Raw)).SelectNodes('//AddInItem').Count; Say "Lists $count add-ins; it is $kind." }
    catch { Say "Could not be read as XML: $($_.Exception.Message); it is $kind." }
}
else { Say 'Not found.' }
Say ''

$named = @($stagingPaths | Sort-Object -Unique)
if (-not $PSBoundParameters.ContainsKey('Staging') -and $named.Count -gt 0) { $Staging = $named[0] } # The record knows where it is.
Say "== Vortex's staging folder: $Staging =="
if (Test-Path -LiteralPath $Staging) {
    $mods = @(Get-ChildItem -LiteralPath $Staging -Directory -Force)
    Say "$($mods.Count) mod folders:"
    foreach ($mod in $mods) {
        $count = @(Get-ChildItem -LiteralPath $mod.FullName -Recurse -File -Force -ErrorAction SilentlyContinue).Count
        $top = (@(Get-ChildItem -LiteralPath $mod.FullName -Force | Select-Object -First 6 | ForEach-Object Name)) -join ', '
        Say "  $($mod.Name): $count files; at its top: $top"
    }
    $markers = @(Get-ChildItem -LiteralPath $Staging -File -Force | ForEach-Object Name)
    if ($markers.Count -gt 0) { Say "Files beside the mod folders: $($markers -join ', ')" }
}
else { Say 'Not found. If you moved it in Vortex (Settings > Mods > Mod Staging Folder), run again with -Staging.' }

$lines | Set-Content -LiteralPath $Report -Encoding UTF8
Write-Host ''
Write-Host "Saved to $Report. Nothing was changed." -ForegroundColor Green
