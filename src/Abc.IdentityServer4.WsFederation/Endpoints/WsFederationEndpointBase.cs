// ----------------------------------------------------------------------------
// <copyright file="WsFederationEndpointBase.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer4.WsFederation.ResponseProcessing;
using Abc.IdentityServer4.WsFederation.Validation;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Abc.IdentityServer4.WsFederation.Endpoints
{
    internal abstract class WsFederationEndpointBase : IEndpointHandler
    {
        private readonly IEventService _events;
        private readonly ISignInResponseGenerator _generator;
        private readonly ISignInInteractionResponseGenerator _interaction;
        private readonly IWsFederationRequestValidator _validator;

        protected WsFederationEndpointBase(
            IEventService events,
            IWsFederationRequestValidator validator,
            ISignInInteractionResponseGenerator interaction,
            ISignInResponseGenerator generator,
            IUserSession userSession,
            ILogger logger)
        {
            _events = events;
            _validator = validator;
            _interaction = interaction;
            _generator = generator;
            UserSession = userSession;
            Logger = logger;
        }

        protected ILogger Logger { get; }

        protected IUserSession UserSession { get; }

        public abstract Task<IEndpointResult> ProcessAsync(HttpContext context);

        internal async Task<IEndpointResult> ProcessSignInRequestAsync(WsFederationMessage signin, ClaimsPrincipal user, ConsentResponse consent)
        {
            if (user.IsAuthenticated())
            {
                Logger.LogDebug("User in WS-Federation signin request: {subjectId}", user.GetSubjectId());
            }
            else
            {
                Logger.LogDebug("No user present in WS-Federation signin request");
            }

            var validationResult = await _validator.ValidateSignInRequestAsync(signin, user);
            if (validationResult.IsError)
            {
                return await CreateSignInErrorResultAsync(
                    "WS-Federation sign in request validation failed", 
                    validationResult.ValidatedRequest, 
                    validationResult.Error, 
                    validationResult.ErrorDescription);
            }

            var interactionResult = await _interaction.ProcessInteractionAsync(validationResult.ValidatedRequest, consent);
            if (interactionResult.IsError)
            {
                return await CreateSignInErrorResultAsync(
                    "WS-Federation interaction generator error", 
                    validationResult.ValidatedRequest, 
                    interactionResult.Error, 
                    interactionResult.ErrorDescription, 
                    false);
            }

            if (interactionResult.IsLogin)
            {
                return new Results.LoginPageResult(validationResult.ValidatedRequest);
            }

            if (interactionResult.IsRedirect)
            {
                return new Results.CustomRedirectResult(validationResult.ValidatedRequest, interactionResult.RedirectUrl);
            }

            var responseMessage = await _generator.GenerateResponseAsync(validationResult);
            await UserSession.AddClientIdAsync(validationResult.ValidatedRequest.ClientId);
            
            await _events.RaiseAsync(new Events.SignInTokenIssuedSuccessEvent(responseMessage, validationResult.ValidatedRequest));
            
            return new Results.SignInResult(responseMessage);
        }

        internal async Task<IEndpointResult> ProcessSignOutRequestAsync(WsFederationMessage message, ClaimsPrincipal user)
        {
            if (string.IsNullOrWhiteSpace(message.Wreply) ||
                string.IsNullOrWhiteSpace(message.Wtrealm))
            {
                return new Results.LogoutPageResult();
            }

            var validationResult = await _validator.ValidateSignOutRequestAsync(message);
            if (validationResult.IsError)
            {
                return await CreateSignOutErrorResultAsync(
                    "WS-Federation sign out request validation failed", 
                    validationResult.ValidatedRequest, 
                    validationResult.Error, 
                    validationResult.ErrorDescription);
            }

            return new Results.SignOutResult(validationResult.ValidatedRequest);
        }

        protected async Task<IEndpointResult> CreateSignInErrorResultAsync(
            string logMessage, 
            ValidatedWsFederationRequest request = null, 
            string error = "Server", 
            string errorDescription = null, 
            bool logError = true)
        {
            if (logError)
            {
                Logger.LogError(string.Concat(logMessage, ": {ErrorDescription}"), errorDescription);
            }

            if (request != null)
            {
                Logger.LogInformation("{@validationDetails}", new Logging.ValidatedWsFederationRequestLog(request, new string[0]));
            }

            await _events.RaiseAsync(new Events.SignInTokenIssuedFailureEvent(request, error, errorDescription));

            return new Results.ErrorPageResult(error, errorDescription);
        }

        protected Task<IEndpointResult> CreateSignOutErrorResultAsync(
            string logMessage, 
            ValidatedWsFederationRequest request = null, 
            string error = "Server", 
            string errorDescription = null, 
            bool logError = true)
        {
            if (logError)
            {
                Logger.LogError(logMessage);
            }

            if (request != null)
            {
                Logger.LogInformation("{@validationDetails}", new Logging.ValidatedWsFederationRequestLog(request, new string[0]));
            }

            return Task.FromResult<IEndpointResult>(new Results.ErrorPageResult(error, errorDescription));
        }
    }
}