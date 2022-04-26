// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Azure.Data.Tables;

namespace ElCamino.AspNetCore.Identity.AzureTable
{
    public class IdentityCloudContext : IDisposable
    {
        protected TableServiceClient _client = null;
        protected bool _disposed = false;
        protected IdentityConfiguration _config = null;
        protected TableClient _roleTable;
        protected TableClient _indexTable;
        protected TableClient _userTable;

        public IdentityCloudContext(IdentityConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            Initialize(config);
        }

        protected virtual void Initialize(IdentityConfiguration config)
        {
            _config = config;
            _client = new TableServiceClient(_config.StorageConnectionString);
           
            _indexTable = _client.GetTableClient(FormatTableNameWithPrefix(!string.IsNullOrWhiteSpace(_config.IndexTableName) ? _config.IndexTableName : TableConstants.TableNames.IndexTable));
            _roleTable = _client.GetTableClient(FormatTableNameWithPrefix(!string.IsNullOrWhiteSpace(_config.RoleTableName) ? _config.RoleTableName : TableConstants.TableNames.RolesTable));
            _userTable = _client.GetTableClient(FormatTableNameWithPrefix(!string.IsNullOrWhiteSpace(_config.UserTableName) ? _config.UserTableName : TableConstants.TableNames.UsersTable));
        }

        ~IdentityCloudContext()
        {
            Dispose(false);
        }

        private string FormatTableNameWithPrefix(string baseTableName)
        {
            if(!string.IsNullOrWhiteSpace(_config.TablePrefix))
            {
                return string.Format("{0}{1}", _config.TablePrefix, baseTableName);
            }
            return baseTableName;
        }

        public TableClient RoleTable
        {
            get
            {
                ThrowIfDisposed();
                return _roleTable;
            }
        }

        public TableClient UserTable
        {
            get
            {
                ThrowIfDisposed();
                return _userTable;
            }
        }

        public TableClient IndexTable
        {
            get
            {
                ThrowIfDisposed();
                return _indexTable;
            }
        }

        public TableServiceClient Client
        {
            get
            {
                ThrowIfDisposed();
                return _client;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(base.GetType().Name);
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _client = null;
                _indexTable = null;
                _roleTable = null;
                _userTable = null;
                _disposed = true;
            }
        }
    }
}
