﻿// -----------------------------------------------------------------------------------------
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
        private const string OdataTrue = "true";
        private const string OdataFalse = "false";

        /// <summary>
        /// Max take count for a given query
        /// </summary>
        public int? TakeCount { get; set; }

        /// <summary>
        /// Defines Odata query string
        /// </summary>
        public string? FilterString { get; set; }

        /// <summary>
        /// If defined, only returns the given column names in the query. null value returns all columns.
        /// </summary>
        public List<string>? SelectColumns { get; set; } = null;

        /// <summary>
        /// Generates a property filter condition string for the string value.
        /// </summary>
        /// <param name="propertyName">A string containing the name of the property to compare.</param>
        /// <param name="operation">A string containing the comparison operator to use.</param>
        /// <param name="givenValue">A string containing the value to compare with the property.</param>
        /// <returns>A string containing the formatted filter condition.</returns>
        public static string GenerateFilterCondition(string propertyName, string operation, string? givenValue)
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
            return GenerateFilterCondition(propertyName, operation, givenValue ? OdataTrue : OdataFalse, EdmType.Boolean);
        }

        /// <summary>
        /// Generates a property filter condition string for a null boolean value.
        /// </summary>
        /// <param name="propertyName">A string containing the name of the property to compare.</param>
        /// <param name="operation">A string containing the comparison operator to use.  <seealso cref="QueryComparisons.Equal"/> Is Null or <seealso cref="QueryComparisons.NotEqual"/> Not Null</param>
        /// <returns>A string containing the formatted filter condition.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static string GenerateFilterConditionForBoolNull(string propertyName, string operation)
        {
            string validBoolean = $"({GenerateFilterConditionForBool(propertyName, QueryComparisons.Equal, true)} {TableOperators.Or} {GenerateFilterConditionForBool(propertyName, QueryComparisons.Equal, false)})";
            switch (operation)
            {
                case QueryComparisons.Equal: //isNull
                    return $"not {validBoolean}";
                case QueryComparisons.NotEqual: //notNull
                    return validBoolean;
                default:
                    break;
            }

            throw new ArgumentOutOfRangeException(nameof(operation), $"{operation ?? string.Empty} is not supported. Only {QueryComparisons.Equal} and {QueryComparisons.NotEqual} operators are supported.");
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
        /// Generates a property filter condition string for a null string value.
        /// </summary>
        /// <param name="propertyName">A string containing the name of the property to compare.</param>
        /// <param name="operation">A string containing the comparison operator to use. <seealso cref="QueryComparisons.Equal"/> Is Null or <seealso cref="QueryComparisons.NotEqual"/> Not Null</param>
        /// <returns>A string containing the formatted filter condition.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static string GenerateFilterConditionForStringNull(string propertyName, string operation)
        {
            string validString = $"{propertyName} {QueryComparisons.GreaterThan} ''";
            switch (operation)
            {
                case QueryComparisons.Equal: //isNull
                    return $"not ({validString})";
                case QueryComparisons.NotEqual: //notNull
                    return validString;
                default:
                    break;
            }

            throw new ArgumentOutOfRangeException(nameof(operation), $"{operation ?? string.Empty} is not supported. Only {QueryComparisons.Equal} and {QueryComparisons.NotEqual} operators are supported.");
        }

#if NET8_0_OR_GREATER
        /// <summary>
        /// Generate operand value for the given value and <see cref="EdmType"/>.
        /// </summary>
        /// <param name="givenValue"></param>
        /// <param name="edmType"></param>
        /// <returns></returns>
        private static ReadOnlySpan<char> GenerateValueOperand(string givenValue, EdmType edmType)
        {
            switch(edmType)
            {
                case EdmType.Boolean:
                case EdmType.Int32:
                    return givenValue.AsSpan();
                case EdmType.Double:
                    bool isInteger = int.TryParse(givenValue, out _);
                    if (isInteger)
                    {
                        return $"{givenValue}.0".AsSpan();
                    }
                    return givenValue.AsSpan();
                case EdmType.Int64:
                    return $"{givenValue}L".AsSpan();
                case EdmType.DateTime:
                    return $"datetime'{givenValue}'".AsSpan();
                case EdmType.Guid:
                    return $"guid'{givenValue}'".AsSpan();
                case EdmType.Binary:
                    return $"X'{givenValue}'".AsSpan();
            }
            // OData readers expect single quote to be escaped in a param value.
            return string.Format(CultureInfo.InvariantCulture, "'{0}'", givenValue.Replace("'", "''")).AsSpan();
        }

        private static ReadOnlySpan<char> GenerateFilterCondition(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> operation, string givenValue, EdmType edmType)
        {
            ReadOnlySpan<char> valueOperand = GenerateValueOperand(givenValue, edmType);
            return $"{propertyName} {operation} {valueOperand}".AsSpan();
        }
#endif
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
#if NET9_0_OR_GREATER
            return GenerateFilterCondition(propertyName.AsSpan(), operation.AsSpan(), givenValue, edmType).ToString();
#else
            string valueOperand;
            if (edmType == EdmType.Boolean || edmType == EdmType.Int32)
            {
                valueOperand = givenValue;
            }
            else if (edmType == EdmType.Double)
            {
                bool isInteger = int.TryParse(givenValue, out _);
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
#endif
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

        /// <summary>
        /// Creates a filter condition using the specified logical operator on two filter conditions.
        /// </summary>
        /// <param name="filterA">A ReadOnlySpan containing the first formatted filter condition.</param>
        /// <param name="operatorString">A string containing the operator to use (AND, OR).</param>
        /// <param name="filterB">A ReadOnlySpan containing the second formatted filter condition.</param>
        /// <returns>A ReadOnlySpan containing the combined filter expression.</returns>
        public static ReadOnlySpan<char> CombineFilters(ReadOnlySpan<char> filterA, string operatorString, ReadOnlySpan<char> filterB)
        {
            return $"({filterA.ToString()}) {operatorString} ({filterB.ToString()})".AsSpan();
        }

    }
}
