// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
#if !net45
using System;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;

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

            // Add Identity services to the services container.
            services.AddIdentity<IdentityUser, IdentityRole>((config) =>
            {
                
            })
                //.AddEntityFrameworkStores<ApplicationDbContext>()
                .AddAzureTableStores<IdentityCloudContext>(new Func<IdentityConfiguration>(() =>
                {
                    return new IdentityConfiguration() {
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
            app.UseIdentity();
            
        }
    }
}
#endif