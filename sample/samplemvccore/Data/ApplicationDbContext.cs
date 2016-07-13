using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using samplemvccore.Models;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Model;

namespace samplemvccore.Data
{
    public class ApplicationDbContext : IdentityCloudContext
    {
        public ApplicationDbContext() : base() { }

        public ApplicationDbContext(IdentityConfiguration config) : base(config) { }
    }
}
