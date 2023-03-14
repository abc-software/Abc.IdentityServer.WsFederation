using Abc.IdentityServer4.WsFederation.ResponseProcessing;
using Abc.IdentityServer4.WsFederation.Validation;
using System.Threading.Tasks;

namespace Abc.IdentityServer4.WsFederation.Endpoint.UnitTests
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