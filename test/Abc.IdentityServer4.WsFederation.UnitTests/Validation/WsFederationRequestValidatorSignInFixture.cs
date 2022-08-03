using FluentAssertions;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Xunit;
using System;
using System.Threading.Tasks;

namespace Abc.IdentityServer4.WsFederation.Validation.UnitTests
{
    public class WsFederationRequestValidatorSignInFixture : WsFederationRequestValidatorBase
    {
        [Fact]
        public async Task Valid_request()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignin1.0",
                Wtrealm = "urn:test",
            };

            var result = await validator.ValidateSignInRequestAsync(message, null);

            result.IsError.Should().Be(false);
        }

        [Fact]
        public async Task Valid_request_with_all_paramters()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignin1.0",
                Wtrealm = "urn:test",
                Wreply = "https://wsfed/callback",
                Wctx = "context",
                Wfresh = "1",
                Wct = clock.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Wreq = "<xml/>",
                Whr = "local",
            };

            var result = await validator.ValidateSignInRequestAsync(message, null);

            result.IsError.Should().Be(false);
        }

        [Fact]
        public void Null_Parameter()
        {
            Func<Task> act = () => validator.ValidateSignInRequestAsync(null, null);
            act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task Empty_Parameters()
        {
            var result = await validator.ValidateSignInRequestAsync(new WsFederationMessage(), null);

            result.IsError.Should().BeTrue();
            result.Error.Should().Be("invalid_request");
        }

        [Fact]
        public async Task Missing_Wtrealm()
        {
            var parameters = new WsFederationMessage()
            {
                Wa = "wsignin1.0",
            };

            var result = await validator.ValidateSignInRequestAsync(parameters, null);

            result.IsError.Should().BeTrue();
            result.Error.Should().Be("invalid_request");
        }

        [Fact]
        public async Task Invalid_Protocol_Client()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignin1.0",
                Wtrealm = "codeclient",
            };

            var result = await validator.ValidateSignInRequestAsync(message, null);

            result.IsError.Should().BeTrue();
            result.Error.Should().Be("invalid_relying_party");
        }

        [Fact]
        public async Task Malformed_Wreply()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignin1.0",
                Wtrealm = "urn:test",
                Wreply = "malformed",
            };

            var result = await validator.ValidateSignInRequestAsync(message, null);

            result.IsError.Should().BeTrue();
            result.Error.Should().Be("invalid_request");
        }

        [Fact]
        public async Task Ignore_Invalid_Wreply()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignin1.0",
                Wtrealm = "urn:test",
                Wreply = "https://host/reply",
            };

            var result = await validator.ValidateSignInRequestAsync(message, null);

            result.IsError.Should().BeFalse();
        }

        [Fact]
        public async Task Malformed_Wreply_Triple_Slash()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignin1.0",
                Wtrealm = "urn:test",
                Wreply = "https:///attacker.com",
            };

            var result = await validator.ValidateSignInRequestAsync(message, null);

            result.IsError.Should().BeTrue();
            result.Error.Should().Be("invalid_request");
        }

        [Fact]
        public async Task Malformed_Wfresh()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignin1.0",
                Wtrealm = "urn:test",
                Wfresh = "malformed",
            };

            var result = await validator.ValidateSignInRequestAsync(message, null);

            result.IsError.Should().BeTrue();
            result.Error.Should().Be("invalid_request");
        }

        [Fact]
        public async Task Negative_Wfresh()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignin1.0",
                Wtrealm = "urn:test",
                Wfresh = "-1",
            };

            var result = await validator.ValidateSignInRequestAsync(message, null);

            result.IsError.Should().BeTrue();
            result.Error.Should().Be("invalid_request");
        }

        [Fact]
        public async Task Malformed_Wct()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignin1.0",
                Wtrealm = "urn:test",
                Wct = "malformed",
            };

            var result = await validator.ValidateSignInRequestAsync(message, null);

            result.IsError.Should().BeTrue();
            result.Error.Should().Be("invalid_request");
        }

        [Fact]
        public async Task Invalid_Wct_in_funture()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignin1.0",
                Wtrealm = "urn:test",
                Wct = clock.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            };

            var result = await validator.ValidateSignInRequestAsync(message, null);

            result.IsError.Should().BeTrue();
            result.Error.Should().Be("invalid_request");
        }

        [Fact]
        public async Task Invalid_Wct_in_past()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignin1.0",
                Wtrealm = "urn:test",
                Wct = clock.UtcNow.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            };

            var result = await validator.ValidateSignInRequestAsync(message, null);

            result.IsError.Should().BeTrue();
            result.Error.Should().Be("invalid_request");
        }


        [Fact]
        public async Task Both_Wreq_and_Wreqptr()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignin1.0",
                Wtrealm = "urn:test",
                Wreq = "<wreq></wreq>",
                Wreqptr = "https://poiter"
            };

            var result = await validator.ValidateSignInRequestAsync(message, null);

            result.IsError.Should().BeTrue();
            result.Error.Should().Be("invalid_request");
        }

    }
}
