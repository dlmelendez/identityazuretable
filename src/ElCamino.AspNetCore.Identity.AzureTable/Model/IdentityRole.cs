// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
#if net45
using Microsoft.AspNet.Identity;
using ElCamino.AspNet.Identity.AzureTable.Helpers;
#else
using Microsoft.AspNetCore.Identity;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
#endif
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Data.Services.Common;

#if net45
namespace ElCamino.AspNet.Identity.AzureTable.Model
#else
namespace ElCamino.AspNetCore.Identity.AzureTable.Model
#endif
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
#if net45
		 ,IRole<TKey>
#endif
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
