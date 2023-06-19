// ----------------------------------------------------------------------------
// <copyright file="WsFederationReturnUrlParser.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer.Extensions;
using Abc.IdentityServer.WsFederation.Validation;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System;
using System.Threading.Tasks;

namespace Abc.IdentityServer.WsFederation
{
    public class WsFederationReturnUrlParser : IReturnUrlParser
    {
        private readonly ILogger<WsFederationReturnUrlParser> _logger;
        private readonly IWsFederationRequestValidator _validator;
        private readonly IUserSession _userSession;
        private readonly IAuthorizationParametersMessageStore _authorizationParametersMessageStore;

        public WsFederationReturnUrlParser(
            IUserSession userSession,
            IWsFederationRequestValidator validator,
            ILogger<WsFederationReturnUrlParser> logger,
            IAuthorizationParametersMessageStore authorizationParametersMessageStore = null)
        {
            _validator = validator;
            _userSession = userSession;
            _logger = logger;
            _authorizationParametersMessageStore = authorizationParametersMessageStore;
        }

        public bool IsValidReturnUrl(string returnUrl)
        {
            if (returnUrl != null && returnUrl.IsLocalUrl())
            {
                int index = returnUrl.IndexOf('?');
                if (index >= 0)
                {
                    returnUrl = returnUrl.Substring(0, index);
                }

                if (returnUrl.EndsWith(WsFederationConstants.ProtocolRoutePaths.WsFederationCallback, StringComparison.Ordinal) && index >= 0)
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

            var signInMessage = await GetSignInRequestMessageAsync(returnUrl);
            if (signInMessage == null)
            {
                return null;
            }

            var user = await _userSession.GetUserAsync();
            var result = await _validator.ValidateSignInRequestAsync(signInMessage, user);
            if (result.IsError)
            {
                return null;
            }

            var validatedRequest = result.ValidatedRequest;
            var request = new AuthorizationRequest()
            {
                Client = validatedRequest.Client,
                IdP = validatedRequest.WsFederationMessage.Whr,
                AcrValues = validatedRequest.WsFederationMessage.GetAcrValues(),
                RedirectUri = validatedRequest.ReplyUrl,
                ValidatedResources = validatedRequest.ValidatedResources,
            };

            // add parameters include 'scope'
            foreach (var item in validatedRequest.WsFederationMessage.Parameters)
            {
                request.Parameters.Add(item.Key, item.Value);
            }

            return request;
        }

        private async Task<WsFederationMessage> GetSignInRequestMessageAsync(string returnUrl)
        {
            int index = returnUrl.IndexOf('?');
            if (index >= 0)
            {
                returnUrl = returnUrl.Substring(index);
            }

            WsFederationMessage message;
            if (_authorizationParametersMessageStore != null)
            {
                var query = QueryHelpers.ParseNullableQuery(returnUrl);
                if (!query.ContainsKey(WsFederationConstants.DefaultRoutePathParams.MessageStoreIdParameterName))
                {
                    return null;
                }

                string messageStoreId = query[WsFederationConstants.DefaultRoutePathParams.MessageStoreIdParameterName];
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