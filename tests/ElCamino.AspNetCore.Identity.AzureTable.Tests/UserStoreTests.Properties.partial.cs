// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.WindowsAzure.Storage.Table;
using Xunit;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using ElCamino.Web.Identity.AzureTable.Tests.ModelTests;
using ElCamino.Web.Identity.AzureTable.Tests.Fixtures;
using IdentityUser = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityUser<string>;
using IdentityRole = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityRole;


namespace ElCamino.AspNetCore.Identity.AzureTable.Tests
{
    public partial class UserStoreTests : BaseUserStoreTests<ApplicationUser, IdentityRole, IdentityCloudContext, UserStore<ApplicationUser, IdentityRole, IdentityCloudContext>>
    {
        [Fact(DisplayName = "AccessFailedCount")]
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
        public override Task AccessFailedCount()
        {
            return base.AccessFailedCount();
        }

        [Fact(DisplayName = "Email")]
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
        public override Task Email()
        {
            return base.Email();
        }

        [Fact(DisplayName = "EmailConfirmed")]
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
        public override Task EmailConfirmed()
        {
            return base.EmailConfirmed();
        }

        [Fact(DisplayName = "EmailNone")]
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
        public override Task EmailNone()
        {
            return base.EmailNone();
        }

        [Fact(DisplayName = "LockoutEnabled")]
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
        public override Task LockoutEnabled()
        {
            return base.LockoutEnabled();
        }

        [Fact(DisplayName = "PasswordHash")]
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
        public override Task PasswordHash()
        {
            return base.PasswordHash();
        }

        [Fact(DisplayName = "PhoneNumber")]
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
        public override Task PhoneNumber()
        {
            return base.PhoneNumber();
        }

        [Fact(DisplayName = "PhoneNumberConfirmed")]
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
        public override Task PhoneNumberConfirmed()
        {
            return base.PhoneNumberConfirmed();
        }

        [Fact(DisplayName = "SecurityStamp")]
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
        public override Task SecurityStamp()
        {
            return base.SecurityStamp();
        }

        [Fact(DisplayName = "TwoFactorEnabled")]
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
        public override Task TwoFactorEnabled()
        {
            return base.TwoFactorEnabled();
        }
    }

    public partial class BaseUserStoreTests<TUser, TRole, TContext, TUserStore> : IClassFixture<UserFixture<TUser, TRole, TContext, TUserStore>>
        where TUser : IdentityUser, IApplicationUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityCloudContext, new()
        where TUserStore : UserStoreV2<TUser, TRole, TContext>
    {
        public virtual async Task AccessFailedCount()
        {
            using (var store = userFixture.CreateUserStore())
            {
                var idOptions = new IdentityOptions()
                {
                    Lockout = new LockoutOptions()
                    {
                        DefaultLockoutTimeSpan = TimeSpan.FromHours(2),
                        MaxFailedAccessAttempts = 2
                    }
                };

                using (var manager = userFixture.CreateUserManager(idOptions))
                {
                    var user = await CreateTestUserLiteAsync(createPassword: true, createEmail: true);
                    var accessFailedCount = await manager.GetAccessFailedCountAsync(user);

                    Assert.Equal<int>(user.AccessFailedCount, accessFailedCount);

                    var taskAccessResult = await manager.AccessFailedAsync(user);

                    Assert.True(taskAccessResult.Succeeded, string.Concat(taskAccessResult.Errors.Select(e => e.Code).ToArray()));
                    await manager.AccessFailedAsync(user);
                    await manager.AccessFailedAsync(user);

                    DateTime dtUtc = DateTime.UtcNow;
                    user = await manager.FindByIdAsync(user.Id);
                    Assert.True(user.LockoutEnd.HasValue);
                    Assert.True(user.LockoutEnd.Value < dtUtc.Add(idOptions.Lockout.DefaultLockoutTimeSpan));
                    Assert.True(user.LockoutEnd.Value > dtUtc.Add(idOptions.Lockout.DefaultLockoutTimeSpan.Add(TimeSpan.FromMinutes(-1.0))));

                    var resetAccessFailedCountResult = await manager.ResetAccessFailedCountAsync(user);
                    Assert.True(resetAccessFailedCountResult.Succeeded, string.Concat(resetAccessFailedCountResult.Errors));

                    user = await manager.FindByIdAsync(user.Id);
                    Assert.True(user.AccessFailedCount == 0);

                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetAccessFailedCountAsync(null));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.IncrementAccessFailedCountAsync(null));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.ResetAccessFailedCountAsync(null));
                }
            }
        }

        private async Task SetValidateEmailAsync(UserManager<TUser> manager,
            TUserStore store,
            TUser user,
            string strNewEmail)
        {
            string originalEmail = user.Email;
            var emailResult = await manager.SetEmailAsync(user, strNewEmail);
            Assert.True(emailResult.Succeeded, string.Concat(emailResult.Errors));

            var email = await manager.GetEmailAsync(user);
            Assert.Equal(strNewEmail, email);

            if (!string.IsNullOrWhiteSpace(strNewEmail))
            {
                var taskFind = manager.FindByEmailAsync(strNewEmail);
                taskFind.Wait();
                Assert.Equal(strNewEmail, taskFind.Result.Email);
            }
            else
            {
                TableQuery query = new TableQuery();
                query.SelectColumns = new List<string>() { "Id" };
                query.FilterString = TableQuery.GenerateFilterCondition("Id", QueryComparisons.Equal, user.Id);
                query.Take(1);
                var results = store.Context.IndexTable.ExecuteQuerySegmentedAsync(query, new TableContinuationToken()).Result;
                Assert.True(!results.Any(x => x.RowKey.StartsWith("E_")), string.Format("Email index not deleted for user {0}", user.Id));
            }
            //Should not find old by old email.
            if (!string.IsNullOrWhiteSpace(originalEmail))
            {
                var taskFind = manager.FindByEmailAsync(originalEmail);
                taskFind.Wait();
                Assert.Null(taskFind.Result);
            }

        }

        public virtual async Task EmailNone()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    var user = await CreateTestUserLiteAsync(createPassword: false, createEmail: false); 
                    string strNewEmail = string.Format("{0}@hotmail.com", Guid.NewGuid().ToString("N"));
                    await SetValidateEmailAsync(manager, store, user, strNewEmail);
                    await SetValidateEmailAsync(manager, store, user, string.Empty);
                }
            }
        }

        public virtual async Task Email()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    var user = await CreateTestUserLiteAsync(createPassword: true, createEmail: true);

                    string strNewEmail = string.Format("{0}@gmail.com", Guid.NewGuid().ToString("N"));
                    await SetValidateEmailAsync(manager, store, user, strNewEmail);

                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetEmailAsync(null));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetEmailAsync(null, strNewEmail));
                    // TODO: check
                    // await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetEmailAsync(user, null));
                }
            }
        }

        public virtual async Task EmailConfirmed()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    var user = await CreateTestUserLiteAsync(createPassword: true, createEmail: true);
                    var token = await manager.GenerateEmailConfirmationTokenAsync(user);
                    Assert.False(string.IsNullOrWhiteSpace(token), "GenerateEmailConfirmationToken failed.");

                    var confirmation = await manager.ConfirmEmailAsync(user, token);
                    Assert.True(confirmation.Succeeded, string.Concat(confirmation.Errors));

                    user = await manager.FindByEmailAsync(user.Email);
                    var confirmationResult2 = await store.GetEmailConfirmedAsync(user);
                    Assert.True(confirmationResult2, "Email not confirmed");

                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetEmailConfirmedAsync(null, true));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetEmailConfirmedAsync(null));
                }
            }
        }

        public virtual async Task LockoutEnabled()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    var user = await CreateTestUserLiteAsync(createPassword: true, createEmail: true);
                    var enableLockoutResult = await manager.SetLockoutEnabledAsync(user, true);
                    Assert.True(enableLockoutResult.Succeeded, string.Concat(enableLockoutResult.Errors));

                    DateTimeOffset offSet = new DateTimeOffset(DateTime.Now.AddMinutes(3));
                    var setLockoutEndDateResult = await manager.SetLockoutEndDateAsync(user, offSet);
                    Assert.True(setLockoutEndDateResult.Succeeded, string.Concat(setLockoutEndDateResult.Errors));

                    var lockoutEnabled = await manager.GetLockoutEnabledAsync(user);
                    Assert.True(lockoutEnabled, "Lockout not true");

                    var lockoutEndDate = await manager.GetLockoutEndDateAsync(user);
                    Assert.Equal(offSet, lockoutEndDate);

                    DateTime tmpDate = DateTime.UtcNow.AddDays(1);
                    user.LockoutEnd = tmpDate;
                    lockoutEndDate = await store.GetLockoutEndDateAsync(user);
                    Assert.Equal<DateTimeOffset?>(new DateTimeOffset?(tmpDate), lockoutEndDate);

                    user.LockoutEnd = null;
                    lockoutEndDate = await store.GetLockoutEndDateAsync(user);
                    Assert.Equal<DateTimeOffset?>(new DateTimeOffset?(), lockoutEndDate);

                    var minOffSet = DateTimeOffset.MinValue;
                    var setLockoutEndDateResult2 = store.SetLockoutEndDateAsync(user, minOffSet);
                    Assert.NotNull(user.LockoutEnd);

                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetLockoutEnabledAsync(null));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetLockoutEndDateAsync(null));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetLockoutEndDateAsync(null, offSet));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetLockoutEnabledAsync(null, false));
                }
            }
        }

        public virtual async Task PhoneNumber()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    var user = await CreateTestUserLiteAsync(createPassword: true, createEmail: true);

                    string strNewPhoneNumber = "542-887-3434";
                    var setPhoneNumberResult = await manager.SetPhoneNumberAsync(user, strNewPhoneNumber);
                    Assert.True(setPhoneNumberResult.Succeeded, string.Concat(setPhoneNumberResult.Errors));

                    var phoneNumber = await manager.GetPhoneNumberAsync(user);
                    Assert.Equal(strNewPhoneNumber, phoneNumber);

                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetPhoneNumberAsync(null));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetPhoneNumberAsync(null, strNewPhoneNumber));
                    // TODO: check
                    // await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetPhoneNumberAsync(user, null));
                }
            }
        }

        public virtual async Task PhoneNumberConfirmed()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    var user = await CreateTestUserLiteAsync(createPassword: true, createEmail: true);
                    string strNewPhoneNumber = "425-555-1111";
                    var token = await manager.GenerateChangePhoneNumberTokenAsync(user, strNewPhoneNumber);
                    Assert.False(string.IsNullOrWhiteSpace(token), "GeneratePhoneConfirmationToken failed.");

                    var confirmationResult = await manager.ChangePhoneNumberAsync(user, strNewPhoneNumber, token);
                    Assert.True(confirmationResult.Succeeded, string.Concat(confirmationResult.Errors));

                    user = await manager.FindByEmailAsync(user.Email);
                    var confirmation = await store.GetPhoneNumberConfirmedAsync(user);
                    Assert.True(confirmation, "Phone not confirmed");

                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetPhoneNumberConfirmedAsync(null, true));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetPhoneNumberConfirmedAsync(null));
                }
            }
        }

        public virtual async Task TwoFactorEnabled()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    var user = await CreateTestUserLiteAsync(createPassword: true, createEmail: true);

                    bool twoFactorEnabled = true;
                    var setTwoFactorEnabledResult = await manager.SetTwoFactorEnabledAsync(user, twoFactorEnabled);
                    Assert.True(setTwoFactorEnabledResult.Succeeded, string.Concat(setTwoFactorEnabledResult.Errors));

                    var twoFactorEnabledResult = await manager.GetTwoFactorEnabledAsync(user);
                    Assert.Equal<bool>(twoFactorEnabled, twoFactorEnabledResult);

                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetTwoFactorEnabledAsync(null));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetTwoFactorEnabledAsync(null, twoFactorEnabled));
                }
            }
        }

        public virtual async Task PasswordHash()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    var user = await CreateTestUserLiteAsync(createPassword:true, createEmail:true);
                    string passwordPlain = Guid.NewGuid().ToString("N");
                    string passwordHash = new PasswordHasher<TUser>().HashPassword(user, passwordPlain);
                    await store.SetPasswordHashAsync(user, passwordHash);

                    var hasPasswordHash = await manager.HasPasswordAsync(user);
                    Assert.True(hasPasswordHash, "PasswordHash not set");

                    var passwordHashResult = await store.GetPasswordHashAsync(user);
                    Assert.Equal(passwordHash, passwordHashResult);

                    user.PasswordHash = passwordHash;

                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetPasswordHashAsync(null));
                }
            }
        }

        public virtual async Task SecurityStamp()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager())
                {
                    var user = await CreateTestUserLiteAsync(createPassword: true, createEmail: true);
                    var stamp = await manager.GetSecurityStampAsync(user);
                    Assert.Equal(user.SecurityStamp, stamp);

                    string strNewSecurityStamp = Guid.NewGuid().ToString("N");
                    await store.SetSecurityStampAsync(user, strNewSecurityStamp);

                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetSecurityStampAsync(null));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetSecurityStampAsync(null, strNewSecurityStamp));
                    // TODO: check
                    // await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetSecurityStampAsync(user, null));
                }
            }
        }
    }
}
