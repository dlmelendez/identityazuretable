using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Microsoft.WindowsAzure.Storage.Table;

namespace ElCamino.Identity.AzureTable.DataUtility
{
    public class UsernameMigrateIndex : IMigration
    {
        public TableQuery GetUserTableQuery()
        {
            TableQuery tq = new TableQuery();
            tq.SelectColumns = new List<string>() { "PartitionKey", "RowKey", "Username" };
            string partitionFilter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, Constants.RowKeyConstants.PreFixIdentityUserName),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThan, "V_"));
            string rowFilter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, Constants.RowKeyConstants.PreFixIdentityUserName),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, "V_"));
            tq.FilterString = TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);
            return tq;
        }

        public bool UserWhereFilter(DynamicTableEntity d)
        {
            return true;//every user has a username :)
        }

        public void ProcessMigrate(IdentityCloudContext ic, IList<DynamicTableEntity> userResults, int maxDegreesParallel, Action updateComplete = null,
            Action<string> updateError = null)
        {
            var userIds = userResults
                .Where(UserWhereFilter)
                .Select(d => new { UserId = d.PartitionKey, Username = d.Properties["Username"].StringValue })
                .ToList();


            var result2 = Parallel.ForEach(userIds, new ParallelOptions() { MaxDegreeOfParallelism = maxDegreesParallel }, (userId) =>
            {

                //Add the email index
                try
                {
                    IdentityUserIndex index = CreateUsernameIndex(userId.UserId, userId.Username);
                    var r = ic.IndexTable.ExecuteAsync(TableOperation.InsertOrReplace(index)).Result;
                    updateComplete?.Invoke();
                }
                catch (Exception ex)
                {
                    updateError?.Invoke(string.Format("{0}\t{1}", userId.UserId, ex.Message));
                }

            });
        }

        /// <summary>
        /// Creates an email index suitable for a crud operation
        /// </summary>
        /// <param name="userid">Formatted UserId from the KeyHelper or IdentityUser.Id.ToString()</param>
        /// <param name="email">Plain email address.</param>
        /// <returns></returns>
        private IdentityUserIndex CreateUsernameIndex(string userid, string username)
        {
            return new IdentityUserIndex()
            {
                Id = userid,
                PartitionKey = KeyHelper.GenerateRowKeyUserName(username),
                RowKey = userid,
                KeyVersion = KeyHelper.KeyVersion,
                ETag = Constants.ETagWildcard
            };
        }
    }
}
