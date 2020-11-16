using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Azure.Data.Tables
{
    public static class TableEntityExtensions
    {
        public static void ResetKeys(this TableEntity entity, string partitionKey, string rowKey, ETag eTag = default)
        {
            if(eTag == default)
            {
                eTag = ETag.All;
            }
            entity.PartitionKey = partitionKey;
            entity.RowKey = rowKey;
            entity.ETag = eTag;
        }
    }
}
