using Abc.IdentityServer.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Abc.IdentityServer.WsFederation.Endpoints.Results.UnitTests
{
    public class ErrorPageResultFixture
    {
        private ErrorPageResult _target;
        private IdentityServerOptions _options;
        private MockMessageStore<ErrorMessage> _errorMessageStore;
        private IClock _clock = new StubClock();
        private DefaultHttpContext _context;
        private IServerUrls _urls;

        public ErrorPageResultFixture()
        {
            _context = new DefaultHttpContext();
            _context.Response.Body = new MemoryStream();

            _options = new IdentityServerOptions();
            _options.UserInteraction.ErrorUrl = "~/error";
            _options.UserInteraction.ErrorIdParameter = "errorId";

            _urls = new MockServerUrls()
            {
                Origin = "https://server",
                BasePath = "/".RemoveTrailingSlash(), // as in DefaultServerUrls
            };

            _errorMessageStore = new MockMessageStore<ErrorMessage>();

            _target = new ErrorPageResult("some_error", "some_desciption", _options, _clock, _urls, _errorMessageStore);
        }

        [Fact]
        public async Task error_should_redirect_to_error_page_and_passs_info()
        {
            _target.Error.Should().Be("some_error");
            _target.ErrorDescription.Should().Be("some_desciption");

            await _target.ExecuteAsync(_context);

            _errorMessageStore.Messages.Count.Should().Be(1);
            _context.Response.StatusCode.Should().Be(302);

            var location = _context.Response.Headers["Location"].First();
            location.Should().StartWith("https://server/error");

            var query = QueryHelpers.ParseQuery(new Uri(location).Query);
            var message = _errorMessageStore.Messages.First();
            query["errorId"].First().Should().Be(message.Key);
            message.Value.Data.Error.Should().Be("some_error");
            message.Value.Data.ErrorDescription.Should().Be("some_desciption");
        }
    }
}