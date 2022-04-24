// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Azure.Data.Tables
{
    public static class AzureSdkHelper
    {
        //Azure SDK changes, you are still killing me.
        public static async IAsyncEnumerable<T> ExecuteQueryAsync<T>(this TableClient ct, TableQuery tq)
            where T : class, ITableEntity, new()
        { 
#if DEBUG
            int iCounter = 0;
#endif

            AsyncPageable<T> segment = ct.QueryAsync<T>(tq.FilterString, tq.TakeCount, tq.SelectColumns);
            await foreach (T result in segment.ConfigureAwait(false))
            {
#if DEBUG
                iCounter++;
#endif
                yield return result;
            }
            
#if DEBUG
            Debug.WriteLine("ExecuteQueryAsync: (Count): {0}", iCounter);
            Debug.WriteLine("ExecuteQueryAsync (Query): " + tq.FilterString);
#endif
        }

    }
}
