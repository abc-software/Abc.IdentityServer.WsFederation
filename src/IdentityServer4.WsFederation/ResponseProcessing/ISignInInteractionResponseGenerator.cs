using IdentityServer4.ResponseHandling;
using IdentityServer4.WsFederation.Validation;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation
{
    /// <summary>
    /// Interface for determining if user must login when making requests to the WS-Federation endpoint.
    /// </summary>
    public interface ISignInInteractionResponseGenerator
    {
        /// <summary>
        /// Processes the interaction logic.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="consent">The consent.</param>
        /// <returns>The interaction response.</returns>
        Task<InteractionResponse> ProcessInteractionAsync(ValidatedWsFederationRequest request);
    }
}
