// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using Newtonsoft.Json;

#if net45
namespace ElCamino.AspNet.Identity.AzureTable.Model
#else
namespace ElCamino.AspNetCore.Identity.AzureTable.Model
#endif
{
    [JsonObject("identityConfiguration")]
    public class IdentityConfiguration
    {
        [JsonProperty("tablePrefix")]
        public string TablePrefix { get; set; }

        [JsonProperty("storageConnectionString")]
        public string StorageConnectionString { get; set; }

        [JsonProperty("locationMode")]
        public string LocationMode { get; set; }

    }
}
