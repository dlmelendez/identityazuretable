// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

#if !net45
using System;
#if net45
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Builder;
using ElCamino.AspNet.Identity.AzureTable;
using ElCamino.AspNet.Identity.AzureTable.Model;
#else
using Microsoft.AspNetCore.Identity;
using ElCamino.AspNetCore.Identity.AzureTable.Model;
using ElCamino.AspNetCore.Identity.AzureTable;
#endif

namespace Microsoft.Extensions.DependencyInjection
{
	public static class IdentityAzureTableBuilderExtensions
	{

		public static IdentityBuilder AddAzureTableStores<TContext>(this IdentityBuilder builder, Func<IdentityConfiguration> configAction)
			where TContext : IdentityCloudContext, new()
		{
			builder.Services.AddSingleton<IdentityConfiguration>(new Func<IServiceProvider, IdentityConfiguration>(p=> configAction()));

            Type contextType = typeof(TContext);
            Type userStoreType = typeof(UserStore<,,>).MakeGenericType(builder.UserType, builder.RoleType, contextType);
            Type roleStoreType = typeof(RoleStore<,>).MakeGenericType(builder.RoleType, contextType);

            builder.Services.AddScoped(contextType, contextType);

            builder.Services.AddScoped(
                typeof(IUserStore<>).MakeGenericType(builder.UserType),
                userStoreType);
            builder.Services.AddScoped(
                typeof(IRoleStore<>).MakeGenericType(builder.RoleType),
                roleStoreType);

            return builder;
		}

		public static IdentityBuilder CreateAzureTablesIfNotExists<TContext>(this IdentityBuilder builder)
            where TContext : IdentityCloudContext, new()
        {
            Type contextType = typeof(TContext);
            Type userStoreType = typeof(UserStore<,,>).MakeGenericType(builder.UserType, builder.RoleType, contextType);

            var userStore = ActivatorUtilities.GetServiceOrCreateInstance(builder.Services.BuildServiceProvider(),
                userStoreType) as dynamic;
            
            userStore.CreateTablesIfNotExists();

            return builder;
            
        }
    }
}
#endif