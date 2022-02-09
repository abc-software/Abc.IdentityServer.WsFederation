// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using IdentityServer4.WsFederation.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation.Validation
{
    public class SignInValidator : ISignInValidator
    {
        private readonly IClientStore _clients;
        private readonly IRelyingPartyStore _relyingParties;
        private readonly IRedirectUriValidator _uriValidator;
        private readonly IdentityServerOptions _options;
        private readonly IUserSession _userSession;
        private readonly ISystemClock _clock;
        private readonly ILogger _logger;

        public SignInValidator(
            IdentityServerOptions options,
            IClientStore clients,
            IRelyingPartyStore relyingParties,
            IRedirectUriValidator uriValidator,
            IUserSession userSession,
            ISystemClock clock,
            ILogger<SignInValidator> logger)
        {
            _options = options;
            _clients = clients;
            _relyingParties = relyingParties;
            _uriValidator = uriValidator;
            _userSession = userSession;
            _clock = clock;
            _logger = logger;
        }

        public virtual async Task<SignInValidationResult> ValidateAsync(WsFederationMessage message, ClaimsPrincipal user)
        {
            _logger.LogInformation("Start WS-Federation signin request validation");

            var validatedResult = new ValidatedWsFederationRequest()
            {
                Options = _options,
                WsFederationMessage = message,
            };

            // check sender current time
            if (!string.IsNullOrEmpty(message.Wct))
            {
                if (!message.Wct.TryParseToUtcDateTime(out var senderTime))
                {
                    return new SignInValidationResult(validatedResult, "invalid_sender_time", $"Sender current time '{message.Wct}' is not XML Schema datetime");
                }

                var now = _clock.UtcNow.UtcDateTime;
                if (senderTime.InFuture(now) || senderTime.InPast(now)) 
                {
                    return new SignInValidationResult(validatedResult, "invalid_sender_time", "Sender current time is in past or future");
                }

                message.Wct = null;
            }

            // check client
            var client = await _clients.FindEnabledClientByIdAsync(message.Wtrealm);
            if (client == null)
            {
                return new SignInValidationResult(validatedResult, "invalid_relying_party", "Cannot find Client configuration");
            }

            if (client.ProtocolType != IdentityServerConstants.ProtocolTypes.WsFederation)
            {
                return new SignInValidationResult(validatedResult, "invalid_relying_party", "Client is not configured for WS-Federation");
            }

            validatedResult.SetClient(client);

            if (!string.IsNullOrEmpty(message.Wreply))
            {
                if (await _uriValidator.IsRedirectUriValidAsync(message.Wreply, validatedResult.Client))
                {
                    validatedResult.ReplyUrl = message.Wreply;
                }
                else
                {
                    _logger.LogWarning("Invalid Wreply: {Wreply}", message.Wreply);
                }
            }
            
            if (validatedResult.ReplyUrl == null)
            {
                validatedResult.ReplyUrl = client.RedirectUris.FirstOrDefault();
            }

            if (validatedResult.ReplyUrl == null)
            {
                return new SignInValidationResult(validatedResult, "invalid_relying_party", "No redirect URL configured for relying party");
            }

            // check if additional relying party settings exist
            validatedResult.RelyingParty = await _relyingParties.FindRelyingPartyByRealm(message.Wtrealm);

            if (!string.IsNullOrEmpty(message.Whr) 
                && client.IdentityProviderRestrictions != null
                && client.IdentityProviderRestrictions.Any()
                && !client.IdentityProviderRestrictions.Contains(message.Whr))
            {
                _logger.LogWarning($"WHR (idp) requested '{message.Whr}' is not in client restriction list.");
                message.Whr = null;
            }

            validatedResult.SessionId = await _userSession.GetSessionIdAsync();
            validatedResult.Subject = user;

            await ValidateRequestedResourcesAsync(validatedResult);

            return new SignInValidationResult(validatedResult);
        }

        protected virtual Task ValidateRequestedResourcesAsync(ValidatedWsFederationRequest validatedRequest) {
            /*
            var resourceValidationResult = await resourceValidator.ValidateRequestedResourcesAsync(new ResourceValidationRequest
            {
                Client = validatedResult.Client,
                Scopes = validatedResult.Client.AllowedScopes,
            });
            if (!resourceValidationResult.Succeeded)
            {
                if (resourceValidationResult.InvalidScopes.Any())
                {
                    LogError("Invalid scopes requested");
                }
                else
                {
                    LogError("Invalid scopes for client requested");
                }
                return false;
            }
            */
            var resourceValidationResult = new ResourceValidationResult();

            foreach (var item in validatedRequest.Client.AllowedScopes)
            {
                resourceValidationResult.ParsedScopes.Add(new ParsedScopeValue(item));
            }

            validatedRequest.ValidatedResources = resourceValidationResult;
            return Task.CompletedTask;
        }
    }
}