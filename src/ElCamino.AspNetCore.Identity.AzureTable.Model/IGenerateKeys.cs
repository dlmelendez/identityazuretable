// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    public interface IGenerateKeys
    {
        void GenerateKeys(IKeyHelper keyHelper);

        string PeekRowKey(IKeyHelper keyHelper);

        double KeyVersion { get; set; }
    }
}
