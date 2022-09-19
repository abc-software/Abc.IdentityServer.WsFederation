// ----------------------------------------------------------------------------
// <copyright file="WsFederationBuilderExtensions.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer4.Extensions;
using Abc.IdentityServer4.WsFederation;
using Abc.IdentityServer4.WsFederation.Endpoints;
using Abc.IdentityServer4.WsFederation.ResponseProcessing;
using Abc.IdentityServer4.WsFederation.Services;
using Abc.IdentityServer4.WsFederation.Stores;
using Abc.IdentityServer4.WsFederation.Validation;
using IdentityServer4.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for the IdentityServer builder.
    /// </summary>
    public static class WsFederationBuilderExtensions
    {
        /// <summary>
        /// Adds the WS-federation service provider.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public static IIdentityServerBuilder AddWsFederation(this IIdentityServerBuilder builder)
        {
            return AddWsFederation<NoRelyingPartyStore>(builder);
        }

        /// <summary>
        /// Adds the WS-federation service provider with specified store.
        /// </summary>
        /// <typeparam name="TStore">The store type.</typeparam>
        /// <param name="builder">The builder.</param>
        public static IIdentityServerBuilder AddWsFederation<TStore>(this IIdentityServerBuilder builder) 
            where TStore : class, IRelyingPartyStore
        {
            builder.Services.AddSingleton(
                resolver => resolver.GetRequiredService<IOptions<WsFederationOptions>>().Value);

            builder.Services.AddTransient<IMetadataResponseGenerator, MetadataResponseGenerator>();
            builder.Services.AddTransient<ISignInResponseGenerator, SignInResponseGenerator>();
            builder.Services.AddTransient<IWsFederationRequestValidator, WsFederationRequestValidator>();
            builder.Services.AddTransient<ISignInInteractionResponseGenerator, SignInInteractionResponseGenerator>();
            builder.Services.AddTransient<IdentityServer4.Services.IReturnUrlParser, WsFederationReturnUrlParser>();
            builder.Services.AddTransient<IClaimsService, DefaultClaimsService>();
            builder.Services.TryAddTransient<IRelyingPartyStore, TStore>();

            builder.AddEndpoint<WsFederationEndpoint>(WsFederationConstants.EndpointNames.WsFederation, WsFederationConstants.ProtocolRoutePaths.WsFederation.EnsureLeadingSlash());
            builder.AddEndpoint<WsFederationCallbackEndpoint>(WsFederationConstants.EndpointNames.WsFederationCallback, WsFederationConstants.ProtocolRoutePaths.WsFederationCallback.EnsureLeadingSlash());
            builder.AddEndpoint<WsFederationMetadataEndpoint>(WsFederationConstants.EndpointNames.Metadata, WsFederationConstants.ProtocolRoutePaths.Metadata.EnsureLeadingSlash());
            return builder;
        }

        /// <summary>
        /// Adds the WS-federation service provider with setup action.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="setupAction">The setup action.</param>
        public static IIdentityServerBuilder AddWsFederation(this IIdentityServerBuilder builder, Action<WsFederationOptions> setupAction)
        {
            builder.Services.Configure(setupAction);
            return builder.AddWsFederation();
        }

        /// <summary>
        /// Adds the WS-federation service provider with configuration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="configuration">The configuration.</param>
        public static IIdentityServerBuilder AddWsFederation(this IIdentityServerBuilder builder, IConfiguration configuration)
        {
            builder.Services.Configure<WsFederationOptions>(configuration);
            return builder.AddWsFederation();
        }

        public static IIdentityServerBuilder AddInMemoryRelyingParties(this IIdentityServerBuilder builder, IEnumerable<RelyingParty> relyingParties)
        {
            builder.Services.AddSingleton(relyingParties);
            builder.Services.AddSingleton<IRelyingPartyStore, InMemoryRelyingPartyStore>();
            return builder;
        }

        public static IIdentityServerBuilder AddRelyingPartyStore<T>(this IIdentityServerBuilder builder)
            where T : class, IRelyingPartyStore
        {
            builder.Services.AddTransient<IRelyingPartyStore, T>();

            return builder;
        }

        public static IIdentityServerBuilder AddRelyingPartyStoreCache<T>(this IIdentityServerBuilder builder)
            where T : class, IRelyingPartyStore
        {
            builder.Services.TryAddTransient(typeof(T));
            builder.Services.AddTransient<IRelyingPartyStore, CachingRelyingPartyStore<T>>();

            return builder;
        }
    }
}