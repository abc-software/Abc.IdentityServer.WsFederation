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

namespace IdentityServer4.WsFederation.Validation
{
    public class SignOutValidator : ISignOutValidator
    {
        private readonly IClientStore _clients;
        private readonly IRelyingPartyStore _relyingParties;
        private readonly WsFederationOptions _options;
        private readonly ISystemClock _clock;
        private readonly IRedirectUriValidator _uriValidator;
        private readonly IUserSession _userSession;
        private readonly ILogger _logger;


        public SignOutValidator(
            WsFederationOptions options,
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

        public async Task<SignOutValidationResult> ValidateAsync(WsFederationMessage message, ClaimsPrincipal user)
        {
            _logger.LogInformation("Start WS-Federation signout request validation");
            var result = new SignOutValidationResult
            {
                WsFederationMessage = message
            };

            // check client
            var client = await _clients.FindEnabledClientByIdAsync(message.Wtrealm);
            if (client == null)
            {
                LogError("Client not found: " + message.Wtrealm, result);

                return new SignOutValidationResult
                {
                    Error = "invalid_relying_party"
                };
            }

            if (client.ProtocolType != IdentityServerConstants.ProtocolTypes.WsFederation)
            {
                LogError("Client is not configured for WS-Federation", result);

                return new SignOutValidationResult
                {
                    Error = "invalid_relying_party"
                };
            }

            result.Client = client;

            if (!string.IsNullOrEmpty(message.Wreply))
            {
                if (await _uriValidator.IsPostLogoutRedirectUriValidAsync(message.Wreply, result.Client))
                {
                    result.ReplyUrl = message.Wreply;
                }
                else
                {
                    _logger.LogWarning("Invalid Wreply: {Wreply}", message.Wreply);
                }
            }
            else if (client.PostLogoutRedirectUris.Count == 1)
            {
                result.ReplyUrl = client.PostLogoutRedirectUris.First();
            }

            // check if additional relying party settings exist
            result.RelyingParty = await _relyingParties.FindRelyingPartyByRealm(message.Wtrealm);

            if (user == null ||
                user.Identity.IsAuthenticated == false)
            {
                result.SignOutRequired = false;
                return result;
            }
            else
            {
                result.SessionId = await _userSession.GetSessionIdAsync();
                result.ClientIds = await _userSession.GetClientListAsync();
                result.SignOutRequired = true;
            }

            result.User = user;

            LogSuccess(result);
            return result;
        }

        private void LogSuccess(SignOutValidationResult result)
        {
            // var log = JsonConvert.SerializeObject(result, Formatting.Indented);
            // _logger.LogInformation("End WS-Federation signin request validation\n{0}", log.ToString());
        }

        private void LogError(string message, SignOutValidationResult result)
        {
            // var log = JsonConvert.SerializeObject(result, Formatting.Indented);
            // _logger.LogError("{0}\n{1}", message, log.ToString());
        }
    }
}