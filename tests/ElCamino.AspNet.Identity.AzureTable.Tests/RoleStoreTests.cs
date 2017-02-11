// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Linq;
#if net45
using Microsoft.AspNet.Identity;
using ElCamino.AspNet.Identity.AzureTable;
using ElCamino.AspNet.Identity.AzureTable.Model;
#else
using Microsoft.AspNetCore.Identity;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
#endif
using Xunit;
using Xunit.Abstractions;
using ElCamino.Web.Identity.AzureTable.Tests.Fixtures;
using ElCamino.Web.Identity.AzureTable.Tests.ModelTests;
using System.Security.Claims;

namespace ElCamino.AspNet.Identity.AzureTable.Tests
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
#if net45
        [Trait("Identity.Azure.RoleStore", "")]
#else
        [Trait("Identity.Azure.RoleStoreV2", "")]
#endif
        public void RoleStoreCtors()
        {
            try
            {
                roleFixture.CreateRoleStore(null);
            }
            catch (ArgumentException) { }
        }


#if net45
        [Fact(DisplayName = "RoleStoreGet_Roles")]
        [Trait("Identity.Azure.RoleStore", "")]
        public void RoleStoreGet_Roles()
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                Xunit.Assert.NotNull(store.Roles);
            }
        }
#endif

#if !net45

        private Claim GenRoleClaim()
        {
            return new Claim(Constants.AccountClaimTypes.AccountTestUserClaim, Guid.NewGuid().ToString());
        }

        [Fact(DisplayName = "AddRemoveRoleClaim")]
        [Trait("Identity.Azure.UserStoreV2", "")]
        public void AddRemoveRoleClaim()
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                using (RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager(store))
                {
                    string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
                    var role = new IdentityRole(roleNew);
                    var start = DateTime.UtcNow;
                    var createTask = manager.CreateAsync(role);
                    createTask.Wait();

                    output.WriteLine("CreateRoleAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
                    Claim c1 = GenRoleClaim();
                    Claim c2 = GenRoleClaim();

                    AddRoleClaimHelper(role, c1);
                    AddRoleClaimHelper(role, c2);
                    
                    RemoveRoleClaimHelper(role, c1);

                }
            }
        }

        [Fact(DisplayName = "AddRoleClaim")]
        [Trait("Identity.Azure.UserStoreV2", "")]
        public void AddRoleClaim()
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                using (RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager(store))
                {
                    string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
                    var role = new IdentityRole(roleNew);
                    var start = DateTime.UtcNow;
                    var createTask = manager.CreateAsync(role);
                    createTask.Wait();

                    output.WriteLine("CreateRoleAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

                    AddRoleClaimHelper(role, GenRoleClaim());
                }
            }
        }

        private void AddRoleClaimHelper(IdentityRole role, Claim claim)
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                using (RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager(store))
                {
                    var userClaimTask = manager.AddClaimAsync(role, claim);

                    userClaimTask.Wait();
                    var claimsTask = manager.GetClaimsAsync(role);

                    claimsTask.Wait();
                    Assert.True(claimsTask.Result.ToList().Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not found");
                }
            }

        }

        private void RemoveRoleClaimHelper(IdentityRole role, Claim claim)
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                using (RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager(store))
                {
                    var userClaimTask = manager.RemoveClaimAsync(role, claim);

                    userClaimTask.Wait();
                    var claimsTask = manager.GetClaimsAsync(role);

                    claimsTask.Wait();
                    Assert.False(claimsTask.Result.ToList().Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not found");
                }
            }

        }

#endif
        [Fact(DisplayName = "CreateRole")]
#if net45
        [Trait("Identity.Azure.RoleStore", "")]
#else
        [Trait("Identity.Azure.RoleStoreV2", "")]
#endif
        public void CreateRole()
        {    
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                using (RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager(store))
                {
                    string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
                    var role = new IdentityRole(roleNew);
                    var start = DateTime.UtcNow;
                    var createTask = manager.CreateAsync(role);
                    createTask.Wait();
                    
                    output.WriteLine("CreateRoleAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

                    CurrentRole = role;
                    WriteLineObject<IdentityRole>(CurrentRole);

                    try
                    {
                        var task = store.CreateAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Xunit.Assert.NotNull(ex);
                    }
                }
            }
        }

        [Fact(DisplayName = "ThrowIfDisposed")]
#if net45
        [Trait("Identity.Azure.RoleStore", "")]
#else
        [Trait("Identity.Azure.RoleStoreV2", "")]
#endif
        public void ThrowIfDisposed()
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager(store);
                manager.Dispose();

                try
                {
                    var task = store.DeleteAsync(null);
                }
                catch (ArgumentException) { }
            }
        }

        [Fact(DisplayName = "UpdateRole")]
#if net45
        [Trait("Identity.Azure.RoleStore", "")]
#else
        [Trait("Identity.Azure.RoleStoreV2", "")]
#endif
        public void UpdateRole()
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                using (RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager(store))
                {
                    string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());

                    var role = new IdentityRole(roleNew);
                    var createTask = manager.CreateAsync(role);
                    createTask.Wait();

                    role.Name = Guid.NewGuid() + role.Name;
                    var updateTask = manager.UpdateAsync(role);
                    updateTask.Wait();

                    var findTask = manager.FindByIdAsync(role.RowKey);

                    Xunit.Assert.NotNull(findTask.Result);
                    Xunit.Assert.Equal<string>(role.RowKey, findTask.Result.RowKey);
                    Xunit.Assert.NotEqual<string>(roleNew, findTask.Result.Name);

                    try
                    {
                        var task = store.UpdateAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Xunit.Assert.NotNull(ex);
                    }
                }
            }
        }

        [Fact(DisplayName = "UpdateRole2")]
#if net45
        [Trait("Identity.Azure.RoleStore", "")]
#else
        [Trait("Identity.Azure.RoleStoreV2", "")]
#endif
        public void UpdateRole2()
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                using (RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager(store))
                {
                    string roleNew = string.Format("{0}_TestRole", Guid.NewGuid());

                    var role = new IdentityRole(roleNew);
                    var createTask = manager.CreateAsync(role);
                    createTask.Wait();

                    role.Name = role.Name + Guid.NewGuid();
                    var updateTask = manager.UpdateAsync(role);
                    updateTask.Wait();

                    var findTask = manager.FindByIdAsync(role.RowKey);
                    findTask.Wait();
                    Xunit.Assert.NotNull(findTask.Result);
                    Xunit.Assert.Equal<string>(role.RowKey, findTask.Result.RowKey);
                    Xunit.Assert.NotEqual<string>(roleNew, findTask.Result.Name);
                }
            }
        }

        [Fact(DisplayName = "DeleteRole")]
#if net45
        [Trait("Identity.Azure.RoleStore", "")]
#else
        [Trait("Identity.Azure.RoleStoreV2", "")]
#endif
        public void DeleteRole()
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                using (RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager(store))
                {
                    string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
                    var role = new IdentityRole(roleNew);
                    var createTask = manager.CreateAsync(role);
                    createTask.Wait();

                    var start = DateTime.UtcNow;
                    var delTask = manager.DeleteAsync(role);
                    delTask.Wait();
                    output.WriteLine("DeleteRole: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

                    var findTask = manager.FindByIdAsync(role.RowKey);
                    findTask.Wait();
                    Xunit.Assert.Null(findTask.Result);

                    try
                    {
                        var task = store.DeleteAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Xunit.Assert.NotNull(ex);
                    }
                }
            }
        }


        [Fact(DisplayName = "FindRoleById")]
#if net45
        [Trait("Identity.Azure.RoleStore", "")]
#else
        [Trait("Identity.Azure.RoleStoreV2", "")]
#endif
        public void FindRoleById()
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                using (RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager(store))
                {
                    DateTime start = DateTime.UtcNow;
                    var findTask = manager.FindByIdAsync(CurrentRole.Id);
                    findTask.Wait();
                    output.WriteLine("FindByIdAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

                    Xunit.Assert.NotNull(findTask.Result);
                    WriteLineObject<IdentityRole>(findTask.Result);
                    Xunit.Assert.Equal<string>(CurrentRole.Id, findTask.Result.RowKey);
                }
            }
        }

        [Fact(DisplayName = "FindRoleByName")]
#if net45
        [Trait("Identity.Azure.RoleStore", "")]
#else
        [Trait("Identity.Azure.RoleStoreV2", "")]
#endif
        public void FindRoleByName()
        {
            using (RoleStore<IdentityRole> store = roleFixture.CreateRoleStore())
            {
                using (RoleManager<IdentityRole> manager = roleFixture.CreateRoleManager(store))
                {
                    DateTime start = DateTime.UtcNow;
                    var findTask = manager.FindByNameAsync(CurrentRole.Name);
                    findTask.Wait();
                    output.WriteLine("FindByNameAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

                    Xunit.Assert.NotNull(findTask.Result);
                    Xunit.Assert.Equal<string>(CurrentRole.Name, findTask.Result.Name);
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
