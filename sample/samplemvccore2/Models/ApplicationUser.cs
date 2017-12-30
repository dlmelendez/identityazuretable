using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElCamino.AspNetCore.Identity.AzureTable.Model;

namespace samplemvccore2.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUserV2 //or use IdentityUser if your code depends on the Role, Claim and Token collections
    {
    }
}
