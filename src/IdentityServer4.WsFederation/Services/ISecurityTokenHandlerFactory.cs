using Microsoft.IdentityModel.Tokens;

namespace IdentityServer4.WsFederation.Services {
    public interface ISecurityTokenHandlerFactory {
        SecurityTokenHandler CreateHandler(string tokenType);
    }
}
