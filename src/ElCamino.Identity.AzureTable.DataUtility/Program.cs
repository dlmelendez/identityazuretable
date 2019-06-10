// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using ElCamino.AspNetCore.Identity.AzureTable;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using System.Threading;
using System.IO;

namespace ElCamino.Identity.AzureTable.DataUtility
{
    public class Program
    {
        private static int iUserTotal = 0;
        private static int iUserSuccessConvert = 0;
        private static int iUserFailureConvert = 0;
        private static object ObjectLock = new object();
        private static ConcurrentBag<string> userIdFailures = new ConcurrentBag<string>();

        private readonly static List<string> helpTokens = new List<string>() { "/?", "/help" };
        private const string previewToken = "/preview:";
        private const string migrateToken = "/migrate:";
        private readonly static List<string> validCommands = new List<string>() {
            MigrationFactory.Roles,
            MigrationFactory.Users
        };
        private const string nodeleteToken = "/nodelete";
        private const string maxdegreesparallelToken = "/maxparallel:";
        private static int iMaxdegreesparallel = Environment.ProcessorCount * 2;
        private static string MigrateCommand = string.Empty;

        private const string startPageToken = "/startpage:";
        private const string finishPageToken = "/finishpage:";
        private const string pageSizeToken = "/pagesize:";


        private static int iStartPage = -1;
        private static int iFinishPage = -1;
        private static int iPageSize = 1000;

        private static bool migrateOption = false;
        private static bool deleteOption = false;

        public static IConfigurationRoot Configuration { get; private set; }

        public static void Main(string[] args)
        {
            if (!ValidateArgs(args))
            {
                return;
            }

            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
            Configuration = builder.Build();

            IdentityConfiguration sourceConfig = new IdentityConfiguration();
            sourceConfig.TablePrefix = Configuration.GetSection("source:IdentityConfiguration:TablePrefix")?.Value;
            sourceConfig.StorageConnectionString = Configuration.GetSection("source:IdentityConfiguration:StorageConnectionString")?.Value;
            sourceConfig.LocationMode = Configuration.GetSection("source:IdentityConfiguration:LocationMode")?.Value ?? string.Empty;
            sourceConfig.UserTableName = Configuration.GetSection("source:IdentityConfiguration:UserTableName")?.Value ?? string.Empty;
            sourceConfig.IndexTableName = Configuration.GetSection("source:IdentityConfiguration:IndexTableName")?.Value ?? string.Empty;
            sourceConfig.RoleTableName = Configuration.GetSection("source:IdentityConfiguration:RoleTableName")?.Value ?? string.Empty;

            IdentityConfiguration targetConfig = new IdentityConfiguration();
            targetConfig.TablePrefix = Configuration.GetSection("target:IdentityConfiguration:TablePrefix")?.Value;
            targetConfig.StorageConnectionString = Configuration.GetSection("target:IdentityConfiguration:StorageConnectionString")?.Value;
            targetConfig.LocationMode = Configuration.GetSection("target:IdentityConfiguration:LocationMode")?.Value ?? string.Empty;
            targetConfig.UserTableName = Configuration.GetSection("target:IdentityConfiguration:UserTableName")?.Value ?? string.Empty;
            targetConfig.IndexTableName = Configuration.GetSection("target:IdentityConfiguration:IndexTableName")?.Value ?? string.Empty;
            targetConfig.RoleTableName = Configuration.GetSection("target:IdentityConfiguration:RoleTableName")?.Value ?? string.Empty;


            Console.WriteLine("MaxDegreeOfParallelism: {0}", iMaxdegreesparallel);
            Console.WriteLine("PageSize: {0}", iPageSize);
            Console.WriteLine("MigrateCommand: {0}", MigrateCommand);

            var migration = MigrationFactory.CreateMigration(MigrateCommand);
            using (IdentityCloudContext targetContext = new IdentityCloudContext(targetConfig))
            {
                Task.WhenAll(targetContext.IndexTable.CreateIfNotExistsAsync(),
                            targetContext.UserTable.CreateIfNotExistsAsync(),
                            targetContext.RoleTable.CreateIfNotExistsAsync()).Wait();
                Console.WriteLine($"Target IndexTable: {targetContext.IndexTable.Name}");
                Console.WriteLine($"Target UserTable: {targetContext.UserTable.Name}");
                Console.WriteLine($"Target RoleTable: {targetContext.RoleTable.Name}");

                string entityRecordName = "Users";

                using (IdentityCloudContext sourceContext = new IdentityCloudContext(sourceConfig))
                {
                    Console.WriteLine($"Source IndexTable: {sourceContext.IndexTable.Name}");
                    Console.WriteLine($"Source UserTable: {sourceContext.UserTable.Name}");
                    Console.WriteLine($"Source RoleTable: {sourceContext.RoleTable.Name}");

                    DateTime startLoad = DateTime.UtcNow;
                    var allDataList = new List<DynamicTableEntity>(iPageSize);

                    TableQuery tq = migration.GetSourceTableQuery();

                    tq.TakeCount = iPageSize;
                    TableContinuationToken continueToken = new TableContinuationToken();

                    int iSkippedUserCount = 0;
                    int iSkippedPageCount = 0;
                    int iPageCounter = 0;
                    while (continueToken != null)
                    {
                        DateTime batchStart = DateTime.UtcNow;

                        CloudTable sourceTable = sourceContext.UserTable;
                        if (MigrateCommand == MigrationFactory.Roles)
                        {
                            sourceTable = sourceContext.RoleTable;
                            entityRecordName = "Role and Role Claims";
                        }
                        var sourceResults = sourceTable.ExecuteQuerySegmentedAsync(tq, continueToken).Result;
                        continueToken = sourceResults.ContinuationToken;


                        int batchCount = sourceResults.Count(migration.UserWhereFilter);
                        iUserTotal += batchCount;
                        iPageCounter++;

                        bool includePage = (iStartPage == -1 || iPageCounter >= iStartPage) && (iFinishPage == -1 || iPageCounter <= iFinishPage);

                        if (includePage)
                        {
                            if (migrateOption)
                            {
                                migration.ProcessMigrate(targetContext, sourceContext, sourceResults.Results, iMaxdegreesparallel,
                                () =>
                                {
                                    Interlocked.Increment(ref iUserSuccessConvert);
                                    Console.WriteLine($"{entityRecordName}(s) Complete: {iUserSuccessConvert}");
                                },
                                (exMessage) =>
                                {
                                    if (!string.IsNullOrWhiteSpace(exMessage))
                                    {
                                        userIdFailures.Add(exMessage);
                                    }
                                    Interlocked.Increment(ref iUserFailureConvert);
                                });
                            }

                        }
                        else
                        {
                            iSkippedPageCount++;
                            iSkippedUserCount += batchCount;
                        }

                        Console.WriteLine("Page: {2}{3}, {4} Batch: {1}: {0} seconds", (DateTime.UtcNow - batchStart).TotalSeconds, batchCount, iPageCounter, includePage ? string.Empty : "(Skipped)", entityRecordName);

                        //Are we done yet?
                        if (iFinishPage > 0 && iPageCounter >= iFinishPage)
                        {
                            break;
                        }
                    }


                    Console.WriteLine("");
                    Console.WriteLine("Elapsed time: {0} seconds", (DateTime.UtcNow - startLoad).TotalSeconds);
                    Console.WriteLine("Total {2} Skipped: {0}, Total Pages: {1}", iSkippedUserCount, iSkippedPageCount, entityRecordName);
                    Console.WriteLine("Total {2} To Convert: {0}, Total Pages: {1}", iUserTotal - iSkippedUserCount, iPageCounter - iSkippedPageCount, entityRecordName);

                    Console.WriteLine("");
                    if (migrateOption)
                    {
                        Console.WriteLine("Total {1} Successfully Converted: {0}", iUserSuccessConvert, entityRecordName);
                        Console.WriteLine("Total {1} Failed to Convert: {0}", iUserFailureConvert, entityRecordName);
                        if (iUserFailureConvert > 0)
                        {
                            Console.WriteLine($"{entityRecordName} Ids Failed:");
                            foreach (string s in userIdFailures)
                            {
                                Console.WriteLine(s);
                            }
                        }
                    }

                }
            }

            DisplayAnyKeyToExit();

        }

        private static void DisplayAnyKeyToExit()
        {
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static bool ValidateArgs(string[] args)
        {
            if (args.Length == 0 || args.Any(a => helpTokens.Any(h => h.Equals(a, StringComparison.OrdinalIgnoreCase))))
            {
                DisplayHelp();
                return false;
            }
            else
            {
                List<string> nonHelpTokens = new List<string>() { previewToken, migrateToken, nodeleteToken, maxdegreesparallelToken, startPageToken, finishPageToken, pageSizeToken };
                if (!args.All(a => nonHelpTokens.Any(h => a.StartsWith(h, StringComparison.OrdinalIgnoreCase))))
                {
                    DisplayInvalidArgs(args.Where(a => !nonHelpTokens.Any(h => h.StartsWith(a, StringComparison.OrdinalIgnoreCase))).ToList());
                    return false;
                }
                bool isPreview = args.Any(a => a.StartsWith(previewToken, StringComparison.OrdinalIgnoreCase));
                bool isMigrate = args.Any(a => a.StartsWith(migrateToken, StringComparison.OrdinalIgnoreCase));
                if (isPreview && isMigrate)
                {
                    DisplayInvalidArgs(new List<string>() { previewToken, migrateToken, "Cannot define /preview and /migrate. Only one can be used." });
                    return false;
                }
                bool isNoDelete = args.Any(a => a.Equals(nodeleteToken, StringComparison.OrdinalIgnoreCase));
                if (isNoDelete && !isMigrate)
                {
                    DisplayInvalidArgs(new List<string>() { nodeleteToken, "/nodelete must be used with /migrate option." });
                    return false;
                }

                if (!ValidateIntToken(maxdegreesparallelToken, ref iMaxdegreesparallel)
                    || !ValidateIntToken(startPageToken, ref iStartPage)
                    || !ValidateIntToken(finishPageToken, ref iFinishPage)
                    || !ValidateIntToken(pageSizeToken, ref iPageSize))
                    return false;

                if (isPreview)
                {
                    if(!ValidateCommandToken(previewToken, ref MigrateCommand))
                        return false;
                }

                if (isMigrate)
                {
                    if (!ValidateCommandToken(migrateToken, ref MigrateCommand))
                        return false;
                }

                if (iPageSize > 1000)
                {
                    DisplayInvalidArgs(new List<string>() { pageSizeToken, string.Format("{0} must be less than 1000", pageSizeToken) });
                    return false;
                }
                migrateOption = isMigrate;
                deleteOption = isMigrate && !isNoDelete;

                return true;
            }
        }

        private static bool ValidateIntToken(string token, ref int iTokenValue)
        {
            string args = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(token, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(args))
            {
                string[] splitArgs = args.Split(":".ToCharArray());
                int iTempValue = 0;
                if (splitArgs.Length == 2
                    && int.TryParse(splitArgs[1], out iTempValue)
                    && iTempValue > 0)
                {
                    iTokenValue = iTempValue;
                }
                else
                {
                    DisplayInvalidArgs(new List<string>() { args, string.Format("{0} must be followed by an int greater than 0. e.g. {0}3", token) });
                    return false;
                }
            }
            return true;
        }

        private static bool ValidateCommandToken(string token, ref string commandValue)
        {
            string args = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(token, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(args))
            {
                string[] splitArgs = args.Split(":".ToCharArray());
                if (splitArgs.Length == 2
                    && validCommands.Any(v=> v.Equals(splitArgs[1].ToLower())))
                {
                    commandValue = splitArgs[1];
                }
                else
                {
                    DisplayInvalidArgs(new List<string>() { args, string.Format("{0} must be followed by a valid command arg {1}", token, string.Join(",", validCommands.ToArray()))});
                    return false;
                }
            }
            return true;
        }

        private static void DisplayInvalidArgs(List<string> args)
        {
            if (args != null && args.Count > 0)
            {
                foreach (string a in args)
                {
                    Console.WriteLine("Invalid argument: {0}.", a);
                }
            }
            else
            {
                Console.WriteLine("Invalid argument(s).");
            }

            DisplayAnyKeyToExit();
        }
        private static void DisplayHelp()
        {
            StreamReader sr = new StreamReader(System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream("ElCamino.Identity.AzureTable.DataUtility.help.txt"));
            Console.WriteLine(sr.ReadToEndAsync().Result);

            DisplayAnyKeyToExit();
        }
    }
}
