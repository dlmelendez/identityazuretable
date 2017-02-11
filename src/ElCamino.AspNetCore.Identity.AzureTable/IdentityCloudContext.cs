// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
#if net45
using ElCamino.AspNet.Identity.AzureTable.Configuration;
using Microsoft.Azure;
using ElCamino.AspNet.Identity.AzureTable.Model;
#else
using ElCamino.AspNetCore.Identity.AzureTable.Model;
#endif
using Microsoft.WindowsAzure.Storage.RetryPolicies;

#if net45
namespace ElCamino.AspNet.Identity.AzureTable
#else
namespace ElCamino.AspNetCore.Identity.AzureTable
#endif
{
    public class IdentityCloudContext : IDisposable
    {
        private CloudTableClient _client = null;
        private bool _disposed = false;
        private IdentityConfiguration _config = null;
        private CloudTable _roleTable;
        private CloudTable _indexTable;
        private CloudTable _userTable;

#if net45
		public IdentityCloudContext() 
        {
            IdentityConfiguration config = IdentityConfigurationSection.GetCurrent();
            //For backwards compat for those who do not use the new configSection.
            if (config == null)
            {
                config = new IdentityConfiguration()
                {
                    StorageConnectionString =  CloudConfigurationManager.GetSetting(Constants.AppSettingsKeys.DefaultStorageConnectionStringKey),
                    TablePrefix = string.Empty
                };
            }
            Initialize(config);
        }

        [System.Obsolete("Please use the default constructor IdentityCloudContext() to load the configSection from web/app.config or " +
            "the constructor IdentityCloudContext(IdentityConfiguration config) for more options.")]
        public IdentityCloudContext(string connectionStringKey)
        {
            string strConnection = CloudConfigurationManager.GetSetting(connectionStringKey);
            Initialize(new IdentityConfiguration()
            {
                StorageConnectionString = string.IsNullOrWhiteSpace(strConnection) ?
                    connectionStringKey : strConnection,
                TablePrefix = string.Empty
            });
            
        }
#else
		public IdentityCloudContext() { }
#endif
		public IdentityCloudContext(IdentityConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }
            Initialize(config);
        }


        private void Initialize(IdentityConfiguration config)
        {
            _config = config;
            _client = CloudStorageAccount.Parse(_config.StorageConnectionString).CreateCloudTableClient();
			_client.DefaultRequestOptions.PayloadFormat = TablePayloadFormat.Json;
            if (!string.IsNullOrWhiteSpace(_config.LocationMode))
            {
                LocationMode mode = LocationMode.PrimaryOnly;
                if (Enum.TryParse<LocationMode>(_config.LocationMode, out mode))
                {
                    _client.DefaultRequestOptions.LocationMode = mode;
                }
                else
                {
                    throw new ArgumentException("Invalid LocationMode defined in config. For more information on geo-replication location modes: http://msdn.microsoft.com/en-us/library/azure/microsoft.windowsazure.storage.retrypolicies.locationmode.aspx", "config.LocationMode");
                }
            }
            _indexTable = _client.GetTableReference(FormatTableNameWithPrefix(Constants.TableNames.IndexTable));
            _roleTable = _client.GetTableReference(FormatTableNameWithPrefix(Constants.TableNames.RolesTable)); 
            _userTable = _client.GetTableReference(FormatTableNameWithPrefix(Constants.TableNames.UsersTable));
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

        public CloudTable RoleTable
        {
            get
            {
                ThrowIfDisposed();
                return _roleTable;
            }
        }

        public CloudTable UserTable
        {
            get
            {
                ThrowIfDisposed();
                return _userTable;
            }
        }

        public CloudTable IndexTable
        {
            get
            {
                ThrowIfDisposed();
                return _indexTable;
            }
        }

        public CloudTableClient Client
        {
            get
            {
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
