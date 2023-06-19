using Abc.IdentityServer.WsFederation.IntegrationTests.Common;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Microsoft.IdentityModel.Tokens.Saml;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Xunit;

namespace Abc.IdentityServer.WsFederation.Endpoints.IntegrationTests
{
    public class WsFederationEndpointFixture
    {
        private const string Category = "WS-Federation single sign-on endpoint";

        private IdentityServerPipeline _mockPipeline = new IdentityServerPipeline();

        public WsFederationEndpointFixture()
        {
            _mockPipeline.Clients.Add(new Client
            {
                ClientId = "urn:client1",
                RequireConsent = false,
                ProtocolType = "wsfed",
                AllowedScopes = new List<string> { "wsfed_client" },
                RedirectUris = new List<string> { "https://client1/callback" },
                FrontChannelLogoutUri = "https://client1/signout",
                PostLogoutRedirectUris = new List<string> { "https://client1/signout-callback" },
                AllowAccessTokensViaBrowser = true
            });

            _mockPipeline.Clients.Add(new Client
            {
                ClientId = "urn:client2",
                RequireConsent = false,
                ProtocolType = "wsfed",
                AllowedScopes = new List<string> { "wsfed_client" },
                RedirectUris = new List<string> { "https://client2/callback" },
                FrontChannelLogoutUri = "https://client2/signout",
                PostLogoutRedirectUris = new List<string> {
                    "https://client2/signout-callback",
                    "https://client2/signout-callback2"
                },
                AllowAccessTokensViaBrowser = true
            });

            _mockPipeline.Clients.Add(new Client
            {
                ClientId = "urn:client3",
                RequireConsent = false,
                ProtocolType = "wsfed",
                AllowedScopes = new List<string> { "wsfed_client" },
                RedirectUris = new List<string> { "https://client3/callback" },
                FrontChannelLogoutUri = "https://client3/signout",
                PostLogoutRedirectUris = new List<string> { "https://client3/signout-callback" },
                AllowAccessTokensViaBrowser = true,
                IdentityProviderRestrictions = new List<string> { "google" },
            });

            _mockPipeline.Users.Add(new TestUser
            {
                SubjectId = "bob",
                Username = "bob",
                Claims = new Claim[]
                {
                    new Claim("name", "Bob Loblaw"),
                    new Claim("email", "bob@loblaw.com"),
                    new Claim("role", "Attorney")
                }
            });

            _mockPipeline.OnPreConfigureServices += s =>
            {
                s.Configure<WsFederationOptions>(c =>
                {
                    c.DefaultTokenType = WsFederationConstants.TokenTypes.Saml11TokenProfile11;
                });
            };

            _mockPipeline.Initialize();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task request_should_not_return_404()
        {
            var response = await _mockPipeline.BrowserClient.GetAsync(IdentityServerPipeline.WsFedSingleSignOnEndpoint);

            response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task post_request_without_form_should_return_415()
        {
            var response = await _mockPipeline.BrowserClient.PostAsync(IdentityServerPipeline.WsFedSingleSignOnEndpoint, new StringContent("foo"));

            response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task attribute_request_should_return_400()
        {
            var wsMessage = new WsFederationMessage
            {
                IssuerAddress = IdentityServerPipeline.WsFedSingleSignOnEndpoint,
                Wa = "wattr1.0",
            };

            var response = await _mockPipeline.BrowserClient.GetAsync(wsMessage.BuildRedirectUrl());

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Theory]
        [InlineData("get")]
        [InlineData("post")]
        [Trait("Category", Category)]
        public async Task signin_request_anonymous_user_should_be_passed_to_login_page(string method)
        {
            var request = _mockPipeline.CreateSignIn(
                   clientId: "urn:client1",
                   redirectUri: "https://client1/callback",
                   state: "123_state",
                   date: null,
                   method: method);

            var response = await _mockPipeline.BrowserClient.SendAsync(request);

            _mockPipeline.LoginWasCalled.Should().BeTrue();
            _mockPipeline.LoginRequest.Should().NotBeNull();
            _mockPipeline.LoginRequest.Client.ClientId.Should().Be("urn:client1");
            _mockPipeline.LoginRequest.RedirectUri.Should().Be("https://client1/callback");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task signin_request_with_whr_should_be_passed_to_login_page()
        {
            var request = _mockPipeline.CreateSignIn(
                   clientId: "urn:client3",
                   acr: "google");

            var response = await _mockPipeline.BrowserClient.SendAsync(request);

            _mockPipeline.LoginWasCalled.Should().BeTrue();
            _mockPipeline.LoginRequest.IdP.Should().Be("google");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task signin_request_with_whr_not_allowed_by_client_should_not_be_passed_to_login_page()
        {
            var request = _mockPipeline.CreateSignIn(
                   clientId: "urn:client3",
                   acr: "facebok");

            var response = await _mockPipeline.BrowserClient.SendAsync(request);

            _mockPipeline.LoginWasCalled.Should().BeTrue();
            _mockPipeline.LoginRequest.IdP.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task for_invalid_client_error_page_should_not_receive_client_id()
        {
            await _mockPipeline.LoginAsync("bob");

            var request = _mockPipeline.CreateSignIn(
               clientId: null);
            var response = await _mockPipeline.BrowserClient.SendAsync(request);

            _mockPipeline.ErrorWasCalled.Should().BeTrue();
            _mockPipeline.ErrorMessage.ClientId.Should().BeNull();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task signin_request_with_wfresh_set_to_0_user_is_authenticated_wfresh_exceeded_passed_to_login_page()
        {
            await _mockPipeline.LoginAsync("bob");

            var request = _mockPipeline.CreateSignIn(
                     clientId: "urn:client1",
                     state: "123_state",
                     freshness: 0
                     );

            var response = await _mockPipeline.BrowserClient.SendAsync(request);

            _mockPipeline.LoginWasCalled.Should().BeTrue();
            _mockPipeline.LoginRequest.Should().NotBeNull();
            _mockPipeline.LoginRequest.Client.ClientId.Should().Be("urn:client1");
        }

        [Theory]
        [InlineData("get", WsTrustVersion.WsTrust2005)]
        [InlineData("post", WsTrustVersion.WsTrust2005)]
        [InlineData("get", WsTrustVersion.WsTrust13)]
        [InlineData("post", WsTrustVersion.WsTrust13)]
        public async Task sigin_request_for_logged_in_user_return_assertion(string method, WsTrustVersion trustVersion)
        {
            _mockPipeline.OnPostConfigureServices += s =>
            {
                s.Configure<WsFederationOptions>(c =>
                {
                    c.DefaultWsTrustVersion = trustVersion;
                });
            };
            _mockPipeline.Initialize();

            await _mockPipeline.LoginAsync("bob");

            var request = _mockPipeline.CreateSignIn(
                     clientId: "urn:client1",
                     method: method
                     );

            _mockPipeline.BrowserClient.AllowAutoRedirect = false;
            var response = await _mockPipeline.BrowserClient.SendAsync(request);

            var contentAsText = await response.Content.ReadAsStringAsync();
            contentAsText.Should().NotBeEmpty();

            var wsResponse = new WsFedLoginResponse(contentAsText);
            wsResponse.Action.Should().Be("https://client1/callback");

            var tokenString = wsResponse.Message.GetToken();
            var handler = new SamlSecurityTokenHandler(); // TODO: SAML2
            var canReadToken = handler.CanReadToken(tokenString);
            Assert.True(canReadToken);
        }

        
        [Fact]
        public async Task signin_request_with_wfresh_set_to_0_user_is_authenticated_force_resignin()
        {
            await _mockPipeline.LoginAsync("bob");

            var request = _mockPipeline.CreateSignIn(
                      clientId: "urn:client1"
                      );

            _mockPipeline.BrowserClient.AllowAutoRedirect = false;
            await _mockPipeline.BrowserClient.SendAsync(request);

            await Task.Delay(3000); // TODO: bad workaround to simulate login for 3 seconds

            var request1 = _mockPipeline.CreateSignIn(
                      clientId: "urn:client2",
                      freshness: 0
                      );

            _mockPipeline.BrowserClient.AllowAutoRedirect = true;
            var response = await _mockPipeline.BrowserClient.SendAsync(request1);

            _mockPipeline.LoginWasCalled.Should().BeTrue();
            _mockPipeline.LoginRequest.Should().NotBeNull();
            _mockPipeline.LoginRequest.Client.ClientId.Should().Be("urn:client2");
        }

        [Fact]
        public async Task signin_request_with_wfresh_user_is_authenticated_wfresh_in_time_frame_return_assertion()
        {
            // login user
            await _mockPipeline.LoginAsync("bob");
            var authTime = System.DateTime.UtcNow;

            var request = _mockPipeline.CreateSignIn(
                      clientId: "urn:client1"
                      );

            _mockPipeline.BrowserClient.AllowAutoRedirect = false;
            await _mockPipeline.BrowserClient.SendAsync(request);

            // create ws-fed signin message with wfresh=5
            var request1 = _mockPipeline.CreateSignIn(
              clientId: "urn:client2",
              freshness: 5
            );

            var response = await _mockPipeline.BrowserClient.SendAsync(request1);

            var contentAsText = await response.Content.ReadAsStringAsync();
            contentAsText.Should().NotBeEmpty();

            var wsResponse = new WsFedLoginResponse(contentAsText);
            wsResponse.Action.Should().Be("https://client2/callback");

            var tokenString = wsResponse.Message.GetToken();
            var handler = new SamlSecurityTokenHandler();
            var canReadToken = handler.CanReadToken(tokenString);
            canReadToken.Should().BeTrue();

            var token = handler.ReadSamlToken(tokenString);
            var authStatements = token.Assertion.Statements.OfType<SamlAuthenticationStatement>();
            authStatements.Should().ContainSingle();
            var authStatement = authStatements.First();
            authStatement.AuthenticationInstant.Should().BeLessThan(System.TimeSpan.FromMinutes(5));
        }
        
        [Theory]
        [InlineData("get")]
        [InlineData("post")]
        public async Task signout_request_with_no_wtrealm_redirect_to_logout_page(string method)
        {
            await _mockPipeline.LoginAsync("bob");

            var signIn = _mockPipeline.CreateSignIn(
                 clientId: "urn:client1",
                 redirectUri: "https://client1/callback",
                 state: "123_state");
            
            _mockPipeline.BrowserClient.AllowAutoRedirect = false;
            await _mockPipeline.BrowserClient.SendAsync(signIn);

            var request = _mockPipeline.CreateSignOut(
                     clientId: "urn:client1",
                     method: method
                     );

            _mockPipeline.BrowserClient.AllowAutoRedirect = true;
            var response = await _mockPipeline.BrowserClient.SendAsync(request);

            _mockPipeline.LogoutRequest.PostLogoutRedirectUri.Should().BeNull();

            var signoutFrameUrl = _mockPipeline.LogoutRequest.SignOutIFrameUrl;
            response = await _mockPipeline.BrowserClient.GetAsync(signoutFrameUrl);
            var html = await response.Content.ReadAsStringAsync();
            html.Should().Contain(HtmlEncoder.Default.Encode("https://client1/signout?wa=wsignoutcleanup1.0"));
        }

        [Theory]
        [InlineData("get")]
        [InlineData("post")]
        public async Task signout_request_redirect_to_logout_page(string method)
        {
            await _mockPipeline.LoginAsync("bob");

            var signIn = _mockPipeline.CreateSignIn(
                clientId: "urn:client1",
                redirectUri: "https://client1/callback",
                state: "123_state");

            _mockPipeline.BrowserClient.AllowAutoRedirect = false;
            await _mockPipeline.BrowserClient.SendAsync(signIn);

            var request = _mockPipeline.CreateSignOut(
                     clientId: "urn:client1",
                     redirectUri: "https://client1/signout-callback",
                     method: method
                     );

            _mockPipeline.BrowserClient.AllowAutoRedirect = true;
            var response = await _mockPipeline.BrowserClient.SendAsync(request);

            _mockPipeline.LogoutRequest.PostLogoutRedirectUri.Should().Be("https://client1/signout-callback");

            var signoutFrameUrl = _mockPipeline.LogoutRequest.SignOutIFrameUrl;
            response = await _mockPipeline.BrowserClient.GetAsync(signoutFrameUrl);
            var html = await response.Content.ReadAsStringAsync();
            html.Should().Contain(HtmlEncoder.Default.Encode("https://client1/signout?wa=wsignoutcleanup1.0"));
        }

        [Theory]
        [InlineData("get")]
        [InlineData("post")]
        public async Task signoutcleanup_request_redirect_to_logout_page_success(string method)
        {
            await _mockPipeline.LoginAsync("bob");

            var signIn = _mockPipeline.CreateSignIn(
                clientId: "urn:client1",
                redirectUri: "https://client1/callback",
                state: "123_state");

            _mockPipeline.BrowserClient.AllowAutoRedirect = false;
            await _mockPipeline.BrowserClient.SendAsync(signIn);

            var request = _mockPipeline.CreateSignOutCleanup(
                     clientId: "urn:client1",
                     redirectUri: "https://client1/signout-callback",
                     method: method
                     );

            _mockPipeline.BrowserClient.AllowAutoRedirect = true;
            var response = await _mockPipeline.BrowserClient.SendAsync(request);

            _mockPipeline.LogoutRequest.PostLogoutRedirectUri.Should().Be("https://client1/signout-callback");

            var signoutFrameUrl = _mockPipeline.LogoutRequest.SignOutIFrameUrl;
            response = await _mockPipeline.BrowserClient.GetAsync(signoutFrameUrl);
            var html = await response.Content.ReadAsStringAsync();
            html.Should().Contain(HtmlEncoder.Default.Encode("https://client1/signout?wa=wsignoutcleanup1.0"));
        }

    }
}
