// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Stores;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.WsFederation.Stores;
using Microsoft.IdentityModel.Protocols.WsFederation;
using IdentityServer4.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json;
using IdentityServer4.Validation;

namespace IdentityServer4.WsFederation.Validation
{
    public class SignInValidator
    {
        private readonly IClientStore _clients;
        private readonly IRelyingPartyStore _relyingParties;
        private readonly IRedirectUriValidator _uriValidator;
        private readonly WsFederationOptions _options;
        private readonly ISystemClock _clock;
        private readonly ILogger _logger;

        public SignInValidator(
            WsFederationOptions options, 
            IClientStore clients,
            IRelyingPartyStore relyingParties,
            IRedirectUriValidator uriValidator,
            ISystemClock clock,
            ILogger<SignInValidator> logger)
        {
            _options = options;
            _clients = clients;
            _relyingParties = relyingParties;
            _uriValidator = uriValidator;
            _clock = clock;
            _logger = logger;
        }

        public async Task<SignInValidationResult> ValidateAsync(WsFederationMessage message, ClaimsPrincipal user)
        {
            _logger.LogInformation("Start WS-Federation signin request validation");
            var result = new SignInValidationResult
            {
                WsFederationMessage = message
            };
            
            // check client
            var client = await _clients.FindEnabledClientByIdAsync(message.Wtrealm);

            if (client == null)
            {
                LogError("Client not found: " + message.Wtrealm, result);

                return new SignInValidationResult
                {
                    Error = "invalid_relying_party"
                };
            }
            if (client.Enabled == false)
            {
                LogError("Client is disabled: " + message.Wtrealm, result);

                return new SignInValidationResult
                {
                    Error = "invalid_relying_party"
                };
            }
            if (client.ProtocolType != IdentityServerConstants.ProtocolTypes.WsFederation)
            {
                LogError("Client is not configured for WS-Federation", result);

                return new SignInValidationResult
                {
                    Error = "invalid_relying_party"
                };
            }

            result.Client = client;

            if (!string.IsNullOrEmpty(message.Wreply))
            {
                if (await _uriValidator.IsRedirectUriValidAsync(message.Wreply, result.Client))
                {
                    result.ReplyUrl = message.Wreply;
                }
                else
                {
                    _logger.LogWarning("Invalid Wreply: {Wreply}", message.Wreply);
                }
            }
            else if (client.RedirectUris.Count == 1)
            {
                result.ReplyUrl = client.RedirectUris.First();
            }

            // check if additional relying party settings exist
            var rp = await _relyingParties.FindRelyingPartyByRealm(message.Wtrealm);
            if (rp == null)
            {
                rp = new RelyingParty
                {
                    TokenType = _options.DefaultTokenType,
                    SignatureAlgorithm = _options.DefaultSignatureAlgorithm,
                    DigestAlgorithm = _options.DefaultDigestAlgorithm,
                    SamlNameIdentifierFormat = _options.DefaultSamlNameIdentifierFormat,
                    ClaimMapping = _options.DefaultClaimMapping
                };
            }

            result.RelyingParty = rp;

            if (user == null ||
                user.Identity.IsAuthenticated == false)
            {
                result.SignInRequired = true;
                return result;
            }

            result.User = user;

            if (!string.IsNullOrEmpty(message.Wfresh))
            {
                if (int.TryParse(message.Wfresh, out int maxAgeInMinutes))
                {
                    if (maxAgeInMinutes == 0)
                    {
                        _logger.LogInformation("Showing login: Requested wfresh=0.");
                        message.Wfresh = null;
                        result.SignInRequired = true;
                        return result;
                    }
                    var authTime = user.GetAuthenticationTime();
                    if (_clock.UtcNow.UtcDateTime > authTime.AddMinutes(maxAgeInMinutes))
                    {
                        _logger.LogInformation("Showing login: Requested wfresh time exceeded.");
                        result.SignInRequired = true;
                        return result;
                    }
                }
            }
            
            LogSuccess(result);
            return result;
        }

        private void LogSuccess(SignInValidationResult result)
        {
            // var log = JsonConvert.SerializeObject(result, Formatting.Indented);
            // _logger.LogInformation("End WS-Federation signin request validation\n{0}", log.ToString());
        }

        private void LogError(string message, SignInValidationResult result)
        {
            // var log = JsonConvert.SerializeObject(result, Formatting.Indented);
            // _logger.LogError("{0}\n{1}", message, log.ToString());
        }
    }
}