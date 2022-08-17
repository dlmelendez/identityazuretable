// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElCamino.AspNetCore.Identity.AzureTable
{
    public static class TableConstants
    {
        public static readonly ETag ETagWildcard =  ETag.All;

        public static class TableNames
        {
            public const string RolesTable = "AspNetRoles";
            public const string UsersTable = "AspNetUsers";
            public const string IndexTable = "AspNetIndex";
        }

        public static class RowKeyConstants
        {
            public const string PreFixIdentityUserClaim = "C_";
            public const string PreFixIdentityUserClaimUpperBound = "D_";
            public const string PreFixIdentityUserRole = "R_";
            public const string PreFixIdentityUserRoleUpperBound = "S_";
            public const string PreFixIdentityUserLogin = "L_";
            public const string PreFixIdentityUserLoginUpperBound = "M_";
            public const string PreFixIdentityUserEmail = "E_";
            public const string PreFixIdentityUserToken = "T_";
            public const string PreFixIdentityUserId = "U_";
            public const string PreFixIdentityUserIdUpperBound = "V_";
            public const string PreFixIdentityUserName = "N_";

            public const string FormatterIdentityUserClaim = PreFixIdentityUserClaim + "{0}";
            public const string FormatterIdentityUserRole = PreFixIdentityUserRole + "{0}";
            public const string FormatterIdentityUserLogin = PreFixIdentityUserLogin + "{0}";
            public const string FormatterIdentityUserEmail = PreFixIdentityUserEmail + "{0}";
            public const string FormatterIdentityUserToken = PreFixIdentityUserToken + "{0}";
            public const string FormatterIdentityUserId = PreFixIdentityUserId + "{0}";
            public const string FormatterIdentityUserName = PreFixIdentityUserName + "{0}";

            public const string PreFixIdentityRole = "R_";
            public const string PreFixIdentityRoleUpperBound = "S_";
            public const string PreFixIdentityRoleClaim = "C_";
            public const string FormatterIdentityRole = PreFixIdentityRole + "{0}";
            public const string FormatterIdentityRoleClaim = PreFixIdentityRoleClaim + "{0}";
        }
    }
}
