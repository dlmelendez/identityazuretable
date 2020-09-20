// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Xunit;

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests.ModelTests
{
    public class IdentityRoleTests
    {
        [Fact(DisplayName = "IdentityRoleSet_Id")]
        [Trait("IdentityCore.Azure.Model", "")]
        public void IdentityRoleSet_Id()
        {
            var role = new IdentityRole();
            role.Id = Guid.NewGuid().ToString();
            Assert.Equal(role.RowKey, role.Id);
        }
    }
}
