﻿// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Xunit;
using Xunit.Abstractions;

namespace ElCamino.Azure.Data.Tables.Tests
{
    public class TableQueryTests : BaseTest
    {

        public TableQueryTests(TableFixture tableFixture, ITestOutputHelper output) :
            base(tableFixture, output)
        { }

        private async Task SetupTableAsync()
        {
            //Setup Create table
            await _tableClient.CreateIfNotExistsAsync();
            _output.WriteLine("Table created {0}", TableName);

        }        

        [Fact]
        public async Task QueryNullPropertyString()
        {
            string propertyName = "newProperty";

            //Create Table
            await SetupTableAsync();
            //Setup Entity
            var key = "a-" + Guid.NewGuid().ToString("N");
            _output.WriteLine("PartitionKey {0}", key);
            _output.WriteLine("RowKey {0}", key);
            var entity = new TableEntity(key, key);
            Assert.Equal(default, entity.ETag);
            Assert.Equal(default, entity.Timestamp);

            //Execute Add
            var addedEntity = await _tableClient.AddEntityWithHeaderValuesAsync(entity);

            //Execute isNull Query
            string filterByPartitionKey = TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, addedEntity.PartitionKey);
            string filterByRowKey = TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, addedEntity.PartitionKey);
            string filterByNullProperty = TableQuery.GenerateFilterConditionForStringNull(propertyName, QueryComparisons.Equal);
            string filterByNotNullProperty = TableQuery.GenerateFilterConditionForStringNull(propertyName, QueryComparisons.NotEqual);

            string filterNull = TableQuery.CombineFilters(
                                TableQuery.CombineFilters(filterByPartitionKey, TableOperators.And, filterByRowKey),
                                TableOperators.And,
                                filterByNullProperty);
            string filterNotNull = TableQuery.CombineFilters(
                    TableQuery.CombineFilters(filterByPartitionKey, TableOperators.And, filterByRowKey),
                    TableOperators.And,
                    filterByNotNullProperty);

            _output.WriteLine($"{nameof(filterNull)}:{filterNull}");
            _output.WriteLine($"{nameof(filterNotNull)}:{filterNotNull}");

            //Assert
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
            Assert.Equal(1, await _tableClient.QueryAsync<TableEntity>(filter: filterNotNull).CountAsync());


        }

        [Fact]
        public async Task QueryNullPropertyBool()
        {
            string propertyName = "newProperty";

            //Create Table
            await SetupTableAsync();
            //Setup Entity
            var key = "a-" + Guid.NewGuid().ToString("N");
            _output.WriteLine("PartitionKey {0}", key);
            _output.WriteLine("RowKey {0}", key);
            var entity = new TableEntity(key, key);
            Assert.Equal(default, entity.ETag);
            Assert.Equal(default, entity.Timestamp);

            //Execute Add
            var addedEntity = await _tableClient.AddEntityWithHeaderValuesAsync(entity);

            //Execute isNull Query
            string filterByPartitionKey = TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, addedEntity.PartitionKey);
            string filterByRowKey = TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, addedEntity.PartitionKey);
            string filterByNullProperty = TableQuery.GenerateFilterConditionForBoolNull(propertyName, QueryComparisons.Equal);
            string filterByNotNullProperty = TableQuery.GenerateFilterConditionForBoolNull(propertyName, QueryComparisons.NotEqual);

            string filterNull = TableQuery.CombineFilters(
                                TableQuery.CombineFilters(filterByPartitionKey, TableOperators.And, filterByRowKey),
                                TableOperators.And,
                                filterByNullProperty);
            string filterNotNull = TableQuery.CombineFilters(
                    TableQuery.CombineFilters(filterByPartitionKey, TableOperators.And, filterByRowKey),
                    TableOperators.And,
                    filterByNotNullProperty);

            _output.WriteLine($"{nameof(filterNull)}:{filterNull}");
            _output.WriteLine($"{nameof(filterNotNull)}:{filterNotNull}");

            //Assert
            Assert.Equal(1, await _tableClient.QueryAsync<TableEntity>(filter: filterNull).CountAsync());
            Assert.Equal(0, await _tableClient.QueryAsync<TableEntity>(filter: filterNotNull).CountAsync());

            //Modify update
            var updateEntity = new TableEntity(key, key)
            {
                { propertyName, true }
            };

            await Task.Delay(1000); //wait 1 second for timestamp 

            _ = await _tableClient.UpdateEntityWithHeaderValuesAsync(updateEntity, addedEntity.ETag);

            //Assert
            Assert.Equal(0, await _tableClient.QueryAsync<TableEntity>(filter: filterNull).CountAsync());
            Assert.Equal(1, await _tableClient.QueryAsync<TableEntity>(filter: filterNotNull).CountAsync());


        }


    }
}
