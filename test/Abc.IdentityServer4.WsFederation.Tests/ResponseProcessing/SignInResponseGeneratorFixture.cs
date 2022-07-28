using Abc.IdentityServer4.WsFederation.ResponseProcessing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Xunit;
using System;
using System.Collections.Generic;
using System.Text;

namespace Abc.IdentityServer4.WsFederation.Tests.ResponseProcessing
{
    public class SignInResponseGeneratorFixture
    {
        private SignInResponseGenerator _target;
        private StubClock _clock = new StubClock();
        private ILogger<SignInResponseGenerator> _logger = TestLogger.Create<SignInResponseGenerator>();
        private WsFederationOptions _options = new WsFederationOptions();

        public SignInResponseGeneratorFixture()
        {
            _target = new SignInResponseGenerator(
                _contextAncessor,
                _options,
                _claimsService,
                _keys,
                _resources,
                _clock,
                _logger
                );
        }
    }
}
