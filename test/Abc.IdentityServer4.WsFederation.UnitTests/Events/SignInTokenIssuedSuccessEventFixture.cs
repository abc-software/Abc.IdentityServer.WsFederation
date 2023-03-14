using Abc.IdentityServer4.WsFederation.Validation;
using FluentAssertions;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Xunit;

namespace Abc.IdentityServer4.WsFederation.Events.UnitTests
{
    public class SignInTokenIssuedSuccessEventFixture
    {
        private ValidatedWsFederationRequest _request;
        private WsFederationMessage _response;

        public SignInTokenIssuedSuccessEventFixture()
        {
            var validatedResources = new ResourceValidationResult(null, new ParsedScopeValue[] { new ParsedScopeValue("s1"), new ParsedScopeValue("s2") });

            _request = new ValidatedWsFederationRequest()
            {
                ClientId = "client",
                Client = new Client()
                {
                    ClientId = "client",
                    ClientName = "clientName",
                },
                Subject = new Ids.IdentityServerUser("bob").CreatePrincipal(),
                ValidatedResources = validatedResources,
            };

            _response = new WsFederationMessage()
            {
                Wa = "wsigin1.0",
                Wresult = "some_token",
            };
        }

        [Fact]
        public void Ctor_SignInTokenIssuedSuccessEvent_null_response()
        {
            var target = new SignInTokenIssuedSuccessEvent(null, _request);

            target.Tokens.Should().BeEmpty();
        }

        [Fact]
        public void Ctor_SignInTokenIssuedSuccessEvent_null_request()
        {
            var target = new SignInTokenIssuedSuccessEvent(_response, null);

            target.Endpoint.Should().Be("WsFederation");
            target.SubjectId.Should().BeNull();
            target.ClientId.Should().BeNull();
            target.ClientName.Should().BeNull();
            target.Scopes.Should().BeNull();
        }

        [Fact]
        public void Ctor_SignInTokenIssuedSuccessEvent_empty_response()
        {
            var response = new WsFederationMessage();

            var target = new SignInTokenIssuedSuccessEvent(response, _request);

            target.Tokens.Should().NotBeEmpty();
        }

        [Fact]
        public void Ctor_SignInTokenIssuedSuccessEvent_success()
        {
            var target = new SignInTokenIssuedSuccessEvent(_response, _request);

            target.Tokens.Should().NotBeNullOrEmpty();
            target.Endpoint.Should().Be("WsFederation");
            target.SubjectId.Should().Be("bob");
            target.ClientId.Should().Be("client");
            target.ClientName.Should().Be("clientName");
            target.Scopes.Should().Be("s1 s2");

            target.Category.Should().Be("Token");
            target.Message.Should().BeNull();
            target.Id.Should().Be(2000);
            target.Name.Should().Be("Token Issued Success");
            target.EventType.Should().Be(EventTypes.Success);

            target.ActivityId.Should().BeNull();
            target.GrantType.Should().BeNull();
        }
    }
}