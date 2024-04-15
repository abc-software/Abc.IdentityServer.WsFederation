using Abc.IdentityServer.WsFederation.Services;
using Abc.IdentityServer.WsFederation.Validation;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Saml11 = Microsoft.IdentityModel.Tokens.Saml;
using Saml2 = Microsoft.IdentityModel.Tokens.Saml2;

namespace Abc.IdentityServer.WsFederation.ResponseProcessing.UnitTests
{
    public class SignInResponseGeneratorFixture
    {
        private TestSignInResponseGenerator _target;
        private StubClock _clock = new StubClock();
        private ILogger<SignInResponseGenerator> _logger = TestLogger.Create<SignInResponseGenerator>();
        private WsFederationOptions _options = new WsFederationOptions();
        private MockClaimsService _claimsService = new MockClaimsService();
        private MockKeyMaterialService _keys = new MockKeyMaterialService();
        private MockResourceStore _resources = new MockResourceStore();
        private TestIssuerNameService _issuerNameService = new TestIssuerNameService();

        private class TestSignInResponseGenerator : SignInResponseGenerator
        {
            public TestSignInResponseGenerator(WsFederationOptions options, WsFederation.Services.IClaimsService claimsService, IKeyMaterialService keys, IResourceStore resources, IIssuerNameService issuerNameService, IClock clock, ILogger<SignInResponseGenerator> logger) 
                : base(options, claimsService, keys, resources, issuerNameService, clock, logger)
            {
            }

            public new Task<IList<string>> GetRequestedClaimTypesAsync(IEnumerable<string> scopes)
            {
                return base.GetRequestedClaimTypesAsync(scopes);
            }

            public new Task<ClaimsIdentity> CreateSubjectAsync(WsFederationValidationResult result)
            {
                return base.CreateSubjectAsync(result);
            }
        }

        public SignInResponseGeneratorFixture()
        {
            _target = new TestSignInResponseGenerator(
                _options,
                _claimsService,
                _keys,
                _resources,
                _issuerNameService,
                _clock,
                _logger
                );
        }

        [Fact]
        public async Task requested_claims_from_resourcestore_identityresources()
        {
            _resources.IdentityResources.Add(new IdentityResources.OpenId());
            _resources.IdentityResources.Add(new IdentityResource() { Name = "scope1",  UserClaims = new string[] { "claim1" } });
            var scopes = new[] { "openid", "scope1" };

            var claims = await _target.GetRequestedClaimTypesAsync(scopes);

            claims.Should().Contain("sub");
            claims.Should().Contain("claim1");
        }

        [Fact]
        public async Task autenticationmethod_for_pwd_should_be_set_password()
        {
            var user = CreateDefaultUser();
            var request = CreateDefaultWsFederationRequest(user);

            var identity = await _target.CreateSubjectAsync(new WsFederationValidationResult(request));

            var expectedClaims = new List<Claim>
            {
                { new Claim(ClaimTypes.NameIdentifier, "123").AddProperty(Saml11.ClaimProperties.SamlNameIdentifierFormat, "urn:oasis:names:tc:SAML:1.1:nameid-format:unspecified") },
                { new Claim(ClaimTypes.AuthenticationMethod, "urn:oasis:names:tc:SAML:1.0:am:password") },
                { new Claim(ClaimTypes.AuthenticationInstant, user.AuthenticationTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.000Z"), ClaimValueTypes.DateTime) },
            };

            identity.AuthenticationType.Should().Be("idsrv");
            identity.Claims.Should().BeEquivalentTo(expectedClaims, opts => opts.Excluding(si => si.Subject));
        }

        [Fact]
        public async Task default_nameidentifierformat_should_be_set_from_options()
        {
            _options.DefaultNameIdentifierFormat = "urn:format";

            var user = CreateDefaultUser();
            var request = CreateDefaultWsFederationRequest(user);

            var identity = await _target.CreateSubjectAsync(new WsFederationValidationResult(request));

            var expectedClaims = new List<Claim>
            {
                { new Claim(ClaimTypes.NameIdentifier, "123").AddProperty(Saml11.ClaimProperties.SamlNameIdentifierFormat, "urn:format") },
                { new Claim(ClaimTypes.AuthenticationMethod, "urn:oasis:names:tc:SAML:1.0:am:password") },
                { new Claim(ClaimTypes.AuthenticationInstant, user.AuthenticationTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.000Z"), ClaimValueTypes.DateTime) },
            };

            identity.AuthenticationType.Should().Be("idsrv");
            identity.Claims.Should().BeEquivalentTo(expectedClaims, opts => opts.Excluding(si => si.Subject));
        }

        [Fact]
        public async Task nameidentifierformat_may_be_set_from_relayingparty()
        {
            var user = CreateDefaultUser();
            var request = CreateDefaultWsFederationRequest(user);
            request.RelyingParty = new Stores.RelyingParty()
            {
                NameIdentifierFormat = "urn:relyingparty"
            };

            var identity = await _target.CreateSubjectAsync(new WsFederationValidationResult(request));

            var expectedClaims = new List<Claim>
            {
                { new Claim(ClaimTypes.NameIdentifier, "123").AddProperty(Saml11.ClaimProperties.SamlNameIdentifierFormat, "urn:relyingparty") },
                { new Claim(ClaimTypes.AuthenticationMethod, "urn:oasis:names:tc:SAML:1.0:am:password") },
                { new Claim(ClaimTypes.AuthenticationInstant, user.AuthenticationTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.000Z"), ClaimValueTypes.DateTime) },
            };

            identity.AuthenticationType.Should().Be("idsrv");
            identity.Claims.Should().BeEquivalentTo(expectedClaims, opts => opts.Excluding(si => si.Subject));
        }

        [Fact]
        public async Task autenticationmethod_not_pwd_should_be_set_unspecified()
        {
            var user = CreateDefaultUser();
            user.AuthenticationMethods.Clear();
            user.AuthenticationMethods.Add("urn:test");
            var request = CreateDefaultWsFederationRequest(user);

            var identity = await _target.CreateSubjectAsync(new WsFederationValidationResult(request));

            var expectedClaims = new List<Claim>
            {
                { new Claim(ClaimTypes.NameIdentifier, "123").AddProperty(Saml11.ClaimProperties.SamlNameIdentifierFormat, "urn:oasis:names:tc:SAML:1.1:nameid-format:unspecified") },
                { new Claim(ClaimTypes.AuthenticationMethod, "urn:oasis:names:tc:SAML:1.0:am:unspecified") },
                { new Claim(ClaimTypes.AuthenticationInstant, user.AuthenticationTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.000Z"), ClaimValueTypes.DateTime) },
            };

            identity.AuthenticationType.Should().Be("idsrv");
            identity.Claims.Should().BeEquivalentTo(expectedClaims, opts => opts.Excluding(si => si.Subject));
        }

        [Fact]
        public async Task autentication_method_may_be_change_in_claims_service()
        {
            _claimsService.TokenClaims.Add(new Claim(ClaimTypes.AuthenticationMethod, "urn:authmehtod"));

            var user = CreateDefaultUser();
            var request = CreateDefaultWsFederationRequest(user);

            var identity = await _target.CreateSubjectAsync(new WsFederationValidationResult(request));

            var expectedClaims = new List<Claim>
            {
                { new Claim(ClaimTypes.NameIdentifier, "123").AddProperty(Saml11.ClaimProperties.SamlNameIdentifierFormat, "urn:oasis:names:tc:SAML:1.1:nameid-format:unspecified") },
                { new Claim(ClaimTypes.AuthenticationMethod, "urn:authmehtod") },
                { new Claim(ClaimTypes.AuthenticationInstant, user.AuthenticationTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.000Z"), ClaimValueTypes.DateTime) },
            };

            identity.AuthenticationType.Should().Be("idsrv");
            identity.Claims.Should().BeEquivalentTo(expectedClaims, opts => opts.Excluding(si => si.Subject));
        }

        [Fact]
        public async Task nameidentifier_may_be_change_in_claims_service()
        {
            _claimsService.TokenClaims.Add(new Claim(ClaimTypes.NameIdentifier, "134").AddProperty(Saml11.ClaimProperties.SamlNameIdentifierFormat, "urn:myformat"));

            var user = CreateDefaultUser();
            var request = CreateDefaultWsFederationRequest(user);

            var identity = await _target.CreateSubjectAsync(new WsFederationValidationResult(request));

            var expectedClaims = new List<Claim>
            {
                { new Claim(ClaimTypes.NameIdentifier, "134").AddProperty(Saml11.ClaimProperties.SamlNameIdentifierFormat, "urn:myformat") },
                { new Claim(ClaimTypes.AuthenticationMethod, "urn:oasis:names:tc:SAML:1.0:am:password") },
                { new Claim(ClaimTypes.AuthenticationInstant, user.AuthenticationTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.000Z"), ClaimValueTypes.DateTime) },
            };

            identity.AuthenticationType.Should().Be("idsrv");
            identity.Claims.Should().BeEquivalentTo(expectedClaims, opts => opts.Excluding(si => si.Subject));
        }

        [Fact]
        public async Task authentication_instant_may_be_change_in_claims_service()
        {
            _claimsService.TokenClaims.Add(new Claim(ClaimTypes.AuthenticationInstant, "2022-01-01T01:01:01.001Z", ClaimValueTypes.DateTime));

            var user = CreateDefaultUser();
            var request = CreateDefaultWsFederationRequest(user);

            var identity = await _target.CreateSubjectAsync(new WsFederationValidationResult(request));

            var expectedClaims = new List<Claim>
            {
                { new Claim(ClaimTypes.NameIdentifier, "123").AddProperty(Saml11.ClaimProperties.SamlNameIdentifierFormat, "urn:oasis:names:tc:SAML:1.1:nameid-format:unspecified") },
                { new Claim(ClaimTypes.AuthenticationMethod, "urn:oasis:names:tc:SAML:1.0:am:password") },
                { new Claim(ClaimTypes.AuthenticationInstant, "2022-01-01T01:01:01.001Z", ClaimValueTypes.DateTime) },
            };

            identity.AuthenticationType.Should().Be("idsrv");
            identity.Claims.Should().BeEquivalentTo(expectedClaims, opts => opts.Excluding(si => si.Subject));
        }

        [Fact]
        public async Task isseraddress_context_saml2_shold_be_set_from_validatedrequest()
        {
            _keys.SigningCredentials.Add(TestCert.LoadSigningCredentials());
            _claimsService.TokenClaims.Add(new Claim("urn:claim1", "some_value"));

            var user = CreateDefaultUser();
            var request = CreateDefaultWsFederationRequest(user);
            request.WsFederationMessage.Wctx = "ctx";

            var message = await _target.GenerateResponseAsync(new WsFederationValidationResult(request));

            message.IssuerAddress.Should().Be("https://reply");
            message.Wa.Should().Be("wsignin1.0");
            message.Wctx.Should().Be("ctx");

            var tokenString = message.GetToken();
            var handler = new Saml2.Saml2SecurityTokenHandler();
            var canReadToken = handler.CanReadToken(tokenString);
            canReadToken.Should().BeTrue();

            var token = handler.ReadSaml2Token(tokenString);
            var authStatements = token.Assertion.Statements.OfType<Saml2.Saml2AuthenticationStatement>();
            authStatements.Should().ContainSingle();
            var authStatement = authStatements.First();
            Assert.True(authStatement.AuthenticationInstant <= user.AuthenticationTime.Value.AddMinutes(5));

            var attrStatements = token.Assertion.Statements.OfType<Saml2.Saml2AttributeStatement>();
            attrStatements.Should().ContainSingle();

            var attrStatement = attrStatements.First();
            attrStatement.Attributes.Should().Contain(x => x.Name == "urn:claim1");
        }

        [Fact]
        public async Task tokentype_may_be_set_from_options()
        {
            _keys.SigningCredentials.Add(TestCert.LoadSigningCredentials());
            _claimsService.TokenClaims.Add(new Claim(ClaimTypes.Name, "some_value"));
            _options.DefaultTokenType = "urn:oasis:names:tc:SAML:1.0:assertion";

            var user = CreateDefaultUser();
            var request = CreateDefaultWsFederationRequest(user);
            request.WsFederationMessage.Wctx = "ctx";

            var message = await _target.GenerateResponseAsync(new WsFederationValidationResult(request));

            message.IssuerAddress.Should().Be("https://reply");
            message.Wa.Should().Be("wsignin1.0");
            message.Wctx.Should().Be("ctx");

            var tokenString = message.GetToken();
            var handler = new Saml11.SamlSecurityTokenHandler();
            var canReadToken = handler.CanReadToken(tokenString);
            canReadToken.Should().BeTrue();

            var token = handler.ReadSamlToken(tokenString);
            var authStatements = token.Assertion.Statements.OfType<Saml11.SamlAuthenticationStatement>();
            authStatements.Should().ContainSingle();
            var authStatement = authStatements.First();
            Assert.True(authStatement.AuthenticationInstant <= user.AuthenticationTime.Value.AddMinutes(5));

            var attrStatements = token.Assertion.Statements.OfType<Saml11.SamlAttributeStatement>();
            attrStatements.Should().ContainSingle();

            var attrStatement = attrStatements.First();
            attrStatement.Attributes.Should().Contain(x => x.ClaimType == ClaimTypes.Name);
        }

        [Fact]
        public async Task tokentype_may_be_set_from_realyingparty()
        {
            _keys.SigningCredentials.Add(TestCert.LoadSigningCredentials());
            _claimsService.TokenClaims.Add(new Claim(ClaimTypes.Name, "some_value"));

            var user = CreateDefaultUser();
            var request = CreateDefaultWsFederationRequest(user);
            request.RelyingParty = new Stores.RelyingParty()
            {
                TokenType = "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV1.1",
            };

            var message = await _target.GenerateResponseAsync(new WsFederationValidationResult(request));

            var tokenString = message.GetToken();
            var handler = new Saml11.SamlSecurityTokenHandler();
            var canReadToken = handler.CanReadToken(tokenString);
            canReadToken.Should().BeTrue();
        }

        private static ValidatedWsFederationRequest CreateDefaultWsFederationRequest(Ids.IdentityServerUser user)
        {
            return new ValidatedWsFederationRequest
            {
                ClientId = "urn:foo",
                Client = new Client() { ClientId = "urn:foo" },
                ValidatedResources = new ResourceValidationResult(),
                Subject = user.CreatePrincipal(),
                ReplyUrl = "https://reply",
            };
        }
        private Ids.IdentityServerUser CreateDefaultUser()
        {
            var user = new Ids.IdentityServerUser("123")
            {
                IdentityProvider = Ids.IdentityServerConstants.LocalIdentityProvider
            };

            user.AuthenticationMethods.Add("pwd");
            user.AuthenticationTime = _clock.UtcNow.UtcDateTime;
            return user;
        }

    }
}
