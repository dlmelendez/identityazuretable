// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace ElCamino.Azure.Data.Tables.Tests
{
    public class TableFixture : IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly TableServiceClient _tableServiceClient;
        private bool disposedValue;

        public TableServiceClient TableService => _tableServiceClient;

        public TableFixture()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("config.json", reloadOnChange: true, optional: false);

            _configuration = configuration.Build();

            _tableServiceClient = new TableServiceClient(_configuration["ElCamino:storageConnectionString"]);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
