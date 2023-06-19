using Abc.IdentityServer.Extensions;
using Abc.IdentityServer.WsFederation.Endpoint.UnitTests;
using Abc.IdentityServer.WsFederation.Validation;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Abc.IdentityServer.WsFederation.UnitTests
{
    public class WsFederationReturnUrlParserFixture
    {
        private WsFederationReturnUrlParser _target;
        private MockUserSession _mockUserSession = new MockUserSession();
        private StubWsFederationRequestValidator _validator;
        private ILogger<WsFederationReturnUrlParser> _logger = TestLogger.Create<WsFederationReturnUrlParser>();
        private AuthorizationParametersMessageStoreMock _mockAuthorizationParametersMessageStore;
        private WsFederationMessage _signIn;

        public WsFederationReturnUrlParserFixture()
        {
            _validator = new StubWsFederationRequestValidator();
            _validator.Result = new WsFederationValidationResult(new ValidatedWsFederationRequest());

            _mockAuthorizationParametersMessageStore = new AuthorizationParametersMessageStoreMock();

            _signIn = new WsFederationMessage() { IssuerAddress = "/wsfed/callback", Wa = "wsignin1.0", Wtrealm = "urn:owinrp" };

            _target = new WsFederationReturnUrlParser(_mockUserSession, _validator, _logger, null);
        }

        [Theory]
        [InlineData("/wsfed/callback?authzId=id")]
        public void returnUrl_valid(string url)
        {
            _target.IsValidReturnUrl(url).Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("https://server/wsfed/callback")]
        [InlineData("/callback")]
        [InlineData("/wsfed/callback")]
        public void returnUrl_invalid(string url)
        {
            _target.IsValidReturnUrl(url).Should().BeFalse();
        }

        [Fact]
        public async Task parse_returnUrl_success_from_messagestore()
        {
            _target = new WsFederationReturnUrlParser(_mockUserSession, _validator, _logger, _mockAuthorizationParametersMessageStore);

            _mockAuthorizationParametersMessageStore.Messages.Add("id", new Message<Dictionary<string, string[]>>(new Dictionary<string, string[]>(_signIn.ToDictionary()), DateTime.UtcNow));

            var result = await _target.ParseAsync("/wsfed/callback?authzId=id");
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task parse_returnUrl_success()
        {
            var result = await _target.ParseAsync(_signIn.BuildRedirectUrl());
            result.Should().NotBeNull();

            //result.Client = 
        }

        [Fact]
        public async Task parse_returnUrl_no_signin_request()
        {
            _signIn.Wa = "wattr1.0";

            var result = await _target.ParseAsync(_signIn.BuildRedirectUrl());
            result.Should().BeNull();
        }

        [Fact]
        public async Task parse_returnUrl_validation_error()
        {
            _validator.Result.IsError = true;

            var result = await _target.ParseAsync(_signIn.BuildRedirectUrl());
            result.Should().BeNull();
        }

    }
}
