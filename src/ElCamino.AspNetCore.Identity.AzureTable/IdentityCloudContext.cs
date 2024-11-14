// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using Azure.Data.Tables;
using ElCamino.AspNetCore.Identity.AzureTable.Model;

namespace ElCamino.AspNetCore.Identity.AzureTable
{
    /// <summary>
    /// Identity table storage access
    /// </summary>
    public class IdentityCloudContext
    {
        private readonly TableServiceClient _client;
        private readonly TableClient _roleTable;
        private readonly TableClient _indexTable;
        private readonly TableClient _userTable;

        /// <summary>
        /// Uses <see cref="IdentityConfiguration"/> and <see cref="TableServiceClient"/> to configure identity table storage access
        /// </summary>
        /// <param name="config">Accepts <see cref="IdentityConfiguration"/></param>
        /// <param name="client">Accepts <see cref="TableServiceClient"/></param>
        public IdentityCloudContext(IdentityConfiguration config, TableServiceClient client)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(config, nameof(config));
            ArgumentNullException.ThrowIfNull(client, nameof(client));
#else
            _ = config ?? throw new ArgumentNullException(nameof(config));
            _ = client ?? throw new ArgumentNullException(nameof(client));
#endif

            _client = client;
            _indexTable = _client.GetTableClient(FormatTableNameWithPrefix(config!.TablePrefix, !string.IsNullOrWhiteSpace(config!.IndexTableName) ? config!.IndexTableName! : TableConstants.TableNames.IndexTable));
            _roleTable = _client.GetTableClient(FormatTableNameWithPrefix(config!.TablePrefix, !string.IsNullOrWhiteSpace(config!.RoleTableName) ? config!.RoleTableName! : TableConstants.TableNames.RolesTable));
            _userTable = _client.GetTableClient(FormatTableNameWithPrefix(config!.TablePrefix, !string.IsNullOrWhiteSpace(config!.UserTableName) ? config!.UserTableName! : TableConstants.TableNames.UsersTable));
        }


        private static string FormatTableNameWithPrefix(string? tablePrefix, string baseTableName)
        {
            if (!string.IsNullOrWhiteSpace(tablePrefix))
            {
                return string.Format("{0}{1}", tablePrefix!, baseTableName);
            }
            return baseTableName;
        }

        /// <summary>
        /// Access Role table information
        /// </summary>
        public TableClient RoleTable => _roleTable;

        /// <summary>
        /// Access User table information
        /// </summary>
        public TableClient UserTable => _userTable;

        /// <summary>
        /// Access Index table information
        /// </summary>
        public TableClient IndexTable => _indexTable;

        /// <summary>
        /// Table Service access
        /// </summary>
        public TableServiceClient Client => _client;
    }
}
