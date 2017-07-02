// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
#if net45
using Microsoft.AspNet.Identity;
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
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;
using Xunit;
using ElCamino.Web.Identity.AzureTable.Tests.ModelTests;
using ElCamino.Web.Identity.AzureTable.Tests.Fixtures;

namespace ElCamino.AspNet.Identity.AzureTable.Tests
{
    public partial class UserStoreTests : IClassFixture<UserFixture<ApplicationUser, IdentityRole, IdentityCloudContext>>
    {
        [Fact(DisplayName = "AccessFailedCount")]
#if net45
        [Trait("Identity.Azure.UserStore.Properties", "")]
#else
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
#endif
        public void AccessFailedCount()
        {
            using (var store = userFixture.CreateUserStore())
            {
#if net45
                using (var manager = userFixture.CreateUserManager(store))
                {
                    manager.MaxFailedAccessAttemptsBeforeLockout = 2;
                    manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromHours(2);
#else
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
#endif
                    var user = CreateTestUser<ApplicationUser>();
#if net45
                    var taskUser = manager.GetAccessFailedCountAsync(user.Id);
#else
                    var taskUser = manager.GetAccessFailedCountAsync(user);
#endif

                    taskUser.Wait();
                    Assert.Equal<int>(user.AccessFailedCount, taskUser.Result);

#if net45
                    var taskAccessFailed = manager.AccessFailedAsync(user.Id);
#else
                    var taskAccessFailed = manager.AccessFailedAsync(user);
#endif

                    taskAccessFailed.Wait();
#if net45
                    Assert.True(taskAccessFailed.Result.Succeeded, string.Concat(taskAccessFailed.Result.Errors));
#else
                    Assert.True(taskAccessFailed.Result.Succeeded, string.Concat(taskAccessFailed.Result.Errors.Select(e => e.Code).ToArray()));
#endif

#if net45
                    manager.AccessFailedAsync(user.Id).Wait();
                    manager.AccessFailedAsync(user.Id).Wait();
#else
                    manager.AccessFailedAsync(user).Wait();
                    manager.AccessFailedAsync(user).Wait();
#endif
                    DateTime dtUtc = DateTime.UtcNow;

#if net45
                    user = manager.FindById(user.Id);
                    Assert.True(user.LockoutEndDateUtc.HasValue);
                    Assert.True(user.LockoutEndDateUtc.Value < dtUtc.Add(manager.DefaultAccountLockoutTimeSpan));
                    Assert.True(user.LockoutEndDateUtc.Value > dtUtc.Add(manager.DefaultAccountLockoutTimeSpan.Add(TimeSpan.FromMinutes(-1.0))));

#else
                    var userTaskFindById = manager.FindByIdAsync(user.Id);
                    userTaskFindById.Wait();
                    user = userTaskFindById.Result;
                    Assert.True(user.LockoutEndDateUtc.HasValue);
                    Assert.True(user.LockoutEndDateUtc.Value < dtUtc.Add(idOptions.Lockout.DefaultLockoutTimeSpan));
                    Assert.True(user.LockoutEndDateUtc.Value > dtUtc.Add(idOptions.Lockout.DefaultLockoutTimeSpan.Add(TimeSpan.FromMinutes(-1.0))));
#endif

#if net45
                    var taskAccessReset = manager.ResetAccessFailedCountAsync(user.Id);
#else
                    var taskAccessReset = manager.ResetAccessFailedCountAsync(user);
#endif

                    taskAccessReset.Wait();
                    Assert.True(taskAccessReset.Result.Succeeded, string.Concat(taskAccessReset.Result.Errors));

#if net45
                    user = manager.FindById(user.Id);
#else
                    user = manager.FindByIdAsync(user.Id).Result;
#endif
                    Assert.True(user.AccessFailedCount == 0);

                    try
                    {
                        var task = store.GetAccessFailedCountAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.NotNull(ex);
                    }

                    try
                    {
                        var task = store.IncrementAccessFailedCountAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.NotNull(ex);
                    }

                    try
                    {
                        var task = store.ResetAccessFailedCountAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.NotNull(ex);
                    }

                }
            }
        }

        private void SetValidateEmail(UserManager<ApplicationUser> manager,
            UserStore<ApplicationUser> store,
            ApplicationUser user,
            string strNewEmail)
        {
            string originalEmail = user.Email;
#if net45
            var taskUserSet = manager.SetEmailAsync(user.Id, strNewEmail);
#else
            var taskUserSet = manager.SetEmailAsync(user, strNewEmail);
#endif

            taskUserSet.Wait();
            Assert.True(taskUserSet.Result.Succeeded, string.Concat(taskUserSet.Result.Errors));

#if net45
            var taskUser = manager.GetEmailAsync(user.Id);
#else
            var taskUser = manager.GetEmailAsync(user);
#endif

            taskUser.Wait();
            Assert.Equal<string>(strNewEmail, taskUser.Result);

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
                Assert.True(results.Where(x => x.RowKey.StartsWith("E_")).Count() == 0, string.Format("Email index not deleted for user {0}", user.Id));
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
#if net45
        [Trait("Identity.Azure.UserStore.Properties", "")]
#else
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
#endif
        public void EmailNone()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = CreateTestUser<ApplicationUser>(false, false);
                    string strNewEmail = string.Format("{0}@hotmail.com", Guid.NewGuid().ToString("N"));
                    SetValidateEmail(manager, store, user, strNewEmail);

                    SetValidateEmail(manager, store, user, string.Empty);

                }
            }
        }

        [Fact(DisplayName = "Email")]
#if net45
        [Trait("Identity.Azure.UserStore.Properties", "")]
#else
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
#endif
        public void Email()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = CurrentUser;

                    string strNewEmail = string.Format("{0}@gmail.com", Guid.NewGuid().ToString("N"));
                    SetValidateEmail(manager, store, user, strNewEmail);

                    try
                    {
                        var task = store.GetEmailAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.NotNull(ex);
                    }

                    try
                    {
                        var task = store.SetEmailAsync(null, strNewEmail);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.NotNull(ex);
                    }

                    try
                    {
                        var task = store.SetEmailAsync(user, null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.NotNull(ex);
                    }
                }
            }
        }


        [Fact(DisplayName = "EmailConfirmed")]
#if net45
        [Trait("Identity.Azure.UserStore.Properties", "")]
#else
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
#endif
        public void EmailConfirmed()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
#if net45
                    manager.UserTokenProvider = new EmailTokenProvider<ApplicationUser>();
#endif
                    var user = CreateTestUser<ApplicationUser>();

#if net45
                    var taskUserSet = manager.GenerateEmailConfirmationTokenAsync(user.Id);
#else
                    var taskUserSet = manager.GenerateEmailConfirmationTokenAsync(user);
#endif

                    taskUserSet.Wait();
                    Assert.False(string.IsNullOrWhiteSpace(taskUserSet.Result), "GenerateEmailConfirmationToken failed.");
                    string token = taskUserSet.Result;

#if net45
                    var taskConfirm = manager.ConfirmEmailAsync(user.Id, token);
#else
                    var taskConfirm = manager.ConfirmEmailAsync(user, token);
#endif

                    taskConfirm.Wait();
                    Assert.True(taskConfirm.Result.Succeeded, string.Concat(taskConfirm.Result.Errors));

#if net45
                    user = manager.FindByEmail(user.Email);
#else
                    var userTask02 = manager.FindByEmailAsync(user.Email);
                    userTask02.Wait();
                    user = userTask02.Result;
#endif

                    var taskConfirmGet = store.GetEmailConfirmedAsync(user);
                    taskConfirmGet.Wait();
                    Assert.True(taskConfirmGet.Result, "Email not confirmed");

                    try
                    {
                        var task = store.SetEmailConfirmedAsync(null, true);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.NotNull(ex);
                    }

                    try
                    {
                        var task = store.GetEmailConfirmedAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.NotNull(ex);
                    }

                }
            }
        }

        [Fact(DisplayName = "LockoutEnabled")]
#if net45
        [Trait("Identity.Azure.UserStore.Properties", "")]
#else
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
#endif
        public void LockoutEnabled()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
#if net45
                    manager.UserTokenProvider = new EmailTokenProvider<ApplicationUser>();
#endif
                    var user = CurrentUser;

#if net45
                    var taskLockoutSet = manager.SetLockoutEnabledAsync(user.Id, true);
#else
                    var taskLockoutSet = manager.SetLockoutEnabledAsync(user, true);
#endif

                    taskLockoutSet.Wait();
                    Assert.True(taskLockoutSet.Result.Succeeded, string.Concat(taskLockoutSet.Result.Errors));

#if net45
                    DateTimeOffset offSet = new DateTimeOffset(DateTime.UtcNow.AddMinutes(3));
                    var taskDateSet = manager.SetLockoutEndDateAsync(user.Id, offSet);
#else
                    DateTimeOffset offSet = new DateTimeOffset(DateTime.Now.AddMinutes(3));
                    var taskDateSet = manager.SetLockoutEndDateAsync(user, offSet);
#endif

                    taskDateSet.Wait();
                    Assert.True(taskDateSet.Result.Succeeded, string.Concat(taskDateSet.Result.Errors));

#if net45
                    var taskEnabledGet = manager.GetLockoutEnabledAsync(user.Id);
#else
                    var taskEnabledGet = manager.GetLockoutEnabledAsync(user);
#endif

                    taskEnabledGet.Wait();
                    Assert.True(taskEnabledGet.Result, "Lockout not true");

#if net45
                    var taskDateGet = manager.GetLockoutEndDateAsync(user.Id);
#else
                    var taskDateGet = manager.GetLockoutEndDateAsync(user);
#endif

                    taskDateGet.Wait();
                    Assert.Equal(offSet, taskDateGet.Result);

                    DateTime tmpDate = DateTime.UtcNow.AddDays(1);
                    user.LockoutEndDateUtc = tmpDate;
                    var taskGet = store.GetLockoutEndDateAsync(user);
                    taskGet.Wait();
#if net45
                    Assert.Equal<DateTimeOffset>(new DateTimeOffset(tmpDate), taskGet.Result);
#else
                    Assert.Equal<DateTimeOffset?>(new DateTimeOffset?(tmpDate), taskGet.Result);
#endif


                    user.LockoutEndDateUtc = null;
                    var taskGet2 = store.GetLockoutEndDateAsync(user);
                    taskGet2.Wait();
#if net45
                    Assert.Equal<DateTimeOffset>(new DateTimeOffset(), taskGet2.Result);
#else
                    Assert.Equal<DateTimeOffset?>(new DateTimeOffset?(), taskGet2.Result);
#endif


                    var minOffSet = DateTimeOffset.MinValue;
                    var taskSet2 = store.SetLockoutEndDateAsync(user, minOffSet);
                    taskSet2.Wait();
#if net45
                    Assert.Null(user.LockoutEndDateUtc);
#else
                    Assert.NotNull(user.LockoutEndDateUtc);
#endif



                    try
                    {
                        store.GetLockoutEnabledAsync(null);
                    }
                    catch (ArgumentException) { }


                    try
                    {
                        store.GetLockoutEndDateAsync(null);
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.SetLockoutEndDateAsync(null, offSet);
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.SetLockoutEnabledAsync(null, false);
                    }
                    catch (ArgumentException) { }
                }
            }
        }

        [Fact(DisplayName = "PhoneNumber")]
#if net45
        [Trait("Identity.Azure.UserStore.Properties", "")]
#else
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
#endif
        public void PhoneNumber()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = CurrentUser;

                    string strNewPhoneNumber = "542-887-3434";
#if net45
                    var taskPhoneNumberSet = manager.SetPhoneNumberAsync(user.Id, strNewPhoneNumber);
#else
                    var taskPhoneNumberSet = manager.SetPhoneNumberAsync(user, strNewPhoneNumber);
#endif

                    taskPhoneNumberSet.Wait();
                    Assert.True(taskPhoneNumberSet.Result.Succeeded, string.Concat(taskPhoneNumberSet.Result.Errors));

#if net45
                    var taskUser = manager.GetPhoneNumberAsync(user.Id);
#else
                    var taskUser = manager.GetPhoneNumberAsync(user);
#endif

                    taskUser.Wait();
                    Assert.Equal<string>(strNewPhoneNumber, taskUser.Result);

                    try
                    {
                        var task = store.GetPhoneNumberAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.NotNull(ex);
                    }

                    try
                    {
                        var task = store.SetPhoneNumberAsync(null, strNewPhoneNumber);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.NotNull(ex);
                    }

                    try
                    {
                        var task = store.SetPhoneNumberAsync(user, null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.NotNull(ex);
                    }
                }
            }
        }

        [Fact(DisplayName = "PhoneNumberConfirmed")]
#if net45
        [Trait("Identity.Azure.UserStore.Properties", "")]
#else
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
#endif
        public void PhoneNumberConfirmed()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
#if net45
                    manager.UserTokenProvider = new PhoneNumberTokenProvider<ApplicationUser>();
#endif

                    var user = CreateTestUser<ApplicationUser>();
                    string strNewPhoneNumber = "425-555-1111";
#if net45
                    var taskUserSet = manager.GenerateChangePhoneNumberTokenAsync(user.Id, strNewPhoneNumber);
#else
                    var taskUserSet = manager.GenerateChangePhoneNumberTokenAsync(user, strNewPhoneNumber);
#endif

                    taskUserSet.Wait();
                    Assert.False(string.IsNullOrWhiteSpace(taskUserSet.Result), "GeneratePhoneConfirmationToken failed.");
                    string token = taskUserSet.Result;

#if net45
                    var taskConfirm = manager.ChangePhoneNumberAsync(user.Id, strNewPhoneNumber, token);
#else
                    var taskConfirm = manager.ChangePhoneNumberAsync(user, strNewPhoneNumber, token);
#endif

                    taskConfirm.Wait();
                    Assert.True(taskConfirm.Result.Succeeded, string.Concat(taskConfirm.Result.Errors));

#if net45
                    user = manager.FindByEmail(user.Email);
#else
                    var uTask01 = manager.FindByEmailAsync(user.Email);
                    uTask01.Wait();
                    user = uTask01.Result;
#endif

                    var taskConfirmGet = store.GetPhoneNumberConfirmedAsync(user);
                    taskConfirmGet.Wait();
                    Assert.True(taskConfirmGet.Result, "Phone not confirmed");

                    try
                    {
                        var task = store.SetPhoneNumberConfirmedAsync(null, true);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.NotNull(ex);
                    }

                    try
                    {
                        var task = store.GetPhoneNumberConfirmedAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.NotNull(ex);
                    }

                }
            }
        }

        [Fact(DisplayName = "TwoFactorEnabled")]
#if net45
        [Trait("Identity.Azure.UserStore.Properties", "")]
#else
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
#endif
        public void TwoFactorEnabled()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = CurrentUser;

                    bool twoFactorEnabled = true;
#if net45
                    var taskTwoFactorEnabledSet = manager.SetTwoFactorEnabledAsync(user.Id, twoFactorEnabled);
#else
                    var taskTwoFactorEnabledSet = manager.SetTwoFactorEnabledAsync(user, twoFactorEnabled);
#endif

                    taskTwoFactorEnabledSet.Wait();
                    Assert.True(taskTwoFactorEnabledSet.Result.Succeeded, string.Concat(taskTwoFactorEnabledSet.Result.Errors));

#if net45
                    var taskUser = manager.GetTwoFactorEnabledAsync(user.Id);
#else
                    var taskUser = manager.GetTwoFactorEnabledAsync(user);
#endif

                    taskUser.Wait();
                    Assert.Equal<bool>(twoFactorEnabled, taskUser.Result);

                    try
                    {
                        var task = store.GetTwoFactorEnabledAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.NotNull(ex);
                    }

                    try
                    {
                        var task = store.SetTwoFactorEnabledAsync(null, twoFactorEnabled);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.NotNull(ex);
                    }

                }
            }
        }

        [Fact(DisplayName = "PasswordHash")]
#if net45
        [Trait("Identity.Azure.UserStore.Properties", "")]
#else
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
#endif
        public void PasswordHash()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = CurrentUser;
                    string passwordPlain = Guid.NewGuid().ToString("N");
#if net45
                    string passwordHash = manager.PasswordHasher.HashPassword(passwordPlain);
#else
                    string passwordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, passwordPlain);
#endif

                    var taskUserSet = store.SetPasswordHashAsync(user, passwordHash);
                    taskUserSet.Wait();

#if net45
                    var taskHasHash = manager.HasPasswordAsync(user.Id);
#else
                    var taskHasHash = manager.HasPasswordAsync(user);
#endif

                    taskHasHash.Wait();
                    Assert.True(taskHasHash.Result, "PasswordHash not set");

                    var taskUser = store.GetPasswordHashAsync(user);
                    taskUser.Wait();
                    Assert.Equal<string>(passwordHash, taskUser.Result);
                    user.PasswordHash = passwordHash;
                    try
                    {
                        var task = store.GetPasswordHashAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.NotNull(ex);
                    }

                    try
                    {
                        var task = store.HasPasswordAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.NotNull(ex);
                    }

                    try
                    {
                        var task = store.SetPasswordHashAsync(null, passwordHash);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.NotNull(ex);
                    }

                    try
                    {
                        var task = store.SetPasswordHashAsync(user, null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.NotNull(ex);
                    }
                }
            }
        }

#if net45
        [Fact(DisplayName = "UsersProperty")]
        [Trait("Identity.Azure.UserStore.Properties", "")]
        public void UsersProperty()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {

                    DateTime start = DateTime.UtcNow;
                    var list = manager.Users.ToList();

                    WriteLineObject<IdentityUser>(list.First());

                    output.WriteLine("UserQuery: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
                    output.WriteLine("UserQuery: {0} users", list.Count());
                    output.WriteLine("");

                    CreateTestUser<ApplicationUser>(true, true, "A" + Guid.NewGuid().ToString() + "@gmail.com");
                    DateTime start2 = DateTime.UtcNow;
                    var list2 = manager.Users.Where(u => u.Email.CompareTo("A") >= 0
                        && u.Email.CompareTo("B") < 0).ToList();

                    WriteLineObject<IdentityUser>(list2.First());

                    output.WriteLine("UserQuery: {0} seconds", (DateTime.UtcNow - start2).TotalSeconds);
                    output.WriteLine("UserQuery: {0} users", list2.Count());
                    output.WriteLine("");

                    DateTime start3 = DateTime.UtcNow;
                    var list3 = manager.Users.Select(s => s.Email).ToList();

                    output.WriteLine(list3.First());

                    output.WriteLine("UserQuery: {0} seconds", (DateTime.UtcNow - start3).TotalSeconds);
                    output.WriteLine("UserQuery.Email: {0} users", list3.Count());
                    output.WriteLine("");

                    DateTime start4 = DateTime.UtcNow;
                    var list4 = manager.Users.Select(s => s).ToList();
                    WriteLineObject<IdentityUser>(list4.First());

                    output.WriteLine("UserQuery: {0} seconds", (DateTime.UtcNow - start4).TotalSeconds);
                    output.WriteLine("UserQuery: {0} users", list4.Count());
                    output.WriteLine("");

                    var type = store.Users.ElementType;
                    System.Collections.IEnumerable enumFoo = store.Users as System.Collections.IEnumerable;
                    var tempEnumerator = enumFoo.GetEnumerator();
                    if (tempEnumerator.MoveNext())
                    {
                        var obj = tempEnumerator.Current as IdentityUser;
                        output.WriteLine("IEnumerable.GetEnumerator: First user");
                        WriteLineObject<IdentityUser>(obj);
                        output.WriteLine("");
                    }

                    var query = manager.Users.Provider.CreateQuery(store.Users.Expression);
                    tempEnumerator = query.GetEnumerator();
                    if (tempEnumerator.MoveNext())
                    {
                        var obj = tempEnumerator.Current as IdentityUser;
                        output.WriteLine("UserQuery.CreateQuery(): First user");
                        WriteLineObject<IdentityUser>(obj);
                        output.WriteLine("");
                    }

                    DateTime start5 = DateTime.UtcNow;
                    var list5 = manager.Users.Skip(1).Take(10).ToList();

                    WriteLineObject<IdentityUser>(list.First());

                    output.WriteLine("UserQuery: {0} seconds", (DateTime.UtcNow - start5).TotalSeconds);
                    output.WriteLine("UserQuery: {0} users", list5.Count());
                    output.WriteLine("");

                    DateTime start6 = DateTime.UtcNow;
                    var list6 = manager.Users.Where(u => u.Email.CompareTo("A") >= 0
                        && u.Email.CompareTo("B") < 0).Count();

                    WriteLineObject<IdentityUser>(list.First());

                    output.WriteLine("UserQuery: {0} seconds", (DateTime.UtcNow - start6).TotalSeconds);
                    output.WriteLine("UserQuery: {0} users", list6);
                    output.WriteLine("");

                    DateTime start7 = DateTime.UtcNow;
                    var list7 = manager.Users.Count();

                    output.WriteLine("UserQuery: {0} seconds", (DateTime.UtcNow - start7).TotalSeconds);
                    output.WriteLine("UserQuery: {0} users", list7);
                    output.WriteLine("");

                    DateTime start8 = DateTime.UtcNow;
                    var list8 = manager.Users.First();

                    output.WriteLine("UserQuery.First(): {0} seconds", (DateTime.UtcNow - start8).TotalSeconds);
                    output.WriteLine("");

                    DateTime start9 = DateTime.UtcNow;
                    var list9 = manager.Users.Select(s => s.Email).First();

                    output.WriteLine("UserQuery.Email.First(): {0} seconds", (DateTime.UtcNow - start9).TotalSeconds);
                    output.WriteLine("");


                    try
                    {
                        manager.Users.Select(s=> s.Email).FirstOrDefault();
                    }
                    catch (NotSupportedException) { }

                    try
                    {
                        manager.Users.Provider.Execute(store.Users.Expression);
                    }
                    catch (NotSupportedException) { }

                    try
                    {
                        manager.Users.Provider.Execute<IdentityUser>(store.Users.Expression);
                    }
                    catch (Exception) { }

                    try
                    {
                        manager.Users.Select(u => u.Roles).ToList();
                    }
                    catch (StorageException) { }
                    Assert.NotNull(store.Users);
 
                }
            }
        }
#endif

        [Fact(DisplayName = "SecurityStamp")]
#if net45
        [Trait("Identity.Azure.UserStore.Properties", "")]
#else
        [Trait("IdentityCore.Azure.UserStore.Properties", "")]
#endif
        public void SecurityStamp()
        {
            using (var store = userFixture.CreateUserStore())
            {
                using (var manager = userFixture.CreateUserManager(store))
                {
                    var user = CreateTestUser<ApplicationUser>();

#if net45
                    var taskUser = manager.GetSecurityStampAsync(user.Id);
#else
                    var taskUser = manager.GetSecurityStampAsync(user);
#endif

                    taskUser.Wait();
                    Assert.Equal<string>(user.SecurityStamp, taskUser.Result);

                    string strNewSecurityStamp = Guid.NewGuid().ToString("N");
                    var taskUserSet = store.SetSecurityStampAsync(user, strNewSecurityStamp);
                    taskUserSet.Wait();

                    try
                    {
                        var task = store.GetSecurityStampAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.NotNull(ex);
                    }

                    try
                    {
                        var task = store.SetSecurityStampAsync(null, strNewSecurityStamp);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.NotNull(ex);
                    }

                    try
                    {
                        var task = store.SetSecurityStampAsync(user, null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.NotNull(ex);
                    }
                }
            }
        }

    }
}
