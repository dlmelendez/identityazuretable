// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

namespace ElCamino.Web.Identity.AzureTable.Tests.ModelTests
{
    public interface IApplicationUser
    {
        string FirstName { get; set; }
        string LastName { get; set; }
    }
}