// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    public interface IKeyHelper
    {
        string PreFixIdentityUserClaim { get; }
        string PreFixIdentityUserClaimUpperBound { get; }
        string PreFixIdentityUserRole { get; }
        string PreFixIdentityUserRoleUpperBound { get; }
        string PreFixIdentityUserLogin { get; }
        string PreFixIdentityUserLoginUpperBound { get; }
        string PreFixIdentityUserEmail { get; }
        string PreFixIdentityUserToken { get; }
        string PreFixIdentityUserId { get; }
        string PreFixIdentityUserIdUpperBound { get; }
        string PreFixIdentityUserName { get; }

        string FormatterIdentityUserClaim { get; }
        string FormatterIdentityUserRole { get; }
        string FormatterIdentityUserLogin { get; }
        string FormatterIdentityUserEmail { get; }
        string FormatterIdentityUserToken { get; }
        string FormatterIdentityUserId { get; }
        string FormatterIdentityUserName { get; }

        string PreFixIdentityRole { get; }
        string PreFixIdentityRoleUpperBound { get; }
        string PreFixIdentityRoleClaim { get; }
        string FormatterIdentityRole { get; }
        string FormatterIdentityRoleClaim { get; }

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
