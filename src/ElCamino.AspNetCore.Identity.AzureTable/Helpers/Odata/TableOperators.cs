using System;
using System.Collections.Generic;
using System.Text;

namespace ElCamino.AspNetCore.Identity.AzureTable.Helpers
{
    /// <summary>
    /// From https://github.com/Azure/azure-storage-net/blob/v9.3.2/Lib/Common/Table/TableOperators.cs
    /// </summary>
    public static class TableOperators
    {
        public const string And = "and";

        /// <summary>
        /// Represents the Not operator.
        /// </summary>
        public const string Not = "not";

        /// <summary>
        /// Represents the Or operator.
        /// </summary>
        public const string Or = "or";
    }
}
