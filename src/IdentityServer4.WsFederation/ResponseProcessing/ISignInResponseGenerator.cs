using IdentityServer4.WsFederation.Validation;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation
{
    public interface ISignInResponseGenerator
    {
        Task<WsFederationMessage> GenerateResponseAsync(SignInValidationResult validationResult);
    }
}