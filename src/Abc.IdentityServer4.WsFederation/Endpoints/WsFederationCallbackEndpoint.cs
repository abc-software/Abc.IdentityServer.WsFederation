// ----------------------------------------------------------------------------
// <copyright file="WsFederationCallbackEndpoint.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer4.Extensions;
using Abc.IdentityServer4.WsFederation.ResponseProcessing;
using Abc.IdentityServer4.WsFederation.Validation;
using IdentityModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Collections.Specialized;
using System.Net;
using System.Threading.Tasks;

namespace Abc.IdentityServer4.WsFederation.Endpoints
{
    internal class WsFederationCallbackEndpoint : WsFederationEndpointBase
    {
        private readonly IConsentMessageStore _consentResponseStore;
        private readonly IAuthorizationParametersMessageStore _authorizationParametersMessageStore;

        public WsFederationCallbackEndpoint(
            IEventService events,
            IWsFederationRequestValidator validator, 
            ISignInInteractionResponseGenerator interaction, 
            ISignInResponseGenerator generator, 
            IUserSession userSession, 
            ILogger<WsFederationCallbackEndpoint> logger,
            IConsentMessageStore consentResponseStore,
            IAuthorizationParametersMessageStore authorizationParametersMessageStore = null) 
            : base(events, validator, interaction, generator, userSession, logger)
        {
            _consentResponseStore = consentResponseStore;
            _authorizationParametersMessageStore = authorizationParametersMessageStore;
        }

        public override async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            Logger.LogDebug("Start WS-Federation callback request");

            if (!HttpMethods.IsGet(context.Request.Method))
            {
                Logger.LogWarning("Invalid HTTP method for WS-Federation callback endpoint.");
                return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
            }

            WsFederationMessage message;
            if (_authorizationParametersMessageStore != null)
            {
                var messageStoreId = context.Request.Query[WsFederationConstants.DefaultRoutePathParams.MessageStoreIdParameterName];
                var data = await _authorizationParametersMessageStore.ReadAsync(messageStoreId);
                await _authorizationParametersMessageStore.DeleteAsync(messageStoreId);

                message = data.Data.ToWsFederationMessage();
            }
            else
            {
                message = context.Request.Query.ToWsFederationMessage();
            }

            if (!message.IsSignInMessage)
            {
                return await CreateSignInErrorResultAsync("WS-Federation message is not sing in message");
            }

            // user can be null here (this differs from HttpContext.User where the anonymous user is filled in)
            var user = await UserSession.GetUserAsync();

            var parameters = new NameValueCollection();
            parameters.Add(OidcConstants.AuthorizeRequest.ClientId, message.Wtrealm);
            parameters.Add(OidcConstants.AuthorizeRequest.Nonce, message.Wct);
            parameters.Add(OidcConstants.AuthorizeRequest.Scope, message.GetParameter("scope")); // TODO: may be use wreq

            var consentRequest = new ConsentRequest(parameters, user?.GetSubjectId());
            var consent = await _consentResponseStore.ReadAsync(consentRequest.Id);
            if (consent != null && consent.Data == null)
            {
                return await CreateSignInErrorResultAsync("consent message is missing data");
            }

            try
            {
                var result = await ProcessSignInRequestAsync(message, user, consent?.Data);

                Logger.LogTrace("End WS-Federation callback request. Result type: {0}", result?.GetType().ToString() ?? "-none-");

                return result;
            }
            finally
            {
                if (consent != null)
                {
                    await _consentResponseStore.DeleteAsync(consentRequest.Id);
                }
            }
        }
    }
}