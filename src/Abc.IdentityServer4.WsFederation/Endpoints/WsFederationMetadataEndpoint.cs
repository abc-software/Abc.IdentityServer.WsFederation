// ----------------------------------------------------------------------------
// <copyright file="WsFederationMetadataEndpoint.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer4.WsFederation.ResponseProcessing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks;

namespace Abc.IdentityServer4.WsFederation.Endpoints
{
    internal class WsFederationMetadataEndpoint : IEndpointHandler
    {
        private readonly ILogger _logger;
        private readonly IMetadataResponseGenerator _metadata;

        public WsFederationMetadataEndpoint(
            IMetadataResponseGenerator metadata, 
            ILogger<WsFederationMetadataEndpoint> logger)
        {
            _metadata = metadata;
            _logger = logger;
        }

        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            if (!HttpMethods.IsGet(context.Request.Method))
            {
                _logger.LogWarning("Metadata endpoint only supports GET requests");
                return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
            }

            _logger.LogDebug("Start WS-Federation metadata request");
            var entity = await _metadata.GenerateAsync();
            return new Results.MetadataResult(entity);
        }
    }
}