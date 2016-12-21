// MIT License Copyright 2016 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using ElCamino.AspNetCore.Identity.AzureTable.Helpers;

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
            return KeyHelper.GenerateRowKeyIdentityUserToken(LoginProvider, TokenName);
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

    public class IdentityUserToken<TKey> : TableEntity
    {
        /// <summary>
        /// Gets or sets the primary key of the user that the token belongs to.
        /// </summary>
        public virtual TKey UserId { get; set; }

        /// <summary>
        /// Gets or sets the LoginProvider this token is from.
        /// </summary>
        public virtual string LoginProvider { get; set; }

        /// <summary>
        /// Gets or sets the name of the token.
        /// </summary>
        public virtual string TokenName { get; set; }

        /// <summary>
        /// Gets or sets the token value.
        /// </summary>
        public virtual string TokenValue { get; set; }

    }

}
