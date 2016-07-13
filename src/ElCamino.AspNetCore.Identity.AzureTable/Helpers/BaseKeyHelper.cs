// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
#if net45
using Microsoft.AspNet.Identity;
#else
using Microsoft.AspNetCore.Identity;
#endif

#if net45
namespace ElCamino.AspNet.Identity.AzureTable.Helpers
#else
namespace ElCamino.AspNetCore.Identity.AzureTable.Helpers
#endif
{
    public abstract class BaseKeyHelper
    {
        public abstract string GenerateRowKeyUserLoginInfo(UserLoginInfo info);

		public abstract string GenerateRowKeyUserLoginInfo(string plainLoginProvider, string plainProviderKey);

		public abstract string GeneratePartitionKeyIndexByLogin(string plainProvider);

        public abstract string GenerateRowKeyUserEmail(string plainEmail);

        public abstract string GeneratePartitionKeyIndexByEmail(string plainEmail);

        public abstract string GenerateRowKeyUserName(string plainUserName);

        public abstract string GenerateRowKeyIdentityUserRole(string plainRoleName);

        public abstract string GenerateRowKeyIdentityRole(string plainRoleName);

        public abstract string GeneratePartitionKeyIdentityRole(string plainRoleName);

        public abstract string GenerateRowKeyIdentityUserClaim(string claimType, string claimValue);

        public abstract string GenerateRowKeyIdentityUserLogin(string loginProvider, string providerKey);

        public abstract string ParsePartitionKeyIdentityRoleFromRowKey(string rowKey);

        public abstract double KeyVersion { get; }
    }
}
