// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
#if net45
using ElCamino.AspNet.Identity.AzureTable.Model;
#else
using ElCamino.AspNetCore.Identity.AzureTable.Model;
#endif
using Xunit;

namespace ElCamino.Web.Identity.AzureTable.Tests.ModelTests
{
    public class IdentityUserRoleTests
    {
        [Fact(DisplayName = "IdentityUserRoleGet_UserId")]
        [Trait("Identity.Azure.Model", "")]
        public void IdentityUserRoleGet_UserId()
        {
            var ur = new IdentityUserRole();
            ur.GenerateKeys();
            Assert.Equal(ur.PartitionKey, ur.UserId);
        }
    }
}
