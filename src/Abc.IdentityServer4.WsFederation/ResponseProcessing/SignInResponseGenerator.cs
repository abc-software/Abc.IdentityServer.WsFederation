﻿// ----------------------------------------------------------------------------
// <copyright file="SignInResponseGenerator.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer.Extensions;
using Abc.IdentityServer.WsFederation.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Saml;
using Microsoft.IdentityModel.Tokens.Saml2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Abc.IdentityServer.WsFederation.ResponseProcessing
{
    /// <summary>
    /// The sign-in response generator.
    /// </summary>
    public class SignInResponseGenerator : ISignInResponseGenerator
    {
        private readonly WsFederationOptions _options;
        private readonly Services.IClaimsService _claimsService;
        private readonly IKeyMaterialService _keys;
        private readonly IResourceStore _resources;
        private readonly IIssuerNameService _issuerNameService;
        private readonly IClock _clock;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SignInResponseGenerator"/> class.
        /// </summary>
        /// <param name="contextAccessor">The context accessor.</param>
        /// <param name="options">The WS-Federation options.</param>
        /// <param name="claimsService">The claims service.</param>
        /// <param name="keys">The keys.</param>
        /// <param name="resources">The resource store.</param>
        /// <param name="issuerNameService">The issuer name service.</param>
        /// <param name="clock">The clock.</param>
        /// <param name="logger">The logger.</param>
        public SignInResponseGenerator(
            WsFederationOptions options,
            Services.IClaimsService claimsService,
            IKeyMaterialService keys,
            IResourceStore resources,
            IIssuerNameService issuerNameService,
            IClock clock,
            ILogger<SignInResponseGenerator> logger)
        {
            _options = options;
            _claimsService = claimsService;
            _keys = keys;
            _resources = resources;
            _issuerNameService = issuerNameService;
            _clock = clock;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<WsFederationMessage> GenerateResponseAsync(WsFederationValidationResult validationResult)
        {
            _logger.LogDebug("Creating WS-Federation signin response");

            var outgoingSubject = await CreateSubjectAsync(validationResult);

            return await CreateResponseAsync(validationResult.ValidatedRequest, outgoingSubject);
        }

        /// <summary>
        /// Creates the subject.
        /// </summary>
        /// <param name="result">The WS-Federation validation result.</param>
        /// <returns>The subject.</returns>
        protected virtual async Task<ClaimsIdentity> CreateSubjectAsync(WsFederationValidationResult result)
        {
            var validatedRequest = result.ValidatedRequest;
            var requestedClaimTypes = await GetRequestedClaimTypesAsync(validatedRequest.ValidatedResources.ParsedScopes.Select(x => x.ParsedName));
            var relyingParty = validatedRequest.RelyingParty;

            var issuedClaims = await _claimsService.GetClaimsAsync(validatedRequest, requestedClaimTypes);

            var tokenType = relyingParty?.TokenType ?? _options.DefaultTokenType;
            var claimMapping =
                relyingParty?.ClaimMapping != null && relyingParty.ClaimMapping.Any()
                ? relyingParty.ClaimMapping
                : _options.DefaultClaimMapping;

            var outboundClaims = new List<Claim>();
            outboundClaims.AddRange(_claimsService.MapClaims(claimMapping, tokenType, issuedClaims));

            if (!outboundClaims.Exists(x => x.Type == ClaimTypes.NameIdentifier))
            {
                var nameid = new Claim(ClaimTypes.NameIdentifier, validatedRequest.Subject.GetSubjectId());
                nameid.Properties[Microsoft.IdentityModel.Tokens.Saml.ClaimProperties.SamlNameIdentifierFormat] =
                    validatedRequest.RelyingParty?.NameIdentifierFormat ?? _options.DefaultNameIdentifierFormat;
                outboundClaims.Add(nameid);
            }

            // The AuthnStatement statement generated from the following 2
            // claims is mandatory for some service providers (i.e. Shibboleth-Sp). 
            // The value of the AuthenticationMethod claim must be one of the constants in
            // System.IdentityModel.Tokens.AuthenticationMethods.
            // Password is the only one that can be directly matched, everything
            // else defaults to Unspecified.
            if (!outboundClaims.Exists(x => x.Type == ClaimTypes.AuthenticationMethod))
            {
                var authenticationMethod = validatedRequest.Subject.GetAuthenticationMethod() == OidcConstants.AuthenticationMethods.Password
                        ? SamlConstants.AuthenticationMethods.PasswordString
                        : SamlConstants.AuthenticationMethods.UnspecifiedString;
                outboundClaims.Add(new Claim(ClaimTypes.AuthenticationMethod, authenticationMethod));
            }

            // authentication instant claim is required
            if (!outboundClaims.Exists(x => x.Type == ClaimTypes.AuthenticationInstant))
            {
                outboundClaims.Add(new Claim(ClaimTypes.AuthenticationInstant, validatedRequest.Subject.GetAuthenticationTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), ClaimValueTypes.DateTime));
            }

            return new ClaimsIdentity(outboundClaims, "idsrv");
        }

        /// <summary>
        /// Get requested claim types.
        /// </summary>
        /// <param name="scopes">The requested scopes.</param>
        /// <returns>The claim types.</returns>
        protected virtual async Task<IList<string>> GetRequestedClaimTypesAsync(IEnumerable<string> scopes)
        {
            var requestedClaimTypes = new List<string>();

            var resources = await _resources.FindEnabledIdentityResourcesByScopeAsync(scopes);
            foreach (var resource in resources)
            {
                foreach (var claim in resource.UserClaims)
                {
                    requestedClaimTypes.Add(claim);
                }
            }

            return requestedClaimTypes;
        }

        private async Task<WsFederationMessage> CreateResponseAsync(ValidatedWsFederationRequest validatedRequest, ClaimsIdentity outgoingSubject)
        {
            var signingKey = await _keys.GetX509SigningKeyAsync();

            var issueInstant = _clock.UtcNow.UtcDateTime;
            var signingCredentials = new SigningCredentials(
                signingKey,
                validatedRequest.RelyingParty?.SignatureAlgorithm ?? _options.DefaultSignatureAlgorithm,
                validatedRequest.RelyingParty?.DigestAlgorithm ?? _options.DefaultDigestAlgorithm);

            var descriptor = new SecurityTokenDescriptor
            {
                Audience = validatedRequest.Client.ClientId,
                IssuedAt = issueInstant,
                NotBefore = issueInstant,
                Expires = issueInstant.AddSeconds(validatedRequest.Client.IdentityTokenLifetime),
                SigningCredentials = signingCredentials,
                Subject = outgoingSubject,
                Issuer = await _issuerNameService.GetCurrentAsync(),
                TokenType = validatedRequest.RelyingParty?.TokenType ?? _options.DefaultTokenType,
            };

            if (validatedRequest.RelyingParty?.EncryptionCertificate != null)
            {
                descriptor.EncryptingCredentials = new X509EncryptingCredentials(
                    validatedRequest.RelyingParty.EncryptionCertificate,
                    validatedRequest.RelyingParty.KeyWrapAlgorithm ?? _options.DefaultKeyWrapAlgorithm,
                    validatedRequest.RelyingParty.EncryptionAlgorithm ?? _options.DefaultEncryptionAlgorithm);
            }

            WsFederationConstants.TokenTypeMap.TryGetValue(descriptor.TokenType, out var securityTokenType);
            var handler = _options.SecurityTokenHandlers.FirstOrDefault(x => x.TokenType == securityTokenType);
            if (handler is null)
            {
                throw new InvalidOperationException($"TokenType: {descriptor.TokenType} not supported.");
            }

            var token = CreateToken(handler, descriptor);
            return CreateResponse(validatedRequest, token, handler, descriptor.TokenType);
        }

        private SecurityToken CreateToken(SecurityTokenHandler handler, SecurityTokenDescriptor descriptor)
        {
            if (descriptor.Subject.HasClaim(c => c.Type == ClaimTypes.AuthenticationMethod) &&
                descriptor.Subject.HasClaim(c => c.Type == ClaimTypes.AuthenticationInstant))
            {
                // if we have authentication information set via claims
                // create AuthenticationInformation from the corresponding namespaces
                // and pass it to the right handler
                var authMethod = descriptor.Subject.Claims.Single(x => x.Type == ClaimTypes.AuthenticationMethod).Value;
                var authTime = descriptor.Subject.Claims.Single(x => x.Type == ClaimTypes.AuthenticationInstant).Value;
                if (handler is SamlSecurityTokenHandler samlSecurityTokenHandler)
                {
                    var auth = new Microsoft.IdentityModel.Tokens.Saml.AuthenticationInformation(new Uri(authMethod), DateTime.Parse(authTime));
                    return samlSecurityTokenHandler.CreateToken(descriptor, auth);
                }

                if (handler is Saml2SecurityTokenHandler saml2SecurityTokenHandler)
                {
                    var auth = new Microsoft.IdentityModel.Tokens.Saml2.AuthenticationInformation(new Uri(authMethod), DateTime.Parse(authTime));
                    return saml2SecurityTokenHandler.CreateToken(descriptor, auth);
                }
            }

            return handler.CreateToken(descriptor);
        }

        private WsFederationMessage CreateResponse(ValidatedWsFederationRequest validatedRequest, SecurityToken token, SecurityTokenHandler handler, string tokenType)
        {
            var rstr = new RequestSecurityTokenResponse
            {
                CreatedAt = token.ValidFrom,
                ExpiresAt = token.ValidTo,
                AppliesTo = validatedRequest.Client.ClientId,
                Context = validatedRequest.WsFederationMessage.Wctx,
                RequestedSecurityToken = token,
                TokenType = tokenType,
            };

            var trustVersion = validatedRequest.RelyingParty?.WsTrustVersion ?? WsTrustVersion.Default;
            if (trustVersion == WsTrustVersion.Default)
            {
                trustVersion = _options.DefaultWsTrustVersion;
            }

            var responseMessage = new WsFederationMessage
            {
                IssuerAddress = validatedRequest.ReplyUrl,
                Wa = Microsoft.IdentityModel.Protocols.WsFederation.WsFederationConstants.WsFederationActions.SignIn,
                Wresult = rstr.Serialize(handler, trustVersion),
                Wctx = validatedRequest.WsFederationMessage.Wctx,
            };

            return responseMessage;
        }
    }
}