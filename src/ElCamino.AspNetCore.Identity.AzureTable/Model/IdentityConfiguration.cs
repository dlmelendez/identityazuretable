// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using Newtonsoft.Json;

namespace ElCamino.AspNetCore.Identity.AzureTable.Model
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

        /// <summary>
        /// If true, then the user ids will never be updated, if false it will change when you change the user name.
        /// Default : false
        /// </summary>
        [JsonProperty("enableImmutableUserId")]
        public bool EnableImmutableUserId { get; set; }
    }
}
