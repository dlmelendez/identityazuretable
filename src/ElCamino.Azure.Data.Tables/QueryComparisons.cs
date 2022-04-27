// -----------------------------------------------------------------------------------------
// <copyright file="QueryComparisons.cs" company="Microsoft">
//    Copyright 2013 Microsoft Corporation
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// -----------------------------------------------------------------------------------------


namespace Azure.Data.Tables
{
    /// <summary>
    /// From https://github.com/Azure/azure-storage-net/blob/v9.3.2/Lib/Common/Table/QueryComparisons.cs
    /// </summary>
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
