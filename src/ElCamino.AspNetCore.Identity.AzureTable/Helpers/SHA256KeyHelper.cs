using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ElCamino.AspNetCore.Identity.AzureTable.Helpers
{
    public class SHA256KeyHelper : BaseKeyHelper
    {
        public override string ConvertKeyToHash(string input)
        {
            if (input != null)
            {
                using (SHA256 sha = SHA256.Create())
                {
                    return GetHash(sha, input, Encoding.UTF8, 64);
                }
            }
            return null;
        }
    }
}
