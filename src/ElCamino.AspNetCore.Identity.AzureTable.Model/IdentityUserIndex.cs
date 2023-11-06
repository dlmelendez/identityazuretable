// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using Azure;
using Azure.Data.Tables;

namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    public class IdentityUserIndex : ITableEntity
    {
        /// <summary>
        /// Holds the userid entity key
        /// </summary>
        public string Id { get; set; } = string.Empty;

        public double KeyVersion { get; set; }
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; } = ETag.All;
    }
}
