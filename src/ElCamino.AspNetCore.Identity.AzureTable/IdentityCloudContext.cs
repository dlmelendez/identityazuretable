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
        /// Uses <see cref="IdentityConfiguration"/> to configure identity table storage access
        /// </summary>
        /// <param name="config">Accepts <see cref="IdentityConfiguration"/></param>
        public IdentityCloudContext(IdentityConfiguration config)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(config, nameof(config));
#else
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }
#endif

            if (string.IsNullOrEmpty(config.StorageConnectionString) && config.StorageConnectionUri == null)
            {
                throw new ArgumentNullException(nameof(config.StorageConnectionString), "Either StorageConnectionString or StorageConnectionUri are required");
            }
            else if (!string.IsNullOrEmpty(config.StorageConnectionString))
            {
                _client = new TableServiceClient(config.StorageConnectionString);

                if (config.TokenCredential != null)
                {
                    //If we've been passed a TokenCredential we can use that instead of the credentials in the connection string
                    _client = new TableServiceClient(_client.Uri, config.TokenCredential);
                }
            }
            else // if (config.StorageConnectionUri != null)
            {
                if (config.TokenCredential == null)
                {
                    throw new ArgumentNullException(nameof(config.TokenCredential), "TokenCredential is required when Uri is specified");
                }
                else
                {
                    _client = new TableServiceClient(config.StorageConnectionUri, config.TokenCredential);
                }
            }

            _indexTable = _client.GetTableClient(FormatTableNameWithPrefix(config.TablePrefix, !string.IsNullOrWhiteSpace(config.IndexTableName) ? config.IndexTableName : TableConstants.TableNames.IndexTable));
            _roleTable = _client.GetTableClient(FormatTableNameWithPrefix(config.TablePrefix, !string.IsNullOrWhiteSpace(config.RoleTableName) ? config.RoleTableName : TableConstants.TableNames.RolesTable));
            _userTable = _client.GetTableClient(FormatTableNameWithPrefix(config.TablePrefix, !string.IsNullOrWhiteSpace(config.UserTableName) ? config.UserTableName : TableConstants.TableNames.UsersTable));
        }

        private static string FormatTableNameWithPrefix(string? tablePrefix, string baseTableName)
        {
            if (!string.IsNullOrWhiteSpace(tablePrefix))
            {
                return string.Format("{0}{1}", tablePrefix, baseTableName);
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
