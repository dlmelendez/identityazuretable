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
    public class LoginMigrateIndex : IMigration
    {
        private readonly IKeyHelper _keyHelper;
        public LoginMigrateIndex(IKeyHelper keyHelper)
        {
            _keyHelper = keyHelper;
        }

        public TableQuery GetSourceTableQuery()
        {
            TableQuery tq = new TableQuery();
            tq.SelectColumns = ["PartitionKey", "RowKey", "LoginProvider", "ProviderKey"];
            var partitionFilter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, _keyHelper.PreFixIdentityUserId),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThan, _keyHelper.PreFixIdentityUserIdUpperBound));
            var rowFilter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, _keyHelper.PreFixIdentityUserLogin),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, _keyHelper.PreFixIdentityUserLoginUpperBound));
            tq.FilterString = TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter).ToString();
            return tq;
        }


        public bool UserWhereFilter(TableEntity d)
        {
            return !string.IsNullOrWhiteSpace(d["LoginProvider"]?.ToString())
                && !string.IsNullOrWhiteSpace(d["ProviderKey"]?.ToString());
        }

        public void ProcessMigrate(IdentityCloudContext targetContext,
            IdentityCloudContext sourceContext,
            IList<TableEntity> sourceUserResults,
            int maxDegreesParallel,
            Action? updateComplete = null,
            Action<string>? updateError = null)
        {
            var userIds = sourceUserResults
                .Where(UserWhereFilter)
                .Select(d => new
                {
                    UserId = d.PartitionKey,
                    LoginProvider = d["LoginProvider"].ToString(),
                    ProviderKey = d["ProviderKey"].ToString()
                })
                .ToList();


            var result2 = Parallel.ForEach(userIds, new ParallelOptions() { MaxDegreeOfParallelism = maxDegreesParallel }, (userId) =>
            {

                //Add the email index
                try
                {
                    IdentityUserIndex index = CreateLoginIndex(userId.UserId, userId.LoginProvider!, userId.ProviderKey!);
                    var r = targetContext.IndexTable.UpsertEntity(index, TableUpdateMode.Replace);
                    updateComplete?.Invoke();
                }
                catch (Exception ex)
                {
                    updateError?.Invoke(string.Format("{0}\t{1}", userId.UserId, ex.Message));
                }

            });

        }

        private IdentityUserIndex CreateLoginIndex(string userid, string loginProvider, string providerKey)
        {
            return new IdentityUserIndex()
            {
                Id = userid,
                PartitionKey = _keyHelper.GeneratePartitionKeyIndexByLogin(loginProvider, providerKey).ToString(),
                RowKey = _keyHelper.GenerateRowKeyIdentityUserLogin(loginProvider, providerKey).ToString(),
                KeyVersion = _keyHelper.KeyVersion,
                ETag = TableConstants.ETagWildcard
            };

        }


    }
}
