// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using IdentityUser = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityUser<string>;
using IdentityRole = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityRole;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ElCamino.Web.Identity.AzureTable.Tests.ModelTests;
using Microsoft.AspNetCore.Identity;

namespace ElCamino.Web.Identity.AzureTable.Tests.Fixtures
{
    public partial class BaseFixture<TUser, TRole, TContext, TUserStore> : IDisposable
        where TUser : IdentityUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityCloudContext, new()
        where TUserStore : UserStoreV2<TUser, TRole, TContext>
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
                LocationMode = root["IdentityAzureTable:identityConfiguration:locationMode"],
                IndexTableName = root["IdentityAzureTable:identityConfiguration:indexTableName"],
                UserTableName = root["IdentityAzureTable:identityConfiguration:userTableName"],
                RoleTableName = root["IdentityAzureTable:identityConfiguration:roleTableName"]
            };

            return idconfig;
        }

        protected bool IsV2()
        {
            return new TUser() is IdentityUserV2;
        }

        public TContext GetContext()
        {
            return GetContext(GetConfig());
        }

        public TContext GetContext(IdentityConfiguration config)
        {
            return Activator.CreateInstance(typeof(TContext), new object[1] {GetConfig()}) as TContext;

        }

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
            var id = services.AddIdentity<TUser, TRole>(
            (config) =>
            {
                config.Lockout = new LockoutOptions() { MaxFailedAccessAttempts = 2 };
                config.Password.RequireDigit = false;
                config.Password.RequiredLength = 3;
                config.Password.RequireLowercase = false;
                config.Password.RequireNonAlphanumeric = false;
                config.Password.RequireUppercase = false;
            });
            if (IsV2())
            {
                id = id.AddAzureTableStoresV2<TContext>(new Func<IdentityConfiguration>(() =>
                {
                    return GetConfig();
                }));
            }
            else
            {
                id = id.AddAzureTableStores<TContext>(new Func<IdentityConfiguration>(() =>
                {
                    return GetConfig();
                }));
            }
            id.CreateAzureTablesIfNotExists<TContext>();
            id.AddDefaultTokenProviders();
            services.AddLogging();

            return services.BuildServiceProvider().GetService(typeof(RoleManager<TRole>)) as RoleManager<TRole>;
        }

        public TUserStore CreateUserStore()
        {
            return CreateUserStore(GetContext(),GetConfig());
        }

        public TUserStore CreateUserStore(TContext context,IdentityConfiguration config)
        {
            var userStore = Activator.CreateInstance(typeof(TUserStore), new object[2] { context,config }) as TUserStore;

            return userStore;
        }

        public UserManager<TUser> CreateUserManager(IdentityOptions options = null)
        {
            if (options == null)
            {
                options = new IdentityOptions();
            }
            //return new RoleManager<TRole>(store);
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Add Identity services to the services container.
            var id = services.AddIdentity<TUser, IdentityRole>((config) =>
            {
                config.User.RequireUniqueEmail = options.User.RequireUniqueEmail;
                config.Lockout.DefaultLockoutTimeSpan = options.Lockout.DefaultLockoutTimeSpan;
                config.Lockout.MaxFailedAccessAttempts = options.Lockout.MaxFailedAccessAttempts;
            });

            if (IsV2())
            {
                id = id.AddAzureTableStoresV2<TContext>(new Func<IdentityConfiguration>(() =>
                {
                    return GetConfig();
                }));
            }
            else
            {
                id = id.AddAzureTableStores<TContext>(new Func<IdentityConfiguration>(() =>
                {
                    return GetConfig();
                }));
            }
            id.AddDefaultTokenProviders();
            services.AddLogging();

            return services.BuildServiceProvider().GetService(typeof(UserManager<TUser>)) as UserManager<TUser>;
        }
    }
}
