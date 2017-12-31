// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage.Table
{
    public static class AzureSdkHelper
    {
        //Azure SDK changes, you are killing me.
        public static async Task<IEnumerable<DynamicTableEntity>> ExecuteQueryAsync(this CloudTable ct, TableQuery tq)
        { 
            var segment = await ct.ExecuteQuerySegmentedAsync(tq, new TableContinuationToken());

            TableContinuationToken t = segment.ContinuationToken;

            List<DynamicTableEntity> tlist = new List<DynamicTableEntity>(segment.Results);
            while (t != null)
            {
                segment = await ct.ExecuteQuerySegmentedAsync(tq, t);
                tlist.AddRange(segment.Results);
                t = segment.ContinuationToken;
            }

            Debug.WriteLine("ExecuteQueryAsync: (Count): {0}", tlist.Count);
            Debug.WriteLine("ExecuteQueryAsync (Query): " + tq.FilterString);

            return tlist;
        }
    }
}
