// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System.Threading.Tasks;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using ElCamino.Web.Identity.AzureTable.Tests.Fixtures;
using Xunit;
using Xunit.Abstractions;
using IdentityRole = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityRole;

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests
{
    public class RoleStoreSHA256Tests : BaseRoleStoreTests<SHA256KeyHelper>
    {
        public const string TestTrait = "IdentityCore.Azure.RoleStore.SHA256";
        public RoleStoreSHA256Tests(RoleFixture<Model.IdentityUser, IdentityRole, IdentityCloudContext, SHA256KeyHelper> roleFix, ITestOutputHelper output)
            : base(roleFix, output)
        {
        }

        [Fact(DisplayName = "RoleStoreCtors")]
        [Trait(TestTrait, "")]
        public override void RoleStoreCtors()
        {
            base.RoleStoreCtors();
        }

        [Fact(DisplayName = "AddRemoveRoleClaim")]
        [Trait(TestTrait, "")]
        public override Task AddRemoveRoleClaim()
        {
            return base.AddRemoveRoleClaim();
        }

        [Fact(DisplayName = "AddRoleClaim")]
        [Trait(TestTrait, "")]
        public override Task AddRoleClaim()
        {
            return base.AddRoleClaim();
        }

        [Fact(DisplayName = "CreateRoleTable")]
        [Trait(TestTrait, "")]
        public override Task CreateRoleTable()
        {
            return base.CreateRoleTable();
        }

        [Fact(DisplayName = "CreateRole")]
        [Trait(TestTrait, "")]
        public override Task CreateRole()
        {
            return base.CreateRole();
        }

        [Fact(DisplayName = "ThrowIfDisposed")]
        [Trait(TestTrait, "")]
        public override Task ThrowIfDisposed()
        {
            return base.ThrowIfDisposed();
        }

        [Fact(DisplayName = "UpdateRole")]
        [Trait(TestTrait, "")]
        public override Task UpdateRole()
        {
            return base.UpdateRole();
        }

        [Fact(DisplayName = "UpdateRole2")]
        [Trait(TestTrait, "")]
        public override Task UpdateRole2()
        {
            return base.UpdateRole2();
        }

        [Fact(DisplayName = "DeleteRole")]
        [Trait(TestTrait, "")]
        public override Task DeleteRole()
        {
            return base.DeleteRole();
        }

        [Fact(DisplayName = "FindRoleById")]
        [Trait(TestTrait, "")]
        public override Task FindRoleById()
        {
            return base.FindRoleById();
        }

        [Fact(DisplayName = "FindRoleByName")]
        [Trait(TestTrait, "")]
        public override Task FindRoleByName()
        {
            return base.FindRoleByName();
        }
    }
}
