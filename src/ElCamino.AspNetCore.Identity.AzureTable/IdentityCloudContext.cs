// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using Azure.Data.Tables;
using ElCamino.AspNetCore.Identity.AzureTable.Model;

namespace ElCamino.AspNetCore.Identity.AzureTable
{
    public class IdentityCloudContext
    {
        protected TableServiceClient _client;
        protected IdentityConfiguration _config;
        protected TableClient _roleTable;
        protected TableClient _indexTable;
        protected TableClient _userTable;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public IdentityCloudContext(IdentityConfiguration config)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            Initialize(config);
        }

        protected virtual void Initialize(IdentityConfiguration config)
        {
            _config = config;
            _client = new TableServiceClient(_config.StorageConnectionString);

            _indexTable = _client.GetTableClient(FormatTableNameWithPrefix(!string.IsNullOrWhiteSpace(_config?.IndexTableName) ? _config!.IndexTableName! : TableConstants.TableNames.IndexTable));
            _roleTable = _client.GetTableClient(FormatTableNameWithPrefix(!string.IsNullOrWhiteSpace(_config?.RoleTableName) ? _config!.RoleTableName! : TableConstants.TableNames.RolesTable));
            _userTable = _client.GetTableClient(FormatTableNameWithPrefix(!string.IsNullOrWhiteSpace(_config?.UserTableName) ? _config!.UserTableName! : TableConstants.TableNames.UsersTable));
        }

        private string FormatTableNameWithPrefix(string baseTableName)
        {
            if (!string.IsNullOrWhiteSpace(_config?.TablePrefix))
            {
                return string.Format("{0}{1}", _config!.TablePrefix, baseTableName);
            }
            return baseTableName;
        }

        public TableClient RoleTable => _roleTable;

        public TableClient UserTable => _userTable;

        public TableClient IndexTable => _indexTable;

        public TableServiceClient Client => _client;
    }
}
