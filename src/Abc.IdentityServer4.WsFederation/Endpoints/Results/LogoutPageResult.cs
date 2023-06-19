// ----------------------------------------------------------------------------
// <copyright file="LogoutPageResult.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Abc.IdentityServer.WsFederation.Endpoints.Results
{
    internal class LogoutPageResult : IEndpointResult
    {
        private IdentityServerOptions _options;

        public LogoutPageResult()
        {
        }

        internal LogoutPageResult(IdentityServerOptions options)
        {
            _options = options;
        }

        public Task ExecuteAsync(HttpContext context)
        {
            Init(context);

            var redirectUrl = _options.UserInteraction.LogoutUrl;
            context.Response.RedirectToAbsoluteUrl(redirectUrl);
            return Task.CompletedTask;
        }

        private void Init(HttpContext context)
        {
            _options ??= context.RequestServices.GetRequiredService<IdentityServerOptions>();
        }
    }
}