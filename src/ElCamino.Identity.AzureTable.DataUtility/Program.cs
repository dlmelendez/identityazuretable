// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Microsoft.Extensions.Configuration;

namespace ElCamino.Identity.AzureTable.DataUtility
{
    public class Program
    {
        private static int iUserTotal = 0;
        private static int iUserSuccessConvert = 0;
        private static int iUserFailureConvert = 0;
        private static readonly ConcurrentBag<string> userIdFailures = [];

        private static readonly List<string> helpTokens = ["/?", "/help"];
        private const string PreviewToken = "/preview:";
        private const string MigrateToken = "/migrate:";
        private static readonly List<string> validCommands = [
            MigrationFactory.Roles,
            MigrationFactory.Users
        ];
        private const string NoDeleteToken = "/nodelete";
        private const string MaxDegreesParallelToken = "/maxparallel:";
        private static int iMaxdegreesparallel = Environment.ProcessorCount * 2;
        private static string MigrateCommand = string.Empty;

        private const string StartPageToken = "/startpage:";
        private const string FinishPageToken = "/finishpage:";
        private const string PageSizeToken = "/pagesize:";


        private static int iStartPage = -1;
        private static int iFinishPage = -1;
        private static int iPageSize = 1000;

        private static bool migrateOption = false;

        public static IConfigurationRoot? Configuration { get; private set; }

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
            string sourceStorageConnectionString = Configuration.GetSection("source:IdentityConfiguration:StorageConnectionString")?.Value ?? string.Empty;
            sourceConfig.UserTableName = Configuration.GetSection("source:IdentityConfiguration:UserTableName")?.Value ?? string.Empty;
            sourceConfig.IndexTableName = Configuration.GetSection("source:IdentityConfiguration:IndexTableName")?.Value ?? string.Empty;
            sourceConfig.RoleTableName = Configuration.GetSection("source:IdentityConfiguration:RoleTableName")?.Value ?? string.Empty;

            IdentityConfiguration targetConfig = new IdentityConfiguration();
            targetConfig.TablePrefix = Configuration.GetSection("target:IdentityConfiguration:TablePrefix")?.Value;
            string targetStorageConnectionString = Configuration.GetSection("target:IdentityConfiguration:StorageConnectionString")?.Value ?? string.Empty;
            targetConfig.UserTableName = Configuration.GetSection("target:IdentityConfiguration:UserTableName")?.Value ?? string.Empty;
            targetConfig.IndexTableName = Configuration.GetSection("target:IdentityConfiguration:IndexTableName")?.Value ?? string.Empty;
            targetConfig.RoleTableName = Configuration.GetSection("target:IdentityConfiguration:RoleTableName")?.Value ?? string.Empty;


            Console.WriteLine("MaxDegreeOfParallelism: {0}", iMaxdegreesparallel);
            Console.WriteLine("PageSize: {0}", iPageSize);
            Console.WriteLine("MigrateCommand: {0}", MigrateCommand);

            var migration = MigrationFactory.CreateMigration(MigrateCommand);
            IdentityCloudContext targetContext = new IdentityCloudContext(targetConfig, new TableServiceClient(targetStorageConnectionString));
            
            Task.WhenAll(targetContext.IndexTable.CreateIfNotExistsAsync(),
                        targetContext.UserTable.CreateIfNotExistsAsync(),
                        targetContext.RoleTable.CreateIfNotExistsAsync()).Wait();
            Console.WriteLine($"Target IndexTable: {targetConfig.IndexTableName}");
            Console.WriteLine($"Target UserTable: {targetConfig.UserTableName}");
            Console.WriteLine($"Target RoleTable: {targetConfig.RoleTableName}");

            string entityRecordName = "Users";

            IdentityCloudContext sourceContext = new IdentityCloudContext(sourceConfig, new TableServiceClient(sourceStorageConnectionString));
            Console.WriteLine($"Source IndexTable: {sourceConfig.IndexTableName}");
            Console.WriteLine($"Source UserTable: {sourceConfig.UserTableName}");
            Console.WriteLine($"Source RoleTable: {sourceConfig.RoleTableName}");

            DateTime startLoad = DateTime.UtcNow;
            var allDataList = new List<TableEntity>(iPageSize);

            TableQuery tq = migration.GetSourceTableQuery();

            tq.TakeCount = iPageSize;
            string? continueToken = string.Empty;

            int iSkippedUserCount = 0;
            int iSkippedPageCount = 0;
            int iPageCounter = 0;
            while (continueToken != null)
            {
                DateTime batchStart = DateTime.UtcNow;

                TableClient sourceTable = sourceContext.UserTable;
                if (MigrateCommand == MigrationFactory.Roles)
                {
                    sourceTable = sourceContext.RoleTable;
                    entityRecordName = "Role and Role Claims";
                }
                var sourceResults = sourceTable.Query<TableEntity>(tq.FilterString, tq.TakeCount).AsPages(continueToken, tq.TakeCount).FirstOrDefault();

                continueToken = sourceResults?.ContinuationToken;


                int batchCount = sourceResults?.Values.Count(migration.UserWhereFilter)??0;
                iUserTotal += batchCount;
                iPageCounter++;

                bool includePage = (iStartPage == -1 || iPageCounter >= iStartPage) && (iFinishPage == -1 || iPageCounter <= iFinishPage);

                if (includePage)
                {
                    if (migrateOption)
                    {
                        migration.ProcessMigrate(targetContext, sourceContext, sourceResults?.Values.ToList() ?? [], iMaxdegreesparallel,
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
                List<string> nonHelpTokens = [PreviewToken, MigrateToken, NoDeleteToken, MaxDegreesParallelToken, StartPageToken, FinishPageToken, PageSizeToken];
                if (!args.All(a => nonHelpTokens.Any(h => a.StartsWith(h, StringComparison.OrdinalIgnoreCase))))
                {
                    DisplayInvalidArgs(args.Where(a => !nonHelpTokens.Any(h => h.StartsWith(a, StringComparison.OrdinalIgnoreCase))).ToList());
                    return false;
                }
                bool isPreview = args.Any(a => a.StartsWith(PreviewToken, StringComparison.OrdinalIgnoreCase));
                bool isMigrate = args.Any(a => a.StartsWith(MigrateToken, StringComparison.OrdinalIgnoreCase));
                if (isPreview && isMigrate)
                {
                    DisplayInvalidArgs([PreviewToken, MigrateToken, "Cannot define /preview and /migrate. Only one can be used."]);
                    return false;
                }
                bool isNoDelete = args.Any(a => a.Equals(NoDeleteToken, StringComparison.OrdinalIgnoreCase));
                if (isNoDelete && !isMigrate)
                {
                    DisplayInvalidArgs([NoDeleteToken, "/nodelete must be used with /migrate option."]);
                    return false;
                }

                if (!ValidateIntToken(MaxDegreesParallelToken, ref iMaxdegreesparallel)
                    || !ValidateIntToken(StartPageToken, ref iStartPage)
                    || !ValidateIntToken(FinishPageToken, ref iFinishPage)
                    || !ValidateIntToken(PageSizeToken, ref iPageSize))
                    return false;

                if (isPreview)
                {
                    if (!ValidateCommandToken(PreviewToken, ref MigrateCommand))
                        return false;
                }

                if (isMigrate)
                {
                    if (!ValidateCommandToken(MigrateToken, ref MigrateCommand))
                        return false;
                }

                if (iPageSize > 1000)
                {
                    DisplayInvalidArgs([PageSizeToken, string.Format("{0} must be less than 1000", PageSizeToken)]);
                    return false;
                }
                migrateOption = isMigrate;

                return true;
            }
        }

        private static bool ValidateIntToken(string token, ref int iTokenValue)
        {
            string? args = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(token, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(args))
            {
                string[] splitArgs = args.Split(":".ToCharArray());
                if (splitArgs.Length == 2
                    && int.TryParse(splitArgs[1], out int iTempValue)
                    && iTempValue > 0)
                {
                    iTokenValue = iTempValue;
                }
                else
                {
                    DisplayInvalidArgs([args, string.Format("{0} must be followed by an int greater than 0. e.g. {0}3", token)]);
                    return false;
                }
            }
            return true;
        }

        private static bool ValidateCommandToken(string token, ref string commandValue)
        {
            string? args = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(token, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(args))
            {
                string[] splitArgs = args.Split(":".ToCharArray());
                if (splitArgs.Length == 2
                    && validCommands.Any(v => v.Equals(splitArgs[1].ToLower())))
                {
                    commandValue = splitArgs[1];
                }
                else
                {
                    DisplayInvalidArgs([args, string.Format("{0} must be followed by a valid command arg {1}", token, string.Join(",", validCommands.ToArray()))]);
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
            StreamReader sr = new StreamReader(System.Reflection.Assembly.GetEntryAssembly()!.GetManifestResourceStream("ElCamino.Identity.AzureTable.DataUtility.help.txt")!);
            Console.WriteLine(sr.ReadToEndAsync().Result);

            DisplayAnyKeyToExit();
        }
    }
}
