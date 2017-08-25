// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    internal class IdentityUserIndex : TableEntity
    {
        /// <summary>
        /// Holds the userid entity key
        /// </summary>
        public string Id { get; set; }

        public double KeyVersion { get; set; }
    }
}
