// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Text;

namespace ElCamino.AspNetCore.Identity.AzureTable.Helpers
{
    public class DefaultKeyHelper : BaseKeyHelper
    {
        public override string ConvertKeyToHash(string input)
        {
            if (input != null)
            {
                var encoding = Encoding.Unicode;
#if NET6_0_OR_GREATER
                // We can elide the SHA1 allocation if this isn't a derived type
                if (GetType() == typeof(DefaultKeyHelper))
                {
                    byte[] data = SHA1.HashData(encoding.GetBytes(input));
                    return Convert.ToHexString(data).ToLowerInvariant();
                }
#endif
                using SHA1 sha = SHA1.Create();
                return GetHash(sha, input, encoding, 40);
            }
            return null;
        }
    }
}
