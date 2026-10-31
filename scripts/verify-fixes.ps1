# Dot-sourced by verify-real-install.ps1 (ROADMAP P2-08, ADR-0013): applies and removes the 4 GB patch, first on a
# throwaway copy of daorigins.exe and then on the real install, and checks the exe ends byte-for-byte as it began.
# DXVK follows in verify-dxvk.ps1, on the same throwaway install.
# Uses Check, Dao, $profilesDir, $gameExe and $results from the calling script.

function Test-LargeAddressAware([string]$exe) {
    $bytes = [System.IO.File]::ReadAllBytes($exe)
    $pe = [BitConverter]::ToInt32($bytes, 0x3C)
    return ([BitConverter]::ToUInt16($bytes, $pe + 22) -band 0x20) -ne 0
}
function Get-Sha([string]$path) { return (Get-FileHash $path -Algorithm SHA256).Hash }

Write-Host "== managed fixes (P2-08: the 4 GB patch)" -ForegroundColor Cyan
$appData = Join-Path $env:LOCALAPPDATA 'DAORework'
$fixRoots = @((Join-Path $appData 'deployments'), (Join-Path $appData 'deployment-backups'), (Join-Path (Join-Path $appData 'library') 'fixes'))
$fixStateBefore = @($fixRoots | Where-Object { Test-Path $_ } | ForEach-Object { Get-ChildItem $_ -Recurse -Force | ForEach-Object FullName })
$realExeBefore = Get-Sha $gameExe
$realWasLaa = Test-LargeAddressAware $gameExe

$fixWork = Join-Path ([System.IO.Path]::GetTempPath()) ("DAORework-verify-fixes-" + [guid]::NewGuid().ToString('N'))
$tempExe = Join-Path (Join-Path $fixWork 'bin_ship') 'daorigins.exe'
New-Item -ItemType Directory -Force -Path (Split-Path $tempExe) | Out-Null
Copy-Item $gameExe $tempExe
$tempExeBefore = Get-Sha $tempExe
$fixProfile = 'VerifyFixes'
Dao @('init', $fixProfile) | Out-Null
Dao @('profile', 'set-path', $fixProfile, 'install', $fixWork) | Out-Null
Dao @('profile', 'set-path', $fixProfile, 'game', $tempExe) | Out-Null

if ($realWasLaa) {
    $results.Add('[SKIP] 4 GB patch checks (your daorigins.exe already has the flag set)')
}
else {
    Check 'fixes status starts with none' ((Dao @('fixes', 'status', '-p', $fixProfile)) -match 'No fixes are applied through DAORework')
    $dry = Dao @('fixes', 'apply', 'laa', '-p', $fixProfile, '--dry-run')
    Check 'a dry run shows the plan and changes nothing' (($dry -match 'Replace bin_ship/daorigins.exe') -and ($dry -match 'Dry run: nothing was changed') -and ((Get-Sha $tempExe) -eq $tempExeBefore))
    Check 'without --yes a script cannot apply' (((Dao @('fixes', 'apply', 'laa', '-p', $fixProfile)) -match 'add --yes') -and ((Get-Sha $tempExe) -eq $tempExeBefore))

    $on = Dao @('fixes', 'apply', 'laa', '-p', $fixProfile, '--yes')
    Check 'fixes apply laa sets the flag' (($on -match 'Done: \d+ step') -and (Test-LargeAddressAware $tempExe)) (($on -split "\r?\n" | Where-Object { $_ -match '^(Done|Backup|Note|  x)' }) -join '; ')
    # The flag and the checksum both sit in the first 4 KB; everything after it must be identical.
    $original = [System.IO.File]::ReadAllBytes($gameExe)
    $patched = [System.IO.File]::ReadAllBytes($tempExe)
    $changed = @(0..4095 | Where-Object { $original[$_] -ne $patched[$_] })
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $sameTail = [Convert]::ToBase64String($sha.ComputeHash($original, 4096, $original.Length - 4096)) -eq [Convert]::ToBase64String($sha.ComputeHash($patched, 4096, $patched.Length - 4096))
    Check 'only the flag and the checksum changed' (($patched.Length -eq $original.Length) -and $sameTail -and ($changed.Count -ge 1) -and ($changed.Count -le 5) -and ($changed[-1] - $changed[0] -le 100)) "$($changed.Count) byte(s) at $($changed -join ', ')"
    Check 'diagnose now reads the flag as set' ((Dao @('diagnose', $fixProfile)) -match 'DAO_LAA_ENABLED')
    Check 'fixes status says it is in effect' ((Dao @('fixes', 'status', '-p', $fixProfile)) -match '4 GB patch \(large-address-aware-[0-9a-f]{8}\): in effect')
    Check 'applying it again changes nothing' ((Dao @('fixes', 'apply', 'laa', '-p', $fixProfile, '--yes')) -match 'Already in place')

    $off = Dao @('fixes', 'remove', 'laa', '-p', $fixProfile, '--yes')
    Check 'fixes remove laa puts back the original byte-for-byte' (($off -match 'Put back the original') -and ((Get-Sha $tempExe) -eq $tempExeBefore))
    Dao @('fixes', 'apply', 'laa', '-p', $fixProfile, '--yes') | Out-Null
    Check 'fixes rollback puts back the original byte-for-byte' (((Dao @('fixes', 'rollback', '-p', $fixProfile, '--yes')) -match 'Rolled back:') -and ((Get-Sha $tempExe) -eq $tempExeBefore))

    # What Steam's "Verify integrity of game files" does: the original exe comes back behind DAORework's back.
    Dao @('fixes', 'apply', 'laa', '-p', $fixProfile, '--yes') | Out-Null
    Copy-Item $gameExe $tempExe -Force
    Check 'after Steam puts the original back, status says the fix is not in effect' ((Dao @('fixes', 'status', '-p', $fixProfile)) -match 'no longer in effect')
    Check '...and remove only forgets it' (((Dao @('fixes', 'remove', 'laa', '-p', $fixProfile, '--yes')) -match 'Nothing on disk needed changing') -and ((Get-Sha $tempExe) -eq $tempExeBefore))

    $lock = [System.IO.File]::Open($tempExe, 'Open', 'Read', 'Read')
    try { $busy = Dao @('fixes', 'apply', 'laa', '-p', $fixProfile, '--yes') } finally { $lock.Dispose() }
    Check 'an exe in use blocks the plan and is left alone' (($busy -match 'in use, most likely by the running game') -and ((Get-Sha $tempExe) -eq $tempExeBefore))

    # The real install: applied and removed again, the exe checked byte-for-byte.
    $realProfile = 'VerifyFixesReal'
    Dao @('init', $realProfile) | Out-Null
    Dao @('profile', 'set-path', $realProfile, 'install', $InstallDir) | Out-Null
    $realOn = Dao @('fixes', 'apply', 'laa', '-p', $realProfile, '--yes')
    Check 'fixes apply laa works on your real install' (($realOn -match 'Done: \d+ step') -and (Test-LargeAddressAware $gameExe)) (($realOn -split "\r?\n" | Where-Object { $_ -match '^(Done|Note|  x)' }) -join '; ')
    Dao @('fixes', 'remove', 'laa', '-p', $realProfile, '--yes') | Out-Null
    Remove-Item (Join-Path $profilesDir "$realProfile.json") -ErrorAction SilentlyContinue
}
Check 'your real daorigins.exe is byte-for-byte as it began' ((Get-Sha $gameExe) -eq $realExeBefore)

. (Join-Path $PSScriptRoot 'verify-dxvk.ps1')

# Leaves the deployment records, backups and fix library as they were.
foreach ($root in $fixRoots | Where-Object { Test-Path $_ }) {
    Get-ChildItem $root -Recurse -Force | Sort-Object { $_.FullName.Length } -Descending | Where-Object { $fixStateBefore -notcontains $_.FullName } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
}
Remove-Item (Join-Path $profilesDir "$fixProfile.json") -ErrorAction SilentlyContinue
Remove-Item $fixWork -Recurse -Force -ErrorAction SilentlyContinue
