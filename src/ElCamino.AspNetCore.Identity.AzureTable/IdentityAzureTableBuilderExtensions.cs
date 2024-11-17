// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using Azure.Data.Tables;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using Microsoft.AspNetCore.Identity;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.DependencyInjection
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// IdentityBuilder extensions for di
    /// </summary>
    public static class IdentityAzureTableBuilderExtensions
    {
        /// <summary>
        /// Use this key to retrieve the <see cref="TableServiceClient"/> for the Identity Azure Tables
        /// </summary>
        public const string IdentityAzureTableServiceClientKey = "IdentityAzureTableServiceClientKey";

        /// <summary>
        /// Use this to load and configure the Identity Azure Tables into the aspnet identity pipeline.
        /// Note: <see cref="IdentityBuilder.AddRoles{TRole}"/> prior to calling this method in the pipeline if you need Roles functionality, otherwise the RoleStore will not be loaded.
        /// </summary>
        /// <typeparam name="TContext">Use or extend <see cref="IdentityCloudContext"/></typeparam>
        /// <param name="builder"><see cref="IdentityBuilder"/> aspnet identity pipeline</param>
        /// <param name="configAction"><see cref="IdentityConfiguration"/></param>
        /// <param name="tableServiceClientAction"><see cref="TableServiceClient"/></param>
        /// <param name="keyHelper">Use <see cref="DefaultKeyHelper"/> that uses SHA1, <see cref="SHA256KeyHelper"/> or a custom keyhelper that implements <see cref="IKeyHelper"/> </param>
        /// <returns><see cref="IdentityBuilder"/></returns>
        public static IdentityBuilder AddAzureTableStores<TContext>(this IdentityBuilder builder, 
            Func<IdentityConfiguration> configAction,
            Func<TableServiceClient> tableServiceClientAction,
            IKeyHelper? keyHelper = null)
            where TContext : IdentityCloudContext
        {
             return builder.AddAzureTableStores<TContext>(_ => configAction(), _ => tableServiceClientAction(), keyHelper);
        }

        /// <summary>
        /// Use this to load and configure the Identity Azure Tables into the aspnet identity pipeline.
        /// Note: <see cref="IdentityBuilder.AddRoles{TRole}"/> prior to calling this method in the pipeline if you need Roles functionality, otherwise the RoleStore will not be loaded.
        /// </summary>
        /// <typeparam name="TContext">Use or extend <see cref="IdentityCloudContext"/></typeparam>
        /// <param name="builder"><see cref="IdentityBuilder"/> aspnet identity pipeline</param>
        /// <param name="configAction"><see cref="IdentityConfiguration"/></param>
        /// <param name="tableServiceClientAction"><see cref="TableServiceClient"/></param>
        /// <param name="keyHelper">Use <see cref="DefaultKeyHelper"/> that uses SHA1, <see cref="SHA256KeyHelper"/> or a custom keyhelper that implements <see cref="IKeyHelper"/> </param>
        /// <returns><see cref="IdentityBuilder"/></returns>
        public static IdentityBuilder AddAzureTableStores<TContext>(this IdentityBuilder builder,
            Func<IServiceProvider, IdentityConfiguration> configAction,
            Func<IServiceProvider, TableServiceClient> tableServiceClientAction,
            IKeyHelper? keyHelper = null)
            where TContext : IdentityCloudContext
        {
            ArgumentNullException.ThrowIfNull(configAction, nameof(configAction));
            ArgumentNullException.ThrowIfNull(tableServiceClientAction, nameof(tableServiceClientAction));

            builder.Services.AddSingleton<IKeyHelper>(keyHelper ?? new DefaultKeyHelper());

            builder.Services.AddSingleton<IdentityConfiguration>(configAction);
                
            Type contextType = typeof(TContext);

            builder.Services.AddKeyedSingleton<TableServiceClient>(IdentityAzureTableServiceClientKey, (sp, o) => tableServiceClientAction(sp));
            builder.Services.AddSingleton(contextType, sp => 
            { 
                return Activator.CreateInstance(contextType, [sp.GetRequiredService<IdentityConfiguration>(), sp.GetRequiredKeyedService<TableServiceClient>(IdentityAzureTableServiceClientKey)]) as TContext
                    ?? throw new InvalidOperationException($"Unable to create instance of {contextType.FullName}. Must have a constructor that accepts {nameof(IdentityConfiguration)}, {nameof(TableServiceClient)}");
            });


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
