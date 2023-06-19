using Abc.IdentityServer.WsFederation.ResponseProcessing;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Xunit;

namespace Abc.IdentityServer.WsFederation.Endpoints.UnitTests
{
    public class WsFederationMetadataEndpointFixture
    {
        private WsFederationMetadataEndpoint _target;
        private DefaultHttpContext _context;
        private IMetadataResponseGenerator _stubMetadataGenerator = new StubMetadataResponseGenerator();

        public WsFederationMetadataEndpointFixture()
        {
            _context = new DefaultHttpContext();
            _context.SetIdentityServerOrigin("https://server");
            _context.SetIdentityServerBasePath("/");

            _target = new WsFederationMetadataEndpoint(
                _stubMetadataGenerator,
                TestLogger.Create<WsFederationMetadataEndpoint>()
                );
        }

        [Fact]
        public async Task metadata_not_get_should_return_405()
        {
            _context.Request.Method = "POST";

            var result = await _target.ProcessAsync(_context);

            var statusCode = result as StatusCodeResult;
            statusCode.Should().NotBeNull();
            statusCode.StatusCode.Should().Be(405);
        }

        [Fact]
        public async Task metadata_should_return_metadata_result()
        {
            _context.Request.Method = "GET";

            var result = await _target.ProcessAsync(_context);

            result.Should().BeOfType<Results.MetadataResult>();
        }
    }
}
