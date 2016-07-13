// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if net45
using ElCamino.AspNet.Identity.AzureTable;
using ElCamino.AspNet.Identity.AzureTable.Model;
using Microsoft.AspNet.Identity;
#else
using Microsoft.AspNetCore.Identity;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Model;

#endif

namespace ElCamino.Web.Identity.AzureTable.Tests.Fixtures
{
    public partial class RoleFixture<TUser, TRole, TContext> : BaseFixture<TUser, TRole, TContext>
        where TUser : IdentityUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityCloudContext, new()
    {
        public RoleFixture() : base()
        {
            CreateRole();
        }

        public void CreateRole()
        {
            using (RoleManager<TRole> manager = CreateRoleManager())
            {
                string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
                
                var role = new TRole();
                role.Name = roleNew;
                role.GenerateKeys();
                var createTask = manager.CreateAsync(role);
                createTask.Wait();
                CurrentRole = role;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
        

        public IdentityRole CurrentRole { get; private set; }
    }


}
