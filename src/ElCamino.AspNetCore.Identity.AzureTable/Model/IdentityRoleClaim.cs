// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;

namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    public class IdentityRoleClaim : IdentityRoleClaim<string>, IGenerateKeys
    {
        public IdentityRoleClaim() { }

        /// <summary>
        /// Generates Row and Id keys.
        /// Partition key is equal to the UserId
        /// </summary>
        public void GenerateKeys()
        {
            RowKey = PeekRowKey();
            KeyVersion = KeyHelper.KeyVersion;
        }

        /// <summary>
        /// Generates the RowKey without setting it on the object.
        /// </summary>
        /// <returns></returns>
        public string PeekRowKey()
        {
            return KeyHelper.GenerateRowKeyIdentityRoleClaim(ClaimType, ClaimValue);
        }

        public double KeyVersion { get; set; }

        [IgnoreProperty]
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
        public DateTimeOffset Timestamp { get; set; }
        public string ETag { get; set; }

        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            TableEntity.ReadUserObject(this, properties, operationContext);
        }

        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            return TableEntity.WriteUserObject(this, operationContext);
        }
    }
}
