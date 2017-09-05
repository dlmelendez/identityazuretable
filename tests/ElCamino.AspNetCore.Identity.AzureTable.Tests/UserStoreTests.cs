// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Xunit;
using Xunit.Abstractions;
using ElCamino.AspNetCore.Identity.AzureTable;
using IdentityUser = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityUser;
using IdentityRole = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityRole;
using ElCamino.Web.Identity.AzureTable.Tests.ModelTests;
using ElCamino.Web.Identity.AzureTable.Tests.Fixtures;
using Microsoft.AspNetCore.Identity;

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests
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

        private void WriteLineObject<t>(t obj) where t : class
        {
            output.WriteLine(typeof(t).Name);
            string strLine = obj == null ? "Null" : Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
            output.WriteLine("{0}", strLine);
        }

        private static string GenClaimValue()
        {
            string strValue = "EAABdGMzTwv8BALkEPOvXXRz3LNo6l77ymX75OO3gzadoxYLOs7KrMBi2zdjqULQ2CJAGUgwwsEGRFo2XIp0JPvoJEvaQdXgXZB1UzX8plkawZAdC93btP6lZBHettE0kfB91RODbWaj1aJbr3ejytBKq7vyP4mWq8lA9DWzCgZDZD";
            string strGuid = Guid.NewGuid().ToString("N");
            return strValue + strGuid;
        }
        private static Claim GenAdminClaim()
        {
            return new Claim(Constants.AccountClaimTypes.AccountTestAdminClaim, GenClaimValue());
        }

        private Claim GenAdminClaimEmptyValue()
        {
            return new Claim(Constants.AccountClaimTypes.AccountTestAdminClaim, string.Empty);
        }

        private Claim GenUserClaim()
        {
            return new Claim(Constants.AccountClaimTypes.AccountTestUserClaim, GenClaimValue());
        }

        private static UserLoginInfo GenGoogleLogin()
         => new UserLoginInfo(Constants.LoginProviders.GoogleProvider.LoginProvider,
                              Constants.LoginProviders.GoogleProvider.ProviderKey, string.Empty);

        private static ApplicationUser GenTestUser()
        {
            Guid id = Guid.NewGuid();
            ApplicationUser user = new ApplicationUser()
            {
                Email = id.ToString() + "@live.com",
                UserName = id.ToString("N"),
                LockoutEnabled = false,
                LockoutEnd = null,
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
                LockoutEnd = null,
                PhoneNumber = "555-555-5555",
                TwoFactorEnabled = false,
                FirstName = "Jim",
                LastName = "Bob"
            };
            return user;
        }

        [Fact(DisplayName = "UserStoreCtors")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public void UserStoreCtors()
        {
            Assert.Throws<ArgumentNullException>(() => userFixture.CreateUserStore(null));
        }

        [Trait("IdentityCore.Azure.UserStore", "")]
        [Fact(DisplayName = "CheckDupUser")]
        public async Task CheckDupUser()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = GenTestUser();
                    var user2 = GenTestUser();
                    var result = await manager.CreateAsync(user);
                    Assert.True(result.Succeeded, string.Concat(result.Errors.Select(e => e.Code)));

                    user2.UserName = user.UserName;
                    var result2 = await manager.CreateAsync(user2);
                    Assert.False(result2.Succeeded);
                    Assert.True(new IdentityErrorDescriber().DuplicateUserName(user.UserName).Code
                        == result2.Errors.First().Code);
                }
            }
        }

        [Fact(DisplayName = "CheckDupEmail")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public async Task CheckDupEmail()
        {
            using (var store = userFixture.CreateUserStore())
            {
                IdentityOptions options = new IdentityOptions();
                options.User.RequireUniqueEmail = true;
                using (var manager = userFixture.CreateUserManager(store, options))
                {
                    var user = GenTestUser();
                    var user2 = GenTestUser();
                    var result1 = await manager.CreateAsync(user);
                    Assert.True(result1.Succeeded, string.Concat(result1.Errors.Select(e => e.Code)));

                    user2.Email = user.Email;
                    var result2 = await manager.CreateAsync(user2);

                    Assert.False(result2.Succeeded);
                    Assert.True(new IdentityErrorDescriber().DuplicateEmail(user.Email).Code
                        == result2.Errors.First().Code);
                }
            }
        }

        [Fact(DisplayName = "CreateUser")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public void CreateUserTest()
        {
            WriteLineObject(CreateTestUserAsync<ApplicationUser>());
        }

        public static Task<T> CreateUserAsync<T>() where T : Model.IdentityUser, new()
         => CreateTestUserAsync<T>();

        private static async Task<T> CreateTestUserAsync<T>(bool createPassword = true, bool createEmail = true,
            string emailAddress = null) where T : Model.IdentityUser, new()
        {
            string strValidConnection = userFixture.GetConfig().StorageConnectionString;

            using (var store = userFixture.CreateUserStore(
                 new IdentityCloudContext(userFixture.GetConfig())))
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
                    var createUserResult = await (createPassword ?
                        manager.CreateAsync(user, DefaultUserPassword) :
                        manager.CreateAsync(user));
                    Assert.True(createUserResult.Succeeded, string.Concat(createUserResult.Errors));

                    for (int i = 0; i < 5; i++)
                    {
                        await AddUserClaimHelper(user, GenAdminClaim());
                        await AddUserLoginAsyncHelper(user, GenGoogleLogin());
                        await AddUserRoleAsyncHelper(user, string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N")));
                    }

                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.CreateAsync(null));

                    var getUserResult = await manager.FindByIdAsync(user.Id);
                    return getUserResult as T;
                }
            }
        }

        private async Task<ApplicationUser> CreateTestUserLiteAsync(bool createPassword = true, bool createEmail = true,
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
        [Trait("IdentityCore.Azure.UserStore", "")]
        public async Task DeleteUser()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = GenTestUser();

                    var userCreationResult = await manager.CreateAsync(user, DefaultUserPassword);
                    Assert.True(userCreationResult.Succeeded, string.Concat(userCreationResult.Errors));

                    for (int i = 0; i < 35; i++)
                    {
                        await AddUserClaimHelper(user, GenAdminClaim());
                        await AddUserLoginAsyncHelper(user, GenGoogleLogin());
                        await AddUserRoleAsyncHelper(user, string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N")));
                    }

                    user = await manager.FindByIdAsync(user.Id);
                    WriteLineObject(user);

                    var sw = new Stopwatch();
                    sw.Start();
                    var userDelitionResult = await manager.DeleteAsync(user);
                    sw.Stop();
                    Assert.True(userDelitionResult.Succeeded, string.Concat(userCreationResult.Errors));
                    output.WriteLine("DeleteAsync: {0} seconds", sw.Elapsed.TotalSeconds);

                    await Task.Delay(2000);

                    var user2 = await manager.FindByIdAsync(user.Id);
                    Assert.Null(user2);

                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.DeleteAsync(null));
                }
            }
        }

        [Fact(DisplayName = "UpdateApplicationUser")]
        [Trait("IdentityCore.Azure.UserStore", "")]
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
        [Trait("IdentityCore.Azure.UserStore", "")]
        public async Task UpdateUser()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = GenTestUser();
                    WriteLineObject<IdentityUser>(user);
                    var creationResult = await manager.CreateAsync(user, DefaultUserPassword);
                    Assert.True(creationResult.Succeeded, string.Concat(creationResult.Errors));

                    var updationResult = await manager.UpdateAsync(user);
                    Assert.True(updationResult.Succeeded, string.Concat(updationResult.Errors));

                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.UpdateAsync(null));
                }
            }
        }

        [Fact(DisplayName = "ChangeUserName")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public async Task ChangeUserName()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var firstUser = await CreateTestUserAsync<ApplicationUser>();
                    output.WriteLine("{0}", "Original User");
                    WriteLineObject(firstUser);
                    string originalPlainUserName = firstUser.UserName;
                    string originalUserId = firstUser.Id;
                    string userNameChange = Guid.NewGuid().ToString("N");

                    var sw = new Stopwatch();
                    sw.Start();
                    var userUpdate = await manager.SetUserNameAsync(firstUser, userNameChange);
                    sw.Stop();
                    output.WriteLine("UpdateAsync(ChangeUserName): {0} seconds", sw.Elapsed.TotalSeconds);
                    Assert.True(userUpdate.Succeeded, string.Concat(userUpdate.Errors));

                    await Task.Delay(200);
                    var userChangedResult = await manager.FindByNameAsync(userNameChange);
                    var changedUser = userChangedResult;
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
                    var findEmailResult = await manager.FindByEmailAsync(changedUser.Email);
                    Assert.NotNull(findEmailResult);

                    //Check the old username is deleted
                    var oldUser = await manager.FindByIdAsync(originalUserId);
                    Assert.Null(oldUser);

                    //Check logins
                    foreach (var log in findEmailResult.Logins)
                    {
                        var findLoginResult = await manager.FindByLoginAsync(log.LoginProvider, log.ProviderKey);
                        Assert.NotNull(findLoginResult);
                        Assert.NotEqual<string>(originalUserId, findLoginResult.Id.ToString());
                    }

                    await Assert.ThrowsAsync<ArgumentNullException>(()=>store.UpdateAsync(null));
                }
            }
        }

        [Fact(DisplayName = "FindUserByEmail")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public async Task FindUserByEmail()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = await CreateUserAsync<ApplicationUser>();
                    WriteLineObject<IdentityUser>(user);

                    var sw = new Stopwatch();
                    sw.Start();
                    var findUserResult = await manager.FindByEmailAsync(user.Email);
                    sw.Stop();
                    output.WriteLine("FindByEmailAsync: {0} seconds", sw.Elapsed.TotalSeconds);

                    Assert.Equal<string>(user.Email, findUserResult.Email);
                }
            }
        }

        [Fact(DisplayName = "FindUsersByEmail")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public async Task FindUsersByEmail()
        {
            string strEmail = Guid.NewGuid().ToString() + "@live.com";

            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    int createdCount = 51;
                    for (int i = 0; i < createdCount; i++)
                    {
                        await CreateTestUserLiteAsync(true, true, strEmail);
                    }

                    output.WriteLine("FindAllByEmailAsync: {0}", strEmail);
                    var sw = new Stopwatch();
                    sw.Start();
                    var allResult = await store.FindAllByEmailAsync(strEmail);
                    sw.Stop();
                    output.WriteLine("FindAllByEmailAsync: {0} seconds", sw.Elapsed.TotalSeconds);
                    output.WriteLine("Users Found: {0}", allResult.Count());
                    Assert.Equal<int>(createdCount, allResult.Count());

                    var listCreated = allResult.ToList();

                    //Change email and check results
                    string strEmailChanged = Guid.NewGuid().ToString() + "@live.com";
                    var userToChange = listCreated.Last();
                    await manager.SetEmailAsync(userToChange, strEmailChanged);
                    var changedResult = await manager.FindByEmailAsync(strEmailChanged);
                    Assert.Equal<string>(userToChange.Id, changedResult.Id);
                    Assert.NotEqual<string>(strEmail, changedResult.Email);

                    //Make sure changed user doesn't show up in previous query
                    sw.Restart();

                    allResult = await store.FindAllByEmailAsync(strEmail);
                    output.WriteLine("FindAllByEmailAsync: {0} seconds", sw.Elapsed.TotalSeconds);
                    output.WriteLine("Users Found: {0}", allResult.Count());
                    Assert.Equal<int>(listCreated.Count - 1, allResult.Count());
                }
            }
        }

        [Fact(DisplayName = "FindUserById")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public async Task FindUserById()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = CurrentUser;
                    var sw = new Stopwatch();
                    sw.Start();
                    var result = await manager.FindByIdAsync(user.Id);
                    sw.Stop();
                    output.WriteLine("FindByIdAsync: {0} seconds", sw.Elapsed.TotalSeconds);

                    Assert.Equal<string>(user.Id, result.Id);
                }
            }
        }

        [Fact(DisplayName = "FindUserByName")]
        [Trait("Identity.Azure.UserStore", "")]
        public async Task FindUserByName()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = CurrentUser;
                    WriteLineObject<IdentityUser>(user);
                    var sw = new Stopwatch();
                    sw.Start();
                    var result = await manager.FindByNameAsync(user.UserName);
                    sw.Stop();
                    output.WriteLine("FindByNameAsync: {0} seconds", sw.Elapsed.TotalSeconds);

                    Assert.Equal<string>(user.UserName, result.UserName);
                }
            }
        }

        [Fact(DisplayName = "AddUserLogin")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public async Task AddUserLogin()
        {
            var user = await CreateTestUserAsync<ApplicationUser>(false);
            WriteLineObject(user);
            await AddUserLoginAsyncHelper(user, GenGoogleLogin());
        }

        public static async Task AddUserLoginAsyncHelper(ApplicationUser user, UserLoginInfo loginInfo)
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var loginResult = await manager.AddLoginAsync(user, loginInfo);
                    Assert.True(loginResult.Succeeded, string.Concat(loginResult.Errors));

                    var loginsResult = await manager.GetLoginsAsync(user);
                    Assert.True(loginsResult
                        .Any(log => log.LoginProvider == loginInfo.LoginProvider
                            & log.ProviderKey == loginInfo.ProviderKey), "LoginInfo not found: GetLoginsAsync");

                    var sw = new Stopwatch();
                    sw.Start();
                    var loginResult2 = await manager.FindByLoginAsync(loginsResult.First().LoginProvider, loginsResult.First().ProviderKey);
                    sw.Stop();
                    Debug.WriteLine(string.Format("FindAsync(By Login): {0} seconds", sw.Elapsed.TotalSeconds));
                    Assert.NotNull(loginResult2);
                }
            }
        }

        [Fact(DisplayName = "AddRemoveUserToken")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public async Task AddRemoveUserToken()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = GenTestUser();
                    WriteLineObject<IdentityUser>(user);
                    var userResult = await manager.CreateAsync(user, DefaultUserPassword);
                    Assert.True(userResult.Succeeded, string.Concat(userResult.Errors));

                    string tokenValue = Guid.NewGuid().ToString();
                    string tokenName = string.Format("TokenName{0}", Guid.NewGuid().ToString());
                    string tokenName2 = string.Format("TokenName2{0}", Guid.NewGuid().ToString());

                    await manager.SetAuthenticationTokenAsync(user,
                        Constants.LoginProviders.GoogleProvider.LoginProvider,
                        tokenName,
                        tokenValue);

                    string getTokenValue = await manager.GetAuthenticationTokenAsync(user,
                        Constants.LoginProviders.GoogleProvider.LoginProvider,
                        tokenName);
                    Assert.NotNull(tokenName);
                    Assert.Equal(getTokenValue, tokenValue);

                    await manager.SetAuthenticationTokenAsync(user,
                        Constants.LoginProviders.GoogleProvider.LoginProvider,
                        tokenName2,
                        tokenValue);

                    await manager.RemoveAuthenticationTokenAsync(user,
                        Constants.LoginProviders.GoogleProvider.LoginProvider,
                        tokenName);

                    getTokenValue = await manager.GetAuthenticationTokenAsync(user,
                        Constants.LoginProviders.GoogleProvider.LoginProvider,
                        tokenName);
                    Assert.Null(getTokenValue);

                    getTokenValue = await manager.GetAuthenticationTokenAsync(user,
                        Constants.LoginProviders.GoogleProvider.LoginProvider,
                        tokenName2);
                    Assert.NotNull(getTokenValue);
                    Assert.Equal(getTokenValue, tokenValue);
                }
            }
        }

        [Fact(DisplayName = "AddRemoveUserLogin")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public async Task AddRemoveUserLogin()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = GenTestUser();
                    WriteLineObject<IdentityUser>(user);
                    var userResult = await manager.CreateAsync(user, DefaultUserPassword);
                    Assert.True(userResult.Succeeded, string.Concat(userResult.Errors));

                    var loginInfo = GenGoogleLogin();
                    var addLoginResult = await manager.AddLoginAsync(user, loginInfo);
                    Assert.True(addLoginResult.Succeeded, string.Concat(addLoginResult.Errors));

                    var getLoginResult = await manager.GetLoginsAsync(user);
                    Assert.True(getLoginResult
                        .Any(log => log.LoginProvider == loginInfo.LoginProvider
                            & log.ProviderKey == loginInfo.ProviderKey), "LoginInfo not found: GetLoginsAsync");

                    var getLoginResult2 = await manager.FindByLoginAsync(getLoginResult.First().LoginProvider, getLoginResult.First().ProviderKey);
                    Assert.NotNull(getLoginResult2);

                    var userRemoveLoginResultNeg1 = await manager.RemoveLoginAsync(user, string.Empty, loginInfo.ProviderKey);
                    var userRemoveLoginResultNeg2 = await manager.RemoveLoginAsync(user, loginInfo.LoginProvider, string.Empty);
                    var userRemoveLoginResult = await manager.RemoveLoginAsync(user, loginInfo.LoginProvider, loginInfo.ProviderKey);
                    Assert.True(userRemoveLoginResult.Succeeded, string.Concat(userRemoveLoginResult.Errors));

                    var loginGetResult3 = await manager.GetLoginsAsync(user);
                    Assert.True(!loginGetResult3.Any(), "LoginInfo not removed");

                    //Negative cases
                    var loginFindNeg = await manager.FindByLoginAsync("asdfasdf", "http://4343443dfaksjfaf");
                    Assert.Null(loginFindNeg);

                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.AddLoginAsync(null, loginInfo));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.AddLoginAsync(user, null));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.RemoveLoginAsync(null, loginInfo.ProviderKey, loginInfo.LoginProvider));
                    // TODO: check why null login provider is accepted
                    // await Assert.ThrowsAsync<ArgumentNullException>(() => store.RemoveLoginAsync(user, null, null));
                    // await Assert.ThrowsAsync<ArgumentNullException>(() => store.FindByLoginAsync(null, null));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetLoginsAsync(null));
                }
            }
        }

        [Fact(DisplayName = "AddUserRole")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public async Task AddUserRole()
        {
            string strUserRole = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"));
            WriteLineObject<IdentityUser>(CurrentUser);
            await AddUserRoleAsyncHelper(CurrentUser, strUserRole);
        }

        [Fact(DisplayName = "GetUsersByRole")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public async Task GetUsersByRole()
        {
            string strUserRole = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"));
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    int userCount = 4;
                    var sw2 = new Stopwatch();
                    ApplicationUser tempUser = null;
                    for (int i = 0; i < userCount; i++)
                    {
                        var sw = new Stopwatch();
                        output.WriteLine("CreateTestUserLite()");
                        sw.Start();
                        tempUser = await CreateTestUserLiteAsync(true, true);
                        sw.Stop();
                        output.WriteLine("CreateTestUserLite(): {0} seconds", sw.Elapsed.TotalSeconds);
                        await AddUserRoleAsyncHelper(tempUser, strUserRole);
                    }
                    sw2.Stop();
                    output.WriteLine("GenerateUsers(): {0} user count", userCount);
                    output.WriteLine("GenerateUsers(): {0} seconds", sw2.Elapsed.TotalSeconds);

                    var users = await manager.GetUsersInRoleAsync(strUserRole);
                    Assert.Equal(userCount, users.Count);
                }
            }
        }

        public static async Task AddUserRoleAsyncHelper(ApplicationUser user, string roleName)
        {
            using (RoleStore<IdentityRole> rstore = userFixture.CreateRoleStore())
            {
                var userRole = rstore.FindByNameAsync(roleName);
                userRole.Wait();

                if (userRole.Result == null)
                {
                    await rstore.CreateAsync(new IdentityRole(roleName));
                }
            }

            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var userRoleResult = await manager.AddToRoleAsync(user, roleName);
                    Assert.True(userRoleResult.Succeeded, string.Concat(userRoleResult.Errors));

                    var roles2Result = await manager.IsInRoleAsync(user, roleName);
                    Assert.True(roles2Result, "Role not found");
                }
            }
        }

        [Fact(DisplayName = "AddRemoveUserRole")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public async Task AddRemoveUserRole()
        {
            string roleName = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestAdminRole, Guid.NewGuid().ToString("N"));

            using (RoleStore<IdentityRole> rstore = userFixture.CreateRoleStore())
            {
                await rstore.CreateAsync(new IdentityRole(roleName));
                await rstore.FindByNameAsync(roleName);
            }

            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = CurrentUser;
                    WriteLineObject(user);
                    var userRole = await manager.AddToRoleAsync(user, roleName);
                    Assert.True(userRole.Succeeded, string.Concat(userRole.Errors));

                    var sw = new Stopwatch();
                    sw.Start();
                    var roles = await manager.GetRolesAsync(user);
                    sw.Stop();
                    var getout = string.Format("{0} ms", sw.Elapsed.TotalMilliseconds);
                    Debug.WriteLine(getout);
                    output.WriteLine(getout);
                    Assert.True(roles.Contains(roleName), "Role not found"); 

                    sw.Start();
                    var roles2 = await manager.IsInRoleAsync(user, roleName);
                    sw.Stop();
                    var isInout = string.Format("{0} ms", sw.Elapsed.TotalMilliseconds);
                    Debug.WriteLine(isInout);
                    output.WriteLine(isInout);
                    Assert.True(roles2, "Role not found");

                    var userRemoveResult = await manager.RemoveFromRoleAsync(user, roleName);
                    var rolesResult2 = await manager.GetRolesAsync(user);
                    Assert.False(rolesResult2.Contains(roleName), "Role not removed.");

                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.AddToRoleAsync(null, roleName));
                    await Assert.ThrowsAsync<ArgumentException>(() => store.AddToRoleAsync(user, null));
                    // TODO: check
                    // await Assert.ThrowsAsync<ArgumentException>(() => store.AddToRoleAsync(user, Guid.NewGuid().ToString()));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.RemoveFromRoleAsync(null, roleName));
                    await Assert.ThrowsAsync<ArgumentException>(() => store.RemoveFromRoleAsync(user, null));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetRolesAsync(null));
                }
            }
        }

        [Fact(DisplayName = "IsUserInRole")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public async Task IsUserInRole()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = CurrentUser;
                    WriteLineObject(user);
                    string roleName = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"));

                    await AddUserRoleAsyncHelper(user, roleName);

                    var sw = new Stopwatch();
                    sw.Start();
                    var result = await manager.IsInRoleAsync(user, roleName);
                    sw.Stop();
                    output.WriteLine("IsInRoleAsync: {0} seconds", sw.Elapsed.TotalSeconds);
                    Assert.True(result, "Role not found");

                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.IsInRoleAsync(null, roleName));
                    await Assert.ThrowsAsync<ArgumentException>(() => store.IsInRoleAsync(user, null));
                }
            }
        }

        [Fact(DisplayName = "GenerateUsers", Skip = "true")]
        [Trait("IdentityCore.Azure.UserStore", "")]
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
                        var sw = new Stopwatch();
                        output.WriteLine("CreateTestUserLite()");
                        sw.Start();
                        await CreateTestUserLiteAsync(true, true);
                        sw.Stop();
                        output.WriteLine("CreateTestUserLite(): {0} seconds", sw.Elapsed.TotalSeconds);
                    }
                    output.WriteLine("GenerateUsers(): {0} user count", userCount);
                    output.WriteLine("GenerateUsers(): {0} seconds", (DateTime.UtcNow - start2).TotalSeconds);
                }
            }
        }

        [Fact(DisplayName = "AddUserClaim")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public Task AddUserClaim()
        {
            WriteLineObject<IdentityUser>(CurrentUser);
            return AddUserClaimHelper(CurrentUser, GenUserClaim());
        }

        private static async Task AddUserClaimHelper(ApplicationUser user, Claim claim)
        {
            using (var store = userFixture.CreateUserStore(new IdentityCloudContext()))
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var userClaim = await manager.AddClaimAsync(user, claim);
                    Assert.True(userClaim.Succeeded, string.Concat(userClaim.Errors.Select(e => e.Code)));

                    var claims = await manager.GetClaimsAsync(user);
                    Assert.True(claims.Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not found");
                }
            }
        }

        [Fact(DisplayName = "GetUsersByClaim")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public async Task GetUsersByClaim()
        {
            var claim = GenUserClaim();
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    int userCount = 4;// 101;
                    DateTime start2 = DateTime.UtcNow;
                    ApplicationUser tempUser = null;
                    for (int i = 0; i < userCount; i++)
                    {
                        var sw = new Stopwatch();
                        output.WriteLine("CreateTestUserLite()");
                        sw.Start();
                        tempUser = await CreateTestUserLiteAsync(true, true);
                        sw.Stop();
                        output.WriteLine("CreateTestUserLite(): {0} seconds", sw.Elapsed.TotalSeconds);
                        await AddUserClaimHelper(tempUser, claim);
                    }
                    output.WriteLine("GenerateUsers(): {0} user count", userCount);
                    output.WriteLine("GenerateUsers(): {0} seconds", (DateTime.UtcNow - start2).TotalSeconds);

                    DateTime start3 = DateTime.UtcNow;
                    var users = await manager.GetUsersForClaimAsync(claim);
                    output.WriteLine("GetUsersForClaimAsync(): {0} seconds", (DateTime.UtcNow - start3).TotalSeconds);
                    output.WriteLine("GetUsersForClaimAsync(): {0} user count", users.Count());
                    Assert.Equal(users.Count(), userCount);
                }
            }
        }

        [Fact(DisplayName = "AddRemoveUserClaim")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public async Task AddRemoveUserClaim()
        {
            using (var store = userFixture.CreateUserStore(new IdentityCloudContext()))
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = CurrentUser;
                    WriteLineObject<IdentityUser>(user);
                    Claim claim = GenAdminClaim();
                    var addClaimResult = await manager.AddClaimAsync(user, claim);
                    Assert.True(addClaimResult.Succeeded, string.Concat(addClaimResult.Errors));

                    var claims = await manager.GetClaimsAsync(user);
                    Assert.True(claims.Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not found");

                    var userRemoveClaimTask = manager.RemoveClaimAsync(user, claim);
                    userRemoveClaimTask.Wait();
                    Assert.True(addClaimResult.Succeeded, string.Concat(addClaimResult.Errors));

                    var claims2 = await manager.GetClaimsAsync(user);
                    Assert.True(!claims2.Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not removed");

                    //adding test for removing an empty claim
                    Claim claimEmpty = GenAdminClaimEmptyValue();
                    var addClaimResult2 = await manager.AddClaimAsync(user, claimEmpty);
                    var removeClaimResult2 = await manager.RemoveClaimAsync(user, claimEmpty);
                    Assert.True(addClaimResult2.Succeeded, string.Concat(addClaimResult2.Errors));

                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.AddClaimAsync(null, claim));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.AddClaimAsync(user, null));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.RemoveClaimAsync(null, claim));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.RemoveClaimAsync(user, null));
                    await Assert.ThrowsAsync<ArgumentException>(()=>store.RemoveClaimAsync(user, new Claim(string.Empty, Guid.NewGuid().ToString())));
                    await Assert.ThrowsAsync<ArgumentNullException>(()=>store.RemoveClaimAsync(user, new Claim(claim.Type, null)));
                    await Assert.ThrowsAsync<ArgumentNullException>(()=>store.GetClaimsAsync(null));
                }
            }
        }

        [Fact(DisplayName = "ThrowIfDisposed")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public async Task ThrowIfDisposed()
        {
            var store = userFixture.CreateUserStore();
            store.Dispose();
            GC.Collect();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => store.DeleteAsync(null));
        }
    }
}
