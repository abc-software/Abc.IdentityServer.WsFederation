using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.WsFederation.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation.Endpoints.Results
{
    internal class CustomRedirectResult : IEndpointResult
    {
        private readonly ValidatedWsFederationRequest _request;
        private readonly string _url;
        private IdentityServerOptions _options;
        private IAuthorizationParametersMessageStore _authorizationParametersMessageStore;

        public CustomRedirectResult(ValidatedWsFederationRequest request, string url)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (url.IsMissing()) throw new ArgumentNullException(nameof(url));

            _request = request;
            _url = url;
        }

        internal CustomRedirectResult(ValidatedWsFederationRequest request, string url, IdentityServerOptions options, IAuthorizationParametersMessageStore authorizationParametersMessageStore = null)
            : this(request, url)
        {
            _options = options;
            _authorizationParametersMessageStore = authorizationParametersMessageStore;
        }

        public async Task ExecuteAsync(HttpContext context)
        {
            Init(context);

            var returnUrl = context.GetIdentityServerBasePath().EnsureTrailingSlash() + WsFederationConstants.ProtocolRoutePaths.WsFederation;
            if (_authorizationParametersMessageStore != null)
            {
                var msg = new Message<IDictionary<string, string[]>>(_request.WsFederationMessage.ToDictionary(), DateTime.Now);
                returnUrl = returnUrl.AddQueryString(WsFederationConstants.DefaultRoutePathParams.MessageStoreIdParameterName, await _authorizationParametersMessageStore.WriteAsync(msg));
            }
            else
            {
                //returnUrl = returnUrl.AddQueryString(_request.Raw.ToQueryString());
                returnUrl = returnUrl.AddQueryString(_request.WsFederationMessage.ToQueryString());
            }

            if (!_url.IsLocalUrl())
            {
                // this converts the relative redirect path to an absolute one if we're 
                // redirecting to a different server
                returnUrl = context.GetIdentityServerBaseUrl().EnsureTrailingSlash() + returnUrl.RemoveLeadingSlash();
            }

            var url = _url.AddQueryString(_options.UserInteraction.CustomRedirectReturnUrlParameter, returnUrl);
            context.Response.RedirectToAbsoluteUrl(url);
        }

        private void Init(HttpContext context)
        {
            _options ??= context.RequestServices.GetRequiredService<IdentityServerOptions>();
            _authorizationParametersMessageStore ??= context.RequestServices.GetService<IAuthorizationParametersMessageStore>();
        }
    }
}
