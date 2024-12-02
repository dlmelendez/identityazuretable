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
        /// <summary>
        /// Max entitie transactions by partition key, this is a table storage limit.
        /// </summary>
        public const int MaxEntitiesPerBatch = 100;

        private readonly Dictionary<string, List<TableTransactionAction>> _batches = [];

        private readonly TableClient _table;

        /// <summary>
        /// BatchOperationHelper constructor
        /// </summary>
        /// <param name="table">Table to target batch(es) of entity transactions</param>
        public BatchOperationHelper(TableClient table)
        {
            _table = table;
        }

        /// <summary>
        /// Add entities with <see cref="TableTransactionAction"/> <see cref="TableTransactionActionType.Add"/> 
        /// </summary>
        /// <typeparam name="T"><see cref="ITableEntity"/> class, new()</typeparam>
        /// <param name="entities">Entities to submit for <see cref="TableTransactionActionType.Add"/>  </param>
        public virtual void AddEntities<T>(IEnumerable<T> entities) where T : class, ITableEntity, new()
        {
            foreach (T entity in entities)
            {
                AddEntity<T>(entity);
            }
        }

        /// <summary>
        /// Add entity with <see cref="TableTransactionAction"/> <see cref="TableTransactionActionType.Add"/> 
        /// </summary>
        /// <typeparam name="T"><see cref="ITableEntity"/> class, new()</typeparam>
        /// <param name="entity">Entity to submit for <see cref="TableTransactionActionType.Add"/>  </param>
        public virtual void AddEntity<T>(T entity) where T : class, ITableEntity, new()
        {
            GetCurrent(entity.PartitionKey).Add(new TableTransactionAction(TableTransactionActionType.Add, entity));
        }

        /// <summary>
        /// Delete entity with <see cref="TableTransactionAction"/> <see cref="TableTransactionActionType.Delete"/> by partitionKey
        /// </summary>
        /// <param name="partitionKey">PartitionKey of entity to delete</param>
        /// <param name="rowKey">RowKey of entity to delete</param>
        /// <param name="ifMatch"><see cref="ETag"/> of entity to delete</param>
        public virtual void DeleteEntity(string partitionKey, string rowKey, ETag ifMatch = default)
        {
            GetCurrent(partitionKey).Add(new TableTransactionAction(TableTransactionActionType.Delete, new TableEntity(partitionKey, rowKey), ifMatch));
        }

        /// <summary>
        /// Submits all transactions
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="IEnumerable{Response}"/> of the <see cref="TableClient.SubmitTransactionAsync"/> calls </returns>
        public virtual async Task<IEnumerable<Response>> SubmitBatchAsync(CancellationToken cancellationToken = default)
        {
            ConcurrentBag<Response> bag = [];
            List<Task> batches = [];
            foreach (KeyValuePair<string, List<TableTransactionAction>> kv in _batches)
            {
                int total = kv.Value.Count;
                int skip = 0;
                int take = total > MaxEntitiesPerBatch ? MaxEntitiesPerBatch : total;

                while (take > 0)
                {
                    batches.Add(_table.SubmitTransactionAsync(kv.Value.Skip(skip).Take(take), cancellationToken)
                        .ContinueWith((result) =>
                        {
                            foreach (var r in result.Result.Value)
                            {
                                bag.Add(r);
                            }
                        }, cancellationToken));

                    skip += take;
                    take = (total - skip) > MaxEntitiesPerBatch ? MaxEntitiesPerBatch : (total - skip);
                }
            }
            await Task.WhenAll(batches).ConfigureAwait(false);
            Clear();
            return bag;
        }

        /// <summary>
        /// Update entities with <see cref="TableTransactionAction"/> <see cref="TableTransactionActionType.UpdateMerge"/> or <see cref="TableTransactionActionType.UpdateReplace"/> 
        /// </summary>
        /// <typeparam name="T"><see cref="ITableEntity"/> class, new()</typeparam>
        /// <param name="entity">Entity to submit for update </param>
        /// <param name="ifMatch"><see cref="ETag"/> of entity to update</param>
        /// <param name="mode"><see cref="TableUpdateMode.Merge"/> default, otherwise <see cref="TableUpdateMode.Replace"/></param>
        public virtual void UpdateEntity<T>(T entity, ETag ifMatch, TableUpdateMode mode = TableUpdateMode.Merge) where T : class, ITableEntity, new()
        {
            GetCurrent(entity.PartitionKey).Add(new TableTransactionAction(mode == TableUpdateMode.Merge ? TableTransactionActionType.UpdateMerge : TableTransactionActionType.UpdateReplace, entity, ifMatch));
        }

        /// <summary>
        /// Upsert entities with <see cref="TableTransactionAction"/> <see cref="TableTransactionActionType.UpsertMerge"/> or <see cref="TableTransactionActionType.UpsertReplace"/> 
        /// </summary>
        /// <typeparam name="T"><see cref="ITableEntity"/> class, new()</typeparam>
        /// <param name="entity">Entity to submit for upsert </param>
        /// <param name="mode"><see cref="TableUpdateMode.Merge"/> default, otherwise <see cref="TableUpdateMode.Replace"/></param>
        public virtual void UpsertEntity<T>(T entity, TableUpdateMode mode = TableUpdateMode.Merge) where T : class, ITableEntity, new()
        {
            GetCurrent(entity.PartitionKey).Add(new TableTransactionAction(mode == TableUpdateMode.Merge ? TableTransactionActionType.UpsertMerge : TableTransactionActionType.UpsertReplace, entity));
        }

        /// <summary>
        /// Clears the non-submitted transaction dictionary, called after SubmitBatchAsync() by default
        /// </summary>
        public void Clear()
        {
            _batches.Clear();
        }

        private List<TableTransactionAction> GetCurrent(string partitionKey)
        {
            if (_batches.TryGetValue(partitionKey, out var tableTransactionActions))
            {
                return tableTransactionActions;
            }

            _batches.Add(partitionKey, []);

            return _batches[partitionKey];
        }
    }
}
