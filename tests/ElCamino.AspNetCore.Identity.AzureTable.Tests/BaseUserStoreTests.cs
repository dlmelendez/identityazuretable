// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using ElCamino.Web.Identity.AzureTable.Tests.Fixtures;
using ElCamino.Web.Identity.AzureTable.Tests.ModelTests;
using Microsoft.AspNetCore.Identity;
using Xunit;
using Xunit.Abstractions;
using IdentityRole = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityRole;
using IdentityUser = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityUser<string>;

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests
{
    public partial class BaseUserStoreTests<TUser, TRole, TContext, TUserStore, TKeyHelper> : BaseUserStoreTests<TUser, TContext, TUserStore, TKeyHelper>,
        IClassFixture<BaseFixture<TUser, TRole, TContext, TUserStore, TKeyHelper>>
         where TUser : IdentityUser, IApplicationUser, new()
         where TRole : IdentityRole, new()
         where TContext : IdentityCloudContext
         where TUserStore : UserStore<TUser, TRole, TContext>
       where TKeyHelper : IKeyHelper, new()
    {
        protected new BaseFixture<TUser, TRole, TContext, TUserStore, TKeyHelper> userFixture;

        public BaseUserStoreTests(BaseFixture<TUser, TRole, TContext, TUserStore, TKeyHelper> userFix, ITestOutputHelper output)
            : base(userFix, output)
        {
            userFixture = userFix;

        }

        public async Task AddUserRoleAsyncHelper(TUser user, string roleName)
        {
            using (var rstore = userFixture.CreateRoleStore())
            {
                var userRole = await rstore.FindByNameAsync(roleName).ConfigureAwait(false);

                if (userRole == null)
                {
                    var r = (TRole)Activator.CreateInstance(typeof(TRole), new object[1] { roleName });

                    await rstore.CreateAsync(r).ConfigureAwait(false);
                }
            }

            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var userRoleResult = await manager.AddToRoleAsync(user, roleName).ConfigureAwait(false);
            Assert.True(userRoleResult.Succeeded, string.Concat(userRoleResult.Errors));

            var roles2Result = await manager.IsInRoleAsync(user, roleName).ConfigureAwait(false);
            Assert.True(roles2Result, "Role not found");
        }

        public virtual async Task AddRemoveUserRole()
        {
            string roleName = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestAdminRole, Guid.NewGuid().ToString("N"));

            using (RoleStore<TRole> rstore = userFixture.CreateRoleStore())
            {
                var r = (TRole)Activator.CreateInstance(typeof(TRole), new object[1] { roleName });
                await rstore.CreateAsync(r).ConfigureAwait(false);
                await rstore.FindByNameAsync(roleName).ConfigureAwait(false);
            }

            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = await CreateTestUserLiteAsync().ConfigureAwait(false);
            WriteLineObject(user);
            var userRole = await manager.AddToRoleAsync(user, roleName).ConfigureAwait(false);
            Assert.True(userRole.Succeeded, string.Concat(userRole.Errors));

            var sw = new Stopwatch();
            sw.Start();
            var roles = await manager.GetRolesAsync(user).ConfigureAwait(false);
            sw.Stop();
            var getout = string.Format("{0} ms", sw.Elapsed.TotalMilliseconds);
            Debug.WriteLine(getout);
            _output.WriteLine(getout);
            Assert.True(roles.Contains(roleName), "Role not found");

            sw.Start();
            var roles2 = await manager.IsInRoleAsync(user, roleName).ConfigureAwait(false);
            sw.Stop();
            var isInout = string.Format("{0} ms", sw.Elapsed.TotalMilliseconds);
            Debug.WriteLine(isInout);
            _output.WriteLine(isInout);
            Assert.True(roles2, "Role not found");

            var userRemoveResult = await manager.RemoveFromRoleAsync(user, roleName).ConfigureAwait(false);
            var rolesResult2 = await manager.GetRolesAsync(user).ConfigureAwait(false);
            Assert.False(rolesResult2.Contains(roleName), "Role not removed.");

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.AddToRoleAsync(null, roleName)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentException>(() => store.AddToRoleAsync(user, null)).ConfigureAwait(false);
            // TODO: check
            // await Assert.ThrowsAsync<ArgumentException>(() => store.AddToRoleAsync(user, Guid.NewGuid().ToString())).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.RemoveFromRoleAsync(null, roleName)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentException>(() => store.RemoveFromRoleAsync(user, null)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetRolesAsync(null)).ConfigureAwait(false);
        }

        public virtual async Task IsUserInRole()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = await CreateUserAsync<TUser>().ConfigureAwait(false);
            WriteLineObject(user);
            string roleName = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"));

            await AddUserRoleAsyncHelper(user, roleName).ConfigureAwait(false);

            var sw = new Stopwatch();
            sw.Start();
            var result = await manager.IsInRoleAsync(user, roleName).ConfigureAwait(false);
            sw.Stop();
            _output.WriteLine("IsInRoleAsync: {0} seconds", sw.Elapsed.TotalSeconds);
            Assert.True(result, "Role not found");

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.IsInRoleAsync(null, roleName)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentException>(() => store.IsInRoleAsync(user, null)).ConfigureAwait(false);
        }

        protected override async Task<T> CreateTestUserAsync<T>(bool createPassword = true, bool createEmail = true,
            string emailAddress = null)
        {
            string strValidConnection = userFixture.GetConfig().StorageConnectionString;

            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
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
                manager.CreateAsync(user)).ConfigureAwait(false);
            Assert.True(createUserResult.Succeeded, string.Concat(createUserResult.Errors));

            for (int i = 0; i < 2; i++)
            {
                await AddUserClaimHelper(user, GenAdminClaim()).ConfigureAwait(false);
                await AddUserLoginAsyncHelper(user, GenGoogleLogin()).ConfigureAwait(false);
                await AddUserRoleAsyncHelper(user, string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"))).ConfigureAwait(false);
                await manager.SetAuthenticationTokenAsync(user,
                    Constants.LoginProviders.GoogleProvider.LoginProvider,
                    string.Format("TokenName{0}", Guid.NewGuid().ToString()),
                    Guid.NewGuid().ToString()).ConfigureAwait(false);
            }

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.CreateAsync(null)).ConfigureAwait(false);

            var getUserResult = await manager.FindByIdAsync(user.Id).ConfigureAwait(false);
            return getUserResult as T;
        }

        public override async Task DeleteUser()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = GenTestUser();

            var userCreationResult = await manager.CreateAsync(user, DefaultUserPassword).ConfigureAwait(false);
            Assert.True(userCreationResult.Succeeded, string.Concat(userCreationResult.Errors));

            for (int i = 0; i < 20; i++)
            {
                await store.AddClaimAsync(user, GenAdminClaim()).ConfigureAwait(false);
                await store.AddLoginAsync(user, GenGoogleLogin()).ConfigureAwait(false);
                await AddUserRoleAsyncHelper(user, string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"))).ConfigureAwait(false);
                await store.SetTokenAsync(user,
                    Constants.LoginProviders.GoogleProvider.LoginProvider,
                    string.Format("TokenName{0}", Guid.NewGuid().ToString()),
                    Guid.NewGuid().ToString(), new CancellationToken()).ConfigureAwait(false);

            }

            user = await manager.FindByIdAsync(user.Id).ConfigureAwait(false);
            WriteLineObject(user);

            var sw = new Stopwatch();
            sw.Start();
            var userDelitionResult = await manager.DeleteAsync(user).ConfigureAwait(false);
            sw.Stop();
            Assert.True(userDelitionResult.Succeeded, string.Concat(userCreationResult.Errors));
            _output.WriteLine("DeleteAsync: {0} seconds", sw.Elapsed.TotalSeconds);

            await Task.Delay(2000).ConfigureAwait(false);

            var user2 = await manager.FindByIdAsync(user.Id).ConfigureAwait(false);
            Assert.Null(user2);

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.DeleteAsync(null)).ConfigureAwait(false);
        }

        public virtual async Task AddUserRole()
        {
            string strUserRole = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"));
            var user = await CreateUserAsync<TUser>().ConfigureAwait(false);
            WriteLineObject<TUser>(user);
            await AddUserRoleAsyncHelper(user, strUserRole).ConfigureAwait(false);
        }

        public virtual async Task GetUsersByRole()
        {
            string strUserRole = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"));
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            int userCount = 4;
            var sw2 = new Stopwatch();
            sw2.Start();
            TUser tempUser = null;
            for (int i = 0; i < userCount; i++)
            {
                var sw = new Stopwatch();
                _output.WriteLine("CreateTestUserLite()");
                sw.Start();
                tempUser = await CreateTestUserLiteAsync(true, true).ConfigureAwait(false);
                sw.Stop();
                _output.WriteLine("CreateTestUserLite(): {0} seconds", sw.Elapsed.TotalSeconds);
                await AddUserRoleAsyncHelper(tempUser, strUserRole).ConfigureAwait(false);
            }
            sw2.Stop();
            _output.WriteLine("GenerateUsers(): {0} user count", userCount);
            _output.WriteLine("GenerateUsers(): {0} seconds", sw2.Elapsed.TotalSeconds);

            sw2.Reset();
            sw2.Start();
            var users = await manager.GetUsersInRoleAsync(strUserRole).ConfigureAwait(false);
            _output.WriteLine("GetUsersInRoleAsync(): {0} seconds", sw2.Elapsed.TotalSeconds);
            Assert.Equal(userCount, users.Count);

            users = await manager.GetUsersInRoleAsync(Guid.NewGuid().ToString()).ConfigureAwait(false);
            Assert.Empty(users);

            await Assert.ThrowsAsync<ArgumentException>(() => manager.GetUsersInRoleAsync(string.Empty)).ConfigureAwait(false);
        }

        public override async Task ChangeUserName()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var firstUser = await CreateTestUserAsync<TUser>().ConfigureAwait(false);
            _output.WriteLine("{0}", "Original User");
            WriteLineObject(firstUser);
            string originalPlainUserName = firstUser.UserName;
            string originalUserId = firstUser.Id;
            string userNameChange = Guid.NewGuid().ToString("N");

            const int count = 2;
            var firstUserRoles = await manager.GetRolesAsync(firstUser).ConfigureAwait(false);
            Assert.True(firstUserRoles.Count == count);
            var firstUserClaims = await manager.GetClaimsAsync(firstUser).ConfigureAwait(false);
            Assert.True(firstUserClaims.Count == count);
            var firstUserLogins = await manager.GetLoginsAsync(firstUser).ConfigureAwait(false);
            Assert.True(firstUserLogins.Count == count);

            string tokenValue = Guid.NewGuid().ToString();
            string tokenName = string.Format("TokenName{0}", Guid.NewGuid().ToString());

            await manager.SetAuthenticationTokenAsync(firstUser,
                Constants.LoginProviders.GoogleProvider.LoginProvider,
                tokenName,
                tokenValue).ConfigureAwait(false);

            var sw = new Stopwatch();
            sw.Start();
            var userUpdate = await manager.SetUserNameAsync(firstUser, userNameChange).ConfigureAwait(false);
            sw.Stop();
            _output.WriteLine("UpdateAsync(ChangeUserName): {0} seconds", sw.Elapsed.TotalSeconds);
            Assert.True(userUpdate.Succeeded, string.Concat(userUpdate.Errors));

            await Task.Delay(200).ConfigureAwait(false);
            var userChangedResult = await manager.FindByNameAsync(userNameChange).ConfigureAwait(false);
            var changedUser = userChangedResult;
            _output.WriteLine("{0}", "Changed User");
            WriteLineObject<IdentityUser>(changedUser);

            Assert.NotNull(changedUser);
            Assert.False(originalPlainUserName.Equals(changedUser.UserName, StringComparison.OrdinalIgnoreCase), "UserName property not updated.");

            var changedUserRoles = await manager.GetRolesAsync(changedUser).ConfigureAwait(false);
            Assert.True(changedUserRoles.Count == count);
            var changedUserClaims = await manager.GetClaimsAsync(changedUser).ConfigureAwait(false);
            Assert.True(changedUserClaims.Count == count);
            var changedUserLogins = await manager.GetLoginsAsync(changedUser).ConfigureAwait(false);
            Assert.True(changedUserLogins.Count == count);

            Assert.Equal<int>(firstUserRoles.Count, changedUserRoles.Count);

            Assert.Equal<int>(firstUserClaims.Count, changedUserClaims.Count);

            Assert.Equal<int>(firstUserLogins.Count, changedUserLogins.Count);

            //Immutable Id
            Assert.Equal(originalUserId, changedUser.Id);

            //Check email
            var findEmailResult = await manager.FindByEmailAsync(changedUser.Email).ConfigureAwait(false);
            Assert.NotNull(findEmailResult);

            //Check the old username is should be the same and still work now.
            var oldUser = await manager.FindByIdAsync(originalUserId).ConfigureAwait(false);
            Assert.NotNull(oldUser);


            //Query for the old username, should be null
            var oldUserNameResult = await manager.FindByNameAsync(originalPlainUserName).ConfigureAwait(false);
            Assert.Null(oldUserNameResult);


            //Check logins
            foreach (var log in changedUserLogins)
            {
                var findLoginResult = await manager.FindByLoginAsync(log.LoginProvider, log.ProviderKey).ConfigureAwait(false);
                Assert.NotNull(findLoginResult);
                Assert.Equal(originalUserId, findLoginResult.Id.ToString());
            }

            //Check role indexes
            foreach (var cRole in changedUserRoles)
            {
                var findUsersInRole = await manager.GetUsersInRoleAsync(cRole).ConfigureAwait(false);
                Assert.Contains(findUsersInRole, fr => fr.Id == changedUser.Id);
            }

            //Check claims indexes
            foreach (var cClaim in changedUserClaims)
            {
                var findUsersInClaim = await manager.GetUsersForClaimAsync(cClaim).ConfigureAwait(false);
                Assert.Contains(findUsersInClaim, fr => fr.Id == changedUser.Id);
            }

            //Check token
            string changedUserTokenValue = await manager.GetAuthenticationTokenAsync(changedUser, Constants.LoginProviders.GoogleProvider.LoginProvider, tokenName).ConfigureAwait(false);
            Assert.True(changedUserTokenValue == tokenValue);

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.UpdateAsync(null)).ConfigureAwait(false);
        }


    }

    public partial class BaseUserStoreTests<TUser, TContext, TUserStore, TKeyHelper> : IClassFixture<BaseFixture<TUser, TContext, TUserStore, TKeyHelper>>
         where TUser : IdentityUser, IApplicationUser, new()
         where TContext : IdentityCloudContext
         where TUserStore : UserOnlyStore<TUser, TContext, string, Model.IdentityUserClaim, Model.IdentityUserLogin, Model.IdentityUserToken>
         where TKeyHelper : IKeyHelper, new()
    {
        public static readonly string DefaultUserPassword = "M" + Guid.NewGuid().ToString();

        public static bool TablesCreated { get; protected set; }

        protected readonly ITestOutputHelper _output;

        protected BaseFixture<TUser, TContext, string, Model.IdentityUserClaim, Model.IdentityUserLogin, Model.IdentityUserToken, TUserStore, TKeyHelper> userFixture;

        public BaseUserStoreTests(BaseFixture<TUser, TContext, string, Model.IdentityUserClaim, Model.IdentityUserLogin, Model.IdentityUserToken, TUserStore, TKeyHelper> userFix, ITestOutputHelper output)
        {
            userFixture = userFix;

            Initialize();
            _output = output;
        }

        public void Initialize()
        {
            //--Changes to speed up tests that don't require a new user, sharing a static user
            //--Also limiting table creation to once per test run
            if (!TablesCreated)
            {
                using var store = userFixture.CreateUserStore();
                store.CreateTablesIfNotExistsAsync().Wait();
                TablesCreated = true;
            }
            //--
        }

        protected void WriteLineObject<T>(T obj) where T : class
        {
            _output.WriteLine(typeof(T).Name);
            string strLine = obj == null ? "Null" : Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
            _output.WriteLine("{0}", strLine);
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
         => new(Constants.LoginProviders.GoogleProvider.LoginProvider,
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
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = GenTestUser();
            var user2 = GenTestUser();
            var result = await manager.CreateAsync(user).ConfigureAwait(false);
            Assert.True(result.Succeeded, string.Concat(result.Errors.Select(e => e.Code)));

            user2.UserName = user.UserName;
            var result2 = await manager.CreateAsync(user2).ConfigureAwait(false);
            Assert.False(result2.Succeeded);
            Assert.True(new IdentityErrorDescriber().DuplicateUserName(user.UserName).Code
                == result2.Errors.First().Code);
        }

        public virtual async Task CheckDupEmail()
        {
            using var store = userFixture.CreateUserStore();
            IdentityOptions options = new IdentityOptions();
            options.User.RequireUniqueEmail = true;
            using var manager = userFixture.CreateUserManager(options);
            var user = GenTestUser();
            var user2 = GenTestUser();
            var result1 = await manager.CreateAsync(user).ConfigureAwait(false);
            Assert.True(result1.Succeeded, string.Concat(result1.Errors.Select(e => e.Code)));

            user2.Email = user.Email;
            var result2 = await manager.CreateAsync(user2).ConfigureAwait(false);

            Assert.False(result2.Succeeded);
            Assert.True(new IdentityErrorDescriber().DuplicateEmail(user.Email).Code
                == result2.Errors.First().Code);
        }

        public virtual async Task CreateUserTest()
        {
            WriteLineObject(await CreateTestUserAsync<TUser>().ConfigureAwait(false));
        }

        public virtual void MapEntityTest()
        {
            var user = GenTestUser();
            Type type = user.GetType();
            var entity = new TableEntity(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty)
                .ToDictionary(p => p.Name, p => (object)p.GetValue(user)));
            var sw = new Stopwatch();
            sw.Start();
            var testMap = entity.MapTableEntity<TUser>();
            sw.Stop();
            var getout = string.Format("Take1 {0} ms", sw.Elapsed.TotalMilliseconds);
            _output.WriteLine(getout);

            sw.Reset();
            var entity2 = new TableEntity(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty)
                .ToDictionary(p => p.Name, p => (object)p.GetValue(user)));
            sw.Start();
            var testMap2 = entity2.MapTableEntity<TUser>();
            sw.Stop();
            var getout2 = string.Format("Take2: {0} ms", sw.Elapsed.TotalMilliseconds);
            _output.WriteLine(getout2);

            sw.Reset();
            var entity3 = new TableEntity(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty)
                .ToDictionary(p => p.Name, p => (object)p.GetValue(user)));
            sw.Start();
            var testMap3 = entity3.MapTableEntity<TUser>();
            sw.Stop();
            var getout3 = string.Format("Take3: {0} ms", sw.Elapsed.TotalMilliseconds);
            _output.WriteLine(getout3);

            sw.Reset();
            var entity4 = new TableEntity(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty)
                .ToDictionary(p => p.Name, p => (object)p.GetValue(user)));
            sw.Start();
            var testMap4 = entity4.MapTableEntity<TUser>();
            sw.Stop();
            var getout4 = string.Format("Take4: {0} ms", sw.Elapsed.TotalMilliseconds);
            _output.WriteLine(getout4);
            sw.Reset();

            WriteLineObject(testMap);
        }

        public Task<T> CreateUserAsync<T>()
            where T : Model.IdentityUser<string>, new()
        {
            return CreateTestUserAsync<T>();
        }

        protected virtual async Task<T> CreateTestUserAsync<T>(bool createPassword = true, bool createEmail = true,
            string emailAddress = null) where T : Model.IdentityUser<string>, new()
        {
            string strValidConnection = userFixture.GetConfig().StorageConnectionString;

            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
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
                manager.CreateAsync(user)).ConfigureAwait(false);
            Assert.True(createUserResult.Succeeded, string.Concat(createUserResult.Errors));

            for (int i = 0; i < 2; i++)
            {
                await AddUserClaimHelper(user, GenAdminClaim()).ConfigureAwait(false);
                await AddUserLoginAsyncHelper(user, GenGoogleLogin()).ConfigureAwait(false);
                await manager.SetAuthenticationTokenAsync(user,
                    Constants.LoginProviders.GoogleProvider.LoginProvider,
                    string.Format("TokenName{0}", Guid.NewGuid().ToString()),
                    Guid.NewGuid().ToString()).ConfigureAwait(false);
            }

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.CreateAsync(null)).ConfigureAwait(false);

            var getUserResult = await manager.FindByIdAsync(user.Id).ConfigureAwait(false);
            return getUserResult as T;
        }

        protected async Task<TUser> CreateTestUserLiteAsync(bool createPassword = true, bool createEmail = true,
            string emailAddress = null)
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
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
                await manager.CreateAsync(user, DefaultUserPassword).ConfigureAwait(false) :
                await manager.CreateAsync(user).ConfigureAwait(false);
            Assert.True(taskUser.Succeeded, string.Concat(taskUser.Errors));


            return user;
        }

        public virtual async Task DeleteUser()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = GenTestUser();

            var userCreationResult = await manager.CreateAsync(user, DefaultUserPassword).ConfigureAwait(false);
            Assert.True(userCreationResult.Succeeded, string.Concat(userCreationResult.Errors));

            for (int i = 0; i < 20; i++)
            {
                await store.AddClaimAsync(user, GenAdminClaim()).ConfigureAwait(false);
                await store.AddLoginAsync(user, GenGoogleLogin()).ConfigureAwait(false);
                await store.SetTokenAsync(user,
                    Constants.LoginProviders.GoogleProvider.LoginProvider,
                    string.Format("TokenName{0}", Guid.NewGuid().ToString()),
                    Guid.NewGuid().ToString(), new CancellationToken()).ConfigureAwait(false);

            }

            user = await manager.FindByIdAsync(user.Id).ConfigureAwait(false);
            WriteLineObject(user);

            var sw = new Stopwatch();
            sw.Start();
            var userDelitionResult = await manager.DeleteAsync(user).ConfigureAwait(false);
            sw.Stop();
            Assert.True(userDelitionResult.Succeeded, string.Concat(userCreationResult.Errors));
            _output.WriteLine("DeleteAsync: {0} seconds", sw.Elapsed.TotalSeconds);

            await Task.Delay(2000).ConfigureAwait(false);

            var user2 = await manager.FindByIdAsync(user.Id).ConfigureAwait(false);
            Assert.Null(user2);

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.DeleteAsync(null)).ConfigureAwait(false);
        }

        public virtual async Task UpdateApplicationUser()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = GetTestAppUser();
            WriteLineObject<TUser>(user);
            var taskUser = await manager.CreateAsync(user, DefaultUserPassword).ConfigureAwait(false);
            Assert.True(taskUser.Succeeded, string.Concat(taskUser.Errors));

            string oFirstName = user.FirstName;
            string oLastName = user.LastName;

            var taskFind1 = await manager.FindByNameAsync(user.UserName).ConfigureAwait(false);
            Assert.Equal(oFirstName, taskFind1.FirstName);
            Assert.Equal(oLastName, taskFind1.LastName);

            string cFirstName = string.Format("John_{0}", Guid.NewGuid());
            string cLastName = string.Format("Doe_{0}", Guid.NewGuid());

            user.FirstName = cFirstName;
            user.LastName = cLastName;

            var taskUserUpdate = await manager.UpdateAsync(user).ConfigureAwait(false);
            Assert.True(taskUserUpdate.Succeeded, string.Concat(taskUserUpdate.Errors));

            var taskFind = await manager.FindByNameAsync(user.UserName).ConfigureAwait(false);
            Assert.Equal(cFirstName, taskFind.FirstName);
            Assert.Equal(cLastName, taskFind.LastName);
        }

        public virtual async Task UpdateUser()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = GenTestUser();
            WriteLineObject<IdentityUser>(user);
            var creationResult = await manager.CreateAsync(user, DefaultUserPassword).ConfigureAwait(false);
            Assert.True(creationResult.Succeeded, string.Concat(creationResult.Errors));

            var updationResult = await manager.UpdateAsync(user).ConfigureAwait(false);
            Assert.True(updationResult.Succeeded, string.Concat(updationResult.Errors));

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.UpdateAsync(null)).ConfigureAwait(false);
        }

        public virtual async Task ChangeUserName()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var firstUser = await CreateTestUserAsync<TUser>().ConfigureAwait(false);
            _output.WriteLine("{0}", "Original User");
            WriteLineObject(firstUser);
            string originalPlainUserName = firstUser.UserName;
            string originalUserId = firstUser.Id;
            string userNameChange = Guid.NewGuid().ToString("N");

            const int count = 2;
            var firstUserClaims = await manager.GetClaimsAsync(firstUser).ConfigureAwait(false);
            Assert.True(firstUserClaims.Count == count);
            var firstUserLogins = await manager.GetLoginsAsync(firstUser).ConfigureAwait(false);
            Assert.True(firstUserLogins.Count == count);

            string tokenValue = Guid.NewGuid().ToString();
            string tokenName = string.Format("TokenName{0}", Guid.NewGuid().ToString());

            await manager.SetAuthenticationTokenAsync(firstUser,
                Constants.LoginProviders.GoogleProvider.LoginProvider,
                tokenName,
                tokenValue).ConfigureAwait(false);

            var sw = new Stopwatch();
            sw.Start();
            var userUpdate = await manager.SetUserNameAsync(firstUser, userNameChange).ConfigureAwait(false);
            sw.Stop();
            _output.WriteLine("UpdateAsync(ChangeUserName): {0} seconds", sw.Elapsed.TotalSeconds);
            Assert.True(userUpdate.Succeeded, string.Concat(userUpdate.Errors));

            await Task.Delay(200).ConfigureAwait(false);
            var userChangedResult = await manager.FindByNameAsync(userNameChange).ConfigureAwait(false);
            var changedUser = userChangedResult;
            _output.WriteLine("{0}", "Changed User");
            WriteLineObject<IdentityUser>(changedUser);

            Assert.NotNull(changedUser);
            Assert.False(originalPlainUserName.Equals(changedUser.UserName, StringComparison.OrdinalIgnoreCase), "UserName property not updated.");

            var changedUserClaims = await manager.GetClaimsAsync(changedUser).ConfigureAwait(false);
            Assert.True(changedUserClaims.Count == count);
            var changedUserLogins = await manager.GetLoginsAsync(changedUser).ConfigureAwait(false);
            Assert.True(changedUserLogins.Count == count);


            Assert.Equal<int>(firstUserClaims.Count, changedUserClaims.Count);

            Assert.Equal<int>(firstUserLogins.Count, changedUserLogins.Count);

            //Immutable Id
            Assert.Equal(originalUserId, changedUser.Id);

            //Check email
            var findEmailResult = await manager.FindByEmailAsync(changedUser.Email).ConfigureAwait(false);
            Assert.NotNull(findEmailResult);

            //Check the old username is should be the same and still work now.
            var oldUser = await manager.FindByIdAsync(originalUserId).ConfigureAwait(false);
            Assert.NotNull(oldUser);



            //Check logins
            foreach (var log in changedUserLogins)
            {
                var findLoginResult = await manager.FindByLoginAsync(log.LoginProvider, log.ProviderKey).ConfigureAwait(false);
                Assert.NotNull(findLoginResult);
                Assert.Equal(originalUserId, findLoginResult.Id.ToString());
            }


            //Check claims indexes
            foreach (var cClaim in changedUserClaims)
            {
                var findUsersInClaim = await manager.GetUsersForClaimAsync(cClaim).ConfigureAwait(false);
                Assert.Contains(findUsersInClaim, fr => fr.Id == changedUser.Id);
            }

            //Check token
            string changedUserTokenValue = await manager.GetAuthenticationTokenAsync(changedUser, Constants.LoginProviders.GoogleProvider.LoginProvider, tokenName).ConfigureAwait(false);
            Assert.True(changedUserTokenValue == tokenValue);

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.UpdateAsync(null)).ConfigureAwait(false);
        }

        public virtual async Task FindUserByEmail()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = await CreateUserAsync<TUser>().ConfigureAwait(false);
            WriteLineObject<TUser>(user);

            var sw = new Stopwatch();
            sw.Start();
            var findUserResult = await manager.FindByEmailAsync(user.Email).ConfigureAwait(false);
            sw.Stop();
            _output.WriteLine("FindByEmailAsync: {0} seconds", sw.Elapsed.TotalSeconds);

            Assert.Equal(user.Email, findUserResult.Email);
        }

        public virtual async Task FindUsersByEmail()
        {
            string strEmail = Guid.NewGuid().ToString() + "@live.com";

            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            int createdCount = 51;
            for (int i = 0; i < createdCount; i++)
            {
                await CreateTestUserLiteAsync(true, true, strEmail).ConfigureAwait(false);
            }

            _output.WriteLine("FindAllByEmailAsync: {0}", strEmail);
            var sw = new Stopwatch();
            sw.Start();
            var allResult = await store.FindAllByEmailAsync(strEmail).ConfigureAwait(false);
            sw.Stop();
            _output.WriteLine($"{nameof(store.FindAllByEmailAsync)}: {sw.Elapsed.Milliseconds} ms");
            _output.WriteLine("Users Found: {0}", allResult.Count());
            Assert.Equal<int>(createdCount, allResult.Count());

            var listCreated = allResult.ToList();

            //Change email and check results
            string strEmailChanged = Guid.NewGuid().ToString() + "@live.com";
            var userToChange = listCreated.Last();
            await manager.SetEmailAsync(userToChange, strEmailChanged).ConfigureAwait(false);
            var changedResult = await manager.FindByEmailAsync(strEmailChanged).ConfigureAwait(false);
            Assert.Equal(userToChange.Id, changedResult.Id);
            Assert.NotEqual<string>(strEmail, changedResult.Email);

            //Make sure changed user doesn't show up in previous query
            sw.Reset();
            sw.Start();
            allResult = await store.FindAllByEmailAsync(strEmail).ConfigureAwait(false);
            sw.Stop();
            _output.WriteLine($"{nameof(store.FindAllByEmailAsync)}: {sw.Elapsed.Milliseconds} ms");
            _output.WriteLine("Users Found: {0}", allResult.Count());
            Assert.Equal<int>(listCreated.Count - 1, allResult.Count());
        }

        public virtual async Task FindUserById()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = await CreateUserAsync<TUser>().ConfigureAwait(false);
            var sw = new Stopwatch();
            sw.Start();
            var result = await manager.FindByIdAsync(user.Id).ConfigureAwait(false);
            sw.Stop();
            _output.WriteLine($"{nameof(manager.FindByIdAsync)}: {sw.Elapsed.TotalSeconds} seconds");

            Assert.Equal(user.Id, result.Id);
        }

        public virtual async Task FindUserByName()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = await CreateUserAsync<TUser>().ConfigureAwait(false);
            WriteLineObject<IdentityUser>(user);
            var sw = new Stopwatch();
            sw.Start();
            var result = await manager.FindByNameAsync(user.UserName).ConfigureAwait(false);
            sw.Stop();
            _output.WriteLine($"{nameof(manager.FindByNameAsync)}: {sw.Elapsed.TotalSeconds} seconds");

            Assert.Equal(user.UserName, result.UserName);

            sw.Reset();
            sw.Start();
            var result1 = manager.Users.Where(w => w.UserName == user.UserName).ToList().FirstOrDefault();
            sw.Stop();
            _output.WriteLine("Users where UserName: {0} seconds", sw.Elapsed.TotalSeconds);
            Assert.Equal(user.UserName, result1.UserName);
        }

        public virtual async Task AddUserLogin()
        {
            using var manager = userFixture.CreateUserManager();
            var user = await CreateTestUserLiteAsync(createPassword: false).ConfigureAwait(false);
            WriteLineObject(user);
            var loginInfo = GenGoogleLogin();
            await AddUserLoginAsyncHelper(user, loginInfo).ConfigureAwait(false);

            var loginsResult = await manager.GetLoginsAsync(user).ConfigureAwait(false);
            Assert.Contains(loginsResult,
                (log) => log.LoginProvider == loginInfo.LoginProvider
                    && log.ProviderKey == loginInfo.ProviderKey); //, "LoginInfo not found: GetLoginsAsync");

            var sw = new Stopwatch();
            sw.Start();
            var loginResult2 = await manager.FindByLoginAsync(loginsResult.First().LoginProvider, loginsResult.First().ProviderKey).ConfigureAwait(false);
            sw.Stop();
            _output.WriteLine(string.Format($"{nameof(manager.FindByLoginAsync)}: {sw.Elapsed.TotalSeconds} seconds"));
            Assert.Equal(user.Id, loginResult2.Id);
        }

        public async Task AddUserLoginAsyncHelper(TUser user, UserLoginInfo loginInfo)
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var loginResult = await manager.AddLoginAsync(user, loginInfo).ConfigureAwait(false);
            Assert.True(loginResult.Succeeded, string.Concat(loginResult.Errors));
        }

        public virtual async Task AddRemoveUserToken()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = GenTestUser();
            WriteLineObject<IdentityUser>(user);
            var userResult = await manager.CreateAsync(user, DefaultUserPassword).ConfigureAwait(false);
            Assert.True(userResult.Succeeded, string.Concat(userResult.Errors));

            string tokenValue = Guid.NewGuid().ToString();
            string tokenName = string.Format("TokenName{0}", Guid.NewGuid().ToString());
            string tokenName2 = string.Format("TokenName2{0}", Guid.NewGuid().ToString());

            await manager.SetAuthenticationTokenAsync(user,
                Constants.LoginProviders.GoogleProvider.LoginProvider,
                tokenName,
                tokenValue).ConfigureAwait(false);

            string getTokenValue = await manager.GetAuthenticationTokenAsync(user,
                Constants.LoginProviders.GoogleProvider.LoginProvider,
                tokenName).ConfigureAwait(false);
            Assert.NotNull(tokenName);
            Assert.Equal(getTokenValue, tokenValue);

            await manager.SetAuthenticationTokenAsync(user,
                Constants.LoginProviders.GoogleProvider.LoginProvider,
                tokenName2,
                tokenValue).ConfigureAwait(false);

            await manager.RemoveAuthenticationTokenAsync(user,
                Constants.LoginProviders.GoogleProvider.LoginProvider,
                tokenName).ConfigureAwait(false);

            getTokenValue = await manager.GetAuthenticationTokenAsync(user,
                Constants.LoginProviders.GoogleProvider.LoginProvider,
                tokenName).ConfigureAwait(false);
            Assert.Null(getTokenValue);

            getTokenValue = await manager.GetAuthenticationTokenAsync(user,
                Constants.LoginProviders.GoogleProvider.LoginProvider,
                tokenName2).ConfigureAwait(false);
            Assert.NotNull(getTokenValue);
            Assert.Equal(getTokenValue, tokenValue);
        }

        public virtual async Task AddRemoveUserLogin()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = GenTestUser();
            WriteLineObject<IdentityUser>(user);
            var userResult = await manager.CreateAsync(user, DefaultUserPassword).ConfigureAwait(false);
            Assert.True(userResult.Succeeded, string.Concat(userResult.Errors));

            var loginInfo = GenGoogleLogin();
            var addLoginResult = await manager.AddLoginAsync(user, loginInfo).ConfigureAwait(false);
            Assert.True(addLoginResult.Succeeded, string.Concat(addLoginResult.Errors));

            var getLoginResult = await manager.GetLoginsAsync(user).ConfigureAwait(false);
            Assert.Contains(getLoginResult,
                (log) => log.LoginProvider == loginInfo.LoginProvider
                    && log.ProviderKey == loginInfo.ProviderKey); //, "LoginInfo not found: GetLoginsAsync");

            var getLoginResult2 = await manager.FindByLoginAsync(getLoginResult.First().LoginProvider, getLoginResult.First().ProviderKey).ConfigureAwait(false);
            Assert.NotNull(getLoginResult2);

            var userRemoveLoginResultNeg1 = await manager.RemoveLoginAsync(user, string.Empty, loginInfo.ProviderKey).ConfigureAwait(false);
            var userRemoveLoginResultNeg2 = await manager.RemoveLoginAsync(user, loginInfo.LoginProvider, string.Empty).ConfigureAwait(false);
            var userRemoveLoginResult = await manager.RemoveLoginAsync(user, loginInfo.LoginProvider, loginInfo.ProviderKey).ConfigureAwait(false);
            Assert.True(userRemoveLoginResult.Succeeded, string.Concat(userRemoveLoginResult.Errors));

            var loginGetResult3 = await manager.GetLoginsAsync(user).ConfigureAwait(false);
            Assert.DoesNotContain(loginGetResult3, (log) => true);// , "LoginInfo not removed");

            //Negative cases
            var loginFindNeg = await manager.FindByLoginAsync("asdfasdf", "http://4343443dfaksjfaf").ConfigureAwait(false);
            Assert.Null(loginFindNeg);

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.AddLoginAsync(null, loginInfo)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.AddLoginAsync(user, null)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.RemoveLoginAsync(null, loginInfo.ProviderKey, loginInfo.LoginProvider)).ConfigureAwait(false);
            // TODO: check why null login provider is accepted
            // await Assert.ThrowsAsync<ArgumentNullException>(() => store.RemoveLoginAsync(user, null, null)).ConfigureAwait(false);
            // await Assert.ThrowsAsync<ArgumentNullException>(() => store.FindByLoginAsync(null, null)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetLoginsAsync(null)).ConfigureAwait(false);
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
        //                await CreateTestUserLiteAsync(true, true).ConfigureAwait(false);
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
            using var manager = userFixture.CreateUserManager();

            var user = await CreateTestUserLiteAsync().ConfigureAwait(false);
            WriteLineObject<TUser>(user);
            var c1 = GenUserClaim();
            var c2 = GenUserClaimEmptyType();

            await AddUserClaimHelper(user, c1).ConfigureAwait(false);
            await AddUserClaimHelper(user, c2).ConfigureAwait(false);

            var claims = await manager.GetClaimsAsync(user).ConfigureAwait(false);
            Assert.Contains(claims, (c) => c.Value == c1.Value && c.ValueType == c1.ValueType); //, "Claim not found");
            Assert.Contains(claims, (c) => c.Value == c2.Value && c.ValueType == c2.ValueType); //, "Claim not found");
        }

        protected async Task AddUserClaimHelper(TUser user, Claim claim)
        {
            using var manager = userFixture.CreateUserManager();
            var userClaim = await manager.AddClaimAsync(user, claim).ConfigureAwait(false);
            Assert.True(userClaim.Succeeded, string.Concat(userClaim.Errors.Select(e => e.Code)));
        }

        public virtual async Task GetUsersByClaim()
        {
            var claim = GenUserClaim();
            using var store = userFixture.CreateUserStore();
            using (var manager = userFixture.CreateUserManager())
            {
                int userCount = 101;
                Stopwatch sw = new Stopwatch();
                sw.Start();
                TUser tempUser = null;
                for (int i = 0; i < userCount; i++)
                {
                    tempUser = await CreateTestUserLiteAsync(true, true).ConfigureAwait(false);
                    await store.AddClaimAsync(tempUser, claim).ConfigureAwait(false);
                }
                sw.Stop();
                _output.WriteLine("GenerateUsers(): {0} user count", userCount);
                _output.WriteLine("GenerateUsers(): {0} seconds", sw.Elapsed.TotalSeconds);

                sw.Reset();
                sw.Start();
                var users = await manager.GetUsersForClaimAsync(claim).ConfigureAwait(false);
                sw.Stop();
                _output.WriteLine($"{nameof(manager.GetUsersForClaimAsync)}: {sw.Elapsed.TotalSeconds} seconds");
                _output.WriteLine($"{nameof(manager.GetUsersForClaimAsync)}: {users.Count} user count");
                Assert.Equal(userCount, users.Count);
            }

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetUsersForClaimAsync(null)).ConfigureAwait(false);
        }

        public virtual async Task AddRemoveUserClaim()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = await CreateTestUserLiteAsync().ConfigureAwait(false);
            WriteLineObject<TUser>(user);
            Claim claim = GenAdminClaim();
            var addClaimResult = await manager.AddClaimAsync(user, claim).ConfigureAwait(false);
            Assert.True(addClaimResult.Succeeded, string.Concat(addClaimResult.Errors));

            var claims = await manager.GetClaimsAsync(user).ConfigureAwait(false);
            Assert.Contains(claims, (c) => c.Value == claim.Value && c.Type == claim.Type); //, "Claim not found");

            var userRemoveClaimResult = await manager.RemoveClaimAsync(user, claim).ConfigureAwait(false);
            Assert.True(userRemoveClaimResult.Succeeded, string.Concat(userRemoveClaimResult.Errors));

            var claims2 = await manager.GetClaimsAsync(user).ConfigureAwait(false);
            Assert.DoesNotContain(claims2, (c) => c.Value == claim.Value && c.Type == claim.Type); //, "Claim not removed");

            //adding test for removing an empty claim
            Claim claimEmpty = GenAdminClaimEmptyValue();
            var addClaimResult2 = await manager.AddClaimAsync(user, claimEmpty).ConfigureAwait(false);
            var removeClaimResult2 = await manager.RemoveClaimAsync(user, claimEmpty).ConfigureAwait(false);
            Assert.True(addClaimResult2.Succeeded, string.Concat(addClaimResult2.Errors));
            Assert.True(removeClaimResult2.Succeeded, string.Concat(removeClaimResult2.Errors));

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.AddClaimAsync(null, claim)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.AddClaimAsync(user, null)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.RemoveClaimAsync(null, claim)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.RemoveClaimAsync(user, null)).ConfigureAwait(false);
            await store.RemoveClaimAsync(user, new Claim(string.Empty, Guid.NewGuid().ToString())).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.RemoveClaimAsync(user, new Claim(claim.Type, null))).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetClaimsAsync(null)).ConfigureAwait(false);
        }

        public virtual async Task AddReplaceRemoveUserClaim()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = await CreateTestUserLiteAsync(createPassword: true, createEmail: true).ConfigureAwait(false);
            WriteLineObject<TUser>(user);
            Claim claim = GenAdminClaim();
            await store.AddClaimAsync(user, claim).ConfigureAwait(false);


            var claims = await manager.GetClaimsAsync(user).ConfigureAwait(false);
            Assert.Contains(claims, (c) => c.Value == claim.Value && c.Type == claim.Type); //, "Claim not found");


            Claim nClaim = new Claim(claim.Type, "new claim value here");
            var userReplaceClaimResult = await manager.ReplaceClaimAsync(user, claim, nClaim).ConfigureAwait(false);
            Assert.True(userReplaceClaimResult.Succeeded, string.Concat(userReplaceClaimResult.Errors));

            var claims2 = await manager.GetClaimsAsync(user).ConfigureAwait(false);
            Assert.DoesNotContain(claims2, (c) => c.Value == claim.Value && c.Type == claim.Type); //, "Claim not replaced, old claim found");
            Assert.Contains(claims2, (c) => c.Value == nClaim.Value && c.Type == nClaim.Type); //, "Claim not replaced, new claim not found.");


            await Assert.ThrowsAsync<ArgumentNullException>(() => store.ReplaceClaimAsync(null, claim, nClaim)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.ReplaceClaimAsync(user, null, nClaim)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.ReplaceClaimAsync(user, claim, null)).ConfigureAwait(false);

            await store.RemoveClaimAsync(user, nClaim).ConfigureAwait(false);
            claims2 = await manager.GetClaimsAsync(user).ConfigureAwait(false);
            Assert.DoesNotContain(claims2, (c) => c.Value == nClaim.Value && c.Type == nClaim.Type); //, "Claim not removed, new claim found.");
        }

        public virtual async Task CanFindByNameIfImmutableIdSetUp()
        {
            var userStore = userFixture.CreateUserStore();

            var user = GenTestUser();
            _ = await userFixture.CreateUserManager().CreateAsync(user).ConfigureAwait(false);

            var userFound = await userStore.FindByNameAsync(user.UserName).ConfigureAwait(false);


            Assert.NotNull(user);
            Assert.Equal(user.Id, userFound.Id);
            Assert.Equal(user.PartitionKey, userFound.PartitionKey);
            Assert.Equal(user.RowKey, userFound.RowKey);
        }

        public virtual async Task CanFindByIdIfImmutableIdSetUp()
        {
            var userStore = userFixture.CreateUserStore();

            var user = GenTestUser();
            _ = await userFixture.CreateUserManager().CreateAsync(user).ConfigureAwait(false);

            var userFound = await userStore.FindByIdAsync(user.Id).ConfigureAwait(false);

            Assert.NotNull(user);
            Assert.Equal(user.Id, userFound.Id);
            Assert.Equal(user.PartitionKey, userFound.PartitionKey);
            Assert.Equal(user.RowKey, userFound.RowKey);
        }

        public virtual async Task ThrowIfDisposed()
        {
            var store = userFixture.CreateUserStore();
            store.Dispose();
            GC.Collect();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => store.DeleteAsync(null)).ConfigureAwait(false);
        }
    }

}
