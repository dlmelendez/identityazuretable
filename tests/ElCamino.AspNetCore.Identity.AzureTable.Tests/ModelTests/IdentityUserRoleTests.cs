// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Xunit;

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests.ModelTests
{
    public class IdentityUserRoleTests
    {
        [Fact(DisplayName = "IdentityUserRoleGet_UserId")]
        [Trait("IdentityCore.Azure.Model", "")]
        public void IdentityUserRoleGet_UserId()
        {
            var ur = new IdentityUserRole();
            ur.GenerateKeys(new DefaultKeyHelper());
            Assert.Null(ur.UserId);
            Assert.Equal(string.Empty, ur.PartitionKey);

            var ur2 = new IdentityUserRole();
            ur2.GenerateKeys(new SHA256KeyHelper());
            Assert.Null(ur2.UserId);
            Assert.Equal(string.Empty, ur2.PartitionKey);
        }
    }
}
