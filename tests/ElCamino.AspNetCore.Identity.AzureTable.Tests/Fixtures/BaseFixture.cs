// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IdentityRole = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityRole;
using IdentityUser = ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityUser<string>;
using Model = ElCamino.AspNetCore.Identity.AzureTable.Model;

namespace ElCamino.Web.Identity.AzureTable.Tests.Fixtures
{
    public class BaseFixture<TUser, TRole, TContext, TUserStore, TKeyHelper>
        : BaseFixture<TUser, TContext, string, Model.IdentityUserClaim, Model.IdentityUserLogin, Model.IdentityUserToken, TUserStore, TKeyHelper>
        where TUser : IdentityUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityCloudContext
        where TUserStore : UserStore<TUser, TRole, TContext>
        where TKeyHelper : IKeyHelper, new()
    {

        public RoleStore<TRole> CreateRoleStore()
        {
            return new RoleStore<TRole>(GetContext(), new TKeyHelper());
        }

        public RoleStore<TRole> CreateRoleStore(TContext context)
        {
            return new RoleStore<TRole>(context, new TKeyHelper());
        }

        public RoleManager<TRole> CreateRoleManager()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            // Add Identity services to the services container.
            var id = services.AddIdentityCore<TUser>(
            (config) =>
            {
                config.Lockout = new LockoutOptions() { MaxFailedAccessAttempts = 2 };
                config.Password.RequireDigit = false;
                config.Password.RequiredLength = 3;
                config.Password.RequireLowercase = false;
                config.Password.RequireNonAlphanumeric = false;
                config.Password.RequireUppercase = false;
            });

            id.AddRoles<IdentityRole>();


            id = id.AddAzureTableStores<TContext>(new Func<IdentityConfiguration>(() =>
            {
                return GetConfig();
            }), GetKeyHelper());


            id.CreateAzureTablesIfNotExists<TContext>();
            id.Services.AddDataProtection();
            id.AddDefaultTokenProviders();

            services.AddLogging();

            return services.BuildServiceProvider().GetService(typeof(RoleManager<TRole>)) as RoleManager<TRole>;
        }

        public override UserManager<TUser> CreateUserManager(IdentityOptions options = null)
        {
            if (options == null)
            {
                options = new IdentityOptions();
            }
            //return new RoleManager<TRole>(store);
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Add Identity services to the services container.
            var id = services.AddIdentityCore<TUser>((config) =>
            {
                config.User.RequireUniqueEmail = options.User.RequireUniqueEmail;
                config.Lockout.DefaultLockoutTimeSpan = options.Lockout.DefaultLockoutTimeSpan;
                config.Lockout.MaxFailedAccessAttempts = options.Lockout.MaxFailedAccessAttempts;
            });

            //Add the role here to load the correct UserStore
            id.AddRoles<IdentityRole>();

            id = id.AddAzureTableStores<TContext>(new Func<IdentityConfiguration>(() =>
            {
                return GetConfig();
            }), GetKeyHelper());

            id.Services.AddDataProtection();
            id.AddDefaultTokenProviders();

            id.AddSignInManager();

            services.AddLogging();

            return services.BuildServiceProvider().GetService(typeof(UserManager<TUser>)) as UserManager<TUser>;
        }

    }

    public class BaseFixture<TUser, TContext, TUserStore, TKeyHelper>
    : BaseFixture<TUser, TContext, string, Model.IdentityUserClaim, Model.IdentityUserLogin, Model.IdentityUserToken, TUserStore, TKeyHelper>
    where TUser : IdentityUser, new()
    where TContext : IdentityCloudContext
    where TUserStore : UserOnlyStore<TUser, TContext, string, Model.IdentityUserClaim, Model.IdentityUserLogin, Model.IdentityUserToken>
    where TKeyHelper : IKeyHelper, new()
    {

    }

    public class BaseFixture<TUser, TContext, TKey, TUserClaim, TUserLogin, TUserToken, TUserStore, TKeyHelper> : IDisposable
        where TUser : Model.IdentityUser<TKey>, new()
        where TKey : IEquatable<TKey>
        where TUserLogin : Model.IdentityUserLogin<TKey>, new()
        where TUserClaim : Model.IdentityUserClaim<TKey>, new()
        where TUserToken : Model.IdentityUserToken<TKey>, new()
        where TContext : IdentityCloudContext
        where TUserStore : UserOnlyStore<TUser, TContext, TKey, TUserClaim, TUserLogin, TUserToken>
        where TKeyHelper : IKeyHelper, new()
    {

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
            GC.SuppressFinalize(this);
        }

        public TKeyHelper GetKeyHelper()
        {
            return new TKeyHelper();
        }

        public IdentityConfiguration GetConfig()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("config.json", reloadOnChange: true, optional: false);

            var root = configuration.Build();

            var idconfig = new IdentityConfiguration()
            {
                StorageConnectionString = root["IdentityAzureTable:identityConfiguration:storageConnectionString"],
                TablePrefix = root["IdentityAzureTable:identityConfiguration:tablePrefix"],
                IndexTableName = root["IdentityAzureTable:identityConfiguration:indexTableName"],
                UserTableName = root["IdentityAzureTable:identityConfiguration:userTableName"],
                RoleTableName = root["IdentityAzureTable:identityConfiguration:roleTableName"]
            };

            return idconfig;
        }

        protected bool IsV2()
        {
            return new TUser() is Model.IdentityUser;
        }

        public TContext GetContext()
        {
            return GetContext(GetConfig());
        }

        public TContext GetContext(IdentityConfiguration config)
        {
            return Activator.CreateInstance(typeof(TContext), new object[1] { config }) as TContext;

        }

        public TUserStore CreateUserStore()
        {
            return CreateUserStore(GetContext());
        }

        public TUserStore CreateUserStore(TContext context)
        {
            var userStore = Activator.CreateInstance(typeof(TUserStore), new object[2] { context, GetKeyHelper() }) as TUserStore;

            return userStore;
        }

        public virtual UserManager<TUser> CreateUserManager(IdentityOptions options = null)
        {
            if (options == null)
            {
                options = new IdentityOptions();
            }

            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Add Identity services to the services container.
            var id = services.AddIdentityCore<TUser>((config) =>
            {
                config.User.RequireUniqueEmail = options.User.RequireUniqueEmail;
                config.Lockout.DefaultLockoutTimeSpan = options.Lockout.DefaultLockoutTimeSpan;
                config.Lockout.MaxFailedAccessAttempts = options.Lockout.MaxFailedAccessAttempts;
            });


            id = id.AddAzureTableStores<TContext>(new Func<IdentityConfiguration>(() =>
            {
                return GetConfig();
            }), GetKeyHelper());

            id.Services.AddDataProtection();
            id.AddDefaultTokenProviders();

            id.AddSignInManager();

            services.AddLogging();

            return services.BuildServiceProvider().GetService(typeof(UserManager<TUser>)) as UserManager<TUser>;
        }
    }
}
