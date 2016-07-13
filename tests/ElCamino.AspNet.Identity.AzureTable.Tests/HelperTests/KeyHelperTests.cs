// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using Xunit;
#if net45
using ElCamino.AspNet.Identity.AzureTable.Helpers;
#else
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
#endif

namespace ElCamino.AspNet.Identity.AzureTable.Tests.HelperTests
{
    public class KeyHelperTests
    {
        [Fact(DisplayName = "GeneratePartitionKeyIndexByEmail")]
        [Trait("Identity.Azure.Helper.KeyHelper", "")]
        public void GeneratePartitionKeyIndexByEmail()
        {
            //Only keeping this method around for any backwards compat issues.
            string strEmail = Guid.NewGuid().ToString() + "@.hotmail.com";
            string key = KeyHelper.GeneratePartitionKeyIndexByEmail(strEmail);
        }
    }
}
