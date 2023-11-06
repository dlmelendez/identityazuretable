// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;
using Azure;
using Azure.Data.Tables;

namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    public class IdentityRole : IdentityRole<string, IdentityUserRole>, IGenerateKeys
    {
        public IdentityRole() : base() { }

        /// <summary>
        /// Generates Row and Id keys.
        /// Partition key is equal to the UserId
        /// </summary>
        public void GenerateKeys(IKeyHelper keyHelper)
        {
            RowKey = PeekRowKey(keyHelper);
            PartitionKey = keyHelper.GeneratePartitionKeyIdentityRole(Name);
            KeyVersion = keyHelper.KeyVersion;
        }

        /// <summary>
        /// Generates the RowKey without setting it on the object.
        /// </summary>
        /// <returns></returns>
        public string PeekRowKey(IKeyHelper keyHelper)
        {
            return keyHelper.GenerateRowKeyIdentityRole(Name);
        }

        public double KeyVersion { get; set; }

        public IdentityRole(string roleName)
            : this()
        {
            base.Name = roleName;
        }

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

    public class IdentityRole<TKey, TUserRole> : Microsoft.AspNetCore.Identity.IdentityRole<TKey>, ITableEntity
        where TKey : IEquatable<TKey>
        where TUserRole : IdentityUserRole<TKey>
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; } = ETag.All;

        public IdentityRole() : base()
        {
        }

        [IgnoreDataMember]
        public override TKey Id 
        {
            get { return base.Id; }
            set { base.Id = value; }
        }



    }
}
