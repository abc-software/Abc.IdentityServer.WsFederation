// ----------------------------------------------------------------------------
// <copyright file="MetadataResponseGenerator.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityModel.Metadata;
using Abc.IdentityServer.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Xml;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Abc.IdentityServer.WsFederation.ResponseProcessing
{
    /// <summary>
    /// Default implementation of the meta data endpoint response generator.
    /// </summary>
    public class MetadataResponseGenerator : IMetadataResponseGenerator
    {
        private readonly IdentityServerOptions _options;
        private readonly IResourceStore _resources;
        private readonly Services.IClaimsService _claims;
        private readonly IServerUrls _urls;
        private readonly IKeyMaterialService _keys;
        private readonly WsFederationOptions _wsFederationOptions;
        private readonly IIssuerNameService _issuerNameService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataResponseGenerator"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="resources">The resource store.</param>
        /// <param name="claimsService">The claims service.</param>
        /// <param name="issuerNameService">The issuer name service.</param>
        /// <param name="urls">The server urls.</param>
        /// <param name="keys">The keys.</param>
        /// <param name="wsFederationOptions">The WS-Federation options.</param>
        public MetadataResponseGenerator(
            IdentityServerOptions options,
            IResourceStore resources,
            Services.IClaimsService claimsService,
            IIssuerNameService issuerNameService,
            IServerUrls urls,
            IKeyMaterialService keys,
            WsFederationOptions wsFederationOptions)
        {
            _keys = keys;
            _wsFederationOptions = wsFederationOptions;
            _options = options;
            _resources = resources;
            _claims = claimsService;
            _issuerNameService = issuerNameService;
            _urls = urls;
        }

        /// <inheritdoc/>
        public virtual async Task<DescriptorBase> GenerateAsync()
        {
            var signingKey = await _keys.GetX509SigningKeyAsync();

            var issuer = await _issuerNameService.GetCurrentAsync();
            var baseUrl = _urls.BaseUrl;
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256Signature, SecurityAlgorithms.Sha256Digest);

            var entityDescriptor = new EntityDescriptor(new EntityId(issuer));
            var descriptor = new SecurityTokenServiceDescriptor();
            descriptor.ProtocolsSupported.Add(new Uri(Microsoft.IdentityModel.Protocols.WsFederation.WsFederationConstants.Namespace));

            if (_options.Discovery.ShowKeySet)
            {
                var keyDescriptor = new KeyDescriptor(new KeyInfo(signingKey.Certificate))
                {
                    Use = KeyType.Signing,
                };

                descriptor.KeyDescriptors.Add(keyDescriptor);
            }

            if (_options.Discovery.ShowEndpoints)
            {
                var address = baseUrl + WsFederationConstants.ProtocolRoutePaths.WsFederation.EnsureLeadingSlash();
                descriptor.PassiveRequestorEndpoints.Add(new EndpointReference(address));
                descriptor.SecurityTokenServiceEndpoints.Add(new EndpointReference(address));
            }

            foreach (var token in _wsFederationOptions.SecurityTokenHandlers.Select(x => x.TokenType))
            {
                var tokenType = WsFederationConstants.TokenTypeMap.FirstOrDefault(x => x.Value == token);
                if (tokenType.Key != null)
                {
                    descriptor.TokenTypesOffered.Add(new Uri(tokenType.Key));
                }
            }

            if (_options.Discovery.ShowClaims)
            {
                var resources = await _resources.GetAllEnabledResourcesAsync();

                // exclude standard OIDC identity resources
                var oidcResources = new[] { Ids.IdentityServerConstants.StandardScopes.OpenId, Ids.IdentityServerConstants.StandardScopes.Profile };
                var claims = resources
                    .IdentityResources.Where(x => x.ShowInDiscoveryDocument && !oidcResources.Contains(x.Name))
                .SelectMany(x => x.UserClaims)
                .Distinct()
                    .Select(c => new Claim(c, string.Empty));

                var mappedClaims = _claims.MapClaims(_wsFederationOptions.DefaultClaimMapping, null, claims);
                foreach (var item in mappedClaims)
                {
                    descriptor.ClaimTypesOffered.Add(new ClaimType(new Uri(item.Type)));
                }
            }

            entityDescriptor.RoleDescriptors.Add(descriptor);
            entityDescriptor.SigningCredentials = signingCredentials;
            return entityDescriptor;
        }
    }
}