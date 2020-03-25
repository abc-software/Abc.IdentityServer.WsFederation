﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.WsFederation.Validation;
using IdentityServer4.Configuration;
using IdentityServer4.Services;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Linq;
using Microsoft.AspNetCore.Http;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;

namespace IdentityServer4.WsFederation
{
    public class WsFederationController : Controller
    {
        private readonly IUserSession _userSession;
        private readonly SignInResponseGenerator _generator;
        private readonly ILogger<WsFederationController> _logger;
        private readonly MetadataResponseGenerator _metadata;
        private readonly IdentityServerOptions _options;
        private readonly SignInValidator _signinValidator;
        private readonly SignOutValidator _signoutValidator;
        private readonly ISystemClock _clock;
        private readonly IMessageStore<LogoutMessage> _logoutMessageStore;

        public WsFederationController(
            MetadataResponseGenerator metadata, 
            SignInValidator signinValidator, 
            SignOutValidator signoutValidator, 
            IdentityServerOptions options,
            SignInResponseGenerator generator,
            IUserSession userSession,
            ISystemClock clock,
            IMessageStore<LogoutMessage> logoutMessageStore,
            ILogger<WsFederationController> logger)
        {
            _metadata = metadata;
            _signinValidator = signinValidator;
            _signoutValidator = signoutValidator;
            _logoutMessageStore = logoutMessageStore;
            _options = options;
            _generator = generator;
            _userSession = userSession;
            _clock = clock;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // GET + no parameters = metadata request
            if (!Request.QueryString.HasValue)
            {
                _logger.LogDebug("Start WS-Federation metadata request");

                var entity = await _metadata.GenerateAsync(Url.Action("Index", "WsFederation", null, Request.Scheme, Request.Host.Value));
                return new MetadataResult(entity);
            }

            var url = Url.Action("Index", "WsFederation", null, Request.Scheme, Request.Host.Value) + Request.QueryString;
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
                return await ProcessSignOutAsync(message);
            }

            return BadRequest("Invalid WS-Federation request");
        }

        
        private async Task<IActionResult> ProcessSignInAsync(WsFederationMessage signin, ClaimsPrincipal user)
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
                var returnUrl = Url.Action("Index");
                // remove wfresh parameter to ensure we don't trigger sign in after the user signes in
                var query = Request.Query.Where(q => !q.Key.Equals("wfresh", StringComparison.OrdinalIgnoreCase));
                returnUrl = returnUrl.AddQueryString(QueryString.Create(query).Value);

                var loginUrl = Request.PathBase + _options.UserInteraction.LoginUrl;
                var url = loginUrl.AddQueryString(_options.UserInteraction.LoginReturnUrlParameter, returnUrl);

                return Redirect(url);
            }
            else
            {
                // create protocol response
                var responseMessage = await _generator.GenerateResponseAsync(result);
                await _userSession.AddClientIdAsync(result.Client.ClientId);
                
                return new SignInResult(responseMessage);
            }
        }

        private async Task<IActionResult> ProcessSignOutAsync(WsFederationMessage message)
        {
            if (string.IsNullOrWhiteSpace(message.Wreply) ||
                string.IsNullOrWhiteSpace(message.Wtrealm))
            {
                return RedirectToLogOut();
            }

            var result = await _signoutValidator.ValidateAsync(message, User);
            if (result.IsError)
            {
                throw new Exception(result.Error);
            }

            return await RedirectToLogOutAsync(result);
        }

        private IActionResult RedirectToLogOut()
        {
            return Redirect(_options.UserInteraction.LogoutUrl);
        }

        private async Task<IActionResult> RedirectToLogOutAsync(SignOutValidationResult validatedResult)
        {
            var logoutMessage = new LogoutMessage()
            {
                ClientId = validatedResult.Client?.ClientId,
                ClientName = validatedResult.Client?.ClientName,
                SubjectId = validatedResult.User?.GetSubjectId(),
                ClientIds = validatedResult.ClientIds,
                PostLogoutRedirectUri = validatedResult.ReplyUrl
            };

            string id = null;
            if (logoutMessage.ClientId != null && logoutMessage.ClientIds.Any())
            {
                var msg = new Message<LogoutMessage>(logoutMessage, _clock.UtcNow.UtcDateTime);
                id = await _logoutMessageStore.WriteAsync(msg);
            }

            var redirectUrl = _options.UserInteraction.LogoutUrl;

            if (redirectUrl.IsLocalUrl())
            {
                redirectUrl = HttpContext.GetIdentityServerRelativeUrl(redirectUrl);
            }

            if (id != null)
            {
                redirectUrl = redirectUrl.AddQueryString(_options.UserInteraction.LogoutIdParameter, id);
            }

            return Redirect(redirectUrl);
        }
    }
}