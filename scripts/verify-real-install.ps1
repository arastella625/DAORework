#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Verifies the CLI against a real Dragon Age: Origins install (the owner check in CONTRIBUTING.md).
.EXAMPLE
  .\scripts\verify-real-install.ps1 -InstallDir "D:\SteamLibrary\steamapps\common\Dragon Age Ultimate Edition"
  .\scripts\verify-real-install.ps1 -InstallDir "D:\GOG Games\Dragon Age Origins" -Branch refactor/P0-10-launch-game-use-case -SkipLaunch
  .\scripts\verify-real-install.ps1 -InstallDir "..." -ModsDir "$env:USERPROFILE\Downloads\dao-mods"   # also imports your downloads and activates them on a throwaway folder
.NOTES
  Runs against the checked-out branch unless -Branch is given. It creates a profile
  named "Verify" and deletes it at the end. The mod-change checks (verify-mod-changes.ps1)
  activate two small test mods on your real documents folder and roll them back, checking
  every file there is byte-for-byte as it was. The fix checks (verify-fixes.ps1) apply the
  4 GB patch to your real daorigins.exe and remove it again, checking the exe is byte-for-byte
  as it was, then do the same with DXVK (downloaded from its GitHub releases) when the install
  has no d3d9.dll of its own. Paste the summary into the task's review.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$InstallDir,
    [string]$Branch,
    [string]$ProfileName = 'Verify',
    [string]$ModsDir,
    [switch]$SkipLaunch
)

$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)

$results = [System.Collections.Generic.List[string]]::new()
function Check([string]$name, [bool]$ok, [string]$detail = '') {
    $tag = if ($ok) { 'PASS' } else { 'FAIL' }
    $results.Add("[$tag] $name" + $(if ($detail) { " - $detail" } else { '' }))
    Write-Host "[$tag] $name" -ForegroundColor $(if ($ok) { 'Green' } else { 'Red' })
}
function Dao([string[]]$cliArgs) {
    $output = & dotnet run --project src/DAORework.Cli --no-build -- @cliArgs 2>&1 | Out-String
    Write-Host $output
    return $output
}

if ($Branch) {
    Write-Host "== Checking out $Branch" -ForegroundColor Cyan
    $before = git rev-parse HEAD
    git fetch origin | Out-Null
    git switch $Branch | Out-Null
    git pull --ff-only origin $Branch | Out-Null
    Check 'On the task branch' ((git branch --show-current) -eq $Branch)

    # PowerShell parsed this file before the switch. If the branch changed it, run the new version instead.
    git diff --quiet $before HEAD -- $PSCommandPath
    if ($LASTEXITCODE -ne 0) {
        Write-Host "== The script changed on $Branch; re-running the new version" -ForegroundColor Cyan
        & $PSCommandPath -InstallDir $InstallDir -ProfileName $ProfileName -ModsDir $ModsDir -SkipLaunch:$SkipLaunch
        exit $LASTEXITCODE
    }
}
else {
    Write-Host "== Using the checked-out branch: $(git branch --show-current)" -ForegroundColor Cyan
    Check 'On the task branch' $true (git branch --show-current)
}

Write-Host "== Building and testing" -ForegroundColor Cyan
dotnet build DAORework.sln -clp:ErrorsOnly | Out-Null
Check 'Build' ($LASTEXITCODE -eq 0)
$testOutput = dotnet test DAORework.sln --no-build 2>&1 | Out-String
$testLines = $testOutput -split "\r?\n" | ForEach-Object {
    if ($_ -match '(Passed!|Failed!).*Total:\s+(\d+).*DAORework\.(\S+)\.dll') { "$($Matches[3]) $($Matches[2]) $($Matches[1].TrimEnd('!'))" }
}
Check 'Tests' ($LASTEXITCODE -eq 0) (($testLines -join '; ') + " ($($testLines.Count) projects)")

$gameExe = Join-Path $InstallDir 'bin_ship\daorigins.exe'
$launcherExe = Join-Path $InstallDir 'DAOriginsLauncher.exe'
Check 'Game executable exists on disk' (Test-Path $gameExe) $gameExe

Write-Host "== detect" -ForegroundColor Cyan
$detect = Dao @('detect')
Check 'detect finds the install automatically' ($detect -match [regex]::Escape($InstallDir)) (($detect -split "\r?\n" | Where-Object { $_ -match '^\[' }) -join '; ')
$manual = Dao @('detect', '--path', $InstallDir)
Check 'detect --path accepts the install folder' ($manual -match [regex]::Escape($InstallDir))
Check 'detect lists an existing documents folder' ($detect -match '\[KnownFolder\] .*\(exists') (($detect -split "\r?\n" | Where-Object { $_ -match '^\s+\[(KnownFolder|UserProfile|OneDrive)\]' }) -join '; ')

Write-Host "== Creating profile '$ProfileName'" -ForegroundColor Cyan
Dao @('init', $ProfileName) | Out-Null
Dao @('profile', 'set-path', $ProfileName, 'install', $InstallDir) | Out-Null
Dao @('profile', 'set-path', $ProfileName, 'game', $gameExe) | Out-Null
if (Test-Path $launcherExe) { Dao @('profile', 'set-path', $ProfileName, 'launcher', $launcherExe) | Out-Null }

$profilesDir = Join-Path $env:LOCALAPPDATA 'DAORework\profiles'
$profileJson = Get-Content (Join-Path $profilesDir "$ProfileName.json") -Raw
Check 'profile file carries schemaVersion' ($profileJson -match '"schemaVersion": 1')
Check 'no temp files left in the profiles folder' (-not (Get-ChildItem $profilesDir -Filter '*.tmp'))

Write-Host "== legacy (v0) profile migration" -ForegroundColor Cyan
Set-Content (Join-Path $profilesDir 'VerifyLegacy.json') '{"name":"Verify:Legacy","launchTarget":1}' -NoNewline
$legacy = Dao @('profile', 'show', 'VerifyLegacy')
Check 'legacy profile loads with the file name as its name' ($legacy -match 'Profile:\s+VerifyLegacy')
Check 'legacy profile keeps its launch target' ($legacy -match 'Launch Target:\s+OfficialLauncher')
Remove-Item (Join-Path $profilesDir 'VerifyLegacy.json') -ErrorAction SilentlyContinue

Write-Host "== diagnose" -ForegroundColor Cyan
$diag = Dao @('diagnose', $ProfileName)
Check 'diagnose reports DAO_PROFILE_VALID' ($diag -match 'DAO_PROFILE_VALID')
Check 'diagnose finds the game executable' ($diag -notmatch 'DAO_GAME_EXECUTABLE_(MISSING|NOT_FOUND)')
$p103 = ($diag -split "\r?\n" | Where-Object { $_ -match 'DAO_(LAA|PREREQ|CONFIG|SAVES|DLC)_' }) -join ' | '
Check 'diagnose reads the 4 GB flag' ($diag -match 'DAO_LAA_(ENABLED|DISABLED)') (($diag -split "\r?\n" | Where-Object { $_ -match 'DAO_LAA_' }) -join '')
Check 'diagnose checks DirectX and VC++' (($diag -match 'DAO_PREREQ_DIRECTX_') -and ($diag -match 'DAO_PREREQ_VCREDIST_'))
Check 'diagnose reports settings, saves and DLC' (($diag -match 'DAO_CONFIG_') -and ($diag -match 'DAO_SAVES_') -and ($diag -match 'DAO_DLC_')) $p103
Check 'the new rules raise no errors' ($diag -notmatch '\[ERROR\] DAO_(LAA|PREREQ|CONFIG|SAVES|DLC)_')
$report = Get-ChildItem (Join-Path $env:LOCALAPPDATA 'DAORework\reports') -Filter 'dao-runtime-report-*.json' | Sort-Object LastWriteTime | Select-Object -Last 1
Check 'report name has milliseconds and a suffix' ($report.Name -match '^dao-runtime-report-\d{8}-\d{6}-\d{3}-[0-9a-f]{4}\.json$') $report.Name
Check 'report carries schemaVersion' ((Get-Content $report.FullName -Raw) -match '"schemaVersion": 1')

Write-Host "== support bundle" -ForegroundColor Cyan
$bundleOut = Dao @('support-bundle', $ProfileName)
$bundlePath = (($bundleOut -split "\r?\n") | Where-Object { $_ -match '^Support bundle written to: ' } | Select-Object -First 1) -replace '^Support bundle written to: ', ''
$bundleDetail = if ($bundlePath) { $bundlePath } else { 'no path printed; output began: ' + (($bundleOut -split "\r?\n" | Where-Object { $_.Trim() }) | Select-Object -First 1) }
Check 'support-bundle writes a zip' ($bundlePath -and (Test-Path $bundlePath.Trim())) $bundleDetail
if ($bundlePath -and (Test-Path $bundlePath.Trim())) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($bundlePath.Trim())
    try {
        $names = $zip.Entries | ForEach-Object { $_.FullName }
        Check 'bundle holds manifest, profile, diagnostics and inventory' ((@('manifest.json', 'profile.json', 'diagnostics.json') | Where-Object { $names -notcontains $_ }).Count -eq 0 -and ($names -match '^mod-inventory')) ($names -join ', ')
        $text = ($zip.Entries | ForEach-Object { $r = [System.IO.StreamReader]::new($_.Open()); try { $r.ReadToEnd() } finally { $r.Dispose() } }) -join "`n"
        $user = $env:USERNAME
        Check 'bundle does not contain your Windows user name' (($user.Length -lt 3) -or ($text -notmatch [regex]::Escape($user))) "checked for '$($user.Substring(0, [Math]::Min(1, $user.Length)))...'"
    }
    finally { $zip.Dispose() }
}

Write-Host "== save backups" -ForegroundColor Cyan
$real = Dao @('saves', 'backup', $ProfileName)
Check 'saves backup on the real profile answers sensibly' (($real -match 'Backed up \d+ file') -or ($real -match 'There are no saves to back up')) ((($real -split "\r?\n") | Where-Object { $_.Trim() } | Select-Object -First 1))

# A throwaway saves folder, so the backup path is exercised even before the game has been played.
$fakeDocs = Join-Path ([System.IO.Path]::GetTempPath()) ("DAORework-verify-" + [guid]::NewGuid().ToString('N'))
$fakeSlot = Join-Path $fakeDocs 'Characters\Verify\Saves\Slot_1'
New-Item -ItemType Directory -Force -Path $fakeSlot | Out-Null
Set-Content (Join-Path $fakeSlot 'Slot_1.das') ('x' * 4096) -NoNewline
Set-Content (Join-Path $fakeSlot 'screen.dds') ('y' * 1024) -NoNewline
$before = Get-ChildItem $fakeDocs -Recurse -File | ForEach-Object { "$($_.FullName)|$((Get-FileHash $_.FullName).Hash)" }
$savesProfile = 'VerifySaves'
Dao @('init', $savesProfile) | Out-Null
Dao @('profile', 'set-path', $savesProfile, 'documents', $fakeDocs) | Out-Null
1..3 | ForEach-Object { Dao @('saves', 'backup', $savesProfile, '--keep', '2') | Out-Null }
$backupDir = Join-Path $env:LOCALAPPDATA "DAORework\save-backups\$savesProfile"
$zips = @(Get-ChildItem $backupDir -Filter '*.zip' -ErrorAction SilentlyContinue)
Check 'three backups with --keep 2 leave two' ($zips.Count -eq 2) "$($zips.Count) in $backupDir"
$after = Get-ChildItem $fakeDocs -Recurse -File | ForEach-Object { "$($_.FullName)|$((Get-FileHash $_.FullName).Hash)" }
Check 'the saves folder is byte-for-byte unchanged' ((Compare-Object @($before) @($after)) -eq $null)
if ($zips.Count -gt 0) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $z = [System.IO.Compression.ZipFile]::OpenRead($zips[0].FullName)
    try { $names = $z.Entries | ForEach-Object { $_.FullName } } finally { $z.Dispose() }
    Check 'a backup holds the saves and a manifest' (($names -contains 'Characters/Verify/Saves/Slot_1/Slot_1.das') -and ($names -contains 'manifest.json')) ($names -join ', ')
}
$listed = Dao @('saves', 'list', $savesProfile)
Check 'saves list shows the two backups' (([regex]::Matches($listed, '\.zip')).Count -eq 2)
Remove-Item $fakeDocs -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $backupDir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $env:LOCALAPPDATA "DAORework\profiles\$savesProfile.json") -ErrorAction SilentlyContinue

Write-Host "== known-items catalog" -ForegroundColor Cyan
Check 'diagnose runs the known-items catalog' (($diag -match 'DAO_KNOWN_ITEMS_(FOUND|NONE)') -and ($diag -notmatch 'DAO_CATALOG_INVALID')) (($diag -split "\r?\n" | Where-Object { $_ -match 'DAO_KNOWN_ITEM' }) -join '; ')

# A throwaway install: a copy of the real game executable plus DXVK 2.3's own DLLs from GitHub. The real install is only read.
$realD3d9 = Test-Path (Join-Path $InstallDir 'bin_ship\d3d9.dll')
$fakeInstall = Join-Path ([System.IO.Path]::GetTempPath()) ("DAORework-verify-dxvk-" + [guid]::NewGuid().ToString('N'))
$fakeD3d9 = Join-Path $fakeInstall 'bin_ship\d3d9.dll'
New-Item -ItemType Directory -Force -Path (Join-Path $fakeInstall 'bin_ship'), (Join-Path $fakeInstall 'dxvk') | Out-Null
Copy-Item $gameExe (Join-Path $fakeInstall 'bin_ship\daorigins.exe')
try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $tgz = Join-Path $fakeInstall 'dxvk-2.3.tar.gz'
    Invoke-WebRequest 'https://github.com/doitsujin/dxvk/releases/download/v2.3/dxvk-2.3.tar.gz' -OutFile $tgz -UseBasicParsing
    tar -xzf $tgz -C (Join-Path $fakeInstall 'dxvk')
}
catch { Write-Host "DXVK download failed: $($_.Exception.Message)" -ForegroundColor Red }
$x32 = Join-Path $fakeInstall 'dxvk\dxvk-2.3\x32\d3d9.dll'
Check 'DXVK 2.3 downloaded from GitHub' (Test-Path $x32)
if (Test-Path $x32) {
    $dxvkProfile = 'VerifyDxvk'
    Dao @('init', $dxvkProfile) | Out-Null
    Dao @('profile', 'set-path', $dxvkProfile, 'install', $fakeInstall) | Out-Null
    Dao @('profile', 'set-path', $dxvkProfile, 'game', (Join-Path $fakeInstall 'bin_ship\daorigins.exe')) | Out-Null
    Copy-Item $x32 $fakeD3d9
    $d = Dao @('diagnose', $dxvkProfile)
    Check 'the x32 DXVK DLL is recognized with its version' ($d -match 'DAO_KNOWN_ITEMS_FOUND: .*DXVK v2\.3') (($d -split "\r?\n" | Where-Object { $_ -match 'DAO_KNOWN_ITEMS_FOUND' }) -join '')
    Copy-Item (Join-Path $fakeInstall 'dxvk\dxvk-2.3\x64\d3d9.dll') $fakeD3d9 -Force
    $d = Dao @('diagnose', $dxvkProfile)
    Check 'the x64 DXVK DLL is flagged as unloadable' ($d -match 'DAO_KNOWN_ITEM_WRONG_ARCHITECTURE')
    Remove-Item $fakeD3d9
    $d = Dao @('diagnose', $dxvkProfile)
    Check 'no d3d9.dll suggests DXVK' ($d -match 'DAO_KNOWN_ITEM_SUGGESTED: DXVK is not installed')
    Remove-Item (Join-Path $profilesDir "$dxvkProfile.json") -ErrorAction SilentlyContinue
}
Remove-Item $fakeInstall -Recurse -Force -ErrorAction SilentlyContinue
Check 'the real install was not touched' ((Test-Path (Join-Path $InstallDir 'bin_ship\d3d9.dll')) -eq $realD3d9)

Write-Host "== mod library (P2-07: import only; nothing is installed)" -ForegroundColor Cyan
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
function New-TestZip([string]$path, [hashtable]$entries) {
    $zip = [System.IO.Compression.ZipFile]::Open($path, 'Create')
    try { foreach ($name in $entries.Keys) { $w = [System.IO.StreamWriter]::new($zip.CreateEntry($name).Open()); $w.Write($entries[$name]); $w.Dispose() } }
    finally { $zip.Dispose() }
}
$documents = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'BioWare\Dragon Age'
$docsBefore = if (Test-Path $documents) { Get-ChildItem $documents -Recurse -File | ForEach-Object { "$($_.FullName)|$($_.Length)" } } else { @() }
$libDir = Join-Path $env:LOCALAPPDATA 'DAORework\library'
$tempMods = Join-Path ([System.IO.Path]::GetTempPath()) ("DAORework-verify-mods-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tempMods | Out-Null
$testDazip = Join-Path $tempMods 'verify.dazip'
New-TestZip $testDazip @{ 'Manifest.xml' = '<Manifest Type="AddIn"><AddInsList><AddInItem UID="daorework_verify" Name="DAORework Verify" /></AddInsList></Manifest>'; 'Contents/addins/daorework_verify/verify.txt' = 'verify' }
$imported = Dao @('mods', 'import', $testDazip)
# Only an id read from this import's own output is ever deleted below.
$verifyId = if ($imported -match 'Imported: DAORework Verify \(add-in daorework_verify\) as (daorework-verify-[0-9a-f]{8})') { $Matches[1] } else { $null }
Check 'mods import reads a .dazip into the library' ($null -ne $verifyId) (($imported -split "\r?\n" | Select-Object -First 1))
Check 'importing it again finds it already there' ((Dao @('mods', 'import', $testDazip)) -match 'Already in the library')
Check 'mods library lists it' ((Dao @('mods', 'library')) -match [regex]::Escape("$verifyId  DAORework Verify"))
$evil = Join-Path $tempMods 'evil.zip'
New-TestZip $evil @{ '../../evil.txt' = 'x' }
Check 'an archive that would write outside the library is refused' (((Dao @('mods', 'import', $evil)) -match 'would be written outside') -and -not (Test-Path (Join-Path $env:LOCALAPPDATA 'DAORework\evil.txt')))
$realModIds = [System.Collections.Generic.List[string]]::new()
if ($ModsDir) {
    foreach ($download in Get-ChildItem $ModsDir -File -Include *.zip, *.dazip -Recurse) {
        $real = Dao @('mods', 'import', $download.FullName)
        Check "mods import $($download.Name)" ($real -match '(Imported|Already in the library): ') (($real -split "\r?\n" | Where-Object { $_.Trim() } | Select-Object -First 1))
        if ($real -match ' as ([a-z0-9-]+), \d+ file\(s\)') { $realModIds.Add($Matches[1]) }
    }
}
$docsAfter = if (Test-Path $documents) { Get-ChildItem $documents -Recurse -File | ForEach-Object { "$($_.FullName)|$($_.Length)" } } else { @() }
Check 'the documents folder is unchanged by importing' ((Compare-Object @($docsBefore) @($docsAfter)) -eq $null)
if ($verifyId) {
    Remove-Item (Join-Path $libDir $verifyId) -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $libDir "$verifyId.mod.json") -ErrorAction SilentlyContinue
}
Remove-Item $tempMods -Recurse -Force -ErrorAction SilentlyContinue

. (Join-Path $PSScriptRoot 'verify-mod-changes.ps1')
. (Join-Path $PSScriptRoot 'verify-fixes.ps1')

Write-Host "== launch of an unknown profile" -ForegroundColor Cyan
$nope = Dao @('launch', 'NopeVerify')
Check 'launch NopeVerify reports not found' ($nope -match "Profile 'NopeVerify' was not found")
Check 'launch NopeVerify created no profile file' (-not (Test-Path (Join-Path $profilesDir 'NopeVerify.json')))

if (-not $SkipLaunch) {
    Write-Host "== launch (this starts the game; close it when it is up)" -ForegroundColor Cyan
    $launch = Dao @('launch', $ProfileName)
    Check 'launch succeeded with a process id' ($launch -match 'Launch succeeded\. ProcessId: \d+')
    if (Test-Path $launcherExe) {
        Read-Host 'Press Enter after closing the game to test the official launcher target'
        Dao @('profile', 'set-target', $ProfileName, 'OfficialLauncher') | Out-Null
        $launch2 = Dao @('launch', $ProfileName)
        Check 'launch via OfficialLauncher succeeded' ($launch2 -match 'Launch succeeded\. ProcessId: \d+')
    }
}
else {
    $results.Add('[SKIP] launch (SkipLaunch)')
}

Remove-Item (Join-Path $profilesDir "$ProfileName.json") -ErrorAction SilentlyContinue

Write-Host ''
Write-Host '== Summary' -ForegroundColor Cyan
$results | ForEach-Object { Write-Host $_ }
Write-Host ''
Write-Host 'Paste the summary back to Claude.' -ForegroundColor Cyan
