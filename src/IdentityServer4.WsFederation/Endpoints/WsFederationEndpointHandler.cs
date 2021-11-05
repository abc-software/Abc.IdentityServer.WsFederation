// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityServer4.Configuration;
using IdentityServer4.Endpoints.Results;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.WsFederation.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation.Endpoints
{
    public class WsFederationEndpointHandler : IEndpointHandler
    {
        private readonly IUserSession _userSession;
        private readonly ISignInResponseGenerator _generator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<WsFederationEndpointHandler> _logger;
        private readonly IMetadataResponseGenerator _metadata;
        private readonly IdentityServerOptions _options;
        private readonly ISignInValidator _signinValidator;
        private readonly ISignOutValidator _signoutValidator;
        private readonly ISystemClock _clock;
        private readonly IMessageStore<LogoutMessage> _logoutMessageStore;

        public WsFederationEndpointHandler(
            IMetadataResponseGenerator metadata, 
            ISignInValidator signinValidator,
            ISignOutValidator signoutValidator, 
            IdentityServerOptions options,
            ISignInResponseGenerator generator,
            IHttpContextAccessor httpContextAccessor,
            IUserSession userSession,
            ISystemClock clock,
            IMessageStore<LogoutMessage> logoutMessageStore,
            ILogger<WsFederationEndpointHandler> logger)
        {
            _metadata = metadata;
            _signinValidator = signinValidator;
            _signoutValidator = signoutValidator;
            _logoutMessageStore = logoutMessageStore;
            _options = options;
            _generator = generator;
            _httpContextAccessor = httpContextAccessor;
            _userSession = userSession;
            _clock = clock;
            _logger = logger;
        }

        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            if (context.Request.Method != "GET")
            {
                _logger.LogWarning("WS-Federation endpoint only supports GET requests");
                return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
            }

            // GET + no parameters = metadata request
            if (!context.Request.QueryString.HasValue)
            {
                _logger.LogDebug("Start WS-Federation metadata request");
                var entity = await _metadata.GenerateAsync();
                return new Results.MetadataResult(entity);
            }

            var url = context.Request.GetEncodedUrl();
            _logger.LogDebug("Start WS-Federation request: {url}", url);

            // user can be null here (this differs from HttpContext.User where the anonymous user is filled in)
            var user = await _userSession.GetUserAsync();
            WsFederationMessage message = WsFederationMessage.FromUri(new Uri(url));
            var isSignin = message.IsSignInMessage;
            if (isSignin)
            {
                return await ProcessSignInAsync(message, user);
            }

            var isSignout = message.IsSignOutMessage;
            if (isSignout)
            {
                return await ProcessSignOutAsync(message, user);
            }

            return new StatusCodeResult(HttpStatusCode.BadRequest);
        }

        private async Task<IEndpointResult> ProcessSignInAsync(WsFederationMessage signin, ClaimsPrincipal user)
        {
            if (user != null && user.Identity.IsAuthenticated)
            {
                _logger.LogDebug("User in WS-Federation signin request: {subjectId}", user.GetSubjectId());
            }
            else
            {
                _logger.LogDebug("No user present in WS-Federation signin request");
            }

            // validate request 
            var result = await _signinValidator.ValidateAsync(signin, user);
            if (result.IsError)
            {
                throw new Exception(result.Error);
            }

            if (result.SignInRequired)
            {
                return new Results.LoginPageResult(result.ValidatedRequest.WsFederationMessage);
            }
            else
            {
                // create protocol response
                var responseMessage = await _generator.GenerateResponseAsync(result);
                await _userSession.AddClientIdAsync(result.ValidatedRequest.ClientId);
                
                return new Results.SignInResult(responseMessage);
            }
        }

        private async Task<IEndpointResult> ProcessSignOutAsync(WsFederationMessage message, ClaimsPrincipal user)
        {
            if (string.IsNullOrWhiteSpace(message.Wreply) ||
                string.IsNullOrWhiteSpace(message.Wtrealm))
            {
                return new Results.LogoutPageResult();
            }

            var result = await _signoutValidator.ValidateAsync(message);
            if (result.IsError)
            {
                throw new Exception(result.Error);
            }

            return await RedirectToLogOutAsync(result.ValidatedRequest);
        }

        private async Task<IEndpointResult> RedirectToLogOutAsync(ValidatedWsFederationRequest validatedRequest)
        {
            var logoutMessage = new LogoutMessage()
            {
                ClientId = validatedRequest.Client?.ClientId,
                ClientName = validatedRequest.Client?.ClientName,
                SubjectId = validatedRequest.Subject?.GetSubjectId(),
                SessionId = validatedRequest.SessionId,
                ClientIds = validatedRequest.ClientIds,
                PostLogoutRedirectUri = validatedRequest.ReplyUrl
            };

            string id = null;
            if (logoutMessage.ClientId != null && logoutMessage.ClientIds.Any()) {
                var msg = new Message<LogoutMessage>(logoutMessage, _clock.UtcNow.UtcDateTime);
                id = await _logoutMessageStore.WriteAsync(msg);
            }

            return new Results.SignOutResult(id);
        }
    }
}