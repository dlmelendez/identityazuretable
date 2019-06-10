// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

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
    public class RolesMigration : IMigration
    {
        public TableQuery GetSourceTableQuery()
        {
            //Get all User key records
            TableQuery tq = new TableQuery();
            string keyVersionFilter = TableQuery.GenerateFilterConditionForDouble("KeyVersion", QueryComparisons.LessThan, KeyHelper.KeyVersion);

            tq.FilterString = keyVersionFilter;
            return tq;
        }

        public void ProcessMigrate(IdentityCloudContext targetContext, IdentityCloudContext sourceContext, IList<DynamicTableEntity> sourceUserKeysResults, int maxDegreesParallel, Action updateComplete = null, Action<string> updateError = null)
        {

            var result2 = Parallel.ForEach(sourceUserKeysResults, new ParallelOptions() { MaxDegreeOfParallelism = maxDegreesParallel }, (dte) =>
            {
                try
                {
                    targetContext.RoleTable.ExecuteAsync(TableOperation.InsertOrReplace(ConvertToTargetRoleEntity(dte, sourceContext)));
                   
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

        private string GetRoleNameBySourceId(string roleRowKey, IdentityCloudContext sourcesContext)
        {

            var tr = sourcesContext.RoleTable.ExecuteAsync(
                TableOperation.Retrieve<DynamicTableEntity>(KeyHelper.ParsePartitionKeyIdentityRoleFromRowKey(roleRowKey),
               roleRowKey, new List<string>() { "Name", "PartitionKey", "RowKey" })).Result;
            if (tr.Result != null)
            {
                var role = (DynamicTableEntity)tr.Result;
                if (role.Properties.TryGetValue("Name", out EntityProperty nameProperty))
                {
                    return nameProperty.StringValue;
                }
            }
            return null;
        }

        private DynamicTableEntity ConvertToTargetRoleEntity(DynamicTableEntity sourceEntity, IdentityCloudContext sourcesContext)
        {
            DynamicTableEntity targetEntity = null;
            //RoleClaim record
            if (sourceEntity.PartitionKey.StartsWith(Constants.RowKeyConstants.PreFixIdentityRole)
                && sourceEntity.RowKey.StartsWith(Constants.RowKeyConstants.PreFixIdentityRoleClaim))
            {
                sourceEntity.Properties.TryGetValue("ClaimType", out EntityProperty claimTypeProperty);
                string claimType = claimTypeProperty.StringValue;

                sourceEntity.Properties.TryGetValue("ClaimValue", out EntityProperty claimValueProperty);
                string claimValue = claimValueProperty.StringValue;

                string roleName = GetRoleNameBySourceId(sourceEntity.PartitionKey, sourcesContext);

                targetEntity = new DynamicTableEntity(KeyHelper.GenerateRowKeyIdentityRole(roleName), 
                    KeyHelper.GenerateRowKeyIdentityRoleClaim(claimType, claimValue), Constants.ETagWildcard, sourceEntity.Properties);
                targetEntity.Properties["KeyVersion"] = new EntityProperty(KeyHelper.KeyVersion);
            }
            else if (sourceEntity.RowKey.StartsWith(Constants.RowKeyConstants.PreFixIdentityRole))
            {
                sourceEntity.Properties.TryGetValue("Name", out EntityProperty roleNameProperty);
                string roleName = roleNameProperty.StringValue;

                targetEntity = new DynamicTableEntity(KeyHelper.GeneratePartitionKeyIdentityRole(roleName), KeyHelper.GenerateRowKeyIdentityRole(roleName), Constants.ETagWildcard, sourceEntity.Properties);
                targetEntity.Properties["KeyVersion"] = new EntityProperty(KeyHelper.KeyVersion);

            }

            return targetEntity;
        }

        public bool UserWhereFilter(DynamicTableEntity d)
        {
            return true;
        }
    }
}