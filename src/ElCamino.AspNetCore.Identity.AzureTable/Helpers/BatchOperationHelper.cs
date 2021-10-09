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

        private readonly Dictionary<string, List<TableTransactionAction>> _batches = new Dictionary<string, List<TableTransactionAction>>();

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
            current.Add(new TableTransactionAction(TableTransactionActionType.Add, entity));
        }
        public virtual void DeleteEntity(string partitionKey, string rowKey, ETag ifMatch = default) 
        {
            var current = GetCurrent(partitionKey);
            current.Add(new TableTransactionAction(TableTransactionActionType.Delete, new TableEntity(partitionKey, rowKey),ifMatch));
        }

        public virtual async Task<IEnumerable<Response>> SubmitBatchAsync(CancellationToken cancellationToken = default) 
        {
            ConcurrentBag<Response> bag = new ConcurrentBag<Response>();
            List<Task> batches = new List<Task>(this._batches.Count);
            foreach(KeyValuePair<string, List<TableTransactionAction>> kv in this._batches)
            {
                batches.Add(_table.SubmitTransactionAsync(kv.Value, cancellationToken)
                    .ContinueWith((result) =>
                    {
                        foreach (var r in result.Result.Value)
                        {
                            bag.Add(r);
                        }
                    }));
            }
            await Task.WhenAll(batches);
            Clear();
            return bag;
        }

        //public bool TryGetFailedEntityFromException(RequestFailedException exception, out ITableEntity failedEntity)
        //{
        //    foreach(var t in _batches.Values.SelectMany(s => s))
        //    {
        //        if(t.TryGetFailedEntityFromException(exception, out failedEntity))
        //        {
        //            return true;
        //        }
        //    }
        //    failedEntity = null;
        //    return false;
        //}
        public virtual void UpdateEntity<T>(T entity, ETag ifMatch, TableUpdateMode mode = TableUpdateMode.Merge) where T : class, ITableEntity, new()
        {
            var current = GetCurrent(entity.PartitionKey);
            current.Add(new TableTransactionAction(mode == TableUpdateMode.Merge? TableTransactionActionType.UpdateMerge : TableTransactionActionType.UpdateReplace, entity, ifMatch));
        }

        public virtual void UpsertEntity<T>(T entity, TableUpdateMode mode = TableUpdateMode.Merge) where T : class, ITableEntity, new() 
        {
            var current = GetCurrent(entity.PartitionKey);
            current.Add(new TableTransactionAction(mode == TableUpdateMode.Merge ? TableTransactionActionType.UpsertMerge : TableTransactionActionType.UpsertReplace, entity));
        }

        public void Clear()
        {
            _batches.Clear();
        }

        private List<TableTransactionAction> GetCurrent(string partitionKey)
        {
            if(!_batches.ContainsKey(partitionKey))
            {
                _batches.Add(partitionKey, new List<TableTransactionAction>());
            }

            return _batches[partitionKey];
        }
    }
}
