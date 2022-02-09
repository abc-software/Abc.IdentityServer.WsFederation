// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation.Endpoints.Results
{
    public class SignInResult : IEndpointResult
    {
        private IdentityServerOptions _options;

        public WsFederationMessage Message { get; set; }

        public SignInResult(WsFederationMessage message)
        {
            Message = message;
        }

        internal SignInResult(WsFederationMessage message, IdentityServerOptions options)
            : this(message)
        {
            _options = options;
        }

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