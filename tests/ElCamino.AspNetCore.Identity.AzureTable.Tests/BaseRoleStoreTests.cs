// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
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
    public class BaseRoleStoreTests<TKeyHelper> : IClassFixture<RoleFixture<Model.IdentityUser, IdentityRole, IdentityCloudContext, TKeyHelper>>
        where TKeyHelper : IKeyHelper, new()
    {
        private readonly ITestOutputHelper _output;
        private readonly RoleFixture<Model.IdentityUser, IdentityRole, IdentityCloudContext, TKeyHelper> roleFixture;
        public BaseRoleStoreTests(RoleFixture<Model.IdentityUser, IdentityRole, IdentityCloudContext, TKeyHelper> roleFix, ITestOutputHelper output)
        {
            _output = output;
            roleFixture = roleFix;
            CreateRoleTable().Wait();
        }

        public virtual void RoleStoreCtors()
        {
            Assert.Throws<ArgumentNullException>(() => roleFixture.CreateRoleStore(null));
            using var rstore = roleFixture.CreateRoleStore();
            Assert.NotNull(rstore);
        }

        private static Claim GenRoleClaim()
         => new(Constants.AccountClaimTypes.AccountTestUserClaim, Guid.NewGuid().ToString());

        public virtual async Task AddRemoveRoleClaim()
        {
            using RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager();
            string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
            var role = new IdentityRole(roleNew);
            var sw = new Stopwatch();
            sw.Start();
            await manager.CreateAsync(role).ConfigureAwait(false);
            sw.Stop();

            _output.WriteLine("CreateRoleAsync: {0} seconds", sw.Elapsed.TotalSeconds);
            Claim c1 = GenRoleClaim();
            Claim c2 = GenRoleClaim();

            await AddRoleClaimAsyncHelper(role, c1).ConfigureAwait(false);
            await AddRoleClaimAsyncHelper(role, c2).ConfigureAwait(false);

            await RemoveRoleClaimAsyncHelper(role, c1).ConfigureAwait(false);
        }

        public virtual async Task AddRoleClaim()
        {
            using RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager();
            string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
            var role = new IdentityRole(roleNew);
            var sw = new Stopwatch();
            sw.Start();
            await manager.CreateAsync(role).ConfigureAwait(false);
            sw.Stop();

            _output.WriteLine("CreateRoleAsync: {0} seconds", sw.Elapsed.TotalSeconds);

            await AddRoleClaimAsyncHelper(role, GenRoleClaim()).ConfigureAwait(false);
        }

        private async Task AddRoleClaimAsyncHelper(IdentityRole role, Claim claim)
        {
            using RoleStore<IdentityRole> store = roleFixture.CreateRoleStore();
            using RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager();
            await manager.AddClaimAsync(role, claim).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.AddClaimAsync(null, claim)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.AddClaimAsync(role, null)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetClaimsAsync(null)).ConfigureAwait(false);

            var claims = await manager.GetClaimsAsync(role).ConfigureAwait(false);
            Assert.Contains(claims, (c) => c.Value == claim.Value && c.Type == claim.Type);//, "Claim not found");
        }

        private async Task RemoveRoleClaimAsyncHelper(IdentityRole role, Claim claim)
        {
            using RoleStore<IdentityRole> store = roleFixture.CreateRoleStore();
            using RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager();
            await manager.RemoveClaimAsync(role, claim).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.RemoveClaimAsync(null, claim)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.RemoveClaimAsync(role, null)).ConfigureAwait(false);

            var c1 = new Claim(string.Empty, claim.Value);
            await Assert.ThrowsAsync<ArgumentException>(() => store.RemoveClaimAsync(role, c1)).ConfigureAwait(false);

            var claims = await manager.GetClaimsAsync(role).ConfigureAwait(false);
            Assert.DoesNotContain(claims, (c) => c.Value == claim.Value && c.Type == claim.Type);  //, "Claim not found");
        }

        public virtual async Task CreateRoleTable()
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                await store.CreateTableIfNotExistsAsync().ConfigureAwait(false);
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

        public virtual async Task CreateRole()
        {
            using RoleStore<IdentityRole> store = roleFixture.CreateRoleStore();
            var role = await CreateRoleAsync().ConfigureAwait(false);

            WriteLineObject<IdentityRole>(role);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.CreateAsync(null)).ConfigureAwait(false);
        }

        protected async Task<IdentityRole> CreateRoleAsync()
        {
            using RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager();
            string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
            var role = new IdentityRole(roleNew);
            var sw = new Stopwatch();
            sw.Start();
            await manager.CreateAsync(role).ConfigureAwait(false);
            sw.Stop();

            _output.WriteLine("CreateRoleAsync: {0} seconds", sw.Elapsed.TotalSeconds);
            return role;
        }


        public virtual async Task ThrowIfDisposed()
        {
            using RoleStore<IdentityRole> store = roleFixture.CreateRoleStore();
            RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager();
            manager.Dispose();
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.DeleteAsync(null)).ConfigureAwait(false);
        }

        public virtual async Task UpdateRole()
        {
            using RoleStore<IdentityRole> store = roleFixture.CreateRoleStore();
            using RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager();
            string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());

            var role = new IdentityRole(roleNew);
            await manager.CreateAsync(role).ConfigureAwait(false);

            role.Name = Guid.NewGuid() + role.Name;
            await manager.UpdateAsync(role).ConfigureAwait(false);

            var rRole = await manager.FindByIdAsync(role.RowKey).ConfigureAwait(false);

            Assert.NotNull(rRole);
            Assert.Equal(role.RowKey, rRole.RowKey);
            Assert.NotEqual<string>(roleNew, rRole.Name);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.UpdateAsync(null)).ConfigureAwait(false);
        }

        public virtual async Task UpdateRole2()
        {
            using RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager();
            string roleNew = string.Format("{0}_TestRole", Guid.NewGuid());

            var role = new IdentityRole(roleNew);
            await manager.CreateAsync(role).ConfigureAwait(false);

            role.Name += Guid.NewGuid();
            await manager.UpdateAsync(role).ConfigureAwait(false);

            var rRole = await manager.FindByIdAsync(role.RowKey).ConfigureAwait(false);

            Assert.NotNull(rRole);
            Assert.Equal(role.RowKey, rRole.RowKey);
            Assert.NotEqual<string>(roleNew, rRole.Name);
        }

        public virtual async Task DeleteRole()
        {
            using RoleStore<IdentityRole> store = roleFixture.CreateRoleStore();
            using RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager();
            string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
            var role = new IdentityRole(roleNew);
            await manager.CreateAsync(role).ConfigureAwait(false);

            var sw = new Stopwatch();
            sw.Start();
            await manager.DeleteAsync(role).ConfigureAwait(false);
            sw.Stop();
            _output.WriteLine("DeleteRole: {0} seconds", sw.Elapsed.TotalSeconds);

            var result = await manager.FindByIdAsync(role.RowKey).ConfigureAwait(false);
            Assert.Null(result);
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.DeleteAsync(null)).ConfigureAwait(false);
        }


        public virtual async Task FindRoleById()
        {
            var role = await CreateRoleAsync().ConfigureAwait(false);
            using RoleStore<IdentityRole> store = roleFixture.CreateRoleStore();
            using RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager();
            var sw = new Stopwatch();
            sw.Start();
            var result = await manager.FindByIdAsync(role.Id).ConfigureAwait(false);
            sw.Stop();
            _output.WriteLine("FindByIdAsync: {0} seconds", sw.Elapsed.TotalSeconds);

            Assert.NotNull(result);
            WriteLineObject<IdentityRole>(result);
            Assert.Equal(role.Id, result.RowKey);
        }

        public virtual async Task FindRoleByName()
        {
            var role = await CreateRoleAsync().ConfigureAwait(false);

            using RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager();
            var sw = new Stopwatch();
            sw.Start();
            var result = await manager.FindByNameAsync(role.Name).ConfigureAwait(false);
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

        private void WriteLineObject<T>(T obj) where T : class
        {
            _output.WriteLine(typeof(T).Name);
            string strLine = obj == null ? "Null" : Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
            _output.WriteLine("{0}", strLine);
        }
    }
}
