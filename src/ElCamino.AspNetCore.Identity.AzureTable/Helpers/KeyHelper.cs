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
        private static BaseKeyHelper baseHelper = new UriEncodeKeyHelper();
        private static BaseKeyHelper hashHelper = new HashKeyHelper();

        public static string GeneratePartitionKeyIndexByLogin(string plainLoginProvider, string plainProviderKey)
        {
            return baseHelper.GeneratePartitionKeyIndexByLogin(plainLoginProvider, plainProviderKey);
        }

        public static string GenerateRowKeyUserEmail(string plainEmail)
        {
            return baseHelper.GenerateRowKeyUserEmail(plainEmail);
        }


        public static string GenerateRowKeyUserName(string plainUserName)
        {
            return baseHelper.GenerateRowKeyUserName(plainUserName);
        }

        public static string GenerateRowKeyIdentityUserRole(string plainRoleName)
        {
            return baseHelper.GenerateRowKeyIdentityUserRole(plainRoleName);
        }

        public static string GenerateRowKeyIdentityRole(string plainRoleName)
        {
            return baseHelper.GenerateRowKeyIdentityRole(plainRoleName);
        }

        public static string GeneratePartitionKeyIdentityRole(string plainRoleName)
        {
            return baseHelper.GeneratePartitionKeyIdentityRole(plainRoleName);
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
            return baseHelper.GenerateRowKeyIdentityUserClaim(claimType, claimValue);
        }

        public static string GenerateRowKeyIdentityUserLogin(string loginProvider, string providerKey)
        {
            return baseHelper.GenerateRowKeyIdentityUserLogin(loginProvider, providerKey);
        }

        public static string ParsePartitionKeyIdentityRoleFromRowKey(string rowKey)
        {
            return baseHelper.ParsePartitionKeyIdentityRoleFromRowKey(rowKey);
        }

        public static double KeyVersion { get { return baseHelper.KeyVersion; } }
    }
}
