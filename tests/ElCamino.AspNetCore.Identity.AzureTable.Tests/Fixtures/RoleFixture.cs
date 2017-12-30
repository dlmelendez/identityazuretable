// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElCamino.AspNetCore.Identity.AzureTable;
using IdentityUser = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityUser;
using IdentityRole = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityRole;
using Microsoft.AspNetCore.Identity;

namespace ElCamino.Web.Identity.AzureTable.Tests.Fixtures
{
    public partial class RoleFixture<TUser, TRole, TContext> : BaseFixture<TUser, TRole, TContext, UserStore<TUser, TRole, TContext>>
        where TUser : IdentityUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityCloudContext, new()
    {
        private IdentityRole currentRole;

        public RoleFixture() : base()
        {
        }

        public async Task CreateRoleAsync()
        {
            using (RoleManager<TRole> manager = CreateRoleManager())
            {
                string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());

                var role = new TRole() { Name = roleNew };
                role.GenerateKeys();
                await manager.CreateAsync(role);
                currentRole = role;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public IdentityRole CurrentRole
        {
            get
            {
                if(currentRole == null)
                {
                    CreateRoleAsync().Wait();
                }

                return currentRole;
            }
        }
    }
}
