﻿using Abc.IdentityServer.Extensions;
using Abc.IdentityServer.WsFederation.Endpoints;
using Abc.IdentityServer.WsFederation.Validation;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Abc.IdentityServer.WsFederation.Endpoint.UnitTests
{
    public class WsFederationCallbackEndpointFixture
    {
        private const string Category = "WsFederationCallback Endpoint";
        private WsFederationCallbackEndpoint _subject;

        private TestEventService _fakeEventService;
        private ILogger<WsFederationCallbackEndpoint> _fakeLogger = TestLogger.Create<WsFederationCallbackEndpoint>();
        private IdentityServerOptions _options = TestIdentityServerOptions.Create();
        private MockUserSession _mockUserSession = new MockUserSession();
        private ClaimsPrincipal _user = new Ids.IdentityServerUser("bob").CreatePrincipal();
        private DefaultHttpContext _context;
        private StubWsFederationRequestValidator _stubSignInRequestValidator;
        private StubSignInInteractionResponseGenerator _stubInteractionGenerator;
        private StubSignInResponseGenerator _stubSigninResponseGenerator;
        private MockConsentMessageStore _mockUserConsentResponseMessageStore;

        private WsFederationMessage _signIn;
        private ValidatedWsFederationRequest _validatedAuthorizeRequest;
        private AuthorizationParametersMessageStoreMock _mockAuthorizationParametersMessageStore;

        private string ClientId = "client";
        private string AuthContext = "some_context";

        public WsFederationCallbackEndpointFixture()
        {
            _context = new DefaultHttpContext();

            _stubSignInRequestValidator = new StubWsFederationRequestValidator();
            _stubInteractionGenerator = new StubSignInInteractionResponseGenerator();
            _stubSigninResponseGenerator = new StubSignInResponseGenerator();

            _fakeEventService = new TestEventService();

            _mockAuthorizationParametersMessageStore = new AuthorizationParametersMessageStoreMock();
            _mockUserConsentResponseMessageStore = new MockConsentMessageStore();

            _signIn = new WsFederationMessage() { Wa = "wsignin1.0", Wtrealm = ClientId, Wct = AuthContext };

            _validatedAuthorizeRequest = new ValidatedWsFederationRequest()
            {
                ReplyUrl = "http://client/callback",
                ClientId = ClientId,
                Client = new Client
                {
                    ClientId = ClientId,
                    ClientName = "Test Client"
                },
                //Raw = _params,
                Subject = _user
            };

            _stubSigninResponseGenerator.Result = new WsFederationMessage();

            _stubSignInRequestValidator.Result = new WsFederationValidationResult(_validatedAuthorizeRequest);

            _subject = new WsFederationCallbackEndpoint(
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
                { "client_id", ClientId },
                { "nonce", AuthContext },
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
                { "client_id", ClientId },
                { "nonce", AuthContext },
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
                { "client_id", ClientId },
                { "nonce", AuthContext },
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

            _mockUserConsentResponseMessageStore.Messages.Count.Should().Be(0);
            _mockAuthorizationParametersMessageStore.Messages.Count.Should().Be(0);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task valid_consent_message_should_return_authorize_result()
        {
            var parameters = new NameValueCollection()
            {
                { "client_id", ClientId },
                { "nonce", AuthContext },
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
