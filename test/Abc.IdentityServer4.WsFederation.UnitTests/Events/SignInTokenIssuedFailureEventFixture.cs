using Abc.IdentityServer.WsFederation.Validation;
using FluentAssertions;
using Xunit;

namespace Abc.IdentityServer.WsFederation.Events.UnitTests
{
    public class SignInTokenIssuedFailureEventFixture
    {
        private const string Error = "some_error";
        private const string Description = "some_description";

        [Fact]
        public void Ctor_SignInTokenIssuedFailureEvent_null_request()
        {
            var target = new SignInTokenIssuedFailureEvent(null, Error, Description);

            target.Error.Should().Be(Error);
            target.ErrorDescription.Should().Be(Description);
            target.Endpoint.Should().Be("WsFederation");
            target.SubjectId.Should().BeNull();
            target.ClientId.Should().BeNull();
            target.ClientName.Should().BeNull();
            target.Scopes.Should().BeNull();
        }

        [Fact]
        public void Ctor_SignInTokenIssuedFailureEvent_empty_request()
        {
            var request = new ValidatedWsFederationRequest()
            {

            };

            var target = new SignInTokenIssuedFailureEvent(request, Error, Description);

            target.Error.Should().Be(Error);
            target.ErrorDescription.Should().Be(Description);
            target.Endpoint.Should().Be("WsFederation");
            target.SubjectId.Should().BeNull();
            target.ClientId.Should().BeNull();
            target.ClientName.Should().BeNull();
            target.Scopes.Should().BeNull();
        }

        [Fact]
        public void Ctor_SignInTokenIssuedFailureEvent_success_request()
        {
            var request = new ValidatedWsFederationRequest()
            {
                ClientId = "client",
                Client = new Client()
                {
                    ClientId = "client",
                    ClientName = "clientName",
                },
                Subject = new Ids.IdentityServerUser("bob").CreatePrincipal(),
            };

            var target = new SignInTokenIssuedFailureEvent(request, Error, Description);

            target.Error.Should().Be(Error);
            target.ErrorDescription.Should().Be(Description);
            target.Endpoint.Should().Be("WsFederation");
            target.SubjectId.Should().Be("bob");
            target.ClientId.Should().Be("client");
            target.ClientName.Should().Be("clientName");
            target.Scopes.Should().BeNull();

            target.Category.Should().Be("Token");
            target.Message.Should().BeNull();
            target.Id.Should().Be(2001);
            target.Name.Should().Be("Token Issued Failure");
            target.EventType.Should().Be(EventTypes.Failure);

            target.ActivityId.Should().BeNull();
            target.GrantType.Should().BeNull();
        }
    }
}