﻿// -----------------------------------------------------------------------------------------
// <copyright file="TableOperators.cs" company="Microsoft">
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

using ElCamino.Azure.Data.Tables;

namespace Azure.Data.Tables
{
    /// <summary>
    /// From https://github.com/Azure/azure-storage-net/blob/v9.3.2/Lib/Common/Table/TableOperators.cs
    /// </summary>
    public static class TableOperators
    {
        /// <summary>
        /// Represents the And operator.
        /// </summary>

        public const string And = "and";

        /// <summary>
        /// Represents the Not operator.
        /// </summary>
        public const string Not = "not";

        /// <summary>
        /// Represents the Or operator.
        /// </summary>
        public const string Or = "or";

        /// <summary>
        /// Gets the operator string for the specified <see cref="TableOperator"/>.
        /// </summary>
        /// <param name="tableOperator"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException"></exception>
        public static string GetOperator(TableOperator tableOperator)
        {
            return tableOperator switch
            {
                TableOperator.And => And,
                TableOperator.Not => Not,
                TableOperator.Or => Or,
                _ => throw new ArgumentException($"Invalid table operator: {tableOperator}", nameof(tableOperator)),
            };
        }
    }
}
