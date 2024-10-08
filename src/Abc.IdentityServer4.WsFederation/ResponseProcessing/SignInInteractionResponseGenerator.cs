﻿// ----------------------------------------------------------------------------
// <copyright file="SignInInteractionResponseGenerator.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer.Extensions;
using Abc.IdentityServer.WsFederation.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Abc.IdentityServer.WsFederation.ResponseProcessing
{
    /// <summary>
    /// Default logic for determining if user must login or consent when making requests to the sign in endpoint.
    /// </summary>
    /// <seealso cref="ISignInInteractionResponseGenerator" />
    public class SignInInteractionResponseGenerator : ISignInInteractionResponseGenerator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SignInInteractionResponseGenerator"/> class.
        /// </summary>
        /// <param name="profile">The profile.</param>
        /// <param name="clock">The clock.</param>
        /// <param name="logger">The logger.</param>
        public SignInInteractionResponseGenerator(IProfileService profile, IClock clock, ILogger<SignInInteractionResponseGenerator> logger)
        {
            Profile = profile;
            Clock = clock;
            Logger = logger;
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Gets the clock.
        /// </summary>
        protected IClock Clock { get; }

        /// <summary>
        /// Gets the profile service.
        /// </summary>
        protected IProfileService Profile { get; }

        /// <inheritdoc/>
        public virtual async Task<InteractionResponse> ProcessInteractionAsync(ValidatedWsFederationRequest request, ConsentResponse consent = null)
        {
            var interactionResponse = await ProcessLoginAsync(request);
            if (!interactionResponse.IsLogin && !interactionResponse.IsError && !interactionResponse.IsRedirect)
            {
                interactionResponse = await ProcessConsentAsync(request, consent);
            }

            return interactionResponse;
        }

        /// <summary>
        /// Processes the login logic.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The interaction response.</returns>
        protected internal virtual async Task<InteractionResponse> ProcessLoginAsync(ValidatedWsFederationRequest request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var freshness = request.Freshness;
            if (freshness.HasValue && freshness.Value == 0)
            {
                Logger.LogInformation("Showing login: Requested wfresh=0.");
                return new InteractionResponse { IsLogin = true };
            }

            // unauthenticated user
            var user = request.Subject;
            if (!user.IsAuthenticated())
            {
                Logger.LogInformation("Showing login: user not authenticated.");
                return new InteractionResponse { IsLogin = true };
            }

            // user de-activated
            var isActiveCtx = new IsActiveContext(user, request.Client, "WS-Federation");
            await Profile.IsActiveAsync(isActiveCtx);
            if (!isActiveCtx.IsActive)
            {
                Logger.LogInformation("Showing login: User is not active");
                return new InteractionResponse { IsLogin = true };
            }

            // check if idp login hint matches current provider
            var currentIdp = user.GetIdentityProvider();
            var idp = request.HomeRealm;
            if (idp.IsPresent() && idp != currentIdp)
            {
                Logger.LogInformation("Showing login: Current IdP ({currentIdp}) is not the requested IdP ({idp})", currentIdp, idp);
                return new InteractionResponse { IsLogin = true };
            }

            // check authentication freshness
            if (freshness.HasValue)
            {
                var authTime = user.GetAuthenticationTime();
                if (Clock.UtcNow.UtcDateTime > authTime.AddMinutes(freshness.Value))
                {
                    Logger.LogInformation("Showing login: Requested wfresh time exceeded.");
                    return new InteractionResponse { IsLogin = true };
                }
            }

            // check local idp restrictions
            if (currentIdp == Ids.IdentityServerConstants.LocalIdentityProvider)
            {
                if (!request.Client.EnableLocalLogin)
                {
                    Logger.LogInformation("Showing login: User logged in locally, but client does not allow local logins");
                    return new InteractionResponse { IsLogin = true };
                }
            }
            // check external idp restrictions if user not using local idp
            else if (request.Client.IdentityProviderRestrictions != null &&
                request.Client.IdentityProviderRestrictions.Any() &&
                !request.Client.IdentityProviderRestrictions.Contains(currentIdp))
            {
                Logger.LogInformation("Showing login: User is logged in with idp: {idp}, but idp not in client restriction list.", currentIdp);
                return new InteractionResponse { IsLogin = true };
            }

            // check client's user SSO timeout
            if (request.Client.UserSsoLifetime.HasValue)
            {
                long authTimeEpoch = user.GetAuthenticationTimeEpoch();
                long diff = Clock.UtcNow.ToUnixTimeSeconds() - authTimeEpoch;
                if (diff > request.Client.UserSsoLifetime.Value)
                {
                    Logger.LogInformation("Showing login: User's auth session duration: {sessionDuration} exceeds client's user SSO lifetime: {userSsoLifetime}.", diff, request.Client.UserSsoLifetime);
                    return new InteractionResponse { IsLogin = true };
                }
            }

            return new InteractionResponse();
        }

        /// <summary>
        /// Processes the consent logic.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="consent">The consent.</param>
        /// <returns>The interaction response.</returns>
        protected internal virtual Task<InteractionResponse> ProcessConsentAsync(ValidatedWsFederationRequest request, ConsentResponse consent = null)
        {
            return Task.FromResult(new InteractionResponse());
        }
    }
}