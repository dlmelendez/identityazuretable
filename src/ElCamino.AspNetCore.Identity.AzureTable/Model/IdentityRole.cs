// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Data.Services.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.WindowsAzure.Storage.Table;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;

namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    public class IdentityRole : IdentityRole<string, IdentityUserRole>, IGenerateKeys
    {
        public IdentityRole() : base() { }

        /// <summary>
        /// Generates Row and Id keys.
        /// Partition key is equal to the UserId
        /// </summary>
        public void GenerateKeys()
        {
            RowKey = PeekRowKey();
            PartitionKey = KeyHelper.GeneratePartitionKeyIdentityRole(Name);
            KeyVersion = KeyHelper.KeyVersion;
        }

        /// <summary>
        /// Generates the RowKey without setting it on the object.
        /// </summary>
        /// <returns></returns>
        public string PeekRowKey()
        {
            return KeyHelper.GenerateRowKeyIdentityRole(Name);
        }

        public double KeyVersion { get; set; }

        public IdentityRole(string roleName)
            : this()
        {
            base.Name = roleName;
        }

        [Microsoft.WindowsAzure.Storage.Table.IgnoreProperty]
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

    public class IdentityRole<TKey, TUserRole> : TableEntity
        where TUserRole : IdentityUserRole<TKey>
    {

        public IdentityRole() : base()
        {
            this.Users = new List<TUserRole>();
        }

        [Microsoft.WindowsAzure.Storage.Table.IgnoreProperty]
        public virtual TKey Id { get; set; }

        public string Name { get; set; }

        public string NormalizedName { get; set; }

        [Microsoft.WindowsAzure.Storage.Table.IgnoreProperty]
        public ICollection<TUserRole> Users { get; private set; }
    }
}
