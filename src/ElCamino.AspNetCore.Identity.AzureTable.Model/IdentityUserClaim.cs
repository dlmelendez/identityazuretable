// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using Azure;
using Azure.Data.Tables;

namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    /// <inheritdoc/>
    public class IdentityUserClaim : IdentityUserClaim<string>, IGenerateKeys
    {
        /// <inheritdoc/>
        public IdentityUserClaim() { }

        /// <summary>
        /// Generates Row and Id keys.
        /// Partition key is equal to the UserId
        /// </summary>
        public void GenerateKeys(IKeyHelper keyHelper)
        {
            RowKey = PeekRowKey(keyHelper);
            KeyVersion = keyHelper.KeyVersion;
        }

        /// <summary>
        /// Generates the RowKey without setting it on the object.
        /// </summary>
        /// <returns></returns>
        public string PeekRowKey(IKeyHelper keyHelper)
        {
            return keyHelper.GenerateRowKeyIdentityUserClaim(ClaimType, ClaimValue).ToString();
        }

        /// <inheritdoc/>
        public double KeyVersion { get; set; }

    }

    /// <inheritdoc/>
    public class IdentityUserClaim<TKey> : Microsoft.AspNetCore.Identity.IdentityUserClaim<TKey>,
        ITableEntity
        where TKey : IEquatable<TKey>
    {
        /// <inheritdoc/>
        public string PartitionKey { get; set; } = string.Empty;

        /// <inheritdoc/>
        public string RowKey { get; set; } = string.Empty;

        /// <inheritdoc/>
        public DateTimeOffset? Timestamp { get; set; }

        /// <inheritdoc/>
        public ETag ETag { get; set; } = ETag.All;

    }
}
