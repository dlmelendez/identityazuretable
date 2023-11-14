// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using Azure;

namespace ElCamino.AspNetCore.Identity.AzureTable
{
    /// <summary>
    /// Default table constants
    /// </summary>
    public static class TableConstants
    {
        /// <summary>
        /// ETag.All <seealso cref="ETag"/>
        /// </summary>
        public static readonly ETag ETagWildcard = ETag.All;

        /// <summary>
        /// Default Table Names
        /// </summary>
        public static class TableNames
        {
            /// <summary>
            /// Defaut Roles Table Name
            /// </summary>
            public const string RolesTable = "AspNetRoles";

            /// <summary>
            /// Defaut Users Table Name
            /// </summary>
            public const string UsersTable = "AspNetUsers";

            /// <summary>
            /// Defaut Index Table Name
            /// </summary>
            public const string IndexTable = "AspNetIndex";
        }

        /// <summary>
        /// Default Key Constants
        /// </summary>
        public static class RowKeyConstants
        {
            /// <summary>
            /// Default Key PreFix for IdentityUserClaim
            /// </summary>
            public const string PreFixIdentityUserClaim = "C_";

            /// <summary>
            /// Default Key PreFix for IdentityUserClaimUpperBound
            /// </summary>
            public const string PreFixIdentityUserClaimUpperBound = "D_";

            /// <summary>
            /// Default Key PreFix for IdentityUserRole
            /// </summary>
            public const string PreFixIdentityUserRole = "R_";

            /// <summary>
            /// Default Key PreFix for IdentityUserRoleUpperBound
            /// </summary>
            public const string PreFixIdentityUserRoleUpperBound = "S_";

            /// <summary>
            /// Default Key PreFix for IdentityUserLogin
            /// </summary>
            public const string PreFixIdentityUserLogin = "L_";

            /// <summary>
            /// Default Key PreFix for IdentityUserLoginUpperBound
            /// </summary>
            public const string PreFixIdentityUserLoginUpperBound = "M_";

            /// <summary>
            /// Default Key PreFix for IdentityUserEmail
            /// </summary>
            public const string PreFixIdentityUserEmail = "E_";

            /// <summary>
            /// Default Key PreFix for IdentityUserToken
            /// </summary>
            public const string PreFixIdentityUserToken = "T_";

            /// <summary>
            /// Default Key PreFix for IdentityUserId
            /// </summary>
            public const string PreFixIdentityUserId = "U_";

            /// <summary>
            /// Default Key PreFix for IdentityUserIdUpperBound
            /// </summary>
            public const string PreFixIdentityUserIdUpperBound = "V_";

            /// <summary>
            /// Default Key PreFix for IdentityUserName
            /// </summary>
            public const string PreFixIdentityUserName = "N_";

            /// <summary>
            /// Default Key Formatter for IdentityUserClaim
            /// </summary>
            public const string FormatterIdentityUserClaim = PreFixIdentityUserClaim + "{0}";

            /// <summary>
            /// Default Key Formatter for IdentityUserRole
            /// </summary>
            public const string FormatterIdentityUserRole = PreFixIdentityUserRole + "{0}";

            /// <summary>
            /// Default Key Formatter for IdentityUserLogin
            /// </summary>
            public const string FormatterIdentityUserLogin = PreFixIdentityUserLogin + "{0}";

            /// <summary>
            /// Default Key Formatter for IdentityUserEmail
            /// </summary>
            public const string FormatterIdentityUserEmail = PreFixIdentityUserEmail + "{0}";

            /// <summary>
            /// Default Key Formatter for IdentityUserToken
            /// </summary>
            public const string FormatterIdentityUserToken = PreFixIdentityUserToken + "{0}";

            /// <summary>
            /// Default Key Formatter for IdentityUserId
            /// </summary>
            public const string FormatterIdentityUserId = PreFixIdentityUserId + "{0}";

            /// <summary>
            /// Default Key Formatter for IdentityUserName
            /// </summary>
            public const string FormatterIdentityUserName = PreFixIdentityUserName + "{0}";

            /// <summary>
            /// Default Key PreFix for IdentityRole
            /// </summary>
            public const string PreFixIdentityRole = "R_";

            /// <summary>
            /// Default Key PreFix for IdentityRoleUpperBound
            /// </summary>
            public const string PreFixIdentityRoleUpperBound = "S_";

            /// <summary>
            /// Default Key PreFix for IdentityRoleClaim
            /// </summary>
            public const string PreFixIdentityRoleClaim = "C_";

            /// <summary>
            /// Default Key Formatter for IdentityRole
            /// </summary>
            public const string FormatterIdentityRole = PreFixIdentityRole + "{0}";

            /// <summary>
            /// Default Key Formatter for IdentityRoleClaim
            /// </summary>
            public const string FormatterIdentityRoleClaim = PreFixIdentityRoleClaim + "{0}";
        }
    }
}
