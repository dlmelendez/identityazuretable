using ElCamino.AspNetCore.Identity.AzureTable.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace samplemvccore3.Models
{
    public class ApplicationUser : IdentityUser //or use IdentityUser if your code depends on the Role, Claim and Token collections
    {
    }
}
