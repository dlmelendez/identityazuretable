// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Text;

namespace ElCamino.AspNetCore.Identity.AzureTable.Helpers
{
    /// <summary>
    /// *Experimental* Uses SHA256 for hashing keys. UserId is not hashed for use with row/partition keys
    /// </summary>
    public class SHA256KeyHelper : BaseKeyHelper
    {
        public override string ConvertKeyToHash(string input)
        {
            if (input != null)
            {
                var encoding = Encoding.UTF8;
#if NET6_0_OR_GREATER
                // We can elide the SHA256 allocation if this isn't a derived type
                if (GetType() == typeof(SHA256KeyHelper))
                {
                    byte[] data = SHA256.HashData(encoding.GetBytes(input));
                    return Convert.ToHexString(data).ToLowerInvariant();
                }
#endif
                using SHA256 sha = SHA256.Create();
                return GetHash(sha, input, encoding, 64);
            }
            return null;
        }

        public override string GenerateRowKeyUserId(string plainUserId)
        {
            return string.Format(FormatterIdentityUserId, plainUserId);
        }

    }
}
