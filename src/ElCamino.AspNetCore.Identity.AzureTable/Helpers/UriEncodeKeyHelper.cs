// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if net45
using Microsoft.AspNet.Identity;
#else
using Microsoft.AspNetCore.Identity;
#endif

#if net45
namespace ElCamino.AspNet.Identity.AzureTable.Helpers
#else
namespace ElCamino.AspNetCore.Identity.AzureTable.Helpers
#endif
{
    public class UriEncodeKeyHelper : BaseKeyHelper
    {
        public const string ReplaceIllegalChar = "%";
        public const string NewCharForIllegalChar = "_";


		public override string GenerateRowKeyUserLoginInfo(string plainLoginProvider, string plainProviderKey)
		{
			string strTemp = string.Format("{0}_{1}", EscapeKey(plainLoginProvider), EscapeKey(plainProviderKey));
			return string.Format(Constants.RowKeyConstants.FormatterIdentityUserLogin, strTemp);
		}


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
#if !net45
        public override string GenerateRowKeyIdentityRoleClaim(string claimType, string claimValue)
        {
            string strTemp = string.Format("{0}_{1}", EscapeKey(claimType), EscapeKey(claimValue));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityRoleClaim, strTemp);

        }
#endif

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
            get { return 1.65; }
        }
    }
}
