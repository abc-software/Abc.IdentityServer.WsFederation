using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation.Endpoints.Results
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
            if (redirectUrl.IsLocalUrl())
            {
                redirectUrl = context.GetIdentityServerRelativeUrl(redirectUrl);
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
