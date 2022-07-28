using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Abc.IdentityServer4.WsFederation.Validation
{
    /// <summary>
    /// WS-Federation endpoint request validator.
    /// </summary>
    public interface IWsFederationRequestValidator
    {
        /// <summary>
        /// Validates the sign in request.
        /// </summary>
        /// <param name="message">The WS-Federation message.</param>
        /// <param name="user">The user.</param>
        /// <returns></returns>
        Task<WsFederationValidationResult> ValidateSignInRequestAsync(WsFederationMessage message, ClaimsPrincipal user);

        /// <summary>
        /// Validates the sign out request.
        /// </summary>
        /// <param name="message">The WS-Federation message.</param>
        /// <returns></returns>
        Task<WsFederationValidationResult> ValidateSignOutRequestAsync(WsFederationMessage message);
    }
}