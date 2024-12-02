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
    public ref struct TableQueryBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TableQueryBuilder"/> class for fluent query building for Azure Table Storage using OData.
        /// </summary>
        public TableQueryBuilder() { }

        private Span<char> _currentQuery = Span<char>.Empty;
        private uint _filterCount = 0;
        private uint _beginGroupCount = 0;
        private uint _endGroupCount = 0;

        /// <summary>
        /// Gets a value indicating whether the query has any filters.
        /// </summary>
        public bool HasFilter => _filterCount > 0;

        private void AppendCondition(ReadOnlySpan<char> condition)
        {
            if(_currentQuery.IsEmpty)
            {
                _currentQuery = new Span<char>(['(', .. condition, ')']);
            }
            else
            {
                Span<char> temp = stackalloc char[_currentQuery.Length + condition.Length + 2];
                _currentQuery.CopyTo(temp);
                temp[_currentQuery.Length] = '(';
                condition.CopyTo(temp[(_currentQuery.Length + 1)..]);
                temp[^1] = ')';
                _currentQuery = new Span<char>(temp.ToArray());
            }
            _filterCount++;
            _beginGroupCount++;
            _endGroupCount++;
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
            Span<char> temp = stackalloc char[_currentQuery.Length + 1];
            _currentQuery.CopyTo(temp);
            temp[^1] = '(';
            _currentQuery = new Span<char>(temp.ToArray());
            _beginGroupCount++;
        }

        private void AppendEndGroup()
        {
            ThrowIfNoFilter();
            Span<char> temp = stackalloc char[_currentQuery.Length + 1];
            _currentQuery.CopyTo(temp);
            temp[^1] = ')';
            _currentQuery = new Span<char>(temp.ToArray());
            _endGroupCount++;
        }

        private void PrependAppendGroupAll()
        {
            ThrowIfNoFilter();
            Span<char> temp = stackalloc char[_currentQuery.Length + 2];
            temp[0] = '(';
            _currentQuery.CopyTo(temp[1..]);
            _beginGroupCount++;
            temp[^1] = ')';
            _endGroupCount++;
            _currentQuery = new Span<char>(temp.ToArray());
        }

        private void AppendTableOperator(TableOperator tableOperator)
        {
            ThrowIfNoFilter();
            ReadOnlySpan<char> tableOperatorSpan = $" {TableOperators.GetOperator(tableOperator)} ".AsSpan();
            Span<char> temp = stackalloc char[_currentQuery.Length + tableOperatorSpan.Length];
            _currentQuery.CopyTo(temp);
            tableOperatorSpan.CopyTo(temp[_currentQuery.Length..]);
            _currentQuery = new Span<char>(temp.ToArray());
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
            return _currentQuery.ToString();
        }
    }
}
#endif
