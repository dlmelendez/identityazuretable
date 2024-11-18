// MIT License Copyright 2022 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests.Fakes
{
    internal class HashTestKeyHelperFake : BaseKeyHelper
    {
        public override ReadOnlySpan<char> ConvertKeyToHash(ReadOnlySpan<char> input)
        {
            throw new NotImplementedException();
        }

        public string ConvertKeyToHashBackwardCompatSHA1(string input)
        {
            if (input != null)
            {
                using SHA1 sha = SHA1.Create();
                return GetHash(sha, input, Encoding.Unicode, 40);
            }
            return null;
        }

        public string ConvertKeyToHashBackwardCompatSHA256(string input)
        {
            if (input != null)
            {
                using SHA256 sha = SHA256.Create();
                return GetHash(sha, input, Encoding.UTF8, 64);
            }
            return null;
        }
    }
}
