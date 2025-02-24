
using System;
using System.Collections.Generic;
using System.Text;
using Azure.Data.Tables;

namespace ElCamino.Azure.Data.Tables
{
    /// <summary>
    /// This class allows for a fluent query builder for Azure Table Storage using OData.
    /// </summary>
    public class TableQueryBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TableQueryBuilder"/> class for fluent query building for Azure Table Storage using OData.
        /// </summary>
        public TableQueryBuilder() { }

        private const int BufferSize = 1024;
        private char[] _bufferQuery = new char[BufferSize];
        private int _currentQueryLength = 0;
        private uint _filterCount = 0;
        private uint _beginGroupCount = 0;
        private uint _endGroupCount = 0;

        /// <summary>
        /// Gets a value indicating whether the query has any filters.
        /// </summary>
        public bool HasFilter => _filterCount > 0;

        /// <summary>
        /// Gets the current query filter.
        /// </summary>
        public ReadOnlySpan<char> QueryFilter => _bufferQuery.AsSpan().Slice(0, _currentQueryLength);

        private void AllocateBuffer(int lengthToAdd)
        {
            if ((_currentQueryLength + lengthToAdd > _bufferQuery.Length))
            {
                Span<char> newBuffer = stackalloc char[_bufferQuery.Length + BufferSize];
                QueryFilter.CopyTo(newBuffer);
                _bufferQuery = [..newBuffer];
            }
        }

        private void AppendCondition(ReadOnlySpan<char> condition)
        {
            int segmentLength = condition.Length + 2;

            if (_currentQueryLength <= 0)
            {
                _bufferQuery[0] = '(';
                condition.CopyTo(_bufferQuery.AsSpan()[1..]);
                _bufferQuery[condition.Length + 1] = ')';
            }
            else
            {
                Span<char> temp = stackalloc char[_currentQueryLength + segmentLength];
                AllocateBuffer(segmentLength);
                QueryFilter.CopyTo(temp);
                temp[_currentQueryLength] = '(';
                condition.CopyTo(temp[(_currentQueryLength + 1)..]);
                temp[^1] = ')';
                temp.CopyTo(_bufferQuery);
            }
            _currentQueryLength += segmentLength;
            _filterCount++;
            _beginGroupCount++;
            _endGroupCount++;
        }

        /// <summary>
        /// Adds a filter condition to the query.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="operation"></param>
        /// <param name="givenValue"></param>
        /// <returns></returns>
        public TableQueryBuilder AddFilter(ReadOnlySpan<char> propertyName, QueryComparison operation, ReadOnlySpan<char> givenValue)
        {
            AppendCondition(TableQuery.GenerateFilterCondition(
                propertyName, QueryComparisons.GetComparison(operation), givenValue));
            return this;
        }

        /// <summary>
        /// Adds a filter condition to the query.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="operation"></param>
        /// <param name="givenValue"></param>
        /// <returns></returns>
        public TableQueryBuilder AddFilter(ReadOnlySpan<char> propertyName, QueryComparison operation, string? givenValue)
        {
            AppendCondition(
                givenValue is null ? 
                TableQuery.GenerateFilterConditionForStringNull(
                    propertyName, QueryComparisons.GetComparison(operation)) 
                :
                TableQuery.GenerateFilterCondition(
                propertyName, QueryComparisons.GetComparison(operation), givenValue));
            return this;
        }

        /// <summary>
        /// Adds a filter condition to the query.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="operation"></param>
        /// <param name="givenValue"></param>
        /// <returns></returns>
        public TableQueryBuilder AddFilterBool(ReadOnlySpan<char> propertyName, QueryComparison operation, bool? givenValue)
        {
            AppendCondition(
                !givenValue.HasValue ?
                TableQuery.GenerateFilterConditionForBoolNull(
                    propertyName, QueryComparisons.GetComparison(operation))
                :
                TableQuery.GenerateFilterConditionForBool(
                propertyName, QueryComparisons.GetComparison(operation), givenValue.Value));
            return this;
        }

        /// <summary>
        /// Adds a filter condition to the query.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="operation"></param>
        /// <param name="givenValue"></param>
        /// <returns></returns>
        public TableQueryBuilder AddFilterInt(ReadOnlySpan<char> propertyName, QueryComparison operation, int? givenValue)
        {
            AppendCondition(
                !givenValue.HasValue ?
                TableQuery.GenerateFilterConditionForIntNull(
                    propertyName, QueryComparisons.GetComparison(operation))
                :
                TableQuery.GenerateFilterConditionForInt(
                propertyName, QueryComparisons.GetComparison(operation), givenValue.Value));
            return this;
        }

        /// <summary>
        /// Adds a filter condition to the query.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="operation"></param>
        /// <param name="givenValue"></param>
        /// <returns></returns>
        public TableQueryBuilder AddFilterDouble(ReadOnlySpan<char> propertyName, QueryComparison operation, double? givenValue)
        {
            AppendCondition(
                !givenValue.HasValue ?
                TableQuery.GenerateFilterConditionForDoubleNull(
                    propertyName, QueryComparisons.GetComparison(operation))
                :
                TableQuery.GenerateFilterConditionForDouble(
                propertyName, QueryComparisons.GetComparison(operation), givenValue.Value));
            return this;
        }

        /// <summary>
        /// Adds a filter condition to the query.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="operation"></param>
        /// <param name="givenValue"></param>
        /// <returns></returns>
        public TableQueryBuilder AddFilterLong(ReadOnlySpan<char> propertyName, QueryComparison operation, long? givenValue)
        {
            AppendCondition(
                !givenValue.HasValue ?
                TableQuery.GenerateFilterConditionForLongNull(
                    propertyName, QueryComparisons.GetComparison(operation))
                :
                TableQuery.GenerateFilterConditionForLong(
                propertyName, QueryComparisons.GetComparison(operation), givenValue.Value));
            return this;
        }

        /// <summary>
        /// Adds a filter condition to the query.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="operation"></param>
        /// <param name="givenValue"></param>
        /// <returns></returns>
        public TableQueryBuilder AddFilterDate(ReadOnlySpan<char> propertyName, QueryComparison operation, DateTimeOffset givenValue)
        {
            AppendCondition(
                TableQuery.GenerateFilterConditionForDate(
                propertyName, QueryComparisons.GetComparison(operation), givenValue));
            return this;
        }

        /// <summary>
        /// Adds a filter condition to the query.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="operation"></param>
        /// <param name="givenValue"></param>
        /// <returns></returns>
        public TableQueryBuilder AddFilterGuid(ReadOnlySpan<char> propertyName, QueryComparison operation, Guid? givenValue)
        {
            AppendCondition(
                !givenValue.HasValue ?
                TableQuery.GenerateFilterConditionForGuidNull(
                    propertyName, QueryComparisons.GetComparison(operation))
                :
                TableQuery.GenerateFilterConditionForGuid(
                propertyName, QueryComparisons.GetComparison(operation), givenValue.Value));
            return this;
        }

        /// <summary>
        /// Adds a filter condition to the query.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="operation"></param>
        /// <param name="givenValue"></param>
        /// <returns></returns>
        public TableQueryBuilder AddFilterBytes(ReadOnlySpan<char> propertyName, QueryComparison operation, ReadOnlySpan<byte> givenValue)
        {
            AppendCondition(                
                TableQuery.GenerateFilterConditionForBinary(propertyName, QueryComparisons.GetComparison(operation), givenValue));
            return this;
        }

        /// <summary>
        /// Adds a filter condition to the query.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="operation"></param>
        /// <param name="givenValue"></param>
        /// <returns></returns>
        public TableQueryBuilder AddFilterBytes(ReadOnlySpan<char> propertyName, QueryComparison operation, byte[] givenValue)
        {
            ArgumentNullException.ThrowIfNull(givenValue, nameof(givenValue));
            AppendCondition(
                TableQuery.GenerateFilterConditionForBinary(propertyName, QueryComparisons.GetComparison(operation), givenValue));
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
            AllocateBuffer(1);
            Span<char> temp = stackalloc char[_currentQueryLength + 1];
            QueryFilter.CopyTo(temp);
            temp[^1] = '(';
            temp.CopyTo(_bufferQuery);
            _currentQueryLength++;
            _beginGroupCount++;
        }

        private void AppendEndGroup()
        {
            ThrowIfNoFilter();
            AllocateBuffer(1);
            Span<char> temp = stackalloc char[_currentQueryLength + 1];
            QueryFilter.CopyTo(temp);
            temp[^1] = ')';
            temp.CopyTo(_bufferQuery);
            _currentQueryLength++;
            _endGroupCount++;
        }

        private void PrependAppendGroupAll()
        {
            ThrowIfNoFilter();
            AllocateBuffer(2);
            Span<char> temp = stackalloc char[_currentQueryLength + 2];
            temp[0] = '(';
            QueryFilter.CopyTo(temp[1..]);
            _beginGroupCount++;
            temp[^1] = ')';
            _endGroupCount++;
            temp.CopyTo(_bufferQuery);
            _currentQueryLength += 2;
        }

        private void AppendTableOperator(TableOperator tableOperator)
        {
            ThrowIfNoFilter();
            ReadOnlySpan<char> tableOperatorSpan = $" {TableOperators.GetOperator(tableOperator)} ".AsSpan();
            int segmentLength = tableOperatorSpan.Length;
            AllocateBuffer(segmentLength);

            Span<char> temp = stackalloc char[_currentQueryLength + segmentLength];
            QueryFilter.CopyTo(temp);
            tableOperatorSpan.CopyTo(temp[_currentQueryLength..]);
            temp.CopyTo(_bufferQuery);
            _currentQueryLength += segmentLength;
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
            return QueryFilter.ToString();
        }
    }
}
