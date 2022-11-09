// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Text;

namespace ElCamino.AspNetCore.Identity.AzureTable.Helpers
{
    public class DefaultKeyHelper : BaseKeyHelper
    {
#if NET6_0_OR_GREATER

        public sealed override string ConvertKeyToHash(string input)
        {
            if (input != null)
            {
                byte[] data = SHA1.HashData(Encoding.Unicode.GetBytes(input));
                return FormatHashedData(data);
            }
            return null;
        }
#else
        public sealed override string ConvertKeyToHash(string input)
        {
            if (input != null)
            {
                using SHA1 sha = SHA1.Create();
                return GetHash(sha, input, Encoding.Unicode, 40);
            }
            return null;
        }
#endif
    }
}
