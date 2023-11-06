// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.


namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    public class IdentityConfiguration
    {
        public string? TablePrefix { get; set; }

        public string? StorageConnectionString { get; set; }

        public string? IndexTableName { get; set; }

        public string? UserTableName { get; set; }

        public string? RoleTableName { get; set; }

    }
}
