// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Services;
using System;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Extensions;
using Microsoft.Extensions.Logging;
using IdentityServer4.WsFederation.Validation;
using Microsoft.AspNetCore.Http;
using System.Net;
using Microsoft.IdentityModel.Protocols.WsFederation;
using IdentityServer4.Validation;
using IdentityServer4.Stores;
using System.Collections.Generic;
using System.Linq;

namespace IdentityServer4.WsFederation
{
    public class WsFederationReturnUrlParser : IReturnUrlParser
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ILogger<WsFederationReturnUrlParser> _logger;
        private readonly ISignInValidator _signinValidator;
        private readonly IUserSession _userSession;
        private readonly IScopeParser _scopeParser;
        private readonly IResourceStore _resourceStore;

        public WsFederationReturnUrlParser(
            IUserSession userSession,
            IHttpContextAccessor contextAccessor,
            ISignInValidator signinValidator,
            IScopeParser scopeParser,
            IResourceStore resourceStore,
            ILogger<WsFederationReturnUrlParser> logger)
        {
            _contextAccessor = contextAccessor;
            _signinValidator = signinValidator;
            _scopeParser = scopeParser;
            _resourceStore = resourceStore;
            _userSession = userSession;
            _logger = logger;
        }

        public bool IsValidReturnUrl(string returnUrl)
        {
            if (returnUrl.IsLocalUrl())
            {
                var message = GetSignInRequestMessage(returnUrl);
                if (message != null)
                {
                    return true;
                }

                _logger.LogTrace("not a valid WS-Federation return URL");
                return false;
            }

            return false;
        }

        public async Task<AuthorizationRequest> ParseAsync(string returnUrl)
        {
            var user = await _userSession.GetUserAsync();

            var signInMessage = GetSignInRequestMessage(returnUrl);
            if (signInMessage == null)
            {
                return null;
            }

            // call validator
            var result = await _signinValidator.ValidateAsync(signInMessage, user);
            if (result.IsError)
            {
                return null;
            }

            ResourceValidationResult resourceValidationResult = null;
            if (result.Client?.AllowedScopes != null)
            {
                resourceValidationResult = await CreateResourceValidationResult(result.Client.AllowedScopes);
            }

            // populate request
            var request = new AuthorizationRequest()
            {
                Client = result.Client,
                IdP = result.WsFederationMessage.Whr,
                RedirectUri = result.ReplyUrl,
                ValidatedResources = resourceValidationResult,
            };

            foreach (var item in result.WsFederationMessage.Parameters)
            {
                request.Parameters.Add(item.Key, item.Value);
            }

            return request;
        }

        protected virtual async Task<ResourceValidationResult> CreateResourceValidationResult(IEnumerable<string> scopeValues)
        {
            var parsedScopesResult = _scopeParser.ParseScopeValues(scopeValues);
            var parsedScopeValues = parsedScopesResult.ParsedScopes;
            return new ResourceValidationResult(await _resourceStore.FindEnabledResourcesByScopeAsync(parsedScopeValues.Select(x => x.ParsedName)), parsedScopeValues);
        }

        private WsFederationMessage GetSignInRequestMessage(string returnUrl)
        {
            var decoded = WebUtility.UrlDecode(returnUrl);
            int index = decoded.IndexOf('?');
            if (0 <= index)
            {
                decoded = decoded.Substring(index);
            }

            WsFederationMessage message = WsFederationMessage.FromQueryString(decoded);
            if (message.IsSignInMessage)
            {
                return message;
            }

            return null;
        }
    }
}