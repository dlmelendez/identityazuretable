// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using ElCamino.AspNetCore.Identity.AzureTable.Tests.Fixtures;
using ElCamino.Web.Identity.AzureTable.Tests.ModelTests;
using IdentityUser = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityUser<string>;
using Model = ElCamino.AspNetCore.Identity.AzureTable.Model;

namespace ElCamino.Web.Identity.AzureTable.Tests.Fixtures
{
    public class UserFixture<TUser, TRole, TContext, TUserStore, TKeyHelper> : BaseFixture<TUser, TRole, TContext, TUserStore, TKeyHelper>
        where TUser : IdentityUser, IApplicationUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityCloudContext
        where TUserStore : UserStore<TUser, TRole, TContext>
        where TKeyHelper : IKeyHelper, new()
    {
        public UserFixture() : base()
        {
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

    }

    public class UserFixture<TUser, TContext, TUserStore, TKeyHelper> : BaseFixture<TUser, TContext, TUserStore, TKeyHelper>
       where TUser : IdentityUser, IApplicationUser, new()
       where TContext : IdentityCloudContext
       where TUserStore : UserOnlyStore<TUser, TContext, string, Model.IdentityUserClaim, Model.IdentityUserLogin, Model.IdentityUserToken>
       where TKeyHelper : IKeyHelper, new()
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
