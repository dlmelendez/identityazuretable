// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using Microsoft.WindowsAzure.Storage;

namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    public class IdentityUserToken : IdentityUserToken<string>, IGenerateKeys
    {
        public IdentityUserToken() { }


        /// <summary>
        /// Generates Row and Id keys.
        /// Partition key is equal to the UserId
        /// </summary>
        public void GenerateKeys()
        {
            RowKey = PeekRowKey();
            KeyVersion = KeyHelper.KeyVersion;
        }

        public double KeyVersion { get; set; }

        /// <summary>
        /// Generates the RowKey without setting it on the object.
        /// </summary>
        /// <returns></returns>
        public string PeekRowKey()
        {
            return KeyHelper.GenerateRowKeyIdentityUserToken(LoginProvider, Name);
        }

        [Microsoft.WindowsAzure.Storage.Table.IgnoreProperty]
        public override string UserId
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

    public class IdentityUserToken<TKey> : Microsoft.AspNetCore.Identity.IdentityUserToken<TKey>
        , ITableEntity
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

        //TODO: Figure out migration path for Name and Value properties
        /// <summary>
        /// Gets or sets the name of the token.
        /// </summary>
        //public virtual string TokenName { get; set; }

        /// <summary>
        /// Gets or sets the token value.
        /// </summary>
        //public virtual string TokenValue { get; set; }

    }

}
