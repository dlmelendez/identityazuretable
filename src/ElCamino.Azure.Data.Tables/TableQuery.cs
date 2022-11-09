// -----------------------------------------------------------------------------------------
// <copyright file="TableQuery.cs" company="Microsoft">
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

using System.Globalization;
using System.Text;

namespace Azure.Data.Tables
{
    /// <summary>
    /// From https://github.com/Azure/azure-storage-net/blob/v9.3.2/Lib/Common/Table/TableQuery.cs
    /// </summary>
    public class TableQuery
    {

        public int? TakeCount { get; set; }

        public string FilterString { get; set; }

        public List<string> SelectColumns { get; set; } = null;

        /// <summary>
        /// Generates a property filter condition string for the string value.
        /// </summary>
        /// <param name="propertyName">A string containing the name of the property to compare.</param>
        /// <param name="operation">A string containing the comparison operator to use.</param>
        /// <param name="givenValue">A string containing the value to compare with the property.</param>
        /// <returns>A string containing the formatted filter condition.</returns>
        public static string GenerateFilterCondition(string propertyName, string operation, string givenValue)
        {
            givenValue ??= string.Empty;
            return GenerateFilterCondition(propertyName, operation, givenValue, EdmType.String);
        }

        /// <summary>
        /// Generates a property filter condition string for the boolean value.
        /// </summary>
        /// <param name="propertyName">A string containing the name of the property to compare.</param>
        /// <param name="operation">A string containing the comparison operator to use.</param>
        /// <param name="givenValue">A <c>bool</c> containing the value to compare with the property.</param>
        /// <returns>A string containing the formatted filter condition.</returns>
        public static string GenerateFilterConditionForBool(string propertyName, string operation, bool givenValue)
        {
            return GenerateFilterCondition(propertyName, operation, givenValue ? "true" : "false", EdmType.Boolean);
        }

        /// <summary>
        /// Generates a property filter condition string for the binary value.
        /// </summary>
        /// <param name="propertyName">A string containing the name of the property to compare.</param>
        /// <param name="operation">A string containing the comparison operator to use.</param>
        /// <param name="givenValue">A byte array containing the value to compare with the property.</param>
        /// <returns>A string containing the formatted filter condition.</returns>
        public static string GenerateFilterConditionForBinary(
            string propertyName,
            string operation,
            byte[] givenValue)
        {
            string hash;

#if NET6_0_OR_GREATER
            hash = Convert.ToHexString(givenValue).ToLowerInvariant();
#else
            StringBuilder sb = new StringBuilder();

            foreach (byte b in givenValue)
            {
                sb.AppendFormat("{0:x2}", b);
            }

            hash = sb.ToString();
#endif

            return GenerateFilterCondition(propertyName, operation, hash, EdmType.Binary);
        }

        /// <summary>
        /// Generates a property filter condition string for the <see cref="DateTimeOffset"/> value.
        /// </summary>
        /// <param name="propertyName">A string containing the name of the property to compare.</param>
        /// <param name="operation">A string containing the comparison operator to use.</param>
        /// <param name="givenValue">A <see cref="DateTimeOffset"/> containing the value to compare with the property.</param>
        /// <returns>A string containing the formatted filter condition.</returns>
        public static string GenerateFilterConditionForDate(string propertyName, string operation, DateTimeOffset givenValue)
        {
            return GenerateFilterCondition(propertyName, operation, givenValue.UtcDateTime.ToString("o", CultureInfo.InvariantCulture), EdmType.DateTime);
        }

        /// <summary>
        /// Generates a property filter condition string for the <see cref="double"/> value.
        /// </summary>
        /// <param name="propertyName">A string containing the name of the property to compare.</param>
        /// <param name="operation">A string containing the comparison operator to use.</param>
        /// <param name="givenValue">A <see cref="double"/> containing the value to compare with the property.</param>
        /// <returns>A string containing the formatted filter condition.</returns>
        public static string GenerateFilterConditionForDouble(string propertyName, string operation, double givenValue)
        {
            return GenerateFilterCondition(propertyName, operation, Convert.ToString(givenValue, CultureInfo.InvariantCulture), EdmType.Double);
        }

        /// <summary>
        /// Generates a property filter condition string for an <see cref="int"/> value.
        /// </summary>
        /// <param name="propertyName">A string containing the name of the property to compare.</param>
        /// <param name="operation">A string containing the comparison operator to use.</param>
        /// <param name="givenValue">An <see cref="int"/> containing the value to compare with the property.</param>
        /// <returns>A string containing the formatted filter condition.</returns>
        public static string GenerateFilterConditionForInt(string propertyName, string operation, int givenValue)
        {
            return GenerateFilterCondition(propertyName, operation, Convert.ToString(givenValue, CultureInfo.InvariantCulture), EdmType.Int32);
        }

        /// <summary>
        /// Generates a property filter condition string for an <see cref="long"/> value.
        /// </summary>
        /// <param name="propertyName">A string containing the name of the property to compare.</param>
        /// <param name="operation">A string containing the comparison operator to use.</param>
        /// <param name="givenValue">An <see cref="long"/> containing the value to compare with the property.</param>
        /// <returns>A string containing the formatted filter condition.</returns>
        public static string GenerateFilterConditionForLong(string propertyName, string operation, long givenValue)
        {
            return GenerateFilterCondition(propertyName, operation, Convert.ToString(givenValue, CultureInfo.InvariantCulture), EdmType.Int64);
        }

        /// <summary>
        /// Generates a property filter condition string for the <see cref="Guid"/> value.
        /// </summary>
        /// <param name="propertyName">A string containing the name of the property to compare.</param>
        /// <param name="operation">A string containing the comparison operator to use.</param>
        /// <param name="givenValue">A <see cref="Guid"/> containing the value to compare with the property.</param>
        /// <returns>A string containing the formatted filter condition.</returns>
        public static string GenerateFilterConditionForGuid(string propertyName, string operation, Guid givenValue)
        {
            return GenerateFilterCondition(propertyName, operation, givenValue.ToString(), EdmType.Guid);
        }

        /// <summary>
        /// Generates a property filter condition string for the <see cref="EdmType"/> value, formatted as the specified <see cref="EdmType"/>.
        /// </summary>
        /// <param name="propertyName">A string containing the name of the property to compare.</param>
        /// <param name="operation">A string containing the comparison operator to use.</param>
        /// <param name="givenValue">A string containing the value to compare with the property.</param>
        /// <param name="edmType">The <see cref="EdmType"/> to format the value as.</param>
        /// <returns>A string containing the formatted filter condition.</returns>
        private static string GenerateFilterCondition(string propertyName, string operation, string givenValue, EdmType edmType)
        {
            string valueOperand;
            if (edmType == EdmType.Boolean || edmType == EdmType.Int32)
            {
                valueOperand = givenValue;
            }
            else if (edmType == EdmType.Double)
            {
#pragma warning disable IDE0059 // Unnecessary assignment of a value
                bool isInteger = int.TryParse(givenValue, out int parsedInt);
#pragma warning restore IDE0059 // Unnecessary assignment of a value
                valueOperand = isInteger ? string.Format(CultureInfo.InvariantCulture, "{0}.0", givenValue) : givenValue;
            }
            else if (edmType == EdmType.Int64)
            {
                valueOperand = string.Format(CultureInfo.InvariantCulture, "{0}L", givenValue);
            }
            else if (edmType == EdmType.DateTime)
            {
                valueOperand = string.Format(CultureInfo.InvariantCulture, "datetime'{0}'", givenValue);
            }
            else if (edmType == EdmType.Guid)
            {
                valueOperand = string.Format(CultureInfo.InvariantCulture, "guid'{0}'", givenValue);
            }
            else if (edmType == EdmType.Binary)
            {
                valueOperand = string.Format(CultureInfo.InvariantCulture, "X'{0}'", givenValue);
            }
            else
            {
                // OData readers expect single quote to be escaped in a param value.
                valueOperand = string.Format(CultureInfo.InvariantCulture, "'{0}'", givenValue.Replace("'", "''"));
            }

            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", propertyName, operation, valueOperand);
        }

        /// <summary>
        /// Creates a filter condition using the specified logical operator on two filter conditions.
        /// </summary>
        /// <param name="filterA">A string containing the first formatted filter condition.</param>
        /// <param name="operatorString">A string containing the operator to use (AND, OR).</param>
        /// <param name="filterB">A string containing the second formatted filter condition.</param>
        /// <returns>A string containing the combined filter expression.</returns>
        public static string CombineFilters(string filterA, string operatorString, string filterB)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0}) {1} ({2})", filterA, operatorString, filterB);
        }
    }
}
