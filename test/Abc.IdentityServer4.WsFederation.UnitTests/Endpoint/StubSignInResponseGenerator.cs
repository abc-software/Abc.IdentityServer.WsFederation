using Abc.IdentityServer.WsFederation.Validation;
using Abc.IdentityServer.WsFederation.ResponseProcessing;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Threading.Tasks;

namespace Abc.IdentityServer.WsFederation.Endpoint.UnitTests
{
    internal class StubSignInResponseGenerator : ISignInResponseGenerator
    {
        public WsFederationMessage Result { get; set; }

        public Task<WsFederationMessage> GenerateResponseAsync(WsFederationValidationResult validationResult)
        {
            return Task.FromResult(Result);
        }
    }
}