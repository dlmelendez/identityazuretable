// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Model;

namespace ElCamino.Identity.AzureTable.DataUtility
{
    public class RoleAndClaimMigrateIndex : IMigration
    {
        private readonly IKeyHelper _keyHelper;
        public RoleAndClaimMigrateIndex(IKeyHelper keyHelper)
        {
            _keyHelper = keyHelper;
        }

        public TableQuery GetSourceTableQuery()
        {
            TableQuery tq = new TableQuery();
            tq.SelectColumns = new List<string>() { "PartitionKey", "RowKey", "KeyVersion" };
            string partitionFilter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, _keyHelper.PreFixIdentityUserId),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThan, _keyHelper.PreFixIdentityUserIdUpperBound));
            string rowFilterRole = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, _keyHelper.PreFixIdentityUserRole),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, _keyHelper.PreFixIdentityUserRoleUpperBound));
            string rowFilterClaim = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, _keyHelper.PreFixIdentityUserClaim),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, _keyHelper.PreFixIdentityUserClaimUpperBound));

            string keyVersionFilter = TableQuery.GenerateFilterConditionForDouble("KeyVersion", QueryComparisons.LessThan, 2.0);

            string rowFilter = TableQuery.CombineFilters(rowFilterRole, TableOperators.Or, rowFilterClaim);
            string keysFilter = TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);
            tq.FilterString = TableQuery.CombineFilters(keysFilter, TableOperators.And, keyVersionFilter);
            return tq;
        }

        public void ProcessMigrate(IdentityCloudContext targetContext, IdentityCloudContext sourceContext, IList<TableEntity> sourceUserResults, int maxDegreesParallel, Action updateComplete = null, Action<string> updateError = null)
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
                        ETag = TableConstants.ETagWildcard
                    };
                    var r = targetContext.IndexTable.UpsertEntity(index, TableUpdateMode.Replace);
                    updateComplete?.Invoke();
                }
                catch (Exception ex)
                {
                    updateError?.Invoke(string.Format("{0}\t{1}", dte.PartitionKey, ex.Message));
                }

            });
        }

        public bool UserWhereFilter(TableEntity d)
        {
            return d.RowKey.StartsWith(_keyHelper.PreFixIdentityUserRole) || d.RowKey.StartsWith(_keyHelper.PreFixIdentityUserClaim);
        }
    }
}
