using ElCamino.AspNetCore.Identity.AzureTable;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElCamino.Identity.AzureTable.DataUtility
{
    public interface IMigrateIndex
    {
        TableQuery GetUserTableQuery();

        bool UserWhereFilter(DynamicTableEntity d);

        void ProcessMigrate(IdentityCloudContext ic,
            IList<DynamicTableEntity> userResults,
            int maxDegreesParallel,
            Action updateComplete = null,
            Action<string> updateError = null);
    }
}
