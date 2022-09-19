// ----------------------------------------------------------------------------
// <copyright file="WsFederationRequestValidator.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer4.Extensions;
using Abc.IdentityServer4.WsFederation.Stores;
using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Abc.IdentityServer4.WsFederation.Validation
{
    /// <summary>
    /// Validates requests to the WS-Federation endpoint.
    /// </summary>
    public class WsFederationRequestValidator : IWsFederationRequestValidator
    {
        private readonly IClientStore _clients;
        private readonly IRelyingPartyStore _relyingParties;
        private readonly IRedirectUriValidator _uriValidator;
        private readonly IdentityServerOptions _options;
        private readonly IUserSession _userSession;
        private readonly ISystemClock _clock;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WsFederationRequestValidator"/> class.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="clients"></param>
        /// <param name="relyingParties"></param>
        /// <param name="uriValidator"></param>
        /// <param name="userSession"></param>
        /// <param name="clock"></param>
        /// <param name="logger"></param>
        public WsFederationRequestValidator(
            IdentityServerOptions options,
            IClientStore clients,
            IRelyingPartyStore relyingParties,
            IRedirectUriValidator uriValidator,
            IUserSession userSession,
            ISystemClock clock,
            ILogger<WsFederationRequestValidator> logger)
        {
            _options = options;
            _clients = clients;
            _relyingParties = relyingParties;
            _uriValidator = uriValidator;
            _userSession = userSession;
            _clock = clock;
            _logger = logger;
        }

        /// <inheritdoc/>
        public virtual async Task<WsFederationValidationResult> ValidateSignInRequestAsync(WsFederationMessage message, ClaimsPrincipal user)
        {
            _logger.LogInformation("Start WS-Federation sign in request validation");

            var request = new ValidatedWsFederationRequest()
            {
                Options = _options,
                WsFederationMessage = message,
            };

            // validate wtrealm
            var clientResult = await ValidateClientAsync(request);
            if (clientResult.IsError)
            {
                return clientResult;
            }

            // validate wreq and wreqptr
            var requesResult = ValidateRequest(request);
            if (requesResult.IsError)
            {
                return requesResult;
            }

            // validate wreply
            var replyResult = await ValidateReplyAsync(request, false);
            if (replyResult.IsError)
            {
                return replyResult;
            }

            // check if additional relying party settings exist
            request.RelyingParty = await _relyingParties.FindRelyingPartyByRealmAsync(request.ClientId);

            // validate wct, whr and wfresh
            var optionalResult = ValidateOptionalParameters(request);
            if (optionalResult.IsError)
            {
                return optionalResult;
            }

            request.SessionId = await _userSession.GetSessionIdAsync();
            request.Subject = user;

            await ValidateRequestedResourcesAsync(request);

            _logger.LogTrace("WS-Federation sign in request validation successful");

            return new WsFederationValidationResult(request);
        }

        /// <inheritdoc/>
        public virtual async Task<WsFederationValidationResult> ValidateSignOutRequestAsync(WsFederationMessage message)
        {
            _logger.LogInformation("Start WS-Federation sign out request validation");

            var request = new ValidatedWsFederationRequest()
            {
                Options = _options,
                WsFederationMessage = message,
            };

            // validate wtrealm
            var clientResult = await ValidateClientAsync(request);
            if (clientResult.IsError)
            {
                return clientResult;
            }

            // validate wreply
            var replyResult = await ValidateReplyAsync(request, true);
            if (replyResult.IsError)
            {
                return replyResult;
            }

            // check if additional relying party settings exist
            request.RelyingParty = await _relyingParties.FindRelyingPartyByRealmAsync(request.ClientId);

            // validate wct, whr and wfresh
            var optionalResult = ValidateOptionalParameters(request);
            if (optionalResult.IsError)
            {
                return optionalResult;
            }

            var user = await _userSession.GetUserAsync();
            if (user.IsAuthenticated())
            {
                request.SessionId = await _userSession.GetSessionIdAsync();
                request.ClientIds = await _userSession.GetClientListAsync();
                request.Subject = user;
            }

            _logger.LogTrace("WS-Federation sign out request validation successful");

            return new WsFederationValidationResult(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="validatedRequest">The validated WS-federation request.</param>
        /// <returns></returns>
        protected virtual Task ValidateRequestedResourcesAsync(ValidatedWsFederationRequest validatedRequest)
        {
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

        private WsFederationValidationResult ValidateRequest(ValidatedWsFederationRequest request)
        {
            var message = request.WsFederationMessage;
            var wstrustRequest = message.Wreq;
            var wstrustRequestUri = message.Wreqptr;

            if (wstrustRequest.IsPresent() && wstrustRequestUri.IsPresent())
            {
                return new WsFederationValidationResult(request, "invalid_request", "Only one request parameter is allowed");
            }

            if (wstrustRequestUri.IsPresent())
            {
                // 512 is from the spec
                if (wstrustRequestUri.Length > 512)
                {
                    return new WsFederationValidationResult(request, "invalid_request", "Parameter 'wreqptr' is too long");
                }

                // TODO: IWSTrustRequestUriHttpClient
                /*
                var xml = await _wstrustRequestUriHttpClient.GetXmlAsync(wstrustRequestUri, request.Client);
                if (xml.IsMissing())
                {
                    return new WsFederationValidationResult(request, "invalid_request", "No value returned from 'wreqptr'");
                }

                wstrustRequest = xml;
                */
            }

            // check length restrictions
            if (wstrustRequest.IsPresent()
                && wstrustRequest.Length >= _options.InputLengthRestrictions.Jwt)
            {
                return new WsFederationValidationResult(request, "invalid_request", "Request value is too long");
            }

            return new WsFederationValidationResult(request);
        }

        private async Task<WsFederationValidationResult> ValidateReplyAsync(ValidatedWsFederationRequest request, bool logOut = false)
        {
            var message = request.WsFederationMessage;
            var replyUri = message.Wreply;

            if (replyUri.IsPresent())
            {
                // wreply must be valid URI
                if (replyUri.IsMissingOrTooLong(_options.InputLengthRestrictions.RedirectUri))
                {
                    return new WsFederationValidationResult(request, "invalid_request", "Invalid reply URI");
                }

                if (!Uri.TryCreate(replyUri, UriKind.Absolute, out _))
                {
                    return new WsFederationValidationResult(request, "invalid_request", $"Reply '{replyUri}' is not URI");
                }

                // check if wreply is valid
                var valid = logOut
                    ? await _uriValidator.IsPostLogoutRedirectUriValidAsync(replyUri, request.Client)
                    : await _uriValidator.IsRedirectUriValidAsync(replyUri, request.Client);
                if (valid)
                {
                    request.ReplyUrl = message.Wreply;
                }
                else
                {
                    _logger.LogWarning("Invalid Wreply: {Wreply}", message.Wreply);
                }
            }

            // if wreply not set, use the first configured one
            if (request.ReplyUrl == null)
            {
                var uris = logOut
                    ? request.Client.PostLogoutRedirectUris
                    : request.Client.RedirectUris;

                request.ReplyUrl = uris.FirstOrDefault();
            }

            if (request.ReplyUrl == null)
            {
                return new WsFederationValidationResult(request, "invalid_relying_party", "No redirect URL configured for relying party");
            }

            return new WsFederationValidationResult(request);
        }

        private WsFederationValidationResult ValidateOptionalParameters(ValidatedWsFederationRequest request)
        {
            var message = request.WsFederationMessage;

            // time
            var time = message.Wct;
            if (time.IsPresent())
            {
                if (!time.TryParseToUtcDateTime(out var senderTime))
                {
                    return new WsFederationValidationResult(request, "invalid_request", $"Sender current time '{time}' is not XML Schema datetime");
                }

                var now = _clock.UtcNow.UtcDateTime;
                if (senderTime.InFuture(now, 300) || senderTime.InPast(now, 300)) // TODO: TimeTolerance from config
                {
                    return new WsFederationValidationResult(request, "invalid_request", "Sender current time is in past or future");
                }

                // validate only on request, suppress validation on callback
                message.Wct = null;
            }

            // freshness
            var freshness = message.Wfresh;
            if (freshness.IsPresent())
            {
                if (!int.TryParse(freshness, out int maxAgeInMinutes))
                {
                    return new WsFederationValidationResult(request, "invalid_request", "Invalid freshness value");
                }

                if (maxAgeInMinutes < 0)
                {
                    return new WsFederationValidationResult(request, "invalid_request", "Invalid freshness value");
                }

                if (maxAgeInMinutes == 0)
                {
                    // remove wfresh so when we redirect back in from login page
                    // we won't think we need to force a login again
                    message.Wfresh = null;
                }

                request.Freshness = maxAgeInMinutes;
            }

            // home realm
            var idp = message.Whr;
            if (idp.IsPresent())
            {
                if (request.Client.IdentityProviderRestrictions != null
                    && request.Client.IdentityProviderRestrictions.Any()
                    && !request.Client.IdentityProviderRestrictions.Contains(idp))
                {
                    _logger.LogWarning("WHR (idp) requested '{whr}' is not in client restriction list.", idp);
                    message.Whr = null;
                }
                else
                {
                    request.HomeRealm = idp;
                }
            }

            return new WsFederationValidationResult(request);
        }

        private async Task<WsFederationValidationResult> ValidateClientAsync(ValidatedWsFederationRequest request)
        {
            var message = request.WsFederationMessage;
            var realm = message.Wtrealm;

            // wtrealm parameter must be present
            if (realm.IsMissingOrTooLong(_options.InputLengthRestrictions.ClientId))
            {
                return new WsFederationValidationResult(request, "invalid_request", "Invalid wtrealm");
            }

            request.ClientId = realm;

            // check for valid client
            var client = await _clients.FindEnabledClientByIdAsync(realm);
            if (client == null)
            {
                return new WsFederationValidationResult(request, "invalid_relying_party", "Cannot find Client configuration");
            }

            if (client.ProtocolType != IdentityServerConstants.ProtocolTypes.WsFederation)
            {
                return new WsFederationValidationResult(request, "invalid_relying_party", "Client is not configured for WS-Federation");
            }

            request.SetClient(client);

            return new WsFederationValidationResult(request);
        }
    }
}