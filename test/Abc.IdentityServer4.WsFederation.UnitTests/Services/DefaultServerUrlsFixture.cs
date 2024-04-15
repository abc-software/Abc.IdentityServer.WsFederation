using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Abc.IdentityServer.WsFederation.Services.UnitTests
{
    public class DefaultServerUrlsFixture
    {
        private MockHttpContextAccessor _contextAncessor = new MockHttpContextAccessor();

        [Fact]
        public void get_default_orign()
        {
            _contextAncessor.HttpContext.Request.Scheme = "http";
            _contextAncessor.HttpContext.Request.Host = new HostString("server");

            var target = new DefaultServerUrls(_contextAncessor);
            target.Origin.Should().Be("http://server");
        }

        [Fact]
        public void get_set_origin()
        {
            var target = new DefaultServerUrls(_contextAncessor);
            target.Origin = "http://server";
            target.Origin.Should().Be("http://server");
        }


        [Fact]
        public void get_set_basePath()
        {
            var target = new DefaultServerUrls(_contextAncessor);
            target.BasePath = "/path/";
            target.BasePath.Should().Be("/path");
        }
    }
}