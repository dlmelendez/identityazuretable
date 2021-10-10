// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    public interface IGenerateKeys
    {
        void GenerateKeys(IKeyHelper keyHelper);

        string PeekRowKey(IKeyHelper keyHelper);

        double KeyVersion { get; set; }
    }
}
