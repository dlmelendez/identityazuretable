// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Identity;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using ElCamino.AspNetCore.Identity.AzureTable.Tests;

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
            CurrentUser = UserStoreTests.CreateUserAsync<TUser>().Result;
            CurrentEmailUser = UserStoreTests.CreateUserAsync<TUser>().Result;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public TUser CurrentUser { get;  set; }
        public TUser CurrentEmailUser { get;  set; }
    }
}
