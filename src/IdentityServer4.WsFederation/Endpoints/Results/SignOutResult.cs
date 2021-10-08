using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation.Endpoints.Results
{
    public class SignOutResult : IEndpointResult
    {
        private readonly string _logoutId;
        private IdentityServerOptions _options;

        public SignOutResult(string logoutId)
        {
            _logoutId = logoutId;
        }

        internal SignOutResult(string logoutId, IdentityServerOptions options) 
            : this(logoutId)
        {
            _options = options;
        }

        public Task ExecuteAsync(HttpContext context)
        {
            Init(context);

            var redirectUrl = _options.UserInteraction.LogoutUrl;

            if (redirectUrl.IsLocalUrl())
            {
                redirectUrl = context.GetIdentityServerRelativeUrl(redirectUrl);
            }

            if (_logoutId != null)
            {
                redirectUrl = redirectUrl.AddQueryString(_options.UserInteraction.LogoutIdParameter, _logoutId);
            }

            context.Response.Redirect(redirectUrl);
            return Task.CompletedTask;
        }

        private void Init(HttpContext context)
        {
            _options = _options ?? context.RequestServices.GetRequiredService<IdentityServerOptions>();
        }
    }
}
