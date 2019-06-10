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
    public class HashKeyHelper : BaseKeyHelper
    {
        public override string GeneratePartitionKeyIndexByLogin(string plainLoginProvider, string plainProviderKey)
        {
            string strTemp = string.Format("{0}_{1}",plainLoginProvider?.ToUpper(),plainProviderKey?.ToUpper());
            string hash = ConvertKeyToHash(strTemp);
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserLogin, hash);
        }

        public override string GenerateRowKeyUserEmail(string plainEmail)
        {
            string hash = ConvertKeyToHash(plainEmail?.ToUpper());
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserEmail, hash);
        }

        public override string GenerateUserId()
        {
            return Guid.NewGuid().ToString("N");
        }

        public override string GenerateRowKeyUserId(string plainUserId)
        {
            string hash = ConvertKeyToHash(plainUserId?.ToUpper());
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserId, hash);
        }

        public override string GeneratePartitionKeyUserName(string plainUserName)
        {
            string hash = ConvertKeyToHash(plainUserName?.ToUpper());
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserName, hash);
        }

        public override string GenerateRowKeyIdentityUserRole(string plainRoleName)
        {
            string hash = ConvertKeyToHash(plainRoleName?.ToUpper());
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserRole, hash);
        }

        public override string GenerateRowKeyIdentityRole(string plainRoleName)
        {
            string hash = ConvertKeyToHash(plainRoleName?.ToUpper());
            return string.Format(Constants.RowKeyConstants.FormatterIdentityRole, hash);
        }

        public override string GeneratePartitionKeyIdentityRole(string plainRoleName)
        {
            string hash = ConvertKeyToHash(plainRoleName?.ToUpper());
            return hash.Substring(0, 1);
        }

        public override string GenerateRowKeyIdentityUserClaim(string claimType, string claimValue)
        {
            string strTemp = string.Format("{0}_{1}", claimType?.ToUpper(), claimValue?.ToUpper());
            string hash = ConvertKeyToHash(strTemp);
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserClaim, hash);
        }

        public override string GenerateRowKeyIdentityRoleClaim(string claimType, string claimValue)
        {
            string strTemp = string.Format("{0}_{1}", claimType?.ToUpper(), claimValue?.ToUpper());
            string hash = ConvertKeyToHash(strTemp);
            return string.Format(Constants.RowKeyConstants.FormatterIdentityRoleClaim, hash);
        }

        public override string GenerateRowKeyIdentityUserToken(string loginProvider, string name)
        {
            string strTemp = string.Format("{0}_{1}", loginProvider?.ToUpper(), name?.ToUpper());
            string hash = ConvertKeyToHash(strTemp);
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserToken, hash);
        }

        public override string ParsePartitionKeyIdentityRoleFromRowKey(string rowKey)
        {
            return rowKey.Substring(Constants.RowKeyConstants.PreFixIdentityRole.Length, 1);
        }

        public override string GenerateRowKeyIdentityUserLogin(string loginProvider, string providerKey)
        {
            string strTemp = string.Format("{0}_{1}", loginProvider?.ToUpper(), providerKey?.ToUpper());
            string hash = ConvertKeyToHash(strTemp);
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserLogin, hash);
        }

        public override double KeyVersion => 2.2;

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
