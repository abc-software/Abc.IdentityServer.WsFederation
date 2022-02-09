using IdentityServer4.Stores;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.WsFederation.Stores;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using IdentityServer4.Configuration;
using IdentityServer4.Extensions;

namespace IdentityServer4.WsFederation.Validation
{
    public class SignOutValidator : ISignOutValidator
    {
        private readonly IClientStore _clients;
        private readonly IRelyingPartyStore _relyingParties;
        private readonly IdentityServerOptions _options;
        private readonly ISystemClock _clock;
        private readonly IRedirectUriValidator _uriValidator;
        private readonly IUserSession _userSession;
        private readonly ILogger _logger;


        public SignOutValidator(
            IdentityServerOptions options,
            IClientStore clients,
            IRelyingPartyStore relyingParties,
            ISystemClock clock,
            IRedirectUriValidator uriValidator,
            IUserSession userSession,
            ILogger<SignOutValidator> logger)
        {
            _options = options;
            _clients = clients;
            _relyingParties = relyingParties;
            _clock = clock;
            _uriValidator = uriValidator;
            _userSession = userSession;
            _logger = logger;
        }

        public virtual async Task<SignOutValidationResult> ValidateAsync(WsFederationMessage message)
        {
            _logger.LogInformation("Start WS-Federation signout request validation");

            var validatedResult = new ValidatedWsFederationRequest()
            {
                Options = _options,
                WsFederationMessage = message,
            };

            // check sender current time
            if (!string.IsNullOrEmpty(message.Wct))
            {
                if (!message.Wct.TryParseToUtcDateTime(out var senderTime))
                {
                    return new SignOutValidationResult(validatedResult, "invalid_sender_time", $"Sender current time '{message.Wct}' is not XML Schema datetime");
                }

                var now = _clock.UtcNow.UtcDateTime;
                if (senderTime.InFuture(now) || senderTime.InPast(now))
                {
                    return new SignOutValidationResult(validatedResult, "invalid_sender_time", "Sender current time is in past or future");
                }

                message.Wct = null;
            }

            // check client
            var client = await _clients.FindEnabledClientByIdAsync(message.Wtrealm);
            if (client == null)
            {
                return new SignOutValidationResult(validatedResult, "invalid_relying_party", "Cannot find Client configuration");
            }

            if (client.ProtocolType != IdentityServerConstants.ProtocolTypes.WsFederation)
            {
                return new SignOutValidationResult(validatedResult, "invalid_relying_party", "Client is not configured for WS-Federation");
            }

            validatedResult.SetClient(client);

            if (!string.IsNullOrEmpty(message.Wreply))
            {
                if (await _uriValidator.IsPostLogoutRedirectUriValidAsync(message.Wreply, validatedResult.Client))
                {
                    validatedResult.ReplyUrl = message.Wreply;
                }
                else
                {
                    _logger.LogWarning("Invalid Wreply: {Wreply}", message.Wreply);
                }
            }
            
            if (validatedResult.ReplyUrl == null)
            {
                validatedResult.ReplyUrl = client.PostLogoutRedirectUris.FirstOrDefault();
            }
            
            if (validatedResult.ReplyUrl == null)
            {
                return new SignOutValidationResult(validatedResult, "invalid_relying_party", "No post logout URL configured for relying party");
            }

            // check if additional relying party settings exist
            validatedResult.RelyingParty = await _relyingParties.FindRelyingPartyByRealm(message.Wtrealm);

            var user = await _userSession.GetUserAsync();
            if (user == null || user.Identity.IsAuthenticated == false)
            {
                return new SignOutValidationResult(validatedResult);
            }

            validatedResult.SessionId = await _userSession.GetSessionIdAsync();
            validatedResult.ClientIds = await _userSession.GetClientListAsync();
            validatedResult.Subject = user;

            return new SignOutValidationResult(validatedResult);
        }
    }
}