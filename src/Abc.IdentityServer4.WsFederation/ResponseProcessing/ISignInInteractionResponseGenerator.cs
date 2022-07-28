using Abc.IdentityServer4.WsFederation.Validation;
using IdentityServer4.Models;
using IdentityServer4.ResponseHandling;
using System.Threading.Tasks;

namespace Abc.IdentityServer4.WsFederation.ResponseProcessing
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
        Task<InteractionResponse> ProcessInteractionAsync(ValidatedWsFederationRequest request, ConsentResponse consent = null);
    }
}
