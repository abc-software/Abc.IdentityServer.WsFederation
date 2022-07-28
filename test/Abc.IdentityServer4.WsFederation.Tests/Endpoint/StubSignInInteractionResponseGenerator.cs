
using IdentityServer4.Models;
using IdentityServer4.ResponseHandling;
using Abc.IdentityServer4.WsFederation.Validation;
using Abc.IdentityServer4.WsFederation.ResponseProcessing;
using System.Threading.Tasks;

namespace Abc.IdentityServer4.WsFederation.Tests.Endpoint
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