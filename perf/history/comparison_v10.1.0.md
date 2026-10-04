| Method                                    | Parameters                                                                   | Current Source | Baseline Source |  Mean (Current) |    Mean (NuGet) | Mean Δ% | Alloc (Current) | Alloc (NuGet) | Alloc Δ% |
| ----------------------------------------- | ---------------------------------------------------------------------------- | -------------- | --------------- | --------------: | --------------: | ------: | --------------: | ------------: | -------: |
| TableQuery_GenerateFilterCondition        | ValueLength=42;Quotes=0                                                      | LocalProject   | NuGet(10.1.0)   |        29.36 ns |        43.32 ns |  -32.23 |           144 B |         256 B |   -43.75 |
| TableQuery_GenerateFilterCondition        | ValueLength=42;Quotes=1                                                      | LocalProject   | NuGet(10.1.0)   |        42.15 ns |        56.21 ns |  -25.01 |           144 B |         264 B |   -45.45 |
| TableQuery_GenerateFilterCondition        | ValueLength=66;Quotes=0                                                      | LocalProject   | NuGet(10.1.0)   |        34.41 ns |        57.43 ns |  -40.08 |           192 B |         352 B |   -45.45 |
| TableQuery_GenerateFilterCondition        | ValueLength=66;Quotes=1                                                      | LocalProject   | NuGet(10.1.0)   |        57.63 ns |        65.56 ns |   -12.1 |           192 B |         360 B |   -46.67 |
| TableQuery_GenerateFilterCondition        | ValueLength=1022;Quotes=0                                                    | LocalProject   | NuGet(10.1.0)   |       166.34 ns |       646.63 ns |  -74.28 |          2104 B |        4176 B |   -49.62 |
| TableQuery_GenerateFilterCondition        | ValueLength=1022;Quotes=1                                                    | LocalProject   | NuGet(10.1.0)   |       607.07 ns |       681.24 ns |  -10.89 |          4184 B |        4184 B |        0 |
| TableQuery_GenerateFilterCondition        | ValueLength=1023;Quotes=0                                                    | LocalProject   | NuGet(10.1.0)   |       232.66 ns |       621.69 ns |  -62.58 |          4184 B |        4184 B |        0 |
| TableQuery_GenerateFilterCondition        | ValueLength=1023;Quotes=1                                                    | LocalProject   | NuGet(10.1.0)   |       604.32 ns |       649.04 ns |   -6.89 |          4192 B |        4192 B |        0 |
| TableQuery_GenerateFilterConditionForBool |                                                                              | LocalProject   | NuGet(10.1.0)   |        19.40 ns |        16.50 ns |   17.58 |            72 B |          72 B |        0 |
| TableQuery_GenerateFilterConditionForGuid |                                                                              | LocalProject   | NuGet(10.1.0)   |        40.86 ns |        52.14 ns |  -21.63 |           336 B |         336 B |        0 |
| UserOnlyStore_CreateAsync                 |                                                                              | LocalProject   | NuGet(10.1.0)   |        374.2 μs |        375.0 μs |   -0.21 |        51.58 KB |      51.58 KB |        0 |
| UserOnlyStore_DeleteAsync                 |                                                                              | LocalProject   | NuGet(10.1.0)   |        64.19 μs |        66.52 μs |    -3.5 |        17.74 KB |       17.9 KB |    -0.89 |
| UserOnlyStore_GetUserQueryAsync           | UserCount=0                                                                  | LocalProject   | NuGet(10.1.0)   |        17.19 ns |        17.42 ns |   -1.32 |           104 B |         104 B |        0 |
| UserOnlyStore_GetUserQueryAsync           | UserCount=1                                                                  | LocalProject   | NuGet(10.1.0)   |     4,422.94 ns |     4,339.36 ns |    1.93 |          5354 B |        5578 B |    -4.02 |
| UserOnlyStore_GetUserQueryAsync           | UserCount=10                                                                 | LocalProject   | NuGet(10.1.0)   |    50,381.38 ns |    50,767.44 ns |   -0.76 |         70120 B |       72346 B |    -3.08 |
| UserOnlyStore_GetUserQueryAsync           | UserCount=100                                                                | LocalProject   | NuGet(10.1.0)   | 1,042,330.40 ns | 1,068,377.82 ns |   -2.44 |       3156100 B |     3179781 B |    -0.74 |
| UserOnlyStore_MapUserAggregate            | UserCount=10;ClaimsPerUser=2;RolesPerUser=2;LoginsPerUser=2;TokensPerUser=2  | LocalProject   | NuGet(10.1.0)   |        22.21 μs |        21.38 μs |    3.88 |        17.66 KB |      17.66 KB |        0 |
| UserOnlyStore_MapUserAggregate            | UserCount=100;ClaimsPerUser=2;RolesPerUser=2;LoginsPerUser=2;TokensPerUser=2 | LocalProject   | NuGet(10.1.0)   |       218.57 μs |       219.40 μs |   -0.38 |       176.57 KB |     176.57 KB |        0 |
| UserStore_AddToRoleAsync                  |                                                                              | LocalProject   | NuGet(10.1.0)   |        66.11 μs |        62.49 μs |    5.79 |        26.99 KB |      26.99 KB |        0 |
| UserStore_MapUserAggregate                | UserCount=10;ClaimsPerUser=2;RolesPerUser=2;LoginsPerUser=2;TokensPerUser=2  | LocalProject   | NuGet(10.1.0)   |        25.13 μs |        24.98 μs |     0.6 |        18.98 KB |      18.98 KB |        0 |
| UserStore_MapUserAggregate                | UserCount=100;ClaimsPerUser=2;RolesPerUser=2;LoginsPerUser=2;TokensPerUser=2 | LocalProject   | NuGet(10.1.0)   |       242.10 μs |       243.92 μs |   -0.75 |       189.85 KB |     189.84 KB |     0.01 |

Current Source: LocalProject

Baseline Source: NuGet(10.1.0)

Local CSV files:

- perf/artifacts/local/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.TableQueryFilterConditionBenchmarks-report.csv
- perf/artifacts/local/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.TableQueryTypedFilterConditionBenchmarks-report.csv
- perf/artifacts/local/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserAggregateMapBenchmarks-report.csv
- perf/artifacts/local/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserOnlyStoreCreateBenchmarks-report.csv
- perf/artifacts/local/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserOnlyStoreDeleteBenchmarks-report.csv
- perf/artifacts/local/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserOnlyStoreGetUserQueryBenchmarks-report.csv
- perf/artifacts/local/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserStoreAddToRoleBenchmarks-report.csv

NuGet CSV files:

- perf/artifacts/nuget/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.TableQueryFilterConditionBenchmarks-report.csv
- perf/artifacts/nuget/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.TableQueryTypedFilterConditionBenchmarks-report.csv
- perf/artifacts/nuget/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserAggregateMapBenchmarks-report.csv
- perf/artifacts/nuget/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserOnlyStoreCreateBenchmarks-report.csv
- perf/artifacts/nuget/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserOnlyStoreDeleteBenchmarks-report.csv
- perf/artifacts/nuget/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserOnlyStoreGetUserQueryBenchmarks-report.csv
- perf/artifacts/nuget/results/ElCamino.AspNetCore.Identity.AzureTable.Benchmark.UserStoreAddToRoleBenchmarks-report.csv
