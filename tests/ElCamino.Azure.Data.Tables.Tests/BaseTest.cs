// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using Azure.Data.Tables;
using Xunit;
using Xunit.Abstractions;

namespace ElCamino.Azure.Data.Tables.Tests
{
    public class BaseTest : IClassFixture<TableFixture>
    {
        protected readonly ITestOutputHelper _output;
        protected readonly TableFixture _tableFixture;
        protected readonly TableServiceClient _tableServiceClient;
        protected const string TableName = "aatabletests";
        protected readonly TableClient _tableClient;

        public BaseTest(TableFixture tableFixture, ITestOutputHelper output)
        {
            _output = output;
            _tableFixture = tableFixture;
            _tableServiceClient = _tableFixture.TableService;
            _tableClient = _tableServiceClient.GetTableClient(TableName);
        }

    }
}
