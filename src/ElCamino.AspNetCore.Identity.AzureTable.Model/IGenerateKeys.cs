// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

namespace ElCamino.AspNetCore.Identity.AzureTable.Model
{
    /// <summary>
    /// Generates keys suitable for table storage
    /// </summary>
    public interface IGenerateKeys
    {
        /// <summary>
        /// Accept the keyhelper to generate keys for an entity
        /// </summary>
        /// <param name="keyHelper"></param>
        void GenerateKeys(IKeyHelper keyHelper);

        /// <summary>
        /// Returns the rowkey for the entity without setting it
        /// </summary>
        /// <param name="keyHelper"></param>
        /// <returns></returns>
        string PeekRowKey(IKeyHelper keyHelper);

        /// <summary>
        /// Key Version for the entity
        /// </summary>
        double KeyVersion { get; set; }
    }
}
