using Abc.IdentityServer4.Extensions;
using Abc.IdentityServer4.WsFederation.Validation;
using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Abc.IdentityServer4.WsFederation.Endpoints.Results
{
    public class LoginPageResult : IEndpointResult
    {
        private readonly ValidatedWsFederationRequest _request;
        private IdentityServerOptions _options;
        private ISystemClock _clock;
        private IAuthorizationParametersMessageStore _authorizationParametersMessageStore;

        public LoginPageResult(ValidatedWsFederationRequest request)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));
        }

        internal LoginPageResult(ValidatedWsFederationRequest request, IdentityServerOptions options, ISystemClock clock, IAuthorizationParametersMessageStore authorizationParametersMessageStore = null)
            : this(request)
        {
            _options = options;
            _clock = clock;
            _authorizationParametersMessageStore = authorizationParametersMessageStore;
        }

        /// <summary>
        /// Executes the result.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns></returns>
        public async Task ExecuteAsync(HttpContext context)
        {
            Init(context);

            string returnUrl = context.GetIdentityServerBasePath().EnsureTrailingSlash() + WsFederationConstants.ProtocolRoutePaths.WsFederationCallback;
            if (_authorizationParametersMessageStore != null)
            {
                var msg = new Message<IDictionary<string, string[]>>(_request.WsFederationMessage.ToDictionary(), _clock.UtcNow.UtcDateTime);
                var id = await _authorizationParametersMessageStore.WriteAsync(msg);
                returnUrl = returnUrl.AddQueryString(WsFederationConstants.DefaultRoutePathParams.MessageStoreIdParameterName, id);
            }
            else
            {
                returnUrl = returnUrl.AddQueryString(_request.WsFederationMessage.Parameters);
            }

            var loginUrl = _options.UserInteraction.LoginUrl;
            if (!loginUrl.IsLocalUrl())
            {
                // this converts the relative redirect path to an absolute one if we're 
                // redirecting to a different server
                returnUrl = context.GetIdentityServerHost().EnsureTrailingSlash() + returnUrl.RemoveLeadingSlash();
            }

            var url = loginUrl.AddQueryString(_options.UserInteraction.LoginReturnUrlParameter, returnUrl);
            context.Response.RedirectToAbsoluteUrl(url);
        }

        private void Init(HttpContext context)
        {
            _options ??= context.RequestServices.GetRequiredService<IdentityServerOptions>();
            _authorizationParametersMessageStore ??= context.RequestServices.GetService<IAuthorizationParametersMessageStore>();
            _clock ??= context.RequestServices.GetRequiredService<ISystemClock>();
        }
    }
}
