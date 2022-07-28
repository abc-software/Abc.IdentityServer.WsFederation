using FluentAssertions;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Xunit;
using System;
using System.Threading.Tasks;

namespace Abc.IdentityServer4.WsFederation.Tests.Validation
{
    public class WsFederationRequestValidatorSignOutFixture : WsFederationRequestValidatorBase
    {
        [Fact]
        public async Task Valid_request()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignout1.0",
                Wtrealm = "urn:test",
            };

            var result = await validator.ValidateSignOutRequestAsync(message);

            result.IsError.Should().Be(false);
        }

        [Fact]
        public async Task Valid_request_with_all_paramters()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignout1.0",
                Wtrealm = "urn:test",
                Wreply = "https://wsfed/postlogout",
                Wctx = "context",
                Wct = clock.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            };

            var result = await validator.ValidateSignOutRequestAsync(message);

            result.IsError.Should().Be(false);
        }

        [Fact]
        public void Null_Parameter()
        {
            Func<Task> act = () => validator.ValidateSignOutRequestAsync(null);
            act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task Empty_Parameters()
        {
            var result = await validator.ValidateSignOutRequestAsync(new WsFederationMessage());

            result.IsError.Should().BeTrue();
            result.Error.Should().Be("invalid_request");
        }

        [Fact]
        public async Task Missing_Wtrealm()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsigninout1.0",
            };

            var result = await validator.ValidateSignOutRequestAsync(message);

            result.IsError.Should().BeTrue();
            result.Error.Should().Be("invalid_request");
        }

        [Fact]
        public async Task Invalid_Protocol_Client()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignout1.0",
                Wtrealm = "codeclient",
            };

            var result = await validator.ValidateSignOutRequestAsync(message);

            result.IsError.Should().BeTrue();
            result.Error.Should().Be("invalid_relying_party");
        }

        [Fact]
        public async Task Malformed_Wreply()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignout1.0",
                Wtrealm = "urn:test",
                Wreply = "malformed",
            };

            var result = await validator.ValidateSignOutRequestAsync(message);

            result.IsError.Should().BeTrue();
            result.Error.Should().Be("invalid_request");
        }

        [Fact]
        public async Task Ignore_Invalid_Wreply()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignout1.0",
                Wtrealm = "urn:test",
                Wreply = "https://host/reply",
            };

            var result = await validator.ValidateSignOutRequestAsync(message);

            result.IsError.Should().BeFalse();
        }

        [Fact]
        public async Task Malformed_Wreply_Triple_Slash()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignout1.0",
                Wtrealm = "urn:test",
                Wreply = "https:///attacker.com",
            };

            var result = await validator.ValidateSignOutRequestAsync(message);

            result.IsError.Should().BeTrue();
            result.Error.Should().Be("invalid_request");
        }

        [Fact]
        public async Task Malformed_Wct()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignout1.0",
                Wtrealm = "urn:test",
                Wct = "malformed",
            };

            var result = await validator.ValidateSignOutRequestAsync(message);

            result.IsError.Should().BeTrue();
            result.Error.Should().Be("invalid_request");
        }

        [Fact]
        public async Task Invalid_Wct_in_funture()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignout1.0",
                Wtrealm = "urn:test",
                Wct = clock.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            };

            var result = await validator.ValidateSignOutRequestAsync(message);

            result.IsError.Should().BeTrue();
            result.Error.Should().Be("invalid_request");
        }

        [Fact]
        public async Task Invalid_Wct_in_past()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignout1.0",
                Wtrealm = "urn:test",
                Wct = clock.UtcNow.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            };

            var result = await validator.ValidateSignOutRequestAsync(message);

            result.IsError.Should().BeTrue();
            result.Error.Should().Be("invalid_request");
        }
    }
}
