// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using Azure.Data.Tables;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using ElCamino.Web.Identity.AzureTable.Tests.Fixtures;
using Xunit;

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests.ModelTests
{
    public class IdentityCloudContextTests : IClassFixture<RoleFixture<IdentityUser, IdentityRole, IdentityCloudContext, DefaultKeyHelper>>
    {
        private readonly RoleFixture<IdentityUser, IdentityRole, IdentityCloudContext, DefaultKeyHelper> roleFixture;
        public IdentityCloudContextTests(RoleFixture<IdentityUser, IdentityRole, IdentityCloudContext, DefaultKeyHelper> roleFix)
        {
            roleFixture = roleFix;
        }

        [Fact(DisplayName = "IdentityCloudContextCtors")]
        [Trait("IdentityCore.Azure.Model", "")]
        public void IdentityCloudContextCtors()
        {
            Assert.Throws<ArgumentNullException>(() => new IdentityCloudContext(null));
            var locConfig = roleFixture.GetConfig();
            //LocationMode is deprecated
            //locConfig.LocationMode = "invalidMode";
            //Assert.Throws<ArgumentException>(() => new IdentityCloudContext(locConfig));

            //Coverage for FormatTableNameWithPrefix()
            var tableConfig = roleFixture.GetConfig().config;
            tableConfig.TablePrefix = string.Empty;
            TableServiceClient client = new TableServiceClient(roleFixture.GetConfig().connectionString);
            var tContext = new IdentityCloudContext(tableConfig, client);

            tableConfig.TablePrefix = "a";
            tContext = new IdentityCloudContext(tableConfig);
            //Covers Client get
            Assert.NotNull(tContext.Client);
        }
    }

}
