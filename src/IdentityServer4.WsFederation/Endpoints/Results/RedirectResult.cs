using IdentityServer4.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation.Endpoints.Results
{
    internal class RedirectResult : IEndpointResult
    {
        private readonly string _location;

        public RedirectResult(string location)
        {
            _location = location;
        }

        public Task ExecuteAsync(HttpContext context)
        {
            context.Response.Redirect(_location);
            return Task.CompletedTask;
        }
    }
}
