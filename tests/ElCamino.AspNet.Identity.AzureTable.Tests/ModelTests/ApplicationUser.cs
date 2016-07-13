// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

#if net45
using ElCamino.AspNet.Identity.AzureTable.Model;
#else
using ElCamino.AspNetCore.Identity.AzureTable.Model;
#endif
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElCamino.Web.Identity.AzureTable.Tests.ModelTests
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }
    }
}
