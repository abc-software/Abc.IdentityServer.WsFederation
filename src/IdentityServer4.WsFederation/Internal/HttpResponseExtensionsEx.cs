using IdentityServer4.Configuration;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Http;

namespace IdentityServer4.Extensions
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
