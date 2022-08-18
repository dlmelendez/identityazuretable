// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Xunit;

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests.ModelTests
{
    public class IdentityUserClaimTests
    {
        [Fact(DisplayName = "IdentityUserClaimGet_UserId")]
        [Trait("IdentityCore.Azure.Model", "")]
        public void IdentityUserClaimGet_UserId()
        {
            var uc = new IdentityUserClaim();
            uc.GenerateKeys(new DefaultKeyHelper());

            var uc2 = new IdentityUserClaim();
            uc2.GenerateKeys(new SHA256KeyHelper());
        }
    }
}
