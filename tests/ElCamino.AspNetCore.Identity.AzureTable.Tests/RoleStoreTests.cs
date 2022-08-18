// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using ElCamino.Web.Identity.AzureTable.Tests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using IdentityRole = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityRole;
using IdentityUser = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityUser;

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests
{
    public class RoleStoreTests : IClassFixture<RoleFixture<Model.IdentityUser, IdentityRole, IdentityCloudContext, DefaultKeyHelper>>
    {
        private readonly ITestOutputHelper _output;
        private readonly RoleFixture<Model.IdentityUser, IdentityRole, IdentityCloudContext, DefaultKeyHelper> roleFixture;
        public RoleStoreTests(RoleFixture<Model.IdentityUser, IdentityRole, IdentityCloudContext, DefaultKeyHelper> roleFix, ITestOutputHelper output)
        {
            _output = output;
            roleFixture = roleFix;
            CreateRoleTable().Wait();
        }

        [Fact(DisplayName = "RoleStoreCtors")]
        [Trait("IdentityCore.Azure.RoleStore", "")]
        public void RoleStoreCtors()
        {
            Assert.Throws<ArgumentNullException>(() => roleFixture.CreateRoleStore(null));
            using var rstore = roleFixture.CreateRoleStore();
            Assert.NotNull(rstore);
        }

        private static Claim GenRoleClaim()
         => new(Constants.AccountClaimTypes.AccountTestUserClaim, Guid.NewGuid().ToString());

        [Fact(DisplayName = "AddRemoveRoleClaim")]
        [Trait("IdentityCore.Azure.RoleStore", "")]
        public async Task AddRemoveRoleClaim()
        {
            using RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager();
            string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
            var role = new IdentityRole(roleNew);
            var sw = new Stopwatch();
            sw.Start();
            await manager.CreateAsync(role);
            sw.Stop();

            _output.WriteLine("CreateRoleAsync: {0} seconds", sw.Elapsed.TotalSeconds);
            Claim c1 = GenRoleClaim();
            Claim c2 = GenRoleClaim();

            await AddRoleClaimAsyncHelper(role, c1);
            await AddRoleClaimAsyncHelper(role, c2);

            await RemoveRoleClaimAsyncHelper(role, c1);
        }

        [Fact(DisplayName = "AddRoleClaim")]
        [Trait("IdentityCore.Azure.RoleStore", "")]
        public async Task AddRoleClaim()
        {
            using RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager();
            string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
            var role = new IdentityRole(roleNew);
            var sw = new Stopwatch();
            sw.Start();
            await manager.CreateAsync(role);
            sw.Stop();

            _output.WriteLine("CreateRoleAsync: {0} seconds", sw.Elapsed.TotalSeconds);

            await AddRoleClaimAsyncHelper(role, GenRoleClaim());
        }

        private async Task AddRoleClaimAsyncHelper(IdentityRole role, Claim claim)
        {
            using RoleStore<IdentityRole> store = roleFixture.CreateRoleStore();
            using RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager();
            await manager.AddClaimAsync(role, claim);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.AddClaimAsync(null, claim));
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.AddClaimAsync(role, null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetClaimsAsync(null));

            var claims = await manager.GetClaimsAsync(role);
            Assert.Contains(claims, (c) => c.Value == claim.Value && c.Type == claim.Type);//, "Claim not found");
        }

        private async Task RemoveRoleClaimAsyncHelper(IdentityRole role, Claim claim)
        {
            using RoleStore<IdentityRole> store = roleFixture.CreateRoleStore();
            using RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager();
            await manager.RemoveClaimAsync(role, claim);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.RemoveClaimAsync(null, claim));
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.RemoveClaimAsync(role, null));

            var c1 = new Claim(string.Empty, claim.Value);
            await Assert.ThrowsAsync<ArgumentException>(() => store.RemoveClaimAsync(role, c1));

            var claims = await manager.GetClaimsAsync(role);
            Assert.DoesNotContain(claims, (c) => c.Value == claim.Value && c.Type == claim.Type);  //, "Claim not found");
        }

        [Fact(DisplayName = "CreateRoleTable")]
        [Trait("IdentityCore.Azure.RoleStore", "")]
        public async Task CreateRoleTable()
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                await store.CreateTableIfNotExistsAsync();
            }
            ServiceCollection services = new ServiceCollection();
            // Adding coverage for CreateAzureTablesIfNotExists();
            services.AddIdentityCore<IdentityUser>()
                .AddAzureTableStores<IdentityCloudContext>(new Func<IdentityConfiguration>(() =>
                {
                    return roleFixture.GetConfig();
                }), roleFixture.GetKeyHelper())
                .CreateAzureTablesIfNotExists<IdentityCloudContext>();

        }

        [Fact(DisplayName = "CreateRole")]
        [Trait("IdentityCore.Azure.RoleStore", "")]
        public async Task CreateRole()
        {
            using RoleStore<IdentityRole> store = roleFixture.CreateRoleStore();
            var role = await CreateRoleAsync();

            WriteLineObject<IdentityRole>(role);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.CreateAsync(null));
        }

        protected async Task<IdentityRole> CreateRoleAsync()
        {
            using RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager();
            string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
            var role = new IdentityRole(roleNew);
            var sw = new Stopwatch();
            sw.Start();
            await manager.CreateAsync(role);
            sw.Stop();

            _output.WriteLine("CreateRoleAsync: {0} seconds", sw.Elapsed.TotalSeconds);
            return role;
        }


        [Fact(DisplayName = "ThrowIfDisposed")]
        [Trait("IdentityCore.Azure.RoleStore", "")]
        public async Task ThrowIfDisposed()
        {
            using RoleStore<IdentityRole> store = roleFixture.CreateRoleStore();
            RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager();
            manager.Dispose();
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.DeleteAsync(null));
        }

        [Fact(DisplayName = "UpdateRole")]
        [Trait("IdentityCore.Azure.RoleStore", "")]
        public async Task UpdateRole()
        {
            using RoleStore<IdentityRole> store = roleFixture.CreateRoleStore();
            using RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager();
            string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());

            var role = new IdentityRole(roleNew);
            await manager.CreateAsync(role);

            role.Name = Guid.NewGuid() + role.Name;
            await manager.UpdateAsync(role);

            var rRole = await manager.FindByIdAsync(role.RowKey);

            Assert.NotNull(rRole);
            Assert.Equal(role.RowKey, rRole.RowKey);
            Assert.NotEqual<string>(roleNew, rRole.Name);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.UpdateAsync(null));
        }

        [Fact(DisplayName = "UpdateRole2")]
        [Trait("IdentityCore.Azure.RoleStore", "")]
        public async Task UpdateRole2()
        {
            using RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager();
            string roleNew = string.Format("{0}_TestRole", Guid.NewGuid());

            var role = new IdentityRole(roleNew);
            await manager.CreateAsync(role);

            role.Name += Guid.NewGuid();
            await manager.UpdateAsync(role);

            var rRole = await manager.FindByIdAsync(role.RowKey);

            Assert.NotNull(rRole);
            Assert.Equal(role.RowKey, rRole.RowKey);
            Assert.NotEqual<string>(roleNew, rRole.Name);
        }

        [Fact(DisplayName = "DeleteRole")]
        [Trait("IdentityCore.Azure.RoleStore", "")]
        public async Task DeleteRole()
        {
            using RoleStore<IdentityRole> store = roleFixture.CreateRoleStore();
            using RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager();
            string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
            var role = new IdentityRole(roleNew);
            await manager.CreateAsync(role);

            var sw = new Stopwatch();
            sw.Start();
            await manager.DeleteAsync(role);
            sw.Stop();
            _output.WriteLine("DeleteRole: {0} seconds", sw.Elapsed.TotalSeconds);

            var result = await manager.FindByIdAsync(role.RowKey);
            Assert.Null(result);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.DeleteAsync(null));
        }


        [Fact(DisplayName = "FindRoleById")]
        [Trait("IdentityCore.Azure.RoleStore", "")]
        public async Task FindRoleById()
        {
            var role = await CreateRoleAsync();
            using RoleStore<IdentityRole> store = roleFixture.CreateRoleStore();
            using RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager();
            var sw = new Stopwatch();
            sw.Start();
            var result = await manager.FindByIdAsync(role.Id);
            sw.Stop();
            _output.WriteLine("FindByIdAsync: {0} seconds", sw.Elapsed.TotalSeconds);

            Assert.NotNull(result);
            WriteLineObject<IdentityRole>(result);
            Assert.Equal(role.Id, result.RowKey);
        }

        [Fact(DisplayName = "FindRoleByName")]
        [Trait("IdentityCore.Azure.RoleStore", "")]
        public async Task FindRoleByName()
        {
            var role = await CreateRoleAsync();

            using RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager();
            var sw = new Stopwatch();
            sw.Start();
            var result = await manager.FindByNameAsync(role.Name);
            sw.Stop();
            _output.WriteLine("FindByNameAsync: {0} seconds", sw.Elapsed.TotalSeconds);

            Assert.NotNull(result);
            Assert.Equal(role.Name, result.Name);

            sw.Reset();
            sw.Start();
            var result1 = manager.Roles.Where(r => r.Name == role.Name).ToList();
            sw.Stop();
            _output.WriteLine("RoleManager.Roles where name: {0} seconds", sw.Elapsed.TotalSeconds);

            Assert.NotNull(result1.SingleOrDefault());
            Assert.Equal(role.Name, result1.SingleOrDefault().Name);
        }

        private void WriteLineObject<t>(t obj) where t : class
        {
            _output.WriteLine(typeof(t).Name);
            string strLine = obj == null ? "Null" : Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
            _output.WriteLine("{0}", strLine);
        }
    }
}
