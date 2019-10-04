// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos.Table;
using Xunit;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using ElCamino.Web.Identity.AzureTable.Tests.ModelTests;
using ElCamino.Web.Identity.AzureTable.Tests.Fixtures;
using IdentityUser = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityUser<string>;
using IdentityRole = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityRole;


namespace ElCamino.AspNetCore.Identity.AzureTable.Tests
{
    public partial class UserOnlyStoreTests : BaseUserStoreTests<ApplicationUserV2, IdentityCloudContext, UserOnlyStore<ApplicationUserV2, IdentityCloudContext>>
    {
        public const string UserOnlyStoreTraitProperties = UserOnlyStoreTrait + ".Properties";

        [Fact(DisplayName = "AccessFailedCount")]
        [Trait(UserOnlyStoreTraitProperties, "")]
        public override Task AccessFailedCount()
        {
            return base.AccessFailedCount();
        }

        [Fact(DisplayName = "Email")]
        [Trait(UserOnlyStoreTraitProperties, "")]
        public override Task Email()
        {
            return base.Email();
        }

        [Fact(DisplayName = "EmailConfirmed")]
        [Trait(UserOnlyStoreTraitProperties, "")]
        public override Task EmailConfirmed()
        {
            return base.EmailConfirmed();
        }

        [Fact(DisplayName = "EmailNone")]
        [Trait(UserOnlyStoreTraitProperties, "")]
        public override Task EmailNone()
        {
            return base.EmailNone();
        }

        [Fact(DisplayName = "LockoutEnabled")]
        [Trait(UserOnlyStoreTraitProperties, "")]
        public override Task LockoutEnabled()
        {
            return base.LockoutEnabled();
        }

        [Fact(DisplayName = "PasswordHash")]
        [Trait(UserOnlyStoreTraitProperties, "")]
        public override Task PasswordHash()
        {
            return base.PasswordHash();
        }

        [Fact(DisplayName = "PhoneNumber")]
        [Trait(UserOnlyStoreTraitProperties, "")]
        public override Task PhoneNumber()
        {
            return base.PhoneNumber();
        }

        [Fact(DisplayName = "PhoneNumberConfirmed")]
        [Trait(UserOnlyStoreTraitProperties, "")]
        public override Task PhoneNumberConfirmed()
        {
            return base.PhoneNumberConfirmed();
        }

        [Fact(DisplayName = "SecurityStamp")]
        [Trait(UserOnlyStoreTraitProperties, "")]
        public override Task SecurityStamp()
        {
            return base.SecurityStamp();
        }

        [Fact(DisplayName = "TwoFactorEnabled")]
        [Trait(UserOnlyStoreTraitProperties, "")]
        public override Task TwoFactorEnabled()
        {
            return base.TwoFactorEnabled();
        }
    }

}
