using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using samplemvccore2.Models;

namespace samplemvccore2.Data
{
    public class ApplicationDbContext : IdentityCloudContext
    {
        public ApplicationDbContext() : base() { }

        public ApplicationDbContext(IdentityConfiguration config) : base(config) { }
    }
}
