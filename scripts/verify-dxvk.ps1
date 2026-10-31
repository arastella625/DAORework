# Dot-sourced by verify-fixes.ps1 (ROADMAP P2-08, ADR-0013): applies and removes DXVK from its GitHub release, first
# on the throwaway install and then on the real one, and checks bin_ship\d3d9.dll ends as it began.
# Uses Check, Dao, Get-Sha, $fixWork, $fixProfile, $InstallDir, $profilesDir and $results from the calling scripts.

Write-Host "== managed fixes (P2-08: DXVK)" -ForegroundColor Cyan
$releases = @{}
foreach ($v in '2.2', '2.3') {
    $releases[$v] = Join-Path $fixWork "dxvk-$v.tar.gz"
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest "https://github.com/doitsujin/dxvk/releases/download/v$v/dxvk-$v.tar.gz" -OutFile $releases[$v] -UseBasicParsing
    }
    catch { Write-Host "DXVK $v download failed: $($_.Exception.Message)" -ForegroundColor Red }
}
Check 'DXVK 2.2 and 2.3 downloaded from GitHub' ((Test-Path $releases['2.2']) -and (Test-Path $releases['2.3']))

$tempDll = Join-Path (Join-Path $fixWork 'bin_ship') 'd3d9.dll'
if ((Test-Path $releases['2.2']) -and (Test-Path $releases['2.3'])) {
    $extracted = Join-Path $fixWork 'extracted'
    New-Item -ItemType Directory -Force -Path $extracted | Out-Null
    tar -xzf $releases['2.2'] -C $extracted
    tar -xzf $releases['2.3'] -C $extracted
    $dll22 = Join-Path (Join-Path (Join-Path $extracted 'dxvk-2.2') 'x32') 'd3d9.dll'
    $x64 = Join-Path (Join-Path (Join-Path $extracted 'dxvk-2.3') 'x64') 'd3d9.dll'

    Check 'apply dxvk without a release says where to get one' ((Dao @('fixes', 'apply', 'dxvk', '-p', $fixProfile, '--yes')) -match 'github.com/doitsujin/dxvk/releases')
    $dry = Dao @('fixes', 'apply', 'dxvk', $releases['2.3'], '-p', $fixProfile, '--dry-run')
    Check 'a DXVK dry run shows the plan and changes nothing' (($dry -match 'Add bin_ship/d3d9.dll') -and -not (Test-Path $tempDll))
    $on = Dao @('fixes', 'apply', 'dxvk', $releases['2.3'], '-p', $fixProfile, '--yes')
    Check 'fixes apply dxvk places the x32 DLL from the .tar.gz' (($on -match 'Done: \d+ step') -and (Test-Path $tempDll)) (($on -split "\r?\n" | Where-Object { $_ -match '^(Apply|Done|  x)' }) -join '; ')
    Check 'diagnose recognizes it as DXVK v2.3' ((Dao @('diagnose', $fixProfile)) -match 'DAO_KNOWN_ITEMS_FOUND: .*DXVK v2\.3')
    Check 'fixes status says DXVK 2.3 is in effect' ((Dao @('fixes', 'status', '-p', $fixProfile)) -match 'DXVK 2\.3 \(dxvk-2-3-[0-9a-f]{8}\): in effect')
    Check 'fixes remove dxvk deletes the DLL it placed' (((Dao @('fixes', 'remove', 'dxvk', '-p', $fixProfile, '--yes')) -match 'Delete bin_ship/d3d9.dll') -and -not (Test-Path $tempDll))

    Copy-Item $dll22 $tempDll
    $before22 = Get-Sha $tempDll
    Check 'DXVK 2.3 replaces a 2.2 placed by hand' (((Dao @('fixes', 'apply', 'dxvk', $releases['2.3'], '-p', $fixProfile, '--yes')) -match 'Replace bin_ship/d3d9.dll') -and ((Get-Sha $tempDll) -ne $before22))
    Check '...and removing it puts the 2.2 back byte-for-byte' (((Dao @('fixes', 'remove', 'dxvk', '-p', $fixProfile, '--yes')) -match 'Put back the original') -and ((Get-Sha $tempDll) -eq $before22))
    Remove-Item $tempDll

    Check 'the x64 build is refused' ((Dao @('fixes', 'apply', 'dxvk', $x64, '-p', $fixProfile, '--yes')) -match 'x32 folder')
    Set-Content $tempDll 'not dxvk' -NoNewline
    $blocked = Dao @('fixes', 'apply', 'dxvk', $releases['2.3'], '-p', $fixProfile, '--yes')
    Check 'another d3d9.dll blocks the plan and is left alone' (($blocked -match 'Blocked; nothing will be changed') -and ((Get-Content $tempDll -Raw) -eq 'not dxvk'))
    Remove-Item $tempDll

    # The real install, only when it has no d3d9.dll of its own: DXVK added and removed again.
    $realDll = Join-Path (Join-Path $InstallDir 'bin_ship') 'd3d9.dll'
    if (Test-Path $realDll) {
        $results.Add("[SKIP] DXVK on your real install (it already has a d3d9.dll, which this check leaves alone)")
    }
    else {
        $realProfile = 'VerifyDxvkReal'
        Dao @('init', $realProfile) | Out-Null
        Dao @('profile', 'set-path', $realProfile, 'install', $InstallDir) | Out-Null
        $realOn = Dao @('fixes', 'apply', 'dxvk', $releases['2.3'], '-p', $realProfile, '--yes')
        Check 'fixes apply dxvk works on your real install' (($realOn -match 'Done: \d+ step') -and (Test-Path $realDll))
        Dao @('fixes', 'remove', 'dxvk', '-p', $realProfile, '--yes') | Out-Null
        Check '...and fixes remove dxvk leaves no d3d9.dll behind' (-not (Test-Path $realDll))
        Remove-Item (Join-Path $profilesDir "$realProfile.json") -ErrorAction SilentlyContinue
    }
}
