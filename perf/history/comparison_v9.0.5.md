| Method | Parameters | Current Source | Baseline Source | Mean (Current) | Mean (NuGet) | Mean Δ% | Alloc (Current) | Alloc (NuGet) | Alloc Δ% |
|---|---|---|---|---:|---:|---:|---:|---:|---:|

Current Source: LocalProject
Baseline Source: NuGet(9.0.5)
Local CSV files:
- C:\Users\dave\source\repos\identityazuretable\perf\artifacts\local\results\ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserAggregateMapBenchmarks-report.csv
- C:\Users\dave\source\repos\identityazuretable\perf\artifacts\local\results\ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserOnlyStoreCreateBenchmarks-report.csv
- C:\Users\dave\source\repos\identityazuretable\perf\artifacts\local\results\ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserOnlyStoreDeleteBenchmarks-report.csv
- C:\Users\dave\source\repos\identityazuretable\perf\artifacts\local\results\ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserOnlyStoreGetUserQueryBenchmarks-report.csv
- C:\Users\dave\source\repos\identityazuretable\perf\artifacts\local\results\ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserStoreAddToRoleBenchmarks-report.csv
NuGet CSV files:
- C:\Users\dave\source\repos\identityazuretable\perf\artifacts\nuget\results\ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserAggregateMapBenchmarks-report.csv
- C:\Users\dave\source\repos\identityazuretable\perf\artifacts\nuget\results\ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserOnlyStoreCreateBenchmarks-report.csv
- C:\Users\dave\source\repos\identityazuretable\perf\artifacts\nuget\results\ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserOnlyStoreDeleteBenchmarks-report.csv
- C:\Users\dave\source\repos\identityazuretable\perf\artifacts\nuget\results\ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserOnlyStoreGetUserQueryBenchmarks-report.csv
- C:\Users\dave\source\repos\identityazuretable\perf\artifacts\nuget\results\ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserStoreAddToRoleBenchmarks-report.csv

| UserOnlyStore_CreateAsync |  | LocalProject | NuGet(9.0.5) | 395.7 μs | 410.4 μs | -3.58 | 52.08 KB | 52.08 KB | 0 |
| UserOnlyStore_DeleteAsync |  | LocalProject | NuGet(9.0.5) | 67.43 μs | 68.05 μs | -0.91 | 18 KB | 18 KB | 0 |
| UserOnlyStore_GetUserQueryAsync | UserCount=0 | LocalProject | NuGet(9.0.5) | 19.55 ns | 19.11 ns | 2.3 | 104 B | 104 B | 0 |
| UserOnlyStore_GetUserQueryAsync | UserCount=1 | LocalProject | NuGet(9.0.5) | 4,741.95 ns | 4,396.01 ns | 0 | 5578 B | 5578 B | 0 |
| UserOnlyStore_GetUserQueryAsync | UserCount=10 | LocalProject | NuGet(9.0.5) | 51,588.76 ns | 49,851.10 ns | 4.08 | 72345 B | 72356 B | -0.02 |
| UserOnlyStore_GetUserQueryAsync | UserCount=100 | LocalProject | NuGet(9.0.5) | 1,092,449.01 ns | 1,084,982.31 ns | 0 | 3181718 B | 3182644 B | -0.03 |
| UserOnlyStore_MapUserAggregate | UserCount=10;ClaimsPerUser=2;RolesPerUser=2;LoginsPerUser=2;TokensPerUser=2 | LocalProject | NuGet(9.0.5) | 23.71 μs | 24.74 μs | -4.16 | 20.7 KB | 20.7 KB | 0 |
| UserOnlyStore_MapUserAggregate | UserCount=100;ClaimsPerUser=2;RolesPerUser=2;LoginsPerUser=2;TokensPerUser=2 | LocalProject | NuGet(9.0.5) | 243.20 μs | 240.26 μs | 1.22 | 207.03 KB | 207.03 KB | 0 |
| UserStore_AddToRoleAsync |  | LocalProject | NuGet(9.0.5) | 57.64 μs | 59.01 μs | -2.32 | 26.99 KB | 26.99 KB | 0 |
| UserStore_MapUserAggregate | UserCount=10;ClaimsPerUser=2;RolesPerUser=2;LoginsPerUser=2;TokensPerUser=2 | LocalProject | NuGet(9.0.5) | 27.87 μs | 28.36 μs | -1.73 | 22.81 KB | 22.81 KB | 0 |
| UserStore_MapUserAggregate | UserCount=100;ClaimsPerUser=2;RolesPerUser=2;LoginsPerUser=2;TokensPerUser=2 | LocalProject | NuGet(9.0.5) | 274.48 μs | 279.76 μs | -1.89 | 228.13 KB | 228.13 KB | 0 |
