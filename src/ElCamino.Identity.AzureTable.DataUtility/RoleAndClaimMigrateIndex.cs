// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElCamino.AspNetCore.Identity.AzureTable;
using Microsoft.Azure.Cosmos.Table;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using ElCamino.AspNetCore.Identity.AzureTable.Model;

namespace ElCamino.Identity.AzureTable.DataUtility
{
    public class RoleAndClaimMigrateIndex : IMigration
    {
        private IKeyHelper _keyHelper;
        public RoleAndClaimMigrateIndex(IKeyHelper keyHelper)
        {
            _keyHelper = keyHelper;
        }

        public TableQuery GetSourceTableQuery()
        {
            TableQuery tq = new TableQuery();
            tq.SelectColumns = new List<string>() { "PartitionKey", "RowKey", "KeyVersion" };
            string partitionFilter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, Constants.RowKeyConstants.PreFixIdentityUserId),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThan, "V_"));
            string rowFilterRole = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, Constants.RowKeyConstants.PreFixIdentityUserRole),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, "S_"));
            string rowFilterClaim = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, Constants.RowKeyConstants.PreFixIdentityUserClaim),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, "D_"));

            string keyVersionFilter = TableQuery.GenerateFilterConditionForDouble("KeyVersion", QueryComparisons.LessThan, 2.0);

            string rowFilter = TableQuery.CombineFilters(rowFilterRole, TableOperators.Or, rowFilterClaim);
            string keysFilter = TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);
            tq.FilterString = TableQuery.CombineFilters(keysFilter, TableOperators.And, keyVersionFilter);
            return tq;
        }

        public void ProcessMigrate(IdentityCloudContext targetContext, IdentityCloudContext sourceContext, IList<DynamicTableEntity> sourceUserResults, int maxDegreesParallel, Action updateComplete = null, Action<string> updateError = null)
        {
            var rolesAndClaims = sourceUserResults
                            .Where(UserWhereFilter);


            var result2 = Parallel.ForEach(rolesAndClaims, new ParallelOptions() { MaxDegreeOfParallelism = maxDegreesParallel }, (dte) =>
            {

                //Add the role or claim index
                try
                {
                    IdentityUserIndex index = new IdentityUserIndex()
                    {
                        Id = dte.PartitionKey,
                        PartitionKey = dte.RowKey,
                        RowKey = dte.PartitionKey,
                        KeyVersion = _keyHelper.KeyVersion,
                        ETag = Constants.ETagWildcard
                    };
                    var r = targetContext.IndexTable.ExecuteAsync(TableOperation.InsertOrReplace(index)).Result;
                    updateComplete?.Invoke();
                }
                catch (Exception ex)
                {
                    updateError?.Invoke(string.Format("{0}\t{1}", dte.PartitionKey, ex.Message));
                }

            });
        }

        public bool UserWhereFilter(DynamicTableEntity d)
        {
            return d.RowKey.StartsWith(Constants.RowKeyConstants.PreFixIdentityUserRole) || d.RowKey.StartsWith(Constants.RowKeyConstants.PreFixIdentityUserClaim);
        }
    }
}
