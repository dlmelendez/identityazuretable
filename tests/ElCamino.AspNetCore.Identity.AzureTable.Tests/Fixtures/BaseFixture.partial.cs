// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using IdentityUser = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityUser;
using IdentityRole = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityRole;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ElCamino.Web.Identity.AzureTable.Tests.ModelTests;
using Microsoft.AspNetCore.Identity;

namespace ElCamino.Web.Identity.AzureTable.Tests.Fixtures
{
    public partial class BaseFixture<TUser, TRole, TContext> : IDisposable
        where TUser : IdentityUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityCloudContext, new()
    {

        #region IDisposable Support
        protected bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //  dispose managed state (managed objects).
                }

                // free unmanaged resources (unmanaged objects) and override a finalizer below.
                // set large fields to null.

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        public IdentityConfiguration GetConfig()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("config.json", reloadOnChange: true, optional: false);

            var root = configuration.Build();

            var idconfig = new IdentityConfiguration()
            {
                StorageConnectionString = root["IdentityAzureTable:identityConfiguration:storageConnectionString"],
                TablePrefix = root["IdentityAzureTable:identityConfiguration:tablePrefix"],
                LocationMode = root["IdentityAzureTable:identityConfiguration:locationMode"]
            };

            return idconfig;
        }

        public IdentityCloudContext GetContext() => new IdentityCloudContext(GetConfig());

        public RoleStore<TRole> CreateRoleStore()
        {
            return new RoleStore<TRole>(GetContext());
        }

        public RoleStore<TRole> CreateRoleStore(TContext context)
        {
            return new RoleStore<TRole>(context);
        }

        public RoleManager<TRole> CreateRoleManager()
        {
            return CreateRoleManager(CreateRoleStore());
        }

        public RoleManager<TRole> CreateRoleManager(TContext context)
        {
            return CreateRoleManager(new RoleStore<TRole>(context));
        }

        public RoleManager<TRole> CreateRoleManager(RoleStore<TRole> store)
        {
            //return new RoleManager<TRole>(store);
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            // Add Identity services to the services container.
            services.AddIdentity<ApplicationUser, TRole>(
            (config) =>
            {
                config.Lockout = new LockoutOptions() { MaxFailedAccessAttempts = 2 };
                config.Password.RequireDigit = false;
                config.Password.RequiredLength = 3;
                config.Password.RequireLowercase = false;
                config.Password.RequireNonAlphanumeric = false;
                config.Password.RequireUppercase = false;
            })
            //.AddEntityFrameworkStores<ApplicationDbContext>()
            .AddAzureTableStores<IdentityCloudContext>(new Func<IdentityConfiguration>(() =>
            {
                return GetConfig();
            }))
            .AddDefaultTokenProviders();
            services.AddLogging();

            return services.BuildServiceProvider().GetService(typeof(RoleManager<TRole>)) as RoleManager<TRole>;
        }

        public UserStore<TUser> CreateUserStore()
        {
            return new UserStore<TUser>(GetContext());
        }

        public UserStore<TUser> CreateUserStore(TContext context)
        {
            return new UserStore<TUser>(context);
        }

        public UserManager<TUser> CreateUserManager()
        {
            return CreateUserManager(new UserStore<TUser>(GetContext()));
        }

        public UserManager<TUser> CreateUserManager(TContext context)
        {
            return CreateUserManager(new UserStore<TUser>(context));
        }

        public UserManager<TUser> CreateUserManager(UserStore<TUser> store, IdentityOptions options = null)
        {
            if (options == null)
            {
                options = new IdentityOptions();
            }
            //return new RoleManager<TRole>(store);
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Add Identity services to the services container.
            services.AddIdentity<TUser, IdentityRole>((config) =>
            {
                config.User.RequireUniqueEmail = options.User.RequireUniqueEmail;
                config.Lockout.DefaultLockoutTimeSpan = options.Lockout.DefaultLockoutTimeSpan;
                config.Lockout.MaxFailedAccessAttempts = options.Lockout.MaxFailedAccessAttempts;
            })
                //.AddEntityFrameworkStores<ApplicationDbContext>()
                .AddAzureTableStores<IdentityCloudContext>(new Func<IdentityConfiguration>(
                    () => GetConfig()))
                .AddDefaultTokenProviders();
            services.AddLogging();

            return services.BuildServiceProvider().GetService(typeof(UserManager<TUser>)) as UserManager<TUser>;
        }
    }
}
