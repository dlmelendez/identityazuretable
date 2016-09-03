// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;

#if net45
using Microsoft.AspNet.Identity;
using Microsoft.Azure;
using ElCamino.AspNet.Identity.AzureTable;
using ElCamino.AspNet.Identity.AzureTable.Model;
#else
using Microsoft.AspNetCore.Identity;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Microsoft.AspNetCore.Builder;

#endif

using System.Collections.Generic;
using System.Security.Claims;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElCamino.Web.Identity.AzureTable.Tests.ModelTests;
using Xunit.Abstractions;
using ElCamino.Web.Identity.AzureTable.Tests.Fixtures;
using Xunit;
using System.Diagnostics;

namespace ElCamino.AspNet.Identity.AzureTable.Tests
{
    public partial class UserStoreTests : IClassFixture<UserFixture<ApplicationUser, IdentityRole, IdentityCloudContext>>
    {
#region Static and Const Members
        public static string DefaultUserPassword = "M" + Guid.NewGuid().ToString();

        private static bool tablesCreated = false;

#endregion

        private readonly ITestOutputHelper output;
        private ApplicationUser CurrentUser = null;
        private ApplicationUser CurrentEmailUser = null;
        private static UserFixture<ApplicationUser, IdentityRole, IdentityCloudContext> userFixture;

        public UserStoreTests(UserFixture<ApplicationUser, IdentityRole, IdentityCloudContext> userFix, ITestOutputHelper output)
        {
            Initialize();
            this.output = output;
            userFixture = userFix;
            userFixture.Init();
            CurrentUser = userFix.CurrentUser;
            CurrentEmailUser = userFix.CurrentEmailUser;

        }

        static UserStoreTests()
        {
            //Look out, hack!
            userFixture = new UserFixture<ApplicationUser, IdentityRole, IdentityCloudContext>();
        }

        #region Test Initialization
        public void Initialize()
        {

            //--Changes to speed up tests that don't require a new user, sharing a static user
            //--Also limiting table creation to once per test run
            if (!tablesCreated)
            {
                using (var store = userFixture.CreateUserStore())
                {
                    var taskCreateTables = store.CreateTablesIfNotExists();
                    taskCreateTables.Wait();
                }

                tablesCreated = true;
            }
            //--
        }
#endregion

        private void WriteLineObject<t> (t obj)  where t : class
        {
            output.WriteLine(typeof(t).Name);
            string strLine = obj == null ? "Null" : Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
            output.WriteLine("{0}", strLine);
        }

        private static Claim GenAdminClaim()
        {
            return new Claim(Constants.AccountClaimTypes.AccountTestAdminClaim, Guid.NewGuid().ToString());
        }

        private Claim GenAdminClaimEmptyValue()
        {
            return new Claim(Constants.AccountClaimTypes.AccountTestAdminClaim, string.Empty);
        }

        private Claim GenUserClaim()
        {
            return new Claim(Constants.AccountClaimTypes.AccountTestUserClaim, Guid.NewGuid().ToString());
        }
        private static UserLoginInfo GenGoogleLogin()
        {
#if net45
            return new UserLoginInfo(Constants.LoginProviders.GoogleProvider.LoginProvider,
                        Constants.LoginProviders.GoogleProvider.ProviderKey);
#else
            return new UserLoginInfo(Constants.LoginProviders.GoogleProvider.LoginProvider,
                        Constants.LoginProviders.GoogleProvider.ProviderKey, string.Empty);

#endif
        }

        private static ApplicationUser GenTestUser()
        {
            Guid id = Guid.NewGuid();
            ApplicationUser user = new ApplicationUser()
            {
                Email = id.ToString() + "@live.com",
                UserName = id.ToString("N"),
                LockoutEnabled = false,
                LockoutEndDateUtc = null,
                PhoneNumber = "555-555-5555",
                TwoFactorEnabled = false,
            };

            return user;
        }

        private ApplicationUser GetTestAppUser()
        {
            Guid id = Guid.NewGuid();
            ApplicationUser user = new ApplicationUser()
            {
                Email = id.ToString() + "@live.com",
                UserName = id.ToString("N"),
                LockoutEnabled = false,
                LockoutEndDateUtc = null,
                PhoneNumber = "555-555-5555",
                TwoFactorEnabled = false,
                FirstName = "Jim",
                LastName = "Bob"
            };
            return user;
        }

        [Fact(DisplayName = "UserStoreCtors")]
#if net45
        [Trait("Identity.Azure.UserStore", "")]
#else
        [Trait("Identity.Azure.UserStoreV2", "")]
#endif
        public void UserStoreCtors()
        {
            try
            {
                userFixture.CreateUserStore(null);
            }
            catch (ArgumentException) { }
        }

        [Fact(DisplayName = "CheckDupUser")]
        public void CheckDupUser()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = GenTestUser();
                    var user2 = GenTestUser();
                    var result1 = manager.CreateAsync(user).Result;
#if net45
                    Assert.True(result1.Succeeded, string.Concat(result1.Errors));
#else
                    Assert.True(result1.Succeeded, string.Concat(result1.Errors.Select(e => e.Code)));
#endif
                    user2.UserName = user.UserName;
                    var result2 = manager.CreateAsync(user2).Result;
#if net45
                    Assert.False(result2.Succeeded);
#else
                    Assert.False(result2.Succeeded);
                    Assert.True(new IdentityErrorDescriber().DuplicateUserName(user.UserName).Code 
                        == result2.Errors.First().Code);
#endif

                }
            }
        }

#if !net45

        [Fact(DisplayName = "CheckDupEmail")]
        public void CheckDupEmail()
        {
            using (var store = userFixture.CreateUserStore())
            {
                IdentityOptions options = new IdentityOptions();
                options.User.RequireUniqueEmail = true;
                using (var manager = userFixture.CreateUserManager(store, options))
                {
                    var user = GenTestUser();
                    var user2 = GenTestUser();
                    var result1 = manager.CreateAsync(user).Result;
                    Assert.True(result1.Succeeded, string.Concat(result1.Errors.Select(e => e.Code)));

                    user2.Email = user.Email;
                    var result2 = manager.CreateAsync(user2).Result;

                    Assert.False(result2.Succeeded);
                    Assert.True(new IdentityErrorDescriber().DuplicateEmail(user.Email).Code 
                        == result2.Errors.First().Code);
                }
            }
        }
#endif

        [Fact(DisplayName = "CreateUser")]
#if net45
        [Trait("Identity.Azure.UserStore", "")]
#else
        [Trait("Identity.Azure.UserStoreV2", "")]
#endif
        public void CreateUserTest()
        {
            WriteLineObject(CreateTestUser<ApplicationUser>());
        }

        public static T CreateUser<T>() where T : IdentityUser, new()
        {
            return CreateTestUser<T>();
        }

        private static T CreateTestUser<T>(bool createPassword = true, bool createEmail = true,
            string emailAddress = null) where T : IdentityUser, new()
        {
#if net45
            string strValidConnection = CloudConfigurationManager.GetSetting(
                ElCamino.AspNet.Identity.AzureTable.Constants.AppSettingsKeys.DefaultStorageConnectionStringKey);
#else
            string strValidConnection = userFixture.GetConfig().StorageConnectionString;
#endif

            using (var store = userFixture.CreateUserStore(
#if net45
                new IdentityCloudContext(strValidConnection)))
#else
                 new IdentityCloudContext(userFixture.GetConfig())))
#endif
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = GenTestUser();
                    if (!createEmail)
                    {
                        user.Email = null;
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(emailAddress))
                        {
                            user.Email = emailAddress;
                        }
                    }
                    var taskUser = createPassword ?
                        manager.CreateAsync(user, DefaultUserPassword) :
                        manager.CreateAsync(user);
                    taskUser.Wait();
                    Assert.True(taskUser.Result.Succeeded, string.Concat(taskUser.Result.Errors));

                    for (int i = 0; i < 5; i++)
                    {
                        AddUserClaimHelper(user, GenAdminClaim());
                        AddUserLoginHelper(user, GenGoogleLogin());
                        AddUserRoleHelper(user, string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N")));
                    }

                    try
                    {
                        var task = store.CreateAsync(null);
                        task.Wait();
                    }
                    catch (AggregateException agg)
                    {
                        agg.ValidateAggregateException<ArgumentException>();
                    }

                    var getUserTask = manager.FindByIdAsync(user.Id);
                    getUserTask.Wait();
                    return getUserTask.Result as T;
                }
            }
        }

        private async Task<ApplicationUser> CreateTestUserLite(bool createPassword = true, bool createEmail = true,
            string emailAddress = null)
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = GenTestUser();
                    if (!createEmail)
                    {
                        user.Email = null;
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(emailAddress))
                        {
                            user.Email = emailAddress;
                        }
                    }
                    var taskUser = createPassword ?
                        await manager.CreateAsync(user, DefaultUserPassword) :
                        await manager.CreateAsync(user);
                    output.WriteLine("User Id: {0}", user.Id);
                    Assert.True(taskUser.Succeeded, string.Concat(taskUser.Errors));

                    for (int i = 0; i < 5; i++)
                    {
                        await store.AddToRoleAsync(user, string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N")));
                        await store.AddClaimAsync(user, GenAdminClaim());
                        await store.AddLoginAsync(user, GenGoogleLogin());
                    }

                    return user;
                }
            }
        }

        [Fact(DisplayName = "DeleteUser")]
#if net45
        [Trait("Identity.Azure.UserStore", "")]
#else
        [Trait("Identity.Azure.UserStoreV2", "")]
#endif
        public void DeleteUser()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = GenTestUser();

                    var taskUser = manager.CreateAsync(user, DefaultUserPassword);
                    taskUser.Wait();
                    Assert.True(taskUser.Result.Succeeded, string.Concat(taskUser.Result.Errors));


                    for (int i = 0; i < 35; i++)
                    {
                        AddUserClaimHelper(user, GenAdminClaim());
                        AddUserLoginHelper(user, GenGoogleLogin());
                        AddUserRoleHelper(user, string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N")));
                    }

                    var findUserTask2 = manager.FindByIdAsync(user.Id);
                    findUserTask2.Wait();
                    user = findUserTask2.Result;
                    WriteLineObject<IdentityUser>(user);


                    DateTime start = DateTime.UtcNow;
                    var taskUserDel = manager.DeleteAsync(user);
                    taskUserDel.Wait();
                    Assert.True(taskUserDel.Result.Succeeded, string.Concat(taskUser.Result.Errors));
                    output.WriteLine("DeleteAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

                    Thread.Sleep(1000);

                    var findUserTask = manager.FindByIdAsync(user.Id);
                    findUserTask.Wait();
                    Assert.Null(findUserTask.Result);

                    try
                    {
                        var task = store.DeleteAsync(null);
                        task.Wait();
                    }
                    catch (AggregateException agg)
                    {
                        agg.ValidateAggregateException<ArgumentException>();
                    }
                }
            }
        }


        [Fact(DisplayName = "UpdateApplicationUser")]
#if net45
        [Trait("Identity.Azure.UserStore", "")]
#else
        [Trait("Identity.Azure.UserStoreV2", "")]
#endif
        public void UpdateApplicationUser()
        {
            using (UserStore<ApplicationUser> store = new UserStore<ApplicationUser>())
            {
                using (UserManager<ApplicationUser> manager = userFixture.CreateUserManager(store))
                {
                    var user = GetTestAppUser();
                    WriteLineObject<ApplicationUser>(user);
                    var taskUser = manager.CreateAsync(user, DefaultUserPassword);
                    taskUser.Wait();
                    Assert.True(taskUser.Result.Succeeded, string.Concat(taskUser.Result.Errors));

                    string oFirstName = user.FirstName;
                    string oLastName = user.LastName;

                    var taskFind1 = manager.FindByNameAsync(user.UserName);
                    taskFind1.Wait();
                    Assert.Equal<string>(oFirstName, taskFind1.Result.FirstName);
                    Assert.Equal<string>(oLastName, taskFind1.Result.LastName);

                    string cFirstName = string.Format("John_{0}", Guid.NewGuid());
                    string cLastName = string.Format("Doe_{0}", Guid.NewGuid());

                    user.FirstName = cFirstName;
                    user.LastName = cLastName;

                    var taskUserUpdate = manager.UpdateAsync(user);
                    taskUserUpdate.Wait();
                    Assert.True(taskUserUpdate.Result.Succeeded, string.Concat(taskUserUpdate.Result.Errors));

                    var taskFind = manager.FindByNameAsync(user.UserName);
                    taskFind.Wait();
                    Assert.Equal<string>(cFirstName, taskFind.Result.FirstName);
                    Assert.Equal<string>(cLastName, taskFind.Result.LastName);
                }
            }
        }

        [Fact(DisplayName = "UpdateUser")]
#if net45
        [Trait("Identity.Azure.UserStore", "")]
#else
        [Trait("Identity.Azure.UserStoreV2", "")]
#endif
        public void UpdateUser()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = GenTestUser();
                    WriteLineObject<IdentityUser>(user);
                    var taskUser = manager.CreateAsync(user, DefaultUserPassword);
                    taskUser.Wait();
                    Assert.True(taskUser.Result.Succeeded, string.Concat(taskUser.Result.Errors));

                    var taskUserUpdate = manager.UpdateAsync(user);
                    taskUserUpdate.Wait();
                    Assert.True(taskUserUpdate.Result.Succeeded, string.Concat(taskUserUpdate.Result.Errors));

                    try
                    {
                        store.UpdateAsync(null);
                    }
                    catch (ArgumentException) { }
                }
            }
        }

        [Fact(DisplayName = "ChangeUserName")]
#if net45
        [Trait("Identity.Azure.UserStore", "")]
#else
        [Trait("Identity.Azure.UserStoreV2", "")]
#endif
        public void ChangeUserName()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var firstUser = CreateTestUser<ApplicationUser>();
                    output.WriteLine("{0}", "Original User");
                    WriteLineObject(firstUser);
                    string originalPlainUserName = firstUser.UserName;
                    string originalUserId = firstUser.Id;
                    string userNameChange = Guid.NewGuid().ToString("N");

                    DateTime start = DateTime.UtcNow;
#if net45
                    firstUser.UserName = userNameChange;
                    var taskUserUpdate = manager.UpdateAsync(firstUser);
#else
                    var taskUserUpdate = manager.SetUserNameAsync(firstUser, userNameChange);

#endif
                    taskUserUpdate.Wait();
                    output.WriteLine("UpdateAsync(ChangeUserName): {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

                    Assert.True(taskUserUpdate.Result.Succeeded, string.Concat(taskUserUpdate.Result.Errors));
                    Task.Delay(200).Wait();
                    var taskUserChanged = manager.FindByNameAsync(userNameChange); 
                    taskUserChanged.Wait();
                    var changedUser = taskUserChanged.Result;

                    output.WriteLine("{0}", "Changed User");
                    WriteLineObject<IdentityUser>(changedUser);

                    Assert.NotNull(changedUser);
                    Assert.False(originalPlainUserName.Equals(changedUser.UserName, StringComparison.OrdinalIgnoreCase), "UserName property not updated.");

                    Assert.Equal<int>(firstUser.Roles.Count, changedUser.Roles.Count);
                    Assert.True(changedUser.Roles.All(r => r.PartitionKey == changedUser.Id.ToString()), "Roles partition keys are not equal to the new user id");

                    Assert.Equal<int>(firstUser.Claims.Count, changedUser.Claims.Count);
                    Assert.True(changedUser.Claims.All(r => r.PartitionKey == changedUser.Id.ToString()), "Claims partition keys are not equal to the new user id");

                    Assert.Equal<int>(firstUser.Logins.Count, changedUser.Logins.Count);
                    Assert.True(changedUser.Logins.All(r => r.PartitionKey == changedUser.Id.ToString()), "Logins partition keys are not equal to the new user id");

                    Assert.NotEqual<string>(originalUserId, changedUser.Id);

                    //Check email
                    var taskFindEmail = manager.FindByEmailAsync(changedUser.Email);
                    taskFindEmail.Wait();
                    Assert.NotNull(taskFindEmail.Result);

                    //Check the old username is deleted
                    var oldUserTask = manager.FindByIdAsync(originalUserId);
                    oldUserTask.Wait();
                    Assert.Null(oldUserTask.Result);

                    //Check logins
                    foreach (var log in taskFindEmail.Result.Logins)
                    {
#if net45
                        var taskFindLogin = manager.FindAsync(new UserLoginInfo(log.LoginProvider, log.ProviderKey));
#else
                        var taskFindLogin = manager.FindByLoginAsync(log.LoginProvider, log.ProviderKey);
#endif
                        taskFindLogin.Wait();
                        Assert.NotNull(taskFindLogin.Result);
                        Assert.NotEqual<string>(originalUserId, taskFindLogin.Result.Id.ToString());
                    }

                    try
                    {
                        store.UpdateAsync(null);
                    }
                    catch (ArgumentException) { }
                }
            }
        }

        [Fact(DisplayName = "FindUserByEmail")]
#if net45
        [Trait("Identity.Azure.UserStore", "")]
#else
        [Trait("Identity.Azure.UserStoreV2", "")]
#endif
        public void FindUserByEmail()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = CreateUser<ApplicationUser>();
                    WriteLineObject<IdentityUser>(user);

                    DateTime start = DateTime.UtcNow;
                    var findUserTask = manager.FindByEmailAsync(user.Email);
                    findUserTask.Wait();
                    output.WriteLine("FindByEmailAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

                    Assert.Equal<string>(user.Email, findUserTask.Result.Email);
                }
            }
        }

        [Fact(DisplayName = "FindUsersByEmail")]
#if net45
        [Trait("Identity.Azure.UserStore", "")]
#else
        [Trait("Identity.Azure.UserStoreV2", "")]
#endif
        public void FindUsersByEmail()
        {
            string strEmail = Guid.NewGuid().ToString() + "@live.com";

            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    int createdCount = 51;
                    for (int i = 0; i < createdCount; i++)
                    {
                        var task = CreateTestUserLite(true, true, strEmail);
                        task.Wait();
                    }

                    DateTime start = DateTime.UtcNow;
                    output.WriteLine("FindAllByEmailAsync: {0}", strEmail);

                    var findUserTask = store.FindAllByEmailAsync(strEmail);
                    findUserTask.Wait();
                    output.WriteLine("FindAllByEmailAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
                    output.WriteLine("Users Found: {0}", findUserTask.Result.Count());
                    Assert.Equal<int>(createdCount, findUserTask.Result.Count());

                    var listCreated = findUserTask.Result.ToList();

                    //Change email and check results
                    string strEmailChanged = Guid.NewGuid().ToString() + "@live.com";
                    var userToChange = listCreated.Last();
#if net45
                    manager.SetEmailAsync(userToChange.Id, strEmailChanged).Wait();
#else
                    manager.SetEmailAsync(userToChange, strEmailChanged).Wait();
#endif

                    var findUserChanged = manager.FindByEmailAsync(strEmailChanged);
                    findUserChanged.Wait();
                    Assert.Equal<string>(userToChange.Id, findUserChanged.Result.Id);
                    Assert.NotEqual<string>(strEmail, findUserChanged.Result.Email);


                    //Make sure changed user doesn't show up in previous query
                    start = DateTime.UtcNow;

                    findUserTask = store.FindAllByEmailAsync(strEmail);
                    findUserTask.Wait();
                    output.WriteLine("FindAllByEmailAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
                    output.WriteLine("Users Found: {0}", findUserTask.Result.Count());
                    Assert.Equal<int>(listCreated.Count - 1, findUserTask.Result.Count());


                }
            }
        }

        [Fact(DisplayName = "FindUserById")]
#if net45
        [Trait("Identity.Azure.UserStore", "")]
#else
        [Trait("Identity.Azure.UserStoreV2", "")]
#endif
        public void FindUserById()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = CurrentUser;
                    DateTime start = DateTime.UtcNow;
                    var findUserTask = manager.FindByIdAsync(user.Id);
                    findUserTask.Wait();
                    output.WriteLine("FindByIdAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

                    Assert.Equal<string>(user.Id, findUserTask.Result.Id);
                }
            }
        }

        [Fact(DisplayName = "FindUserByName")]
        [Trait("Identity.Azure.UserStore", "")]
        public void FindUserByName()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = CurrentUser;
                    WriteLineObject<IdentityUser>(user);
                    DateTime start = DateTime.UtcNow;
                    var findUserTask = manager.FindByNameAsync(user.UserName);
                    findUserTask.Wait();
                    output.WriteLine("FindByNameAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

                    Assert.Equal<string>(user.UserName, findUserTask.Result.UserName);
                }
            }
        }

        [Fact(DisplayName = "AddUserLogin")]
#if net45
        [Trait("Identity.Azure.UserStore", "")]
#else
        [Trait("Identity.Azure.UserStoreV2", "")]
#endif
        public void AddUserLogin()
        {
            var user = CreateTestUser<ApplicationUser>(false);
            WriteLineObject(user);
            AddUserLoginHelper(user, GenGoogleLogin());
        }

        public static void AddUserLoginHelper(ApplicationUser user, UserLoginInfo loginInfo)
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
#if net45
                    var userAddLoginTask = manager.AddLoginAsync(user.Id, loginInfo);
#else
                     var userAddLoginTask = manager.AddLoginAsync(user, loginInfo);
#endif
                    userAddLoginTask.Wait();
                    Assert.True(userAddLoginTask.Result.Succeeded, string.Concat(userAddLoginTask.Result.Errors));

#if net45
                    var loginGetTask = manager.GetLoginsAsync(user.Id);
#else
                    var loginGetTask = manager.GetLoginsAsync(user);
#endif

                    loginGetTask.Wait();
                    Assert.True(loginGetTask.Result
                        .Any(log => log.LoginProvider == loginInfo.LoginProvider
                            & log.ProviderKey == loginInfo.ProviderKey), "LoginInfo not found: GetLoginsAsync");

                    //DateTime start = DateTime.UtcNow;
#if net45
                    var loginGetTask2 = manager.FindAsync(loginGetTask.Result.First());
#else
                    var loginGetTask2 = manager.FindByLoginAsync(loginGetTask.Result.First().LoginProvider, loginGetTask.Result.First().ProviderKey);
#endif

                    loginGetTask2.Wait();
                    //output.WriteLine("FindAsync(By Login): {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
                    Assert.NotNull(loginGetTask2.Result);

                }
            }
        }


        [Fact(DisplayName = "AddRemoveUserLogin")]
#if net45
        [Trait("Identity.Azure.UserStore", "")]
#else
        [Trait("Identity.Azure.UserStoreV2", "")]
#endif
        public void AddRemoveUserLogin()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = GenTestUser();
                    WriteLineObject<IdentityUser>(user);
                    var taskUser = manager.CreateAsync(user, DefaultUserPassword);
                    taskUser.Wait();
                    Assert.True(taskUser.Result.Succeeded, string.Concat(taskUser.Result.Errors));

                    var loginInfo = GenGoogleLogin();

#if net45
                    var userAddLoginTask = manager.AddLoginAsync(user.Id, loginInfo);
#else
                    var userAddLoginTask = manager.AddLoginAsync(user, loginInfo);
#endif

                    userAddLoginTask.Wait();
                    Assert.True(userAddLoginTask.Result.Succeeded, string.Concat(userAddLoginTask.Result.Errors));

#if net45
                    var loginGetTask = manager.GetLoginsAsync(user.Id);
#else
                    var loginGetTask = manager.GetLoginsAsync(user);
#endif

                    loginGetTask.Wait();
                    Assert.True(loginGetTask.Result
                        .Any(log=> log.LoginProvider == loginInfo.LoginProvider
                            & log.ProviderKey == loginInfo.ProviderKey), "LoginInfo not found: GetLoginsAsync");

#if net45
                    var loginGetTask2 = manager.FindAsync(loginGetTask.Result.First());
#else
                    var loginGetTask2 = manager.FindByLoginAsync(loginGetTask.Result.First().LoginProvider, loginGetTask.Result.First().ProviderKey);
#endif

                    loginGetTask2.Wait();
                    Assert.NotNull(loginGetTask2.Result);

#if net45
                    var userRemoveLoginTaskNeg1 = manager.RemoveLoginAsync(user.Id, new UserLoginInfo(string.Empty, loginInfo.ProviderKey));
#else
                    var userRemoveLoginTaskNeg1 = manager.RemoveLoginAsync(user, string.Empty, loginInfo.ProviderKey);
#endif

                    userRemoveLoginTaskNeg1.Wait();

#if net45
                    var userRemoveLoginTaskNeg2 = manager.RemoveLoginAsync(user.Id, new UserLoginInfo(loginInfo.LoginProvider, string.Empty));
#else
                    var userRemoveLoginTaskNeg2 = manager.RemoveLoginAsync(user, loginInfo.LoginProvider, string.Empty);
#endif

                    userRemoveLoginTaskNeg2.Wait();

#if net45
                    var userRemoveLoginTask = manager.RemoveLoginAsync(user.Id, loginInfo);
#else
                    var userRemoveLoginTask = manager.RemoveLoginAsync(user, loginInfo.LoginProvider, loginInfo.ProviderKey);
#endif

                    userRemoveLoginTask.Wait();
                    Assert.True(userRemoveLoginTask.Result.Succeeded, string.Concat(userRemoveLoginTask.Result.Errors));
#if net45
                    var loginGetTask3 = manager.GetLoginsAsync(user.Id);
#else
                    var loginGetTask3 = manager.GetLoginsAsync(user);
#endif

                    loginGetTask3.Wait();
                    Assert.True(!loginGetTask3.Result.Any(), "LoginInfo not removed");

                    //Negative cases

#if net45
                    var loginFindNeg = manager.FindAsync(new UserLoginInfo("asdfasdf", "http://4343443dfaksjfaf"));
#else
                    var loginFindNeg = manager.FindByLoginAsync("asdfasdf", "http://4343443dfaksjfaf");
#endif

                    loginFindNeg.Wait();
                    Assert.Null(loginFindNeg.Result);

                    try
                    {
                        store.AddLoginAsync(null, loginInfo);
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.AddLoginAsync(user, null);
                    }
                    catch (ArgumentException) { }

                    try
                    {
#if net45
                        store.RemoveLoginAsync(null, loginInfo);
#else
                        store.RemoveLoginAsync(null, loginInfo.ProviderKey, loginInfo.LoginProvider);
#endif

                    }
                    catch (ArgumentException) { }

                    try
                    {
#if net45
                        store.RemoveLoginAsync(user, null);
#else
                        store.RemoveLoginAsync(user, null, null);
#endif
                    }
                    catch (ArgumentException) { }

                    try
                    {
#if net45
                        store.FindAsync(null);
#else
                        store.FindByLoginAsync(null, null);
#endif

                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.GetLoginsAsync(null);
                    }
                    catch (ArgumentException) { }
                }
            }
        }

        [Fact(DisplayName = "AddUserRole")]
#if net45
        [Trait("Identity.Azure.UserStore", "")]
#else
        [Trait("Identity.Azure.UserStoreV2", "")]
#endif
        public void AddUserRole()
        {
            string strUserRole = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"));
            WriteLineObject<IdentityUser>(CurrentUser);
            AddUserRoleHelper(CurrentUser, strUserRole);
        }

#if !net45
        [Fact(DisplayName = "GetUsersByRole")]
        [Trait("Identity.Azure.UserStoreV2", "")]
        public void GetUsersByRole()
        {
            string strUserRole = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"));
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    int userCount = 4;
                    DateTime start2 = DateTime.UtcNow;
                    ApplicationUser tempUser = null;
                    for (int i = 0; i < userCount; i++)
                    {
                        DateTime start = DateTime.UtcNow;
                        output.WriteLine("CreateTestUserLite()");
                        tempUser = CreateTestUserLite(true, true).Result;
                        output.WriteLine("CreateTestUserLite(): {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
                        AddUserRoleHelper(tempUser, strUserRole);
                    }
                    output.WriteLine("GenerateUsers(): {0} user count", userCount);
                    output.WriteLine("GenerateUsers(): {0} seconds", (DateTime.UtcNow - start2).TotalSeconds);

                    var users = manager.GetUsersInRoleAsync(strUserRole).Result;
                    Assert.Equal(users.Count(), userCount);
                }
            }
        }
#endif

        public static void AddUserRoleHelper(ApplicationUser user, string roleName)
        {
            using (RoleStore<IdentityRole> rstore = userFixture.CreateRoleStore())
            {
                var userRole = rstore.FindByNameAsync(roleName);
                userRole.Wait();

                if (userRole.Result == null)
                {
                    var taskUser = rstore.CreateAsync(new IdentityRole(roleName));
                    taskUser.Wait();
                }
            }

            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
#if net45
                    var userRoleTask = manager.AddToRoleAsync(user.Id, roleName);
#else
                    var userRoleTask = manager.AddToRoleAsync(user, roleName);
#endif

                    userRoleTask.Wait();
                    Assert.True(userRoleTask.Result.Succeeded, string.Concat(userRoleTask.Result.Errors));

#if net45
                    var roles2Task = manager.IsInRoleAsync(user.Id, roleName);
#else
                    var roles2Task = manager.IsInRoleAsync(user, roleName);
#endif
                    roles2Task.Wait();
                    Assert.True(roles2Task.Result, "Role not found");

                }
            }
        }

        [Fact(DisplayName = "AddRemoveUserRole")]
#if net45
        [Trait("Identity.Azure.UserStore", "")]
#else
        [Trait("Identity.Azure.UserStoreV2", "")]
#endif
        public void AddRemoveUserRole()
        {
            string roleName = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestAdminRole, Guid.NewGuid().ToString("N"));

            using (RoleStore<IdentityRole> rstore = userFixture.CreateRoleStore())
            {
                var taskAdmin = rstore.CreateAsync(new IdentityRole(roleName));
                taskAdmin.Wait();
                var adminRole = rstore.FindByNameAsync(roleName);
                adminRole.Wait();
            }

            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = CurrentUser;
                    WriteLineObject(user);
#if net45
                    var userRoleTask = manager.AddToRoleAsync(user.Id, roleName);
#else
                    var userRoleTask = manager.AddToRoleAsync(user, roleName);
#endif

                    userRoleTask.Wait();
                    Assert.True(userRoleTask.Result.Succeeded, string.Concat(userRoleTask.Result.Errors));
                    DateTime getRolesStart = DateTime.UtcNow;
#if net45
                    var rolesTask = manager.GetRolesAsync(user.Id);
#else
                    var rolesTask = manager.GetRolesAsync(user);
#endif

                    rolesTask.Wait();
                    var getout = string.Format("{0} ms", (DateTime.UtcNow - getRolesStart).TotalMilliseconds);
                    Debug.WriteLine(getout);
                    output.WriteLine(getout);
                    Assert.True(rolesTask.Result.Contains(roleName), "Role not found");

                    DateTime isInRolesStart = DateTime.UtcNow;

#if net45
                    var roles2Task = manager.IsInRoleAsync(user.Id, roleName);
#else
                    var roles2Task = manager.IsInRoleAsync(user, roleName);
#endif

                    roles2Task.Wait();
                    var isInout = string.Format("{0} ms", (DateTime.UtcNow - isInRolesStart).TotalMilliseconds);
                    Debug.WriteLine(isInout);
                    output.WriteLine(isInout);
                    Assert.True(roles2Task.Result, "Role not found");

#if net45
                    var userRemoveTask = manager.RemoveFromRoleAsync(user.Id, roleName);
#else
                    var userRemoveTask = manager.RemoveFromRoleAsync(user, roleName);
#endif

                    userRemoveTask.Wait();
#if net45
                    var rolesTask2 = manager.GetRolesAsync(user.Id);
#else
                    var rolesTask2 = manager.GetRolesAsync(user);
#endif

                    rolesTask2.Wait();
                    Assert.False(rolesTask2.Result.Contains(roleName), "Role not removed.");

                    try
                    {
                        store.AddToRoleAsync(null, roleName);
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.AddToRoleAsync(user, null);
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.AddToRoleAsync(user, Guid.NewGuid().ToString());
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.RemoveFromRoleAsync(null, roleName);
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.RemoveFromRoleAsync(user, null);
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.GetRolesAsync(null);
                    }
                    catch (ArgumentException) { }

                }
            }
        }

        [Fact(DisplayName = "IsUserInRole")]
#if net45
        [Trait("Identity.Azure.UserStore", "")]
#else
        [Trait("Identity.Azure.UserStoreV2", "")]
#endif
        public void IsUserInRole()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = CurrentUser;
                    WriteLineObject(user);
                    string roleName = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"));

                    AddUserRoleHelper(user, roleName);

                    DateTime start = DateTime.UtcNow;
#if net45
                    var roles2Task = manager.IsInRoleAsync(user.Id, roleName);
#else
                    var roles2Task = manager.IsInRoleAsync(user, roleName);
#endif

                    roles2Task.Wait();
                    output.WriteLine("IsInRoleAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
                    Assert.True(roles2Task.Result, "Role not found");

                   
                    try
                    {
                        store.IsInRoleAsync(null, roleName);
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.IsInRoleAsync(user, null);
                    }
                    catch (ArgumentException) { }
                }
            }
        }

        [Fact(DisplayName = "GenerateUsers", Skip = "true")]
#if net45
        [Trait("Identity.Azure.UserStore", "")]
#else
        [Trait("Identity.Azure.UserStoreV2", "")]
#endif
        public async Task GenerateUsers()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    int userCount = 1000;
                    DateTime start2 = DateTime.UtcNow;
                    for (int i = 0; i < userCount; i++)
                    {
                        DateTime start = DateTime.UtcNow;
                        output.WriteLine("CreateTestUserLite()");
                        await CreateTestUserLite(true, true);
                        output.WriteLine("CreateTestUserLite(): {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
                    }
                    output.WriteLine("GenerateUsers(): {0} user count", userCount);
                    output.WriteLine("GenerateUsers(): {0} seconds", (DateTime.UtcNow - start2).TotalSeconds);

                }
            }
        }

        [Fact(DisplayName = "AddUserClaim")]
#if net45
        [Trait("Identity.Azure.UserStore", "")]
#else
        [Trait("Identity.Azure.UserStoreV2", "")]
#endif
        public void AddUserClaim()
        {
            WriteLineObject<IdentityUser>(CurrentUser);
            AddUserClaimHelper(CurrentUser, GenUserClaim());
        }

        private static void AddUserClaimHelper(ApplicationUser user, Claim claim)
        {
            using (var store = userFixture.CreateUserStore(new IdentityCloudContext()))
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
#if net45
                    var userClaimTask = manager.AddClaimAsync(user.Id, claim);
#else
                    var userClaimTask = manager.AddClaimAsync(user, claim);
#endif

                    userClaimTask.Wait();
#if net45
                    Assert.True(userClaimTask.Result.Succeeded, string.Concat(userClaimTask.Result.Errors));
#else
                    Assert.True(userClaimTask.Result.Succeeded, string.Concat(userClaimTask.Result.Errors.Select(e => e.Code)));
#endif                             
#if net45
                    var claimsTask = manager.GetClaimsAsync(user.Id);
#else
                    var claimsTask = manager.GetClaimsAsync(user);
#endif

                    claimsTask.Wait();
                    Assert.True(claimsTask.Result.Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not found");
                }
            }

        }

        #if !net45
        [Fact(DisplayName = "GetUsersByClaim")]
        [Trait("Identity.Azure.UserStoreV2", "")]
        public void GetUsersByClaim()
        {
            var claim = GenUserClaim();
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    int userCount = 101;
                    DateTime start2 = DateTime.UtcNow;
                    ApplicationUser tempUser = null;
                    for (int i = 0; i < userCount; i++)
                    {
                        DateTime start = DateTime.UtcNow;
                        output.WriteLine("CreateTestUserLite()");
                        tempUser = CreateTestUserLite(true, true).Result;
                        output.WriteLine("CreateTestUserLite(): {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
                        AddUserClaimHelper(tempUser, claim);
                    }
                    output.WriteLine("GenerateUsers(): {0} user count", userCount);
                    output.WriteLine("GenerateUsers(): {0} seconds", (DateTime.UtcNow - start2).TotalSeconds);

                    var users = manager.GetUsersForClaimAsync(claim).Result;
                    Assert.Equal(users.Count(), userCount);
                }
            }
        }
#endif

        [Fact(DisplayName = "AddRemoveUserClaim")]
#if net45
        [Trait("Identity.Azure.UserStore", "")]
#else
        [Trait("Identity.Azure.UserStoreV2", "")]
#endif
        public void AddRemoveUserClaim()
        {
            using (var store = userFixture.CreateUserStore(new IdentityCloudContext()))
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = CurrentUser;
                    WriteLineObject<IdentityUser>(user);
                    Claim claim = GenAdminClaim();
#if net45
                    var userClaimTask = manager.AddClaimAsync(user.Id, claim);
#else
                    var userClaimTask = manager.AddClaimAsync(user, claim);
#endif

                    userClaimTask.Wait();
                    Assert.True(userClaimTask.Result.Succeeded, string.Concat(userClaimTask.Result.Errors));
#if net45
                    var claimsTask = manager.GetClaimsAsync(user.Id);
#else
                    var claimsTask = manager.GetClaimsAsync(user);
#endif

                    claimsTask.Wait();
                    Assert.True(claimsTask.Result.Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not found");


#if net45
                    var userRemoveClaimTask = manager.RemoveClaimAsync(user.Id, claim);
#else
                    var userRemoveClaimTask = manager.RemoveClaimAsync(user, claim);
#endif

                    userRemoveClaimTask.Wait();
                    Assert.True(userClaimTask.Result.Succeeded, string.Concat(userClaimTask.Result.Errors));
#if net45
                    var claimsTask2 = manager.GetClaimsAsync(user.Id);
#else
                    var claimsTask2 = manager.GetClaimsAsync(user);
#endif

                    claimsTask2.Wait();
                    Assert.True(!claimsTask2.Result.Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not removed");

                    //adding test for removing an empty claim
                    Claim claimEmpty = GenAdminClaimEmptyValue();
#if net45
                    var userClaimTask2 = manager.AddClaimAsync(user.Id, claimEmpty);
#else
                    var userClaimTask2 = manager.AddClaimAsync(user, claimEmpty);
#endif

                    userClaimTask2.Wait();

#if net45
                    var userRemoveClaimTask2 = manager.RemoveClaimAsync(user.Id, claimEmpty);
#else
                    var userRemoveClaimTask2 = manager.RemoveClaimAsync(user, claimEmpty);
#endif

                    userRemoveClaimTask2.Wait();
                    Assert.True(userClaimTask2.Result.Succeeded, string.Concat(userClaimTask2.Result.Errors));

                    try
                    {
                        var task = store.AddClaimAsync(null, claim);
                    }
                    catch (ArgumentNullException) { }

                    try
                    {
                        store.AddClaimAsync(user, null);
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.RemoveClaimAsync(null, claim);
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.RemoveClaimAsync(user, null);
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.RemoveClaimAsync(user, new Claim(string.Empty, Guid.NewGuid().ToString()));
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.RemoveClaimAsync(user, new Claim(claim.Type, null));
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.GetClaimsAsync(null);
                    }
                    catch (ArgumentException) { }
                }
            }
        }

        [Fact(DisplayName = "ThrowIfDisposed")]
#if net45
        [Trait("Identity.Azure.UserStore", "")]
#else
        [Trait("Identity.Azure.UserStoreV2", "")]
#endif
        public void ThrowIfDisposed()
        {
            var store = userFixture.CreateUserStore();
            store.Dispose();
            GC.Collect();
            try
            {
                var task = store.DeleteAsync(null);
            }
            catch (AggregateException agg)
            {
                agg.ValidateAggregateException<ArgumentException>();
            }
        }

    }
}
