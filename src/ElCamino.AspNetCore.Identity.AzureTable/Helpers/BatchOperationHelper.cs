// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if net45
namespace ElCamino.AspNet.Identity.AzureTable.Helpers
#else
namespace ElCamino.AspNetCore.Identity.AzureTable.Helpers
#endif
{
    /// <summary>
    /// Used to instantiate multiple TableBatchOperations when the 
    /// TableOperation maximum is reached on a single TableBatchOperation
    /// </summary>
    internal class BatchOperationHelper
    {
        /// <summary>
        /// Current max operations supported in a TableBatchOperation
        /// http://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-tables/#insert-batch
        /// </summary>
        public const int MaxOperationsPerBatch = 100;

        private readonly List<TableBatchOperation> _batches = new List<TableBatchOperation>(100);

        public BatchOperationHelper() { }

        /// <summary>
        /// Adds a TableOperation to a TableBatchOperation
        /// and automatically adds a new TableBatchOperation if max TableOperations are 
        /// exceeded.
        /// </summary>
        /// <param name="operation"></param>
        public void Add(TableOperation operation)
        {
            TableBatchOperation current = GetCurrent();
            if (current.Count == MaxOperationsPerBatch)
            {
                _batches.Add(new TableBatchOperation());
                current = GetCurrent();
            }
            current.Add(operation);
        }

        public async Task<IList<TableResult>> ExecuteBatchAsync(CloudTable table)
        {
            return await Task.Run(
            () =>
            {
                ConcurrentBag<TableResult> results = new ConcurrentBag<TableResult>();
				//TODO: Fix for Core 5.0 
#if net45
				Parallel.ForEach(_batches,
#else
				_batches.ForEach(
#endif
			async (batchOperation) =>
                {
                    var x = await table.ExecuteBatchAsync(batchOperation);
                    x.ToList().ForEach((tr) => { results.Add(tr); });
                });
                Clear();
                return results.ToList();
            });
        }

        public void Clear()
        {
            _batches.Clear();
        }

        private TableBatchOperation GetCurrent()
        {
            if (_batches.Count < 1)
            {
                _batches.Add(new TableBatchOperation());
            }

            return _batches[_batches.Count - 1];
        }
    }
}
