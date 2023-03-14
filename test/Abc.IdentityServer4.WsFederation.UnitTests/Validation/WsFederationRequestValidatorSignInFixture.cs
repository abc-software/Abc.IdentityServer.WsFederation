using FluentAssertions;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Xunit;
using System;
using System.Threading.Tasks;
using FluentAssertions.Common;

namespace Abc.IdentityServer4.WsFederation.Validation.UnitTests
{
    public class WsFederationRequestValidatorSignInFixture : WsFederationRequestValidatorBase
    {
        [Fact]
        public async Task valid_request_should_be_ok()
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
        public async Task valid_request_with_all_paramters_should_be_ok()
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
                Whr = "test",
            };

            var result = await validator.ValidateSignInRequestAsync(message, null);

            result.IsError.Should().Be(false);
        }

        [Fact]
        public void null_wsfed_should_be_exception()
        {
            Func<Task> act = () => validator.ValidateSignInRequestAsync(null, null);
            act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task empty_wsfed_should_be_error()
        {
            var result = await validator.ValidateSignInRequestAsync(new WsFederationMessage(), null);

            result.IsError.Should().BeTrue();
            result.Error.Should().Be("invalid_request");
        }

        [Fact]
        public async Task wsfed_without_wtrealm_should_be_error()
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
        public async Task wsfed_invalid_wtrealm_should_be_error()
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
        public async Task wsfed_malformed_wreply_should_be_error()
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
        public async Task wsfed_invalid_wreply_should_be_error()
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
        public async Task wsfed_malformed_triple_slash_wreply_should_be_error()
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
        public async Task wsfed_malformed_wfresh_should_be_error()
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
        public async Task wsfed_negative_wfresh_should_be_error()
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
        public async Task wsfed_malformed_wct_should_be_error()
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
        public async Task wsfed_wct_in_future_should_be_error()
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
        public async Task wsfed_wct_in_past_should_be_error()
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
        public async Task wsfed_wreq_and_wregptr_should_be_error()
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

        [Fact]
        public async Task wfresh_zero_should_remove_from_wsfed()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignin1.0",
                Wtrealm = "urn:test",
                Wfresh = "0",
            };

            var result = await validator.ValidateSignInRequestAsync(message, null);

            result.ValidatedRequest.WsFederationMessage.Wfresh.Should().BeNull();
        }

        [Fact]
        public async Task whr_in_restrictions_should_remove_from_wsfed()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignin1.0",
                Wtrealm = "urn:test",
                Whr = "local",
            };

            var result = await validator.ValidateSignInRequestAsync(message, null);

            result.ValidatedRequest.WsFederationMessage.Whr.Should().BeNull();
        }

        [Fact]
        public async Task wct_should_remove_from_wsfed()
        {
            var message = new WsFederationMessage()
            {
                Wa = "wsignin1.0",
                Wtrealm = "urn:test",
                Wct = clock.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            };

            var result = await validator.ValidateSignInRequestAsync(message, null);

            result.ValidatedRequest.WsFederationMessage.Wct.Should().BeNull();
        }
    }
}
