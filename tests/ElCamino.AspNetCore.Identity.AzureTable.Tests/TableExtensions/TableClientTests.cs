﻿// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using ElCamino.Web.Identity.AzureTable.Tests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests.TableExtensions
{
    public class TableClientTests : IClassFixture<TableFixture>
    {
        private readonly ITestOutputHelper _output;
        private readonly TableFixture _tableFixture;
        private readonly TableServiceClient _tableServiceClient;
        private readonly string _tableName = "aatabletests";
        private readonly TableClient _tableClient;

        public TableClientTests(TableFixture tableFixture, ITestOutputHelper output)
        {
            _output = output;
            _tableFixture = tableFixture;
            _tableServiceClient = _tableFixture.TableService;
            _tableClient = _tableServiceClient.GetTableClient(_tableName);
        }


        private async Task SetupTableAsync()
        {
            //Setup Create table
            await _tableClient.CreateIfNotExistsAsync().ConfigureAwait(false);
            _output.WriteLine("Table created {0}", _tableName);

        }

        [Fact]
        public async Task AddUpdateGetEntityWithHeaderValues()
        {
            //Create Table
            await SetupTableAsync().ConfigureAwait(false);
            //Setup Entity
            string key = "a-" + Guid.NewGuid().ToString("N");
            _output.WriteLine("PartitionKey {0}", key);
            _output.WriteLine("RowKey {0}", key);
            TableEntity entity = new TableEntity(key, key);
            Assert.Equal(default, entity.ETag);
            Assert.Equal(default, entity.Timestamp);

            //Execute Upsert
            var addedEntity = await _tableClient.AddEntityWithHeaderValuesAsync<TableEntity>(entity).ConfigureAwait(false);

            //Assert
            Assert.NotEqual(default, addedEntity.ETag);
            Assert.NotEqual(default, addedEntity.Timestamp);

            //Modify update
            string propertyName = "newProperty";
            TableEntity updateEntity = new TableEntity(key, key);
            updateEntity.Add(propertyName, propertyName);

            await Task.Delay(1000).ConfigureAwait(false); //wait 1 second for timestamp 

            var updatedEntity = await _tableClient.UpdateEntityWithHeaderValuesAsync<TableEntity>(updateEntity, addedEntity.ETag).ConfigureAwait(false);
            //Assert
            Assert.NotEqual(default, updatedEntity.ETag);
            Assert.NotEqual(default, updatedEntity.Timestamp);

            Assert.NotEqual(addedEntity.ETag, updatedEntity.ETag);
            //TimeStamp is too flaky to test
            //Assert.NotEqual(addedEntity.Timestamp, updatedEntity.Timestamp);

            //Get and check new property
            var getEntity = await _tableClient.GetEntityOrDefaultAsync<TableEntity>(updateEntity.PartitionKey, updatedEntity.RowKey).ConfigureAwait(false);
            Assert.Equal(propertyName, getEntity.GetString(propertyName));

        }

        [Fact]
        public async Task UpsertGetEntityWithHeaderValues()
        {
            //Create Table
            await SetupTableAsync().ConfigureAwait(false);
            //Setup Entity
            string key = "a-" + Guid.NewGuid().ToString("N");
            _output.WriteLine("PartitionKey {0}", key);
            _output.WriteLine("RowKey {0}", key);
            TableEntity entity = new TableEntity(key, key);
            Assert.Equal(default, entity.ETag);
            Assert.Equal(default, entity.Timestamp);

            //Execute Upsert
            var addedEntity = await _tableClient.UpsertEntityWithHeaderValuesAsync<TableEntity>(entity).ConfigureAwait(false);

            //Assert
            Assert.NotEqual(default, addedEntity.ETag);
            Assert.NotEqual(default, addedEntity.Timestamp);

            //Modify update
            string propertyName = "newProperty";
            TableEntity updateEntity = new TableEntity(key, key);
            updateEntity.Add(propertyName, propertyName);

            await Task.Delay(1000).ConfigureAwait(false); //wait 1 second for timestamp 

            var updatedEntity = await _tableClient.UpsertEntityWithHeaderValuesAsync<TableEntity>(updateEntity).ConfigureAwait(false);
            //Assert
            Assert.NotEqual(default, updatedEntity.ETag);
            Assert.NotEqual(default, updatedEntity.Timestamp);

            Assert.NotEqual(addedEntity.ETag, updatedEntity.ETag);
            //TimeStamp is too flaky to test
            //Assert.NotEqual(addedEntity.Timestamp, updatedEntity.Timestamp);

            //Get and check new property
            var getEntity = await _tableClient.GetEntityOrDefaultAsync<TableEntity>(updateEntity.PartitionKey, updatedEntity.RowKey).ConfigureAwait(false);
            Assert.Equal(propertyName, getEntity.GetString(propertyName));

        }

        [Fact]
        public async Task ExecuteTableQueryTakeCount()
        {
            //Create Table
            await SetupTableAsync().ConfigureAwait(false);
            //Setup Entity
            string partitionKey = "b-" + Guid.NewGuid().ToString("N");
            _output.WriteLine("PartitionKey {0}", partitionKey);

            string filterByPartitionKey = TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, partitionKey);
            int count = await _tableClient.QueryAsync<TableEntity>(filter: filterByPartitionKey).CountAsync().ConfigureAwait(false);
            const int maxTestEntities = 1001;
            _output.WriteLine("Entities found {0}", count);

            BatchOperationHelper batch = new BatchOperationHelper(_tableClient);

            if (count < maxTestEntities)
            {                
                for (int i = 0; i < (maxTestEntities - count); i++)
                {
                    string rowKey = "b-" + Guid.NewGuid().ToString("N");
                    TableEntity entity = new TableEntity(partitionKey, rowKey);
                    batch.UpsertEntity(entity, TableUpdateMode.Replace);
                }
                await batch.SubmitBatchAsync().ConfigureAwait(false);
                count = await _tableClient.QueryAsync<TableEntity>(filter: filterByPartitionKey).CountAsync().ConfigureAwait(false);
                _output.WriteLine("Entities found after batch create {0}", count);
            }
            Assert.True(count >= maxTestEntities);

            //Execute Query Take 1
            TableQuery tq = new TableQuery();
            tq.FilterString = filterByPartitionKey;
            tq.TakeCount = 1;
            var take = await _tableClient.ExecuteQueryAsync<TableEntity>(tq).ToListAsync().ConfigureAwait(false);
            Assert.Equal(tq.TakeCount.Value, take.Count);
            _output.WriteLine($"Expected:{tq.TakeCount.Value} Actual:{take.Count}");

            //Execute Query Take 0
            tq.TakeCount = 0;
            take = await _tableClient.ExecuteQueryAsync<TableEntity>(tq).ToListAsync().ConfigureAwait(false);
            Assert.Equal(tq.TakeCount.Value, take.Count);
            _output.WriteLine($"Expected:{tq.TakeCount.Value} Actual:{take.Count}");

            //Execute Query Take null
            tq.TakeCount = null;
            take = await _tableClient.ExecuteQueryAsync<TableEntity>(tq).ToListAsync().ConfigureAwait(false);
            Assert.Equal(count, take.Count);
            _output.WriteLine($"Expected:{count} Actual:{take.Count}");

            //Execute Query Take 100
            tq.TakeCount = 100;
            take = await _tableClient.ExecuteQueryAsync<TableEntity>(tq).ToListAsync().ConfigureAwait(false);
            Assert.Equal(tq.TakeCount.Value, take.Count);
            _output.WriteLine($"Expected:{tq.TakeCount.Value} Actual:{take.Count}");

            //Execute Query Take 1000
            tq.TakeCount = 1000;
            take = await _tableClient.ExecuteQueryAsync<TableEntity>(tq).ToListAsync().ConfigureAwait(false);
            Assert.Equal(tq.TakeCount.Value, take.Count);
            _output.WriteLine($"Expected:{tq.TakeCount.Value} Actual:{take.Count}");

            //Execute Query Take 1001
            tq.TakeCount = 1001;
            take = await _tableClient.ExecuteQueryAsync<TableEntity>(tq).ToListAsync().ConfigureAwait(false);
            Assert.Equal(tq.TakeCount.Value, take.Count);
            _output.WriteLine($"Expected:{tq.TakeCount.Value} Actual:{take.Count}");

            foreach (TableEntity te in take)
            {
                batch.DeleteEntity(te.PartitionKey, te.RowKey, te.ETag);
            }
            await batch.SubmitBatchAsync().ConfigureAwait(false);
            count = await _tableClient.QueryAsync<TableEntity>(filter: filterByPartitionKey).CountAsync().ConfigureAwait(false);
            _output.WriteLine("Entities found after batch delete {0}", count);
            Assert.Equal(0, count);

        }

    }
}