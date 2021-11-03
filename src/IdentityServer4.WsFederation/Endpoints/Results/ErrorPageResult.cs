using IdentityServer4.Hosting;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation.Endpoints.Results
{
    internal class ErrorPageResult : IEndpointResult
    {
        ISystemClock _clock;
        private IMessageStore<ErrorMessage> errorMessageStore;

        public ErrorPageResult(string error, string errorDescription)
        {
            Error = error;
            ErrorDescription = errorDescription;
        }

        public string Error { get; }
        public string ErrorDescription { get; }

        public async Task ExecuteAsync(HttpContext context)
        {
            Init(context);

            var message = new Message<ErrorMessage>(new ErrorMessage
            {
                RequestId = context.TraceIdentifier,
                Error = Error,
                ErrorDescription = ErrorDescription
            }, _clock.UtcNow.UtcDateTime);
            
            var id = await errorMessageStore.WriteAsync(message);

            //string url = identityServerOptions.UserInteraction.ErrorUrl.AddQueryString(identityServerOptions.UserInteraction.ErrorIdParameter, id);
            //context.Response.RedirectToAbsoluteUrl(url, pathConfiguration.BaseUrl);
        }

        private void Init(HttpContext context)
        {
            errorMessageStore = errorMessageStore ?? context.RequestServices.GetRequiredService<IMessageStore<ErrorMessage>>();
            //identityServerOptions = identityServerOptions ?? context.RequestServices.GetRequiredService<IWsFederationIdentityServerOptions>();
        }
    }
}
