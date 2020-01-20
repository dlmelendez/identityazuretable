// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Microsoft.AspNetCore.Identity;

namespace ElCamino.AspNetCore.Identity.AzureTable.Helpers
{
    public class DefaultKeyHelper : IKeyHelper
    {
        #region RowConstants
        public string PreFixIdentityUserClaim => Constants.RowKeyConstants.PreFixIdentityUserClaim;

        public string PreFixIdentityUserClaimUpperBound => Constants.RowKeyConstants.PreFixIdentityUserClaimUpperBound;

        public string PreFixIdentityUserRole => Constants.RowKeyConstants.PreFixIdentityUserRole;

        public string PreFixIdentityUserRoleUpperBound => Constants.RowKeyConstants.PreFixIdentityUserRoleUpperBound;

        public string PreFixIdentityUserLogin => Constants.RowKeyConstants.PreFixIdentityUserLogin;

        public string PreFixIdentityUserLoginUpperBound => Constants.RowKeyConstants.PreFixIdentityUserLoginUpperBound;

        public string PreFixIdentityUserEmail => Constants.RowKeyConstants.PreFixIdentityUserEmail;

        public string PreFixIdentityUserToken => Constants.RowKeyConstants.PreFixIdentityUserToken;

        public string PreFixIdentityUserId => Constants.RowKeyConstants.PreFixIdentityUserId;

        public string PreFixIdentityUserIdUpperBound => Constants.RowKeyConstants.PreFixIdentityUserIdUpperBound;

        public string PreFixIdentityUserName => Constants.RowKeyConstants.PreFixIdentityUserName;

        public string FormatterIdentityUserClaim => Constants.RowKeyConstants.FormatterIdentityUserClaim;

        public string FormatterIdentityUserRole => Constants.RowKeyConstants.FormatterIdentityUserRole;

        public string FormatterIdentityUserLogin => Constants.RowKeyConstants.FormatterIdentityUserLogin;

        public string FormatterIdentityUserEmail => Constants.RowKeyConstants.FormatterIdentityUserEmail;

        public string FormatterIdentityUserToken => Constants.RowKeyConstants.FormatterIdentityUserToken;

        public string FormatterIdentityUserId => Constants.RowKeyConstants.FormatterIdentityUserId;

        public string FormatterIdentityUserName => Constants.RowKeyConstants.FormatterIdentityUserName;

        public string PreFixIdentityRole => Constants.RowKeyConstants.PreFixIdentityRole;

        public string PreFixIdentityRoleClaim => Constants.RowKeyConstants.PreFixIdentityRoleClaim;

        public string FormatterIdentityRole => Constants.RowKeyConstants.FormatterIdentityRole;

        public string FormatterIdentityRoleClaim => Constants.RowKeyConstants.FormatterIdentityRoleClaim;

        #endregion

        public string GeneratePartitionKeyIndexByLogin(string plainLoginProvider, string plainProviderKey)
        {
            string strTemp = string.Format("{0}_{1}",plainLoginProvider?.ToUpper(),plainProviderKey?.ToUpper());
            string hash = ConvertKeyToHash(strTemp);
            return string.Format(FormatterIdentityUserLogin, hash);
        }

        public string GenerateRowKeyUserEmail(string plainEmail)
        {
            string hash = ConvertKeyToHash(plainEmail?.ToUpper());
            return string.Format(FormatterIdentityUserEmail, hash);
        }

        public string GenerateUserId()
        {
            return Guid.NewGuid().ToString("N");
        }

        public string GenerateRowKeyUserId(string plainUserId)
        {
            string hash = ConvertKeyToHash(plainUserId?.ToUpper());
            return string.Format(FormatterIdentityUserId, hash);
        }

        public string GenerateRowKeyUserName(string plainUserName)
        {
            return GeneratePartitionKeyUserName(plainUserName);
        }

        public string GeneratePartitionKeyUserName(string plainUserName)
        {
            string hash = ConvertKeyToHash(plainUserName?.ToUpper());
            return string.Format(FormatterIdentityUserName, hash);
        }

        public string GenerateRowKeyIdentityUserRole(string plainRoleName)
        {
            string hash = ConvertKeyToHash(plainRoleName?.ToUpper());
            return string.Format(FormatterIdentityUserRole, hash);
        }

        public string GenerateRowKeyIdentityRole(string plainRoleName)
        {
            string hash = ConvertKeyToHash(plainRoleName?.ToUpper());
            return string.Format(FormatterIdentityRole, hash);
        }

        public string GeneratePartitionKeyIdentityRole(string plainRoleName)
        {
            string hash = ConvertKeyToHash(plainRoleName?.ToUpper());
            return hash.Substring(0, 1);
        }

        public string GenerateRowKeyIdentityUserClaim(string claimType, string claimValue)
        {
            string strTemp = string.Format("{0}_{1}", claimType?.ToUpper(), claimValue?.ToUpper());
            string hash = ConvertKeyToHash(strTemp);
            return string.Format(FormatterIdentityUserClaim, hash);
        }

        public string GenerateRowKeyIdentityRoleClaim(string claimType, string claimValue)
        {
            string strTemp = string.Format("{0}_{1}", claimType?.ToUpper(), claimValue?.ToUpper());
            string hash = ConvertKeyToHash(strTemp);
            return string.Format(FormatterIdentityRoleClaim, hash);
        }

        public string GenerateRowKeyIdentityUserToken(string loginProvider, string name)
        {
            string strTemp = string.Format("{0}_{1}", loginProvider?.ToUpper(), name?.ToUpper());
            string hash = ConvertKeyToHash(strTemp);
            return string.Format(FormatterIdentityUserToken, hash);
        }

        public string ParsePartitionKeyIdentityRoleFromRowKey(string rowKey)
        {
            return rowKey.Substring(PreFixIdentityRole.Length, 1);
        }

        public string GenerateRowKeyIdentityUserLogin(string loginProvider, string providerKey)
        {
            string strTemp = string.Format("{0}_{1}", loginProvider?.ToUpper(), providerKey?.ToUpper());
            string hash = ConvertKeyToHash(strTemp);
            return string.Format(FormatterIdentityUserLogin, hash);
        }

        public double KeyVersion => 3.1;

        public static string ConvertKeyToHash(string input)
        {
            if (input != null)
            {
                using (SHA1 sha = SHA1.Create())
                {
                    return GetHash(sha, input);
                }
            }
            return null;
        }

        private static string GetHash(SHA1 shaHash, string input)
        {
            // Convert the input string to a byte array and compute the hash. 
            byte[] data = shaHash.ComputeHash(Encoding.Unicode.GetBytes(input));
            Debug.WriteLine(string.Format("Key Size before hash: {0} bytes", Encoding.UTF8.GetBytes(input).Length));

            // Create a new StringBuilder to collect the bytes 
            // and create a string.
            StringBuilder sBuilder = new StringBuilder(40);

            // Loop through each byte of the hashed data  
            // and format each one as a hexadecimal string. 
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            Debug.WriteLine(string.Format("Key Size after hash: {0} bytes", data.Length));

            // Return the hexadecimal string. 
            return sBuilder.ToString();
        }
    }
}
