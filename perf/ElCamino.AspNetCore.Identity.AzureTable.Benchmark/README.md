# ElCamino.AspNetCore.Identity.AzureTable.Benchmark

This project benchmarks core allocation-sensitive mapping paths for `UserOnlyStore` and `UserStore`.

## Run against local source (current branch)

```powershell
dotnet run -c Release --project perf/ElCamino.AspNetCore.Identity.AzureTable.Benchmark -- --filter *UserAggregateMapBenchmarks*
```

## Run against a published NuGet package version

```powershell
dotnet run -c Release --project perf/ElCamino.AspNetCore.Identity.AzureTable.Benchmark -p:UseLocalProject=false -p:BenchmarkPackageVersion=10.0.0 -- --filter *UserAggregateMapBenchmarks*
```

## Export comparable reports

```powershell
dotnet run -c Release --project perf/ElCamino.AspNetCore.Identity.AzureTable.Benchmark -- --filter *UserAggregateMapBenchmarks* --exporters json github

dotnet run -c Release --project perf/ElCamino.AspNetCore.Identity.AzureTable.Benchmark -p:UseLocalProject=false -p:BenchmarkPackageVersion=10.0.0 -- --filter *UserAggregateMapBenchmarks* --exporters json github
```

Compare generated JSON/Markdown outputs to evaluate mean execution time and allocations between local code and package baseline.

## One-command side-by-side comparison

Run from solution root:

```powershell
pwsh ./perf/compare.ps1
```

Optional parameters:

```powershell
pwsh ./perf/compare.ps1 -PackageVersion 10.0.* -Filter "*UserAggregateMapBenchmarks*"
```

Outputs:

- `perf/artifacts/comparison/comparison.csv`
- `perf/artifacts/comparison/comparison.md`
