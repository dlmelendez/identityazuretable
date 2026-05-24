# ElCamino.AspNetCore.Identity.AzureTable.Benchmark

This project benchmarks core allocation-sensitive mapping paths and mocked store operation paths for `UserOnlyStore` and `UserStore`.

## Run against local source (current branch)

```powershell
dotnet run -c Release --project perf/ElCamino.AspNetCore.Identity.AzureTable.Benchmark -- --filter *UserAggregateMapBenchmarks*
```

Run the mocked store operation benchmarks:

```powershell
dotnet run -c Release --project perf/ElCamino.AspNetCore.Identity.AzureTable.Benchmark -- --filter *Store*Benchmarks*
```

Run all benchmark classes:

```powershell
dotnet run -c Release --project perf/ElCamino.AspNetCore.Identity.AzureTable.Benchmark -- --filter *Benchmarks*
```

The store operation benchmarks use benchmark-local in-memory `TableClient` test doubles. They measure store control flow, entity mapping, query construction, batching behavior, and allocations without Azurite, Azure Storage network latency, or external table state.

## Run against a published NuGet package version

```powershell
dotnet run -c Release --project perf/ElCamino.AspNetCore.Identity.AzureTable.Benchmark -p:UseLocalProject=false -p:BenchmarkPackageVersion=10.0.0 -- --filter *Benchmarks*
```

## Export comparable reports

```powershell
dotnet run -c Release --project perf/ElCamino.AspNetCore.Identity.AzureTable.Benchmark -- --filter *Benchmarks* --exporters json github

dotnet run -c Release --project perf/ElCamino.AspNetCore.Identity.AzureTable.Benchmark -p:UseLocalProject=false -p:BenchmarkPackageVersion=10.0.0 -- --filter *Benchmarks* --exporters json github
```

Compare generated JSON/Markdown outputs to evaluate mean execution time and allocations between local code and package baseline.

## One-command side-by-side comparison

Run from solution root:

```powershell
pwsh ./perf/compare.ps1
```

Optional parameters:

```powershell
pwsh ./perf/compare.ps1 -PackageVersion 10.0.* -Filter "*Benchmarks*"
```

Outputs:

- `perf/artifacts/comparison/comparison.csv`
- `perf/artifacts/comparison/comparison.md`
