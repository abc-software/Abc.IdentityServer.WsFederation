using Abc.IdentityServer.WsFederation.ResponseProcessing;
using Abc.IdentityServer.WsFederation.Validation;
using System.Threading.Tasks;

namespace Abc.IdentityServer.WsFederation.Endpoint.UnitTests
{
    internal class StubSignInInteractionResponseGenerator : ISignInInteractionResponseGenerator
    {
        internal InteractionResponse Response { get; set; } = new InteractionResponse();

        public Task<InteractionResponse> ProcessInteractionAsync(ValidatedWsFederationRequest request, ConsentResponse consent = null)
        {
            return Task.FromResult(Response);
        }
    }
}