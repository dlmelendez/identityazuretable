// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    public class IdentityUserToken : IdentityUserToken<string>, IGenerateKeys
    {
        public IdentityUserToken() { }


        /// <summary>
        /// Generates Row and Id keys.
        /// Partition key is equal to the UserId
        /// </summary>
        public void GenerateKeys(IKeyHelper keyHelper)
        {
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
            return keyHelper.GenerateRowKeyIdentityUserToken(LoginProvider, Name);
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

        public virtual void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            TableEntity.ReadUserObject(this, properties, operationContext);
        }

        public virtual IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            return TableEntity.WriteUserObject(this, operationContext);
        }

        [IgnoreProperty]
        public override string Name { get => base.Name; set => base.Name = value; }

        [IgnoreProperty]
        public override string Value { get => base.Value; set => base.Value = value; }

        //These properties are more descriptive fields in storage, also allows for backcompat
        /// <summary>
        /// Gets or sets the name of the token.
        /// </summary>
        public virtual string TokenName { get => base.Name; set => base.Name = value; }

        /// <summary>
        /// Gets or sets the token value.
        /// </summary>
        public virtual string TokenValue { get => base.Value; set => base.Value = value; }

    }

}
