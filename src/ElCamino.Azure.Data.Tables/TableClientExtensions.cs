// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Azure.Data.Tables
{
    public static class TableClientExtensions
    {
        public const int MaxEntitiesPerPage = 1000;

        /// <summary>
        /// Sets the etag and timestamp values from the response header on the entity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <param name="entity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Entity with the etag and timestamp values set from response header values.</returns>
        /// <exception cref="RequestFailedException"></exception>
        public static async Task<T> AddEntityWithHeaderValuesAsync<T>(this TableClient table, T entity, CancellationToken cancellationToken = default) where T : class, ITableEntity, new()
        {
            var response = await table.AddEntityAsync(entity, cancellationToken).ConfigureAwait(false);
            entity.ETag = response.Headers.ETag.GetValueOrDefault();
            entity.Timestamp = response.Headers.Date;
            return entity;
        }

        /// <summary>
        /// Sets the etag and timestamp values from the response header on the entity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <param name="entity"></param>
        /// <param name="ifMatch"></param>
        /// <param name="mode"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Entity with the etag and timestamp values set from response header values.</returns>
        /// <exception cref="RequestFailedException"></exception>
        public static async Task<T> UpdateEntityWithHeaderValuesAsync<T>(this TableClient table, T entity, ETag ifMatch, TableUpdateMode mode = TableUpdateMode.Replace, CancellationToken cancellationToken = default) where T : class, ITableEntity, new()
        {
            var response = await table.UpdateEntityAsync(entity, ifMatch, mode, cancellationToken).ConfigureAwait(false);
            entity.ETag = response.Headers.ETag.GetValueOrDefault();
            entity.Timestamp = response.Headers.Date;
            return entity;
        }

        /// <summary>
        /// Sets the etag and timestamp values from the response header on the entity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <param name="entity"></param>
        /// <param name="mode"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Entity with the etag and timestamp values set from response header values.</returns>
        /// <exception cref="RequestFailedException"></exception>
        public static async Task<T> UpsertEntityWithHeaderValuesAsync<T>(this TableClient table, T entity, TableUpdateMode mode = TableUpdateMode.Replace, CancellationToken cancellationToken = default) where T : class, ITableEntity, new()
        {
            var response = await table.UpsertEntityAsync(entity, mode, cancellationToken).ConfigureAwait(false);
            entity.ETag = response.Headers.ETag.GetValueOrDefault();
            entity.Timestamp = response.Headers.Date;
            return entity;

        }

        /// <summary>
        /// Gets entity by row and partition key, but does NOT throw <seealso cref="RequestFailedException"/> if entity does not exist.
        /// Helpful if you want to return a default value instead of handling the ResourceNotFound exception and popping the stack when an entity doesn't exist.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <param name="partitionKey"></param>
        /// <param name="rowKey"></param>
        /// <param name="select"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Found entity or default value of the entity type</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="RequestFailedException"></exception>
        public static async Task<T> GetEntityOrDefaultAsync<T>(
            this TableClient table,
            string partitionKey,
            string rowKey,
            IEnumerable<string> select = null,
            CancellationToken cancellationToken = default) where T : class, ITableEntity, new()
        {
            partitionKey = partitionKey ?? throw new ArgumentNullException(nameof(partitionKey));
            rowKey = rowKey ?? throw new ArgumentNullException(nameof(rowKey));

            string filterString = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, partitionKey),
                TableOperators.And,
                TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, rowKey));

            var page = await table.QueryAsync<T>(filter: filterString, maxPerPage: 1, select: select, cancellationToken)
                        .AsPages(continuationToken: null, pageSizeHint: 1).FirstOrDefaultAsync().ConfigureAwait(false);
            return page?.Values.FirstOrDefault();
        }

        /// <summary>
        /// Execute a query with a max takecount. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ct"></param>
        /// <param name="tq"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Table query results with max take. Takecount is null returns all results. </returns>
        public static async IAsyncEnumerable<T> ExecuteQueryAsync<T>(this TableClient ct, TableQuery tq,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
            where T : class, ITableEntity, new()
        {
            int iCounter = 0;

            if (tq.TakeCount.HasValue && tq.TakeCount.Value < 1)
            {
                yield break;
            }

            if (!tq.TakeCount.HasValue)
            {
                AsyncPageable<T> segment = ct.QueryAsync<T>(filter: tq.FilterString, select: tq.SelectColumns, cancellationToken: cancellationToken);
                await foreach (T result in segment.ConfigureAwait(false))
                {
                    iCounter++;
                    yield return result;
                }
            }
            else
            {
                if (tq.TakeCount.Value > 0 && tq.TakeCount.Value <= MaxEntitiesPerPage)
                {
                    var segment = await ct.QueryAsync<T>(filter: tq.FilterString, maxPerPage: tq.TakeCount.Value, select: tq.SelectColumns, cancellationToken: cancellationToken)
                        .AsPages(pageSizeHint: tq.TakeCount.Value)
                        .FirstOrDefaultAsync(cancellationToken)
                        .ConfigureAwait(false);
                    foreach (T result in segment.Values)
                    {
                        iCounter++;
                        yield return result;
                        if (iCounter >= tq.TakeCount.Value)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    AsyncPageable<T> segment = ct.QueryAsync<T>(filter: tq.FilterString, select: tq.SelectColumns, cancellationToken: cancellationToken);
                    await foreach (T result in segment.ConfigureAwait(false))
                    {
                        iCounter++;
                        yield return result;
                        if (iCounter >= tq.TakeCount.Value)
                        {
                            break;
                        }
                    }
                }
            }

#if DEBUG
            Debug.WriteLine("ExecuteQueryAsync: (Count): {0}", iCounter);
            Debug.WriteLine("ExecuteQueryAsync (Query): " + tq.FilterString);
#endif
            yield break;
        }

    }
}
