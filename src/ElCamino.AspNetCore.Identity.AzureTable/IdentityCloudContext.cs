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

        public IdentityCloudContext() { }
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
            //if (!string.IsNullOrWhiteSpace(_config.LocationMode))
            //{
            //    LocationMode mode = LocationMode.PrimaryOnly;
            //    if (Enum.TryParse<LocationMode>(_config.LocationMode, out mode))
            //    {
            //        _client.DefaultRequestOptions.LocationMode = mode;
            //    }
            //    else
            //    {
            //        throw new ArgumentException("Invalid LocationMode defined in config. For more information on geo-replication location modes: http://msdn.microsoft.com/en-us/library/azure/microsoft.windowsazure.storage.retrypolicies.locationmode.aspx", "config.LocationMode");
            //    }
            //}

            _indexTable = _client.GetTableClient(FormatTableNameWithPrefix(!string.IsNullOrWhiteSpace(_config.IndexTableName) ? _config.IndexTableName : Constants.TableNames.IndexTable));
            _roleTable = _client.GetTableClient(FormatTableNameWithPrefix(!string.IsNullOrWhiteSpace(_config.RoleTableName) ? _config.RoleTableName : Constants.TableNames.RolesTable));
            _userTable = _client.GetTableClient(FormatTableNameWithPrefix(!string.IsNullOrWhiteSpace(_config.UserTableName) ? _config.UserTableName : Constants.TableNames.UsersTable));
        }

        ~IdentityCloudContext()
        {
            this.Dispose(false);
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
            if (this._disposed)
            {
                throw new ObjectDisposedException(base.GetType().Name);
            }
        }
        public void Dispose()
        {
            this.Dispose(true);
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
