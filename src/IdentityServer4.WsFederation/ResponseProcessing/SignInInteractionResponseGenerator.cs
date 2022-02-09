using IdentityServer4.Extensions;
using IdentityServer4.ResponseHandling;
using IdentityServer4.WsFederation.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation
{
    public class SignInInteractionResponseGenerator : ISignInInteractionResponseGenerator
    {
        private readonly ISystemClock _clock;
        private readonly ILogger _logger;

        public SignInInteractionResponseGenerator(ISystemClock clock, ILogger<SignInInteractionResponseGenerator> logger)
        {
            _clock = clock;
            _logger = logger;
        }

        public virtual Task<InteractionResponse> ProcessInteractionAsync(ValidatedWsFederationRequest request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var message = request.WsFederationMessage;

            // unauthenticated user
            var user = request.Subject;
            if (user == null || !user.Identity.IsAuthenticated)
            {
                _logger.LogInformation("Showing login: user not authenticated.");
                return Task.FromResult(new InteractionResponse { IsLogin = true });
            }

            // check if idp login hint matches current provider
            var currentIdp = user.GetIdentityProvider();
            var idp = message.Whr;
            if (idp.IsPresent() && idp != currentIdp)
            {
                _logger.LogInformation("Showing login: Current IdP ({currentIdp}) is not the requested IdP ({idp})", currentIdp, idp);
                return Task.FromResult(new InteractionResponse { IsLogin = true });
            }

            // check authentication freshness
            var wfresh = message.Wfresh;
            if (wfresh.IsPresent())
            {
                if (!int.TryParse(wfresh, out int maxAgeInMinutes))
                {
                    _logger.LogWarning("Requested wfresh has invalid value.");
                }

                if (maxAgeInMinutes == 0)
                {
                    // remove wfresh so when we redirect back in from login page
                    // we won't think we need to force a login again
                    message.Wfresh = null;

                    _logger.LogInformation("Showing login: Requested wfresh=0.");
                    return Task.FromResult(new InteractionResponse { IsLogin = true });
                }

                var authTime = user.GetAuthenticationTime();
                if (_clock.UtcNow.UtcDateTime > authTime.AddMinutes(maxAgeInMinutes))
                {
                    _logger.LogInformation("Showing login: Requested wfresh time exceeded.");
                    return Task.FromResult(new InteractionResponse { IsLogin = true });
                }
            }

            // check local idp restrictions
            if (currentIdp == IdentityServerConstants.LocalIdentityProvider)
            {
                if (!request.Client.EnableLocalLogin)
                {
                    _logger.LogInformation("Showing login: User logged in locally, but client does not allow local logins");
                    return Task.FromResult(new InteractionResponse { IsLogin = true });
                }
            }
            // check external idp restrictions if user not using local idp
            else if (request.Client.IdentityProviderRestrictions != null &&
                request.Client.IdentityProviderRestrictions.Any() &&
                !request.Client.IdentityProviderRestrictions.Contains(currentIdp))
            {
                _logger.LogInformation("Showing login: User is logged in with idp: {idp}, but idp not in client restriction list.", currentIdp);
                return Task.FromResult(new InteractionResponse { IsLogin = true });
            }

            // check client's user SSO timeout
            if (request.Client.UserSsoLifetime.HasValue)
            {
                long authTimeEpoch = user.GetAuthenticationTimeEpoch();
                long diff = _clock.UtcNow.ToUnixTimeSeconds() - authTimeEpoch;
                if (diff > request.Client.UserSsoLifetime.Value)
                {
                    _logger.LogInformation("Showing login: User's auth session duration: {sessionDuration} exceeds client's user SSO lifetime: {userSsoLifetime}.", diff, request.Client.UserSsoLifetime);
                    return Task.FromResult(new InteractionResponse { IsLogin = true });
                }
            }

            return Task.FromResult(new InteractionResponse());
        }
    }
}
