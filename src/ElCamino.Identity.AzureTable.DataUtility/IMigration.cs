// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Azure.Data.Tables;
using ElCamino.AspNetCore.Identity.AzureTable;

namespace ElCamino.Identity.AzureTable.DataUtility
{
    public interface IMigration
    {
        TableQuery GetSourceTableQuery();

        bool UserWhereFilter(TableEntity d);

        void ProcessMigrate(IdentityCloudContext targetContext,
            IdentityCloudContext sourceContext,
            IList<TableEntity> sourceUserResults,
            int maxDegreesParallel,
            Action? updateComplete = null,
            Action<string>? updateError = null);
    }
}
