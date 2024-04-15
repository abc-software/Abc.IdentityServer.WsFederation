// ----------------------------------------------------------------------------
// <copyright file="CustomRedirectResult.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer.Extensions;
using Abc.IdentityServer.WsFederation.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Abc.IdentityServer.WsFederation.Endpoints.Results
{
    internal class CustomRedirectResult : IEndpointResult
    {
        private readonly ValidatedWsFederationRequest _request;
        private readonly string _url;
        private IdentityServerOptions _options;
        private IClock _clock;
        private IServerUrls _urls;
        private IAuthorizationParametersMessageStore _authorizationParametersMessageStore;

        public CustomRedirectResult(ValidatedWsFederationRequest request, string url)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (url.IsMissing())
            {
                throw new ArgumentNullException(nameof(url));
            }

            _request = request;
            _url = url;
        }

        internal CustomRedirectResult(ValidatedWsFederationRequest request, string url, IdentityServerOptions options, IClock clock, IServerUrls urls, IAuthorizationParametersMessageStore authorizationParametersMessageStore = null)
            : this(request, url)
        {
            _options = options;
            _clock = clock;
            _urls = urls;
            _authorizationParametersMessageStore = authorizationParametersMessageStore;
        }

        public async Task ExecuteAsync(HttpContext context)
        {
            Init(context);

            var returnUrl = _urls.BaseUrl.EnsureTrailingSlash() + WsFederationConstants.ProtocolRoutePaths.WsFederationCallback;
            if (_authorizationParametersMessageStore != null)
            {
                var msg = new Message<IDictionary<string, string[]>>(_request.WsFederationMessage.ToDictionary(), _clock.UtcNow.UtcDateTime);
                returnUrl = returnUrl.AddQueryString(WsFederationConstants.DefaultRoutePathParams.MessageStoreIdParameterName, await _authorizationParametersMessageStore.WriteAsync(msg));
            }
            else
            {
                returnUrl = returnUrl.AddQueryString(_request.WsFederationMessage.Parameters);
            }

            if (!_url.IsLocalUrl())
            {
                // this converts the relative redirect path to an absolute one if we're 
                // redirecting to a different server
                returnUrl = _urls.BaseUrl.EnsureTrailingSlash() + returnUrl.RemoveLeadingSlash();
            }

            var url = _url.AddQueryString(_options.UserInteraction.CustomRedirectReturnUrlParameter, returnUrl);
            context.Response.Redirect(_urls.GetAbsoluteUrl(url));
        }

        private void Init(HttpContext context)
        {
            _options ??= context.RequestServices.GetRequiredService<IdentityServerOptions>();
            _authorizationParametersMessageStore ??= context.RequestServices.GetService<IAuthorizationParametersMessageStore>();
            _urls ??= context.RequestServices.GetRequiredService<IServerUrls>();
            _clock ??= context.RequestServices.GetRequiredService<IClock>();
        }
    }
}