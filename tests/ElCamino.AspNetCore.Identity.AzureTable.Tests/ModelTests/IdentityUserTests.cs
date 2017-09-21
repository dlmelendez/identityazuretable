// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using Xunit;
using ElCamino.AspNetCore.Identity.AzureTable.Model;

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests.ModelTests
{
    public class IdentityUserTests
    {
        [Fact(DisplayName = "IdentityUserCtors")]
        [Trait("IdentityCore.Azure.Model", "")]
        public void IdentityUserCtors()
        {
            Assert.NotNull(new IdentityUser(Guid.NewGuid().ToString()));
        }
    }
}
