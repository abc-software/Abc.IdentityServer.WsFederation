using Abc.IdentityServer.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Abc.IdentityServer.WsFederation.Endpoints.Results.UnitTests
{
    public class LogoutPageResultFixture
    {
        private LogoutPageResult _target;
        private IdentityServerOptions _options;
        private DefaultHttpContext _context;
        private IServerUrls _urls;

        public LogoutPageResultFixture()
        {
            _context = new DefaultHttpContext();
            _context.Response.Body = new MemoryStream();

            _options = new IdentityServerOptions();
            _options.UserInteraction.LogoutUrl = "~/logout";

            _urls = new MockServerUrls()
            {
                Origin = "https://server",
                BasePath = "/".RemoveTrailingSlash(), // as in DefaultServerUrls
            };

            _target = new LogoutPageResult(_options, _urls);
        }

        [Fact]
        public async Task logout_should_redirect_to_logout_page()
        {
            await _target.ExecuteAsync(_context);
            _context.Response.StatusCode.Should().Be(302);

            var location = _context.Response.Headers["Location"].First();
            location.Should().StartWith("https://server/logout");
        }
    }
}