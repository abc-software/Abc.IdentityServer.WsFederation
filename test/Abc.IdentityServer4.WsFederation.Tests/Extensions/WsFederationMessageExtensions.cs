using System;
using System.Net.Http;

namespace Microsoft.IdentityModel.Protocols.WsFederation
{
    internal static class WsFederationMessageExtensions
    {
        public static HttpRequestMessage ToHttpRequest(this WsFederationMessage wsMessage, string method)
        {
            var request = new HttpRequestMessage();
            if (method == "post")
            {
                request.RequestUri = new Uri(wsMessage.IssuerAddress, UriKind.RelativeOrAbsolute);
                request.Method = HttpMethod.Post;
                request.Content = new FormUrlEncodedContent(wsMessage.Parameters);
            }
            else
            {
                request.RequestUri = new Uri(wsMessage.BuildRedirectUrl(), UriKind.RelativeOrAbsolute);
                request.Method = HttpMethod.Get;
            }

            return request;
        }
    }
}
