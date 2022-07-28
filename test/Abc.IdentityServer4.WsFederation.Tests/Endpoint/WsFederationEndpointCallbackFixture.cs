using FluentAssertions;
using IdentityServer4.Configuration;
using IdentityServer4.Hosting;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Abc.IdentityServer4.WsFederation.Endpoints;
using Abc.IdentityServer4.WsFederation.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Xunit;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Stores;
using System.Collections.Specialized;
using System.Collections.Generic;
using Abc.IdentityServer4.Extensions;
using IdentityServer4;
using StatusCodeResult = IdentityServer4.Endpoints.Results.StatusCodeResult;

namespace Abc.IdentityServer4.WsFederation.Tests.Endpoint
{
    public class WsFederationEndpointCallbackFixture
    {
        private const string Category = "WsFederation Endpoint";
        private WsFederationEndpointCallback _subject;

        private TestEventService _fakeEventService;
        private ILogger<WsFederationEndpointCallback> _fakeLogger = TestLogger.Create<WsFederationEndpointCallback>();
        private IdentityServerOptions _options = TestIdentityServerOptions.Create();
        private MockUserSession _mockUserSession = new MockUserSession();
        private ClaimsPrincipal _user = new IdentityServerUser("bob").CreatePrincipal();
        private DefaultHttpContext _context;
        private StubWsFederationRequestValidator _stubSignInRequestValidator;
        private StubSignInInteractionResponseGenerator _stubInteractionGenerator;
        private StubSignInResponseGenerator _stubSigninResponseGenerator;
        private MockConsentMessageStore _mockUserConsentResponseMessageStore;

        private WsFederationMessage _signIn;
        private ValidatedWsFederationRequest _validatedAuthorizeRequest;
        private AuthorizationParametersMessageStoreMock _mockAuthorizationParametersMessageStore;

        public WsFederationEndpointCallbackFixture()
        {
            _context = new DefaultHttpContext();
            _context.SetIdentityServerOrigin("https://server");
            _context.SetIdentityServerBasePath("/");

            _stubSignInRequestValidator = new StubWsFederationRequestValidator();
            _stubInteractionGenerator = new StubSignInInteractionResponseGenerator();
            _stubSigninResponseGenerator = new StubSignInResponseGenerator();

            _fakeEventService = new TestEventService();

            _mockAuthorizationParametersMessageStore = new AuthorizationParametersMessageStoreMock();
            _mockUserConsentResponseMessageStore = new MockConsentMessageStore();

            _signIn = new WsFederationMessage() { Wa = "wsignin1.0", Wtrealm = "client", Wct = "some_nonce" };

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

            _subject = new WsFederationEndpointCallback(
                _fakeEventService,
                _stubSignInRequestValidator,
                _stubInteractionGenerator,
                _stubSigninResponseGenerator,
                _mockUserSession,
                _fakeLogger,
                _mockUserConsentResponseMessageStore,
                _mockAuthorizationParametersMessageStore);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task post_to_entry_point_should_return_405()
        {
            _context.Request.Method = "POST";

            var result = await _subject.ProcessAsync(_context);

            var statusCode = result as StatusCodeResult;
            statusCode.Should().NotBeNull();
            statusCode.StatusCode.Should().Be(405);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task signin_after_consent_path_should_return_signin_result()
        {
            var parameters = new NameValueCollection()
            {
                { "client_id", "client" },
                { "nonce", "some_nonce" },
            };
            var request = new ConsentRequest(parameters, _user.GetSubjectId());
            _mockUserConsentResponseMessageStore.Messages.Add(request.Id, new Message<ConsentResponse>(new ConsentResponse(), DateTime.UtcNow));

            var key = Guid.NewGuid().ToString();
            _mockAuthorizationParametersMessageStore.Messages.Add(key, new Message<Dictionary<string, string[]>>(new Dictionary<string, string[]>(_signIn.ToDictionary()), DateTime.UtcNow));

            _mockUserSession.User = _user;

            _context.Request.Method = "GET";
            _context.Request.Path = new PathString("/wsfed/callback");
            _context.Request.QueryString = new QueryString("?authzId=" + key);

            var result = await _subject.ProcessAsync(_context);

            result.Should().BeOfType<Endpoints.Results.SignInResult>();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task signin_after_login_path_should_return_signin_result()
        {
            var key = Guid.NewGuid().ToString();
            _mockAuthorizationParametersMessageStore.Messages.Add(key, new Message<Dictionary<string, string[]>>(new Dictionary<string, string[]>(_signIn.ToDictionary()), DateTime.UtcNow));

            _context.Request.Method = "GET";
            _context.Request.Path = new PathString("/wsfed/callback");
            _context.Request.QueryString = new QueryString("?authzId=" + key);

            _mockUserSession.User = _user;

            var result = await _subject.ProcessAsync(_context);

            result.Should().BeOfType<Endpoints.Results.SignInResult>();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task sigin_invalid_data_should_return_error_page()
        {
            _signIn.Wa = "wsignout1.0";

            var key = Guid.NewGuid().ToString();
            _mockAuthorizationParametersMessageStore.Messages.Add(key, new Message<Dictionary<string, string[]>>(new Dictionary<string, string[]>(_signIn.ToDictionary()), DateTime.UtcNow));

            _mockUserSession.User = _user;

            _context.Request.Method = "GET";
            _context.Request.Path = new PathString("/wsfed/callback");
            _context.Request.QueryString = new QueryString("?authzId=" + key);

            var result = await _subject.ProcessAsync(_context);

            result.Should().BeOfType<Endpoints.Results.ErrorPageResult>();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task consent_missing_consent_data_should_return_error_page()
        {
            var parameters = new NameValueCollection()
            {
                { "client_id", "client" },
                { "nonce", "some_nonce" },
            };
            var request = new ConsentRequest(parameters, _user.GetSubjectId());
            _mockUserConsentResponseMessageStore.Messages.Add(request.Id, new Message<ConsentResponse>(null, DateTime.UtcNow));

            var key = Guid.NewGuid().ToString();
            _mockAuthorizationParametersMessageStore.Messages.Add(key, new Message<Dictionary<string, string[]>>(new Dictionary<string, string[]>(_signIn.ToDictionary()), DateTime.UtcNow));

            _mockUserSession.User = _user;

            _context.Request.Method = "GET";
            _context.Request.Path = new PathString("/wsfed/callback");
            _context.Request.QueryString = new QueryString("?authzId=" + key);

            var result = await _subject.ProcessAsync(_context);

            result.Should().BeOfType<Endpoints.Results.ErrorPageResult>();
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task valid_consent_message_should_cleanup_consent_cookie()
        {
             var parameters = new NameValueCollection()
            {
                { "client_id", "client" },
                { "nonce", "some_nonce" },
            };
            var request = new ConsentRequest(parameters, _user.GetSubjectId());
            _mockUserConsentResponseMessageStore.Messages.Add(request.Id, new Message<ConsentResponse>(new ConsentResponse() { ScopesValuesConsented = new string[] { "api1", "api2" } }, DateTime.UtcNow));

            var key = Guid.NewGuid().ToString();
            _mockAuthorizationParametersMessageStore.Messages.Add(key, new Message<Dictionary<string, string[]>>(new Dictionary<string, string[]>(_signIn.ToDictionary()), DateTime.UtcNow));

            _mockUserSession.User = _user;

            _context.Request.Method = "GET";
            _context.Request.Path = new PathString("/connect/authorize/callback");
            _context.Request.QueryString = new QueryString("?authzId=" + key);

            var result = await _subject.ProcessAsync(_context);

            _mockUserConsentResponseMessageStore.Messages.Count.Should().Be(0);
            _mockAuthorizationParametersMessageStore.Messages.Count.Should().Be(0);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task valid_consent_message_should_return_authorize_result()
        {
            var parameters = new NameValueCollection()
            {
                { "client_id", "client" },
                { "nonce", "some_nonce" },
            };

            var request = new ConsentRequest(parameters, _user.GetSubjectId());
            _mockUserConsentResponseMessageStore.Messages.Add(request.Id, new Message<ConsentResponse>(new ConsentResponse() { ScopesValuesConsented = new string[] { "api1", "api2" } }, DateTime.UtcNow));

            var key = Guid.NewGuid().ToString();
            _mockAuthorizationParametersMessageStore.Messages.Add(key, new Message<Dictionary<string, string[]>>(new Dictionary<string, string[]>(_signIn.ToDictionary()), DateTime.UtcNow));

            _mockUserSession.User = _user;

            _context.Request.Method = "GET";
            _context.Request.Path = new PathString("/wsfed/callback");
            _context.Request.QueryString = new QueryString("?authzId=" + key);

            var result = await _subject.ProcessAsync(_context);

            result.Should().BeOfType<Endpoints.Results.SignInResult>();
        }
    }
}
