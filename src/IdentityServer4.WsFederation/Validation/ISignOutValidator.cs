using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation.Validation
{
    public interface ISignOutValidator
    {
        Task<SignOutValidationResult> ValidateAsync(WsFederationMessage message);
    }
}