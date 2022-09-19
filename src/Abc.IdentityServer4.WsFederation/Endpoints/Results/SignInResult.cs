// ----------------------------------------------------------------------------
// <copyright file="SignInResult.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer4.Extensions;
using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Threading.Tasks;

namespace Abc.IdentityServer4.WsFederation.Endpoints.Results
{
    public class SignInResult : IEndpointResult
    {
        private IdentityServerOptions _options;

        public SignInResult(WsFederationMessage message)
        {
            Message = message ?? throw new System.ArgumentNullException(nameof(message));
        }

        internal SignInResult(WsFederationMessage message, IdentityServerOptions options) 
            : this(message)
        {
            _options = options;
        }

        /// <summary>
        /// Gets the WS-Federation message.
        /// </summary>
        /// <value>
        /// The WS-Federation message.
        /// </value>
        public WsFederationMessage Message { get; }

        /// <inheritdoc/>
        public Task ExecuteAsync(HttpContext context)
        {
            Init(context);

            context.Response.SetNoCache();
            context.Response.AddFormPostCspHeaders(_options.Csp, Message.IssuerAddress.GetOrigin(), "sha256-veRHIN/XAFeehi7cRkeVBpkKTuAUMFxwA+NMPmu2Bec=");

            var html = Message.BuildFormPost();
            return context.Response.WriteHtmlAsync(html);
        }

        private void Init(HttpContext context)
        {
            _options ??= context.RequestServices.GetRequiredService<IdentityServerOptions>();
        }
    }
}