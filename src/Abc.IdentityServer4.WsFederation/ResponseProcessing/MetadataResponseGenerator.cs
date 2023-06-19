// ----------------------------------------------------------------------------
// <copyright file="MetadataResponseGenerator.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Xml;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Abc.IdentityServer.WsFederation.ResponseProcessing
{
    public class MetadataResponseGenerator : IMetadataResponseGenerator
    {
        private readonly IKeyMaterialService _keys;
        private readonly WsFederationOptions _options;
        private readonly IHttpContextAccessor _contextAccessor;

        public MetadataResponseGenerator(IHttpContextAccessor contextAccessor, IKeyMaterialService keys, WsFederationOptions options)
        {
            _keys = keys;
            _options = options;
            _contextAccessor = contextAccessor;
        }

        public async Task<WsFederationConfigurationEx> GenerateAsync()
        {
            var signingKey = (await _keys.GetSigningCredentialsAsync()).Key as X509SecurityKey;
            if (signingKey == null)
            {
                throw new InvalidOperationException("Missing signing key");
            }

            var cert = signingKey.Certificate;
            var issuer = _contextAccessor.HttpContext.GetIdentityServerIssuerUri();
            var baseUrl = _contextAccessor.HttpContext.GetIdentityServerBaseUrl();
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256Signature, SecurityAlgorithms.Sha256Digest);
            var config = new WsFederationConfigurationEx()
            {
                Issuer = issuer,
                TokenEndpoint = baseUrl + WsFederationConstants.ProtocolRoutePaths.WsFederation.EnsureLeadingSlash(),
                SigningCredentials = signingCredentials,
            };

            config.SigningKeys.Add(signingKey);
            config.KeyInfos.Add(new KeyInfo(cert));

            foreach (var token in _options.SecurityTokenHandlers.Select(x => x.TokenType))
            {
                var tokenType = WsFederationConstants.TokenTypeMap.FirstOrDefault(x => x.Value == token);
                if (tokenType.Key != null)
                {
                    config.TokenTypesOffered.Add(tokenType.Key);
                }
            }

            return config;
        }
    }
}