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
        private IServerUrls _urls;

        public LogoutPageResult()
        {
        }

        internal LogoutPageResult(IdentityServerOptions options, IServerUrls urls)
        {
            _options = options;
            _urls = urls;
        }

        public Task ExecuteAsync(HttpContext context)
        {
            Init(context);

            var redirectUrl = _options.UserInteraction.LogoutUrl;
            context.Response.Redirect(_urls.GetAbsoluteUrl(redirectUrl));
            return Task.CompletedTask;
        }

        private void Init(HttpContext context)
        {
            _options ??= context.RequestServices.GetRequiredService<IdentityServerOptions>();
            _urls ??= context.RequestServices.GetRequiredService<IServerUrls>();
        }
    }
}