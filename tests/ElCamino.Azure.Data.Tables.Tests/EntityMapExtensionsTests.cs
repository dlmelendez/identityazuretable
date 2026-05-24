// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using Azure;
using Azure.Data.Tables;
using Xunit;

namespace ElCamino.Azure.Data.Tables.Tests
{
    public class EntityMapExtensionsTests
    {
        [Fact]
        public void MapTableEntityMapsExactMatches()
        {
            TableEntity entity = CreateTableEntity();
            entity[nameof(MapTestEntity.Name)] = "test-name";
            entity[nameof(MapTestEntity.Count)] = 42;

            MapTestEntity mapped = entity.MapTableEntity<MapTestEntity>();

            Assert.Equal("test-name", mapped.Name);
            Assert.Equal(42, mapped.Count);
        }

        [Fact]
        public void MapTableEntityMapsNullableValueTypeFromUnderlyingValue()
        {
            TableEntity entity = CreateTableEntity();
            entity[nameof(MapTestEntity.OptionalCount)] = 42;

            MapTestEntity mapped = entity.MapTableEntity<MapTestEntity>();

            Assert.Equal(42, mapped.OptionalCount);
        }

        [Fact]
        public void MapTableEntityMapsAssignableReferenceType()
        {
            TableEntity entity = CreateTableEntity();
            entity[nameof(MapTestEntity.AssignableValue)] = "assignable-value";

            MapTestEntity mapped = entity.MapTableEntity<MapTestEntity>();

            Assert.Equal("assignable-value", mapped.AssignableValue);
        }

        [Fact]
        public void MapTableEntitySkipsIncompatibleValues()
        {
            TableEntity entity = CreateTableEntity();
            entity[nameof(MapTestEntity.Count)] = "not-an-int";

            MapTestEntity mapped = entity.MapTableEntity<MapTestEntity>();

            Assert.Equal(0, mapped.Count);
        }

        [Fact]
        public void MapTableEntitySkipsNullValues()
        {
            TableEntity entity = CreateTableEntity();
            entity[nameof(MapTestEntity.Name)] = null;

            MapTestEntity mapped = entity.MapTableEntity<MapTestEntity>();

            Assert.Equal("default-name", mapped.Name);
        }

        private static TableEntity CreateTableEntity()
        {
            return new TableEntity("partition", "row");
        }

        private sealed class MapTestEntity : ITableEntity
        {
            public string PartitionKey { get; set; } = string.Empty;

            public string RowKey { get; set; } = string.Empty;

            public DateTimeOffset? Timestamp { get; set; }

            public ETag ETag { get; set; }

            public string Name { get; set; } = "default-name";

            public int Count { get; set; }

            public int? OptionalCount { get; set; }

            public object AssignableValue { get; set; }
        }
    }
}