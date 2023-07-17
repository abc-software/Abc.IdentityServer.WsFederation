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
using Microsoft.IdentityModel.Protocols.WsFederation;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Saml2;
using Microsoft.IdentityModel.Xml;
using Microsoft.VisualBasic;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Abc.IdentityServer.WsFederation.ResponseProcessing
{
    public class MetadataResponseGenerator : IMetadataResponseGenerator
    {
        private readonly IdentityServerOptions _options;
        private readonly IResourceStore _resources;
        private readonly Services.IClaimsService _claims;
        private readonly IKeyMaterialService _keys;
        private readonly WsFederationOptions _wsFederationOptions;
        private readonly IHttpContextAccessor _contextAccessor;

        public MetadataResponseGenerator(
            IdentityServerOptions options,
            IResourceStore resources,
            Services.IClaimsService claims,
            IHttpContextAccessor contextAccessor,
            IKeyMaterialService keys,
            WsFederationOptions wsFederationOptions)
        {
            _keys = keys;
            _wsFederationOptions = wsFederationOptions;
            _options = options;
            _resources = resources;
            _claims = claims;
            _contextAccessor = contextAccessor;
        }

        public virtual async Task<DescriptorBase> GenerateAsync()
        {
            var signingKey = await _keys.GetX509SigningKeyAsync();

            var issuer = _contextAccessor.HttpContext.GetIdentityServerIssuerUri();
            var baseUrl = _contextAccessor.HttpContext.GetIdentityServerBaseUrl();
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