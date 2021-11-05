// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using IdentityServer4.WsFederation.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Net;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation
{
    public class WsFederationReturnUrlParser : IReturnUrlParser
    {
        private readonly ILogger<WsFederationReturnUrlParser> _logger;
        private readonly ISignInValidator _signinValidator;
        private readonly IUserSession _userSession;

        public WsFederationReturnUrlParser(
            IUserSession userSession,
            ISignInValidator signinValidator,
            ILogger<WsFederationReturnUrlParser> logger)
        {
            _signinValidator = signinValidator;
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
            var signInMessage = GetSignInRequestMessage(returnUrl);
            if (signInMessage == null)
            {
                return null;
            }

            var user = await _userSession.GetUserAsync();
            var result = await _signinValidator.ValidateAsync(signInMessage, user);
            if (result.IsError)
            {
                return null;
            }

            var validatedRequest = result.ValidatedRequest;
            var request = new AuthorizationRequest()
            {
                Client = validatedRequest.Client,
                IdP = validatedRequest.WsFederationMessage.Whr,
                RedirectUri = validatedRequest.ReplyUrl,
                ValidatedResources = validatedRequest.ValidatedResources,
            };

            foreach (var item in validatedRequest.WsFederationMessage.Parameters)
            {
                request.Parameters.Add(item.Key, item.Value);
            }

            return request;
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