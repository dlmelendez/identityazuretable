// MIT License Copyright 2016 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
#if !net45
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        [Microsoft.WindowsAzure.Storage.Table.IgnoreProperty]
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

    public class IdentityRoleClaim<TKey> : TableEntity
    {
        public virtual string ClaimType { get; set; }

        public virtual string ClaimValue { get; set; }

        public virtual TKey RoleId { get; set; }

    }

}
#endif