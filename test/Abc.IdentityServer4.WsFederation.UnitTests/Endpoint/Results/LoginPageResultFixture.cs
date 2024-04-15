using Abc.IdentityServer.Extensions;
using Abc.IdentityServer.WsFederation.Validation;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Abc.IdentityServer.WsFederation.Endpoints.Results.UnitTests
{
    public class LoginPageResultFixture
    {
        private LoginPageResult _target;
        private ValidatedWsFederationRequest _request;
        private IdentityServerOptions _options;
        private IClock _clock = new StubClock();
        private DefaultHttpContext _context;
        private AuthorizationParametersMessageStoreMock _authorizationParametersMessageStore;
        private IServerUrls _urls;

        public LoginPageResultFixture()
        {
            _context = new DefaultHttpContext();
            _context.RequestServices = new ServiceCollection().BuildServiceProvider();

            _options = new IdentityServerOptions();
            _options.UserInteraction.LoginUrl = "~/login";
            _options.UserInteraction.LoginReturnUrlParameter = "returnUrl";

            _authorizationParametersMessageStore = new AuthorizationParametersMessageStoreMock();

            _request = new ValidatedWsFederationRequest();
            _request.WsFederationMessage = new WsFederationMessage()
            {
                Wa = "wsigin1.0",
                Wtrealm = "urn:owinrp",
            };

            _urls = new MockServerUrls()
            {
                Origin = "https://server",
                BasePath = "/".RemoveTrailingSlash(), // as in DefaultServerUrls
            };
        }

        [Fact]
        public void login_ctor()
        {
            Action action = () =>
            {
                _target = new LoginPageResult(null, _options, _clock, _urls, _authorizationParametersMessageStore);
            };

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task login_should_redirect_to_login_page_and_passs_info()
        {
            _target = new LoginPageResult(_request, _options, _clock, _urls, _authorizationParametersMessageStore);

            await _target.ExecuteAsync(_context);

            _authorizationParametersMessageStore.Messages.Count.Should().Be(1);
            _context.Response.StatusCode.Should().Be(302);

            var location = _context.Response.Headers["Location"].First();
            location.Should().StartWith("https://server/login");

            var query = QueryHelpers.ParseQuery(new Uri(location).Query);
            query["returnUrl"].First().Should().StartWith("/wsfed/callback");
            query["returnUrl"].First().Should().Contain("?authzId=" + _authorizationParametersMessageStore.Messages.First().Key);
        }

        [Fact]
        public async Task login_should_redirect_to_login_page_and_passs_info_in_query()
        {
            _target = new LoginPageResult(_request, _options, _clock, _urls, null);

            await _target.ExecuteAsync(_context);

            _context.Response.StatusCode.Should().Be(302);

            var location = _context.Response.Headers["Location"].First();
            location.Should().StartWith("https://server/login");

            var query = QueryHelpers.ParseQuery(new Uri(location).Query);
            query["returnUrl"].First().Should().StartWith("/wsfed/callback");
            query["returnUrl"].First().Should().Contain("?wa=wsigin1.0&wtrealm=urn%3Aowinrp");
        }

    }
}