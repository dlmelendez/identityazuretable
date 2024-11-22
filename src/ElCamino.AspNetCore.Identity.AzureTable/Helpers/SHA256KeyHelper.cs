// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Text;

namespace ElCamino.AspNetCore.Identity.AzureTable.Helpers
{
    /// <summary>
    /// Uses SHA256 for hashing keys with UTF8 encoding. UserId is not hashed for use with row/partition keys
    /// </summary>
    public class SHA256KeyHelper : BaseKeyHelper
    {

        /// <inheritdoc/>
        public sealed override ReadOnlySpan<char> ConvertKeyToHash(ReadOnlySpan<char> input)
        {
            Span<byte> encodedBytes = stackalloc byte[Encoding.UTF8.GetMaxByteCount(input.Length)];
            int encodedByteCount = Encoding.UTF8.GetBytes(input, encodedBytes);
            Span<byte> hashedBytes = stackalloc byte[SHA256.HashSizeInBytes];
            int hashedByteCount = SHA256.HashData(encodedBytes.Slice(0, encodedByteCount), hashedBytes);
            return FormatHashedData(hashedBytes.Slice(0, hashedByteCount));
        }

        /// <inheritdoc/>
        public override ReadOnlySpan<char> GenerateRowKeyUserId(string? plainUserId)
        {
            ArgumentNullException.ThrowIfNull(plainUserId);
            return string.Format(FormatterIdentityUserId, plainUserId);
        }
    }
}
