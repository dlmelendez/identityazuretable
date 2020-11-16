using System;
using System.Collections.Generic;
using System.Text;

namespace ElCamino.AspNetCore.Identity.AzureTable.Helpers
{
    public static class QueryComparisons
    {
        /// <summary>
        /// Represents the Equal operator.
        /// </summary>
        public const string Equal = "eq";

        /// <summary>
        /// Represents the Not Equal operator.
        /// </summary>
        public const string NotEqual = "ne";

        /// <summary>
        /// Represents the Greater Than operator.
        /// </summary>
        public const string GreaterThan = "gt";

        /// <summary>
        /// Represents the Greater Than or Equal operator.
        /// </summary>
        public const string GreaterThanOrEqual = "ge";

        /// <summary>
        /// Represents the Less Than operator.
        /// </summary>
        public const string LessThan = "lt";

        /// <summary>
        /// Represents the Less Than or Equal operator.
        /// </summary>
        public const string LessThanOrEqual = "le";
    }
}
