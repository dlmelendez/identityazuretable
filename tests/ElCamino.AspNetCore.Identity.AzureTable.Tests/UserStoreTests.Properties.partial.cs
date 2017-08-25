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

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests
{
    public partial class UserStoreTests : IClassFixture<UserFixture<ApplicationUser, IdentityRole, IdentityCloudContext>>
    {
        [Fact(DisplayName = "AccessFailedCount")]
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
        public async Task AccessFailedCount()
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

                using (var manager = userFixture.CreateUserManager(store, idOptions))
                {
                    var user = await CreateTestUserAsync<ApplicationUser>();
                    var accessFailedCount = await manager.GetAccessFailedCountAsync(user);

                    Assert.Equal<int>(user.AccessFailedCount, accessFailedCount);

                    var taskAccessResult = await manager.AccessFailedAsync(user);

                    Assert.True(taskAccessResult.Succeeded, string.Concat(taskAccessResult.Errors.Select(e => e.Code).ToArray()));
                    await manager.AccessFailedAsync(user);
                    await manager.AccessFailedAsync(user);

                    DateTime dtUtc = DateTime.UtcNow;
                    user = await manager.FindByIdAsync(user.Id);
                    Assert.True(user.LockoutEndDateUtc.HasValue);
                    Assert.True(user.LockoutEndDateUtc.Value < dtUtc.Add(idOptions.Lockout.DefaultLockoutTimeSpan));
                    Assert.True(user.LockoutEndDateUtc.Value > dtUtc.Add(idOptions.Lockout.DefaultLockoutTimeSpan.Add(TimeSpan.FromMinutes(-1.0))));

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

        private async Task SetValidateEmailAsync(UserManager<ApplicationUser> manager,
            UserStore<ApplicationUser> store,
            ApplicationUser user,
            string strNewEmail)
        {
            string originalEmail = user.Email;
            var emailResult = await manager.SetEmailAsync(user, strNewEmail);
            Assert.True(emailResult.Succeeded, string.Concat(emailResult.Errors));

            var email = await manager.GetEmailAsync(user);
            Assert.Equal<string>(strNewEmail, email);

            if (!string.IsNullOrWhiteSpace(strNewEmail))
            {
                var taskFind = manager.FindByEmailAsync(strNewEmail);
                taskFind.Wait();
                Assert.Equal<string>(strNewEmail, taskFind.Result.Email);
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

        [Fact(DisplayName = "EmailNone")]
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
        public async Task EmailNone()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = await CreateTestUserAsync<ApplicationUser>(false, false);
                    string strNewEmail = string.Format("{0}@hotmail.com", Guid.NewGuid().ToString("N"));
                    await SetValidateEmailAsync(manager, store, user, strNewEmail);
                    await SetValidateEmailAsync(manager, store, user, string.Empty);
                }
            }
        }

        [Fact(DisplayName = "Email")]
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
        public async Task Email()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = CurrentUser;

                    string strNewEmail = string.Format("{0}@gmail.com", Guid.NewGuid().ToString("N"));
                    await SetValidateEmailAsync(manager, store, user, strNewEmail);

                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetEmailAsync(null));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetEmailAsync(null, strNewEmail));
                    // TODO: check
                    // await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetEmailAsync(user, null));
                }
            }
        }

        [Fact(DisplayName = "EmailConfirmed")]
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
        public async Task EmailConfirmed()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = await CreateTestUserAsync<ApplicationUser>();
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

        [Fact(DisplayName = "LockoutEnabled")]
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
        public async Task LockoutEnabled()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = CurrentUser;
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
                    user.LockoutEndDateUtc = tmpDate;
                    lockoutEndDate = await store.GetLockoutEndDateAsync(user);
                    Assert.Equal<DateTimeOffset?>(new DateTimeOffset?(tmpDate), lockoutEndDate);

                    user.LockoutEndDateUtc = null;
                    lockoutEndDate = await store.GetLockoutEndDateAsync(user);
                    Assert.Equal<DateTimeOffset?>(new DateTimeOffset?(), lockoutEndDate);

                    var minOffSet = DateTimeOffset.MinValue;
                    var setLockoutEndDateResult2 = store.SetLockoutEndDateAsync(user, minOffSet);
                    Assert.NotNull(user.LockoutEndDateUtc);

                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetLockoutEnabledAsync(null));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetLockoutEndDateAsync(null));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetLockoutEndDateAsync(null, offSet));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetLockoutEnabledAsync(null, false));
                }
            }
        }

        [Fact(DisplayName = "PhoneNumber")]
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
        public async Task PhoneNumber()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = CurrentUser;

                    string strNewPhoneNumber = "542-887-3434";
                    var setPhoneNumberResult = await manager.SetPhoneNumberAsync(user, strNewPhoneNumber);
                    Assert.True(setPhoneNumberResult.Succeeded, string.Concat(setPhoneNumberResult.Errors));

                    var phoneNumber = await manager.GetPhoneNumberAsync(user);
                    Assert.Equal<string>(strNewPhoneNumber, phoneNumber);

                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetPhoneNumberAsync(null));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetPhoneNumberAsync(null, strNewPhoneNumber));
                    // TODO: check
                    // await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetPhoneNumberAsync(user, null));
                }
            }
        }

        [Fact(DisplayName = "PhoneNumberConfirmed")]
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
        public async Task PhoneNumberConfirmed()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = await CreateTestUserAsync<ApplicationUser>();
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

        [Fact(DisplayName = "TwoFactorEnabled")]
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
        public async Task TwoFactorEnabled()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = CurrentUser;

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

        [Fact(DisplayName = "PasswordHash")]
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
        public async Task PasswordHash()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = CurrentUser;
                    string passwordPlain = Guid.NewGuid().ToString("N");
                    string passwordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, passwordPlain);
                    await store.SetPasswordHashAsync(user, passwordHash);

                    var hasPasswordHash = await manager.HasPasswordAsync(user);
                    Assert.True(hasPasswordHash, "PasswordHash not set");

                    var passwordHashResult = await store.GetPasswordHashAsync(user);
                    Assert.Equal<string>(passwordHash, passwordHashResult);

                    user.PasswordHash = passwordHash;

                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetPasswordHashAsync(null));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.HasPasswordAsync(null));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetPasswordHashAsync(null, passwordHash));
                    // TODO: check why existed
                    // await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetPasswordHashAsync(user, null));
                }
            }
        }

        [Fact(DisplayName = "SecurityStamp")]
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
        public async Task SecurityStamp()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = await CreateTestUserAsync<ApplicationUser>();
                    var stamp = await manager.GetSecurityStampAsync(user);
                    Assert.Equal<string>(user.SecurityStamp, stamp);

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
