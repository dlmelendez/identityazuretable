// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;
using Azure;
using Azure.Data.Tables;

namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    /// <inheritdoc/>
    public class IdentityUserRole : IdentityUserRole<string>, IGenerateKeys
    {
        /// <inheritdoc/>
        public IdentityUserRole() { }

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

        /// <summary>
        /// Generates the RowKey without setting it on the object.
        /// </summary>
        /// <returns></returns>
        public string PeekRowKey(IKeyHelper keyHelper)
        {
            return keyHelper.GenerateRowKeyIdentityUserRole(RoleName).ToString();
        }

        /// <inheritdoc/>
        public double KeyVersion { get; set; }

        /// <inheritdoc/>
        public string Id
        {
            get
            {
                return RoleId;
            }
            set
            {
                RoleId = value;
            }
        }

    }

    /// <inheritdoc/>
    public class IdentityUserRole<TKey> : Microsoft.AspNetCore.Identity.IdentityUserRole<TKey>
        , ITableEntity
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

        /// <inheritdoc/>
        [IgnoreDataMember]
        public override TKey RoleId { get => base.RoleId; set => base.RoleId = value; }

        /// <summary>
        /// Role Name
        /// </summary>
        public string? RoleName { get; set; }
    }
}
