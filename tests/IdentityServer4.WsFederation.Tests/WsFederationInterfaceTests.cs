using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using IdentityServer4.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Net;
using Microsoft.IdentityModel.Tokens.Saml;
using System;
using System.Linq;
using IdentityServer4.Extensions;

namespace IdentityServer4.WsFederation.Tests
{
    public class WsFederationInterfaceTests
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;
        public WsFederationInterfaceTests()
        {
            var builder = new WebHostBuilder()
                 .ConfigureServices(InitializeServices)
                 .Configure(app =>
                 {
                     app.UseIdentityServer();
                    //  app.UseAuthorization();
                     app.UseAuthentication();
                     app.UseMvc(routes =>
                        routes.MapRoute(
                            "default",
                            "{controller}/{action=index}/{id?}"
                        )
                     );
                 });
            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        protected virtual void InitializeServices(IServiceCollection services)
        {
            var startupAssembly = typeof(Startup).GetTypeInfo().Assembly;
            var wsFedController = typeof(WsFederationController).GetTypeInfo().Assembly;
            var accountController = typeof(FakeAccountController).GetTypeInfo().Assembly;

            // Inject a custom application part manager. Overrides AddMvcCore() because that uses TryAdd().
            var manager = new ApplicationPartManager();
            manager.ApplicationParts.Add(new AssemblyPart(startupAssembly));
            manager.ApplicationParts.Add(new AssemblyPart(wsFedController));
            manager.ApplicationParts.Add(new AssemblyPart(accountController));

            manager.FeatureProviders.Add(new ControllerFeatureProvider());
            manager.FeatureProviders.Add(new ViewComponentFeatureProvider());

            services.AddSingleton(manager);
            services.TryAddSingleton<IKeyMaterialService>(
                new DefaultKeyMaterialService(
                    new IValidationKeysStore[] { }, 
                    new DefaultSigningCredentialsStore(TestCert.LoadSigningCredentials()))); 
            // TestLogger.Create<DefaultTokenCreationService>()));

            services.AddIdentityServer()
                .AddSigningCredential(TestCert.LoadSigningCredentials())
                .AddInMemoryClients(Config.GetClients())
                .AddInMemoryIdentityResources(Config.GetIdentityResources())
                .AddInMemoryRelyingParties(Config.GetRelyingParties())
                .AddTestUsers(Config.GetTestUsers())
                .AddWsFederation();
            services.TryAddTransient<IHttpContextAccessor, HttpContextAccessor>();
            services.AddMvc();
        }

        [Fact]
        public async Task WsFederation_metadata_success()
        {
            var response = await _client.GetAsync("/wsfederation");
            var message = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(message);
            Assert.True(message.StartsWith("<EntityDescriptor entityID=\"http://localhost\""));
        }

        [Fact]
        public async Task WsFederation_signin_request_user_not_authenticated_redirect_to_login_page_success()
        {
            var wsMessage = new WsFederationMessage
            {
                IssuerAddress = "/wsfederation",
                Wtrealm = "urn:owinrp",
                Wreply = "http://localhost:10313/",
            };
            var signInUrl = wsMessage.CreateSignInUrl();
            var response = await _client.GetAsync(signInUrl);
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            var expectedLocation = "/Account/Login?ReturnUrl=%2Fwsfederation%3Fwtrealm%3Durn%253Aowinrp%26wreply%3Dhttp%253A%252F%252Flocalhost%253A10313%252F%26wa%3Dwsignin1.0";
            Assert.Equal(expectedLocation, response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task WsFederation_signin_request_with_wfresh_set_to_0_user_is_authenticated_wfresh_exceeded_redirect_to_login_page_success()
        {
            // login user
            var subjectId = "user1";
            var loginUrl = string.Format("/account/login?subjectId={0}", WebUtility.UrlEncode(subjectId));
            var loginResponse = await _client.GetAsync(loginUrl);

            // create ws fed sigin message with wfresh
            var wsMessage = new WsFederationMessage
            {
                Wa = "wsignin1.0",
                IssuerAddress = "/wsfederation",
                Wtrealm = "urn:owinrp",
                Wreply = "http://localhost:10313/",
                Wfresh = "0",
            };
            var signInUrl = wsMessage.CreateSignInUrl();
            var request = new HttpRequestMessage(HttpMethod.Get, signInUrl);
            // test server doesnt save cookies between requests,
            // so we set them explicitly for the next request
            request.SetCookiesFromResponse(loginResponse);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            var expectedLocation = "/Account/Login?ReturnUrl=%2Fwsfederation%3Fwa%3Dwsignin1.0%26wtrealm%3Durn%253Aowinrp%26wreply%3Dhttp%253A%252F%252Flocalhost%253A10313%252F";
            Assert.Equal(expectedLocation, response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task WsFederation_signin_request_with_wfresh_set_to_0_user_is_authenticated_force_resignin_return_assertion_success()
        {
            // login user
            var subjectId = "user1";
            var loginUrl = string.Format("/account/login?subjectId={0}", WebUtility.UrlEncode(subjectId));
            var loginResponse = await _client.GetAsync(loginUrl);
            var authTime = DateTime.UtcNow;

            Thread.Sleep(3000); // TODO: bad workaround to sumulate login for 3 seconds

            // create ws fed sigin message with wfresh
            var wsMessage = new WsFederationMessage
            {
                Wa = "wsignin1.0",
                IssuerAddress = "/wsfederation",
                Wtrealm = "urn:owinrp",
                Wreply = "http://localhost:10313/",
                Wfresh = "0",
            };
            var uri = wsMessage.CreateSignInUrl();
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            // test server doesnt save cookies between requests,
            // so we set them explicitly for the next request
            request.SetCookiesFromResponse(loginResponse);

            // make auth request, for allready logged in user
            var response = await _client.SendAsync(request);

            // redirect to sign in package because we enforce it with wfresh=0
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            uri = response.Headers.Location.OriginalString + "&subjectId=" + subjectId;
            request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.SetCookiesFromResponse(response);

            // login again to satisfy wfresh=0
            response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            uri = response.Headers.Location.OriginalString;
            request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.SetCookiesFromResponse(response);

            // do the redirect to auth endpoint
            response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var contentAsText = await response.Content.ReadAsStringAsync();
            Assert.NotEqual(String.Empty, contentAsText);
            Assert.Contains("action=\"http://localhost:10313/\"", contentAsText);

            // extract wreturn to use it later to check if our token is a valid token
            var wreturn = ExtractInBetween(contentAsText, "wresult\" value=\"", "\"");
            var wsResponseMessage = new WsFederationMessage 
            {
                Wresult = WebUtility.HtmlDecode(wreturn),
            };
            var tokenString = wsResponseMessage.GetToken();
            var handler = new SamlSecurityTokenHandler();
            var canReadToken = handler.CanReadToken(tokenString);
            Assert.True(canReadToken);
            var token = handler.ReadSamlToken(tokenString);
            var authStatements = token.Assertion.Statements.OfType<SamlAuthenticationStatement>();
            Assert.Equal(1, authStatements.Count());
            var authStatement = authStatements.First();
            Assert.True(authStatement.AuthenticationInstant <= authTime.AddMinutes(5));
        }

        [Fact]
        public async Task WsFederation_signin_request_with_wfresh_user_is_authenticated_wfresh_in_time_frame_return_assertion_success()
        {
            // login user
            var subjectId = "user1";
            var loginUrl = string.Format("/account/login?subjectId={0}", WebUtility.UrlEncode(subjectId));
            var loginResponse = await _client.GetAsync(loginUrl);
            var authTime = DateTime.UtcNow;

            // create ws fed sigin message with wfresh=5
            var wsMessage = new WsFederationMessage
            {
                Wa = "wsignin1.0",
                IssuerAddress = "/wsfederation",
                Wtrealm = "urn:owinrp",
                Wreply = "http://localhost:10313/",
                Wfresh = "5",
            };
            var signInUrl = wsMessage.CreateSignInUrl();
            var request = new HttpRequestMessage(HttpMethod.Get, signInUrl);
            // test server doesnt save cookies between requests,
            // so we set them explicitly for the next request
            request.SetCookiesFromResponse(loginResponse);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var contentAsText = await response.Content.ReadAsStringAsync();
            Assert.NotEqual(String.Empty, contentAsText);
            Assert.Contains("action=\"http://localhost:10313/\"", contentAsText);

            // extract wreturn to use it later to check if our token is a valid token
            var wreturn = ExtractInBetween(contentAsText, "wresult\" value=\"", "\"");
            var wsResponseMessage = new WsFederationMessage 
            {
                Wresult = WebUtility.HtmlDecode(wreturn),
            };
            var tokenString = wsResponseMessage.GetToken();
            var handler = new SamlSecurityTokenHandler();
            var canReadToken = handler.CanReadToken(tokenString);
            Assert.True(canReadToken);
            var token = handler.ReadSamlToken(tokenString);
            var authStatements = token.Assertion.Statements.OfType<SamlAuthenticationStatement>();
            Assert.Equal(1, authStatements.Count());
            var authStatement = authStatements.First();
            Assert.True(authStatement.AuthenticationInstant <= authTime.AddMinutes(5));
        }

        [Fact]
        public async Task WsFederation_sigin_request_for_logged_in_user_return_assertion_success()
        {
            // login user
            var subjectId = "user1";
            var loginUrl = string.Format("/account/login?subjectId={0}", WebUtility.UrlEncode(subjectId));
            var loginResponse = await _client.GetAsync(loginUrl);
            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

            // create ws fed sign in message
            var wsSignInMessage = new WsFederationMessage
            {
                Wa = "wsignin1.0",
                IssuerAddress = "/wsfederation",
                Wtrealm = "urn:owinrp",
                Wreply = "http://localhost:10313/",
            };
            var signInUrl = wsSignInMessage.CreateSignInUrl();
            var request = new HttpRequestMessage(HttpMethod.Get, signInUrl);
            // test server doesnt save cookies between requests,
            // so we set them explicitly for the next request
            request.SetCookiesFromResponse(loginResponse);

            // send ws fed sign in request
            var wsResponse = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, wsResponse.StatusCode);
            var contentAsText = await wsResponse.Content.ReadAsStringAsync();
            Assert.NotEqual(String.Empty, contentAsText);
            Assert.Contains("action=\"http://localhost:10313/\"", contentAsText);
            // extract wreturn to use it later to check if our token is a valid token
            var wreturn = ExtractInBetween(contentAsText, "wresult\" value=\"", "\"");
            Assert.False(wreturn.StartsWith("%EF%BB%BF")); //don't start with BOM (Byte Order Mark)
            var wsMessage = new WsFederationMessage 
            {
                Wresult = WebUtility.HtmlDecode(wreturn),
            };
            var tokenString = wsMessage.GetToken();
            var handler = new SamlSecurityTokenHandler();
            var canReadToken = handler.CanReadToken(tokenString);
            Assert.True(canReadToken);
        }

        private string ExtractInBetween(string source, string startStr, string endStr)
        {
            var startIndex = source.IndexOf(startStr) + startStr.Length;
            var length = source.IndexOf(endStr, startIndex) - startIndex;
            var result = source.Substring(startIndex, length);
            return result;
        }
    }
}
