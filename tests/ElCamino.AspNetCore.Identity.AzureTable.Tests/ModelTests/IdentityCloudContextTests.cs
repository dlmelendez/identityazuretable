// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Resources;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Xunit;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.Web.Identity.AzureTable.Tests.Fixtures;
using Xunit.Abstractions;

namespace ElCamino.AspNet.Identity.AzureTable.Tests.ModelTests
{
    public class IdentityCloudContextTests : IClassFixture<RoleFixture<IdentityUser, IdentityRole, IdentityCloudContext>>
    {
        private readonly ITestOutputHelper output;
        private RoleFixture<IdentityUser, IdentityRole, IdentityCloudContext> roleFixture;

        public IdentityCloudContextTests(RoleFixture<IdentityUser, IdentityRole, IdentityCloudContext> roleFix, ITestOutputHelper output)
        {
            this.output = output;
            roleFixture = roleFix;
        }

        [Fact(DisplayName = "IdentityCloudContextCtors")]
        [Trait("IdentityCore.Azure.Model", "")]
        public void IdentityCloudContextCtors()
        {
            var ic = new IdentityCloudContext();
            Assert.NotNull(ic);

            Assert.Throws<ArgumentNullException>(() => new IdentityCloudContext(null));
            var locConfig = roleFixture.GetConfig();
            locConfig.LocationMode = "invalidMode";
            Assert.Throws<ArgumentException>(() => new IdentityCloudContext(locConfig));

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
