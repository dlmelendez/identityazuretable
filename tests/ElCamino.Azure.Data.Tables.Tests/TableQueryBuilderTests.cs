// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Xunit;
using Xunit.Abstractions;

namespace ElCamino.Azure.Data.Tables.Tests
{
    public class TableQueryBuilderTests : BaseTest
    {

        public TableQueryBuilderTests(TableFixture tableFixture, ITestOutputHelper output) :
            base(tableFixture, output)
        { }

        private async Task SetupTableAsync()
        {
            //Setup Create table
            await _tableClient.CreateIfNotExistsAsync();
            _output.WriteLine("Table created {0}", TableName);

        }

        [Fact]
        public async Task QueryBuilderPropertyString()
        {
            string propertyName = "newProperty1";

            //Create Table
            await SetupTableAsync();
            //Setup Entity
            var key = "b-" + Guid.NewGuid().ToString("N");
            _output.WriteLine("PartitionKey {0}", key);
            _output.WriteLine("RowKey {0}", key);
            var entity = new TableEntity(key, key);
            Assert.Equal(default, entity.ETag);
            Assert.Equal(default, entity.Timestamp);

            //Execute Add
            var addedEntity = await _tableClient.AddEntityWithHeaderValuesAsync(entity);
            Stopwatch sw = new Stopwatch();

            sw.Start();
            TableQueryBuilder queryBuilderNull = new TableQueryBuilder();
            string filterNullBuilder = queryBuilderNull
                .AddFilter(nameof(TableEntity.PartitionKey), QueryComparison.Equal, addedEntity.PartitionKey)
                .CombineFilters(TableOperator.And)
                .AddFilter(nameof(TableEntity.RowKey), QueryComparison.Equal, addedEntity.RowKey)
                .CombineFilters(TableOperator.And)
                .AddFilter(propertyName, QueryComparison.Equal, null)
                .ToString();
            sw.Stop();
            _output.WriteLine($"{nameof(filterNullBuilder)}: {sw.Elapsed.TotalMilliseconds}ms");
            _output.WriteLine($"{nameof(filterNullBuilder)}:{filterNullBuilder}");


            //Assert
            Assert.Equal(1, await _tableClient.QueryAsync<TableEntity>(filter: filterNullBuilder).CountAsync());

        }

        [Fact]
        public void QueryBuilderNullMemory()
        {
            string propertyName = "newProperty1";

            //Setup Entity
            var key = "b-" + Guid.NewGuid().ToString("N");
            _output.WriteLine("PartitionKey {0}", key);
            _output.WriteLine("RowKey {0}", key);
            var addedEntity = new TableEntity(key, key);
            Assert.Equal(default, addedEntity.ETag);
            Assert.Equal(default, addedEntity.Timestamp);

            Stopwatch sw = new Stopwatch();
            long mem = GC.GetTotalAllocatedBytes();

            sw.Start();
            //Execute isNull Query
            string filterByPartitionKey = TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, addedEntity.PartitionKey).ToString();
            string filterByRowKey = TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, addedEntity.PartitionKey).ToString();
            string filterByNullProperty = TableQuery.GenerateFilterConditionForStringNull(propertyName, QueryComparisons.Equal).ToString();

            string filterNull = TableQuery.CombineFilters(
                                TableQuery.CombineFilters(filterByPartitionKey, TableOperators.And, filterByRowKey),
                                TableOperators.And,
                                filterByNullProperty).ToString();
            for (int trial = 0; trial < 150; trial++)
            {
                filterNull = TableQuery.CombineFilters(filterNull, TableOperators.And, filterByNullProperty).ToString();
            }
            sw.Stop();
            mem = GC.GetTotalAllocatedBytes() - mem;
            _output.WriteLine($"{nameof(filterNull)}: {sw.Elapsed.TotalMilliseconds}ms, Alloc: {mem / 1024.0 / 1024:N2}mb");
            _output.WriteLine($"{nameof(filterNull)}:{filterNull}");

            mem = GC.GetTotalAllocatedBytes();
            sw.Restart();
            TableQueryBuilder queryBuilderNull = new TableQueryBuilder();

            queryBuilderNull = queryBuilderNull
            .BeginGroup()
            .AddFilter(nameof(TableEntity.PartitionKey), QueryComparison.Equal, addedEntity.PartitionKey)
            .CombineFilters(TableOperator.And)
            .AddFilter(nameof(TableEntity.RowKey), QueryComparison.Equal, addedEntity.RowKey)
            .EndGroup()
            .CombineFilters(TableOperator.And)
            .AddFilter(propertyName, QueryComparison.Equal, null);
            for (int trial = 0; trial < 150; trial++)
            {
                queryBuilderNull = queryBuilderNull
                .GroupAll()
                .CombineFilters(TableOperator.And)
                .AddFilter(propertyName, QueryComparison.Equal, null);
            }
            string filterNullBuilder = queryBuilderNull.ToString();
            sw.Stop();
            mem = GC.GetTotalAllocatedBytes() - mem;

            _output.WriteLine($"{nameof(filterNullBuilder)}: {sw.Elapsed.TotalMilliseconds}ms, Alloc: {mem / 1024.0 / 1024:N2}mb");
            _output.WriteLine($"{nameof(filterNullBuilder)}:{filterNullBuilder}");
            _output.WriteLine($"{nameof(filterNullBuilder)}:{filterNullBuilder.Length}");

            //Assert
            Assert.Equal(filterNull, filterNullBuilder);

        }

        /// <summary>
        /// Conditions can be caller supplied text of any length: a condition larger than the builder's buffer or the stack must be appended, not throw or overflow the stack.
        /// The smaller conditions straddle the 1022 chars that fit, with their parentheses, in the builder's initial 1024 char buffer.
        /// </summary>
        [Theory]
        [InlineData(1022)]
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(4096)]
        [InlineData(2 * 1024 * 1024)]
        public void QueryBuilderLargeCondition(int conditionLength)
        {
            string condition = new string('a', conditionLength);

            string firstFilter = new TableQueryBuilder()
                .AddFilter(condition)
                .ToString();
            string laterFilter = new TableQueryBuilder()
                .AddFilter(nameof(TableEntity.PartitionKey), QueryComparison.Equal, "a")
                .CombineFilters(TableOperator.And)
                .AddFilter(condition)
                .ToString();

            Assert.Equal($"({condition})", firstFilter);
            Assert.Equal($"(PartitionKey eq 'a') and ({condition})", laterFilter);
        }

        /// <summary>
        /// The number of filters is up to the caller: a query larger than the stack must keep taking filters, operators and groups, not overflow the stack.
        /// </summary>
        [Fact]
        public void QueryBuilderLargeQuery()
        {
            const int filterCount = 50_000;
            TableQueryBuilder queryBuilder = new TableQueryBuilder();
            StringBuilder expectedFilters = new StringBuilder();

            for (int filterIndex = 0; filterIndex < filterCount; filterIndex++)
            {
                string rowKey = filterIndex.ToString("D8");
                if (filterIndex > 0)
                {
                    queryBuilder.CombineFilters(TableOperator.Or);
                    expectedFilters.Append(" or ");
                }
                queryBuilder.AddFilter(nameof(TableEntity.RowKey), QueryComparison.Equal, rowKey);
                expectedFilters.Append("(RowKey eq '").Append(rowKey).Append("')");
            }

            string filter = queryBuilder
                .GroupAll()
                .CombineFilters(TableOperator.And)
                .BeginGroup()
                .AddFilter(nameof(TableEntity.PartitionKey), QueryComparison.Equal, "a")
                .EndGroup()
                .ToString();

            Assert.Equal($"({expectedFilters}) and ((PartitionKey eq 'a'))", filter);
        }

        /// <summary>
        /// Each kind of append has to grow the buffer when it is the one to pass the end of it.
        /// A first condition of 1022 chars fills the builder's initial 1024 char buffer, the shorter ones leave room for 1 and 2 more chars.
        /// </summary>
        [Theory]
        [InlineData(1020)]
        [InlineData(1021)]
        [InlineData(1022)]
        public void QueryBuilderAppendAtBufferEnd(int conditionLength)
        {
            string condition = new string('a', conditionLength);

            Assert.Equal($"(({condition}))", new TableQueryBuilder().AddFilter(condition).GroupAll().ToString());
            Assert.Equal($"({condition})(", new TableQueryBuilder().AddFilter(condition).BeginGroup().ToString());
            Assert.Equal($"({condition}))", new TableQueryBuilder().AddFilter(condition).EndGroup().ToString());
            Assert.Equal($"({condition}) and ", new TableQueryBuilder().AddFilter(condition).CombineFilters(TableOperator.And).ToString());
            Assert.Equal($"({condition}) or ({condition})", new TableQueryBuilder().AddFilter(condition).CombineFilters(TableOperator.Or, condition).ToString());
        }

        /// <summary>
        /// A condition can be a span over the builder's own buffer, such as its <see cref="TableQueryBuilder.QueryFilter"/>.
        /// With 600 chars the append grows the buffer that the condition is read from, with 10 chars it does not.
        /// </summary>
        [Theory]
        [InlineData(10)]
        [InlineData(600)]
        public void QueryBuilderAppendOwnQueryFilter(int conditionLength)
        {
            string condition = new string('a', conditionLength);
            TableQueryBuilder queryBuilder = new TableQueryBuilder().AddFilter(condition);

            queryBuilder.CombineFilters(TableOperator.Or, queryBuilder.QueryFilter);

            Assert.Equal($"({condition}) or (({condition}))", queryBuilder.ToString());
        }

        [Fact]
        public async Task QueryBuilderNullPropertyString()
        {
            string propertyName = "newPropertyString";

            //Create Table
            await SetupTableAsync();
            //Setup Entity
            var key = "b-" + Guid.NewGuid().ToString("N");
            _output.WriteLine("PartitionKey {0}", key);
            _output.WriteLine("RowKey {0}", key);
            var entity = new TableEntity(key, key);
            Assert.Equal(default, entity.ETag);
            Assert.Equal(default, entity.Timestamp);

            //Execute Add
            var addedEntity = await _tableClient.AddEntityWithHeaderValuesAsync(entity);
            Stopwatch sw = new Stopwatch();

            sw.Start();
            //Execute isNull Query
            string filterByPartitionKey = TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, addedEntity.PartitionKey).ToString();
            string filterByRowKey = TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, addedEntity.PartitionKey).ToString();
            string filterByNullProperty = TableQuery.GenerateFilterConditionForStringNull(propertyName, QueryComparisons.Equal).ToString();
            string filterByNotNullProperty = TableQuery.GenerateFilterConditionForStringNull(propertyName, QueryComparisons.NotEqual).ToString();

            string filterNull = TableQuery.CombineFilters(
                                TableQuery.CombineFilters(filterByPartitionKey, TableOperators.And, filterByRowKey),
                                TableOperators.And,
                                filterByNullProperty).ToString();
            sw.Stop();
            _output.WriteLine($"{nameof(filterNull)}: {sw.Elapsed.TotalMilliseconds}ms");

            sw.Restart();
            TableQueryBuilder queryBuilderNull = new TableQueryBuilder();
            string filterNullBuilder = queryBuilderNull
                .BeginGroup()
                .AddFilter(nameof(TableEntity.PartitionKey), QueryComparison.Equal, addedEntity.PartitionKey)
                .CombineFilters(TableOperator.And)
                .AddFilter(nameof(TableEntity.RowKey), QueryComparison.Equal, addedEntity.RowKey)
                .EndGroup()
                .CombineFilters(TableOperator.And)
                .AddFilter(propertyName, QueryComparison.Equal, null)
                .ToString();
            sw.Stop();
            _output.WriteLine($"{nameof(filterNullBuilder)}: {sw.Elapsed.TotalMilliseconds}ms");
            _output.WriteLine($"{nameof(filterNullBuilder)}:{filterNullBuilder}");

            string filterNotNull = TableQuery.CombineFilters(
                    TableQuery.CombineFilters(filterByPartitionKey, TableOperators.And, filterByRowKey),
                    TableOperators.And,
                    filterByNotNullProperty).ToString();

            _output.WriteLine($"{nameof(filterNull)}:{filterNull}");
            _output.WriteLine($"{nameof(filterNotNull)}:{filterNotNull}");

            //Assert
            Assert.Equal(1, await _tableClient.QueryAsync<TableEntity>(filter: filterNullBuilder).CountAsync());
            Assert.Equal(1, await _tableClient.QueryAsync<TableEntity>(filter: filterNull).CountAsync());
            Assert.Equal(0, await _tableClient.QueryAsync<TableEntity>(filter: filterNotNull).CountAsync());

            //Modify update
            var updateEntity = new TableEntity(key, key)
            {
                { propertyName, propertyName }
            };

            await Task.Delay(1000); //wait 1 second for timestamp 

            _ = await _tableClient.UpdateEntityWithHeaderValuesAsync(updateEntity, addedEntity.ETag);

            //Assert
            Assert.Equal(0, await _tableClient.QueryAsync<TableEntity>(filter: filterNull).CountAsync());
            Assert.Equal(1, await _tableClient.QueryAsync<TableEntity>(filter: filterNotNull.ToString()).CountAsync());


        }

        [Fact]
        public async Task QueryBuilderNullPropertyInt()
        {
            string propertyName = "newPropertyInt";

            //Create Table
            await SetupTableAsync();
            //Setup Entity
            var key = "b-" + Guid.NewGuid().ToString("N");
            _output.WriteLine("PartitionKey {0}", key);
            _output.WriteLine("RowKey {0}", key);
            var entity = new TableEntity(key, key);
            Assert.Equal(default, entity.ETag);
            Assert.Equal(default, entity.Timestamp);

            //Execute Add
            var addedEntity = await _tableClient.AddEntityWithHeaderValuesAsync(entity);
            Stopwatch sw = new Stopwatch();

            sw.Start();
            TableQueryBuilder queryBuilderNull = new TableQueryBuilder();
            string filterNullBuilder = queryBuilderNull
                .BeginGroup()
                .AddFilter(nameof(TableEntity.PartitionKey), QueryComparison.Equal, addedEntity.PartitionKey)
                .CombineFilters(TableOperator.And)
                .AddFilter(nameof(TableEntity.RowKey), QueryComparison.Equal, addedEntity.RowKey)
                .EndGroup()
                .CombineFilters(TableOperator.And)
                .AddFilterInt(propertyName, QueryComparison.Equal, null)
                .ToString();
            sw.Stop();
            _output.WriteLine($"{nameof(filterNullBuilder)}: {sw.Elapsed.TotalMilliseconds}ms");
            _output.WriteLine($"{nameof(filterNullBuilder)}:{filterNullBuilder}");

            sw.Restart();
            TableQueryBuilder queryBuilderNotNull = new TableQueryBuilder();
            string filterNotNullBuilder = queryBuilderNotNull
                .BeginGroup()
                .AddFilter(nameof(TableEntity.PartitionKey), QueryComparison.Equal, addedEntity.PartitionKey)
                .CombineFilters(TableOperator.And)
                .AddFilter(nameof(TableEntity.RowKey), QueryComparison.Equal, addedEntity.RowKey)
                .EndGroup()
                .CombineFilters(TableOperator.And)
                .AddFilterInt(propertyName, QueryComparison.NotEqual, null)
                .ToString();
            sw.Stop();
            _output.WriteLine($"{nameof(filterNotNullBuilder)}: {sw.Elapsed.TotalMilliseconds}ms");
            _output.WriteLine($"{nameof(filterNotNullBuilder)}:{filterNotNullBuilder}");

            //Assert
            Assert.Equal(1, await _tableClient.QueryAsync<TableEntity>(filter: filterNullBuilder).CountAsync());
            Assert.Equal(0, await _tableClient.QueryAsync<TableEntity>(filter: filterNotNullBuilder).CountAsync());

            //Modify update
            var updateEntity = new TableEntity(key, key)
            {
                { propertyName, -10 }
            };

            await Task.Delay(1000); //wait 1 second for timestamp 

            _ = await _tableClient.UpdateEntityWithHeaderValuesAsync(updateEntity, addedEntity.ETag);

            //Assert
            Assert.Equal(0, await _tableClient.QueryAsync<TableEntity>(filter: filterNullBuilder).CountAsync());
            Assert.Equal(1, await _tableClient.QueryAsync<TableEntity>(filter: filterNotNullBuilder).CountAsync());
        }

        [Fact]
        public async Task QueryBuilderNullPropertyLong()
        {
            string propertyName = "newPropertyLong";

            //Create Table
            await SetupTableAsync();
            //Setup Entity
            var key = "b-" + Guid.NewGuid().ToString("N");
            _output.WriteLine("PartitionKey {0}", key);
            _output.WriteLine("RowKey {0}", key);
            var entity = new TableEntity(key, key);
            Assert.Equal(default, entity.ETag);
            Assert.Equal(default, entity.Timestamp);

            //Execute Add
            var addedEntity = await _tableClient.AddEntityWithHeaderValuesAsync(entity);
            Stopwatch sw = new Stopwatch();

            sw.Start();
            TableQueryBuilder queryBuilderNull = new TableQueryBuilder();
            string filterNullBuilder = queryBuilderNull
                .BeginGroup()
                .AddFilter(nameof(TableEntity.PartitionKey), QueryComparison.Equal, addedEntity.PartitionKey)
                .CombineFilters(TableOperator.And)
                .AddFilter(nameof(TableEntity.RowKey), QueryComparison.Equal, addedEntity.RowKey)
                .EndGroup()
                .CombineFilters(TableOperator.And)
                .AddFilterLong(propertyName, QueryComparison.Equal, null)
                .ToString();
            sw.Stop();
            _output.WriteLine($"{nameof(filterNullBuilder)}: {sw.Elapsed.TotalMilliseconds}ms");
            _output.WriteLine($"{nameof(filterNullBuilder)}:{filterNullBuilder}");

            sw.Restart();
            TableQueryBuilder queryBuilderNotNull = new TableQueryBuilder();
            string filterNotNullBuilder = queryBuilderNotNull
                .BeginGroup()
                .AddFilter(nameof(TableEntity.PartitionKey), QueryComparison.Equal, addedEntity.PartitionKey)
                .CombineFilters(TableOperator.And)
                .AddFilter(nameof(TableEntity.RowKey), QueryComparison.Equal, addedEntity.RowKey)
                .EndGroup()
                .CombineFilters(TableOperator.And)
                .AddFilterLong(propertyName, QueryComparison.NotEqual, null)
                .ToString();
            sw.Stop();
            _output.WriteLine($"{nameof(filterNotNullBuilder)}: {sw.Elapsed.TotalMilliseconds}ms");
            _output.WriteLine($"{nameof(filterNotNullBuilder)}:{filterNotNullBuilder}");

            //Assert
            Assert.Equal(1, await _tableClient.QueryAsync<TableEntity>(filter: filterNullBuilder).CountAsync());
            Assert.Equal(0, await _tableClient.QueryAsync<TableEntity>(filter: filterNotNullBuilder).CountAsync());

            //Modify update
#pragma warning disable IDE0028 // Simplify collection initialization
            var updateEntity = new TableEntity(key, key);
#pragma warning restore IDE0028 // Simplify collection initialization
            updateEntity.Add(propertyName, 90000000L);
            await Task.Delay(1000); //wait 1 second for timestamp 

            _ = await _tableClient.UpdateEntityWithHeaderValuesAsync(updateEntity, addedEntity.ETag);

            //Assert
            Assert.Equal(0, await _tableClient.QueryAsync<TableEntity>(filter: filterNullBuilder).CountAsync());
            Assert.Equal(1, await _tableClient.QueryAsync<TableEntity>(filter: filterNotNullBuilder).CountAsync());
        }

        [Fact]
        public async Task QueryBuilderNullPropertyGuid()
        {
            string propertyName = "newPropertyGuid";

            //Create Table
            await SetupTableAsync();
            //Setup Entity
            var key = "b-" + Guid.NewGuid().ToString("N");
            _output.WriteLine("PartitionKey {0}", key);
            _output.WriteLine("RowKey {0}", key);
            var entity = new TableEntity(key, key);
            Assert.Equal(default, entity.ETag);
            Assert.Equal(default, entity.Timestamp);

            //Execute Add
            var addedEntity = await _tableClient.AddEntityWithHeaderValuesAsync(entity);
            Stopwatch sw = new Stopwatch();

            sw.Start();
            TableQueryBuilder queryBuilderNull = new TableQueryBuilder();
            string filterNullBuilder = queryBuilderNull
                .BeginGroup()
                .AddFilter(nameof(TableEntity.PartitionKey), QueryComparison.Equal, addedEntity.PartitionKey)
                .CombineFilters(TableOperator.And)
                .AddFilter(nameof(TableEntity.RowKey), QueryComparison.Equal, addedEntity.RowKey)
                .EndGroup()
                .CombineFilters(TableOperator.And)
                .AddFilterGuid(propertyName, QueryComparison.Equal, null)
                .ToString();
            sw.Stop();
            _output.WriteLine($"{nameof(filterNullBuilder)}: {sw.Elapsed.TotalMilliseconds}ms");
            _output.WriteLine($"{nameof(filterNullBuilder)}:{filterNullBuilder}");

            sw.Restart();
            TableQueryBuilder queryBuilderNotNull = new TableQueryBuilder();
            string filterNotNullBuilder = queryBuilderNotNull
                .BeginGroup()
                .AddFilter(nameof(TableEntity.PartitionKey), QueryComparison.Equal, addedEntity.PartitionKey)
                .CombineFilters(TableOperator.And)
                .AddFilter(nameof(TableEntity.RowKey), QueryComparison.Equal, addedEntity.RowKey)
                .EndGroup()
                .CombineFilters(TableOperator.And)
                .AddFilterGuid(propertyName, QueryComparison.NotEqual, null)
                .ToString();
            sw.Stop();
            _output.WriteLine($"{nameof(filterNotNullBuilder)}: {sw.Elapsed.TotalMilliseconds}ms");
            _output.WriteLine($"{nameof(filterNotNullBuilder)}:{filterNotNullBuilder}");

            //Assert
            Assert.Equal(1, await _tableClient.QueryAsync<TableEntity>(filter: filterNullBuilder).CountAsync());
            Assert.Equal(0, await _tableClient.QueryAsync<TableEntity>(filter: filterNotNullBuilder).CountAsync());

            //Modify update
            var updateEntity = new TableEntity(key, key)
            {
                { propertyName, Guid.NewGuid() }
            };

            await Task.Delay(1000); //wait 1 second for timestamp 

            _ = await _tableClient.UpdateEntityWithHeaderValuesAsync(updateEntity, addedEntity.ETag);

            //Assert
            Assert.Equal(0, await _tableClient.QueryAsync<TableEntity>(filter: filterNullBuilder).CountAsync());
            Assert.Equal(1, await _tableClient.QueryAsync<TableEntity>(filter: filterNotNullBuilder).CountAsync());
        }

        [Fact]
        public async Task QueryBuilderNullPropertyBool()
        {
            string propertyName = "newPropertyBool";

            //Create Table
            await SetupTableAsync();
            //Setup Entity
            var key = "b-" + Guid.NewGuid().ToString("N");
            _output.WriteLine("PartitionKey {0}", key);
            _output.WriteLine("RowKey {0}", key);
            var entity = new TableEntity(key, key);
            Assert.Equal(default, entity.ETag);
            Assert.Equal(default, entity.Timestamp);

            //Execute Add
            var addedEntity = await _tableClient.AddEntityWithHeaderValuesAsync(entity);
            Stopwatch sw = new Stopwatch();

            sw.Start();
            TableQueryBuilder queryBuilderNull = new TableQueryBuilder();
            queryBuilderNull
                .BeginGroup()
                .AddFilter(nameof(TableEntity.PartitionKey), QueryComparison.Equal, addedEntity.PartitionKey)
                .CombineFilters(TableOperator.And)
                .AddFilter(nameof(TableEntity.RowKey), QueryComparison.Equal, addedEntity.RowKey)
                .EndGroup().ToString();
            queryBuilderNull.CombineFilters(TableOperator.And)
                .AddFilterBool(propertyName, QueryComparison.Equal, null);
            string filterNullBuilder = queryBuilderNull.ToString();
            sw.Stop();
            _output.WriteLine($"{nameof(filterNullBuilder)}: {sw.Elapsed.TotalMilliseconds}ms");
            _output.WriteLine($"{nameof(filterNullBuilder)}:{filterNullBuilder}");

            sw.Restart();
            TableQueryBuilder queryBuilderNotNull = new TableQueryBuilder();
            string filterNotNullBuilder = queryBuilderNotNull
                .BeginGroup()
                .AddFilter(nameof(TableEntity.PartitionKey), QueryComparison.Equal, addedEntity.PartitionKey)
                .CombineFilters(TableOperator.And)
                .AddFilter(nameof(TableEntity.RowKey), QueryComparison.Equal, addedEntity.RowKey)
                .EndGroup()
                .CombineFilters(TableOperator.And)
                .AddFilterBool(propertyName, QueryComparison.NotEqual, null)
                .ToString();
            sw.Stop();
            _output.WriteLine($"{nameof(filterNotNullBuilder)}: {sw.Elapsed.TotalMilliseconds}ms");
            _output.WriteLine($"{nameof(filterNotNullBuilder)}:{filterNotNullBuilder}");

            //Assert
            Assert.Equal(1, await _tableClient.QueryAsync<TableEntity>(filter: filterNullBuilder).CountAsync());
            Assert.Equal(0, await _tableClient.QueryAsync<TableEntity>(filter: filterNotNullBuilder).CountAsync());

            //Modify update
            var updateEntity = new TableEntity(key, key)
            {
                { propertyName, true }
            };

            await Task.Delay(1000); //wait 1 second for timestamp 

            _ = await _tableClient.UpdateEntityWithHeaderValuesAsync(updateEntity, addedEntity.ETag);

            //Assert
            Assert.Equal(0, await _tableClient.QueryAsync<TableEntity>(filter: filterNullBuilder).CountAsync());
            Assert.Equal(1, await _tableClient.QueryAsync<TableEntity>(filter: filterNotNullBuilder).CountAsync());
        }

        [Fact]
        public async Task QueryBuilderPropertyDate()
        {
            string propertyName = "newPropertyInt";

            DateTimeOffset targetDate = DateTimeOffset.UtcNow;
            //Create Table
            await SetupTableAsync();
            //Setup Entity
            var key = "b-" + Guid.NewGuid().ToString("N");
            _output.WriteLine("PartitionKey {0}", key);
            _output.WriteLine("RowKey {0}", key);
            var entity = new TableEntity(key, key);
            Assert.Equal(default, entity.ETag);
            Assert.Equal(default, entity.Timestamp);

            //Execute Add
            var addedEntity = await _tableClient.AddEntityWithHeaderValuesAsync(entity);
            Stopwatch sw = new Stopwatch();

            sw.Start();
            TableQueryBuilder queryBuilderNull = new TableQueryBuilder();
            string filterNullBuilder = queryBuilderNull
                .BeginGroup()
                .AddFilter(nameof(TableEntity.PartitionKey), QueryComparison.Equal, addedEntity.PartitionKey)
                .CombineFilters(TableOperator.And)
                .AddFilter(nameof(TableEntity.RowKey), QueryComparison.Equal, addedEntity.RowKey)
                .EndGroup()
                .CombineFilters(TableOperator.And)
                .AddFilterDate(propertyName, QueryComparison.Equal, targetDate)
                .ToString();
            sw.Stop();
            _output.WriteLine($"{nameof(filterNullBuilder)}: {sw.Elapsed.TotalMilliseconds}ms");
            _output.WriteLine($"{nameof(filterNullBuilder)}:{filterNullBuilder}");

            sw.Restart();
            TableQueryBuilder queryBuilderNotNull = new TableQueryBuilder();
            string filterNotNullBuilder = queryBuilderNotNull
                .BeginGroup()
                .AddFilter(nameof(TableEntity.PartitionKey), QueryComparison.Equal, addedEntity.PartitionKey)
                .CombineFilters(TableOperator.And)
                .AddFilter(nameof(TableEntity.RowKey), QueryComparison.Equal, addedEntity.RowKey)
                .EndGroup()
                .CombineFilters(TableOperator.And)
                .AddFilterDate(propertyName, QueryComparison.NotEqual, targetDate)
                .ToString();
            sw.Stop();
            _output.WriteLine($"{nameof(filterNotNullBuilder)}: {sw.Elapsed.TotalMilliseconds}ms");
            _output.WriteLine($"{nameof(filterNotNullBuilder)}:{filterNotNullBuilder}");

            //Assert
            Assert.Equal(0, await _tableClient.QueryAsync<TableEntity>(filter: filterNullBuilder).CountAsync());
            //This is a bug in the emulator, this should pass on real storage account
            Assert.Equal(0, await _tableClient.QueryAsync<TableEntity>(filter: filterNotNullBuilder).CountAsync());

            //Modify update
            var updateEntity = new TableEntity(key, key)
            {
                { propertyName, targetDate }
            };

            await Task.Delay(1000); //wait 1 second for timestamp 

            _ = await _tableClient.UpdateEntityWithHeaderValuesAsync(updateEntity, addedEntity.ETag);

            //Assert
            Assert.Equal(1, await _tableClient.QueryAsync<TableEntity>(filter: filterNullBuilder).CountAsync());
            Assert.Equal(0, await _tableClient.QueryAsync<TableEntity>(filter: filterNotNullBuilder).CountAsync());
        }

    }
}
