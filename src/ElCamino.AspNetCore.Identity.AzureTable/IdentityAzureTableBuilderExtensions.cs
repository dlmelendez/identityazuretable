// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Identity;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using ElCamino.AspNetCore.Identity.AzureTable;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IdentityAzureTableBuilderExtensions
    {
        /// <summary>
        /// Call .AddRoles<IdentityRole>() in the pipeline if you need Roles functionality, otherwise the RoleStore will not be loaded.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="builder"></param>
        /// <param name="configAction"></param>
        /// <returns></returns>
        public static IdentityBuilder AddAzureTableStores<TContext>(this IdentityBuilder builder, Func<IdentityConfiguration> configAction)
            where TContext : IdentityCloudContext, new()
        {
            builder.Services.AddSingleton<IKeyHelper>(new DefaultKeyHelper());

            builder.Services.AddSingleton<IdentityConfiguration>(new Func<IServiceProvider, IdentityConfiguration>(p => configAction()));

            Type contextType = typeof(TContext);
            builder.Services.AddScoped(contextType, contextType);

            Type userStoreType = builder.RoleType != null ? typeof(UserStore<,,>).MakeGenericType(builder.UserType, builder.RoleType, contextType)
                : typeof(UserOnlyStore<,>).MakeGenericType(builder.UserType, contextType);

            builder.Services.AddScoped(
                typeof(IUserStore<>).MakeGenericType(builder.UserType),
                userStoreType);

            if (builder.RoleType != null)
            {
                Type roleStoreType = typeof(RoleStore<,>).MakeGenericType(builder.RoleType, contextType);

                builder.Services.AddScoped(
                    typeof(IRoleStore<>).MakeGenericType(builder.RoleType), roleStoreType);
            }
            return builder;
        }

        public static IdentityBuilder CreateAzureTablesIfNotExists<TContext>(this IdentityBuilder builder)
            where TContext : IdentityCloudContext, new()
        {
            Type contextType = typeof(TContext);
            Type userStoreType = typeof(IUserStore<>).MakeGenericType(builder.UserType);

            var userStore = ActivatorUtilities.GetServiceOrCreateInstance(builder.Services.BuildServiceProvider(),
                userStoreType) as dynamic;

            userStore.CreateTablesIfNotExistsAsync();

            return builder;
        }
    }
}
