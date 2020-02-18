using IdentityServer4.Stores;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.WsFederation.Stores;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using IdentityServer4.Services;

namespace IdentityServer4.WsFederation.Validation
{
    public class SignOutValidator
    {
        private readonly IClientStore _clients;
        private readonly IRelyingPartyStore _relyingParties;
        private readonly WsFederationOptions _options;
        private readonly ISystemClock _clock;
        private readonly IUserSession _userSession;
        private readonly ILogger _logger;


        public SignOutValidator(
            WsFederationOptions options, 
            IClientStore clients,
            IRelyingPartyStore relyingParties,
            ISystemClock clock,
            IUserSession userSession,
            ILogger<SignOutValidator> logger)
        {
            _options = options;
            _clients = clients;
            _relyingParties = relyingParties;
            _clock = clock;
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
            if (client.Enabled == false)
            {
                LogError("Client is disabled: " + message.Wtrealm, result);

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
            result.ReplyUrl = client.RedirectUris.First();

            // check if additional relying party settings exist
            var rp = await _relyingParties.FindRelyingPartyByRealm(message.Wtrealm);
            if (rp == null)
            {
                rp = new RelyingParty
                {
                    TokenType = _options.DefaultTokenType,
                    SignatureAlgorithm = _options.DefaultSignatureAlgorithm,
                    DigestAlgorithm = _options.DefaultDigestAlgorithm,
                    SamlNameIdentifierFormat = _options.DefaultSamlNameIdentifierFormat,
                    ClaimMapping = _options.DefaultClaimMapping
                };
            }

            result.RelyingParty = rp;

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