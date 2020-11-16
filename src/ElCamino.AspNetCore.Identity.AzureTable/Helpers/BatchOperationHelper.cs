// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;

namespace ElCamino.AspNetCore.Identity.AzureTable.Helpers
{
    /// <summary>
    /// Used to instantiate multiple TableBatchOperations when the
    /// TableOperation maximum is reached on a single TableBatchOperation
    /// </summary>
    internal class BatchOperationHelper
    {

        private readonly Dictionary<string, TableTransactionalBatch> _batches = new Dictionary<string, TableTransactionalBatch>();

        private TableClient _table;
        public BatchOperationHelper(TableClient table) 
        {
            _table = table;
        }
        
        public virtual void AddEntities<T>(IEnumerable<T> entities) where T : class, ITableEntity, new() 
        { 
            foreach(T entity in entities)
            {
                AddEntity<T>(entity);
            }
        }
        public virtual void AddEntity<T>(T entity) where T : class, ITableEntity, new() 
        {
            var current = GetCurrent(entity.PartitionKey);
            current.AddEntity<T>(entity);
        }
        public virtual void DeleteEntity(string partitionKey, string rowKey, ETag ifMatch = default) 
        {
            var current = GetCurrent(partitionKey);
            current.DeleteEntity(partitionKey, rowKey, ifMatch);
        }

        public virtual IEnumerable<Response<TableBatchResponse>> SubmitBatch(CancellationToken cancellationToken = default) 
        {
            ConcurrentBag<Response<TableBatchResponse>> bag = new ConcurrentBag<Response<TableBatchResponse>>();

            var result = Parallel.ForEach(this._batches.Values, (v) => {
                bag.Add(v.SubmitBatch(cancellationToken));
            });
            Clear();
            return bag;
        }
        public async virtual Task<IEnumerable<Response<TableBatchResponse>>> SubmitBatchAsync(CancellationToken cancellationToken = default) 
        {
            Task<Response<TableBatchResponse>>[] tasks = this._batches.Values.Select(s => s.SubmitBatchAsync(cancellationToken)).ToArray();

            await Task.WhenAll(tasks);
            Clear();
            return tasks.Select(t => t.Result);
        }

        public bool TryGetFailedEntityFromException(RequestFailedException exception, out ITableEntity failedEntity)
        {
            foreach(var t in _batches.Values)
            {
                if(t.TryGetFailedEntityFromException(exception, out failedEntity))
                {
                    return true;
                }
            }
            failedEntity = null;
            return false;
        }
        public virtual void UpdateEntity<T>(T entity, ETag ifMatch, TableUpdateMode mode = TableUpdateMode.Merge) where T : class, ITableEntity, new()
        {
            var current = GetCurrent(entity.PartitionKey);
            current.UpdateEntity<T>(entity, ifMatch, mode);
        }

        public virtual void UpsertEntity<T>(T entity, TableUpdateMode mode = TableUpdateMode.Merge) where T : class, ITableEntity, new() 
        {
            var current = GetCurrent(entity.PartitionKey);
            current.UpsertEntity<T>(entity, mode);
        }

        public void Clear()
        {
            _batches.Clear();
        }

        private TableTransactionalBatch GetCurrent(string partitionKey)
        {
            if(!_batches.ContainsKey(partitionKey))
            {
                _batches.Add(partitionKey, _table.CreateTransactionalBatch(partitionKey));
            }

            return _batches[partitionKey];
        }
    }
}
