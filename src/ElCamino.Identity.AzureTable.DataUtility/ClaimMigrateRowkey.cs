﻿// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Model;

namespace ElCamino.Identity.AzureTable.DataUtility
{
    public class ClaimMigrateRowkey : IMigration
    {
        private readonly IKeyHelper _keyHelper;
        public ClaimMigrateRowkey(IKeyHelper keyHelper)
        {
            _keyHelper = keyHelper;
        }

        public TableQuery GetSourceTableQuery()
        {
            TableQuery tq = new TableQuery();
            var partitionFilter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, _keyHelper.PreFixIdentityUserId),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThan, _keyHelper.PreFixIdentityUserIdUpperBound));
            var rowFilter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, _keyHelper.PreFixIdentityUserClaim),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, _keyHelper.PreFixIdentityUserClaimUpperBound));
            tq.FilterString = TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter).ToString();
            return tq;
        }


        public bool UserWhereFilter(TableEntity d)
        {
            string? claimType = d["ClaimType"]?.ToString();
            string? claimValue = d["ClaimValue"]?.ToString();

            if (!string.IsNullOrWhiteSpace(claimType))
            {
                return (!d.RowKey.AsSpan().Equals(_keyHelper.GenerateRowKeyIdentityUserClaim(claimType, claimValue), StringComparison.OrdinalIgnoreCase));
            }

            return false;
        }

        public void ProcessMigrate(IdentityCloudContext targetContext,
            IdentityCloudContext sourceContext,
            IList<TableEntity> claimResults,
            int maxDegreesParallel,
            Action? updateComplete = null,
            Action<string>? updateError = null)
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

                    var claimNew = new TableEntity(claim);
                    claimNew.ResetKeys(claim.PartitionKey,
                        _keyHelper.GenerateRowKeyIdentityUserClaim(claim["ClaimType"].ToString(), claim["ClaimValue"].ToString()).ToString(),
                         TableConstants.ETagWildcard);
                    if (claimNew.ContainsKey(KeyVersion))
                    {
                        claimNew[KeyVersion] = _keyHelper.KeyVersion;
                    }
                    else
                    {
                        claimNew.Add(KeyVersion, _keyHelper.KeyVersion);
                    }

                    targetContext.UserTable.UpsertEntity(claimNew, TableUpdateMode.Replace);

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
