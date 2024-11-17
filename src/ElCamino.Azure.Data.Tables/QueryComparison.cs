using System;
using System.Collections.Generic;
using System.Text;

namespace ElCamino.Azure.Data.Tables
{
    /// <summary>
    /// EXPERIMENTAL: Used for TableQueryBuilder
    /// From https://github.com/Azure/azure-storage-net/blob/v9.3.2/Lib/Common/Table/QueryComparisons.cs
    /// </summary>
    public enum QueryComparison : byte
    {
        /// <summary>
        /// Represents the Equal operator.
        /// </summary>
        Equal,

        /// <summary>
        /// Represents the Not Equal operator.
        /// </summary>
        NotEqual,

        /// <summary>
        /// Represents the Greater Than operator.
        /// </summary>
        GreaterThan,

        /// <summary>
        /// Represents the Greater Than or Equal operator.
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// Represents the Less Than operator.
        /// </summary>
        LessThan,

        /// <summary>
        /// Represents the Less Than or Equal operator.
        /// </summary>
        LessThanOrEqual
    }
}
