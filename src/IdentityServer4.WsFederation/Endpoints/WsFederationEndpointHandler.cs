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
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation.Endpoints
{
    public class WsFederationEndpointHandler : IEndpointHandler
    {
        private readonly IUserSession _userSession;
        private readonly IEventService _events;
        private readonly ISignInResponseGenerator _generator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger _logger;
        private readonly IMetadataResponseGenerator _metadata;
        private readonly IdentityServerOptions _options;
        private readonly ISignInInteractionResponseGenerator _interaction;
        private readonly ISignInValidator _signinValidator;
        private readonly ISignOutValidator _signoutValidator;
        private readonly IAuthorizationParametersMessageStore _authorizationParametersMessageStore;

        public WsFederationEndpointHandler(
            IMetadataResponseGenerator metadata, 
            ISignInValidator signinValidator,
            ISignOutValidator signoutValidator, 
            IdentityServerOptions options,
            ISignInInteractionResponseGenerator interaction,
            ISignInResponseGenerator generator,
            IHttpContextAccessor httpContextAccessor,
            IUserSession userSession,
            ISystemClock clock,
            IMessageStore<LogoutMessage> logoutMessageStore,
            IEventService events,
            ILogger<WsFederationEndpointHandler> logger,
            IAuthorizationParametersMessageStore authorizationParametersMessageStore = null)
        {
            _metadata = metadata;
            _signinValidator = signinValidator;
            _signoutValidator = signoutValidator;
            _options = options;
            _interaction = interaction;
            _generator = generator;
            _httpContextAccessor = httpContextAccessor;
            _userSession = userSession;
            _events = events;
            _logger = logger;
            _authorizationParametersMessageStore = authorizationParametersMessageStore;
        }

        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            // GET + no parameters = metadata request
            if (HttpMethods.IsGet(context.Request.Method) && !context.Request.QueryString.HasValue)
            {
                _logger.LogDebug("Start WS-Federation metadata request");
                var entity = await _metadata.GenerateAsync();
                return new Results.MetadataResult(entity);
            }

            var url = context.Request.GetEncodedUrl();
            _logger.LogDebug("Start WS-Federation request: {url}", url);

            // user can be null here (this differs from HttpContext.User where the anonymous user is filled in)
            var user = await _userSession.GetUserAsync();

            var messageStoreId = context.Request.Query[WsFederationConstants.DefaultRoutePathParams.MessageStoreIdParameterName];
            if (!string.IsNullOrWhiteSpace(messageStoreId))
            {
                return await ProcessSignInCallbackAsync(messageStoreId, user);
            }

            var message = context.Request.HasFormContentType
                ? context.Request.Form.ToWsFederationMessage()
                : context.Request.Query.ToWsFederationMessage();

            if (message.IsSignInMessage)
            {
                return await ProcessSignInAsync(message, user);
            }

            if (message.IsSignOutMessage)
            {
                return await ProcessSignOutAsync(message, user);
            }

            return new StatusCodeResult(HttpStatusCode.BadRequest);
        }

        internal async Task<IEndpointResult> ProcessSignInAsync(WsFederationMessage signin, ClaimsPrincipal user)
        {
            if (user != null && user.Identity.IsAuthenticated)
            {
                _logger.LogDebug("User in WS-Federation signin request: {subjectId}", user.GetSubjectId());
            }
            else
            {
                _logger.LogDebug("No user present in WS-Federation signin request");
            }

            var validationResult = await _signinValidator.ValidateAsync(signin, user);
            if (validationResult.IsError)
            {
                return await CreateSignInErrorResult(
                    "WS-Federation sign in request validation failed", 
                    validationResult.ValidatedRequest, 
                    validationResult.Error, 
                    validationResult.ErrorDescription);
            }

            var interactionResult = await _interaction.ProcessInteractionAsync(validationResult.ValidatedRequest);
            if (interactionResult.IsError)
            {
                return await CreateSignInErrorResult(
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
            await _userSession.AddClientIdAsync(validationResult.ValidatedRequest.ClientId);
            
            await _events.RaiseAsync(new Events.SignInTokenIssuedSuccessEvent(responseMessage, validationResult));
            
            return new Results.SignInResult(responseMessage);
        }

        internal async Task<IEndpointResult> ProcessSignOutAsync(WsFederationMessage message, ClaimsPrincipal user)
        {
            if (string.IsNullOrWhiteSpace(message.Wreply) ||
                string.IsNullOrWhiteSpace(message.Wtrealm))
            {
                return new Results.LogoutPageResult();
            }

            var validationResult = await _signoutValidator.ValidateAsync(message);
            if (validationResult.IsError)
            {
                return await CreateSignOutErrorResult(
                    "WS-Federation sign out request validation failed", 
                    validationResult.ValidatedRequest, 
                    validationResult.Error, 
                    validationResult.ErrorDescription);
            }

            return new Results.SignOutResult(validationResult.ValidatedRequest);
        }

        internal async Task<IEndpointResult> ProcessSignInCallbackAsync(string messageStoreId, ClaimsPrincipal user)
        {
            if (_authorizationParametersMessageStore != null)
            {
                var data = await _authorizationParametersMessageStore.ReadAsync(messageStoreId);
                await _authorizationParametersMessageStore.DeleteAsync(messageStoreId);

                var message = data.Data.ToWsFederationMessage();
                if (message.IsSignInMessage)
                {
                    return await ProcessSignInAsync(message, user);
                }
            }

            return new StatusCodeResult(HttpStatusCode.BadRequest);
        }

        protected async Task<IEndpointResult> CreateSignInErrorResult(
            string logMessage, 
            ValidatedWsFederationRequest request = null, 
            string error = "Server", 
            string errorDescription = null, 
            bool logError = true)
        {
            if (logError)
            {
                _logger.LogError(logMessage);
            }

            if (request != null)
            {
                _logger.LogInformation("{@validationDetails}", new Logging.ValidatedWsFederationRequestLog(request, new string[0]));
            }

            await _events.RaiseAsync(new Events.SignInTokenIssuedFailureEvent(request, error, errorDescription));

            return new Results.ErrorPageResult(error, errorDescription);
        }

        protected Task<IEndpointResult> CreateSignOutErrorResult(
            string logMessage, 
            ValidatedWsFederationRequest request = null, 
            string error = "Server", 
            string errorDescription = null, 
            bool logError = true)
        {
            if (logError)
            {
                _logger.LogError(logMessage);
            }

            if (request != null)
            {
                _logger.LogInformation("{@validationDetails}", new Logging.ValidatedWsFederationRequestLog(request, new string[0]));
            }

            return Task.FromResult<IEndpointResult>(new Results.ErrorPageResult(error, errorDescription));
        }
    }
}