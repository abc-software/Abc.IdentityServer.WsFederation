using Microsoft.IdentityModel.Protocols.WsFederation;
using System;
using System.Net.Http;
using Actions = Microsoft.IdentityModel.Protocols.WsFederation.WsFederationConstants.WsFederationActions;

namespace Abc.IdentityServer4.WsFederation.IntegrationTests.Common
{
    public partial class IdentityServerPipeline
    {
        public const string WsFedSingleSignOnEndpoint = BaseUrl + "/wsfed";
        public const string WsFedSingleSignOnCallbackEndpoint = BaseUrl + "/wsfed/callback";
        public const string WsFedMetadataEndpoint = BaseUrl + "/wsfed/metadata";

        public HttpRequestMessage CreateSignIn(
            string clientId,
            string redirectUri = null,
            string state = null,
            string acr = null,
            string date = null,
            int? freshness = null,
            string method = null
           )
        {
            var wsMessage = new WsFederationMessage
            {
                IssuerAddress = WsFedSingleSignOnEndpoint,
                Wa = Actions.SignIn,
                Wtrealm = clientId,
                Wreply = redirectUri,
                Whr = acr,
                Wct = date,
                Wctx = state,
                Wfresh = freshness?.ToString(),
            };

            return ToHttpRequest(wsMessage, method);
        }

        public HttpRequestMessage CreateSignOut(
            string clientId,
            string redirectUri = null,
            string state = null,
            string method = null
           )
        {
            var wsMessage = new WsFederationMessage
            {
                IssuerAddress = WsFedSingleSignOnEndpoint,
                Wa = Actions.SignOut,
                Wtrealm = clientId,
                Wreply = redirectUri,
                Wctx = state,
            };

            return ToHttpRequest(wsMessage, method);
        }

        public HttpRequestMessage CreateSignOutCleanup(
            string clientId,
            string redirectUri = null,
            string state = null,
            string method = null
           )
        {
            var wsMessage = new WsFederationMessage
            {
                IssuerAddress = WsFedSingleSignOnEndpoint,
                Wa = Actions.SignOutCleanup,
                Wtrealm = clientId,
                Wreply = redirectUri,
                Wctx = state,
            };

            return ToHttpRequest(wsMessage, method);
        }

        public static HttpRequestMessage ToHttpRequest(WsFederationMessage wsMessage, string method)
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
