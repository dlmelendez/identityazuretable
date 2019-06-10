// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

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

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests
{
    public partial class UserStoreV2Tests : BaseUserStoreTests<ApplicationUserV2, IdentityRole, IdentityCloudContext, UserStoreV2<ApplicationUserV2, IdentityRole, IdentityCloudContext>>
    {
        public UserStoreV2Tests(
            UserFixture<ApplicationUserV2, IdentityRole, IdentityCloudContext,
                UserStoreV2<ApplicationUserV2, IdentityRole, IdentityCloudContext>> userFix, ITestOutputHelper output) :
            base(userFix, output)
        {
            
        }

        [Fact(DisplayName = "AddRemoveUserClaim")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override Task AddRemoveUserClaim()
        {
            return base.AddRemoveUserClaim();
        }

        [Fact(DisplayName = "AddRemoveUserLogin")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override Task AddRemoveUserLogin()
        {
            return base.AddRemoveUserLogin();
        }

        [Fact(DisplayName = "AddRemoveUserRole")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override Task AddRemoveUserRole()
        {
            return base.AddRemoveUserRole();
        }

        [Fact(DisplayName = "AddRemoveUserToken")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override Task AddRemoveUserToken()
        {
            return base.AddRemoveUserToken();
        }

        [Fact(DisplayName = "AddReplaceRemoveUserClaim")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override Task AddReplaceRemoveUserClaim()
        {
            return base.AddReplaceRemoveUserClaim();
        } 

        [Fact(DisplayName = "AddUserClaim")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override Task AddUserClaim()
        {
            return base.AddUserClaim();
        }

        [Fact(DisplayName = "AddUserLogin")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override Task AddUserLogin()
        {
            return base.AddUserLogin();
        }

        [Fact(DisplayName = "AddUserRole")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override Task AddUserRole()
        {
            return base.AddUserRole();
        }

        [Fact(DisplayName = "ChangeUserName")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override Task ChangeUserName()
        {
            return base.ChangeUserName();
        }

        [Fact(DisplayName = "CheckDupEmail")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override Task CheckDupEmail()
        {
            return base.CheckDupEmail();
        }

        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        [Fact(DisplayName = "CheckDupUser")]
        public override Task CheckDupUser()
        {
            return base.CheckDupUser();
        }

        [Fact(DisplayName = "CreateUser")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override void CreateUserTest()
        {
            base.CreateUserTest();
        }

        [Fact(DisplayName = "DeleteUser")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override Task DeleteUser()
        {
            return base.DeleteUser();
        }

        [Fact(DisplayName = "FindUserByEmail")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override Task FindUserByEmail()
        {
            return base.FindUserByEmail();
        }

        [Fact(DisplayName = "FindUserById")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override Task FindUserById()
        {
            return base.FindUserById();
        }

        [Fact(DisplayName = "FindUserByName")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override Task FindUserByName()
        {
            return base.FindUserByName();
        }

        [Fact(DisplayName = "FindUsersByEmail")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override Task FindUsersByEmail()
        {
            return base.FindUsersByEmail();
        }

        //[Fact(DisplayName = "GenerateUsers", Skip = "true")]
        //[Trait("IdentityCore.Azure.UserStoreV2", "")]
        //public override Task GenerateUsers()
        //{
        //    return base.GenerateUsers();
        //}

        [Fact(DisplayName = "GetUsersByClaim")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override Task GetUsersByClaim()
        {
            return base.GetUsersByClaim();
        }

        [Fact(DisplayName = "GetUsersByRole")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override Task GetUsersByRole()
        {
            return base.GetUsersByRole();
        }

        [Fact(DisplayName = "IsUserInRole")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override Task IsUserInRole()
        {
            return base.IsUserInRole();
        }

        [Fact(DisplayName = "ThrowIfDisposed")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override Task ThrowIfDisposed()
        {
            return base.ThrowIfDisposed();
        }

        [Fact(DisplayName = "UpdateApplicationUser")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override Task UpdateApplicationUser()
        {
            return base.UpdateApplicationUser();
        }

        [Fact(DisplayName = "UpdateUser")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override Task UpdateUser()
        {
            return base.UpdateUser();
        }

        [Fact(DisplayName = "UserStoreCtors")]
        [Trait("IdentityCore.Azure.UserStoreV2", "")]
        public override void UserStoreCtors()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new UserStoreV2<ApplicationUserV2, IdentityRole, IdentityCloudContext>(null,null);
            });
        }

        #region Properties

        [Fact(DisplayName = "AccessFailedCount")]
        [Trait("IdentityCore.Azure.UserStoreV2.Properties", "")]
        public override Task AccessFailedCount()
        {
            return base.AccessFailedCount();
        }

        [Fact(DisplayName = "Email")]
        [Trait("IdentityCore.Azure.UserStoreV2.Properties", "")]
        public override Task Email()
        {
            return base.Email();
        }

        [Fact(DisplayName = "EmailConfirmed")]
        [Trait("IdentityCore.Azure.UserStoreV2.Properties", "")]
        public override Task EmailConfirmed()
        {
            return base.EmailConfirmed();
        }

        [Fact(DisplayName = "EmailNone")]
        [Trait("IdentityCore.Azure.UserStoreV2.Properties", "")]
        public override Task EmailNone()
        {
            return base.EmailNone();
        }

        [Fact(DisplayName = "LockoutEnabled")]
        [Trait("IdentityCore.Azure.UserStoreV2.Properties", "")]
        public override Task LockoutEnabled()
        {
            return base.LockoutEnabled();
        }

        [Fact(DisplayName = "PasswordHash")]
        [Trait("IdentityCore.Azure.UserStoreV2.Properties", "")]
        public override Task PasswordHash()
        {
            return base.PasswordHash();
        }

        [Fact(DisplayName = "PhoneNumber")]
        [Trait("IdentityCore.Azure.UserStoreV2.Properties", "")]
        public override Task PhoneNumber()
        {
            return base.PhoneNumber();
        }

        [Fact(DisplayName = "PhoneNumberConfirmed")]
        [Trait("IdentityCore.Azure.UserStoreV2.Properties", "")]
        public override Task PhoneNumberConfirmed()
        {
            return base.PhoneNumberConfirmed();
        }

        [Fact(DisplayName = "SecurityStamp")]
        [Trait("IdentityCore.Azure.UserStoreV2.Properties", "")]
        public override Task SecurityStamp()
        {
            return base.SecurityStamp();
        }

        [Fact(DisplayName = "TwoFactorEnabled")]
        [Trait("IdentityCore.Azure.UserStoreV2.Properties", "")]
        public override Task TwoFactorEnabled()
        {
            return base.TwoFactorEnabled();
        }

        #endregion


        [Fact(DisplayName = "UserIdNotChangedIfImmutableIdSetUp")]
        [Trait("IdentityCore.Azure.UserStoreV2.Properties", "")]

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

        private UserStoreV2<ApplicationUserV2, IdentityRole, IdentityCloudContext> GetImmutableUserIdStore()
        {
            var config = userFixture.GetConfig();
            var userStore = userFixture.CreateUserStore(userFixture.GetContext(config), config);
            return userStore;
        }

        
        [Fact(DisplayName = "CanFindByNameIfImmutableIdSetUp")]
        [Trait("IdentityCore.Azure.UserStoreV2.Properties", "")]

        public async Task CanFindByNameIfImmutableIdSetUp()
        {
            var userStore = GetImmutableUserIdStore();

            var user = GenTestUser();
            await userStore.CreateAsync(user);

            var userFound = await userStore.FindByNameAsync(user.UserName);

            Assert.NotNull(user);
            Assert.Equal(user.Id, userFound.Id);
            Assert.Equal(user.PartitionKey, userFound.PartitionKey);
            Assert.Equal(user.RowKey, userFound.RowKey);
        }

        [Fact(DisplayName = "CanFindByIdIfImmutableIdSetUp")]
        [Trait("IdentityCore.Azure.UserStoreV2.Properties", "")]

        public async Task CanFindByIdIfImmutableIdSetUp()
        {
            var userStore = GetImmutableUserIdStore();

            var user = GenTestUser();
            await userStore.CreateAsync(user);

            var userFound = await userStore.FindByIdAsync(user.Id);

            Assert.NotNull(user);
            Assert.Equal(user.Id, userFound.Id);
            Assert.Equal(user.PartitionKey, userFound.PartitionKey);
            Assert.Equal(user.RowKey, userFound.RowKey);
        }
    }
}