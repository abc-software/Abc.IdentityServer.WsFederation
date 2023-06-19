using Abc.IdentityServer.WsFederation.Endpoints;
using Abc.IdentityServer.WsFederation.ResponseProcessing;
using Abc.IdentityServer.WsFederation.Validation;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Abc.IdentityServer.WsFederation.Endpoint.UnitTests
{
    public class WsFederationEndpointBaseFixture
    {
        private const string Category = "WsFederation Endpoint";
        private TestWsFederationEndpoint _subject;

        private TestEventService _fakeEventService;
        private ILogger<TestWsFederationEndpoint> _fakeLogger = TestLogger.Create<TestWsFederationEndpoint>();
        private IdentityServerOptions _options = TestIdentityServerOptions.Create();
        private MockUserSession _mockUserSession = new MockUserSession();
        private ClaimsPrincipal _user = new Ids.IdentityServerUser("bob").CreatePrincipal();

        private StubWsFederationRequestValidator _stubSignInRequestValidator = new StubWsFederationRequestValidator();
        private StubSignInInteractionResponseGenerator _stubInteractionGenerator = new StubSignInInteractionResponseGenerator();
        private StubSignInResponseGenerator _stubSigninResponseGenerator = new StubSignInResponseGenerator();

        private WsFederationMessage _signIn;
        private WsFederationMessage _signOut;
        private ValidatedWsFederationRequest _validatedAuthorizeRequest;

        public WsFederationEndpointBaseFixture()
        {
            _stubSignInRequestValidator = new StubWsFederationRequestValidator();
            _stubInteractionGenerator = new StubSignInInteractionResponseGenerator();
            _stubSigninResponseGenerator = new StubSignInResponseGenerator();

            _fakeEventService = new TestEventService();

            _signIn = new WsFederationMessage() { Wa = "wsignin1.0", Wtrealm = "urn:realm" };
            _signOut = new WsFederationMessage() { Wa = "wsignout1.0", Wtrealm = "urn:realm", Wreply = "http://localhost/" };

            _validatedAuthorizeRequest = new ValidatedWsFederationRequest()
            {
                ReplyUrl = "http://client/callback",
                ClientId = "client",
                Client = new Client
                {
                    ClientId = "client",
                    ClientName = "Test Client"
                },
                //Raw = _params,
                Subject = _user
            };

            _stubSigninResponseGenerator.Result = new WsFederationMessage();

            _stubSignInRequestValidator.Result = new WsFederationValidationResult(_validatedAuthorizeRequest);

            _subject = new TestWsFederationEndpoint(
                _fakeEventService,
                _stubSignInRequestValidator,
                _stubInteractionGenerator,
                _stubSigninResponseGenerator,
                _mockUserSession,
                _fakeLogger);
        }

        internal class TestWsFederationEndpoint : WsFederationEndpointBase
        {
            public TestWsFederationEndpoint(
                IEventService events, 
                IWsFederationRequestValidator validator, 
                ISignInInteractionResponseGenerator interaction, 
                ISignInResponseGenerator generator, 
                IUserSession userSession, 
                ILogger logger) 
                : base(events, validator, interaction, generator, userSession, logger)
            {
            }

            public override Task<IEndpointResult> ProcessAsync(HttpContext context)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task signin_request_validation_produces_error_should_display_error_page()
        {
            _stubSignInRequestValidator.Result.IsError = true;
            _stubSignInRequestValidator.Result.Error = "some_error";

            var result = await _subject.ProcessSignInRequestAsync(_signIn, _user, null);

            result.Should().BeOfType<Endpoints.Results.ErrorPageResult>();
        }

        /*
        [Fact]
        [Trait("Category", Category)]
        public async Task interaction_generator_consent_produces_consent_should_show_consent_page()
        {
            _stubInteractionGenerator.Response.IsConsent = true;

            var result = await _subject.ProcessSignInRequestAsync(_signIn, _user, null);

            result.Should().BeOfType<IdentityServer4.Endpoints.Results.ConsentPageResult>();
        }
        */

        [Fact]
        [Trait("Category", Category)]
        public async Task interaction_produces_error_should_show_error_page()
        {
            _stubInteractionGenerator.Response.Error = "error";

            var result = await _subject.ProcessSignInRequestAsync(_signIn, _user, null);

            result.Should().BeOfType<Endpoints.Results.ErrorPageResult>();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task interaction_produces_error_should_show_error_page_with_error_description_if_present()
        {
            var errorDescription = "some error description";

            _stubInteractionGenerator.Response.Error = "error";
            _stubInteractionGenerator.Response.ErrorDescription = errorDescription;

            var result = await _subject.ProcessSignInRequestAsync(_signIn, _user, null);

            result.Should().BeOfType<Endpoints.Results.ErrorPageResult>();
            var errorResult = (Endpoints.Results.ErrorPageResult)result;
            errorResult.ErrorDescription.Should().Be(errorDescription);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task interaction_produces_login_result_should_trigger_login()
        {
            _stubInteractionGenerator.Response.IsLogin = true;

            var result = await _subject.ProcessSignInRequestAsync(_signIn, _user, null);

            result.Should().BeOfType<Endpoints.Results.LoginPageResult>();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task custom_interaction_redirect_result_should_issue_redirect()
        {
            _mockUserSession.User = _user;
            _stubInteractionGenerator.Response.RedirectUrl = "http://foo.com";

            var result = await _subject.ProcessSignInRequestAsync(_signIn, _user, null);

            result.Should().BeOfType<Endpoints.Results.CustomRedirectResult>();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task successful_signin_request_should_generate_signin_result()
        {
            var result = await _subject.ProcessSignInRequestAsync(_signIn, _user, null);

            result.Should().BeOfType<Endpoints.Results.SignInResult>();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task signout_request_without_wtrealm_should_display_logout_page()
        {
            _signOut.Wtrealm = null;

            var result = await _subject.ProcessSignOutRequestAsync(_signOut, _user);

            result.Should().BeOfType<Endpoints.Results.LogoutPageResult>();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task signout_request_without_wreply_should_display_logout_page()
        {
            _signOut.Wreply = null;

            var result = await _subject.ProcessSignOutRequestAsync(_signOut, _user);

            result.Should().BeOfType<Endpoints.Results.LogoutPageResult>();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task signout_request_validation_produces_error_should_display_error_page()
        {
            _stubSignInRequestValidator.Result.IsError = true;
            _stubSignInRequestValidator.Result.Error = "some_error";

            var result = await _subject.ProcessSignOutRequestAsync(_signOut, _user);

            result.Should().BeOfType<Endpoints.Results.ErrorPageResult>();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task successful_authorization_request_should_generate_signout_result()
        {
            var result = await _subject.ProcessSignOutRequestAsync(_signOut, _user);

            result.Should().BeOfType<Endpoints.Results.SignOutResult>();
        }
    }
}
