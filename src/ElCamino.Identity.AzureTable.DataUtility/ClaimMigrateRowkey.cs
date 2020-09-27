﻿// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElCamino.Identity.AzureTable.DataUtility
{
    public class ClaimMigrateRowkey : IMigration
    {
        private IKeyHelper _keyHelper;
        public ClaimMigrateRowkey(IKeyHelper keyHelper)
        {
            _keyHelper = keyHelper;
        }

        public TableQuery GetSourceTableQuery()
        {
            TableQuery tq = new TableQuery();
            string partitionFilter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, _keyHelper.PreFixIdentityUserId),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThan, _keyHelper.PreFixIdentityUserIdUpperBound));
            string rowFilter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, _keyHelper.PreFixIdentityUserClaim),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, _keyHelper.PreFixIdentityUserClaimUpperBound));
            tq.FilterString = TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);
            return tq;
        }


        public bool UserWhereFilter(DynamicTableEntity d)
        {
            string claimType = d.Properties["ClaimType"]?.StringValue;
            string claimValue = d.Properties["ClaimValue"]?.StringValue;

            if(!string.IsNullOrWhiteSpace(claimType))
            {
                return (d.RowKey != _keyHelper.GenerateRowKeyIdentityUserClaim(claimType, claimValue??string.Empty));                
            }

            return false;
        }

        public void ProcessMigrate(IdentityCloudContext targetContext,
            IdentityCloudContext sourceContext,
            IList<DynamicTableEntity> claimResults,
            int maxDegreesParallel,
            Action updateComplete = null,
            Action<string> updateError = null)
        {
            const string KeyVersion = "KeyVersion";

            var claims = claimResults
                .Where(UserWhereFilter)
                .ToList();


            var result2 = Parallel.ForEach(claims, new ParallelOptions() { MaxDegreeOfParallelism = maxDegreesParallel }, (claim) =>
            {

                //Add the new claim index
                try
                {

                    var claimNew = new DynamicTableEntity(claim.PartitionKey,
                        _keyHelper.GenerateRowKeyIdentityUserClaim(claim.Properties["ClaimType"].StringValue, claim.Properties["ClaimValue"].StringValue),
                        Constants.ETagWildcard,
                        claim.Properties);
                    if (claimNew.Properties.ContainsKey(KeyVersion))
                    {
                        claimNew.Properties[KeyVersion].DoubleValue = _keyHelper.KeyVersion;
                    }
                    else
                    {
                        claimNew.Properties.Add(KeyVersion, EntityProperty.GeneratePropertyForDouble(_keyHelper.KeyVersion));
                    }

                    var taskExecute = targetContext.UserTable.ExecuteAsync(TableOperation.InsertOrReplace(claimNew));
                    taskExecute.Wait();

                    updateComplete?.Invoke();
                }
                catch (Exception ex)
                {
                    updateError?.Invoke(string.Format("{0}\t{1}", claim.PartitionKey, ex.Message));
                }

            });

        }        

    }

}
