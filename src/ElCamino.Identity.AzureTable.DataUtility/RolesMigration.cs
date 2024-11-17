// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Model;

namespace ElCamino.Identity.AzureTable.DataUtility
{
    public class RolesMigration : IMigration
    {
        private readonly IKeyHelper _keyHelper;
        public RolesMigration(IKeyHelper keyHelper)
        {
            _keyHelper = keyHelper;
        }

        public TableQuery GetSourceTableQuery()
        {
            //Get all User key records
            TableQuery tq = new TableQuery();
            var keyVersionFilter = TableQuery.GenerateFilterConditionForDouble("KeyVersion", QueryComparisons.LessThan, _keyHelper.KeyVersion);

            tq.FilterString = keyVersionFilter.ToString();
            return tq;
        }

        public void ProcessMigrate(IdentityCloudContext targetContext, IdentityCloudContext sourceContext, IList<TableEntity> sourceUserKeysResults, int maxDegreesParallel, Action? updateComplete = null, Action<string>? updateError = null)
        {

            var result2 = Parallel.ForEach(sourceUserKeysResults, new ParallelOptions() { MaxDegreeOfParallelism = maxDegreesParallel }, (dte) =>
            {
                try
                {
                    targetContext.RoleTable.UpsertEntity(ConvertToTargetRoleEntity(dte, sourceContext), TableUpdateMode.Replace);

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

        private string? GetRoleNameBySourceId(string roleRowKey, IdentityCloudContext sourcesContext)
        {
            var tr = sourcesContext.RoleTable.GetEntityOrDefaultAsync<TableEntity>(
                _keyHelper.ParsePartitionKeyIdentityRoleFromRowKey(roleRowKey),
                roleRowKey, new List<string>() { nameof(IdentityRole.Name), nameof(TableEntity.PartitionKey), nameof(TableEntity.RowKey) }).Result;

            if (tr != null)
            {
                var role = (TableEntity)tr;
                if (role.TryGetValue(nameof(IdentityRole.Name), out object nameProperty))
                {
                    return nameProperty?.ToString();
                }
            }
            return null;
        }

        private TableEntity? ConvertToTargetRoleEntity(TableEntity sourceEntity, IdentityCloudContext sourcesContext)
        {
            TableEntity? targetEntity = null;
            //RoleClaim record
            if (sourceEntity.PartitionKey.StartsWith(_keyHelper.PreFixIdentityRole)
                && sourceEntity.RowKey.StartsWith(_keyHelper.PreFixIdentityRoleClaim))
            {
                sourceEntity.TryGetValue("ClaimType", out object claimTypeProperty);
                string? claimType = claimTypeProperty?.ToString();

                sourceEntity.TryGetValue("ClaimValue", out object claimValueProperty);
                string? claimValue = claimValueProperty?.ToString();

                string? roleName = GetRoleNameBySourceId(sourceEntity.PartitionKey, sourcesContext);

                targetEntity = new TableEntity(sourceEntity);
                targetEntity.ResetKeys(_keyHelper.GenerateRowKeyIdentityRole(roleName),
                    _keyHelper.GenerateRowKeyIdentityRoleClaim(claimType, claimValue), TableConstants.ETagWildcard);
                targetEntity["KeyVersion"] = _keyHelper.KeyVersion;
            }
            else if (sourceEntity.RowKey.StartsWith(_keyHelper.PreFixIdentityRole))
            {
                sourceEntity.TryGetValue("Name", out object roleNameProperty);
                string? roleName = roleNameProperty?.ToString();

                targetEntity = new TableEntity(sourceEntity);
                targetEntity.ResetKeys(_keyHelper.GeneratePartitionKeyIdentityRole(roleName), _keyHelper.GenerateRowKeyIdentityRole(roleName), TableConstants.ETagWildcard);
                targetEntity["KeyVersion"] = _keyHelper.KeyVersion;

            }

            return targetEntity;
        }

        public bool UserWhereFilter(TableEntity d)
        {
            return true;
        }
    }
}
