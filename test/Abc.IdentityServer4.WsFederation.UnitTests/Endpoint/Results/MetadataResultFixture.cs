using Abc.IdentityModel.Metadata;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Abc.IdentityServer.WsFederation.Endpoints.Results.UnitTests
{
    public class MetadataResultFixture
    {
        private EntityDescriptor _descriptor;
        private IServerUrls _urls;
        private MetadataResult _target;
        private DefaultHttpContext _context;

        public MetadataResultFixture()
        {
            _context = new DefaultHttpContext();
            _context.Response.Body = new MemoryStream();
            _context.RequestServices = new ServiceCollection().BuildServiceProvider();

            var applicationDescriptor = new SecurityTokenServiceDescriptor();
            applicationDescriptor.SecurityTokenServiceEndpoints.Add(new EndpointReference("https://localhost/wsfed"));
            applicationDescriptor.ProtocolsSupported.Add(new Uri(Microsoft.IdentityModel.Protocols.WsFederation.WsFederationConstants.Namespace));

            _descriptor = new EntityDescriptor(new EntityId("urn:issuer"));
            _descriptor.RoleDescriptors.Add(applicationDescriptor);

            _urls = new MockServerUrls()
            {
                Origin = "https://server",
                BasePath = "/",
            };

            _target = new MetadataResult(_descriptor);
        }

        [Fact]
        public void metadata_ctor()
        {
            Action action = () =>
            {
                _target = new MetadataResult(null);
            };

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task metadata_should_pass_results_in_body()
        {
            await _target.ExecuteAsync(_context);
            _context.Response.StatusCode.Should().Be(200);
            _context.Response.ContentType.Should().Contain("application/xml");

            _context.Response.Body.Seek(0, SeekOrigin.Begin);
            using (var rdr = new StreamReader(_context.Response.Body))
            {
                var xml = rdr.ReadToEnd();
                xml.Should().Contain(@"<mt:EntityDescriptor entityID=""urn:issuer""");
            }
        }
    }
}