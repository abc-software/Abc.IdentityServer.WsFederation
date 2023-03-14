using FluentAssertions;
using IdentityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Abc.IdentityServer4.WsFederation.Services.UnitTests
{
    public class DefaultClaimServiceFixture
    {
        private DefaultClaimsService _target;
        private ValidatedRequest _validatedRequest;
        private MockProfileService _mockMockProfileService = new MockProfileService();
        private Client _client;
        private ClaimsPrincipal _user;

        public DefaultClaimServiceFixture()
        {
            _client = new Client
            {
                ClientId = "client",
                Claims = { new ClientClaim("some_claim", "some_claim_value") }
            };

            _user = new Ids.IdentityServerUser("bob")
            {
                IdentityProvider = "idp",
                AuthenticationMethods = { OidcConstants.AuthenticationMethods.Password },
                AuthenticationTime = new System.DateTime(2000, 1, 1),
            }.CreatePrincipal();

            _target = new DefaultClaimsService(_mockMockProfileService, TestLogger.Create<DefaultClaimsService>());

            _validatedRequest = new ValidatedRequest();
            _validatedRequest.Subject = _user;
            _validatedRequest.Options = new IdentityServerOptions();
            _validatedRequest.SetClient(_client);
        }

        [Fact]
        public void Ctor_DefaultClaimsService_should_throws_exception()
        {
            {
                Action act = () => new DefaultClaimsService(null, TestLogger.Create<DefaultClaimsService>());
                act.Should().Throw<ArgumentNullException>();
            }

            {
                Action act = () => new DefaultClaimsService(_mockMockProfileService, null);
                act.Should().Throw<ArgumentNullException>();
            }
        }

        [Fact]
        public async Task GetClaimsAsync_should_return_profile_user_claims()
        {
            _mockMockProfileService.ProfileClaims.Add(new Claim(JwtClaimTypes.Subject, "sub"));

            var requestedClaimTypes = new string[0];
            var claims = await _target.GetClaimsAsync(_validatedRequest, requestedClaimTypes);

            var types = claims.Select(x => x.Type);
            types.Should().Contain(JwtClaimTypes.Subject);
        }

        [Theory]
        [InlineData(WsFederationConstants.TokenTypes.OasisWssSaml2TokenProfile11)]
        [InlineData(WsFederationConstants.TokenTypes.Saml2TokenProfile11)]
        public void MapAsync_should_return_mapped_saml2_claims(string tokenType)
        {
            var claims = new List<Claim>() {
                new Claim(JwtClaimTypes.Subject, "sub"),
                new Claim(JwtClaimTypes.Name, "bob").AddProperty("property", "p_val"),
                new Claim(JwtClaimTypes.NickName, "bob_nick"),
                new Claim(ClaimTypes.Email, "bob@a.lv"), // long claim name
            };

            var mapping = new Dictionary<string, string>()
            {
                { JwtClaimTypes.Subject, "urn:nameidentifier" },
                { JwtClaimTypes.Name, "http://test.org/name" },
            };

            var mappedClaims = _target.MapClaims(mapping, tokenType, claims);

            var expected = new List<Claim>() {
                new Claim("urn:nameidentifier", "sub").AddProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties/ShortTypeName", JwtClaimTypes.Subject),
                new Claim("http://test.org/name", "bob").AddProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties/ShortTypeName", JwtClaimTypes.Name).AddProperty("property", "p_val"),
                new Claim(JwtClaimTypes.NickName, "bob_nick"),
                new Claim(ClaimTypes.Email, "bob@a.lv"),
            };

            mappedClaims.Should().BeEquivalentTo(expected);
        }

        [Theory]
        [InlineData(WsFederationConstants.TokenTypes.OasisWssSaml11TokenProfile11)]
        [InlineData(WsFederationConstants.TokenTypes.Saml11TokenProfile11)]
        public void MapAsync_should_return_mapped_saml11_claims(string tokenType)
        {
            var claims = new List<Claim>() {
                new Claim(JwtClaimTypes.Subject, "sub").AddProperty("format", "f1"),
                new Claim(JwtClaimTypes.Name, "bob"),
                new Claim(JwtClaimTypes.NickName, "bob_nick"),
                new Claim("/address", "address"), // invalid claim name
                new Claim(ClaimTypes.Email, "bob@a.lv"), // long claim name
            };

            var mapping = new Dictionary<string, string>()
            {
                { JwtClaimTypes.Subject, "urn:nameidentifier" },
                { JwtClaimTypes.Name, "http://test.org/name" },
            };

            var mappedClaims = _target.MapClaims(mapping, tokenType, claims);

            var expected = new List<Claim>() {
                new Claim("urn:nameidentifier", "sub").AddProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties/ShortTypeName", JwtClaimTypes.Subject).AddProperty("format", "f1"),
                new Claim("http://test.org/name", "bob").AddProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties/ShortTypeName", JwtClaimTypes.Name),
                new Claim(ClaimTypes.Email, "bob@a.lv"),
            };

            mappedClaims.Should().BeEquivalentTo(expected);
        }
    }
}
