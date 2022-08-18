// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using ElCamino.Web.Identity.AzureTable.Tests.Fixtures;
using ElCamino.Web.Identity.AzureTable.Tests.ModelTests;
using Microsoft.AspNetCore.Identity;
using Xunit;
using IdentityRole = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityRole;
using IdentityUser = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityUser<string>;

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests
{
    public partial class BaseUserStoreTests<TUser, TRole, TContext, TUserStore, TKeyHelper> : BaseUserStoreTests<TUser, TContext, TUserStore, TKeyHelper>,
        IClassFixture<UserFixture<TUser, TRole, TContext, TUserStore, TKeyHelper>>
         where TUser : IdentityUser, IApplicationUser, new()
         where TRole : IdentityRole, new()
         where TContext : IdentityCloudContext
         where TUserStore : UserStore<TUser, TRole, TContext>
         where TKeyHelper : IKeyHelper, new()
    {
    }

    public partial class BaseUserStoreTests<TUser, TContext, TUserStore, TKeyHelper> : IClassFixture<UserFixture<TUser, TContext, TUserStore, TKeyHelper>>
         where TUser : IdentityUser, IApplicationUser, new()
         where TContext : IdentityCloudContext
         where TUserStore : UserOnlyStore<TUser, TContext, string, Model.IdentityUserClaim, Model.IdentityUserLogin, Model.IdentityUserToken>
         where TKeyHelper : IKeyHelper, new()
    {
        public virtual async Task AccessFailedCount()
        {
            using var store = userFixture.CreateUserStore();
            var idOptions = new IdentityOptions()
            {
                Lockout = new LockoutOptions()
                {
                    DefaultLockoutTimeSpan = TimeSpan.FromHours(2),
                    MaxFailedAccessAttempts = 2
                }
            };

            using var manager = userFixture.CreateUserManager(idOptions);
            var user = await CreateTestUserLiteAsync(createPassword: true, createEmail: true).ConfigureAwait(false);
            var accessFailedCount = await manager.GetAccessFailedCountAsync(user).ConfigureAwait(false);

            Assert.Equal<int>(user.AccessFailedCount, accessFailedCount);

            var taskAccessResult = await manager.AccessFailedAsync(user).ConfigureAwait(false);

            Assert.True(taskAccessResult.Succeeded, string.Concat(taskAccessResult.Errors.Select(e => e.Code).ToArray()));
            await manager.AccessFailedAsync(user).ConfigureAwait(false);
            await manager.AccessFailedAsync(user).ConfigureAwait(false);

            DateTime dtUtc = DateTime.UtcNow;
            user = await manager.FindByIdAsync(user.Id).ConfigureAwait(false);
            Assert.True(user.LockoutEnd.HasValue);
            Assert.True(user.LockoutEnd.Value < dtUtc.Add(idOptions.Lockout.DefaultLockoutTimeSpan));
            Assert.True(user.LockoutEnd.Value > dtUtc.Add(idOptions.Lockout.DefaultLockoutTimeSpan.Add(TimeSpan.FromMinutes(-1.0))));

            var resetAccessFailedCountResult = await manager.ResetAccessFailedCountAsync(user).ConfigureAwait(false);
            Assert.True(resetAccessFailedCountResult.Succeeded, string.Concat(resetAccessFailedCountResult.Errors));

            user = await manager.FindByIdAsync(user.Id).ConfigureAwait(false);
            Assert.True(user.AccessFailedCount == 0);

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetAccessFailedCountAsync(null)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.IncrementAccessFailedCountAsync(null)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.ResetAccessFailedCountAsync(null)).ConfigureAwait(false);
        }

        private static async Task SetValidateEmailAsync(UserManager<TUser> manager,
            TUserStore store,
            TUser user,
            string strNewEmail)
        {
            string originalEmail = user.Email;
            var emailResult = await manager.SetEmailAsync(user, strNewEmail).ConfigureAwait(false);
            Assert.True(emailResult.Succeeded, string.Concat(emailResult.Errors));

            var email = await manager.GetEmailAsync(user).ConfigureAwait(false);
            Assert.Equal(strNewEmail, email);

            if (!string.IsNullOrWhiteSpace(strNewEmail))
            {
                var taskFind = await manager.FindByEmailAsync(strNewEmail).ConfigureAwait(false);
                Assert.Equal(strNewEmail, taskFind.Email);
            }
            else
            {
                var selectColumns = new List<string>() { nameof(IdentityUserIndex.Id) };
                string filterString = TableQuery.GenerateFilterCondition(nameof(IdentityUserIndex.Id), QueryComparisons.Equal, user.Id);
                var results = await store.Context.IndexTable.QueryAsync<TableEntity>(filter: filterString, select: selectColumns).ToListAsync().ConfigureAwait(false);
                Assert.DoesNotContain(results, (x) => x.RowKey.StartsWith(AzureTable.TableConstants.RowKeyConstants.PreFixIdentityUserEmail)); //, string.Format("Email index not deleted for user {0}", user.Id));
            }
            //Should not find old by old email.
            if (!string.IsNullOrWhiteSpace(originalEmail))
            {
                var taskFind = await manager.FindByEmailAsync(originalEmail).ConfigureAwait(false);
                Assert.Null(taskFind);
            }

        }

        public virtual async Task EmailNone()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = await CreateTestUserLiteAsync(createPassword: false, createEmail: false).ConfigureAwait(false);
            string strNewEmail = string.Format("{0}@hotmail.com", Guid.NewGuid().ToString("N"));
            await SetValidateEmailAsync(manager, store, user, strNewEmail).ConfigureAwait(false);
            await SetValidateEmailAsync(manager, store, user, string.Empty).ConfigureAwait(false);
        }

        public virtual async Task Email()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = await CreateTestUserLiteAsync(createPassword: true, createEmail: true).ConfigureAwait(false);

            string strNewEmail = string.Format("{0}@gmail.com", Guid.NewGuid().ToString("N"));
            await SetValidateEmailAsync(manager, store, user, strNewEmail).ConfigureAwait(false);

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetEmailAsync(null)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetEmailAsync(null, strNewEmail)).ConfigureAwait(false);
            // TODO: check
            // await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetEmailAsync(user, null)).ConfigureAwait(false);
        }

        public virtual async Task EmailConfirmed()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = await CreateTestUserLiteAsync(createPassword: true, createEmail: true).ConfigureAwait(false);
            var token = await manager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);
            Assert.False(string.IsNullOrWhiteSpace(token), "GenerateEmailConfirmationToken failed.");

            var confirmation = await manager.ConfirmEmailAsync(user, token).ConfigureAwait(false);
            Assert.True(confirmation.Succeeded, string.Concat(confirmation.Errors));

            user = await manager.FindByEmailAsync(user.Email).ConfigureAwait(false);
            var confirmationResult2 = await store.GetEmailConfirmedAsync(user).ConfigureAwait(false);
            Assert.True(confirmationResult2, "Email not confirmed");

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetEmailConfirmedAsync(null, true)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetEmailConfirmedAsync(null)).ConfigureAwait(false);
        }

        public virtual async Task LockoutEnabled()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = await CreateTestUserLiteAsync(createPassword: true, createEmail: true).ConfigureAwait(false);
            var enableLockoutResult = await manager.SetLockoutEnabledAsync(user, true).ConfigureAwait(false);
            Assert.True(enableLockoutResult.Succeeded, string.Concat(enableLockoutResult.Errors));

            DateTimeOffset offSet = new DateTimeOffset(DateTime.Now.AddMinutes(3));
            var setLockoutEndDateResult = await manager.SetLockoutEndDateAsync(user, offSet).ConfigureAwait(false);
            Assert.True(setLockoutEndDateResult.Succeeded, string.Concat(setLockoutEndDateResult.Errors));

            var lockoutEnabled = await manager.GetLockoutEnabledAsync(user).ConfigureAwait(false);
            Assert.True(lockoutEnabled, "Lockout not true");

            var lockoutEndDate = await manager.GetLockoutEndDateAsync(user).ConfigureAwait(false);
            Assert.Equal(offSet, lockoutEndDate);

            DateTime tmpDate = DateTime.UtcNow.AddDays(1);
            user.LockoutEnd = tmpDate;
            lockoutEndDate = await store.GetLockoutEndDateAsync(user).ConfigureAwait(false);
            Assert.Equal<DateTimeOffset?>(new DateTimeOffset?(tmpDate), lockoutEndDate);

            user.LockoutEnd = null;
            lockoutEndDate = await store.GetLockoutEndDateAsync(user).ConfigureAwait(false);
            Assert.Equal<DateTimeOffset?>(new DateTimeOffset?(), lockoutEndDate);

            var minOffSet = DateTimeOffset.MinValue;
            var setLockoutEndDateResult2 = store.SetLockoutEndDateAsync(user, minOffSet);
            Assert.NotNull(user.LockoutEnd);

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetLockoutEnabledAsync(null)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetLockoutEndDateAsync(null)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetLockoutEndDateAsync(null, offSet)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetLockoutEnabledAsync(null, false)).ConfigureAwait(false);
        }

        public virtual async Task PhoneNumber()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = await CreateTestUserLiteAsync(createPassword: true, createEmail: true).ConfigureAwait(false);

            string strNewPhoneNumber = "542-887-3434";
            var setPhoneNumberResult = await manager.SetPhoneNumberAsync(user, strNewPhoneNumber).ConfigureAwait(false);
            Assert.True(setPhoneNumberResult.Succeeded, string.Concat(setPhoneNumberResult.Errors));

            var phoneNumber = await manager.GetPhoneNumberAsync(user).ConfigureAwait(false);
            Assert.Equal(strNewPhoneNumber, phoneNumber);

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetPhoneNumberAsync(null)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetPhoneNumberAsync(null, strNewPhoneNumber)).ConfigureAwait(false);
            // TODO: check
            // await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetPhoneNumberAsync(user, null)).ConfigureAwait(false);
        }

        public virtual async Task PhoneNumberConfirmed()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = await CreateTestUserLiteAsync(createPassword: true, createEmail: true).ConfigureAwait(false);
            string strNewPhoneNumber = "425-555-1111";
            var token = await manager.GenerateChangePhoneNumberTokenAsync(user, strNewPhoneNumber).ConfigureAwait(false);
            Assert.False(string.IsNullOrWhiteSpace(token), "GeneratePhoneConfirmationToken failed.");

            var confirmationResult = await manager.ChangePhoneNumberAsync(user, strNewPhoneNumber, token).ConfigureAwait(false);
            Assert.True(confirmationResult.Succeeded, string.Concat(confirmationResult.Errors));

            user = await manager.FindByEmailAsync(user.Email).ConfigureAwait(false);
            var confirmation = await store.GetPhoneNumberConfirmedAsync(user).ConfigureAwait(false);
            Assert.True(confirmation, "Phone not confirmed");

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetPhoneNumberConfirmedAsync(null, true)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetPhoneNumberConfirmedAsync(null)).ConfigureAwait(false);
        }

        public virtual async Task TwoFactorEnabled()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = await CreateTestUserLiteAsync(createPassword: true, createEmail: true).ConfigureAwait(false);

            bool twoFactorEnabled = true;
            var setTwoFactorEnabledResult = await manager.SetTwoFactorEnabledAsync(user, twoFactorEnabled).ConfigureAwait(false);
            Assert.True(setTwoFactorEnabledResult.Succeeded, string.Concat(setTwoFactorEnabledResult.Errors));

            var twoFactorEnabledResult = await manager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false);
            Assert.Equal<bool>(twoFactorEnabled, twoFactorEnabledResult);

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetTwoFactorEnabledAsync(null)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetTwoFactorEnabledAsync(null, twoFactorEnabled)).ConfigureAwait(false);
        }

        public virtual async Task PasswordHash()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = await CreateTestUserLiteAsync(createPassword: true, createEmail: true).ConfigureAwait(false);
            string passwordPlain = Guid.NewGuid().ToString("N");
            string passwordHash = new PasswordHasher<TUser>().HashPassword(user, passwordPlain);
            await store.SetPasswordHashAsync(user, passwordHash).ConfigureAwait(false);

            var hasPasswordHash = await manager.HasPasswordAsync(user).ConfigureAwait(false);
            Assert.True(hasPasswordHash, "PasswordHash not set");

            var passwordHashResult = await store.GetPasswordHashAsync(user).ConfigureAwait(false);
            Assert.Equal(passwordHash, passwordHashResult);

            user.PasswordHash = passwordHash;

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetPasswordHashAsync(null)).ConfigureAwait(false);
        }

        public virtual async Task SecurityStamp()
        {
            using var store = userFixture.CreateUserStore();
            using var manager = userFixture.CreateUserManager();
            var user = await CreateTestUserLiteAsync(createPassword: true, createEmail: true).ConfigureAwait(false);
            var stamp = await manager.GetSecurityStampAsync(user).ConfigureAwait(false);
            Assert.Equal(user.SecurityStamp, stamp);

            string strNewSecurityStamp = Guid.NewGuid().ToString("N");
            await store.SetSecurityStampAsync(user, strNewSecurityStamp).ConfigureAwait(false);

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetSecurityStampAsync(null)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetSecurityStampAsync(null, strNewSecurityStamp)).ConfigureAwait(false);
            // TODO: check
            // await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetSecurityStampAsync(user, null)).ConfigureAwait(false);
        }
    }

}
