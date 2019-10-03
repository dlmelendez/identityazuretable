// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElCamino.AspNetCore.Identity.AzureTable;
using IdentityRole = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityRole;
using Microsoft.AspNetCore.Identity;

namespace ElCamino.Web.Identity.AzureTable.Tests.Fixtures
{
#pragma warning disable 0618
    public partial class RoleFixture<TUser, TRole, TContext> : BaseFixture<TUser, TRole, TContext, UserStoreV2<TUser, TRole, TContext>>
        where TUser : AspNetCore.Identity.AzureTable.Model.IdentityUserV2, new()
#pragma warning restore 0618
        where TRole : IdentityRole, new()
        where TContext : IdentityCloudContext, new()
    {

        public RoleFixture() : base()
        {
        }

       
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

    }
}
