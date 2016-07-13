// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if net45
using ElCamino.AspNet.Identity.AzureTable.Helpers;

namespace ElCamino.AspNet.Identity.AzureTable.Model
#else
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;

namespace ElCamino.AspNetCore.Identity.AzureTable.Model
#endif
{
public class IdentityUserLogin : IdentityUserLogin<string>, IGenerateKeys
    {
        public IdentityUserLogin() { }


        /// <summary>
        /// Generates Row and Id keys.
        /// Partition key is equal to the UserId
        /// </summary>
        public void GenerateKeys()
        {
            Id = Guid.NewGuid().ToString();
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
            return KeyHelper.GenerateRowKeyIdentityUserLogin(LoginProvider, ProviderKey);
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

    public class IdentityUserLogin<TKey> : TableEntity
    {
        public virtual string LoginProvider { get; set; }

        public virtual string ProviderKey { get; set; }

		public virtual string ProviderDisplayName { get; set; }

		public virtual TKey UserId { get; set; }

        public virtual string Id { get; set; }

    }

}
