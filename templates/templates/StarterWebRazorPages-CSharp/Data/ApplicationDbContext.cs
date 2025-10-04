using Azure.Data.Tables;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Model;

namespace samplerazorpagescore.Data
{
    public class ApplicationDbContext : IdentityCloudContext
    {
        public ApplicationDbContext(IdentityConfiguration config, TableServiceClient client) : base(config, client)
        {
        }
    }
}
