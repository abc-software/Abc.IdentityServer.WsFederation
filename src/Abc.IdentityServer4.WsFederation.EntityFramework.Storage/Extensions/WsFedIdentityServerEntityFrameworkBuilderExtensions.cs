// ----------------------------------------------------------------------------
// <copyright file="WsFedIdentityServerEntityFrameworkBuilderExtensions.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer.WsFederation.EntityFramework.DbContext;
using Abc.IdentityServer.WsFederation.EntityFramework.Interfaces;
using Abc.IdentityServer.WsFederation.EntityFramework.Options;
using Abc.IdentityServer.WsFederation.EntityFramework.Stores;
using Abc.IdentityServer.WsFederation.Stores;
using Microsoft.EntityFrameworkCore;
using System;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to add EF database support to IdentityServer.
/// </summary>
public static class WsFedIdentityServerEntityFrameworkBuilderExtensions
{
    /// <summary>
    /// Configures EF implementation of IClientStore, IResourceStore, and ICorsPolicyService with IdentityServer.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="storeOptionsAction">The store options action.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IIdentityServerBuilder AddWsFedConfigurationStore(
        this IIdentityServerBuilder builder,
        Action<WsFedConfigurationStoreOptions> storeOptionsAction = null)
    {
        return builder.AddWsFedConfigurationStore<WsFedConfigurationDbContext>(storeOptionsAction);
    }

    /// <summary>
    /// Configures EF implementation of IClientStore, IResourceStore, and ICorsPolicyService with IdentityServer.
    /// </summary>
    /// <typeparam name="TContext">The IConfigurationDbContext to use.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="storeOptionsAction">The store options action.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IIdentityServerBuilder AddWsFedConfigurationStore<TContext>(
        this IIdentityServerBuilder builder,
        Action<WsFedConfigurationStoreOptions> storeOptionsAction = null)
        where TContext : DbContext, IConfigurationDbContext, IWsFedConfigurationDbContext
    {
        var options = new WsFedConfigurationStoreOptions();
        builder.Services.AddSingleton(options);
        storeOptionsAction?.Invoke(options);

        builder.AddConfigurationStore<TContext>(options.Apply);

        builder.Services.AddTransient<IRelyingPartyStore, RelyingPartyStore>();
        builder.Services.AddScoped<IWsFedConfigurationDbContext>(svcs => svcs.GetRequiredService<TContext>());

        return builder;
    }
}