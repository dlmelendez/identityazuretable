﻿// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

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
    public class HashKeyHelper : UriEncodeKeyHelper
    {
        public override string GeneratePartitionKeyIndexByLogin(string plainLoginProvider, string plainProviderKey)
        {
            string hash = ConvertKeyToHash(base.GenerateRowKeyIdentityUserLogin(plainLoginProvider, plainProviderKey));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserLogin, hash);
        }

        public override string GenerateRowKeyUserEmail(string plainEmail)
        {
            string hash = ConvertKeyToHash(base.GenerateRowKeyUserEmail(plainEmail));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserEmail, hash);

        }

        public override string GenerateRowKeyUserName(string plainUserName)
        {
            string hash = ConvertKeyToHash(base.GenerateRowKeyUserName(plainUserName));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserName, hash);
        }

        public override string GenerateRowKeyIdentityUserRole(string plainRoleName)
        {
            string hash = ConvertKeyToHash(base.GenerateRowKeyIdentityUserRole(plainRoleName));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserRole, hash);

        }

        public override string GenerateRowKeyIdentityRole(string plainRoleName)
        {
            string hash = ConvertKeyToHash(base.GenerateRowKeyIdentityRole(plainRoleName));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityRole, hash);

        }

        public override string GeneratePartitionKeyIdentityRole(string plainRoleName)
        {
            return ParsePartitionKeyIdentityRoleFromRowKey(GenerateRowKeyIdentityRole(plainRoleName));
        }

        public override string GenerateRowKeyIdentityUserClaim(string claimType, string claimValue)
        {
            string hash = ConvertKeyToHash(base.GenerateRowKeyIdentityUserClaim(claimType, claimValue));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserClaim, hash);
        }

        public override string GenerateRowKeyIdentityRoleClaim(string claimType, string claimValue)
        {
            string hash = ConvertKeyToHash(base.GenerateRowKeyIdentityRoleClaim(claimType, claimValue));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityRoleClaim, hash);
        }

        public override string GenerateRowKeyIdentityUserToken(string loginProvider, string name)
        {
            string hash = ConvertKeyToHash(base.GenerateRowKeyIdentityUserToken(loginProvider, name));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserToken, hash);
        }

        public override string ParsePartitionKeyIdentityRoleFromRowKey(string rowKey)
        {
            return base.ParsePartitionKeyIdentityRoleFromRowKey(rowKey);
        }

        public override string GenerateRowKeyIdentityUserLogin(string loginProvider, string providerKey)
        {
            string hash = ConvertKeyToHash(base.GenerateRowKeyIdentityUserLogin(loginProvider, providerKey));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserLogin, hash);
        }

        public override double KeyVersion => 1.67;

        public static string ConvertKeyToHash(string input)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return GetHash(sha, input);
            }
        }

        private static string GetHash(SHA256 shaHash, string input)
        {
            // Convert the input string to a byte array and compute the hash. 
            byte[] data = shaHash.ComputeHash(Encoding.Unicode.GetBytes(input));
            Debug.WriteLine(string.Format("Key Size before hash: {0} bytes", Encoding.Unicode.GetBytes(input).Length));

            // Create a new StringBuilder to collect the bytes 
            // and create a string.
            StringBuilder sBuilder = new StringBuilder(32);

            // Loop through each byte of the hashed data  
            // and format each one as a hexadecimal string. 
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("X2"));
            }
            Debug.WriteLine(string.Format("Key Size after hash: {0} bytes", data.Length));

            // Return the hexadecimal string. 
            return sBuilder.ToString();
        }
    }
}
