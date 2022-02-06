// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; } = ETag.All;

        [IgnoreDataMember]
        public override string Name { get => base.Name; set => base.Name = value; }

        [IgnoreDataMember]
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
