using Azure.Data.Tables;
using BenchmarkDotNet.Attributes;
using ElCamino.Azure.Data.Tables;

namespace ElCamino.AspNetCore.Identity.AzureTable.Benchmark;

// The conditions are built in setup, so only TableQueryBuilder putting them together is measured.
[MemoryDiagnoser]
[MarkdownExporter]
public class TableQueryBuilderBenchmarks
{
    private string[] _partitionKeyConditions = [];
    private string[] _rowKeyConditions = [];
    private string[] _userAggregateConditions = [];

    // 1 is a filter that stays inside the builder's initial buffer, 50 is the most user ids the stores put in one filter.
    // 16 is a filter that has just outgrown a doubling of the buffer, which is when the growth allocates the most for the length of the filter.
    // 500 is a larger filter than the stores build, of 64,000 to 96,000 chars.
    [Params(1, 10, 16, 50, 500)]
    public int UserCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // 42 chars is the length of a SHA1 hashed key.
        string claimRowKeyCondition = TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, "C_" + new string('c', 40)).ToString();

        _partitionKeyConditions = new string[UserCount];
        _rowKeyConditions = new string[UserCount];
        _userAggregateConditions = new string[UserCount];
        for (int userIndex = 0; userIndex < UserCount; userIndex++)
        {
            string userId = "U_" + userIndex.ToString("x40");
            _partitionKeyConditions[userIndex] = TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, userId).ToString();
            _rowKeyConditions[userIndex] = TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, userId).ToString();
            _userAggregateConditions[userIndex] = TableQuery.CombineFilters(
                _partitionKeyConditions[userIndex],
                TableOperators.And,
                TableQuery.CombineFilters(_rowKeyConditions[userIndex], TableOperators.Or, claimRowKeyCondition)).ToString();
        }
    }

    // The builder calls of UserOnlyStore.GetUserQueryAsync.
    [Benchmark]
    public int TableQueryBuilder_UserQueryFilter()
    {
        TableQueryBuilder queryBuilder = new TableQueryBuilder();
        for (int userIndex = 0; userIndex < UserCount; userIndex++)
        {
            if (userIndex > 0)
            {
                queryBuilder.CombineFilters(TableOperator.Or);
                queryBuilder.BeginGroup();
                queryBuilder.AddFilter(_partitionKeyConditions[userIndex]);
                queryBuilder.CombineFilters(TableOperator.And);
                queryBuilder.AddFilter(_rowKeyConditions[userIndex]);
                queryBuilder.EndGroup();
            }
            else
            {
                queryBuilder.AddFilter(_partitionKeyConditions[userIndex]);
                queryBuilder.CombineFilters(TableOperator.And);
                queryBuilder.AddFilter(_rowKeyConditions[userIndex]);
                queryBuilder.GroupAll();
            }
        }

        return queryBuilder.ToString().Length;
    }

    // The builder calls of UserOnlyStore.GetUserAggregateQueryAsync, with the condition per user id of GetUsersForClaimAsync.
    [Benchmark]
    public int TableQueryBuilder_UserAggregateFilter()
    {
        TableQueryBuilder queryBuilder = new TableQueryBuilder();
        for (int userIndex = 0; userIndex < UserCount; userIndex++)
        {
            if (userIndex > 0)
            {
                queryBuilder.CombineFilters(TableOperator.Or, _userAggregateConditions[userIndex]);
            }
            else
            {
                queryBuilder.AddFilter(_userAggregateConditions[userIndex]);
            }
        }

        return queryBuilder.ToString().Length;
    }
}
