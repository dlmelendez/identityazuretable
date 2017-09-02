// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    public interface IGenerateKeys
    {
        void GenerateKeys();

        string PeekRowKey();

        double KeyVersion { get; set; }
    }
}
