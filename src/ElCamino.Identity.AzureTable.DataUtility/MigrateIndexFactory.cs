﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElCamino.Identity.AzureTable.DataUtility
{
    public static class MigrationFactory
    {
        public const string EmailIndex = "emailindex";
        public const string LoginIndex = "loginindex";
        public const string UserNameIndex = "usernameindex";
        public const string ClaimRowkey = "claimrowkey";
        public const string RoleAndClaimIndex = "roleandclaimindex";


        public static IMigration CreateMigration(string migrateCommand)
        {
            string cmd = migrateCommand.ToLower();
            switch(cmd)
            {
                case EmailIndex:
                    return new EmailMigrateIndex();
                case LoginIndex:
                    return new LoginMigrateIndex();
                case ClaimRowkey:
                    return new ClaimMigrateRowkey();
                case RoleAndClaimIndex:
                    return new RoleAndClaimMigrateIndex();
                case UserNameIndex:
                    return new UsernameMigrateIndex();
                default:
                    break;
            }
            return null;
        }
    }
}
