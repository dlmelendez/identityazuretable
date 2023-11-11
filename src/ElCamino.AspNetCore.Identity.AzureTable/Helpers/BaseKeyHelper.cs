// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using ElCamino.AspNetCore.Identity.AzureTable.Model;


namespace ElCamino.AspNetCore.Identity.AzureTable.Helpers
{
    /// <inheritdoc/>
    public abstract class BaseKeyHelper : IKeyHelper
    {
        /// <inheritdoc/>
        public virtual string PreFixIdentityUserClaim => TableConstants.RowKeyConstants.PreFixIdentityUserClaim;

        /// <inheritdoc/>
        public virtual string PreFixIdentityUserClaimUpperBound => TableConstants.RowKeyConstants.PreFixIdentityUserClaimUpperBound;

        /// <inheritdoc/>
        public virtual string PreFixIdentityUserRole => TableConstants.RowKeyConstants.PreFixIdentityUserRole;

        /// <inheritdoc/>
        public virtual string PreFixIdentityUserRoleUpperBound => TableConstants.RowKeyConstants.PreFixIdentityUserRoleUpperBound;

        /// <inheritdoc/>
        public virtual string PreFixIdentityUserLogin => TableConstants.RowKeyConstants.PreFixIdentityUserLogin;

        /// <inheritdoc/>
        public virtual string PreFixIdentityUserLoginUpperBound => TableConstants.RowKeyConstants.PreFixIdentityUserLoginUpperBound;

        /// <inheritdoc/>
        public virtual string PreFixIdentityUserEmail => TableConstants.RowKeyConstants.PreFixIdentityUserEmail;

        /// <inheritdoc/>
        public virtual string PreFixIdentityUserToken => TableConstants.RowKeyConstants.PreFixIdentityUserToken;

        /// <inheritdoc/>
        public virtual string PreFixIdentityUserId => TableConstants.RowKeyConstants.PreFixIdentityUserId;

        /// <inheritdoc/>
        public virtual string PreFixIdentityUserIdUpperBound => TableConstants.RowKeyConstants.PreFixIdentityUserIdUpperBound;

        /// <inheritdoc/>
        public virtual string PreFixIdentityUserName => TableConstants.RowKeyConstants.PreFixIdentityUserName;

        /// <inheritdoc/>
        public virtual string FormatterIdentityUserClaim => TableConstants.RowKeyConstants.FormatterIdentityUserClaim;

        /// <inheritdoc/>
        public virtual string FormatterIdentityUserRole => TableConstants.RowKeyConstants.FormatterIdentityUserRole;

        /// <inheritdoc/>
        public virtual string FormatterIdentityUserLogin => TableConstants.RowKeyConstants.FormatterIdentityUserLogin;

        /// <inheritdoc/>
        public virtual string FormatterIdentityUserEmail => TableConstants.RowKeyConstants.FormatterIdentityUserEmail;

        /// <inheritdoc/>
        public virtual string FormatterIdentityUserToken => TableConstants.RowKeyConstants.FormatterIdentityUserToken;

        /// <inheritdoc/>
        public virtual string FormatterIdentityUserId => TableConstants.RowKeyConstants.FormatterIdentityUserId;

        /// <inheritdoc/>
        public virtual string FormatterIdentityUserName => TableConstants.RowKeyConstants.FormatterIdentityUserName;

        /// <inheritdoc/>
        public virtual string PreFixIdentityRole => TableConstants.RowKeyConstants.PreFixIdentityRole;

        /// <inheritdoc/>
        public virtual string PreFixIdentityRoleUpperBound => TableConstants.RowKeyConstants.PreFixIdentityRoleUpperBound;

        /// <inheritdoc/>
        public virtual string PreFixIdentityRoleClaim => TableConstants.RowKeyConstants.PreFixIdentityRoleClaim;

        /// <inheritdoc/>
        public virtual string FormatterIdentityRole => TableConstants.RowKeyConstants.FormatterIdentityRole;

        /// <inheritdoc/>
        public virtual string FormatterIdentityRoleClaim => TableConstants.RowKeyConstants.FormatterIdentityRoleClaim;

        /// <inheritdoc/>
        public virtual string GeneratePartitionKeyIndexByLogin(string plainLoginProvider, string plainProviderKey)
        {
            string strTemp = string.Format("{0}_{1}", plainLoginProvider?.ToUpper(), plainProviderKey?.ToUpper());
            string? hash = ConvertKeyToHash(strTemp);
            return string.Format(FormatterIdentityUserLogin, hash);
        }

        /// <inheritdoc/>
        public virtual string GenerateRowKeyUserEmail(string? plainEmail)
        {
            string? hash = ConvertKeyToHash(plainEmail?.ToUpper());
            return string.Format(FormatterIdentityUserEmail, hash);
        }

        /// <inheritdoc/>
        public virtual string GenerateUserId()
        {
            return Guid.NewGuid().ToString("N");
        }

        /// <inheritdoc/>
        public virtual string GenerateRowKeyUserId(string? plainUserId)
        {
            string? hash = ConvertKeyToHash(plainUserId?.ToUpper());
            return string.Format(FormatterIdentityUserId, hash);
        }

        /// <inheritdoc/>
        public virtual string GenerateRowKeyUserName(string? plainUserName)
        {
            return GeneratePartitionKeyUserName(plainUserName);
        }

        /// <inheritdoc/>
        public virtual string GeneratePartitionKeyUserName(string? plainUserName)
        {
            string? hash = ConvertKeyToHash(plainUserName?.ToUpper());
            return string.Format(FormatterIdentityUserName, hash);
        }

        /// <inheritdoc/>
        public virtual string GenerateRowKeyIdentityUserRole(string? plainRoleName)
        {
            string? hash = ConvertKeyToHash(plainRoleName?.ToUpper());
            return string.Format(FormatterIdentityUserRole, hash);
        }

        /// <inheritdoc/>
        public virtual string GenerateRowKeyIdentityRole(string? plainRoleName)
        {
            string? hash = ConvertKeyToHash(plainRoleName?.ToUpper());
            return string.Format(FormatterIdentityRole, hash);
        }

        /// <inheritdoc/>
        public virtual string GeneratePartitionKeyIdentityRole(string? plainRoleName)
        {
            string? hash = ConvertKeyToHash(plainRoleName?.ToUpper());
            return hash?.Substring(startIndex: 0, length: 1)??string.Empty;
        }

        /// <inheritdoc/>
        public virtual string GenerateRowKeyIdentityUserClaim(string? claimType, string? claimValue)
        {
            string strTemp = string.Format("{0}_{1}", claimType?.ToUpper(), claimValue?.ToUpper());
            string? hash = ConvertKeyToHash(strTemp);
            return string.Format(FormatterIdentityUserClaim, hash);
        }

        /// <inheritdoc/>
        public virtual string GenerateRowKeyIdentityRoleClaim(string? claimType, string? claimValue)
        {
            string strTemp = string.Format("{0}_{1}", claimType?.ToUpper(), claimValue?.ToUpper());
            string? hash = ConvertKeyToHash(strTemp);
            return string.Format(FormatterIdentityRoleClaim, hash);
        }

        /// <inheritdoc/>
        public virtual string GenerateRowKeyIdentityUserToken(string? loginProvider, string? name)
        {
            string strTemp = string.Format("{0}_{1}", loginProvider?.ToUpper(), name?.ToUpper());
            string? hash = ConvertKeyToHash(strTemp);
            return string.Format(FormatterIdentityUserToken, hash);
        }

        /// <inheritdoc/>
        public virtual string ParsePartitionKeyIdentityRoleFromRowKey(string rowKey)
        {
            return rowKey.Substring(PreFixIdentityRole.Length, 1);
        }

        /// <inheritdoc/>
        public virtual string GenerateRowKeyIdentityUserLogin(string? loginProvider, string? providerKey)
        {
            string strTemp = string.Format("{0}_{1}", loginProvider?.ToUpper(), providerKey?.ToUpper());
            string? hash = ConvertKeyToHash(strTemp);
            return string.Format(FormatterIdentityUserLogin, hash);
        }

        /// <inheritdoc/>
        public double KeyVersion => 8.0;

        /// <summary>
        /// Convert Key Data to a hex hash string
        /// </summary>
        /// <param name="input">Plain text input</param>
        /// <returns>Returns a hex string</returns>
        public abstract string? ConvertKeyToHash(string? input);

        /// <summary>
        /// Left only for backwards compat for older frameworks.
        /// </summary>
        /// <param name="shaHash"></param>
        /// <param name="input"></param>
        /// <param name="encoding"></param>
        /// <param name="hashHexLength"></param>
        /// <returns></returns>
        protected virtual string GetHash(HashAlgorithm shaHash, string input, Encoding encoding, int hashHexLength)
        {
            // Convert the input string to a byte array and compute the hash. 
            byte[] data = shaHash.ComputeHash(encoding.GetBytes(input));
            Debug.WriteLine(string.Format("Key Size before hash: {0} bytes", encoding.GetBytes(input).Length));

            // Create a new StringBuilder to collect the bytes 
            // and create a string.
            StringBuilder sBuilder = new StringBuilder(hashHexLength);

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

#if NET6_0_OR_GREATER
        /// <summary>
        /// Format byte array to hex string
        /// </summary>
        /// <param name="hashedData">byte array to format</param>
        /// <returns></returns>
        protected static string FormatHashedData(byte[] hashedData)
        {
            // Convert the input string to a byte array and compute the hash. 
            return Convert.ToHexString(hashedData).ToLowerInvariant();
        }
#endif

    }
}
