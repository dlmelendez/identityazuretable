// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using ElCamino.AspNetCore.Identity.AzureTable;
using Microsoft.WindowsAzure.Storage.Table;
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

        private static List<string> helpTokens = new List<string>() { "/?", "/help" };
        private static string previewToken = "/preview";
        private static string migrateToken = "/migrate";
        private static string nodeleteToken = "/nodelete";
        private static string maxdegreesparallelToken = "/maxparallel:";
        private static int iMaxdegreesparallel = Environment.ProcessorCount * 2;

        private static string startPageToken = "/startpage:";
        private static string finishPageToken = "/finishpage:";
        private static string pageSizeToken = "/pagesize:";


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

            IdentityConfiguration idconfig = new IdentityConfiguration();
            idconfig.TablePrefix = Configuration.GetSection("IdentityAzureTable:IdentityConfiguration:TablePrefix").Value;
            idconfig.StorageConnectionString = Configuration.GetSection("IdentityAzureTable:IdentityConfiguration:StorageConnectionString").Value;
            idconfig.LocationMode = Configuration.GetSection("IdentityAzureTable:IdentityConfiguration:LocationMode").Value;

            Console.WriteLine("MaxDegreeOfParallelism: {0}", iMaxdegreesparallel);
            Console.WriteLine("PageSize: {0}", iPageSize);


            using (IdentityCloudContext ic = new IdentityCloudContext(idconfig))
            {
                DateTime startLoad = DateTime.UtcNow;
                var allDataList = new List<DynamicTableEntity>(iPageSize);

                TableQuery tq = new TableQuery();
                tq.SelectColumns = new List<string>() { "PartitionKey", "RowKey", "Email" };
                string partitionFilter = TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, Constants.RowKeyConstants.PreFixIdentityUserName),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThan, "V_"));
                string rowFilter = TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, Constants.RowKeyConstants.PreFixIdentityUserName),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, "V_"));
                tq.FilterString = TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);
                tq.TakeCount = iPageSize;
                TableContinuationToken continueToken = new TableContinuationToken();

                int iSkippedUserCount = 0;
                int iSkippedPageCount = 0;
                int iPageCounter = 0;
                while (continueToken != null)
                {
                    DateTime batchStart = DateTime.UtcNow;

                    var userResults = ic.UserTable.ExecuteQuerySegmentedAsync(tq, continueToken).Result;
                    continueToken = userResults.ContinuationToken;

                    List<Tuple<string, string>> userIds = userResults.Results
                        .Where(d => !string.IsNullOrWhiteSpace(d.Properties["Email"].StringValue))
                        .Select(d => new Tuple<string, string>(d.PartitionKey, d.Properties["Email"].StringValue))
                        .ToList();

                    int batchCount = userIds.Count();
                    iUserTotal += batchCount;
                    iPageCounter++;

                    bool includePage = (iStartPage == -1 || iPageCounter >= iStartPage) && (iFinishPage == -1 || iPageCounter <= iFinishPage);

                    if (includePage)
                    {
                        var result2 = Parallel.ForEach<Tuple<string, string>>(userIds, new ParallelOptions() { MaxDegreeOfParallelism = iMaxdegreesparallel }, (userId) =>
                        {

                            if (migrateOption)
                            {
                                //Add the email index
                                try
                                {
                                    IdentityUserIndex index = CreateEmailIndex(userId.Item1, userId.Item2);
                                    var r = ic.IndexTable.ExecuteAsync(TableOperation.InsertOrReplace(index)).Result;
                                    Interlocked.Increment(ref iUserSuccessConvert);
                                }
                                catch (Exception ex)
                                {
                                    userIdFailures.Add(string.Format("{0}\t{1}", userId.Item1, ex.Message));
                                    Interlocked.Increment(ref iUserFailureConvert);
                                }                               
                            }

                        });                       
                    }
                    else
                    {
                        iSkippedPageCount++;
                        iSkippedUserCount += batchCount;
                    }

                    Console.WriteLine("Page: {2}{3}, Users Batch: {1}: {0} seconds", (DateTime.UtcNow - batchStart).TotalSeconds, batchCount, iPageCounter, includePage ? string.Empty : "(Skipped)");

                    //Are we done yet?
                    if(iFinishPage > 0 && iPageCounter >= iFinishPage)
                    {
                        break;
                    }
                }


                Console.WriteLine("");
                Console.WriteLine("Elapsed time: {0} seconds", (DateTime.UtcNow - startLoad).TotalSeconds);
                Console.WriteLine("Total Users Skipped: {0}, Total Pages: {1}", iSkippedUserCount, iSkippedPageCount);
                Console.WriteLine("Total Users To Convert: {0}, Total Pages: {1}", iUserTotal - iSkippedUserCount, iPageCounter - iSkippedPageCount);

                Console.WriteLine("");
                if (migrateOption)
                {
                    Console.WriteLine("Total Users Successfully Converted: {0}", iUserSuccessConvert);
                    Console.WriteLine("Total Users Failed to Convert: {0}", iUserFailureConvert);
                    if (iUserFailureConvert > 0)
                    {
                        Console.WriteLine("User Ids Failed:");
                        foreach (string s in userIdFailures)
                        {
                            Console.WriteLine(s);
                        }
                    }
                }

            }

            DisplayAnyKeyToExit();

        }

        /// <summary>
        /// Creates an email index suitable for a crud operation
        /// </summary>
        /// <param name="userid">Formatted UserId from the KeyHelper or IdentityUser.Id.ToString()</param>
        /// <param name="email">Plain email address.</param>
        /// <returns></returns>
        private static IdentityUserIndex CreateEmailIndex(string userid, string email)
        {
            return new IdentityUserIndex()
            {
                Id = userid,
                PartitionKey = KeyHelper.GenerateRowKeyUserEmail(email),
                RowKey = userid,
                KeyVersion = KeyHelper.KeyVersion,
                ETag = Constants.ETagWildcard
            };
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
                bool isPreview = args.Any(a => a.Equals(previewToken, StringComparison.OrdinalIgnoreCase));
                bool isMigrate = args.Any(a => a.Equals(migrateToken, StringComparison.OrdinalIgnoreCase));
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
