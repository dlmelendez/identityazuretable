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
            var strTemp = string.Format("{0}_{1}", plainLoginProvider?.ToUpper(), plainProviderKey?.ToUpper()).AsSpan();
            var hash = ConvertKeyToHash(strTemp);
            return string.Format(FormatterIdentityUserLogin, hash.ToString());
        }

        /// <inheritdoc/>
        public virtual string GenerateRowKeyUserEmail(string? plainEmail)
        {
            var hash = ConvertKeyToHash(plainEmail?.ToUpper());
            return string.Format(FormatterIdentityUserEmail, hash.ToString());
        }

        /// <inheritdoc/>
        public virtual string GenerateUserId()
        {
            return Guid.NewGuid().ToString("N");
        }

        /// <inheritdoc/>
        public virtual string GenerateRowKeyUserId(string? plainUserId)
        {
            var hash = ConvertKeyToHash(plainUserId?.ToUpper());
            return string.Format(FormatterIdentityUserId, hash.ToString());
        }

        /// <inheritdoc/>
        public virtual string GenerateRowKeyUserName(string? plainUserName)
        {
            return GeneratePartitionKeyUserName(plainUserName);
        }

        /// <inheritdoc/>
        public virtual string GeneratePartitionKeyUserName(string? plainUserName)
        {
            var hash = ConvertKeyToHash(plainUserName?.ToUpper());
            return string.Format(FormatterIdentityUserName, hash.ToString());
        }

        /// <inheritdoc/>
        public virtual string GenerateRowKeyIdentityUserRole(string? plainRoleName)
        {
            var hash = ConvertKeyToHash(plainRoleName?.ToUpper());
            return string.Format(FormatterIdentityUserRole, hash.ToString());
        }

        /// <inheritdoc/>
        public virtual string GenerateRowKeyIdentityRole(string? plainRoleName)
        {
            var hash = ConvertKeyToHash(plainRoleName?.ToUpper());
            return string.Format(FormatterIdentityRole, hash.ToString());
        }

        /// <inheritdoc/>
        public virtual string GeneratePartitionKeyIdentityRole(string? plainRoleName)
        {
            var hash = ConvertKeyToHash(plainRoleName?.ToUpper());
            if(hash.IsEmpty)
            {
                return string.Empty;
            }
            return hash.Slice(start: 0, length: 1).ToString();
        }

        /// <inheritdoc/>
        public virtual string GenerateRowKeyIdentityUserClaim(string? claimType, string? claimValue)
        {
            var strTemp = string.Format("{0}_{1}", claimType?.ToUpper(), claimValue?.ToUpper()).AsSpan();
            var hash = ConvertKeyToHash(strTemp);
            return string.Format(FormatterIdentityUserClaim, hash.ToString());
        }

        /// <inheritdoc/>
        public virtual string GenerateRowKeyIdentityRoleClaim(string? claimType, string? claimValue)
        {
            var strTemp = string.Format("{0}_{1}", claimType?.ToUpper(), claimValue?.ToUpper()).AsSpan();
            var hash = ConvertKeyToHash(strTemp);
            return string.Format(FormatterIdentityRoleClaim, hash.ToString());
        }

        /// <inheritdoc/>
        public virtual string GenerateRowKeyIdentityUserToken(string? loginProvider, string? name)
        {
            var strTemp = string.Format("{0}_{1}", loginProvider?.ToUpper(), name?.ToUpper()).AsSpan();
            var hash = ConvertKeyToHash(strTemp);
            return string.Format(FormatterIdentityUserToken, hash.ToString());
        }

        /// <inheritdoc/>
        public virtual string ParsePartitionKeyIdentityRoleFromRowKey(string rowKey)
        {
            return rowKey.AsSpan().Slice(PreFixIdentityRole.Length, 1).ToString();
        }

        /// <inheritdoc/>
        public virtual string GenerateRowKeyIdentityUserLogin(string? loginProvider, string? providerKey)
        {
            var strTemp = string.Format("{0}_{1}", loginProvider?.ToUpper(), providerKey?.ToUpper()).AsSpan();
            var hash = ConvertKeyToHash(strTemp);
            return string.Format(FormatterIdentityUserLogin, hash.ToString());
        }

        /// <inheritdoc/>
        public double KeyVersion => 8.0;

        /// <summary>
        /// Convert Key Data to a hex hash string
        /// </summary>
        /// <param name="input">Plain text input</param>
        /// <returns>Returns a hex string</returns>
        public abstract ReadOnlySpan<char> ConvertKeyToHash(ReadOnlySpan<char> input);

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

        /// <summary>
        /// Format byte array to hex string
        /// </summary>
        /// <param name="hashedData">byte array to format</param>
        /// <returns></returns>
        protected static string FormatHashedData(ReadOnlySpan<byte> hashedData)
        {
            // Convert the input string to a byte array and compute the hash. 
            return Convert.ToHexString(hashedData).ToLowerInvariant();
        }

    }
}
