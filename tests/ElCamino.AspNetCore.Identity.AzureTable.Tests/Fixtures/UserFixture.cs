// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using ElCamino.AspNetCore.Identity.AzureTable.Tests;
using ElCamino.Web.Identity.AzureTable.Tests.ModelTests;
using IdentityUser = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityUser<string>;
using Model = ElCamino.AspNetCore.Identity.AzureTable.Model;

namespace ElCamino.Web.Identity.AzureTable.Tests.Fixtures
{
    public partial class UserFixture<TUser, TRole, TContext, TUserStore> : BaseFixture<TUser, TRole, TContext, TUserStore>
        where TUser : IdentityUser, IApplicationUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityCloudContext, new()
        where TUserStore : UserStore<TUser, TRole, TContext>
    {
        public UserFixture() : base()
        {
        }

      
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

    }

    public partial class UserFixture<TUser, TContext, TUserStore> : BaseFixture<TUser, TContext, TUserStore>
       where TUser : IdentityUser, IApplicationUser, new()
       where TContext : IdentityCloudContext, new()
       where TUserStore : UserOnlyStore<TUser, TContext, string, Model.IdentityUserClaim, Model.IdentityUserLogin, Model.IdentityUserToken>
    {
        public UserFixture() : base()
        {
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

    }
}
