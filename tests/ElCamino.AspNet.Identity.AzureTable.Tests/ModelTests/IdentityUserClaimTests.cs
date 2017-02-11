// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
#if net45
using ElCamino.AspNet.Identity.AzureTable.Model;
#else
using ElCamino.AspNetCore.Identity.AzureTable.Model;
#endif
using Xunit;

namespace ElCamino.Web.Identity.AzureTable.Tests.ModelTests
{
    public class IdentityUserClaimTests
    {
        [Fact(DisplayName = "IdentityUserClaimGet_UserId")]
        [Trait("Identity.Azure.Model", "")]
        public void IdentityUserClaimGet_UserId()
        {
            var uc = new IdentityUserClaim();
            uc.GenerateKeys();
            Assert.Equal(uc.PartitionKey, uc.UserId);
        }
    }
}
