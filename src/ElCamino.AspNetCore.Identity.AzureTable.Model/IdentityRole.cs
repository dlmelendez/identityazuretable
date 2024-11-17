// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;
using Azure;
using Azure.Data.Tables;

namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    /// <inheritdoc/>
    public class IdentityRole : IdentityRole<string, IdentityUserRole>, IGenerateKeys
    {
        /// <inheritdoc/>
        public IdentityRole() : base() { }

        /// <summary>
        /// Generates Row and Id keys.
        /// Partition key is equal to the UserId
        /// </summary>
        public void GenerateKeys(IKeyHelper keyHelper)
        {
            RowKey = PeekRowKey(keyHelper);
            PartitionKey = keyHelper.GeneratePartitionKeyIdentityRole(Name).ToString();
            KeyVersion = keyHelper.KeyVersion;
        }

        /// <summary>
        /// Generates the RowKey without setting it on the object.
        /// </summary>
        /// <returns></returns>
        public string PeekRowKey(IKeyHelper keyHelper)
        {
            return keyHelper.GenerateRowKeyIdentityRole(Name).ToString();
        }

        /// <inheritdoc/>
        public double KeyVersion { get; set; }

        /// <inheritdoc/>
        public IdentityRole(string roleName)
            : this()
        {
            base.Name = roleName;
        }

        /// <inheritdoc/>
        [IgnoreDataMember]
        public override string Id
        {
            get
            {
                return RowKey;
            }
            set
            {
                RowKey = value;
            }
        }
    }

    /// <inheritdoc/>
    public class IdentityRole<TKey, TUserRole> : Microsoft.AspNetCore.Identity.IdentityRole<TKey>, ITableEntity
        where TKey : IEquatable<TKey>
        where TUserRole : IdentityUserRole<TKey>
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
        public IdentityRole() : base()
        {
        }

        /// <inheritdoc/>
        [IgnoreDataMember]
        public override TKey Id 
        {
            get { return base.Id; }
            set { base.Id = value; }
        }



    }
}
