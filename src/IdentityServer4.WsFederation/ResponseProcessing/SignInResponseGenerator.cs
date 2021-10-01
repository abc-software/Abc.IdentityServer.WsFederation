// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.WsFederation.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Saml;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.IdentityModel.Tokens.Saml2;
using IdentityServer4.WsFederation.Services;
using Microsoft.AspNetCore.Authentication;
using IdentityServer4.WsFederation.Stores;

namespace IdentityServer4.WsFederation
{
    public class SignInResponseGenerator : ISignInResponseGenerator
    {
        private readonly IdentityServerOptions _options;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IProfileService _profile;
        private readonly IKeyMaterialService _keys;
        private readonly IResourceStore _resources;
        private readonly ISystemClock _clock;
        private readonly ISecurityTokenHandlerFactory _securityTokenHandlerFactory;
        private readonly ILogger<SignInResponseGenerator> _logger;

        public SignInResponseGenerator(
            IHttpContextAccessor contextAccessor,
            IdentityServerOptions options,
            IProfileService profile,
            IKeyMaterialService keys,
            IResourceStore resources,
            ISystemClock clock,
            ISecurityTokenHandlerFactory securityTokenHandlerFactory,
            ILogger<SignInResponseGenerator> logger)
        {
            _contextAccessor = contextAccessor;
            _options = options;
            _profile = profile;
            _keys = keys;
            _resources = resources;
            _clock = clock;
            _securityTokenHandlerFactory = securityTokenHandlerFactory;
            _logger = logger;
        }

        public async Task<WsFederationMessage> GenerateResponseAsync(SignInValidationResult validationResult)
        {
            _logger.LogDebug("Creating WS-Federation signin response");

            // create subject
            var outgoingSubject = await CreateSubjectAsync(validationResult);

            // create token for user
            var token = await CreateSecurityTokenAsync(validationResult, outgoingSubject);

            // return response
            return CreateResponse(validationResult, token);
        }

        protected virtual async Task<ClaimsIdentity> CreateSubjectAsync(SignInValidationResult result)
        {
            var requestedClaimTypes = await GetRequestedClaimTypesAsync(result.Client.AllowedScopes);
            var issuedClaims = await GetIssuedClaimsAsync(result, requestedClaimTypes);

            // map outbound claims
            var nameid = new Claim(ClaimTypes.NameIdentifier, result.User.GetSubjectId());
            nameid.Properties[Microsoft.IdentityModel.Tokens.Saml.ClaimProperties.SamlNameIdentifierFormat] = result.RelyingParty.SamlNameIdentifierFormat;

            var outboundClaims = new List<Claim> { nameid };
            outboundClaims.AddRange(MapClaims(result.RelyingParty, issuedClaims));

            // The AuthnStatement statement generated from the following 2
            // claims is mandatory for some service providers (i.e. Shibboleth-Sp). 
            // The value of the AuthenticationMethod claim must be one of the constants in
            // System.IdentityModel.Tokens.AuthenticationMethods.
            // Password is the only one that can be directly matched, everything
            // else defaults to Unspecified.
            if (result.User.GetAuthenticationMethod() == OidcConstants.AuthenticationMethods.Password)
            {
                outboundClaims.Add(new Claim(ClaimTypes.AuthenticationMethod, SamlConstants.AuthenticationMethods.PasswordString));
            }
            else
            {
                outboundClaims.Add(new Claim(ClaimTypes.AuthenticationMethod, SamlConstants.AuthenticationMethods.UnspecifiedString));
            }

            // authentication instant claim is required
            outboundClaims.Add(new Claim(ClaimTypes.AuthenticationInstant, XmlConvert.ToString(result.User.GetAuthenticationTime(), "yyyy-MM-ddTHH:mm:ss.fffZ"), ClaimValueTypes.DateTime));

            return new ClaimsIdentity(outboundClaims, "idsrv");
        }

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

        protected virtual async Task<IList<Claim>> GetIssuedClaimsAsync(SignInValidationResult validationResult, IEnumerable<string> requestedClaimTypes)
        {
            var ctx = new ProfileDataRequestContext(validationResult.User, validationResult.Client, "WS-Federation", requestedClaimTypes);
            await _profile.GetProfileDataAsync(ctx);
            return ctx.IssuedClaims;
        }

        protected virtual IEnumerable<Claim> MapClaims(RelyingParty relyingParty, IEnumerable<Claim> claims)
        {
            var outboundClaims = new List<Claim>();
            foreach (var claim in claims)
            {
                if (relyingParty.ClaimMapping.ContainsKey(claim.Type))
                {
                    var outboundClaim = new Claim(relyingParty.ClaimMapping[claim.Type], claim.Value);
                    if (outboundClaim.Type == ClaimTypes.NameIdentifier)
                    {
                        outboundClaim.Properties[Microsoft.IdentityModel.Tokens.Saml.ClaimProperties.SamlNameIdentifierFormat] = relyingParty.SamlNameIdentifierFormat;
                        outboundClaims.RemoveAll(c => c.Type == ClaimTypes.NameIdentifier); //remove previously added nameid claim
                    }

                    outboundClaims.Add(outboundClaim);
                }
                else if (relyingParty.TokenType != WsFederationConstants.TokenTypes.Saml11TokenProfile11)
                {
                    outboundClaims.Add(claim);
                }
                else
                {
                    _logger.LogInformation("No explicit claim type mapping for {claimType} configured. Saml11 requires a URI claim type. Skipping.", claim.Type);
                }
            }

            return outboundClaims;
        }

        private async Task<SecurityToken> CreateSecurityTokenAsync(SignInValidationResult result, ClaimsIdentity outgoingSubject)
        {
            var credential = await _keys.GetSigningCredentialsAsync();
            var key = credential.Key as Microsoft.IdentityModel.Tokens.X509SecurityKey;
            if (key == null)
            {
                throw new InvalidOperationException("Missing signing key");
            }

            var issueInstant = _clock.UtcNow.DateTime;
            var descriptor = new SecurityTokenDescriptor
            {
                Audience = result.Client.ClientId,
                IssuedAt = issueInstant,
                NotBefore = issueInstant,
                Expires = issueInstant.AddSeconds(result.Client.IdentityTokenLifetime),
                SigningCredentials = new SigningCredentials(key, result.RelyingParty.SignatureAlgorithm, result.RelyingParty.DigestAlgorithm),
                Subject = outgoingSubject,
                Issuer = _contextAccessor.HttpContext.GetIdentityServerIssuerUri(),
            };

            if (result.RelyingParty.EncryptionCertificate != null)
            {
                descriptor.EncryptingCredentials = new X509EncryptingCredentials(result.RelyingParty.EncryptionCertificate);
            }

            var handler = _securityTokenHandlerFactory.CreateHandler(result.RelyingParty.TokenType);
            return CreateToken(handler, descriptor);
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
                if (handler is SamlSecurityTokenHandler)
                {
                    var auth = new Microsoft.IdentityModel.Tokens.Saml.AuthenticationInformation(new Uri(authMethod), DateTime.Parse(authTime));
                    return ((SamlSecurityTokenHandler)handler).CreateToken(descriptor, auth);
                }
                if (handler is Saml2SecurityTokenHandler)
                {
                    var auth = new Microsoft.IdentityModel.Tokens.Saml2.AuthenticationInformation(new Uri(authMethod), DateTime.Parse(authTime));
                    return ((Saml2SecurityTokenHandler)handler).CreateToken(descriptor, auth);
                }
            }

            return handler.CreateToken(descriptor);
        }

        private WsFederationMessage CreateResponse(SignInValidationResult validationResult, SecurityToken token)
        {
            var handler = _securityTokenHandlerFactory.CreateHandler(validationResult.RelyingParty.TokenType);
            var rstr = new RequestSecurityTokenResponse
            {
                CreatedAt = token.ValidFrom,
                ExpiresAt = token.ValidTo,
                AppliesTo = validationResult.Client.ClientId,
                Context = validationResult.WsFederationMessage.Wctx,
                ReplyTo = validationResult.ReplyUrl,
                RequestedSecurityToken = token,
                SecurityTokenHandler = handler,
            };

            var responseMessage = new WsFederationMessage
            {
                IssuerAddress = validationResult.ReplyUrl,
                Wa = Microsoft.IdentityModel.Protocols.WsFederation.WsFederationConstants.WsFederationActions.SignIn,
                Wresult = rstr.Serialize(),
                Wctx = validationResult.WsFederationMessage.Wctx
            };

            return responseMessage;
        }
    }
}
