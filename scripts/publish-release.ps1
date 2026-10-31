#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Builds the release zip: self-contained single-file DAORework.exe (the window) and dao.exe (the CLI) for Windows,
  with the README, license, changelog and third-party licenses.
.EXAMPLE
  ./scripts/publish-release.ps1                  # version from Directory.Build.props, output in ./artifacts
  ./scripts/publish-release.ps1 -NoZip           # build the folder only, so the executables can be signed (ADR-0018)
  ./scripts/publish-release.ps1 -ZipOnly         # zip and checksum the folder a -NoZip run built, after signing
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$Runtime = 'win-x64',
    [string]$OutDir = 'artifacts',
    [switch]$NoZip,
    [switch]$ZipOnly
)

$ErrorActionPreference = 'Stop'
if ($NoZip -and $ZipOnly) { throw 'Use -NoZip or -ZipOnly, not both.' }
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
if (-not $Version) { $Version = ([xml](Get-Content 'Directory.Build.props' -Raw)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1 }
if (-not $Version) { throw 'No <Version> in Directory.Build.props.' }

$staging = Join-Path $OutDir "DAORework-$Version-$Runtime"
if ($ZipOnly) {
    if (-not (Test-Path (Join-Path $staging 'DAORework.exe'))) { throw "No built folder at $staging; run with -NoZip first." }
}
else {
    Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force -Path $staging, (Join-Path $staging 'dotnet') | Out-Null
    $publishArgs = @('-c', 'Release', '-r', $Runtime, '--self-contained', '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true', '-p:EnableCompressionInSingleFile=true', '-p:DebugType=none')

    foreach ($app in @(@{ Project = 'src/DAORework.App'; Exe = 'DAORework.App.exe'; As = 'DAORework.exe' }, @{ Project = 'src/DAORework.Cli'; Exe = 'DAORework.Cli.exe'; As = 'dao.exe' })) {
        $out = Join-Path $OutDir "publish/$(Split-Path -Leaf $app.Project)"
        dotnet publish $app.Project @publishArgs -o $out
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish $($app.Project) failed." }
        Copy-Item (Join-Path $out $app.Exe) (Join-Path $staging $app.As)
}

Copy-Item README.md, LICENSE, CHANGELOG.md -Destination $staging
Copy-Item 'src/DAORework.App/Assets/Fonts/OFL.txt' (Join-Path $staging 'Cinzel-OFL.txt')
./scripts/third-party-licenses.ps1 -AssetsFiles 'src/DAORework.App/obj/project.assets.json', 'src/DAORework.Cli/obj/project.assets.json' -Runtime $Runtime -OutFile (Join-Path $staging 'THIRD-PARTY-LICENSES.md')

$cache = ((Get-Content 'src/DAORework.Cli/obj/project.assets.json' -Raw | ConvertFrom-Json).packageFolders.PSObject.Properties | Select-Object -First 1).Name
$runtimePack = Get-ChildItem (Join-Path $cache "microsoft.netcore.app.runtime.$Runtime") -Directory | Sort-Object { [version]($_.Name -replace '-.*$', '') } | Select-Object -Last 1
Copy-Item (Join-Path $runtimePack.FullName 'LICENSE.TXT'), (Join-Path $runtimePack.FullName 'THIRD-PARTY-NOTICES.TXT') -Destination (Join-Path $staging 'dotnet')
}

if ($NoZip) {
    Write-Host "Release folder: $staging (not zipped)"
    return
}

$zip = Join-Path $OutDir "DAORework-$Version-$Runtime.zip"
Remove-Item $zip -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $zip
$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -Path "$zip.sha256" -Value "$hash  $(Split-Path -Leaf $zip)" -NoNewline
Write-Host "Release zip: $zip ($([math]::Round((Get-Item $zip).Length / 1MB, 1)) MB, sha256 $hash)"
