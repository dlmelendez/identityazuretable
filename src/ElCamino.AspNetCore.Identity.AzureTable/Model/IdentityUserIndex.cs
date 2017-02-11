// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if net45
namespace ElCamino.AspNet.Identity.AzureTable.Model
#else
namespace ElCamino.AspNetCore.Identity.AzureTable.Model
#endif
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
