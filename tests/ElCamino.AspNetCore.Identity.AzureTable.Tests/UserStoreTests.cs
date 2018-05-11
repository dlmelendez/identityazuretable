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
using IdentityUser = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityUser<string>;
using IdentityRole = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityRole;
using ElCamino.Web.Identity.AzureTable.Tests.ModelTests;
using ElCamino.Web.Identity.AzureTable.Tests.Fixtures;
using Microsoft.AspNetCore.Identity;

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests
{
    public partial class UserStoreTests : BaseUserStoreTests<ApplicationUser, IdentityRole, IdentityCloudContext, UserStore<ApplicationUser, IdentityRole, IdentityCloudContext>>
    {
        public UserStoreTests(UserFixture<ApplicationUser, IdentityRole, IdentityCloudContext, UserStore<ApplicationUser, IdentityRole, IdentityCloudContext>> userFix, ITestOutputHelper output) :
            base(userFix, output) {  }

        [Fact(DisplayName = "AddRemoveUserClaim")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override Task AddRemoveUserClaim()
        {
            return base.AddRemoveUserClaim();
        }

        [Fact(DisplayName = "AddRemoveUserLogin")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override Task AddRemoveUserLogin()
        {
            return base.AddRemoveUserLogin();
        }

        [Fact(DisplayName = "AddRemoveUserRole")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override Task AddRemoveUserRole()
        {
            return base.AddRemoveUserRole();
        }

        [Fact(DisplayName = "AddRemoveUserToken")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override Task AddRemoveUserToken()
        {
            return base.AddRemoveUserToken();
        }

        [Fact(DisplayName = "AddReplaceRemoveUserClaim")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override Task AddReplaceRemoveUserClaim()
        {
            return base.AddReplaceRemoveUserClaim();
        }

        [Fact(DisplayName = "AddUserClaim")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override Task AddUserClaim()
        {
            return base.AddUserClaim();
        }

        [Fact(DisplayName = "AddUserLogin")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override Task AddUserLogin()
        {
            return base.AddUserLogin();
        }

        [Fact(DisplayName = "AddUserRole")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override Task AddUserRole()
        {
            return base.AddUserRole();
        }

        [Fact(DisplayName = "ChangeUserName")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override Task ChangeUserName()
        {
            return base.ChangeUserName();
        }

        [Fact(DisplayName = "CheckDupEmail")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override Task CheckDupEmail()
        {
            return base.CheckDupEmail();
        }

        [Trait("IdentityCore.Azure.UserStore", "")]
        [Fact(DisplayName = "CheckDupUser")]
        public override Task CheckDupUser()
        {
            return base.CheckDupUser();
        }

        [Fact(DisplayName = "CreateUser")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override void CreateUserTest()
        {
            base.CreateUserTest();
        }

        [Fact(DisplayName = "DeleteUser")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override Task DeleteUser()
        {
            return base.DeleteUser();
        }

        [Fact(DisplayName = "FindUserByEmail")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override Task FindUserByEmail()
        {
            return base.FindUserByEmail();
        }

        [Fact(DisplayName = "FindUserById")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override Task FindUserById()
        {
            return base.FindUserById();
        }

        [Fact(DisplayName = "FindUserByName")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override Task FindUserByName()
        {
            return base.FindUserByName();
        }

        [Fact(DisplayName = "FindUsersByEmail")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override Task FindUsersByEmail()
        {
            return base.FindUsersByEmail();
        }

        //[Fact(DisplayName = "GenerateUsers", Skip = "true")]
        //[Trait("IdentityCore.Azure.UserStore", "")]
        //public override Task GenerateUsers()
        //{
        //    return base.GenerateUsers();
        //}

        [Fact(DisplayName = "GetUsersByClaim")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override Task GetUsersByClaim()
        {
            return base.GetUsersByClaim();
        }

        [Fact(DisplayName = "GetUsersByRole")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override Task GetUsersByRole()
        {
            return base.GetUsersByRole();
        }

        [Fact(DisplayName = "IsUserInRole")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override Task IsUserInRole()
        {
            return base.IsUserInRole();
        }

        [Fact(DisplayName = "ThrowIfDisposed")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override Task ThrowIfDisposed()
        {
            return base.ThrowIfDisposed();
        }

        [Fact(DisplayName = "UpdateApplicationUser")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override Task UpdateApplicationUser()
        {
            return base.UpdateApplicationUser();
        }

        [Fact(DisplayName = "UpdateUser")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override Task UpdateUser()
        {
            return base.UpdateUser();
        }

        [Fact(DisplayName = "UserStoreCtors")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public override void UserStoreCtors()
        {
            Assert.Throws<ArgumentNullException>(() => 
            {
                new UserStore<ApplicationUser, IdentityRole, IdentityCloudContext>(null,null);
            });
        }
    }

    public partial class BaseUserStoreTests<TUser, TRole, TContext, TUserStore> : IClassFixture<UserFixture<TUser, TRole, TContext, TUserStore>>
        where TUser : IdentityUser, IApplicationUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityCloudContext, new()
        where TUserStore : UserStoreV2<TUser, TRole, TContext> 
    {
        #region Static and Const Members
        public static string DefaultUserPassword = "M" + Guid.NewGuid().ToString();

        protected static bool tablesCreated = false;

        #endregion

        protected readonly ITestOutputHelper output;


        protected UserFixture<TUser, TRole, TContext, TUserStore> userFixture;

        public BaseUserStoreTests(UserFixture<TUser, TRole, TContext, TUserStore> userFix, ITestOutputHelper output)
        {
            userFixture = userFix;

            Initialize();
            this.output = output;
        }

        #region Test Initialization
        public void Initialize()
        {
            //--Changes to speed up tests that don't require a new user, sharing a static user
            //--Also limiting table creation to once per test run
            if (!tablesCreated)
            {
                Task.Run(async () => {
                    using (var store = userFixture.CreateUserStore())
                    {
                        await store.CreateTablesIfNotExists().ConfigureAwait(continueOnCapturedContext: false);
                    }
                }).Wait();
                

                tablesCreated = true;
            }
            //--
        }
        #endregion

        protected void WriteLineObject<t>(t obj) where t : class
        {
            output.WriteLine(typeof(t).Name);
            string strLine = obj == null ? "Null" : Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
            output.WriteLine("{0}", strLine);
        }

        protected static string GenClaimValue()
        {
            string strValue = "EAABdGMzTwv8BALkEPOvXXRz3LNo6l77ymX75OO3gzadoxYLOs7KrMBi2zdjqULQ2CJAGUgwwsEGRFo2XIp0JPvoJEvaQdXgXZB1UzX8plkawZAdC93btP6lZBHettE0kfB91RODbWaj1aJbr3ejytBKq7vyP4mWq8lA9DWzCgZDZD";
            string strGuid = Guid.NewGuid().ToString("N");
            return strValue + strGuid;
        }
        protected static Claim GenAdminClaim()
        {
            return new Claim(Constants.AccountClaimTypes.AccountTestAdminClaim, GenClaimValue());
        }

        protected Claim GenAdminClaimEmptyValue()
        {
            return new Claim(Constants.AccountClaimTypes.AccountTestAdminClaim, string.Empty);
        }

        protected Claim GenUserClaim()
        {
            return new Claim(Constants.AccountClaimTypes.AccountTestUserClaim, GenClaimValue());
        }

        protected Claim GenUserClaimEmptyType()
        {
            return new Claim(string.Empty, GenClaimValue());
        }

        protected static UserLoginInfo GenGoogleLogin()
         => new UserLoginInfo(Constants.LoginProviders.GoogleProvider.LoginProvider,
                              Constants.LoginProviders.GoogleProvider.ProviderKey, string.Empty);

        protected static TUser GenTestUser()
        {
            Guid id = Guid.NewGuid();
            var user = new TUser()
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

        protected TUser GetTestAppUser()
        {
            Guid id = Guid.NewGuid();
            TUser user = new TUser()
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

        public virtual void UserStoreCtors()
        {
            //noop.
        }

        public virtual async Task CheckDupUser()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
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

        public virtual async Task CheckDupEmail()
        {
            using (var store = userFixture.CreateUserStore())
            {
                IdentityOptions options = new IdentityOptions();
                options.User.RequireUniqueEmail = true;
                using (var manager = userFixture.CreateUserManager(options))
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

        public virtual void CreateUserTest()
        {
            WriteLineObject(CreateTestUserAsync<TUser>());
        }

        public Task<T> CreateUserAsync<T>()
            where T : Model.IdentityUser<string>, new()
        {
            return CreateTestUserAsync<T>();
        }

        protected async Task<T> CreateTestUserAsync<T>(bool createPassword = true, bool createEmail = true,
            string emailAddress = null) where T : Model.IdentityUser<string>, new()
        {
            string strValidConnection = userFixture.GetConfig().StorageConnectionString;

            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
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

                    for (int i = 0; i < 2; i++)
                    {
                        await AddUserClaimHelper(user, GenAdminClaim());
                        await AddUserLoginAsyncHelper(user, GenGoogleLogin());
                        await AddUserRoleAsyncHelper(user, string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N")));
                        await manager.SetAuthenticationTokenAsync(user,
                            Constants.LoginProviders.GoogleProvider.LoginProvider,
                            string.Format("TokenName{0}", Guid.NewGuid().ToString()),
                            Guid.NewGuid().ToString());
                    }

                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.CreateAsync(null));

                    var getUserResult = await manager.FindByIdAsync(user.Id);
                    return getUserResult as T;
                }
            }
        }

        protected async Task<TUser> CreateTestUserLiteAsync(bool createPassword = true, bool createEmail = true,
            string emailAddress = null)
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
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


                    return user;
                }
            }
        }

        public virtual async Task DeleteUser()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    var user = GenTestUser();

                    var userCreationResult = await manager.CreateAsync(user, DefaultUserPassword);
                    Assert.True(userCreationResult.Succeeded, string.Concat(userCreationResult.Errors));

                    for (int i = 0; i < 20; i++)
                    {
                        await store.AddClaimAsync(user, GenAdminClaim());
                        await store.AddLoginAsync(user, GenGoogleLogin());
                        await AddUserRoleAsyncHelper(user, string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N")));
                        await store.SetTokenAsync(user,
                            Constants.LoginProviders.GoogleProvider.LoginProvider,
                            string.Format("TokenName{0}", Guid.NewGuid().ToString()),
                            Guid.NewGuid().ToString(), new CancellationToken());

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

        public virtual async Task UpdateApplicationUser()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    var user = GetTestAppUser();
                    WriteLineObject<TUser>(user);
                    var taskUser = await manager.CreateAsync(user, DefaultUserPassword);
                    Assert.True(taskUser.Succeeded, string.Concat(taskUser.Errors));

                    string oFirstName = user.FirstName;
                    string oLastName = user.LastName;

                    var taskFind1 = await manager.FindByNameAsync(user.UserName);
                    Assert.Equal(oFirstName, taskFind1.FirstName);
                    Assert.Equal(oLastName, taskFind1.LastName);

                    string cFirstName = string.Format("John_{0}", Guid.NewGuid());
                    string cLastName = string.Format("Doe_{0}", Guid.NewGuid());

                    user.FirstName = cFirstName;
                    user.LastName = cLastName;

                    var taskUserUpdate = await manager.UpdateAsync(user);
                    Assert.True(taskUserUpdate.Succeeded, string.Concat(taskUserUpdate.Errors));

                    var taskFind = await manager.FindByNameAsync(user.UserName);
                    Assert.Equal(cFirstName, taskFind.FirstName);
                    Assert.Equal(cLastName, taskFind.LastName);
                }
            }
        }

        public virtual async Task UpdateUser()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
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

        public virtual async Task ChangeUserName()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    var firstUser = await CreateTestUserAsync<TUser>();
                    output.WriteLine("{0}", "Original User");
                    WriteLineObject(firstUser);
                    string originalPlainUserName = firstUser.UserName;
                    string originalUserId = firstUser.Id;
                    string userNameChange = Guid.NewGuid().ToString("N");

                    const int count = 2;
                    var firstUserRoles = await manager.GetRolesAsync(firstUser);
                    Assert.True(firstUserRoles.Count == count);
                    var firstUserClaims = await manager.GetClaimsAsync(firstUser);
                    Assert.True(firstUserClaims.Count == count);
                    var firstUserLogins = await manager.GetLoginsAsync(firstUser);
                    Assert.True(firstUserLogins.Count == count);

                    string tokenValue = Guid.NewGuid().ToString();
                    string tokenName = string.Format("TokenName{0}", Guid.NewGuid().ToString());

                    await manager.SetAuthenticationTokenAsync(firstUser,
                        Constants.LoginProviders.GoogleProvider.LoginProvider,
                        tokenName,
                        tokenValue);

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

                    var changedUserRoles = await manager.GetRolesAsync(changedUser);
                    Assert.True(changedUserRoles.Count == count);
                    var changedUserClaims = await manager.GetClaimsAsync(changedUser);
                    Assert.True(changedUserClaims.Count == count);
                    var changedUserLogins = await manager.GetLoginsAsync(changedUser);
                    Assert.True(changedUserLogins.Count == count);

                    Assert.Equal<int>(firstUserRoles.Count, changedUserRoles.Count);

                    Assert.Equal<int>(firstUserClaims.Count, changedUserClaims.Count);

                    Assert.Equal<int>(firstUserLogins.Count, changedUserLogins.Count);

                    Assert.NotEqual<string>(originalUserId, changedUser.Id);

                    //Check email
                    var findEmailResult = await manager.FindByEmailAsync(changedUser.Email);
                    Assert.NotNull(findEmailResult);

                    //Check the old username is deleted
                    var oldUser = await manager.FindByIdAsync(originalUserId);
                    Assert.Null(oldUser);

                    

                    //Check logins
                    foreach (var log in changedUserLogins)
                    {
                        var findLoginResult = await manager.FindByLoginAsync(log.LoginProvider, log.ProviderKey);                        
                        Assert.NotNull(findLoginResult);
                        Assert.NotEqual<string>(originalUserId, findLoginResult.Id.ToString());
                    }

                    //Check role indexes
                    foreach(var cRole in changedUserRoles)
                    {
                        var findUsersInRole = await manager.GetUsersInRoleAsync(cRole);
                        Assert.Contains(findUsersInRole, fr => fr.Id == changedUser.Id);
                    }

                    //Check claims indexes
                    foreach (var cClaim in changedUserClaims)
                    {
                        var findUsersInClaim = await manager.GetUsersForClaimAsync(cClaim);
                        Assert.Contains(findUsersInClaim, fr => fr.Id == changedUser.Id);
                    }

                    //Check token
                    string changedUserTokenValue = await manager.GetAuthenticationTokenAsync(changedUser, Constants.LoginProviders.GoogleProvider.LoginProvider, tokenName);
                    Assert.True(changedUserTokenValue == tokenValue);

                    await Assert.ThrowsAsync<ArgumentNullException>(()=>store.UpdateAsync(null));
                }
            }
        }

        public virtual async Task FindUserByEmail()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    var user = await CreateUserAsync<TUser>();
                    WriteLineObject<TUser>(user);

                    var sw = new Stopwatch();
                    sw.Start();
                    var findUserResult = await manager.FindByEmailAsync(user.Email);
                    sw.Stop();
                    output.WriteLine("FindByEmailAsync: {0} seconds", sw.Elapsed.TotalSeconds);

                    Assert.Equal(user.Email, findUserResult.Email);
                }
            }
        }

        public virtual async Task FindUsersByEmail()
        {
            string strEmail = Guid.NewGuid().ToString() + "@live.com";

            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
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
                    Assert.Equal(userToChange.Id, changedResult.Id);
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

        public virtual async Task FindUserById()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    var user = await CreateUserAsync<TUser>();
                    var sw = new Stopwatch();
                    sw.Start();
                    var result = await manager.FindByIdAsync(user.Id);
                    sw.Stop();
                    output.WriteLine("FindByIdAsync: {0} seconds", sw.Elapsed.TotalSeconds);

                    Assert.Equal(user.Id, result.Id);
                }
            }
        }

        public virtual async Task FindUserByName()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    var user = await CreateUserAsync<TUser>();
                    WriteLineObject<IdentityUser>(user);
                    var sw = new Stopwatch();
                    sw.Start();
                    var result = await manager.FindByNameAsync(user.UserName);
                    sw.Stop();
                    output.WriteLine("FindByNameAsync: {0} seconds", sw.Elapsed.TotalSeconds);

                    Assert.Equal(user.UserName, result.UserName);
                }
            }
        }

        public virtual async Task AddUserLogin()
        {
            using (var manager = userFixture.CreateUserManager())
            {
                var user = await CreateTestUserLiteAsync(createPassword: false);
                WriteLineObject(user);
                var loginInfo = GenGoogleLogin();
                await AddUserLoginAsyncHelper(user, loginInfo);

                var loginsResult = await manager.GetLoginsAsync(user);
                Assert.Contains(loginsResult,
                    (log) => log.LoginProvider == loginInfo.LoginProvider
                        && log.ProviderKey == loginInfo.ProviderKey); //, "LoginInfo not found: GetLoginsAsync");

                var sw = new Stopwatch();
                sw.Start();
                var loginResult2 = await manager.FindByLoginAsync(loginsResult.First().LoginProvider, loginsResult.First().ProviderKey);
                sw.Stop();
                Debug.WriteLine(string.Format("FindAsync(By Login): {0} seconds", sw.Elapsed.TotalSeconds));
                Assert.Equal(user.Id, loginResult2.Id);
            }
        }

        public async Task AddUserLoginAsyncHelper(TUser user, UserLoginInfo loginInfo)
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    var loginResult = await manager.AddLoginAsync(user, loginInfo);
                    Assert.True(loginResult.Succeeded, string.Concat(loginResult.Errors));                    
                }
            }
        }

        public virtual async Task AddRemoveUserToken()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
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

        public virtual async Task AddRemoveUserLogin()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    var user = GenTestUser();
                    WriteLineObject<IdentityUser>(user);
                    var userResult = await manager.CreateAsync(user, DefaultUserPassword);
                    Assert.True(userResult.Succeeded, string.Concat(userResult.Errors));

                    var loginInfo = GenGoogleLogin();
                    var addLoginResult = await manager.AddLoginAsync(user, loginInfo);
                    Assert.True(addLoginResult.Succeeded, string.Concat(addLoginResult.Errors));

                    var getLoginResult = await manager.GetLoginsAsync(user);
                    Assert.Contains(getLoginResult,
                        (log) => log.LoginProvider == loginInfo.LoginProvider
                            && log.ProviderKey == loginInfo.ProviderKey); //, "LoginInfo not found: GetLoginsAsync");

                    var getLoginResult2 = await manager.FindByLoginAsync(getLoginResult.First().LoginProvider, getLoginResult.First().ProviderKey);
                    Assert.NotNull(getLoginResult2);

                    var userRemoveLoginResultNeg1 = await manager.RemoveLoginAsync(user, string.Empty, loginInfo.ProviderKey);
                    var userRemoveLoginResultNeg2 = await manager.RemoveLoginAsync(user, loginInfo.LoginProvider, string.Empty);
                    var userRemoveLoginResult = await manager.RemoveLoginAsync(user, loginInfo.LoginProvider, loginInfo.ProviderKey);
                    Assert.True(userRemoveLoginResult.Succeeded, string.Concat(userRemoveLoginResult.Errors));

                    var loginGetResult3 = await manager.GetLoginsAsync(user);
                    Assert.DoesNotContain(loginGetResult3, (log) => true);// , "LoginInfo not removed");

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

        public virtual async Task AddUserRole()
        {
            string strUserRole = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"));
            var user = await CreateUserAsync<TUser>();
            WriteLineObject<TUser>(user);
            await AddUserRoleAsyncHelper(user, strUserRole);
        }

        public virtual async Task GetUsersByRole()
        {
            string strUserRole = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"));
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    int userCount = 4;
                    var sw2 = new Stopwatch();
                    sw2.Start();
                    TUser tempUser = null;
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

                    sw2.Reset();
                    sw2.Start();
                    var users = await manager.GetUsersInRoleAsync(strUserRole);
                    output.WriteLine("GetUsersInRoleAsync(): {0} seconds", sw2.Elapsed.TotalSeconds);
                    Assert.Equal(userCount, users.Count);

                    users = await manager.GetUsersInRoleAsync(Guid.NewGuid().ToString());
                    Assert.Equal(0, users.Count);

                    await Assert.ThrowsAsync<ArgumentException>(() => manager.GetUsersInRoleAsync(string.Empty));
                }
            }
        }

        public async Task AddUserRoleAsyncHelper(TUser user, string roleName)
        {
            using (var rstore = userFixture.CreateRoleStore())
            {
                var userRole = await rstore.FindByNameAsync(roleName);

                if (userRole == null)
                {
                    var r = (TRole)Activator.CreateInstance(typeof(TRole), new object[1] { roleName });

                    await rstore.CreateAsync(r);
                }
            }

            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    var userRoleResult = await manager.AddToRoleAsync(user, roleName);
                    Assert.True(userRoleResult.Succeeded, string.Concat(userRoleResult.Errors));

                    var roles2Result = await manager.IsInRoleAsync(user, roleName);
                    Assert.True(roles2Result, "Role not found");
                }
            }
        }

        public virtual async Task AddRemoveUserRole()
        {
            string roleName = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestAdminRole, Guid.NewGuid().ToString("N"));

            using (RoleStore<TRole> rstore = userFixture.CreateRoleStore())
            {
                var r = (TRole)Activator.CreateInstance(typeof(TRole), new object[1] { roleName });
                await rstore.CreateAsync(r);
                await rstore.FindByNameAsync(roleName);
            }

            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    var user = await CreateTestUserLiteAsync();
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

        public virtual async Task IsUserInRole()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    var user = await CreateUserAsync<TUser>();
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

        //public virtual async Task GenerateUsers()
        //{
        //    using (var store = userFixture.CreateUserStore())
        //    {
        //        using (var manager = userFixture.CreateUserManager())
        //        {
        //            int userCount = 1000;
        //            DateTime start2 = DateTime.UtcNow;
        //            for (int i = 0; i < userCount; i++)
        //            {
        //                var sw = new Stopwatch();
        //                output.WriteLine("CreateTestUserLite()");
        //                sw.Start();
        //                await CreateTestUserLiteAsync(true, true);
        //                sw.Stop();
        //                output.WriteLine("CreateTestUserLite(): {0} seconds", sw.Elapsed.TotalSeconds);
        //            }
        //            output.WriteLine("GenerateUsers(): {0} user count", userCount);
        //            output.WriteLine("GenerateUsers(): {0} seconds", (DateTime.UtcNow - start2).TotalSeconds);
        //        }
        //    }
        //}

        public virtual async Task AddUserClaim()
        {
            using (var manager = userFixture.CreateUserManager())
            {

                var user = await CreateTestUserLiteAsync();
                WriteLineObject<TUser>(user);
                var c1 = GenUserClaim();
                var c2 = GenUserClaimEmptyType();

                await AddUserClaimHelper(user, c1);
                await AddUserClaimHelper(user, c2);

                var claims = await manager.GetClaimsAsync(user);
                Assert.Contains(claims, (c) => c.Value == c1.Value && c.ValueType == c1.ValueType); //, "Claim not found");
                Assert.Contains(claims, (c) => c.Value == c2.Value && c.ValueType == c2.ValueType); //, "Claim not found");

            }
        }

        protected async Task AddUserClaimHelper(TUser user, Claim claim)
        {
            using (var manager = userFixture.CreateUserManager())
            {
                var userClaim = await manager.AddClaimAsync(user, claim);
                Assert.True(userClaim.Succeeded, string.Concat(userClaim.Errors.Select(e => e.Code)));
            }
        }

        public virtual async Task GetUsersByClaim()
        {
            var claim = GenUserClaim();
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    int userCount = 101;
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    TUser tempUser = null;
                    for (int i = 0; i < userCount; i++)
                    {
                        var sw2 = new Stopwatch();
                        output.WriteLine("CreateTestUserLite()");
                        sw2.Start();
                        tempUser = await CreateTestUserLiteAsync(true, true);
                        sw2.Stop();
                        output.WriteLine("CreateTestUserLite(): {0} seconds", sw2.Elapsed.TotalSeconds);
                        await store.AddClaimAsync(tempUser, claim);
                    }
                    sw.Stop();
                    output.WriteLine("GenerateUsers(): {0} user count", userCount);
                    output.WriteLine("GenerateUsers(): {0} seconds", sw.Elapsed.TotalSeconds);

                    sw.Reset();                   
                    sw.Start();
                    var users = await manager.GetUsersForClaimAsync(claim);
                    sw.Stop();
                    output.WriteLine("GetUsersForClaimAsync(): {0} seconds", sw.Elapsed.TotalSeconds);
                    output.WriteLine("GetUsersForClaimAsync(): {0} user count", users.Count());
                    Assert.Equal(userCount, users.Count());
                }

                await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetUsersForClaimAsync(null));

            }
        }

        public virtual async Task AddRemoveUserClaim()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    var user = await CreateTestUserLiteAsync();
                    WriteLineObject<TUser>(user);
                    Claim claim = GenAdminClaim();
                    var addClaimResult = await manager.AddClaimAsync(user, claim);
                    Assert.True(addClaimResult.Succeeded, string.Concat(addClaimResult.Errors));

                    var claims = await manager.GetClaimsAsync(user);
                    Assert.Contains(claims, (c) => c.Value == claim.Value && c.Type == claim.Type); //, "Claim not found");

                    var userRemoveClaimResult = await manager.RemoveClaimAsync(user, claim);
                    Assert.True(userRemoveClaimResult.Succeeded, string.Concat(userRemoveClaimResult.Errors));

                    var claims2 = await manager.GetClaimsAsync(user);
                    Assert.DoesNotContain(claims2, (c) => c.Value == claim.Value && c.Type == claim.Type); //, "Claim not removed");

                    //adding test for removing an empty claim
                    Claim claimEmpty = GenAdminClaimEmptyValue();
                    var addClaimResult2 = await manager.AddClaimAsync(user, claimEmpty);
                    var removeClaimResult2 = await manager.RemoveClaimAsync(user, claimEmpty);
                    Assert.True(addClaimResult2.Succeeded, string.Concat(addClaimResult2.Errors));
                    Assert.True(removeClaimResult2.Succeeded, string.Concat(removeClaimResult2.Errors));

                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.AddClaimAsync(null, claim));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.AddClaimAsync(user, null));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.RemoveClaimAsync(null, claim));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.RemoveClaimAsync(user, null));
                    await store.RemoveClaimAsync(user, new Claim(string.Empty, Guid.NewGuid().ToString()));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.RemoveClaimAsync(user, new Claim(claim.Type, null)));
                    await Assert.ThrowsAsync<ArgumentNullException>(()=>store.GetClaimsAsync(null));
                }
            }
        }

        public virtual async Task AddReplaceRemoveUserClaim()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    var user = await CreateTestUserLiteAsync(createPassword:true, createEmail:true);
                    WriteLineObject<TUser>(user);
                    Claim claim = GenAdminClaim();
                    await store.AddClaimAsync(user, claim);
                    

                    var claims = await manager.GetClaimsAsync(user);
                    Assert.Contains(claims, (c) => c.Value == claim.Value && c.Type == claim.Type); //, "Claim not found");
                    

                    Claim nClaim = new Claim(claim.Type, "new claim value here");
                    var userReplaceClaimResult = await manager.ReplaceClaimAsync(user, claim, nClaim);
                    Assert.True(userReplaceClaimResult.Succeeded, string.Concat(userReplaceClaimResult.Errors));

                    var claims2 = await manager.GetClaimsAsync(user);
                    Assert.DoesNotContain(claims2, (c) => c.Value == claim.Value && c.Type == claim.Type); //, "Claim not replaced, old claim found");
                    Assert.Contains(claims2, (c) => c.Value == nClaim.Value && c.Type == nClaim.Type); //, "Claim not replaced, new claim not found.");


                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.ReplaceClaimAsync(null, claim, nClaim));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.ReplaceClaimAsync(user, null, nClaim));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.ReplaceClaimAsync(user, claim, null));

                    await store.RemoveClaimAsync(user, nClaim);
                    claims2 = await manager.GetClaimsAsync(user);
                    Assert.DoesNotContain(claims2, (c) => c.Value == nClaim.Value && c.Type == nClaim.Type); //, "Claim not removed, new claim found.");
                }
            }
        }

        public virtual async Task ThrowIfDisposed()
        {
            var store = userFixture.CreateUserStore();
            store.Dispose();
            GC.Collect();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => store.DeleteAsync(null));
        }
    }
}
