// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using Azure;
using Azure.Data.Tables;

namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    public class IdentityUserLogin : IdentityUserLogin<string>, IGenerateKeys
    {
        public IdentityUserLogin() { }

        /// <summary>
        /// Generates Row and Id keys.
        /// Partition key is equal to the UserId
        /// </summary>
        public void GenerateKeys(IKeyHelper keyHelper)
        {
            Id = Guid.NewGuid().ToString();
            RowKey = PeekRowKey(keyHelper);
            KeyVersion = keyHelper.KeyVersion;
        }

        public double KeyVersion { get; set; }

        /// <summary>
        /// Generates the RowKey without setting it on the object.
        /// </summary>
        /// <returns></returns>
        public string PeekRowKey(IKeyHelper keyHelper)
        {
            return keyHelper.GenerateRowKeyIdentityUserLogin(LoginProvider, ProviderKey);
        }


    }

    public class IdentityUserLogin<TKey> : Microsoft.AspNetCore.Identity.IdentityUserLogin<TKey>
        , ITableEntity
        where TKey : IEquatable<TKey>
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; } = ETag.All;

        public virtual string Id { get; set; } = string.Empty;
    }
}
