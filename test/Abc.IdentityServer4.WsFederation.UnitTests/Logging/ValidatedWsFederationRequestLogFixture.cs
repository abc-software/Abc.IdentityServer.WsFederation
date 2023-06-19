using Abc.IdentityServer.WsFederation.Validation;
using FluentAssertions;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Xunit;

namespace Abc.IdentityServer.WsFederation.Logging.UnitTests
{
    public class ValidatedWsFederationRequestLogFixture
    {
        private ValidatedWsFederationRequest _request;

        public ValidatedWsFederationRequestLogFixture()
        {
            _request = new ValidatedWsFederationRequest()
            {
                ClientId = "client",
                Client = new Client()
                {
                    ClientId = "client",
                    ClientName = "clientName",
                },
                Subject = new Ids.IdentityServerUser("bob").CreatePrincipal(),
                WsFederationMessage = new WsFederationMessage()
                {
                    Wa = "wsignin1.0",
                    Wctx = "context",
                },
            };
        }

        [Fact]
        public void Serialzie()
        {
            var target = new ValidatedWsFederationRequestLog(_request, new string[] { "wctx" });
            var str = target.ToString();

            target.SubjectId.Should().Be("bob");
            target.ClientId.Should().Be("client");
            target.ClientName.Should().Be("clientName");

            var expected = @"{
  ""ClientId"": ""client"",
  ""ClientName"": ""clientName"",
  ""AllowedRedirectUris"": [],
  ""AllowedPostLogoutRedirectUris"": [],
  ""SubjectId"": ""bob"",
  ""Raw"": {
    ""wa"": ""wsignin1.0"",
    ""wctx"": ""***REDACTED***""
  }
}";

            str.Should().Be(expected);
        }
    }
}
