using Abc.IdentityServer4.WsFederation.Endpoints;
using Abc.IdentityServer4.WsFederation.Validation;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Abc.IdentityServer4.WsFederation.Endpoint.UnitTests
{
    public class WsFederationEndpointFixture
    {
        private const string Category = "WsFederation Endpoint";
        private WsFederationEndpoint _subject;

        private TestEventService _fakeEventService;
        private ILogger<WsFederationEndpoint> _fakeLogger = TestLogger.Create<WsFederationEndpoint>();
        private IdentityServerOptions _options = TestIdentityServerOptions.Create();
        private MockUserSession _mockUserSession = new MockUserSession();
        private ClaimsPrincipal _user = new Ids.IdentityServerUser("bob").CreatePrincipal();

        private StubWsFederationRequestValidator _stubSignInRequestValidator = new StubWsFederationRequestValidator();
        private StubSignInInteractionResponseGenerator _stubInteractionGenerator = new StubSignInInteractionResponseGenerator();
        private StubSignInResponseGenerator _stubSigninResponseGenerator = new StubSignInResponseGenerator();

        private WsFederationMessage _signIn;
        private WsFederationMessage _signOut;
        private ValidatedWsFederationRequest _validatedAuthorizeRequest;
        private DefaultHttpContext _context;

        public WsFederationEndpointFixture()
        {
            _context = new DefaultHttpContext();
            _context.SetIdentityServerOrigin("https://server");
            _context.SetIdentityServerBasePath("/");

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

            _subject = new WsFederationEndpoint(
                _fakeEventService,
                _stubSignInRequestValidator,
                _stubInteractionGenerator,
                _stubSigninResponseGenerator,
                _mockUserSession,
                _fakeLogger);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        [Trait("Category", Category)]
        public async Task sigin_path_should_return_signin_result(string method)
        {
            var request = new Dictionary<string, StringValues> { 
                { "wa", "wsignin1.0" }, 
                { "wtrealm", "urn:owinrp" } 
            };
           
            if (method == "GET")
            {
                _context.Request.Query = new QueryCollection(request);
            }
            else
            {
                _context.Request.Form = new FormCollection(request);
                _context.Request.ContentType = "application /x-www-form-urlencoded; charset=utf-8";
            }

            _context.Request.Method = method;
            _context.Request.Path = new PathString("/wsfed");

            _mockUserSession.User = _user;

            var result = await _subject.ProcessAsync(_context);

            result.Should().BeOfType<Endpoints.Results.SignInResult>();
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        [Trait("Category", Category)]
        public async Task sigout_path_should_return_logout_result(string method)
        {
            var request = new Dictionary<string, StringValues> {
                { "wa", "wsignout1.0" },
                { "wtrealm", "urn:owinrp" }
            };

            if (method == "GET")
            {
                _context.Request.Query = new QueryCollection(request);
            }
            else
            {
                _context.Request.Form = new FormCollection(request);
                _context.Request.ContentType = "application /x-www-form-urlencoded; charset=utf-8";
            }

            _context.Request.Method = method;
            _context.Request.Path = new PathString("/wsfed");

            _mockUserSession.User = _user;

            var result = await _subject.ProcessAsync(_context);

            result.Should().BeOfType<Endpoints.Results.LogoutPageResult>();
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        [Trait("Category", Category)]
        public async Task sigoutcleanup_path_should_return_logout_result(string method)
        {
            var request = new Dictionary<string, StringValues> {
                { "wa", "wsignoutcleanup1.0" },
                { "wtrealm", "urn:owinrp" }
            };

            if (method == "GET")
            {
                _context.Request.Query = new QueryCollection(request);
            }
            else
            {
                _context.Request.Form = new FormCollection(request);
                _context.Request.ContentType = "application /x-www-form-urlencoded; charset=utf-8";
            }

            _context.Request.Method = method;
            _context.Request.Path = new PathString("/wsfed");

            _mockUserSession.User = _user;

            var result = await _subject.ProcessAsync(_context);

            result.Should().BeOfType<Endpoints.Results.LogoutPageResult>();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task sigin_invalid_should_return_400()
        {
            _context.Request.Method = "GET";
            _context.Request.Path = new PathString("/wsfed");
            _context.Request.QueryString = new QueryString("?wa=wattr1.0&wtrealm=urn:owinrp");
            _mockUserSession.User = _user;

            var result = await _subject.ProcessAsync(_context);

            var statusCode = result as StatusCodeResult;
            statusCode.Should().NotBeNull();
            statusCode.StatusCode.Should().Be(400);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task signin_post_without_form_content_type_should_return_415()
        {
            _context.Request.Method = "POST";

            var result = await _subject.ProcessAsync(_context);

            var statusCode = result as StatusCodeResult;
            statusCode.Should().NotBeNull();
            statusCode.StatusCode.Should().Be(415);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task signin_not_post_and_get_should_return_405()
        {
            _context.Request.Method = "HEAD";

            var result = await _subject.ProcessAsync(_context);

            var statusCode = result as StatusCodeResult;
            statusCode.Should().NotBeNull();
            statusCode.StatusCode.Should().Be(405);
        }

    }
}
