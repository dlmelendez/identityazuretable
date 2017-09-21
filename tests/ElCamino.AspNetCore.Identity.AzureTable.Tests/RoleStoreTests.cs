// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using ElCamino.Web.Identity.AzureTable.Tests.Fixtures;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Microsoft.AspNetCore.Identity;
using IdentityUser = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityUser;
using IdentityRole = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityRole;

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests
{
    public class RoleStoreTests : IClassFixture<RoleFixture<IdentityUser, IdentityRole, IdentityCloudContext>>
    {
        private IdentityRole CurrentRole;
        private readonly ITestOutputHelper output;
        private RoleFixture<IdentityUser, IdentityRole, IdentityCloudContext> roleFixture;

        public RoleStoreTests(RoleFixture<IdentityUser, IdentityRole, IdentityCloudContext> roleFix, ITestOutputHelper output)
        {
            this.output = output;
            CurrentRole = roleFix.CurrentRole;
            roleFixture = roleFix;
        }

        [Fact(DisplayName = "RoleStoreCtors")]
        [Trait("IdentityCore.Azure.RoleStore", "")]
        public void RoleStoreCtors()
        {
            Assert.Throws<ArgumentNullException>(() => roleFixture.CreateRoleStore(null));
        }

        private Claim GenRoleClaim()
         => new Claim(Constants.AccountClaimTypes.AccountTestUserClaim, Guid.NewGuid().ToString());

        [Fact(DisplayName = "AddRemoveRoleClaim")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public async Task AddRemoveRoleClaim()
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                using (RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager(store))
                {
                    string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
                    var role = new IdentityRole(roleNew);
                    var sw = new Stopwatch();
                    sw.Start();
                    await manager.CreateAsync(role);
                    sw.Stop();

                    output.WriteLine("CreateRoleAsync: {0} seconds", sw.Elapsed.TotalSeconds);
                    Claim c1 = GenRoleClaim();
                    Claim c2 = GenRoleClaim();

                    await AddRoleClaimAsyncHelper(role, c1);
                    await AddRoleClaimAsyncHelper(role, c2);

                    await RemoveRoleClaimAsyncHelper(role, c1);
                }
            }
        }

        [Fact(DisplayName = "AddRoleClaim")]
        [Trait("IdentityCore.Azure.UserStore", "")]
        public async Task AddRoleClaim()
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                using (RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager(store))
                {
                    string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
                    var role = new IdentityRole(roleNew);
                    var sw = new Stopwatch();
                    sw.Start();
                    await manager.CreateAsync(role);
                    sw.Stop();

                    output.WriteLine("CreateRoleAsync: {0} seconds", sw.Elapsed.TotalSeconds);

                    await AddRoleClaimAsyncHelper(role, GenRoleClaim());
                }
            }
        }

        private async Task AddRoleClaimAsyncHelper(IdentityRole role, Claim claim)
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                using (RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager(store))
                {
                    await manager.AddClaimAsync(role, claim);
                    var claims = await manager.GetClaimsAsync(role);
                    Assert.True(claims.ToList().Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not found");
                }
            }
        }

        private async Task RemoveRoleClaimAsyncHelper(IdentityRole role, Claim claim)
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                using (RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager(store))
                {
                    await manager.RemoveClaimAsync(role, claim);
                    var claims = await manager.GetClaimsAsync(role);
                    Assert.False(claims.ToList().Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not found");
                }
            }
        }

        [Fact(DisplayName = "CreateRoleTable")]
        [Trait("IdentityCore.Azure.RoleStore", "")]
        public async Task CreateRoleTable()
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                var r = await store.CreateTableIfNotExistsAsync();
                Assert.True(await store.Context.RoleTable.ExistsAsync());
            }
        }

        [Fact(DisplayName = "CreateRole")]
        [Trait("IdentityCore.Azure.RoleStore", "")]
        public async Task CreateRole()
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                using (RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager(store))
                {
                    string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
                    var role = new IdentityRole(roleNew);
                    var sw = new Stopwatch();
                    sw.Start();
                    await manager.CreateAsync(role);
                    sw.Stop();

                    output.WriteLine("CreateRoleAsync: {0} seconds", sw.Elapsed.TotalSeconds);

                    CurrentRole = role;
                    WriteLineObject<IdentityRole>(CurrentRole);
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.CreateAsync(null));
                }
            }
        }

        [Fact(DisplayName = "ThrowIfDisposed")]
        [Trait("IdentityCore.Azure.RoleStore", "")]
        public async Task ThrowIfDisposed()
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager(store);
                manager.Dispose();
                await Assert.ThrowsAsync<ArgumentNullException>(() => store.DeleteAsync(null));
            }
        }

        [Fact(DisplayName = "UpdateRole")]
        [Trait("IdentityCore.Azure.RoleStore", "")]
        public async Task UpdateRole()
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                using (RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager(store))
                {
                    string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());

                    var role = new IdentityRole(roleNew);
                    await manager.CreateAsync(role);

                    role.Name = Guid.NewGuid() + role.Name;
                    await manager.UpdateAsync(role);

                    var findTask = manager.FindByIdAsync(role.RowKey);

                    Assert.NotNull(findTask.Result);
                    Assert.Equal<string>(role.RowKey, findTask.Result.RowKey);
                    Assert.NotEqual<string>(roleNew, findTask.Result.Name);
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.UpdateAsync(null));
                }
            }
        }

        [Fact(DisplayName = "UpdateRole2")]
        [Trait("IdentityCore.Azure.RoleStore", "")]
        public async Task UpdateRole2()
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                using (RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager(store))
                {
                    string roleNew = string.Format("{0}_TestRole", Guid.NewGuid());

                    var role = new IdentityRole(roleNew);
                    await manager.CreateAsync(role);

                    role.Name = role.Name + Guid.NewGuid();
                    await manager.UpdateAsync(role);

                    var findTask = manager.FindByIdAsync(role.RowKey);
                    findTask.Wait();
                    Assert.NotNull(findTask.Result);
                    Assert.Equal<string>(role.RowKey, findTask.Result.RowKey);
                    Assert.NotEqual<string>(roleNew, findTask.Result.Name);
                }
            }
        }

        [Fact(DisplayName = "DeleteRole")]
        [Trait("IdentityCore.Azure.RoleStore", "")]
        public async Task DeleteRole()
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                using (RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager(store))
                {
                    string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
                    var role = new IdentityRole(roleNew);
                    await manager.CreateAsync(role);

                    var sw = new Stopwatch();
                    sw.Start();
                    await manager.DeleteAsync(role);
                    sw.Stop();
                    output.WriteLine("DeleteRole: {0} seconds", sw.Elapsed.TotalSeconds);

                    var result = await manager.FindByIdAsync(role.RowKey);
                    Assert.Null(result);
                    await Assert.ThrowsAsync<ArgumentNullException>(() => store.DeleteAsync(null));
                }
            }
        }


        [Fact(DisplayName = "FindRoleById")]
        [Trait("IdentityCore.Azure.RoleStore", "")]
        public async Task FindRoleById()
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                using (RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager(store))
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    var result = await manager.FindByIdAsync(CurrentRole.Id);
                    sw.Stop();
                    output.WriteLine("FindByIdAsync: {0} seconds", sw.Elapsed.TotalSeconds);

                    Assert.NotNull(result);
                    WriteLineObject<IdentityRole>(result);
                    Assert.Equal<string>(CurrentRole.Id, result.RowKey);
                }
            }
        }

        [Fact(DisplayName = "FindRoleByName")]
        [Trait("IdentityCore.Azure.RoleStore", "")]
        public async Task FindRoleByName()
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                using (RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager(store))
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    var result = await manager.FindByNameAsync(CurrentRole.Name);
                    sw.Stop();
                    output.WriteLine("FindByNameAsync: {0} seconds", sw.Elapsed.TotalSeconds);

                    Assert.NotNull(result);
                    Assert.Equal<string>(CurrentRole.Name, result.Name);
                }
            }
        }

        private void WriteLineObject<t>(t obj) where t : class
        {
            output.WriteLine(typeof(t).Name);
            string strLine = obj == null ? "Null" : Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
            output.WriteLine("{0}", strLine);
        }
    }
}
