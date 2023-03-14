// ----------------------------------------------------------------------------
// <copyright file="CustomRedirectResult.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer4.Extensions;
using Abc.IdentityServer4.WsFederation.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Abc.IdentityServer4.WsFederation.Endpoints.Results
{
    internal class CustomRedirectResult : IEndpointResult
    {
        private readonly ValidatedWsFederationRequest _request;
        private readonly string _url;
        private IdentityServerOptions _options;
        private ISystemClock _clock;
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

        internal CustomRedirectResult(ValidatedWsFederationRequest request, string url, IdentityServerOptions options, ISystemClock clock, IAuthorizationParametersMessageStore authorizationParametersMessageStore = null)
            : this(request, url)
        {
            _options = options;
            _clock = clock;
            _authorizationParametersMessageStore = authorizationParametersMessageStore;
        }

        public async Task ExecuteAsync(HttpContext context)
        {
            Init(context);

            var returnUrl = context.GetIdentityServerBasePath().EnsureTrailingSlash() + WsFederationConstants.ProtocolRoutePaths.WsFederationCallback;
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
                returnUrl = context.GetIdentityServerBaseUrl().EnsureTrailingSlash() + returnUrl.RemoveLeadingSlash();
            }

            var url = _url.AddQueryString(_options.UserInteraction.CustomRedirectReturnUrlParameter, returnUrl);
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