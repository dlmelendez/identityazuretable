// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Text;

namespace ElCamino.AspNetCore.Identity.AzureTable.Helpers
{
    /// <summary>
    /// Default Key Helpers users SHA1 with Unicode encoding
    /// </summary>
    public class DefaultKeyHelper : BaseKeyHelper
    {
        /// <inheritdoc/>
        public sealed override ReadOnlySpan<char> ConvertKeyToHash(ReadOnlySpan<char> input)
        {
            Span<byte> encodedBytes = stackalloc byte[Encoding.Unicode.GetMaxByteCount(input.Length)];
            int encodedByteCount = Encoding.Unicode.GetBytes(input, encodedBytes);

            Span<byte> hashedBytes = stackalloc byte[SHA1.HashSizeInBytes];
            int hashedByteCount = SHA1.HashData(encodedBytes.Slice(0, encodedByteCount), hashedBytes);

            return FormatHashedData(hashedBytes.Slice(0, hashedByteCount));
        }
    }
}
