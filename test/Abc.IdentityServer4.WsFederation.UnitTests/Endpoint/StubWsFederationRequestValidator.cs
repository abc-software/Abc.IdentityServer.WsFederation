using Abc.IdentityServer.WsFederation.Validation;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Abc.IdentityServer.WsFederation.Endpoint.UnitTests
{
    internal class StubWsFederationRequestValidator : IWsFederationRequestValidator
    {
        public WsFederationValidationResult Result { get; set; }

        public Task<WsFederationValidationResult> ValidateSignInRequestAsync(WsFederationMessage message, ClaimsPrincipal user)
        {
            return Task.FromResult(Result);
        }

        public Task<WsFederationValidationResult> ValidateSignOutRequestAsync(WsFederationMessage message)
        {
            return Task.FromResult(Result);
        }
    }
}