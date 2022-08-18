// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Xunit;

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
