// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Resources;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Xunit;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.Web.Identity.AzureTable.Tests.Fixtures;
using Xunit.Abstractions;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests.ModelTests
{
    public class IdentityCloudContextTests : IClassFixture<RoleFixture<IdentityUser, IdentityRole, IdentityCloudContext, DefaultKeyHelper>>
    {
        private readonly ITestOutputHelper output;
        private RoleFixture<IdentityUser, IdentityRole, IdentityCloudContext, DefaultKeyHelper> roleFixture;
        public IdentityCloudContextTests(RoleFixture<IdentityUser, IdentityRole, IdentityCloudContext, DefaultKeyHelper> roleFix, ITestOutputHelper output)
        {
            this.output = output;
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
            var tableConfig = roleFixture.GetConfig();
            tableConfig.TablePrefix = string.Empty;
            new IdentityCloudContext(tableConfig);

            tableConfig.TablePrefix = "a";
            var tContext = new IdentityCloudContext(tableConfig);
            //Covers Client get
            Assert.NotNull(tContext.Client);
            tContext.Dispose();
            Assert.Throws<ObjectDisposedException>(() => tContext.Client);

        }
    }

}
