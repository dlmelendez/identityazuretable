#if NET9_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Text;

namespace ElCamino.Azure.Data.Tables
{
    /// <summary>
    /// EXPERIMENTAL: Used for TableQueryBuilder
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public ref struct QueryCondition<T>
        where T : allows ref struct
    {
        /// <summary>
        /// Query condition for adding filter conditon to the TableQueryBuilder
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="operation"></param>
        /// <param name="givenValue"></param>
        public QueryCondition(ReadOnlySpan<char> propertyName, QueryComparison operation, T givenValue)
        {
            PropertyName = propertyName;
            Operation = operation;
            GivenValue = givenValue;
        }

        /// <summary>
        /// Property name to query
        /// </summary>
        public ReadOnlySpan<char> PropertyName { get; }

        /// <summary>
        /// Query operation <see cref="QueryComparison"/> 
        /// </summary>
        public QueryComparison Operation { get; }

        /// <summary>
        /// Value to compare
        /// </summary>
        public T GivenValue { get; }
    }
}
#endif
