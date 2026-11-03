#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Reports how Vortex has set up Dragon Age: Origins on this PC, for ADR-0028 (taking over from Vortex). Read-only.
.EXAMPLE
  .\scripts\check-vortex.ps1
  .\scripts\check-vortex.ps1 -Documents "D:\Documents\BioWare\Dragon Age" -Staging "E:\Vortex Mods\dragonage"
.NOTES
  Nothing is assumed to be on C:. The game, its documents folder and Vortex's staging folder are looked for where
  they really are: DAORework's own profiles, the registry, Steam's libraries, Vortex's records, and four folders deep
  on every fixed drive. Folders given with -Documents, -Install or -Staging are checked as well.

  It only reads. It never changes a file, and it never opens a mod's own files: it lists Vortex's record files
  (vortex.deployment*), the fields they hold, which mods they name and how many files each placed, whether a placed
  file is a link, the names of the staging folder's mod folders, and how many add-ins AddIns.xml lists. Your user
  name and profile folder are replaced by %USERPROFILE% in the report. Paste the report (vortex-report.txt) into
  the conversation or the task's issue.
#>
[CmdletBinding()]
param(
    [string[]]$Documents = @(),
    [string[]]$Install = @(),
    [string[]]$Staging = @(),
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
function Quiet([scriptblock]$block) { try { & $block } catch { @() } }
function Has([string]$path) { try { Test-Path -LiteralPath $path } catch { $false } } # A folder it may not read is skipped.
function LinkKind($item) { if ($item.LinkType) { $item.LinkType } else { 'a plain file' } }

# Every place a folder was named, so the report says why each one was looked at.
$found = @{ Documents = [ordered]@{}; Install = [ordered]@{}; Staging = [ordered]@{} }
function Note([string]$kind, [string]$path, [string]$why) {
    if (-not $path) { return }
    $path = $path.TrimEnd('\', '/')
    $key = $path.ToLowerInvariant()
    if ($found[$kind].Contains($key)) { $found[$kind][$key].Why += ", $why" }
    else { $found[$kind][$key] = [pscustomobject]@{ Path = $path; Why = $why } }
}

function Find-Places {
    foreach ($path in $Documents) { Note Documents $path 'given with -Documents' }
    foreach ($path in $Install) { Note Install $path 'given with -Install' }
    foreach ($path in $Staging) { Note Staging $path 'given with -Staging' }

    $profiles = if ($env:LOCALAPPDATA) { Join-Path $env:LOCALAPPDATA 'DAORework\profiles' } else { '' }
    foreach ($file in (Quiet { Get-ChildItem -LiteralPath $profiles -Filter '*.json' -File })) {
        $profile = Quiet { Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json }
        if (-not $profile) { continue }
        Note Documents $profile.UserDocumentsDirectory "DAORework profile '$($profile.Name)'"
        Note Install $profile.InstallDirectory "DAORework profile '$($profile.Name)'"
    }

    $myDocuments = [Environment]::GetFolderPath('MyDocuments')
    if ($myDocuments) { Note Documents (Join-Path $myDocuments 'BioWare\Dragon Age') 'the Documents folder Windows reports' }
    if ($env:OneDrive) { Note Documents (Join-Path $env:OneDrive 'Documents\BioWare\Dragon Age') 'OneDrive' }

    foreach ($key in 'HKLM:\SOFTWARE\WOW6432Node\BioWare\Dragon Age', 'HKLM:\SOFTWARE\BioWare\Dragon Age') {
        Note Install (Quiet { (Get-ItemProperty -LiteralPath $key).Path }) 'the BioWare registry key'
    }
    foreach ($game in (Quiet { Get-ChildItem -LiteralPath 'HKLM:\SOFTWARE\WOW6432Node\GOG.com\Games' })) {
        $values = Quiet { Get-ItemProperty -LiteralPath $game.PSPath }
        if ($values -and $values.gameName -like 'Dragon Age*') { Note Install $values.path 'the GOG registry key' }
    }
    Find-SteamInstalls

    if ($env:APPDATA) { Note Staging (Join-Path $env:APPDATA 'Vortex\dragonage\mods') "Vortex's default" }
    Find-OnDrives
}

function Find-SteamInstalls {
    $steam = Quiet { (Get-ItemProperty -LiteralPath 'HKCU:\Software\Valve\Steam').SteamPath }
    if (-not $steam) { return }
    $libraries = @($steam)
    $vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
    foreach ($match in (Quiet { Select-String -LiteralPath $vdf -Pattern '"path"\s+"([^"]+)"' })) {
        $libraries += $match.Matches[0].Groups[1].Value.Replace('\\', '\')
    }
    foreach ($library in $libraries) {
        foreach ($name in 'Dragon Age Ultimate Edition', 'Dragon Age Origins', 'Dragon Age Origins Ultimate Edition') {
            $path = Join-Path $library "steamapps\common\$name"
            if (Test-Path -LiteralPath $path) { Note Install $path 'a Steam library' }
        }
    }
}

# Four folders deep on every fixed drive: "D:\Documents\BioWare\Dragon Age", "D:\Games\Dragon Age Origins",
# "D:\Vortex Mods\dragonage". Folders only, plus Vortex's marker file, so it stays quick.
function Find-OnDrives {
    $skip = 'Windows', 'Program Files', 'Program Files (x86)', 'ProgramData', '$Recycle.Bin', 'System Volume Information'
    foreach ($drive in [System.IO.DriveInfo]::GetDrives() | Where-Object { $_.DriveType -eq 'Fixed' -and $_.IsReady }) {
        Write-Host "Looking on $($drive.Name) ..."
        $tops = Quiet { Get-ChildItem -LiteralPath $drive.RootDirectory.FullName -Directory -Force } | Where-Object { $skip -notcontains $_.Name }
        foreach ($top in $tops) {
            $folders = @($top) + @(Quiet { Get-ChildItem -LiteralPath $top.FullName -Directory -Recurse -Depth 2 -Force -ErrorAction SilentlyContinue })
            foreach ($folder in $folders) {
                if ($folder.Name -eq 'Dragon Age' -and $folder.Parent.Name -eq 'BioWare') { Note Documents $folder.FullName "found on $($drive.Name)" }
                if ($folder.Name -like 'Dragon Age*' -and (Has (Join-Path $folder.FullName 'bin_ship\daorigins.exe'))) {
                    Note Install $folder.FullName "found on $($drive.Name)"
                }
                if (Has (Join-Path $folder.FullName '__vortex_staging_folder')) {
                    if ($folder.Name -eq 'dragonage' -or $folder.FullName -like '*dragonage*') { Note Staging $folder.FullName "found on $($drive.Name)" }
                }
            }
        }
    }
}

function Show-Manifest([System.IO.FileInfo]$file) {
    Say "  Record file: $($file.FullName) ($($file.Length) bytes, changed $($file.LastWriteTime.ToString('yyyy-MM-dd HH:mm')))"
    if ($file.Extension -ne '.json') { Say '    Not JSON; only its name and size are reported.'; return }
    try { $manifest = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json }
    catch { Say "    Could not be read as JSON: $($_.Exception.Message)"; return }

    if ($manifest.stagingPath) { $script:stagingPaths.Add([string]$manifest.stagingPath) }
    foreach ($property in $manifest.PSObject.Properties) {
        if ($property.Value -is [System.Array]) { Say "    $($property.Name): a list of $($property.Value.Count)" }
        elseif ($property.Value -is [System.Management.Automation.PSCustomObject]) { Say "    $($property.Name): an object" }
        else { Say "    $($property.Name): $($property.Value)" }
    }
    $files = @($manifest.files)
    if ($files.Count -eq 0) { return }
    Say "    Fields of a file entry: $(($files[0].PSObject.Properties.Name) -join ', ')"
    Say '    Mods named (source), with how many files each placed:'
    $files | Group-Object -Property source | Sort-Object Name | ForEach-Object { Say "      $($_.Name): $($_.Count)" }
    $merged = @($files | Where-Object { $_.merged })
    if ($merged.Count -gt 0) { Say "    Entries with 'merged': $($merged.Count), for example $($merged[0].relPath)" }
    Say '    The first five placed files, and whether each is still a link:'
    foreach ($entry in ($files | Select-Object -First 5)) {
        $placed = Join-Path $file.DirectoryName $entry.relPath
        if (-not (Test-Path -LiteralPath $placed)) { Say "      $($entry.relPath): missing"; continue }
        Say "      $($entry.relPath): $(LinkKind (Get-Item -LiteralPath $placed -Force))"
    }
}

function Show-VortexFiles([string]$root) {
    $records = @(Quiet { Get-ChildItem -LiteralPath $root -Recurse -Depth 4 -Force -File -Filter 'vortex.deployment*' -ErrorAction SilentlyContinue })
    if ($records.Count -eq 0) { Say '  Vortex record files: none.' }
    foreach ($record in $records) { Show-Manifest $record }
    $backups = @(Quiet { Get-ChildItem -LiteralPath $root -Recurse -Depth 5 -Force -File -Filter '*vortex_backup*' -ErrorAction SilentlyContinue })
    if ($backups.Count -eq 0) { Say '  Files Vortex set aside (*.vortex_backup): none.' }
    foreach ($backup in $backups) { Say "  Set aside by Vortex: $($backup.FullName)" }
}

function Show-Documents($place) {
    Say "Documents folder: $($place.Path)"
    Say "  Why looked at: $($place.Why)"
    if (-not (Test-Path -LiteralPath $place.Path)) { Say '  Does not exist.'; return }
    $override = Join-Path $place.Path 'packages\core\override'
    $count = @(Quiet { Get-ChildItem -LiteralPath $override -Recurse -File -Force -ErrorAction SilentlyContinue }).Count
    Say "  override: $count files. AddIns folders: $(@(Quiet { Get-ChildItem -LiteralPath (Join-Path $place.Path 'AddIns') -Directory }).Count)."
    $addIns = Join-Path $place.Path 'Settings\AddIns.xml'
    if (Test-Path -LiteralPath $addIns) {
        $kind = LinkKind (Get-Item -LiteralPath $addIns -Force)
        try { Say "  AddIns.xml lists $(([xml](Get-Content -LiteralPath $addIns -Raw)).SelectNodes('//AddInItem').Count) add-ins; it is $kind." }
        catch { Say "  AddIns.xml could not be read as XML: $($_.Exception.Message); it is $kind." }
    }
    else { Say '  AddIns.xml: not found.' }
    Show-VortexFiles $place.Path
}

function Show-Install($place) {
    Say "Game folder: $($place.Path)"
    Say "  Why looked at: $($place.Why)"
    if (-not (Test-Path -LiteralPath $place.Path)) { Say '  Does not exist.'; return }
    Say "  bin_ship\daorigins.exe: $(Test-Path -LiteralPath (Join-Path $place.Path 'bin_ship\daorigins.exe'))"
    $override = Join-Path $place.Path 'packages\core\override'
    Say "  packages\core\override: $(@(Quiet { Get-ChildItem -LiteralPath $override -Recurse -File -Force -ErrorAction SilentlyContinue }).Count) files."
    Show-VortexFiles $place.Path
}

function Show-Staging($place) {
    Say "Staging folder: $($place.Path)"
    Say "  Why looked at: $($place.Why)"
    if (-not (Test-Path -LiteralPath $place.Path)) { Say '  Does not exist.'; return }
    $mods = @(Get-ChildItem -LiteralPath $place.Path -Directory -Force)
    Say "  $($mods.Count) mod folders:"
    foreach ($mod in $mods) {
        $count = @(Quiet { Get-ChildItem -LiteralPath $mod.FullName -Recurse -File -Force -ErrorAction SilentlyContinue }).Count
        $top = (@(Get-ChildItem -LiteralPath $mod.FullName -Force | Select-Object -First 6 | ForEach-Object Name)) -join ', '
        Say "    $($mod.Name): $count files; at its top: $top"
    }
    $markers = @(Get-ChildItem -LiteralPath $place.Path -File -Force | ForEach-Object Name)
    if ($markers.Count -gt 0) { Say "  Files beside the mod folders: $($markers -join ', ')" }
}

Say "check-vortex.ps1, $(Get-Date -Format 'yyyy-MM-dd HH:mm'), PowerShell $($PSVersionTable.PSVersion)"
Find-Places
$vortexData = if ($env:APPDATA) { Join-Path $env:APPDATA 'Vortex' } else { $null }
if ($vortexData -and (Test-Path -LiteralPath $vortexData)) {
    $games = @(Get-ChildItem -LiteralPath $vortexData -Directory -Force | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'mods') } | ForEach-Object Name)
    Say "Vortex's own folder: $vortexData; games with a mods folder there: $($games -join ', ')"
}
else { Say "Vortex's own folder (%APPDATA%\Vortex): not found." }
Say ''

Say '== Dragon Age documents folders =='
foreach ($place in $found.Documents.Values) { Show-Documents $place; Say '' }
Say '== Dragon Age game folders =='
if ($found.Install.Count -eq 0) { Say 'None found. Run again with -Install "D:\path\to\Dragon Age".'; Say '' }
foreach ($place in $found.Install.Values) { Show-Install $place; Say '' }

foreach ($path in $stagingPaths) { Note Staging $path "named in a Vortex record" }
Say "== Vortex's staging folders for Dragon Age =="
foreach ($place in $found.Staging.Values) { Show-Staging $place; Say '' }

$lines | Set-Content -LiteralPath $Report -Encoding UTF8
Write-Host ''
Write-Host "Saved to $Report. Nothing was changed." -ForegroundColor Green
