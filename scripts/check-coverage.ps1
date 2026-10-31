#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Enforces the line-coverage floor on DAORework.Core and DAORework.Application.
.DESCRIPTION
  Reads every Cobertura report under -ResultsDirectory (produced by
  `dotnet test --collect:"XPlat Code Coverage"`), sums line coverage over the
  gated assemblies, and fails when the combined rate is below -Floor percent.
  ENGINEERING_STANDARDS §5 sets the target at 80; CI passes the current ratchet value.
#>
[CmdletBinding()]
param(
    [string]$ResultsDirectory = 'TestResults',
    [double]$Floor = 80,
    [string[]]$Assemblies = @('DAORework.Core', 'DAORework.Application')
)

$ErrorActionPreference = 'Stop'

$reports = Get-ChildItem -Path $ResultsDirectory -Recurse -Filter 'coverage.cobertura.xml' -File
if (-not $reports) {
    Write-Host "No coverage.cobertura.xml found under '$ResultsDirectory'."
    exit 1
}

# Several test projects each produce a report that includes the gated assemblies,
# so a line is covered when ANY report saw it hit. Union by (assembly, class, line):
# the class name is stable across reports, whereas file names are relative to each
# report's own source root.
$covered = @{}
foreach ($report in $reports) {
    [xml]$xml = Get-Content $report.FullName
    foreach ($package in $xml.coverage.packages.package) {
        if ($Assemblies -notcontains $package.name) { continue }
        if (-not $covered.ContainsKey($package.name)) { $covered[$package.name] = @{} }
        $lines = $covered[$package.name]
        foreach ($class in $package.classes.class) {
            foreach ($line in $class.lines.line) {
                $key = "$($class.name):$($line.number)"
                if (-not $lines.ContainsKey($key)) { $lines[$key] = $false }
                if ([int]$line.hits -gt 0) { $lines[$key] = $true }
            }
        }
    }
}

$totalLines = 0
$coveredLines = 0
$perAssembly = @{}
foreach ($name in $covered.Keys) {
    $lines = $covered[$name]
    $hit = @($lines.Values | Where-Object { $_ }).Count
    $perAssembly[$name] = @{ Lines = $lines.Count; Covered = $hit }
    $totalLines += $lines.Count
    $coveredLines += $hit
}

foreach ($name in $Assemblies) {
    if ($perAssembly.ContainsKey($name)) {
        $a = $perAssembly[$name]
        $rate = if ($a.Lines -gt 0) { 100.0 * $a.Covered / $a.Lines } else { 0 }
        Write-Host ("{0,-24} {1,6:N1}%  ({2}/{3} lines)" -f $name, $rate, $a.Covered, $a.Lines)
    }
    else {
        Write-Host ("{0,-24} not found in any report" -f $name)
    }
}

if ($totalLines -eq 0) {
    Write-Host 'No gated assemblies were found in the coverage reports.'
    exit 1
}

$combined = 100.0 * $coveredLines / $totalLines
Write-Host ("{0,-24} {1,6:N1}%  (floor {2}%)" -f 'Combined', $combined, $Floor)

if ($combined -lt $Floor) {
    Write-Host "Coverage gate failed: $([math]::Round($combined, 1))% is below the floor of $Floor%."
    exit 1
}
Write-Host 'Coverage gate passed.'
