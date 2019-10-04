using System;
using System.Collections.Generic;
using System.Text;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Model;

namespace samplemvccore4.Data
{
    public class ApplicationDbContext : IdentityCloudContext
    {
        public ApplicationDbContext() : base()
        {
        }

        public ApplicationDbContext(IdentityConfiguration config) : base(config)
        {
        }
    }
}
