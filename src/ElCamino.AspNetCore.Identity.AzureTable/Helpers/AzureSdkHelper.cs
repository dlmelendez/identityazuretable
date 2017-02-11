// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
#if !net45
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage.Table
{
    public static class AzureSdkHelper
    {
		//Azure SDK changes, you are killing me.
		public static IEnumerable<DynamicTableEntity> ExecuteQuery(this CloudTable ct, TableQuery tq)
		{
			var task = ct.ExecuteQuerySegmentedAsync(tq, new TableContinuationToken());
			
			task.Wait();
            var segment = task.Result;
            TableContinuationToken t = segment.ContinuationToken;

			if (t == null)
			{
				return segment.Results;
			}
			else
			{
				List<DynamicTableEntity> tlist = new List<DynamicTableEntity>(segment.Results);
				while (t != null)
				{
					var task2 = ct.ExecuteQuerySegmentedAsync(tq, t);
					task2.Wait();
					segment = task2.Result; 
					tlist.AddRange(segment.Results);
					t = segment.ContinuationToken;
                }
				return tlist;
			}
			
		}
    }
}
#endif