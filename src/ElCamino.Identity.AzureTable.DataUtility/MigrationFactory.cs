// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using ElCamino.AspNetCore.Identity.AzureTable.Model;

namespace ElCamino.Identity.AzureTable.DataUtility
{
    public static class MigrationFactory
    {
        public const string EmailIndex = "emailindex";
        public const string LoginIndex = "loginindex";
        public const string ClaimRowkey = "claimrowkey";
        public const string RoleAndClaimIndex = "roleandclaimindex";
        public const string Users = "users";
        public const string Roles = "roles";
        public static readonly IKeyHelper KeyHelper = new DefaultKeyHelper();

        public static IMigration CreateMigration(string migrateCommand)
        {
            string cmd = migrateCommand.ToLower();
            switch (cmd)
            {
                case EmailIndex:
                    return new EmailMigrateIndex(KeyHelper);
                case LoginIndex:
                    return new LoginMigrateIndex(KeyHelper);
                case ClaimRowkey:
                    return new ClaimMigrateRowkey(KeyHelper);
                case RoleAndClaimIndex:
                    return new RoleAndClaimMigrateIndex(KeyHelper);
                case Users:
                    return new UsersMigration(KeyHelper);
                case Roles:
                    return new RolesMigration(KeyHelper);
                default:
                    break;
            }
            throw new ArgumentException($"Invalid Migration Command: {migrateCommand}", nameof(migrateCommand));
        }
    }
}
