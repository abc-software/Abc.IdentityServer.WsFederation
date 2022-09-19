// ----------------------------------------------------------------------------
// <copyright file="WsFederationEndpoint.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer4.Extensions;
using Abc.IdentityServer4.WsFederation.ResponseProcessing;
using Abc.IdentityServer4.WsFederation.Validation;
using IdentityServer4.Endpoints.Results;
using IdentityServer4.Hosting;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Net;
using System.Threading.Tasks;

namespace Abc.IdentityServer4.WsFederation.Endpoints
{
    internal class WsFederationEndpoint : WsFederationEndpointBase
    {
        public WsFederationEndpoint(
            IEventService events,
            IWsFederationRequestValidator validator,
            ISignInInteractionResponseGenerator interaction,
            ISignInResponseGenerator generator,
            IUserSession userSession,
            ILogger<WsFederationEndpoint> logger)
            : base(events, validator, interaction, generator, userSession, logger)
        {
        }

        public override async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            Logger.LogDebug("Start WS-Federation request");

            // user can be null here (this differs from HttpContext.User where the anonymous user is filled in)
            System.Security.Claims.ClaimsPrincipal user = await UserSession.GetUserAsync();

            WsFederationMessage message;
            if (HttpMethods.IsGet(context.Request.Method))
            {
                message = context.Request.Query.ToWsFederationMessage();
            }
            else if (HttpMethods.IsPost(context.Request.Method))
            {
                if (!context.Request.HasApplicationFormContentType())
                {
                    return new StatusCodeResult(HttpStatusCode.UnsupportedMediaType);
                }

                message = context.Request.Form.ToWsFederationMessage();
            }
            else
            {
                return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
            }

            if (message.IsSignInMessage)
            {
                return await ProcessSignInRequestAsync(message, user, null);
            }

            if (message.IsSignOutMessage || message.IsSignOutCleanupMessage())
            {
                return await ProcessSignOutRequestAsync(message, user);
            }

            return new StatusCodeResult(HttpStatusCode.BadRequest);
        }
    }
}