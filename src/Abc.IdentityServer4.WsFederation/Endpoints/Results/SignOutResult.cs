// ----------------------------------------------------------------------------
// <copyright file="SignOutResult.cs" company="ABC software Ltd">
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
using System.Linq;
using System.Threading.Tasks;

namespace Abc.IdentityServer4.WsFederation.Endpoints.Results
{
    public class SignOutResult : IEndpointResult
    {
        private readonly ValidatedWsFederationRequest _validatedRequest;
        private IdentityServerOptions _options;
        private ISystemClock _clock;
        private IMessageStore<LogoutMessage> _logoutMessageStore;

        public SignOutResult(ValidatedWsFederationRequest validatedRequest)
        {
            _validatedRequest = validatedRequest ?? throw new ArgumentNullException(nameof(validatedRequest));
        }

        internal SignOutResult(ValidatedWsFederationRequest validatedRequest, IdentityServerOptions options, ISystemClock clock, IMessageStore<LogoutMessage> logoutMessageStore) 
            : this(validatedRequest)
        {
            _options = options;
            _clock = clock;
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
                PostLogoutRedirectUri = _validatedRequest.ReplyUrl,
            };

            string id = null;
            if (logoutMessage.ClientId.IsPresent() || logoutMessage.ClientIds?.Any() == true)
            {
                var msg = new Message<LogoutMessage>(logoutMessage, _clock.UtcNow.UtcDateTime);
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
            _options ??= context.RequestServices.GetRequiredService<IdentityServerOptions>();
            _logoutMessageStore ??= context.RequestServices.GetRequiredService<IMessageStore<LogoutMessage>>();
            _clock ??= context.RequestServices.GetRequiredService<ISystemClock>();
        }
    }
}