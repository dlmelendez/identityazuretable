using Azure.Data.Tables;
using BenchmarkDotNet.Attributes;

namespace ElCamino.AspNetCore.Identity.AzureTable.Benchmark;

[MemoryDiagnoser]
[MarkdownExporter]
public class TableQueryFilterConditionBenchmarks
{
    private string _value = string.Empty;

    // 42 and 66 are the lengths of SHA1 and SHA256 hashed keys.
    // 1022 and 1023 are either side of the 1024 char quoted value that moves from the stack to the heap.
    [Params(42, 66, 1022, 1023)]
    public int ValueLength { get; set; }

    [Params(0, 1)]
    public int Quotes { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _value = new string('a', ValueLength - Quotes) + new string('\'', Quotes);
    }

    [Benchmark]
    public int TableQuery_GenerateFilterCondition()
    {
        return TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, _value).Length;
    }
}

[MemoryDiagnoser]
[MarkdownExporter]
public class TableQueryTypedFilterConditionBenchmarks
{
    private readonly Guid _guid = Guid.Parse("f1ffcc02-83e0-4347-8377-72d6007e3d93");

    [Benchmark]
    public int TableQuery_GenerateFilterConditionForBool()
    {
        return TableQuery.GenerateFilterConditionForBool("EmailConfirmed", QueryComparisons.Equal, true).Length;
    }

    [Benchmark]
    public int TableQuery_GenerateFilterConditionForGuid()
    {
        return TableQuery.GenerateFilterConditionForGuid(nameof(TableEntity.RowKey), QueryComparisons.Equal, _guid).Length;
    }
}
