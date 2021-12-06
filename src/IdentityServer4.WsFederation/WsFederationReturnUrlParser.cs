// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using IdentityServer4.WsFederation.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System;
using System.Net;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation
{
    public class WsFederationReturnUrlParser : IReturnUrlParser
    {
        private readonly ILogger<WsFederationReturnUrlParser> _logger;
        private readonly ISignInValidator _signinValidator;
        private readonly IUserSession _userSession;
        private readonly IAuthorizationParametersMessageStore _authorizationParametersMessageStore;

        public WsFederationReturnUrlParser(
            IUserSession userSession,
            ISignInValidator signinValidator,
            ILogger<WsFederationReturnUrlParser> logger,
            IAuthorizationParametersMessageStore authorizationParametersMessageStore = null)
        {
            _signinValidator = signinValidator;
            _userSession = userSession;
            _logger = logger;
            _authorizationParametersMessageStore = authorizationParametersMessageStore;
        }

        public bool IsValidReturnUrl(string returnUrl)
        {
            if (returnUrl != null && returnUrl.IsLocalUrl())
            {
                int index = returnUrl.IndexOf('?');
                if (0 <= index)
                {
                    returnUrl = returnUrl.Substring(0, index);
                }

                if (returnUrl.EndsWith(WsFederationConstants.ProtocolRoutePaths.WsFederation, StringComparison.Ordinal) && 0 <= index)
                {
                    _logger.LogTrace("wsfed - returnUrl is valid");
                    return true;
                }
            }

            _logger.LogTrace("wsfed - returnUrl is not valid");
            return false;
        }

        public async Task<AuthorizationRequest> ParseAsync(string returnUrl)
        {
            if (!IsValidReturnUrl(returnUrl))
            {
                return null;
            }

            var signInMessage = await GetSignInRequestMessage(returnUrl);
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

        private async Task<WsFederationMessage> GetSignInRequestMessage(string returnUrl)
        {
            int index = returnUrl.IndexOf('?');
            if (0 <= index)
            {
                returnUrl = returnUrl.Substring(index);
            }

            WsFederationMessage message;
            if (_authorizationParametersMessageStore != null)
            {
                var query = QueryHelpers.ParseNullableQuery(returnUrl);
                if (!query.ContainsKey(WsFederationConstants.AuthorizationParamsStore.MessageStoreIdParameterName))
                {
                    return null;
                }

                string messageStoreId = query[WsFederationConstants.AuthorizationParamsStore.MessageStoreIdParameterName];
                var data = await _authorizationParametersMessageStore.ReadAsync(messageStoreId);
                message = data.Data.ToWsFederationMessage();
            }
            else
            {
                message = WsFederationMessage.FromQueryString(returnUrl);
            }

            if (message.IsSignInMessage)
            {
                return message;
            }

            return null;
        }
    }
}