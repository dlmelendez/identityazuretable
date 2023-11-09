// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Azure.Data.Tables
{
    /// <summary>
    /// Extensions for <see cref="IAsyncEnumerable{T}"/>
    /// </summary>
    public static class IAsyncEnumerableExtensions
    {
        /// <summary>
        /// FirstOrDefaultAsync{T}
        /// </summary>
        /// <typeparam name="T">Generic type</typeparam>
        /// <param name="asyncEnumerable"><see cref="IAsyncEnumerable{T}"/></param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>Optional, default </param>
        /// <returns>First in the enumerator or the default value</returns>
        public static async Task<T?> FirstOrDefaultAsync<T>(
            this IAsyncEnumerable<T> asyncEnumerable,
            CancellationToken cancellationToken = default)
        {
            await using var enumerator = asyncEnumerable.GetAsyncEnumerator(cancellationToken);
            if (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                return enumerator.Current;
            }
            return default;
        }

        /// <summary>
        /// ToListAsync{T}
        /// </summary>
        /// <typeparam name="T">Generic type</typeparam>
        /// <param name="asyncEnumerable"><see cref="IAsyncEnumerable{T}"/></param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>Optional, default </param>
        /// <returns>A <see cref="List{T}"/> List</returns>
        public static async Task<List<T>> ToListAsync<T>(
            this IAsyncEnumerable<T> asyncEnumerable,
            CancellationToken cancellationToken = default)
        {
            await using var enumerator = asyncEnumerable.GetAsyncEnumerator(cancellationToken);
            List<T> list = new List<T>();
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                list.Add(enumerator.Current);
            }
            return list;
        }

        /// <summary>
        /// ForEachAsync{T}
        /// </summary>
        /// <typeparam name="T">Generic type</typeparam>
        /// <param name="asyncEnumerable"><see cref="IAsyncEnumerable{T}"/></param>
        /// <param name="action"><see cref="Action{T}"/> Action for element T</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>Optional, default </param>
        /// <returns>A <see cref="Task"/></returns>
        public static async Task ForEachAsync<T>(
            this IAsyncEnumerable<T> asyncEnumerable,
            Action<T> action,
            CancellationToken cancellationToken = default)
        {
            await using var enumerator = asyncEnumerable.GetAsyncEnumerator(cancellationToken);
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                action(enumerator.Current);
            }
        }

        /// <summary>
        /// AnyAsync{T}
        /// </summary>
        /// <typeparam name="T">Generic type</typeparam>
        /// <param name="asyncEnumerable"><see cref="IAsyncEnumerable{T}"/></param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>Optional, default </param>
        /// <returns>A <see cref="bool"/> result if it exist in the enumerator</returns>
        public static async Task<bool> AnyAsync<T>(
           this IAsyncEnumerable<T> asyncEnumerable,
           CancellationToken cancellationToken = default)
        {
            await using var enumerator = asyncEnumerable.GetAsyncEnumerator(cancellationToken);
            return await enumerator.MoveNextAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// CountAsync{T}
        /// </summary>
        /// <typeparam name="T">Generic type</typeparam>
        /// <param name="asyncEnumerable"><see cref="IAsyncEnumerable{T}"/></param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>Optional, default </param>
        /// <returns>A <see cref="int"/> count of the enumerator</returns>
        public static async Task<int> CountAsync<T>(
            this IAsyncEnumerable<T> asyncEnumerable,
            CancellationToken cancellationToken = default)
        {
            await using var enumerator = asyncEnumerable.GetAsyncEnumerator(cancellationToken);
            int counter = 0;
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                counter++;
            }
            return counter;
        }
    }
}
