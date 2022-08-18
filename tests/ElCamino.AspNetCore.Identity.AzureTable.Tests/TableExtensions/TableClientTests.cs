// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
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
            await _tableClient.CreateIfNotExistsAsync();
            _output.WriteLine("Table created {0}", _tableName);

        }

        [Fact]
        public async Task AddUpdateGetEntityWithHeaderValues()
        {
            //Create Table
            await SetupTableAsync();
            //Setup Entity
            string key = "a-" + Guid.NewGuid().ToString("N");
            _output.WriteLine("PartitionKey {0}", key);
            _output.WriteLine("RowKey {0}", key);
            TableEntity entity = new TableEntity(key, key);
            Assert.Equal(default, entity.ETag);
            Assert.Equal(default, entity.Timestamp);

            //Execute Upsert
            var addedEntity = await _tableClient.AddEntityWithHeaderValuesAsync<TableEntity>(entity);

            //Assert
            Assert.NotEqual(default, addedEntity.ETag);
            Assert.NotEqual(default, addedEntity.Timestamp);

            //Modify update
            string propertyName = "newProperty";
            TableEntity updateEntity = new TableEntity(key, key);
            updateEntity.Add(propertyName, propertyName);

            await Task.Delay(1000); //wait 1 second for timestamp 

            var updatedEntity = await _tableClient.UpdateEntityWithHeaderValuesAsync<TableEntity>(updateEntity, addedEntity.ETag);
            //Assert
            Assert.NotEqual(default, updatedEntity.ETag);
            Assert.NotEqual(default, updatedEntity.Timestamp);

            Assert.NotEqual(addedEntity.ETag, updatedEntity.ETag);
            Assert.NotEqual(addedEntity.Timestamp, updatedEntity.Timestamp);

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
            string key = "a-" + Guid.NewGuid().ToString("N");
            _output.WriteLine("PartitionKey {0}", key);
            _output.WriteLine("RowKey {0}", key);
            TableEntity entity = new TableEntity(key, key);
            Assert.Equal(default, entity.ETag);
            Assert.Equal(default, entity.Timestamp);

            //Execute Upsert
            var addedEntity = await _tableClient.UpsertEntityWithHeaderValuesAsync<TableEntity>(entity);

            //Assert
            Assert.NotEqual(default, addedEntity.ETag);
            Assert.NotEqual(default, addedEntity.Timestamp);

            //Modify update
            string propertyName = "newProperty";
            TableEntity updateEntity = new TableEntity(key, key);
            updateEntity.Add(propertyName, propertyName);

            await Task.Delay(1000); //wait 1 second for timestamp 

            var updatedEntity = await _tableClient.UpsertEntityWithHeaderValuesAsync<TableEntity>(updateEntity);
            //Assert
            Assert.NotEqual(default, updatedEntity.ETag);
            Assert.NotEqual(default, updatedEntity.Timestamp);

            Assert.NotEqual(addedEntity.ETag, updatedEntity.ETag);
            Assert.NotEqual(addedEntity.Timestamp, updatedEntity.Timestamp);

            //Get and check new property
            var getEntity = await _tableClient.GetEntityOrDefaultAsync<TableEntity>(updateEntity.PartitionKey, updatedEntity.RowKey);
            Assert.Equal(propertyName, getEntity.GetString(propertyName));

        }
    }
}
