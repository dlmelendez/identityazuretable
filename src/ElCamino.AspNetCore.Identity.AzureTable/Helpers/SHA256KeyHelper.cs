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

#if NET6_0_OR_GREATER
        public sealed override string? ConvertKeyToHash(string? input)
        {
            if (input != null)
            {
                byte[] data = SHA256.HashData(Encoding.UTF8.GetBytes(input));
                return FormatHashedData(data);
            }
            return null;
        }

#else

        public sealed override string? ConvertKeyToHash(string? input)
        {
            if (input != null)
            {
                using SHA256 sha = SHA256.Create();
                return GetHash(sha, input, Encoding.UTF8, 64);
            }
            return null;
        }
#endif

        public override string GenerateRowKeyUserId(string? plainUserId)
        {
            return string.Format(FormatterIdentityUserId, plainUserId);
        }

    }
}
