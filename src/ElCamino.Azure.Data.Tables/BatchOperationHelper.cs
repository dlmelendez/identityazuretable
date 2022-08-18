// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Concurrent;

namespace Azure.Data.Tables
{
    /// <summary>
    /// Used to instantiate multiple TableBatchOperations when the
    /// TableOperation maximum is reached on a single TableBatchOperation
    /// </summary>
    public class BatchOperationHelper
    {
        private readonly Dictionary<string, List<TableTransactionAction>> _batches = new();

        private readonly TableClient _table;

        public BatchOperationHelper(TableClient table)
        {
            _table = table;
        }

        public virtual void AddEntities<T>(IEnumerable<T> entities) where T : class, ITableEntity, new()
        {
            foreach (T entity in entities)
            {
                AddEntity<T>(entity);
            }
        }
        public virtual void AddEntity<T>(T entity) where T : class, ITableEntity, new()
        {
            GetCurrent(entity.PartitionKey).Add(new TableTransactionAction(TableTransactionActionType.Add, entity));
        }
        public virtual void DeleteEntity(string partitionKey, string rowKey, ETag ifMatch = default)
        {
            GetCurrent(partitionKey).Add(new TableTransactionAction(TableTransactionActionType.Delete, new TableEntity(partitionKey, rowKey), ifMatch));
        }

        public virtual async Task<IEnumerable<Response>> SubmitBatchAsync(CancellationToken cancellationToken = default)
        {
            ConcurrentBag<Response> bag = new ConcurrentBag<Response>();
            List<Task> batches = new List<Task>(_batches.Count);
            foreach (KeyValuePair<string, List<TableTransactionAction>> kv in _batches)
            {
                batches.Add(_table.SubmitTransactionAsync(kv.Value, cancellationToken)
                    .ContinueWith((result) =>
                    {
                        foreach (var r in result.Result.Value)
                        {
                            bag.Add(r);
                        }
                    }, cancellationToken));
            }
            await Task.WhenAll(batches).ConfigureAwait(false);
            Clear();
            return bag;
        }

        public virtual void UpdateEntity<T>(T entity, ETag ifMatch, TableUpdateMode mode = TableUpdateMode.Merge) where T : class, ITableEntity, new()
        {
            GetCurrent(entity.PartitionKey).Add(new TableTransactionAction(mode == TableUpdateMode.Merge ? TableTransactionActionType.UpdateMerge : TableTransactionActionType.UpdateReplace, entity, ifMatch));
        }

        public virtual void UpsertEntity<T>(T entity, TableUpdateMode mode = TableUpdateMode.Merge) where T : class, ITableEntity, new()
        {
            GetCurrent(entity.PartitionKey).Add(new TableTransactionAction(mode == TableUpdateMode.Merge ? TableTransactionActionType.UpsertMerge : TableTransactionActionType.UpsertReplace, entity));
        }

        public void Clear()
        {
            _batches.Clear();
        }

        private List<TableTransactionAction> GetCurrent(string partitionKey)
        {
            if (!_batches.ContainsKey(partitionKey))
            {
                _batches.Add(partitionKey, new List<TableTransactionAction>());
            }

            return _batches[partitionKey];
        }
    }
}
