param(
    [string]$ProjectPath = "perf/ElCamino.AspNetCore.Identity.AzureTable.Benchmark/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.csproj",
    [string]$Filter = "*Benchmarks*",
    [string]$PackageVersion = "10.0.*",
    [string]$ArtifactsRoot = "perf/artifacts",
    [switch]$SkipRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Parse-BdnMetric {
    param(
        [AllowNull()][string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return [pscustomobject]@{ Number = $null; Unit = $null; Raw = $Value }
    }

    $text = $Value.Trim()

    if ($text -match '^(?<num>[-+]?\d+(?:\.\d+)?)\s*(?<unit>.*)$') {
        $num = $Matches['num']
        $unit = $Matches['unit'].Trim()
        return [pscustomobject]@{
            Number = [double]::Parse($num, [System.Globalization.CultureInfo]::InvariantCulture)
            Unit   = $unit
            Raw    = $text
        }
    }

    return [pscustomobject]@{ Number = $null; Unit = $null; Raw = $text }
}

function Get-BenchmarkCsvs {
    param(
        [Parameter(Mandatory)][string]$Root
    )

    return @(Get-ChildItem -Path $Root -Recurse -Filter '*-report.csv' -File |
        Sort-Object FullName)
}

function Invoke-BenchmarkRun {
    param(
        [Parameter(Mandatory)][string]$Project,
        [Parameter(Mandatory)][string]$FilterValue,
        [Parameter(Mandatory)][string]$ArtifactsPath,
        [switch]$UseNuget,
        [string]$NugetVersion
    )

    $arguments = @(
        'run',
        '-c', 'Release',
        '--project', $Project
    )

    if ($UseNuget) {
        $arguments += @('-p:UseLocalProject=false', "-p:BenchmarkPackageVersion=$NugetVersion")
    }

    $arguments += @(
        '--',
        '--filter', $FilterValue,
        '--exporters', 'csv', 'github', 'json',
        '--artifacts', $ArtifactsPath
    )

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Benchmark run failed with exit code $LASTEXITCODE."
    }
}

function Get-JoinKey {
    param(
        [Parameter(Mandatory)]$Row
    )

    $exclude = @(
        'Namespace', 'Type', 'Method', 'Job', 'Mean', 'Error', 'StdDev',
        'Median', 'Gen0', 'Gen1', 'Gen2', 'Allocated', 'Alloc Ratio',
        'Op/s', 'Ratio', 'RatioSD', 'Rank', 'Baseline', 'Origin'
    )

    $paramParts = @(Get-ParameterParts -Row $Row)

    return "Method=$($Row.Method)|$([string]::Join(';', $paramParts))"
}

function Get-ParameterParts {
    param(
        [Parameter(Mandatory)]$Row
    )

    $columns = @($Row.PSObject.Properties.Name)
    $start = [Array]::IndexOf($columns, 'WarmupCount') + 1
    $end = [Array]::IndexOf($columns, 'Mean')

    if ($start -le 0 -or $end -le $start) {
        return @()
    }

    $paramColumns = $columns[$start..($end - 1)]

    $paramParts = foreach ($col in $paramColumns) {
        "$col=$($Row.$col)"
    }

    return $paramParts
}

$localArtifacts = Join-Path $ArtifactsRoot 'local'
$nugetArtifacts = Join-Path $ArtifactsRoot 'nuget'
$comparisonArtifacts = Join-Path $ArtifactsRoot 'comparison'
$currentSource = 'LocalProject'
$baselineSource = "NuGet($PackageVersion)"

New-Item -Path $localArtifacts -ItemType Directory -Force | Out-Null
New-Item -Path $nugetArtifacts -ItemType Directory -Force | Out-Null
New-Item -Path $comparisonArtifacts -ItemType Directory -Force | Out-Null

if (-not $SkipRun) {
    Write-Host "Running benchmarks against current local project..." -ForegroundColor Cyan
    Invoke-BenchmarkRun -Project $ProjectPath -FilterValue $Filter -ArtifactsPath $localArtifacts

    Write-Host "Running benchmarks against NuGet package version '$PackageVersion'..." -ForegroundColor Cyan
    Invoke-BenchmarkRun -Project $ProjectPath -FilterValue $Filter -ArtifactsPath $nugetArtifacts -UseNuget -NugetVersion $PackageVersion
}

$localCsvs = Get-BenchmarkCsvs -Root $localArtifacts
$nugetCsvs = Get-BenchmarkCsvs -Root $nugetArtifacts

if ($localCsvs.Count -lt 1) {
    throw "Could not find local benchmark CSV in '$localArtifacts'."
}

if ($nugetCsvs.Count -lt 1) {
    throw "Could not find NuGet benchmark CSV in '$nugetArtifacts'."
}

Write-Host "Using local CSV files:"
$localCsvs | ForEach-Object { Write-Host "  $($_.FullName)" }
Write-Host "Using NuGet CSV files:"
$nugetCsvs | ForEach-Object { Write-Host "  $($_.FullName)" }

$localRows = foreach ($csv in $localCsvs) {
    Import-Csv -Path $csv.FullName
}

$nugetRows = foreach ($csv in $nugetCsvs) {
    Import-Csv -Path $csv.FullName
}

$localByKey = @{}
foreach ($row in $localRows) {
    $localByKey[(Get-JoinKey -Row $row)] = $row
}

$nugetByKey = @{}
foreach ($row in $nugetRows) {
    $nugetByKey[(Get-JoinKey -Row $row)] = $row
}

$allKeys = @($localByKey.Keys + $nugetByKey.Keys | Sort-Object -Unique)

$comparison = foreach ($key in $allKeys) {
    $local = $localByKey[$key]
    $nuget = $nugetByKey[$key]

    $method = if ($local) { $local.Method } elseif ($nuget) { $nuget.Method } else { '' }

    $meanLocal = Parse-BdnMetric -Value $(if ($local) { $local.Mean } else { $null })
    $meanNuget = Parse-BdnMetric -Value $(if ($nuget) { $nuget.Mean } else { $null })
    $allocLocal = Parse-BdnMetric -Value $(if ($local) { $local.Allocated } else { $null })
    $allocNuget = Parse-BdnMetric -Value $(if ($nuget) { $nuget.Allocated } else { $null })
    $parameterText = if ($key -match '^Method=[^|]+\|(?<params>.*)$') { $Matches['params'] } else { '' }

    $meanDeltaPct = $null
    if ($meanNuget.Number -ne $null -and [math]::Abs($meanNuget.Number) -gt [double]::Epsilon -and $meanLocal.Number -ne $null) {
        $meanDeltaPct = (($meanLocal.Number - $meanNuget.Number) / $meanNuget.Number) * 100
    }

    $allocDeltaPct = $null
    if ($allocNuget.Number -ne $null -and [math]::Abs($allocNuget.Number) -gt [double]::Epsilon -and $allocLocal.Number -ne $null) {
        $allocDeltaPct = (($allocLocal.Number - $allocNuget.Number) / $allocNuget.Number) * 100
    }

    [pscustomobject]@{
        Key                 = $key
        Method              = $method
        Parameters          = $parameterText
        Current_Source      = $currentSource
        Baseline_Source     = $baselineSource
        Mean_Current        = $meanLocal.Raw
        Mean_NuGet          = $meanNuget.Raw
        Mean_DeltaPct       = if ($meanDeltaPct -ne $null) { [math]::Round($meanDeltaPct, 2) } else { $null }
        Allocated_Current   = $allocLocal.Raw
        Allocated_NuGet     = $allocNuget.Raw
        Allocated_DeltaPct  = if ($allocDeltaPct -ne $null) { [math]::Round($allocDeltaPct, 2) } else { $null }
    }
}

$comparisonCsv = Join-Path $comparisonArtifacts 'comparison.csv'
$comparisonMd = Join-Path $comparisonArtifacts 'comparison.md'

$comparison |
    Sort-Object Method, Key |
    Export-Csv -Path $comparisonCsv -NoTypeInformation -Encoding UTF8

$header = "| Method | Parameters | Current Source | Baseline Source | Mean (Current) | Mean (NuGet) | Mean Δ% | Alloc (Current) | Alloc (NuGet) | Alloc Δ% |"
$separator = "|---|---|---|---|---:|---:|---:|---:|---:|---:|"

$lines = @($header, $separator)
$lines += ""
$lines += "Current Source: $currentSource"
$lines += "Baseline Source: $baselineSource"
$lines += "Local CSV files:"
$lines += $localCsvs | ForEach-Object { "- $($_.FullName)" }
$lines += "NuGet CSV files:"
$lines += $nugetCsvs | ForEach-Object { "- $($_.FullName)" }
$lines += ""
foreach ($row in ($comparison | Sort-Object Method, Key)) {
    $lines += "| $($row.Method) | $($row.Parameters) | $($row.Current_Source) | $($row.Baseline_Source) | $($row.Mean_Current) | $($row.Mean_NuGet) | $($row.Mean_DeltaPct) | $($row.Allocated_Current) | $($row.Allocated_NuGet) | $($row.Allocated_DeltaPct) |"
}

$lines | Set-Content -Path $comparisonMd -Encoding UTF8

Write-Host "Comparison complete."
Write-Host "CSV: $comparisonCsv"
Write-Host "Markdown: $comparisonMd"
