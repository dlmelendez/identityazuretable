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
    public class UsersMigration : IMigration
    {
        private readonly IKeyHelper _keyHelper;
        public UsersMigration(IKeyHelper keyHelper)
        {
            _keyHelper = keyHelper;
        }

        public TableQuery GetSourceTableQuery()
        {
            //Get all User key records
            TableQuery tq = new TableQuery();
            tq.SelectColumns = new List<string>() { "PartitionKey", "RowKey", "KeyVersion" };
            string partitionFilter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, _keyHelper.PreFixIdentityUserId),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThan, _keyHelper.PreFixIdentityUserIdUpperBound));
            string rowFilter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, _keyHelper.PreFixIdentityUserId),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, _keyHelper.PreFixIdentityUserIdUpperBound));
            string keyVersionFilter = TableQuery.GenerateFilterConditionForDouble("KeyVersion", QueryComparisons.LessThan, _keyHelper.KeyVersion);
            string keysFilter = TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);

            tq.FilterString = TableQuery.CombineFilters(keysFilter, TableOperators.And, keyVersionFilter);
            return tq;
        }

        public void ProcessMigrate(IdentityCloudContext targetContext, IdentityCloudContext sourceContext, IList<TableEntity> sourceUserKeysResults, int maxDegreesParallel, Action? updateComplete = null, Action<string>? updateError = null)
        {

            var result2 = Parallel.ForEach(sourceUserKeysResults, new ParallelOptions() { MaxDegreeOfParallelism = maxDegreesParallel }, (dte) =>
            {
                try
                {
                    var sourceUserEntities = GetUserEntitiesBySourceId(dte.PartitionKey, sourceContext);
                    string targetUserId = _keyHelper.GenerateUserId();
                    var targetEntities = ConvertToTargetUserEntities(targetUserId, sourceUserEntities);
                    List<Task> mainTasks = new List<Task>(2);
                    List<Task> indexTasks = new List<Task>(100);
                    BatchOperationHelper batchOperation = new BatchOperationHelper(targetContext.UserTable);
                    targetEntities.targetUserEntities
                        .ForEach(targetUserRecord => batchOperation.UpsertEntity<TableEntity>(targetUserRecord, mode: TableUpdateMode.Replace));
                    mainTasks.Add(batchOperation.SubmitBatchAsync());
                    targetEntities.targetUserIndexes
                    .ForEach(targetIndexRecord => indexTasks.Add(targetContext.IndexTable.UpsertEntityAsync(targetIndexRecord, TableUpdateMode.Replace)));
                    mainTasks.Add(Task.WhenAll(indexTasks));
                    Task.WhenAll(mainTasks).Wait();
                    updateComplete?.Invoke();
                }
                catch (AggregateException exagg)
                {
                    updateError?.Invoke(string.Format("{0}-{1}\t{2}", dte.PartitionKey, dte.RowKey, exagg.Flatten().Message));
                }
                catch (Exception ex)
                {
                    updateError?.Invoke(string.Format("{0}-{1}\t{2}", dte.PartitionKey, dte.RowKey, ex.Message));
                }

            });
        }

        private List<TableEntity> GetUserEntitiesBySourceId(string userPartitionKey, IdentityCloudContext sourcesContext)
        {
            TableQuery tq = new TableQuery();
            string partitionKeyFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userPartitionKey);
            string keyVersionFilter = TableQuery.GenerateFilterConditionForDouble("KeyVersion", QueryComparisons.LessThan, _keyHelper.KeyVersion);
            tq.FilterString = TableQuery.CombineFilters(partitionKeyFilter, TableOperators.And, keyVersionFilter);
            var r = sourcesContext.UserTable.Query<TableEntity>(tq.FilterString);
            return r.ToList();
        }

        private (List<TableEntity> targetUserEntities, List<IdentityUserIndex> targetUserIndexes) ConvertToTargetUserEntities(string userId, List<TableEntity> sourceUserEntities)
        {
            List<TableEntity> targetUserEntities = new List<TableEntity>(100);
            List<IdentityUserIndex> targetUserIndexes = new List<IdentityUserIndex>(100);
            foreach (TableEntity sourceEntity in sourceUserEntities)
            {
                if (sourceEntity.PartitionKey.StartsWith(_keyHelper.PreFixIdentityUserId))
                {
                    string targetUserPartitionKey = _keyHelper.GenerateRowKeyUserId(userId);

                    //User record
                    if (sourceEntity.RowKey.StartsWith(_keyHelper.PreFixIdentityUserId))
                    {
                        //New User 
                        //Add UserName Index
                        //Add Email Index
                        TableEntity tgtDte = new TableEntity(sourceEntity);
                        tgtDte.ResetKeys(targetUserPartitionKey, targetUserPartitionKey, TableConstants.ETagWildcard);
                        tgtDte["Id"] = userId;
                        tgtDte["KeyVersion"] = _keyHelper.KeyVersion;
                        targetUserEntities.Add(tgtDte);

                        //UserName index
                        tgtDte.TryGetValue("UserName", out object userNameProperty);
                        string userNameKey = _keyHelper.GenerateRowKeyUserName(userNameProperty.ToString());
                        IdentityUserIndex userNameIndex = new IdentityUserIndex()
                        {
                            Id = targetUserPartitionKey,
                            PartitionKey = userNameKey,
                            RowKey = targetUserPartitionKey,
                            KeyVersion = _keyHelper.KeyVersion,
                            ETag = TableConstants.ETagWildcard
                        };
                        targetUserIndexes.Add(userNameIndex);

                        //Email index - only if email exists
                        if (tgtDte.TryGetValue("Email", out object emailProperty))
                        {
                            string emailKey = _keyHelper.GenerateRowKeyUserEmail(emailProperty.ToString());
                            IdentityUserIndex emailIndex = new IdentityUserIndex()
                            {
                                Id = targetUserPartitionKey,
                                PartitionKey = emailKey,
                                RowKey = targetUserPartitionKey,
                                KeyVersion = _keyHelper.KeyVersion,
                                ETag = TableConstants.ETagWildcard
                            };
                            targetUserIndexes.Add(emailIndex);
                        }
                        continue;
                    }
                    //User Claim record
                    if (sourceEntity.RowKey.StartsWith(_keyHelper.PreFixIdentityUserClaim))
                    {
                        //New User Claim
                        //Add Claim Index
                        sourceEntity.TryGetValue("ClaimType", out object claimTypeProperty);
                        string? claimType = claimTypeProperty?.ToString();
                        sourceEntity.TryGetValue("ClaimValue", out object claimValueProperty);
                        string? claimValue = claimValueProperty?.ToString();

                        string targetUserRowKey = _keyHelper.GenerateRowKeyIdentityUserClaim(claimType, claimValue);
                        TableEntity tgtDte = new TableEntity(sourceEntity);
                        tgtDte.ResetKeys(targetUserPartitionKey, targetUserRowKey, TableConstants.ETagWildcard);
                        tgtDte["UserId"] = userId;
                        tgtDte["KeyVersion"] = _keyHelper.KeyVersion;
                        targetUserEntities.Add(tgtDte);

                        //Claim index
                        IdentityUserIndex claimIndex = new IdentityUserIndex()
                        {
                            Id = targetUserPartitionKey,
                            PartitionKey = targetUserRowKey,
                            RowKey = targetUserPartitionKey,
                            KeyVersion = _keyHelper.KeyVersion,
                            ETag = TableConstants.ETagWildcard
                        };
                        targetUserIndexes.Add(claimIndex);
                        continue;
                    }
                    //User Logon record
                    if (sourceEntity.RowKey.StartsWith(_keyHelper.PreFixIdentityUserLogin))
                    {
                        //New User Logon
                        //Add Logon Index
                        sourceEntity.TryGetValue("LoginProvider", out object loginProviderProperty);
                        string? loginProvider = loginProviderProperty?.ToString();
                        sourceEntity.TryGetValue("ProviderKey", out object providerKeyProperty);
                        string? providerKey = providerKeyProperty?.ToString();

                        string targetUserRowKey = _keyHelper.GenerateRowKeyIdentityUserLogin(loginProvider, providerKey);
                        TableEntity tgtDte = new TableEntity(sourceEntity);
                        tgtDte.ResetKeys(targetUserPartitionKey, targetUserRowKey, TableConstants.ETagWildcard);
                        tgtDte["UserId"] = userId;
                        tgtDte["KeyVersion"] = _keyHelper.KeyVersion;
                        targetUserEntities.Add(tgtDte);

                        //Logon index
                        if (!string.IsNullOrWhiteSpace(loginProvider) 
                            && !string.IsNullOrWhiteSpace(providerKey))
                        {
                            IdentityUserIndex logonIndex = new IdentityUserIndex()
                            {
                                Id = targetUserPartitionKey,
                                PartitionKey = _keyHelper.GeneratePartitionKeyIndexByLogin(loginProvider, providerKey),
                                RowKey = _keyHelper.GenerateRowKeyIdentityUserLogin(loginProvider, providerKey),
                                KeyVersion = _keyHelper.KeyVersion,
                                ETag = TableConstants.ETagWildcard
                            };
                            targetUserIndexes.Add(logonIndex);
                        }
                        continue;
                    }
                    //User Role record
                    if (sourceEntity.RowKey.StartsWith(_keyHelper.PreFixIdentityUserRole))
                    {
                        //New User Role
                        //Add Role Index
                        sourceEntity.TryGetValue(nameof(IdentityUserRole<string>.RoleName), out object roleNameProperty);
                        string? roleName = roleNameProperty?.ToString();

                        string targetUserRowKey = _keyHelper.GenerateRowKeyIdentityUserRole(roleName);
                        TableEntity tgtDte = new TableEntity(sourceEntity);
                        tgtDte.ResetKeys(targetUserPartitionKey, targetUserRowKey, TableConstants.ETagWildcard);
                        tgtDte["UserId"] = userId;
                        tgtDte["KeyVersion"] = _keyHelper.KeyVersion;
                        targetUserEntities.Add(tgtDte);

                        //Role index
                        IdentityUserIndex roleIndex = new IdentityUserIndex()
                        {
                            Id = targetUserPartitionKey,
                            PartitionKey = targetUserRowKey,
                            RowKey = targetUserPartitionKey,
                            KeyVersion = _keyHelper.KeyVersion,
                            ETag = TableConstants.ETagWildcard
                        };
                        targetUserIndexes.Add(roleIndex);
                        continue;
                    }
                    //User Token record
                    if (sourceEntity.RowKey.StartsWith(_keyHelper.PreFixIdentityUserToken))
                    {
                        //New User Token
                        sourceEntity.TryGetValue("LoginProvider", out object loginProviderProperty);
                        string? loginProvider = loginProviderProperty?.ToString();
                        sourceEntity.TryGetValue("TokenName", out object tokenNameProperty);
                        string? tokenName = tokenNameProperty?.ToString();

                        if (!string.IsNullOrWhiteSpace(loginProvider) 
                            && !string.IsNullOrWhiteSpace(tokenName))
                        {
                            string targetUserRowKey = _keyHelper.GenerateRowKeyIdentityUserToken(loginProvider, tokenName);
                            TableEntity tgtDte = new TableEntity(sourceEntity);
                            tgtDte.ResetKeys(targetUserPartitionKey, targetUserRowKey, TableConstants.ETagWildcard);
                            tgtDte["UserId"] = userId;
                            tgtDte["KeyVersion"] = _keyHelper.KeyVersion;
                            targetUserEntities.Add(tgtDte);
                        }
                        continue;

                    }
                }
            }
            return (targetUserEntities, targetUserIndexes);
        }

        public bool UserWhereFilter(TableEntity d)
        {
            return true;
        }
    }
}
