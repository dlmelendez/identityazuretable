using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;
using Azure;
using Azure.Core;
using Azure.Data.Tables;

namespace ElCamino.AspNetCore.Identity.AzureTable.Benchmark;

internal sealed class BenchmarkTableServiceClient : TableServiceClient
{
    private readonly ConcurrentDictionary<string, InMemoryTableClient> _tables = new(StringComparer.OrdinalIgnoreCase);

    public override TableClient GetTableClient(string tableName)
    {
        return _tables.GetOrAdd(tableName, static name => new InMemoryTableClient(name));
    }

    public void ClearAll()
    {
        foreach (var table in _tables.Values)
        {
            table.Clear();
        }
    }
}

internal sealed class InMemoryTableClient : TableClient
{
    private readonly ConcurrentDictionary<(string PartitionKey, string RowKey), TableEntity> _entities = [];

    public InMemoryTableClient(string tableName)
    {
        Name = tableName;
    }

    public int Count => _entities.Count;

    public override string Name { get; }

    public void Clear()
    {
        _entities.Clear();
    }

    public void SeedEntity<T>(T entity) where T : class, ITableEntity
    {
        Upsert(entity);
    }

    public override Task<Response> AddEntityAsync<T>(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tableEntity = ToTableEntity(entity);

        if (!_entities.TryAdd((tableEntity.PartitionKey, tableEntity.RowKey), tableEntity))
        {
            throw new RequestFailedException(409, "Entity already exists.");
        }

        return Task.FromResult<Response>(BenchmarkResponse.Instance);
    }

    public override Task<Response> UpsertEntityAsync<T>(T entity, TableUpdateMode mode = TableUpdateMode.Merge, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Upsert(entity);
        return Task.FromResult<Response>(BenchmarkResponse.Instance);
    }

    public override Task<Response> UpdateEntityAsync<T>(T entity, ETag ifMatch, TableUpdateMode mode = TableUpdateMode.Merge, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Upsert(entity);
        return Task.FromResult<Response>(BenchmarkResponse.Instance);
    }

    public override Task<Response> DeleteEntityAsync(string partitionKey, string rowKey, ETag ifMatch = default, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _entities.TryRemove((partitionKey, rowKey), out _);
        return Task.FromResult<Response>(BenchmarkResponse.Instance);
    }

    public override Task<Response<IReadOnlyList<Response>>> SubmitTransactionAsync(IEnumerable<TableTransactionAction> transactionActions, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        List<Response> responses = [];
        foreach (var action in transactionActions)
        {
            Apply(action);
            responses.Add(BenchmarkResponse.Instance);
        }

        return Task.FromResult(Response.FromValue<IReadOnlyList<Response>>(responses, BenchmarkResponse.Instance));
    }

    public override AsyncPageable<T> QueryAsync<T>(string? filter = null, int? maxPerPage = null, IEnumerable<string>? select = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var values = _entities.Values
            .Where(entity => MatchesFilter(entity, filter))
            .OrderBy(entity => entity.PartitionKey, StringComparer.Ordinal)
            .ThenBy(entity => entity.RowKey, StringComparer.Ordinal)
            .Select(entity => ToEntity<T>(entity))
            .ToArray();

        int pageSize = maxPerPage.GetValueOrDefault(values.Length == 0 ? 1 : values.Length);
        if (pageSize < 1)
        {
            pageSize = values.Length == 0 ? 1 : values.Length;
        }

        List<Page<T>> pages = [];
        for (int offset = 0; offset < values.Length; offset += pageSize)
        {
            pages.Add(Page<T>.FromValues(values.Skip(offset).Take(pageSize).ToArray(), null, BenchmarkResponse.Instance));
        }

        if (pages.Count == 0)
        {
            pages.Add(Page<T>.FromValues([], null, BenchmarkResponse.Instance));
        }

        return AsyncPageable<T>.FromPages(pages);
    }

    private void Upsert<T>(T entity) where T : ITableEntity
    {
        var tableEntity = ToTableEntity(entity);
        _entities[(tableEntity.PartitionKey, tableEntity.RowKey)] = tableEntity;
    }

    private void Apply(TableTransactionAction action)
    {
        switch (action.ActionType)
        {
            case TableTransactionActionType.Add:
                _entities.TryAdd((action.Entity.PartitionKey, action.Entity.RowKey), ToTableEntity(action.Entity));
                break;
            case TableTransactionActionType.Delete:
                _entities.TryRemove((action.Entity.PartitionKey, action.Entity.RowKey), out _);
                break;
            case TableTransactionActionType.UpdateMerge:
            case TableTransactionActionType.UpdateReplace:
            case TableTransactionActionType.UpsertMerge:
            case TableTransactionActionType.UpsertReplace:
                _entities[(action.Entity.PartitionKey, action.Entity.RowKey)] = ToTableEntity(action.Entity);
                break;
        }
    }

    private static bool MatchesFilter(TableEntity entity, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        List<string> partitionKeys = ExtractEqualValues(filter, nameof(TableEntity.PartitionKey));
        List<string> rowKeys = ExtractEqualValues(filter, nameof(TableEntity.RowKey));

        bool partitionMatches = partitionKeys.Count == 0 || partitionKeys.Contains(entity.PartitionKey, StringComparer.Ordinal);
        bool rowMatches = rowKeys.Count == 0 || rowKeys.Contains(entity.RowKey, StringComparer.Ordinal);

        return partitionMatches && rowMatches;
    }

    private static List<string> ExtractEqualValues(string filter, string propertyName)
    {
        List<string> values = [];
        string marker = propertyName + " eq '";
        int searchIndex = 0;

        while (searchIndex < filter.Length)
        {
            int markerIndex = filter.IndexOf(marker, searchIndex, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                break;
            }

            int valueStart = markerIndex + marker.Length;
            int valueEnd = filter.IndexOf('\'', valueStart);
            if (valueEnd < 0)
            {
                break;
            }

            values.Add(filter[valueStart..valueEnd]);
            searchIndex = valueEnd + 1;
        }

        return values;
    }

    private static T ToEntity<T>(TableEntity entity) where T : class, ITableEntity
    {
        if (typeof(T) == typeof(TableEntity))
        {
            return (T)(object)new TableEntity(entity);
        }

        T result = Activator.CreateInstance<T>();
        var properties = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty)
            .Where(property => property.GetCustomAttribute<IgnoreDataMemberAttribute>() is null);

        foreach (var property in properties)
        {
            if (entity.TryGetValue(property.Name, out object? value))
            {
                property.SetValue(result, value);
            }
        }

        return result;
    }

    private static TableEntity ToTableEntity<T>(T entity) where T : ITableEntity
    {
        if (entity is TableEntity tableEntity)
        {
            return new TableEntity(tableEntity);
        }

        var properties = entity.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty)
            .Where(property => property.GetCustomAttribute<IgnoreDataMemberAttribute>() is null);

        Dictionary<string, object?> dictionary = [];
        foreach (var property in properties)
        {
            dictionary[property.Name] = property.GetValue(entity);
        }

        return new TableEntity(dictionary);
    }
}

internal sealed class BenchmarkResponse : Response
{
    public static readonly BenchmarkResponse Instance = new();

    private BenchmarkResponse()
    {
    }

    public override int Status => 204;

    public override string ReasonPhrase => "No Content";

    public override Stream? ContentStream { get; set; }

    public override string ClientRequestId { get; set; } = string.Empty;

    public override void Dispose()
    {
    }

    protected override bool ContainsHeader(string name)
    {
        return false;
    }

    protected override IEnumerable<HttpHeader> EnumerateHeaders()
    {
        return [];
    }

    protected override bool TryGetHeader(string name, out string value)
    {
        value = string.Empty;
        return false;
    }

    protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
    {
        values = [];
        return false;
    }
}
