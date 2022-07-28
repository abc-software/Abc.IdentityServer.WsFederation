using Abc.IdentityServer4.WsFederation.Validation;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Threading.Tasks;

namespace Abc.IdentityServer4.WsFederation.ResponseProcessing
{
    public interface ISignInResponseGenerator
    {
        Task<WsFederationMessage> GenerateResponseAsync(WsFederationValidationResult validationResult);
    }
}