using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.WsFederation.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation.Endpoints.Results
{
    public class SignOutResult : IEndpointResult
    {
        private readonly ValidatedWsFederationRequest _validatedRequest;
        private IdentityServerOptions _options;
        private IMessageStore<LogoutMessage> _logoutMessageStore;

        public SignOutResult(ValidatedWsFederationRequest validatedRequest)
        {
            _validatedRequest = validatedRequest;
        }

        internal SignOutResult(ValidatedWsFederationRequest validatedRequest, IdentityServerOptions options, IMessageStore<LogoutMessage> logoutMessageStore) 
            : this(validatedRequest)
        {
            _options = options;
            _logoutMessageStore = logoutMessageStore;
        }

        public async Task ExecuteAsync(HttpContext context)
        {
            Init(context);

            var logoutMessage = new LogoutMessage()
            {
                ClientId = _validatedRequest.Client?.ClientId,
                ClientName = _validatedRequest.Client?.ClientName,
                SubjectId = _validatedRequest.Subject?.GetSubjectId(),
                SessionId = _validatedRequest.SessionId,
                ClientIds = _validatedRequest.ClientIds,
                PostLogoutRedirectUri = _validatedRequest.ReplyUrl
            };

            string id = null;
            if (logoutMessage.ClientId != null && logoutMessage.ClientIds.Any())
            {
                var msg = new Message<LogoutMessage>(logoutMessage, DateTime.UtcNow);
                id = await _logoutMessageStore.WriteAsync(msg);
            }

            var redirectUrl = _options.UserInteraction.LogoutUrl;
            if (id != null)
            {
                redirectUrl = redirectUrl.AddQueryString(_options.UserInteraction.LogoutIdParameter, id);
            }

            context.Response.RedirectToAbsoluteUrl(redirectUrl);
        }

        private void Init(HttpContext context)
        {
            _options = _options ?? context.RequestServices.GetRequiredService<IdentityServerOptions>();
            _logoutMessageStore = _logoutMessageStore ?? context.RequestServices.GetRequiredService<IMessageStore<LogoutMessage>>();
        }
    }
}
