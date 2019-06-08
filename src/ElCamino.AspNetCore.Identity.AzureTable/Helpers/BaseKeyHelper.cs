// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Identity;

namespace ElCamino.AspNetCore.Identity.AzureTable.Helpers
{
    public abstract class BaseKeyHelper
    {
        public abstract string GeneratePartitionKeyIndexByLogin(string plainLoginProvider, string plainProviderKey);

        public abstract string GenerateRowKeyUserEmail(string plainEmail);

        public abstract string GenerateUserId();

        public abstract string GenerateRowKeyUserName(string plainUserName);

        public abstract string GenerateRowKeyUserId(string plainUserId);

        public abstract string GenerateRowKeyIdentityUserRole(string plainRoleName);

        public abstract string GenerateRowKeyIdentityRole(string plainRoleName);

        public abstract string GeneratePartitionKeyIdentityRole(string plainRoleName);

        public abstract string GenerateRowKeyIdentityUserClaim(string claimType, string claimValue);

        public abstract string GenerateRowKeyIdentityRoleClaim(string claimType, string claimValue);

        public abstract string GenerateRowKeyIdentityUserToken(string loginProvider, string tokenName);

        public abstract string GenerateRowKeyIdentityUserLogin(string loginProvider, string providerKey);

        public abstract string ParsePartitionKeyIdentityRoleFromRowKey(string rowKey);

        public abstract double KeyVersion { get; }
    }
}
