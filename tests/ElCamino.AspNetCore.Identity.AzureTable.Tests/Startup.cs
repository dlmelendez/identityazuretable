// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Identity.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using IdentityUser = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityUser;
using IdentityRole = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityRole;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;

namespace ElCamino.AspNetCore.Identity.AzureTable.TestsExp
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            // Setup configuration sources.
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("config.json")
                .AddJsonFile($"config.{env.EnvironmentName}.json", optional: true);

            configuration.AddEnvironmentVariables();
            Configuration = configuration.Build();
        }

        public IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddDataProtection();

            // Add Identity services to the services container.
            services.AddIdentityCore<IdentityUser>((config) =>
            {

            })
            //.AddEntityFrameworkStores<ApplicationDbContext>()            
            .AddAzureTableStoresV2<IdentityCloudContext>(new Func<IdentityConfiguration>(() =>
            {
                return new IdentityConfiguration()
                {
                    StorageConnectionString = Configuration.GetValue<string>("IdentityAzureTable:identityConfiguration:storageConnectionString")
                };
            }))
            .AddDefaultTokenProviders();

            // Add MVC services to the services container.
            //services.AddMvc();

        }

        // Configure is called after ConfigureServices is called.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerfactory)
        {
            // Add cookie-based authentication to the request pipeline.
            app.UseAuthentication();
        }
    }
}
