using Abc.IdentityServer.WsFederation.IntegrationTests.Common;
using FluentAssertions;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Xunit;

namespace Abc.IdentityServer.WsFederation.Endpoints.IntegrationTests
{
    public class WsFederationMetadataEndpointFixture
    {
        private const string Category = "WS-Federation metadata endpoint";

        [Fact]
        [Trait("Category", Category)]
        public async Task entityId_should_be_lowercase()
        {
            IdentityServerPipeline pipeline = new IdentityServerPipeline();
            pipeline.Initialize("/ROOT");

            var result = await pipeline.BackChannelClient.GetAsync("HTTPS://SERVER/ROOT/WSFED/METADATA");

            var xml = await result.Content.ReadAsStringAsync();
            var data = XDocument.Parse(xml);
            var issuer = data.Root.Attribute("entityID").Value;

            issuer.Should().Be("https://server/root");
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task when_lower_case_issuer_option_disabled_issuer_uri_should_be_preserved()
        {
            IdentityServerPipeline pipeline = new IdentityServerPipeline();
            pipeline.Initialize("/ROOT");

            pipeline.Options.LowerCaseIssuerUri = false;

            var result = await pipeline.BackChannelClient.GetAsync("HTTPS://SERVER/ROOT/WSFED/METADATA");

            var xml = await result.Content.ReadAsStringAsync();
            var data = XDocument.Parse(xml);
            var issuer = data.Root.Attribute("entityID").Value;

            issuer.Should().Be("https://server/ROOT");
        }


        [Fact]
        [Trait("Category", Category)]
        public async Task post_metadata_should_return_405()
        {
            IdentityServerPipeline pipeline = new IdentityServerPipeline();
            pipeline.Initialize();

            var response = await pipeline.BackChannelClient.PostAsync(IdentityServerPipeline.WsFedMetadataEndpoint, new StringContent(string.Empty));
            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        }

        [Fact]
        [Trait("Category", Category)]
        public async Task metadata_should_contains_required_values()
        {
            IdentityServerPipeline pipeline = new IdentityServerPipeline();
            pipeline.Initialize();

            var result = await pipeline.BackChannelClient.GetAsync(IdentityServerPipeline.WsFedMetadataEndpoint);

            var xml = await result.Content.ReadAsStringAsync();
            var data = XDocument.Parse(xml);

            const string ns = "urn:oasis:names:tc:SAML:2.0:metadata";
            const string fed = "http://docs.oasis-open.org/wsfed/federation/200706";
            const string wsa = "http://www.w3.org/2005/08/addressing";
            const string dsig = "http://www.w3.org/2000/09/xmldsig#";

            var descriptor = data.Root.Element(XName.Get("RoleDescriptor", ns));

            var protocolSupportEnumeration = descriptor.Attribute("protocolSupportEnumeration").Value;
            protocolSupportEnumeration.Should().Be("http://docs.oasis-open.org/wsfed/federation/200706");

            var keyDescriptor = descriptor.Elements(XName.Get("KeyDescriptor", ns)).First(x => x.Attribute("use").Value == "signing");
            var certBase64Value = keyDescriptor.Element(XName.Get("KeyInfo", dsig)).Element(XName.Get("X509Data", dsig)).Element(XName.Get("X509Certificate", dsig)).Value;

            var tokenTypes = descriptor.Element(XName.Get("TokenTypesOffered", fed)).Elements(XName.Get("TokenType", fed)).Select(x => x.Attribute("Uri").Value);
            tokenTypes.Should().Contain("urn:oasis:names:tc:SAML:2.0:assertion");
            tokenTypes.Should().Contain("urn:oasis:names:tc:SAML:1.0:assertion");

            var passiveRequesterEndpoint = descriptor.Element(XName.Get("PassiveRequestorEndpoint", fed));
            var address = passiveRequesterEndpoint.Element(XName.Get("EndpointReference", wsa)).Element(XName.Get("Address", wsa)).Value;
            address.Should().Be("https://server/wsfed");

            var securityTokenServiceEndpoint = descriptor.Element(XName.Get("SecurityTokenServiceEndpoint", fed));
            address = securityTokenServiceEndpoint.Element(XName.Get("EndpointReference", wsa)).Element(XName.Get("Address", wsa)).Value;
            address.Should().Be("https://server/wsfed");
        }
    }
}
