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
public class IdentityUserRole : IdentityUserRole<string>, IGenerateKeys
    {
        public IdentityUserRole() { }

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

        /// <summary>
        /// Generates the RowKey without setting it on the object.
        /// </summary>
        /// <returns></returns>
        public string PeekRowKey()
        {
            return KeyHelper.GenerateRowKeyIdentityUserRole(RoleName);
        }

        public double KeyVersion { get; set; }

        public string Id
        {
            get
            {
                return RoleId;
            }
            set
            {
                RoleId = value;
            }
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


    public class IdentityUserRole<TKey> : TableEntity
    {

        [Microsoft.WindowsAzure.Storage.Table.IgnoreProperty]
        public virtual TKey RoleId { get; set; }

        public virtual TKey UserId { get; set; }

        public string RoleName { get; set; }

    }

}
