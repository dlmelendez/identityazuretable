| Method | Parameters | Current Source | Baseline Source | Mean (Current) | Mean (NuGet) | Mean Δ% | Alloc (Current) | Alloc (NuGet) | Alloc Δ% |
|---|---|---|---|---:|---:|---:|---:|---:|---:|

Current Source: LocalProject
Baseline Source: NuGet(10.0.*)
Local CSV files:
- perf/artifacts/local/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserAggregateMapBenchmarks-report.csv
- perf/artifacts/local/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserOnlyStoreCreateBenchmarks-report.csv
- perf/artifacts/local/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserOnlyStoreDeleteBenchmarks-report.csv
- perf/artifacts/local/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserOnlyStoreGetUserQueryBenchmarks-report.csv
- perf/artifacts/local/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserStoreAddToRoleBenchmarks-report.csv
NuGet CSV files:
- perf/artifacts/nuget/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserAggregateMapBenchmarks-report.csv
- perf/artifacts/nuget/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserOnlyStoreCreateBenchmarks-report.csv
- perf/artifacts/nuget/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserOnlyStoreDeleteBenchmarks-report.csv
- perf/artifacts/nuget/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserOnlyStoreGetUserQueryBenchmarks-report.csv
- perf/artifacts/nuget/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserStoreAddToRoleBenchmarks-report.csv

| UserOnlyStore_CreateAsync |  | LocalProject | NuGet(10.0.*) | 412.0 μs | 398.0 μs | 3.52 | 51.61 KB | 51.61 KB | 0 |
| UserOnlyStore_DeleteAsync |  | LocalProject | NuGet(10.0.*) | 67.04 μs | 68.79 μs | -2.54 | 18 KB | 18.02 KB | -0.11 |
| UserOnlyStore_GetUserQueryAsync | UserCount=0 | LocalProject | NuGet(10.0.*) | 19.43 ns | 21.48 ns | -9.54 | 104 B | 104 B | 0 |
| UserOnlyStore_GetUserQueryAsync | UserCount=1 | LocalProject | NuGet(10.0.*) | 4,742.44 ns | 5,512.07 ns | -20 | 5578 B | 5578 B | 0 |
| UserOnlyStore_GetUserQueryAsync | UserCount=10 | LocalProject | NuGet(10.0.*) | 51,987.46 ns | 50,370.57 ns | 2 | 72356 B | 72356 B | 0 |
| UserOnlyStore_GetUserQueryAsync | UserCount=100 | LocalProject | NuGet(10.0.*) | 2,857,675.36 ns | 2,920,109.15 ns | 0 | 3183029 B | 3184156 B | -0.04 |
| UserOnlyStore_MapUserAggregate | UserCount=10;ClaimsPerUser=2;RolesPerUser=2;LoginsPerUser=2;TokensPerUser=2 | LocalProject | NuGet(10.0.*) | 24.75 μs | 24.74 μs | 0.04 | 20.7 KB | 20.7 KB | 0 |
| UserOnlyStore_MapUserAggregate | UserCount=100;ClaimsPerUser=2;RolesPerUser=2;LoginsPerUser=2;TokensPerUser=2 | LocalProject | NuGet(10.0.*) | 243.25 μs | 244.02 μs | -0.32 | 207.03 KB | 207.03 KB | 0 |
| UserStore_AddToRoleAsync |  | LocalProject | NuGet(10.0.*) | 57.51 μs | 59.25 μs | -2.94 | 26.99 KB | 26.99 KB | 0 |
| UserStore_MapUserAggregate | UserCount=10;ClaimsPerUser=2;RolesPerUser=2;LoginsPerUser=2;TokensPerUser=2 | LocalProject | NuGet(10.0.*) | 28.02 μs | 28.85 μs | -2.88 | 22.81 KB | 22.81 KB | 0 |
| UserStore_MapUserAggregate | UserCount=100;ClaimsPerUser=2;RolesPerUser=2;LoginsPerUser=2;TokensPerUser=2 | LocalProject | NuGet(10.0.*) | 279.88 μs | 280.54 μs | -0.24 | 228.13 KB | 228.13 KB | 0 |
