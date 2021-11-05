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

            // check client
            var client = await _clients.FindEnabledClientByIdAsync(message.Wtrealm);
            if (client == null)
            {
                //LogError("Client not found: " + message.Wtrealm, result);

                return new SignOutValidationResult(validatedResult, "invalid_relying_party", "Cannot find Client configuration");
            }

            if (client.ProtocolType != IdentityServerConstants.ProtocolTypes.WsFederation)
            {
                //LogError("Client is not configured for WS-Federation", result);

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
            if (user == null ||
                user.Identity.IsAuthenticated == false)
            {
                return new SignOutValidationResult(validatedResult);
            }

            validatedResult.SessionId = await _userSession.GetSessionIdAsync();
            validatedResult.ClientIds = await _userSession.GetClientListAsync();
            validatedResult.Subject = user;

            // LogSuccess(result);
            return new SignOutValidationResult(validatedResult);
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