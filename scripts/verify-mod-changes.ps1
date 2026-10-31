# Dot-sourced by verify-real-install.ps1 (ROADMAP P2-06): activates, deactivates and rolls back mods, first on a
# throwaway documents folder and then on the real one, and checks each ends byte-for-byte as it began.
# Uses Check, Dao, New-TestZip, $profilesDir, $documents and $realModIds from the calling script.

function Get-FolderHashes([string]$folder) {
    if (-not (Test-Path $folder)) { return @() }
    Get-ChildItem $folder -Recurse -File -Force | Sort-Object FullName | ForEach-Object { "$($_.FullName.Substring($folder.Length))|$((Get-FileHash $_.FullName).Hash)" }
}
function Test-Same($before, $after) { return ((@($before) -join "`n") -eq (@($after) -join "`n")) }
function Get-ImportedId([string]$output) { if ($output -match ' as ([a-z0-9-]+), \d+ file\(s\)') { return $Matches[1] } return $null }

Write-Host "== mod changes (P2-06: activate, deactivate, rollback)" -ForegroundColor Cyan
$appData = Join-Path $env:LOCALAPPDATA 'DAORework'
$stateRoots = @((Join-Path $appData 'deployments'), (Join-Path $appData 'deployment-backups'))
$stateBefore = @($stateRoots | Where-Object { Test-Path $_ } | ForEach-Object { Get-ChildItem $_ -Recurse -Force | ForEach-Object FullName })

$work = Join-Path ([System.IO.Path]::GetTempPath()) ("DAORework-verify-changes-" + [guid]::NewGuid().ToString('N'))
$tempDocs = Join-Path $work 'Dragon Age'
New-Item -ItemType Directory -Force -Path (Join-Path $tempDocs 'Settings'), (Join-Path $tempDocs 'packages\core\override\My Mods') | Out-Null
[System.IO.File]::WriteAllText((Join-Path $tempDocs 'Settings\AddIns.xml'), "<?xml version=`"1.0`" encoding=`"utf-8`"?>`r`n<AddInsList>`r`n  <AddInItem UID=`"users_own`" Name=`"Mine`" />`r`n</AddInsList>")
Set-Content (Join-Path $tempDocs 'packages\core\override\My Mods\mine.dds') 'hand placed' -NoNewline

$changeDazip = Join-Path $work 'verify-change.dazip'
New-TestZip $changeDazip @{ 'Manifest.xml' = '<Manifest Type="AddIn"><AddInsList><AddInItem UID="daorework_verify_change" Name="DAORework Verify Change" /></AddInsList></Manifest>'; 'Contents/addins/daorework_verify_change/verify.txt' = 'change' }
$overrideZip = Join-Path $work 'DAORework Verify Override.zip'
New-TestZip $overrideZip @{ 'textures/verify.dds' = 'override' }
# Only ids read from these imports' own output are ever deleted below.
$testIds = @((Get-ImportedId (Dao @('mods', 'import', $changeDazip))), (Get-ImportedId (Dao @('mods', 'import', $overrideZip))))
Check 'the two test mods were imported' (($testIds | Where-Object { $_ }).Count -eq 2) ($testIds -join ', ')

$modsProfile = 'VerifyMods'
Dao @('init', $modsProfile) | Out-Null
Dao @('profile', 'set-path', $modsProfile, 'documents', $tempDocs) | Out-Null
$start = Get-FolderHashes $tempDocs

$dry = Dao (@('mods', 'activate') + $testIds + @('-p', $modsProfile, '--dry-run', '--files'))
Check 'a dry run shows the plan and changes nothing' (($dry -match 'Dry run: nothing was changed') -and ($dry -match 'AddIns.xml: add daorework_verify_change') -and (Test-Same $start (Get-FolderHashes $tempDocs)))
$noYes = Dao (@('mods', 'activate') + $testIds + @('-p', $modsProfile))
Check 'without --yes a script cannot activate' (($noYes -match 'add --yes') -and (Test-Same $start (Get-FolderHashes $tempDocs)))

$on = Dao (@('mods', 'activate') + $testIds + @('-p', $modsProfile, '--yes'))
$addIn = Join-Path $tempDocs 'AddIns\daorework_verify_change\verify.txt'
$override = Join-Path $tempDocs "packages\core\override\DAORework\$($testIds[1])\textures\verify.dds"
Check 'activate --yes installs both mods' (($on -match 'Done: \d+ step') -and (Test-Path $addIn) -and (Test-Path $override)) (($on -split "\r?\n" | Where-Object { $_ -match '^Done' }) -join '')
Check 'the add-in is registered in AddIns.xml' ((Get-Content (Join-Path $tempDocs 'Settings\AddIns.xml') -Raw) -match 'daorework_verify_change')
Check 'the hand-placed override file is untouched' ((Get-Content (Join-Path $tempDocs 'packages\core\override\My Mods\mine.dds') -Raw) -eq 'hand placed')
Check 'mods status lists them in order' ((Dao @('mods', 'status', '-p', $modsProfile)) -match [regex]::Escape("Active, lowest priority first: $($testIds -join ', ')"))

Dao (@('mods', 'deactivate') + $testIds + @('-p', $modsProfile, '--yes')) | Out-Null
Check 'deactivate leaves the folder byte-for-byte as it began' (Test-Same $start (Get-FolderHashes $tempDocs))
Dao (@('mods', 'activate') + $testIds + @('-p', $modsProfile, '--yes')) | Out-Null
$back = Dao @('mods', 'rollback', '-p', $modsProfile, '--yes')
Check 'rollback leaves the folder byte-for-byte as it began' (($back -match 'Rolled back:') -and (Test-Same $start (Get-FolderHashes $tempDocs)))
Check 'recover with nothing to recover says so' ((Dao @('mods', 'recover', '-p', $modsProfile, '--finish')) -match 'There is no unfinished change')

if ($realModIds.Count -gt 0) {
    $real = Dao (@('mods', 'activate') + $realModIds + @('-p', $modsProfile, '--yes'))
    Check "your library's mods activate on the throwaway folder" ($real -match 'Done: \d+ step') (($real -split "\r?\n" | Where-Object { $_ -match '^(Done|Blocked|  )' }) -join '; ')
    Dao (@('mods', 'deactivate') + $realModIds + @('-p', $modsProfile, '--yes')) | Out-Null
    Check '...and deactivating them leaves it as it began' (Test-Same $start (Get-FolderHashes $tempDocs))
}
Remove-Item (Join-Path $profilesDir "$modsProfile.json") -ErrorAction SilentlyContinue

# The real documents folder: only the two test mods, and only when DAORework has nothing active there already.
if (Test-Path $documents) {
    $realProfile = 'VerifyModsReal'
    Dao @('init', $realProfile) | Out-Null
    Dao @('profile', 'set-path', $realProfile, 'documents', $documents) | Out-Null
    $realStart = Get-FolderHashes $documents
    $realFolders = @(Get-ChildItem $documents -Recurse -Directory -Force | ForEach-Object FullName)
    if ((Dao @('mods', 'status', '-p', $realProfile)) -match 'No mods are active through DAORework') {
        $on = Dao (@('mods', 'activate') + $testIds + @('-p', $realProfile, '--yes'))
        Check 'activate --yes works on your real documents folder' (($on -match 'Done: \d+ step') -and (Test-Path (Join-Path $documents 'AddIns\daorework_verify_change\verify.txt')))
        Dao @('mods', 'rollback', '-p', $realProfile, '--yes') | Out-Null
        Check 'rollback leaves your real documents folder byte-for-byte as it began' (Test-Same $realStart (Get-FolderHashes $documents))

        # DAORework keeps the game's own AddIns and override folders even when empty; ones this run created are removed here.
        $created = @(Get-ChildItem $documents -Recurse -Directory -Force | Where-Object { $realFolders -notcontains $_.FullName } | Sort-Object { $_.FullName.Length } -Descending)
        $created | Where-Object { -not (Get-ChildItem $_.FullName -Force) } | ForEach-Object { Remove-Item $_.FullName }
        $left = @(Get-ChildItem $documents -Recurse -Directory -Force | Where-Object { $realFolders -notcontains $_.FullName })
        Check 'no folder is left behind in your real documents folder' ($left.Count -eq 0) (($created | ForEach-Object { $_.FullName.Substring($documents.Length) }) -join ', ')
    }
    else {
        $results.Add('[SKIP] real documents folder (DAORework already has mods active there)')
    }
    Remove-Item (Join-Path $profilesDir "$realProfile.json") -ErrorAction SilentlyContinue
}
else {
    $results.Add("[SKIP] real documents folder ($documents does not exist)")
}

# Leaves the deployment records and backups as they were, and the library without the test mods.
foreach ($root in $stateRoots | Where-Object { Test-Path $_ }) {
    Get-ChildItem $root -Recurse -Force | Sort-Object { $_.FullName.Length } -Descending | Where-Object { $stateBefore -notcontains $_.FullName } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
}
foreach ($id in $testIds | Where-Object { $_ }) {
    Remove-Item (Join-Path $libDir $id) -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $libDir "$id.mod.json") -ErrorAction SilentlyContinue
}
Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
