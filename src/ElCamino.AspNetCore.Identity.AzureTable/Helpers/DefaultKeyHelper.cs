// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Text;

namespace ElCamino.AspNetCore.Identity.AzureTable.Helpers
{
    /// <summary>
    /// Default Key Helpers users SHA1
    /// </summary>
    public class DefaultKeyHelper : BaseKeyHelper
    {
#if NET6_0_OR_GREATER
        /// <inheritdoc/>
        public sealed override string? ConvertKeyToHash(string? input)
        {
            if (input is not null)
            {
                byte[] data = SHA1.HashData(Encoding.Unicode.GetBytes(input));
                return FormatHashedData(data);
            }
            return null;
        }
#else
        /// <inheritdoc/>
        public sealed override string? ConvertKeyToHash(string? input)
        {
            if (input is not null)
            {
                using SHA1 sha = SHA1.Create();
                return GetHash(sha, input, Encoding.Unicode, 40);
            }
            return null;
        }
#endif
    }
}
