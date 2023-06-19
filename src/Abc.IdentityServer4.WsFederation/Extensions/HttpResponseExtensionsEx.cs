// ----------------------------------------------------------------------------
// <copyright file="HttpResponseExtensionsEx.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;

namespace Abc.IdentityServer.Extensions
{
    internal static class HttpResponseExtensionsEx
    {
        public static void AddFormPostCspHeaders(this HttpResponse response, CspOptions options, string origin, string hash)
        {
            var csp1part = options.Level == CspLevel.One ? "'unsafe-inline' " : string.Empty;
            var hashPart = hash.IsPresent() ? $"'{hash}' " : string.Empty;
            var cspHeader = $"default-src 'none'; frame-ancestors {origin}; script-src {csp1part}{hashPart}";

            HttpResponseExtensions.AddCspHeaders(response.Headers, options, cspHeader);
        }
    }
}