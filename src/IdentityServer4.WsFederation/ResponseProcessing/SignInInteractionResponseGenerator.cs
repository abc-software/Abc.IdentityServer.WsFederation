using IdentityServer4.Extensions;
using IdentityServer4.ResponseHandling;
using IdentityServer4.WsFederation.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using System;
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

            var user = request.Subject;
            if (user == null || user.Identity.IsAuthenticated == false)
            {
                _logger.LogInformation("Showing login: user not authenticated.");
                return Task.FromResult(new InteractionResponse { IsLogin = true });
            }

            var message = request.WsFederationMessage;
            if (!string.IsNullOrEmpty(message.Wfresh))
            {
                if (int.TryParse(message.Wfresh, out int maxAgeInMinutes))
                {
                    if (maxAgeInMinutes == 0)
                    {
                        _logger.LogInformation("Showing login: Requested wfresh=0.");
                        message.Wfresh = null;
                        return Task.FromResult(new InteractionResponse { IsLogin = true });
                    }

                    var authTime = user.GetAuthenticationTime();
                    if (_clock.UtcNow.UtcDateTime > authTime.AddMinutes(maxAgeInMinutes))
                    {
                        _logger.LogInformation("Showing login: Requested wfresh time exceeded.");
                        return Task.FromResult(new InteractionResponse { IsLogin = true });
                    }
                }
            }

            var requestedIdentityProvider = message.Whr;
            if (!string.IsNullOrEmpty(requestedIdentityProvider))
            {
                var currentIdentityProvider = user.GetIdentityProvider();
                if (requestedIdentityProvider != currentIdentityProvider)
                {
                    _logger.LogInformation($"Showing login: Current IdP '{currentIdentityProvider}' is not the requested IdP '{requestedIdentityProvider}'");
                    return Task.FromResult(new InteractionResponse { IsLogin = true });
                }
            }

            return Task.FromResult(new InteractionResponse());
        }
    }
}
