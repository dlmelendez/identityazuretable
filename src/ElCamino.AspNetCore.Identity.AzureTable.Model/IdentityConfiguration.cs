// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.


namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    /// <summary>
    /// Table Storage Configuration
    /// </summary>
    public class IdentityConfiguration
    {
        /// <summary>
        /// Optional field, prefixes all given table names 
        /// </summary>
        public string? TablePrefix { get; set; }

        /// <summary>
        /// Optional, default value is AspNetIndex
        /// </summary>
        public string? IndexTableName { get; set; }

        /// <summary>
        /// Optional, default value is AspNetUsers
        /// </summary>
        public string? UserTableName { get; set; }

        /// <summary>
        /// Optional, default value is AspNetRoles
        /// </summary>
        public string? RoleTableName { get; set; }

    }
}
