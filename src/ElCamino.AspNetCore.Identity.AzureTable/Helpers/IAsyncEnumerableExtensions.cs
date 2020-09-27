// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ElCamino.AspNetCore.Identity.AzureTable
{
    public static class IAsyncEnumerableExtensions
    {
        public static async Task<T> FirstOrDefaultAsync<T>(
            this IAsyncEnumerable<T> asyncEnumerable, 
            CancellationToken cancellationToken = default)
        {
            await using (var enumerator = asyncEnumerable.GetAsyncEnumerator(cancellationToken))
            {
                if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    return enumerator.Current;
                }
                return default;
            }
        }

        public static async Task<List<T>> ToListAsync<T>(
            this IAsyncEnumerable<T> asyncEnumerable, 
            CancellationToken cancellationToken = default)
        {
            await using (var enumerator = asyncEnumerable.GetAsyncEnumerator(cancellationToken))
            {
                List<T> list = new List<T>();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    list.Add(enumerator.Current);
                }
                return list;
            }
        }

        public static async Task ForEachAsync<T>(
            this IAsyncEnumerable<T> asyncEnumerable,
            Action<T> action,
            CancellationToken cancellationToken = default)
        {
            await using (var enumerator = asyncEnumerable.GetAsyncEnumerator(cancellationToken))
            {
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    action(enumerator.Current);
                }
            }
        }

        public static async Task<bool> AnyAsync<T>(
           this IAsyncEnumerable<T> asyncEnumerable,
           CancellationToken cancellationToken = default)
        {
            await using (var enumerator = asyncEnumerable.GetAsyncEnumerator(cancellationToken))
                return await enumerator.MoveNextAsync().ConfigureAwait(false);
        }
    }
}