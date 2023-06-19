using Abc.IdentityServer.Extensions;
using FluentAssertions;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System;
using Xunit;

namespace Abc.IdentityServer.WsFederation.Extensions.UnitTests
{
    public class WsFederationMessageExtensionsFixture
    {
        #region GetAcrValues

        [Fact]
        public void null_message_should_throws_exception()
        {
            Action act = () =>
            {
                WsFederationMessage message = null;
                message.GetAcrValues();
            };

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void message_without_parameter_auth_shoud_be_return_null()
        {
            var message = new WsFederationMessage();
            message.GetAcrValues().Should().BeNull();
        }

        [Fact]
        public void message_with_parameter_auth_shoud_return()
        {
            var message = new WsFederationMessage()
            {
                Wauth = "pwd",
            };

            message.GetAcrValues().Should().BeEquivalentTo(new string[] { "pwd" });
        }

        #endregion
    }
}
