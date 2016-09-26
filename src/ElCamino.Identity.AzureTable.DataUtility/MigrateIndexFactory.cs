using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElCamino.Identity.AzureTable.DataUtility
{
    public static class MigrateIndexFactory
    {
        public const string EmailIndex = "emailindex";
        public const string LoginIndex = "loginindex";


        public static IMigrateIndex CreateMigrateIndex(string migrateCommand)
        {
            string cmd = migrateCommand.ToLower();
            switch(cmd)
            {
                case EmailIndex:
                    return new EmailMigrateIndex();
                case LoginIndex:
                    return new LoginMigrateIndex();
                default:
                    break;
            }
            return null;
        }
    }
}
