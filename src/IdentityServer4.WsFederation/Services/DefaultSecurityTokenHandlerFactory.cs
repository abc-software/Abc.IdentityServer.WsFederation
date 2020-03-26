using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Saml;
using Microsoft.IdentityModel.Tokens.Saml2;
using System;

namespace IdentityServer4.WsFederation.Services {
    public class DefaultSecurityTokenHandlerFactory : ISecurityTokenHandlerFactory {
        /// <inheritdoc/>
        public SecurityTokenHandler CreateHandler(string tokenType) {
            switch (tokenType) {
                case WsFederationConstants.TokenTypes.Saml11TokenProfile11:
                    return new SamlSecurityTokenHandler();
                case WsFederationConstants.TokenTypes.Saml2TokenProfile11:
                    return new Saml2SecurityTokenHandler();
                default:
                    throw new NotImplementedException($"TokenType: {tokenType} not implemented");
            }
        }
    }
}
