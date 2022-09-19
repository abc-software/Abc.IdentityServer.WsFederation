// ----------------------------------------------------------------------------
// <copyright file="HttpRequestExtensionsEx.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;

namespace Abc.IdentityServer4.Extensions
{
    internal static class HttpRequestExtensionsEx
    {
        internal static bool HasApplicationFormContentType(this HttpRequest request)
        {
            if (request.ContentType is null)
            {
                return false;
            }

            if (MediaTypeHeaderValue.TryParse(request.ContentType, out var header))
            {
                // Content-Type: application/x-www-form-urlencoded; charset=utf-8
                return header.MediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}