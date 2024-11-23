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

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Azure.Data.Tables
#pragma warning restore IDE0130 // Namespace does not match folder structure
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
        public static ReadOnlySpan<char> GenerateFilterCondition(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> operation, ReadOnlySpan<char> givenValue)
        {
            return GenerateFilterCondition(propertyName, operation, givenValue, EdmType.String);
        }

        /// <summary>
        /// Generates a property filter condition string for the boolean value.
        /// </summary>
        /// <param name="propertyName">A string containing the name of the property to compare.</param>
        /// <param name="operation">A string containing the comparison operator to use.</param>
        /// <param name="givenValue">A <c>bool</c> containing the value to compare with the property.</param>
        /// <returns>A string containing the formatted filter condition.</returns>
        public static ReadOnlySpan<char> GenerateFilterConditionForBool(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> operation, bool givenValue)
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
        public static ReadOnlySpan<char> GenerateFilterConditionForBoolNull(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> operation)
        {
            ReadOnlySpan<char> validBoolean = $"({GenerateFilterConditionForBool(propertyName, QueryComparisons.Equal, true)} {TableOperators.Or} {GenerateFilterConditionForBool(propertyName, QueryComparisons.Equal, false)})";
            switch (operation)
            {
                case QueryComparisons.Equal: //isNull
                    return $"not {validBoolean}";
                case QueryComparisons.NotEqual: //notNull
                    return validBoolean;
                default:
                    break;
            }

            throw new ArgumentOutOfRangeException(nameof(operation), $"{operation} is not supported. Only {QueryComparisons.Equal} and {QueryComparisons.NotEqual} operators are supported.");
        }

        /// <summary>
        /// Generates a property filter condition string for the binary value.
        /// </summary>
        /// <param name="propertyName">A string containing the name of the property to compare.</param>
        /// <param name="operation">A string containing the comparison operator to use.</param>
        /// <param name="givenValue">A byte array containing the value to compare with the property.</param>
        /// <returns>A string containing the formatted filter condition.</returns>
        public static ReadOnlySpan<char> GenerateFilterConditionForBinary(
            ReadOnlySpan<char> propertyName,
            ReadOnlySpan<char> operation,
            byte[] givenValue)
        {
            return GenerateFilterCondition(propertyName, operation, Convert.ToHexString(givenValue).ToLowerInvariant(), EdmType.Binary);
        }

        /// <summary>
        /// Generates a property filter condition string for the <see cref="DateTimeOffset"/> value.
        /// </summary>
        /// <param name="propertyName">A string containing the name of the property to compare.</param>
        /// <param name="operation">A string containing the comparison operator to use.</param>
        /// <param name="givenValue">A <see cref="DateTimeOffset"/> containing the value to compare with the property.</param>
        /// <returns>A string containing the formatted filter condition.</returns>
        public static ReadOnlySpan<char> GenerateFilterConditionForDate(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> operation, DateTimeOffset givenValue)
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
        public static ReadOnlySpan<char> GenerateFilterConditionForDouble(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> operation, double givenValue)
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
        public static ReadOnlySpan<char> GenerateFilterConditionForInt(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> operation, int givenValue)
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
        public static ReadOnlySpan<char> GenerateFilterConditionForLong(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> operation, long givenValue)
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
        public static ReadOnlySpan<char> GenerateFilterConditionForGuid(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> operation, Guid givenValue)
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
        public static ReadOnlySpan<char> GenerateFilterConditionForStringNull(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> operation)
        {
            ReadOnlySpan<char> validString = $"{propertyName} {QueryComparisons.GreaterThan} ''";
            switch (operation)
            {
                case QueryComparisons.Equal: //isNull
                    return $"not ({validString})";
                case QueryComparisons.NotEqual: //notNull
                    return validString;
                default:
                    break;
            }

            throw new ArgumentOutOfRangeException(nameof(operation), $"{operation} is not supported. Only {QueryComparisons.Equal} and {QueryComparisons.NotEqual} operators are supported.");
        }

        /// <summary>
        /// Generate operand value for the given value and <see cref="EdmType"/>.
        /// </summary>
        /// <param name="givenValue"></param>
        /// <param name="edmType"></param>
        /// <returns></returns>
        private static ReadOnlySpan<char> GenerateValueOperand(ReadOnlySpan<char> givenValue, EdmType edmType)
        {
            switch (edmType)
            {
                case EdmType.Boolean:
                case EdmType.Int32:
                    return givenValue;
                case EdmType.Double:
                    bool isInteger = int.TryParse(givenValue, out _);
                    if (isInteger)
                    {
                        return $"{givenValue}.0";
                    }
                    return givenValue;
                case EdmType.Int64:
                    return $"{givenValue}L";
                case EdmType.DateTime:
                    return $"datetime'{givenValue}'";
                case EdmType.Guid:
                    return $"guid'{givenValue}'";
                case EdmType.Binary:
                    return $"X'{givenValue}'";
            }
            // OData readers expect single quote to be escaped in a param value.
            int splitCounter = givenValue.Count('\'');
            if (splitCounter <= 0)
            {
                Span<char> chars = stackalloc char[givenValue.Length + 2];
                chars[0] = '\'';
                int index = 1;
                foreach (char c in givenValue)
                {
                    chars[index++] = c;
                }
                chars[chars.Length - 1] = '\'';
                return new ReadOnlySpan<char>([.. chars]);
            }

            Span<char> joinArray = stackalloc char[givenValue.Length + splitCounter + 2];
            joinArray[0] = '\'';
            int indexCounter = 1;
            foreach (var c in givenValue)
            {
                joinArray[indexCounter++] = c;
                if (c == '\'')
                {
                    joinArray[indexCounter++] = '\'';
                }
            }
            joinArray[joinArray.Length - 1] = '\'';
            return new ReadOnlySpan<char>([.. joinArray]);
        }

        /// <summary>
        /// Generates a property filter condition string for the <see cref="EdmType"/> value, formatted as the specified <see cref="EdmType"/>.
        /// </summary>
        /// <param name="propertyName">A string containing the name of the property to compare.</param>
        /// <param name="operation">A string containing the comparison operator to use.</param>
        /// <param name="givenValue">A string containing the value to compare with the property.</param>
        /// <param name="edmType">The <see cref="EdmType"/> to format the value as.</param>
        /// <returns>A string containing the formatted filter condition.</returns>
        private static ReadOnlySpan<char> GenerateFilterCondition(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> operation, ReadOnlySpan<char> givenValue, EdmType edmType)
        {
            ReadOnlySpan<char> valueOperand = GenerateValueOperand(givenValue, edmType);
            return $"{propertyName} {operation} {valueOperand}";
        }

        /// <summary>
        /// Creates a filter condition using the specified logical operator on two filter conditions.
        /// </summary>
        /// <param name="filterA">A ReadOnlySpan containing the first formatted filter condition.</param>
        /// <param name="operatorString">A string containing the operator to use (AND, OR).</param>
        /// <param name="filterB">A ReadOnlySpan containing the second formatted filter condition.</param>
        /// <returns>A ReadOnlySpan containing the combined filter expression.</returns>
        public static ReadOnlySpan<char> CombineFilters(ReadOnlySpan<char> filterA, ReadOnlySpan<char> operatorString, ReadOnlySpan<char> filterB)
        {
            return $"({filterA}) {operatorString} ({filterB})";
        }
    }
}
