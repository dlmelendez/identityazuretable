// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
                using SHA256 sha = SHA256.Create();
                return GetHash(sha, input, Encoding.UTF8, 64);
            }
            return null;
        }

        public override string GenerateRowKeyUserId(string plainUserId)
        {
            return string.Format(FormatterIdentityUserId, plainUserId);
        }
       
    }
}
