#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Writes THIRD-PARTY-LICENSES.md for a release: every NuGet package whose code ships inside the published
  executables, with its version, copyright, license and project link, followed by the license texts.
.DESCRIPTION
  Reads the restore output (obj/project.assets.json) of each published project for the given runtime, keeps the
  packages that contribute runtime or native files (analyzers and build-only packages ship nothing), and reads each
  package's .nuspec from the NuGet cache. Expression licenses get the matching text from docs/licenses; packages
  that ship a license file get that file verbatim.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string[]]$AssetsFiles,
    [Parameter(Mandatory)] [string]$OutFile,
    [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function Test-Ships($library) {
    foreach ($section in 'runtime', 'native', 'runtimeTargets') {
        $assets = $library.$section
        if ($assets -and ($assets.PSObject.Properties.Name | Where-Object { -not $_.EndsWith('/_._') })) { return $true }
    }
    return $false
}

$packages = @{}
foreach ($assetsFile in $AssetsFiles) {
    $assets = Get-Content $assetsFile -Raw | ConvertFrom-Json
    $cache = ($assets.packageFolders.PSObject.Properties | Select-Object -First 1).Name
    $target = $assets.targets.PSObject.Properties | Where-Object { $_.Name -like "*/$Runtime" } | Select-Object -First 1
    if (-not $target) { throw "No '$Runtime' target in $assetsFile; restore or publish with -r $Runtime first." }
    foreach ($entry in $target.Value.PSObject.Properties) {
        if ($entry.Value.type -ne 'package' -or -not (Test-Ships $entry.Value)) { continue }
        $name, $version = $entry.Name -split '/', 2
        $packages["$name/$version"] = [pscustomobject]@{ Name = $name; Version = $version; Folder = Join-Path $cache "$($name.ToLowerInvariant())/$($version.ToLowerInvariant())" }
    }
}

$rows = foreach ($package in $packages.Values | Sort-Object Name) {
    $nuspec = Get-ChildItem $package.Folder -Filter '*.nuspec' | Select-Object -First 1
    [xml]$xml = Get-Content $nuspec.FullName -Raw
    $meta = $xml.package.metadata
    $license = $meta.license
    [pscustomobject]@{
        Name = $package.Name
        Version = $package.Version
        Copyright = if ($meta.copyright) { $meta.copyright } else { $meta.authors }
        Project = if ($meta.projectUrl) { $meta.projectUrl } else { "https://www.nuget.org/packages/$($package.Name)" }
        LicenseType = if ($license) { $license.type } else { 'url' }
        License = if ($license) { $license.'#text' } else { $meta.licenseUrl }
        Folder = $package.Folder
    }
}

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('# Third-party licenses')
$lines.Add('')
$lines.Add('DAORework is licensed under PolyForm Noncommercial 1.0.0 (see LICENSE). The executables in this release also contain the packages below,')
$lines.Add('each under its own license. The .NET runtime bundled in the executables is listed in dotnet/LICENSE.TXT and')
$lines.Add('dotnet/THIRD-PARTY-NOTICES.TXT.')
$lines.Add('')
$lines.Add('| Package | Version | License | Copyright | Project |')
$lines.Add('|---|---|---|---|---|')
foreach ($row in $rows) {
    $licenseCell = if ($row.LicenseType -eq 'file') { "see *$($row.Name)* below" } else { $row.License }
    $lines.Add("| $($row.Name) | $($row.Version) | $licenseCell | $(($row.Copyright -replace '\|', '/').Trim()) | $($row.Project) |")
}

foreach ($expression in $rows | Where-Object LicenseType -eq 'expression' | Select-Object -ExpandProperty License -Unique | Sort-Object) {
    $text = Join-Path $root "docs/licenses/$expression.txt"
    if (-not (Test-Path $text)) { throw "No license text for '$expression'; add docs/licenses/$expression.txt." }
    $lines.Add('')
    $lines.Add("## $expression")
    $lines.Add('')
    $lines.Add('```')
    $lines.Add((Get-Content $text -Raw).TrimEnd())
    $lines.Add('```')
}

foreach ($row in $rows | Where-Object LicenseType -eq 'file') {
    $lines.Add('')
    $lines.Add("## $($row.Name)")
    $lines.Add('')
    $lines.Add('```')
    $lines.Add((Get-Content (Join-Path $row.Folder $row.License) -Raw).TrimEnd())
    $lines.Add('```')
}

foreach ($row in $rows | Where-Object LicenseType -eq 'url') {
    Write-Warning "$($row.Name) $($row.Version) declares only a license URL ($($row.License)); check it by hand."
}

Set-Content -Path $OutFile -Value $lines -Encoding utf8
Write-Host "Wrote $OutFile ($(@($rows).Count) packages)."
