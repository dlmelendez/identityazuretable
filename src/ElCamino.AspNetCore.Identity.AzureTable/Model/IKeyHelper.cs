// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Identity;

namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    public interface IKeyHelper
    {
        string GeneratePartitionKeyIndexByLogin(string plainLoginProvider, string plainProviderKey);

        string GenerateRowKeyUserEmail(string plainEmail);

        string GenerateUserId();

        string GenerateRowKeyUserName(string plainUserName);

        string GeneratePartitionKeyUserName(string plainUserName);

        string GenerateRowKeyUserId(string plainUserId);

        string GenerateRowKeyIdentityUserRole(string plainRoleName);

        string GenerateRowKeyIdentityRole(string plainRoleName);

        string GeneratePartitionKeyIdentityRole(string plainRoleName);

        string GenerateRowKeyIdentityUserClaim(string claimType, string claimValue);

        string GenerateRowKeyIdentityRoleClaim(string claimType, string claimValue);

        string GenerateRowKeyIdentityUserToken(string loginProvider, string tokenName);

        string GenerateRowKeyIdentityUserLogin(string loginProvider, string providerKey);

        string ParsePartitionKeyIdentityRoleFromRowKey(string rowKey);

        double KeyVersion { get; }
    }
}
