// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
#if net45
using ElCamino.AspNet.Identity.AzureTable.Model;
#else
using ElCamino.AspNetCore.Identity.AzureTable.Model;
#endif
using Xunit;

namespace ElCamino.AspNet.Identity.AzureTable.Tests.ModelTests
{
    public class IdentityUserTests
    {
        [Fact(DisplayName = "IdentityUserCtors")]
        [Trait("Identity.Azure.Model", "")]
        public void IdentityUserCtors()
        {
            Assert.NotNull(new IdentityUser(Guid.NewGuid().ToString()));
        }
    }
}
