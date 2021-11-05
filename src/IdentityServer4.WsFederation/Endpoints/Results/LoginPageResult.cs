using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.WsFederation.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation.Endpoints.Results
{
    public class LoginPageResult : IEndpointResult
    {
        private readonly ValidatedWsFederationRequest _request;

        private IdentityServerOptions _options;
        private WsFederationOptions _wsFederationOptions;
        private IAuthorizationParametersMessageStore _authorizationParametersMessageStore;

        public LoginPageResult(ValidatedWsFederationRequest request)
        {
            _request = request;
        }

        internal LoginPageResult(ValidatedWsFederationRequest request, IdentityServerOptions options, WsFederationOptions wsFederationOptions, IAuthorizationParametersMessageStore authorizationParametersMessageStore = null)
            : this(request)
        {
            _options = options;
            _wsFederationOptions = wsFederationOptions;
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

            string returnUrl = context.GetIdentityServerBasePath().EnsureTrailingSlash() + WsFederationConstants.ProtocolRoutePaths.WsFederation;
            if (_authorizationParametersMessageStore != null)
            {
                IDictionary<string, string[]> dictionary = new Dictionary<string, string[]>();
                foreach(var p in _request.WsFederationMessage.Parameters)
                {
                    dictionary.Add(p.Key, new string[] { p.Value });
                }

                var msg = new Message<IDictionary<string, string[]>>(dictionary, DateTime.UtcNow);
                var id = await _authorizationParametersMessageStore.WriteAsync(msg);
                returnUrl = returnUrl.AddQueryString(WsFederationConstants.AuthorizationParamsStore.MessageStoreIdParameterName, id);
            }
            else
            {
                returnUrl = returnUrl.AddQueryString(_request.WsFederationMessage.BuildRedirectUrl());
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
            _options = _options ?? context.RequestServices.GetRequiredService<IdentityServerOptions>();
            _wsFederationOptions = _wsFederationOptions ?? context.RequestServices.GetRequiredService<WsFederationOptions>();
            _authorizationParametersMessageStore = _authorizationParametersMessageStore ?? context.RequestServices.GetService<IAuthorizationParametersMessageStore>();
        }
    }
}
