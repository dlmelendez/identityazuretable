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
    public class IdentityRoleTests
    {
        [Fact(DisplayName = "IdentityRoleSet_Id")]
        [Trait("Identity.Azure.Model", "")]
        public void IdentityRoleSet_Id()
        {
            var role = new IdentityRole();
            role.Id = Guid.NewGuid().ToString();
            Assert.Equal<string>(role.RowKey, role.Id);
        }
    }
}
