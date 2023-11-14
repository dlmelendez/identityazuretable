// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Xunit;
using Xunit.Abstractions;

namespace ElCamino.Azure.Data.Tables.Tests
{
    public class TableClientTests : BaseTest
    {

        public TableClientTests(TableFixture tableFixture, ITestOutputHelper output) :
            base(tableFixture, output)
        { }

        private async Task SetupTableAsync()
        {
            //Setup Create table
            await _tableClient.CreateIfNotExistsAsync();
            _output.WriteLine("Table created {0}", TableName);

        }        

        [Fact]
        public async Task AddUpdateGetEntityWithHeaderValues()
        {
            //Create Table
            await SetupTableAsync();
            //Setup Entity
            var key = "a-" + Guid.NewGuid().ToString("N");
            _output.WriteLine("PartitionKey {0}", key);
            _output.WriteLine("RowKey {0}", key);
            var entity = new TableEntity(key, key);
            Assert.Equal(default, entity.ETag);
            Assert.Equal(default, entity.Timestamp);

            //Execute Upsert
            var addedEntity = await _tableClient.AddEntityWithHeaderValuesAsync(entity);

            //Assert
            Assert.NotEqual(default, addedEntity.ETag);
            Assert.NotEqual(default, addedEntity.Timestamp);

            //Modify update
            var propertyName = "newProperty";
            var updateEntity = new TableEntity(key, key)
            {
                { propertyName, propertyName }
            };

            await Task.Delay(1000); //wait 1 second for timestamp 

            var updatedEntity = await _tableClient.UpdateEntityWithHeaderValuesAsync(updateEntity, addedEntity.ETag);
            //Assert
            Assert.NotEqual(default, updatedEntity.ETag);
            Assert.NotEqual(default, updatedEntity.Timestamp);

            Assert.NotEqual(addedEntity.ETag, updatedEntity.ETag);
            //TimeStamp is too flaky to test
            //Assert.NotEqual(addedEntity.Timestamp, updatedEntity.Timestamp);

            //Get and check new property
            var getEntity = await _tableClient.GetEntityOrDefaultAsync<TableEntity>(updateEntity.PartitionKey, updatedEntity.RowKey);
            Assert.Equal(propertyName, getEntity.GetString(propertyName));

        }

        [Fact]
        public async Task UpsertGetEntityWithHeaderValues()
        {
            //Create Table
            await SetupTableAsync();
            //Setup Entity
            var key = "a-" + Guid.NewGuid().ToString("N");
            _output.WriteLine("PartitionKey {0}", key);
            _output.WriteLine("RowKey {0}", key);
            var entity = new TableEntity(key, key);
            Assert.Equal(default, entity.ETag);
            Assert.Equal(default, entity.Timestamp);

            //Execute Upsert
            var addedEntity = await _tableClient.UpsertEntityWithHeaderValuesAsync(entity);

            //Assert
            Assert.NotEqual(default, addedEntity.ETag);
            Assert.NotEqual(default, addedEntity.Timestamp);

            //Modify update
            var propertyName = "newProperty";
            var updateEntity = new TableEntity(key, key)
            {
                { propertyName, propertyName }
            };

            await Task.Delay(1000); //wait 1 second for timestamp 

            var updatedEntity = await _tableClient.UpsertEntityWithHeaderValuesAsync(updateEntity);
            //Assert
            Assert.NotEqual(default, updatedEntity.ETag);
            Assert.NotEqual(default, updatedEntity.Timestamp);

            Assert.NotEqual(addedEntity.ETag, updatedEntity.ETag);
            //TimeStamp is too flaky to test
            //Assert.NotEqual(addedEntity.Timestamp, updatedEntity.Timestamp);

            //Get and check new property
            var getEntity = await _tableClient.GetEntityOrDefaultAsync<TableEntity>(updateEntity.PartitionKey, updatedEntity.RowKey);
            Assert.Equal(propertyName, getEntity.GetString(propertyName));

        }

        [Fact]
        public async Task ExecuteTableQueryTakeCount()
        {
            //Create Table
            await SetupTableAsync();
            //Setup Entity
            var partitionKey = "b-" + Guid.NewGuid().ToString("N");
            _output.WriteLine("PartitionKey {0}", partitionKey);

            var filterByPartitionKey = TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, partitionKey);
            var count = await _tableClient.QueryAsync<TableEntity>(filter: filterByPartitionKey).CountAsync();
            const int maxTestEntities = 1001;
            _output.WriteLine("Entities found {0}", count);

            var batch = new BatchOperationHelper(_tableClient);

            if (count < maxTestEntities)
            {
                for (var i = 0; i < maxTestEntities - count; i++)
                {
                    var rowKey = "b-" + Guid.NewGuid().ToString("N");
                    var entity = new TableEntity(partitionKey, rowKey);
                    batch.UpsertEntity(entity, TableUpdateMode.Replace);
                }
                await batch.SubmitBatchAsync();
                count = await _tableClient.QueryAsync<TableEntity>(filter: filterByPartitionKey).CountAsync();
                _output.WriteLine("Entities found after batch create {0}", count);
            }
            Assert.True(count >= maxTestEntities);

            //Execute Query Take 1
            var tq = new TableQuery();
            tq.FilterString = filterByPartitionKey;
            tq.TakeCount = 1;
            var take = await _tableClient.ExecuteQueryAsync<TableEntity>(tq).ToListAsync();
            Assert.Equal(tq.TakeCount.Value, take.Count);
            _output.WriteLine($"Expected:{tq.TakeCount.Value} Actual:{take.Count}");

            //Execute Query Take 0
            tq.TakeCount = 0;
            take = await _tableClient.ExecuteQueryAsync<TableEntity>(tq).ToListAsync();
            Assert.Equal(tq.TakeCount.Value, take.Count);
            _output.WriteLine($"Expected:{tq.TakeCount.Value} Actual:{take.Count}");

            //Execute Query Take null
            tq.TakeCount = null;
            take = await _tableClient.ExecuteQueryAsync<TableEntity>(tq).ToListAsync();
            Assert.Equal(count, take.Count);
            _output.WriteLine($"Expected:{count} Actual:{take.Count}");

            //Execute Query Take 100
            tq.TakeCount = 100;
            take = await _tableClient.ExecuteQueryAsync<TableEntity>(tq).ToListAsync();
            Assert.Equal(tq.TakeCount.Value, take.Count);
            _output.WriteLine($"Expected:{tq.TakeCount.Value} Actual:{take.Count}");

            //Execute Query Take 1000
            tq.TakeCount = 1000;
            take = await _tableClient.ExecuteQueryAsync<TableEntity>(tq).ToListAsync();
            Assert.Equal(tq.TakeCount.Value, take.Count);
            _output.WriteLine($"Expected:{tq.TakeCount.Value} Actual:{take.Count}");

            //Execute Query Take 1001
            tq.TakeCount = 1001;
            take = await _tableClient.ExecuteQueryAsync<TableEntity>(tq).ToListAsync();
            Assert.Equal(tq.TakeCount.Value, take.Count);
            _output.WriteLine($"Expected:{tq.TakeCount.Value} Actual:{take.Count}");

            foreach (var te in take)
            {
                batch.DeleteEntity(te.PartitionKey, te.RowKey, te.ETag);
            }
            await batch.SubmitBatchAsync();
            count = await _tableClient.QueryAsync<TableEntity>(filter: filterByPartitionKey).CountAsync();
            _output.WriteLine("Entities found after batch delete {0}", count);
            Assert.Equal(0, count);

        }

    }
}
