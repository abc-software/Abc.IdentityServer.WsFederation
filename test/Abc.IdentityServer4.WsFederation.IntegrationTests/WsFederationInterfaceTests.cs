using FluentAssertions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Abc.IdentityServer4.WsFederation.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Microsoft.IdentityModel.Tokens.Saml;
using Xunit;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Abc.IdentityServer4.WsFederation.IntegrationTests
{
    [Trait("Category", "WsFederation")]
    public class WsFederationInterfaceTests
    {
        private readonly HttpClient _client;
        public WsFederationInterfaceTests()
        {
            var builder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                webHost
                    .UseTestServer()
                    .ConfigureServices(InitializeServices)
                    .Configure(app =>
                     {
                         app.UseIdentityServer();
                         //  app.UseAuthorization();
                         app.UseAuthentication();
                         app.UseRouting();
                         app.UseEndpoints(c =>
                         {
                             c.MapControllerRoute(
                                 "default",
                                "{controller}/{action=index}/{id?}"
                                );
                         });
                     }));

            var host = builder.Start();
            _client = host.GetTestClient();
        }

        protected virtual void InitializeServices(IServiceCollection services)
        {
            var startupAssembly = typeof(WsFederationInterfaceTests).GetTypeInfo().Assembly;
            var wsFedController = typeof(WsFederationEndpoint).GetTypeInfo().Assembly;
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
                    new[] { new InMemorySigningCredentialsStore(TestCert.LoadSigningCredentials()) })); 
            // TestLogger.Create<DefaultTokenCreationService>()));

            services.AddIdentityServer()
                .AddSigningCredential(TestCert.LoadSigningCredentials())
                .AddInMemoryClients(Config.GetClients())
                .AddInMemoryApiScopes(Config.GetApiScopes())
                .AddInMemoryIdentityResources(Config.GetIdentityResources())
                .AddInMemoryRelyingParties(Config.GetRelyingParties())
                .AddTestUsers(Config.GetTestUsers())
                .AddWsFederation();
            services.TryAddTransient<IHttpContextAccessor, HttpContextAccessor>();
            services.AddMvc();
        }

        [Fact]
        public async Task request_should_not_return_404()
        {
            var response = await _client.GetAsync("/wsfed");

            response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task post_request_without_form_should_return_415()
        {
            var response = await _client.PostAsync("/wsfed", new StringContent("foo"));

            response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        }

        [Fact]
        public async Task attribute_request_should_return_400()
        {
            var wsMessage = new WsFederationMessage
            {
                IssuerAddress = "/wsfed",
                Wtrealm = "urn:owinrp",
                Wreply = "http://localhost:10313/",
                Wa = "wattr1.0",
            };

            var response = await _client.GetAsync(wsMessage.BuildRedirectUrl());

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task metadata_success()
        {
            var response = await _client.GetAsync("/wsfed/metadata");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var message = await response.Content.ReadAsStringAsync();
            message.Should().NotBeEmpty();
            message.Should().StartWith("<EntityDescriptor entityID=\"http://localhost\"");
        }

        [Fact]
        public async Task post_metadata_should_return_405()
        {
            var response = await _client.PostAsync("/wsfed/metadata", new StringContent(string.Empty));
            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        }

        [Theory]
        [InlineData("get")]
        [InlineData("post")]
        public async Task signin_request_user_not_authenticated_redirect_to_login_page_success(string method)
        {
            var wsMessage = new WsFederationMessage
            {
                IssuerAddress = "/wsfed",
                Wtrealm = "urn:owinrp",
                Wreply = "http://localhost:10313/",
                Wa = "wsignin1.0",
            };

            var response = await _client.SendAsync(wsMessage.ToHttpRequest(method));
            response.StatusCode.Should().Be(HttpStatusCode.Found);

            var expectedLocation = "http://localhost/Account/Login?ReturnUrl=%2Fwsfed%2Fcallback%3Fwtrealm%3Durn%253Aowinrp%26wreply%3Dhttp%253A%252F%252Flocalhost%253A10313%252F%26wa%3Dwsignin1.0";
            response.Headers.Location.OriginalString.Should().Be(expectedLocation);
        }

        [Fact]
        public async Task signin_request_with_wfresh_set_to_0_user_is_authenticated_wfresh_exceeded_redirect_to_login_page_success()
        {
            // login user
            var subjectId = "user1";
            var loginUrl = string.Format("/account/login?subjectId={0}", WebUtility.UrlEncode(subjectId));
            var loginResponse = await _client.GetAsync(loginUrl);

            // create ws fed sigin message with wfresh
            var wsMessage = new WsFederationMessage
            {
                Wa = "wsignin1.0",
                IssuerAddress = "/wsfed",
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
            response.StatusCode.Should().Be(HttpStatusCode.Found);

            var expectedLocation = "http://localhost/Account/Login?ReturnUrl=%2Fwsfed%2Fcallback%3Fwa%3Dwsignin1.0%26wtrealm%3Durn%253Aowinrp%26wreply%3Dhttp%253A%252F%252Flocalhost%253A10313%252F";
            response.Headers.Location.OriginalString.Should().Be(expectedLocation);
        }

        [Fact]
        public async Task signin_request_with_wfresh_set_to_0_user_is_authenticated_force_resignin_return_assertion_success()
        {
            // login user
            var subjectId = "user1";
            var loginUrl = string.Format("/account/login?subjectId={0}", WebUtility.UrlEncode(subjectId));
            var loginResponse = await _client.GetAsync(loginUrl);
            var authTime = DateTime.UtcNow;

            Thread.Sleep(3000); // TODO: bad workaround to sumulate login for 3 seconds

            // create ws-fed sigin message with wfresh
            var wsMessage = new WsFederationMessage
            {
                Wa = "wsignin1.0",
                IssuerAddress = "/wsfed",
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
            response.StatusCode.Should().Be(HttpStatusCode.Redirect);

            uri = response.Headers.Location.OriginalString + "&subjectId=" + subjectId;
            request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.SetCookiesFromResponse(response);

            // login again to satisfy wfresh=0
            response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);

            uri = response.Headers.Location.OriginalString;
            request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.SetCookiesFromResponse(response);

            // do the redirect to auth endpoint
            response = await _client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var contentAsText = await response.Content.ReadAsStringAsync();
            contentAsText.Should().NotBeEmpty();
            contentAsText.Should().Contain("action=\"http://localhost:10313/\"");

            // extract wreturn to use it later to check if our token is a valid token
            var wreturn = ExtractInBetween(contentAsText, "wresult\" value=\"", "\"");
            var wsResponseMessage = new WsFederationMessage 
            {
                Wresult = WebUtility.HtmlDecode(wreturn),
            };
            var tokenString = wsResponseMessage.GetToken();
            var handler = new SamlSecurityTokenHandler();
            var canReadToken = handler.CanReadToken(tokenString);
            canReadToken.Should().BeTrue();

            var token = handler.ReadSamlToken(tokenString);
            var authStatements = token.Assertion.Statements.OfType<SamlAuthenticationStatement>();
            authStatements.Should().ContainSingle();
            var authStatement = authStatements.First();
            Assert.True(authStatement.AuthenticationInstant <= authTime.AddMinutes(5));
        }

        [Fact]
        public async Task signin_request_with_wfresh_user_is_authenticated_wfresh_in_time_frame_return_assertion_success()
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
                IssuerAddress = "/wsfed",
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var contentAsText = await response.Content.ReadAsStringAsync();
            contentAsText.Should().NotBeEmpty();
            contentAsText.Should().Contain("action=\"http://localhost:10313/\"");

            // extract wreturn to use it later to check if our token is a valid token
            var wreturn = ExtractInBetween(contentAsText, "wresult\" value=\"", "\"");
            var wsResponseMessage = new WsFederationMessage 
            {
                Wresult = WebUtility.HtmlDecode(wreturn),
            };
            var tokenString = wsResponseMessage.GetToken();
            var handler = new SamlSecurityTokenHandler();
            var canReadToken = handler.CanReadToken(tokenString);
            canReadToken.Should().BeTrue();
           
            var token = handler.ReadSamlToken(tokenString);
            var authStatements = token.Assertion.Statements.OfType<SamlAuthenticationStatement>();
            authStatements.Should().ContainSingle();

            var authStatement = authStatements.First();
            Assert.True(authStatement.AuthenticationInstant <= authTime.AddMinutes(5));
        }

        [Theory]
        [InlineData("get")]
        [InlineData("post")]
        public async Task sigin_request_for_logged_in_user_return_assertion_success(string method)
        {
            // login user
            var subjectId = "user1";
            var loginUrl = string.Format("/account/login?subjectId={0}", WebUtility.UrlEncode(subjectId));
            var loginResponse = await _client.GetAsync(loginUrl);
            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // create ws fed sign in message
            var wsSignInMessage = new WsFederationMessage
            {
                Wa = "wsignin1.0",
                IssuerAddress = "/wsfed",
                Wtrealm = "urn:owinrp",
                Wreply = "http://localhost:10313/",
            };

            var request = wsSignInMessage.ToHttpRequest(method);
            // test server doesnt save cookies between requests,
            // so we set them explicitly for the next request
            request.SetCookiesFromResponse(loginResponse);

            // send ws fed sign in request
            var wsResponse = await _client.SendAsync(request);
            wsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var contentAsText = await wsResponse.Content.ReadAsStringAsync();
            contentAsText.Should().NotBeEmpty();
            contentAsText.Should().Contain("action=\"http://localhost:10313/\"");
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

        [Theory]
        [InlineData("get")]
        [InlineData("post")]
        public async Task signout_request_with_no_wtrealm_redirect_to_logout_page_success(string method)
        {
            // login user
            var subjectId = "user1";
            var loginUrl = string.Format("/account/login?subjectId={0}", WebUtility.UrlEncode(subjectId));
            var loginResponse = await _client.GetAsync(loginUrl);

            // create ws fed sigin message with wfresh
            var wsMessage = new WsFederationMessage
            {
                Wa = "wsignout1.0",
                IssuerAddress = "/wsfed",
                // Wtrealm = "urn:owinrp",
                Wreply = "http://localhost:10313/",
            };

            var request = wsMessage.ToHttpRequest(method);
            // test server doesnt save cookies between requests,
            // so we set them explicitly for the next request
            request.SetCookiesFromResponse(loginResponse);

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Found);
            var expectedLocation = "http://localhost/Account/Logout";
            response.Headers.Location.OriginalString.Should().Be(expectedLocation);
        }

        [Theory]
        [InlineData("get")]
        [InlineData("post")]
        public async Task signout_request_redirect_to_logout_page_success(string method)
        {
            // login user
            var subjectId = "user1";
            var loginUrl = string.Format("/account/login?subjectId={0}", WebUtility.UrlEncode(subjectId));
            var loginResponse = await _client.GetAsync(loginUrl);

            // create ws fed sigin out message
            var wsMessage = new WsFederationMessage
            {
                Wa = "wsignout1.0",
                IssuerAddress = "/wsfed",
                Wtrealm = "urn:owinrp",
                Wreply = "http://localhost:10313/",
            };

            var request = wsMessage.ToHttpRequest(method);
            // test server doesnt save cookies between requests,
            // so we set them explicitly for the next request
            request.SetCookiesFromResponse(loginResponse);

            var response = await _client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.Found);

            var expectedLocation = "http://localhost/Account/Logout?logoutId=";
            response.Headers.Location.OriginalString.Should().StartWith(expectedLocation);
        }

        [Theory]
        [InlineData("get")]
        [InlineData("post")]
        public async Task signoutcleanup_request_redirect_to_logout_page_success(string method)
        {
            // login user
            var subjectId = "user1";
            var loginUrl = string.Format("/account/login?subjectId={0}", WebUtility.UrlEncode(subjectId));
            var loginResponse = await _client.GetAsync(loginUrl);

            // create ws fed sigout cleanup message
            var wsMessage = new WsFederationMessage
            {
                Wa = "wsignoutcleanup1.0",
                IssuerAddress = "/wsfed",
                Wtrealm = "urn:owinrp",
                Wreply = "http://localhost:10313/",
            };

            var request = wsMessage.ToHttpRequest(method);

            // test server doesnt save cookies between requests,
            // so we set them explicitly for the next request
            request.SetCookiesFromResponse(loginResponse);

            var response = await _client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.Found);

            var expectedLocation = "http://localhost/Account/Logout?logoutId=";
            response.Headers.Location.OriginalString.Should().StartWith(expectedLocation);
        }
    }
}
