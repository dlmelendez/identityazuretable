#if NET9_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Text;
using Azure.Data.Tables;

namespace ElCamino.Azure.Data.Tables
{
    /// <summary>
    /// EXPERIMENTAL: API is subject to change
    /// This class allows for a fluent query builder for Azure Table Storage using OData.
    /// </summary>
    public class TableQueryBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TableQueryBuilder"/> class for fluent query building for Azure Table Storage using OData.
        /// </summary>
        public TableQueryBuilder() { }

        private StringBuilder _queryBuilder = new StringBuilder();
        private uint _filterCount = 0;
        private uint _beginGroupCount = 0;
        private uint _endGroupCount = 0;

        /// <summary>
        /// Gets a value indicating whether the query has any filters.
        /// </summary>
        public bool HasFilter => _filterCount > 0;

        private void AppendCondition(ReadOnlySpan<char> condition)
        {
            AppendBeginGroup();
            _queryBuilder.Append(condition);
            _filterCount++;
            AppendEndGroup();
        }

        /// <summary>
        /// Adds a filter condition to the query.
        /// </summary>
        /// <param name="queryCondition"></param>
        /// <returns></returns>
        public TableQueryBuilder AddFilter(QueryCondition<ReadOnlySpan<char>> queryCondition)
        {
            AppendCondition(TableQuery.GenerateFilterCondition(
                queryCondition.PropertyName, QueryComparisons.GetComparison(queryCondition.Operation), queryCondition.GivenValue));
            return this;
        }

        /// <summary>
        /// Adds a filter condition to the query.
        /// </summary>
        /// <param name="queryCondition"></param>
        /// <returns></returns>
        public TableQueryBuilder AddFilter(QueryCondition<string?> queryCondition)
        {
            AppendCondition(
                queryCondition.GivenValue is null ? 
                TableQuery.GenerateFilterConditionForStringNull(
                    queryCondition.PropertyName, QueryComparisons.GetComparison(queryCondition.Operation)) 
                :
                TableQuery.GenerateFilterCondition(
                queryCondition.PropertyName, QueryComparisons.GetComparison(queryCondition.Operation), queryCondition.GivenValue));
            return this;
        }

        /// <summary>
        /// Adds a filter condition to the query.
        /// </summary>
        /// <param name="queryCondition"></param>
        /// <returns></returns>
        public TableQueryBuilder AddFilter(QueryCondition<bool?> queryCondition)
        {
            AppendCondition(
                !queryCondition.GivenValue.HasValue ?
                TableQuery.GenerateFilterConditionForBoolNull(
                    queryCondition.PropertyName, QueryComparisons.GetComparison(queryCondition.Operation))
                :
                TableQuery.GenerateFilterConditionForBool(
                queryCondition.PropertyName, QueryComparisons.GetComparison(queryCondition.Operation), queryCondition.GivenValue.Value));
            return this;
        }

        /// <summary>
        /// Combines filters using the specified table operator.
        /// </summary>
        /// <param name="tableOperator"></param>
        /// <returns></returns>
        public TableQueryBuilder CombineFilters(TableOperator tableOperator)
        {
            AppendTableOperator(tableOperator);
            return this;
        }

        /// <summary>
        /// Begins a group of filters.
        /// </summary>
        /// <returns></returns>
        public TableQueryBuilder BeginGroup()
        {
            AppendBeginGroup();
            return this;
        }

        /// <summary>
        /// Ends a group of filters.
        /// </summary>
        /// <returns></returns>
        public TableQueryBuilder EndGroup()
        {     
            AppendEndGroup();
            return this;
        }

        /// <summary>
        /// Groups all filters.
        /// </summary>
        /// <returns></returns>
        public TableQueryBuilder GroupAll()
        {
            PrependAppendGroupAll();
            return this;
        }

        private void AppendBeginGroup()
        {
            _queryBuilder.Append('(');
            _beginGroupCount++;
        }

        private void AppendEndGroup()
        {
            ThrowIfNoFilter();
            _queryBuilder.Append(')');
            _endGroupCount++;
        }

        private void PrependAppendGroupAll()
        {
            ThrowIfNoFilter();
            _queryBuilder.Insert(0, '(');
            _beginGroupCount++;
            AppendEndGroup();
        }

        private void AppendTableOperator(TableOperator tableOperator)
        {
            ThrowIfNoFilter();
            _queryBuilder.Append($" {TableOperators.GetOperator(tableOperator)} ");
        }

        private void ThrowIfNoFilter()
        {
            if (!HasFilter)
            {
                throw new InvalidOperationException($"No filter has been added to the query. Add a filter condition using .{nameof(AddFilter)} method.");
            }
        }

        /// <summary>
        /// Emits a string for Odata Azure Table Storage query
        /// </summary>
        /// <returns>Returns an Odata Azure Table Storage query or an empty string</returns>
        public override string ToString()
        {
            return _queryBuilder.ToString();
        }
    }
}
#endif
