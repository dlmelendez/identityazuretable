// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Identity;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using ElCamino.AspNetCore.Identity.AzureTable;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IdentityAzureTableBuilderExtensions
    {
        [Obsolete("AddAzureTableStoresV2 will be renamed AddAzureTableStores in a future version. Use AddAzureTableStoresV2.")]
        public static IdentityBuilder AddAzureTableStores<TContext>(this IdentityBuilder builder, Func<IdentityConfiguration> configAction)
            where TContext : IdentityCloudContext, new()
        {
            builder.Services.AddSingleton<IdentityConfiguration>(new Func<IServiceProvider, IdentityConfiguration>(p => configAction()));

            Type contextType = typeof(TContext);
            Type userStoreType = typeof(UserStore<,,>).MakeGenericType(builder.UserType, builder.RoleType ?? typeof(ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityRole), contextType);
            Type roleStoreType = typeof(RoleStore<,>).MakeGenericType(builder.RoleType ?? typeof(ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityRole), contextType);

            builder.Services.AddScoped(contextType, contextType);

            builder.Services.AddScoped(
                typeof(IUserStore<>).MakeGenericType(builder.UserType),
                userStoreType);
            builder.Services.AddScoped(
                typeof(IRoleStore<>).MakeGenericType(builder.RoleType ?? typeof(ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityRole)),
                roleStoreType);

            return builder;
        }

        public static IdentityBuilder AddAzureTableStoresV2<TContext>(this IdentityBuilder builder, Func<IdentityConfiguration> configAction)
            where TContext : IdentityCloudContext, new()
        {
            builder.Services.AddSingleton<IdentityConfiguration>(new Func<IServiceProvider, IdentityConfiguration>(p => configAction()));

            Type contextType = typeof(TContext);
            Type userStoreType = typeof(UserStoreV2<,,>).MakeGenericType(builder.UserType, builder.RoleType ?? typeof(ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityRole), contextType);
            Type roleStoreType = typeof(RoleStore<,>).MakeGenericType(builder.RoleType??typeof(ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityRole), contextType);

            builder.Services.AddScoped(contextType, contextType);

            builder.Services.AddScoped(
                typeof(IUserStore<>).MakeGenericType(builder.UserType),
                userStoreType);
            builder.Services.AddScoped(
                typeof(IRoleStore<>).MakeGenericType(builder.RoleType ?? typeof(ElCamino.AspNetCore.Identity.AzureTable.Model.IdentityRole)),
                roleStoreType);

            return builder;
        }

        public static IdentityBuilder CreateAzureTablesIfNotExists<TContext>(this IdentityBuilder builder)
            where TContext : IdentityCloudContext, new()
        {
            Type contextType = typeof(TContext);
            Type userStoreType = typeof(IUserStore<>).MakeGenericType(builder.UserType);

            var userStore = ActivatorUtilities.GetServiceOrCreateInstance(builder.Services.BuildServiceProvider(),
                userStoreType) as dynamic;

            userStore.CreateTablesIfNotExists();

            return builder;
        }
    }
}
