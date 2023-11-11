// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Microsoft.AspNetCore.Identity;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// IdentityBuilder extensions for di
    /// </summary>
    public static class IdentityAzureTableBuilderExtensions
    {
        /// <summary>
        /// Use this to load and configure the Identity Azure Tables into the aspnet identity pipeline.
        /// Note: <see cref="IdentityBuilder.AddRoles{TRole}"/> prior to calling this method in the pipeline if you need Roles functionality, otherwise the RoleStore will not be loaded.
        /// </summary>
        /// <typeparam name="TContext">Use or extend <see cref="IdentityCloudContext"/></typeparam>
        /// <param name="builder"><see cref="IdentityBuilder"/> aspnet identity pipeline</param>
        /// <param name="configAction"><see cref="IdentityConfiguration"/></param>
        /// <param name="keyHelper">Use <see cref="DefaultKeyHelper"/> that uses SHA1, <see cref="SHA256KeyHelper"/> or a custom keyhelper that implements <see cref="IKeyHelper"/> </param>
        /// <returns><see cref="IdentityBuilder"/></returns>
        public static IdentityBuilder AddAzureTableStores<TContext>(this IdentityBuilder builder, Func<IdentityConfiguration> configAction,
            IKeyHelper? keyHelper = null)
            where TContext : IdentityCloudContext
        {
            return builder.AddAzureTableStores<TContext>(_ => configAction(), keyHelper);
        }

        /// <summary>
        /// Use this to load and configure the Identity Azure Tables into the aspnet identity pipeline.
        /// Note: <see cref="IdentityBuilder.AddRoles{TRole}"/> prior to calling this method in the pipeline if you need Roles functionality, otherwise the RoleStore will not be loaded.
        /// </summary>
        /// <typeparam name="TContext">Use or extend <see cref="IdentityCloudContext"/></typeparam>
        /// <param name="builder"><see cref="IdentityBuilder"/> aspnet identity pipeline</param>
        /// <param name="configAction"><see cref="IdentityConfiguration"/></param>
        /// <param name="keyHelper">Use <see cref="DefaultKeyHelper"/> that uses SHA1, <see cref="SHA256KeyHelper"/> or a custom keyhelper that implements <see cref="IKeyHelper"/> </param>
        /// <returns><see cref="IdentityBuilder"/></returns>
        public static IdentityBuilder AddAzureTableStores<TContext>(this IdentityBuilder builder, Func<IServiceProvider, IdentityConfiguration> configAction,
            IKeyHelper? keyHelper = null)
            where TContext : IdentityCloudContext
        {

            builder.Services.AddSingleton<IKeyHelper>(keyHelper ?? new DefaultKeyHelper());

            builder.Services.AddSingleton<IdentityConfiguration>(configAction);

            Type contextType = typeof(TContext);
            builder.Services.AddSingleton(contextType, contextType);

            Type userStoreType = builder.RoleType is not null ? typeof(UserStore<,,>).MakeGenericType(builder.UserType, builder.RoleType, contextType)
                : typeof(UserOnlyStore<,>).MakeGenericType(builder.UserType, contextType);

            builder.Services.AddScoped(
                typeof(IUserStore<>).MakeGenericType(builder.UserType),
                userStoreType);

            if (builder.RoleType is not null)
            {
                Type roleStoreType = typeof(RoleStore<,>).MakeGenericType(builder.RoleType, contextType);

                builder.Services.AddScoped(
                    typeof(IRoleStore<>).MakeGenericType(builder.RoleType), roleStoreType);
            }
            return builder;
        }

        /// <summary>
        /// Use this to create all table resources needed. Execute this after any table name configuration change. Remove after first run if you want.
        /// </summary>
        /// <typeparam name="TContext">Use or extend <see cref="IdentityCloudContext"/></typeparam>
        /// <param name="builder"><see cref="IdentityBuilder"/> aspnet identity pipeline</param>
        /// <returns><see cref="IdentityBuilder"/></returns>
        public static IdentityBuilder CreateAzureTablesIfNotExists<TContext>(this IdentityBuilder builder)
            where TContext : IdentityCloudContext
        {
            Type userStoreType = typeof(IUserStore<>).MakeGenericType(builder.UserType);

            var userStore = ActivatorUtilities.GetServiceOrCreateInstance(builder.Services.BuildServiceProvider(),
                userStoreType) as dynamic;

            userStore.CreateTablesIfNotExistsAsync().Wait();

            return builder;
        }
    }
}
