// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace ElCamino.AspNetCore.Identity.AzureTable.Helpers
{
    public static class KeyHelper
    {
        private static BaseKeyHelper hashHelper = new HashKeyHelper();

        public static string GeneratePartitionKeyIndexByLogin(string plainLoginProvider, string plainProviderKey)
        {
            return hashHelper.GeneratePartitionKeyIndexByLogin(plainLoginProvider, plainProviderKey);
        }

        public static string GenerateRowKeyUserEmail(string plainEmail)
        {
            return hashHelper.GenerateRowKeyUserEmail(plainEmail);
        }

        public static string GenerateUserId()
        {
            return hashHelper.GenerateUserId();
        }

        public static string GenerateRowKeyUserId(string plainUserId)
        {
            return hashHelper.GenerateRowKeyUserId(plainUserId);
        }

        public static string GenerateRowKeyUserName(string plainUserName)
        {
            return hashHelper.GeneratePartitionKeyUserName(plainUserName);
        }

        public static string GenerateRowKeyIdentityUserRole(string plainRoleName)
        {
            return hashHelper.GenerateRowKeyIdentityUserRole(plainRoleName);
        }

        public static string GenerateRowKeyIdentityRole(string plainRoleName)
        {
            return hashHelper.GenerateRowKeyIdentityRole(plainRoleName);
        }

        public static string GeneratePartitionKeyIdentityRole(string plainRoleName)
        {
            return hashHelper.GeneratePartitionKeyIdentityRole(plainRoleName);
        }

        public static string GenerateRowKeyIdentityUserClaim(string claimType, string claimValue)
        {
            return hashHelper.GenerateRowKeyIdentityUserClaim(claimType, claimValue);
        }

        public static string GenerateRowKeyIdentityRoleClaim(string claimType, string claimValue)
        {
            return hashHelper.GenerateRowKeyIdentityRoleClaim(claimType, claimValue);
        }

        public static string GenerateRowKeyIdentityUserToken(string loginProvider, string name)
        {
            return hashHelper.GenerateRowKeyIdentityUserToken(loginProvider, name);
        }
        public static string GenerateRowKeyIdentityUserClaim_Pre1_7(string claimType, string claimValue)
        {
            return hashHelper.GenerateRowKeyIdentityUserClaim(claimType, claimValue);
        }

        public static string GenerateRowKeyIdentityUserLogin(string loginProvider, string providerKey)
        {
            return hashHelper.GenerateRowKeyIdentityUserLogin(loginProvider, providerKey);
        }

        public static string ParsePartitionKeyIdentityRoleFromRowKey(string rowKey)
        {
            return hashHelper.ParsePartitionKeyIdentityRoleFromRowKey(rowKey);
        }

        public static double KeyVersion { get { return hashHelper.KeyVersion; } }
    }
}
