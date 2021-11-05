using IdentityServer4.ResponseHandling;
using IdentityServer4.WsFederation.Validation;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation
{
    public interface ISignInInteractionResponseGenerator
    {
        Task<InteractionResponse> ProcessInteractionAsync(ValidatedWsFederationRequest request);
    }
}
