using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.WsFederation.Services;
using IdentityServer4.WsFederation.Validation;
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

namespace IdentityServer4.WsFederation
{
    public class SignInResponseGenerator : ISignInResponseGenerator
    {
        private readonly WsFederationOptions _options;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly Services.IClaimsService _claimsService;
        private readonly IKeyMaterialService _keys;
        private readonly IResourceStore _resources;
        private readonly ISystemClock _clock;
        private readonly ISecurityTokenHandlerFactory _securityTokenHandlerFactory;
        private readonly ILogger<SignInResponseGenerator> _logger;

        public SignInResponseGenerator(
            IHttpContextAccessor contextAccessor,
            WsFederationOptions options,
            Services.IClaimsService claimsService,
            IKeyMaterialService keys,
            IResourceStore resources,
            ISystemClock clock,
            ISecurityTokenHandlerFactory securityTokenHandlerFactory,
            ILogger<SignInResponseGenerator> logger)
        {
            _contextAccessor = contextAccessor;
            _options = options;
            _claimsService = claimsService;
            _keys = keys;
            _resources = resources;
            _clock = clock;
            _securityTokenHandlerFactory = securityTokenHandlerFactory;
            _logger = logger;
        }

        public async Task<WsFederationMessage> GenerateResponseAsync(SignInValidationResult validationResult)
        {
            _logger.LogDebug("Creating WS-Federation signin response");

            var outgoingSubject = await CreateSubjectAsync(validationResult);

            return await CreateResponseAsync(validationResult.ValidatedRequest, outgoingSubject);
        }

        protected virtual async Task<ClaimsIdentity> CreateSubjectAsync(SignInValidationResult result)
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

            if (!outboundClaims.Exists(x => x.Type == ClaimTypes.NameIdentifier)) {
                var nameid = new Claim(ClaimTypes.NameIdentifier, validatedRequest.Subject.GetSubjectId());
                nameid.Properties[Microsoft.IdentityModel.Tokens.Saml.ClaimProperties.SamlNameIdentifierFormat] =
                    validatedRequest.RelyingParty?.SamlNameIdentifierFormat ?? _options.DefaultSamlNameIdentifierFormat;
                outboundClaims.Add(nameid);
            }

            // The AuthnStatement statement generated from the following 2
            // claims is mandatory for some service providers (i.e. Shibboleth-Sp). 
            // The value of the AuthenticationMethod claim must be one of the constants in
            // System.IdentityModel.Tokens.AuthenticationMethods.
            // Password is the only one that can be directly matched, everything
            // else defaults to Unspecified.
            if (!outboundClaims.Exists(x => x.Type == ClaimTypes.AuthenticationMethod)) {
                outboundClaims.Add(new Claim(ClaimTypes.AuthenticationMethod,
                validatedRequest.Subject.GetAuthenticationMethod() == OidcConstants.AuthenticationMethods.Password
                    ? SamlConstants.AuthenticationMethods.PasswordString
                    : SamlConstants.AuthenticationMethods.UnspecifiedString));
            }

            // authentication instant claim is required
            if (!outboundClaims.Exists(x => x.Type == ClaimTypes.AuthenticationInstant))
            {
                outboundClaims.Add(new Claim(ClaimTypes.AuthenticationInstant, validatedRequest.Subject.GetAuthenticationTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), ClaimValueTypes.DateTime));
            }

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

        private async Task<WsFederationMessage> CreateResponseAsync(ValidatedWsFederationRequest validatedRequest, ClaimsIdentity outgoingSubject)
        {
            var credential = await _keys.GetSigningCredentialsAsync();
            var key = credential.Key as Microsoft.IdentityModel.Tokens.X509SecurityKey;
            if (key == null)
            {
                throw new InvalidOperationException("Missing signing key");
            }

            var issueInstant = _clock.UtcNow.UtcDateTime;
            var signingCredentials = new SigningCredentials(
                key,
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
                Issuer = _contextAccessor.HttpContext.GetIdentityServerIssuerUri(),
                TokenType = validatedRequest.RelyingParty?.TokenType ?? _options.DefaultTokenType,
            };

            if (validatedRequest.RelyingParty?.EncryptionCertificate != null)
            {
                descriptor.EncryptingCredentials = new X509EncryptingCredentials(
                    validatedRequest.RelyingParty.EncryptionCertificate,
                    validatedRequest.RelyingParty.KeyWrapAlgoithm ?? _options.DefaultKeyWrapAlgorithm,
                    validatedRequest.RelyingParty.EncryptionAlgoithm ?? _options.DefaultEncryptionAlgorithm);
            }

            var handler = _securityTokenHandlerFactory.CreateHandler(descriptor.TokenType);
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
                Wctx = validatedRequest.WsFederationMessage.Wctx
            };

            return responseMessage;
        }
    }
}
