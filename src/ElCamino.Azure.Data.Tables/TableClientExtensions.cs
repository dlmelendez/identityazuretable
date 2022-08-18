// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

namespace Azure.Data.Tables
{
    public static class TableClientExtensions
    {
        /// <summary>
        /// Sets the etag and timestamp values from the response header on the entity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <param name="entity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Entity with the etag and timestamp values set from response header values.</returns>
        public static async Task<T> AddEntityWithHeaderValuesAsync<T>(this TableClient table, T entity, CancellationToken cancellationToken = default) where T : class, ITableEntity, new()
        {
            var response = await table.AddEntityAsync(entity, cancellationToken);
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
        public static async Task<T> UpdateEntityWithHeaderValuesAsync<T>(this TableClient table, T entity, ETag ifMatch, TableUpdateMode mode = TableUpdateMode.Replace, CancellationToken cancellationToken = default) where T : class, ITableEntity, new()
        {
            var response = await table.UpdateEntityAsync(entity, ifMatch, mode, cancellationToken);
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
        public static async Task<T> UpsertEntityWithHeaderValuesAsync<T>(this TableClient table, T entity, TableUpdateMode mode = TableUpdateMode.Replace, CancellationToken cancellationToken = default) where T : class, ITableEntity, new()
        {
            var response = await table.UpsertEntityAsync(entity, mode, cancellationToken);
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
    }
}
