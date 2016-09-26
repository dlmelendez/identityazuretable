using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElCamino.Identity.AzureTable.DataUtility
{
    public class LoginMigrateIndex : IMigrateIndex
    {
        public TableQuery GetUserTableQuery()
        {
            TableQuery tq = new TableQuery();
            tq.SelectColumns = new List<string>() { "PartitionKey", "RowKey", "LoginProvider", "ProviderKey" };
            string partitionFilter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, Constants.RowKeyConstants.PreFixIdentityUserName),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThan, "V_"));
            string rowFilter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, Constants.RowKeyConstants.PreFixIdentityUserLogin),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, "M_"));
            tq.FilterString = TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);
            return tq;
        }


        public bool UserWhereFilter(DynamicTableEntity d)
        {
            return !string.IsNullOrWhiteSpace(d.Properties["LoginProvider"].StringValue)
                && !string.IsNullOrWhiteSpace(d.Properties["ProviderKey"].StringValue);
        }

        public void ProcessMigrate(IdentityCloudContext ic,
            IList<DynamicTableEntity> userResults,
            int maxDegreesParallel,
            Action updateComplete = null,
            Action<string> updateError = null)
        {
            var userIds = userResults
                .Where(UserWhereFilter)
                .Select(d => new
                {
                    UserId = d.PartitionKey,
                    LoginProvider = d.Properties["LoginProvider"].StringValue,
                    ProviderKey = d.Properties["ProviderKey"].StringValue
                })
                .ToList();


            var result2 = Parallel.ForEach(userIds, new ParallelOptions() { MaxDegreeOfParallelism = maxDegreesParallel }, (userId) =>
            {

                //Add the email index
                try
                {
                    IdentityUserIndex index = CreateLoginIndex(userId.UserId, userId.LoginProvider, userId.ProviderKey);
                    var r = ic.IndexTable.ExecuteAsync(TableOperation.InsertOrReplace(index)).Result;
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
                PartitionKey = KeyHelper.GeneratePartitionKeyIndexByLogin(loginProvider, providerKey),
                RowKey = KeyHelper.GenerateRowKeyIdentityUserLogin(loginProvider, providerKey),
                KeyVersion = KeyHelper.KeyVersion,
                ETag = Constants.ETagWildcard
            };

        }


    }
}
