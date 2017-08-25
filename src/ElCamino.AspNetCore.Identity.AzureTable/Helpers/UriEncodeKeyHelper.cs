// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace ElCamino.AspNetCore.Identity.AzureTable.Helpers
{
    public class UriEncodeKeyHelper : BaseKeyHelper
    {
        public const string ReplaceIllegalChar = "%";
        public const string NewCharForIllegalChar = "_";


        public override string GeneratePartitionKeyIndexByLogin(string plainLoginProvider, string plainProviderKey)
        {
            string strTemp = string.Format("{0}_{1}", EscapeKey(plainLoginProvider), EscapeKey(plainProviderKey));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserLogin, strTemp);
        }

        public override string GenerateRowKeyUserEmail(string plainEmail)
        {
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserEmail,
                    EscapeKey(plainEmail));
        }

        public override string GenerateRowKeyUserName(string plainUserName)
        {
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserName,
                    EscapeKey(plainUserName));
        }

        public override string GenerateRowKeyIdentityUserRole(string plainRoleName)
        {
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserRole,
                EscapeKey(plainRoleName));
        }

        public override string GenerateRowKeyIdentityRole(string plainRoleName)
        {
            return string.Format(Constants.RowKeyConstants.FormatterIdentityRole,
                    EscapeKey(plainRoleName));
        }

        public override string GeneratePartitionKeyIdentityRole(string plainRoleName)
        {
            return EscapeKey(plainRoleName.Substring(0, 1));
        }

        public override string GenerateRowKeyIdentityUserClaim(string claimType, string claimValue)
        {
            string strTemp = string.Format("{0}_{1}", EscapeKey(claimType), EscapeKey(claimValue));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserClaim, strTemp);
        }

        public override string GenerateRowKeyIdentityRoleClaim(string claimType, string claimValue)
        {
            string strTemp = string.Format("{0}_{1}", EscapeKey(claimType), EscapeKey(claimValue));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityRoleClaim, strTemp);
        }

        public override string GenerateRowKeyIdentityUserToken(string loginProvider, string name)
        {
            string strTemp = string.Format("{0}_{1}", EscapeKey(loginProvider), EscapeKey(name));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserToken, strTemp);
        }

        public override string ParsePartitionKeyIdentityRoleFromRowKey(string rowKey)
        {
            return rowKey.Substring(Constants.RowKeyConstants.PreFixIdentityRole.Length, 1);
        }

        private static string EscapeKey(string keyUnsafe)
        {
            if (!string.IsNullOrWhiteSpace(keyUnsafe))
            {
                // Need to replace '%' because azure bug.
                return System.Uri.EscapeDataString(keyUnsafe).Replace(ReplaceIllegalChar, NewCharForIllegalChar).ToUpper();
            }
            return null;
        }

        public override string GenerateRowKeyIdentityUserLogin(string loginProvider, string providerKey)
        {
            string strTemp = string.Format("{0}_{1}", EscapeKey(loginProvider), EscapeKey(providerKey));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserLogin, strTemp);
        }

        public override double KeyVersion
        {
            get { return 1.67; }
        }
    }
}
