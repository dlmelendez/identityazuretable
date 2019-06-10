// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using ElCamino.AspNetCore.Identity.AzureTable.Model;

namespace ElCamino.Web.Identity.AzureTable.Tests.ModelTests
{
#pragma warning disable 0618
    public class ApplicationUser : IdentityUser, IApplicationUser
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }
    }
#pragma warning restore 0618
    public class ApplicationUserV2 : IdentityUserV2, IApplicationUser
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }
    }

}
