// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

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
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using IdentityUser = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityUser;
using IdentityRole = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityRole;
using ElCamino.Web.Identity.AzureTable.Tests.ModelTests;
using ElCamino.Web.Identity.AzureTable.Tests.Fixtures;
using Microsoft.AspNetCore.Identity;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests
{
    public class UserStoreSHA256Tests : BaseUserStoreTests<ApplicationUserV2, IdentityRole, IdentityCloudContext, UserStore<ApplicationUserV2, IdentityRole, IdentityCloudContext>, SHA256KeyHelper>
    {
        public const string UserStoreTrait = "IdentityCore.Azure.UserStore.SHA256";
        public const string UserStoreTraitProperties = UserStoreTrait + ".Properties";

        public UserStoreSHA256Tests(
            UserFixture<ApplicationUserV2, IdentityRole, IdentityCloudContext,
                UserStore<ApplicationUserV2, IdentityRole, IdentityCloudContext>, SHA256KeyHelper> userFix, ITestOutputHelper output) :
            base(userFix, output)
        {

        }

        [Fact(DisplayName = "AddRemoveUserClaim")]
        [Trait(UserStoreTrait, "")]
        public override Task AddRemoveUserClaim()
        {
            return base.AddRemoveUserClaim();
        }

        [Fact(DisplayName = "AddRemoveUserLogin")]
        [Trait(UserStoreTrait, "")]
        public override Task AddRemoveUserLogin()
        {
            return base.AddRemoveUserLogin();
        }

        [Fact(DisplayName = "AddRemoveUserRole")]
        [Trait(UserStoreTrait, "")]
        public override Task AddRemoveUserRole()
        {
            return base.AddRemoveUserRole();
        }

        [Fact(DisplayName = "AddRemoveUserToken")]
        [Trait(UserStoreTrait, "")]
        public override Task AddRemoveUserToken()
        {
            return base.AddRemoveUserToken();
        }

        [Fact(DisplayName = "AddReplaceRemoveUserClaim")]
        [Trait(UserStoreTrait, "")]
        public override Task AddReplaceRemoveUserClaim()
        {
            return base.AddReplaceRemoveUserClaim();
        }

        [Fact(DisplayName = "AddUserClaim")]
        [Trait(UserStoreTrait, "")]
        public override Task AddUserClaim()
        {
            return base.AddUserClaim();
        }

        [Fact(DisplayName = "AddUserLogin")]
        [Trait(UserStoreTrait, "")]
        public override Task AddUserLogin()
        {
            return base.AddUserLogin();
        }

        [Fact(DisplayName = "AddUserRole")]
        [Trait(UserStoreTrait, "")]
        public override Task AddUserRole()
        {
            return base.AddUserRole();
        }

        [Fact(DisplayName = "ChangeUserName")]
        [Trait(UserStoreTrait, "")]
        public override Task ChangeUserName()
        {
            return base.ChangeUserName();
        }

        [Fact(DisplayName = "CheckDupEmail")]
        [Trait(UserStoreTrait, "")]
        public override Task CheckDupEmail()
        {
            return base.CheckDupEmail();
        }

        [Trait(UserStoreTrait, "")]
        [Fact(DisplayName = "CheckDupUser")]
        public override Task CheckDupUser()
        {
            return base.CheckDupUser();
        }

        [Fact(DisplayName = "CreateUser")]
        [Trait(UserStoreTrait, "")]
        public override Task CreateUserTest()
        {
            return base.CreateUserTest();
        }

        [Fact(DisplayName = "DeleteUser")]
        [Trait(UserStoreTrait, "")]
        public override Task DeleteUser()
        {
            return base.DeleteUser();
        }

        [Fact(DisplayName = "FindUserByEmail")]
        [Trait(UserStoreTrait, "")]
        public override Task FindUserByEmail()
        {
            return base.FindUserByEmail();
        }

        [Fact(DisplayName = "FindUserById")]
        [Trait(UserStoreTrait, "")]
        public override Task FindUserById()
        {
            return base.FindUserById();
        }

        [Fact(DisplayName = "FindUserByName")]
        [Trait(UserStoreTrait, "")]
        public override Task FindUserByName()
        {
            return base.FindUserByName();
        }

        [Fact(DisplayName = "FindUsersByEmail")]
        [Trait(UserStoreTrait, "")]
        public override Task FindUsersByEmail()
        {
            return base.FindUsersByEmail();
        }

        //[Fact(DisplayName = "GenerateUsers", Skip = "true")]
        //[Trait(UserStoreTrait, "")]
        //public override Task GenerateUsers()
        //{
        //    return base.GenerateUsers();
        //}

        [Fact(DisplayName = "GetUsersByClaim")]
        [Trait(UserStoreTrait, "")]
        public override Task GetUsersByClaim()
        {
            return base.GetUsersByClaim();
        }

        [Fact(DisplayName = "GetUsersByRole")]
        [Trait(UserStoreTrait, "")]
        public override Task GetUsersByRole()
        {
            return base.GetUsersByRole();
        }

        [Fact(DisplayName = "IsUserInRole")]
        [Trait(UserStoreTrait, "")]
        public override Task IsUserInRole()
        {
            return base.IsUserInRole();
        }

        [Fact(DisplayName = "MapEntityTest")]
        [Trait(UserStoreTrait, "")]
        public override void MapEntityTest()
        {
            base.MapEntityTest();
        }

        [Fact(DisplayName = "ThrowIfDisposed")]
        [Trait(UserStoreTrait, "")]
        public override Task ThrowIfDisposed()
        {
            return base.ThrowIfDisposed();
        }

        [Fact(DisplayName = "UpdateApplicationUser")]
        [Trait(UserStoreTrait, "")]
        public override Task UpdateApplicationUser()
        {
            return base.UpdateApplicationUser();
        }

        [Fact(DisplayName = "UpdateUser")]
        [Trait(UserStoreTrait, "")]
        public override Task UpdateUser()
        {
            return base.UpdateUser();
        }

        [Fact(DisplayName = "UserStoreCtors")]
        [Trait(UserStoreTrait, "")]
        public override void UserStoreCtors()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new UserStore<ApplicationUserV2, IdentityRole, IdentityCloudContext>(null, null);
            });
        }

        #region Properties

        [Fact(DisplayName = "AccessFailedCount")]
        [Trait(UserStoreTraitProperties, "")]
        public override Task AccessFailedCount()
        {
            return base.AccessFailedCount();
        }

        [Fact(DisplayName = "Email")]
        [Trait(UserStoreTraitProperties, "")]
        public override Task Email()
        {
            return base.Email();
        }

        [Fact(DisplayName = "EmailConfirmed")]
        [Trait(UserStoreTraitProperties, "")]
        public override Task EmailConfirmed()
        {
            return base.EmailConfirmed();
        }

        [Fact(DisplayName = "EmailNone")]
        [Trait(UserStoreTraitProperties, "")]
        public override Task EmailNone()
        {
            return base.EmailNone();
        }

        [Fact(DisplayName = "LockoutEnabled")]
        [Trait(UserStoreTraitProperties, "")]
        public override Task LockoutEnabled()
        {
            return base.LockoutEnabled();
        }

        [Fact(DisplayName = "PasswordHash")]
        [Trait(UserStoreTraitProperties, "")]
        public override Task PasswordHash()
        {
            return base.PasswordHash();
        }

        [Fact(DisplayName = "PhoneNumber")]
        [Trait(UserStoreTraitProperties, "")]
        public override Task PhoneNumber()
        {
            return base.PhoneNumber();
        }

        [Fact(DisplayName = "PhoneNumberConfirmed")]
        [Trait(UserStoreTraitProperties, "")]
        public override Task PhoneNumberConfirmed()
        {
            return base.PhoneNumberConfirmed();
        }

        [Fact(DisplayName = "SecurityStamp")]
        [Trait(UserStoreTraitProperties, "")]
        public override Task SecurityStamp()
        {
            return base.SecurityStamp();
        }

        [Fact(DisplayName = "TwoFactorEnabled")]
        [Trait(UserStoreTraitProperties, "")]
        public override Task TwoFactorEnabled()
        {
            return base.TwoFactorEnabled();
        }

        #endregion


        [Fact(DisplayName = "UserIdNotChangedIfImmutableIdSetUp")]
        [Trait(UserStoreTraitProperties, "")]

        public async Task UserIdNotChangedIfImmutableIdSetUp()
        {
            var userStore = GetImmutableUserIdStore();

            var user = GenTestUser();
            await userStore.CreateAsync(user);

            var idBefore = user.Id;
            var pkBefore = user.PartitionKey;
            var rkBefore = user.RowKey;

            user.UserName += "changed";
            await userStore.UpdateAsync(user);

            Assert.Equal(idBefore, user.Id);
            Assert.Equal(pkBefore, user.PartitionKey);
            Assert.Equal(rkBefore, user.RowKey);

        }

        private UserStore<ApplicationUserV2, IdentityRole, IdentityCloudContext> GetImmutableUserIdStore()
        {
            var config = userFixture.GetConfig();
            var userStore = userFixture.CreateUserStore(userFixture.GetContext(config));
            return userStore;
        }


        [Fact(DisplayName = "CanFindByNameIfImmutableIdSetUp")]
        [Trait(UserStoreTraitProperties, "")]

        public override Task CanFindByNameIfImmutableIdSetUp()
        {
            return base.CanFindByNameIfImmutableIdSetUp();
        }

        [Fact(DisplayName = "CanFindByIdIfImmutableIdSetUp")]
        [Trait(UserStoreTraitProperties, "")]

        public override Task CanFindByIdIfImmutableIdSetUp()
        {
            return base.CanFindByIdIfImmutableIdSetUp();
        }
    }
}