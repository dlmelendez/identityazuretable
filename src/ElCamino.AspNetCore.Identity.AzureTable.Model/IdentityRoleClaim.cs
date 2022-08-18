// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.


using System;
using System.Runtime.Serialization;
using Azure;
using Azure.Data.Tables;


namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    public class IdentityRoleClaim : IdentityRoleClaim<string>, IGenerateKeys
    {
        public IdentityRoleClaim() { }

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
            return keyHelper.GenerateRowKeyIdentityRoleClaim(ClaimType, ClaimValue);
        }

        public double KeyVersion { get; set; }

        [IgnoreDataMember]
        public override string RoleId
        {
            get
            {
                return PartitionKey;
            }
            set
            {
                PartitionKey = value;
            }
        }
    }

    public class IdentityRoleClaim<TKey> : Microsoft.AspNetCore.Identity.IdentityRoleClaim<TKey>, ITableEntity
        where TKey : IEquatable<TKey>
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; } = ETag.All;

    }
}
