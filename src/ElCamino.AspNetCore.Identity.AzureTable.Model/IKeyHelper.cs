// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;

namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    /// <summary>
    /// Manages all table storage keys
    /// </summary>
    public interface IKeyHelper
    {
        /// <summary>
        /// Key prefix for IdentityUserClaim
        /// </summary>
        string PreFixIdentityUserClaim { get; }

        /// <summary>
        /// Key prefix for IdentityUserClaimUpperBound
        /// </summary>
        string PreFixIdentityUserClaimUpperBound { get; }

        /// <summary>
        /// Key prefix for IdentityUserRole
        /// </summary>
        string PreFixIdentityUserRole { get; }

        /// <summary>
        /// Key prefix for IdentityUserRoleUpperBound
        /// </summary>
        string PreFixIdentityUserRoleUpperBound { get; }

        /// <summary>
        /// Key prefix for IdentityUserLogin
        /// </summary>
        string PreFixIdentityUserLogin { get; }

        /// <summary>
        /// Key prefix for IdentityUserLoginUpperBound
        /// </summary>
        string PreFixIdentityUserLoginUpperBound { get; }

        /// <summary>
        /// Key prefix for IdentityUserEmail
        /// </summary>
        string PreFixIdentityUserEmail { get; }

        /// <summary>
        /// Key prefix for IdentityUserToken
        /// </summary>
        string PreFixIdentityUserToken { get; }

        /// <summary>
        /// Key prefix for IdentityUserId
        /// </summary>
        string PreFixIdentityUserId { get; }

        /// <summary>
        /// Key prefix for IdentityUserIdUpperBound
        /// </summary>
        string PreFixIdentityUserIdUpperBound { get; }

        /// <summary>
        /// Key prefix for IdentityUserName
        /// </summary>
        string PreFixIdentityUserName { get; }

        /// <summary>
        /// Key Formatter for IdentityUserClaim
        /// </summary>
        string FormatterIdentityUserClaim { get; }

        /// <summary>
        /// Key Formatter for IdentityUserRole
        /// </summary>
        string FormatterIdentityUserRole { get; }

        /// <summary>
        /// Key Formatter for IdentityUserLogin
        /// </summary>
        string FormatterIdentityUserLogin { get; }

        /// <summary>
        /// Key Formatter for IdentityUserEmail
        /// </summary>
        string FormatterIdentityUserEmail { get; }

        /// <summary>
        /// Key Formatter for IdentityUserToken
        /// </summary>
        string FormatterIdentityUserToken { get; }

        /// <summary>
        /// Key Formatter for IdentityUserId
        /// </summary>
        string FormatterIdentityUserId { get; }

        /// <summary>
        /// Key Formatter for IdentityUserName
        /// </summary>
        string FormatterIdentityUserName { get; }

        /// <summary>
        /// Key prefix for IdentityRole
        /// </summary>
        string PreFixIdentityRole { get; }

        /// <summary>
        /// Key prefix for IdentityRoleUpperBound
        /// </summary>
        string PreFixIdentityRoleUpperBound { get; }

        /// <summary>
        /// Key prefix for IdentityRoleClaim
        /// </summary>
        string PreFixIdentityRoleClaim { get; }

        /// <summary>
        /// Key Formatter for IdentityRole
        /// </summary>
        string FormatterIdentityRole { get; }

        /// <summary>
        /// Key Formatter for IdentityRoleClaim
        /// </summary>
        string FormatterIdentityRoleClaim { get; }

        /// <summary>
        /// Generate key for PartitionKeyIndexByLogin
        /// </summary>
        /// <param name="plainLoginProvider"></param>
        /// <param name="plainProviderKey"></param>
        /// <returns></returns>
        ReadOnlySpan<char> GeneratePartitionKeyIndexByLogin(string plainLoginProvider, string plainProviderKey);

        /// <summary>
        /// Generate key for RowKeyUserEmail
        /// </summary>
        /// <param name="plainEmail"></param>
        /// <returns></returns>
        ReadOnlySpan<char> GenerateRowKeyUserEmail(string? plainEmail);

        /// <summary>
        /// Generate key for UserId
        /// </summary>
        /// <returns></returns>
        string GenerateUserId();

        /// <summary>
        /// Generate key for RowKeyUserName
        /// </summary>
        /// <param name="plainUserName"></param>
        /// <returns></returns>
        string GenerateRowKeyUserName(string? plainUserName);

        /// <summary>
        /// Generate key for PartitionKeyUserName
        /// </summary>
        /// <param name="plainUserName"></param>
        /// <returns></returns>
        string GeneratePartitionKeyUserName(string? plainUserName);

        /// <summary>
        /// Generate key for RowKeyUserId
        /// </summary>
        /// <param name="plainUserId"></param>
        /// <returns></returns>
        string GenerateRowKeyUserId(string? plainUserId);

        /// <summary>
        /// Generate key for RowKeyIdentityUserRole
        /// </summary>
        /// <param name="plainRoleName"></param>
        /// <returns></returns>
        string GenerateRowKeyIdentityUserRole(string? plainRoleName);

        /// <summary>
        /// Generate key for RowKeyIdentityRole
        /// </summary>
        /// <param name="plainRoleName"></param>
        /// <returns></returns>
        string GenerateRowKeyIdentityRole(string? plainRoleName);

        /// <summary>
        /// Generate key for PartitionKeyIdentityRole
        /// </summary>
        /// <param name="plainRoleName"></param>
        /// <returns></returns>
        string GeneratePartitionKeyIdentityRole(string? plainRoleName);

        /// <summary>
        /// Generate key for RowKeyIdentityUserClaim
        /// </summary>
        /// <param name="claimType"></param>
        /// <param name="claimValue"></param>
        /// <returns></returns>
        string GenerateRowKeyIdentityUserClaim(string? claimType, string? claimValue);

        /// <summary>
        /// Generate key for RowKeyIdentityRoleClaim
        /// </summary>
        /// <param name="claimType"></param>
        /// <param name="claimValue"></param>
        /// <returns></returns>
        string GenerateRowKeyIdentityRoleClaim(string? claimType, string? claimValue);

        /// <summary>
        /// Generate key for RowKeyIdentityUserToken
        /// </summary>
        /// <param name="loginProvider"></param>
        /// <param name="tokenName"></param>
        /// <returns></returns>
        string GenerateRowKeyIdentityUserToken(string? loginProvider, string? tokenName);

        /// <summary>
        /// Generate key for RowKeyIdentityUserLogin
        /// </summary>
        /// <param name="loginProvider"></param>
        /// <param name="providerKey"></param>
        /// <returns></returns>
        string GenerateRowKeyIdentityUserLogin(string? loginProvider, string? providerKey);

        /// <summary>
        /// Parse PartitionKey From RowKey for IdentityRole
        /// </summary>
        /// <param name="rowKey"></param>
        /// <returns></returns>
        string ParsePartitionKeyIdentityRoleFromRowKey(string rowKey);

        /// <summary>
        /// Key Version
        /// </summary>
        double KeyVersion { get; }
    }
}
