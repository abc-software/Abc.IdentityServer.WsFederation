// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation.Endpoints.Results
{
    public class SignInResult : IEndpointResult
    {
        public WsFederationMessage Message { get; set; }

        public SignInResult(WsFederationMessage message)
        {
            Message = message;
        }

        public Task ExecuteAsync(HttpContext context)
        {
            context.Response.ContentType = "text/html";
            var message = Message.BuildFormPost();
            return context.Response.WriteAsync(message);
        }
    }
}