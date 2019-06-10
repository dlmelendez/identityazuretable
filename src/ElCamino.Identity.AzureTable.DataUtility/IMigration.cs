using ElCamino.AspNetCore.Identity.AzureTable;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElCamino.Identity.AzureTable.DataUtility
{
    public interface IMigration
    {
        TableQuery GetSourceTableQuery();

        bool UserWhereFilter(DynamicTableEntity d);

        void ProcessMigrate(IdentityCloudContext targetContext,
            IdentityCloudContext sourceContext,
            IList<DynamicTableEntity> sourceUserResults,
            int maxDegreesParallel,
            Action updateComplete = null,
            Action<string> updateError = null);
    }
}
