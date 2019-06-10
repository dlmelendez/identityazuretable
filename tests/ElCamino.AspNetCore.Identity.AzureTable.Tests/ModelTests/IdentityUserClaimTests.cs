// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Xunit;

namespace ElCamino.Web.Identity.AzureTable.Tests.ModelTests
{
    public class IdentityUserClaimTests
    {
        [Fact(DisplayName = "IdentityUserClaimGet_UserId")]
        [Trait("IdentityCore.Azure.Model", "")]
        public void IdentityUserClaimGet_UserId()
        {
            var uc = new IdentityUserClaim();
            uc.GenerateKeys();
        }
    }
}
