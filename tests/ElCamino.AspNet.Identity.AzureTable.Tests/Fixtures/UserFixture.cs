// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using ElCamino.AspNet.Identity.AzureTable.Tests;
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
    public partial class UserFixture<TUser, TRole, TContext> : BaseFixture<TUser, TRole, TContext>
        where TUser : IdentityUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityCloudContext, new()
    {
        public UserFixture() : base()
        {
        }

        public void Init()
        {
            CurrentUser = UserStoreTests.CreateUser<TUser>();
            CurrentEmailUser = UserStoreTests.CreateUser<TUser>();
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public TUser CurrentUser { get;  set; }
        public TUser CurrentEmailUser { get;  set; }

    }


}
